namespace SubTerra.App.Readiness
{
    /// <summary>
    /// BuildingData Runtime Prefab이 공용 임시 placeholder인지 실제 기능 Prefab인지 구분한다.
    /// </summary>
    public static class PlaceholderRuntimeClassifier
    {
        public const string PlaceholderNameToken = "BuildingPlaceholder";
        public const string PlaceholderPathToken = "BuildingPlaceholder.prefab";

        public static bool IsPlaceholder(string prefabName, string assetPath)
        {
            if (!string.IsNullOrEmpty(prefabName)
                && prefabName.IndexOf(PlaceholderNameToken, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(assetPath)
                && assetPath.IndexOf(PlaceholderPathToken, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        public static string ClassifyLabel(string prefabName, string assetPath, bool prefabMissing)
        {
            if (prefabMissing)
            {
                return "missing";
            }

            return IsPlaceholder(prefabName, assetPath) ? "placeholder" : "real";
        }
    }
}
