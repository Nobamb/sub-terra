using System;
using System.Collections.Generic;
using SubTerra.App.Inventory;
using SubTerra.App.Core.Data;
using SubTerra.App.State;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Economy
{
    /// <summary>
    /// 광물 판매와 시설 비용 지갑(IResourceWallet) 구현.
    /// 가격은 UI가 아니라 카탈로그 unitPrice만 사용하며,
    /// 판매·차감은 사전 전량 검증 후 한 성공 경로에서만 커밋한다(부분 적용 분기 없음).
    /// </summary>
    public sealed class EconomyService : IResourceWallet, IResourceBalanceProvider
    {
        private readonly InventoryService inventory;
        private readonly IMineralCatalogLookup catalog;
        private readonly GameState gameState;
        private readonly ISellGate sellGate;
        private readonly IUpgradeEffectProvider effects;
        public int GoldGainBonusPercent => effects?.GetGoldGainBonusPercent() ?? 0;

        /// <summary>성공·실패 모두 발행. 실패는 상태 이벤트를 동반하지 않는다.</summary>
        public event Action<EconomyTransactionResult> TransactionCompleted;

        /// <summary>성공 거래 직후 1회. Phase K 자동 저장 구독 지점.</summary>
        public event Action<EconomyAutoSaveRequest> AutoSaveRequested;

        public EconomyTransactionResult LastResult { get; private set; }

        /// <summary>
        /// 생성. sellGate null = 판매 허용(기존 단위 테스트 new EconomyService(inv, catalog, state) 무수정).
        /// </summary>
        public EconomyService(
            InventoryService inventory,
            IMineralCatalogLookup catalog,
            GameState gameState,
            ISellGate sellGate = null,
            IUpgradeEffectProvider effects = null)
        {
            this.inventory = inventory;
            this.catalog = catalog;
            this.gameState = gameState;
            this.sellGate = sellGate;
            this.effects = effects;
            LastResult = EconomyTransactionResult.Fail(
                EconomyTransactionStatus.InvalidRequest,
                EconomyTransactionKind.Sell,
                "No transaction yet.");
        }

        /// <summary>
        /// 선택한 광물만 판매 수량만큼 차감하고 골드 = 카탈로그 단가 × 수량을 지급한다.
        /// 검증 실패 시 인벤토리·골드 모두 불변.
        /// </summary>
        public EconomyTransactionResult TrySellMineral(string mineralId, int quantity)
        {
            if (inventory == null || catalog == null || gameState == null)
            {
                return CompleteFail(
                    EconomyTransactionStatus.DependencyMissing,
                    EconomyTransactionKind.Sell,
                    "필수 서비스가 없습니다.",
                    "Inventory, catalog, or GameState missing.");
            }

            // optional 게이트: null이면 허용. false면 InvalidRequest + DenyReason.
            if (sellGate != null && !sellGate.IsSellAllowed)
            {
                var deny = string.IsNullOrEmpty(sellGate.DenyReason)
                    ? "Surface Base에서만 판매할 수 있습니다."
                    : sellGate.DenyReason;
                return CompleteFail(
                    EconomyTransactionStatus.InvalidRequest,
                    EconomyTransactionKind.Sell,
                    deny,
                    "SellGate denied.");
            }

            if (string.IsNullOrEmpty(mineralId))
            {
                return CompleteFail(
                    EconomyTransactionStatus.InvalidRequest,
                    EconomyTransactionKind.Sell,
                    "잘못된 광물입니다.",
                    "Empty mineral id.");
            }

            if (quantity <= 0)
            {
                return CompleteFail(
                    EconomyTransactionStatus.InvalidRequest,
                    EconomyTransactionKind.Sell,
                    "판매 수량은 1 이상이어야 합니다.",
                    "Quantity must be positive.");
            }

            // 가격 원천: 카탈로그만. UI/호출자가 단가를 넘기지 않는다.
            if (!catalog.TryGetMineral(mineralId, out var info))
            {
                return CompleteFail(
                    EconomyTransactionStatus.InvalidRequest,
                    EconomyTransactionKind.Sell,
                    "알 수 없는 광물입니다.",
                    "Unknown mineral id.");
            }

            if (info.UnitPrice < 0)
            {
                return CompleteFail(
                    EconomyTransactionStatus.InvalidRequest,
                    EconomyTransactionKind.Sell,
                    "판매할 수 없는 광물입니다.",
                    "Negative unit price.");
            }

            var owned = inventory.State.GetQuantity(mineralId);
            if (owned < quantity)
            {
                return CompleteFail(
                    EconomyTransactionStatus.InsufficientResources,
                    EconomyTransactionKind.Sell,
                    "보유 수량이 부족합니다.",
                    "Owned=" + owned + " need=" + quantity);
            }

            // 골드 오버플로 사전 검사. 차감 전에 거부해야 부분 적용이 없다.
            // EconomyPricing과 동일 규칙 공유.
            if (!EconomyPricing.TryComputeGoldGain(info.UnitPrice, quantity, out var goldGain, out var goldDiag))
            {
                return CompleteFail(
                    EconomyTransactionStatus.GoldOverflow,
                    EconomyTransactionKind.Sell,
                    "골드 한도를 초과합니다.",
                    goldDiag);
            }

            var currentGold = gameState.Player.Gold;
            if (!EconomyPricing.TryAddBonus(goldGain, GoldGainBonusPercent, currentGold,
                out var goldBonus, out goldGain, out _))
            {
                return CompleteFail(
                    EconomyTransactionStatus.GoldOverflow,
                    EconomyTransactionKind.Sell,
                    "골드 한도를 초과합니다.",
                    "Gold balance overflow.");
            }

            // 커밋: 인벤 차감 → 골드 증가. 차감이 실패하면 골드를 건드리지 않는다.
            var reduce = inventory.TryReduceMineral(mineralId, quantity);
            if (!reduce.DidChange || reduce.Status != InventoryMutationStatus.Success)
            {
                // 사전 검증과 불일치(경합) — 상태 유지.
                return CompleteFail(
                    EconomyTransactionStatus.SpendFailed,
                    EconomyTransactionKind.Sell,
                    "판매에 실패했습니다.",
                    "Reduce failed after pre-check: " + reduce.Status);
            }

            gameState.AddGold(goldGain);

            var result = EconomyTransactionResult.OkSell(mineralId, quantity, goldGain,
                goldBonus > 0 ? "판매 완료  " + EconomyPricing.FormatGoldGain(goldGain, goldBonus) : null);
            LastResult = result;
            TransactionCompleted?.Invoke(result);
            // 성공 시 자동 저장 요청 1회.
            AutoSaveRequested?.Invoke(
                new EconomyAutoSaveRequest(EconomyTransactionKind.Sell, mineralId, quantity, goldGain));
            return result;
        }

        /// <summary>
        /// 여러 광물 일괄 판매(B-136). 보너스는 단건 판매와 같은 규칙으로 광물별로 계산해 합산한다.
        /// 전 항목을 사전 검증한 뒤 TryReduceMany 한 번으로 차감하고 골드를 한 번에 지급한다.
        /// 하나라도 실패하면 인벤토리·골드 모두 불변이며 자동 저장 요청은 성공 시 1회만 발행한다.
        /// </summary>
        public EconomyTransactionResult TrySellMinerals(IReadOnlyList<KeyValuePair<string, int>> items)
        {
            if (inventory == null || catalog == null || gameState == null)
            {
                return CompleteFail(
                    EconomyTransactionStatus.DependencyMissing,
                    EconomyTransactionKind.Sell,
                    "필수 서비스가 없습니다.",
                    "Inventory, catalog, or GameState missing.");
            }

            if (sellGate != null && !sellGate.IsSellAllowed)
            {
                var deny = string.IsNullOrEmpty(sellGate.DenyReason)
                    ? "Surface Base에서만 판매할 수 있습니다."
                    : sellGate.DenyReason;
                return CompleteFail(
                    EconomyTransactionStatus.InvalidRequest,
                    EconomyTransactionKind.Sell,
                    deny,
                    "SellGate denied.");
            }

            if (items == null || items.Count == 0)
            {
                return CompleteFail(
                    EconomyTransactionStatus.InvalidRequest,
                    EconomyTransactionKind.Sell,
                    "판매할 자원을 선택하세요.",
                    "Empty sell list.");
            }

            // 같은 ID는 합산한다. 순서는 첫 등장 순서를 유지한다.
            var merged = new List<KeyValuePair<string, int>>(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                var id = items[i].Key;
                var quantity = items[i].Value;
                if (string.IsNullOrEmpty(id) || quantity <= 0)
                {
                    return CompleteFail(
                        EconomyTransactionStatus.InvalidRequest,
                        EconomyTransactionKind.Sell,
                        "판매 수량은 1 이상이어야 합니다.",
                        "Invalid id or quantity in batch.");
                }

                var found = false;
                for (var j = 0; j < merged.Count; j++)
                {
                    if (merged[j].Key != id)
                    {
                        continue;
                    }

                    if (merged[j].Value > int.MaxValue - quantity)
                    {
                        return CompleteFail(
                            EconomyTransactionStatus.InvalidRequest,
                            EconomyTransactionKind.Sell,
                            "판매 수량이 너무 큽니다.",
                            "Batch quantity overflow.");
                    }

                    merged[j] = new KeyValuePair<string, int>(id, merged[j].Value + quantity);
                    found = true;
                    break;
                }

                if (!found)
                {
                    merged.Add(new KeyValuePair<string, int>(id, quantity));
                }
            }

            long total = 0;
            long bonusTotal = 0;
            var totalQuantity = 0;
            var bonusPercent = GoldGainBonusPercent;
            for (var i = 0; i < merged.Count; i++)
            {
                var id = merged[i].Key;
                var quantity = merged[i].Value;
                // 가격 원천: 카탈로그만.
                if (!catalog.TryGetMineral(id, out var info) || info.UnitPrice < 0)
                {
                    return CompleteFail(
                        EconomyTransactionStatus.InvalidRequest,
                        EconomyTransactionKind.Sell,
                        "판매할 수 없는 광물이 있습니다.",
                        "Unknown or unsellable id=" + id);
                }

                var owned = inventory.State.GetQuantity(id);
                if (owned < quantity)
                {
                    return CompleteFail(
                        EconomyTransactionStatus.InsufficientResources,
                        EconomyTransactionKind.Sell,
                        "보유 수량이 부족합니다.",
                        "id=" + id + " owned=" + owned + " need=" + quantity);
                }

                if (!EconomyPricing.TryComputeGoldGain(info.UnitPrice, quantity, out var lineGold, out var lineDiag))
                {
                    return CompleteFail(
                        EconomyTransactionStatus.GoldOverflow,
                        EconomyTransactionKind.Sell,
                        "골드 한도를 초과합니다.",
                        lineDiag);
                }

                var lineBonus = EconomyPricing.ComputeGoldBonus(lineGold, bonusPercent);
                total += (long)lineGold + lineBonus;
                bonusTotal += lineBonus;
                totalQuantity = totalQuantity > int.MaxValue - quantity ? int.MaxValue : totalQuantity + quantity;
            }

            if (total > (long)int.MaxValue - gameState.Player.Gold)
            {
                return CompleteFail(
                    EconomyTransactionStatus.GoldOverflow,
                    EconomyTransactionKind.Sell,
                    "골드 한도를 초과합니다.",
                    "Gold balance overflow.");
            }

            // 커밋: 일괄 차감 → 골드 증가. 차감이 실패하면 골드를 건드리지 않는다.
            var reduce = inventory.TryReduceMany(merged);
            if (!reduce.DidChange || reduce.Status != InventoryMutationStatus.Success)
            {
                return CompleteFail(
                    EconomyTransactionStatus.SpendFailed,
                    EconomyTransactionKind.Sell,
                    "판매에 실패했습니다.",
                    "TryReduceMany failed after pre-check: " + reduce.Status);
            }

            var goldGain = (int)total;
            gameState.AddGold(goldGain);

            var primaryId = merged.Count == 1 ? merged[0].Key : string.Empty;
            var result = EconomyTransactionResult.OkSell(primaryId, totalQuantity, goldGain,
                merged.Count + "종 판매 완료  +" + EconomyPricing.FormatGoldGain(goldGain, (int)bonusTotal));
            LastResult = result;
            TransactionCompleted?.Invoke(result);
            AutoSaveRequested?.Invoke(
                new EconomyAutoSaveRequest(EconomyTransactionKind.Sell, merged[0].Key, totalQuantity, goldGain));
            return result;
        }

        /// <summary>
        /// 읽기 전용 지불 가능 검사. 동일 ID 비용을 합산한 뒤 보유량과 비교한다.
        /// State·이벤트·예약을 변경하지 않는다.
        /// </summary>
        public bool CanAfford(IReadOnlyList<ItemCostDto> costs)
        {
            return TryValidateSpend(costs, out _, out _) == null;
        }

        /// <summary>골드는 GameState, 그 외는 인벤토리 보유량. 상태를 바꾸지 않는다.</summary>
        public int GetOwnedQuantity(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return 0;
            }

            if (itemId == DataIds.Currency.Gold)
            {
                return gameState != null ? gameState.Player.Gold : 0;
            }

            return inventory != null ? inventory.State.GetQuantity(itemId) : 0;
        }

        /// <summary>
        /// 비용을 재검증한 뒤 전량 보유 시에만 한 번에 차감한다.
        /// 부분 루프 차감 금지 — InventoryService.TryReduceMany 일괄 경로 사용.
        /// </summary>
        public bool TrySpend(IReadOnlyList<ItemCostDto> costs)
        {
            var fail = TryValidateSpend(costs, out var normalized, out var diagnostic);
            if (fail != null)
            {
                LastResult = fail.Value;
                TransactionCompleted?.Invoke(fail.Value);
                // 실패는 상태 이벤트를 발생시키지 않는다(인벤/골드 불변).
                return false;
            }

            if (normalized.Count == 0)
            {
                // 무료: 차감은 없지만 성공 거래로 취급해 자동 저장 훅은 1회 연다.
                var free = EconomyTransactionResult.OkSpend(string.Empty, 0, "비용 없음");
                LastResult = free;
                TransactionCompleted?.Invoke(free);
                AutoSaveRequested?.Invoke(
                    new EconomyAutoSaveRequest(EconomyTransactionKind.Spend, string.Empty, 0, 0));
                return true;
            }

            var pairs = new List<KeyValuePair<string, int>>(normalized.Count);
            var goldCost = 0;
            var totalQty = 0;
            for (var i = 0; i < normalized.Count; i++)
            {
                if (normalized[i].ItemId == DataIds.Currency.Gold)
                    goldCost = normalized[i].Quantity;
                else
                    pairs.Add(new KeyValuePair<string, int>(normalized[i].ItemId, normalized[i].Quantity));
                totalQty += normalized[i].Quantity;
            }

            var reduce = pairs.Count > 0 ? inventory.TryReduceMany(pairs) : default;
            if (pairs.Count > 0 && (reduce.Status != InventoryMutationStatus.Success || !reduce.DidChange))
            {
                // 사전 검증 후 실패는 경합. 부분 차감은 TryReduceMany가 막는다.
                var spendFail = EconomyTransactionResult.Fail(
                    EconomyTransactionStatus.SpendFailed,
                    EconomyTransactionKind.Spend,
                    "자원 차감에 실패했습니다.",
                    "TryReduceMany failed: " + reduce.Status + " " + diagnostic);
                LastResult = spendFail;
                TransactionCompleted?.Invoke(spendFail);
                return false;
            }

            if (goldCost > 0) gameState.AddGold(-goldCost);

            var primaryId = normalized[0].ItemId;
            var result = EconomyTransactionResult.OkSpend(primaryId, totalQty);
            LastResult = result;
            TransactionCompleted?.Invoke(result);
            AutoSaveRequested?.Invoke(
                new EconomyAutoSaveRequest(EconomyTransactionKind.Spend, primaryId, totalQty, 0));
            return true;
        }

        /// <summary>
        /// 지불 검증 공통 경로. 성공 시 null, 실패 시 결과 반환.
        /// CanAfford와 TrySpend가 동일 규칙을 쓰도록 한곳으로 모은다.
        /// </summary>
        private EconomyTransactionResult? TryValidateSpend(
            IReadOnlyList<ItemCostDto> costs,
            out List<ItemCostDto> normalized,
            out string diagnostic)
        {
            normalized = new List<ItemCostDto>();
            diagnostic = string.Empty;

            if (inventory == null || catalog == null)
            {
                return EconomyTransactionResult.Fail(
                    EconomyTransactionStatus.DependencyMissing,
                    EconomyTransactionKind.Spend,
                    "필수 서비스가 없습니다.",
                    "Inventory or catalog missing.");
            }

            if (!CostAggregator.TryNormalize(costs, out normalized, out diagnostic))
            {
                return EconomyTransactionResult.Fail(
                    EconomyTransactionStatus.InvalidRequest,
                    EconomyTransactionKind.Spend,
                    "비용 데이터가 올바르지 않습니다.",
                    diagnostic);
            }

            for (var i = 0; i < normalized.Count; i++)
            {
                var entry = normalized[i];
                // 비용 아이템은 MVP에서 광물 카탈로그로 검증한다.
                if (entry.ItemId == DataIds.Currency.Gold && gameState == null)
                {
                    return EconomyTransactionResult.Fail(EconomyTransactionStatus.DependencyMissing,
                        EconomyTransactionKind.Spend, "골드 지갑이 없습니다.");
                }
                if (entry.ItemId != DataIds.Currency.Gold && !catalog.TryGetMineral(entry.ItemId, out _))
                {
                    diagnostic = "Unknown cost item id=" + entry.ItemId;
                    return EconomyTransactionResult.Fail(
                        EconomyTransactionStatus.InvalidRequest,
                        EconomyTransactionKind.Spend,
                        "알 수 없는 비용 항목입니다.",
                        diagnostic);
                }

                var owned = entry.ItemId == DataIds.Currency.Gold
                    ? gameState.Player.Gold : inventory.State.GetQuantity(entry.ItemId);
                if (owned < entry.Quantity)
                {
                    diagnostic = "Insufficient id=" + entry.ItemId
                        + " owned=" + owned + " need=" + entry.Quantity;
                    return EconomyTransactionResult.Fail(
                        EconomyTransactionStatus.InsufficientResources,
                        EconomyTransactionKind.Spend,
                        "자원이 부족합니다.",
                        diagnostic);
                }
            }

            return null;
        }

        private EconomyTransactionResult CompleteFail(
            EconomyTransactionStatus status,
            EconomyTransactionKind kind,
            string userMessage,
            string diagnostic)
        {
            // 실패 사용자 메시지와 디버그 진단을 분리. 세이브 원문·전체 덤프는 남기지 않는다.
            if (status == EconomyTransactionStatus.InvalidRequest
                || status == EconomyTransactionStatus.GoldOverflow
                || status == EconomyTransactionStatus.DependencyMissing)
            {
                Debug.LogWarning("[SubTerra] Economy rejected: " + status + " " + (diagnostic ?? string.Empty));
            }

            var result = EconomyTransactionResult.Fail(status, kind, userMessage, diagnostic);
            LastResult = result;
            TransactionCompleted?.Invoke(result);
            return result;
        }
    }
}
