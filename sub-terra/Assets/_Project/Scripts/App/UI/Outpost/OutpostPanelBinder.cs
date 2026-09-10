using System;
using SubTerra.App.Outpost;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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

        public OutpostPanelPresenter Presenter => presenter;
        public bool IsBound => presenter != null && presenter.IsBound;

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
        }

        private void OnDisable()
        {
            if (interactAction != null)
            {
                interactAction.started -= OnInteractStarted;
            }

            UnwireCloseButton();
        }

        private void Update()
        {
            if (presenter == null || !presenter.IsInteractionPanelOpen)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                ClosePanel();
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
        }

        public void SetPrimaryInteractionClaim(Func<bool> claim)
        {
            primaryInteractionClaim = claim;
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
            var closeButton = view != null ? view.CloseButton : null;
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
            var closeButton = view != null ? view.CloseButton : null;
            if (closeButton == null)
            {
                return;
            }

            closeButton.onClick.RemoveListener(ClosePanel);
        }
    }
}
