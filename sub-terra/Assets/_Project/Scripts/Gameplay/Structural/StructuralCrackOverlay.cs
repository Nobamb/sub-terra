using System.Collections.Generic;
using SubTerra.Shared;
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
        // Legacy fallback for scenes that have not yet received the three authored crack tiles.
        [SerializeField] private TileBase crackTile;
        [SerializeField] private TileBase cautionCrackTile;
        [SerializeField] private TileBase dangerCrackTile;
        [SerializeField] private TileBase imminentCrackTile;
        [SerializeField] private Color cautionColor = new(1f, 0.82f, 0.2f, 0.55f);
        [SerializeField] private Color dangerColor = new(1f, 0.38f, 0.08f, 0.72f);
        [SerializeField] private Color imminentColor = new(0.95f, 0.08f, 0.06f, 0.9f);
        [SerializeField, Min(0.05f)] private float pulseSeconds = 0.28f;
        [SerializeField, Min(0.05f)] private float telegraphFlashSeconds = 0.12f;
        // 기존 노란 마름모/붉은 직사각형 원인 표식을 같은 조건의 흔들림으로 치환한다.
        [SerializeField, Min(0f)] private float unsupportedShakeDistance = 0.1f;
        [SerializeField, Min(0.005f)] private float unsupportedShakeInterval = 0.02f;
        [SerializeField, Min(0f)] private float impactShakeDistance = 0.3f;
        [SerializeField, Min(0.005f)] private float impactShakeInterval = 0.01f;

        private readonly Dictionary<Vector3Int, StructuralRiskLevel> cellRisks = new();
        private readonly Dictionary<Vector3Int, float> cellIntensities = new();
        private readonly Dictionary<Vector3Int, StructuralRiskCause> cellCauses = new();
        private readonly Dictionary<Vector3Int, float> pulseRemaining = new();
        private readonly HashSet<Vector3Int> telegraphingCells = new();
        private readonly Dictionary<Vector3Int, ShakeVisual> shakeVisuals = new();
        private readonly List<FallingVisual> fallingVisuals = new();
        private readonly List<DustVisual> dustVisuals = new();
        private readonly HashSet<Vector3Int> visibleCells = new();
        private ICollapseDamageReceiver collapseDamageReceiver;
        private Tilemap sourceTilemap;

        private Tile runtimeCrackTile;
        private Sprite runtimeCrackSprite;
        private Texture2D runtimeCrackTexture;
        private Sprite runtimeEffectSprite;
        private Texture2D runtimeEffectTexture;

        private sealed class ShakeVisual
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public Color OriginalCellColor;
            public TileFlags OriginalTileFlags;
            public Matrix4x4 OriginalOverlayTransform;
            public StructuralRiskCause Cause;
            public float Elapsed;
        }

        private sealed class FallingVisual
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public Vector3 Start;
            public Vector3 End;
            public float Elapsed;
            public float Duration;
            public bool ContactConsumed;
        }

        private sealed class DustVisual
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public float Elapsed;
            public float Duration;
        }

        public Tilemap OverlayTilemap => overlayTilemap;

        public void BindSourceTilemap(Tilemap source)
        {
            if (sourceTilemap == source) return;

            ClearShakeVisuals();
            sourceTilemap = source;
            if (sourceTilemap == null) return;

            var cells = new List<Vector3Int>(visibleCells);
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3Int cell = cells[i];
                if (cellCauses.TryGetValue(cell, out StructuralRiskCause cause))
                    UpdateShakeVisual(cell, cause);
            }
        }

        public void BindCollapseDamageReceiver(ICollapseDamageReceiver receiver)
        {
            collapseDamageReceiver = receiver;
        }

        /// <summary>단일 천장 셀의 균열 표시를 해당 셀 위험 단계로 설정한다.</summary>
        public void SetCell(Vector3Int cell, StructuralRiskLevel risk)
        {
            SetCell(cell, risk, risk == StructuralRiskLevel.Caution ? 0f : 1f, StructuralRiskCause.None);
        }

        /// <summary>같은 단계에서도 점수 비율에 따라 균열의 농도를 다르게 표시한다.</summary>
        public void SetCell(
            Vector3Int cell,
            StructuralRiskLevel risk,
            float intensity,
            StructuralRiskCause cause)
        {
            if (overlayTilemap == null) return;

            if (risk == StructuralRiskLevel.Stable)
            {
                ClearCell(cell);
                return;
            }

            TileBase tile = ResolveCrackTile(risk);
            overlayTilemap.SetTile(cell, tile);
            overlayTilemap.SetTileFlags(cell, TileFlags.None);
            cellRisks[cell] = risk;
            cellIntensities[cell] = Mathf.Clamp01(intensity);
            cellCauses[cell] = cause;
            overlayTilemap.SetColor(cell, ResolveAnimatedColor(cell));
            visibleCells.Add(cell);
            UpdateShakeVisual(cell, cause);
        }

        /// <summary>이번 채굴로 약해진 천장만 짧게 펄스한다.</summary>
        public void PulseCell(Vector3Int cell)
        {
            if (!cellRisks.ContainsKey(cell)) return;
            pulseRemaining[cell] = pulseSeconds;
        }

        public void SetTelegraphing(Vector3Int cell, bool telegraphing)
        {
            if (telegraphing) telegraphingCells.Add(cell);
            else telegraphingCells.Remove(cell);
        }

        /// <summary>삭제 직전 타일 스프라이트를 짧게 낙하시켜 증발처럼 보이지 않게 한다.</summary>
        public void PlayCollapse(Vector3Int cell, Tilemap source, float duration)
        {
            if (source == null) return;
            Sprite sprite = source.GetSprite(cell);
            if (sprite == null) sprite = ResolveCrackTile(StructuralRiskLevel.CollapseImminent) is Tile tile ? tile.sprite : null;
            if (sprite == null) return;

            var root = new GameObject("StructuralFallingRock");
            root.transform.SetParent(transform, true);
            Vector3 start = source.GetCellCenterWorld(cell);
            root.transform.position = start;
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.72f, 0.64f, 0.54f, 1f);
            renderer.sortingOrder = 90;
            fallingVisuals.Add(new FallingVisual
            {
                Root = root,
                Renderer = renderer,
                Start = start,
                End = ResolveFallEnd(cell, source, start),
                Duration = Mathf.Max(0.05f, duration)
            });
        }

        private static Vector3 ResolveFallEnd(Vector3Int sourceCell, Tilemap source, Vector3 fallback)
        {
            int minimumY = source.cellBounds.yMin;
            var probe = sourceCell + Vector3Int.down;
            while (probe.y >= minimumY)
            {
                if (source.HasTile(probe))
                    return source.GetCellCenterWorld(probe + Vector3Int.up);
                probe += Vector3Int.down;
            }

            return fallback + Vector3.down * 3f;
        }

        /// <summary>지정 셀의 균열만 제거한다. 다른 구역에는 영향 없다.</summary>
        public void ClearCell(Vector3Int cell)
        {
            if (overlayTilemap == null) return;
            if (!visibleCells.Remove(cell) && !cellRisks.ContainsKey(cell)) return;

            overlayTilemap.SetTile(cell, null);
            cellRisks.Remove(cell);
            cellIntensities.Remove(cell);
            cellCauses.Remove(cell);
            pulseRemaining.Remove(cell);
            telegraphingCells.Remove(cell);
            RemoveShakeVisual(cell);
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
                cellIntensities.Clear();
                cellCauses.Clear();
                pulseRemaining.Clear();
                telegraphingCells.Clear();
                ClearShakeVisuals();
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

        public bool TryGetCellIntensity(Vector3Int cell, out float intensity)
        {
            return cellIntensities.TryGetValue(cell, out intensity);
        }

        public bool IsTelegraphing(Vector3Int cell)
        {
            return telegraphingCells.Contains(cell);
        }

        private void Update()
        {
            UpdateCrackAnimation(Time.unscaledDeltaTime);
            UpdateFallingVisuals(Time.unscaledDeltaTime);
            UpdateDustVisuals(Time.unscaledDeltaTime);
        }

        private void UpdateCrackAnimation(float deltaTime)
        {
            if (overlayTilemap == null || visibleCells.Count == 0) return;
            bool reduceMotion = AccessibilityPreferences.ReduceMotion;
            var cells = new List<Vector3Int>(visibleCells);
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3Int cell = cells[i];
                if (pulseRemaining.TryGetValue(cell, out float remaining))
                {
                    remaining -= deltaTime;
                    if (remaining <= 0f) pulseRemaining.Remove(cell);
                    else pulseRemaining[cell] = remaining;
                }

                if (reduceMotion)
                {
                    pulseRemaining.Remove(cell);
                }

                overlayTilemap.SetColor(cell, ResolveAnimatedColor(cell));
                UpdateShakeAnimation(cell, deltaTime, reduceMotion);
            }
        }

        private Color ResolveAnimatedColor(Vector3Int cell)
        {
            if (!cellRisks.TryGetValue(cell, out StructuralRiskLevel risk))
                return Color.clear;

            Color color = HasAuthoredCrackTile(risk) ? Color.white : GetColor(risk);
            float intensity = cellIntensities.TryGetValue(cell, out float value) ? value : 0f;
            color.a = GetColor(risk).a * Mathf.Lerp(0.58f, 1f, intensity);

            if (!AccessibilityPreferences.ReduceMotion)
            {
                if (pulseRemaining.TryGetValue(cell, out float remaining) && pulseSeconds > 0f)
                    color.a = Mathf.Lerp(color.a, 1f, Mathf.Clamp01(remaining / pulseSeconds));
                if (telegraphingCells.Contains(cell) && telegraphFlashSeconds > 0f)
                {
                    float phase = Mathf.PingPong(Time.unscaledTime / telegraphFlashSeconds, 1f);
                    color.a = Mathf.Lerp(0.35f, 1f, phase);
                }
            }

            return color;
        }

        private void UpdateShakeVisual(Vector3Int cell, StructuralRiskCause cause)
        {
            if (cause == StructuralRiskCause.None)
            {
                RemoveShakeVisual(cell);
                return;
            }

            if (sourceTilemap == null || !sourceTilemap.HasTile(cell))
            {
                RemoveShakeVisual(cell);
                return;
            }

            if (shakeVisuals.TryGetValue(cell, out ShakeVisual existing)
                && existing.Root != null)
            {
                if (existing.Cause != cause)
                    existing.Elapsed = 0f;
                existing.Cause = cause;
                return;
            }

            Sprite sprite = sourceTilemap.GetSprite(cell);
            if (sprite == null) return;

            var root = new GameObject("StructuralShake_" + cell.x + "_" + cell.y);
            root.layer = sourceTilemap.gameObject.layer;
            root.transform.SetParent(transform, true);
            root.transform.position = sourceTilemap.GetCellCenterWorld(cell);

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            TilemapRenderer sourceRenderer = sourceTilemap.GetComponent<TilemapRenderer>();
            if (sourceRenderer != null)
            {
                renderer.sortingLayerID = sourceRenderer.sortingLayerID;
                renderer.sortingOrder = sourceRenderer.sortingOrder;
                renderer.sharedMaterial = sourceRenderer.sharedMaterial;
            }

            Color originalColor = sourceTilemap.GetColor(cell);
            renderer.color = originalColor * sourceTilemap.color;
            TileFlags originalFlags = sourceTilemap.GetTileFlags(cell);
            // 물리 Tilemap은 제자리에 둔 채 시각 복제본만 이동해 Collider 재생성을 막는다.
            sourceTilemap.SetTileFlags(cell, originalFlags & ~TileFlags.LockColor);
            Color hiddenColor = originalColor;
            hiddenColor.a = 0f;
            sourceTilemap.SetColor(cell, hiddenColor);

            shakeVisuals[cell] = new ShakeVisual
            {
                Root = root,
                Renderer = renderer,
                OriginalCellColor = originalColor,
                OriginalTileFlags = originalFlags,
                OriginalOverlayTransform = overlayTilemap.GetTransformMatrix(cell),
                Cause = cause
            };
        }

        private void UpdateShakeAnimation(Vector3Int cell, float deltaTime, bool reduceMotion)
        {
            if (!shakeVisuals.TryGetValue(cell, out ShakeVisual visual)
                || visual.Root == null)
            {
                return;
            }

            visual.Elapsed += Mathf.Max(0f, deltaTime);
            float distance = visual.Cause == StructuralRiskCause.Unsupported
                ? unsupportedShakeDistance
                : impactShakeDistance;
            float interval = visual.Cause == StructuralRiskCause.Unsupported
                ? unsupportedShakeInterval
                : impactShakeInterval;
            Vector2 offset = reduceMotion
                ? Vector2.zero
                : CalculateShakeOffset(visual.Elapsed, distance, interval, cell);
            Vector3 center = sourceTilemap != null
                ? sourceTilemap.GetCellCenterWorld(cell)
                : overlayTilemap.GetCellCenterWorld(cell);
            if (sourceTilemap != null && visual.Renderer != null)
                visual.Renderer.color = visual.OriginalCellColor * sourceTilemap.color;
            Vector3 worldOffset = new(offset.x, offset.y, 0f);
            visual.Root.transform.position = center + worldOffset;
            overlayTilemap.SetTransformMatrix(
                cell,
                reduceMotion
                    ? visual.OriginalOverlayTransform
                    : Matrix4x4.Translate(worldOffset)
                        * visual.OriginalOverlayTransform);
        }

        public static Vector2 CalculateShakeOffset(
            float elapsedSeconds,
            float distance,
            float interval,
            Vector3Int cell)
        {
            if (distance <= 0f || interval <= 0f || elapsedSeconds <= 0f)
                return Vector2.zero;

            float stepTime = elapsedSeconds / interval;
            int step = Mathf.FloorToInt(stepTime);
            float progress = Mathf.SmoothStep(0f, 1f, stepTime - step);
            Vector2 previous = step <= 0
                ? Vector2.zero
                : CalculateShakeTarget(cell, step, distance);
            Vector2 next = CalculateShakeTarget(cell, step + 1, distance);
            return Vector2.LerpUnclamped(previous, next, progress);
        }

        private static Vector2 CalculateShakeTarget(
            Vector3Int cell,
            int step,
            float distance)
        {
            uint hash = CalculateShakeHash(cell, step);
            bool positiveDirection = (hash & 8u) == 0u;
            if ((step & 1) == 0)
                return (positiveDirection ? Vector2.up : Vector2.down) * distance;
            return (positiveDirection ? Vector2.right : Vector2.left) * distance;
        }

        private static uint CalculateShakeHash(Vector3Int cell, int step)
        {
            unchecked
            {
                uint hash = (uint)cell.x * 0x9E3779B1u;
                hash ^= (uint)cell.y * 0x85EBCA77u;
                hash ^= (uint)cell.z * 0xC2B2AE3Du;
                hash ^= (uint)step * 0x27D4EB2Fu;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                hash *= 0x846CA68Bu;
                hash ^= hash >> 16;
                return hash;
            }
        }

        public bool HasShakeVisual(Vector3Int cell)
        {
            return shakeVisuals.TryGetValue(cell, out ShakeVisual visual)
                && visual.Root != null;
        }

        private void RemoveShakeVisual(Vector3Int cell)
        {
            if (!shakeVisuals.TryGetValue(cell, out ShakeVisual visual)) return;
            shakeVisuals.Remove(cell);

            if (sourceTilemap != null)
            {
                sourceTilemap.SetTileFlags(
                    cell,
                    visual.OriginalTileFlags & ~TileFlags.LockColor);
                sourceTilemap.SetColor(cell, visual.OriginalCellColor);
                sourceTilemap.SetTileFlags(cell, visual.OriginalTileFlags);
            }

            if (overlayTilemap != null)
                overlayTilemap.SetTransformMatrix(cell, visual.OriginalOverlayTransform);

            if (visual.Root != null)
                DestroyRuntimeObject(visual.Root);
        }

        private void ClearShakeVisuals()
        {
            if (shakeVisuals.Count == 0) return;
            var cells = new List<Vector3Int>(shakeVisuals.Keys);
            for (int i = 0; i < cells.Count; i++)
                RemoveShakeVisual(cells[i]);
        }

        private void UpdateFallingVisuals(float deltaTime)
        {
            for (int i = fallingVisuals.Count - 1; i >= 0; i--)
            {
                FallingVisual visual = fallingVisuals[i];
                if (visual.Root == null)
                {
                    fallingVisuals.RemoveAt(i);
                    continue;
                }

                visual.Elapsed += deltaTime;
                float t = Mathf.Clamp01(visual.Elapsed / visual.Duration);
                Vector3 previousPosition = visual.Root.transform.position;
                Vector3 nextPosition = Vector3.Lerp(visual.Start, visual.End, t * t);
                visual.Root.transform.position = nextPosition;
                if (!visual.ContactConsumed
                    && collapseDamageReceiver != null
                    && collapseDamageReceiver.IsCollapseContact(
                        previousPosition.x,
                        previousPosition.y,
                        nextPosition.x,
                        nextPosition.y))
                {
                    visual.ContactConsumed = true;
                    collapseDamageReceiver.ApplyCollapseImpact();
                }
                if (t < 1f) continue;

                // 충돌 뒤에는 낮고 흐린 비충돌 잔해로 남겨 낙하 결과 위치를 보여 준다.
                visual.Renderer.color = new Color(0.48f, 0.42f, 0.36f, 0.5f);
                visual.Root.transform.localScale = new Vector3(1f, 0.22f, 1f);
                if (!AccessibilityPreferences.ReduceMotion)
                    CreateImpactDust(visual.End);
                fallingVisuals.RemoveAt(i);
            }
        }

        private void CreateImpactDust(Vector3 position)
        {
            var root = new GameObject("StructuralImpactDust");
            root.transform.SetParent(transform, true);
            root.transform.position = position;
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ResolveEffectSprite();
            renderer.color = new Color(0.62f, 0.52f, 0.4f, 0.55f);
            renderer.sortingOrder = 92;
            root.transform.localScale = Vector3.one * 0.12f;
            dustVisuals.Add(new DustVisual
            {
                Root = root,
                Renderer = renderer,
                Duration = 0.3f
            });
        }

        private void UpdateDustVisuals(float deltaTime)
        {
            for (int i = dustVisuals.Count - 1; i >= 0; i--)
            {
                DustVisual dust = dustVisuals[i];
                if (dust.Root == null)
                {
                    dustVisuals.RemoveAt(i);
                    continue;
                }

                dust.Elapsed += deltaTime;
                float t = Mathf.Clamp01(dust.Elapsed / dust.Duration);
                dust.Root.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.85f, t);
                Color color = dust.Renderer.color;
                color.a = Mathf.Lerp(0.55f, 0f, t);
                dust.Renderer.color = color;
                if (t < 1f) continue;
                DestroyRuntimeObject(dust.Root);
                dustVisuals.RemoveAt(i);
            }
        }

        private Color GetColor(StructuralRiskLevel risk)
        {
            return risk == StructuralRiskLevel.Caution
                ? cautionColor
                : risk == StructuralRiskLevel.Danger ? dangerColor : imminentColor;
        }

        private Sprite ResolveEffectSprite()
        {
            if (runtimeEffectSprite != null) return runtimeEffectSprite;
            const int size = 8;
            runtimeEffectTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeStructuralEffect",
                filterMode = FilterMode.Point
            };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool border = x == 1 || x == size - 2 || y == 1 || y == size - 2;
                runtimeEffectTexture.SetPixel(x, y, border ? Color.white : Color.clear);
            }
            runtimeEffectTexture.Apply();
            runtimeEffectSprite = Sprite.Create(
                runtimeEffectTexture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            return runtimeEffectSprite;
        }

        private bool HasAuthoredCrackTile(StructuralRiskLevel risk)
        {
            return risk == StructuralRiskLevel.Caution
                ? cautionCrackTile != null
                : risk == StructuralRiskLevel.Danger
                    ? dangerCrackTile != null
                    : risk == StructuralRiskLevel.CollapseImminent
                        && imminentCrackTile != null;
        }

        private TileBase ResolveCrackTile(StructuralRiskLevel risk)
        {
            TileBase authored = risk == StructuralRiskLevel.Caution
                ? cautionCrackTile
                : risk == StructuralRiskLevel.Danger
                    ? dangerCrackTile
                    : risk == StructuralRiskLevel.CollapseImminent
                        ? imminentCrackTile
                        : null;
            if (authored != null) return authored;
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
            ClearShakeVisuals();
            DestroyRuntimeObject(runtimeCrackTile);
            DestroyRuntimeObject(runtimeCrackSprite);
            DestroyRuntimeObject(runtimeCrackTexture);
            DestroyRuntimeObject(runtimeEffectSprite);
            DestroyRuntimeObject(runtimeEffectTexture);
        }

        private static void DestroyRuntimeObject(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
