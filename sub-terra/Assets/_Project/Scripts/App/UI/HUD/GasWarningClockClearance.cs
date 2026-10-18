namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 화면에 켜져 있는 광산 초기화 시계 수. 가스 경고 패널이 이 값만 보고 자리를 정한다.
    /// </summary>
    public static class GasWarningClockClearance
    {
        private static int onScreenCount;

        public static bool ClockOnScreen { get; private set; }

        public static void Add(int delta)
        {
            if (delta == 0)
            {
                return;
            }

            onScreenCount += delta;
            if (onScreenCount < 0)
            {
                onScreenCount = 0;
            }

            var any = onScreenCount > 0;
            if (any == ClockOnScreen)
            {
                return;
            }

            ClockOnScreen = any;
        }

        public static void ResetForTests()
        {
            onScreenCount = 0;
            ClockOnScreen = false;
        }
    }
}
