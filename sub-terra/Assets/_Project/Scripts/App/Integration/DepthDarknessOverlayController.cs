using SubTerra.App.State;
using SubTerra.Gameplay.Hazards;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 캐릭터 주변을 제외한 화면 암전(10m 50% → 30m 95%)과
    /// 어두운 영역 블록 명도(45% → 0%)·흰 테두리를 함께 표시한다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public sealed class DepthDarknessOverlayController : MonoBehaviour
    {
        public const string ShaderName = "SubTerra/DepthDarknessOverlayUI";
        public const string ShaderResourceName = "DepthDarknessOverlayUI";
        public const int StartDepth = DepthDarknessBlockVisual.StartDepth;
        public const int FullDepth = DepthDarknessBlockVisual.FullDepth;
        public const float StartLuminance = DepthDarknessBlockVisual.StartLuminance;
        public const float FullLuminance = DepthDarknessBlockVisual.FullLuminance;
        public const float StartOpacity = DepthDarknessBlockVisual.StartScreenOpacity;
        public const float FullOpacity = DepthDarknessBlockVisual.FullScreenOpacity;

        private static readonly int DarkColorId = Shader.PropertyToID("_DarkColor");
        private static readonly int PlayerViewportId = Shader.PropertyToID("_PlayerViewport");
        private static readonly int PlayerRadiusId = Shader.PropertyToID("_PlayerRadius");
        private static readonly int FeatherId = Shader.PropertyToID("_Feather");
        private static readonly int OccupancyTexId = Shader.PropertyToID("_OccupancyTex");
        private static readonly int OccWorldMinId = Shader.PropertyToID("_OccWorldMin");
        private static readonly int WorldMinId = Shader.PropertyToID("_WorldMin");
        private static readonly int WorldMaxId = Shader.PropertyToID("_WorldMax");
        private static readonly int CellSizeId = Shader.PropertyToID("_CellSize");
        private static readonly int OccTexSizeId = Shader.PropertyToID("_OccTexSize");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int BlockDarkAlphaId = Shader.PropertyToID("_BlockDarkAlpha");

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image overlayImage;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Tilemap terrainTilemap;
        [SerializeField, Range(0.01f, 0.5f)] private float playerVisibleRadius = 0.11f;
        [SerializeField, Range(0.001f, 0.2f)] private float edgeFeather = 0.04f;

        private GameState gameState;
        private Material runtimeMaterial;
        private Texture2D occupancyTexture;
        private Color32[] occupancyPixels;
        private int currentDepth;

        public float CurrentOpacity { get; private set; }
        public float CurrentOccupiedDarkAlpha { get; private set; }
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

            if (occupancyTexture != null)
            {
                Destroy(occupancyTexture);
                occupancyTexture = null;
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

        public void SetTerrainTilemap(Tilemap tilemap)
        {
            terrainTilemap = tilemap;
            ApplyVisual();
        }

        public static float EvaluateLuminance(int depth, bool isInsideLight)
        {
            return DepthDarknessBlockVisual.EvaluateLuminance(depth, isInsideLight);
        }

        public static float EvaluateOpacity(int depth, bool isInsideLight)
        {
            return DepthDarknessBlockVisual.EvaluateOpacity(depth, isInsideLight);
        }

        public static float EvaluateOccupiedDarkAlpha(int depth, bool isInsideLight)
        {
            return DepthDarknessBlockVisual.EvaluateOccupiedDarkAlpha(depth, isInsideLight);
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
            if (gameState != null)
            {
                currentDepth = gameState.Run.Depth;
            }

            IsClearedByLight = playerTransform != null
                && GasVisionClearanceSource.IsCleared(playerTransform.position);
            CurrentOpacity = EvaluateOpacity(currentDepth, IsClearedByLight);
            CurrentOccupiedDarkAlpha = EvaluateOccupiedDarkAlpha(currentDepth, IsClearedByLight);

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
            runtimeMaterial.SetFloat(BlockDarkAlphaId, CurrentOccupiedDarkAlpha);
            runtimeMaterial.SetFloat(PlayerRadiusId, playerVisibleRadius);
            runtimeMaterial.SetFloat(FeatherId, edgeFeather);
            runtimeMaterial.SetFloat(OutlineWidthId, DepthDarknessBlockVisual.OutlineWidthCells);
            runtimeMaterial.SetColor(OutlineColorId, Color.white);

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

            ApplyOccupancy(camera);
        }

        private void ApplyOccupancy(Camera camera)
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            if (terrainTilemap == null || camera == null || CurrentOpacity <= 0.001f)
            {
                runtimeMaterial.SetVector(OccTexSizeId, Vector4.zero);
                return;
            }

            var dist = Mathf.Abs(camera.transform.position.z);
            var worldBl = camera.ViewportToWorldPoint(new Vector3(0f, 0f, dist));
            var worldTr = camera.ViewportToWorldPoint(new Vector3(1f, 1f, dist));
            var worldMin = new Vector3(
                Mathf.Min(worldBl.x, worldTr.x),
                Mathf.Min(worldBl.y, worldTr.y),
                0f);
            var worldMax = new Vector3(
                Mathf.Max(worldBl.x, worldTr.x),
                Mathf.Max(worldBl.y, worldTr.y),
                0f);

            var cellSize = terrainTilemap.cellSize;
            if (cellSize.x < 0.0001f)
            {
                cellSize.x = 1f;
            }

            if (cellSize.y < 0.0001f)
            {
                cellSize.y = 1f;
            }

            var z = terrainTilemap.origin.z;
            var minCell = terrainTilemap.WorldToCell(worldMin) - new Vector3Int(1, 1, 0);
            var maxCell = terrainTilemap.WorldToCell(worldMax) + new Vector3Int(1, 1, 0);
            minCell.z = z;
            maxCell.z = z;

            var width = Mathf.Clamp(maxCell.x - minCell.x + 1, 1, 192);
            var height = Mathf.Clamp(maxCell.y - minCell.y + 1, 1, 192);
            EnsureOccupancyTexture(width, height);

            var bounds = new BoundsInt(minCell.x, minCell.y, z, width, height, 1);
            var tiles = terrainTilemap.GetTilesBlock(bounds);
            var count = width * height;
            for (var i = 0; i < count; i++)
            {
                var occupied = tiles != null && i < tiles.Length && tiles[i] != null;
                occupancyPixels[i] = occupied
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(0, 0, 0, 0);
            }

            occupancyTexture.SetPixels32(occupancyPixels);
            occupancyTexture.Apply(false, false);

            var occWorldMin = terrainTilemap.GetCellCenterWorld(minCell)
                - new Vector3(cellSize.x * 0.5f, cellSize.y * 0.5f, 0f);

            runtimeMaterial.SetTexture(OccupancyTexId, occupancyTexture);
            runtimeMaterial.SetVector(OccWorldMinId, occWorldMin);
            runtimeMaterial.SetVector(WorldMinId, worldMin);
            runtimeMaterial.SetVector(WorldMaxId, worldMax);
            runtimeMaterial.SetVector(CellSizeId, new Vector4(cellSize.x, cellSize.y, 0f, 0f));
            runtimeMaterial.SetVector(OccTexSizeId, new Vector4(width, height, 0f, 0f));
        }

        private void EnsureOccupancyTexture(int width, int height)
        {
            if (occupancyTexture != null
                && occupancyTexture.width == width
                && occupancyTexture.height == height)
            {
                if (occupancyPixels == null || occupancyPixels.Length != width * height)
                {
                    occupancyPixels = new Color32[width * height];
                }

                return;
            }

            if (occupancyTexture != null)
            {
                Destroy(occupancyTexture);
            }

            occupancyTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "DepthDarknessOccupancy",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            occupancyPixels = new Color32[width * height];
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
