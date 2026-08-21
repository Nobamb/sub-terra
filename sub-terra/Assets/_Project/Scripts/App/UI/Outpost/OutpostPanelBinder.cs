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
        }

        private void OnEnable()
        {
            interactAction ??= inputActions?.FindAction(interactActionPath, false);
            if (interactAction != null)
            {
                interactAction.started += OnInteractStarted;
            }
        }

        private void OnDisable()
        {
            if (interactAction != null)
            {
                interactAction.started -= OnInteractStarted;
            }
        }

        private void OnDestroy()
        {
            if (quantityInput != null)
            {
                quantityInput.onValueChanged.RemoveListener(OnQuantityChanged);
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
    }
}
