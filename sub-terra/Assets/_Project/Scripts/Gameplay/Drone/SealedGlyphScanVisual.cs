using SubTerra.Gameplay.Mining;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Drone
{
    /// <summary>
    /// 드론 스캔 펄스가 신호 칸을 비출 때만 문양 에셋을 올리고, 끝나면 회색 석재로 되돌린다.
    /// 타일 ID는 유지하고 오버레이만 바꾼다.
    /// </summary>
    public sealed class SealedGlyphScanVisual : MonoBehaviour
    {
        [SerializeField] private Tilemap foregroundTilemap;
        [SerializeField] private MiningTileResolver tileResolver;
        [SerializeField] private DroneSensor droneSensor;
        [SerializeField] private Sprite glyphSprite;
        [SerializeField] private int sortingOrder = 2;

        private SpriteRenderer overlay;
        private bool overlayVisible;

        public void Configure(
            Tilemap tilemap,
            MiningTileResolver resolver,
            DroneSensor sensor,
            Sprite awakenedGlyph)
        {
            foregroundTilemap = tilemap;
            tileResolver = resolver;
            droneSensor = sensor;
            glyphSprite = awakenedGlyph;
        }

        private void LateUpdate()
        {
            if (!TryGetActiveGlyphCell(out Vector3Int cell, out Vector3 worldPosition))
            {
                HideOverlay();
                return;
            }

            ShowOverlay(worldPosition);
        }

        private bool TryGetActiveGlyphCell(out Vector3Int cell, out Vector3 worldPosition)
        {
            cell = default;
            worldPosition = default;
            if (droneSensor == null || foregroundTilemap == null || glyphSprite == null)
            {
                return false;
            }

            DroneScanPulseView pulseView = droneSensor.ScanPulseView;
            if (pulseView == null)
            {
                return false;
            }

            var targets = droneSensor.LastPulseTargets;
            if (targets == null || targets.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < targets.Count; index++)
            {
                DroneScanTarget target = targets[index];
                if (target.Kind != DroneScanTargetKind.SealedGlyph)
                {
                    continue;
                }

                if (!pulseView.TryGetActiveTarget(target.Cell, out DroneScanTargetKind kind)
                    || kind != DroneScanTargetKind.SealedGlyph)
                {
                    continue;
                }

                if (!IsLockedSignalCell(target.Cell))
                {
                    continue;
                }

                cell = target.Cell;
                worldPosition = target.WorldPosition;
                return true;
            }

            return false;
        }

        private bool IsLockedSignalCell(Vector3Int cell)
        {
            if (foregroundTilemap == null || tileResolver == null)
            {
                return false;
            }

            TileBase tile = foregroundTilemap.GetTile(cell);
            return tile != null
                && tileResolver.TryResolve(tile, out MiningTileDto definition)
                && definition.tileId == "tile.locked.signal";
        }

        private void ShowOverlay(Vector3 worldPosition)
        {
            EnsureOverlay();
            if (overlay == null)
            {
                return;
            }

            overlay.transform.position = worldPosition;
            overlay.sprite = glyphSprite;
            overlay.enabled = true;
            overlay.gameObject.SetActive(true);
            overlayVisible = true;
        }

        private void HideOverlay()
        {
            if (!overlayVisible || overlay == null)
            {
                return;
            }

            overlay.enabled = false;
            overlay.gameObject.SetActive(false);
            overlayVisible = false;
        }

        private void EnsureOverlay()
        {
            if (overlay != null)
            {
                return;
            }

            var root = new GameObject("SealedGlyphOverlay");
            root.layer = gameObject.layer;
            root.transform.SetParent(transform, true);
            overlay = root.AddComponent<SpriteRenderer>();
            overlay.sprite = glyphSprite;
            overlay.sortingOrder = sortingOrder;
            overlay.drawMode = SpriteDrawMode.Simple;
            overlay.enabled = false;
            root.SetActive(false);
        }

        private void OnDisable()
        {
            HideOverlay();
        }
    }
}
