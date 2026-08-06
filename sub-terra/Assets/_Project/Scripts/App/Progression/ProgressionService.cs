using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Economy;
using SubTerra.Shared;
// ItemDisplayNames: 업그레이드 표시 이름 한국어 폴백

namespace SubTerra.App.Progression
{
    /// <summary>
    /// 데이터 기반 업그레이드 구매와 심층 잠금 해제를 처리한다.
    /// 정의/현재 레벨/비용을 모두 검증한 뒤 지갑을 한 번만 호출하고,
    /// 차감 성공 직후 실패할 수 없는 레벨 커밋만 수행한다.
    /// </summary>
    public sealed class ProgressionService
    {
        private readonly UpgradeState state;
        private readonly IUpgradeCatalog catalog;
        private readonly IResourceWallet wallet;
        private readonly UpgradeEffectProvider effects;

        public UpgradeState State => state;
        public IUpgradeEffectProvider Effects => effects;
        public ProgressionPurchaseResult LastPurchaseResult { get; private set; }

        public event Action<ProgressionPurchaseResult> PurchaseCompleted;
        public event Action<UpgradeSnapshot> UpgradeChanged;
        public event Action<ZoneAccessResult> DeepZoneAccessChanged;
        public event Action<ProgressionAutoSaveRequest> AutoSaveRequested;

        public ProgressionService(
            UpgradeState state,
            IUpgradeCatalog catalog,
            IResourceWallet wallet)
        {
            this.state = state;
            this.catalog = catalog;
            this.wallet = wallet;
            effects = new UpgradeEffectProvider(state, catalog);
            LastPurchaseResult = ProgressionPurchaseResult.Fail(
                ProgressionPurchaseStatus.InvalidRequest,
                string.Empty,
                0,
                "구매 내역이 없습니다.",
                "No purchase yet.");
        }

        public ProgressionPurchaseResult TryPurchase(string upgradeId)
        {
            if (state == null || catalog == null || wallet == null)
            {
                return CompleteFailure(
                    ProgressionPurchaseStatus.DependencyMissing,
                    upgradeId,
                    0,
                    "필수 서비스가 없습니다.",
                    "State, catalog, or wallet missing.");
            }

            if (string.IsNullOrEmpty(upgradeId))
            {
                return CompleteFailure(
                    ProgressionPurchaseStatus.InvalidRequest,
                    upgradeId,
                    0,
                    "잘못된 업그레이드입니다.",
                    "Empty upgrade id.");
            }

            if (!catalog.TryGetUpgrade(upgradeId, out var data) || data == null)
            {
                return CompleteFailure(
                    ProgressionPurchaseStatus.UpgradeNotFound,
                    upgradeId,
                    state.GetLevel(upgradeId),
                    "업그레이드를 찾을 수 없습니다.",
                    "Unknown upgrade id.");
            }

            var currentLevel = state.GetLevel(upgradeId);
            if (data.MaxLevel <= 0
                || data.Levels == null
                || data.Levels.Count != data.MaxLevel
                || currentLevel < 0
                || currentLevel > data.MaxLevel)
            {
                return CompleteFailure(
                    ProgressionPurchaseStatus.InvalidDefinition,
                    upgradeId,
                    currentLevel,
                    "업그레이드 데이터가 올바르지 않습니다.",
                    "Invalid maximum level, level count, or restored level.");
            }

            if (currentLevel == data.MaxLevel)
            {
                return CompleteFailure(
                    ProgressionPurchaseStatus.MaximumLevel,
                    upgradeId,
                    currentLevel,
                    "이미 최대 레벨입니다.",
                    "Maximum level reached.");
            }

            var nextLevelNumber = currentLevel + 1;
            var nextLevel = data.Levels[currentLevel];
            if (nextLevel == null
                || nextLevel.Level != nextLevelNumber
                || nextLevel.EffectValue <= 0f
                || float.IsNaN(nextLevel.EffectValue)
                || float.IsInfinity(nextLevel.EffectValue)
                || nextLevel.Costs == null
                || nextLevel.Costs.Count == 0)
            {
                return CompleteFailure(
                    ProgressionPurchaseStatus.InvalidDefinition,
                    upgradeId,
                    currentLevel,
                    "업그레이드 단계 데이터가 올바르지 않습니다.",
                    "Missing or invalid next level, effect, or costs.");
            }

            var costs = ItemCostMapping.ToDtoList(nextLevel.Costs);
            if (!CostAggregator.TryNormalize(costs, out var normalizedCosts, out var costDiagnostic)
                || normalizedCosts.Count == 0)
            {
                return CompleteFailure(
                    ProgressionPurchaseStatus.InvalidDefinition,
                    upgradeId,
                    currentLevel,
                    "업그레이드 비용 데이터가 올바르지 않습니다.",
                    costDiagnostic);
            }

            // 여기까지 모든 정의/상태 검증을 끝낸다. CanAfford는 읽기 전용이다.
            if (!wallet.CanAfford(normalizedCosts))
            {
                return CompleteFailure(
                    ProgressionPurchaseStatus.InsufficientResources,
                    upgradeId,
                    currentLevel,
                    "업그레이드 비용이 부족합니다.",
                    "Wallet cannot afford normalized costs.");
            }

            // 지갑은 전량 검증 후 일괄 차감한다. 성공 뒤 남은 커밋은 예외 없는 정수 레벨 대입뿐이다.
            if (!wallet.TrySpend(normalizedCosts))
            {
                return CompleteFailure(
                    ProgressionPurchaseStatus.SpendFailed,
                    upgradeId,
                    currentLevel,
                    "업그레이드 비용 차감에 실패했습니다.",
                    "Wallet spend failed after affordability check.");
            }

            state.ApplyPurchasedLevel(upgradeId, nextLevelNumber);

            var result = ProgressionPurchaseResult.Success(
                upgradeId,
                currentLevel,
                nextLevelNumber,
                nextLevel.EffectValue);
            LastPurchaseResult = result;
            PurchaseCompleted?.Invoke(result);
            if (TryGetSnapshot(upgradeId, out var snapshot))
            {
                UpgradeChanged?.Invoke(snapshot);
            }

            // Phase K는 이 훅에서 업그레이드 레벨 커밋이 끝난 상태를 저장한다.
            AutoSaveRequested?.Invoke(new ProgressionAutoSaveRequest(upgradeId, nextLevelNumber));
            return result;
        }

