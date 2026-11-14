using SubTerra.Gameplay.Hazards;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 화면 전체 가스 암전 UI에 조명 5칸 구멍을 넣는다.
    /// Canvas Overlay는 2D Renderer SpriteMask를 쓰지 않으므로 여기서 구멍을 계산한다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public sealed class GasVisionOverlayDriver : MonoBehaviour
    {
        public const string ShaderName = "SubTerra/GasVisionOverlayUI";

        private static readonly int DarkColorId = Shader.PropertyToID("_DarkColor");
        private static readonly int LightColorId = Shader.PropertyToID("_LightColor");
        private static readonly int LightsId = Shader.PropertyToID("_GasLights");
        private static readonly int LightCountId = Shader.PropertyToID("_GasLightCount");
        private static readonly int LightInnerId = Shader.PropertyToID("_LightInner");

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image overlayImage;
        [SerializeField] private Camera targetCamera;
        [SerializeField, Range(0f, 0.99f)] private float lightHoleInnerFraction = GasVisualRules.LightHoleInnerFraction;
        [SerializeField, Range(0f, 1f)] private float hazeTintStrength = 0.5f;
        [SerializeField, Range(0.05f, 0.6f)] private float lightHoleCenterAlpha = GasVisualRules.LightHoleCenterOpacity;
        [SerializeField, Range(0f, 0.3f)] private float hazeHoleExtraAlpha = 0.08f;

        private Material runtimeMaterial;
        private readonly Vector4[] lightBuffer = new Vector4[GasVisionHoleEvaluator.MaxLights];
        private float opacity;

        public float Opacity => opacity;

        private void Awake()
        {
            EnsureBindings();
            ApplyVisual();
        }

        private void LateUpdate()
        {
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

        public Color SampleViewport(Vector2 viewportPosition)
        {
            var camera = ResolveCamera();
            if (camera == null)
            {
                return GasVisionHoleEvaluator.Sample(viewportPosition, opacity, lightHoleInnerFraction, lightHoleCenterAlpha);
            }

            var world = camera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, 0f));
            return GasVisionHoleEvaluator.Sample(world, opacity, lightHoleInnerFraction, lightHoleCenterAlpha);
        }

        private void ApplyVisual()
        {
            EnsureBindings();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = opacity > 0.001f ? 1f : 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (runtimeMaterial == null)
            {
                return;
            }

            var haze = GasInteriorHaze.Amount;
            runtimeMaterial.SetColor(DarkColorId, GasVisualMath.VeilDark(opacity, haze, hazeTintStrength));
            runtimeMaterial.SetColor(
                LightColorId,
                GasVisualMath.LightHole(haze, hazeTintStrength, hazeHoleExtraAlpha, lightHoleCenterAlpha));
            runtimeMaterial.SetFloat(LightInnerId, lightHoleInnerFraction);

            var camera = ResolveCamera();
            var count = GasVisionHoleEvaluator.CopyViewportLights(camera, lightBuffer);
            runtimeMaterial.SetVectorArray(LightsId, lightBuffer);
            runtimeMaterial.SetFloat(LightCountId, count);
        }

        private Camera ResolveCamera()
        {
            if (targetCamera != null)
            {
                return targetCamera;
            }

            targetCamera = Camera.main;
            return targetCamera;
        }

        private void EnsureBindings()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (overlayImage == null)
            {
                overlayImage = GetComponent<Image>();
            }

            if (runtimeMaterial == null)
            {
                var shader = Shader.Find(ShaderName);
                if (shader == null)
                {
                    return;
                }

                runtimeMaterial = new Material(shader)
                {
                    name = "GasVisionOverlayRuntime",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            if (overlayImage != null && overlayImage.material != runtimeMaterial)
            {
                overlayImage.material = runtimeMaterial;
                overlayImage.color = Color.white;
            }
        }
    }
}
