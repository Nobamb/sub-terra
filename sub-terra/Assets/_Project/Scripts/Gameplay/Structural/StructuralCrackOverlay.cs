using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>
    /// 원본 지형과 분리된 Tilemap에 국소 균열만 표시한다.
    /// 천장 타일 단위로 색을 칠하며, 다른 구역 셀을 기하 반경으로 지우지 않는다.
    /// </summary>
    public sealed class StructuralCrackOverlay : MonoBehaviour
    {
        [SerializeField] private Tilemap overlayTilemap;
        [SerializeField] private TileBase crackTile;
        [SerializeField] private Color cautionColor = new(1f, 0.82f, 0.2f, 0.55f);
        [SerializeField] private Color dangerColor = new(1f, 0.38f, 0.08f, 0.72f);
        [SerializeField] private Color imminentColor = new(0.95f, 0.08f, 0.06f, 0.9f);

        private readonly Dictionary<Vector3Int, StructuralRiskLevel> cellRisks = new();
        private readonly HashSet<Vector3Int> visibleCells = new();

        private Tile runtimeCrackTile;
        private Sprite runtimeCrackSprite;
        private Texture2D runtimeCrackTexture;

        public Tilemap OverlayTilemap => overlayTilemap;

        /// <summary>단일 천장 셀의 균열 표시를 해당 셀 위험 단계로 설정한다.</summary>
        public void SetCell(Vector3Int cell, StructuralRiskLevel risk)
        {
            if (overlayTilemap == null) return;

            if (risk == StructuralRiskLevel.Stable)
            {
                ClearCell(cell);
                return;
            }

            TileBase tile = ResolveCrackTile();
            overlayTilemap.SetTile(cell, tile);
            overlayTilemap.SetTileFlags(cell, TileFlags.None);
            overlayTilemap.SetColor(cell, GetColor(risk));
            visibleCells.Add(cell);
            cellRisks[cell] = risk;
        }

        /// <summary>지정 셀의 균열만 제거한다. 다른 구역에는 영향 없다.</summary>
        public void ClearCell(Vector3Int cell)
        {
            if (overlayTilemap == null) return;
            if (!visibleCells.Remove(cell) && !cellRisks.ContainsKey(cell)) return;

            overlayTilemap.SetTile(cell, null);
            cellRisks.Remove(cell);
        }

        /// <summary>
        /// 세이브 복원·전체 재계산 전 표시를 비운다.
        /// 원본 지형 Tilemap은 건드리지 않는다.
        /// </summary>
        public void ClearAll()
        {
            if (overlayTilemap == null)
            {
                visibleCells.Clear();
                cellRisks.Clear();
                return;
            }

            if (visibleCells.Count == 0 && cellRisks.Count == 0)
            {
                return;
            }

            var cells = new List<Vector3Int>(visibleCells);
            foreach (Vector3Int cell in cellRisks.Keys)
            {
                if (!visibleCells.Contains(cell))
                {
                    cells.Add(cell);
                }
            }

            for (int i = 0; i < cells.Count; i++)
            {
                ClearCell(cells[i]);
            }

            visibleCells.Clear();
            cellRisks.Clear();
        }

        /// <summary>
        /// 하위 호환: 구역 단위 갱신. 소유 셀을 지운 뒤 후보를 타일 단위로 다시 칠한다.
        /// </summary>
        public void UpdateRegion(
            Vector3Int center,
            int radius,
            IReadOnlyList<Vector3Int> candidates,
            StructuralRiskLevel risk)
        {
            if (overlayTilemap == null) return;

            ClearRegion(center, radius);
            if (candidates == null || candidates.Count == 0 || risk == StructuralRiskLevel.Stable)
            {
                return;
            }

            int visibleCount = GetVisibleCount(candidates.Count, risk);
            for (int index = 0; index < visibleCount; index++)
            {
                SetCell(candidates[index], risk);
            }
        }

        /// <summary>center 주변 반경의 표시 셀만 제거(하위 호환).</summary>
        public void ClearRegion(Vector3Int center, int radius)
        {
            if (overlayTilemap == null || visibleCells.Count == 0) return;

            var removed = new List<Vector3Int>();
            foreach (Vector3Int cell in visibleCells)
            {
                if (Mathf.Abs(cell.x - center.x) > radius
                    || Mathf.Abs(cell.y - center.y) > radius)
                {
                    continue;
                }

                removed.Add(cell);
            }

            for (int i = 0; i < removed.Count; i++)
            {
                ClearCell(removed[i]);
            }
        }

        public static int GetVisibleCount(int candidateCount, StructuralRiskLevel risk)
        {
            if (candidateCount <= 0 || risk == StructuralRiskLevel.Stable) return 0;
            float ratio = risk == StructuralRiskLevel.Caution
                ? 0.34f
                : risk == StructuralRiskLevel.Danger ? 0.67f : 1f;
            return Mathf.Clamp(Mathf.CeilToInt(candidateCount * ratio), 1, candidateCount);
        }

        public bool HasVisibleCell(Vector3Int cell)
        {
            return visibleCells.Contains(cell)
                && overlayTilemap != null
                && overlayTilemap.HasTile(cell);
        }

        public bool TryGetCellRisk(Vector3Int cell, out StructuralRiskLevel risk)
        {
            return cellRisks.TryGetValue(cell, out risk);
        }

        private Color GetColor(StructuralRiskLevel risk)
        {
            return risk == StructuralRiskLevel.Caution
                ? cautionColor
                : risk == StructuralRiskLevel.Danger ? dangerColor : imminentColor;
        }

        private TileBase ResolveCrackTile()
        {
            if (crackTile != null) return crackTile;
            if (runtimeCrackTile != null) return runtimeCrackTile;

            const int size = 16;
            runtimeCrackTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeStructuralCrack",
                filterMode = FilterMode.Point
            };
            var pixels = new Color32[size * size];
            runtimeCrackTexture.SetPixels32(pixels);
            DrawLine(runtimeCrackTexture, 8, 15, 7, 10);
            DrawLine(runtimeCrackTexture, 7, 10, 10, 7);
            DrawLine(runtimeCrackTexture, 7, 10, 4, 7);
            DrawLine(runtimeCrackTexture, 10, 7, 9, 3);
            DrawLine(runtimeCrackTexture, 4, 7, 6, 1);
            runtimeCrackTexture.Apply();
            runtimeCrackSprite = Sprite.Create(
                runtimeCrackTexture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            runtimeCrackTile = ScriptableObject.CreateInstance<Tile>();
            runtimeCrackTile.sprite = runtimeCrackSprite;
            runtimeCrackTile.colliderType = Tile.ColliderType.None;
            return runtimeCrackTile;
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = -Mathf.Abs(y1 - y0);
            int stepX = x0 < x1 ? 1 : -1;
            int stepY = y0 < y1 ? 1 : -1;
            int error = dx + dy;
            while (true)
            {
                texture.SetPixel(x0, y0, Color.white);
                if (x0 == x1 && y0 == y1) break;
                int doubled = 2 * error;
                if (doubled >= dy) { error += dy; x0 += stepX; }
                if (doubled <= dx) { error += dx; y0 += stepY; }
            }
        }

        private void OnDestroy()
        {
            DestroyRuntimeObject(runtimeCrackTile);
            DestroyRuntimeObject(runtimeCrackSprite);
            DestroyRuntimeObject(runtimeCrackTexture);
        }

        private static void DestroyRuntimeObject(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
