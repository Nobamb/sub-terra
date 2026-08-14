using System.Text;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>시설별 전진기지 View와 Service를 연결한다. UI에서 State를 직접 변경하지 않는다.</summary>
    public sealed class OutpostPanelPresenter
    {
        private readonly IOutpostPanelView view;
        private OutpostService service;
        private OutpostSnapshot latestSnapshot;
        private bool busy;
        private bool interactionPanelRequested;
        private string openedFacilityInstanceId = string.Empty;
        private string openedFacilityBuildingId = string.Empty;
        private string selectedMineralId = string.Empty;
        private int selectedQuantity = 1;
        private OutpostPanelMode activeMode;

        public bool IsBound => service != null;
        public OutpostPanelMode ActiveMode => activeMode;

        public OutpostPanelPresenter(IOutpostPanelView view)
        {
            this.view = view;
        }

        public void Bind(OutpostService outpostService)
        {
            Unbind();
            service = outpostService;
            if (service != null)
            {
                service.SnapshotChanged += Render;
                service.OperationCompleted += OnOperationCompleted;
                service.TutorialRequested += OnTutorialRequested;
            }

            view?.SetBusy(false);
            view?.SetResult(string.Empty, false);
            Render(service?.GetSnapshot());
        }

        public void Unbind()
        {
            if (service != null)
            {
                service.SnapshotChanged -= Render;
                service.OperationCompleted -= OnOperationCompleted;
                service.TutorialRequested -= OnTutorialRequested;
                service = null;
            }

            latestSnapshot = null;
            busy = false;
            CloseInteractionPanel();
            view?.SetBusy(false);
        }

        public void ToggleInteractionPanel()
        {
            if (service != null
                && service.TryGetPowerDisconnectedInteractionMessage(out var message))
            {
                CloseInteractionPanel();
                view?.ShowTemporaryMessage(message, 3f);
                return;
            }

            var mode = ResolveMode(service?.InteractionFacilityBuildingId);
            if (service == null || !service.IsFacilityInteraction || mode == OutpostPanelMode.None)
            {
                CloseInteractionPanel();
                return;
            }

            if (interactionPanelRequested && IsCurrentTarget())
            {
                CloseInteractionPanel();
                return;
            }

            interactionPanelRequested = true;
            openedFacilityInstanceId = service.InteractionFacilityInstanceId;
            openedFacilityBuildingId = service.InteractionFacilityBuildingId;
            selectedMineralId = string.Empty;
            selectedQuantity = 1;
            activeMode = mode;
            view?.SetMode(activeMode);
            view?.SetResult(string.Empty, false);
            Render(service.GetSnapshot());

            if (activeMode == OutpostPanelMode.Charger)
            {
                RequestCharge();
            }
        }

        public void SelectMineral(string mineralId)
        {
            selectedMineralId = mineralId ?? string.Empty;
            selectedQuantity = 1;
            RenderSelection();
        }

        public void SetQuantity(int quantity)
        {
            selectedQuantity = quantity > 0 ? quantity : 0;
            RenderSelection();
        }

        public OutpostOperationResult RequestCharge()
        {
            return Execute(() => service.TryCharge(), OutpostOperationKind.Charge);
        }

        public OutpostOperationResult RequestDeposit(string mineralId, int quantity)
        {
            return Execute(
                () => service.TryDeposit(mineralId, quantity),
                OutpostOperationKind.Deposit);
        }

        public OutpostOperationResult RequestWithdraw(string mineralId, int quantity)
        {
            return Execute(
                () => service.TryWithdraw(mineralId, quantity),
                OutpostOperationKind.Withdraw);
        }

        public OutpostOperationResult RequestSellSelected(string mineralId, int quantity)
        {
            return Execute(
                () => service.TrySettlePlayerCargo(mineralId, quantity),
                OutpostOperationKind.SettlePlayerCargo);
        }

        public OutpostOperationResult RequestSettlement(OutpostSettlementSource source)
        {
            var kind = source == OutpostSettlementSource.PlayerCargo
                ? OutpostOperationKind.SettlePlayerCargo
                : OutpostOperationKind.SettleStorage;
            return Execute(() => service.TrySettle(source), kind);
        }

        public void DismissTutorial()
        {
            view?.SetTutorialVisible(false);
        }

        private OutpostOperationResult Execute(
            System.Func<OutpostOperationResult> operation,
            OutpostOperationKind kind)
        {
            if (busy)
            {
                var blocked = new OutpostOperationResult(
                    OutpostOperationStatus.InvalidRequest,
                    kind,
                    string.Empty,
                    0,
                    0,
                    "처리 중입니다.");
                OnOperationCompleted(blocked);
                return blocked;
            }

            if (service == null)
            {
                var missing = new OutpostOperationResult(
                    OutpostOperationStatus.DependencyMissing,
                    kind,
                    string.Empty,
                    0,
                    0,
                    "전진기지 서비스가 연결되지 않았습니다.");
                OnOperationCompleted(missing);
                return missing;
            }

            busy = true;
            view?.SetBusy(true);
            try
            {
                return operation();
            }
            finally
            {
                busy = false;
                view?.SetBusy(false);
            }
        }

        private void Render(OutpostSnapshot snapshot)
        {
            latestSnapshot = snapshot;
            if (snapshot == null)
            {
                CloseInteractionPanel();
                return;
            }

            if (interactionPanelRequested && !IsCurrentTarget())
            {
                CloseInteractionPanel();
            }

            view?.SetVisible(interactionPanelRequested);
            view?.SetMode(interactionPanelRequested ? activeMode : OutpostPanelMode.None);
            view?.SetPower(
                snapshot.PowerSupply,
                snapshot.PowerConsumption,
                snapshot.IsActive,
                snapshot.InactiveReasonId);
            view?.SetFacilities(snapshot.Facilities);
            view?.SetCargo(
                FormatInventory(snapshot.PlayerCargo),
                FormatInventory(snapshot.Storage));
            view?.SetSettlementCargo(FormatSettlementInventory(snapshot.PlayerCargo));
            view?.SetCheckpoint(string.IsNullOrEmpty(snapshot.CheckpointId)
                ? "체크포인트 없음"
                : snapshot.CheckpointId + " (" + snapshot.CheckpointX + ", " + snapshot.CheckpointY + ")");
            RenderSelection();
        }

        private void RenderSelection()
        {
            if (string.IsNullOrEmpty(selectedMineralId) || latestSnapshot == null)
            {
                view?.SetSelectedMineral("자원을 선택하세요.");
                return;
            }

            var playerStack = FindStack(latestSnapshot.PlayerCargo, selectedMineralId);
            var storageStack = FindStack(latestSnapshot.Storage, selectedMineralId);
            var displayName = playerStack?.DisplayName;
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = storageStack?.DisplayName;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = selectedMineralId;
            }

            var owned = playerStack?.Quantity ?? 0;
            var stored = storageStack?.Quantity ?? 0;
            if (activeMode == OutpostPanelMode.Settlement)
            {
                var unitPrice = playerStack?.UnitPrice ?? 0;
                var preview = selectedQuantity > 0 && unitPrice <= int.MaxValue / selectedQuantity
                    ? unitPrice * selectedQuantity
                    : 0;
                view?.SetSelectedMineral(
                    displayName + " | 보유 " + owned + " | 수량 " + selectedQuantity
                    + " | 예상 +" + preview + "G");
                return;
            }

            view?.SetSelectedMineral(
                displayName + " | 보유 " + owned + " | 보관 " + stored
                + " | 수량 " + selectedQuantity);
        }

        private void OnOperationCompleted(OutpostOperationResult result)
        {
            view?.SetResult(result.Message, !result.IsSuccess);
        }

        private void OnTutorialRequested()
        {
            view?.SetTutorialVisible(true);
        }

        private void CloseInteractionPanel()
        {
            interactionPanelRequested = false;
            openedFacilityInstanceId = string.Empty;
            openedFacilityBuildingId = string.Empty;
            selectedMineralId = string.Empty;
            selectedQuantity = 1;
            activeMode = OutpostPanelMode.None;
            view?.SetMode(OutpostPanelMode.None);
            view?.SetVisible(false);
        }

        private bool IsCurrentTarget()
        {
            return service != null
                && service.IsFacilityInteraction
                && service.InteractionFacilityInstanceId == openedFacilityInstanceId
                && service.InteractionFacilityBuildingId == openedFacilityBuildingId;
        }

        private static OutpostPanelMode ResolveMode(string buildingId)
        {
            if (buildingId == DataIds.Buildings.OutpostCoreBasic)
            {
                return OutpostPanelMode.Core;
            }

            if (buildingId == DataIds.Buildings.ChargerBasic)
            {
                return OutpostPanelMode.Charger;
            }

            if (buildingId == DataIds.Buildings.SettlementBasic)
            {
                return OutpostPanelMode.Settlement;
            }

            if (buildingId == DataIds.Buildings.StorageBasic)
            {
                return OutpostPanelMode.Storage;
            }

            return OutpostPanelMode.None;
        }

        private static InventoryStackEntry? FindStack(InventorySnapshot snapshot, string mineralId)
        {
            if (snapshot?.Stacks == null)
            {
                return null;
            }

            for (var i = 0; i < snapshot.Stacks.Count; i++)
            {
                if (snapshot.Stacks[i].MineralId == mineralId)
                {
                    return snapshot.Stacks[i];
                }
            }

            return null;
        }

        private static string FormatInventory(InventorySnapshot snapshot)
        {
            if (snapshot?.Stacks == null || snapshot.Stacks.Count == 0)
            {
                return "비어 있음";
            }

            var builder = new StringBuilder();
            for (var i = 0; i < snapshot.Stacks.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("  ·  ");
                }

                var stack = snapshot.Stacks[i];
                builder.Append(string.IsNullOrEmpty(stack.DisplayName)
                        ? stack.MineralId
                        : stack.DisplayName)
                    .Append(" x")
                    .Append(stack.Quantity);
            }

            builder.Append("  /  ")
                .Append(snapshot.CurrentWeight.ToString("0.##"))
                .Append("kg");
            return builder.ToString();
        }

        private static string FormatSettlementInventory(InventorySnapshot snapshot)
        {
            if (snapshot?.Stacks == null || snapshot.Stacks.Count == 0)
            {
                return "판매할 자원이 없습니다.";
            }

            var builder = new StringBuilder();
            for (var i = 0; i < snapshot.Stacks.Count; i++)
            {
                if (i > 0)
                {
                    builder.AppendLine();
                }

                var stack = snapshot.Stacks[i];
                var total = stack.UnitPrice > 0 && stack.Quantity <= int.MaxValue / stack.UnitPrice
                    ? stack.UnitPrice * stack.Quantity
                    : 0;
                builder.Append(string.IsNullOrEmpty(stack.DisplayName)
                        ? stack.MineralId
                        : stack.DisplayName)
                    .Append("    보유 ")
                    .Append(stack.Quantity)
                    .Append("    개당 ")
                    .Append(stack.UnitPrice)
                    .Append("G    전량 ")
                    .Append(total)
                    .Append('G');
            }

            return builder.ToString();
        }
    }
}
