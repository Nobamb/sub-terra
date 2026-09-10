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
            public const string LadderBasic = "building.ladder.basic";
            public const string LightBasic = "building.light.basic";
            public const string ChargerBasic = "building.charger.basic";
            public const string ClinicBasic = "building.clinic.basic";
            public const string StorageBasic = "building.storage.basic";
            public const string SettlementBasic = "building.settlement.basic";
            public const string OutpostCoreBasic = "building.outpost_core.basic";
            public const string EmergencyEscapePortal = "building.escape_portal.emergency";
        }

        public static class Upgrades
        {
            public const string DrillSpeed = "upgrade.drill.speed";
            public const string DrillEfficiency = "upgrade.drill.efficiency";
            public const string MaximumEnergy = "upgrade.energy.maximum";
            public const string MaximumHealth = "upgrade.health.maximum";
            public const string HealthRegeneration = "upgrade.health.regeneration";
            public const string MaximumCargo = "upgrade.cargo.maximum";
            public const string DroneScan = "upgrade.drone.scan";
            public const string DroneRescue = "upgrade.drone.rescue";
            public const string GasResistance = "upgrade.gas.resistance";
        }

        public static class Recipes
        {
            public const string SupportBasic = "recipe.building.support.basic";
        }

        public static class Zones
        {
            public const string Deep = "zone.deep";
        }

        public static class Dialogue
        {
            public const string LowPowerWarning = "dialogue.low_power.warning";
            public const string DroneEmergency = "dialogue.drone.survival.emergency";
            public const string DroneStructuralWarning = "dialogue.drone.structural.warning";
            public const string DroneGasWarning = "dialogue.drone.gas.warning";
            public const string DroneCargoFull = "dialogue.drone.cargo.full";
            public const string DroneReturn = "dialogue.drone.return";
            public const string DroneLithium = "dialogue.drone.lithium";
            public const string DroneOutpost = "dialogue.drone.outpost";
            public const string DroneExplore = "dialogue.drone.explore";
        }
    }
}
