using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// HUD 하단 구조 상태 행 View. 아이콘·문구·색·발광만 설정하며 위험 계산을 하지 않는다.
    /// 비활성·재활성 시 마지막 표시값으로 복구한다.
    /// </summary>
    public sealed class StructuralHudView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI structuralRiskText;
        [SerializeField] private Image statusIcon;
        [SerializeField] private Sprite[] stateIcons;
        [SerializeField] private Image glowImage;
        [SerializeField] private StructuralStatusPulse pulse;

        private string currentText = string.Empty;
        private StructuralStatusKind currentKind = StructuralStatusKind.Safe;

        public TextMeshProUGUI StructuralRiskText => structuralRiskText;
        public Image StatusIcon => statusIcon;
        public Image GlowImage => glowImage;
        public StructuralStatusKind CurrentKind => currentKind;
        public bool IsPulsing => pulse != null && pulse.enabled;

        private void OnEnable()
        {
            Apply();
        }

        public void SetStructuralRisk(string text, StructuralStatusKind kind)
        {
            currentText = text ?? string.Empty;
            currentKind = kind;
            Apply();
        }

        public bool HasRequiredReferences()
        {
            return structuralRiskText != null;
        }

        private void Apply()
        {
            var style = StructuralStatusPresentation.Resolve(currentKind);
            if (structuralRiskText != null)
            {
                structuralRiskText.text = currentText;
                structuralRiskText.color = style.TextColor;
            }

            if (statusIcon != null)
            {
                if (stateIcons != null && style.IconIndex < stateIcons.Length
                    && stateIcons[style.IconIndex] != null)
                {
                    statusIcon.sprite = stateIcons[style.IconIndex];
                }

                statusIcon.color = Color.white;
            }

            if (glowImage != null)
            {
                var hidden = style.GlowColor;
                hidden.a = 0f;
                glowImage.color = hidden;
            }

            if (pulse != null)
            {
                pulse.Configure(glowImage, style.GlowColor);
                pulse.enabled = style.Pulses && glowImage != null;
            }
        }
    }
}
