namespace SubTerra.App.Tutorial
{
    /// <summary>
    /// 데모 필수 13단계 목표의 영구 ID.
    /// 표시 문구와 분리하며, 세이브·전이·UI는 이 ID만 사용한다.
    /// </summary>
    public static class DemoObjectiveIds
    {
        public const string ExploreStart = "demo.explore_start";
        public const string MineCopperIron = "demo.mine_copper_iron";
        public const string PathGuide = "demo.path_guide";
        public const string MineLithium = "demo.mine_lithium";
        public const string StructuralCrack = "demo.structural_crack";
        public const string PlaceSupport = "demo.place_support";
        public const string GasEncounter = "demo.gas_encounter";
        public const string OutpostInstall = "demo.outpost_install";
        public const string ReturnRecommend = "demo.return_recommend";
        public const string Settlement = "demo.settlement";
        public const string BatteryUpgrade = "demo.battery_upgrade";
        public const string DeepSignal = "demo.deep_signal";
        public const string DemoEnd = "demo.end";

        public static readonly string[] Ordered =
        {
            ExploreStart,
            MineCopperIron,
            PathGuide,
            MineLithium,
            StructuralCrack,
            PlaceSupport,
            GasEncounter,
            OutpostInstall,
            ReturnRecommend,
            Settlement,
            BatteryUpgrade,
            DeepSignal,
            DemoEnd
        };

        public const int RequiredCount = 13;
    }
}
