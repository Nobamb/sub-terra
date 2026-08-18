using System.Collections.Generic;
using SubTerra.Gameplay.Structural;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Building
{
    /// <summary>
    /// Visual-only grid preview. It never creates a building or spends resources.
    /// 1x1은 단일 스프라이트, 2x2 등은 칸마다 점(dot) 마커를 표시한다.
    /// </summary>
    public sealed class BuildingPlacementPreview : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color validColor = new(0.2f, 0.9f, 0.35f, 0.55f);
        [SerializeField] private Color invalidColor = new(0.95f, 0.2f, 0.2f, 0.55f);
        [SerializeField, Min(0.05f)] private float multiCellDotScale = 0.28f;

        private readonly List<SpriteRenderer> multiCellMarkers = new();
        private readonly List<SpriteRenderer> supportRangeMarkers = new();
        private Sprite cachedDotSprite;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Configure(Sprite sprite)
        {
            if (spriteRenderer != null) spriteRenderer.sprite = sprite;
        }

        public void SetCell(Tilemap tilemap, Vector3Int cell, bool isValid)
        {
            HideMultiCellMarkers();
            transform.position = tilemap != null ? tilemap.GetCellCenterWorld(cell) : cell;
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = isValid ? validColor : invalidColor;
            }

            gameObject.SetActive(true);
        }

        /// <summary>버팀목 선택 중 지지 반경과 설치 즉시 사라질 균열을 함께 표시한다.</summary>
        public void SetSupportRange(
            Tilemap tilemap,
            Vector3Int origin,
            float radius,
            StructuralIntegritySystem structuralSystem)
        {
            HideSupportRange();
            if (tilemap == null || radius <= 0f) return;

            int reach = Mathf.CeilToInt(radius);
            var cells = new List<Vector3Int>();
            for (int x = origin.x - reach; x <= origin.x + reach; x++)
            for (int y = origin.y - reach; y <= origin.y + reach; y++)
            {
                var cell = new Vector3Int(x, y, origin.z);
                if (Vector2.Distance(
                        tilemap.GetCellCenterWorld(origin),
                        tilemap.GetCellCenterWorld(cell)) <= radius)
                {
                    cells.Add(cell);
                }
            }

            EnsureSupportRangeMarkers(cells.Count);
            Sprite dot = GetOrCreateDotSprite();
            for (int i = 0; i < supportRangeMarkers.Count; i++)
            {
                SpriteRenderer marker = supportRangeMarkers[i];
                if (i >= cells.Count)
                {
                    marker.gameObject.SetActive(false);
                    continue;
                }

                Vector3Int cell = cells[i];
                bool clearsCrack = structuralSystem != null && structuralSystem.HasRiskAtCell(cell);
                marker.transform.position = tilemap.GetCellCenterWorld(cell);
                marker.transform.localScale = Vector3.one * (clearsCrack ? 0.24f : 0.1f);
                marker.sprite = dot;
                marker.color = clearsCrack
                    ? new Color(0.2f, 1f, 0.95f, 0.9f)
                    : new Color(0.2f, 0.8f, 1f, 0.3f);
                marker.gameObject.SetActive(true);
            }
        }

        public void HideSupportRange()
        {
            for (int i = 0; i < supportRangeMarkers.Count; i++)
            {
                if (supportRangeMarkers[i] != null)
                    supportRangeMarkers[i].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// footprint 전체 칸에 점 마커를 표시한다(긴급 탈출 포탈 2x2 = 4점).
        /// 단일 칸이면 SetCell과 동일하게 본 스프라이트를 사용한다.
        /// </summary>
        public void SetCells(Tilemap tilemap, IReadOnlyList<Vector3Int> cells, bool isValid)
        {
            if (cells == null || cells.Count == 0)
            {
                Hide();
                return;
            }

            if (cells.Count == 1)
            {
                SetCell(tilemap, cells[0], isValid);
                return;
            }

            gameObject.SetActive(true);
            if (spriteRenderer != null)
            {
                // 다중 칸은 점 마커만 쓰고 본 스프라이트는 끈다.
                spriteRenderer.enabled = false;
            }

            EnsureMultiCellMarkers(cells.Count);
            Color color = isValid ? validColor : invalidColor;
            Sprite dot = GetOrCreateDotSprite();
            Vector3 anchor = tilemap != null
                ? tilemap.GetCellCenterWorld(cells[0])
                : (Vector3)cells[0];
            transform.position = anchor;

            for (int i = 0; i < multiCellMarkers.Count; i++)
            {
                SpriteRenderer marker = multiCellMarkers[i];
                if (i >= cells.Count)
                {
                    marker.gameObject.SetActive(false);
                    continue;
                }

                Vector3 world = tilemap != null
                    ? tilemap.GetCellCenterWorld(cells[i])
                    : (Vector3)cells[i];
                marker.transform.position = world;
                marker.sprite = dot;
                marker.color = color;
                marker.transform.localScale = Vector3.one * multiCellDotScale;
                marker.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            HideMultiCellMarkers();
            HideSupportRange();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            gameObject.SetActive(false);
        }

        private void HideMultiCellMarkers()
        {
            for (int i = 0; i < multiCellMarkers.Count; i++)
            {
                if (multiCellMarkers[i] != null)
                {
                    multiCellMarkers[i].gameObject.SetActive(false);
                }
            }
        }

        private void EnsureMultiCellMarkers(int count)
        {
            while (multiCellMarkers.Count < count)
            {
                var markerObject = new GameObject($"PlacementDot_{multiCellMarkers.Count}");
                markerObject.transform.SetParent(transform, false);
                var renderer = markerObject.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 50;
                multiCellMarkers.Add(renderer);
            }
        }

        private void EnsureSupportRangeMarkers(int count)
        {
            while (supportRangeMarkers.Count < count)
            {
                var markerObject = new GameObject($"SupportRange_{supportRangeMarkers.Count}");
                markerObject.transform.SetParent(transform, false);
                var renderer = markerObject.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 49;
                supportRangeMarkers.Add(renderer);
            }
        }

        private Sprite GetOrCreateDotSprite()
        {
            if (cachedDotSprite != null)
            {
                return cachedDotSprite;
            }

            // 간단 원형 점: 8x8 텍스처로 Preview 전용 마커를 만든다.
            const int size = 8;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "BuildingPlacementDot"
            };
            float center = (size - 1) * 0.5f;
            float radius = center - 0.4f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float alpha = dx * dx + dy * dy <= radius * radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, false);
            cachedDotSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            cachedDotSprite.name = "BuildingPlacementDotSprite";
            return cachedDotSprite;
        }
    }
}
