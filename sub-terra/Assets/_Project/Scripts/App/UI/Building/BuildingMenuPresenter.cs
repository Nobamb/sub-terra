using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Economy;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.UI.Building
{
    /// <summary>
    /// 시설 목록·선택·비용 가능 여부를 관리한다.
    /// 위치 판정은 A의 DTO를 소비만 하며 설치나 자원 상태를 직접 변경하지 않는다.
    /// </summary>
    public sealed class BuildingMenuPresenter
    {
        private readonly IBuildingMenuView view;
        private readonly List<BuildingMenuItemReadModel> items = new();

        private GameDataCatalog catalog;
        private IResourceWallet wallet;
        private InventoryService inventory;
        private IBuildingPlacementPort placement;
        private GameState gameState;
        private BuildingData selected;
        private BuildingPlacementResultDto latestPlacement;
        private bool unbinding;

        public bool IsBound => catalog != null && placement != null;
        public string SelectedBuildingId => selected != null ? selected.Id : string.Empty;

        public BuildingMenuPresenter(IBuildingMenuView view)
        {
            this.view = view;
        }

        public void Bind(
            GameDataCatalog dataCatalog,
            IResourceWallet resourceWallet,
            InventoryService inventoryService,
            IBuildingPlacementPort placementPort,
            GameState state)
        {
            Unbind();
            catalog = dataCatalog;
            wallet = resourceWallet;
            inventory = inventoryService;
            placement = placementPort;
            gameState = state;

            if (inventory != null)
            {
                inventory.InventoryChanged += OnInventoryChanged;
            }

            if (placement != null)
            {
                placement.PlacementChanged += OnPlacementChanged;
            }

            RefreshList();
            ClearSelectionState();
        }

        public void Unbind()
        {
            if (unbinding)
            {
                return;
            }

            unbinding = true;
            try
            {
                if (inventory != null)
                {
                    inventory.InventoryChanged -= OnInventoryChanged;
                }

                if (placement != null)
                {
                    placement.PlacementChanged -= OnPlacementChanged;
                    placement.CancelPreview();
                }

                ClearSelectionState();
                catalog = null;
                wallet = null;
                inventory = null;
                placement = null;
                gameState = null;
                items.Clear();
                view?.SetBuildingList(Array.Empty<BuildingMenuItemReadModel>());
            }
            finally
            {
                unbinding = false;
            }
        }

        public bool SelectBuilding(string buildingId)
        {
            if (catalog == null
                || placement == null
                || string.IsNullOrEmpty(buildingId)
                || !catalog.TryGetBuilding(buildingId, out var data)
                || data == null
                || data.RuntimePrefab == null)
            {
                view?.SetStatusMessage("선택할 수 없는 시설입니다.");
                return false;
            }

            if (selected != null)
            {
                placement.CancelPreview();
            }

            selected = data;
            latestPlacement = new BuildingPlacementResultDto
            {
                state = BuildingPlacementState.Previewing,
                buildingId = data.Id
            };

            if (!placement.BeginPreview(data.Id))
            {
                view?.SetStatusMessage("건설 Preview를 시작할 수 없습니다.");
                ClearSelectionState();
                return false;
            }

            gameState?.SetBuildingSelection(data.Id, data.DisplayName);
            view?.SetSelection(CreateItem(data));
            view?.SetStatusMessage(string.Empty);
            RefreshAvailability();
            return true;
        }

        public void CancelSelection()
        {
            placement?.CancelPreview();
            ClearSelectionState();
            view?.SetStatusMessage("건설 선택을 취소했습니다.");
        }

        private void RefreshList()
        {
            items.Clear();
            if (catalog != null && catalog.Buildings != null)
            {
                for (var i = 0; i < catalog.Buildings.Count; i++)
                {
                    var data = catalog.Buildings[i];
                    if (data != null)
                    {
                        items.Add(CreateItem(data));
                    }
                }
            }

            view?.SetBuildingList(items);
        }

        private BuildingMenuItemReadModel CreateItem(BuildingData data)
        {
            var snapshot = inventory?.GetSnapshot();
            var costs = new List<BuildingCostReadModel>();
            if (data.BuildCosts != null)
            {
                for (var i = 0; i < data.BuildCosts.Count; i++)
                {
                    var cost = data.BuildCosts[i];
                    costs.Add(new BuildingCostReadModel(
                        cost.ItemId,
                        cost.Quantity,
                        snapshot?.GetQuantity(cost.ItemId) ?? 0));
                }
            }

            return new BuildingMenuItemReadModel(
                data.Id,
                data.DisplayName,
                data.Description,
                data.Icon,
                data.PowerDraw,
                costs);
        }

        private void OnInventoryChanged(InventorySnapshot _)
        {
            RefreshList();
            if (selected != null)
            {
                view?.SetSelection(CreateItem(selected));
                RefreshAvailability();
            }
        }

        private void OnPlacementChanged(BuildingPlacementResultDto result)
        {
            if (result == null || selected == null)
            {
                return;
            }

            // 다른 Preview의 늦은 이벤트가 현재 선택 UI를 덮어쓰지 못하게 한다.
            if (!string.IsNullOrEmpty(result.buildingId) && result.buildingId != selected.Id)
            {
                return;
            }

            latestPlacement = result;
            RefreshAvailability();

            if (result.state == BuildingPlacementState.Placed)
            {
                view?.SetStatusMessage("시설 설치가 완료되었습니다.");
                ClearSelectionState();
            }
            else if (result.state == BuildingPlacementState.Failed)
            {
                view?.SetStatusMessage(FormatPlacementReason(result.reasonId, true));
                ClearSelectionState();
            }
            else if (result.state == BuildingPlacementState.Cancelled)
            {
                ClearSelectionState();
            }
        }

        private void RefreshAvailability()
        {
            if (selected == null)
            {
                return;
            }

            var costs = ItemCostMapping.ToDtoList(selected.BuildCosts);
            var canAfford = wallet != null && wallet.CanAfford(costs);
            var message = !canAfford
                ? "자원이 부족합니다."
                : FormatPlacementReason(latestPlacement?.reasonId, false);

            view?.SetAvailability(new BuildingAvailabilityReadModel(
                latestPlacement?.state ?? BuildingPlacementState.Previewing,
                canAfford,
                message));
        }

        private void ClearSelectionState()
        {
            selected = null;
            latestPlacement = null;
            gameState?.SetBuildingSelection(string.Empty);
            view?.ClearSelection();
            view?.SetAvailability(new BuildingAvailabilityReadModel(
                BuildingPlacementState.None,
                false,
                string.Empty));
        }

        private static string FormatPlacementReason(string reasonId, bool failed)
        {
            switch (reasonId)
            {
                case "occupied":
                    return "다른 시설이나 지형이 차지한 위치입니다.";
                case "missing_ground":
                    return "시설을 지지할 지면이 없습니다.";
                case "invalid_definition":
                    return "시설 데이터가 올바르지 않습니다.";
                case "instantiate_failed":
                    return "시설 생성에 실패했습니다.";
                case "spend_failed":
                    return "비용 차감에 실패했습니다.";
                case "cannot_afford":
                    return "자원이 부족합니다.";
                case "wallet_unavailable":
                    return "건설 자원 상태를 확인할 수 없습니다.";
                case "no_selection":
                    return "선택된 시설이 없습니다.";
                case "out_of_range":
                    return "플레이어에게서 너무 먼 위치입니다.";
                case "outside_allowed_area":
                    return "건설이 허용되지 않은 구역입니다.";
                default:
                    return failed ? "시설 설치에 실패했습니다." : string.Empty;
            }
        }
    }
}
