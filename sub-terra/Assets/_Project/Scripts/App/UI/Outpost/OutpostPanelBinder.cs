using SubTerra.App.Outpost;
using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>Prefab 입력과 Presenter 수명을 연결하는 얇은 Binder.</summary>
    public sealed class OutpostPanelBinder : MonoBehaviour
    {
        [SerializeField] private OutpostPanelView view;
        [SerializeField] private TMP_InputField quantityInput;

        private OutpostPanelPresenter presenter;
        private string selectedMineralId = string.Empty;

        public OutpostPanelPresenter Presenter => presenter;
        public bool IsBound => presenter != null && presenter.IsBound;

        private void Awake()
        {
            if (view == null)
            {
                view = GetComponent<OutpostPanelView>();
            }

            presenter = new OutpostPanelPresenter(view);
        }

        private void OnDestroy()
        {
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

        public void SelectMineral(string mineralId)
        {
            selectedMineralId = mineralId ?? string.Empty;
            view?.SetSelectedMineral(selectedMineralId);
        }

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

        private int ReadQuantity()
        {
            return quantityInput != null
                && int.TryParse(quantityInput.text, out var quantity)
                    ? quantity
                    : 0;
        }
    }
}
