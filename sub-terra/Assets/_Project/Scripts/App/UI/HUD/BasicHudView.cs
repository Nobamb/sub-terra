using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 기본 HUD View. TextMeshPro 참조만 보유하고 State를 읽거나 쓰지 않는다.
    /// </summary>
    public sealed class BasicHudView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI depthText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI cargoText;
        [SerializeField] private TextMeshProUGUI unsettledValueText;
        [SerializeField] private TextMeshProUGUI buildingSelectionText;
        [SerializeField] private TextMeshProUGUI interactionPromptText;

        public TextMeshProUGUI EnergyText => energyText;
        public TextMeshProUGUI DepthText => depthText;
        public TextMeshProUGUI GoldText => goldText;
        public TextMeshProUGUI CargoText => cargoText;
        public TextMeshProUGUI UnsettledValueText => unsettledValueText;
        public TextMeshProUGUI BuildingSelectionText => buildingSelectionText;
        public TextMeshProUGUI InteractionPromptText => interactionPromptText;

        public void SetEnergy(string text)
        {
            SetText(energyText, text);
        }

        public void SetDepth(string text)
        {
            SetText(depthText, text);
        }

        public void SetGold(string text)
        {
            SetText(goldText, text);
        }

        public void SetCargo(string text)
        {
            SetText(cargoText, text);
        }

        public void SetUnsettledValue(string text)
        {
            SetText(unsettledValueText, text);
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
