using System;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Audio
{
    /// <summary>
    /// B-169: 창 TV 켜짐·꺼짐 소리(WindowTvOpen·WindowTvClose). UI 버스 AudioSource 하나만 써서
    /// 새 소리가 앞 소리를 끊고 처음부터 낸다(연달아 열면 끊고 다시). SfxPlayer 풀을 쓰지 않으므로 멈춤 API가 필요 없다(B-166).
    /// 음량은 칸 음량 × 버스 × 효과음 설정, 음소거면 무음이다.
    /// </summary>
    public static class WindowTvSfx
    {
        private static AudioSource source;
        private static GameObject host;
        private static SfxLibrary library;
        private static Func<float> clock = DefaultClock;
        private static Func<bool> isMuted = DefaultMuted;
        private static float lastStartedAt;
        private static float lastEndsAt;
        private static bool configuredForTests;

        /// <summary>실제로 재생을 시작한 횟수(음소거·빈 칸은 세지 않는다).</summary>
        public static int StartedCount { get; private set; }
        /// <summary>앞 소리가 아직 나는 중에 새 소리로 끊은 횟수.</summary>
        public static int InterruptedCount { get; private set; }
        public static SfxId LastId { get; private set; }
        public static AudioSource Source => source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            source = null;
            host = null;
            library = null;
            clock = DefaultClock;
            isMuted = DefaultMuted;
            configuredForTests = false;
            ClearCounters();
        }

        /// <returns>재생을 시작했으면 true.</returns>
        public static bool Play(SfxId id)
        {
            if (id != SfxId.WindowTvOpen && id != SfxId.WindowTvClose)
            {
                return false;
            }

            // 에디트 모드(다른 EditMode 검사가 창을 열 때)는 검사가 주입했을 때만 낸다. 숨은 오브젝트를 남기지 않는다.
            if (!Application.isPlaying && !configuredForTests)
            {
                return false;
            }

            try
            {
                if (isMuted() || !TryResolve(id, out var entry, out var busVolume))
                {
                    return false;
                }

                EnsureSource();
                var now = clock();
                if (StartedCount > 0 && now < lastEndsAt)
                {
                    InterruptedCount++;
                }

                var pitch = Mathf.Clamp(entry.Pitch, SfxPlayback.MinPitch, SfxPlayback.MaxPitch);
                var plan = SfxPlayback.Plan(entry.Clip.length, entry.StartTime, entry.MaxDuration, entry.FadeOut, pitch);
                source.Stop();
                source.clip = entry.Clip;
                source.pitch = pitch;
                source.volume = entry.Volume * busVolume * SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx);
                source.time = plan.StartTime;
                source.Play();

                LastId = id;
                lastStartedAt = now;
                lastEndsAt = now + plan.RealDuration;
                StartedCount++;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[WindowTvSfx] 재생 실패: " + exception.Message);
                return false;
            }
        }

        /// <summary>테스트용. 라이브러리·시계·음소거를 주입하고 기록을 비운다. null이면 실제 값을 쓴다.</summary>
        public static void ResetForTests(SfxLibrary testLibrary, Func<float> testClock, Func<bool> testMuted)
        {
            ReleaseForTests();
            library = testLibrary;
            clock = testClock ?? DefaultClock;
            isMuted = testMuted ?? DefaultMuted;
            configuredForTests = true;
            ClearCounters();
        }

        public static void ReleaseForTests()
        {
            if (host != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(host);
                else UnityEngine.Object.DestroyImmediate(host);
            }

            host = null;
            source = null;
            library = null;
            clock = DefaultClock;
            isMuted = DefaultMuted;
            configuredForTests = false;
            ClearCounters();
        }

        private static void ClearCounters()
        {
            StartedCount = 0;
            InterruptedCount = 0;
            LastId = SfxId.None;
            lastStartedAt = 0f;
            lastEndsAt = 0f;
        }

        private static bool TryResolve(SfxId id, out SfxEntry entry, out float busVolume)
        {
            entry = null;
            busVolume = 1f;
            if (library == null)
            {
                library = SfxPlayer.Instance != null
                    ? SfxPlayer.Instance.Library
                    : Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            }

            if (library == null || !library.TryGetEntry(id, out entry) || entry == null || entry.Clip == null)
            {
                return false;
            }

            busVolume = library.GetBusVolume(entry.Bus);
            return true;
        }

        private static void EnsureSource()
        {
            if (source != null)
            {
                return;
            }

            host = new GameObject("WindowTvSfx");
            if (Application.isPlaying)
            {
                // SfxPlayer처럼 씬을 넘어 유지한다. 부모가 있으면 함께 따라간다.
                if (SfxPlayer.Instance != null)
                {
                    host.transform.SetParent(SfxPlayer.Instance.transform, false);
                }
                else
                {
                    UnityEngine.Object.DontDestroyOnLoad(host);
                }
            }
            else
            {
                host.hideFlags = HideFlags.DontSave;
            }

            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.priority = 128;
            // 일시정지 중에 연 창도 소리가 난다.
            source.ignoreListenerPause = true;
        }

        private static float DefaultClock() => Time.realtimeSinceStartup;

        private static bool DefaultMuted() => SettingsRuntimeApplier.IsMasterMuted
            || SettingsRuntimeApplier.IsMuted(AudioChannel.Sfx);
    }
}
