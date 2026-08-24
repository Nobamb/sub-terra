using System;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Mining
{
    public enum MiningPhase
    {
        Idle = 0,
        Mining = 1,
        Completed = 2,
        Cancelled = 3,
        Failed = 4
    }

    public enum MiningFailureReason
    {
        None = 0,
        InvalidTarget = 1,
        NotMineable = 2,
        DrillLevelTooLow = 3,
        InsufficientEnergy = 4,
        InventoryFull = 5,
        TargetChanged = 6,
        DependencyMissing = 7,
        InvalidReward = 8,
        OutOfRange = 9,
        DeepZoneLocked = 10
    }

    public readonly struct MiningProgressState
    {
        public MiningPhase Phase { get; }
        public MiningFailureReason FailureReason { get; }
        public float Progress { get; }
        public float Duration { get; }
        public int EnergyCost { get; }

        public MiningProgressState(
            MiningPhase phase,
            MiningFailureReason failureReason,
            float progress,
            float duration,
            int energyCost)
        {
            Phase = phase;
            FailureReason = failureReason;
            Progress = progress;
            Duration = duration;
            EnergyCost = energyCost;
        }
    }

    public sealed class MiningSystem : MonoBehaviour
    {
        private const string LockedSignalTileId = "tile.locked.signal";

        [SerializeField] private Tilemap foregroundTilemap;
        [SerializeField] private MiningTileResolver tileResolver;
        [SerializeField] private MonoBehaviour rewardReceiverBehaviour;
        [SerializeField] private MonoBehaviour miningTransactionBehaviour;
        [SerializeField] private MonoBehaviour upgradeEffectProviderBehaviour;
        [SerializeField] private Sprite resourceDropSprite;
        [SerializeField] private Vector3Int[] protectedCells = Array.Empty<Vector3Int>();
        [SerializeField, Min(0.1f)] private float defaultMiningDuration = 1f;
        [SerializeField, Min(0.1f)] private float resourceDropSize = 0.35f;

        private IMiningRewardReceiver rewardReceiver;
        private IMiningTransaction miningTransaction;
        private IUpgradeEffectProvider upgradeEffects;
        private IDeepZoneAccessProvider deepZoneAccess;
        private Func<Vector3Int, bool> cellProtectionPredicate;
        private int deepZoneTopY;
        private int deepZoneMinDepth;
        private int deepZoneMaxDepth;
        private bool hasDeepZoneBoundary;
        private Vector3Int activeCell;
        private TileBase activeTileAsset;
        private MiningTileDto activeTile;
        private float elapsed;

        public bool IsMining { get; private set; }
        public bool HasMiningPower { get; private set; } = true;
        public float Progress { get; private set; }
        public float EffectiveDuration { get; private set; }
        public int RequiredEnergy { get; private set; }
        public int SpawnedResourceDropCount { get; private set; }
        public MiningFailureReason LastFailure { get; private set; }
        public event Action<Vector3Int, MiningTileDto> TileMined;
        public event Action<Vector3Int> DeepZoneSignalAccessed;
        public event Action<MiningProgressState> ProgressChanged;

        private void Awake() => ResolveServices();

        public void SetRuntimeServices(
            IMiningTransaction transaction,
            IUpgradeEffectProvider effectProvider,
            IDeepZoneAccessProvider deepZoneAccessProvider = null)
        {
            miningTransaction = transaction;
            upgradeEffects = effectProvider;
            deepZoneAccess = deepZoneAccessProvider;
        }

        public void SetMiningPowerAvailable(bool available)
        {
            HasMiningPower = available;
            if (!available && IsMining)
            {
                Fail(MiningFailureReason.InsufficientEnergy);
            }
        }

        /// <summary>시설 배치처럼 런타임에 변하는 채굴 금지 셀 판정을 연결한다.</summary>
        public void SetCellProtectionPredicate(Func<Vector3Int, bool> predicate)
        {
            cellProtectionPredicate = predicate;
        }

        public void ConfigureDeepZoneBoundary(int topY, int minDepth, int maxDepth)
        {
            deepZoneTopY = topY;
            deepZoneMinDepth = Mathf.Max(1, minDepth);
            deepZoneMaxDepth = Mathf.Max(deepZoneMinDepth, maxDepth);
            hasDeepZoneBoundary = true;
        }

        public bool TryStartMining(Vector3Int cell)
        {
            if (IsMining && activeCell == cell)
            {
                return true;
            }

            if (IsMining)
            {
                CancelMining();
            }

            ResolveServices();
            if (!HasMiningPower || foregroundTilemap == null || tileResolver == null)
            {
                return Fail(HasMiningPower
                    ? MiningFailureReason.DependencyMissing
                    : MiningFailureReason.InsufficientEnergy);
            }

            if (IsCellProtected(cell))
            {
                return Fail(MiningFailureReason.NotMineable);
            }

            TileBase tile = foregroundTilemap.GetTile(cell);
            if (tile == null || !tileResolver.TryResolve(tile, out MiningTileDto definition))
            {
                return Fail(MiningFailureReason.InvalidTarget);
            }

            if (definition.tileId == LockedSignalTileId)
            {
                return TryAccessDeepZoneSignal(cell);
            }

            if (IsDeepZoneCell(cell) && deepZoneAccess?.IsDeepZoneUnlocked != true)
            {
                return Fail(MiningFailureReason.DeepZoneLocked);
            }

            if (!definition.isMineable)
            {
                return Fail(MiningFailureReason.NotMineable);
            }

            var drillLevel = upgradeEffects?.GetDrillLevel() ?? 0;
            if (drillLevel < Mathf.Max(0, definition.requiredDrillLevel))
            {
                return Fail(MiningFailureReason.DrillLevelTooLow);
            }

            RequiredEnergy = CalculateEnergyCost(definition.energyCost);
            if (RequiredEnergy > 0
                && (miningTransaction == null
                    || !miningTransaction.CanAffordEnergy(RequiredEnergy)))
            {
                return Fail(miningTransaction == null
                    ? MiningFailureReason.DependencyMissing
                    : MiningFailureReason.InsufficientEnergy);
            }

            activeCell = cell;
            activeTileAsset = tile;
            activeTile = definition;
            elapsed = 0f;
            Progress = 0f;
            LastFailure = MiningFailureReason.None;
            EffectiveDuration = CalculateDuration(definition.miningTime);
            IsMining = true;
            Publish(MiningPhase.Mining);
            return true;
        }

        public bool TryStartMiningFrom(Vector2 origin, float facingDirection, float range)
        {
            return TryGetDirectionalCell(origin, facingDirection, range, out var cell)
                && TryStartMining(cell);
        }

        public bool TryStartMiningAtWorldPoint(Vector2 worldPoint, Vector2 origin, float range)
        {
            return TryGetWorldPointCell(worldPoint, origin, range, out var cell)
                && TryStartMining(cell);
        }

        public bool TryMineInstant(Vector3Int cell)
        {
            if (!TryStartMining(cell))
            {
                return false;
            }

            return IsMining ? CompleteMining() : LastFailure == MiningFailureReason.None;
        }

        public bool TryMineInstantFrom(Vector2 origin, float facingDirection, float range)
        {
            return TryGetDirectionalCell(origin, facingDirection, range, out var cell)
                && TryMineInstant(cell);
        }

        public bool TryMineInstantAtWorldPoint(Vector2 worldPoint, Vector2 origin, float range)
        {
            return TryGetWorldPointCell(worldPoint, origin, range, out var cell)
                && TryMineInstant(cell);
        }

        public void TickMining(float deltaTime)
        {
            if (!ValidateActiveMining())
            {
                return;
            }

            elapsed += Mathf.Max(0f, deltaTime);
            Progress = Mathf.Clamp01(elapsed / EffectiveDuration);
            Publish(MiningPhase.Mining);
            if (Progress >= 1f)
            {
                CompleteMining();
            }
        }

        public void TickMining(float deltaTime, Vector2 origin, float range)
        {
            if (IsMining && !IsCellInRange(activeCell, origin, range))
            {
                Fail(MiningFailureReason.OutOfRange);
                return;
            }

            TickMining(deltaTime);
        }

        public void CancelMining()
        {
            if (!IsMining)
            {
                return;
            }

            IsMining = false;
            elapsed = 0f;
            Progress = 0f;
            LastFailure = MiningFailureReason.None;
            Publish(MiningPhase.Cancelled);
        }

        private bool ValidateActiveMining()
        {
            if (!IsMining)
            {
                return false;
            }

            if (!HasMiningPower
                || (RequiredEnergy > 0
                    && (miningTransaction == null
                        || !miningTransaction.CanAffordEnergy(RequiredEnergy))))
            {
                Fail(miningTransaction == null && HasMiningPower
                    ? MiningFailureReason.DependencyMissing
                    : MiningFailureReason.InsufficientEnergy);
                return false;
            }

            if (IsCellProtected(activeCell))
            {
                Fail(MiningFailureReason.NotMineable);
                return false;
            }

            if (foregroundTilemap.GetTile(activeCell) != activeTileAsset)
            {
                Fail(MiningFailureReason.TargetChanged);
                return false;
            }

            return true;
        }

        private bool IsCellProtected(Vector3Int cell)
        {
            return Array.IndexOf(protectedCells, cell) >= 0
                || (cellProtectionPredicate != null && cellProtectionPredicate(cell));
        }

        private bool IsDeepZoneCell(Vector3Int cell)
        {
            if (!hasDeepZoneBoundary)
            {
                return false;
            }

            int depth = deepZoneTopY - cell.y + 1;
            return depth >= deepZoneMinDepth && depth <= deepZoneMaxDepth;
        }

        private bool CompleteMining()
        {
            if (!ValidateActiveMining())
            {
                return false;
            }

            if (miningTransaction != null)
            {
                var commit = miningTransaction.TryCommitMining(
                    activeTile.mineralId,
                    activeTile.quantity,
                    RequiredEnergy);
                if (!commit.Succeeded)
                {
                    Fail(ToFailureReason(commit.Status));
                    return false;
                }
            }
            else if (RequiredEnergy > 0)
            {
                return Fail(MiningFailureReason.DependencyMissing);
            }

            TileBase tile = foregroundTilemap.GetTile(activeCell);
            if (tile != activeTileAsset)
            {
                return Fail(MiningFailureReason.TargetChanged);
            }

            Sprite minedSprite = foregroundTilemap.GetSprite(activeCell);
            Color minedColor = tile is Tile coloredTile ? coloredTile.color : Color.white;
            foregroundTilemap.SetTile(activeCell, null);

            if (miningTransaction == null
                && rewardReceiver != null
                && !string.IsNullOrEmpty(activeTile.mineralId)
                && activeTile.quantity > 0)
            {
                rewardReceiver.AddMineral(activeTile.mineralId, activeTile.quantity);
            }
            else if (miningTransaction == null
                && rewardReceiver == null
                && !string.IsNullOrEmpty(activeTile.mineralId))
            {
                // Inventory 소유자가 없을 때만 월드 드롭을 만든다. 두 보상 경로를 동시에 만들지 않는다.
                SpawnResourceDrop(activeCell, activeTile.mineralId, minedSprite, minedColor);
            }

            IsMining = false;
            Progress = 1f;
            LastFailure = MiningFailureReason.None;
            TileMined?.Invoke(activeCell, activeTile);
            Publish(MiningPhase.Completed);
            return true;
        }

        private bool TryGetDirectionalCell(
            Vector2 origin,
            float facingDirection,
            float range,
            out Vector3Int cell)
        {
            cell = default;
            if (foregroundTilemap == null || Mathf.Approximately(facingDirection, 0f))
            {
                return false;
            }

            int horizontalDirection = facingDirection > 0f ? 1 : -1;
            Vector3Int sideCell = foregroundTilemap.WorldToCell(origin);
            float sideCellCenterX = foregroundTilemap.GetCellCenterWorld(sideCell).x;
            if ((horizontalDirection > 0 && sideCellCenterX <= origin.x)
                || (horizontalDirection < 0 && sideCellCenterX >= origin.x))
            {
                sideCell.x += horizontalDirection;
            }

            // 엔터 채굴은 가까운 방향의 바로 옆을 먼저 보고, 빈칸이면 위와 아래를 차례로 확인한다.
            if (IsDirectionalCandidate(sideCell, origin, range))
            {
                cell = sideCell;
                return true;
            }

            Vector3Int upperCell = sideCell + Vector3Int.up;
            if (IsDirectionalCandidate(upperCell, origin, range))
            {
                cell = upperCell;
                return true;
            }

            Vector3Int lowerCell = sideCell + Vector3Int.down;
            if (IsDirectionalCandidate(lowerCell, origin, range))
            {
                cell = lowerCell;
                return true;
            }

            return false;
        }

        private bool IsDirectionalCandidate(Vector3Int cell, Vector2 origin, float range)
        {
            return foregroundTilemap.HasTile(cell) && IsCellInRange(cell, origin, range);
        }

        private bool TryGetWorldPointCell(
            Vector2 worldPoint,
            Vector2 origin,
            float range,
            out Vector3Int cell)
        {
            cell = default;
            if (foregroundTilemap == null)
            {
                return false;
            }

            cell = foregroundTilemap.WorldToCell(worldPoint);
            return IsCellInRange(cell, origin, range);
        }

        private bool IsCellInRange(Vector3Int cell, Vector2 origin, float range)
        {
            if (foregroundTilemap == null)
            {
                return false;
            }

            Vector3 center = foregroundTilemap.GetCellCenterWorld(cell);
            float allowed = Mathf.Max(0f, range) + 0.5f;
            return Mathf.Abs(center.x - origin.x) <= allowed
                && Mathf.Abs(center.y - origin.y) <= allowed;
        }

        private float CalculateDuration(float baseDuration)
        {
            var duration = baseDuration > 0f ? baseDuration : defaultMiningDuration;
            var speed = upgradeEffects?.GetDrillSpeedMultiplier() ?? 1f;
            if (speed <= 0f || float.IsNaN(speed) || float.IsInfinity(speed))
            {
                speed = 1f;
            }

            return Mathf.Max(0.01f, duration / speed);
        }

        private int CalculateEnergyCost(int baseCost)
        {
            if (baseCost <= 0)
            {
                return 0;
            }

            var efficiency = upgradeEffects?.GetEnergyEfficiencyMultiplier() ?? 1f;
            if (efficiency <= 0f || float.IsNaN(efficiency) || float.IsInfinity(efficiency))
            {
                efficiency = 1f;
            }

            return Mathf.Max(1, Mathf.CeilToInt(baseCost / efficiency));
        }

        private bool Fail(MiningFailureReason reason)
        {
            IsMining = false;
            elapsed = 0f;
            LastFailure = reason;
            Publish(MiningPhase.Failed);
            return false;
        }

        private bool TryAccessDeepZoneSignal(Vector3Int cell)
        {
            if (deepZoneAccess?.IsDeepZoneUnlocked != true)
            {
                return Fail(MiningFailureReason.DeepZoneLocked);
            }

            LastFailure = MiningFailureReason.None;
            DeepZoneSignalAccessed?.Invoke(cell);
            return true;
        }

        private void Publish(MiningPhase phase)
        {
            ProgressChanged?.Invoke(new MiningProgressState(
                phase,
                LastFailure,
                Progress,
                EffectiveDuration,
                RequiredEnergy));
        }

        private static MiningFailureReason ToFailureReason(MiningCommitStatus status)
        {
            return status switch
            {
                MiningCommitStatus.InsufficientEnergy => MiningFailureReason.InsufficientEnergy,
                MiningCommitStatus.InventoryFull => MiningFailureReason.InventoryFull,
                MiningCommitStatus.InvalidReward => MiningFailureReason.InvalidReward,
                MiningCommitStatus.DependencyMissing => MiningFailureReason.DependencyMissing,
                _ => MiningFailureReason.None
            };
        }

        private void SpawnResourceDrop(
            Vector3Int cell,
            string mineralId,
            Sprite minedSprite,
            Color minedColor)
        {
            Transform dropRoot = transform.Find("MinedResourceDrops");
            if (dropRoot == null)
            {
                dropRoot = new GameObject("MinedResourceDrops").transform;
                dropRoot.SetParent(transform, false);
            }

            var drop = new GameObject("MinedResource_" + mineralId.Replace('.', '_'));
            drop.transform.SetParent(dropRoot, false);
            drop.transform.position = foregroundTilemap.GetCellCenterWorld(cell) + Vector3.up * 0.2f;
            var renderer = drop.AddComponent<SpriteRenderer>();
            renderer.sprite = resourceDropSprite != null ? resourceDropSprite : minedSprite;
            renderer.color = minedColor;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = Vector2.one * resourceDropSize;
            renderer.sortingOrder = 12;
            SpawnedResourceDropCount++;
        }

        private void ResolveServices()
        {
            rewardReceiver = rewardReceiverBehaviour as IMiningRewardReceiver;
            miningTransaction ??= miningTransactionBehaviour as IMiningTransaction;
            upgradeEffects ??= upgradeEffectProviderBehaviour as IUpgradeEffectProvider;
        }
    }
}
