using System;
using System.Collections.Generic;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Power
{
    /// <summary>
    /// B-185(#292): 시설이 꺼짐→켜짐으로 바뀔 때 청록 정전기 몇 줄기와 은은한 청록빛을 짧게 보여 준다.
    /// 소리는 붙이지 않고 Started/BatchStarted 이벤트만 연다. 오브젝트는 시설당 하나를 재사용한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PowerOnEffect : MonoBehaviour
    {
        // ★ PM 조정용 숫자. 길이(초)·밝기(알파)·크기(배율)·개수.
        public const float PowerOnDuration = 0.7f;            // ★ 전체 길이(0.5~0.8)
        public const float GlowStartScale = 0.8f;             // ★ 빛 시작 크기(시설 크기 대비)
        public const float GlowEndScale = 1.3f;               // ★ 빛 끝 크기
        public const float GlowMaxAlpha = 0.35f;              // ★ 빛 최대 알파
        public const float GlowRiseSeconds = 0.15f;           // ★ 빛이 가장 밝아지는 시점
        public const float GlowFadeEndSeconds = 0.6f;         // ★ 빛이 다 사라지는 시점
        public const float GlowSizeFactor = 1.1f;             // ★ 시설 그림 크기 대비 빛 기준 지름
        public const int SparkCountMin = 2;                   // ★ 정전기 최소 개수
        public const int SparkCountMax = 4;                   // ★ 정전기 최대 개수
        public const float SparkWindowSeconds = 0.35f;        // ★ 정전기가 튀는 구간(처음 0.35초)
        public const float SparkLifeMin = 0.05f;              // ★ 정전기 한 줄기 최소 표시 시간
        public const float SparkLifeMax = 0.08f;              // ★ 정전기 한 줄기 최대 표시 시간
        public const float SparkLength = 0.36f;               // ★ 정전기 길이(월드 단위)
        public const float SparkEdgeInset = 0.12f;            // ★ 시설 가장자리에서 안쪽으로 들어온 거리
        public const float PoweredFadeIn = 0.25f;             // ★ 켜짐 그림 페이드 시간
        public const bool ReducedMotionShowsGlow = true;      // ★ 모션 감소: true=약한 빛, false=연출 없음
        public const float ReducedMotionGlowAlpha = 0.12f;    // ★ 모션 감소 빛 최대 알파
        public const float ReducedMotionRiseSeconds = 0.1f;   // ★ 모션 감소 빛 최대 시점
        public const float ReducedMotionDuration = 0.35f;     // ★ 모션 감소 전체 길이
        public const int GlowSortingOffset = 1;               // ★ 시설 최대 sortingOrder + 1
        public const int SparkSortingOffset = 2;              // ★ 시설 최대 sortingOrder + 2

        /// <summary>MinimapPalette.Cyan과 같은 값. Gameplay는 App을 참조하지 않으므로 여기 둔다.</summary>
        public static readonly Color Cyan = new(0.26f, 0.9f, 1f, 1f);
        public static readonly Color CyanCore = new(0.78f, 1f, 1f, 1f);

        public const string EffectRootName = "PowerOnEffect";
        private const float VisibleRendererAlpha = 0.1f;
        private const int SparkVariantCount = 3;

        /// <summary>연출이 시작된 시설마다 한 번. isBatch는 같은 Rebuild에서 둘 이상 켜졌는지.</summary>
        public static event Action<PowerFacility, bool> Started;
        /// <summary>한 Rebuild에서 연출이 하나 이상 시작되면 한 번만(켜진 개수, 시설 중심 평균).</summary>
        public static event Action<int, Vector3> BatchStarted;

        private static readonly List<PowerOnEffect> pendingStarts = new();
        private static int suppressionDepth;
        private static Sprite glowSprite;
        private static Sprite[] sparkSprites;

        private PowerFacility owner;
        private PowerNode ownerNode;
        private GameObject effectRoot;
        private SpriteRenderer glowRenderer;
        private readonly List<SpriteRenderer> sparkRenderers = new();
        private readonly float[] sparkStarts = new float[SparkCountMax];
        private readonly float[] sparkLives = new float[SparkCountMax];
        private readonly List<SpriteRenderer> fadeTargets = new();
        private readonly Dictionary<SpriteRenderer, Color> fadeOriginals = new();
        private System.Random random;
        private Vector2 glowBaseSize = Vector2.one;
        private float elapsed;
        private float duration;
        private float peakGlowAlpha;
        private bool reducedMotion;
        private bool fading;

        /// <summary>불러오기 등 조용히 켜야 하는 구간이면 true. 이때는 연출·이벤트가 없다.</summary>
        public static bool IsSuppressed => suppressionDepth > 0;
        public bool IsPlaying { get; private set; }
        public int ActiveSparkCount { get; private set; }
        /// <summary>이번 연출에서 빛이 낼 수 있는 최대 알파.</summary>
        public float PeakGlowAlpha => IsPlaying ? peakGlowAlpha : 0f;
        public float CurrentGlowAlpha => glowRenderer != null && IsPlaying ? glowRenderer.color.a : 0f;
        public float Elapsed => elapsed;
        public float Duration => duration;
        public GameObject EffectRoot => effectRoot;

        /// <summary>RestoreSnapshot 같은 구간 시작. 반드시 EndSuppression과 짝을 맞춘다.</summary>
        public static void BeginSuppression() => suppressionDepth++;
        public static void EndSuppression() => suppressionDepth = Mathf.Max(0, suppressionDepth - 1);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            pendingStarts.Clear();
            suppressionDepth = 0;
            Started = null;
            BatchStarted = null;
        }

        public void Bind(PowerFacility facility, PowerNode node)
        {
            owner = facility;
            ownerNode = node;
        }

        /// <summary>연출을 처음부터 시작한다. 생략(모션 감소로 빛 없음)이면 false.</summary>
        public bool Play(IReadOnlyList<GameObject> poweredVisuals)
        {
            Stop();
            reducedMotion = AccessibilityPreferences.ReduceMotion;
            if (reducedMotion && !ReducedMotionShowsGlow) return false;

            EnsureObjects();
            ResolveFrame(out Bounds frame, out int sortingLayerId, out int maxOrder);
            effectRoot.transform.position = frame.center;
            Vector3 parentScale = transform.lossyScale;
            effectRoot.transform.localScale = new Vector3(SafeInverse(parentScale.x), SafeInverse(parentScale.y), 1f);

            float width = Mathf.Max(0.3f, frame.size.x) * GlowSizeFactor;
            float height = Mathf.Clamp(Mathf.Max(0.3f, frame.size.y) * GlowSizeFactor, width * 0.6f, width * 1.6f);
            glowBaseSize = new Vector2(width, height);
            glowRenderer.sortingLayerID = sortingLayerId;
            glowRenderer.sortingOrder = maxOrder + GlowSortingOffset;
            peakGlowAlpha = reducedMotion ? ReducedMotionGlowAlpha : GlowMaxAlpha;
            duration = reducedMotion ? ReducedMotionDuration : PowerOnDuration;

            ScheduleSparks(frame, sortingLayerId, maxOrder + SparkSortingOffset);
            PrepareFade(reducedMotion ? null : poweredVisuals);

            elapsed = 0f;
            IsPlaying = true;
            effectRoot.SetActive(true);
            Apply(0f);
            QueueStart();
            return true;
        }

        /// <summary>연출을 즉시 정리한다. 켜짐 그림 알파는 원래대로 돌린다.</summary>
        public void Stop()
        {
            pendingStarts.Remove(this);
            RestoreFade();
            if (effectRoot != null) effectRoot.SetActive(false);
            IsPlaying = false;
            ActiveSparkCount = 0;
            elapsed = 0f;
        }

        /// <summary>정지 중(Time.deltaTime 0)에는 멈춘다. 테스트는 직접 호출한다.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsPlaying) return;
            elapsed += Mathf.Max(0f, deltaTime);
            if (elapsed >= duration)
            {
                Stop();
                return;
            }
            Apply(elapsed);
        }

        private void Update()
        {
            if (IsPlaying) Tick(Time.deltaTime);
        }

        private void LateUpdate()
        {
            // NetworkRebuilt를 못 받은 경우의 안전망. 보통은 Rebuild 끝에서 이미 비어 있다.
            if (pendingStarts.Count > 0) FlushPendingStarts();
        }

        private void OnDisable() => Stop();

        private void OnDestroy()
        {
            pendingStarts.Remove(this);
            if (ownerNode != null && ownerNode.Network != null) ownerNode.Network.NetworkRebuilt -= OnNetworkRebuilt;
        }

        private void QueueStart()
        {
            if (!pendingStarts.Contains(this)) pendingStarts.Add(this);
            PowerNetworkSystem network = ownerNode != null ? ownerNode.Network : null;
            if (network == null) return;
            network.NetworkRebuilt -= OnNetworkRebuilt;
            network.NetworkRebuilt += OnNetworkRebuilt;
        }

        private static void OnNetworkRebuilt(PowerNetworkSnapshot _) => FlushPendingStarts();

        private static void FlushPendingStarts()
        {
            if (pendingStarts.Count == 0) return;
            PowerOnEffect[] batch = pendingStarts.ToArray();
            pendingStarts.Clear();

            int count = 0;
            Vector3 sum = Vector3.zero;
            foreach (PowerOnEffect effect in batch)
            {
                if (effect == null || !effect.IsPlaying) continue;
                count++;
                sum += effect.effectRoot != null ? effect.effectRoot.transform.position : effect.transform.position;
            }
            if (count == 0) return;

            bool isBatch = count > 1;
            foreach (PowerOnEffect effect in batch)
            {
                if (effect == null || !effect.IsPlaying) continue;
                try
                {
                    Started?.Invoke(effect.owner, isBatch);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, effect);
                }
            }

            try
            {
                BatchStarted?.Invoke(count, sum / count);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void Apply(float t)
        {
            float rise = reducedMotion ? ReducedMotionRiseSeconds : GlowRiseSeconds;
            float end = reducedMotion ? ReducedMotionDuration : GlowFadeEndSeconds;
            float alpha = GlowAlpha(t, rise, end, peakGlowAlpha);
            float scale = reducedMotion
                ? 1f
                : Mathf.Lerp(GlowStartScale, GlowEndScale, EaseOut(Mathf.Clamp01(t / end)));
            glowRenderer.transform.localScale = new Vector3(glowBaseSize.x * scale, glowBaseSize.y * scale, 1f);
            glowRenderer.color = new Color(Cyan.r, Cyan.g, Cyan.b, alpha);
            glowRenderer.enabled = alpha > 0.001f;

            for (int i = 0; i < sparkRenderers.Count; i++)
            {
                bool visible = i < ActiveSparkCount && t >= sparkStarts[i] && t < sparkStarts[i] + sparkLives[i];
                sparkRenderers[i].enabled = visible;
            }

            if (!fading) return;
            float k = PoweredFadeIn <= 0f ? 1f : Mathf.Clamp01(t / PoweredFadeIn);
            foreach (SpriteRenderer target in fadeTargets)
            {
                if (target == null || !fadeOriginals.TryGetValue(target, out Color original)) continue;
                target.color = new Color(original.r, original.g, original.b, original.a * k);
            }
            if (k >= 1f) RestoreFade();
        }

        private static float GlowAlpha(float t, float rise, float end, float peak)
        {
            if (t <= 0f || peak <= 0f) return 0f;
            if (t < rise) return peak * EaseOut(t / rise);
            if (t >= end) return 0f;
            float fall = Mathf.Clamp01((t - rise) / Mathf.Max(0.0001f, end - rise));
            return peak * (1f - Mathf.SmoothStep(0f, 1f, fall));
        }

        private static float EaseOut(float x) => 1f - (1f - x) * (1f - x);

        private static float SafeInverse(float value) => Mathf.Abs(value) < 0.0001f ? 1f : 1f / value;

        private void ScheduleSparks(Bounds frame, int sortingLayerId, int sortingOrder)
        {
            ActiveSparkCount = reducedMotion ? 0 : random.Next(SparkCountMin, SparkCountMax + 1);
            for (int i = 0; i < sparkRenderers.Count; i++)
            {
                SpriteRenderer spark = sparkRenderers[i];
                spark.enabled = false;
                spark.sortingLayerID = sortingLayerId;
                spark.sortingOrder = sortingOrder;
                if (i >= ActiveSparkCount) continue;

                float life = Mathf.Lerp(SparkLifeMin, SparkLifeMax, (float)random.NextDouble());
                float slot = SparkWindowSeconds / ActiveSparkCount;
                float start = slot * i + (float)random.NextDouble() * Mathf.Max(0f, slot - life * 0.5f);
                sparkStarts[i] = Mathf.Min(start, SparkWindowSeconds - life);
                sparkLives[i] = life;

                // 시설 가장자리 둘레의 임의 위치. 가장자리 방향을 따라 눕히고 살짝 비튼다.
                Vector2 half = new(Mathf.Max(0.1f, frame.extents.x - SparkEdgeInset), Mathf.Max(0.1f, frame.extents.y - SparkEdgeInset));
                int side = random.Next(4);
                float along = (float)random.NextDouble() * 2f - 1f;
                Vector2 local;
                float angle;
                switch (side)
                {
                    case 0: local = new Vector2(along * half.x, half.y); angle = 0f; break;
                    case 1: local = new Vector2(along * half.x, -half.y); angle = 0f; break;
                    case 2: local = new Vector2(-half.x, along * half.y); angle = 90f; break;
                    default: local = new Vector2(half.x, along * half.y); angle = 90f; break;
                }
                angle += ((float)random.NextDouble() * 2f - 1f) * 30f;

                spark.sprite = sparkSprites[random.Next(sparkSprites.Length)];
                spark.flipX = random.Next(2) == 0;
                spark.flipY = random.Next(2) == 0;
                spark.color = Color.white;
                spark.transform.localPosition = local;
                spark.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                spark.transform.localScale = Vector3.one * SparkLength;
            }
        }

        private void PrepareFade(IReadOnlyList<GameObject> poweredVisuals)
        {
            fadeTargets.Clear();
            fading = false;
            if (poweredVisuals == null || PoweredFadeIn <= 0f) return;
            foreach (GameObject visual in poweredVisuals)
            {
                if (visual == null) continue;
                foreach (SpriteRenderer target in visual.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (target == null) continue;
                    // 원래 색은 처음 본 값으로 한 번만 기억한다(페이드 중간값을 덮어쓰지 않게).
                    if (!fadeOriginals.ContainsKey(target)) fadeOriginals[target] = target.color;
                    fadeTargets.Add(target);
                }
            }
            fading = fadeTargets.Count > 0;
        }

        private void RestoreFade()
        {
            if (!fading) return;
            foreach (SpriteRenderer target in fadeTargets)
            {
                if (target != null && fadeOriginals.TryGetValue(target, out Color original)) target.color = original;
            }
            fadeTargets.Clear();
            fading = false;
        }

        /// <summary>시설 본체 그림(VisualRoot/Artwork) 기준 영역과 정렬값을 찾는다.</summary>
        private void ResolveFrame(out Bounds frame, out int sortingLayerId, out int maxOrder)
        {
            frame = new Bounds(transform.position, Vector3.one);
            sortingLayerId = 0;
            maxOrder = 0;
            bool hasFrame = false;
            bool hasOrder = false;

            Transform artwork = transform.Find("VisualRoot/Artwork");
            if (artwork != null && artwork.TryGetComponent(out SpriteRenderer artworkRenderer)
                && artworkRenderer.enabled && artworkRenderer.sprite != null)
            {
                frame = artworkRenderer.bounds;
                sortingLayerId = artworkRenderer.sortingLayerID;
                hasFrame = true;
            }

            foreach (SpriteRenderer candidate in GetComponentsInChildren<SpriteRenderer>(false))
            {
                if (candidate == null || !candidate.enabled || candidate.sprite == null) continue;
                if (effectRoot != null && candidate.transform.IsChildOf(effectRoot.transform)) continue;
                // 가스 걷힘처럼 거의 투명한 덧그림은 정렬 기준에서 뺀다.
                if (candidate.color.a < VisibleRendererAlpha) continue;
                if (!hasOrder || candidate.sortingOrder > maxOrder) maxOrder = candidate.sortingOrder;
                if (!hasOrder && !hasFrame) sortingLayerId = candidate.sortingLayerID;
                hasOrder = true;
                if (hasFrame) continue;
                frame = candidate.bounds;
                hasFrame = true;
            }
        }

        private void EnsureObjects()
        {
            random ??= new System.Random(unchecked(GetEntityId().GetHashCode() * 7919 + 17));
            EnsureSprites();
            if (effectRoot != null) return;

            effectRoot = new GameObject(EffectRootName);
            effectRoot.layer = gameObject.layer;
            effectRoot.transform.SetParent(transform, false);
            effectRoot.SetActive(false);

            var glowObject = new GameObject("Glow");
            glowObject.layer = gameObject.layer;
            glowObject.transform.SetParent(effectRoot.transform, false);
            glowRenderer = glowObject.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = glowSprite;

            sparkRenderers.Clear();
            for (int i = 0; i < SparkCountMax; i++)
            {
                var sparkObject = new GameObject("Spark" + i);
                sparkObject.layer = gameObject.layer;
                sparkObject.transform.SetParent(effectRoot.transform, false);
                SpriteRenderer spark = sparkObject.AddComponent<SpriteRenderer>();
                spark.enabled = false;
                sparkRenderers.Add(spark);
            }
        }

        private static void EnsureSprites()
        {
            if (glowSprite == null) glowSprite = CreateGlowSprite();
            if (sparkSprites == null || sparkSprites.Length != SparkVariantCount || sparkSprites[0] == null)
            {
                sparkSprites = new[]
                {
                    CreateSparkSprite("RuntimePowerOnSparkA", new[] { new Vector2Int(0, 3), new Vector2Int(3, 5), new Vector2Int(5, 1), new Vector2Int(8, 5), new Vector2Int(10, 2), new Vector2Int(11, 3) }),
                    CreateSparkSprite("RuntimePowerOnSparkB", new[] { new Vector2Int(0, 2), new Vector2Int(2, 5), new Vector2Int(5, 2), new Vector2Int(7, 4), new Vector2Int(9, 1), new Vector2Int(11, 4) }),
                    CreateSparkSprite("RuntimePowerOnSparkC", new[] { new Vector2Int(0, 4), new Vector2Int(4, 1), new Vector2Int(6, 5), new Vector2Int(9, 2), new Vector2Int(11, 3) })
                };
            }
        }

        /// <summary>부드러운 원형 그라디언트(지름 1 월드 단위).</summary>
        private static Sprite CreateGlowSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimePowerOnGlow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - radius) / radius;
                float dy = (y + 0.5f - radius) / radius;
                float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float a = (1f - r) * (1f - r);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>픽셀 느낌의 짧은 지그재그(가로 1 월드 단위). 가운데 구간만 조금 밝다.</summary>
        private static Sprite CreateSparkSprite(string name, Vector2Int[] points)
        {
            const int width = 12;
            const int height = 7;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            var core = new bool[width * height];
            for (int i = 0; i + 1 < points.Length; i++) MarkLine(core, width, points[i], points[i + 1]);

            Color halo = new(Cyan.r, Cyan.g, Cyan.b, 0.35f);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (core[index])
                {
                    bool middle = x >= width / 3 && x < width - width / 3;
                    pixels[index] = middle ? CyanCore : Cyan;
                    continue;
                }
                bool nearCore = (y > 0 && core[index - width]) || (y + 1 < height && core[index + width]);
                pixels[index] = nearCore ? halo : Color.clear;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), width);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static void MarkLine(bool[] mask, int width, Vector2Int from, Vector2Int to)
        {
            int x = from.x, y = from.y;
            int dx = Mathf.Abs(to.x - x), sx = x < to.x ? 1 : -1;
            int dy = -Mathf.Abs(to.y - y), sy = y < to.y ? 1 : -1;
            int error = dx + dy;
            while (true)
            {
                mask[y * width + x] = true;
                if (x == to.x && y == to.y) return;
                int doubled = error * 2;
                if (doubled >= dy) { error += dy; x += sx; }
                if (doubled <= dx) { error += dx; y += sy; }
            }
        }
    }
}
