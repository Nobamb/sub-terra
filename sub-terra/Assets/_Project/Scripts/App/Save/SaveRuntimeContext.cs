using System;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Save
{
    public sealed class SaveCaptureContext
    {
        public GameState GameState { get; }
        public InventoryState Inventory { get; }
        public UpgradeState Upgrades { get; }
        public TemplateDialogueGenerator DialogueGenerator { get; }
        public IWorldSnapshotProvider WorldProvider { get; }
        public string TargetSceneName { get; }
        public string GameVersion { get; }

        /// <summary>
        /// Provider가 없을 때(Surface Base 등) 사용할 마지막 Mine world 폴백.
        /// null이면 기존처럼 빈 WorldSnapshotDto를 저장한다.
        /// </summary>
        public WorldSnapshotDto MineWorldFallback { get; }

        public SaveCaptureContext(
            GameState gameState,
            InventoryState inventory,
            UpgradeState upgrades,
            TemplateDialogueGenerator dialogueGenerator,
            IWorldSnapshotProvider worldProvider,
            string targetSceneName,
            string gameVersion,
            WorldSnapshotDto mineWorldFallback = null)
        {
            GameState = gameState;
            Inventory = inventory;
            Upgrades = upgrades;
            DialogueGenerator = dialogueGenerator;
            WorldProvider = worldProvider;
            TargetSceneName = targetSceneName ?? string.Empty;
            GameVersion = gameVersion ?? string.Empty;
            MineWorldFallback = mineWorldFallback;
        }
    }

    public sealed class RestoredSaveState
    {
        public GameState GameState { get; }
        public InventoryState Inventory { get; }
        public UpgradeState Upgrades { get; }
        public DroneSaveData Drone { get; }
        public WorldSnapshotDto World { get; }
        public string TargetSceneName { get; }

        public RestoredSaveState(
            GameState gameState,
            InventoryState inventory,
            UpgradeState upgrades,
            DroneSaveData drone,
            WorldSnapshotDto world,
            string targetSceneName)
        {
            GameState = gameState;
            Inventory = inventory;
            Upgrades = upgrades;
            Drone = drone;
            World = world;
            TargetSceneName = targetSceneName ?? string.Empty;
        }
    }

    public interface ISaveClock
    {
        long UtcNowSeconds { get; }
    }

    public sealed class SystemSaveClock : ISaveClock
    {
        public long UtcNowSeconds => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
