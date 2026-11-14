using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>
    /// 가스 생성 이펙트. 중심에서 1초에 걸쳐 5칸 반경·불투명도 70% 이하로 나타난다.
    /// 구름은 가장자리가 부드럽게 사라지고, 바닥 쪽에 고이며, 느리게 흐른다. 시각은 항상 판정 반지름 안쪽이다.
    /// 화면 탁화(접근 이펙트)와는 별개다.
    /// </summary>
    [RequireComponent(typeof(GasZone))]
    public sealed class GasZoneVisual : MonoBehaviour
    {
        private static readonly int NoiseScaleAId = Shader.PropertyToID("_NoiseScaleA");
        private static readonly int NoiseScaleBId = Shader.PropertyToID("_NoiseScaleB");
        private static readonly int ScrollAId = Shader.PropertyToID("_ScrollA");
        private static readonly int ScrollBId = Shader.PropertyToID("_ScrollB");
        private static readonly int NoiseAmountId = Shader.PropertyToID("_NoiseAmount");
        private static readonly int EdgeWarpId = Shader.PropertyToID("_EdgeWarp");
        private static readonly int CoreId = Shader.PropertyToID("_Core");
        private static readonly int FloorBiasId = Shader.PropertyToID("_FloorBias");
        private static readonly int TopDensityId = Shader.PropertyToID("_TopDensity");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");

        [SerializeField] private SpriteRenderer cloudRenderer;
        [SerializeField] private Material cloudMaterial;
        [SerializeField] private Sprite softSprite;
        [SerializeField] private GasCloudTuning tuning = new();

        private readonly List<SpriteRenderer> motes = new();
        private MaterialPropertyBlock propertyBlock;
        private GasZone zone;
        private bool propertiesDirty = true;

        public GasCloudTuning Tuning => tuning;
        public SpriteRenderer CloudRenderer => cloudRenderer;
        public int ActiveMoteCount => motes.Count;

        private void Awake()
        {
            zone = GetComponent<GasZone>();
            EnsureRenderer();
            ApplyVisual();
        }

        private void LateUpdate()
        {
            ApplyVisual();
            // 화면 밖 구역은 입자를 풀에 돌려줘 비용을 줄인다. 구름 스크롤은 셰이더라 화면 밖이면 그려지지 않는다.
            UpdateMotes(cloudRenderer != null && cloudRenderer.isVisible, Time.time);
        }

        private void OnDisable()
        {
            ReleaseMotes();
        }

        private void OnValidate()
        {
            propertiesDirty = true;
        }

        public void ApplyVisual()
        {
            EnsureRenderer();
            if (cloudRenderer == null || zone == null)
            {
                return;
            }

            var progress = zone.IsActive ? zone.SpawnProgress : 0f;
            var diameter = zone.Radius * 2f * Mathf.Max(0.05f, progress);
            FitSpriteToWorldDiameter(cloudRenderer, diameter);
            var color = GasVisualRules.GasTint;
            color.a = tuning.OpacityCapped * progress;
            cloudRenderer.color = color;
            cloudRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            cloudRenderer.sortingOrder = 20;
            cloudRenderer.enabled = zone.IsActive && progress > 0.001f;
            ApplyPropertyBlock();
        }

        /// <summary>
        /// 입자를 갱신한다. visible이 false면 입자를 풀에 돌려준다. 테스트가 직접 호출할 수 있다.
        /// 입자는 판정 반지름의 90% 안쪽, 바닥 쪽에만 떠다닌다.
        /// </summary>
        public void UpdateMotes(bool visible, float time)
        {
            if (zone == null || cloudRenderer == null || !zone.IsActive || !visible)
            {
                ReleaseMotes();
                return;
            }

            var progress = zone.SpawnProgress;
            var target = progress > 0.4f ? tuning.MotesCapped : 0;
            while (motes.Count > target)
            {
                GasMotePool.Return(motes[motes.Count - 1]);
                motes.RemoveAt(motes.Count - 1);
            }

            while (motes.Count < target)
            {
                var renderer = GasMotePool.Rent(transform, ResolveSoftSprite());
                if (renderer == null)
                {
                    break;
                }

                motes.Add(renderer);
            }

            var seed = ResolveSeed();
            var radius = zone.Radius * Mathf.Max(0.05f, progress);
            var size = tuning.moteWorldSize;
            for (var i = 0; i < motes.Count; i++)
            {
                GasVisualMath.EvaluateMote(i, seed, time, tuning.floorBias, out var local, out var alpha, out var scale);
                var mote = motes[i];
                mote.transform.localPosition = new Vector3(local.x * radius, local.y * radius, 0f);
                FitSpriteToWorldDiameter(mote, size * scale);
                var color = GasVisualRules.GasTint;
                color.r = Mathf.Min(1f, color.r + 0.12f);
                color.g = Mathf.Min(1f, color.g + 0.12f);
                color.b = Mathf.Min(1f, color.b + 0.12f);
                color.a = tuning.moteMaxAlpha * alpha * Mathf.Clamp01(progress);
                mote.color = color;
            }
        }

        private void ReleaseMotes()
        {
            for (var i = motes.Count - 1; i >= 0; i--)
            {
                GasMotePool.Return(motes[i]);
            }

            motes.Clear();
        }

        private void ApplyPropertyBlock()
        {
            if (!propertiesDirty || cloudRenderer == null)
            {
                return;
            }

            propertiesDirty = false;
            propertyBlock ??= new MaterialPropertyBlock();
            cloudRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(NoiseScaleAId, tuning.noiseScaleA);
            propertyBlock.SetFloat(NoiseScaleBId, tuning.noiseScaleB);
            propertyBlock.SetVector(ScrollAId, tuning.scrollA);
            propertyBlock.SetVector(ScrollBId, tuning.scrollB);
            propertyBlock.SetFloat(NoiseAmountId, tuning.noiseAmount);
            propertyBlock.SetFloat(EdgeWarpId, tuning.edgeWarp);
            propertyBlock.SetFloat(CoreId, tuning.core);
            propertyBlock.SetFloat(FloorBiasId, tuning.floorBias);
            propertyBlock.SetFloat(TopDensityId, tuning.topDensity);
            propertyBlock.SetFloat(SeedId, ResolveSeed());
            cloudRenderer.SetPropertyBlock(propertyBlock);
        }

        private float ResolveSeed()
        {
            var id = zone != null ? zone.GasZoneId : null;
            // 구역마다 노이즈 위치가 달라 보이게 하는 값일 뿐이라 id가 없으면 위치로 정한다.
            var position = transform.position;
            var hash = string.IsNullOrEmpty(id)
                ? Mathf.RoundToInt(position.x * 13f + position.y * 31f)
                : id.GetHashCode();
            return (Mathf.Abs(hash % 997)) * 0.173f;
        }

        private void EnsureRenderer()
        {
            if (zone == null)
            {
                zone = GetComponent<GasZone>();
            }

            if (cloudRenderer != null)
            {
                EnsureCloudAppearance();
                return;
            }

            var cloud = transform.Find(GasVisualRules.GasCloudChildName);
            if (cloud == null)
            {
                var created = new GameObject(GasVisualRules.GasCloudChildName);
                created.transform.SetParent(transform, false);
                cloud = created.transform;
            }

            cloudRenderer = cloud.GetComponent<SpriteRenderer>();
            if (cloudRenderer == null)
            {
                cloudRenderer = cloud.gameObject.AddComponent<SpriteRenderer>();
            }

            EnsureCloudAppearance();

            var rootRenderer = GetComponent<SpriteRenderer>();
            if (rootRenderer != null && rootRenderer != cloudRenderer)
            {
                rootRenderer.enabled = false;
            }
        }

        // 예전 프리팹의 딱딱한 Knob 스프라이트가 남아 있어도 부드러운 스프라이트·공유 머티리얼로 바꾼다.
        private void EnsureCloudAppearance()
        {
            var sprite = cloudRenderer.sprite;
            if (sprite == null || sprite.name == "Knob")
            {
                cloudRenderer.sprite = ResolveSoftSprite();
                propertiesDirty = true;
            }

            var shared = ResolveCloudMaterial();
            if (shared != null && cloudRenderer.sharedMaterial != shared)
            {
                cloudRenderer.sharedMaterial = shared;
                propertiesDirty = true;
            }
        }

        private Sprite ResolveSoftSprite()
        {
            return softSprite != null ? softSprite : GasSoftSprites.Radial;
        }

        private static Material cachedRuntimeMaterial;

        // 모든 구역이 머티리얼 하나를 공유한다. 구역별 값은 MaterialPropertyBlock으로만 준다.
        private Material ResolveCloudMaterial()
        {
            if (cloudMaterial != null)
            {
                return cloudMaterial;
            }

            if (cachedRuntimeMaterial != null)
            {
                return cachedRuntimeMaterial;
            }

            var shader = Shader.Find(GasVisualRules.CloudShaderName);
            if (shader == null)
            {
                return null;
            }

            cachedRuntimeMaterial = new Material(shader)
            {
                name = "GasCloudRuntimeShared",
                hideFlags = HideFlags.HideAndDontSave
            };
            return cachedRuntimeMaterial;
        }

        public static void FitSpriteToWorldDiameter(SpriteRenderer renderer, float diameter)
        {
            if (renderer == null)
            {
                return;
            }

            var sprite = renderer.sprite;
            var bounds = sprite != null ? sprite.bounds.size : Vector3.one;
            var parent = renderer.transform.parent;
            var parentScale = parent != null ? parent.lossyScale : Vector3.one;
            var worldX = Mathf.Max(0.0001f, bounds.x * Mathf.Abs(parentScale.x));
            var worldY = Mathf.Max(0.0001f, bounds.y * Mathf.Abs(parentScale.y));
            var size = Mathf.Max(0f, diameter);
            renderer.transform.localScale = new Vector3(size / worldX, size / worldY, 1f);
        }
    }

    /// <summary>부드러운 방사형 스프라이트(런타임 대체용). 에셋이 있으면 에셋을 우선한다.</summary>
    public static class GasSoftSprites
    {
        private static Sprite cachedRadial;

        public static Sprite Radial
        {
            get
            {
                if (cachedRadial == null)
                {
                    cachedRadial = CreateRadial();
                }

                return cachedRadial;
            }
        }

        private static Sprite CreateRadial()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var r = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = 1f - GasVisualMath.SmoothStep(0.2f, 1f, r);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect);
            sprite.name = GasVisualRules.SoftSpriteName + "-runtime";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }

    /// <summary>가스 입자 렌더러 풀. 전역 상한을 넘겨 빌려주지 않는다.</summary>
    public static class GasMotePool
    {
        private static readonly Stack<SpriteRenderer> Idle = new();

        public static int ActiveCount { get; private set; }
        public static int Cap { get; set; } = GasCloudTuning.GlobalMoteCap;

        public static SpriteRenderer Rent(Transform parent, Sprite sprite)
        {
            if (ActiveCount >= Cap)
            {
                return null;
            }

            SpriteRenderer renderer = null;
            while (Idle.Count > 0 && renderer == null)
            {
                renderer = Idle.Pop();
            }

            if (renderer == null)
            {
                var go = new GameObject("GasMote");
                renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 21;
                renderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            }

            renderer.sprite = sprite;
            renderer.transform.SetParent(parent, false);
            renderer.gameObject.SetActive(true);
            ActiveCount++;
            return renderer;
        }

        public static void Return(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            // 부모(구역)가 파괴되면 입자도 함께 파괴되고, 풀에서는 null로 걸러진다.
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
            renderer.gameObject.SetActive(false);
            Idle.Push(renderer);
        }

        public static void ResetForTests()
        {
            while (Idle.Count > 0)
            {
                var renderer = Idle.Pop();
                if (renderer != null)
                {
                    Object.DestroyImmediate(renderer.gameObject);
                }
            }

            ActiveCount = 0;
            Cap = GasCloudTuning.GlobalMoteCap;
        }
    }
}
