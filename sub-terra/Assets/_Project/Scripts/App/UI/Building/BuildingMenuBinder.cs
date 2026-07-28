using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.UI.Building
{
    /// <summary>
    /// Prefab 수명과 건설 Presenter를 연결한다.
    /// 파괴 시 Preview와 이벤트 구독을 함께 해제해 Scene 전환 잔존 상태를 막는다.
    /// </summary>
    public sealed class BuildingMenuBinder : MonoBehaviour
    {
        [SerializeField] private BuildingMenuView view;
        [SerializeField] private GameDataCatalog catalog;
        [SerializeField] private MonoBehaviour placementPortBehaviour;

        private BuildingMenuPresenter presenter;

        public BuildingMenuPresenter Presenter => presenter;
        public bool IsBound => presenter != null && presenter.IsBound;

        private void Awake()
        {
            if (view == null)
            {
                view = GetComponent<BuildingMenuView>();
            }

            presenter = new BuildingMenuPresenter(view);
        }

        private void OnDestroy()
        {
            presenter?.Unbind();
            presenter = null;
        }

        public void BindTo(
            IResourceWallet wallet,
            InventoryService inventory,
            GameState gameState,
            IBuildingPlacementPort placementPort = null)
        {
            if (presenter == null)
            {
                presenter = new BuildingMenuPresenter(view);
            }

            var port = placementPort ?? placementPortBehaviour as IBuildingPlacementPort;
            presenter.Bind(catalog, wallet, inventory, port, gameState);
        }

        public bool SelectBuilding(string buildingId)
        {
            return presenter != null && presenter.SelectBuilding(buildingId);
        }

        public void CancelSelection()
        {
            presenter?.CancelSelection();
        }

        public bool HasRequiredReferences()
        {
            // 실제 A 포트는 통합 Scene의 BuildingUiIntegrationBinder가 런타임 주입할 수 있다.
            return view != null && catalog != null;
        }
    }
}
