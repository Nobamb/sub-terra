using System;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>구조 단계별 경고음을 재생하고 카메라 흔들림 요청을 분리해 전달한다.</summary>
    public sealed class StructuralRiskFeedback : MonoBehaviour
    {
        [SerializeField] private StructuralIntegritySystem structuralSystem;
        // 경고음은 SfxRequests로 옮겼다. PhaseG 셋업이 연결하는 필드라 남겨 둔다.
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource explorationBgmSource;
        [SerializeField] private AudioSource dangerBgmSource;
        [SerializeField] private bool reduceMotion;
        [SerializeField] private PlayerCameraFollow cameraFollow;

        private StructuralRiskLevel lastRisk = StructuralRiskLevel.Stable;

        public event Action<StructuralRiskLevel> CameraShakeRequested;

        private void OnEnable()
        {
            if (structuralSystem != null)
            {
                structuralSystem.RiskChanged += OnRiskChanged;
                structuralSystem.CollapseTriggered += OnCollapseTriggered;
            }
        }

        private void OnDisable()
        {
            if (structuralSystem != null)
            {
                structuralSystem.RiskChanged -= OnRiskChanged;
                structuralSystem.CollapseTriggered -= OnCollapseTriggered;
            }
        }

        private void OnRiskChanged(StructuralRiskLevel risk)
        {
            UpdateDangerBgm(risk);

            var previous = lastRisk;
            lastRisk = risk;
            PlayRiskSfx(previous, risk);

            if (risk == StructuralRiskLevel.Stable)
            {
                return;
            }

            if (ShouldRequestCameraShake(risk, reduceMotion || AccessibilityPreferences.ReduceMotion))
            {
                CameraShakeRequested?.Invoke(risk);
                ApplyCameraShake(ResolveShakeAmplitude(risk), ResolveShakeDuration(risk));
            }
        }

        private void OnCollapseTriggered(StructuralCollapseEventDto collapse)
        {
            if (explorationBgmSource != null && explorationBgmSource.isPlaying)
            {
                explorationBgmSource.Pause();
            }

            if (dangerBgmSource != null && !dangerBgmSource.isPlaying)
            {
                dangerBgmSource.Play();
            }

            if (collapse == null || AccessibilityPreferences.ReduceMotion || reduceMotion)
            {
                return;
            }

            // 실제 붕괴는 위험 단계 상승보다 강한 흔들림을 준다.
            CameraShakeRequested?.Invoke(StructuralRiskLevel.CollapseImminent);
            ApplyCameraShake(0.38f, 0.4f);
        }

        /// <summary>오를 때만 경보, 내릴 때는 틱 한 번, 같은 단계면 무음.</summary>
        private static void PlayRiskSfx(StructuralRiskLevel previous, StructuralRiskLevel risk)
        {
            switch (ResolveRiskSound(previous, risk))
            {
                case RiskSound.Alarm:
                    SfxRequests.PlayAlarm(
                        SfxId.AlarmMineWail,
                        ResolveAlarmRepeat(risk),
                        ResolveAlarmInterval(risk),
                        GetPitch(risk));
                    break;
                case RiskSound.Tick:
                    SfxRequests.Play(SfxId.AlertRiskTick);
                    break;
            }
        }

        public enum RiskSound
        {
            None = 0,
            Alarm = 1,
            Tick = 2
        }

        public static RiskSound ResolveRiskSound(StructuralRiskLevel previous, StructuralRiskLevel risk)
        {
            if (risk > previous) return RiskSound.Alarm;
            return risk < previous ? RiskSound.Tick : RiskSound.None;
        }

        public static int ResolveAlarmRepeat(StructuralRiskLevel risk)
        {
            return risk == StructuralRiskLevel.CollapseImminent ? 3
                : risk == StructuralRiskLevel.Danger ? 2
                : 1;
        }

        public static float ResolveAlarmInterval(StructuralRiskLevel risk)
        {
            return risk == StructuralRiskLevel.CollapseImminent ? 0.24f
                : risk == StructuralRiskLevel.Danger ? 0.34f
                : 0f;
        }

        public static float GetPitch(StructuralRiskLevel risk)
        {
            return risk == StructuralRiskLevel.Caution
                ? 0.85f
                : risk == StructuralRiskLevel.Danger ? 1.05f : 1.3f;
        }

        public static bool ShouldRequestCameraShake(StructuralRiskLevel risk, bool reduceMotion)
        {
            return !reduceMotion && risk >= StructuralRiskLevel.Danger;
        }

        public static float ResolveShakeAmplitude(StructuralRiskLevel risk)
        {
            return risk == StructuralRiskLevel.CollapseImminent ? 0.32f
                : risk == StructuralRiskLevel.Danger ? 0.22f
                : 0.12f;
        }

        public static float ResolveShakeDuration(StructuralRiskLevel risk)
        {
            return risk == StructuralRiskLevel.CollapseImminent ? 0.38f
                : risk == StructuralRiskLevel.Danger ? 0.28f
                : 0.18f;
        }

        private void ApplyCameraShake(float amplitude, float duration)
        {
            PlayerCameraFollow follow = cameraFollow;
            if (follow == null)
            {
                Camera main = Camera.main;
                if (main != null)
                {
                    follow = main.GetComponent<PlayerCameraFollow>();
                }
            }

            follow?.RequestShake(amplitude, duration);
        }

        private void UpdateDangerBgm(StructuralRiskLevel risk)
        {
            if (dangerBgmSource == null)
            {
                return;
            }

            if (risk >= StructuralRiskLevel.Danger)
            {
                if (explorationBgmSource != null && explorationBgmSource.isPlaying)
                {
                    explorationBgmSource.Pause();
                }

                if (!dangerBgmSource.isPlaying)
                {
                    dangerBgmSource.Play();
                }
            }
            else if (dangerBgmSource.isPlaying)
            {
                dangerBgmSource.Pause();

                if (explorationBgmSource != null)
                {
                    explorationBgmSource.UnPause();
                }
            }
        }
    }
}
