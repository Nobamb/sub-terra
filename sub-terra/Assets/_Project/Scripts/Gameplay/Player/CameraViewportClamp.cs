using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    public readonly struct CameraClampLimits
    {
        public CameraClampLimits(float minX, float maxX, float minY, float maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }

        public float MinX { get; }
        public float MaxX { get; }
        public float MinY { get; }
        public float MaxY { get; }

        public Vector3 Clamp(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, MinX, MaxX);
            position.y = Mathf.Clamp(position.y, MinY, MaxY);
            return position;
        }
    }

    /// <summary>해상도와 무관하게 viewport 전체가 월드 경계 안에 머물도록 계산합니다.</summary>
    public static class CameraViewportClamp
    {
        public static CameraClampLimits Calculate(
            Bounds worldBounds,
            float orthographicSize,
            float aspect)
        {
            float halfHeight = Mathf.Max(0f, orthographicSize);
            float halfWidth = halfHeight * Mathf.Max(0f, aspect);

            CalculateAxis(
                worldBounds.min.x,
                worldBounds.max.x,
                halfWidth,
                out float minX,
                out float maxX);
            CalculateAxis(
                worldBounds.min.y,
                worldBounds.max.y,
                halfHeight,
                out float minY,
                out float maxY);

            return new CameraClampLimits(minX, maxX, minY, maxY);
        }

        private static void CalculateAxis(
            float worldMin,
            float worldMax,
            float viewportHalfSize,
            out float cameraMin,
            out float cameraMax)
        {
            if (worldMax - worldMin <= viewportHalfSize * 2f)
            {
                cameraMin = cameraMax = (worldMin + worldMax) * 0.5f;
                return;
            }

            cameraMin = worldMin + viewportHalfSize;
            cameraMax = worldMax - viewportHalfSize;
        }
    }
}
