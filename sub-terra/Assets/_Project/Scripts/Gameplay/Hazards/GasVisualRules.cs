namespace SubTerra.Gameplay.Hazards
{
    /// <summary>
    /// 유독가스 생성/접근 연출과 조명 시야 구멍의 공통 수치.
    /// 월드 단위는 Grid cellSize 1을 1칸으로 본다.
    /// </summary>
    public static class GasVisualRules
    {
        public const float SpawnDurationSeconds = 1f;
        public const float ApproachFadeSeconds = 1f;
        public const float GasRadiusBlocks = 5f;
        public const float GasVisualOpacity = 0.70f;
        public const float InitialApproachOpacity = 0.35f;
        public const float FullApproachOpacity = 0.95f;
        public const float LightClearRadiusBlocks = 5f;
        public const float LightClearRedOpacity = 0.05f;

        public const string GasCloudChildName = "GasCloud";
        public const string LightClearanceChildName = "GasClearance";
    }
}
