using System;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>구조 단계별 경고음을 재생하고 카메라 흔들림 요청을 분리해 전달한다.</summary>
    public sealed class StructuralRiskFeedback : MonoBehaviour
    {
        [SerializeField] private StructuralIntegritySystem structuralSystem;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip warningTone;
        [SerializeField] private bool reduceMotion;

        private AudioClip runtimeWarningTone;

        public event Action<StructuralRiskLevel> CameraShakeRequested;

        private void OnEnable()
        {
            if (structuralSystem != null) structuralSystem.RiskChanged += OnRiskChanged;
        }

        private void OnDisable()
        {
            if (structuralSystem != null) structuralSystem.RiskChanged -= OnRiskChanged;
        }

        private void OnRiskChanged(StructuralRiskLevel risk)
        {
            if (risk == StructuralRiskLevel.Stable) return;

            if (audioSource != null)
            {
                audioSource.pitch = GetPitch(risk);
                audioSource.PlayOneShot(ResolveWarningTone());
            }

            if (ShouldRequestCameraShake(risk, reduceMotion || AccessibilityPreferences.ReduceMotion))
                CameraShakeRequested?.Invoke(risk);
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
