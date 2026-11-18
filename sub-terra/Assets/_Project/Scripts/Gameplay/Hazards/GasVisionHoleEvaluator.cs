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
                // 시각 값이라 전원이 꺼진 조명은 구멍을 만들지 않는다(판정 IsCleared는 별개).
                if (source == null || !source.isActiveAndEnabled || !source.IsLit)
                {
                    continue;
                }

                var distance = Vector2.Distance(worldPosition, source.WorldPosition);
                cover = Mathf.Max(cover, GasVisualMath.LightCover(distance, source.VisualRadius, inner));
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
            return Sample(worldPosition, darkOpacity, inner, GasVisualRules.LightHoleCenterOpacity);
        }

        /// <summary>centerAlpha는 구멍 중심 알파. 가스 안 추가분은 포함하지 않는 기본 시각 샘플이다.</summary>
        public static Color Sample(Vector2 worldPosition, float darkOpacity, float inner, float centerAlpha)
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
                Mathf.Clamp01(centerAlpha));
            // 셰이더와 같은 프리멀티플라이드 보간.
            var cover = SampleCover(worldPosition, inner);
            var alpha = Mathf.Lerp(dark.a, light.a, cover);
            var rgb = Vector3.Lerp(
                new Vector3(dark.r, dark.g, dark.b) * dark.a,
                new Vector3(light.r, light.g, light.b) * light.a,
                cover) / Mathf.Max(alpha, 0.0001f);
            return new Color(rgb.x, rgb.y, rgb.z, alpha);
        }

        /// <summary>전원이 켜진 조명만 (x, y, 0, 반지름)으로 복사한다. 화면 거르기 없음.</summary>
        public static int CopyActiveLights(Vector4[] destination)
        {
            return CopyActiveLights(destination, null);
        }

        /// <summary>
        /// 전원이 켜진 조명 중 worldView와 겹치는 것만 복사한다(null이면 화면 거르기 없음).
        /// 개수는 destination 길이(최대 MaxLights)로 제한한다.
        /// </summary>
        public static int CopyActiveLights(Vector4[] destination, Rect? worldView)
        {
            if (destination == null)
            {
                return 0;
            }

            var limit = Mathf.Min(destination.Length, MaxLights);
            var count = 0;
            var sources = GasVisionClearanceSource.ResolveActive();
            for (var i = 0; i < sources.Count && count < limit; i++)
            {
                var source = sources[i];
                if (!IsVisualLight(source, worldView))
                {
                    continue;
                }

                var position = source.WorldPosition;
                destination[count] = new Vector4(position.x, position.y, 0f, source.VisualRadius);
                count++;
            }

            for (var i = count; i < destination.Length; i++)
            {
                destination[i] = Vector4.zero;
            }

            return count;
        }

        /// <summary>
        /// 화면(뷰포트) 기준 (vx, vy, 세로 반지름, 가로세로비)로 복사한다. 암전·가스 오버레이 UI 셰이더용.
        /// 전원이 켜진 화면 안 조명만 담고 MaxLights로 제한한다.
        /// </summary>
        public static int CopyViewportLights(Camera camera, Vector4[] destination)
        {
            if (destination == null)
            {
                return 0;
            }

            Rect? view = null;
            if (TryGetCameraWorldView(camera, out var rect))
            {
                view = rect;
            }

            var aspect = camera != null ? Mathf.Max(0.0001f, camera.aspect) : 1f;
            var limit = Mathf.Min(destination.Length, MaxLights);
            var count = 0;
            var sources = GasVisionClearanceSource.ResolveActive();
            for (var i = 0; i < sources.Count && count < limit; i++)
            {
                var source = sources[i];
                if (!IsVisualLight(source, view))
                {
                    continue;
                }

                var world = source.WorldPosition;
                Vector2 viewport;
                float radiusY;
                if (camera != null)
                {
                    var center = camera.WorldToViewportPoint(new Vector3(world.x, world.y, 0f));
                    var edge = camera.WorldToViewportPoint(new Vector3(world.x, world.y + source.VisualRadius, 0f));
                    viewport = center;
                    radiusY = Mathf.Abs(edge.y - center.y);
                }
                else
                {
                    viewport = world;
                    radiusY = source.VisualRadius;
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

        /// <summary>직교 카메라가 보는 월드 영역. 직교가 아니면 false(거르지 않는다).</summary>
        public static bool TryGetCameraWorldView(Camera camera, out Rect view)
        {
            view = default;
            if (camera == null || !camera.orthographic)
            {
                return false;
            }

            var halfHeight = camera.orthographicSize;
            var halfWidth = halfHeight * Mathf.Max(0.0001f, camera.aspect);
            var position = camera.transform.position;
            view = new Rect(position.x - halfWidth, position.y - halfHeight, halfWidth * 2f, halfHeight * 2f);
            return true;
        }

        private static bool IsVisualLight(GasVisionClearanceSource source, Rect? worldView)
        {
            if (source == null || !source.isActiveAndEnabled || !source.IsLit)
            {
                return false;
            }

            return !worldView.HasValue
                || GasVisualMath.CircleIntersectsRect(source.WorldPosition, source.Radius, worldView.Value);
        }
    }
}
