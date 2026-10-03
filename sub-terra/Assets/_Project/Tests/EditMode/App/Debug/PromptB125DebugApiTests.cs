using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.Debug
{
    public sealed class PromptB125DebugApiTests
    {
        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

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
            UiPauseGate.Release(SaveRuntimeController.DebugTerminalPauseOwner);
        }

        [Test]
        public void SetHealthAbsolute_Zero_DoesNotRequestFailure()
        {
            var host = new GameObject("PromptB125Health");
            created.Add(host);
            var controller = host.AddComponent<PlayerSurvivalController>();
            var settings = typeof(PlayerSurvivalController)
                .GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(controller) as PlayerSurvivalSettings;
            if (settings != null)
            {
                created.Add(settings);
            }

            var failures = 0;
            var healthEvents = 0;
            controller.FailureRequested += _ => failures++;
            controller.HealthChanged += _ => healthEvents++;

            controller.SetHealthAbsolute(0);

            Assert.That(failures, Is.Zero);
            Assert.That(healthEvents, Is.EqualTo(1));
            Assert.That(controller.State.Health, Is.EqualTo(0f));
            Assert.That(controller.State.CanAct, Is.False);

            controller.SetHealthAbsolute(-20);
            Assert.That(controller.State.Health, Is.EqualTo(0f));
            Assert.That(controller.State.CanAct, Is.False);
            Assert.That(failures, Is.Zero);

            controller.SetHealthAbsolute(100000);
            Assert.That(controller.State.Health, Is.EqualTo(controller.State.MaximumHealth));
            Assert.That(controller.State.CanAct, Is.True);
            Assert.That(failures, Is.Zero);
            Assert.That(healthEvents, Is.EqualTo(3));
        }

        [Test]
        public void SetUpgradeLevelAbsolute_Clamps_Publishes_KeepsZoneUnlocks()
        {
            var state = new UpgradeState();
            Assert.That(state.TryRestoreUnlockedZones(new[] { "zone.keep" }), Is.True);
            var drill = CreateUpgrade(DataIds.Upgrades.DrillSpeed, 3);
            var service = new ProgressionService(state, new Catalog(drill), null);
            var changed = new List<string>();
            var purchases = 0;
            service.UpgradeChanged += snapshot => changed.Add(snapshot.UpgradeId);
            service.PurchaseCompleted += _ => purchases++;

            Assert.That(service.SetUpgradeLevelAbsolute(DataIds.Upgrades.DrillSpeed, 99), Is.True);
            Assert.That(state.GetLevel(DataIds.Upgrades.DrillSpeed), Is.EqualTo(3));
            Assert.That(service.SetUpgradeLevelAbsolute(DataIds.Upgrades.DrillSpeed, -4), Is.True);
            Assert.That(state.GetLevel(DataIds.Upgrades.DrillSpeed), Is.Zero);
            Assert.That(service.SetUpgradeLevelAbsolute("upgrade.missing", 2), Is.False);
            Assert.That(state.Levels.Count, Is.EqualTo(1));

            Assert.That(changed, Is.EqualTo(new[]
            {
                DataIds.Upgrades.DrillSpeed,
                DataIds.Upgrades.DrillSpeed
            }));
            Assert.That(purchases, Is.Zero);
            Assert.That(state.IsZoneUnlocked("zone.keep"), Is.True);
            Assert.That(state.IsZoneUnlocked(DataIds.Zones.Deep), Is.False);
        }

        [Test]
        public void ForceUnlockDeepZone_RaisesRequirements_AndUnlocksDeep()
        {
            var state = new UpgradeState();
            Assert.That(state.TryRestoreUnlockedZones(new[] { "zone.keep" }), Is.True);
            Assert.That(
                state.TryRestore(new[]
                {
                    new UpgradeLevelState(DataIds.Upgrades.DrillSpeed, 3),
                    new UpgradeLevelState(DataIds.Upgrades.GasResistance, 2)
                }),
                Is.True);
            var service = new ProgressionService(
                state,
                new Catalog(
                    CreateUpgrade(DataIds.Upgrades.DrillSpeed, 3),
                    CreateUpgrade(DataIds.Upgrades.DroneScan, 2),
                    CreateUpgrade(DataIds.Upgrades.GasResistance, 3)),
                null);
            var changed = new List<string>();
            ZoneAccessResult access = default;
            var accessEvents = 0;
            var purchases = 0;
            service.UpgradeChanged += snapshot => changed.Add(snapshot.UpgradeId);
            service.DeepZoneAccessChanged += result =>
            {
                accessEvents++;
                access = result;
            };
            service.PurchaseCompleted += _ => purchases++;

            service.ForceUnlockDeepZone();

            Assert.That(state.GetLevel(DataIds.Upgrades.DrillSpeed), Is.EqualTo(3));
            Assert.That(state.GetLevel(DataIds.Upgrades.DroneScan), Is.EqualTo(2));
            Assert.That(state.GetLevel(DataIds.Upgrades.GasResistance), Is.EqualTo(2));
            Assert.That(state.IsZoneUnlocked(DataIds.Zones.Deep), Is.True);
            Assert.That(state.IsZoneUnlocked("zone.keep"), Is.True);
            Assert.That(changed, Is.EquivalentTo(new[]
            {
                DataIds.Upgrades.DrillSpeed,
                DataIds.Upgrades.DroneScan,
                DataIds.Upgrades.GasResistance
            }));
            Assert.That(accessEvents, Is.EqualTo(1));
            Assert.That(access.IsUnlocked, Is.True);
            Assert.That(access.DidUnlockNow, Is.True);
            Assert.That(purchases, Is.Zero);
        }

        [Test]
        public void FreePaidResetOverride_NextFeeIs1000()
        {
            var state = GameState.CreateNew();
            state.SetGold(0);
            state.AddMineResetElapsed(15d);
            var cache = new MineWorldCache();
            cache.ReplaceFromProvider(new WorldSnapshotDto
            {
                worldSeed = 41,
                generatorVersion = 1
            });
            var seeds = new SequenceSeedSource(99, 100);

            MineResetService.SetNextPaidResetFeeOverride(state, 0);
            Assert.That(MineResetService.GetFeeGold(state), Is.EqualTo(500));
            Assert.That(state.MineResetCycle.PaidResetCount, Is.Zero);

            var success = MineResetService.TryReset(state, cache, seeds, out var first);

            Assert.That(success, Is.True);
            Assert.That(first.Status, Is.EqualTo(MineResetStatus.Success));
            Assert.That(first.FeeCharged, Is.Zero);
            Assert.That(first.RemainingGold, Is.Zero);
            Assert.That(state.Player.Gold, Is.Zero);
            Assert.That(state.MineResetCycle.PaidResetCount, Is.EqualTo(1));
            Assert.That(state.MineResetCycle.ElapsedSeconds, Is.Zero);
            Assert.That(MineResetService.GetFeeGold(state), Is.EqualTo(1000));
            Assert.That(cache.Peek().worldSeed, Is.EqualTo(99));

            state.SetGold(1000);
            var secondOk = MineResetService.TryReset(state, cache, seeds, out var second);

            Assert.That(secondOk, Is.True);
            Assert.That(second.FeeCharged, Is.EqualTo(1000));
            Assert.That(state.Player.Gold, Is.Zero);
            Assert.That(state.MineResetCycle.PaidResetCount, Is.EqualTo(2));
            Assert.That(MineResetService.GetFeeGold(state), Is.EqualTo(2000));
        }

        [Test]
        public void SetMineResetElapsedSeconds_WritesWhilePaused()
        {
            var scale = Time.timeScale;
            var acquired = UiPauseGate.Acquire(SaveRuntimeController.DebugTerminalPauseOwner);
            GameObject host = null;
            try
            {
                Assert.That(acquired, Is.True);
                typeof(SaveRuntimeController)
                    .GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, null);
                host = new GameObject("PromptB125MineReset");
                var runtime = host.AddComponent<SaveRuntimeController>();
                var state = GameState.CreateNew();
                typeof(SaveRuntimeController)
                    .GetField("boundState", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(runtime, state);

                runtime.SetMineResetElapsedSeconds(12d);
                Assert.That(state.MineResetCycle.ElapsedSeconds, Is.EqualTo(12d));

                UiPauseGate.Release(SaveRuntimeController.DebugTerminalPauseOwner);
                acquired = false;
                Time.timeScale = scale;

                state.AddMineResetElapsed(-5d);
                Assert.That(state.MineResetCycle.ElapsedSeconds, Is.EqualTo(12d));
                runtime.SetMineResetElapsedSeconds(double.NaN);
                Assert.That(state.MineResetCycle.ElapsedSeconds, Is.EqualTo(12d));
                runtime.SetMineResetElapsedSeconds(-3d);
                Assert.That(state.MineResetCycle.ElapsedSeconds, Is.Zero);
            }
            finally
            {
                if (acquired)
                {
                    UiPauseGate.Release(SaveRuntimeController.DebugTerminalPauseOwner);
                    Time.timeScale = scale;
                }

                if (host != null)
                {
                    UnityEngine.Object.DestroyImmediate(host);
                }
            }
        }

        [Test]
        public void DebugTerminalPause_BlocksSettingsEscape()
        {
            var scale = Time.timeScale;
            var surface = typeof(SurfaceBaseBinder).GetMethod(
                "CanOpenSettingsFromEscape",
                BindingFlags.NonPublic | BindingFlags.Static);
            var menu = typeof(MainMenuBinder).GetMethod(
                "CanOpenSettingsFromEscape",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(surface, Is.Not.Null);
            Assert.That(menu, Is.Not.Null);
            Assert.That((bool)surface.Invoke(null, null), Is.True);
            Assert.That((bool)menu.Invoke(null, null), Is.True);

            var acquired = UiPauseGate.Acquire(SaveRuntimeController.DebugTerminalPauseOwner);
            try
            {
                Assert.That(acquired, Is.True);
                Assert.That((bool)surface.Invoke(null, null), Is.False);
                Assert.That((bool)menu.Invoke(null, null), Is.False);
            }
            finally
            {
                if (acquired)
                {
                    UiPauseGate.Release(SaveRuntimeController.DebugTerminalPauseOwner);
                    Time.timeScale = scale;
                }
            }
        }

        [Test]
        public void CheatApis_CompileOnlyForEditorOrDevelopment()
        {
            AssertGated(
                "_Project/Scripts/Shared/Contracts/PlayerHealthContracts.cs",
                "void SetHealthAbsolute(int health)");
            AssertGated(
                "_Project/Scripts/Gameplay/Player/PlayerSurvivalController.cs",
                "void SetHealthAbsolute(int health)");
            AssertGated(
                "_Project/Scripts/Gameplay/Player/PlayerSurvivalState.cs",
                "void SetHealthAbsolute(int health)");
            AssertGated(
                "_Project/Scripts/App/Progression/ProgressionService.cs",
                "bool SetUpgradeLevelAbsolute(string upgradeId, int level)");
            AssertGated(
                "_Project/Scripts/App/Progression/ProgressionService.cs",
                "void ForceUnlockDeepZone()");
            AssertGated(
                "_Project/Scripts/App/Save/MineResetService.cs",
                "void SetNextPaidResetFeeOverride(GameState state, int feeGold)");
            AssertGated(
                "_Project/Scripts/App/Save/MineResetService.cs",
                "TryPeekNextPaidResetFeeOverride");
            AssertGated(
                "_Project/Scripts/App/State/GameState.cs",
                "void SetNextPaidResetFeeOverride(int feeGold)");
            AssertGated(
                "_Project/Scripts/App/State/MineResetCycleState.cs",
                "bool SetElapsedSeconds(double elapsedSeconds)");
            AssertGated(
                "_Project/Scripts/App/Save/SaveRuntimeController.cs",
                "DebugTerminalPauseOwner");
            AssertGated(
                "_Project/Scripts/App/Save/SaveRuntimeController.cs",
                "void SetMineResetElapsedSeconds(double elapsedSeconds)");
            AssertGated(
                "_Project/Scripts/App/Save/SaveRuntimeController.cs",
                "void ResetMineWithoutPopup()");
            AssertGated(
                "_Project/Scripts/App/UI/SurfaceBase/SurfaceBaseBinder.cs",
                "DebugTerminalPauseOwner");
            AssertGated(
                "_Project/Scripts/App/UI/MainMenu/MainMenuBinder.cs",
                "DebugTerminalPauseOwner");

            var controller = File.ReadAllText(SourcePath(
                "_Project/Scripts/App/Save/SaveRuntimeController.cs"));
            var setter = Slice(
                controller,
                "void SetMineResetElapsedSeconds",
                "void ResetMineWithoutPopup");
            Assert.That(setter, Does.Contain("ExecuteTimedMineReset()"));
            Assert.That(setter, Does.Not.Contain("TryTimedReset"));
            Assert.That(setter, Does.Not.Contain("TryReset("));
            var silent = Slice(controller, "void ResetMineWithoutPopup", "#endif");
            Assert.That(silent, Does.Contain("ExecuteTimedMineReset(false)"));
            Assert.That(silent, Does.Not.Contain("ShowTimedResetPopup"));
            Assert.That(silent, Does.Not.Contain("TryReset("));
            var popup = controller.IndexOf("ShowTimedResetPopup", StringComparison.Ordinal);
            var guard = controller.LastIndexOf("if (showPopup)", popup, StringComparison.Ordinal);
            Assert.That(guard, Is.GreaterThanOrEqualTo(0));
        }

        private static void AssertGated(string assetRelativePath, string token)
        {
            var text = File.ReadAllText(SourcePath(assetRelativePath));
            var index = text.IndexOf(token, StringComparison.Ordinal);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), token + " in " + assetRelativePath);
            var ifIndex = text.LastIndexOf("#if", index, StringComparison.Ordinal);
            var endIndex = text.LastIndexOf("#endif", index, StringComparison.Ordinal);
            Assert.That(ifIndex, Is.GreaterThan(endIndex), token);
            var lineEnd = text.IndexOf('\n', ifIndex);
            if (lineEnd < 0)
            {
                lineEnd = text.Length;
            }

            var line = text.Substring(ifIndex, lineEnd - ifIndex).Trim().TrimEnd('\r');
            Assert.That(
                line,
                Is.EqualTo("#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT"),
                token);
        }

        private static string Slice(string text, string startToken, string endToken)
        {
            var start = text.IndexOf(startToken, StringComparison.Ordinal);
            var end = text.IndexOf(endToken, start + startToken.Length, StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), startToken);
            Assert.That(end, Is.GreaterThan(start), endToken);
            return text.Substring(start, end - start);
        }

        private static string SourcePath(string assetRelativePath)
        {
            return Path.Combine(Application.dataPath, assetRelativePath);
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

        private sealed class SequenceSeedSource : IMineResetSeedSource
        {
            private readonly long[] seeds;
            private int index;

            public SequenceSeedSource(params long[] seeds)
            {
                this.seeds = seeds;
            }

            public long NextSeed()
            {
                var value = seeds[index < seeds.Length ? index : seeds.Length - 1];
                index++;
                return value;
            }
        }
    }
}
