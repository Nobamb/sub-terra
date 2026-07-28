using System.Text;
using SubTerra.App.Inventory;
using SubTerra.App.UI.HUD;

namespace SubTerra.App.UI.Inventory
{
    /// <summary>
    /// InventoryService 스냅샷 → 패널 View. Bind/Unbind 대칭, Update 폴링 없음.
    /// 중량·가치 수치는 스냅샷 값을 포맷만 하며 재계산하지 않는다.
    /// </summary>
    public sealed class InventoryPanelPresenter
    {
        private readonly IInventoryPanelView view;
        private InventoryService boundService;

        public InventoryPanelPresenter(IInventoryPanelView view)
        {
            this.view = view;
        }

        public bool IsBound => boundService != null;

        public void Bind(InventoryService service)
        {
            Unbind();
            boundService = service;
            if (boundService == null)
            {
                RenderEmpty();
                return;
            }

            boundService.InventoryChanged += OnInventoryChanged;
            Render(boundService.GetSnapshot());
        }

        public void Unbind()
        {
            if (boundService == null)
            {
                return;
            }

            boundService.InventoryChanged -= OnInventoryChanged;
            boundService = null;
        }

        private void OnInventoryChanged(InventorySnapshot snapshot)
        {
            Render(snapshot);
        }

        private void Render(InventorySnapshot snapshot)
        {
            if (snapshot == null)
            {
                RenderEmpty();
                return;
            }

            view.SetCargoSummary(
                HudFormatter.FormatCargo(snapshot.CurrentWeight) + " / " +
                HudFormatter.FormatCargo(snapshot.MaxCapacity));
            view.SetUnsettledValue(HudFormatter.FormatUnsettledValue(snapshot.UnsettledValue));
            view.SetStacksText(FormatStacks(snapshot));
        }

        private void RenderEmpty()
        {
            view.SetCargoSummary(HudFormatter.FormatCargo(0f) + " / " + HudFormatter.FormatCargo(0f));
            view.SetUnsettledValue(HudFormatter.FormatUnsettledValue(0f));
            view.SetStacksText(string.Empty);
        }

        private static string FormatStacks(InventorySnapshot snapshot)
        {
            var stacks = snapshot.Stacks;
            if (stacks == null || stacks.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            for (var i = 0; i < stacks.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append('\n');
                }

                var entry = stacks[i];
                var name = string.IsNullOrEmpty(entry.DisplayName) ? entry.MineralId : entry.DisplayName;
                sb.Append(name);
                sb.Append(" x");
                sb.Append(entry.Quantity);
            }

            return sb.ToString();
        }
    }
}
