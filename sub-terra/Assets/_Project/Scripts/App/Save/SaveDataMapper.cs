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

            // Provider가 있으면 캡처. 없으면(Surface Base 등) 마지막 Mine 캐시 폴백.
            // 캐시도 없으면 빈 스냅샷으로 저장해 구 슬롯·호환 경로를 유지한다.
            var world = CaptureWorld(context);
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
                    hasSeenOutpostTutorial = game.Progress.HasSeenOutpostTutorial,
                    currentObjectiveId = game.Progress.CurrentObjectiveId ?? string.Empty,
                    isDemoComplete = game.Progress.IsDemoComplete
                },
                run = new RunSaveData
                {
                    depth = game.Run.Depth,
                    maximumDepth = game.Run.MaximumDepth,
                    isSafe = game.Run.IsSafe,
                    structuralRisk = (int)game.Run.StructuralRisk,
                    gasExposure = (int)game.Run.GasExposure,
                    lifecyclePhase = (int)game.Run.LifecyclePhase
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
                data.progress.hasSeenOutpostTutorial,
                data.progress.currentObjectiveId,
                data.progress.isDemoComplete);
            var run = new RunState(
                data.run.depth,
                data.run.maximumDepth,
                data.run.isSafe,
                (StructuralRiskLevel)data.run.structuralRisk,
                (GasRiskLevel)data.run.gasExposure,
                (RunLifecyclePhase)data.run.lifecyclePhase);
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

        /// <summary>
        /// 월드 스냅샷 확보 정책:
        /// 1) Provider 있음 → CaptureSnapshot
        /// 2) Provider 없음 + MineWorldFallback 있음 → 캐시 사용(빈 world 덮어쓰기 방지)
        /// 3) 둘 다 없음 → 빈 DTO
        /// </summary>
        private static WorldSnapshotDto CaptureWorld(SaveCaptureContext context)
        {
            if (context.WorldProvider != null)
            {
                return context.WorldProvider.CaptureSnapshot();
            }

            if (context.MineWorldFallback != null)
            {
                return MineWorldCache.Clone(context.MineWorldFallback)
                    ?? context.MineWorldFallback;
            }

            return new WorldSnapshotDto();
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
