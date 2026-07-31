namespace SubTerra.App.Readiness
{
    /// <summary>
    /// MVP2 완료 게이트 상태. 허용 라벨은 네 가지뿐이며 추측으로 완료 처리하지 않는다.
    /// </summary>
    public enum ReadinessStatus
    {
        Complete = 0,
        Partial = 1,
        Unimplemented = 2,
        Unverified = 3
    }

    /// <summary>보고서/테스트에서 사용하는 한국어 상태 문자열.</summary>
    public static class ReadinessStatusLabels
    {
        public const string Complete = "완료";
        public const string Partial = "부분";
        public const string Unimplemented = "미구현";
        public const string Unverified = "미검증";

        public static string ToLabel(ReadinessStatus status)
        {
            switch (status)
            {
                case ReadinessStatus.Complete:
                    return Complete;
                case ReadinessStatus.Partial:
                    return Partial;
                case ReadinessStatus.Unimplemented:
                    return Unimplemented;
                case ReadinessStatus.Unverified:
                    return Unverified;
                default:
                    return Unverified;
            }
        }

        public static bool IsAllowedLabel(string label)
        {
            return label == Complete
                || label == Partial
                || label == Unimplemented
                || label == Unverified;
        }

        public static bool TryParse(string label, out ReadinessStatus status)
        {
            if (label == Complete)
            {
                status = ReadinessStatus.Complete;
                return true;
            }

            if (label == Partial)
            {
                status = ReadinessStatus.Partial;
                return true;
            }

            if (label == Unimplemented)
            {
                status = ReadinessStatus.Unimplemented;
                return true;
            }

            if (label == Unverified)
            {
                status = ReadinessStatus.Unverified;
                return true;
            }

            status = ReadinessStatus.Unverified;
            return false;
        }
    }
}
