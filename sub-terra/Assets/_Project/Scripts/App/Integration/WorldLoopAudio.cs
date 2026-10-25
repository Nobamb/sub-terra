using System;
using SubTerra.App.Audio;
using SubTerra.App.UI;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Integration
{
    public enum WorldLoopAction
    {
        Hold,
        Start,
        FadeOut,
        Stop
    }

    /// <summary>
    /// B-164: 반복 소리 켜고 끄기 판정. 켜짐 조건이 참이고 일시정지·실패 처리·행동불능이 모두 아닐 때만 소리가 난다.
    /// 막히면 바로 멈추고, 풀린 뒤 조건이 남아 있으면 다시 낸다. 조건만 꺼지면 짧게 줄이며 끝낸다.
    /// </summary>
    public sealed class WorldLoopGate
    {
        public bool IsOn { get; private set; }

        public WorldLoopAction Evaluate(bool wanted, bool paused, bool failureHandling, bool canAct)
        {
            if (paused || failureHandling || !canAct)
            {
                return Halt();
            }

            if (wanted)
            {
                if (IsOn)
                {
                    return WorldLoopAction.Hold;
                }

                IsOn = true;
                return WorldLoopAction.Start;
            }

            if (!IsOn)
            {
                return WorldLoopAction.Hold;
            }

            IsOn = false;
            return WorldLoopAction.FadeOut;
        }

        /// <summary>OnDisable·씬 언로드·음소거. 바로 멈춘다.</summary>
        public WorldLoopAction Halt()
        {
            if (!IsOn)
            {
                return WorldLoopAction.Hold;
            }

            IsOn = false;
            return WorldLoopAction.Stop;
        }
    }

    /// <summary>
    /// B-164: 드릴·엘리베이터 승강처럼 조건이 이어지는 동안 나는 반복 소리. 원샷 풀(SfxPlayer)과 따로 AudioSource 하나만 쓴다.
    /// 음량은 칸 음량 × 버스 × 효과음 설정이고, 음소거면 멈춘다. 씬 오브젝트에 붙여 씬과 함께 사라진다.
    /// </summary>
    public sealed class WorldLoopAudio : MonoBehaviour
    {
        public const float FadeInSeconds = 0.05f;
        public const float FadeOutSeconds = 0.08f;

        private readonly WorldLoopGate gate = new WorldLoopGate();
        private SfxId loopId;
        private Func<bool> isWanted = () => false;
        private Func<bool> isFailureHandling = () => false;
        private Func<bool> canAct = () => true;
        // B-165: 광산 ESC 설정창은 UiPauseGate를 잡지 않으므로 설정창 열림도 정지로 본다.
        private Func<bool> isPaused = () => UiPauseGate.IsHeld || ControlPreferences.IsSettingsOpen;
        private Func<bool> isMuted = () => SettingsRuntimeApplier.IsMasterMuted
            || SettingsRuntimeApplier.IsMuted(AudioChannel.Sfx);
        private SfxLibrary library;
        private AudioSource source;
        private float fade;
        private bool fadingOut;
        private bool playing;
        private bool errorLogged;

        public SfxId LoopId => loopId;
        /// <summary>반복 소리를 내고 있는가(줄어드는 중 포함).</summary>
        public bool IsPlaying => playing;
        public bool IsOn => gate.IsOn;
        public float Fade => fade;
        public AudioSource Source => source;

        /// <summary>host에 같은 칸 반복 소리가 있으면 다시 설정만 하고, 없으면 붙인다.</summary>
        public static WorldLoopAudio Attach(
            GameObject host,
            SfxId id,
            Func<bool> wanted,
            Func<bool> failureHandling,
            Func<bool> playerCanAct)
        {
            if (host == null)
            {
                return null;
            }

            WorldLoopAudio loop = null;
            foreach (var existing in host.GetComponents<WorldLoopAudio>())
            {
                if (existing.loopId == id)
                {
                    loop = existing;
                    break;
                }
            }

            if (loop == null)
            {
                loop = host.AddComponent<WorldLoopAudio>();
            }

            loop.Configure(id, wanted, failureHandling, playerCanAct);
            return loop;
        }

        public void Configure(SfxId id, Func<bool> wanted, Func<bool> failureHandling, Func<bool> playerCanAct)
        {
            loopId = id;
            isWanted = wanted ?? (() => false);
            isFailureHandling = failureHandling ?? (() => false);
            canAct = playerCanAct ?? (() => true);
        }

        /// <summary>테스트용. 라이브러리·음소거·정지 판정을 주입한다.</summary>
        public void ConfigureForTests(SfxLibrary testLibrary, Func<bool> testMuted, Func<bool> testPaused)
        {
            library = testLibrary;
            if (testMuted != null) isMuted = testMuted;
            if (testPaused != null) isPaused = testPaused;
        }

        private void OnDisable()
        {
            gate.Halt();
            StopNow();
        }

        // 같은 프레임에 대상 블록이 바뀌어 Cancelled 뒤 바로 Mining이 와도 프레임 끝 상태만 보므로 끊기지 않는다.
        private void LateUpdate()
        {
            try
            {
                Tick(Time.unscaledDeltaTime);
            }
            catch (Exception exception)
            {
                gate.Halt();
                StopNow();
                if (!errorLogged)
                {
                    errorLogged = true;
                    Debug.LogWarning("[WorldLoopAudio] " + loopId + " 반복 소리 갱신 실패: " + exception.Message);
                }
            }
        }

        /// <summary>테스트와 LateUpdate가 같은 경로로 계산한다.</summary>
        public void Tick(float deltaTime)
        {
            if (isMuted() || !TryResolve(out var entry, out var busVolume))
            {
                gate.Halt();
                StopNow();
                return;
            }

            switch (gate.Evaluate(isWanted(), isPaused(), isFailureHandling(), canAct()))
            {
                case WorldLoopAction.Start:
                    Begin(entry);
                    break;
                case WorldLoopAction.FadeOut:
                    fadingOut = true;
                    break;
                case WorldLoopAction.Stop:
                    StopNow();
                    return;
            }

            if (!playing)
            {
                return;
            }

            var step = Mathf.Max(0f, deltaTime);
            fade = fadingOut
                ? Mathf.Max(0f, fade - step / FadeOutSeconds)
                : Mathf.Min(1f, fade + step / FadeInSeconds);
            if (fadingOut && fade <= 0f)
            {
                StopNow();
                return;
            }

            source.volume = entry.Volume * busVolume * SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx) * fade;
        }

        private void Begin(SfxEntry entry)
        {
            EnsureSource();
            fadingOut = false;
            // 줄어드는 중에 다시 켜지면 처음부터 틀지 않고 음량만 되돌린다.
            if (playing && source.clip == entry.Clip)
            {
                return;
            }

            fade = 0f;
            source.clip = entry.Clip;
            source.volume = 0f;
            source.Play();
            playing = true;
        }

        private void StopNow()
        {
            fade = 0f;
            fadingOut = false;
            if (!playing)
            {
                return;
            }

            playing = false;
            if (source != null)
            {
                source.Stop();
            }
        }

        private bool TryResolve(out SfxEntry entry, out float busVolume)
        {
            entry = null;
            busVolume = 1f;
            if (loopId == SfxId.None)
            {
                return false;
            }

            if (library == null)
            {
                library = SfxPlayer.Instance != null
                    ? SfxPlayer.Instance.Library
                    : Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            }

            if (library == null || !library.TryGetEntry(loopId, out entry) || entry.Clip == null)
            {
                return false;
            }

            busVolume = library.GetBusVolume(entry.Bus);
            return true;
        }

        private void EnsureSource()
        {
            if (source != null)
            {
                return;
            }

            var child = new GameObject("WorldLoop_" + loopId);
            child.transform.SetParent(transform, false);
            source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.priority = 128;
        }
    }
}
