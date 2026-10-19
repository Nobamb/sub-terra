using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.Gameplay.Drone
{
    /// <summary>스캔 대상 칸만 재사용 표식으로 강조한다. 주변 일반 지형을 조명하지 않는다.</summary>
    public sealed class DroneScanPulseView : MonoBehaviour
    {
        [SerializeField] private Color mineralColor = new(0.92f, 0.97f, 1f, 1f);
        [SerializeField] private Color hazardColor = new(1f, 0.12f, 0.08f, 1f);
        [SerializeField] private Color goldColor = new(1f, 0.82f, 0.18f, 1f);
        [SerializeField] private Color sealedGlyphColor = new(0.18f, 0.92f, 0.88f, 1f);
        [SerializeField, Min(0f)] private float lightIntensity = 1.8f;
        [SerializeField, Min(0.1f)] private float lightRadius = 1.05f;
        [SerializeField, Min(0.1f)] private float ringDuration = 0.9f;

        private sealed class PooledMarker
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public SpriteRenderer GoldAccent;
            public SpriteRenderer[] Sparks;
            public DroneScanTarget Target;
            public Color Color;
            public float RevealAt;
        }

        private readonly List<PooledMarker> pool = new();
        private readonly Dictionary<Vector3Int, PooledMarker> activeTargets = new();
        private GameObject visualRoot;
        private SpriteRenderer rangeRing;
        private Texture2D ringTexture;
        private Sprite ringSprite;
        private Texture2D markerTexture;
        private Sprite markerSprite;
        private Sprite sparkSprite;
        private float expiresAt;
        private float ringExpiresAt;
        private float pulseStartedAt;
        private float growthEndsAt;
        private float ringEndRadius;
        private int activeCount;
        private Func<DroneScanTarget, bool> targetIsValid;

        /// <summary>기존 호출부 호환용 이름. 실제 Light2D 수가 아니라 활성 대상 표식 수다.</summary>
        public int ActiveLightCount => activeCount;
        public int ActiveMarkerCount => activeCount;
        public bool IsRingVisible => rangeRing != null && rangeRing.gameObject.activeSelf;

        public void Show(
            IReadOnlyList<DroneScanTarget> targets,
            Vector3 center,
            int radius,
            float nextExpiresAt,
            float currentTime,
            Func<DroneScanTarget, bool> isTargetValid = null)
        {
            EnsureVisualRoot();
            activeCount = 0;
            targetIsValid = isTargetValid;
            activeTargets.Clear();
            for (int index = 0; index < (targets?.Count ?? 0); index++)
            {
                DroneScanTarget target = targets[index];
                if (targetIsValid != null && !targetIsValid(target)) continue;
                PooledMarker marker = GetOrCreate(activeCount++);
                marker.Target = target;
                marker.Root.transform.position = target.WorldPosition + Vector3.back * 0.1f;
                marker.Root.SetActive(true);
                ApplyKind(marker, target);
                activeTargets[target.Cell] = marker;
            }

            for (int index = activeCount; index < pool.Count; index++)
            {
                pool[index].Root.SetActive(false);
            }

            expiresAt = nextExpiresAt;
            pulseStartedAt = currentTime;
            growthEndsAt = currentTime + Mathf.Max(0.1f, ringDuration) * 0.65f;
            ringEndRadius = Mathf.Max(0.15f, radius);
            // 논리 스캔은 기존 격자 반경을 유지한다. 드론에서 가장 먼 감지 칸도 파동이 지난다.
            for (int index = 0; index < activeCount; index++)
                ringEndRadius = Mathf.Max(ringEndRadius, Vector2.Distance(center, pool[index].Target.WorldPosition));
            // 같은 거리의 대상도 한꺼번에 번쩍이지 않도록 가까운 순서로 초점을 맞춘다.
            pool.Sort(0, activeCount, Comparer<PooledMarker>.Create((a, b) =>
                (a.Target.WorldPosition - center).sqrMagnitude.CompareTo((b.Target.WorldPosition - center).sqrMagnitude)));
            float previousReveal = currentTime - 0.012f;
            for (int index = 0; index < activeCount; index++)
            {
                var marker = pool[index];
                float distance = Vector2.Distance(center, marker.Target.WorldPosition);
                marker.RevealAt = Mathf.Max(previousReveal + 0.012f,
                    currentTime + (growthEndsAt - currentTime) * Mathf.InverseLerp(0.15f, ringEndRadius, distance));
                previousReveal = marker.RevealAt;
            }
            ShowRangeRing(center, radius > 0 ? 0.15f : 0f);
            ringExpiresAt = currentTime + Mathf.Max(0.1f, ringDuration);
            UpdateMarkerVisuals(currentTime);
        }

        public bool TryGetActiveTarget(Vector3Int cell, out DroneScanTargetKind kind)
        {
            if (activeTargets.TryGetValue(cell, out var marker) && marker.Renderer.color.a > 0f)
            {
                kind = marker.Target.Kind;
                return true;
            }
            kind = default;
            return false;
        }

        public void Tick(float currentTime)
        {
            PruneInvalidTargets();
            if (activeCount > 0 && currentTime >= expiresAt)
            {
                DeactivateMarkers();
            }

            if (rangeRing != null && rangeRing.gameObject.activeSelf && currentTime >= ringExpiresAt)
            {
                rangeRing.gameObject.SetActive(false);
            }
            else if (rangeRing != null && rangeRing.gameObject.activeSelf)
            {
                float expansion = Mathf.InverseLerp(pulseStartedAt, growthEndsAt, currentTime);
                rangeRing.transform.localScale = Vector3.one * Mathf.Lerp(0.15f, ringEndRadius, expansion);
                float remaining = 1f - Mathf.InverseLerp(growthEndsAt, ringExpiresAt, currentTime);
                rangeRing.color = new Color(0.35f, 0.72f, 0.8f, 0.45f * remaining);
            }
            UpdateMarkerVisuals(currentTime);
        }

        private void UpdateMarkerVisuals(float currentTime)
        {
            float fade = Mathf.Clamp01((expiresAt - currentTime) / Mathf.Max(0.001f, expiresAt - growthEndsAt));
            for (int index = 0; index < activeCount; index++)
            {
                var marker = pool[index];
                float age = currentTime - marker.RevealAt;
                float focus = Mathf.Clamp01(age / 0.16f);
                float flash = Mathf.Clamp01(1f - Mathf.Abs(age - 0.16f) / 0.045f);
                float exit = Mathf.Clamp01((currentTime - (expiresAt - 0.28f)) / 0.28f);
                if (exit > 0f)
                    flash = Mathf.Clamp01(1f - exit / 0.24f);
                float alpha = age <= 0f ? 0f : Mathf.Lerp(0.8f * (0.45f + 0.55f * fade), 1f, flash)
                    * Mathf.Clamp01(age / 0.08f) * (1f - exit);
                marker.Root.transform.localScale = Vector3.one *
                    (Mathf.Lerp(1.24f, 1f, 1f - Mathf.Pow(1f - focus, 3f)) + 0.24f * exit);
                var color = Color.Lerp(marker.Color, Color.white, flash * 0.9f);
                color.a = alpha;
                marker.Renderer.color = color;
                if (marker.GoldAccent != null)
                    marker.GoldAccent.color = new Color(goldColor.r, goldColor.g, goldColor.b,
                        alpha * (0.5f + 0.2f * Mathf.Sin(age * 8f)));
                if (marker.Sparks == null) continue;
                for (int spark = 0; spark < marker.Sparks.Length; spark++)
                {
                    float phase = Mathf.Repeat(age * 0.8f + spark / 3f, 1f);
                    float angle = spark * Mathf.PI * 2f / 3f + age;
                    marker.Sparks[spark].transform.localPosition = new Vector3(
                        Mathf.Cos(angle) * (0.4f + phase * 0.18f), Mathf.Sin(angle) * 0.4f + phase * 0.3f, 0f);
                    marker.Sparks[spark].color = new Color(0.45f, 1f, 0.65f, alpha * (1f - phase));
                }
            }
        }

        private void PruneInvalidTargets()
        {
            if (targetIsValid == null) return;
            for (int index = activeCount - 1; index >= 0; index--)
            {
                var marker = pool[index];
                if (targetIsValid(marker.Target)) continue;
                marker.Root.SetActive(false);
                activeTargets.Remove(marker.Target.Cell);
                activeCount--;
                // 활성 표식을 앞쪽에 유지해 다음 스캔에서도 같은 풀을 재사용한다.
                pool[index] = pool[activeCount];
                pool[activeCount] = marker;
            }
        }

        public void Clear()
        {
            DeactivateMarkers();
            if (rangeRing != null)
            {
                rangeRing.gameObject.SetActive(false);
            }
        }

        private void EnsureVisualRoot()
        {
            if (visualRoot != null) return;
            visualRoot = new GameObject("DroneScanPulseVisuals");
            visualRoot.hideFlags = HideFlags.DontSave;
            visualRoot.layer = gameObject.layer;
            CreateRing();
        }

        private PooledMarker GetOrCreate(int index)
        {
            if (index < pool.Count) return pool[index];

            var root = new GameObject("DroneScanMarker_" + pool.Count);
            root.layer = gameObject.layer;
            root.transform.SetParent(visualRoot.transform, true);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ResolveMarkerSprite();
            renderer.sortingOrder = 95;

            var marker = new PooledMarker
            {
                Root = root,
                Renderer = renderer
            };
            pool.Add(marker);
            return marker;
        }

        private void ApplyKind(PooledMarker marker, DroneScanTarget target)
        {
            Color color = target.Kind switch
            {
                DroneScanTargetKind.GasHazard => hazardColor,
                DroneScanTargetKind.GoldDrop => goldColor,
                DroneScanTargetKind.SealedGlyph => sealedGlyphColor,
                _ => mineralColor
            };
            if (target.Kind != DroneScanTargetKind.GasHazard)
                color = target.MineralId switch
                {
                    "mineral.copper" => new Color(0.83f, 0.45f, 0.22f),
                    "mineral.iron" => new Color(0.68f, 0.73f, 0.79f),
                    "mineral.lithium" => new Color(0.12f, 0.85f, 0.91f),
                    "item.rare.engine_fuel" => new Color(0.45f, 1f, 0.65f),
                    _ => color
                };
            marker.Color = color;
            marker.Renderer.color = new Color(color.r, color.g, color.b, 0f);
            bool goldAccent = target.HasGold && !string.IsNullOrEmpty(target.MineralId)
                && target.Kind != DroneScanTargetKind.GasHazard;
            if (goldAccent && marker.GoldAccent == null)
            {
                marker.GoldAccent = CreateOverlay(marker, "GoldAccent", 96);
                marker.GoldAccent.sprite = ResolveMarkerSprite();
                marker.GoldAccent.transform.localScale = Vector3.one * 1.08f;
            }
            if (marker.GoldAccent != null) marker.GoldAccent.gameObject.SetActive(goldAccent);
            bool fuel = target.MineralId == "item.rare.engine_fuel" && target.Kind != DroneScanTargetKind.GasHazard;
            if (fuel && marker.Sparks == null)
            {
                if (sparkSprite == null)
                    sparkSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
                marker.Sparks = new SpriteRenderer[3];
                for (int index = 0; index < marker.Sparks.Length; index++)
                {
                    var spark = CreateOverlay(marker, "FuelSpark_" + index, 96);
                    spark.sprite = sparkSprite;
                    spark.transform.localScale = Vector3.one * 0.035f;
                    marker.Sparks[index] = spark;
                }
            }
            if (marker.Sparks != null)
                foreach (var spark in marker.Sparks) spark.gameObject.SetActive(fuel);
        }

        private SpriteRenderer CreateOverlay(PooledMarker marker, string name, int sortingOrder)
        {
            var child = new GameObject(name);
            child.layer = gameObject.layer;
            child.transform.SetParent(marker.Root.transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private Sprite ResolveMarkerSprite()
        {
            if (markerSprite != null) return markerSprite;
            const int size = 64;
            markerTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeDroneTargetMarker", filterMode = FilterMode.Point
            };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int edgeX = Mathf.Min(x, size - 1 - x);
                int edgeY = Mathf.Min(y, size - 1 - y);
                bool bracket = edgeX >= 3 && edgeY >= 3
                    && ((edgeX <= 4 && edgeY <= 16) || (edgeY <= 4 && edgeX <= 16));
                float dx = Mathf.Abs(x - 31.5f);
                float dy = Mathf.Abs(y - 31.5f);
                bool cross = dx >= 5f && dx <= 13f && Mathf.Abs(dx - dy) <= 0.8f;
                markerTexture.SetPixel(x, y, bracket || cross ? Color.white : Color.clear);
            }
            markerTexture.Apply();
            markerSprite = Sprite.Create(markerTexture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size);
            return markerSprite;
        }

        private void ShowRangeRing(Vector3 center, float radius)
        {
            if (rangeRing == null) return;
            if (radius <= 0) { rangeRing.gameObject.SetActive(false); return; }
            rangeRing.transform.position = center + Vector3.back * 0.05f;
            rangeRing.transform.localScale = Vector3.one * radius;
            rangeRing.color = new Color(0.35f, 0.72f, 0.8f, 0.45f);
            rangeRing.gameObject.SetActive(true);
        }

        private void CreateRing()
        {
            const int size = 512;
            ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeDroneScanRing",
                filterMode = FilterMode.Bilinear
            };
            float center = (size - 1) * 0.5f;
            float ringRadius = center - 1.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float edge = Mathf.InverseLerp(0.3f, 1.3f, Mathf.Abs(distance - ringRadius));
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, edge);
                ringTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            ringTexture.Apply();
            ringSprite = Sprite.Create(
                ringTexture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size * 0.5f);
            var ringObject = new GameObject("DroneScanRangeRing");
            ringObject.layer = gameObject.layer;
            ringObject.transform.SetParent(visualRoot.transform, true);
            rangeRing = ringObject.AddComponent<SpriteRenderer>();
            rangeRing.sprite = ringSprite;
            rangeRing.sortingOrder = 94;
            ringObject.SetActive(false);
        }

        private void DeactivateMarkers()
        {
            for (int index = 0; index < pool.Count; index++)
            {
                pool[index].Root.SetActive(false);
            }
            activeCount = 0;
            activeTargets.Clear();
            targetIsValid = null;
        }

        private void OnDisable()
        {
            Clear();
        }

        private void OnDestroy()
        {
            if (visualRoot != null) DestroyRuntimeObject(visualRoot);
            DestroyRuntimeObject(ringSprite);
            DestroyRuntimeObject(ringTexture);
            DestroyRuntimeObject(markerSprite);
            DestroyRuntimeObject(markerTexture);
            DestroyRuntimeObject(sparkSprite);
        }

        private static void DestroyRuntimeObject(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
