using System;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>구조 단계별 경고음을 재생하고 카메라 흔들림 요청을 분리해 전달한다.</summary>
    public sealed class StructuralRiskFeedback : MonoBehaviour
    {
        [SerializeField] private StructuralIntegritySystem structuralSystem;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource explorationBgmSource;
        [SerializeField] private AudioSource dangerBgmSource;
        [SerializeField] private AudioClip warningTone;
        [SerializeField] private bool reduceMotion;
        [SerializeField] private PlayerCameraFollow cameraFollow;

        private AudioClip runtimeWarningTone;

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

            if (risk == StructuralRiskLevel.Stable)
            {
                return;
            }

            if (audioSource != null)
            {
                audioSource.pitch = GetPitch(risk);
                audioSource.PlayOneShot(ResolveWarningTone());
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

        private AudioClip ResolveWarningTone()
        {
            if (warningTone != null) return warningTone;
            if (runtimeWarningTone != null) return runtimeWarningTone;

            const int sampleRate = 22050;
            const int sampleCount = 2205;
            var samples = new float[sampleCount];
            for (int index = 0; index < sampleCount; index++)
            {
                float envelope = 1f - index / (float)sampleCount;
                samples[index] = Mathf.Sin(2f * Mathf.PI * 440f * index / sampleRate)
                    * envelope * 0.18f;
            }

            runtimeWarningTone = AudioClip.Create(
                "StructuralWarningTone",
                sampleCount,
                1,
                sampleRate,
                false);
            runtimeWarningTone.SetData(samples, 0);
            return runtimeWarningTone;
        }

        private void OnDestroy()
        {
            if (runtimeWarningTone == null) return;
            if (Application.isPlaying) Destroy(runtimeWarningTone);
            else DestroyImmediate(runtimeWarningTone);
        }
    }
}
