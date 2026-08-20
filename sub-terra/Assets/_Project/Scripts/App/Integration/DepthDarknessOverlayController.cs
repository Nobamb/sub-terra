using SubTerra.App.State;
using SubTerra.Gameplay.Hazards;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>깊이에 따른 광산 암부를 표시하고 플레이어가 조명 범위에 들어오면 해제한다.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public sealed class DepthDarknessOverlayController : MonoBehaviour
    {
        public const string ShaderName = "SubTerra/DepthDarknessOverlayUI";
        public const string ShaderResourceName = "DepthDarknessOverlayUI";
        public const int StartDepth = 10;
        public const int FullDepth = 30;
        public const float StartOpacity = 0.5f;
        public const float FullOpacity = 0.95f;

        private static readonly int DarkColorId = Shader.PropertyToID("_DarkColor");
        private static readonly int PlayerViewportId = Shader.PropertyToID("_PlayerViewport");
        private static readonly int PlayerRadiusId = Shader.PropertyToID("_PlayerRadius");
        private static readonly int FeatherId = Shader.PropertyToID("_Feather");

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image overlayImage;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Camera targetCamera;
        [SerializeField, Range(0.01f, 0.5f)] private float playerVisibleRadius = 0.11f;
        [SerializeField, Range(0.001f, 0.2f)] private float edgeFeather = 0.04f;

        private GameState gameState;
        private Material runtimeMaterial;
        private int currentDepth;

        public float CurrentOpacity { get; private set; }
        public bool IsClearedByLight { get; private set; }

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
            Unbind();
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
                runtimeMaterial = null;
            }
        }

        public void Bind(GameState state, Transform player)
        {
            Unbind();
            gameState = state;
            playerTransform = player;
            if (gameState != null)
            {
                currentDepth = gameState.Run.Depth;
                gameState.DepthChanged += OnDepthChanged;
            }

            ApplyVisual();
        }

        public void SetPlayer(Transform player)
        {
            playerTransform = player;
            ApplyVisual();
        }

        public static float EvaluateOpacity(int depth, bool isInsideLight)
        {
            if (isInsideLight || depth < StartDepth)
            {
                return 0f;
            }

            var progress = Mathf.InverseLerp(StartDepth, FullDepth, depth);
            return Mathf.Lerp(StartOpacity, FullOpacity, progress);
        }

        private void Unbind()
        {
            if (gameState != null)
            {
                gameState.DepthChanged -= OnDepthChanged;
                gameState = null;
            }
        }

        private void OnDepthChanged(int depth)
        {
            currentDepth = depth;
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            EnsureBindings();
            IsClearedByLight = playerTransform != null
                && GasVisionClearanceSource.IsCleared(playerTransform.position);
            CurrentOpacity = EvaluateOpacity(currentDepth, IsClearedByLight);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = CurrentOpacity > 0.001f ? 1f : 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (runtimeMaterial == null)
            {
                return;
            }

            runtimeMaterial.SetColor(DarkColorId, new Color(0f, 0f, 0f, CurrentOpacity));
            runtimeMaterial.SetFloat(PlayerRadiusId, playerVisibleRadius);
            runtimeMaterial.SetFloat(FeatherId, edgeFeather);

            var camera = ResolveCamera();
            var viewport = new Vector2(0.5f, 0.5f);
            var aspect = 1f;
            if (camera != null)
            {
                aspect = Mathf.Max(0.0001f, camera.aspect);
                if (playerTransform != null)
                {
                    viewport = camera.WorldToViewportPoint(playerTransform.position);
                }
            }

            runtimeMaterial.SetVector(
                PlayerViewportId,
                new Vector4(viewport.x, viewport.y, aspect, 0f));
        }

        private Camera ResolveCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

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
                var shader = Resources.Load<Shader>(ShaderResourceName);
                if (shader == null)
                {
                    shader = Shader.Find(ShaderName);
                }
                if (shader != null)
                {
                    runtimeMaterial = new Material(shader)
                    {
                        name = "DepthDarknessOverlayRuntime",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }
            }

            if (overlayImage != null)
            {
                overlayImage.raycastTarget = false;
                overlayImage.color = Color.white;
                if (runtimeMaterial != null && overlayImage.material != runtimeMaterial)
                {
                    overlayImage.material = runtimeMaterial;
                }
            }
        }
    }
}
