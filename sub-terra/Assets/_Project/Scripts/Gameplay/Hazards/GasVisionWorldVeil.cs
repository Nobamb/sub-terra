using UnityEngine;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>
    /// 유독가스 접근 시 카메라 전체를 덮는 월드 암전.
    /// 설치된 조명 5칸 안쪽은 셰이더가 부드럽게 뚫어 중심에 불투명도 5% 붉은 빛만 남긴다.
    /// 가스 안에서는 GasInteriorHaze만큼 베일이 가스 녹색으로 살짝 물든다.
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
        private static readonly int LightInnerId = Shader.PropertyToID("_LightInner");

        [SerializeField] private SpriteRenderer veilRenderer;
        [SerializeField] private float opacity;
        [SerializeField, Range(0f, 0.99f)] private float lightHoleInnerFraction = GasVisualRules.LightHoleInnerFraction;
        [SerializeField, Range(0f, 1f)] private float hazeTintStrength = 0.5f;
        [SerializeField, Range(0f, 0.3f)] private float hazeHoleExtraAlpha = 0.08f;

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
            return GasVisionHoleEvaluator.Sample(worldPosition, opacity, lightHoleInnerFraction);
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
                var haze = GasInteriorHaze.Amount;
                runtimeMaterial.SetColor(ColorId, GasVisualMath.VeilDark(opacity, haze, hazeTintStrength));
                runtimeMaterial.SetColor(
                    LightColorId,
                    GasVisualMath.LightHole(haze, hazeTintStrength, hazeHoleExtraAlpha));
                runtimeMaterial.SetFloat(LightInnerId, lightHoleInnerFraction);
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
