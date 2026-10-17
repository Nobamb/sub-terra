using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 가스 경고 패널과 광산 초기화 시계의 세로 배치.
    /// 시계 루트는 상단 중앙, y=-12, 높이 78이다.
    /// </summary>
    public static class GasWarningClockLayout
    {
        public const float ClockTopY = -12f;
        public const float ClockHeight = 78f;
        public const float Gap = 8f;

        public static float BelowClockY => ClockTopY - ClockHeight - Gap;

        public static Vector2 Position(bool clockOnScreen, Vector2 resting)
        {
            if (!clockOnScreen)
            {
                return resting;
            }

            return new Vector2(resting.x, BelowClockY);
        }
    }
}
