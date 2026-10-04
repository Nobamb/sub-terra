using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 위험 상태에서만 켜지는 하단 행 발광 펄스. 구조 상태 행 안의 glow Image 알파만 바꾼다.
    /// 비활성 상태에서는 Update가 돌지 않으며 StructuralHudView가 켜고 끈다.
    /// </summary>
    public sealed class StructuralStatusPulse : MonoBehaviour
    {
        private Image glow;
        private Color glowColor;

        public void Configure(Image target, Color color)
        {
            glow = target;
            glowColor = color;
            Apply(0f);
        }

        private void OnEnable()
        {
            Apply(StructuralStatusPresentation.PulseFactor(Time.unscaledTime));
        }

        private void Update()
        {
            Apply(StructuralStatusPresentation.PulseFactor(Time.unscaledTime));
        }

        public void Apply(float factor)
        {
            if (glow == null)
            {
                return;
            }

            var color = glowColor;
            color.a = glowColor.a * Mathf.Clamp01(factor);
            glow.color = color;
        }
    }
}
