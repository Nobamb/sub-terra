using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Tutorial;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Tutorial
{
    /// <summary>prompt-B 95 퀘스트 보상 수치, 지급, 화물 부족 선택, 상세 로그 UI.</summary>
    public sealed class PromptB95QuestRewardTests
    {
        [Test]
        public void CatalogRewards_MatchProposalBaseline()
        {
            AssertReward(DemoObjectiveIds.MineBlock, 3, 0, 0, 0);
            AssertReward(DemoObjectiveIds.MineCopper, 3, 0, 0, 50);
            AssertReward(DemoObjectiveIds.UpgradeDrillSpeed, 5, 1, 0, 40);
            AssertReward(DemoObjectiveIds.TravelToSurface, 0, 0, 0, 100);
            AssertReward(DemoObjectiveIds.ReturnToMine, 2, 0, 0, 0);
            AssertReward(DemoObjectiveIds.MineIron, 0, 2, 0, 0);
            AssertReward(DemoObjectiveIds.PlaceSupportInDanger, 3, 1, 0, 0);
            AssertReward(DemoObjectiveIds.PlaceLadder, 0, 3, 0, 0);
            AssertReward(DemoObjectiveIds.PlaceLightAtDepth, 0, 3, 0, 0);
            AssertReward(DemoObjectiveIds.StoreMineral, 3, 2, 0, 0);
            AssertReward(DemoObjectiveIds.InstallOutpostCore, 3, 3, 0, 200);
            AssertReward(DemoObjectiveIds.ChargeNearOutpost, 3, 2, 0, 0);
            AssertReward(DemoObjectiveIds.HealNearOutpost, 3, 2, 0, 0);
            AssertReward(DemoObjectiveIds.UnlockDeepZone, 0, 2, 1, 80);
            AssertReward(DemoObjectiveIds.MineLithium, 0, 0, 2, 0);
            AssertReward(DemoObjectiveIds.PurifyGasWithOutpost, 0, 0, 3, 0);
            AssertReward(DemoObjectiveIds.SellAtSettlement, 0, 2, 2, 100);
            AssertReward(DemoObjectiveIds.EmergencyEscapeReturn, 0, 0, 5, 300);
        }

        [Test]
        public void Grant_GoldOnlyNeverBlocksOnFullCargo()
        {
            var env = CreateEnv(maxCapacity: 0.1f);
            env.State.SetDemoProgress(DemoObjectiveIds.ReturnToMine, 4, false);
            env.State.SetQuestRewardSettlement(string.Empty, 3);

            var offered = env.Rewards.SyncFromProgress();
            Assert.That(offered.IsAwaitingClaim, Is.True);
            Assert.That(env.State.Player.Gold, Is.Zero);

            var result = env.Rewards.ClaimPending();

            Assert.That(result.DidGrant, Is.True);
            Assert.That(env.State.Player.Gold, Is.EqualTo(100));
            Assert.That(env.State.Progress.QuestRewardSettledCount, Is.EqualTo(4));
            Assert.That(env.State.Progress.PendingQuestRewardId, Is.Empty);
        }

        [Test]
        public void Grant_AddsMineralsAndGoldAtomically()
        {
            var env = CreateEnv(maxCapacity: 50f);
            env.State.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);

            var offered = env.Rewards.SyncFromProgress();
            Assert.That(offered.IsAwaitingClaim, Is.True);
            Assert.That(env.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.Zero);

            var result = env.Rewards.ClaimPending();

            Assert.That(result.DidGrant, Is.True);
            Assert.That(env.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(3));
            Assert.That(env.State.Player.Gold, Is.Zero);
            Assert.That(env.State.Progress.QuestRewardSettledCount, Is.EqualTo(1));
            Assert.That(env.State.Progress.PendingQuestRewardId, Is.Empty);
        }

        [Test]
        public void Grant_WhenCapacityShort_ChangesNothingAndWaits()
        {
            var env = CreateEnv(maxCapacity: 4f);
            env.Inventory.TryAddMineral(DataIds.Minerals.Copper, 2);
            env.State.SetGold(7);
            env.State.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);

            var offered = env.Rewards.SyncFromProgress();
            Assert.That(offered.IsAwaitingClaim, Is.True);
            Assert.That(env.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(2));
            Assert.That(env.State.Player.Gold, Is.EqualTo(7));

            var result = env.Rewards.ClaimPending();

            Assert.That(result.NeedsPlayerChoice, Is.True);
            Assert.That(env.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(2));
            Assert.That(env.State.Player.Gold, Is.EqualTo(7));
            Assert.That(env.State.Progress.PendingQuestRewardId, Is.EqualTo(DemoObjectiveIds.MineBlock));
            Assert.That(env.State.Progress.QuestRewardSettledCount, Is.Zero);
        }

        [Test]
        public void DumpThenRetry_GrantsWhenSpaceFrees()
        {
            var env = CreateEnv(maxCapacity: 6f);
            env.Inventory.TryAddMineral(DataIds.Minerals.Iron, 3);
            env.State.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
            Assert.That(env.Rewards.SyncFromProgress().IsAwaitingClaim, Is.True);
            Assert.That(env.Rewards.ClaimPending().NeedsPlayerChoice, Is.True);

            env.Rewards.Dump(DataIds.Minerals.Iron, 3);
            var retry = env.Rewards.RetryPending();

            Assert.That(retry.DidGrant, Is.True);
            Assert.That(env.Inventory.State.GetQuantity(DataIds.Minerals.Iron), Is.Zero);
            Assert.That(env.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(3));
            Assert.That(env.State.Progress.QuestRewardSettledCount, Is.EqualTo(1));
        }

        [Test]
        public void Forfeit_SkipsRewardAndDoesNotSell()
        {
            var env = CreateEnv(maxCapacity: 4f);
            env.Inventory.TryAddMineral(DataIds.Minerals.Copper, 2);
            env.State.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
            env.Rewards.SyncFromProgress();

            var result = env.Rewards.ForfeitPending();

            Assert.That(result.Status, Is.EqualTo(QuestRewardGrantStatus.Forfeited));
            Assert.That(env.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(2));
            Assert.That(env.State.Player.Gold, Is.Zero);
            Assert.That(env.State.Progress.QuestRewardSettledCount, Is.EqualTo(1));
            Assert.That(env.State.Progress.PendingQuestRewardId, Is.Empty);
        }

        [Test]
        public void Presenter_NavigatesQuestLogAndShowsRewards()
        {
            var view = new RecordingView();
            var presenter = new DemoObjectivePresenter(view);
            var director = new DemoObjectiveDirector();
            var state = GameState.CreateNew();
            director.BindGameState(state);
            director.RestoreFromProgress(new ProgressState(2, false, DemoObjectiveIds.UpgradeDrillSpeed));
            presenter.Bind(director);

            presenter.OpenDetails();
            Assert.That(view.DetailsVisible, Is.True);
            Assert.That(view.IndexText, Is.EqualTo("3/18"));
            Assert.That(view.StatusText, Is.EqualTo("진행 중"));
            Assert.That(view.RewardText, Does.Contain("구리 5"));

            presenter.ShowPreviousQuest();
            Assert.That(view.IndexText, Is.EqualTo("2/18"));
            Assert.That(view.StatusText, Is.EqualTo("클리어"));
            Assert.That(view.RewardText, Does.Contain("골드 50"));

            presenter.ShowNextQuest();
            presenter.ShowNextQuest();
            Assert.That(view.IndexText, Is.EqualTo("4/18"));
            Assert.That(view.StatusText, Is.EqualTo("미완료"));

            presenter.CloseDetails();
            Assert.That(view.DetailsVisible, Is.False);
        }

        [Test]
        public void Presenter_DumpCloseWithoutSpace_ReopensCapacityChoice()
        {
            var env = CreateEnv(maxCapacity: 4f);
            env.Inventory.TryAddMineral(DataIds.Minerals.Copper, 2);
            env.State.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
            var view = new RecordingView();
            var presenter = new DemoObjectivePresenter(view);
            var director = new DemoObjectiveDirector();
            director.BindGameState(env.State);
            director.RestoreFromProgress(env.State.Progress);
            presenter.Bind(director, env.Rewards);

            Assert.That(view.ClaimVisible, Is.True);
            Assert.That(view.CapacityVisible, Is.False);
            presenter.ConfirmClaim();
            Assert.That(view.ClaimVisible, Is.False);
            Assert.That(view.CapacityVisible, Is.True);

            presenter.ChooseDumpInventory();
            Assert.That(view.DumpVisible, Is.True);
            Assert.That(view.CapacityVisible, Is.False);

            presenter.CloseDumpPanel();
            Assert.That(view.DumpVisible, Is.False);
            Assert.That(view.CapacityVisible, Is.True);
            Assert.That(env.State.Progress.PendingQuestRewardId, Is.EqualTo(DemoObjectiveIds.MineBlock));
        }

        [Test]
        public void Presenter_ShowsClaimPopupOnClear_AndGrantsWhenClosed()
        {
            var env = CreateEnv(maxCapacity: 50f);
            env.State.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
            var view = new RecordingView();
            var presenter = new DemoObjectivePresenter(view);
            var director = new DemoObjectiveDirector();
            director.BindGameState(env.State);
            director.RestoreFromProgress(env.State.Progress);
            presenter.Bind(director, env.Rewards);

            Assert.That(view.ClaimVisible, Is.True);
            Assert.That(view.ClaimQuestTitle, Is.EqualTo("블록 제거"));
            Assert.That(view.ClaimRewardText, Does.Contain("구리 3"));
            Assert.That(env.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.Zero);

            presenter.ConfirmClaim();

            Assert.That(view.ClaimVisible, Is.False);
            Assert.That(env.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(3));
            Assert.That(env.State.Progress.QuestRewardSettledCount, Is.EqualTo(1));
        }

        [Test]
        public void Presenter_ClaimCloseWhenCargoFull_AsksDumpOrForfeit()
        {
            var env = CreateEnv(maxCapacity: 4f);
            env.Inventory.TryAddMineral(DataIds.Minerals.Copper, 2);
            env.State.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
            var view = new RecordingView();
            var presenter = new DemoObjectivePresenter(view);
            var director = new DemoObjectiveDirector();
            director.BindGameState(env.State);
            director.RestoreFromProgress(env.State.Progress);
            presenter.Bind(director, env.Rewards);

            presenter.ConfirmClaim();

            Assert.That(view.ClaimVisible, Is.False);
            Assert.That(view.CapacityVisible, Is.True);
            Assert.That(env.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(2));
            Assert.That(env.State.Progress.PendingQuestRewardId, Is.EqualTo(DemoObjectiveIds.MineBlock));

            presenter.ChooseForfeitReward();
            Assert.That(view.CapacityVisible, Is.False);
            Assert.That(env.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(2));
            Assert.That(env.State.Progress.QuestRewardSettledCount, Is.EqualTo(1));
        }

        [Test]
        public void SaveRoundTrip_KeepsPendingAndSettledReward()
        {
            var env = CreateEnv(maxCapacity: 4f);
            env.Inventory.TryAddMineral(DataIds.Minerals.Copper, 2);
            env.State.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
            env.Rewards.SyncFromProgress();

            var mapper = new SaveDataMapper(new FixedClock(10));
            var upgrades = new UpgradeState();
            Assert.That(upgrades.TryRestore(new List<UpgradeLevelState>()), Is.True);
            Assert.That(upgrades.TryRestoreUnlockedZones(new List<string>()), Is.True);
            var data = mapper.Capture(new SaveCaptureContext(
                env.State,
                env.Inventory.State,
                upgrades,
                null,
                null,
                "Mine_Demo_Integration",
                "1.0.0"));

            Assert.That(data.progress.pendingQuestRewardId, Is.EqualTo(DemoObjectiveIds.MineBlock));
            Assert.That(data.progress.questRewardSettledCount, Is.Zero);
            Assert.That(mapper.TryRestore(data, out var restored), Is.True);
            Assert.That(
                restored.GameState.Progress.PendingQuestRewardId,
                Is.EqualTo(DemoObjectiveIds.MineBlock));
            Assert.That(restored.GameState.Progress.QuestRewardSettledCount, Is.Zero);
        }

        [Test]
        public void Migration3To4_MarksExistingCompletionsSettled()
        {
            var data = new GameSaveData
            {
                saveVersion = 3,
                targetSceneName = "Mine_Demo_Integration",
                progress = new ProgressSaveData
                {
                    completedObjectives = 5,
                    currentObjectiveId = DemoObjectiveIds.MineIron
                }
            };

            var status = new SaveMigrationService().TryMigrate(data);
            Assert.That(status, Is.EqualTo(SaveMigrationStatus.Migrated));
            Assert.That(data.saveVersion, Is.EqualTo(SaveVersions.Current));
            Assert.That(data.progress.questRewardSettledCount, Is.EqualTo(5));
            Assert.That(data.progress.pendingQuestRewardId, Is.Empty);
        }

        [Test]
        public void TryAddManyExact_IsAtomicWhenCapacityIsShort()
        {
            var catalog = CreateCatalog();
            var inventory = new InventoryService(catalog, 5f);
            inventory.TryAddMineral(DataIds.Minerals.Copper, 2);
            var additions = new List<KeyValuePair<string, int>>
            {
                new KeyValuePair<string, int>(DataIds.Minerals.Copper, 3),
                new KeyValuePair<string, int>(DataIds.Minerals.Iron, 1)
            };

            var result = inventory.TryAddManyExact(additions);

            Assert.That(result.Status, Is.EqualTo(InventoryMutationStatus.CapacityFull));
            Assert.That(inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(2));
            Assert.That(inventory.State.GetQuantity(DataIds.Minerals.Iron), Is.Zero);
        }

        [Test]
        public void IntegrationScene_HasQuestLogAndOverflowPanels()
        {
            var scene = SceneManager.GetSceneByPath(PromptB95QuestRewardUiBuilder.IntegrationScenePath);
            var closeAfter = !scene.IsValid() || !scene.isLoaded;
            if (closeAfter)
            {
                scene = EditorSceneManager.OpenScene(
                    PromptB95QuestRewardUiBuilder.IntegrationScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                var root = Find(scene, "DemoObjectiveRoot");
                var details = Find(scene, "QuestDetailsPanel");
                var prev = Find(scene, "QuestDetailsPrevButton");
                var next = Find(scene, "QuestDetailsNextButton");
                var index = Find(scene, "QuestDetailsIndex");
                var reward = Find(scene, "QuestDetailsReward");
                var capacity = Find(scene, "QuestRewardCapacityPanel");
                var dump = Find(scene, "QuestRewardDumpPanel");
                var dumpAll = Find(scene, "DumpAll");
                var claim = Find(scene, "QuestClearRewardPanel");
                var claimConfirm = Find(scene, "ClaimConfirmButton");
                var dumpExisting = Find(scene, "CapacityDumpButton");
                var forfeitReward = Find(scene, "CapacityForfeitButton");
                Assert.That(root, Is.Not.Null);
                Assert.That(details, Is.Not.Null);
                Assert.That(prev, Is.Not.Null);
                Assert.That(next, Is.Not.Null);
                Assert.That(index, Is.Not.Null);
                Assert.That(reward, Is.Not.Null);
                Assert.That(capacity, Is.Not.Null);
                Assert.That(dump, Is.Not.Null);
                Assert.That(dumpAll, Is.Not.Null);
                Assert.That(claim, Is.Not.Null);
                Assert.That(claimConfirm, Is.Not.Null);

                var view = root.GetComponent<DemoObjectiveView>();
                Assert.That(view, Is.Not.Null);
                Assert.That(view.HasRewardLogReferences(), Is.True);
                Assert.That(view.HasOverflowReferences(), Is.True);
                Assert.That(view.HasClaimReferences(), Is.True);
                Assert.That(details.activeSelf, Is.False);
                Assert.That(capacity.activeSelf, Is.False);
                Assert.That(dump.activeSelf, Is.False);
                Assert.That(claim.activeSelf, Is.False);
                Assert.That(details.GetComponent<Canvas>().overrideSorting, Is.True);
                Assert.That(
                    details.GetComponent<Canvas>().sortingOrder,
                    Is.EqualTo(UiLayerPriority.QuestPopup));
                Assert.That(
                    details.GetComponent<Canvas>().sortingOrder,
                    Is.GreaterThan(SubTerra.App.UI.Drone.DroneDialogueSocket.OverlaySortingOrder));
                Assert.That(claim.GetComponent<Canvas>().overrideSorting, Is.True);
                Assert.That(
                    claim.GetComponent<Canvas>().sortingOrder,
                    Is.EqualTo(UiLayerPriority.QuestPopup));
                Assert.That(
                    dumpExisting.GetComponentInChildren<TMP_Text>(true).text,
                    Is.EqualTo("기존 자원 버리기"));
                Assert.That(
                    forfeitReward.GetComponentInChildren<TMP_Text>(true).text,
                    Is.EqualTo("퀘스트 보상 버리기"));
                Assert.That(
                    claimConfirm.GetComponent<Button>().onClick.GetPersistentMethodName(0),
                    Is.EqualTo(nameof(DemoObjectiveView.OnClaimConfirmClicked)));
                Assert.That(
                    prev.GetComponent<Button>().onClick.GetPersistentMethodName(0),
                    Is.EqualTo(nameof(DemoObjectiveView.OnDetailsPrevClicked)));
                Assert.That(
                    next.GetComponent<Button>().onClick.GetPersistentMethodName(0),
                    Is.EqualTo(nameof(DemoObjectiveView.OnDetailsNextClicked)));
            }
            finally
            {
                if (closeAfter && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void AssertReward(string id, int copper, int iron, int lithium, int gold)
        {
            var definition = DemoObjectiveCatalog.GetRequired(id);
            Assert.That(definition, Is.Not.Null, id);
            Assert.That(definition.Reward.Copper, Is.EqualTo(copper), id);
            Assert.That(definition.Reward.Iron, Is.EqualTo(iron), id);
            Assert.That(definition.Reward.Lithium, Is.EqualTo(lithium), id);
            Assert.That(definition.Reward.Gold, Is.EqualTo(gold), id);
        }

        private static Env CreateEnv(float maxCapacity)
        {
            var state = GameState.CreateNew();
            var inventory = new InventoryService(CreateCatalog(), maxCapacity, state);
            var rewards = new QuestRewardService();
            rewards.Bind(inventory, state);
            return new Env(state, inventory, rewards);
        }

        private static InMemoryMineralCatalog CreateCatalog()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(DataIds.Minerals.Copper, 1.5f, 10, "구리");
            catalog.Register(DataIds.Minerals.Iron, 2f, 15, "철");
            catalog.Register(DataIds.Minerals.Lithium, 0.8f, 40, "리튬");
            return catalog;
        }

        private static GameObject Find(Scene scene, string objectName)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (var j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == objectName)
                    {
                        return transforms[j].gameObject;
                    }
                }
            }

            return null;
        }

        private readonly struct Env
        {
            public GameState State { get; }
            public InventoryService Inventory { get; }
            public QuestRewardService Rewards { get; }

            public Env(GameState state, InventoryService inventory, QuestRewardService rewards)
            {
                State = state;
                Inventory = inventory;
                Rewards = rewards;
            }
        }

        private sealed class FixedClock : ISaveClock
        {
            private readonly long seconds;

            public FixedClock(long utcSeconds)
            {
                seconds = utcSeconds;
            }

            public long UtcNowSeconds => seconds;
        }

        private sealed class RecordingView : IDemoObjectiveView
        {
            public bool DetailsVisible { get; private set; }
            public bool CapacityVisible { get; private set; }
            public bool DumpVisible { get; private set; }
            public bool ClaimVisible { get; private set; }
            public string RewardText { get; private set; } = string.Empty;
            public string StatusText { get; private set; } = string.Empty;
            public string IndexText { get; private set; } = string.Empty;
            public string ClaimRewardText { get; private set; } = string.Empty;
            public string ClaimQuestTitle { get; private set; } = string.Empty;

            public void SetObjective(DemoObjectiveReadModel model) { }
            public void SetGuidanceVisible(bool visible) { }
            public void SetGuidanceText(string title, string body) { }
            public void SetInputLocked(bool locked) { }
            public void SetHazardYield(bool yieldToHazard) { }
            public void SetDemoCompleteVisible(bool visible, string summary) { }
            public void SetDetailsVisible(bool visible) => DetailsVisible = visible;
            public void SetDetailsText(string title, string body, string nextAction) { }
            public void SetDetailsReward(string rewardText) => RewardText = rewardText ?? string.Empty;
            public void SetDetailsStatus(string statusText) => StatusText = statusText ?? string.Empty;
            public void SetDetailsIndex(string indexText) => IndexText = indexText ?? string.Empty;
            public void SetDetailsNavInteractable(bool previousEnabled, bool nextEnabled) { }
            public void SetCapacityChoiceVisible(bool visible) => CapacityVisible = visible;
            public void SetCapacityChoiceText(string title, string body) { }
            public void SetDumpPanelVisible(bool visible) => DumpVisible = visible;
            public void SetDumpSummary(string summary) { }
            public void SetDumpRows(IReadOnlyList<QuestDumpRow> rows) { }
            public void SetClaimVisible(bool visible) => ClaimVisible = visible;
            public void SetClaimText(string title, string questTitle, string rewardText, string hint)
            {
                ClaimQuestTitle = questTitle ?? string.Empty;
                ClaimRewardText = rewardText ?? string.Empty;
            }
        }
    }
}
