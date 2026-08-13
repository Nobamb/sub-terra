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

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image overlayImage;
        [SerializeField] private Camera targetCamera;

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
                return GasVisionHoleEvaluator.Sample(viewportPosition, opacity);
            }

            var world = camera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, 0f));
            return GasVisionHoleEvaluator.Sample(world, opacity);
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

            runtimeMaterial.SetColor(DarkColorId, new Color(0.04f, 0.05f, 0.05f, opacity));
            runtimeMaterial.SetColor(
                LightColorId,
                new Color(1f, 0.12f, 0.08f, GasVisualRules.LightClearRedOpacity));

            var camera = ResolveCamera();
            var count = CopyViewportLights(camera, lightBuffer);
            runtimeMaterial.SetVectorArray(LightsId, lightBuffer);
            runtimeMaterial.SetFloat(LightCountId, count);
        }

        private int CopyViewportLights(Camera camera, Vector4[] destination)
        {
            if (destination == null)
            {
                return 0;
            }

            var count = 0;
            var sources = GasVisionClearanceSource.ActiveSources;
            if (sources.Count == 0)
            {
                sources = Object.FindObjectsByType<GasVisionClearanceSource>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            }

            var aspect = camera != null ? Mathf.Max(0.0001f, camera.aspect) : 1f;
            for (var i = 0; i < sources.Count && count < destination.Length; i++)
            {
                var source = sources[i];
                if (source == null || !source.isActiveAndEnabled)
                {
                    continue;
                }

                var world = source.WorldPosition;
                Vector2 viewport;
                float radiusY;
                if (camera != null)
                {
                    var center = camera.WorldToViewportPoint(new Vector3(world.x, world.y, 0f));
                    var edge = camera.WorldToViewportPoint(new Vector3(world.x, world.y + source.Radius, 0f));
                    viewport = center;
                    radiusY = Mathf.Abs(edge.y - center.y);
                }
                else
                {
                    viewport = world;
                    radiusY = source.Radius;
                }

                destination[count] = new Vector4(viewport.x, viewport.y, radiusY, aspect);
                count++;
            }

            for (var i = count; i < destination.Length; i++)
            {
                destination[i] = Vector4.zero;
            }

            return count;
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
