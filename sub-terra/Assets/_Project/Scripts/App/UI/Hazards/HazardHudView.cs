using SubTerra.App.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Hazards
{
    /// <summary>
    /// 색과 함께 기호·문구·실제 수치를 표시하는 위험 HUD.
    /// 색각 구분이 어려워도 상태를 읽을 수 있도록 텍스트 접두 기호를 유지한다.
    /// Canvas sort는 튜토리얼 안내보다 항상 높게 유지한다.
    /// </summary>
    public sealed class HazardHudView : MonoBehaviour, IHazardStatusView
    {
        [SerializeField] private TMP_Text structuralText;
        [SerializeField] private Image structuralIcon;
        [SerializeField] private TMP_Text gasText;
        [SerializeField] private Image gasIcon;
        [SerializeField] private GameObject gasWarningRoot;
        [SerializeField] private TMP_Text powerText;
        [SerializeField] private Image powerIcon;
        [SerializeField] private Canvas hazardCanvas;

        private void Awake()
        {
            EnsureHazardSortOrder(false);
        }

        public void SetStructuralStatus(HazardStatusReadModel status)
        {
            ApplyHazard(structuralText, structuralIcon, "구조", status);
        }

        public void SetGasStatus(HazardStatusReadModel status)
        {
            ApplyHazard(gasText, gasIcon, "가스", status);
            if (gasWarningRoot != null)
            {
                gasWarningRoot.SetActive(status.Severity != HazardSeverity.Safe);
            }
        }

        public void SetPowerStatus(PowerStatusReadModel status)
        {
            var connectedLabel = status.IsConnected ? "✓ 연결" : "✕ 미연결";
            if (powerText != null)
            {
                powerText.text = "전력 " + connectedLabel
                    + "  " + status.Supply.ToString("0.#")
                    + "/" + status.Demand.ToString("0.#")
                    + "  활성 " + status.ActiveFacilityCount
                    + (string.IsNullOrEmpty(status.Reason) ? string.Empty : "\n" + status.Reason);
            }

            var color = status.IsConnected
                ? new Color(0.35f, 0.9f, 0.5f)
                : new Color(1f, 0.42f, 0.3f);
            if (powerText != null)
            {
                powerText.color = color;
            }
            if (powerIcon != null)
            {
                powerIcon.color = color;
            }
        }

        public void SetGasPriority(bool isPriority)
        {
            if (isPriority && gasWarningRoot != null)
            {
                gasWarningRoot.transform.SetAsLastSibling();
            }

            EnsureHazardSortOrder(isPriority);
        }

        /// <summary>위험 HUD sort order가 튜토리얼(UiLayerPriority.TutorialGuidance)보다 큰지 보장한다.</summary>
        public void EnsureHazardSortOrder(bool isCritical)
        {
            if (hazardCanvas == null)
            {
                hazardCanvas = GetComponentInParent<Canvas>();
            }

            if (hazardCanvas == null)
            {
                return;
            }

            var target = UiLayerPriority.ResolveHazardSortOrder(isCritical);
            if (hazardCanvas.sortingOrder < target)
            {
                hazardCanvas.sortingOrder = target;
            }
        }

        public int CurrentSortOrder =>
            hazardCanvas != null ? hazardCanvas.sortingOrder : UiLayerPriority.HazardWarning;

        public bool HasRequiredReferences()
        {
            return structuralText != null
                && gasText != null
                && gasWarningRoot != null
                && powerText != null;
        }

        private static void ApplyHazard(
            TMP_Text text,
            Graphic icon,
            string category,
            HazardStatusReadModel status)
        {
            var symbol = status.Severity == HazardSeverity.Safe
                ? "✓"
                : status.Severity == HazardSeverity.Caution ? "⚠" : "!";
            var color = status.Severity == HazardSeverity.Safe
                ? new Color(0.35f, 0.9f, 0.5f)
                : status.Severity == HazardSeverity.Caution
                    ? new Color(1f, 0.78f, 0.25f)
                    : new Color(1f, 0.25f, 0.2f);

            if (text != null)
            {
                text.text = symbol + " " + category + " " + status.Label
                    + (string.IsNullOrEmpty(status.ValueText) ? string.Empty : "  " + status.ValueText);
                text.color = color;
            }

            if (icon != null)
            {
                icon.color = color;
            }
        }
    }
}
