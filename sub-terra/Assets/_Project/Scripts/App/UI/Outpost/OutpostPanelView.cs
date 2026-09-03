using System.Collections;
using System.Collections.Generic;
using System.Text;
using SubTerra.App.Outpost;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>시설 역할별 내용을 표시하는 전진기지 상호작용 View.</summary>
    public sealed class OutpostPanelView : MonoBehaviour, IOutpostPanelView
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private GameObject coreRoot;
        [SerializeField] private GameObject chargerRoot;
        [SerializeField] private GameObject settlementRoot;
        [SerializeField] private GameObject storageRoot;
        [SerializeField] private GameObject transactionRoot;
        [SerializeField] private GameObject storageActionsRoot;
        [SerializeField] private GameObject settlementActionsRoot;
        [SerializeField] private TMP_Text powerText;
        [SerializeField] private TMP_Text facilitiesText;
        [SerializeField] private TMP_Text playerCargoText;
        [SerializeField] private TMP_Text storageCargoText;
        [SerializeField] private TMP_Text settlementCargoText;
        [SerializeField] private TMP_Text checkpointText;
        [SerializeField] private TMP_Text selectedMineralText;
        [SerializeField] private OutpostMineralPickerView mineralPicker;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private GameObject tutorialRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button[] operationButtons;

        public Button CloseButton => closeButton;

        private GameObject interactionMessageRoot;
        private TMP_Text interactionMessageText;
        private Coroutine hideInteractionMessageRoutine;

        public void SetVisible(bool visible)
        {
            (panelRoot != null ? panelRoot : gameObject).SetActive(visible);
        }

        public void SetMode(OutpostPanelMode mode)
        {
            SetActive(coreRoot, mode == OutpostPanelMode.Core);
            SetActive(
                chargerRoot,
                mode == OutpostPanelMode.Charger || mode == OutpostPanelMode.Clinic);
            SetActive(settlementRoot, mode == OutpostPanelMode.Settlement);
            SetActive(storageRoot, mode == OutpostPanelMode.Storage);
            SetActive(
                transactionRoot,
                mode == OutpostPanelMode.Settlement || mode == OutpostPanelMode.Storage);
            SetActive(storageActionsRoot, mode == OutpostPanelMode.Storage);
            SetActive(settlementActionsRoot, mode == OutpostPanelMode.Settlement);

            if (titleText == null)
            {
                return;
            }

            switch (mode)
            {
                case OutpostPanelMode.Core:
                    titleText.text = "전진기지 코어";
                    break;
                case OutpostPanelMode.Charger:
                    titleText.text = "충전기";
                    break;
                case OutpostPanelMode.Clinic:
                    titleText.text = "보건소";
                    break;
                case OutpostPanelMode.Settlement:
                    titleText.text = "정산 콘솔";
                    break;
                case OutpostPanelMode.Storage:
                    titleText.text = "보관함";
                    break;
                default:
                    titleText.text = string.Empty;
                    break;
            }
        }

        public void SetPower(
            float supply,
            float consumption,
            bool active,
            string inactiveReasonId)
        {
            if (powerText == null)
            {
                return;
            }

            powerText.text = "전력 " + supply.ToString("0.##")
                + " / 소비 " + consumption.ToString("0.##")
                + (active
                    ? "  [활성]"
                    : "  [비활성: " + FormatReason(inactiveReasonId) + "]");
        }

        public void SetFacilities(IReadOnlyList<OutpostFacilityReadModel> facilities)
        {
            if (facilitiesText == null)
            {
                return;
            }

            var builder = new StringBuilder();
            if (facilities != null)
            {
                for (var i = 0; i < facilities.Count; i++)
                {
                    var facility = facilities[i];
                    if (!facility.IsActive)
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.AppendLine();
                    }

                    builder.Append("● ")
                        .Append(FormatFacilityName(facility.BuildingId))
                        .Append("  [활성]");
                }
            }

            facilitiesText.text = builder.Length == 0
                ? "활성화된 주변 시설 없음"
                : builder.ToString();
        }

        public void SetCargo(string playerCargo, string storageCargo)
        {
            if (playerCargoText != null)
            {
                playerCargoText.text = "보유 자원: " + (playerCargo ?? string.Empty);
            }

            if (storageCargoText != null)
            {
                storageCargoText.text = "보관 자원: " + (storageCargo ?? string.Empty);
            }
        }

        public void SetSettlementCargo(string cargo)
        {
            if (settlementCargoText != null)
            {
                settlementCargoText.text = cargo ?? string.Empty;
            }
        }

        public void SetCheckpoint(string checkpoint)
        {
            if (checkpointText != null)
            {
                checkpointText.text = checkpoint ?? string.Empty;
            }
        }

        public void SetSelectedMineral(string summary)
        {
            if (selectedMineralText != null)
            {
                selectedMineralText.text = summary ?? string.Empty;
            }
        }

        public void SetMineralOptions(
            IReadOnlyList<OutpostMineralOption> options,
            string selectedMineralId)
        {
            if (mineralPicker != null)
            {
                mineralPicker.SetOptions(options, selectedMineralId);
            }
        }

        public void ClearMineralSearch()
        {
            if (mineralPicker != null)
            {
                mineralPicker.ClearSearch();
            }
        }

        public void SetResult(string message, bool isError)
        {
            if (resultText != null)
            {
                resultText.text = message ?? string.Empty;
                resultText.color = isError
                    ? new Color(1f, 0.45f, 0.35f)
                    : new Color(0.45f, 1f, 0.65f);
            }
        }

        public void ShowTemporaryMessage(string message, float durationSeconds)
        {
            EnsureInteractionMessage();
            if (interactionMessageRoot == null || interactionMessageText == null)
            {
                return;
            }

            interactionMessageText.text = message ?? string.Empty;
            interactionMessageRoot.SetActive(true);
            interactionMessageRoot.transform.SetAsLastSibling();
            if (hideInteractionMessageRoutine != null)
            {
                StopCoroutine(hideInteractionMessageRoutine);
            }

            hideInteractionMessageRoutine = StartCoroutine(
                HideInteractionMessageAfter(Mathf.Max(0f, durationSeconds)));
        }

        public void SetTutorialVisible(bool visible)
        {
            if (tutorialRoot != null)
            {
                tutorialRoot.SetActive(visible);
            }
        }

        public void SetBusy(bool busy)
        {
            if (operationButtons == null)
            {
                return;
            }

            for (var i = 0; i < operationButtons.Length; i++)
            {
                if (operationButtons[i] != null)
                {
                    operationButtons[i].interactable = !busy;
                }
            }
        }

        public bool HasRequiredReferences()
        {
            return panelRoot != null
                && titleText != null
                && coreRoot != null
                && chargerRoot != null
                && settlementRoot != null
                && storageRoot != null
                && transactionRoot != null
                && powerText != null
                && facilitiesText != null
                && playerCargoText != null
                && storageCargoText != null
                && settlementCargoText != null
                && checkpointText != null
                && selectedMineralText != null
                && mineralPicker != null
                && mineralPicker.HasRequiredReferences()
                && resultText != null
                && closeButton != null;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private void EnsureInteractionMessage()
        {
            if (interactionMessageRoot != null && interactionMessageText != null)
            {
                return;
            }

            var canvas = GetComponentInParent<Canvas>(true);
            var parent = canvas != null ? canvas.transform : transform.parent;
            if (parent == null)
            {
                return;
            }

            interactionMessageRoot = new GameObject(
                "FacilityPowerWarning_Runtime",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            var rootRect = interactionMessageRoot.GetComponent<RectTransform>();
            rootRect.SetParent(parent, false);
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(760f, 150f);
            interactionMessageRoot.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.94f);

            var labelObject = new GameObject(
                "Message",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rootRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(28f, 18f);
            labelRect.offsetMax = new Vector2(-28f, -18f);

            interactionMessageText = labelObject.GetComponent<TextMeshProUGUI>();
            interactionMessageText.alignment = TextAlignmentOptions.Center;
            interactionMessageText.fontSize = 25f;
            interactionMessageText.color = Color.white;
            interactionMessageText.enableWordWrapping = true;
            if (resultText != null && resultText.font != null)
            {
                interactionMessageText.font = resultText.font;
            }

            interactionMessageRoot.SetActive(false);
        }

        private IEnumerator HideInteractionMessageAfter(float durationSeconds)
        {
            yield return new WaitForSecondsRealtime(durationSeconds);
            if (interactionMessageRoot != null)
            {
                interactionMessageRoot.SetActive(false);
            }

            hideInteractionMessageRoutine = null;
        }

        private static string FormatReason(string reasonId)
        {
            switch (reasonId)
            {
                case "power_disconnected":
                    return "전력망 미연결";
                case "insufficient_power":
                    return "전력 부족";
                case "out_of_range":
                    return "상호작용 거리 밖";
                case "core_inactive":
                    return "전진기지 코어 비활성";
                default:
                    return string.IsNullOrEmpty(reasonId) ? "원인 정보 없음" : reasonId;
            }
        }

        private static string FormatFacilityName(string buildingId)
        {
            switch (buildingId)
            {
                case "building.charger.basic":
                    return "충전기";
                case "building.clinic.basic":
                    return "보건소";
                case "building.storage.basic":
                    return "보관함";
                case "building.settlement.basic":
                    return "정산 콘솔";
                case "building.light.basic":
                    return "조명";
                case "building.support.basic":
                    return "버팀목";
                default:
                    return string.IsNullOrEmpty(buildingId) ? "알 수 없는 시설" : buildingId;
            }
        }
    }
}
