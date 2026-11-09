using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Tutorial;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    public sealed class PromptB182SfxCargoTests
    {
        private const string Copper = "mineral.copper";
        private const string Iron = "mineral.iron";
        private const string Lithium = "mineral.lithium";
        private readonly List<SfxId> played = new List<SfxId>();
        private GameObject canvas;
        private DemoObjectiveView view;
        private CargoFullPopupView full;
        private CargoDiscardPopupView dump;
        private DemoObjectivePresenter presenter;
        private float now;

        private static void EnableSfx(bool enabled) => typeof(CargoFullPopupView)
            .GetField("SfxInEditModeForTests", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, enabled);

        [SetUp]
        public void SetUp()
        {
            now = 0f;
            played.Clear();
            EnableSfx(true);
            SfxRequests.PlayRequested += Record;
            WindowTvSfx.ResetForTests(Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath), () => now, () => false);
            canvas = new GameObject("B182Canvas", typeof(RectTransform), typeof(Canvas));
            view = canvas.AddComponent<DemoObjectiveView>();
            var fullRoot = Panel("QuestRewardCapacityPanel");
            var title = Label(fullRoot.transform, "CapacityTitle");
            var body = Label(fullRoot.transform, "CapacityBody");
            full = fullRoot.AddComponent<CargoFullPopupView>();
            full.Configure(title, body, Button(fullRoot.transform, "CapacityDumpButton"),
                Button(fullRoot.transform, "CapacityForfeitButton"));
            full.ManualTick = true;
            fullRoot.SetActive(false);
            var dumpRoot = Panel("QuestRewardDumpPanel");
            dump = dumpRoot.AddComponent<CargoDiscardPopupView>();
            dump.ManualTick = true;
            dumpRoot.SetActive(false);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("capacityRoot").objectReferenceValue = fullRoot;
            serialized.FindProperty("capacityPopup").objectReferenceValue = full;
            serialized.FindProperty("dumpRoot").objectReferenceValue = dumpRoot;
            serialized.FindProperty("dumpPopup").objectReferenceValue = dump;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var throttle = (SfxThrottle)typeof(DemoObjectiveView).GetField("discardSfxThrottle",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
            throttle.Clock = () => now;
        }

        [TearDown]
        public void TearDown()
        {
            presenter?.Dispose();
            SfxRequests.PlayRequested -= Record;
            EnableSfx(false);
            WindowTvSfx.ReleaseForTests();
            if (canvas != null) Object.DestroyImmediate(canvas);
        }

        private void Record(SfxId id, float pitch) => played.Add(id);
        private GameObject Panel(string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            return root;
        }

        private static TMP_Text Label(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            root.transform.SetParent(parent, false);
            return root.GetComponent<TMP_Text>();
        }

        private static UnityEngine.UI.Button Button(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button));
            root.transform.SetParent(parent, false);
            Label(root.transform, "Label");
            return root.GetComponent<UnityEngine.UI.Button>();
        }

        private void RenderDump(float weight = 200f, int copper = 100)
        {
            view.SetDumpChoice(CargoShortfallModel.From(weight, 200f, 100f), new QuestReward(10, 0, 0, 0));
            view.SetDumpRows(new[] { new QuestDumpRow(Copper, "구리", copper, 1.5f),
                new QuestDumpRow(Iron, "철", 2, 2f), new QuestDumpRow(Lithium, "리튬", 0, 0.8f) });
        }

        private void OpenDump()
        {
            RenderDump();
            view.SetDumpPanelVisible(true);
            dump.Tick(1f);
            played.Clear();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FreshOpen_OverflowOnce_EvenWhenSkippingEnd(bool largeStep)
        {
            full.Render(CargoShortfallModel.From(200f, 200f, 10f), new QuestReward(1, 0, 0, 0));
            full.Show(false);
            full.Show(false);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvOpen));
            Assert.That(WindowTvSfx.LastVolumeScale, Is.EqualTo(0.6f));
            if (!largeStep)
            {
                full.Tick(full.Timing.GaugeEnd - 0.001f);
                Assert.That(played, Is.Empty);
                full.Tick(0.002f);
            }
            full.Tick(2f);
            full.Tick(2f);
            Assert.That(played, Is.EqualTo(new[] { SfxId.CargoHeavyWarning }));
        }

        [Test]
        public void PageOpen_PlaysImmediatelyOnce_OverflowAndCleanupStayQuiet()
        {
            full.Show(true);
            full.Show(true);
            Assert.That(full.Clock, Is.Zero);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1));
            full.Tick(2f);
            full.HideImmediate();
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1));
            Assert.That(played, Is.Empty);
            full.Show(false);
            full.HideImmediate();
            full.Tick(2f);
            full.Show(false);
            full.gameObject.SetActive(false);
            // EditMode의 비실행 MonoBehaviour는 OnDisable을 자동으로 호출하지 않는다.
            typeof(CargoFullPopupView).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(full, null);
            full.Tick(2f);
            Assert.That(played, Is.Empty);
            WindowTvSfx.ResetForTests(Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath), () => now, () => false);
            EnableSfx(false);
            full.Show(false);
            full.Tick(2f);
            full.DumpButton.GetComponent<UiSfxButton>().OnPointerEnter(null);
            full.Close();
            full.Tick(2f);
            dump.Show(false);
            dump.Tick(2f);
            dump.BackButton.GetComponent<UiSfxButton>().OnPointerEnter(null);
            dump.Close(CargoDiscardPopupView.CloseMode.Complete);
            dump.Tick(2f);
            Assert.That(WindowTvSfx.StartedCount, Is.Zero);
            Assert.That(played, Is.Empty);
        }

        [Test]
        public void AcceptedPageAndBack_TvOnceAtEachDisplay_NoOverflowOnReturn()
        {
            full.Show(false);
            full.Tick(2f);
            RenderDump();
            view.CapacityDumpRequested += () => { view.SetCapacityChoiceVisible(false); view.SetDumpPanelVisible(true); };
            played.Clear();
            view.OnCapacityDumpClicked();
            view.OnCapacityDumpClicked();
            Assert.That(played, Is.EqualTo(new[] { SfxId.KeysPageTurn }));
            full.Tick(1f);
            Assert.That(dump.State, Is.EqualTo(CargoDiscardPopupView.PopupState.Opening));
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(2));
            dump.Tick(1f);
            var tvBefore = WindowTvSfx.StartedCount;
            view.DumpClosedRequested += () => { view.SetDumpPanelVisible(false); view.SetCapacityChoiceVisible(true); };
            played.Clear();
            view.OnDumpClosedClicked();
            view.OnDumpClosedClicked();
            dump.Tick(1f);
            Assert.That(full.Clock, Is.Zero);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(tvBefore + 1));
            full.Tick(1f);
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiClick }));
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(tvBefore + 1));
        }

        [Test]
        public void Forfeit_Once_NoFoldSound()
        {
            full.Show(false);
            full.Tick(2f);
            played.Clear();
            var tvBefore = WindowTvSfx.StartedCount;
            view.CapacityForfeitRequested += () => view.SetCapacityChoiceVisible(false);
            view.OnCapacityForfeitClicked();
            view.OnCapacityForfeitClicked();
            full.Tick(1f);
            Assert.That(played, Is.EqualTo(new[] { SfxId.StorageTakeAll }));
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(tvBefore));
        }

        [TestCase(CargoDiscardKind.One, SfxId.UiScanTick)]
        [TestCase(CargoDiscardKind.Five, SfxId.UiScanTick)]
        [TestCase(CargoDiscardKind.Ten, SfxId.UiScanTick)]
        [TestCase(CargoDiscardKind.Needed, SfxId.UiScanTick)]
        [TestCase(CargoDiscardKind.All, SfxId.StorageTakeAll)]
        public void ActualDiscard_PlaysChosenSound_ThrottlesBothKinds(CargoDiscardKind kind, SfxId sound)
        {
            OpenDump();
            var weight = 200f;
            view.DumpRequested += (_, quantity) => RenderDump(weight -= Math.Min(2, quantity));
            // 필요·전부도 모자라는 철 행으로 검사한다.
            dump.GetButton(Iron, kind).onClick.Invoke();
            dump.GetButton(Iron, kind).onClick.Invoke();
            Assert.That(played, Is.EqualTo(new[] { sound }));
            now = 0.081f;
            dump.GetButton(Iron, kind).onClick.Invoke();
            Assert.That(played, Is.EqualTo(new[] { sound, sound }));
        }

        [Test]
        public void IgnoredOrEmptyClicks_NoSound_AndOpeningLocksHover()
        {
            RenderDump();
            view.SetDumpPanelVisible(true);
            dump.GetButton(Copper, CargoDiscardKind.One).onClick.Invoke();
            dump.BackButton.GetComponent<UiSfxButton>().OnPointerEnter(null);
            Assert.That(played, Is.Empty);
            dump.Tick(1f);
            dump.GetButton(Lithium, CargoDiscardKind.All).onClick.Invoke();
            Assert.That(played, Is.Empty);
        }

        [Test]
        public void AllButtons_HoverOnly_NoDisabledFailure_InitializationQuiet()
        {
            full.Render(CargoShortfallModel.From(200f, 200f, 10f), QuestReward.None);
            RenderDump();
            var buttons = canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            Assert.That(buttons.Length, Is.EqualTo(18));
            foreach (var button in buttons)
            {
                var sfx = button.GetComponent<UiSfxButton>();
                Assert.That(sfx, Is.Not.Null, button.name);
                Assert.That(sfx.ClickSfx, Is.EqualTo(SfxId.None));
                Assert.That(sfx.Hover, Is.True);
                Assert.That(sfx.FailWhenDisabled, Is.False);
            }
            Assert.That(played, Is.Empty);
            Assert.That(WindowTvSfx.StartedCount, Is.Zero);
            full.Show(false);
            full.DumpButton.GetComponent<UiSfxButton>().OnPointerEnter(null);
            Assert.That(played, Is.Empty);
            full.Tick(1f);
            played.Clear();
            full.DumpButton.GetComponent<UiSfxButton>().OnPointerEnter(null);
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiHover }));
        }

        [TestCase(CargoDiscardPopupView.CloseMode.Complete)]
        [TestCase(CargoDiscardPopupView.CloseMode.Page)]
        [TestCase(CargoDiscardPopupView.CloseMode.Fold)]
        public void CloseModes_FoldOnce_CompleteWaits_PageQuiet(CargoDiscardPopupView.CloseMode mode)
        {
            OpenDump();
            var before = WindowTvSfx.StartedCount;
            dump.Close(mode);
            if (mode == CargoDiscardPopupView.CloseMode.Complete)
            {
                dump.Tick(dump.Timing.completeHold - 0.001f);
                Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(before));
                dump.Tick(0.002f);
            }
            dump.Close(mode);
            dump.Tick(2f);
            dump.Tick(2f);
            Assert.That(WindowTvSfx.StartedCount - before, Is.EqualTo(mode == CargoDiscardPopupView.CloseMode.Page ? 0 : 1));
            if (mode != CargoDiscardPopupView.CloseMode.Page)
            {
                Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvClose));
                Assert.That(WindowTvSfx.LastVolumeScale, Is.EqualTo(0.6f));
            }
        }

        [Test]
        public void MissingPageTarget_FoldsOnce()
        {
            full.Show(false);
            full.Tick(1f);
            view.CapacityDumpRequested += () => view.SetCapacityChoiceVisible(false);
            view.OnCapacityDumpClicked();
            var before = WindowTvSfx.StartedCount;
            full.FoldInsteadOfPage();
            full.Tick(1f);
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvClose));
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(before));
            OpenDump();
            dump.Close(CargoDiscardPopupView.CloseMode.Page);
            before = WindowTvSfx.StartedCount;
            dump.FoldInsteadOfPage();
            dump.FoldInsteadOfPage();
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(before + 1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LastDiscard_RealRewardsGrantedOnce_NoTickOrAll_NoNewRewardCue(bool all)
        {
            var state = GameState.CreateNew();
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(Copper, 1.5f, 10, "구리");
            catalog.Register(Iron, 2f, 15, "철");
            catalog.Register(Lithium, 0.8f, 40, "리튬");
            var inventory = new InventoryService(catalog, 20f, state);
            inventory.TryAddMineral(Copper, 8);
            inventory.TryAddMineral(Iron, 4);
            state.SetDemoProgress(DemoObjectiveIds.Ordered[1], 1, false);
            var rewards = new QuestRewardService();
            rewards.Bind(inventory, state);
            var director = new DemoObjectiveDirector();
            director.BindGameState(state);
            director.RestoreFromProgress(state.Progress);
            presenter = new DemoObjectivePresenter(view);
            view.CapacityDumpRequested += presenter.ChooseDumpInventory;
            view.DumpRequested += presenter.DumpMineral;
            presenter.Bind(director, rewards);
            Assert.That(WindowTvSfx.StartedCount, Is.Zero, "Bind 자체는 무음");
            presenter.ConfirmClaim();
            full.Tick(1f);
            view.OnCapacityDumpClicked();
            full.Tick(1f);
            dump.Tick(1f);
            var grants = 0;
            rewards.GrantResolved += result => { if (result.Status == QuestRewardGrantStatus.Granted) grants++; };
            played.Clear();
            var before = WindowTvSfx.StartedCount;
            if (all) view.OnDumpCopperAllClicked();
            else view.OnDumpCopperNeededClicked();
            Assert.That(dump.ActiveCloseMode, Is.EqualTo(CargoDiscardPopupView.CloseMode.Complete));
            Assert.That(played, Is.Empty, "B-180-1 이후 지급 순간에는 87도 재생하지 않는다");
            Assert.That(grants, Is.EqualTo(1));
            view.OnDumpCopperAllClicked();
            Assert.That(grants, Is.EqualTo(1));
            dump.Tick(dump.Timing.completeHold - 0.001f);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(before));
            dump.Tick(0.002f);
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvClose));
        }

        [Test]
        public void QuietBindOrUnbind_CancelsPendingOverflowAndCompleteClose()
        {
            full.Show(false);
            OpenDump();
            dump.Close(CargoDiscardPopupView.CloseMode.Complete);
            var before = WindowTvSfx.StartedCount;
            typeof(DemoObjectiveView).GetMethod("SetCargoSfxQuiet", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(view, new object[] { true });
            typeof(DemoObjectiveView).GetMethod("SetCargoSfxQuiet", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(view, new object[] { false });
            full.Tick(2f);
            dump.Tick(2f);
            Assert.That(played, Is.Empty);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(before));
        }
    }
}
