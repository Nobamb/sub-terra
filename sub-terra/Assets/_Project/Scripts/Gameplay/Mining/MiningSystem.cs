using System;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Mining
{
    public sealed class MiningSystem : MonoBehaviour
    {
        [SerializeField] private Tilemap foregroundTilemap;
        [SerializeField] private MiningTileResolver tileResolver;
        [SerializeField] private MonoBehaviour rewardReceiverBehaviour;
        [SerializeField, Min(0.1f)] private float defaultMiningDuration = 1f;

        private IMiningRewardReceiver rewardReceiver;
        private Vector3Int activeCell;
        private MiningTileDto activeTile;
        private float elapsed;

        public bool IsMining { get; private set; }
        public bool HasMiningPower { get; private set; } = true;
        public float Progress { get; private set; }
        public event Action<Vector3Int, MiningTileDto> TileMined;

        private void Awake() => ResolveRewardReceiver();

        public void SetMiningPowerAvailable(bool available)
        {
            HasMiningPower = available;
            if (!available) CancelMining();
        }

        public bool TryStartMining(Vector3Int cell)
        {
            if (IsMining && activeCell == cell) return true;
            CancelMining();
            ResolveRewardReceiver();
            if (!HasMiningPower || foregroundTilemap == null || tileResolver == null) return false;

            TileBase tile = foregroundTilemap.GetTile(cell);
            if (tile == null || !tileResolver.TryResolve(tile, out MiningTileDto definition) || !definition.isMineable)
                return false;

            activeCell = cell;
            activeTile = definition;
            elapsed = 0f;
            Progress = 0f;
            IsMining = true;
            return true;
        }

        public bool TryStartMiningFrom(Vector2 origin, float facingDirection, float range)
        {
            if (foregroundTilemap == null || Mathf.Approximately(facingDirection, 0f))
            {
                return false;
            }

            Vector3 target = origin + Vector2.right * Mathf.Sign(facingDirection) * Mathf.Max(0f, range);
            return TryStartMining(foregroundTilemap.WorldToCell(target));
        }

        public void TickMining(float deltaTime)
        {
            if (!IsMining) return;
            if (!HasMiningPower) { CancelMining(); return; }

            float duration = activeTile.miningTime > 0f ? activeTile.miningTime : defaultMiningDuration;
            elapsed += Mathf.Max(0f, deltaTime);
            Progress = Mathf.Clamp01(elapsed / duration);
            if (Progress < 1f) return;

            IsMining = false;
            Progress = 1f;
            if (foregroundTilemap.GetTile(activeCell) == null) return;
            foregroundTilemap.SetTile(activeCell, null);
            if (rewardReceiver != null && !string.IsNullOrEmpty(activeTile.mineralId) && activeTile.quantity > 0)
                rewardReceiver.AddMineral(activeTile.mineralId, activeTile.quantity);
            TileMined?.Invoke(activeCell, activeTile);
        }

        public void CancelMining()
        {
            IsMining = false;
            elapsed = 0f;
            Progress = 0f;
        }

        private void ResolveRewardReceiver() => rewardReceiver = rewardReceiverBehaviour as IMiningRewardReceiver;
    }
}
