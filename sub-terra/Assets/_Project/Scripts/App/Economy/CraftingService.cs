using System;
using System.Collections.Generic;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Economy
{
    /// <summary>
    /// 시설 제작·건설 오케스트레이션.
    /// 순서: CanAfford → 배치(TryPlace) 성공 → TrySpend.
    /// 설치 성공 전에는 자원을 차감하지 않으며, Shared 2단계 예약 결제 계약은 만들지 않는다.
    /// </summary>
    public sealed class CraftingService
    {
        private readonly IResourceWallet wallet;
        private bool busy;

        /// <summary>제작·설치 결과. UI와 테스트가 구독한다.</summary>
        public event Action<EconomyTransactionResult> CraftCompleted;

        public EconomyTransactionResult LastResult { get; private set; }

        public CraftingService(IResourceWallet wallet)
        {
            this.wallet = wallet;
            LastResult = EconomyTransactionResult.Fail(
                EconomyTransactionStatus.InvalidRequest,
                EconomyTransactionKind.Craft,
                "No craft yet.");
        }

        /// <summary>
        /// 건설/제작 흐름 전체.
        /// placement가 실패하면 TrySpend를 호출하지 않아 재고가 유지된다.
        /// busy 가드로 동일 처리 중 중복 제출을 한 건만 허용한다.
        /// </summary>
        public EconomyTransactionResult TryCraftBuilding(
            string buildingId,
            IReadOnlyList<ItemCostDto> costs,
            IBuildingPlacementGate placement)
        {
            if (busy)
            {
                var busyResult = EconomyTransactionResult.Fail(
                    EconomyTransactionStatus.Busy,
                    EconomyTransactionKind.Craft,
                    "처리 중입니다.",
                    "Re-entrant craft rejected.");
                LastResult = busyResult;
                // Busy는 상태 변경 없음. 결과 이벤트만 알려 UI가 무시 메시지를 띄울 수 있게 한다.
                CraftCompleted?.Invoke(busyResult);
                return busyResult;
            }

            busy = true;
            try
            {
                return ExecuteCraft(buildingId, costs, placement);
            }
            finally
            {
                busy = false;
            }
        }

        private EconomyTransactionResult ExecuteCraft(
            string buildingId,
            IReadOnlyList<ItemCostDto> costs,
            IBuildingPlacementGate placement)
        {
            if (wallet == null)
            {
                return Complete(
                    EconomyTransactionResult.Fail(
                        EconomyTransactionStatus.DependencyMissing,
                        EconomyTransactionKind.Craft,
                        "지갑 서비스가 없습니다.",
                        "IResourceWallet missing."));
            }

            if (placement == null)
            {
                return Complete(
                    EconomyTransactionResult.Fail(
                        EconomyTransactionStatus.DependencyMissing,
                        EconomyTransactionKind.Craft,
                        "배치 시스템이 없습니다.",
                        "IBuildingPlacementGate missing."));
            }

            if (string.IsNullOrEmpty(buildingId))
            {
                return Complete(
                    EconomyTransactionResult.Fail(
                        EconomyTransactionStatus.InvalidRequest,
                        EconomyTransactionKind.Craft,
                        "잘못된 시설입니다.",
                        "Empty building id."));
            }

            if (costs == null)
            {
                return Complete(
                    EconomyTransactionResult.Fail(
                        EconomyTransactionStatus.InvalidRequest,
                        EconomyTransactionKind.Craft,
                        "비용 데이터가 없습니다.",
                        "Costs null."));
            }

            // 1) 읽기 전용 지불 검사 — 자원 예약/차감 없음.
            if (!wallet.CanAfford(costs))
            {
                return Complete(
                    EconomyTransactionResult.Fail(
                        EconomyTransactionStatus.InsufficientResources,
                        EconomyTransactionKind.Craft,
                        "자원이 부족합니다.",
                        "CanAfford=false before placement."));
            }

            // 2) A 배치/Prefab 생성. 실패 시 TrySpend 미호출 → 재고 유지.
            if (!placement.TryPlace(buildingId))
            {
                return Complete(
                    EconomyTransactionResult.Fail(
                        EconomyTransactionStatus.PlacementFailed,
                        EconomyTransactionKind.Craft,
                        "설치에 실패했습니다.",
                        "Placement gate returned false."));
            }

            // 3) 생성 성공 후에만 실차감. TrySpend가 재검증·일괄 차감한다.
            if (!wallet.TrySpend(costs))
            {
                // 배치 성공 후 차감 실패는 경합/데이터 오류. 자원은 차감되지 않은 상태.
                // Prefab 롤백은 A측 책임(Non-goal). B는 차감하지 않았음을 보장한다.
                Debug.LogWarning(
                    "[SubTerra] Craft spend failed after placement success. buildingId=" + buildingId);
                return Complete(
                    EconomyTransactionResult.Fail(
                        EconomyTransactionStatus.SpendFailed,
                        EconomyTransactionKind.Craft,
                        "비용 차감에 실패했습니다.",
                        "TrySpend failed after placement."));
            }

            return Complete(EconomyTransactionResult.OkCraft(buildingId));
        }

        private EconomyTransactionResult Complete(EconomyTransactionResult result)
        {
            LastResult = result;
            CraftCompleted?.Invoke(result);
            return result;
        }
    }
}
