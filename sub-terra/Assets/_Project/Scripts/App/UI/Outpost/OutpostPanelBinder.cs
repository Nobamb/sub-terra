using System;
using SubTerra.App.Outpost;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>Prefab 입력과 Presenter 수명을 연결하는 얇은 Binder.</summary>
    public sealed class OutpostPanelBinder : MonoBehaviour
    {
        [SerializeField] private OutpostPanelView view;
        [SerializeField] private TMP_InputField quantityInput;
        [SerializeField] private OutpostMineralPickerView mineralPicker;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string interactActionPath = "Player/Interact";

        private OutpostPanelPresenter presenter;
        private string selectedMineralId = string.Empty;
        private InputAction interactAction;
        private Func<bool> primaryInteractionClaim;
        private ResourceSellSession sellSession;
        private SettlementSellBackend sellBackend;
        private Func<string, Sprite> itemIconResolver;

        public OutpostPanelPresenter Presenter => presenter;
        public ResourceSellSession SellSession => sellSession;
        public OutpostPanelView View => view;
        public bool IsBound => presenter != null && presenter.IsBound;
        public bool IsTopWindow(Canvas canvas) => view != null
            && PopupWindowSorting.Contains(canvas, view.ActiveWindowRoot);

        private void Awake()
        {
            if (view == null)
            {
                view = GetComponent<OutpostPanelView>();
            }

            presenter = new OutpostPanelPresenter(view);
            if (quantityInput != null)
            {
                quantityInput.onValueChanged.AddListener(OnQuantityChanged);
            }

            if (mineralPicker != null)
            {
                mineralPicker.SearchChanged += OnMineralSearchChanged;
                mineralPicker.MineralSelected += SelectMineral;
            }
        }

        private void OnEnable()
        {
            interactAction ??= inputActions?.FindAction(interactActionPath, false);
            if (interactAction != null)
            {
                interactAction.started += OnInteractStarted;
            }

            WireCloseButton();
            if (view != null)
            {
                view.SellCloseRequested -= ClosePanel;
                view.SellCloseRequested += ClosePanel;
            }
        }

        private void OnDisable()
        {
            if (interactAction != null)
            {
                interactAction.started -= OnInteractStarted;
            }

            UnwireCloseButton();
            if (view != null)
            {
                view.SellCloseRequested -= ClosePanel;
            }
        }

        private void OnDestroy()
        {
            if (quantityInput != null)
            {
                quantityInput.onValueChanged.RemoveListener(OnQuantityChanged);
            }

            if (mineralPicker != null)
            {
                mineralPicker.SearchChanged -= OnMineralSearchChanged;
                mineralPicker.MineralSelected -= SelectMineral;
            }

            ReleaseSellSession();
            presenter?.Unbind();
            presenter = null;
        }

        public void BindTo(OutpostService service)
        {
            if (presenter == null)
            {
                presenter = new OutpostPanelPresenter(view);
            }

            presenter.Bind(service);

            // B-136: 정산 콘솔도 지상 판매창과 같은 공통 판매 팝업을 쓴다.
            // 정산 규칙(희귀 품목 판매 불가·합계 보너스)은 어댑터가 데이터로 넘긴다.
            ReleaseSellSession();
            if (view != null && service != null)
            {
                sellBackend = new SettlementSellBackend(presenter, service, itemIconResolver);
                sellSession = new ResourceSellSession(sellBackend);
                view.AttachSellSession(sellSession);
            }
        }

        /// <summary>판매 창 자원 아이콘 조회(카탈로그의 기존 아이콘). 표시 전용.</summary>
        public void SetItemIconResolver(Func<string, Sprite> resolver)
        {
            itemIconResolver = resolver;
            if (sellBackend != null)
            {
                sellBackend.SetIconResolver(resolver);
            }
        }

        private void ReleaseSellSession()
        {
            if (view != null)
            {
                view.AttachSellSession(null);
            }

            if (sellSession != null)
            {
                sellSession.Dispose();
                sellSession = null;
            }

            sellBackend = null;
        }

        public void SetPrimaryInteractionClaim(Func<bool> claim)
        {
            primaryInteractionClaim = claim;
        }

        /// <summary>코어 CCTV가 시설 위치와 아이콘을 찾는 방법. 표시 전용이며 접근 판정과 무관하다.</summary>
        public void SetFacilityPresentation(IFacilityWorldLocator locator, Func<string, Sprite> iconResolver)
        {
            if (view == null)
            {
                return;
            }

            view.SetFacilityLocator(locator);
            view.SetFacilityIconResolver(iconResolver);
        }

        public void SelectMineral(string mineralId)
        {
            selectedMineralId = mineralId ?? string.Empty;
            if (quantityInput != null)
            {
                quantityInput.text = "1";
            }

            presenter?.SelectMineral(selectedMineralId);
        }

        public void SetQuantityOne() => SetQuantity(1);
        public void SetQuantityFive() => SetQuantity(5);
        public void SetQuantityTen() => SetQuantity(10);

        public void Charge()
        {
            presenter?.RequestCharge();
        }

        public void Deposit()
        {
            presenter?.RequestDeposit(selectedMineralId, ReadQuantity());
        }

        public void Withdraw()
        {
            presenter?.RequestWithdraw(selectedMineralId, ReadQuantity());
        }

        public void SellSelected()
        {
            presenter?.RequestSellSelected(selectedMineralId, ReadQuantity());
        }

        public void SettlePlayerCargo()
        {
            presenter?.RequestSettlement(OutpostSettlementSource.PlayerCargo);
        }

        public void SettleStorage()
        {
            presenter?.RequestSettlement(OutpostSettlementSource.Storage);
        }

        public void DismissTutorial()
        {
            presenter?.DismissTutorial();
        }

        /// <summary>우측 상단 X와 ESC가 공유하는 닫기 경로. 시설에서 떨어지지 않아도 창만 숨긴다.</summary>
        public void ClosePanel()
        {
            presenter?.DismissInteractionPanel();
            UiKeyboardSubmitGuard.ClearSelection();
        }

        private void OnInteractStarted(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                presenter?.ToggleInteractionPanel(
                    primaryInteractionClaim != null && primaryInteractionClaim());
            }
        }

        private int ReadQuantity()
        {
            return quantityInput != null
                && int.TryParse(quantityInput.text, out var quantity)
                    ? quantity
                    : 0;
        }

        private void SetQuantity(int quantity)
        {
            if (quantityInput != null)
            {
                quantityInput.text = quantity.ToString();
            }

            presenter?.SetQuantity(quantity);
        }

        private void OnQuantityChanged(string _)
        {
            presenter?.SetQuantity(ReadQuantity());
        }

        private void OnMineralSearchChanged(string query)
        {
            presenter?.SetMineralSearch(query);
        }

        private void WireCloseButton()
        {
            WireCloseButton(view != null ? view.CloseButton : null);
            WireCloseButton(view != null ? view.ServiceCloseButton : null);
            WireCloseButton(view != null ? view.CoreCloseButton : null);
        }

        private void WireCloseButton(Button closeButton)
        {
            if (closeButton == null)
            {
                return;
            }

            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(closeButton);
        }

        private void UnwireCloseButton()
        {
            UnwireCloseButton(view != null ? view.CloseButton : null);
            UnwireCloseButton(view != null ? view.ServiceCloseButton : null);
            UnwireCloseButton(view != null ? view.CoreCloseButton : null);
        }

        private void UnwireCloseButton(Button closeButton)
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePanel);
            }
        }
    }
}
