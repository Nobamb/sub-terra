using SubTerra.App.Inventory;
using SubTerra.App.Core.Data;
using UnityEngine;

namespace SubTerra.App.UI.Inventory
{
    /// <summary>
    /// Scene/Prefab 수명에 InventoryPanel Presenter 구독을 연결한다.
    /// OnEnable 구독·풀 렌더, OnDisable 해제로 파괴된 UI가 이벤트에 남지 않게 한다.
    /// UI는 인벤토리 State를 직접 쓰지 않는다.
    /// </summary>
    public sealed class InventoryPanelBinder : MonoBehaviour
    {
        [SerializeField] private InventoryPanelView panelView;
        [SerializeField] private GameDataCatalog catalog;

        private InventoryPanelPresenter presenter;
        private InventoryService boundService;

        public InventoryPanelPresenter Presenter => presenter;
        public InventoryPanelView PanelView => panelView;

        private void OnEnable()
        {
            EnsurePresenter();
            if (boundService != null)
            {
                presenter.Bind(boundService);
            }
        }

        private void OnDisable()
        {
            if (presenter != null)
            {
                presenter.Unbind();
            }
        }

        /// <summary>테스트·수동 주입용. 활성 중이면 즉시 재바인드한다.</summary>
        public void BindTo(InventoryService service)
        {
            boundService = service;
            EnsurePresenter();
            if (isActiveAndEnabled)
            {
                presenter.Bind(boundService);
            }
        }

        public bool HasRequiredReferences()
        {
            return panelView != null && panelView.HasRequiredReferences();
        }

        private void EnsurePresenter()
        {
            if (presenter != null)
            {
                return;
            }

            if (panelView == null)
            {
                panelView = GetComponent<InventoryPanelView>();
            }

            presenter = new InventoryPanelPresenter(panelView, catalog);
        }
    }
}