        public IReadOnlyList<UpgradeSnapshot> GetSnapshots()
        {
            var snapshots = new List<UpgradeSnapshot>();
            if (catalog?.Upgrades == null)
            {
                return snapshots;
            }

            for (var i = 0; i < catalog.Upgrades.Count; i++)
            {
                var data = catalog.Upgrades[i];
                if (data != null && TryGetSnapshot(data.Id, out var snapshot))
                {
                    snapshots.Add(snapshot);
                }
            }

            return snapshots;
        }

        public bool TryGetSnapshot(string upgradeId, out UpgradeSnapshot snapshot)
        {
            snapshot = default;
            if (state == null
                || catalog == null
                || string.IsNullOrEmpty(upgradeId)
                || !catalog.TryGetUpgrade(upgradeId, out var data)
                || data == null)
            {
                return false;
            }

            var current = state.GetLevel(upgradeId);
            var currentEffect = effects.GetCurrentEffect(upgradeId);
            var nextEffect = 0f;
            IReadOnlyList<ItemCostDto> nextCosts = Array.Empty<ItemCostDto>();
            var canAffordNextLevel = false;
            if (current >= 0
                && current < data.MaxLevel
                && data.Levels != null
                && current < data.Levels.Count
                && data.Levels[current] != null)
            {
                nextEffect = data.Levels[current].EffectValue;
                var rawCosts = ItemCostMapping.ToDtoList(data.Levels[current].Costs);
                // TryPurchase와 동일하게 정규화한 뒤 지불 가능 여부를 판정한다.
                if (CostAggregator.TryNormalize(rawCosts, out var normalized, out _)
                    && normalized.Count > 0)
                {
                    nextCosts = normalized;
                    canAffordNextLevel = wallet != null && wallet.CanAfford(normalized);
                }
            }

            var displayName = ItemDisplayNames.PreferDisplay(data.Id, data.DisplayName);
            snapshot = new UpgradeSnapshot(
                data.Id,
                displayName,
                current,
                data.MaxLevel,
                currentEffect,
                nextEffect,
                nextCosts,
                canAffordNextLevel);
            return true;
        }

        /// <summary>현재 진행도로 심층 접근 가능 여부와 첫 미충족 이유를 읽는다.</summary>
        public ZoneAccessResult GetDeepZoneAccess(
            int completedObjectives,
            DeepZoneUnlockRule rule = null)
        {
            if (state == null)
            {
                return new ZoneAccessResult(false, false, "진행도 상태가 없습니다.");
            }

            if (state.IsZoneUnlocked(DataIds.Zones.Deep))
            {
                return new ZoneAccessResult(true, false, "심층 구역 잠금 해제됨");
            }

            var activeRule = rule ?? DeepZoneUnlockRule.Mvp;
            if (completedObjectives < activeRule.RequiredCompletedObjectives)
            {
                return new ZoneAccessResult(
                    false,
                    false,
                    $"목표 완료 {activeRule.RequiredCompletedObjectives}개 필요");
            }

            for (var i = 0; i < activeRule.UpgradeRequirements.Count; i++)
            {
                var requirement = activeRule.UpgradeRequirements[i];
                var current = state.GetLevel(requirement.UpgradeId);
                if (string.IsNullOrEmpty(requirement.UpgradeId)
                    || requirement.RequiredLevel <= 0
                    || current < requirement.RequiredLevel)
                {
                    return new ZoneAccessResult(
                        false,
                        false,
                        $"{requirement.UpgradeId} {requirement.RequiredLevel}레벨 필요");
                }
            }

            return new ZoneAccessResult(true, false, "심층 구역 잠금 해제 조건 충족");
        }

        /// <summary>조건을 만족한 심층 잠금을 영구 상태에 한 번만 기록한다.</summary>
        public ZoneAccessResult TryUnlockDeepZone(
            int completedObjectives,
            DeepZoneUnlockRule rule = null)
        {
            var access = GetDeepZoneAccess(completedObjectives, rule);
            if (!access.IsUnlocked || state == null || state.IsZoneUnlocked(DataIds.Zones.Deep))
            {
                return access;
            }

            var didUnlock = state.ApplyZoneUnlock(DataIds.Zones.Deep);
            var result = new ZoneAccessResult(true, didUnlock, "심층 구역 잠금 해제됨");
            if (didUnlock)
            {
                DeepZoneAccessChanged?.Invoke(result);
            }

            return result;
        }

        private ProgressionPurchaseResult CompleteFailure(
            ProgressionPurchaseStatus status,
            string upgradeId,
            int currentLevel,
            string userMessage,
            string diagnostic)
        {
            var result = ProgressionPurchaseResult.Fail(
                status,
                upgradeId,
                currentLevel,
                userMessage,
                diagnostic);
            LastPurchaseResult = result;
            PurchaseCompleted?.Invoke(result);
            return result;
        }
    }
}
