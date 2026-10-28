using UnityEngine;

namespace SubTerra.App.UI
{
    [CreateAssetMenu(menuName = "SubTerra/Visuals/Mine Backdrop")]
    public sealed class MineBackdropSettings : ScriptableObject
    {
        public const string ResourcePath = "MineBackdropSettings";
        public Texture2D image;
        public Texture2D ironImage;
        public Texture2D lithiumImage;
        [Min(1f)] public float ironDepth = 16f;
        [Min(1f)] public float lithiumDepth = 36f;
        public float mineralSurfaceY = -1f;
        [Min(.1f)] public float depthBlendDistance = 2f;
        [Min(0f)] public float themeSmoothingSeconds = .8f;

        public Vector3 LayerWeights(float depth)
        {
            float iron = ironImage != null ? Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(ironDepth, ironDepth + depthBlendDistance, depth)) : 0f;
            float lithium = lithiumImage != null ? Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(lithiumDepth, lithiumDepth + depthBlendDistance, depth)) : 0f;
            return new Vector3((1f - iron) * (1f - lithium), iron * (1f - lithium), lithium);
        }
        public Color tint = new Color(.65f, .7f, .75f, 1f);
        [Min(1f)] public float horizontalTravelWorld = 160f;
        [Min(1f)] public float depthTravelWorld = 160f;
        public Vector2 surfaceOrigin = Vector2.zero;
        [Range(.05f, .8f)] public float visibleImageHeight = .22f;
        [Range(.01f, .2f)] public float edgePadding = .05f;
        [Min(0f)] public float smoothingSeconds = .25f;

        public Rect Viewport(Vector2 focus, float cameraAspect)
        {
            float padding = Mathf.Clamp(edgePadding, .01f, .2f);
            float imageAspect = image != null ? image.width / (float)image.height : 1f;
            float height = Mathf.Clamp(visibleImageHeight, .01f, 1f - 2f * padding);
            float width = height * Mathf.Max(.01f, cameraAspect) / imageAspect;
            if (width > 1f - 2f * padding)
            {
                width = 1f - 2f * padding;
                height = width * imageAspect / Mathf.Max(.01f, cameraAspect);
            }
            float x = Mathf.Clamp01(.5f + (focus.x - surfaceOrigin.x) / Mathf.Max(1f, horizontalTravelWorld));
            float depth = Mathf.Clamp01((surfaceOrigin.y - focus.y) / Mathf.Max(1f, depthTravelWorld));
            return new Rect(
                Mathf.Lerp(padding, 1f - padding - width, x),
                Mathf.Lerp(1f - padding - height, padding, depth), width, height);
        }
    }
}
