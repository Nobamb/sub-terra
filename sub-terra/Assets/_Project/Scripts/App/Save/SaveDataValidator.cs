using System;
using System.Collections.Generic;
using SubTerra.App.State;

namespace SubTerra.App.Save
{
    public static class SaveDataValidator
    {
        public static bool TryValidate(GameSaveData data, out string reason)
        {
            reason = string.Empty;
            if (data == null
                || data.saveVersion != SaveVersions.Current
                || data.player == null
                || data.progress == null
                || data.run == null
                || data.inventory == null
                || data.upgrades == null
                || data.outpost == null
                || data.drone == null
                || data.world == null
                || string.IsNullOrWhiteSpace(data.targetSceneName))
            {
                reason = "required";
                return false;
            }

            if (data.player.energy < 0
                || data.player.maxEnergy < 0
                || data.player.energy > data.player.maxEnergy
                || data.player.gold < 0
                || !IsNonNegativeFinite(data.player.cargoWeight)
                || !IsNonNegativeFinite(data.player.unsettledValue)
                || !IsFinite(data.player.progress)
                || data.progress.completedObjectives < 0
                || data.run.depth < 0
                || data.run.maximumDepth < data.run.depth
                || !Enum.IsDefined(typeof(StructuralRiskLevel), data.run.structuralRisk)
                || !Enum.IsDefined(typeof(GasRiskLevel), data.run.gasExposure)
                || !Enum.IsDefined(typeof(RunLifecyclePhase), data.run.lifecyclePhase))
            {
                reason = "state-range";
                return false;
            }

            if (!IsNonNegativeFinite(data.inventory.maxCapacity)
                || !IsNonNegativeFinite(data.inventory.currentWeight)
                || !IsNonNegativeFinite(data.inventory.unsettledValue)
                || data.inventory.quantities == null
                || !ValidateQuantities(data.inventory.quantities)
                || data.upgrades.levels == null
                || data.upgrades.unlockedZoneIds == null
                || !ValidateUpgrades(data.upgrades)
                || data.outpost.storage == null
                || data.outpost.installedOutpostIds == null
                || !ValidateQuantities(data.outpost.storage)
                || !ValidateUniqueIds(data.outpost.installedOutpostIds)
                || data.drone.dialogueCooldowns == null
                || !ValidateCooldowns(data.drone.dialogueCooldowns)
                || !ValidateWorld(data))
            {
                reason = "collection";
                return false;
            }

            return true;
        }

        public static void NormalizeMissingCollections(GameSaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.gameVersion ??= string.Empty;
            data.targetSceneName ??= string.Empty;
            data.player ??= new PlayerSaveData();
            data.progress ??= new ProgressSaveData();
            data.progress.currentObjectiveId ??= string.Empty;
            data.run ??= new RunSaveData();
            if (data.run.maximumDepth < data.run.depth)
            {
                data.run.maximumDepth = data.run.depth;
            }
            data.inventory ??= new InventorySaveData();
            data.inventory.quantities ??= new List<QuantitySaveEntry>();
            data.upgrades ??= new UpgradeSaveData();
            data.upgrades.levels ??= new List<UpgradeLevelSaveEntry>();
            data.upgrades.unlockedZoneIds ??= new List<string>();
            data.outpost ??= new OutpostSaveData();
            data.outpost.checkpointId ??= string.Empty;
            data.outpost.storage ??= new List<QuantitySaveEntry>();
            data.outpost.installedOutpostIds ??= new List<string>();
            data.drone ??= new DroneSaveData();
            data.drone.dialogueCooldowns ??= new List<DroneCooldownSaveEntry>();
            data.world ??= new SubTerra.Shared.WorldSnapshotDto();
            data.world.version ??= "1.2";
            data.world.miningChanges ??= new List<SubTerra.Shared.MiningSnapshotDto>();
            data.world.changedTiles ??= new List<SubTerra.Shared.ChangedTileSnapshotDto>();
            data.world.collapseChanges ??= new List<SubTerra.Shared.CollapseSnapshotDto>();
            data.world.buildings ??= new List<SubTerra.Shared.BuildingSnapshotDto>();
            data.world.gasChanges ??= new List<SubTerra.Shared.GasSnapshotDto>();
            data.world.discoveredChunkIds ??= new List<string>();
            var power = data.world.powerState;
            power.cableConnections ??= new List<SubTerra.Shared.PowerConnectionSnapshotDto>();
            data.world.powerState = power;
        }

        private static bool ValidateQuantities(IReadOnlyList<QuantitySaveEntry> entries)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null
                    || string.IsNullOrEmpty(entry.id)
                    || entry.quantity <= 0
                    || !ids.Add(entry.id))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateUpgrades(UpgradeSaveData upgrades)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < upgrades.levels.Count; i++)
            {
                var entry = upgrades.levels[i];
                if (entry == null
                    || string.IsNullOrEmpty(entry.upgradeId)
                    || entry.level < 0
                    || !ids.Add(entry.upgradeId))
                {
                    return false;
                }
            }

            return ValidateUniqueIds(upgrades.unlockedZoneIds);
        }

        private static bool ValidateUniqueIds(IReadOnlyList<string> ids)
        {
            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < ids.Count; i++)
            {
                if (string.IsNullOrEmpty(ids[i]) || !unique.Add(ids[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateCooldowns(IReadOnlyList<DroneCooldownSaveEntry> entries)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null
                    || string.IsNullOrEmpty(entry.templateId)
                    || double.IsNaN(entry.lastShownAt)
                    || double.IsInfinity(entry.lastShownAt)
                    || !ids.Add(entry.templateId))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateWorld(GameSaveData data)
        {
            var world = data.world;
            if (string.IsNullOrEmpty(world.version)
                || world.miningChanges == null
                || world.changedTiles == null
                || world.collapseChanges == null
                || world.buildings == null
                || world.gasChanges == null
                || world.discoveredChunkIds == null
                || world.powerState.cableConnections == null
                || !ValidateUniqueIds(world.discoveredChunkIds))
            {
                return false;
            }

            var buildingIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < world.buildings.Count; i++)
            {
                var entry = world.buildings[i];
                if (string.IsNullOrEmpty(entry.instanceId)
                    || string.IsNullOrEmpty(entry.buildingTypeId)
                    || entry.level < 0
                    || !IsNonNegativeFinite(entry.health)
                    || !buildingIds.Add(entry.instanceId))
                {
                    return false;
                }
            }

            var gasIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < world.gasChanges.Count; i++)
            {
                var entry = world.gasChanges[i];
                if (string.IsNullOrEmpty(entry.gasZoneId)
                    || !IsNonNegativeFinite(entry.concentrationLevel)
                    || !IsNonNegativeFinite(entry.remainingDuration)
                    || !gasIds.Add(entry.gasZoneId))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsNonNegativeFinite(float value)
        {
            return IsFinite(value) && value >= 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
