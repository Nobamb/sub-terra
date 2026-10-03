using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;
using SubTerra.Shared;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.Debug
{
    public sealed class PromptB125AdvancedCommandTests
    {
        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
        private SaveRuntimeController previousRuntime;

        [SetUp]
        public void RememberRuntime()
        {
            previousRuntime = SaveRuntimeController.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(created[i]);
                }
            }

            created.Clear();
            SetRuntimeInstance(previousRuntime);
        }

        [Test]
        public void Upgrade_RequiresSelection_AndClampsAddedLevels()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var state = new UpgradeState();
            var drill = CreateUpgrade(DataIds.Upgrades.DrillSpeed, 3);
            var service = new ProgressionService(state, new Catalog(drill), null);
            var purchases = 0;
            service.PurchaseCompleted += _ => purchases++;
            service.SetUpgradeLevelAbsolute(DataIds.Upgrades.DrillSpeed, 2);

            var missing = Context(null, service, string.Empty, null);
            Assert.That(session.Execute("upgrade", missing), Is.EqualTo("업그레이드 창에서 항목을 선택"));
            Assert.That(session.Execute("업그레이드 2", missing), Is.EqualTo("업그레이드 창에서 항목을 선택"));
            Assert.That(state.GetLevel(DataIds.Upgrades.DrillSpeed), Is.EqualTo(2));

            var selected = Context(null, service, DataIds.Upgrades.DrillSpeed, null);
            Assert.That(
                session.Execute("upgrade", selected),
                Is.EqualTo("upgrade " + DataIds.Upgrades.DrillSpeed + " 2 -> 3"));
            Assert.That(
                session.Execute("UPGRADE 2", selected),
                Is.EqualTo("upgrade " + DataIds.Upgrades.DrillSpeed + " 3 -> 3"));
            Assert.That(session.Execute("upgrade full", selected), Does.Contain("3 -> 3"));
            Assert.That(session.Execute("upgrade clear", selected), Does.Contain("3 -> 0"));
            Assert.That(session.Execute("upgrade 0", selected), Does.Contain("인자 오류"));
            Assert.That(session.Execute("upgrade -1", selected), Does.Contain("인자 오류"));
            Assert.That(state.GetLevel(DataIds.Upgrades.DrillSpeed), Is.Zero);
            Assert.That(purchases, Is.Zero);
        }

        [Test]
        public void Upgrade_FullAll_UsesEachMax_ClearAll_KeepsDeepZone()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var state = new UpgradeState();
            Assert.That(state.TryRestoreUnlockedZones(new[] { DataIds.Zones.Deep, "zone.keep" }), Is.True);
            var service = new ProgressionService(
                state,
                new Catalog(
                    CreateUpgrade(DataIds.Upgrades.DrillSpeed, 3),
                    CreateUpgrade(DataIds.Upgrades.DroneScan, 2),
                    CreateUpgrade(DataIds.Upgrades.DroneRescue, 2),
                    CreateUpgrade(DataIds.Upgrades.GasResistance, 3)),
                null);
            service.SetUpgradeLevelAbsolute(DataIds.Upgrades.DrillSpeed, 1);
            var context = Context(null, service, string.Empty, null);

            Assert.That(session.Execute("upgrade fullall", context), Is.EqualTo("fullall 4"));
            Assert.That(state.GetLevel(DataIds.Upgrades.DrillSpeed), Is.EqualTo(3));
            Assert.That(state.GetLevel(DataIds.Upgrades.DroneScan), Is.EqualTo(2));
            Assert.That(state.GetLevel(DataIds.Upgrades.DroneRescue), Is.EqualTo(2));
            Assert.That(state.GetLevel(DataIds.Upgrades.GasResistance), Is.EqualTo(3));
            Assert.That(state.IsZoneUnlocked(DataIds.Zones.Deep), Is.True);

            Assert.That(session.Execute("업그레이드 clearall", context), Is.EqualTo("clearall 4"));
            Assert.That(state.GetLevel(DataIds.Upgrades.DrillSpeed), Is.Zero);
            Assert.That(state.GetLevel(DataIds.Upgrades.DroneScan), Is.Zero);
            Assert.That(state.GetLevel(DataIds.Upgrades.DroneRescue), Is.Zero);
            Assert.That(state.GetLevel(DataIds.Upgrades.GasResistance), Is.Zero);
            Assert.That(state.IsZoneUnlocked(DataIds.Zones.Deep), Is.True);
            Assert.That(state.IsZoneUnlocked("zone.keep"), Is.True);
        }

        [Test]
        public void DeepUnlock_RaisesAccessWithoutPurchase()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var state = new UpgradeState();
            var service = new ProgressionService(
                state,
                new Catalog(
                    CreateUpgrade(DataIds.Upgrades.DrillSpeed, 3),
                    CreateUpgrade(DataIds.Upgrades.DroneScan, 2),
                    CreateUpgrade(DataIds.Upgrades.GasResistance, 3)),
                null);
            var purchases = 0;
            service.PurchaseCompleted += _ => purchases++;
            var context = Context(null, service, null, null);

            Assert.That(session.Execute("심층지역해금", context), Is.EqualTo("심층 지역 해금"));
            Assert.That(state.IsZoneUnlocked(DataIds.Zones.Deep), Is.True);
            Assert.That(state.GetLevel(DataIds.Upgrades.DrillSpeed), Is.EqualTo(2));
            Assert.That(state.GetLevel(DataIds.Upgrades.DroneScan), Is.EqualTo(2));
            Assert.That(state.GetLevel(DataIds.Upgrades.GasResistance), Is.EqualTo(1));
            Assert.That(session.Execute("deepunlock extra", context), Does.Contain("인자 오류"));
            Assert.That(purchases, Is.Zero);
        }

        [Test]
        public void MineCost_FreeDoesNotTouchPaidCount_AndNextFeeIs1000()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var state = GameState.CreateNew();
            state.SetGold(0);
            var context = Context(state, null, null, null);

            Assert.That(session.Execute("광산비용 공짜", context), Is.EqualTo("minecost 0"));
            Assert.That(state.MineResetCycle.PaidResetCount, Is.Zero);
            Assert.That(MineResetService.GetFeeGold(state), Is.EqualTo(500));

            var cache = new MineWorldCache();
            var reset = MineResetService.TryReset(state, cache, new SequenceSeedSource(99), out var result);

            Assert.That(reset, Is.True);
            Assert.That(result.FeeCharged, Is.Zero);
            Assert.That(state.Player.Gold, Is.Zero);
            Assert.That(state.MineResetCycle.PaidResetCount, Is.EqualTo(1));
            Assert.That(MineResetService.GetFeeGold(state), Is.EqualTo(1000));

            state.SetGold(250);
            Assert.That(session.Execute("minecost 250", context), Is.EqualTo("minecost 250"));
            Assert.That(state.MineResetCycle.PaidResetCount, Is.EqualTo(1));
            var second = MineResetService.TryReset(state, cache, new SequenceSeedSource(100), out var priced);
            Assert.That(second, Is.True);
            Assert.That(priced.FeeCharged, Is.EqualTo(250));
            Assert.That(state.Player.Gold, Is.Zero);
            Assert.That(MineResetService.GetFeeGold(state), Is.EqualTo(2000));
        }

        [Test]
        public void Timer_SignAndUnitChangeElapsed_EndUsesPopupReset()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var state = GameState.CreateNew();
            var runtime = CreateRuntime(state);
            runtime.SetMineResetElapsedSeconds(1000d);
            var context = Context(state, null, null, runtime);

            Assert.That(session.Execute("타이머 -10", context), Is.EqualTo("timer 1000 -> 400 remaining 02:53:20"));
            Assert.That(session.Execute("timer 2h", context), Is.EqualTo("timer 400 -> 7600 remaining 00:53:20"));
            Assert.That(session.Execute("timer 30 sec", context), Is.EqualTo("timer 7600 -> 7630 remaining 00:52:50"));
            Assert.That(session.Execute("timer 1 분", context), Is.EqualTo("timer 7630 -> 7690 remaining 00:51:50"));
            Assert.That(session.Execute("timer", context), Does.Contain("인자 오류"));
            Assert.That(state.MineResetCycle.ElapsedSeconds, Is.EqualTo(7690d));

            var ended = session.Execute("timer end", context);
            Assert.That(ended, Is.EqualTo("timer 7690 -> 0 remaining 03:00:00"));
            Assert.That(state.MineResetCycle.ElapsedSeconds, Is.Zero);
            Assert.That(state.MineResetCycle.PaidResetCount, Is.Zero);
            Assert.That(MineResetService.GetFeeGold(state), Is.EqualTo(500));
            Assert.That(PopupActive(runtime), Is.True);
        }

        [Test]
        public void InitMine_ResetsWithoutPopup()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var state = GameState.CreateNew();
            state.SetGold(40);
            var runtime = CreateRuntime(state);
            runtime.SetMineResetElapsedSeconds(80d);
            var context = Context(state, null, null, runtime);

            Assert.That(session.Execute("광산초기화", context), Is.EqualTo("광산 초기화"));
            Assert.That(state.Player.Gold, Is.EqualTo(40));
            Assert.That(state.MineResetCycle.ElapsedSeconds, Is.Zero);
            Assert.That(state.MineResetCycle.PaidResetCount, Is.Zero);
            Assert.That(MineResetService.GetFeeGold(state), Is.EqualTo(500));
            Assert.That(runtime.PeekMineWorldCache(), Is.Not.Null);
            Assert.That(runtime.PeekMineWorldCache().worldSeed, Is.Not.EqualTo(0));
            Assert.That(PopupActive(runtime), Is.False);
            Assert.That(session.Execute("initmine now", context), Does.Contain("인자 오류"));
        }

        [Test]
        public void MinePayment_OffSurface_DoesNotSpendOrOverride()
        {
            Assert.That(SceneManager.GetActiveScene().name, Is.Not.EqualTo(SceneNames.SurfaceBase));
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var state = GameState.CreateNew();
            state.SetGold(0);
            var context = Context(state, null, null, null);

            Assert.That(session.Execute("minepayment 2 costfree", context), Does.Contain("지상 기지"));
            Assert.That(session.Execute("광산지불 0.2", context), Does.Contain("지상 기지"));
            Assert.That(session.Execute("minepayment nope", context), Does.Contain("인자 오류"));
            Assert.That(state.Player.Gold, Is.Zero);
            Assert.That(state.MineResetCycle.PaidResetCount, Is.Zero);

            var denied = MineResetService.TryReset(
                state,
                new MineWorldCache(),
                new SequenceSeedSource(11),
                out var result);
            Assert.That(denied, Is.False);
            Assert.That(result.Status, Is.EqualTo(MineResetStatus.InsufficientGold));
            Assert.That(result.FeeCharged, Is.EqualTo(500));
        }

        [Test]
        public void MinePayment_OnSurface_StopsOnGold_AndCostFreeRepeats()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var state = GameState.CreateNew();
            state.SetGold(500);
            var runtime = CreateRuntime(state);
            UseMemorySave(runtime);
            var context = Context(state, null, null, runtime);

            using (new SurfaceSceneScope())
            {
                Assert.That(
                    session.Execute("minepayment 2.5", context),
                    Is.EqualTo("광산 지불 1회"));
                Assert.That(state.Player.Gold, Is.Zero);
                Assert.That(state.MineResetCycle.PaidResetCount, Is.EqualTo(1));
                Assert.That(MineResetService.GetFeeGold(state), Is.EqualTo(1000));

                state.SetGold(1000);
                var stopped = session.Execute("minepayment 2", context);
                Assert.That(stopped, Is.EqualTo("골드가 부족합니다. 1회 완료"));
                Assert.That(state.Player.Gold, Is.Zero);
                Assert.That(state.MineResetCycle.PaidResetCount, Is.EqualTo(2));
                Assert.That(MineResetService.GetFeeGold(state), Is.EqualTo(2000));

                var free = session.Execute("광산지불 2 costfree", context);
                Assert.That(free, Is.EqualTo("광산 지불 2회"));
                Assert.That(state.Player.Gold, Is.Zero);
                Assert.That(state.MineResetCycle.PaidResetCount, Is.EqualTo(4));
                Assert.That(MineResetService.GetFeeGold(state), Is.EqualTo(8000));
            }
        }

        [Test]
        public void Help_ListsAdvancedCommands_AndSourcesStayGated()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var help = session.Execute("help", null);
            Assert.That(help, Does.Contain("upgrade [n|full|fullall|clear|clearall]"));
            Assert.That(help, Does.Contain("deepunlock"));
            Assert.That(help, Does.Contain("initmine"));
            Assert.That(help, Does.Contain("timer <n> [sec|min|hour] | end"));
            Assert.That(help, Does.Contain("minecost <n|free|공짜>"));
            Assert.That(help, Does.Contain("minepayment [N] [costfree]"));
            Assert.That(help, Does.Contain("심층지역해금"));
            Assert.That(help, Does.Contain("광산초기화"));
            Assert.That(help, Does.Contain("예: timer -10"));

            var advanced = File.ReadAllText(SourcePath(
                "_Project/Scripts/App/Integration/Debug/DeveloperDebugAdvancedCommands.cs"));
            var registry = File.ReadAllText(SourcePath(
                "_Project/Scripts/App/Integration/Debug/DeveloperDebugCommandRegistry.cs"));
            Assert.That(advanced, Does.Contain("#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT"));
            Assert.That(advanced, Does.Contain("SetUpgradeLevelAbsolute"));
            Assert.That(advanced, Does.Contain("ForceUnlockDeepZone"));
            Assert.That(advanced, Does.Contain("SetNextPaidResetFeeOverride"));
            Assert.That(advanced, Does.Contain("SetMineResetElapsedSeconds"));
            Assert.That(advanced, Does.Contain("ResetMineWithoutPopup()"));
            Assert.That(advanced, Does.Contain("TryResetMine"));
            Assert.That(advanced, Does.Not.Contain("ShowTimedResetPopup"));
            Assert.That(advanced, Does.Not.Contain("TryTimedReset"));
            Assert.That(advanced, Does.Not.Contain("TryReset("));
            Assert.That(advanced, Does.Not.Contain("TryPurchase"));
            Assert.That(advanced, Does.Not.Contain("PaidResetCount"));
            Assert.That(registry, Does.Not.Contain("SetUpgradeLevelAbsolute"));
            Assert.That(registry, Does.Not.Contain("ResetMineWithoutPopup"));
            Assert.That(registry, Does.Contain("ProgressionPanelBinder"));
            Assert.That(registry, Does.Contain("SelectedUpgradeId"));
        }

        private SaveRuntimeController CreateRuntime(GameState state)
        {
            SetRuntimeInstance(null);
            var host = new GameObject("PromptB125AdvancedRuntime");
            created.Add(host);
            var runtime = host.AddComponent<SaveRuntimeController>();
            typeof(SaveRuntimeController)
                .GetField("boundState", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(runtime, state);
            typeof(SaveRuntimeController)
                .GetField("activeSlot", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(runtime, 0);
            return runtime;
        }

        private static void UseMemorySave(SaveRuntimeController runtime)
        {
            var service = new SaveService(
                new MemoryFileSystem(),
                new SavePathPolicy(Path.Combine(Path.GetTempPath(), "subterra-b125-advanced")),
                new SaveDataMapper(new SystemSaveClock()),
                new SaveJsonCodec(new SaveMigrationService()));
            typeof(SaveRuntimeController)
                .GetField("saveService", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(runtime, service);
            typeof(SaveRuntimeController)
                .GetField("activeSlot", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(runtime, 1);
        }

        private static bool PopupActive(SaveRuntimeController runtime)
        {
            var overlay = runtime.GetComponentInChildren<MineResetClockOverlay>(true);
            if (overlay == null)
            {
                return false;
            }

            var popup = overlay.transform.Find("TimedResetPopup");
            return popup != null && popup.gameObject.activeSelf;
        }

        private static DeveloperDebugCommandContext Context(
            GameState state,
            ProgressionService progression,
            string selectedUpgradeId,
            SaveRuntimeController runtime)
        {
            return new DeveloperDebugCommandContext(
                state,
                null,
                null,
                null,
                () => progression,
                () => selectedUpgradeId,
                () => runtime);
        }

        private static void SetRuntimeInstance(SaveRuntimeController value)
        {
            typeof(SaveRuntimeController)
                .GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)
                .SetValue(null, value);
        }

        private UpgradeData CreateUpgrade(string id, int maximumLevel)
        {
            var levels = new List<UpgradeLevelDefinition>();
            for (var level = 1; level <= maximumLevel; level++)
            {
                levels.Add(new UpgradeLevelDefinition(
                    level,
                    level,
                    new List<ItemCostEntry>
                    {
                        new ItemCostEntry(DataIds.Minerals.Copper, level)
                    }));
            }

            var data = ScriptableObject.CreateInstance<UpgradeData>();
            created.Add(data);
            data.EditorSet(id, id, maximumLevel, levels);
            return data;
        }

        private static string SourcePath(string assetRelativePath)
        {
            return Path.Combine(Application.dataPath, assetRelativePath);
        }

        private sealed class SurfaceSceneScope : IDisposable
        {
            private const string SurfacePath = "Assets/_Project/Scenes/App/SurfaceBase.unity";
            private readonly Scene previous;
            private readonly Scene created;
            private readonly bool createdNew;

            public SurfaceSceneScope()
            {
                previous = SceneManager.GetActiveScene();
                created = FindLoadedSurface();
                createdNew = !created.IsValid();
                if (createdNew)
                {
                    created = EditorSceneManager.OpenScene(SurfacePath, OpenSceneMode.Additive);
                }

                if (!created.IsValid() || !SceneManager.SetActiveScene(created))
                {
                    throw new InvalidOperationException("SurfaceBase scene was not activated.");
                }
            }

            public void Dispose()
            {
                if (previous.IsValid() && previous.isLoaded)
                {
                    SceneManager.SetActiveScene(previous);
                }

                if (createdNew && created.IsValid() && created.isLoaded)
                {
                    EditorSceneManager.CloseScene(created, true);
                }
            }

            private static Scene FindLoadedSurface()
            {
                for (var i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded && scene.name == SceneNames.SurfaceBase)
                    {
                        return scene;
                    }
                }

                return default;
            }
        }

        private sealed class SequenceSeedSource : IMineResetSeedSource
        {
            private readonly long[] seeds;
            private int index;

            public SequenceSeedSource(params long[] values)
            {
                seeds = values;
            }

            public long NextSeed()
            {
                if (seeds == null || seeds.Length == 0)
                {
                    return 0;
                }

                var value = seeds[index < seeds.Length ? index : seeds.Length - 1];
                index++;
                return value;
            }
        }

        private sealed class Catalog : IUpgradeCatalog
        {
            private readonly List<UpgradeData> upgrades;

            public Catalog(params UpgradeData[] upgrades)
            {
                this.upgrades = new List<UpgradeData>(upgrades);
            }

            public IReadOnlyList<UpgradeData> Upgrades => upgrades;

            public bool TryGetUpgrade(string upgradeId, out UpgradeData data)
            {
                for (var i = 0; i < upgrades.Count; i++)
                {
                    if (upgrades[i] != null && upgrades[i].Id == upgradeId)
                    {
                        data = upgrades[i];
                        return true;
                    }
                }

                data = null;
                return false;
            }
        }

        private sealed class MemoryFileSystem : ISaveFileSystem
        {
            private readonly Dictionary<string, string> files =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public bool FileExists(string path)
            {
                return files.ContainsKey(path);
            }

            public void CreateDirectory(string path)
            {
            }

            public void WriteAllText(string path, string contents)
            {
                files[path] = contents ?? string.Empty;
            }

            public string ReadAllText(string path)
            {
                return files[path];
            }

            public void DeleteFile(string path)
            {
                files.Remove(path);
            }

            public void MoveFile(string sourcePath, string destinationPath)
            {
                files[destinationPath] = files[sourcePath];
                files.Remove(sourcePath);
            }
        }
    }
}
