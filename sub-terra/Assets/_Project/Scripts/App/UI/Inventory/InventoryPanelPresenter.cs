using System.Text;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
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
        private readonly GameDataCatalog catalog;

        public InventoryPanelPresenter(IInventoryPanelView view, GameDataCatalog gameDataCatalog = null)
        {
            this.view = view;
            catalog = gameDataCatalog;
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
                HudFormatter.FormatCargoSummary(snapshot.CurrentWeight, snapshot.MaxCapacity));
            view.SetUnsettledValue(HudFormatter.FormatUnsettledValue(snapshot.UnsettledValue));
            RenderDetail(snapshot.CurrentWeight, snapshot.MaxCapacity, snapshot.UnsettledValue);
            view.SetStacksText(FormatStacks(snapshot));
            view.SetStacks(CreateStackReadModels(snapshot));
        }

        private void RenderEmpty()
        {
            view.SetCargoSummary(HudFormatter.FormatCargoSummary(0f, 0f));
            view.SetUnsettledValue(HudFormatter.FormatUnsettledValue(0f));
            RenderDetail(0f, 0f, 0f);
            view.SetStacksText(string.Empty);
            view.SetStacks(System.Array.Empty<InventoryStackReadModel>());
        }

        private void RenderDetail(float currentWeight, float maxCapacity, float unsettledValue)
        {
            // prompt-B 112: 스냅샷 수치를 그대로 넘겨 게이지·가치 카드를 갱신한다.
            if (view is IInventoryPanelDetailView detail)
            {
                detail.SetCargoLoad(currentWeight, maxCapacity);
                detail.SetUnsettledAmount(unsettledValue);
            }
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
                var name = ItemDisplayNames.Inventory(entry.MineralId);
                if (string.IsNullOrEmpty(name) || name == entry.MineralId)
                {
                    name = string.IsNullOrEmpty(entry.DisplayName) ? entry.MineralId : entry.DisplayName;
                }

                sb.Append(name);
                sb.Append(" x");
                sb.Append(entry.Quantity);
            }

            return sb.ToString();
        }

        private IReadOnlyList<InventoryStackReadModel> CreateStackReadModels(InventorySnapshot snapshot)
        {
            var result = new List<InventoryStackReadModel>();
            if (catalog != null && catalog.Minerals != null)
            {
                AppendCatalogRows(result, catalog.Minerals, snapshot);
                AppendCatalogRows(result, catalog.RareItems, snapshot);
                return result;
            }

            var stacks = snapshot.Stacks;
            for (var i = 0; i < stacks.Count; i++)
            {
                var stack = stacks[i];
                result.Add(new InventoryStackReadModel(
                    stack.MineralId,
                    stack.DisplayName,
                    null,
                    stack.Quantity,
                    stack.UnitWeight));
            }

            return result;
        }

        private static void AppendCatalogRows(
            List<InventoryStackReadModel> result,
            IReadOnlyList<MineralData> items,
            InventorySnapshot snapshot)
        {
            if (items == null)
            {
                return;
            }

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null || string.IsNullOrEmpty(item.Id))
                {
                    continue;
                }

                result.Add(new InventoryStackReadModel(
                    item.Id,
                    ItemDisplayNames.Inventory(item.Id),
                    item.Icon,
                    snapshot.GetQuantity(item.Id),
                    item.UnitWeight));
            }
        }
    }
}
