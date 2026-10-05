using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Economy;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.App.UI.Sell;

namespace SubTerra.App.UI.Economy
{
    /// <summary>
    /// 지상 판매창 규칙을 공통 판매 창에 넘기는 어댑터.
    /// 모든 보유 자원 판매 가능, 희귀 품목(엔진 연료)은 '최대 선택'에서 빼고 직접 수량 지정으로만 판매,
    /// 골드 보너스는 광물별 판매와 같은 행 단위 반올림. 거래는 EconomyPanelPresenter → EconomyService.
    /// </summary>
    public sealed class SurfaceSellBackend : IResourceSellBackend
    {
        public const string HintText = "보유한 자원을 선택하고 수량을 정해 골드로 판매하세요.";
        public const string BulkNotice = "희귀 품목은 최대 선택에서 제외 · 직접 수량을 정해 판매";
        public const string RareNote = "최대 선택 제외 · 직접 지정";

        private EconomyPanelPresenter presenter;
        private EconomyService economy;
        private InventoryService inventory;
        private GameState gameState;
        private GameDataCatalog catalog;

        public SurfaceSellBackend(
            EconomyPanelPresenter presenter,
            EconomyService economy,
            InventoryService inventory,
            GameState gameState,
            GameDataCatalog catalog)
        {
            this.presenter = presenter;
            this.economy = economy;
            this.inventory = inventory;
            this.gameState = gameState;
            this.catalog = catalog;
            if (inventory != null)
            {
                inventory.InventoryChanged += OnInventoryChanged;
            }

            if (gameState != null)
            {
                gameState.CreditsChanged += OnCreditsChanged;
            }
        }

        public event Action Changed;

        public string Hint => HintText;
        public string BulkRuleNotice => BulkNotice;

        public ResourceSellSnapshot Read(bool fresh)
        {
            if (inventory == null)
            {
                return ResourceSellSnapshot.Empty;
            }

            var snapshot = inventory.GetSnapshot();
            var lines = new List<ResourceSellLine>();
            var stacks = snapshot != null ? snapshot.Stacks : null;
            for (var i = 0; stacks != null && i < stacks.Count; i++)
            {
                var stack = stacks[i];
                if (stack.Quantity <= 0)
                {
                    continue;
                }

                var rare = DataIds.RareItems.IsRare(stack.MineralId);
                lines.Add(new ResourceSellLine(
                    stack.MineralId,
                    EconomyPanelPresenter.ResolveSellDisplayName(stack),
                    stack.Quantity,
                    stack.UnitPrice,
                    stack.UnitWeight,
                    ResolveIcon(stack.MineralId),
                    canSell: true,
                    inBulk: !rare,
                    note: rare ? RareNote : string.Empty));
            }

            return new ResourceSellSnapshot(
                lines,
                gameState != null ? gameState.Player.Gold : 0,
                snapshot != null ? snapshot.CurrentWeight : 0f,
                snapshot != null ? snapshot.MaxCapacity : 0f,
                economy != null ? economy.GoldGainBonusPercent : 0,
                ResourceSellBonusMode.PerLine);
        }

        public ResourceSellCommitResult Commit(IReadOnlyList<KeyValuePair<string, int>> items)
        {
            if (presenter == null)
            {
                return new ResourceSellCommitResult(false, 0, "판매 서비스가 연결되지 않았습니다.");
            }

            var result = presenter.RequestSellBatch(items);
            return new ResourceSellCommitResult(result.IsSuccess, result.GoldDelta, result.UserMessage);
        }

        public void Dispose()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= OnInventoryChanged;
            }

            if (gameState != null)
            {
                gameState.CreditsChanged -= OnCreditsChanged;
            }

            presenter = null;
            economy = null;
            inventory = null;
            gameState = null;
            catalog = null;
            Changed = null;
        }

        private UnityEngine.Sprite ResolveIcon(string itemId)
        {
            return catalog != null && catalog.TryGetInventoryItem(itemId, out var data) ? data.Icon : null;
        }

        private void OnInventoryChanged(InventorySnapshot _) => Changed?.Invoke();

        private void OnCreditsChanged(int _) => Changed?.Invoke();
    }
}
