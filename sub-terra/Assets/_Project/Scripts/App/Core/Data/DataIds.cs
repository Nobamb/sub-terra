namespace SubTerra.App.Core.Data
{
    /// <summary>
    /// 배포 후 변경하지 않는 영구 ID 상수.
    /// 표시 이름·에셋 파일명과 분리하며, 조회는 이 ID만 사용한다.
    /// </summary>
    public static class DataIds
    {
        public static class Minerals
        {
            public const string Copper = "mineral.copper";
            public const string Iron = "mineral.iron";
            public const string Lithium = "mineral.lithium";
        }

        public static class Buildings
        {
            public const string SupportBasic = "building.support.basic";
            public const string LightBasic = "building.light.basic";
            public const string ChargerBasic = "building.charger.basic";
            public const string StorageBasic = "building.storage.basic";
            public const string SettlementBasic = "building.settlement.basic";
            public const string OutpostCoreBasic = "building.outpost_core.basic";
        }

        public static class Upgrades
        {
            public const string DrillSpeed = "upgrade.drill.speed";
            public const string DrillEfficiency = "upgrade.drill.efficiency";
            public const string DroneScan = "upgrade.drone.scan";
            public const string DroneRescue = "upgrade.drone.rescue";
        }

        public static class Recipes
        {
            public const string SupportBasic = "recipe.building.support.basic";
        }

        public static class Dialogue
        {
            public const string LowPowerWarning = "dialogue.low_power.warning";
        }
    }
}
