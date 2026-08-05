using SubTerra.App.Save;

namespace SubTerra.App.RuntimeInfo
{
    /// <summary>Player build channel and persistent-save version shown to testers.</summary>
    public static class BuildVersionInfo
    {
        public static string Channel
        {
            get
            {
#if SUBTERRA_BUILD_RELEASE
                return "Release";
#elif SUBTERRA_BUILD_QA
                return "QA";
#elif SUBTERRA_BUILD_DEVELOPMENT
                return "Development";
#else
                return "Editor";
#endif
            }
        }

        public static string Format(string gameVersion)
        {
            var version = string.IsNullOrEmpty(gameVersion) ? "0.0.0" : gameVersion;
            return "Game " + version + " | Build " + Channel + " | Save v" + SaveVersions.Current;
        }
    }
}
