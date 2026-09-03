namespace SubTerra.App.Tutorial
{
    /// <summary>prompt-B 60/86 데모 퀘스트 18개의 영구 ID와 진행 순서.</summary>
    public static class DemoObjectiveIds
    {
        public const string MineBlock = "demo.quest.mine_block";
        public const string MineCopper = "demo.quest.mine_copper";
        public const string UpgradeDrillSpeed = "demo.quest.upgrade_drill_speed";
        public const string TravelToSurface = "demo.quest.travel_to_surface";
        public const string ReturnToMine = "demo.quest.return_to_mine";
        public const string MineIron = "demo.quest.mine_iron";
        public const string PlaceSupportInDanger = "demo.quest.place_support_in_danger";
        public const string PlaceLadder = "demo.quest.place_ladder";
        public const string PlaceLightAtDepth = "demo.quest.place_light_at_depth";
        public const string StoreMineral = "demo.quest.store_mineral";
        public const string InstallOutpostCore = "demo.quest.install_outpost_core";
        public const string ChargeNearOutpost = "demo.quest.charge_near_outpost";
        public const string HealNearOutpost = "demo.quest.heal_near_outpost";
        public const string UnlockDeepZone = "demo.quest.unlock_deep_zone";
        public const string MineLithium = "demo.quest.mine_lithium";
        public const string PurifyGasWithOutpost = "demo.quest.purify_gas_with_outpost";
        public const string SellAtSettlement = "demo.quest.sell_at_settlement";
        public const string EmergencyEscapeReturn = "demo.quest.emergency_escape_return";

        // 완료 상태도 마지막 퀘스트를 가리켜 별도 18번째 목표가 생기지 않게 한다.
        public const string DemoEnd = EmergencyEscapeReturn;

        public static readonly string[] Ordered =
        {
            MineBlock,
            MineCopper,
            UpgradeDrillSpeed,
            TravelToSurface,
            ReturnToMine,
            MineIron,
            PlaceSupportInDanger,
            PlaceLadder,
            PlaceLightAtDepth,
            StoreMineral,
            InstallOutpostCore,
            ChargeNearOutpost,
            HealNearOutpost,
            UnlockDeepZone,
            MineLithium,
            PurifyGasWithOutpost,
            SellAtSettlement,
            EmergencyEscapeReturn
        };

        public const int RequiredCount = 18;
    }
}
