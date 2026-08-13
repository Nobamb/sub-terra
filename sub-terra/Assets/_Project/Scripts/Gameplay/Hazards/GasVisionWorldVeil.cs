using UnityEngine;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>
    /// 유독가스 접근 시 카메라 전체를 덮는 월드 암전.
    /// 설치된 조명 중심 5칸은 셰이더가 뚫어 불투명도 5% 붉은 빛만 남긴다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class GasVisionWorldVeil : MonoBehaviour
    {
        public const string ObjectName = "GasWorldVeil";
        public const string ShaderName = "SubTerra/GasVisionVeil";
        public const int SortingOrder = 80;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int LightColorId = Shader.PropertyToID("_LightColor");
        private static readonly int LightsId = Shader.PropertyToID("_GasLights");
        private static readonly int LightCountId = Shader.PropertyToID("_GasLightCount");

        [SerializeField] private SpriteRenderer veilRenderer;
        [SerializeField] private float opacity;

        private Material runtimeMaterial;
        private readonly Vector4[] lightBuffer = new Vector4[GasVisionHoleEvaluator.MaxLights];

        public float Opacity => opacity;

        private void Awake()
        {
            DetachFromCamera();
            EnsureRenderer();
            ApplyVisual();
        }

        private void LateUpdate()
        {
            DetachFromCamera();
            FollowCamera();
            ApplyVisual();
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
                runtimeMaterial = null;
            }
        }

        public void SetOpacity(float value)
        {
            opacity = Mathf.Clamp01(value);
            ApplyVisual();
        }

        public Color SampleAt(Vector2 worldPosition)
        {
            return GasVisionHoleEvaluator.Sample(worldPosition, opacity);
        }

        private void DetachFromCamera()
        {
            if (transform.parent == null)
            {
                return;
            }

            var parentCamera = transform.parent.GetComponent<Camera>();
            if (parentCamera != null)
            {
                transform.SetParent(null, true);
            }
        }

        private void FollowCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            var position = cam.transform.position;
            transform.position = new Vector3(position.x, position.y, 0f);
            transform.rotation = Quaternion.identity;

            var height = cam.orthographic
                ? cam.orthographicSize * 2.6f
                : 40f;
            var width = height * Mathf.Max(1f, cam.aspect);
            FitSpriteToWorldSize(veilRenderer, width, height);
        }

        private void ApplyVisual()
        {
            EnsureRenderer();
            if (veilRenderer == null)
            {
                return;
            }

            var lightCount = GasVisionHoleEvaluator.CopyActiveLights(lightBuffer);
            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetColor(ColorId, new Color(0.04f, 0.05f, 0.05f, opacity));
                runtimeMaterial.SetColor(
                    LightColorId,
                    new Color(1f, 0.12f, 0.08f, GasVisualRules.LightClearRedOpacity));
                runtimeMaterial.SetVectorArray(LightsId, lightBuffer);
                runtimeMaterial.SetFloat(LightCountId, lightCount);
            }

            veilRenderer.color = Color.white;
            veilRenderer.sortingOrder = SortingOrder;
            veilRenderer.maskInteraction = SpriteMaskInteraction.None;
            veilRenderer.enabled = opacity > 0.001f;
        }

        private void EnsureRenderer()
        {
            if (veilRenderer == null)
            {
                veilRenderer = GetComponent<SpriteRenderer>();
            }

            if (veilRenderer.sprite == null)
            {
                veilRenderer.sprite = CreateSquareSprite();
            }

            if (runtimeMaterial == null)
            {
                var shader = Shader.Find(ShaderName);
                if (shader != null)
                {
                    runtimeMaterial = new Material(shader)
                    {
                        name = "GasVisionVeilRuntime",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    veilRenderer.sharedMaterial = runtimeMaterial;
                }
            }
        }

        private static void FitSpriteToWorldSize(SpriteRenderer renderer, float worldWidth, float worldHeight)
        {
            if (renderer == null)
            {
                return;
            }

            var sprite = renderer.sprite;
            var bounds = sprite != null ? sprite.bounds.size : Vector3.one;
            renderer.transform.localScale = new Vector3(
                Mathf.Max(0f, worldWidth) / Mathf.Max(0.0001f, bounds.x),
                Mathf.Max(0f, worldHeight) / Mathf.Max(0.0001f, bounds.y),
                1f);
        }

        private static Sprite cachedSquare;

        private static Sprite CreateSquareSprite()
        {
            if (cachedSquare != null)
            {
                return cachedSquare;
            }

            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };
            for (var y = 0; y < 4; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }

            texture.Apply(false, true);
            cachedSquare = Sprite.Create(
                texture,
                new Rect(0f, 0f, 4f, 4f),
                new Vector2(0.5f, 0.5f),
                4f);
            cachedSquare.hideFlags = HideFlags.HideAndDontSave;
            return cachedSquare;
        }
    }
}
