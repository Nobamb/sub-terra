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
        // ★ B-187: 가로형 새 그림에 맞춰 흰색에 가깝게 올린 배경 tint. 전경보다 한 단계 어둡게 남긴다.
        public static readonly Color DefaultTint = new Color(.92f, .95f, 1f, 1f);
        public Color tint = DefaultTint;
        [Min(1f)] public float horizontalTravelWorld = 80f; // ★ 광산 폭(-40~40)과 같게: 좌우 끝에서 그림 좌우 끝까지 보임
        [Min(1f)] public float depthTravelWorld = 60f; // ★ 가로형 기준, 40m 이후까지 세로 이동이 보이도록
        [Header("깊이별 어두워짐 ★ (배경 전용, 전경·암전과 별개)")]
        [Min(0f)] public float depthDarkenStart = 2f;
        [Min(1f)] public float depthDarkenFullDepth = 40f;
        [Range(.05f, 1f)] public float deepBrightness = .35f;

        /// <summary>깊이(m)에 따른 배경 밝기 배율. 시작 깊이까지 1, 최대 깊이 이후 deepBrightness.</summary>
        public float DepthBrightness(float depth)
        {
            float end = Mathf.Max(depthDarkenStart + .1f, depthDarkenFullDepth);
            return Mathf.Lerp(1f, Mathf.Clamp(deepBrightness, .05f, 1f), Mathf.InverseLerp(depthDarkenStart, end, depth));
        }

        [Header("광물 반짝임 ★ (배경 장식 전용)")]
        [Range(0f, .2f)] public float lithiumGlowBase = .08f;
        [Range(0f, .6f)] public float lithiumPulseBoost = .2f;
        [Range(0f, .6f)] public float copperPulseStrength = .45f;
        [Range(0f, .6f)] public float ironPulseStrength = .4f;
        // 광물 픽셀의 상시 숨쉬기 빛. 리튬이 가장 강하다.
        [Range(0f, 1f)] public float lithiumShimmer = .35f;
        [Range(0f, 1f)] public float copperShimmer = .18f;
        [Range(0f, 1f)] public float ironShimmer = .18f;
        [Min(.1f)] public float pulseDurationMin = .35f;
        [Min(.1f)] public float pulseDurationMax = .6f;
        [Min(.5f)] public float pulseIntervalMin = 2.5f;
        [Min(.5f)] public float pulseIntervalMax = 5f;
        // 모션 감소: 고정 약한 glow 상한. 반짝·맥동은 멈춘다.
        [Range(0f, .08f)] public float reducedMotionLithiumGlow = .08f;
        public Vector2 surfaceOrigin = Vector2.zero;
        [Range(.05f, .8f)] public float visibleImageHeight = .65f; // ★ 그림 높이 중 화면에 보이는 비율
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
