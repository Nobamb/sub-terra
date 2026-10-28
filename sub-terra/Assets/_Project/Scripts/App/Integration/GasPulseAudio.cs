using System;
using SubTerra.App.Audio;
using SubTerra.App.Core;
using SubTerra.App.State;
using SubTerra.App.UI;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// B-168: 가스 맥박 판정. 우세 단계의 위상이 0.5(비네팅이 가장 밝은 순간)를 넘는 프레임에 한 번 낸다.
    /// 막히거나 단계가 바뀌면 기준 위상을 새로 잡아, 풀린 뒤 다음 맥박부터 낸다.
    /// </summary>
    public sealed class GasPulseGate
    {
        public const float PeakPhase = 0.5f;
        public const float CautionPitch = 1f;
        public const float CriticalPitch = 1.08f;

        private bool hasLast;
        private GasRiskLevel lastLevel;
        private float lastPhase;

        public bool Observe(bool blocked, GasRiskLevel dominant, float phase)
        {
            if (blocked || dominant == GasRiskLevel.Safe)
            {
                hasLast = false;
                return false;
            }

            if (!hasLast || lastLevel != dominant)
            {
                hasLast = true;
                lastLevel = dominant;
                lastPhase = phase;
                return false;
            }

            var crossed = CrossedPeak(lastPhase, phase);
            lastPhase = phase;
            return crossed;
        }

        public void Reset()
        {
            hasLast = false;
        }

        public static float PitchFor(GasRiskLevel level)
        {
            return level == GasRiskLevel.Hazard ? CriticalPitch : CautionPitch;
        }

        // 위상은 0~1을 돈다. 한 프레임 이동은 한 주기보다 짧으므로(View가 0.1초로 자른다) 넘김은 많아야 한 번이다.
        private static bool CrossedPeak(float previous, float current)
        {
            if (current >= previous)
            {
                return previous < PeakPhase && current >= PeakPhase;
            }

            return previous < PeakPhase || current >= PeakPhase;
        }
    }

    /// <summary>
    /// B-168: 가스 구역 맥박 "두-둥". GasPoisonOverlayView 옆에 붙어 타임라인 위상을 읽는다.
    /// 원샷 풀과 따로 AudioSource 하나를 쓰고, 맥박마다 처음부터 다시 튼다(쌓이지 않는다).
    /// 음량은 칸 × 버스 × 효과음 설정, 멈춤 규칙은 WorldLoopGate에 엘리베이터 탑승·런 종료·가스 밖을 더한 것.
    /// </summary>
    public sealed class GasPulseAudio : MonoBehaviour
    {
        public const float FadeOutSeconds = 0.08f;

        private readonly GasPulseGate gate = new GasPulseGate();
        private GasPoisonOverlayView overlay;
        private Func<bool> isFailureHandling = () => false;
        private Func<bool> canAct = () => true;
        private Func<bool> isRiding = () => false;
        private Func<bool> isPaused = () => UiPauseGate.IsHeld || ControlPreferences.IsSettingsOpen;
        private Func<bool> isRunEnding = DefaultRunEnding;
        private Func<bool> isMuted = () => SettingsRuntimeApplier.IsMasterMuted
            || SettingsRuntimeApplier.IsMuted(AudioChannel.Sfx);
        private SfxLibrary library;
        private AudioSource source;
        private float fade;
        private bool fadingOut;
        private bool errorLogged;

        public int PulseCount { get; private set; }
        public bool IsPlaying => source != null && source.isPlaying;
        public bool IsFadingOut => fadingOut;
        public AudioSource Source => source;

        public static GasPulseAudio Attach(
            GasPoisonOverlayView view,
            Func<bool> failureHandling,
            Func<bool> playerCanAct,
            Func<bool> elevatorRiding)
        {
            if (view == null)
            {
                return null;
            }

            var pulse = view.GetComponent<GasPulseAudio>();
            if (pulse == null)
            {
                pulse = view.gameObject.AddComponent<GasPulseAudio>();
            }

            pulse.Configure(view, failureHandling, playerCanAct, elevatorRiding);
            return pulse;
        }

        public void Configure(
            GasPoisonOverlayView view,
            Func<bool> failureHandling,
            Func<bool> playerCanAct,
            Func<bool> elevatorRiding)
        {
            overlay = view;
            isFailureHandling = failureHandling ?? (() => false);
            canAct = playerCanAct ?? (() => true);
            isRiding = elevatorRiding ?? (() => false);
        }

        /// <summary>테스트용. 라이브러리·음소거·정지·런 종료 판정을 주입한다.</summary>
        public void ConfigureForTests(SfxLibrary testLibrary, Func<bool> testMuted, Func<bool> testPaused, Func<bool> testRunEnding)
        {
            library = testLibrary;
            if (testMuted != null) isMuted = testMuted;
            if (testPaused != null) isPaused = testPaused;
            if (testRunEnding != null) isRunEnding = testRunEnding;
        }

        private void OnDisable()
        {
            gate.Reset();
            StopNow();
        }

        // View가 Update에서 타임라인을 흘린 뒤의 위상을 본다.
        private void LateUpdate()
        {
            try
            {
                Tick(Time.unscaledDeltaTime);
            }
            catch (Exception exception)
            {
                gate.Reset();
                StopNow();
                if (!errorLogged)
                {
                    errorLogged = true;
                    Debug.LogWarning("[GasPulseAudio] 가스 맥박 갱신 실패: " + exception.Message);
                }
            }
        }

        /// <summary>테스트와 LateUpdate가 같은 경로로 계산한다.</summary>
        public void Tick(float deltaTime)
        {
            var timeline = overlay != null ? overlay.Timeline : null;
            if (timeline == null || isMuted() || !TryResolve(out var entry, out var busVolume))
            {
                gate.Reset();
                StopNow();
                return;
            }

            var dominant = timeline.DominantLevel;
            var blocked = timeline.Target == GasRiskLevel.Safe
                || isPaused()
                || isFailureHandling()
                || !canAct()
                || isRiding()
                || isRunEnding();
            if (gate.Observe(blocked, dominant, timeline.DominantPhase))
            {
                Pulse(entry, busVolume, GasPulseGate.PitchFor(dominant));
            }
            else if (blocked && IsPlaying)
            {
                fadingOut = true;
            }

            if (!IsPlaying)
            {
                return;
            }

            if (fadingOut)
            {
                fade = Mathf.Max(0f, fade - Mathf.Max(0f, deltaTime) / FadeOutSeconds);
                if (fade <= 0f)
                {
                    StopNow();
                    return;
                }
            }

            source.volume = entry.Volume * busVolume * SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx) * fade;
        }

        private void Pulse(SfxEntry entry, float busVolume, float pitch)
        {
            EnsureSource();
            source.Stop();
            source.clip = entry.Clip;
            source.pitch = Mathf.Clamp(entry.Pitch * pitch, SfxPlayback.MinPitch, SfxPlayback.MaxPitch);
            source.time = 0f;
            fade = 1f;
            fadingOut = false;
            source.volume = entry.Volume * busVolume * SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx);
            source.Play();
            PulseCount++;
        }

        private void StopNow()
        {
            fade = 0f;
            fadingOut = false;
            if (source != null && source.isPlaying)
            {
                source.Stop();
            }
        }

        private bool TryResolve(out SfxEntry entry, out float busVolume)
        {
            entry = null;
            busVolume = 1f;
            if (library == null)
            {
                library = SfxPlayer.Instance != null
                    ? SfxPlayer.Instance.Library
                    : Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            }

            if (library == null || !library.TryGetEntry(SfxId.GasPulseLoop, out entry) || entry.Clip == null)
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

            var child = new GameObject("GasPulse");
            child.transform.SetParent(transform, false);
            source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.priority = 128;
        }

        private static bool DefaultRunEnding()
        {
            var root = GameBootstrapper.Instance;
            if (root == null || root.State == null || root.State.Run == null)
            {
                return false;
            }

            var phase = root.State.Run.LifecyclePhase;
            return phase == RunLifecyclePhase.Returning || phase == RunLifecyclePhase.Completed;
        }
    }
}
