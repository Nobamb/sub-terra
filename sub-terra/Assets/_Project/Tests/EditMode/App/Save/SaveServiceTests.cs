using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.Save
{
    public sealed class SaveServiceTests
    {
        private readonly List<UnityEngine.Object> created =
            new List<UnityEngine.Object>();
        private string testRoot;
        private SavePathPolicy paths;
        private PhysicalSaveFileSystem physical;
        private SaveDataMapper mapper;
        private SaveJsonCodec json;

        [SetUp]
        public void SetUp()
        {
            testRoot = Path.Combine(
                Path.GetTempPath(),
                "subterra-k-" + Guid.NewGuid().ToString("N"));
            paths = new SavePathPolicy(testRoot);
            physical = new PhysicalSaveFileSystem();
            mapper = new SaveDataMapper(new FixedSaveClock());
            json = new SaveJsonCodec(new SaveMigrationService());
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(created[i]);
            }

            created.Clear();
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, true);
            }
        }

        [Test]
        public void K_F01_AllBStateAndWorldSnapshot_RoundTrip()
        {
            var context = CreateContext(350);
            var save = CreateSaveService(physical);
            var load = CreateLoadService(physical);

            var saved = save.Save(2, context);
            var loaded = load.Load(2);

            Assert.That(saved.Status, Is.EqualTo(SaveStatus.Success));
            Assert.That(loaded.Status, Is.EqualTo(LoadStatus.Success));
            Assert.That(loaded.State.GameState.Player.Gold, Is.EqualTo(350));
            Assert.That(loaded.State.GameState.Player.Energy, Is.EqualTo(70));
            Assert.That(loaded.State.GameState.Progress.CompletedObjectives, Is.EqualTo(3));
            Assert.That(loaded.State.GameState.Progress.HasSeenOutpostTutorial, Is.True);
            Assert.That(loaded.State.GameState.Run.Depth, Is.EqualTo(40));
            Assert.That(loaded.State.GameState.Run.MaximumDepth, Is.EqualTo(64));
            Assert.That(
                loaded.State.GameState.Run.LifecyclePhase,
                Is.EqualTo(RunLifecyclePhase.Active));
            Assert.That(
                loaded.State.GameState.Run.StructuralRisk,
                Is.EqualTo(StructuralRiskLevel.Caution));
            Assert.That(loaded.State.Inventory.GetQuantity("mineral.copper"), Is.EqualTo(3));
            Assert.That(loaded.State.Inventory.CurrentWeight, Is.EqualTo(6f));
            Assert.That(loaded.State.Upgrades.GetLevel("upgrade.drill.speed"), Is.EqualTo(1));
            Assert.That(loaded.State.Upgrades.IsZoneUnlocked("zone.deep.1"), Is.True);
            Assert.That(
                loaded.State.GameState.Outpost.GetStorageQuantity("mineral.copper"),
                Is.EqualTo(7));
            Assert.That(
                loaded.State.GameState.Outpost.InstalledOutpostIds,
                Is.EqualTo(new[] { "outpost.1" }));
            Assert.That(loaded.State.GameState.Outpost.CheckpointId, Is.EqualTo("checkpoint.1"));
            Assert.That(loaded.State.Drone.dialogueCooldowns.Single().templateId, Is.EqualTo("dialogue.test"));
            Assert.That(loaded.State.World.worldSeed, Is.EqualTo(777));
            Assert.That(loaded.State.World.miningChanges.Single().isDestroyed, Is.True);
            Assert.That(loaded.State.World.buildings.Single().instanceId, Is.EqualTo("building.1"));
            Assert.That(
                loaded.State.World.powerState.cableConnections.Single().nodeAInstanceId,
                Is.EqualTo("node.a"));
            Assert.That(loaded.State.TargetSceneName, Is.EqualTo(SceneNames.Integration));

            Assert.That(paths.TryGetPaths(2, out var slotPaths), Is.True);
            Assert.That(File.Exists(slotPaths.Normal), Is.True);
            Assert.That(File.Exists(slotPaths.Temporary), Is.False);
            var metadata = load.GetSlotMetadata(2);
            Assert.That(metadata.SaveVersion, Is.EqualTo(SaveVersions.Current));
            Assert.That(metadata.Gold, Is.EqualTo(350));
        }

        [Test]
        public void K_F02_CorruptNormal_LoadsValidBackupWithoutOverwritingCorruptSource()
        {
            var save = CreateSaveService(physical);
            var load = CreateLoadService(physical);
            Assert.That(save.Save(1, CreateContext(100)).IsSuccess, Is.True);
            Assert.That(save.Save(1, CreateContext(200)).IsSuccess, Is.True);
            paths.TryGetPaths(1, out var slotPaths);
            const string corrupt = "{ broken normal";
            File.WriteAllText(slotPaths.Normal, corrupt);

            var result = load.Load(1);

            Assert.That(result.Status, Is.EqualTo(LoadStatus.RecoveredFromBackup));
            Assert.That(result.State.GameState.Player.Gold, Is.EqualTo(100));
            Assert.That(File.ReadAllText(slotPaths.Normal), Is.EqualTo(corrupt));
            Assert.That(load.GetSlotMetadata(1).IsRecoverableFromBackup, Is.True);
        }

        [Test]
        public void K_F03_BothCopiesCorrupt_ReturnsRecoveryChoicesAndPreservesFiles()
        {
            Directory.CreateDirectory(testRoot);
            paths.TryGetPaths(1, out var slotPaths);
            File.WriteAllText(slotPaths.Normal, "bad-normal");
            File.WriteAllText(slotPaths.Backup, "bad-backup");
            var beforeNormal = File.ReadAllText(slotPaths.Normal);
            var beforeBackup = File.ReadAllText(slotPaths.Backup);

            var result = CreateLoadService(physical).Load(1);

            Assert.That(result.Status, Is.EqualTo(LoadStatus.BothCopiesInvalid));
            Assert.That(
                result.RecoveryChoices,
                Is.EqualTo(SaveRecoveryChoice.Retry | SaveRecoveryChoice.StartNewGame));
            Assert.That(File.ReadAllText(slotPaths.Normal), Is.EqualTo(beforeNormal));
            Assert.That(File.ReadAllText(slotPaths.Backup), Is.EqualTo(beforeBackup));
        }

        [TestCase(FaultStage.TemporaryWrite, SaveStatus.TemporaryWriteFailed)]
        [TestCase(FaultStage.TemporaryValidation, SaveStatus.TemporaryValidationFailed)]
        public void K_F04_TemporaryFailures_DoNotCreateNormalSave(
            FaultStage stage,
            SaveStatus expected)
        {
            var faulting = new FaultingFileSystem(physical, stage);
            var result = CreateSaveService(faulting).Save(1, CreateContext(10));
            paths.TryGetPaths(1, out var slotPaths);

            Assert.That(result.Status, Is.EqualTo(expected));
            Assert.That(File.Exists(slotPaths.Normal), Is.False);
            Assert.That(File.Exists(slotPaths.Temporary), Is.False);
        }

        [TestCase(FaultStage.BackupMove, SaveStatus.BackupFailed)]
        [TestCase(FaultStage.PromoteMove, SaveStatus.PromoteFailed)]
        public void K_F04_MoveFailures_PreserveRecoverablePreviousSave(
            FaultStage stage,
            SaveStatus expected)
        {
            Assert.That(
                CreateSaveService(physical).Save(1, CreateContext(111)).IsSuccess,
                Is.True);
            var faulting = new FaultingFileSystem(physical, stage);

            var result = CreateSaveService(faulting).Save(1, CreateContext(222));
            var loaded = CreateLoadService(physical).Load(1);

            Assert.That(result.Status, Is.EqualTo(expected));
            Assert.That(loaded.IsSuccess, Is.True);
            Assert.That(loaded.State.GameState.Player.Gold, Is.EqualTo(111));
        }

        [Test]
        public void K_F05_OldVersionMigratesInMemory_FutureVersionIsPreservedAndRejected()
        {
            Directory.CreateDirectory(testRoot);
            paths.TryGetPaths(1, out var slotPaths);
            var old = mapper.Capture(CreateContext(90));
            old.saveVersion = 1;
            old.targetSceneName = string.Empty;
            old.outpost = null;
            old.drone = null;
            var oldJson = JsonUtility.ToJson(old, true);
            File.WriteAllText(slotPaths.Normal, oldJson);

            var migrated = CreateLoadService(physical).Load(1);

            Assert.That(migrated.Status, Is.EqualTo(LoadStatus.Success));
            Assert.That(migrated.State.TargetSceneName, Is.EqualTo(SceneNames.Integration));
            Assert.That(migrated.State.Drone, Is.Not.Null);
            Assert.That(File.ReadAllText(slotPaths.Normal), Is.EqualTo(oldJson));
            Assert.That(CreateLoadService(physical).GetSlotMetadata(1).SaveVersion, Is.EqualTo(2));

            var future = mapper.Capture(CreateContext(91));
            future.saveVersion = 99;
            var futureJson = JsonUtility.ToJson(future, true);
            File.WriteAllText(slotPaths.Normal, futureJson);
            var rejected = CreateLoadService(physical).Load(1);

            Assert.That(rejected.Status, Is.EqualTo(LoadStatus.FutureVersion));
            Assert.That(File.ReadAllText(slotPaths.Normal), Is.EqualTo(futureJson));
        }

        [Test]
        public void K_F05_DuplicateIdsAndNegativeLevels_AreRejected()
        {
            var data = mapper.Capture(CreateContext(91));
            data.inventory.quantities.Add(
                new QuantitySaveEntry { id = "mineral.copper", quantity = 1 });

            Assert.That(SaveDataValidator.TryValidate(data, out _), Is.False);

            data = mapper.Capture(CreateContext(92));
            data.upgrades.levels[0].level = -1;

            Assert.That(SaveDataValidator.TryValidate(data, out _), Is.False);
        }

        [Test]
        public void DeleteSlot_WhenAnyDeleteFails_ReturnsFalse()
        {
            Assert.That(
                CreateSaveService(physical).Save(1, CreateContext(93)).IsSuccess,
                Is.True);

            var faulting = new FaultingFileSystem(physical, FaultStage.Delete);

            Assert.That(CreateSaveService(faulting).DeleteSlot(1), Is.False);
        }

        [Test]
        public void K_S02_SaveDtos_DoNotContainUnityObjects()
        {
            AssertNoUnityObject(typeof(GameSaveData), new HashSet<Type>());
        }

        [Test]
        public void K_S05_OnlyIntegerSlotsOneThroughThreeCanResolvePaths()
        {
            Assert.That(paths.TryGetPaths(0, out _), Is.False);
            Assert.That(paths.TryGetPaths(1, out var first), Is.True);
            Assert.That(paths.TryGetPaths(3, out _), Is.True);
            Assert.That(paths.TryGetPaths(4, out _), Is.False);
            Assert.That(Path.GetDirectoryName(first.Normal), Is.EqualTo(Path.GetFullPath(testRoot)));
            Assert.That(first.Normal, Does.EndWith("save_slot_1.json"));
        }

        [Test]
        public async Task K_F06_AutoSaveRequests_AreSerializedAndMergedToLatestDirtyState()
        {
            var writer = new SlowRecordingWriter();
            var captureCount = 0;
            using var coordinator = new AutoSaveCoordinator(
                writer,
                () => new SaveCaptureContext(
                    null,
                    null,
                    null,
                    null,
                    null,
                    string.Empty,
                    "capture-" + (++captureCount)),
                1);

            var run = coordinator.RequestAsync(AutoSaveReason.SurfaceReturn);
            await writer.FirstStarted.Task;
            var sameRun1 = coordinator.RequestAsync(AutoSaveReason.UpgradePurchased);
            var sameRun2 = coordinator.RequestAsync(AutoSaveReason.OutpostInstalled);
            writer.ReleaseFirst.TrySetResult(true);
            var result = await run;

            Assert.That(sameRun1, Is.SameAs(run));
            Assert.That(sameRun2, Is.SameAs(run));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(writer.CallCount, Is.EqualTo(2));
            Assert.That(writer.MaximumConcurrent, Is.EqualTo(1));
            Assert.That(writer.CapturedVersions, Is.EqualTo(new[] { "capture-1", "capture-2" }));
            Assert.That(coordinator.LatestReason, Is.EqualTo(AutoSaveReason.OutpostInstalled));
            Assert.That(await coordinator.FlushAsync(TimeSpan.FromMilliseconds(10)), Is.True);
        }

        private SaveService CreateSaveService(ISaveFileSystem fileSystem)
        {
            return new SaveService(fileSystem, paths, mapper, json);
        }

        private LoadService CreateLoadService(ISaveFileSystem fileSystem)
        {
            return new LoadService(fileSystem, paths, mapper, json);
        }

        private SaveCaptureContext CreateContext(int gold)
        {
            var outpost = new OutpostState();
            Assert.That(
                outpost.TryRestore(
                    new[] { new OutpostStorageEntryState("mineral.copper", 7) },
                    new[] { "outpost.1" },
                    "checkpoint.1",
                    12,
                    -4),
                Is.True);
            var game = GameState.FromParts(
                new PlayerState(70, 120, gold, 6f, 30f, 0.5f),
                new ProgressState(3, true),
                new RunState(
                    40,
                    64,
                    false,
                    StructuralRiskLevel.Caution,
                    GasRiskLevel.Elevated,
                    RunLifecyclePhase.Active),
                outpost);

            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 2f, 10);
            var inventory = new InventoryService(catalog, 100f, game);
            Assert.That(
                inventory.TryAddMineral("mineral.copper", 3).Status,
                Is.EqualTo(InventoryMutationStatus.Success));

            var upgrades = new UpgradeState();
            Assert.That(
                upgrades.TryRestore(
                    new[] { new UpgradeLevelState("upgrade.drill.speed", 1) }),
                Is.True);
            Assert.That(
                upgrades.TryRestoreUnlockedZones(new[] { "zone.deep.1" }),
                Is.True);

            var settings = Track(ScriptableObject.CreateInstance<SubTerra.App.Drone.DroneAnalysisSettings>());
            settings.EditorSetDefaults();
            var dialogue = new TemplateDialogueGenerator(
                Array.Empty<DialogueTemplateData>(),
                new FixedDroneClock(),
                settings);
            Assert.That(
                dialogue.TryRestoreCooldowns(
                    new[] { new DroneDialogueCooldownState("dialogue.test", 12.5d) }),
                Is.True);

            return new SaveCaptureContext(
                game,
                inventory.State,
                upgrades,
                dialogue,
                new FixedWorldProvider(),
                SceneNames.Integration,
                "0.1-test");
        }

        private T Track<T>(T value) where T : UnityEngine.Object
        {
            created.Add(value);
            return value;
        }

        private static void AssertNoUnityObject(Type type, HashSet<Type> visited)
        {
            if (!visited.Add(type))
            {
                return;
            }

            Assert.That(
                typeof(UnityEngine.Object).IsAssignableFrom(type),
                Is.False,
                type.FullName);
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
            {
                return;
            }

            if (type.IsArray)
            {
                AssertNoUnityObject(type.GetElementType(), visited);
                return;
            }

            if (type.IsGenericType)
            {
                foreach (var argument in type.GetGenericArguments())
                {
                    AssertNoUnityObject(argument, visited);
                }
            }

            foreach (var field in type.GetFields(
                BindingFlags.Public | BindingFlags.Instance))
            {
                AssertNoUnityObject(field.FieldType, visited);
            }
        }

        private sealed class FixedSaveClock : ISaveClock
        {
            public long UtcNowSeconds => 123456789;
        }

        private sealed class FixedDroneClock : IDroneClock
        {
            public double Now => 20d;
        }

        private sealed class FixedWorldProvider : IWorldSnapshotProvider
        {
            public WorldSnapshotDto CaptureSnapshot()
            {
                return new WorldSnapshotDto
                {
                    timestamp = 123,
                    worldSeed = 777,
                    miningChanges = new List<MiningSnapshotDto>
                    {
                        new MiningSnapshotDto { x = 1, y = 2, isDestroyed = true }
                    },
                    changedTiles = new List<ChangedTileSnapshotDto>
                    {
                        new ChangedTileSnapshotDto { x = 2, y = 3, tileId = "tile.changed" }
                    },
                    collapseChanges = new List<CollapseSnapshotDto>
                    {
                        new CollapseSnapshotDto { x = 3, y = 4, isCollapsed = true }
                    },
                    buildings = new List<BuildingSnapshotDto>
                    {
                        new BuildingSnapshotDto
                        {
                            instanceId = "building.1",
                            buildingTypeId = "building.support.basic",
                            level = 1,
                            health = 1f
                        }
                    },
                    gasChanges = new List<GasSnapshotDto>
                    {
                        new GasSnapshotDto
                        {
                            gasZoneId = "gas.1",
                            gasTypeId = "basic",
                            concentrationLevel = 0.4f,
                            remainingDuration = 8f
                        }
                    },
                    discoveredChunkIds = new List<string> { "chunk.1" },
                    powerState = new PowerSnapshotDto
                    {
                        cableConnections = new List<PowerConnectionSnapshotDto>
                        {
                            new PowerConnectionSnapshotDto
                            {
                                nodeAInstanceId = "node.a",
                                nodeBInstanceId = "node.b"
                            }
                        }
                    }
                };
            }

            public void RestoreSnapshot(WorldSnapshotDto snapshot)
            {
            }
        }

        public enum FaultStage
        {
            TemporaryWrite,
            TemporaryValidation,
            BackupMove,
            PromoteMove,
            Delete
        }

        private sealed class FaultingFileSystem : ISaveFileSystem
        {
            private readonly ISaveFileSystem inner;
            private readonly FaultStage stage;

            public FaultingFileSystem(ISaveFileSystem fileSystem, FaultStage faultStage)
            {
                inner = fileSystem;
                stage = faultStage;
            }

            public bool FileExists(string path) => inner.FileExists(path);
            public void CreateDirectory(string path) => inner.CreateDirectory(path);

            public void WriteAllText(string path, string contents)
            {
                if (stage == FaultStage.TemporaryWrite && path.EndsWith(".tmp"))
                {
                    throw new IOException();
                }

                inner.WriteAllText(path, contents);
            }

            public string ReadAllText(string path)
            {
                if (stage == FaultStage.TemporaryValidation && path.EndsWith(".tmp"))
                {
                    return "{}";
                }

                return inner.ReadAllText(path);
            }

            public void DeleteFile(string path)
            {
                if (stage == FaultStage.Delete)
                {
                    throw new IOException();
                }

                inner.DeleteFile(path);
            }

            public void MoveFile(string sourcePath, string destinationPath)
            {
                if (stage == FaultStage.BackupMove
                    && sourcePath.EndsWith(".json")
                    && !sourcePath.EndsWith(".backup.json"))
                {
                    throw new IOException();
                }

                if (stage == FaultStage.PromoteMove && sourcePath.EndsWith(".tmp"))
                {
                    throw new IOException();
                }

                inner.MoveFile(sourcePath, destinationPath);
            }
        }

        private sealed class SlowRecordingWriter : ISaveWriter
        {
            private int concurrent;

            public int CallCount { get; private set; }
            public int MaximumConcurrent { get; private set; }
            public List<string> CapturedVersions { get; } = new List<string>();
            public TaskCompletionSource<bool> FirstStarted { get; } =
                new TaskCompletionSource<bool>();
            public TaskCompletionSource<bool> ReleaseFirst { get; } =
                new TaskCompletionSource<bool>();

            public async Task<SaveResult> SaveAsync(
                int slotId,
                SaveCaptureContext context,
                CancellationToken cancellationToken)
            {
                CallCount++;
                concurrent++;
                MaximumConcurrent = Math.Max(MaximumConcurrent, concurrent);
                CapturedVersions.Add(context.GameVersion);
                if (CallCount == 1)
                {
                    FirstStarted.TrySetResult(true);
                    await ReleaseFirst.Task;
                }

                concurrent--;
                return new SaveResult(SaveStatus.Success, slotId);
            }
        }
    }
}
