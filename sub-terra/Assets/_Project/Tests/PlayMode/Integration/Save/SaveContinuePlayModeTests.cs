using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.Save;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.Save
{
    public sealed class SaveContinuePlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameBootstrapper.ResetInstanceForTests();
            yield return null;
        }

        [UnityTest]
        public IEnumerator K_F07_NewSession_RestoresStateSceneWorldDerivedAndUiInOrder()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "subterra-k-play-" + Guid.NewGuid().ToString("N"));
            try
            {
                var order = new List<string>();
                var fileSystem = new PhysicalSaveFileSystem();
                var paths = new SavePathPolicy(root);
                var mapper = new SaveDataMapper(new FixedClock());
                var json = new SaveJsonCodec(new SaveMigrationService());
                var save = new SaveService(fileSystem, paths, mapper, json);
                var load = new LoadService(fileSystem, paths, mapper, json);

                var original = GameState.CreateNew();
                original.SetGold(432);
                original.SetDepth(55);
                var inventory = new InventoryState();
                var upgrades = new UpgradeState();
                upgrades.TryRestore(Array.Empty<UpgradeLevelState>());
                upgrades.TryRestoreUnlockedZones(Array.Empty<string>());
                var captureWorld = new CaptureWorldProvider();
                var context = new SaveCaptureContext(
                    original,
                    inventory,
                    upgrades,
                    null,
                    captureWorld,
                    SceneNames.Integration,
                    "play-test");
                Assert.That(save.Save(1, context).IsSuccess, Is.True);

                var receiver = new RecordingStateReceiver(order);
                var restoreWorld = new RestoreWorldProvider(order);
                var continueService = new ContinueService(
                    load,
                    receiver,
                    new RecordingSceneLoader(order),
                    new RecordingWorldResolver(order, restoreWorld),
                    new RecordingRecalculator(order),
                    new RecordingUiGate(order));

                var result = continueService.Continue(1);
                yield return null;

                Assert.That(result.Status, Is.EqualTo(ContinueStatus.Success));
                Assert.That(receiver.Restored, Is.Not.SameAs(original));
                Assert.That(receiver.Restored.GameState.Player.Gold, Is.EqualTo(432));
                Assert.That(receiver.Restored.GameState.Run.Depth, Is.EqualTo(55));
                Assert.That(restoreWorld.Restored.worldSeed, Is.EqualTo(9876));
                Assert.That(
                    order,
                    Is.EqualTo(new[]
                    {
                        "ui:false",
                        "state",
                        "scene",
                        "resolve-world",
                        "world",
                        "derived",
                        "ui:true"
                    }));
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        [Test]
        public void K_F07_SurfaceBaseContinue_DoesNotRequireWorldProvider()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "subterra-k-surface-" + Guid.NewGuid().ToString("N"));
            try
            {
                var order = new List<string>();
                var fileSystem = new PhysicalSaveFileSystem();
                var paths = new SavePathPolicy(root);
                var mapper = new SaveDataMapper(new FixedClock());
                var json = new SaveJsonCodec(new SaveMigrationService());
                var save = new SaveService(fileSystem, paths, mapper, json);
                var load = new LoadService(fileSystem, paths, mapper, json);
                var state = GameState.CreateNew();
                state.SetGold(321);
                state.SetDepth(12);

                Assert.That(
                    save.Save(
                        1,
                        new SaveCaptureContext(
                            state,
                            new InventoryState(),
                            new UpgradeState(),
                            null,
                            null,
                            SceneNames.SurfaceBase,
                            "play-test")).IsSuccess,
                    Is.True);

                var result = new ContinueService(
                    load,
                    new RecordingStateReceiver(order),
                    new RecordingSceneLoader(order, SceneNames.SurfaceBase),
                    new RecordingWorldResolver(order, null),
                    new RecordingRecalculator(order),
                    new RecordingUiGate(order)).Continue(1);

                Assert.That(result.Status, Is.EqualTo(ContinueStatus.Success));
                Assert.That(
                    order,
                    Is.EqualTo(new[] { "ui:false", "state", "scene", "ui:true" }));
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        [UnityTest]
        public IEnumerator K_F07_BootstrapLoadsInteractiveSaveMenu()
        {
            GameBootstrapper.ResetInstanceForTests();

            SceneManager.LoadScene(SceneNames.Bootstrap);
            yield return null;
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.MainMenu));
            Assert.That(SaveRuntimeController.Instance, Is.Not.Null);
            Assert.That(
                UnityEngine.Object.FindFirstObjectByType<SaveSlotPanelBinder>(FindObjectsInactive.Include),
                Is.Not.Null);
            Assert.That(
                UnityEngine.Object.FindFirstObjectByType<EventSystem>(),
                Is.Not.Null);
        }

        private sealed class FixedClock : ISaveClock
        {
            public long UtcNowSeconds => 1000;
        }

        private sealed class CaptureWorldProvider : IWorldSnapshotProvider
        {
            public WorldSnapshotDto CaptureSnapshot()
            {
                return new WorldSnapshotDto { worldSeed = 9876 };
            }

            public bool RestoreSnapshot(WorldSnapshotDto snapshot) => true;
        }

        private sealed class RestoreWorldProvider : IWorldSnapshotProvider
        {
            private readonly List<string> order;
            public WorldSnapshotDto Restored { get; private set; }

            public RestoreWorldProvider(List<string> calls)
            {
                order = calls;
            }

            public WorldSnapshotDto CaptureSnapshot() => null;

            public bool RestoreSnapshot(WorldSnapshotDto snapshot)
            {
                order.Add("world");
                Restored = snapshot;
                return true;
            }
        }

        private sealed class RecordingStateReceiver : IRestoredStateReceiver
        {
            private readonly List<string> order;
            public RestoredSaveState Restored { get; private set; }

            public RecordingStateReceiver(List<string> calls)
            {
                order = calls;
            }

            public bool RestoreBState(RestoredSaveState state)
            {
                order.Add("state");
                Restored = state;
                return true;
            }
        }

        private sealed class RecordingSceneLoader : ISceneLoader
        {
            private readonly List<string> order;
            private readonly string expectedScene;

            public RecordingSceneLoader(
                List<string> calls,
                string expected = SceneNames.Integration)
            {
                order = calls;
                expectedScene = expected;
            }

            public bool Load(string sceneName)
            {
                order.Add("scene");
                return sceneName == expectedScene;
            }
        }

        private sealed class RecordingWorldResolver : IWorldSnapshotResolver
        {
            private readonly List<string> order;
            private readonly IWorldSnapshotProvider provider;

            public RecordingWorldResolver(
                List<string> calls,
                IWorldSnapshotProvider snapshotProvider)
            {
                order = calls;
                provider = snapshotProvider;
            }

            public IWorldSnapshotProvider Resolve()
            {
                order.Add("resolve-world");
                return provider;
            }
        }

        private sealed class RecordingRecalculator : IDerivedStateRecalculator
        {
            private readonly List<string> order;

            public RecordingRecalculator(List<string> calls)
            {
                order = calls;
            }

            public bool Recalculate()
            {
                order.Add("derived");
                return true;
            }
        }

        private sealed class RecordingUiGate : ILoadedUiGate
        {
            private readonly List<string> order;

            public RecordingUiGate(List<string> calls)
            {
                order = calls;
            }

            public void SetReady(bool ready)
            {
                order.Add("ui:" + ready.ToString().ToLowerInvariant());
            }
        }
    }
}
