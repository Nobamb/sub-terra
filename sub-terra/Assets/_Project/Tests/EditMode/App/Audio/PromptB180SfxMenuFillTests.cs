using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Guide;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Outpost;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.App.UI.Tutorial;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    /// <summary>B-180: 조용하던 메뉴·창 버튼을 기존 소리로 채움. SfxRequests 기록으로 확인한다.</summary>
    public sealed class PromptB180SfxMenuFillTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly string[] MutePrefKeys =
        {
            "subterra.settings.masterVolume",
            SettingsRuntimeApplier.PrefMasterMuted,
            SettingsRuntimeApplier.PrefBgmVolume,
            SettingsRuntimeApplier.PrefSfxVolume,
            SettingsRuntimeApplier.PrefBgmMuted,
            SettingsRuntimeApplier.PrefSfxMuted
        };

        private readonly List<SfxId> played = new List<SfxId>();
        private readonly List<GameObject> created = new List<GameObject>();
        private readonly List<SfxPlayer> players = new List<SfxPlayer>();
        private readonly Dictionary<string, float> savedPrefs = new Dictionary<string, float>();
        private bool prefsSaved;
        private float oldListenerVolume;
        private string testRoot;

        [SetUp]
        public void SetUp()
        {
            played.Clear();
            SfxRequests.PlayRequested += Record;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            foreach (var player in players) if (player != null) player.ReleaseForTests();
            players.Clear();
            foreach (var go in created) if (go != null) Object.DestroyImmediate(go);
            created.Clear();
            if (prefsSaved) RestorePrefs();
            if (!string.IsNullOrEmpty(testRoot) && Directory.Exists(testRoot)) Directory.Delete(testRoot, true);
            testRoot = null;
        }

        private void Record(SfxId id, float pitch) => played.Add(id);

        private int Count(SfxId id)
        {
            var count = 0;
            foreach (var value in played) if (value == id) count++;
            return count;
        }

        private static object Field(object target, string name)
        {
            var field = target.GetType().GetField(name, Private);
            Assert.That(field, Is.Not.Null, name);
            return field.GetValue(target);
        }

        private static void Set(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, Private);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static object Call(object target, string name, params object[] args)
        {
            var method = target.GetType().GetMethod(name, Private | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, name);
            return method.Invoke(target, args);
        }

        private GameObject Track(GameObject go)
        {
            created.Add(go);
            return go;
        }

        private GameObject NewCanvas()
        {
            var canvas = new GameObject("B180Canvas", typeof(RectTransform), typeof(Canvas));
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920f, 1080f);
            return Track(canvas);
        }

        private GameObject SpawnPrefab(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/" + name + ".prefab");
            Assert.That(prefab, Is.Not.Null, name);
            return Track((GameObject)PrefabUtility.InstantiatePrefab(prefab));
        }

        private static UiSfxButton Sound(Button button)
        {
            Assert.That(button, Is.Not.Null);
            var sound = button.GetComponent<UiSfxButton>();
            Assert.That(sound, Is.Not.Null, button.name);
            Assert.That(button.GetComponents<UiSfxButton>().Length, Is.EqualTo(1), button.name + " 한 번만 붙음");
            return sound;
        }

        private static void AssertHoverOnly(Button button)
        {
            var sound = Sound(button);
            Assert.That(sound.ClickSfx, Is.EqualTo(SfxId.None), button.name);
            Assert.That(sound.Hover, Is.True, button.name);
        }

        // EditMode에서는 Update가 돌지 않으므로 B-154 검사처럼 상태 전이만 진행한다.
        private static void ForceShown(StartBriefingPopupMotion transition)
        {
            if (Field(transition, "planner") == null) Call(transition, "Awake");
            var lifecycle = (BriefingLifecycle)Field(transition, "lifecycle");
            lifecycle.Begin();
            lifecycle.TryStartIntro();
            lifecycle.TryFinishIntro();
            Assert.That(transition.IsShown, Is.True);
        }

        // ---- A. 게임 가이드 ----

        private static void Run(GameGuidePopupView popup, float seconds)
        {
            for (var t = 0f; t < seconds - 0.0001f; t += 0.02f) popup.Tick(0.02f);
        }

        [Test]
        public void Guide_AllButtonsClickAndHover_CloseHoverOnly_SilentWhileOpeningAndClosing()
        {
            var popup = GameGuidePopupView.Create(NewCanvas().transform, null);
            popup.UseState(new GameGuideState());
            popup.ManualTick = true;
            popup.Show();
            Assert.That(popup.CanPlayButtonSfx, Is.False, "열리는 중");
            played.Clear();
            Sound(popup.TabButtons[1].Button).OnPointerEnter(null);
            Sound(popup.CloseButton.Button).OnPointerEnter(null);
            Assert.That(played, Is.Empty, "열림 연출 중 호버음 0번");

            Run(popup, GameGuideTimeline.FullOpenDuration + 0.1f);
            Assert.That(popup.State, Is.EqualTo(GameGuidePopupView.PopupState.Open));
            Assert.That(popup.CanPlayButtonSfx, Is.True);

            var buttons = 0;
            foreach (var guideButton in popup.GetComponentsInChildren<GameGuideButton>(true))
            {
                var sound = Sound(guideButton.Button);
                var isClose = guideButton == popup.CloseButton;
                Assert.That(sound.ClickSfx, Is.EqualTo(isClose ? SfxId.None : GameGuidePopupView.GuideClickSfx), guideButton.name);
                Assert.That(sound.Hover, Is.True, guideButton.name);
                Assert.That(sound.HasHoverGate, Is.True, guideButton.name);
                Assert.That(sound.FailWhenDisabled, Is.False, guideButton.name);
                buttons++;
            }

            // 탭 3·필터 3·접기 1·작은 카드 5·X 1 이상
            Assert.That(buttons, Is.GreaterThanOrEqualTo(13));
            var cards = popup.GetComponentsInChildren<GameGuideCardView>(true);
            Assert.That(cards.Length, Is.GreaterThan(0));
            foreach (var card in cards)
            {
                var sound = Sound(card.Button);
                Assert.That(sound.ClickSfx, Is.EqualTo(SfxId.UiClick), card.name);
                Assert.That(sound.Hover, Is.True, card.name);
            }

            Assert.That(GameGuidePopupView.GuideClickSfx, Is.EqualTo(SfxId.UiClick));

            played.Clear();
            var tab = popup.TabButtons[1].Button;
            tab.onClick.Invoke();
            Assert.That(Count(SfxId.UiClick), Is.EqualTo(1), "탭 클릭");
            tab.onClick.Invoke();
            Assert.That(Count(SfxId.UiClick), Is.EqualTo(2), "이미 고른 탭도 누르면 한 번");
            popup.FilterButtons[2].Button.onClick.Invoke();
            popup.FirstToggle.Button.onClick.Invoke();
            Assert.That(Count(SfxId.UiClick), Is.EqualTo(4), "필터·접기");
            cards[0].Button.onClick.Invoke();
            Assert.That(Count(SfxId.UiClick), Is.EqualTo(5), "카드");

            played.Clear();
            popup.CloseButton.Button.onClick.Invoke();
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiPopupClose }), "X는 닫힘음만");
            played.Clear();
            Sound(tab).OnPointerEnter(null);
            Assert.That(played, Is.Empty, "닫힘 연출 중 호버음 0번");
        }

        // ---- B. CCTV 시설 목록 ----

        private static List<OutpostFacilityReadModel> Facilities(int count)
        {
            var result = new List<OutpostFacilityReadModel>();
            for (var i = 0; i < count; i++)
            {
                var charger = i % 2 == 0;
                result.Add(new OutpostFacilityReadModel(
                    (charger ? "charger-" : "clinic-") + i.ToString("D4"),
                    charger ? DataIds.Buildings.ChargerBasic : DataIds.Buildings.ClinicBasic,
                    true,
                    string.Empty));
            }

            return result;
        }

        [Test]
        public void Cctv_FacilityItems_ClickOnceAfterRebuild_HoverOnlyWhileLive()
        {
            var view = SpawnPrefab("OutpostPanel").GetComponent<OutpostPanelView>();
            view.SetMode(OutpostPanelMode.Core);
            view.SetVisible(true);
            view.SetFacilities(Facilities(3));
            var popup = view.CorePopup;
            Assert.That(popup.CanPlayItemSfx, Is.False, "목록이 드러나기 전");
            popup.Tick(CoreCctvTimeline.DecideTime + 0.02f);
            popup.Tick(CoreCctvTimeline.RevealStartConnected - popup.Clock + 0.02f);
            popup.Tick(CoreCctvTimeline.IntroDuration(3));
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));

            // 목록을 다시 그려 항목을 재사용해도 소리 컴포넌트는 하나다.
            view.SetFacilities(Facilities(2));
            view.SetFacilities(Facilities(3));
            for (var i = 0; i < 3; i++)
            {
                var sound = Sound(popup.ItemAt(i).Button);
                Assert.That(sound.ClickSfx, Is.EqualTo(CoreCctvPopupView.CctvItemClickSfx));
                Assert.That(sound.Hover, Is.True);
                Assert.That(sound.FailWhenDisabled, Is.False);
            }

            Assert.That(CoreCctvPopupView.CctvItemClickSfx, Is.EqualTo(SfxId.UiClick));
            played.Clear();
            popup.ItemAt(1).Button.onClick.Invoke();
            // 항목 자체의 선택 리스너는 Awake에서 붙어 EditMode에서는 돌지 않는다. 소리 컴포넌트는 따로 듣는다.
            Assert.That(Count(SfxId.UiClick), Is.EqualTo(1));

            played.Clear();
            popup.Hide();
            played.Clear();
            Sound(popup.ItemAt(0).Button).OnPointerEnter(null);
            Assert.That(played, Is.Empty, "닫는 중 호버음 0번");
        }

        // ---- C. 작은 글리치 창 닫기 호버 ----

        [Test]
        public void QuestClearClose_HoverOnlyWhenShown_ClickSilent_ClosingSilent()
        {
            var root = new GameObject("B180QuestClear", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(NewCanvas().transform, false);
            ((RectTransform)root.transform).sizeDelta = new Vector2(900f, 450f);
            var close = new GameObject(QuestClearPopupMotion.CloseButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            close.transform.SetParent(root.transform, false);
            var popup = root.AddComponent<QuestClearPopupMotion>();
            Call(popup, "Awake");
            var button = close.GetComponent<Button>();
            AssertHoverOnly(button);
            Assert.That(Sound(button).FailWhenDisabled, Is.False);

            played.Clear();
            Sound(button).OnPointerEnter(null);
            Assert.That(played, Is.Empty, "등장 연출 중 0번");

            ForceShown(root.GetComponent<StartBriefingPopupMotion>());
            Assert.That(popup.CanPlayCloseHover, Is.True);
            Sound(button).OnPointerEnter(null);
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiHover }));
            played.Clear();
            button.onClick.Invoke();
            Assert.That(played, Is.Empty, "클릭은 B-178 닫힘음·보상음 몫");

            Set(popup, "closing", true);
            Sound(button).OnPointerEnter(null);
            Assert.That(played, Is.Empty, "닫는 중 0번");
        }

        [Test]
        public void DeepZoneUnlockClose_HoverOnlyWhenShown_ClosingSilent()
        {
            var skin = DeepZoneUnlockPopupSkin.Load();
            Assert.That(skin, Is.Not.Null, "Run SubTerra/UI/Build Prompt-B 154 Deep Zone Unlock Popup first.");
            var parts = DeepZoneUnlockPopupMotion.Create(NewCanvas().transform, skin, 100);
            var motion = parts.Root.GetComponent<DeepZoneUnlockPopupMotion>();
            AssertHoverOnly(parts.CloseButton);

            played.Clear();
            Sound(parts.CloseButton).OnPointerEnter(null);
            Assert.That(played, Is.Empty, "등장 전 0번");

            ForceShown(motion.Transition);
            Sound(parts.CloseButton).OnPointerEnter(null);
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiHover }));

            ((BriefingLifecycle)Field(motion.Transition, "lifecycle")).TryBeginClose();
            Assert.That(motion.CanPlayCloseHover, Is.False);
            played.Clear();
            Sound(parts.CloseButton).OnPointerEnter(null);
            Assert.That(played, Is.Empty, "닫는 중 0번");
        }

        // ---- D. 퀘스트 이전/다음 ----

        [Test]
        public void QuestDetails_PrevNext_PageTurnOnlyWhenIndexChanges()
        {
            Assert.That(DemoObjectivePresenter.QuestPageTurnSfx, Is.EqualTo(SfxId.KeysPageTurn));
            var presenter = new DemoObjectivePresenter(null);
            presenter.Bind(new DemoObjectiveDirector());
            presenter.ShowNextQuest();
            Assert.That(played, Is.Empty, "상세창이 닫혀 있으면 0번");

            presenter.OpenDetails();
            played.Clear();
            presenter.ShowPreviousQuest();
            Assert.That(played, Is.Empty, "처음에서 이전 0번");
            presenter.ShowNextQuest();
            presenter.ShowNextQuest();
            presenter.ShowPreviousQuest();
            Assert.That(played, Is.EqualTo(new[] { SfxId.KeysPageTurn, SfxId.KeysPageTurn, SfxId.KeysPageTurn }));

            for (var i = 0; i < DemoObjectiveIds.RequiredCount + 2; i++) presenter.ShowNextQuest();
            Assert.That(Count(SfxId.KeysPageTurn), Is.EqualTo(3 + DemoObjectiveIds.RequiredCount - 2), "끝에서 다음 0번");

            presenter.CloseDetails();
            presenter.ShowPreviousQuest();
            Assert.That(Count(SfxId.KeysPageTurn), Is.EqualTo(3 + DemoObjectiveIds.RequiredCount - 2));
        }

        [Test]
        public void QuestDetails_PrevNextButtons_HoverOnly()
        {
            var view = SpawnPrefab("SurfaceBasePanel").GetComponentInChildren<DemoObjectiveView>(true);
            Assert.That(view, Is.Not.Null);
            Call(view, "Awake");
            Call(view, "Awake");
            AssertHoverOnly((Button)Field(view, "detailsPrevButton"));
            AssertHoverOnly((Button)Field(view, "detailsNextButton"));
        }

        // ---- E. 슬롯 삭제 ----

        private LoadService NewSaveFolderWithSlot1()
        {
            testRoot = Path.Combine(Path.GetTempPath(), "subterra-b180-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testRoot);
            var paths = new SavePathPolicy(testRoot);
            var fileSystem = new PhysicalSaveFileSystem();
            var mapper = new SaveDataMapper(new SystemSaveClock());
            var json = new SaveJsonCodec(new SaveMigrationService());
            var save = new SaveService(fileSystem, paths, mapper, json);
            var state = GameState.CreateNew();
            var context = new SaveCaptureContext(state, new InventoryState(), new UpgradeState(), null, null,
                SceneNames.SurfaceBase, "b180-test");
            Assert.That(save.Save(1, context).IsSuccess, Is.True);
            return new LoadService(fileSystem, paths, mapper, json);
        }

        [Test]
        public void DeleteSlot_TrashConfirmCancel_SoundsFollowResult()
        {
            var view = new SilentMenuView();
            using (var presenter = new MainMenuPresenter(view, NewSaveFolderWithSlot1(), "test"))
            {
                presenter.Refresh();
                var binder = Track(new GameObject("B180Binder")).AddComponent<MainMenuBinder>();
                // 삭제 확정 이벤트를 구독하지 않으므로 실제 파일은 지우지 않는다.
                Set(binder, "presenter", presenter);
                played.Clear();

                Call(binder, "OnDeleteCancel");
                Assert.That(played, Is.Empty, "확인 창 없을 때 X 키 0번");

                Call(binder, "OnDeleteSlotClicked", 1);
                Assert.That(played, Is.EqualTo(new[] { SfxId.UiClick, SfxId.UiPopupOpen }));
                played.Clear();
                Call(binder, "OnDeleteCancel");
                Assert.That(played, Is.EqualTo(new[] { SfxId.UiPopupClose }), "취소·X");

                played.Clear();
                Call(binder, "OnDeleteSlotClicked", 2);
                Assert.That(played, Is.EqualTo(new[] { SfxId.UiClick }), "확인 창이 뜨지 않으면 클릭음만");

                played.Clear();
                Call(binder, "OnDeleteSlotClicked", 1);
                Call(binder, "OnDeleteConfirm");
                Assert.That(played, Is.EqualTo(new[] { SfxId.UiClick, SfxId.UiPopupOpen, SfxId.RunFailBlackout }));
                played.Clear();
                Call(binder, "OnDeleteConfirm");
                Call(binder, "OnDeleteCancel");
                Assert.That(played, Is.Empty, "확인 창이 닫힌 뒤 확정·취소 0번");

                Call(binder, "OnContinueClicked");
                Assert.That(played, Is.EqualTo(new[] { SfxId.UiConfirm }), "이어하기 확인음은 그대로 한 번");
                played.Clear();
                Set(binder, "continuing", true);
                Call(binder, "OnDeleteSlotClicked", 1);
                Assert.That(played, Is.Empty, "이어하기 중 휴지통 무음");
            }

            Assert.That(MainMenuBinder.DeleteTrashSfx, Is.EqualTo(SfxId.UiClick));
            Assert.That(MainMenuBinder.DeleteConfirmOpenSfx, Is.EqualTo(SfxId.UiPopupOpen));
            Assert.That(MainMenuBinder.DeleteConfirmSfx, Is.EqualTo(SfxId.RunFailBlackout));
            Assert.That(MainMenuBinder.DeleteCancelSfx, Is.EqualTo(SfxId.UiPopupClose));
        }

        [Test]
        public void DeleteSlot_TrashAndConfirmWindowButtons_HoverOnly()
        {
            var main = SpawnPrefab("MainMenuPanel");
            var view = main.GetComponent<MainMenuView>();
            Call(view, "OnEnable");
            foreach (var name in new[] { "deleteConfirmButton", "deleteCancelButton", "deleteCloseButton" })
                AssertHoverOnly((Button)Field(view, name));
            Call(view, "OnDisable");

            var cards = main.GetComponentsInChildren<SaveSlotCardView>(true);
            Assert.That(cards.Length, Is.EqualTo(3));
            foreach (var card in cards)
            {
                Call(card, "Awake");
                var trash = (Button)Field(card, "deleteButton");
                AssertHoverOnly(trash);
                Assert.That(Sound(trash).FailWhenDisabled, Is.False);
            }
        }

        private sealed class SilentMenuView : IMainMenuView
        {
            public void SetSlotDisplay(int slotId, string label, bool canContinue, string statusText) { }
            public void SetSelectedSlot(int slotId, bool canContinue, string message) { }
            public void SetPrimaryAction(string label, bool enabled) { }
            public void SetOverwriteConfirmVisible(bool visible, int slotId) { }
            public void SetSettingsVisible(bool visible) { }
            public void SetSettingsDraft(SettingsValues values) { }
            public void SetVersionLabel(string version) { }
            public void SetMessage(string message) { }
            public void SetQuitBlockedMessage(string message) { }
            public void SetSlotKind(int slotId, SaveSlotCardKind kind) { }
            public void SetDeleteConfirmVisible(bool visible, int slotId) { }
        }

        // ---- F. 카드 접힘 ----

        [Test]
        public void ExitFold_PlaysOnceAtFoldStart_CancelExitSilent()
        {
            Assert.That(MainMenuIntroDirector.ExitFoldSfx, Is.EqualTo(SfxId.WindowTvClose));
            var intro = Track(new GameObject("B180Intro", typeof(RectTransform))).AddComponent<MainMenuIntroDirector>();
            var timeline = new MainMenuExitTimeline(new MainMenuExitSettings());
            Assert.That(timeline.FoldStart, Is.EqualTo(new MainMenuExitSettings().flashDuration * 0.5f));
            Set(intro, "exit", timeline);
            Set(intro, "exiting", true);
            played.Clear();

            Call(intro, "TickExit", timeline.FoldStart * 0.5f);
            Assert.That(played, Is.Empty, "번쩍임 중");
            Call(intro, "TickExit", timeline.FoldStart);
            Assert.That(played, Is.EqualTo(new[] { SfxId.WindowTvClose }), "접힘 시작에 한 번");
            var guard = 0;
            while (intro.IsExiting && guard++ < 100) Call(intro, "TickExit", 0.02f);
            Assert.That(intro.IsExiting, Is.False, "연출 끝");
            Assert.That(played, Is.EqualTo(new[] { SfxId.WindowTvClose }), "남은 진행에서 다시 나지 않음");

            // 접힘 전에 실패해 되돌리면 무음(Update가 더 이상 TickExit을 부르지 않는다).
            played.Clear();
            timeline.Restart();
            Set(intro, "exiting", true);
            Call(intro, "TickExit", timeline.FoldStart * 0.4f);
            intro.CancelExit();
            Assert.That(intro.IsExiting, Is.False);
            Assert.That(played, Is.Empty);
        }

        // ---- G. 음소거 버튼 ----

        private void SavePrefs()
        {
            savedPrefs.Clear();
            foreach (var key in MutePrefKeys)
            {
                if (PlayerPrefs.HasKey(key)) savedPrefs[key] = PlayerPrefs.GetFloat(key, PlayerPrefs.GetInt(key, 0));
                PlayerPrefs.DeleteKey(key);
            }

            oldListenerVolume = AudioListener.volume;
            SettingsRuntimeApplier.RestoreAppliedVolume(SettingsValues.CreateDefaults());
            prefsSaved = true;
        }

        private void RestorePrefs()
        {
            foreach (var key in MutePrefKeys)
            {
                if (!savedPrefs.TryGetValue(key, out var value))
                {
                    PlayerPrefs.DeleteKey(key);
                    continue;
                }

                if (key.EndsWith("Muted")) PlayerPrefs.SetInt(key, (int)value);
                else PlayerPrefs.SetFloat(key, value);
            }

            PlayerPrefs.Save();
            SettingsRuntimeApplier.RestoreAppliedVolume(SettingsRuntimeApplier.LoadOrDefaults());
            AudioListener.volume = oldListenerVolume;
            prefsSaved = false;
        }

        [TestCase(AudioChannel.Master)]
        [TestCase(AudioChannel.Bgm)]
        [TestCase(AudioChannel.Sfx)]
        public void MuteButton_MuteSilent_UnmuteClicksAfterUnmuted(AudioChannel channel)
        {
            Assert.That(SettingsMuteButton.UnmuteClickSfx, Is.EqualTo(SfxId.UiClick));
            SavePrefs();
            var mutedWhenChecked = new List<bool>();
            var clock = 100f;
            var library = Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            var player = SfxPlayer.CreateForTests(library, () => clock, () =>
            {
                var muted = SettingsRuntimeApplier.IsMasterMuted || SettingsRuntimeApplier.IsMuted(AudioChannel.Sfx);
                mutedWhenChecked.Add(muted);
                return muted;
            });
            players.Add(player);

            var sliderGo = Track(new GameObject("B180Slider", typeof(RectTransform), typeof(Slider)));
            var slider = sliderGo.GetComponent<Slider>();
            slider.SetValueWithoutNotify(0.8f);
            var icon = SettingsMuteButton.Ensure(slider, channel);
            Assert.That(icon.Button.GetComponent<UiSfxButton>(), Is.Null, "누르는 순간엔 아직 음소거라 Attach로 붙이지 않는다");

            played.Clear();
            icon.Button.onClick.Invoke();
            Assert.That(SettingsRuntimeApplier.IsMuted(channel), Is.True);
            Assert.That(played, Is.Empty, "음소거를 켜면 무음");

            clock += 1f;
            mutedWhenChecked.Clear();
            var startedBefore = player.StartedCount;
            icon.Button.onClick.Invoke();
            Assert.That(SettingsRuntimeApplier.IsMuted(channel), Is.False);
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiClick }), "풀면 클릭음 한 번");
            Assert.That(mutedWhenChecked.Count, Is.GreaterThan(0));
            Assert.That(mutedWhenChecked[mutedWhenChecked.Count - 1], Is.False, "재생 시점에 음소거가 이미 풀려 있음");
            Assert.That(player.StartedCount, Is.EqualTo(startedBefore + 1), "음소거 확인에 먹히지 않고 재생까지 감");
        }

        // ---- H. 업그레이드 노드 호버 ----

        [Test]
        public void UpgradeNode_HoverAdded_ClickUnchanged_SilentWhileWindowTransitions()
        {
            var surface = SpawnPrefab("SurfaceBasePanel");
            var nodes = surface.GetComponentsInChildren<UpgradeTreeNodeView>(true);
            Assert.That(nodes.Length, Is.GreaterThan(0));
            var node = nodes[0];
            Call(node, "OnEnable");
            Call(node, "OnEnable");
            var button = node.GetComponent<Button>();
            var sound = Sound(button);
            Assert.That(sound.ClickSfx, Is.EqualTo(SfxId.UiClick), "클릭은 그대로");
            Assert.That(sound.Hover, Is.True);
            Assert.That(sound.HasHoverGate, Is.True);
            Assert.That(sound.FailWhenDisabled, Is.False);

            var motion = node.GetComponentInParent<UpgradeWindowMotion>(true);
            if (motion == null) motion = node.transform.parent.gameObject.AddComponent<UpgradeWindowMotion>();

            Set(motion, "state", UpgradeWindowMotion.MotionState.Open);
            played.Clear();
            sound.OnPointerEnter(null);
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiHover }));

            foreach (var state in new[] { UpgradeWindowMotion.MotionState.Opening, UpgradeWindowMotion.MotionState.Closing })
            {
                Set(motion, "state", state);
                played.Clear();
                sound.OnPointerEnter(null);
                Assert.That(played, Is.Empty, state + " 중 호버음 0번");
                button.onClick.Invoke();
                Assert.That(played, Is.EqualTo(new[] { SfxId.UiClick }), state + " 중에도 클릭은 그대로");
            }

            Set(motion, "state", UpgradeWindowMotion.MotionState.Hidden);
        }
    }
}
