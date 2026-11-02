using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
using SubTerra.App.UI.Tutorial;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    /// <summary>B-178: 퀘스트 클리어·심층 해금 작은 창, 보상 지급, 드론 스캔 "핑". SfxRequests 기록으로 확인한다.</summary>
    public sealed class PromptB178SfxTests
    {
        private const string Copper = "mineral.copper";
        private const string Iron = "mineral.iron";
        private const string Lithium = "mineral.lithium";
        private const string PauseOwner = "b178-test";

        private static readonly (SfxId Id, int Number, string File, SfxBus Bus, float Volume)[] Table =
        {
            (SfxId.QuestReward, 87, "quest_reward", SfxBus.UI, 0.5f),
            (SfxId.GlitchPopupOpen, 88, "glitch_popup_open", SfxBus.UI, 0.6f),
            (SfxId.GlitchPopupClose, 89, "glitch_popup_close", SfxBus.UI, 0.6f),
            (SfxId.DroneScanPing, 90, "drone_scan_ping", SfxBus.World, 0.3f),
        };

        private readonly List<Object> created = new List<Object>();
        private readonly List<SfxId> played = new List<SfxId>();
        private SfxPlayer player;

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
            SetCompactSfxInEditMode(false);
            if (player != null)
            {
                player.ReleaseForTests();
                player = null;
            }

            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }

            created.Clear();
            UiPauseGate.Release(PauseOwner);
            UiPauseGate.Release(StartBriefingPopupMotion.PauseOwner);
        }

        private void Record(SfxId id, float pitch) => played.Add(id);

        private int Count(SfxId id)
        {
            var count = 0;
            foreach (var entry in played)
            {
                if (entry == id) count++;
            }

            return count;
        }

        // ---- 칸 ----

        [Test]
        public void Slots87To90_MatchTableAssetAndFiles()
        {
            var defaults = new Dictionary<SfxId, (SfxEntry Entry, string File)>();
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                Assert.That(defaults.ContainsKey(pair.Entry.Id), Is.False, pair.Entry.Id.ToString());
                defaults.Add(pair.Entry.Id, (pair.Entry, pair.FileName));
            }

            Assert.That(defaults.Count, Is.EqualTo(90), "B-170 85칸 + B-178 4칸 + B-179 1칸");
            Assert.That((int)SfxId.MainMenuTypeTick, Is.EqualTo(86), "기존 번호는 그대로");

            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(PromptB156SfxLibraryBuilder.AssetPath);
            Assert.That(library, Is.Not.Null);
            foreach (var row in Table)
            {
                Assert.That((int)row.Id, Is.EqualTo(row.Number), row.Id.ToString());
                Assert.That(defaults.TryGetValue(row.Id, out var actual), Is.True, row.Id.ToString());
                Assert.That(actual.File, Is.EqualTo(row.File), row.Id.ToString());
                Assert.That(actual.Entry.Bus, Is.EqualTo(row.Bus), row.Id.ToString());
                Assert.That(actual.Entry.Volume, Is.EqualTo(row.Volume).Within(0.0001f), row.Id.ToString());
                Assert.That(actual.Entry.Pitch, Is.EqualTo(1f).Within(0.0001f), row.Id.ToString());

                Assert.That(library.TryGetEntry(row.Id, out var asset), Is.True, row.Id.ToString());
                Assert.That(asset.Clip, Is.Not.Null, row.Id.ToString());
                Assert.That(asset.Clip.name, Is.EqualTo(row.File), row.Id.ToString());
                Assert.That(asset.Bus, Is.EqualTo(row.Bus), row.Id.ToString());
                Assert.That(asset.Volume, Is.EqualTo(row.Volume).Within(0.0001f), row.Id.ToString());

                var folder = row.Bus == SfxBus.World ? "World" : "UI";
                var path = SfxLibraryDefaults.ClipRoot + "/" + folder + "/" + row.File + ".wav";
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.forceToMono, Is.True, path);
                Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad), path);
            }
        }

        // ---- A. 작은 글리치 창 ----

        private static object Invoke(object target, string name, params object[] args)
        {
            var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, name);
            return method.Invoke(target, args);
        }

        private static void SetCompactSfxInEditMode(bool value)
        {
            var field = typeof(StartBriefingPopupMotion).GetField("CompactSfxInEditModeForTests",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(field, Is.Not.Null);
            field.SetValue(null, value);
        }

        private GameObject NewCanvas()
        {
            var canvas = new GameObject("B178Canvas", typeof(RectTransform), typeof(Canvas));
            created.Add(canvas);
            return canvas;
        }

        private StartBriefingPopupMotion NewMotion(bool compact)
        {
            var root = new GameObject("B178Popup", typeof(RectTransform));
            root.transform.SetParent(NewCanvas().transform, false);
            ((RectTransform)root.transform).sizeDelta = new Vector2(900f, 450f);
            var motion = root.AddComponent<StartBriefingPopupMotion>();
            if (compact)
            {
                Invoke(motion, "ConfigureCompact", new Vector2(900f, 450f), new Vector2(395f, 155f), null, false,
                    "Unlock", null, 1f);
            }

            // EditMode에서는 Awake/OnEnable이 불리지 않는다.
            Invoke(motion, "Awake");
            Invoke(motion, "OnEnable");
            return motion;
        }

        private static void RunMotion(StartBriefingPopupMotion motion, float seconds)
        {
            for (var t = 0f; t < seconds - 0.0001f; t += 0.02f) Invoke(motion, "Tick", 0.02f);
        }

        private static void Show(StartBriefingPopupMotion motion)
        {
            Invoke(motion, "BeginIntro");
            RunMotion(motion, StartBriefingTimeline.IntroDuration + 0.1f);
            Assert.That(motion.IsShown, Is.True);
        }

        [Test]
        public void Compact_OpenOnce_CloseOnce_RepeatCloseIgnored_NoBriefingSfx()
        {
            SetCompactSfxInEditMode(true);
            var motion = NewMotion(true);
            Assert.That(played, Is.Empty, "켜지기만 해서는(Configure·OnEnable·ResetVisuals) 무음");

            Invoke(motion, "BeginIntro");
            Assert.That(Count(SfxId.GlitchPopupOpen), Is.EqualTo(1), "첫 지직(0초)에 한 번");
            RunMotion(motion, StartBriefingTimeline.IntroDuration + 0.5f);
            Assert.That(Count(SfxId.GlitchPopupOpen), Is.EqualTo(1), "표시 중 짧은 지직은 무음");

            Assert.That(Invoke(motion, "RequestCompactClose", (Action)null), Is.EqualTo(true));
            Assert.That(Invoke(motion, "RequestCompactClose", (Action)null), Is.EqualTo(false), "닫는 중 재호출");
            motion.RequestClose(null);
            RunMotion(motion, StartBriefingTimeline.CloseDuration + 0.05f);

            Assert.That(Count(SfxId.GlitchPopupClose), Is.EqualTo(1));
            Assert.That(Count(SfxId.BriefingGlitch), Is.Zero);
            Assert.That(Count(SfxId.BriefingGlitchShort), Is.Zero);
            Assert.That(Count(SfxId.BriefingClose), Is.Zero);
        }

        [Test]
        public void Compact_SilentInEditModeWithoutTestSwitch()
        {
            var motion = NewMotion(true);
            Show(motion);
            Invoke(motion, "RequestCompactClose", (Action)null);
            RunMotion(motion, StartBriefingTimeline.CloseDuration + 0.05f);

            Assert.That(played, Is.Empty, "Application.isPlaying이 아니면 무음");
        }

        [Test]
        public void StartBriefing_KeepsBriefingSfx_NoCompactSfx()
        {
            SetCompactSfxInEditMode(true);
            var motion = NewMotion(false);
            Show(motion);
            motion.RequestClose(null);
            RunMotion(motion, StartBriefingTimeline.CloseDuration + 0.05f);

            Assert.That(Count(SfxId.BriefingGlitch), Is.EqualTo(1));
            Assert.That(Count(SfxId.BriefingClose), Is.EqualTo(1));
            Assert.That(Count(SfxId.GlitchPopupOpen), Is.Zero);
            Assert.That(Count(SfxId.GlitchPopupClose), Is.Zero);
        }

        // 닫히던 창을 바로 다시 연다: QuestClearPopupMotion.Open이 transition을 껐다 켜고 열림음을 늦춘다.
        private static void Reopen(StartBriefingPopupMotion motion)
        {
            Invoke(motion, "OnDisable");
            Invoke(motion, "OnEnable");
            Invoke(motion, "DelayNextOpenSfx", StartBriefingPopupMotion.ReopenOpenSfxDelay);
            Invoke(motion, "BeginIntro");
        }

        [Test]
        public void Reopen_CloseOnce_ThenOpenAfterDelay()
        {
            SetCompactSfxInEditMode(true);
            Assert.That(StartBriefingPopupMotion.ReopenOpenSfxDelay, Is.EqualTo(0.35f));
            var motion = NewMotion(true);
            Show(motion);
            played.Clear();

            Invoke(motion, "RequestCompactClose", (Action)null);
            Reopen(motion);
            Assert.That(Count(SfxId.GlitchPopupClose), Is.EqualTo(1));
            Assert.That(Count(SfxId.GlitchPopupOpen), Is.Zero, "같은 프레임에는 열림음이 없다");

            RunMotion(motion, 0.3f);
            Assert.That(Count(SfxId.GlitchPopupOpen), Is.Zero);
            RunMotion(motion, 0.1f);
            Assert.That(Count(SfxId.GlitchPopupOpen), Is.EqualTo(1), "0.35초 뒤 한 번");
            RunMotion(motion, StartBriefingTimeline.IntroDuration);
            Assert.That(Count(SfxId.GlitchPopupOpen), Is.EqualTo(1));
        }

        [Test]
        public void Reopen_DisabledOrClosedDuringDelay_NoOpen()
        {
            SetCompactSfxInEditMode(true);
            var motion = NewMotion(true);
            Show(motion);
            played.Clear();
            Invoke(motion, "RequestCompactClose", (Action)null);
            Reopen(motion);
            RunMotion(motion, 0.1f);
            // 꺼진 컴포넌트는 Update가 돌지 않으므로 Tick 대신 남은 예약을 본다.
            Invoke(motion, "OnDisable");
            var pending = typeof(StartBriefingPopupMotion).GetField("pendingOpenSfxAt",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.GetValue(motion), Is.EqualTo(-1f), "꺼지면 늦춘 열림음을 버린다");
            Assert.That(Count(SfxId.GlitchPopupOpen), Is.Zero);

            Invoke(motion, "OnEnable");
            Invoke(motion, "DelayNextOpenSfx", StartBriefingPopupMotion.ReopenOpenSfxDelay);
            Invoke(motion, "BeginIntro");
            RunMotion(motion, 0.1f);
            Invoke(motion, "RequestCompactClose", (Action)null);
            RunMotion(motion, 0.5f);
            Assert.That(Count(SfxId.GlitchPopupOpen), Is.Zero, "닫히면 늦춘 열림음을 버린다");
        }

        [Test]
        public void QuestClearPopup_OpenWhileClosing_DelaysNextOpenSfx()
        {
            var root = new GameObject("B178QuestClear", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(NewCanvas().transform, false);
            ((RectTransform)root.transform).sizeDelta = new Vector2(900f, 450f);
            var popup = root.AddComponent<QuestClearPopupMotion>();
            Invoke(popup, "Awake");
            var transition = root.GetComponent<StartBriefingPopupMotion>();
            Assert.That(transition, Is.Not.Null);
            var delay = typeof(StartBriefingPopupMotion).GetField("nextOpenSfxDelay",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(delay, Is.Not.Null);

            popup.Open();
            Assert.That(delay.GetValue(transition), Is.EqualTo(0f), "처음 열기는 늦추지 않는다");

            typeof(QuestClearPopupMotion).GetField("closing", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(popup, true);
            popup.Open();
            Assert.That(delay.GetValue(transition), Is.EqualTo(StartBriefingPopupMotion.ReopenOpenSfxDelay));
        }

        // ---- B. 보상 지급 ----

        private static InMemoryMineralCatalog CreateCatalog()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(Copper, 1.5f, 10, "구리");
            catalog.Register(Iron, 2f, 15, "철");
            catalog.Register(Lithium, 0.8f, 40, "리튬");
            return catalog;
        }

        // 첫 퀘스트(구리 3) 보상이 기다리는 상태에서 클리어 창을 닫는다.
        private static (InventoryService Inventory, QuestRewardService Rewards, DemoObjectivePresenter Presenter)
            ClaimFirstReward(float capacity, params (string id, int count)[] cargo)
        {
            var state = GameState.CreateNew();
            var inventory = new InventoryService(CreateCatalog(), capacity, state);
            var rewards = new QuestRewardService();
            rewards.Bind(inventory, state);
            foreach (var (id, count) in cargo) inventory.TryAddMineral(id, count);
            state.SetDemoProgress(DemoObjectiveIds.Ordered[1], 1, false);

            var director = new DemoObjectiveDirector();
            director.BindGameState(state);
            director.RestoreFromProgress(state.Progress);
            var presenter = new DemoObjectivePresenter(null);
            presenter.Bind(director, rewards);
            presenter.ConfirmClaim();
            return (inventory, rewards, presenter);
        }

        [Test]
        public void Reward_GrantedOnce()
        {
            var env = ClaimFirstReward(20f);
            Assert.That(env.Rewards.HasPending, Is.False);
            Assert.That(Count(SfxId.QuestReward), Is.EqualTo(1));
            env.Presenter.ConfirmClaim();
            Assert.That(Count(SfxId.QuestReward), Is.EqualTo(1), "다시 눌러도 한 번");
        }

        [Test]
        public void Reward_NeedsCapacityAndForfeit_Silent()
        {
            var env = ClaimFirstReward(4f, (Copper, 2));
            Assert.That(env.Presenter.IsCapacityOpen, Is.True, "준비: 화물 부족");
            Assert.That(Count(SfxId.QuestReward), Is.Zero);
            env.Presenter.ChooseForfeitReward();
            Assert.That(env.Rewards.HasPending, Is.False);
            Assert.That(Count(SfxId.QuestReward), Is.Zero);
        }

        [Test]
        public void Reward_AfterDumpRetry_GrantedOnce()
        {
            // 8/8(철 4) + 구리 3(4.5) → 철 3개를 버리면 RetryPending으로 들어온다.
            var env = ClaimFirstReward(8f, (Iron, 4));
            Assert.That(Count(SfxId.QuestReward), Is.Zero);
            env.Presenter.ChooseDumpInventory();
            env.Presenter.DumpMineral(Iron, 3);
            env.Presenter.DumpMineral(Iron, 1);

            Assert.That(env.Rewards.HasPending, Is.False);
            Assert.That(env.Inventory.State.GetQuantity(Copper), Is.EqualTo(3));
            Assert.That(Count(SfxId.QuestReward), Is.EqualTo(1));
        }

        [Test]
        public void Reward_RuleIgnoresEmptyAndNonGranted()
        {
            var reward = new QuestReward(1, 0, 0, 0);
            Assert.That(DemoObjectivePresenter.ShouldPlayRewardSfx(
                new QuestRewardGrantResult(QuestRewardGrantStatus.Granted, "q", reward, "granted")), Is.True);
            Assert.That(DemoObjectivePresenter.ShouldPlayRewardSfx(
                new QuestRewardGrantResult(QuestRewardGrantStatus.Granted, "q", QuestReward.None, "empty-reward")), Is.False);
            foreach (var status in new[]
                     {
                         QuestRewardGrantStatus.NeedsCapacity, QuestRewardGrantStatus.Forfeited,
                         QuestRewardGrantStatus.AlreadySettled, QuestRewardGrantStatus.NothingPending,
                         QuestRewardGrantStatus.AwaitingClaim
                     })
            {
                Assert.That(DemoObjectivePresenter.ShouldPlayRewardSfx(
                    new QuestRewardGrantResult(status, "q", reward, "x")), Is.False, status.ToString());
            }
        }

        // ---- C. 드론 스캔 ----

        [Test]
        public void DroneRule_SkipsImmediateOverlapSettingsAndPause()
        {
            const float Never = float.PositiveInfinity;
            Assert.That(DroneScanPingRule.SkipIfRecentSeconds, Is.EqualTo(0.6f));
            Assert.That(DroneScanPingRule.Pitch, Is.EqualTo(1f));
            Assert.That(DroneScanPingRule.ShouldPlay(false, Never, Never, false, false), Is.True);
            Assert.That(DroneScanPingRule.ShouldPlay(true, Never, Never, false, false), Is.False, "첫 펄스");
            Assert.That(DroneScanPingRule.ShouldPlay(false, 0.59f, Never, false, false), Is.False, "도착 딩");
            Assert.That(DroneScanPingRule.ShouldPlay(false, Never, 0.1f, false, false), Is.False, "레벨업");
            Assert.That(DroneScanPingRule.ShouldPlay(false, 0.6f, 0.6f, false, false), Is.True);
            Assert.That(DroneScanPingRule.ShouldPlay(false, Never, Never, true, false), Is.False, "설정창");
            Assert.That(DroneScanPingRule.ShouldPlay(false, Never, Never, false, true), Is.False, "UiPauseGate");
        }

        private SfxPlayer NewPlayer(Func<float> clock)
        {
            var library = ScriptableObject.CreateInstance<SfxLibrary>();
            created.Add(library);
            foreach (var id in new[] { SfxId.ElevatorArriveDing, SfxId.UpgradeLevelUp, SfxId.DroneScanPing })
            {
                var clip = AudioClip.Create("b178-" + id, 4410, 1, 44100, false);
                created.Add(clip);
                var entry = new SfxEntry(id, SfxBus.UI, 1f);
                entry.EditorSetClip(clip);
                library.EditorAddEntry(entry);
            }

            player = SfxPlayer.CreateForTests(library, clock, () => false);
            return player;
        }

        [Test]
        public void DronePing_FirstSkipped_NextOnce_OverlapAndMenusSkipped()
        {
            var now = 100f;
            NewPlayer(() => now);
            var settingsOpen = false;
            var ping = new DroneScanPingSfx(() => settingsOpen);

            ping.HandlePulseStarted(true);
            Assert.That(Count(SfxId.DroneScanPing), Is.Zero, "첫 펄스는 링만");
            now += 30f;
            ping.HandlePulseStarted(false);
            Assert.That(Count(SfxId.DroneScanPing), Is.EqualTo(1), "다음 회차");

            now += 30f;
            SfxPlayer.Play(SfxId.ElevatorArriveDing);
            now += 0.3f;
            ping.HandlePulseStarted(false);
            Assert.That(Count(SfxId.DroneScanPing), Is.EqualTo(1), "0.6초 안 도착 딩");

            now += 30f;
            SfxPlayer.Play(SfxId.UpgradeLevelUp);
            now += 0.5f;
            ping.HandlePulseStarted(false);
            Assert.That(Count(SfxId.DroneScanPing), Is.EqualTo(1), "0.6초 안 레벨업");
            Assert.That(player.SecondsSinceLastPlayed(SfxId.UpgradeLevelUp), Is.EqualTo(0.5f).Within(0.0001f));

            now += 30f;
            settingsOpen = true;
            ping.HandlePulseStarted(false);
            Assert.That(Count(SfxId.DroneScanPing), Is.EqualTo(1), "설정창 열림");
            settingsOpen = false;

            UiPauseGate.Acquire(PauseOwner);
            ping.HandlePulseStarted(false);
            Assert.That(Count(SfxId.DroneScanPing), Is.EqualTo(1), "UiPauseGate 중");
            UiPauseGate.Release(PauseOwner);

            now += 30f;
            ping.HandlePulseStarted(false);
            Assert.That(Count(SfxId.DroneScanPing), Is.EqualTo(2), "생략은 그 회차만");
            Assert.That(player.SecondsSinceLastPlayed(SfxId.UiClick), Is.EqualTo(float.PositiveInfinity));
        }
    }
}
