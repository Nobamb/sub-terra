using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 구조 안정도 HUD View. 표시 텍스트만 설정하며 위험 계산을 하지 않는다.
    /// </summary>
    public sealed class StructuralHudView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI structuralRiskText;

        public TextMeshProUGUI StructuralRiskText => structuralRiskText;

        public void SetStructuralRisk(string text)
        {
            if (structuralRiskText != null)
            {
                structuralRiskText.text = text ?? string.Empty;
            }
        }

        public bool HasRequiredReferences()
        {
            return structuralRiskText != null;
        }
    }
}
