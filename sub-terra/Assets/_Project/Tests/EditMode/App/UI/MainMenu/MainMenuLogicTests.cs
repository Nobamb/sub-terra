using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.UI.MainMenu
{
    /// <summary>
    /// Phase L 순수 로직 게이트: 이어하기 적격, 덮어쓰기 취소 보존, 탐사 단일 비행.
    /// 실제 LoadService/SaveService 경로를 구동한다.
    /// </summary>
    public sealed class MainMenuLogicTests
    {
        private string testRoot;
        private SavePathPolicy paths;
        private PhysicalSaveFileSystem physical;
        private SaveDataMapper mapper;
        private SaveJsonCodec json;
        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

        [SetUp]
        public void SetUp()
        {
            testRoot = Path.Combine(
                Path.GetTempPath(),
                "subterra-l-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testRoot);
            paths = new SavePathPolicy(testRoot);
            physical = new PhysicalSaveFileSystem();
            mapper = new SaveDataMapper(new FixedClock());
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
        public void L_F03_ContinueEligibility_MatchesLoadServiceResults()
        {
            var save = CreateSave();
            var load = CreateLoad();

            // empty
            var emptyMeta = load.GetSlotMetadata(1);
            Assert.That(emptyMeta.LoadStatus, Is.EqualTo(LoadStatus.NotFound));
            Assert.That(emptyMeta.CanContinue, Is.False);
            Assert.That(
                SlotContinuePolicy.FromMetadata(emptyMeta),
                Is.EqualTo(SlotContinueEligibility.Empty));

            // valid
            Assert.That(save.Save(1, CreateContext(111)).IsSuccess, Is.True);
            var validMeta = load.GetSlotMetadata(1);
            Assert.That(validMeta.LoadStatus, Is.EqualTo(LoadStatus.Success));
            Assert.That(validMeta.CanContinue, Is.True);
            Assert.That(validMeta.Gold, Is.EqualTo(111));
            Assert.That(
                SlotContinuePolicy.FromMetadata(validMeta),
                Is.EqualTo(SlotContinueEligibility.Ready));

            // backup only
            Assert.That(save.Save(2, CreateContext(222)).IsSuccess, Is.True);
            Assert.That(save.Save(2, CreateContext(333)).IsSuccess, Is.True);
            paths.TryGetPaths(2, out var slot2);
            File.WriteAllText(slot2.Normal, "{ broken");
            var backupMeta = load.GetSlotMetadata(2);
            Assert.That(backupMeta.LoadStatus, Is.EqualTo(LoadStatus.RecoveredFromBackup));
            Assert.That(backupMeta.CanContinue, Is.True);
            Assert.That(backupMeta.IsRecoverableFromBackup, Is.True);
            Assert.That(
                SlotContinuePolicy.FromMetadata(backupMeta),
                Is.EqualTo(SlotContinueEligibility.RecoverableFromBackup));

            // both invalid
            paths.TryGetPaths(3, out var slot3);
            File.WriteAllText(slot3.Normal, "bad-a");
            File.WriteAllText(slot3.Backup, "bad-b");
            var badMeta = load.GetSlotMetadata(3);
            Assert.That(badMeta.LoadStatus, Is.EqualTo(LoadStatus.BothCopiesInvalid));
            Assert.That(badMeta.CanContinue, Is.False);
            Assert.That(
                SlotContinuePolicy.FromMetadata(badMeta),
                Is.EqualTo(SlotContinueEligibility.Unrecoverable));
            Assert.That(
                SlotContinuePolicy.RequiresOverwriteConfirm(
                    SlotContinueEligibility.Unrecoverable),
                Is.True);
        }

        [Test]
        public void L_F02_OverwriteCancel_LeavesSaveBytesAndDoesNotStart()
        {
            var save = CreateSave();
            var load = CreateLoad();
            Assert.That(save.Save(1, CreateContext(777)).IsSuccess, Is.True);
            paths.TryGetPaths(1, out var slotPaths);
            var beforeBytes = File.ReadAllBytes(slotPaths.Normal);
            var beforeState = GameState.CreateNew();
            beforeState.SetGold(5);

            var view = new RecordingMenuView();
            var presenter = new MainMenuPresenter(view, load, "1.0.0-test");
            var started = 0;
            GameState runtimeState = beforeState;
            presenter.StartNewGameConfirmed += _ =>
            {
                started++;
                runtimeState = GameState.CreateNew();
            };

            presenter.SelectSlot(1);
            var request = presenter.RequestNewGame();
            Assert.That(request, Is.EqualTo(NewGameRequestStatus.AwaitingOverwriteConfirm));
            Assert.That(view.OverwriteVisible, Is.True);

            var cancel = presenter.CancelOverwriteNewGame();
            Assert.That(cancel, Is.EqualTo(NewGameRequestStatus.Cancelled));
            Assert.That(started, Is.EqualTo(0));
            Assert.That(runtimeState.Player.Gold, Is.EqualTo(5));
            Assert.That(File.ReadAllBytes(slotPaths.Normal), Is.EqualTo(beforeBytes));
        }

        [Test]
        public void L_F01_EmptySlotNewGame_ReadyWithoutConfirm()
        {
            var load = CreateLoad();
            var view = new RecordingMenuView();
            var presenter = new MainMenuPresenter(view, load, "1.0.0-test");
            var startedSlot = 0;
            presenter.StartNewGameConfirmed += slot => startedSlot = slot;

            presenter.SelectSlot(2);
            var status = presenter.RequestNewGame();
            Assert.That(status, Is.EqualTo(NewGameRequestStatus.ReadyToStart));
            Assert.That(startedSlot, Is.EqualTo(2));
            Assert.That(view.OverwriteVisible, Is.False);
        }

        [Test]
        public void L_F05_ExplorationStart_PreparesAndLoadsOnceOnMultiInvoke()
        {
            // 런타임이 쓰는 단일 가드: 연타 시 prepare/load 각 1회.
            var guard = new ExplorationStartGuard();
            var prepareCount = 0;
            var loadCount = 0;

            bool Attempt()
            {
                return guard.TryStart(
                    () => prepareCount++,
                    () =>
                    {
                        loadCount++;
                        return true;
                    });
            }

            Assert.That(Attempt(), Is.True);
            Assert.That(Attempt(), Is.False);
            Assert.That(Attempt(), Is.False);
            Assert.That(prepareCount, Is.EqualTo(1));
            Assert.That(loadCount, Is.EqualTo(1));
            Assert.That(guard.RunPrepareCount, Is.EqualTo(1));
            Assert.That(guard.SceneLoadCount, Is.EqualTo(1));
        }

        [Test]
        public void L_F05_RequestExplorationStart_FailedStart_ReenablesExploreAndDoesNotClaimSuccess()
        {
            // 실제 Presenter 경로: 런타임 실패를 그대로 받아 busy 해제·실패 메시지·재시도 가능.
            var view = new RecordingSurfaceView();
            var presenter = new SurfaceBasePresenter(view);
            presenter.Bind(GameState.CreateNew(), null);

            var startCalls = 0;
            const string failReason = "유효한 슬롯/상태가 없어 탐사에 진입할 수 없습니다.";
            var ok = presenter.RequestExplorationStart(() =>
            {
                startCalls++;
                // 광산 Scene 로드를 시뮬레이션하지 않음 — 실패만 반환.
                return (false, failReason);
            });

            Assert.That(ok, Is.False);
            Assert.That(startCalls, Is.EqualTo(1));
            Assert.That(view.Busy, Is.False, "실패 후 탐사 버튼이 다시 활성되어야 한다");
            Assert.That(view.Message, Is.EqualTo(failReason));
            Assert.That(view.Message, Does.Not.Contain("준비 중"));

            // 재시도 가능: 두 번째 호출이 다시 startExploration을 탄다.
            var second = presenter.RequestExplorationStart(() =>
            {
                startCalls++;
                return (false, "탐사 Scene 로드에 실패했습니다.");
            });
            Assert.That(second, Is.False);
            Assert.That(startCalls, Is.EqualTo(2));
            Assert.That(view.Busy, Is.False);
            Assert.That(view.Message, Does.Contain("로드"));
        }

        [Test]
        public void L_F05_RequestExplorationStart_SuccessKeepsBusy_MultiStartDelegatesToCaller()
        {
            var view = new RecordingSurfaceView();
            var presenter = new SurfaceBasePresenter(view);
            presenter.Bind(GameState.CreateNew(), null);

            // 런타임 가드를 흉내: 첫 성공 후 연타는 already-in-flight 실패.
            var guard = new ExplorationStartGuard();
            var loadCount = 0;
            (bool success, string reason) StartOnce()
            {
                var started = guard.TryStart(
                    () => { },
                    () =>
                    {
                        loadCount++;
                        return true;
                    });
                return started
                    ? (true, string.Empty)
                    : (false, "탐사 전환이 이미 진행 중입니다.");
            }

            Assert.That(presenter.RequestExplorationStart(StartOnce), Is.True);
            Assert.That(view.Busy, Is.True);
            Assert.That(loadCount, Is.EqualTo(1));

            Assert.That(presenter.RequestExplorationStart(StartOnce), Is.False);
            Assert.That(loadCount, Is.EqualTo(1), "Scene 로드는 한 번만");
            Assert.That(view.Busy, Is.False, "연타 실패 시 busy 해제");
            Assert.That(view.Message, Does.Contain("진행 중"));
        }

        [Test]
        public void L_F04_SurfaceBasePresenter_DoesNotImplementEconomyTransactions()
        {
            var view = new RecordingSurfaceView();
            var presenter = new SurfaceBasePresenter(view);
            var state = GameState.CreateNew();
            state.SetGold(50);
            presenter.Bind(state, null);
            presenter.RefreshReadModel();

            Assert.That(view.Goals, Does.Contain("목표"));
            Assert.That(view.DeepReason, Is.Not.Empty);

            // 소스에 경제 복제 없음 — 단일 가드 진입은 런타임 Func로만.
            var surfacePath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "UI",
                "SurfaceBase",
                "SurfaceBasePresenter.cs");
            var text = File.ReadAllText(surfacePath);
            Assert.That(text, Does.Not.Contain("TrySellMineral"));
            Assert.That(text, Does.Not.Contain("TryPurchase"));
            Assert.That(text, Does.Not.Contain("SetGold"));
            Assert.That(text, Does.Not.Contain("ExplorationStartGuard"));
            Assert.That(text, Does.Contain("startExploration"));

            var binderPath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "UI",
                "SurfaceBase",
                "SurfaceBaseBinder.cs");
            var binderText = File.ReadAllText(binderPath);
            Assert.That(binderText, Does.Contain("TryStartExploration"));
            Assert.That(binderText, Does.Not.Contain("ExplorationStartGuard"));
        }

        [Test]
        public void SurfaceBasePresenter_ShowsEnergyAndDepartureCost_AndRefreshesOnChange()
        {
            var view = new RecordingSurfaceView();
            var presenter = new SurfaceBasePresenter(view);
            var state = GameState.CreateNew();

            presenter.Bind(state, null, 5);

            Assert.That(view.EnergyCurrent, Is.EqualTo(100));
            Assert.That(view.EnergyMax, Is.EqualTo(100));
            Assert.That(view.EnergyCost, Is.EqualTo(5));

            state.SetCurrentEnergy(80);

            Assert.That(view.EnergyCurrent, Is.EqualTo(80));
            presenter.Dispose();
        }

        [Test]
        public void RequestQuit_Source_HasEditorAndPlayerPaths()
        {
            var runtimePath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "Save",
                "SaveRuntimeController.cs");
            var text = File.ReadAllText(runtimePath);
            Assert.That(text, Does.Contain("Application.isEditor"));
            Assert.That(text, Does.Contain("StopEditorPlayMode"));
            Assert.That(text, Does.Contain("Application.Quit"));
            Assert.That(text, Does.Contain("EditorApplication"));
        }

        [Test]
        public void SettingsSession_ApplyCancelDefaults()
        {
            var session = new SettingsSession();
            session.Open();
            session.Draft.MasterVolume = 0.25f;
            session.Apply();
            Assert.That(session.Applied.MasterVolume, Is.EqualTo(0.25f));

            session.Open();
            session.Draft.MasterVolume = 0.9f;
            session.Cancel();
            Assert.That(session.Applied.MasterVolume, Is.EqualTo(0.25f));
            Assert.That(session.Draft.MasterVolume, Is.EqualTo(0.25f));

            session.Open();
            session.ResetDefaults();
            Assert.That(session.Draft.MasterVolume, Is.EqualTo(1f));
        }

        [Test]
        public void QuitPolicy_DefersWhileSaving_AndSavesWhenDirty()
        {
            Assert.That(
                QuitPolicy.Decide(false, false),
                Is.EqualTo(QuitDecision.QuitImmediately));
            Assert.That(
                QuitPolicy.Decide(true, false),
                Is.EqualTo(QuitDecision.SaveThenQuit));
            Assert.That(
                QuitPolicy.Decide(true, true),
                Is.EqualTo(QuitDecision.DeferWhileSaving));
        }

        private SaveService CreateSave()
        {
            return new SaveService(physical, paths, mapper, json);
        }

        private LoadService CreateLoad()
        {
            return new LoadService(physical, paths, mapper, json);
        }

        private SaveCaptureContext CreateContext(int gold)
        {
            var state = GameState.CreateNew();
            state.SetGold(gold);
            state.SetDepth(7);
            var inventory = new InventoryState();
            var upgrades = new UpgradeState();
            return new SaveCaptureContext(
                state,
                inventory,
                upgrades,
                null,
                new EmptyWorld(),
                SceneNames.SurfaceBase,
                "test");
        }

        private sealed class EmptyWorld : IWorldSnapshotProvider
        {
            public WorldSnapshotDto CaptureSnapshot() => new WorldSnapshotDto();
            public void RestoreSnapshot(WorldSnapshotDto snapshot) { }
        }

        private sealed class FixedClock : ISaveClock
        {
            public long UtcNowSeconds => 1_700_000_000;
        }

        private sealed class RecordingMenuView : IMainMenuView
        {
            public bool OverwriteVisible;
            public string Message;

            public void SetSlotDisplay(int slotId, string label, bool canContinue, string statusText) { }
            public void SetSelectedSlot(int slotId, bool canContinue, string message) => Message = message;
            public void SetOverwriteConfirmVisible(bool visible, int slotId) => OverwriteVisible = visible;
            public void SetSettingsVisible(bool visible) { }
            public void SetSettingsDraft(SettingsValues values) { }
            public void SetVersionLabel(string version) { }
            public void SetMessage(string message) => Message = message;
            public void SetQuitBlockedMessage(string message) => Message = message;
        }

        private sealed class RecordingSurfaceView : ISurfaceBaseView
        {
            public string Goals;
            public string DeepReason;
            public bool Busy;
            public string Message;
            public int EnergyCurrent;
            public int EnergyMax;
            public int EnergyCost;

            public void SetGoals(int completedObjectives, string summary) => Goals = summary;
            public void SetEnergy(int current, int max, int explorationCost)
            {
                EnergyCurrent = current;
                EnergyMax = max;
                EnergyCost = explorationCost;
            }
            public void SetDeepZoneLock(bool unlocked, string reason) => DeepReason = reason;
            public void SetRecentRun(int depth, bool isSafe, string structural, string gas) { }
            public void SetExplorationBusy(bool busy) => Busy = busy;
            public void SetMessage(string message) => Message = message;
        }
    }
}
