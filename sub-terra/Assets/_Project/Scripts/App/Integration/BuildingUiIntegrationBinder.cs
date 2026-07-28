using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.Hazards;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>통합 Scene에서 B 서비스와 A Bridge/HUD를 한 번에 연결하는 진입점.</summary>
    public sealed class BuildingUiIntegrationBinder : MonoBehaviour
    {
        [SerializeField] private GameDataCatalog catalog;
        [SerializeField] private GameplayBuildingPlacementBridge placementBridge;
        [SerializeField] private GameplayHazardStatusBridge hazardBridge;
        [SerializeField] private BuildingMenuBinder buildingMenu;
        [SerializeField] private HazardHudBinder hazardHud;

        public void BindTo(
            IResourceWallet wallet,
            InventoryService inventory,
            GameState gameState)
        {
            placementBridge?.BindWallet(wallet, catalog);
            hazardBridge?.BindGameState(gameState);
            buildingMenu?.BindTo(wallet, inventory, gameState, placementBridge);
            hazardHud?.BindTo(hazardBridge);
        }
    }
}
