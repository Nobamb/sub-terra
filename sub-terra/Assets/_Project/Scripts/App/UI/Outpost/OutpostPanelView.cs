using System.Collections.Generic;
using System.Text;
using SubTerra.App.Outpost;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>전력, 시설 상태, 화물과 체크포인트를 표시하는 전진기지 View.</summary>
    public sealed class OutpostPanelView : MonoBehaviour, IOutpostPanelView
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text powerText;
        [SerializeField] private TMP_Text facilitiesText;
        [SerializeField] private TMP_Text playerCargoText;
        [SerializeField] private TMP_Text storageCargoText;
        [SerializeField] private TMP_Text checkpointText;
        [SerializeField] private TMP_Text selectedMineralText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private GameObject tutorialRoot;
        [SerializeField] private Button[] operationButtons;

        public void SetVisible(bool visible)
        {
            (panelRoot != null ? panelRoot : gameObject).SetActive(visible);
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
                    if (i > 0)
                    {
                        builder.AppendLine();
                    }

                    var facility = facilities[i];
                    builder.Append(facility.BuildingId)
                        .Append(facility.IsActive
                            ? " - 활성"
                            : " - 비활성: " + FormatReason(facility.InactiveReasonId));
                }
            }

            facilitiesText.text = builder.Length == 0 ? "연결된 시설 없음" : builder.ToString();
        }

        public void SetCargo(string playerCargo, string storageCargo)
        {
            if (playerCargoText != null)
            {
                playerCargoText.text = "플레이어: " + (playerCargo ?? string.Empty);
            }

            if (storageCargoText != null)
            {
                storageCargoText.text = "보관함: " + (storageCargo ?? string.Empty);
            }
        }

        public void SetCheckpoint(string checkpoint)
        {
            if (checkpointText != null)
            {
                checkpointText.text = checkpoint ?? string.Empty;
            }
        }

        public void SetSelectedMineral(string mineralId)
        {
            if (selectedMineralText != null)
            {
                selectedMineralText.text = "선택: " + (mineralId ?? string.Empty);
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
                && powerText != null
                && facilitiesText != null
                && playerCargoText != null
                && storageCargoText != null
                && checkpointText != null
                && resultText != null;
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
    }
}
