using System;
using SubTerra.App.State;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Inventory
{
    /// <summary>
    /// B측 인벤토리 서비스. Shared IMiningRewardReceiver 구현체.
    /// 검증 → (용량 내 완전 단위 수락) → 스택 커밋 → 합산 1회 → 이벤트 1회 순으로 처리한다.
    /// Shared 계약에 지급 ID가 없으므로 중복 지급 제거를 추측 구현하지 않는다.
    /// </summary>
    public sealed class InventoryService : IMiningRewardReceiver
    {
        private readonly InventoryState state;
        private readonly IMineralCatalogLookup catalog;
        private GameState gameState;

        public InventoryState State => state;
        public InventoryMutationResult LastResult { get; private set; }

        /// <summary>성공 변이 후 전체 스냅샷. InventoryPanel이 구독한다.</summary>
        public event Action<InventorySnapshot> InventoryChanged;

        public InventoryService(
            IMineralCatalogLookup catalog,
            float maxCapacity = InventoryState.DefaultMaxCapacity,
            GameState gameState = null)
        {
            this.catalog = catalog;
            state = new InventoryState(maxCapacity);
            this.gameState = gameState;
            LastResult = InventoryMutationResult.Invalid(
                InventoryMutationStatus.InvalidQuantity,
                string.Empty,
                0,
                "No mutation yet.");
            // 시작 합산(빈 인벤토리)을 GameState 읽기 모델과 맞춘다.
            RecomputeAggregates();
            PushAggregatesToGameState();
        }

        /// <summary>HUD용 GameState 연결. 성공 변이 시 cargo/unsettled를 한 번에 밀어 넣는다.</summary>
        public void BindGameState(GameState target)
        {
            gameState = target;
            PushAggregatesToGameState();
        }

        /// <summary>Shared 계약 진입점. 상세 결과는 LastResult와 내부 이벤트로 남긴다.</summary>
        public void AddMineral(string mineralId, int quantity)
        {
            TryAddMineral(mineralId, quantity);
        }

        /// <summary>B 내부/테스트용. 수락·거절·진단 결과를 반환한다.</summary>
        public InventoryMutationResult TryAddMineral(string mineralId, int quantity)
        {
            if (catalog == null)
            {
                return Fail(InventoryMutationStatus.CatalogMissing, mineralId, quantity, "Catalog missing.");
            }

            if (string.IsNullOrEmpty(mineralId))
            {
                return Fail(InventoryMutationStatus.InvalidId, mineralId, quantity, "Empty mineral id.");
            }

            // 0·음수는 상태를 바꾸지 않는다.
            if (quantity <= 0)
            {
                return Fail(InventoryMutationStatus.InvalidQuantity, mineralId, quantity, "Quantity must be positive.");
            }

            if (!catalog.TryGetMineral(mineralId, out var info))
            {
                return Fail(InventoryMutationStatus.InvalidId, mineralId, quantity, "Unknown mineral id.");
            }

            if (info.UnitWeight <= 0f)
            {
                return Fail(InventoryMutationStatus.InvalidId, mineralId, quantity, "Invalid unit weight.");
            }

            var existing = state.GetQuantity(mineralId);
            // existing + quantity 가 int 범위를 넘으면 거부(원자 실패).
            if (existing > int.MaxValue - quantity)
            {
                return Fail(InventoryMutationStatus.OverflowRisk, mineralId, quantity, "Quantity overflow risk.");
            }

            var remaining = state.MaxCapacity - state.CurrentWeight;
            if (remaining < 0f)
            {
                remaining = 0f;
            }

            var maxFit = InventoryCalculator.MaxFittingUnits(remaining, info.UnitWeight);
            var accepted = quantity <= maxFit ? quantity : maxFit;

            if (accepted <= 0)
            {
                // 한 단위도 못 넣음: State·이벤트 불변, 거절량만 결과로 노출.
                LastResult = InventoryMutationResult.Accepted(
                    InventoryMutationStatus.CapacityFull,
                    mineralId,
                    quantity,
                    0,
                    "No capacity for whole unit.");
                return LastResult;
            }

            var status = accepted == quantity
                ? InventoryMutationStatus.Success
                : InventoryMutationStatus.PartialAccept;

            state.SetQuantity(mineralId, existing + accepted);
            RecomputeAggregates();
            // 성공 논리 변경당 이벤트 1회: GameState 합산 + 상세 스냅샷.
            RaiseChangedOnce();

            LastResult = InventoryMutationResult.Accepted(
                status,
                mineralId,
                quantity,
                accepted,
                status == InventoryMutationStatus.PartialAccept ? "Partial accept by capacity." : null);
            return LastResult;
        }

        /// <summary>
        /// 보관함·정산 등 감소 공통 경로. 사전 검증 후 원자적으로 반영한다.
        /// 일부만 먼저 깎고 실패하는 분기를 두지 않는다.
        /// </summary>
        public InventoryMutationResult TryReduceMineral(string mineralId, int quantity)
        {
            if (catalog == null)
            {
                return Fail(InventoryMutationStatus.CatalogMissing, mineralId, quantity, "Catalog missing.");
            }

            if (string.IsNullOrEmpty(mineralId))
            {
                return Fail(InventoryMutationStatus.InvalidId, mineralId, quantity, "Empty mineral id.");
            }

            if (quantity <= 0)
            {
                return Fail(InventoryMutationStatus.InvalidQuantity, mineralId, quantity, "Quantity must be positive.");
            }

            if (!catalog.TryGetMineral(mineralId, out _))
            {
                return Fail(InventoryMutationStatus.InvalidId, mineralId, quantity, "Unknown mineral id.");
            }

            var existing = state.GetQuantity(mineralId);
            if (existing < quantity)
            {
                return Fail(InventoryMutationStatus.Insufficient, mineralId, quantity, "Insufficient quantity.");
            }

            state.SetQuantity(mineralId, existing - quantity);
            RecomputeAggregates();
            RaiseChangedOnce();

            LastResult = InventoryMutationResult.Accepted(
                InventoryMutationStatus.Success,
                mineralId,
                quantity,
                quantity);
            return LastResult;
        }

        /// <summary>
        /// 여러 광물을 전량 사전 검증한 뒤 한 트랜잭션으로 차감한다.
        /// 하나라도 부족·무효면 상태를 바꾸지 않으며, 성공 시 InventoryChanged는 1회만 발행한다.
        /// Economy TrySpend가 부분 루프 차감을 쓰지 않도록 일괄 경로를 제공한다.
        /// 입력 항목은 이미 동일 ID 합산이 끝난 정규화 목록을 권장한다.
        /// </summary>
        public InventoryMutationResult TryReduceMany(
            System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, int>> reductions)
        {
            if (catalog == null)
            {
                return Fail(InventoryMutationStatus.CatalogMissing, string.Empty, 0, "Catalog missing.");
            }

            if (reductions == null || reductions.Count == 0)
            {
                // 비용 0건은 성공(무변경). 이벤트는 발행하지 않는다.
                LastResult = InventoryMutationResult.Accepted(
                    InventoryMutationStatus.Success,
                    string.Empty,
                    0,
                    0,
                    "Empty reduction list.");
                return LastResult;
            }

            // 1단계: 전 항목 검증. 여기서 실패하면 SetQuantity를 한 번도 호출하지 않는다.
            for (var i = 0; i < reductions.Count; i++)
            {
                var mineralId = reductions[i].Key;
                var quantity = reductions[i].Value;

                if (string.IsNullOrEmpty(mineralId))
                {
                    return Fail(InventoryMutationStatus.InvalidId, mineralId, quantity, "Empty mineral id.");
                }

                if (quantity <= 0)
                {
                    return Fail(
                        InventoryMutationStatus.InvalidQuantity,
                        mineralId,
                        quantity,
                        "Quantity must be positive.");
                }

                if (!catalog.TryGetMineral(mineralId, out _))
                {
                    return Fail(InventoryMutationStatus.InvalidId, mineralId, quantity, "Unknown mineral id.");
                }

                var existing = state.GetQuantity(mineralId);
                if (existing < quantity)
                {
                    return Fail(
                        InventoryMutationStatus.Insufficient,
                        mineralId,
                        quantity,
                        "Insufficient quantity.");
                }
            }

            // 2단계: 검증 통과 후에만 일괄 적용. 중간 이벤트 없음.
            var totalRequested = 0;
            var firstId = reductions[0].Key;
            for (var i = 0; i < reductions.Count; i++)
            {
                var mineralId = reductions[i].Key;
                var quantity = reductions[i].Value;
                var existing = state.GetQuantity(mineralId);
                state.SetQuantity(mineralId, existing - quantity);
                totalRequested += quantity;
            }

            RecomputeAggregates();
            RaiseChangedOnce();

            LastResult = InventoryMutationResult.Accepted(
                InventoryMutationStatus.Success,
                firstId,
                totalRequested,
                totalRequested);
            return LastResult;
        }

        public InventorySnapshot GetSnapshot()
        {
            return state.CreateSnapshot(catalog);
        }

        public float CurrentWeight => state.CurrentWeight;
        public float MaxCapacity => state.MaxCapacity;
        public float UnsettledValue => state.UnsettledValue;

        /// <summary>
        /// 업그레이드 파생 최대 화물 중량을 반영한다.
        /// 보유 화물은 버리지 않고 최대치 표시/추가 획득 경계만 즉시 갱신한다.
        /// </summary>
        public void SetMaximumCapacity(float maximumCapacity)
        {
            var clamped = maximumCapacity < 0f ? 0f : maximumCapacity;
            if (Math.Abs(state.MaxCapacity - clamped) < 0.0001f)
            {
                return;
            }

            state.SetMaxCapacity(clamped);
            RaiseChangedOnce();
        }

        private InventoryMutationResult Fail(
            InventoryMutationStatus status,
            string mineralId,
            int quantity,
            string diagnostic)
        {
            // 실패 로그: ID·수량 수준만. 세이브 경로·전체 덤프는 남기지 않는다.
            if (status == InventoryMutationStatus.InvalidId
                || status == InventoryMutationStatus.InvalidQuantity
                || status == InventoryMutationStatus.OverflowRisk)
            {
                Debug.LogWarning("[SubTerra] Inventory rejected: " + status + " id=" + (mineralId ?? string.Empty));
            }

            LastResult = InventoryMutationResult.Invalid(status, mineralId, quantity, diagnostic);
            return LastResult;
        }

        private void RecomputeAggregates()
        {
            var weight = InventoryCalculator.ComputeTotalWeight(state.Quantities, catalog);
            var value = InventoryCalculator.ComputeUnsettledValue(state.Quantities, catalog);
            state.ApplyAggregates(weight, value);
        }

        private void RaiseChangedOnce()
        {
            PushAggregatesToGameState();
            InventoryChanged?.Invoke(GetSnapshot());
        }

        private void PushAggregatesToGameState()
        {
            if (gameState == null)
            {
                return;
            }

            // 화물·가치를 한 번에 설정해 InventoryChanged가 두 번 나가지 않게 한다.
            gameState.SetInventory(state.CurrentWeight, state.UnsettledValue);
        }
    }
}
