using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 가스 경고 패널 View. 등급 텍스트와 경고 GO 활성만 담당한다.
    /// </summary>
    public sealed class GasWarningPanelView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI gasRiskText;
        [SerializeField] private GameObject warningRoot;

        public TextMeshProUGUI GasRiskText => gasRiskText;
        public GameObject WarningRoot => warningRoot;

        public void SetGasRisk(string text)
        {
            if (gasRiskText != null)
            {
                gasRiskText.text = text ?? string.Empty;
            }
        }

        public void SetGasWarningVisible(bool visible)
        {
            if (warningRoot != null)
            {
                warningRoot.SetActive(visible);
            }
        }

        public bool HasRequiredReferences()
        {
            return gasRiskText != null && warningRoot != null;
        }
    }
}
