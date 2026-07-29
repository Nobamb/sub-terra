using System;
using System.Collections.Generic;
using SubTerra.Shared;

namespace SubTerra.App.Save
{
    [Serializable]
    public sealed class GameSaveData
    {
        public int saveVersion = SaveVersions.Current;
        public string gameVersion = string.Empty;
        public long savedAtUtc;
        public string targetSceneName = string.Empty;
        public PlayerSaveData player = new PlayerSaveData();
        public ProgressSaveData progress = new ProgressSaveData();
        public RunSaveData run = new RunSaveData();
        public InventorySaveData inventory = new InventorySaveData();
        public UpgradeSaveData upgrades = new UpgradeSaveData();
        public OutpostSaveData outpost = new OutpostSaveData();
        public DroneSaveData drone = new DroneSaveData();
        public WorldSnapshotDto world = new WorldSnapshotDto();
    }

    [Serializable]
    public sealed class PlayerSaveData
    {
        public int energy;
        public int maxEnergy;
        public int gold;
        public float cargoWeight;
        public float unsettledValue;
        public float progress;
    }

    [Serializable]
    public sealed class ProgressSaveData
    {
        public int completedObjectives;
        public bool hasSeenOutpostTutorial;
    }

    [Serializable]
    public sealed class RunSaveData
    {
        public int depth;
        public bool isSafe;
        public int structuralRisk;
        public int gasExposure;
    }

    [Serializable]
    public sealed class InventorySaveData
    {
        public float maxCapacity;
        public float currentWeight;
        public float unsettledValue;
        public List<QuantitySaveEntry> quantities = new List<QuantitySaveEntry>();
    }

    [Serializable]
    public sealed class QuantitySaveEntry
    {
        public string id = string.Empty;
        public int quantity;
    }

    [Serializable]
    public sealed class UpgradeSaveData
    {
        public List<UpgradeLevelSaveEntry> levels = new List<UpgradeLevelSaveEntry>();
        public List<string> unlockedZoneIds = new List<string>();
    }

    [Serializable]
    public sealed class UpgradeLevelSaveEntry
    {
        public string upgradeId = string.Empty;
        public int level;
    }

    [Serializable]
    public sealed class OutpostSaveData
    {
        public string checkpointId = string.Empty;
        public int checkpointX;
        public int checkpointY;
        public List<QuantitySaveEntry> storage = new List<QuantitySaveEntry>();
        public List<string> installedOutpostIds = new List<string>();
    }

    [Serializable]
    public sealed class DroneSaveData
    {
        public List<DroneCooldownSaveEntry> dialogueCooldowns =
            new List<DroneCooldownSaveEntry>();
    }

    [Serializable]
    public sealed class DroneCooldownSaveEntry
    {
        public string templateId = string.Empty;
        public double lastShownAt;
    }
}
