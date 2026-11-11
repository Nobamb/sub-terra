using UnityEngine;

namespace SubTerra.App.UI.Building
{
    /// <summary>
    /// 레일 호버 홀로그램 이름표의 가로 펼침 시작 크기와 주사선 위치(순수 계산).
    /// </summary>
    public static class BuildingRailHologramMotion
    {
        public const float ScanPeriod = 1.4f;
        public const float StartScaleX = 0.15f;

        /// <summary>주사선 위치(0=위, 1=아래).</summary>
        public static float ScanPosition(float elapsed)
        {
            if (elapsed <= 0f)
            {
                return 0f;
            }

            return Mathf.Repeat(elapsed, ScanPeriod) / ScanPeriod;
        }

    }
}
