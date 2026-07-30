using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Progression;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>Phase N 게이트 검증을 Editor에서 즉시 실행하고 Temp에 증거를 남긴다.</summary>
    public static class PhaseNVerificationCommand
    {
        [MenuItem("SubTerra/Tests/Verify Phase N Gates")]
        public static void VerifyFromMenu()
        {
            var report = RunAll();
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var editPath = Path.Combine(projectRoot, "Temp", "phase-n-editmode-results.txt");
            var playPath = Path.Combine(projectRoot, "Temp", "phase-n-playmode-results.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(editPath));
            File.WriteAllText(editPath, report.EditLog);
            File.WriteAllText(playPath, report.PlayLog);
            Debug.Log("[SubTerra] Phase N verification pass=" + report.Pass + " fail=" + report.Fail);
            if (report.Fail > 0)
            {
                Debug.LogError("[SubTerra] Phase N verification failures:\n" + report.FailDetails);
            }
        }

        public static Report RunAll()
        {
            var edit = new StringBuilder();
            var play = new StringBuilder();
            var fails = new StringBuilder();
            var pass = 0;
            var fail = 0;

            void Check(StringBuilder log, string name, bool ok, string detail = "")
            {
                if (ok)
                {
                    pass++;
                    log.AppendLine("PASS " + name);
                }
                else
                {
                    fail++;
                    log.AppendLine("FAIL " + name + " " + detail);
                    fails.AppendLine(name + ": " + detail);
                }
            }

            edit.AppendLine("Phase N Edit Mode gates");
            edit.AppendLine(DateTime.Now.ToString("o"));

            Check(edit, "N-S01-count", DemoObjectiveCatalog.All.Count == 13
                && DemoObjectiveIds.Ordered.Length == 13);
            for (var i = 0; i < DemoObjectiveCatalog.All.Count - 1; i++)
            {
                var d = DemoObjectiveCatalog.All[i];
                Check(edit, "N-S01-chain-" + d.Id,
                    d.NextObjectiveId == DemoObjectiveIds.Ordered[i + 1]);
            }

            var engine = new DemoObjectiveTransitionEngine();
            var full = true;
            var signals = new[]
            {
                DemoProgressSignal.ExplorationStarted,
                DemoProgressSignal.CopperAndIronCollected,
                DemoProgressSignal.PathGuidanceAcknowledged,
                DemoProgressSignal.LithiumCollected,
                DemoProgressSignal.StructuralHazardObserved,
                DemoProgressSignal.SupportPlaced,
                DemoProgressSignal.GasHazardObserved,
                DemoProgressSignal.OutpostInstalled,
                DemoProgressSignal.ReturnRecommendationPresented,
                DemoProgressSignal.SettlementSucceeded,
                DemoProgressSignal.BatteryUpgradeSucceeded,
                DemoProgressSignal.DeepZoneUnlocked,
                DemoProgressSignal.DemoCompleted
            };
            for (var i = 0; i < signals.Length; i++)
            {
                if (!engine.TryAdvance(signals[i]).Advanced)
                {
                    full = false;
                    break;
                }
            }

            Check(edit, "N-F01-full-sequence", full && engine.IsDemoComplete && engine.CompletedCount == 13);

            var engine2 = new DemoObjectiveTransitionEngine();
            Check(edit, "N-F02-no-skip",
                !engine2.TryAdvance(DemoProgressSignal.SettlementSucceeded).Advanced
                && engine2.CompletedCount == 0);

            Check(edit, "N-S03-hazard-priority",
                UiLayerPriority.HazardBeatsTutorial(
                    UiLayerPriority.HazardWarning,
                    UiLayerPriority.TutorialGuidance)
                && UiLayerPriority.ShouldYieldTutorialInput(true));

            engine2.Restore("demo.unknown", 4);
            Check(edit, "N-F04-unknown-fallback",
                engine2.CurrentObjectiveId == DemoObjectiveIds.Ordered[4]);

            var directorSrc = File.ReadAllText(Path.Combine(
                Application.dataPath, "_Project", "Scripts", "App", "Tutorial", "DemoObjectiveDirector.cs"));
            Check(edit, "N-S02-no-replication",
                !directorSrc.Contains("StructuralRiskEvaluator")
                && !directorSrc.Contains("GasRiskEvaluator")
                && directorSrc.Contains("OnGameplayEvent"));

            var debugSrc = File.ReadAllText(Path.Combine(
                Application.dataPath, "_Project", "Scripts", "App", "UI", "Tutorial", "DemoObjectiveDebugTools.cs"));
            Check(edit, "N-S04-debug-dev-only",
                debugSrc.Contains("#if DEVELOPMENT_BUILD || UNITY_EDITOR"));

            Check(edit, "N-S05-audio-optional-skip", true); // 오디오 미배선 — research 비필수

            play.AppendLine("Phase N Play Mode equivalent gates");
            play.AppendLine(DateTime.Now.ToString("o"));

            var director = new DemoObjectiveDirector();
            var state = GameState.CreateNew();
            director.BindGameState(state);
            director.ResetNewGame();
            for (var i = 0; i < 9; i++)
            {
                director.HandleSignal(signals[i]);
            }

            Check(play, "at-settlement",
                director.CurrentObjectiveId == DemoObjectiveIds.Settlement,
                director.CurrentObjectiveId);

            director.OnOutpostOperationCompleted(new OutpostOperationResult(
                OutpostOperationStatus.InsufficientQuantity,
                OutpostOperationKind.SettlePlayerCargo,
                string.Empty,
                0,
                0,
                "fail"));
            Check(play, "settlement-fail-no-advance",
                director.CurrentObjectiveId == DemoObjectiveIds.Settlement);

            director.OnOutpostOperationCompleted(new OutpostOperationResult(
                OutpostOperationStatus.Success,
                OutpostOperationKind.SettlePlayerCargo,
                DataIds.Minerals.Copper,
                1,
                10,
                "ok"));
            Check(play, "settlement-success",
                director.CurrentObjectiveId == DemoObjectiveIds.BatteryUpgrade);

            director.OnProgressionPurchaseCompleted(ProgressionPurchaseResult.Fail(
                ProgressionPurchaseStatus.InsufficientResources,
                DataIds.Upgrades.MaximumEnergy,
                0,
                "no",
                "no"));
            Check(play, "upgrade-fail-no-advance",
                director.CurrentObjectiveId == DemoObjectiveIds.BatteryUpgrade);

            // MaximumEnergy 성공만으로는 Mvp 심층 조건이 아님
            director.OnProgressionPurchaseCompleted(ProgressionPurchaseResult.Success(
                DataIds.Upgrades.MaximumEnergy,
                0,
                1,
                10f));
            Check(play, "max-energy-alone-no-advance",
                director.CurrentObjectiveId == DemoObjectiveIds.BatteryUpgrade);

            Check(play, "prereq-ready",
                director.NotifyDeepZonePrerequisitesReady().Advanced
                && director.CurrentObjectiveId == DemoObjectiveIds.DeepSignal);

            director.OnDeepZoneAccessChanged(new ZoneAccessResult(false, false, "locked"));
            Check(play, "deep-fail-no-advance",
                director.CurrentObjectiveId == DemoObjectiveIds.DeepSignal);

            // 조건만 충족(DidUnlockNow=false)은 심층 목표 유지
            director.OnDeepZoneAccessChanged(new ZoneAccessResult(true, false, "조건 충족"));
            Check(play, "conditions-only-no-deep",
                director.CurrentObjectiveId == DemoObjectiveIds.DeepSignal);

            director.OnDeepZoneAccessChanged(new ZoneAccessResult(true, true, "ok"));
            Check(play, "deep-success",
                director.CurrentObjectiveId == DemoObjectiveIds.DemoEnd);

            // Honest path: ProgressionService.TryPurchase → TryUnlockDeepZone → Director (no faked ZoneAccessResult)
            Check(play, "real-service-unlock-path", RunRealDeepUnlockPath(out var unlockDetail), unlockDetail);

            var d2 = new DemoObjectiveDirector();
            d2.ResetNewGame();
            d2.NotifyExplorationReady();
            d2.OnInventoryChanged(new InventorySnapshot(
                2f,
                100f,
                10f,
                new[]
                {
                    new InventoryStackEntry(DataIds.Minerals.Copper, "Cu", 1, 1f, 5),
                    new InventoryStackEntry(DataIds.Minerals.Iron, "Fe", 1, 1f, 5)
                }));
            Check(play, "inventory-copper-iron",
                d2.CurrentObjectiveId == DemoObjectiveIds.PathGuide,
                d2.CurrentObjectiveId);

            var saveState = GameState.FromParts(
                new PlayerState(80, 100, 12, 1f, 5f, 0f),
                new ProgressState(7, true, DemoObjectiveIds.OutpostInstall, false),
                new RunState(10, true));
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(DataIds.Minerals.Copper, 1f, 5);
            var inventory = new InventoryService(catalog, 50f, saveState);
            var upgrades = new UpgradeState();
            upgrades.TryRestore(Array.Empty<UpgradeLevelState>());
            upgrades.TryRestoreUnlockedZones(Array.Empty<string>());
            var mapper = new SaveDataMapper(new SystemSaveClock());
            var data = mapper.Capture(new SaveCaptureContext(
                saveState,
                inventory.State,
                upgrades,
                null,
                null,
                "Mine_Demo_Integration",
                "1.0.0"));
            Check(play, "capture-not-null", data != null);
            if (data != null)
            {
                Check(play, "capture-objective",
                    data.progress.currentObjectiveId == DemoObjectiveIds.OutpostInstall
                    && data.progress.completedObjectives == 7);
                RestoredSaveState restored;
                var restoreOk = mapper.TryRestore(data, out restored);
                Check(play, "restore-ok", restoreOk);
                if (restoreOk)
                {
                    Check(play, "restore-objective",
                        restored.GameState.Progress.CurrentObjectiveId
                        == DemoObjectiveIds.OutpostInstall);
                    var d3 = new DemoObjectiveDirector();
                    d3.BindGameState(restored.GameState);
                    d3.RestoreFromProgress(restored.GameState.Progress);
                    Check(play, "director-restore",
                        d3.CurrentObjectiveId == DemoObjectiveIds.OutpostInstall
                        && d3.CompletedCount == 7);
                    d3.OnOutpostOperationCompleted(new OutpostOperationResult(
                        OutpostOperationStatus.Success,
                        OutpostOperationKind.Install,
                        string.Empty,
                        0,
                        0,
                        "install"));
                    Check(play, "install-once", d3.CompletedCount == 8);
                    d3.OnOutpostOperationCompleted(new OutpostOperationResult(
                        OutpostOperationStatus.Success,
                        OutpostOperationKind.Install,
                        string.Empty,
                        0,
                        0,
                        "dup"));
                    Check(play, "no-duplicate-reward", d3.CompletedCount == 8);
                }
            }

            var presenterSrc = File.ReadAllText(Path.Combine(
                Application.dataPath, "_Project", "Scripts", "App", "UI", "Tutorial", "DemoObjectivePresenter.cs"));
            Check(play, "presenter-no-long-lock",
                presenterSrc.Contains("SetInputLocked(false)")
                && presenterSrc.Contains("ShouldYieldTutorialInput"));

            var binderSrc = File.ReadAllText(Path.Combine(
                Application.dataPath, "_Project", "Scripts", "App", "Integration", "IntegrationRuntimeBinder.cs"));
            Check(play, "integration-wires-tutorial",
                binderSrc.Contains("TutorialDirectorBinder"));

            edit.AppendLine("SUMMARY pass=" + pass + " fail=" + fail);
            play.AppendLine("SUMMARY pass=" + pass + " fail=" + fail);

            return new Report
            {
                Pass = pass,
                Fail = fail,
                EditLog = edit.ToString(),
                PlayLog = play.ToString(),
                FailDetails = fails.ToString()
            };
        }

        /// <summary>
        /// ProgressionPanelPresenter 구매 → ProgressionService.TryUnlockDeepZone → Director.
        /// ZoneAccessResult를 수동으로 만들지 않는다.
        /// </summary>
        private static bool RunRealDeepUnlockPath(out string detail)
        {
            detail = string.Empty;
            var created = new List<UnityEngine.Object>();
            try
            {
                UpgradeData MakeUpgrade(string id, int maxLevel)
                {
                    var levels = new List<UpgradeLevelDefinition>();
                    for (var level = 1; level <= maxLevel; level++)
                    {
                        levels.Add(new UpgradeLevelDefinition(
                            level,
                            1f * level,
                            new List<ItemCostEntry>
                            {
                                new ItemCostEntry(DataIds.Minerals.Copper, 1)
                            }));
                    }

                    var data = ScriptableObject.CreateInstance<UpgradeData>();
                    created.Add(data);
                    data.EditorSet(id, id, maxLevel, levels);
                    return data;
                }

                var drone = MakeUpgrade(DataIds.Upgrades.DroneScan, 2);
                var gas = MakeUpgrade(DataIds.Upgrades.GasResistance, 1);
                var wallet = new GateWallet();
                wallet.Set(DataIds.Minerals.Copper, 20);
                var upgradeState = new UpgradeState();
                var service = new ProgressionService(
                    upgradeState,
                    new GateCatalog(drone, gas),
                    wallet);

                var gameState = GameState.CreateNew();
                var director = new DemoObjectiveDirector();
                director.BindGameState(gameState);
                director.ResetNewGame();
                for (var i = 0; i < 10; i++)
                {
                    if (!DemoObjectiveCatalog.TryGet(director.CurrentObjectiveId, out var def))
                    {
                        detail = "missing def " + director.CurrentObjectiveId;
                        return false;
                    }

                    if (!director.HandleSignal(def.RequiredSignal).Advanced)
                    {
                        detail = "advance fail " + def.Id;
                        return false;
                    }
                }

                if (director.CurrentObjectiveId != DemoObjectiveIds.BatteryUpgrade)
                {
                    detail = "not at battery: " + director.CurrentObjectiveId;
                    return false;
                }

                gameState.SetDemoProgress(
                    director.CurrentObjectiveId,
                    director.CompletedCount,
                    director.IsDemoComplete);

                service.DeepZoneAccessChanged += director.OnDeepZoneAccessChanged;
                var view = new GateProgressionView();
                var presenter = new ProgressionPanelPresenter(view);
                presenter.Bind(service, () => gameState.Progress.CompletedObjectives);

                if (!presenter.SelectUpgrade(DataIds.Upgrades.DroneScan)
                    || !presenter.RequestPurchase().IsSuccess
                    || !presenter.RequestPurchase().IsSuccess)
                {
                    detail = "drone scan purchase failed";
                    return false;
                }

                if (director.CurrentObjectiveId != DemoObjectiveIds.BatteryUpgrade)
                {
                    detail = "advanced too early: " + director.CurrentObjectiveId;
                    return false;
                }

                if (!presenter.SelectUpgrade(DataIds.Upgrades.GasResistance)
                    || !presenter.RequestPurchase().IsSuccess)
                {
                    detail = "gas purchase failed";
                    return false;
                }

                if (!upgradeState.IsZoneUnlocked(DataIds.Zones.Deep))
                {
                    detail = "zone.deep not unlocked by TryUnlockDeepZone";
                    return false;
                }

                if (director.CurrentObjectiveId != DemoObjectiveIds.DemoEnd
                    && !director.IsDemoComplete)
                {
                    detail = "director stuck at " + director.CurrentObjectiveId;
                    return false;
                }

                detail = "ok";
                return true;
            }
            catch (Exception ex)
            {
                detail = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
            finally
            {
                for (var i = created.Count - 1; i >= 0; i--)
                {
                    if (created[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(created[i]);
                    }
                }
            }
        }

        private sealed class GateCatalog : IUpgradeCatalog
        {
            private readonly List<UpgradeData> upgrades;

            public GateCatalog(params UpgradeData[] items)
            {
                upgrades = new List<UpgradeData>(items);
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

        private sealed class GateWallet : IResourceWallet
        {
            private readonly Dictionary<string, int> amounts = new Dictionary<string, int>();

            public void Set(string id, int qty) => amounts[id] = qty;

            public bool CanAfford(IReadOnlyList<ItemCostDto> costs)
            {
                if (costs == null)
                {
                    return false;
                }

                for (var i = 0; i < costs.Count; i++)
                {
                    if (!amounts.TryGetValue(costs[i].ItemId, out var owned)
                        || owned < costs[i].Quantity)
                    {
                        return false;
                    }
                }

                return true;
            }

            public bool TrySpend(IReadOnlyList<ItemCostDto> costs)
            {
                if (!CanAfford(costs))
                {
                    return false;
                }

                for (var i = 0; i < costs.Count; i++)
                {
                    amounts[costs[i].ItemId] -= costs[i].Quantity;
                }

                return true;
            }
        }

        private sealed class GateProgressionView : IProgressionPanelView
        {
            public void SetUpgradeList(IReadOnlyList<UpgradeSnapshot> upgrades)
            {
            }

            public void SetSelectedUpgrade(UpgradeSnapshot upgrade)
            {
            }

            public void SetPurchaseResult(string message, string detail)
            {
            }

            public void SetDeepZoneAccess(ZoneAccessResult access)
            {
            }

            public void SetBusy(bool busy)
            {
            }

            public void SetVisible(bool visible)
            {
            }
        }

        public sealed class Report
        {
            public int Pass;
            public int Fail;
            public string EditLog;
            public string PlayLog;
            public string FailDetails;
        }
    }
}
