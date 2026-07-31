namespace SubTerra.App.Readiness
{
    /// <summary>PRD 기능을 추적하는 네 수준 게이트.</summary>
    public enum ReadinessGateLevel
    {
        Definition = 0,
        Runtime = 1,
        Restore = 2,
        Play = 3
    }

    public static class ReadinessGateLevelLabels
    {
        public const string Definition = "Definition";
        public const string Runtime = "Runtime";
        public const string Restore = "Restore";
        public const string Play = "Play";

        public static string ToLabel(ReadinessGateLevel level)
        {
            switch (level)
            {
                case ReadinessGateLevel.Definition:
                    return Definition;
                case ReadinessGateLevel.Runtime:
                    return Runtime;
                case ReadinessGateLevel.Restore:
                    return Restore;
                case ReadinessGateLevel.Play:
                    return Play;
                default:
                    return level.ToString();
            }
        }
    }
}
