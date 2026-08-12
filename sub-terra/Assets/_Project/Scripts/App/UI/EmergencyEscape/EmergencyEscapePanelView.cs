using System.Collections.Generic;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>엘리베이터·전진기지 코어 드롭다운과 비용 안내를 표시한다.</summary>
    public sealed class EmergencyEscapePanelView : MonoBehaviour, IEmergencyEscapePanelView
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Dropdown destinationDropdown;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;

        public int SelectedDestinationIndex =>
            destinationDropdown != null ? destinationDropdown.value : 0;

        public void SetVisible(bool visible)
        {
            (panelRoot != null ? panelRoot : gameObject).SetActive(visible);
        }

        public void SetDestinations(
            IReadOnlyList<EmergencyEscapeDestinationOption> options,
            int selectedIndex)
        {
            if (destinationDropdown == null)
            {
                return;
            }

            destinationDropdown.ClearOptions();
            var labels = new List<string>();
            if (options != null)
            {
                for (var i = 0; i < options.Count; i++)
                {
                    labels.Add(options[i].DisplayName);
                }
            }

            destinationDropdown.AddOptions(labels);
            if (labels.Count > 0)
            {
                destinationDropdown.value = Mathf.Clamp(selectedIndex, 0, labels.Count - 1);
                destinationDropdown.RefreshShownValue();
            }
        }

        public void SetCost(int gold, int energy)
        {
            if (costText == null)
            {
                return;
            }

            costText.text = "비용: " + gold + "G + 전력 " + energy;
        }

        public void SetResult(string message, bool isError)
        {
            if (resultText == null)
            {
                return;
            }

            resultText.text = message ?? string.Empty;
            resultText.color = isError
                ? new Color(1f, 0.45f, 0.35f)
                : new Color(0.45f, 1f, 0.65f);
        }

        public void SetBusy(bool busy)
        {
            if (confirmButton != null)
            {
                confirmButton.interactable = !busy;
            }

            if (destinationDropdown != null)
            {
                destinationDropdown.interactable = !busy;
            }
        }

        public bool HasRequiredReferences()
        {
            return panelRoot != null
                && destinationDropdown != null
                && costText != null
                && confirmButton != null
                && closeButton != null;
        }
    }
}
