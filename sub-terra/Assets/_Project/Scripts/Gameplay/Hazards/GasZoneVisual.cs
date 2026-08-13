using UnityEngine;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>
    /// 가스 생성 이펙트. 중심에서 1초에 걸쳐 5칸 반경·불투명도 70%로 나타난다.
    /// 화면 탁화(접근 이펙트)와는 별개다.
    /// </summary>
    [RequireComponent(typeof(GasZone))]
    public sealed class GasZoneVisual : MonoBehaviour
    {
        private static readonly Color GasColor = new(0.22f, 0.82f, 0.34f, 1f);

        [SerializeField] private SpriteRenderer cloudRenderer;

        private GasZone zone;

        private void Awake()
        {
            zone = GetComponent<GasZone>();
            EnsureRenderer();
            ApplyVisual();
        }

        private void LateUpdate()
        {
            ApplyVisual();
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
            var color = GasColor;
            color.a = GasVisualRules.GasVisualOpacity * progress;
            cloudRenderer.color = color;
            cloudRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            cloudRenderer.sortingOrder = 20;
            cloudRenderer.enabled = zone.IsActive && progress > 0.001f;
        }

        private void EnsureRenderer()
        {
            if (zone == null)
            {
                zone = GetComponent<GasZone>();
            }

            if (cloudRenderer != null)
            {
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

            if (cloudRenderer.sprite == null)
            {
                cloudRenderer.sprite = ResolveCloudSprite();
            }

            var rootRenderer = GetComponent<SpriteRenderer>();
            if (rootRenderer != null && rootRenderer != cloudRenderer)
            {
                rootRenderer.enabled = false;
            }
        }

        private static Sprite cachedCircleSprite;

        private static Sprite ResolveCloudSprite()
        {
            if (cachedCircleSprite != null)
            {
                return cachedCircleSprite;
            }

#if UNITY_EDITOR
            cachedCircleSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (cachedCircleSprite != null)
            {
                return cachedCircleSprite;
            }
#endif
            cachedCircleSprite = CreateFallbackCircleSprite();
            return cachedCircleSprite;
        }

        private static Sprite CreateFallbackCircleSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var center = (size - 1) * 0.5f;
            var radius = size * 0.5f - 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
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
}
