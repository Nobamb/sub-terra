using SubTerra.App.Core.Data;
using SubTerra.App.Economy;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using UnityEngine;

namespace SubTerra.App.UI.Economy
{
    /// <summary>
    /// Scene/Prefab에서 Economy Presenter와 View를 연결한다.
    /// 수명은 MonoBehaviour, 거래 로직은 Presenter/Service에 둔다.
    /// View 버튼 이벤트를 Presenter 메서드에 위임한다.
    /// </summary>
    public sealed class EconomyPanelBinder : MonoBehaviour
    {
        [SerializeField] private EconomyPanelView view;

        private EconomyPanelPresenter presenter;
        private bool viewWired;

        public EconomyPanelPresenter Presenter => presenter;
        public bool IsBound => presenter != null && presenter.IsBound;
        public bool IsModalVisible => view != null && view.IsVisible;

        private void Awake()
        {
            if (view == null)
            {
                view = GetComponent<EconomyPanelView>();
            }

            presenter = new EconomyPanelPresenter(view);
            WireViewEvents(true);
        }

        private void OnDestroy()
        {
            // 파괴된 UI가 이벤트에 남지 않도록 대칭 Unbind.
            WireViewEvents(false);
            presenter?.Unbind();
            presenter = null;
        }

        /// <summary>기존 2-arg 호환. 목록·크레딧 없이 판매 요청만.</summary>
        public void BindTo(EconomyService economy, CraftingService crafting = null)
        {
            BindTo(economy, crafting, null, null, null);
        }

        /// <summary>
        /// Surface Base 판매 패널 배선: inventory 스냅샷 목록 + GameState 크레딧 + optional 아이콘 카탈로그.
        /// </summary>
        public void BindTo(
            EconomyService economy,
            CraftingService crafting,
            InventoryService inventory,
            GameState gameState,
            GameDataCatalog catalog = null)
        {
            if (presenter == null)
            {
                presenter = new EconomyPanelPresenter(view);
                WireViewEvents(true);
            }

            presenter.Bind(economy, crafting, inventory, gameState, catalog);
        }

        public void Unbind()
        {
            presenter?.Unbind();
        }

        private void WireViewEvents(bool add)
        {
            if (view == null || viewWired == add)
            {
                if (!add)
                {
                    viewWired = false;
                }

                return;
            }

            if (add)
            {
                view.MineralRowSelected += OnMineralRowSelected;
                view.QtyMinusClicked += OnQtyMinus;
                view.QtyPlusClicked += OnQtyPlus;
                view.QtyMaxClicked += OnQtyMax;
                view.SellSelectedClicked += OnSellSelected;
                view.SellAllClicked += OnSellAll;
                viewWired = true;
            }
            else
            {
                view.MineralRowSelected -= OnMineralRowSelected;
                view.QtyMinusClicked -= OnQtyMinus;
                view.QtyPlusClicked -= OnQtyPlus;
                view.QtyMaxClicked -= OnQtyMax;
                view.SellSelectedClicked -= OnSellSelected;
                view.SellAllClicked -= OnSellAll;
                viewWired = false;
            }
        }

        private void OnMineralRowSelected(string mineralId) => presenter?.SelectMineral(mineralId);
        private void OnQtyMinus() => presenter?.AdjustSellQuantity(-1);
        private void OnQtyPlus() => presenter?.AdjustSellQuantity(1);

        private void OnQtyMax()
        {
            if (presenter == null || string.IsNullOrEmpty(presenter.SelectedMineralId))
            {
                return;
            }

            // Max → owned. AdjustSellQuantity가 아닌 절대 수량으로 설정.
            // Owned는 Select 시 캡처되므로 큰 값으로 클램프하면 owned가 된다.
            presenter.SetSellQuantity(int.MaxValue);
        }

        private void OnSellSelected() => presenter?.RequestSellSelected();
        private void OnSellAll() => presenter?.RequestSellAll();
    }
}
