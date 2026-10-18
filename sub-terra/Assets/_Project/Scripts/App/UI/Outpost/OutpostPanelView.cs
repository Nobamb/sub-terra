using System.Collections;
using System.Collections.Generic;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>시설별 팝업으로 상태를 전달하는 전진기지 상호작용 View.</summary>
    public sealed class OutpostPanelView : MonoBehaviour, IOutpostPanelView, IFacilityCooldownPopupView
    {
        [SerializeField] private TMP_FontAsset messageFont;
        [SerializeField] private FacilityServicePopupView servicePopup;
        [SerializeField] private CoreCctvPopupView corePopup;

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
                            : null;

        public ResourceSellPopupView SellPopup => sellPopup;

        /// <summary>보관함 팝업(B-138). 플레이 중 Binder가 붙인 뒤에만 존재한다.</summary>
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
                        messageFont);
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
        private void ApplyWindowVisibility()
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
            ApplyWindowVisibility();
        }

        public void SetFacilities(IReadOnlyList<OutpostFacilityReadModel> facilities)
        {
            if (corePopup != null)
            {
                corePopup.SetFacilities(facilities);
            }
        }

        public void SetMineralOptions(
            IReadOnlyList<OutpostMineralOption> options,
            string selectedMineralId)
        {
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
            if (storagePopup != null)
            {
                storagePopup.ClearSearch();
            }
        }

        public void SetResult(string message, bool isError)
        {
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

        public void SetBusy(bool busy)
        {
            if (storagePopup != null)
            {
                storagePopup.SetBusy(busy);
            }
        }

        public bool HasRequiredReferences()
        {
            return messageFont != null
                && servicePopup != null && servicePopup.CloseButton != null
                && corePopup != null && corePopup.HasRequiredReferences();
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
            if (messageFont != null)
            {
                interactionMessageText.font = messageFont;
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
    }
}
