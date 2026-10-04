using SubTerra.App.State;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>HUD 구조 상태 행의 표시 종류. GameState의 StructuralRiskLevel과 1:1 대응한다.</summary>
    public enum StructuralStatusKind
    {
        Safe = 0,
        Caution = 1,
        Critical = 2,
        Imminent = 3
    }

    /// <summary>표시 종류별 아이콘 번호, 문구 색, 발광 설정. 위험 판정은 하지 않는다.</summary>
    public readonly struct StructuralStatusStyle
    {
        public int IconIndex { get; }
        public Color TextColor { get; }
        public Color GlowColor { get; }
        public bool Pulses { get; }

        public StructuralStatusStyle(int iconIndex, Color textColor, Color glowColor, bool pulses)
        {
            IconIndex = iconIndex;
            TextColor = textColor;
            GlowColor = glowColor;
            Pulses = pulses;
        }
    }

    /// <summary>
    /// 구조 상태 → 표시 스타일 변환. 순수 로직이라 EditMode에서 검증한다.
    /// 단계는 GameState가 확정한 값만 따르고 새 임계값을 만들지 않는다.
    /// </summary>
    public static class StructuralStatusPresentation
    {
        public const float PulsePeriodSeconds = 2.4f;

        private static readonly Color SafeText = Color.white;
        private static readonly Color CautionText = new Color(1f, 0.78f, 0.25f);
        private static readonly Color CriticalText = new Color(1f, 0.25f, 0.2f);
        private static readonly Color ImminentText = new Color(1f, 0.08f, 0.06f);
        // 발광은 위험(Critical)·붕괴 임박에서만 쓰는 약한 붉은색. 최대 알파는 은은하게 제한한다.
        private static readonly Color CriticalGlow = new Color(1f, 0.2f, 0.16f, 0.2f);
        private static readonly Color ImminentGlow = new Color(1f, 0.12f, 0.1f, 0.28f);
        private static readonly Color NoGlow = new Color(1f, 0.2f, 0.16f, 0f);

        public static StructuralStatusKind ToKind(StructuralRiskLevel level)
        {
            switch (level)
            {
                case StructuralRiskLevel.Caution: return StructuralStatusKind.Caution;
                case StructuralRiskLevel.Critical: return StructuralStatusKind.Critical;
                case StructuralRiskLevel.Imminent: return StructuralStatusKind.Imminent;
                default: return StructuralStatusKind.Safe;
            }
        }

        public static StructuralStatusStyle Resolve(StructuralStatusKind kind)
        {
            switch (kind)
            {
                case StructuralStatusKind.Caution:
                    return new StructuralStatusStyle(1, CautionText, NoGlow, false);
                case StructuralStatusKind.Critical:
                    return new StructuralStatusStyle(2, CriticalText, CriticalGlow, true);
                case StructuralStatusKind.Imminent:
                    return new StructuralStatusStyle(3, ImminentText, ImminentGlow, true);
                default:
                    return new StructuralStatusStyle(0, SafeText, NoGlow, false);
            }
        }

        /// <summary>0~1 느린 사인 곡선. 0에서 시작해 PulsePeriodSeconds마다 한 번 반복한다.</summary>
        public static float PulseFactor(float timeSeconds)
        {
            var phase = timeSeconds / PulsePeriodSeconds * Mathf.PI * 2f;
            return 0.5f - 0.5f * Mathf.Cos(phase);
        }
    }
}
