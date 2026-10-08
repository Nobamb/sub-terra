using System.Collections;
using System.Collections.Generic;
using System.Text;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>시설 역할별 내용을 표시하는 전진기지 상호작용 View.</summary>
    public sealed class OutpostPanelView : MonoBehaviour, IOutpostPanelView, IFacilityCooldownPopupView
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
        [SerializeField] private FacilityServicePopupView servicePopup;
        [SerializeField] private CoreCctvPopupView corePopup;

        public Button CloseButton => closeButton;
        public GameObject PanelRoot => panelRoot != null ? panelRoot : gameObject;
        public FacilityServicePopupView ServicePopup => servicePopup;
        public Button ServiceCloseButton => servicePopup != null ? servicePopup.CloseButton : null;
        public CoreCctvPopupView CorePopup => corePopup;
        public Button CoreCloseButton => corePopup != null ? corePopup.CloseButton : null;

        /// <summary>
        /// 지금 화면에 올라와 있는(또는 올라올) 창의 루트. 충전기·보건소는 서비스 팝업, 코어는 CCTV 팝업,
        /// 정산 콘솔은 지상 판매창과 같은 공통 판매 팝업이다(B-136).
        /// </summary>
        public GameObject ActiveWindowRoot =>
            servicePopup != null && FacilityServicePopupTimeline.IsServiceMode(currentMode)
                ? servicePopup.gameObject
                : corePopup != null && currentMode == OutpostPanelMode.Core
                    ? corePopup.gameObject
                    : storagePopup != null && (currentMode == OutpostPanelMode.Storage || storagePopup.IsVisible)
                        ? storagePopup.gameObject
                        : sellPopup != null && (currentMode == OutpostPanelMode.Settlement || sellPopup.IsVisible)
                            ? sellPopup.gameObject
                            : PanelRoot;

        public ResourceSellPopupView SellPopup => sellPopup;

        /// <summary>보관함 팝업(B-138). 플레이 중 Binder가 붙인 뒤에만 존재하며, 없으면 예전 큰 패널을 쓴다.</summary>
        public StoragePopupView StoragePopup => storagePopup;

        /// <summary>판매 팝업의 X 버튼. Binder가 기존 닫기 경로(ClosePanel)에 연결한다.</summary>
        public event System.Action SellCloseRequested;

        private ResourceSellPopupView sellPopup;
        private StoragePopupView storagePopup;

        /// <summary>보관함 팝업을 만든다(플레이 중 1회). 보관 판정은 Service, 입력은 Binder가 연결한다.</summary>
        public StoragePopupView AttachStoragePopup(System.Func<string, Sprite> iconResolver)
        {
            if (storagePopup == null && Application.isPlaying)
            {
                var canvases = GetComponentsInParent<Canvas>(true);
                if (canvases != null && canvases.Length > 0)
                {
                    storagePopup = StoragePopupView.Create(canvases[canvases.Length - 1].transform);
                }
            }

            if (storagePopup != null)
            {
                storagePopup.SetIconResolver(iconResolver);
            }

            return storagePopup;
        }

        /// <summary>공통 판매 팝업에 판매 상태를 연결한다. 플레이 중에만 팝업을 만든다.</summary>
        public void AttachSellSession(ResourceSellSession session)
        {
            if (session != null && sellPopup == null && Application.isPlaying)
            {
                var canvases = GetComponentsInParent<Canvas>(true);
                if (canvases != null && canvases.Length > 0)
                {
                    // 월드 클릭(채굴·건설)을 막지 않도록 창 영역만 입력을 받는다.
                    sellPopup = ResourceSellPopupView.Create(canvases[canvases.Length - 1].transform, false);
                    if (sellPopup != null)
                    {
                        sellPopup.CloseRequested += OnSellCloseRequested;
                    }
                }
            }

            if (sellPopup != null)
            {
                sellPopup.Attach(session);
            }
        }

        private void OnSellCloseRequested()
        {
            SellCloseRequested?.Invoke();
        }

        /// <summary>재사용 대기 팝업(플레이 중 첫 표시 때 생성). 프리팹을 바꾸지 않는다.</summary>
        private FacilityCooldownPopupView cooldownPopup;
        private System.Func<string, Sprite> facilityIconResolver;

        public bool ShowFacilityCooldown(string buildingId, string instanceId, System.Func<string, double> remainingProvider)
        {
            if (cooldownPopup == null && Application.isPlaying)
            {
                var canvases = GetComponentsInParent<Canvas>(true);
                if (canvases != null && canvases.Length > 0)
                {
                    cooldownPopup = FacilityCooldownPopupView.Create(
                        canvases[canvases.Length - 1].transform,
                        resultText != null ? resultText.font : null);
                    if (cooldownPopup != null)
                    {
                        cooldownPopup.SetIconResolver(facilityIconResolver);
                    }
                }
            }

            return cooldownPopup != null
                && cooldownPopup.ShowFacilityCooldown(buildingId, instanceId, remainingProvider);
        }

        public void HideFacilityCooldown()
        {
            if (cooldownPopup != null)
            {
                cooldownPopup.HideFacilityCooldown();
            }
        }

        private void OnDestroy()
        {
            if (cooldownPopup != null)
            {
                Destroy(cooldownPopup.gameObject);
                cooldownPopup = null;
            }

            if (storagePopup != null)
            {
                Destroy(storagePopup.gameObject);
                storagePopup = null;
            }

            if (sellPopup != null)
            {
                sellPopup.CloseRequested -= OnSellCloseRequested;
                sellPopup.Attach(null);
                Destroy(sellPopup.gameObject);
                sellPopup = null;
            }
        }

        private GameObject interactionMessageRoot;
        private TMP_Text interactionMessageText;
        private Coroutine hideInteractionMessageRoutine;
        private bool currentVisible;
        private OutpostPanelMode currentMode;
        // 루트가 꺼졌다 켜져 팝업이 다시 열릴 때 값 없는 빈 게이지가 뜨지 않도록 마지막 표시값을 기억한다.
        private bool hasServiceVital;
        private float serviceVitalBefore;
        private float serviceVitalAfter;
        private float serviceVitalMaximum;
        private string serviceResult = string.Empty;
        private bool serviceResultIsError;

        public void SetVisible(bool visible)
        {
            currentVisible = visible;
            ApplyWindowVisibility();
        }

        /// <summary>CCTV 관찰 위치 조회. 연결하지 않으면 영상은 켜지지만 카메라는 움직이지 않는다.</summary>
        public void SetFacilityLocator(IFacilityWorldLocator locator)
        {
            if (corePopup != null)
            {
                corePopup.SetFacilityLocator(locator);
            }
        }

        public void SetFacilityIconResolver(System.Func<string, Sprite> resolver)
        {
            facilityIconResolver = resolver;
            if (cooldownPopup != null)
            {
                cooldownPopup.SetIconResolver(resolver);
            }

            if (corePopup != null)
            {
                corePopup.SetIconResolver(resolver);
            }
        }

        /// <summary>
        /// 충전기·보건소는 기존 큰 패널 대신 서비스 팝업을 쓴다.
        /// Presenter가 스냅샷마다 호출하므로 표시 전환이 있을 때만 연출을 시작·종료한다.
        /// </summary>
        private void ApplyWindowVisibility(bool fromModeChange = false)
        {
            var useService = servicePopup != null
                && currentVisible
                && FacilityServicePopupTimeline.IsServiceMode(currentMode);
            var useCore = corePopup != null
                && currentVisible
                && currentMode == OutpostPanelMode.Core;
            var useSell = sellPopup != null
                && currentVisible
                && currentMode == OutpostPanelMode.Settlement;
            var useStorage = storagePopup != null
                && currentVisible
                && currentMode == OutpostPanelMode.Storage;
            // 닫는 중 SetMode(None)이 먼저 불려도 예전 큰 패널이 잠깐 켜지지 않게 한다.
            if (!fromModeChange || currentMode != OutpostPanelMode.None)
            {
                (panelRoot != null ? panelRoot : gameObject).SetActive(
                    currentVisible && !useService && !useCore && !useSell && !useStorage);
            }

            if (storagePopup != null)
            {
                // Show는 이미 열려 있으면 아무것도 하지 않는다(스냅샷마다 불려도 안전).
                if (useStorage)
                {
                    storagePopup.Show();
                }
                else if (storagePopup.IsOpen)
                {
                    storagePopup.BeginClose();
                }
            }

            if (sellPopup != null)
            {
                // Presenter가 스냅샷마다 불러도 Show는 이미 열려 있으면 아무것도 하지 않는다.
                if (useSell)
                {
                    sellPopup.Show();
                }
                else if (sellPopup.IsOpen)
                {
                    sellPopup.BeginClose();
                }
            }

            if (corePopup != null)
            {
                if (useCore)
                {
                    corePopup.Show();
                }
                else
                {
                    corePopup.Hide();
                }
            }

            if (servicePopup == null)
            {
                return;
            }

            if (useService)
            {
                var wasShown = servicePopup.IsShown;
                servicePopup.Show(currentMode);
                if (!wasShown)
                {
                    if (hasServiceVital)
                    {
                        servicePopup.SetVital(serviceVitalBefore, serviceVitalAfter, serviceVitalMaximum);
                    }

                    servicePopup.SetResult(serviceResult, serviceResultIsError);
                }
            }
            else
            {
                servicePopup.Hide();
            }
        }

        public void SetServiceVital(OutpostOperationKind kind, float before, float after, float maximum)
        {
            hasServiceVital = true;
            serviceVitalBefore = before;
            serviceVitalAfter = after;
            serviceVitalMaximum = maximum;
            if (servicePopup != null)
            {
                servicePopup.SetVital(before, after, maximum);
            }
        }

        public void SetMode(OutpostPanelMode mode)
        {
            currentMode = mode;
            ApplyWindowVisibility(true);
            // 코어 정보는 CCTV 팝업이 맡는다. 팝업이 없는 구 프리팹만 예전 코어 레이어를 쓴다.
            SetActive(coreRoot, corePopup == null && mode == OutpostPanelMode.Core);
            // 충전기·보건소 안내는 서비스 팝업이 맡는다. 팝업이 없는 구 프리팹만 예전 안내 레이어를 쓴다.
            SetActive(
                chargerRoot,
                servicePopup == null
                    && (mode == OutpostPanelMode.Charger || mode == OutpostPanelMode.Clinic));
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
            if (corePopup != null)
            {
                corePopup.SetFacilities(facilities);
            }

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

            if (storagePopup != null)
            {
                storagePopup.SetMineralOptions(options, selectedMineralId);
            }
        }

        public void SetStorageCargo(InventorySnapshot playerCargo, InventorySnapshot storage)
        {
            if (storagePopup != null)
            {
                storagePopup.SetCargo(playerCargo, storage);
            }
        }

        public void SetStorageSelection(string mineralId, string displayName, int owned, int stored, int quantity)
        {
            if (storagePopup != null)
            {
                storagePopup.SetSelection(mineralId, displayName, owned, stored, quantity);
            }
        }

        public void ClearMineralSearch()
        {
            if (mineralPicker != null)
            {
                mineralPicker.ClearSearch();
            }

            if (storagePopup != null)
            {
                storagePopup.ClearSearch();
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

            serviceResult = message ?? string.Empty;
            serviceResultIsError = isError;
            if (servicePopup != null)
            {
                servicePopup.SetResult(message, isError);
            }

            if (storagePopup != null)
            {
                storagePopup.SetStatus(message, isError);
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
            if (storagePopup != null)
            {
                storagePopup.SetBusy(busy);
            }

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
