using System;
using System.Collections.Generic;
using SubTerra.App.Inventory;
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
    public sealed class EconomyService : IResourceWallet
    {
        private readonly InventoryService inventory;
        private readonly IMineralCatalogLookup catalog;
        private readonly GameState gameState;

        /// <summary>성공·실패 모두 발행. 실패는 상태 이벤트를 동반하지 않는다.</summary>
        public event Action<EconomyTransactionResult> TransactionCompleted;

        /// <summary>성공 거래 직후 1회. Phase K 자동 저장 구독 지점.</summary>
        public event Action<EconomyAutoSaveRequest> AutoSaveRequested;

        public EconomyTransactionResult LastResult { get; private set; }

        public EconomyService(
            InventoryService inventory,
            IMineralCatalogLookup catalog,
            GameState gameState)
        {
            this.inventory = inventory;
            this.catalog = catalog;
            this.gameState = gameState;
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
            if (!TryComputeGoldGain(info.UnitPrice, quantity, out var goldGain, out var goldDiag))
            {
                return CompleteFail(
                    EconomyTransactionStatus.GoldOverflow,
                    EconomyTransactionKind.Sell,
                    "골드 한도를 초과합니다.",
                    goldDiag);
            }

            var currentGold = gameState.Player.Gold;
            if (currentGold > int.MaxValue - goldGain)
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

            var result = EconomyTransactionResult.OkSell(mineralId, quantity, goldGain);
            LastResult = result;
            TransactionCompleted?.Invoke(result);
            // 성공 시 자동 저장 요청 1회.
            AutoSaveRequested?.Invoke(
                new EconomyAutoSaveRequest(EconomyTransactionKind.Sell, mineralId, quantity, goldGain));
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
            var totalQty = 0;
            for (var i = 0; i < normalized.Count; i++)
            {
                pairs.Add(new KeyValuePair<string, int>(normalized[i].ItemId, normalized[i].Quantity));
                totalQty += normalized[i].Quantity;
            }

            var reduce = inventory.TryReduceMany(pairs);
            if (reduce.Status != InventoryMutationStatus.Success || !reduce.DidChange)
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
                if (!catalog.TryGetMineral(entry.ItemId, out _))
                {
                    diagnostic = "Unknown cost item id=" + entry.ItemId;
                    return EconomyTransactionResult.Fail(
                        EconomyTransactionStatus.InvalidRequest,
                        EconomyTransactionKind.Spend,
                        "알 수 없는 비용 항목입니다.",
                        diagnostic);
                }

                var owned = inventory.State.GetQuantity(entry.ItemId);
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

        /// <summary>단가 × 수량 골드 계산. 오버플로 시 false.</summary>
        private static bool TryComputeGoldGain(
            int unitPrice,
            int quantity,
            out int goldGain,
            out string diagnostic)
        {
            goldGain = 0;
            diagnostic = string.Empty;

            if (unitPrice == 0 || quantity == 0)
            {
                goldGain = 0;
                return true;
            }

            // unitPrice * quantity 가 int 범위를 넘는지 검사.
            if (unitPrice > 0 && quantity > int.MaxValue / unitPrice)
            {
                diagnostic = "unitPrice*quantity overflow.";
                return false;
            }

            goldGain = unitPrice * quantity;
            return true;
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
