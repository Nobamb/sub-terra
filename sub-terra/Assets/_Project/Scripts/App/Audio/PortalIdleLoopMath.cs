using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.Audio
{
    /// <summary>
    /// B-162: 포탈 대기음 계산(순수). 가장 가까운 포탈 하나와 거리별 음량·또렷함을 정한다.
    /// 거리는 타일 칸(1칸 = 월드 1) 기준이다.
    /// </summary>
    public static class PortalIdleLoopMath
    {
        public const float DefaultFullDistance = 2f;
        public const float DefaultSilentDistance = 10f;

        /// <summary>가장 가까운 포탈 번호. 없으면 -1. 거리가 같으면 앞 번호를 고른다.</summary>
        public static int Nearest(Vector2 listener, IReadOnlyList<Vector2> portals, out float distance)
        {
            distance = float.PositiveInfinity;
            var nearest = -1;
            if (portals == null)
            {
                return nearest;
            }

            for (var i = 0; i < portals.Count; i++)
            {
                var d = Vector2.Distance(listener, portals[i]);
                if (d < distance)
                {
                    distance = d;
                    nearest = i;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 거리별 음량 배율(0~1). fullDistance 이내 1, silentDistance 이상 0.
        /// 곡선은 0(가까움)→1(멂) 진행값을 받아 배율을 돌려준다. 없으면 부드러운 S 곡선.
        /// </summary>
        public static float Gain(float distance, float fullDistance, float silentDistance, AnimationCurve falloff = null)
        {
            if (float.IsInfinity(distance) || float.IsNaN(distance))
            {
                return 0f;
            }

            var far = Mathf.Max(fullDistance + 0.01f, silentDistance);
            if (distance <= fullDistance)
            {
                return 1f;
            }

            if (distance >= far)
            {
                return 0f;
            }

            var t = Mathf.InverseLerp(fullDistance, far, distance);
            if (falloff != null && falloff.length > 0)
            {
                return Mathf.Clamp01(falloff.Evaluate(t));
            }

            return 1f - t * t * (3f - 2f * t);
        }

        /// <summary>거리별 또렷함(저역 통과 차단 주파수). 가까우면 nearCutoff, 멀면 farCutoff.</summary>
        public static float Cutoff(float gain, float nearCutoff, float farCutoff)
        {
            return Mathf.Lerp(farCutoff, nearCutoff, Mathf.Clamp01(gain));
        }
    }
}
