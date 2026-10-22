using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Tutorial
{
    /// <summary>prompt-B 152 화물 공간 부족 팝업: 부족 계산, 게이지 칸, 문구, 연출 시간표, 중복 클릭 차단.</summary>
    public sealed class PromptB152CargoFullPopupTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null)
                {
                    Object.DestroyImmediate(spawned[i]);
                }
            }

            spawned.Clear();
        }

        [Test]
        public void Shortfall_FullCargoPlusTwoIron_IsFour()
        {
            var weight = new QuestReward(0, 2, 0, 0).ComputeMineralWeight(CreateCatalog());
            var model = CargoShortfallModel.From(20f, 20f, weight);

            Assert.That(weight, Is.EqualTo(4f));
            Assert.That(model.Shortfall, Is.EqualTo(4f));
            Assert.That(model.IsShort, Is.True);
            Assert.That(CargoFullPopupText.LoadLabel(model), Is.EqualTo("20 / 20"));
            Assert.That(CargoFullPopupText.ShortfallLabel(model), Is.EqualTo("부족한 공간 4"));
        }

        [Test]
        public void Shortfall_NineteenPlusOneCopper_IsHalf()
        {
            var weight = new QuestReward(1, 0, 0, 0).ComputeMineralWeight(CreateCatalog());
            var model = CargoShortfallModel.From(19f, 20f, weight);

            Assert.That(model.Shortfall, Is.EqualTo(0.5f));
            Assert.That(CargoFullPopupText.ShortfallLabel(model), Is.EqualTo("부족한 공간 0.5"));
            Assert.That(CargoFullPopupText.LoadLabel(CargoShortfallModel.From(19.5f, 20f, 0f)), Is.EqualTo("19.5 / 20"));
        }

        [Test]
        public void Shortfall_NeverBelowZero()
        {
            var model = CargoShortfallModel.From(10f, 20f, 4f);

            Assert.That(model.Shortfall, Is.Zero);
            Assert.That(model.IsShort, Is.False);
            Assert.That(model.Layout(12, 4).OverflowCells, Is.Zero);
            Assert.That(CargoShortfallModel.From(-3f, 20f, -1f).Shortfall, Is.Zero);
        }

        [Test]
        public void Shortfall_ExcludesGold()
        {
            var catalog = CreateCatalog();
            var withGold = new QuestReward(0, 2, 0, 500).ComputeMineralWeight(catalog);
            var goldOnly = new QuestReward(0, 0, 0, 500).ComputeMineralWeight(catalog);

            Assert.That(withGold, Is.EqualTo(4f));
            Assert.That(goldOnly, Is.Zero);
            Assert.That(CargoShortfallModel.From(20f, 20f, withGold).Shortfall, Is.EqualTo(4f));
        }

        [Test]
        public void Gauge_CellCountsStayInBounds()
        {
            var full = CargoShortfallModel.From(20f, 20f, 4f).Layout(12, 4);
            Assert.That(full.CellCount, Is.EqualTo(12));
            Assert.That(full.FilledCells, Is.EqualTo(12));
            // 칸당 20/12 ≈ 1.67, 부족 4 → ceil(2.4) = 3칸.
            Assert.That(full.OverflowCells, Is.EqualTo(3));

            Assert.That(CargoShortfallModel.From(20f, 20f, 100f).Layout(12, 4).OverflowCells, Is.EqualTo(4));
            Assert.That(CargoShortfallModel.From(20f, 20f, 0.1f).Layout(12, 4).OverflowCells, Is.EqualTo(1));
            Assert.That(CargoShortfallModel.From(19.5f, 20f, 2f).Layout(12, 4).FilledCells, Is.EqualTo(11));
            Assert.That(CargoShortfallModel.From(0.2f, 20f, 30f).Layout(12, 4).FilledCells, Is.EqualTo(1));
            Assert.That(CargoShortfallModel.From(0f, 20f, 30f).Layout(12, 4).FilledCells, Is.Zero);

            var clamped = CargoShortfallModel.From(5f, 5f, 50f).Layout(0, 0);
            Assert.That(clamped.CellCount, Is.EqualTo(1));
            Assert.That(clamped.OverflowCells, Is.EqualTo(1));
            var upper = CargoShortfallModel.From(5f, 5f, 50f).Layout(999, 999);
            Assert.That(upper.CellCount, Is.EqualTo(CargoGaugeLayout.MaxCells));
            Assert.That(upper.OverflowCells, Is.LessThanOrEqualTo(CargoGaugeLayout.MaxOverflowCells));
        }

        [Test]
        public void ForfeitSubtitle_UsesActualReward()
        {
            Assert.That(CargoFullPopupText.ForfeitSubtitle(new QuestReward(0, 2, 0, 0)), Is.EqualTo("철 ×2 수령 포기"));
            Assert.That(
                CargoFullPopupText.ForfeitSubtitle(new QuestReward(3, 2, 0, 0)),
                Is.EqualTo("구리 ×3 · 철 ×2 수령 포기"));
            Assert.That(CargoFullPopupText.RewardLabel(new QuestReward(0, 0, 4, 50)), Is.EqualTo("리튬 ×4 · 골드 ×50"));
        }

        [Test]
        public void Timeline_OpensWithinLimit_FlashesOverflowOnce_ThenHolds()
        {
            var timing = new CargoFullPopupTiming().Sanitized();
            Assert.That(timing.OpenDuration, Is.LessThanOrEqualTo(CargoFullPopupTiming.MaxOpenDuration + 0.0001f));
            Assert.That(timing.closeDuration, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(timing.gaugeDuration, Is.EqualTo(0.25f).Within(0.02f));

            var rises = 0;
            var previous = 0f;
            var lastGauge = 0f;
            for (var t = 0f; t <= 1.2f; t += 0.002f)
            {
                var pose = CargoFullPopupTimeline.EvaluateOpen(t, timing);
                if (previous <= 0.001f && pose.OverflowFlash > 0.001f)
                {
                    rises++;
                }

                Assert.That(pose.Gauge, Is.GreaterThanOrEqualTo(lastGauge - 0.0001f));
                Assert.That(pose.Interactable, Is.EqualTo(t >= timing.OpenDuration));
                previous = pose.OverflowFlash;
                lastGauge = pose.Gauge;
            }

            Assert.That(rises, Is.EqualTo(1));
            var settled = CargoFullPopupTimeline.EvaluateOpen(timing.OpenDuration, timing);
            var later = CargoFullPopupTimeline.EvaluateOpen(timing.OpenDuration + 5f, timing);
            Assert.That(settled.Gauge, Is.EqualTo(1f));
            Assert.That(settled.Content, Is.EqualTo(1f));
            Assert.That(settled.Reveal, Is.EqualTo(1f));
            Assert.That(settled.OverflowFlash, Is.Zero);
            Assert.That(settled.FrameFlash, Is.Zero);
            Assert.That(settled.ScanAlpha, Is.Zero);
            Assert.That(later.Gauge, Is.EqualTo(settled.Gauge));
            Assert.That(later.OverflowFlash, Is.EqualTo(settled.OverflowFlash));
            Assert.That(later.FrameFlash, Is.EqualTo(settled.FrameFlash));
            Assert.That(later.Content, Is.EqualTo(settled.Content));

            var close = CargoFullPopupTimeline.EvaluateClose(timing.closeDuration, timing);
            Assert.That(close.Finished, Is.True);
            Assert.That(close.Alpha, Is.Zero);
        }

        [Test]
        public void Timeline_LongInspectorValues_AreScaledIntoLimit()
        {
            var slow = new CargoFullPopupTiming { gaugeStart = 1f, gaugeDuration = 2f, closeDuration = 5f }.Sanitized();
            Assert.That(slow.OpenDuration, Is.LessThanOrEqualTo(CargoFullPopupTiming.MaxOpenDuration + 0.0001f));
            Assert.That(slow.closeDuration, Is.LessThanOrEqualTo(CargoFullPopupTiming.MaxCloseDuration));
        }

        [Test]
        public void Presenter_PassesRealShortfallAndReward()
        {
            var env = CreateCargoBlocked(out var view, out var presenter);

            Assert.That(view.CapacityVisible, Is.True);
            Assert.That(view.CapacityTitle, Is.EqualTo(CargoFullPopupText.Title));
            // 구리 2개(3) + 퀘스트 보상 구리 3개(4.5) − 최대 4 = 3.5
            Assert.That(view.Shortfall.CurrentWeight, Is.EqualTo(3f));
            Assert.That(view.Shortfall.MaxCapacity, Is.EqualTo(4f));
            Assert.That(view.Shortfall.Shortfall, Is.EqualTo(3.5f));
            Assert.That(view.Reward.Copper, Is.EqualTo(3));
            Assert.That(env.Rewards.CanFitPending(), Is.False);
            Assert.That(presenter.IsInputLocked, Is.False);
        }

        [Test]
        public void Presenter_RepeatedForfeitClicks_ForfeitOnlyOnce()
        {
            var env = CreateCargoBlocked(out var view, out var presenter, completed: 2);
            var nextObjective = DemoObjectiveIds.Ordered[1];

            presenter.ChooseForfeitReward();
            presenter.ChooseForfeitReward();
            presenter.ChooseForfeitReward();

            Assert.That(env.State.Progress.QuestRewardSettledCount, Is.EqualTo(1));
            Assert.That(env.State.Progress.PendingQuestRewardId, Is.EqualTo(nextObjective));
            Assert.That(env.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(2));
            Assert.That(view.CapacityVisible, Is.False);
            Assert.That(view.ClaimVisible, Is.True);
            Assert.That(presenter.IsInputLocked, Is.False);
        }

        [Test]
        public void Presenter_RepeatedDumpClicks_OpenDumpPanelOnce()
        {
            CreateCargoBlocked(out var view, out var presenter);

            presenter.ChooseDumpInventory();
            presenter.ChooseDumpInventory();
            presenter.ChooseForfeitReward();

            Assert.That(view.DumpShowCount, Is.EqualTo(1));
            Assert.That(presenter.IsDumpOpen, Is.True);

            // 돌아가면 새 값으로 다시 그린다.
            var before = view.CapacityRenderCount;
            presenter.CloseDumpPanel();
            Assert.That(view.CapacityVisible, Is.True);
            Assert.That(view.CapacityRenderCount, Is.GreaterThan(before));
            Assert.That(presenter.IsInputLocked, Is.False);
        }

        [Test]
        public void PopupView_LocksDuringOpen_AcceptsOneChoice_ThenClosesOnce()
        {
            var popup = CreatePopup(out _, out _);
            var closed = 0;
            popup.Closed += () => closed++;
            popup.Render(CargoShortfallModel.From(20f, 20f, 4f), new QuestReward(0, 2, 0, 0));
            popup.Show();

            Assert.That(popup.State, Is.EqualTo(CargoFullPopupView.PopupState.Opening));
            Assert.That(popup.TryAcceptChoice(), Is.False);
            Assert.That(popup.DumpButton.interactable, Is.False);

            var peak = 0f;
            for (var i = 0; i < 40; i++)
            {
                popup.Tick(0.02f);
                peak = Mathf.Max(peak, popup.OverflowFlash);
            }

            Assert.That(popup.State, Is.EqualTo(CargoFullPopupView.PopupState.Open));
            Assert.That(peak, Is.GreaterThan(0.5f));
            Assert.That(popup.OverflowFlash, Is.Zero);
            Assert.That(popup.LitCellCount, Is.EqualTo(12));
            Assert.That(popup.GaugeLayout.OverflowCells, Is.EqualTo(3));
            Assert.That(popup.TitleString, Is.EqualTo("화물 공간 부족"));
            Assert.That(popup.LoadString, Is.EqualTo("20 / 20"));
            Assert.That(popup.ShortfallString, Is.EqualTo("부족한 공간 4"));
            Assert.That(popup.RewardString, Is.EqualTo("철 ×2"));
            Assert.That(popup.ForfeitSubtitleString, Is.EqualTo("철 ×2 수령 포기"));
            Assert.That(popup.DumpButton.interactable, Is.True);

            Assert.That(popup.TryAcceptChoice(), Is.True);
            Assert.That(popup.TryAcceptChoice(), Is.False);
            Assert.That(popup.DumpButton.interactable, Is.False);
            Assert.That(popup.ForfeitButton.interactable, Is.False);

            popup.Close();
            Assert.That(popup.IsClosing, Is.True);
            for (var i = 0; i < 15; i++)
            {
                popup.Tick(0.02f);
            }

            Assert.That(popup.State, Is.EqualTo(CargoFullPopupView.PopupState.Hidden));
            Assert.That(popup.gameObject.activeSelf, Is.False);
            Assert.That(closed, Is.EqualTo(1));
        }

        [Test]
        public void DemoObjectiveView_SimultaneousClicks_RaiseOnlyOneChoice()
        {
            var popup = CreatePopup(out var panel, out var view);
            var dumpRequests = 0;
            var forfeitRequests = 0;
            view.CapacityDumpRequested += () => dumpRequests++;
            view.CapacityForfeitRequested += () => forfeitRequests++;

            view.SetCapacityChoice(CargoShortfallModel.From(20f, 20f, 4f), new QuestReward(0, 2, 0, 0));
            view.SetCapacityChoiceVisible(true);
            view.OnCapacityDumpClicked();
            Assert.That(dumpRequests, Is.Zero, "열기 연출 중 클릭은 막는다");

            popup.Tick(1f);
            view.OnCapacityDumpClicked();
            view.OnCapacityForfeitClicked();
            view.OnCapacityDumpClicked();

            Assert.That(dumpRequests + forfeitRequests, Is.EqualTo(1));
            Assert.That(dumpRequests, Is.EqualTo(1));

            // 닫히는 동안 뜬 선택 창은 접힌 뒤에 이어진다.
            var dumpRoot = new GameObject("QuestRewardDumpPanel", typeof(RectTransform));
            dumpRoot.transform.SetParent(view.transform, false);
            dumpRoot.SetActive(false);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("dumpRoot").objectReferenceValue = dumpRoot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            view.SetCapacityChoiceVisible(false);
            view.SetDumpPanelVisible(true);
            Assert.That(dumpRoot.activeSelf, Is.False);
            popup.Tick(0.3f);
            Assert.That(panel.activeSelf, Is.False);
            Assert.That(dumpRoot.activeSelf, Is.True);
        }

        [Test]
        public void IntegrationScene_CapacityPanelKeepsNamesAndWiring()
        {
            var scene = SceneManager.GetSceneByPath(PromptB152CargoFullPopupBuilder.IntegrationScenePath);
            var closeAfter = !scene.IsValid() || !scene.isLoaded;
            if (closeAfter)
            {
                scene = EditorSceneManager.OpenScene(PromptB152CargoFullPopupBuilder.IntegrationScenePath, OpenSceneMode.Additive);
            }

            try
            {
                var root = Find(scene, "DemoObjectiveRoot");
                var panel = root.transform.Find("QuestRewardCapacityPanel");
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.gameObject.activeSelf, Is.False);
                Assert.That(panel.GetComponent<Canvas>(), Is.Null, "정렬 순서는 그대로 둔다");
                var popup = panel.GetComponent<CargoFullPopupView>();
                Assert.That(popup, Is.Not.Null);
                var view = root.GetComponent<DemoObjectiveView>();
                Assert.That(new SerializedObject(view).FindProperty("capacityPopup").objectReferenceValue, Is.EqualTo(popup));
                Assert.That(view.HasOverflowReferences(), Is.True);

                var dump = panel.Find("CapacityDumpButton").GetComponent<Button>();
                var forfeit = panel.Find("CapacityForfeitButton").GetComponent<Button>();
                Assert.That(dump.onClick.GetPersistentMethodName(0), Is.EqualTo(nameof(DemoObjectiveView.OnCapacityDumpClicked)));
                Assert.That(forfeit.onClick.GetPersistentMethodName(0), Is.EqualTo(nameof(DemoObjectiveView.OnCapacityForfeitClicked)));
                Assert.That(dump.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo(CargoFullPopupText.DumpTitle));
                Assert.That(forfeit.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo(CargoFullPopupText.ForfeitTitle));

                var skin = Resources.Load<CargoFullPopupSkin>(CargoFullPopupSkin.ResourcePath);
                Assert.That(skin, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(skin.ironIcon), Is.EqualTo(PromptB152CargoFullPopupBuilder.IronIconPath));
                Assert.That(AssetDatabase.GetAssetPath(skin.warningIcon), Is.EqualTo(PromptB152CargoFullPopupBuilder.WarningIconPath));
                Assert.That(AssetDatabase.GetAssetPath(skin.trashIcon), Is.EqualTo(PromptB152CargoFullPopupBuilder.TrashIconPath));
            }
            finally
            {
                if (closeAfter && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private CargoFullPopupView CreatePopup(out GameObject panel, out DemoObjectiveView view)
        {
            var canvasObject = new GameObject("CargoCanvas", typeof(RectTransform), typeof(Canvas));
            spawned.Add(canvasObject);
            view = canvasObject.AddComponent<DemoObjectiveView>();
            panel = new GameObject("QuestRewardCapacityPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            var title = NewText(panel.transform, "CapacityTitle");
            var body = NewText(panel.transform, "CapacityBody");
            var dump = NewButton(panel.transform, "CapacityDumpButton");
            var forfeit = NewButton(panel.transform, "CapacityForfeitButton");
            var popup = panel.AddComponent<CargoFullPopupView>();
            popup.Configure(title, body, dump, forfeit);
            popup.ManualTick = true;
            panel.SetActive(false);

            var serialized = new SerializedObject(view);
            serialized.FindProperty("capacityRoot").objectReferenceValue = panel;
            serialized.FindProperty("capacityPopup").objectReferenceValue = popup;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        private static TMP_Text NewText(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<TextMeshProUGUI>();
        }

        private static Button NewButton(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            NewText(go.transform, "Label");
            return go.GetComponent<Button>();
        }

        private static Env CreateCargoBlocked(out CountingView view, out DemoObjectivePresenter presenter, int completed = 1)
        {
            var state = GameState.CreateNew();
            var inventory = new InventoryService(CreateCatalog(), 4f, state);
            var rewards = new QuestRewardService();
            rewards.Bind(inventory, state);
            inventory.TryAddMineral(DataIds.Minerals.Copper, 2);
            state.SetDemoProgress(DemoObjectiveIds.Ordered[completed], completed, false);

            view = new CountingView();
            presenter = new DemoObjectivePresenter(view);
            var director = new DemoObjectiveDirector();
            director.BindGameState(state);
            director.RestoreFromProgress(state.Progress);
            presenter.Bind(director, rewards);
            presenter.ConfirmClaim();
            Assert.That(view.CapacityVisible, Is.True, "준비: 첫 보상(블록 제거, 구리 3)이 화물 부족으로 막혀야 한다");
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

        private sealed class CountingView : IDemoObjectiveView
        {
            public bool CapacityVisible { get; private set; }
            public bool DumpVisible { get; private set; }
            public bool ClaimVisible { get; private set; }
            public int DumpShowCount { get; private set; }
            public int CapacityRenderCount { get; private set; }
            public string CapacityTitle { get; private set; } = string.Empty;
            public CargoShortfallModel Shortfall { get; private set; }
            public QuestReward Reward { get; private set; }

            public void SetObjective(DemoObjectiveReadModel model) { }
            public void SetGuidanceVisible(bool visible) { }
            public void SetGuidanceText(string title, string body) { }
            public void SetInputLocked(bool locked) { }
            public void SetHazardYield(bool yieldToHazard) { }
            public void SetDemoCompleteVisible(bool visible, string summary) { }
            public void SetDetailsVisible(bool visible) { }
            public void SetDetailsText(string title, string body, string nextAction) { }
            public void SetDetailsReward(string rewardText) { }
            public void SetDetailsStatus(string statusText) { }
            public void SetDetailsIndex(string indexText) { }
            public void SetDetailsNavInteractable(bool previousEnabled, bool nextEnabled) { }
            public void SetCapacityChoiceVisible(bool visible) => CapacityVisible = visible;
            public void SetCapacityChoiceText(string title, string body) => CapacityTitle = title ?? string.Empty;

            public void SetCapacityChoice(CargoShortfallModel shortfall, QuestReward reward)
            {
                Shortfall = shortfall;
                Reward = reward;
                CapacityRenderCount++;
            }

            public void SetDumpPanelVisible(bool visible)
            {
                if (visible && !DumpVisible)
                {
                    DumpShowCount++;
                }

                DumpVisible = visible;
            }

            public void SetDumpSummary(string summary) { }
            public void SetDumpChoice(CargoShortfallModel shortfall, QuestReward reward) { }
            public void SetDumpRows(IReadOnlyList<QuestDumpRow> rows) { }
            public void SetClaimVisible(bool visible) => ClaimVisible = visible;
            public void SetClaimText(string title, string questTitle, string rewardText, string hint) { }
        }
    }
}
