using System;
using UnityEngine;

namespace SubTerra.App.Progression
{
    /// <summary>
    /// B-155 업그레이드 창 심층 구역 썸네일의 반짝임·해금 연출 값.
    /// DeepZoneDisplaySettings 에셋 인스펙터에서 조정한다(코드 기본값은 기획서 값).
    /// </summary>
    [Serializable]
    public sealed class DeepZoneThumbnailStyle
    {
        [Header("Glint")]
        [Tooltip("반짝임 한 주기(초). 2초 미만으로 내리지 않는다.")]
        [Min(2f)] public float glintPeriod = 2.6f;
        [Tooltip("칸마다 주기를 이만큼 비율로 늘려 시간이 지나도 세 장이 함께 번쩍이지 않게 한다.")]
        [Range(0f, 0.3f)] public float glintPeriodSpread = 0.07f;
        public Color lockedGlintColor = new Color(0.8f, 0.85f, 0.88f, 1f);
        public Color unlockedGlintColor = Color.white;
        public Vector2 lockedGlintAlpha = new Vector2(0.15f, 0.45f);
        public Vector2 unlockedGlintAlpha = new Vector2(0.6f, 0.9f);

        [Header("Grayscale")]
        [Tooltip("잠김 흑백 밝기 배율.")]
        [Range(0.2f, 1.2f)] public float lockedGrayBrightness = 0.6f;
        [Tooltip("잠김 반짝임의 흑백 밝기 배율. 바탕보다 밝아야 광물이 보인다.")]
        [Range(0.2f, 1.5f)] public float glintGrayBrightness = 1.1f;
        [Tooltip("셰이더를 못 읽었을 때 잠김 칸 색.")]
        public Color fallbackLockedTint = new Color(0.45f, 0.45f, 0.45f, 1f);

        [Header("Unlock")]
        [Min(0.2f)] public float unlockDuration = 1f;
        [Tooltip("자물쇠 자리(칸 안 UV). 색은 여기서부터 퍼진다.")]
        public Vector2 lockCenter = new Vector2(0.5f, 0.66f);
        [Min(0.1f)] public float revealRadius = 1.2f;
        [Range(0.01f, 0.5f)] public float revealFeather = 0.18f;
        public float shackleKick = 6f;
        public float lockDrop = 40f;
        public float lockDropAngle = 25f;

        [Header("Fallback")]
        [Tooltip("그림이 없을 때 쓰는 레벨 1 단색. 레벨이 오를수록 fallbackDarkenPerLevel만큼 어두워진다.")]
        public Color fallbackColor = new Color(0.09f, 0.14f, 0.24f, 1f);
        [Range(0f, 0.4f)] public float fallbackDarkenPerLevel = 0.22f;

        public Color FallbackColorFor(int levelIndex)
        {
            var scale = Mathf.Max(0.2f, 1f - fallbackDarkenPerLevel * Mathf.Max(0, levelIndex));
            return new Color(fallbackColor.r * scale, fallbackColor.g * scale, fallbackColor.b * scale, 1f);
        }
    }
}
