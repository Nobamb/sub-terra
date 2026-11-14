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

        /// <summary>
        /// 조명 구멍이 걷은 정도(0~1). 가장 가까운 조명이 아니라 모든 조명 중 최댓값이며(셰이더와 동일),
        /// 조명 반지름 밖에서는 항상 0이다. 구멍 판정(TryGetNearestLight/IsCleared)과 무관한 시각 값이다.
        /// </summary>
        public static float SampleCover(Vector2 worldPosition, float inner)
        {
            var cover = 0f;
            var sources = GasVisionClearanceSource.ResolveActive();
            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (source == null || !source.isActiveAndEnabled)
                {
                    continue;
                }

                var distance = Vector2.Distance(worldPosition, source.WorldPosition);
                cover = Mathf.Max(cover, GasVisualMath.LightCover(distance, source.Radius, inner));
            }

            return cover;
        }

        public static Color Sample(Vector2 worldPosition, float darkOpacity)
        {
            return Sample(worldPosition, darkOpacity, GasVisualRules.LightHoleInnerFraction);
        }

        /// <summary>중심은 약한 붉은 빛, 경계로 갈수록 베일 색으로 스며든다.</summary>
        public static Color Sample(Vector2 worldPosition, float darkOpacity, float inner)
        {
            var dark = new Color(
                GasVisualRules.VeilDarkColor.r,
                GasVisualRules.VeilDarkColor.g,
                GasVisualRules.VeilDarkColor.b,
                Mathf.Clamp01(darkOpacity));
            var light = new Color(
                GasVisualRules.LightClearTint.r,
                GasVisualRules.LightClearTint.g,
                GasVisualRules.LightClearTint.b,
                GasVisualRules.LightClearRedOpacity);
            // 셰이더와 같은 프리멀티플라이드 보간.
            var cover = SampleCover(worldPosition, inner);
            var alpha = Mathf.Lerp(dark.a, light.a, cover);
            var rgb = Vector3.Lerp(
                new Vector3(dark.r, dark.g, dark.b) * dark.a,
                new Vector3(light.r, light.g, light.b) * light.a,
                cover) / Mathf.Max(alpha, 0.0001f);
            return new Color(rgb.x, rgb.y, rgb.z, alpha);
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
