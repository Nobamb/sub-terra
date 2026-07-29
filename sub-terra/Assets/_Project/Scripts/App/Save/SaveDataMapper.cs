using System;
using System.Collections.Generic;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Save
{
    public sealed class SaveDataMapper
    {
        private readonly ISaveClock clock;

        public SaveDataMapper(ISaveClock saveClock)
        {
            clock = saveClock ?? throw new ArgumentNullException(nameof(saveClock));
        }

        public GameSaveData Capture(SaveCaptureContext context)
        {
            if (context == null
                || !GameState.IsComplete(context.GameState)
                || context.Inventory == null
                || context.Upgrades == null)
            {
                return null;
            }

            // Surface Base 등 월드 Provider가 없을 때는 빈 스냅샷으로 저장한다.
            var world = context.WorldProvider != null
                ? context.WorldProvider.CaptureSnapshot()
                : new WorldSnapshotDto();
            if (world == null)
            {
                return null;
            }

            var game = context.GameState;
            var data = new GameSaveData
            {
                saveVersion = SaveVersions.Current,
                gameVersion = context.GameVersion,
                savedAtUtc = clock.UtcNowSeconds,
                targetSceneName = context.TargetSceneName,
                player = new PlayerSaveData
                {
                    energy = game.Player.Energy,
                    maxEnergy = game.Player.MaxEnergy,
                    gold = game.Player.Gold,
                    cargoWeight = game.Player.Cargo,
                    unsettledValue = game.Player.UnsettledValue,
                    progress = game.Player.Progress
                },
                progress = new ProgressSaveData
                {
                    completedObjectives = game.Progress.CompletedObjectives,
                    hasSeenOutpostTutorial = game.Progress.HasSeenOutpostTutorial
                },
                run = new RunSaveData
                {
                    depth = game.Run.Depth,
                    isSafe = game.Run.IsSafe,
                    structuralRisk = (int)game.Run.StructuralRisk,
                    gasExposure = (int)game.Run.GasExposure
                },
                inventory = CaptureInventory(context.Inventory),
                upgrades = CaptureUpgrades(context.Upgrades),
                outpost = CaptureOutpost(game.Outpost),
                drone = CaptureDrone(context.DialogueGenerator),
                world = world
            };

            return data;
        }

        public bool TryRestore(GameSaveData data, out RestoredSaveState restored)
        {
            restored = null;
            if (!SaveDataValidator.TryValidate(data, out _))
            {
                return false;
            }

            var player = new PlayerState(
                data.player.energy,
                data.player.maxEnergy,
                data.player.gold,
                data.player.cargoWeight,
                data.player.unsettledValue,
                data.player.progress);
            var progress = new ProgressState(
                data.progress.completedObjectives,
                data.progress.hasSeenOutpostTutorial);
            var run = new RunState(
                data.run.depth,
                data.run.isSafe,
                (StructuralRiskLevel)data.run.structuralRisk,
                (GasRiskLevel)data.run.gasExposure);
            var outpost = RestoreOutpost(data.outpost);
            var game = GameState.FromParts(player, progress, run, outpost);
            var inventory = RestoreInventory(data.inventory);
            var upgrades = RestoreUpgrades(data.upgrades);
            if (game == null || inventory == null || upgrades == null)
            {
                return false;
            }

            restored = new RestoredSaveState(
                game,
                inventory,
                upgrades,
                data.drone,
                data.world,
                data.targetSceneName);
            return true;
        }

        private static InventorySaveData CaptureInventory(InventoryState state)
        {
            var entries = new List<QuantitySaveEntry>();
            foreach (var pair in state.Quantities)
            {
                if (pair.Value > 0)
                {
                    entries.Add(new QuantitySaveEntry { id = pair.Key, quantity = pair.Value });
                }
            }

            entries.Sort((left, right) => string.CompareOrdinal(left.id, right.id));
            return new InventorySaveData
            {
                maxCapacity = state.MaxCapacity,
                currentWeight = state.CurrentWeight,
                unsettledValue = state.UnsettledValue,
                quantities = entries
            };
        }

        private static UpgradeSaveData CaptureUpgrades(UpgradeState state)
        {
            var result = new UpgradeSaveData();
            for (var i = 0; i < state.Levels.Count; i++)
            {
                var entry = state.Levels[i];
                if (entry != null)
                {
                    result.levels.Add(new UpgradeLevelSaveEntry
                    {
                        upgradeId = entry.UpgradeId,
                        level = entry.Level
                    });
                }
            }

            result.levels.Sort(
                (left, right) => string.CompareOrdinal(left.upgradeId, right.upgradeId));
            result.unlockedZoneIds.AddRange(state.UnlockedZoneIds);
            result.unlockedZoneIds.Sort(StringComparer.Ordinal);
            return result;
        }

        private static OutpostSaveData CaptureOutpost(OutpostState state)
        {
            var result = new OutpostSaveData
            {
                checkpointId = state.CheckpointId,
                checkpointX = state.CheckpointX,
                checkpointY = state.CheckpointY
            };
            for (var i = 0; i < state.Storage.Count; i++)
            {
                var entry = state.Storage[i];
                result.storage.Add(new QuantitySaveEntry
                {
                    id = entry.MineralId,
                    quantity = entry.Quantity
                });
            }

            result.storage.Sort((left, right) => string.CompareOrdinal(left.id, right.id));
            result.installedOutpostIds.AddRange(state.InstalledOutpostIds);
            result.installedOutpostIds.Sort(StringComparer.Ordinal);
            return result;
        }

        private static DroneSaveData CaptureDrone(TemplateDialogueGenerator generator)
        {
            var result = new DroneSaveData();
            if (generator == null)
            {
                return result;
            }

            var cooldowns = generator.CaptureCooldowns();
            for (var i = 0; i < cooldowns.Count; i++)
            {
                result.dialogueCooldowns.Add(new DroneCooldownSaveEntry
                {
                    templateId = cooldowns[i].TemplateId,
                    lastShownAt = cooldowns[i].LastShownAt
                });
            }

            return result;
        }

        private static InventoryState RestoreInventory(InventorySaveData data)
        {
            var state = new InventoryState(data.maxCapacity);
            for (var i = 0; i < data.quantities.Count; i++)
            {
                state.SetQuantity(data.quantities[i].id, data.quantities[i].quantity);
            }

            state.ApplyAggregates(data.currentWeight, data.unsettledValue);
            return state;
        }

        private static UpgradeState RestoreUpgrades(UpgradeSaveData data)
        {
            var state = new UpgradeState();
            var levels = new List<UpgradeLevelState>(data.levels.Count);
            for (var i = 0; i < data.levels.Count; i++)
            {
                levels.Add(new UpgradeLevelState(
                    data.levels[i].upgradeId,
                    data.levels[i].level));
            }

            return state.TryRestore(levels)
                && state.TryRestoreUnlockedZones(data.unlockedZoneIds)
                    ? state
                    : null;
        }

        private static OutpostState RestoreOutpost(OutpostSaveData data)
        {
            var state = new OutpostState();
            var storage = new List<OutpostStorageEntryState>(data.storage.Count);
            for (var i = 0; i < data.storage.Count; i++)
            {
                storage.Add(new OutpostStorageEntryState(
                    data.storage[i].id,
                    data.storage[i].quantity));
            }

            return state.TryRestore(
                storage,
                data.installedOutpostIds,
                data.checkpointId,
                data.checkpointX,
                data.checkpointY)
                    ? state
                    : null;
        }
    }
}
