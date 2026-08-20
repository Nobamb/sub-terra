using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 깊이 암부 규칙. 화면 암전(10m 50% → 30m 95%)과
    /// 점유 블록 명도(10m 45% → 30m 0%)·흰 테두리를 따로 둔다.
    /// </summary>
    public static class DepthDarknessBlockVisual
    {
        public const int StartDepth = 10;
        public const int FullDepth = 30;
        public const float StartScreenOpacity = 0.5f;
        public const float FullScreenOpacity = 0.95f;
        public const float StartLuminance = 0.45f;
        public const float FullLuminance = 0f;
        public const float OutlineWidthCells = 0.07f;
        public const string ForegroundTilemapName = "ForegroundTilemap";

        public static float EvaluateLuminance(int depth, bool isInsideLight)
        {
            if (isInsideLight || depth < StartDepth)
            {
                return 1f;
            }

            var progress = Mathf.InverseLerp(StartDepth, FullDepth, depth);
            return Mathf.Lerp(StartLuminance, FullLuminance, progress);
        }

        public static float EvaluateOpacity(int depth, bool isInsideLight)
        {
            if (isInsideLight || depth < StartDepth)
            {
                return 0f;
            }

            var progress = Mathf.InverseLerp(StartDepth, FullDepth, depth);
            return Mathf.Lerp(StartScreenOpacity, FullScreenOpacity, progress);
        }

        public static float EvaluateOccupiedDarkAlpha(int depth, bool isInsideLight)
        {
            if (isInsideLight || depth < StartDepth)
            {
                return 0f;
            }

            return 1f - EvaluateLuminance(depth, false);
        }

        /// <summary>
        /// 화면 암전이 테두리 위에 덮이므로, 테두리 밝기는 (1 - 화면 불투명도)다.
        /// 10m에서 50%, 30m에서 5%.
        /// </summary>
        public static float EvaluateOutlineBrightness(int depth, bool isInsideLight)
        {
            return 1f - EvaluateOpacity(depth, isInsideLight);
        }

        public static bool IsCellOutline(Vector2 cellUv, float outlineWidth)
        {
            var edge = Mathf.Min(
                Mathf.Min(cellUv.x, 1f - cellUv.x),
                Mathf.Min(cellUv.y, 1f - cellUv.y));
            return edge <= outlineWidth;
        }

        public static bool ShouldDrawOutline(
            bool occupied,
            Vector2 cellUv,
            bool inDarkRegion,
            float outlineWidth = OutlineWidthCells)
        {
            return occupied && inDarkRegion && IsCellOutline(cellUv, outlineWidth);
        }
    }
}
