using UnityEngine;

namespace SubTerra.App.UI.Building
{
    /// <summary>
    /// 레일 호버 홀로그램 이름표의 시간별 모양(순수 계산). MonoBehaviour 없이 EditMode에서 검사한다.
    /// 켜짐: 왼쪽에서 가로로 펼쳐지며 한 번 깜빡이고, 이름 글자가 앞에서부터 찍힌다.
    /// 유지: 주사선이 위에서 아래로 흐르고 밝기가 살짝 일렁인다.
    /// </summary>
    public static class BuildingRailHologramMotion
    {
        public const float OpenDuration = 0.14f;
        public const float TypeDuration = 0.2f;
        public const float CloseDuration = 0.08f;
        public const float ScanPeriod = 1.4f;
        public const float StartScaleX = 0.15f;
        public const float FlickerStart = 0.35f;
        public const float FlickerEnd = 0.5f;
        public const float FlickerAlpha = 0.35f;
        public const float ShimmerAmount = 0.06f;
        public const float ShimmerSpeed = 7f;

        /// <summary>켜진 뒤 경과 시간으로 가로 펼침 비율(0.15→1).</summary>
        public static float ScaleX(float elapsed)
        {
            var t = Mathf.Clamp01(elapsed / OpenDuration);
            return Mathf.Lerp(StartScaleX, 1f, Mathf.SmoothStep(0f, 1f, t));
        }

        /// <summary>켜진 뒤 경과 시간으로 투명도. 펼치는 도중 한 번 깜빡이고, 다 켜진 뒤엔 살짝 일렁인다.</summary>
        public static float Alpha(float elapsed)
        {
            if (elapsed < OpenDuration)
            {
                var t = Mathf.Clamp01(elapsed / OpenDuration);
                var alpha = Mathf.SmoothStep(0f, 1f, t);
                return t >= FlickerStart && t < FlickerEnd ? alpha * FlickerAlpha : alpha;
            }

            return 1f - ShimmerAmount * 0.5f * (1f + Mathf.Sin((elapsed - OpenDuration) * ShimmerSpeed));
        }

        /// <summary>이름 중 보이는 글자 수. 펼침이 반쯤 됐을 때부터 찍기 시작한다.</summary>
        public static int VisibleCharacters(float elapsed, int length)
        {
            if (length <= 0)
            {
                return 0;
            }

            var start = OpenDuration * 0.5f;
            var t = Mathf.Clamp01((elapsed - start) / TypeDuration);
            return Mathf.Clamp(Mathf.CeilToInt(t * length), 0, length);
        }

        /// <summary>주사선 위치(0=위, 1=아래).</summary>
        public static float ScanPosition(float elapsed)
        {
            if (elapsed <= 0f)
            {
                return 0f;
            }

            return Mathf.Repeat(elapsed, ScanPeriod) / ScanPeriod;
        }

        /// <summary>꺼질 때 남은 투명도 비율(1→0).</summary>
        public static float CloseAlpha(float closeElapsed)
        {
            return 1f - Mathf.Clamp01(closeElapsed / CloseDuration);
        }
    }
}
