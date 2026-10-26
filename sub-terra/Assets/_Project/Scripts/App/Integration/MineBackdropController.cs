using SubTerra.App.UI;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>광산 카메라 뒤의 단일 배경. 지형·광석·암전 판정에는 관여하지 않는다.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class MineBackdropController : MonoBehaviour
    {
        private static readonly int UvRectId = Shader.PropertyToID("_UvRect");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private MineBackdropSettings settings;
        private Camera targetCamera;
        private Transform focus;
        private Mesh mesh;
        private Material material;
        private MeshRenderer meshRenderer;
        private Vector2 center;
        private bool centered;
        public Rect CurrentViewport { get; private set; }

        public static MineBackdropController AttachTo(Transform owner, Camera camera, Transform target, Renderer foreground)
        {
            if (owner == null || camera == null || target == null || !camera.orthographic) return null;
            var config = Resources.Load<MineBackdropSettings>(MineBackdropSettings.ResourcePath);
            if (config == null || config.image == null) return null;
            var existing = owner.GetComponentInChildren<MineBackdropController>(true);
            if (existing != null) return existing;
            var go = new GameObject("MineBackdrop") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(owner, false);
            var backdrop = go.AddComponent<MineBackdropController>();
            backdrop.Configure(config, camera, target, foreground);
            return backdrop;
        }

        public void Configure(MineBackdropSettings config, Camera camera, Transform target, Renderer foreground = null)
        {
            if (config == null || config.image == null || camera == null || target == null || !camera.orthographic)
                throw new System.ArgumentException("Mine backdrop requires an image, orthographic camera and focus.");
            settings = config;
            targetCamera = camera;
            focus = target;
            centered = false;
            if (meshRenderer == null)
            {
                var shader = Resources.Load<Shader>("MineBackdrop");
                if (shader == null) throw new System.InvalidOperationException("Mine backdrop shader missing.");
                material = new Material(shader) { hideFlags = HideFlags.DontSave };
                mesh = new Mesh { name = "Mine backdrop quad", hideFlags = HideFlags.DontSave };
                mesh.vertices = new[] { new Vector3(-.5f, -.5f), new Vector3(.5f, -.5f), new Vector3(.5f, .5f), new Vector3(-.5f, .5f) };
                mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
                mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                mesh.RecalculateBounds();
                gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = material;
                meshRenderer.sortingOrder = -32000;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }
            if (foreground != null)
            {
                meshRenderer.sortingLayerID = foreground.sortingLayerID;
                gameObject.layer = foreground.gameObject.layer;
            }
            material.mainTexture = settings.image;
            material.SetColor(TintId, settings.tint);
            Refresh(0f);
        }

        private void LateUpdate() => Refresh(Time.unscaledDeltaTime);

        public void Refresh(float deltaTime)
        {
            if (settings == null || targetCamera == null || focus == null || meshRenderer == null) return;
            var desired = settings.Viewport(AccessibilityPreferences.ReduceMotion ? settings.surfaceOrigin : (Vector2)focus.position, targetCamera.aspect);
            float blend = AccessibilityPreferences.ReduceMotion || settings.smoothingSeconds <= 0f
                ? 1f : 1f - Mathf.Exp(-Mathf.Max(0f, deltaTime) / settings.smoothingSeconds);
            center = centered ? Vector2.Lerp(center, desired.center, blend) : desired.center;
            centered = true;
            float padding = Mathf.Clamp(settings.edgePadding, .01f, .2f);
            center.x = Mathf.Clamp(center.x, padding + desired.width * .5f, 1f - padding - desired.width * .5f);
            center.y = Mathf.Clamp(center.y, padding + desired.height * .5f, 1f - padding - desired.height * .5f);
            CurrentViewport = new Rect(center - desired.size * .5f, desired.size);
            material.SetVector(UvRectId, new Vector4(CurrentViewport.x, CurrentViewport.y, CurrentViewport.width, CurrentViewport.height));
            float distance = Mathf.Clamp(50f, targetCamera.nearClipPlane + .01f, targetCamera.farClipPlane - .01f);
            transform.SetPositionAndRotation(targetCamera.transform.position + targetCamera.transform.forward * distance, targetCamera.transform.rotation);
            float height = 2f * targetCamera.orthographicSize;
            transform.localScale = new Vector3(height * targetCamera.aspect, height, 1f);
        }

        private void OnDisable() { if (meshRenderer != null) meshRenderer.enabled = false; }
        private void OnEnable() { if (meshRenderer != null) meshRenderer.enabled = true; }
        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (mesh != null) Destroy(mesh);
        }
    }
}
