using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.Gameplay.Snapshot;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Tests.Save
{
    /// <summary>
    /// MVP2 Phase M: Seed+변경점 월드 왕복, 원자적 파일, Migration, 활성화 게이트.
    /// 임시 파일 시스템만 사용하며 프로덕션 세이브 경로를 건드리지 않는다.
    /// </summary>
    public sealed class PhaseMWorldSaveTests
    {
        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
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
                "subterra-m-" + Guid.NewGuid().ToString("N"));
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
        public void M_S01_WorldAndGameSaveDtos_ContainZeroUnityObjectFields()
        {
            AssertNoUnityObject(typeof(GameSaveData), new HashSet<Type>());
            AssertNoUnityObject(typeof(WorldSnapshotDto), new HashSet<Type>());
            AssertNoUnityObject(typeof(BuildingSnapshotDto), new HashSet<Type>());
            AssertNoUnityObject(typeof(GasSnapshotDto), new HashSet<Type>());
            AssertNoUnityObject(typeof(PowerConnectionSnapshotDto), new HashSet<Type>());
        }

        [Test]
        public void M_S02_SaveVersion_HasMigrationPathFromFirstToCurrent()
        {
            Assert.That(SaveVersions.First, Is.EqualTo(1));
            Assert.That(SaveVersions.Current, Is.GreaterThanOrEqualTo(SaveVersions.First));

            var data = mapper.Capture(CreateFullContext(55));
            data.saveVersion = SaveVersions.First;
            data.targetSceneName = string.Empty;
            data.world.generatorVersion = 0;
            data.progress.currentObjectiveId = null;

            var status = new SaveMigrationService().TryMigrate(data);

            Assert.That(status, Is.EqualTo(SaveMigrationStatus.Migrated));
            Assert.That(data.saveVersion, Is.EqualTo(SaveVersions.Current));
            Assert.That(data.targetSceneName, Is.EqualTo(SceneNames.Integration));
            Assert.That(data.world.generatorVersion, Is.EqualTo(1));
            Assert.That(data.progress.currentObjectiveId, Is.EqualTo(string.Empty));
        }

        [Test]
        public void M_S03_ContinueService_RestoresInStateSceneWorldDerivedUiOrder()
        {
            var order = new List<string>();
            var save = CreateSaveService();
            Assert.That(save.Save(1, CreateFullContext(77)).IsSuccess, Is.True);

            var continueService = new ContinueService(
                CreateLoadService(),
                new OrderedStateReceiver(order),
                new OrderedSceneLoader(order),
                new OrderedWorldResolver(order, succeed: true),
                new OrderedRecalculator(order),
                new OrderedUiGate(order));

            var result = continueService.Continue(1);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                order,
                Is.EqualTo(new[] { "ui_off", "state", "scene", "world", "derived", "ui_on" }));
        }

        [Test]
        public void M_S04_AtomicSave_UsesTmpValidateBackupOfficialOrder()
        {
            var recorder = new RecordingFileSystem(physical);
            var save = new SaveService(recorder, paths, mapper, json);
            Assert.That(save.Save(1, CreateFullContext(10)).IsSuccess, Is.True);
            Assert.That(save.Save(1, CreateFullContext(20)).IsSuccess, Is.True);

            Assert.That(recorder.Operations.Count(op => op.StartsWith("write:")), Is.GreaterThanOrEqualTo(2));
            Assert.That(recorder.Operations.Any(op => op.Contains(".tmp")), Is.True);
            Assert.That(recorder.Operations.Any(op => op.Contains(".backup.json")), Is.True);
            paths.TryGetPaths(1, out var slotPaths);
            Assert.That(File.Exists(slotPaths.Normal), Is.True);
            Assert.That(File.Exists(slotPaths.Temporary), Is.False);
            Assert.That(File.Exists(slotPaths.Backup), Is.True);
        }

        [Test]
        public void M_S05_SaveServiceSource_DoesNotLogRawJsonOrAbsoluteUserPaths()
        {
            var saveDir = Path.Combine(Application.dataPath, "_Project", "Scripts", "App", "Save");
            var saveSource = File.ReadAllText(Path.Combine(saveDir, "SaveService.cs"));
            var loadSource = File.ReadAllText(Path.Combine(saveDir, "LoadService.cs"));
            var runtimeSource = File.ReadAllText(Path.Combine(saveDir, "SaveRuntimeController.cs"));

            Assert.That(saveSource, Does.Not.Contain("Debug.Log"));
            Assert.That(loadSource, Does.Not.Contain("Debug.Log"));
            Assert.That(runtimeSource, Does.Not.Contain("JsonUtility.ToJson"));
            Assert.That(
                runtimeSource,
                Does.Contain("세이브 원문과 전체 저장 경로는 로그로 남기지 않는다"));
        }

        [Test]
        public void M_F01_FullRoundTrip_RestoresPlayerInventoryUpgradeOutpostRunDroneAndWorldDeltas()
        {
            var world = new ScriptedWorldProvider();
            var context = CreateFullContext(350, world);
            var save = CreateSaveService();
            var load = CreateLoadService();

            Assert.That(save.Save(2, context).IsSuccess, Is.True);
            var loaded = load.Load(2);

            Assert.That(loaded.IsSuccess, Is.True);
            Assert.That(loaded.State.GameState.Player.Gold, Is.EqualTo(350));
            Assert.That(loaded.State.GameState.Player.Energy, Is.EqualTo(70));
            Assert.That(loaded.State.Inventory.GetQuantity("mineral.copper"), Is.EqualTo(3));
            Assert.That(loaded.State.Upgrades.GetLevel("upgrade.drill.speed"), Is.EqualTo(1));
            Assert.That(loaded.State.GameState.Outpost.CheckpointId, Is.EqualTo("checkpoint.1"));
            Assert.That(loaded.State.GameState.Outpost.CheckpointX, Is.EqualTo(12));
            Assert.That(loaded.State.GameState.Outpost.CheckpointY, Is.EqualTo(-4));
            Assert.That(loaded.State.GameState.Run.Depth, Is.EqualTo(40));
            Assert.That(
                loaded.State.GameState.Run.LifecyclePhase,
                Is.EqualTo(RunLifecyclePhase.Active));
            Assert.That(
                loaded.State.Drone.dialogueCooldowns.Single().templateId,
                Is.EqualTo("dialogue.test"));

            Assert.That(loaded.State.World.worldSeed, Is.EqualTo(777));
            Assert.That(loaded.State.World.generatorVersion, Is.EqualTo(3));
            Assert.That(loaded.State.World.miningChanges.Count, Is.EqualTo(1));
            Assert.That(loaded.State.World.changedTiles.Count, Is.EqualTo(1));
            Assert.That(loaded.State.World.buildings.Single().buildingTypeId, Is.EqualTo("building.support.basic"));
            Assert.That(loaded.State.World.gasChanges.Single().gasZoneId, Is.EqualTo("gas.1"));
            Assert.That(loaded.State.World.collapseChanges.Single().isCollapsed, Is.True);
            Assert.That(loaded.State.World.discoveredChunkIds, Is.EqualTo(new[] { "chunk.1" }));
            Assert.That(
                loaded.State.World.powerState.cableConnections.Single().nodeAInstanceId,
                Is.EqualTo("node.a"));
            Assert.That(loaded.State.TargetSceneName, Is.EqualTo(SceneNames.Integration));
        }

        [Test]
        public void M_F02_SeedPlusDeltas_ProduceMatchingOccupiedTileHash()
        {
            var host = Track(new GameObject("PhaseMHash"));
            var tilemapObject = new GameObject("Foreground");
            tilemapObject.transform.SetParent(host.transform);
            var tilemap = tilemapObject.AddComponent<Tilemap>();
            tilemapObject.AddComponent<TilemapRenderer>();
            var tile = Track(ScriptableObject.CreateInstance<Tile>());

            var generator = host.AddComponent<RecordingBaseGenerator>();
            generator.Tilemap = tilemap;
            generator.BaseTile = tile;

            var snapshot = host.AddComponent<WorldSnapshotSystem>();
            SetField(snapshot, "foregroundTilemap", tilemap);
            SetField(snapshot, "baseWorldGeneratorBehaviour", generator);
            snapshot.ConfigureBaseWorldIdentity(42L, 1);

            Assert.That(generator.Regenerate(42L, 1), Is.True);
            // 기본 월드: (0,0)(1,0) 타일. 채굴로 (1,0) 제거.
            tilemap.SetTile(new Vector3Int(1, 0, 0), null);
            snapshot.RecordMinedCell(1, 0, true, 0f);
            snapshot.RecordChangedTile(2, 0, "tile.rock.fractured", 0.5f);
            snapshot.RecordDiscoveredChunk("chunk.deep.1");

            var captured = snapshot.CaptureSnapshot();
            long beforeHash = snapshot.ComputeOccupiedTileHash();

            // 새 런타임처럼 버퍼를 비우고 동일 스냅샷을 복원한다.
            Assert.That(snapshot.RestoreSnapshot(captured), Is.True);
            long afterHash = snapshot.ComputeOccupiedTileHash();

            Assert.That(generator.CallCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(generator.LastSeed, Is.EqualTo(42L));
            Assert.That(afterHash, Is.EqualTo(beforeHash));
            Assert.That(captured.miningChanges.Count, Is.EqualTo(1));
            Assert.That(captured.changedTiles.Count, Is.EqualTo(1));
            Assert.That(captured.discoveredChunkIds, Is.EqualTo(new[] { "chunk.deep.1" }));
        }

        [Test]
        public void M_F03_CorruptOfficial_LoadsBackupWithoutOverwritingCorruptSource()
        {
            var save = CreateSaveService();
            var load = CreateLoadService();
            Assert.That(save.Save(1, CreateFullContext(100)).IsSuccess, Is.True);
            Assert.That(save.Save(1, CreateFullContext(200)).IsSuccess, Is.True);
            paths.TryGetPaths(1, out var slotPaths);
            const string corrupt = "{ broken official for phase m";
            File.WriteAllText(slotPaths.Normal, corrupt);

            var result = load.Load(1);

            Assert.That(result.Status, Is.EqualTo(LoadStatus.RecoveredFromBackup));
            Assert.That(result.State.GameState.Player.Gold, Is.EqualTo(100));
            Assert.That(File.ReadAllText(slotPaths.Normal), Is.EqualTo(corrupt));
        }

        [Test]
        public void M_F04_MidWritePromoteFailure_KeepsPreviousOfficialLoadable()
        {
            Assert.That(CreateSaveService().Save(1, CreateFullContext(111)).IsSuccess, Is.True);
            var faulting = new FaultingFileSystem(physical, FaultStage.PromoteMove);
            var failed = new SaveService(faulting, paths, mapper, json).Save(1, CreateFullContext(222));
            var loaded = CreateLoadService().Load(1);

            Assert.That(failed.Status, Is.EqualTo(SaveStatus.PromoteFailed));
            Assert.That(loaded.IsSuccess, Is.True);
            Assert.That(loaded.State.GameState.Player.Gold, Is.EqualTo(111));
        }

        [Test]
        public void M_F05_OldSaveVersion_MigratesMissingFieldsWithSafeDefaults()
        {
            Directory.CreateDirectory(testRoot);
            paths.TryGetPaths(1, out var slotPaths);
            var old = mapper.Capture(CreateFullContext(90));
            old.saveVersion = 1;
            old.targetSceneName = string.Empty;
            old.outpost = null;
            old.drone = null;
            old.world.generatorVersion = 0;
            File.WriteAllText(slotPaths.Normal, JsonUtility.ToJson(old, true));

            var migrated = CreateLoadService().Load(1);

            Assert.That(migrated.IsSuccess, Is.True);
            Assert.That(migrated.State.TargetSceneName, Is.EqualTo(SceneNames.Integration));
            Assert.That(migrated.State.Drone, Is.Not.Null);
            Assert.That(migrated.State.World.generatorVersion, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void M_F06_ActivationGate_StaysDisabledUntilDerivedThenActivatesOnce()
        {
            var gate = new IntegrationActivationGate();
            Assert.That(gate.TryActivateUi(), Is.False);

            gate.MarkStateReady();
            Assert.That(gate.TryActivateUi(), Is.False);

            gate.MarkWorldRestored();
            Assert.That(gate.TryActivateUi(), Is.False);

            gate.MarkDerivedRecalculated();
            Assert.That(gate.CanActivateUi, Is.True);
            Assert.That(gate.TryActivateUi(), Is.True);
            Assert.That(gate.IsUiActivated, Is.True);
            Assert.That(gate.TryActivateUi(), Is.False);
        }

        [Test]
        public void M_F06b_GeneratorVersionMismatch_SignalsWorldRestoreFailure()
        {
            var order = new List<string>();
            Assert.That(CreateSaveService().Save(1, CreateFullContext(33)).IsSuccess, Is.True);

            var continueService = new ContinueService(
                CreateLoadService(),
                new OrderedStateReceiver(order),
                new OrderedSceneLoader(order),
                new OrderedWorldResolver(order, succeed: false),
                new OrderedRecalculator(order),
                new OrderedUiGate(order));

            var result = continueService.Continue(1);

            Assert.That(result.Status, Is.EqualTo(ContinueStatus.WorldRestoreFailed));
            Assert.That(order, Does.Contain("world"));
            Assert.That(order, Does.Not.Contain("ui_on"));
        }

        [Test]
        public void M_WorldSnapshotSystem_GeneratorMismatch_ReturnsFalse()
        {
            var host = Track(new GameObject("PhaseMGenFail"));
            var generator = host.AddComponent<FailingBaseGenerator>();
            var snapshot = host.AddComponent<WorldSnapshotSystem>();
            SetField(snapshot, "baseWorldGeneratorBehaviour", generator);

            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex("Cannot restore world: generator version 99"));

            var ok = snapshot.RestoreSnapshot(new WorldSnapshotDto
            {
                worldSeed = 9,
                generatorVersion = 99
            });

            Assert.That(ok, Is.False);
            Assert.That(snapshot.LastRestoreSucceeded, Is.False);
            Assert.That(snapshot.LastRestoreFailureReason, Does.Contain("generator_version"));
        }

        private SaveService CreateSaveService()
        {
            return new SaveService(physical, paths, mapper, json);
        }

        private LoadService CreateLoadService()
        {
            return new LoadService(physical, paths, mapper, json);
        }

        private SaveCaptureContext CreateFullContext(
            int gold,
            IWorldSnapshotProvider world = null)
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
                new ProgressState(3, true, "objective.mine.copper", false),
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
                world ?? new ScriptedWorldProvider(),
                SceneNames.Integration,
                "0.2-mvp2-m");
        }

        private T Track<T>(T value) where T : UnityEngine.Object
        {
            created.Add(value);
            return value;
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
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
            public long UtcNowSeconds => 1_700_000_111;
        }

        private sealed class FixedDroneClock : IDroneClock
        {
            public double Now => 20d;
        }

        private sealed class ScriptedWorldProvider : IWorldSnapshotProvider
        {
            public WorldSnapshotDto CaptureSnapshot()
            {
                return new WorldSnapshotDto
                {
                    timestamp = 123,
                    worldSeed = 777,
                    generatorVersion = 3,
                    miningChanges = new List<MiningSnapshotDto>
                    {
                        new MiningSnapshotDto { x = 1, y = 2, isDestroyed = true }
                    },
                    changedTiles = new List<ChangedTileSnapshotDto>
                    {
                        new ChangedTileSnapshotDto
                        {
                            x = 2,
                            y = 3,
                            tileId = "tile.changed",
                            remainingDurability = 0.4f
                        }
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
                            x = 5,
                            y = -2,
                            level = 1,
                            health = 1f
                        }
                    },
                    gasChanges = new List<GasSnapshotDto>
                    {
                        new GasSnapshotDto
                        {
                            gasZoneId = "gas.1",
                            gasTypeId = "Toxic",
                            concentrationLevel = 0.4f,
                            remainingDuration = 8f,
                            isActive = true
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

            public bool RestoreSnapshot(WorldSnapshotDto snapshot) => true;
        }

        private sealed class OrderedStateReceiver : IRestoredStateReceiver
        {
            private readonly List<string> order;

            public OrderedStateReceiver(List<string> calls) => order = calls;

            public bool RestoreBState(RestoredSaveState state)
            {
                order.Add("state");
                return state != null;
            }
        }

        private sealed class OrderedSceneLoader : ISceneLoader
        {
            private readonly List<string> order;

            public OrderedSceneLoader(List<string> calls) => order = calls;

            public bool Load(string sceneName)
            {
                order.Add("scene");
                return !string.IsNullOrEmpty(sceneName);
            }
        }

        private sealed class OrderedWorldResolver : IWorldSnapshotResolver
        {
            private readonly List<string> order;
            private readonly bool succeed;

            public OrderedWorldResolver(List<string> calls, bool succeed)
            {
                order = calls;
                this.succeed = succeed;
            }

            public IWorldSnapshotProvider Resolve() => new OrderedWorld(order, succeed);
        }

        private sealed class OrderedWorld : IWorldSnapshotProvider
        {
            private readonly List<string> order;
            private readonly bool succeed;

            public OrderedWorld(List<string> calls, bool succeed)
            {
                order = calls;
                this.succeed = succeed;
            }

            public WorldSnapshotDto CaptureSnapshot() => new WorldSnapshotDto();

            public bool RestoreSnapshot(WorldSnapshotDto snapshot)
            {
                order.Add("world");
                return succeed;
            }
        }

        private sealed class OrderedRecalculator : IDerivedStateRecalculator
        {
            private readonly List<string> order;

            public OrderedRecalculator(List<string> calls) => order = calls;

            public bool Recalculate()
            {
                order.Add("derived");
                return true;
            }
        }

        private sealed class OrderedUiGate : ILoadedUiGate
        {
            private readonly List<string> order;

            public OrderedUiGate(List<string> calls) => order = calls;

            public void SetReady(bool ready)
            {
                order.Add(ready ? "ui_on" : "ui_off");
            }
        }

        private sealed class RecordingFileSystem : ISaveFileSystem
        {
            private readonly ISaveFileSystem inner;
            public List<string> Operations { get; } = new List<string>();

            public RecordingFileSystem(ISaveFileSystem fileSystem) => inner = fileSystem;

            public bool FileExists(string path) => inner.FileExists(path);

            public void CreateDirectory(string path)
            {
                Operations.Add("mkdir:" + Path.GetFileName(path));
                inner.CreateDirectory(path);
            }

            public void WriteAllText(string path, string contents)
            {
                Operations.Add("write:" + Path.GetFileName(path));
                inner.WriteAllText(path, contents);
            }

            public string ReadAllText(string path)
            {
                Operations.Add("read:" + Path.GetFileName(path));
                return inner.ReadAllText(path);
            }

            public void DeleteFile(string path)
            {
                Operations.Add("delete:" + Path.GetFileName(path));
                inner.DeleteFile(path);
            }

            public void MoveFile(string sourcePath, string destinationPath)
            {
                Operations.Add(
                    "move:" + Path.GetFileName(sourcePath) + "->" + Path.GetFileName(destinationPath));
                inner.MoveFile(sourcePath, destinationPath);
            }
        }

        private enum FaultStage
        {
            PromoteMove
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
            public void WriteAllText(string path, string contents) =>
                inner.WriteAllText(path, contents);
            public string ReadAllText(string path) => inner.ReadAllText(path);
            public void DeleteFile(string path) => inner.DeleteFile(path);

            public void MoveFile(string sourcePath, string destinationPath)
            {
                if (stage == FaultStage.PromoteMove && sourcePath.EndsWith(".tmp"))
                {
                    throw new IOException("promote fault");
                }

                inner.MoveFile(sourcePath, destinationPath);
            }
        }

        private sealed class RecordingBaseGenerator : MonoBehaviour, IWorldBaseGenerator
        {
            public Tilemap Tilemap;
            public TileBase BaseTile;
            public int CallCount { get; private set; }
            public long LastSeed { get; private set; }

            public bool Regenerate(long worldSeed, int generatorVersion)
            {
                CallCount++;
                LastSeed = worldSeed;
                if (Tilemap == null || BaseTile == null) return false;
                Tilemap.ClearAllTiles();
                Tilemap.SetTile(new Vector3Int(0, 0, 0), BaseTile);
                Tilemap.SetTile(new Vector3Int(1, 0, 0), BaseTile);
                return true;
            }
        }

        private sealed class FailingBaseGenerator : MonoBehaviour, IWorldBaseGenerator
        {
            public bool Regenerate(long worldSeed, int generatorVersion) => false;
        }
    }
}
