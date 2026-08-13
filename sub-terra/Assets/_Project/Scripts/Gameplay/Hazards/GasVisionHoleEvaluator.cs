using UnityEngine;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>조명 5칸 구멍이 베일 픽셀을 얼마나 걷는지 계산한다.</summary>
    public static class GasVisionHoleEvaluator
    {
        public const int MaxLights = 32;

        public static bool TryGetNearestLight(
            Vector2 worldPosition,
            out Vector2 lightPosition,
            out float radius)
        {
            lightPosition = default;
            radius = 0f;
            var best = float.MaxValue;
            var sources = GasVisionClearanceSource.ResolveActive();
            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (source == null || !source.isActiveAndEnabled)
                {
                    continue;
                }

                var distance = Vector2.Distance(worldPosition, source.WorldPosition);
                if (distance > source.Radius || distance >= best)
                {
                    continue;
                }

                best = distance;
                lightPosition = source.WorldPosition;
                radius = source.Radius;
            }

            return radius > 0f;
        }

        public static Color Sample(Vector2 worldPosition, float darkOpacity)
        {
            if (TryGetNearestLight(worldPosition, out _, out _))
            {
                return new Color(1f, 0.12f, 0.08f, GasVisualRules.LightClearRedOpacity);
            }

            return new Color(0.04f, 0.05f, 0.05f, Mathf.Clamp01(darkOpacity));
        }

        public static int CopyActiveLights(Vector4[] destination)
        {
            if (destination == null)
            {
                return 0;
            }

            var count = 0;
            var sources = GasVisionClearanceSource.ResolveActive();
            for (var i = 0; i < sources.Count && count < destination.Length; i++)
            {
                var source = sources[i];
                if (source == null || !source.isActiveAndEnabled)
                {
                    continue;
                }

                var position = source.WorldPosition;
                destination[count] = new Vector4(position.x, position.y, 0f, source.Radius);
                count++;
            }

            for (var i = count; i < destination.Length; i++)
            {
                destination[i] = Vector4.zero;
            }

            return count;
        }
    }
}
