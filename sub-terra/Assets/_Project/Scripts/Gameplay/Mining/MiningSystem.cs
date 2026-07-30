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
        [SerializeField] private Sprite resourceDropSprite;
        [SerializeField, Min(0.1f)] private float defaultMiningDuration = 1f;
        [SerializeField, Min(0.1f)] private float resourceDropSize = 0.35f;

        private IMiningRewardReceiver rewardReceiver;
        private Vector3Int activeCell;
        private MiningTileDto activeTile;
        private float elapsed;

        public bool IsMining { get; private set; }
        public bool HasMiningPower { get; private set; } = true;
        public float Progress { get; private set; }
        public int SpawnedResourceDropCount { get; private set; }
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

            Vector3 target = origin
                + Vector2.right * Mathf.Sign(facingDirection) * Mathf.Max(0f, range)
                + Vector2.down * 0.5f;
            return TryStartMining(foregroundTilemap.WorldToCell(target));
        }

        public bool TryMineInstant(Vector3Int cell)
        {
            if (!TryStartMining(cell))
            {
                return false;
            }

            CompleteMining();
            return true;
        }

        public bool TryMineInstantFrom(Vector2 origin, float facingDirection, float range)
        {
            if (foregroundTilemap == null || Mathf.Approximately(facingDirection, 0f))
            {
                return false;
            }

            Vector3 target = origin
                + Vector2.right * Mathf.Sign(facingDirection) * Mathf.Max(0f, range)
                + Vector2.down * 0.5f;
            return TryMineInstant(foregroundTilemap.WorldToCell(target));
        }

        public bool TryMineInstantAtWorldPoint(Vector2 worldPoint, Vector2 origin, float range)
        {
            if (foregroundTilemap == null)
            {
                return false;
            }

            Vector3Int cell = foregroundTilemap.WorldToCell(worldPoint);
            Vector3 center = foregroundTilemap.GetCellCenterWorld(cell);
            float allowed = Mathf.Max(0f, range) + 0.5f;
            if (Mathf.Abs(center.x - origin.x) > allowed
                || Mathf.Abs(center.y - origin.y) > allowed)
            {
                return false;
            }

            return TryMineInstant(cell);
        }

        public void TickMining(float deltaTime)
        {
            if (!IsMining) return;
            if (!HasMiningPower) { CancelMining(); return; }

            float duration = activeTile.miningTime > 0f ? activeTile.miningTime : defaultMiningDuration;
            elapsed += Mathf.Max(0f, deltaTime);
            Progress = Mathf.Clamp01(elapsed / duration);
            if (Progress < 1f) return;

            CompleteMining();
        }

        public void CancelMining()
        {
            IsMining = false;
            elapsed = 0f;
            Progress = 0f;
        }

        private void CompleteMining()
        {
            IsMining = false;
            Progress = 1f;
            TileBase tile = foregroundTilemap.GetTile(activeCell);
            if (tile == null)
            {
                return;
            }

            Sprite minedSprite = foregroundTilemap.GetSprite(activeCell);
            Color minedColor = tile is Tile coloredTile ? coloredTile.color : Color.white;
            foregroundTilemap.SetTile(activeCell, null);
            if (rewardReceiver != null
                && !string.IsNullOrEmpty(activeTile.mineralId)
                && activeTile.quantity > 0)
            {
                rewardReceiver.AddMineral(activeTile.mineralId, activeTile.quantity);
            }

            if (!string.IsNullOrEmpty(activeTile.mineralId))
            {
                SpawnResourceDrop(activeCell, activeTile.mineralId, minedSprite, minedColor);
            }

            TileMined?.Invoke(activeCell, activeTile);
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

            var drop = new GameObject(
                "MinedResource_" + mineralId.Replace('.', '_'));
            drop.transform.SetParent(dropRoot, false);
            drop.transform.position =
                foregroundTilemap.GetCellCenterWorld(cell) + Vector3.up * 0.2f;
            var renderer = drop.AddComponent<SpriteRenderer>();
            renderer.sprite = resourceDropSprite != null
                ? resourceDropSprite
                : minedSprite;
            renderer.color = minedColor;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = Vector2.one * resourceDropSize;
            renderer.sortingOrder = 12;
            SpawnedResourceDropCount++;
        }

        private void ResolveRewardReceiver() => rewardReceiver = rewardReceiverBehaviour as IMiningRewardReceiver;
    }
}
