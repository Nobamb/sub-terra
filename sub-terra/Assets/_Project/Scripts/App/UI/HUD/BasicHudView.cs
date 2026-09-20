using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 기본 HUD View. 전달받은 값으로 텍스트/게이지만 표시하며 State를 읽거나 쓰지 않는다.
    /// </summary>
    public sealed class BasicHudView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI depthText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI cargoText;
        [SerializeField] private TextMeshProUGUI unsettledValueText;
        [SerializeField] private TextMeshProUGUI buildingSelectionText;
        [SerializeField] private TextMeshProUGUI interactionPromptText;
        [SerializeField] private HudGaugeView energyGauge;
        [SerializeField] private HudGaugeView healthGauge;

        public TextMeshProUGUI EnergyText => energyText;
        public TextMeshProUGUI HealthText => healthText;
        public TextMeshProUGUI DepthText => depthText;
        public TextMeshProUGUI GoldText => goldText;
        public TextMeshProUGUI CargoText => cargoText;
        public TextMeshProUGUI UnsettledValueText => unsettledValueText;
        public TextMeshProUGUI BuildingSelectionText => buildingSelectionText;
        public TextMeshProUGUI InteractionPromptText => interactionPromptText;

        private void Awake()
        {
            AlignHealthRow();
        }

        public void AlignHealthRow()
        {
            if (healthGauge != null) return;
            if (healthText == null || energyText == null)
            {
                return;
            }

            RectTransform health = healthText.rectTransform;
            RectTransform energy = energyText.rectTransform;
            health.anchorMin = energy.anchorMin;
            health.anchorMax = energy.anchorMax;
            health.pivot = energy.pivot;
            health.sizeDelta = energy.sizeDelta;
            health.anchoredPosition = new Vector2(
                energy.anchoredPosition.x,
                energy.anchoredPosition.y + energy.rect.height);
        }

        public void SetEnergy(string text)
        {
            SetText(energyText, energyGauge != null ? text.Replace("전력 ", "") : text);
        }

        public void SetEnergyLevel(float current, int maximum)
        {
            if (energyGauge != null) energyGauge.SetValue(current, maximum);
        }

        public void SetHealthLevel(float current, int maximum)
        {
            if (healthGauge != null) healthGauge.SetValue(current, maximum);
        }

        public void SetHealth(string text)
        {
            SetText(healthText, healthGauge != null ? text.Replace("체력 ", "") : text);
        }

        public void SetDepth(string text)
        {
            SetText(depthText, text);
        }

        public void SetGold(string text)
        {
            SetText(goldText, text + "G");
        }

        public void SetCargo(string text)
        {
            SetText(cargoText, text);
        }

        public void SetUnsettledValue(string text)
        {
            SetText(unsettledValueText, text + "G");
        }

        public void SetBuildingSelection(string text)
        {
            SetText(buildingSelectionText, text);
        }

        public void SetInteractionPrompt(string text)
        {
            SetText(interactionPromptText, text);
        }

        public bool HasRequiredReferences()
        {
            return energyText != null
                && healthText != null
                && depthText != null
                && goldText != null
                && cargoText != null
                && unsettledValueText != null
                && buildingSelectionText != null
                && interactionPromptText != null;
        }

        private static void SetText(TextMeshProUGUI target, string text)
        {
            if (target != null)
            {
                target.text = text ?? string.Empty;
            }
        }
    }
}
