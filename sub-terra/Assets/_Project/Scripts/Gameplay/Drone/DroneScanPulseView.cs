using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SubTerra.Gameplay.Drone
{
    /// <summary>스캔 대상만 재사용 Light2D로 표시하고 수명이 끝나면 모두 회수한다.</summary>
    public sealed class DroneScanPulseView : MonoBehaviour
    {
        [SerializeField] private Color mineralColor = new(0.92f, 0.97f, 1f, 1f);
        [SerializeField] private Color hazardColor = new(1f, 0.12f, 0.08f, 1f);
        [SerializeField, Min(0f)] private float lightIntensity = 1.8f;
        [SerializeField, Min(0.1f)] private float lightRadius = 0.72f;
        [SerializeField, Min(0.1f)] private float ringDuration = 1.5f;

        private sealed class PooledMarker
        {
            public GameObject Root;
            public Light2D Light;
            public SpriteRenderer PrimaryShape;
            public SpriteRenderer SecondaryShape;
        }

        private readonly List<PooledMarker> pool = new();
        private GameObject visualRoot;
        private SpriteRenderer rangeRing;
        private Texture2D markerTexture;
        private Sprite markerSprite;
        private Texture2D ringTexture;
        private Sprite ringSprite;
        private float expiresAt;
        private float ringExpiresAt;
        private int activeCount;

        public int ActiveLightCount => activeCount;
        public bool IsRingVisible => rangeRing != null && rangeRing.gameObject.activeSelf;

        public void Show(
            IReadOnlyList<DroneScanTarget> targets,
            Vector3 center,
            int radius,
            float nextExpiresAt,
            float currentTime)
        {
            EnsureVisualRoot();
            activeCount = targets?.Count ?? 0;
            for (int index = 0; index < activeCount; index++)
            {
                PooledMarker marker = GetOrCreate(index);
                DroneScanTarget target = targets[index];
                marker.Root.transform.position = target.WorldPosition + Vector3.back * 0.1f;
                marker.Root.SetActive(true);
                ApplyKind(marker, target.Kind);
            }

            for (int index = activeCount; index < pool.Count; index++)
            {
                pool[index].Root.SetActive(false);
            }

            expiresAt = nextExpiresAt;
            ShowRangeRing(center, radius);
            ringExpiresAt = currentTime + Mathf.Max(0.1f, ringDuration);
        }

        public void Tick(float currentTime)
        {
            if (activeCount > 0 && currentTime >= expiresAt)
            {
                DeactivateMarkers();
            }

            if (rangeRing != null && rangeRing.gameObject.activeSelf && currentTime >= ringExpiresAt)
            {
                rangeRing.gameObject.SetActive(false);
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
            visualRoot.layer = gameObject.layer;
            CreateMarkerSprite();
            CreateRing();
        }

        private PooledMarker GetOrCreate(int index)
        {
            if (index < pool.Count) return pool[index];

            var root = new GameObject("DroneScanLight_" + pool.Count);
            root.layer = gameObject.layer;
            root.transform.SetParent(visualRoot.transform, true);
            Light2D light = root.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.intensity = lightIntensity;
            light.pointLightInnerRadius = lightRadius * 0.3f;
            light.pointLightOuterRadius = lightRadius;

            SpriteRenderer primary = CreateShape(root.transform, "PrimaryShape");
            SpriteRenderer secondary = CreateShape(root.transform, "SecondaryShape");
            var marker = new PooledMarker
            {
                Root = root,
                Light = light,
                PrimaryShape = primary,
                SecondaryShape = secondary
            };
            pool.Add(marker);
            return marker;
        }

        private SpriteRenderer CreateShape(Transform parent, string objectName)
        {
            var shape = new GameObject(objectName);
            shape.layer = gameObject.layer;
            shape.transform.SetParent(parent, false);
            SpriteRenderer renderer = shape.AddComponent<SpriteRenderer>();
            renderer.sprite = markerSprite;
            renderer.sortingOrder = 95;
            return renderer;
        }

        private void ApplyKind(PooledMarker marker, DroneScanTargetKind kind)
        {
            bool hazard = kind == DroneScanTargetKind.GasHazard;
            Color color = hazard ? hazardColor : mineralColor;
            marker.Light.color = color;
            marker.Light.intensity = lightIntensity;
            marker.PrimaryShape.color = color;
            marker.SecondaryShape.color = color;

            if (hazard)
            {
                SetShape(marker.PrimaryShape.transform, new Vector3(0.48f, 0.08f, 1f), 45f, true);
                SetShape(marker.SecondaryShape.transform, new Vector3(0.48f, 0.08f, 1f), -45f, true);
            }
            else
            {
                SetShape(marker.PrimaryShape.transform, new Vector3(0.28f, 0.28f, 1f), 45f, true);
                SetShape(marker.SecondaryShape.transform, Vector3.one, 0f, false);
            }
        }

        private static void SetShape(Transform shape, Vector3 scale, float rotation, bool active)
        {
            shape.gameObject.SetActive(active);
            shape.localScale = scale;
            shape.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private void ShowRangeRing(Vector3 center, int radius)
        {
            if (rangeRing == null || radius <= 0) return;
            rangeRing.transform.position = center + Vector3.back * 0.05f;
            rangeRing.transform.localScale = Vector3.one * radius;
            rangeRing.color = new Color(0.35f, 0.9f, 1f, 0.42f);
            rangeRing.gameObject.SetActive(true);
        }

        private void CreateMarkerSprite()
        {
            markerTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "RuntimeDroneScanMarker"
            };
            markerTexture.SetPixel(0, 0, Color.white);
            markerTexture.Apply();
            markerSprite = Sprite.Create(markerTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private void CreateRing()
        {
            const int size = 128;
            ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeDroneScanRing",
                filterMode = FilterMode.Bilinear
            };
            float center = (size - 1) * 0.5f;
            float outer = center;
            float inner = outer - 2.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                ringTexture.SetPixel(x, y, distance >= inner && distance <= outer ? Color.white : Color.clear);
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
        }

        private void OnDisable()
        {
            Clear();
        }

        private void OnDestroy()
        {
            if (visualRoot != null) DestroyRuntimeObject(visualRoot);
            DestroyRuntimeObject(markerSprite);
            DestroyRuntimeObject(markerTexture);
            DestroyRuntimeObject(ringSprite);
            DestroyRuntimeObject(ringTexture);
        }

        private static void DestroyRuntimeObject(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
