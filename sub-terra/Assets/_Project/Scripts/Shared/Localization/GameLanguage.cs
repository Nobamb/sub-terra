namespace SubTerra.Shared.Localization
{
    /// <summary>지원 언어. 기본은 한국어이며 영어 키/테이블을 함께 유지한다.</summary>
    public enum GameLanguage
    {
        Korean = 0,
        English = 1
    }

    public static class GameLanguageCodes
    {
        public const string Korean = "ko";
        public const string English = "en";

        public static string ToCode(GameLanguage language)
        {
            return language == GameLanguage.English ? English : Korean;
        }

        public static GameLanguage FromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return GameLanguage.Korean;
            }

            code = code.Trim().ToLowerInvariant();
            if (code == English || code.StartsWith("en"))
            {
                return GameLanguage.English;
            }

            return GameLanguage.Korean;
        }
    }
}
