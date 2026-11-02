using System;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Audio
{
    /// <summary>
    /// B-170: 메인 화면 부팅 켜짐 소리(MainMenuBoot). 건너뛰면 줄여 멈춰야 해서 SfxPlayer 풀 대신
    /// 전용 AudioSource 하나를 쓴다(B-166: SfxPlayer에 멈춤 API 없음, WindowTvSfx 방식).
    /// 음량은 칸 음량 × 버스 × 효과음 설정, 음소거면 무음이다.
    /// </summary>
    public static class MainMenuBootSfx
    {
        public const float SkipFadeSeconds = 0.15f;

        private static AudioSource source;
        private static GameObject host;
        private static SfxLibrary library;
        private static Func<bool> isMuted = DefaultMuted;
        private static bool configuredForTests;
        private static float baseVolume;
        private static float fadeDuration;
        private static float fadeLeft;
        private static bool playing;

        /// <summary>실제로 재생을 시작한 횟수(음소거·빈 칸은 세지 않는다).</summary>
        public static int StartedCount { get; private set; }
        public static int FadeCount { get; private set; }
        public static bool IsFading => fadeLeft > 0f;
        public static AudioSource Source => source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            source = null;
            host = null;
            library = null;
            isMuted = DefaultMuted;
            configuredForTests = false;
            ClearState();
        }

        /// <returns>재생을 시작했으면 true.</returns>
        public static bool Play()
        {
            if (!Application.isPlaying && !configuredForTests)
            {
                return false;
            }

            try
            {
                if (isMuted() || !TryResolve(out var entry, out var busVolume))
                {
                    return false;
                }

                EnsureSource();
                source.Stop();
                source.clip = entry.Clip;
                source.pitch = Mathf.Clamp(entry.Pitch, SfxPlayback.MinPitch, SfxPlayback.MaxPitch);
                baseVolume = entry.Volume * busVolume * SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx);
                source.volume = baseVolume;
                source.time = Mathf.Clamp(entry.StartTime, 0f, Mathf.Max(0f, entry.Clip.length - 0.01f));
                source.Play();
                fadeLeft = 0f;
                playing = true;
                StartedCount++;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[MainMenuBootSfx] 재생 실패: " + exception.Message);
                return false;
            }
        }

        /// <summary>건너뛰기: 남은 소리를 seconds 동안 줄여 멈춘다. 재생 중이 아니면 무시.</summary>
        public static void FadeOut(float seconds = SkipFadeSeconds)
        {
            // 테스트(에디트 모드)는 AudioSource가 실제로 돌지 않아 자체 상태로만 본다.
            if (source == null || !playing || fadeLeft > 0f || (!configuredForTests && !source.isPlaying))
            {
                return;
            }

            fadeDuration = Mathf.Max(0.01f, seconds);
            fadeLeft = fadeDuration;
            FadeCount++;
        }

        /// <summary>러너(플레이)나 테스트가 비스케일 시간으로 진행한다.</summary>
        public static void Tick(float deltaSeconds)
        {
            if (source == null || fadeLeft <= 0f)
            {
                return;
            }

            fadeLeft = Mathf.Max(0f, fadeLeft - Mathf.Max(0f, deltaSeconds));
            source.volume = baseVolume * (fadeLeft / fadeDuration);
            if (fadeLeft <= 0f)
            {
                source.Stop();
                playing = false;
            }
        }

        /// <summary>테스트용. 라이브러리·음소거를 주입하고 기록을 비운다.</summary>
        public static void ResetForTests(SfxLibrary testLibrary, Func<bool> testMuted)
        {
            ReleaseForTests();
            library = testLibrary;
            isMuted = testMuted ?? DefaultMuted;
            configuredForTests = true;
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
            isMuted = DefaultMuted;
            configuredForTests = false;
            ClearState();
        }

        private static void ClearState()
        {
            StartedCount = 0;
            FadeCount = 0;
            baseVolume = 0f;
            fadeDuration = 0f;
            fadeLeft = 0f;
            playing = false;
        }

        private static bool TryResolve(out SfxEntry entry, out float busVolume)
        {
            entry = null;
            busVolume = 1f;
            if (library == null)
            {
                library = SfxPlayer.Instance != null
                    ? SfxPlayer.Instance.Library
                    : Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            }

            if (library == null || !library.TryGetEntry(SfxId.MainMenuBoot, out entry) || entry == null || entry.Clip == null)
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

            host = new GameObject("MainMenuBootSfx");
            if (Application.isPlaying)
            {
                if (SfxPlayer.Instance != null)
                {
                    host.transform.SetParent(SfxPlayer.Instance.transform, false);
                }
                else
                {
                    UnityEngine.Object.DontDestroyOnLoad(host);
                }

                host.AddComponent<Runner>();
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
            source.ignoreListenerPause = true;
        }

        private static bool DefaultMuted() => SettingsRuntimeApplier.IsMasterMuted
            || SettingsRuntimeApplier.IsMuted(AudioChannel.Sfx);

        /// <summary>메인 화면 연출이 꺼져도 줄이기가 끝까지 가도록 소리 쪽에서 시간을 돌린다.</summary>
        private sealed class Runner : MonoBehaviour
        {
            private void Update()
            {
                Tick(Time.unscaledDeltaTime);
            }
        }
    }
}
