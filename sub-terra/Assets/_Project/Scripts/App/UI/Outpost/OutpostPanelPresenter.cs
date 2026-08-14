using System.Text;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>전진기지 View와 Service를 연결한다. UI에서 State를 직접 변경하지 않는다.</summary>
    public sealed class OutpostPanelPresenter
    {
        private readonly IOutpostPanelView view;
        private OutpostService service;
        private bool busy;
        private bool interactionPanelRequested;

        public bool IsBound => service != null;

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

            busy = false;
            interactionPanelRequested = false;
            view?.SetBusy(false);
            view?.SetVisible(false);
        }

        public void ToggleInteractionPanel()
        {
            if (service == null || !service.IsOutpostCoreInteraction)
            {
                interactionPanelRequested = false;
                view?.SetVisible(false);
                return;
            }

            interactionPanelRequested = !interactionPanelRequested;
            view?.SetVisible(interactionPanelRequested);
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
            if (snapshot == null)
            {
                interactionPanelRequested = false;
                view?.SetVisible(false);
                return;
            }

            if (service == null || !service.IsOutpostCoreInteraction)
            {
                interactionPanelRequested = false;
            }

            view?.SetVisible(service != null && service.IsOutpostCoreInteraction && interactionPanelRequested);
            view?.SetPower(
                snapshot.PowerSupply,
                snapshot.PowerConsumption,
                snapshot.IsActive,
                snapshot.InactiveReasonId);
            view?.SetFacilities(snapshot.Facilities);
            view?.SetCargo(
                FormatInventory(snapshot.PlayerCargo),
                FormatInventory(snapshot.Storage));
            view?.SetCheckpoint(string.IsNullOrEmpty(snapshot.CheckpointId)
                ? "체크포인트 없음"
                : snapshot.CheckpointId + " (" + snapshot.CheckpointX + ", " + snapshot.CheckpointY + ")");
        }

        private void OnOperationCompleted(OutpostOperationResult result)
        {
            view?.SetResult(result.Message, !result.IsSuccess);
        }

        private void OnTutorialRequested()
        {
            view?.SetTutorialVisible(true);
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
                    builder.Append(", ");
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
    }
}
