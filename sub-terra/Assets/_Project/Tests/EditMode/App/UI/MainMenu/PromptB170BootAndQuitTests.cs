using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.UI;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.QuitConfirm;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI.MainMenu
{
    /// <summary>B-170: 메인 화면 부팅 연출 고침·소리, 게임 종료 확인 창, 종료 연출.</summary>
    public sealed class PromptB170BootAndQuitTests
    {
        private readonly List<(SfxId Id, float Pitch)> requests = new List<(SfxId, float)>();
        private readonly List<GameObject> spawned = new List<GameObject>();
        private FakeTarget fake;
        private bool previousReduceMotion;
        private bool riding;
        private float now;

        [SetUp]
        public void SetUp()
        {
            if (UiPauseGate.IsHeld)
                Assert.Inconclusive("다른 검사가 UiPauseGate를 남김");
            previousReduceMotion = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
            requests.Clear();
            SfxRequests.PlayRequested += Record;
            now = 100f;
            var library = Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            WindowTvSfx.ResetForTests(library, () => now, () => false);
            MainMenuBootSfx.ResetForTests(library, () => false);
            QuitConfirmFlow.DestroyForTests();

            fake = new FakeTarget();
            QuitConfirmFlow.TargetOverride = fake;
            riding = false;
            QuitConfirmFlow.RideCheck = () => riding;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            WindowTvSfx.ReleaseForTests();
            MainMenuBootSfx.ReleaseForTests();
            QuitConfirmFlow.DestroyForTests();
            QuitConfirmFlow.TargetOverride = null;
            QuitConfirmFlow.RideCheck = null;
            AccessibilityPreferences.ReduceMotion = previousReduceMotion;
            foreach (var go in spawned)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
            UiPauseGate.Release(QuitConfirmFlow.PauseOwner);
            UiPauseGate.Release("test.other");
        }

        [Test]
        public void Boot_FirstTwoFramesDiscarded_ThenClampedStep()
        {
            var timeline = new MainMenuBootTimeline();
            Assert.That(timeline.Tick(5f), Is.False);
            Assert.That(timeline.Tick(5f), Is.False);
            Assert.That(timeline.Elapsed, Is.EqualTo(0f));
            Assert.That(timeline.ClockStarted, Is.False);
            Assert.That(timeline.Tick(0.5f), Is.True);
            Assert.That(timeline.Elapsed, Is.EqualTo(1f / 30f).Within(1e-4f));
            Assert.That(timeline.Tick(0.5f), Is.False);
        }

        [Test]
        public void Boot_TotalDuration_About2Seconds_AndOrderKept()
        {
            var timeline = new MainMenuBootTimeline();
            Assert.That(timeline.TotalDuration, Is.InRange(1.8f, 2.2f));
            Assert.That(timeline.TotalDuration <= MainMenuBootTimeline.MaxDuration, Is.True);

            float lineAt = -1f, openAt = -1f, headerAt = -1f, logoAt = -1f, subtitleAt = -1f, continueAt = -1f;
            var cardAt = new[] { -1f, -1f, -1f };
            var secondaryAt = new[] { -1f, -1f, -1f };
            const float step = 0.005f;
            for (var t = 0f; t <= timeline.TotalDuration + step; t += step)
            {
                var frame = timeline.Sample();
                var elapsed = timeline.Elapsed;
                if (lineAt < 0f && frame.LineWidth > 0f) lineAt = elapsed;
                if (openAt < 0f && frame.Open > 0f) openAt = elapsed;
                if (headerAt < 0f && frame.HeaderTyping > 0f) headerAt = elapsed;
                if (logoAt < 0f && frame.LogoAlpha > 0f) logoAt = elapsed;
                if (subtitleAt < 0f && frame.SubtitleTyping > 0f) subtitleAt = elapsed;
                for (var i = 0; i < 3; i++)
                {
                    if (cardAt[i] < 0f && timeline.CardAlpha(i) > 0f) cardAt[i] = elapsed;
                    if (secondaryAt[i] < 0f && timeline.SecondaryAlpha(i) > 0f) secondaryAt[i] = elapsed;
                }
                if (continueAt < 0f && timeline.ContinueAlpha() > 0f) continueAt = elapsed;
                timeline.Advance(step);
            }

            Assert.That(lineAt, Is.LessThan(openAt), "가로선 뒤 펼침");
            Assert.That(openAt, Is.LessThan(headerAt), "펼침 뒤 부팅 문구");
            Assert.That(headerAt, Is.LessThan(logoAt), "부팅 문구 뒤 로고");
            Assert.That(logoAt, Is.LessThan(subtitleAt), "로고 뒤 부제");
            Assert.That(subtitleAt, Is.LessThan(cardAt[0]), "부제 뒤 첫 카드");
            Assert.That(cardAt[0], Is.LessThan(cardAt[1]), "첫 카드 뒤 둘째 카드");
            Assert.That(cardAt[1], Is.LessThan(cardAt[2]), "둘째 카드 뒤 셋째 카드");
            Assert.That(cardAt[2], Is.LessThan(continueAt), "카드 뒤 이어하기");
            Assert.That(continueAt, Is.LessThan(secondaryAt[0]), "이어하기 뒤 보조 버튼");
            Assert.That(secondaryAt[0], Is.LessThan(secondaryAt[2]), "보조 버튼 순서");
        }

        [Test]
        public void Boot_Skip_EndState()
        {
            var timeline = new MainMenuBootTimeline();
            timeline.Tick(0.016f);
            timeline.Tick(0.016f);
            timeline.Tick(0.016f);
            timeline.Skip();
            Assert.That(timeline.IsComplete, Is.True);
            Assert.That(timeline.Sample().Open, Is.EqualTo(1f));
            Assert.That(timeline.CardAlpha(2), Is.EqualTo(1f));
            Assert.That(timeline.SecondaryAlpha(2), Is.EqualTo(1f));
        }

        [Test]
        public void Boot_TypeTicks_AtLeast60msApart()
        {
            var timeline = new MainMenuBootTimeline();
            var gate = new MainMenuTypeTickGate();
            var tickTimes = new List<float>();
            while (!timeline.IsComplete)
            {
                timeline.Tick(1f / 60f);
                if (gate.Step(timeline.TypedCharacters(26, 16), timeline.Elapsed))
                    tickTimes.Add(timeline.Elapsed);
            }

            Assert.That(tickTimes.Count >= 6, Is.True, "타이핑 중 틱이 여러 번 난다");
            Assert.That(tickTimes[0] >= timeline.Settings.headerStart, Is.True, "첫 틱은 문구가 시작된 뒤");
            for (var i = 1; i < tickTimes.Count; i++)
                Assert.That(tickTimes[i] - tickTimes[i - 1], Is.GreaterThanOrEqualTo(0.06f - 1e-4f), "틱 간격은 0.06초 이상");
        }

        [Test]
        public void Boot_TickGate_NoTickWhenCountDoesNotGrow()
        {
            var gate = new MainMenuTypeTickGate();
            Assert.That(gate.Step(5, 0f), Is.True);
            Assert.That(gate.Step(5, 1f), Is.False);
            Assert.That(gate.Step(6, 1.03f), Is.True);
            Assert.That(gate.Step(7, 1.05f), Is.False);
        }

        [Test]
        public void BootSfx_PlayOnce_FadeStopsAfter015()
        {
            Assert.That(MainMenuBootSfx.Play(), Is.True);
            Assert.That(MainMenuBootSfx.StartedCount, Is.EqualTo(1));
            Assert.That(MainMenuBootSfx.Source.volume > 0f, Is.True);
            var initialVolume = MainMenuBootSfx.Source.volume;
            MainMenuBootSfx.FadeOut();
            Assert.That(MainMenuBootSfx.IsFading, Is.True);
            MainMenuBootSfx.Tick(0.1f);
            Assert.That(MainMenuBootSfx.Source.volume < initialVolume && MainMenuBootSfx.Source.volume > 0f, Is.True);
            MainMenuBootSfx.Tick(0.06f);
            Assert.That(MainMenuBootSfx.IsFading, Is.False);
            Assert.That(MainMenuBootSfx.Source.volume, Is.EqualTo(0f).Within(1e-4f));
            MainMenuBootSfx.FadeOut();
            Assert.That(MainMenuBootSfx.FadeCount, Is.EqualTo(1));
        }

        [Test]
        public void BootSfx_Muted_NoStart()
        {
            MainMenuBootSfx.ResetForTests(Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath), () => true);
            Assert.That(MainMenuBootSfx.Play(), Is.False);
            Assert.That(MainMenuBootSfx.StartedCount, Is.EqualTo(0));
        }

        [Test]
        public void Slots_85_86_ValuesAndFiles()
        {
            var rows = new[]
            {
                (Id: SfxId.MainMenuBoot, Number: 85, File: "main_menu_boot", Bus: SfxBus.UI, Volume: 0.45f),
                (Id: SfxId.MainMenuTypeTick, Number: 86, File: "main_menu_type_tick", Bus: SfxBus.UI, Volume: 0.5f),
            };
            var defaults = new Dictionary<SfxId, (SfxEntry Entry, string File)>();
            foreach (var pair in SfxLibraryDefaults.Create())
                defaults[pair.Entry.Id] = (pair.Entry, pair.FileName);
            var library = Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            Assert.That(library, Is.Not.Null, "Resources/SfxLibrary 에셋이 있다");

            foreach (var row in rows)
            {
                var label = row.Id.ToString();
                Assert.That((int)row.Id, Is.EqualTo(row.Number), label);
                Assert.That(defaults.TryGetValue(row.Id, out var actual), Is.True, label);
                Assert.That(actual.File, Is.EqualTo(row.File), label);
                Assert.That(actual.Entry.Bus, Is.EqualTo(row.Bus), label);
                Assert.That(actual.Entry.Volume, Is.EqualTo(row.Volume).Within(0.0001f), label);
                Assert.That(library.TryGetEntry(row.Id, out var asset), Is.True, label);
                Assert.That(asset.Clip, Is.Not.Null, label);
                Assert.That(asset.Clip.name, Is.EqualTo(row.File), label);
            }
        }

        [Test]
        public void Quit_Request_ShowsPopup_NotShutdown()
        {
            Assert.That(RequestShown(true), Is.EqualTo(QuitRequestResult.Shown));
            Assert.That(QuitConfirmFlow.Instance.Popup.IsVisible, Is.True);
            Assert.That(QuitConfirmFlow.Instance.Sequence.Current, Is.EqualTo(QuitSequence.Phase.Confirming));
            Assert.That(fake.ShutdownCount, Is.EqualTo(0));
            Assert.That(fake.SaveCount, Is.EqualTo(0));
            Assert.That(UiPauseGate.IsHeldBy(QuitConfirmFlow.PauseOwner), Is.True);
            Assert.That(QuitConfirmFlow.IsBlockingInput, Is.True);
            Assert.That(QuitConfirmFlow.Instance.Popup.NoteString, Is.EqualTo(QuitConfirmPopupView.SaveNote));

            QuitConfirmFlow.DestroyForTests();
            Assert.That(RequestShown(false), Is.EqualTo(QuitRequestResult.Shown));
            Assert.That(QuitConfirmFlow.Instance.Popup.NoteString, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Quit_SecondRequestWhileOpen_IsBusy()
        {
            RequestShown(true);
            Assert.That(QuitConfirmFlow.Request(true), Is.EqualTo(QuitRequestResult.Busy));
            Assert.That(UnityEngine.Object.FindObjectsByType<QuitConfirmFlow>(FindObjectsInactive.Include).Length,
                Is.EqualTo(1));
        }

        [Test]
        public void Quit_WhileSaving_NoPopup()
        {
            fake.IsSaveInProgress = true;
            Assert.That(QuitConfirmFlow.Request(true), Is.EqualTo(QuitRequestResult.DeferredWhileSaving));
            Assert.That(QuitConfirmFlow.Instance.Popup.IsVisible, Is.False);
            Assert.That(UiPauseGate.IsHeldBy(QuitConfirmFlow.PauseOwner), Is.False);
        }

        [Test]
        public void Quit_Blocked_WhenRidingOrPausedOrCaller()
        {
            riding = true;
            Assert.That(QuitConfirmFlow.Request(true), Is.EqualTo(QuitRequestResult.Blocked));
            riding = false;
            UiPauseGate.Acquire("test.other");
            Assert.That(QuitConfirmFlow.Request(true), Is.EqualTo(QuitRequestResult.Blocked));
            UiPauseGate.Release("test.other");
            Assert.That(QuitConfirmFlow.Request(true, callerBlocked: true), Is.EqualTo(QuitRequestResult.Blocked));
            Assert.That(QuitConfirmFlow.Instance.Popup.IsVisible, Is.False);
        }

        [Test]
        public void Quit_Escape_Closes_WithCloseSound()
        {
            RequestShown(true);
            QuitConfirmFlow.Instance.Popup.Tick(0.6f);
            QuitConfirmFlow.Instance.HandleInput(true, true, 0.016f);
            Assert.That(QuitConfirmFlow.Instance.Popup.IsClosing, Is.True);
            for (var i = 0; i < 7 && QuitConfirmFlow.Instance.Popup.IsVisible; i++)
                QuitConfirmFlow.Instance.Popup.Tick(0.05f);
            Assert.That(QuitConfirmFlow.Instance.Popup.IsVisible, Is.False);
            Assert.That(QuitConfirmFlow.Instance.Sequence.Current, Is.EqualTo(QuitSequence.Phase.Idle));
            Assert.That(UiPauseGate.IsHeldBy(QuitConfirmFlow.PauseOwner), Is.False);
            Assert.That(Count(SfxId.UiPopupOpen), Is.EqualTo(1));
            Assert.That(Count(SfxId.UiPopupClose), Is.EqualTo(1));
            Assert.That(Count(SfxId.UiConfirm), Is.EqualTo(0));
            Assert.That(fake.ShutdownCount, Is.EqualTo(0));
        }

        [Test]
        public void Quit_Enter_DoesNotQuit()
        {
            RequestShown(true);
            QuitConfirmFlow.Instance.Popup.Tick(0.6f);
            for (var i = 0; i < 30; i++)
                QuitConfirmFlow.Instance.HandleInput(false, false, 0.016f);
            Assert.That(QuitConfirmFlow.Instance.Sequence.Current, Is.EqualTo(QuitSequence.Phase.Confirming));
            Assert.That(QuitConfirmFlow.Instance.Popup.ConfirmButton.navigation.mode, Is.EqualTo(Navigation.Mode.None));
            Assert.That(QuitConfirmFlow.Instance.Popup.CancelButton.navigation.mode, Is.EqualTo(Navigation.Mode.None));
        }

        [Test]
        public void Quit_ButtonsIgnoredDuringIntro()
        {
            RequestShown(true);
            Assert.That(QuitConfirmFlow.Instance.Popup.ConfirmButton.interactable, Is.False);
            QuitConfirmFlow.Instance.ConfirmForTests();
            Assert.That(QuitConfirmFlow.Instance.Sequence.Current, Is.EqualTo(QuitSequence.Phase.Confirming));
            Assert.That(fake.SaveCount, Is.EqualTo(0));
            QuitConfirmFlow.Instance.Popup.Tick(0.6f);
            Assert.That(QuitConfirmFlow.Instance.Popup.ConfirmButton.interactable, Is.True);
        }

        [Test]
        public void Quit_ReducedMotion_InstantShowAndHide()
        {
            AccessibilityPreferences.ReduceMotion = true;
            RequestShown(true);
            Assert.That(QuitConfirmFlow.Instance.Popup.IsStable, Is.True);
            Assert.That(QuitConfirmFlow.Instance.Popup.AnyGlitchPieceVisible, Is.False);
            QuitConfirmFlow.Instance.Close();
            Assert.That(QuitConfirmFlow.Instance.Popup.IsVisible, Is.False);
            Assert.That(Count(SfxId.UiPopupOpen), Is.EqualTo(1));
            Assert.That(Count(SfxId.UiPopupClose), Is.EqualTo(1));
        }

        [Test]
        public void Quit_Confirm_SavesBeforeOutro_AndShutsDownOnce()
        {
            fake.IsDirty = true;
            RequestShown(true);
            QuitConfirmFlow.Instance.Popup.Tick(0.6f);
            QuitConfirmFlow.Instance.ConfirmForTests();
            Assert.That(QuitConfirmFlow.Instance.Popup.IsVisible, Is.False);
            Assert.That(fake.SaveCount, Is.EqualTo(1));
            Assert.That(fake.Order, Is.EqualTo(new[] { "save" }));
            Assert.That(QuitConfirmFlow.Instance.Sequence.Current, Is.EqualTo(QuitSequence.Phase.Outro));
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvClose));
            Assert.That(QuitConfirmFlow.Instance.Overlay.gameObject.activeSelf, Is.True);

            var elapsed = 0f;
            for (var i = 0; i < 100 && QuitConfirmFlow.Instance.Sequence.Current != QuitSequence.Phase.Done; i++)
            {
                QuitConfirmFlow.Instance.HandleInput(false, false, 0.05f);
                elapsed += 0.05f;
            }
            Assert.That(elapsed <= QuitOutroTimeline.OverlayDuration + 0.05f, Is.True);
            Assert.That(fake.Order, Is.EqualTo(new[] { "save", "shutdown" }));
            QuitConfirmFlow.Instance.HandleInput(false, false, 0.05f);
            Assert.That(fake.ShutdownCount, Is.EqualTo(1));
        }

        [Test]
        public void Quit_Outro_WaitsForSave()
        {
            RequestShown(true);
            QuitConfirmFlow.Instance.Popup.Tick(0.6f);
            QuitConfirmFlow.Instance.ConfirmForTests();
            fake.IsSaveInProgress = true;
            for (var i = 0; i < 10; i++)
                QuitConfirmFlow.Instance.HandleInput(false, false, 0.1f);
            Assert.That(fake.ShutdownCount, Is.EqualTo(0));
            Assert.That(QuitConfirmFlow.Instance.Sequence.Current, Is.EqualTo(QuitSequence.Phase.WaitingSave));
            fake.IsSaveInProgress = false;
            QuitConfirmFlow.Instance.HandleInput(false, false, 0.016f);
            Assert.That(fake.ShutdownCount, Is.EqualTo(1));
        }

        [Test]
        public void Quit_Outro_AnyInput_QuitsImmediately()
        {
            RequestShown(true);
            QuitConfirmFlow.Instance.Popup.Tick(0.6f);
            QuitConfirmFlow.Instance.ConfirmForTests();
            // EditMode에서는 Time.frameCount가 늘지 않아 같은 프레임 입력으로 처리되므로 확인 프레임을 넘긴 값으로 둔다.
            typeof(QuitConfirmFlow).GetField("confirmFrame", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(QuitConfirmFlow.Instance, -1);
            QuitConfirmFlow.Instance.HandleInput(false, true, 0.016f);
            Assert.That(fake.ShutdownCount, Is.EqualTo(1));
        }

        [Test]
        public void Quit_ReducedMotion_Outro_BlackThenQuitAt035()
        {
            AccessibilityPreferences.ReduceMotion = true;
            RequestShown(true);
            QuitConfirmFlow.Instance.ConfirmForTests();
            Assert.That(QuitConfirmFlow.Instance.Overlay.Open, Is.EqualTo(0f));
            Assert.That(QuitConfirmFlow.Instance.Overlay.LineAlpha, Is.EqualTo(0f));
            for (var i = 0; i < 18; i++)
                QuitConfirmFlow.Instance.HandleInput(false, false, 0.016f);
            Assert.That(fake.ShutdownCount, Is.EqualTo(0));
            for (var i = 0; i < 5; i++)
                QuitConfirmFlow.Instance.HandleInput(false, false, 0.016f);
            Assert.That(fake.ShutdownCount, Is.EqualTo(1));
        }

        [Test]
        public void MenuOutro_ReverseSampling_MatchesBootReversed()
        {
            var boot = new MainMenuBootTimeline();
            var outro = new FakeMenuOutro(boot);
            Assert.That(outro.QuitOutroDuration <= QuitOutroTimeline.MenuMaxDuration, Is.True);
            var comparison = new MainMenuBootTimeline();
            foreach (var progress in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
            {
                outro.ApplyQuitOutro(progress);
                comparison.SetElapsed(boot.TotalDuration * (1f - progress));
                Assert.That(outro.Timeline.Sample().Open, Is.EqualTo(comparison.Sample().Open).Within(1e-4f));
                Assert.That(outro.Timeline.CardAlpha(0), Is.EqualTo(comparison.CardAlpha(0)).Within(1e-4f));
                Assert.That(outro.Timeline.ContinueAlpha(), Is.EqualTo(comparison.ContinueAlpha()).Within(1e-4f));
            }
            outro.ApplyQuitOutro(1f);
            Assert.That(boot.Sample().Open, Is.EqualTo(0f));
            Assert.That(outro.LineWidth, Is.EqualTo(0f));
            outro.ApplyQuitOutro(0f);
            Assert.That(boot.Sample().Open, Is.EqualTo(1f));

            var target = new FakeTarget();
            var sequence = new QuitSequence(target);
            Assert.That(sequence.Request(false), Is.EqualTo(QuitRequestResult.Shown));
            Assert.That(sequence.Confirm(outro, false), Is.True);
            var elapsed = 0f;
            while (sequence.Current != QuitSequence.Phase.Done && elapsed < outro.QuitOutroDuration + 0.05f)
            {
                sequence.Tick(0.05f, false);
                elapsed += 0.05f;
            }
            Assert.That(sequence.Current, Is.EqualTo(QuitSequence.Phase.Done));
            Assert.That(target.ShutdownCount, Is.EqualTo(1));
            Assert.That(target.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void Overlay_ReverseScreenPart()
        {
            var host = new GameObject("Host", typeof(RectTransform));
            spawned.Add(host);
            var overlay = QuitOutroOverlay.Create(host.transform);
            overlay.ApplyQuitOutro(0f);
            Assert.That(overlay.Open, Is.EqualTo(1f));
            Assert.That(overlay.LineAlpha, Is.EqualTo(0f).Within(1e-4f));
            overlay.ApplyQuitOutro(1f);
            Assert.That(overlay.Open, Is.EqualTo(0f));
            Assert.That(overlay.LineWidth, Is.EqualTo(0f));
            overlay.ApplyQuitOutro(0.5f);
            Assert.That((overlay.Open > 0f && overlay.Open < 1f) || (overlay.LineWidth > 0f && overlay.LineWidth < 1f), Is.True);
        }

        [Test]
        public void ThreeQuitPaths_UseQuitConfirmFlow()
        {
            var main = ReadProjectScript("App/UI/MainMenu/MainMenuBinder.cs");
            var surfaceBinder = ReadProjectScript("App/UI/SurfaceBase/SurfaceBaseBinder.cs");
            var underground = ReadProjectScript("App/Integration/UndergroundMenuController.cs");
            var mainView = ReadProjectScript("App/UI/MainMenu/MainMenuView.cs");
            var surfaceView = ReadProjectScript("App/UI/SurfaceBase/SurfaceBaseView.cs");
            Assert.That(main, Does.Contain("QuitConfirmFlow.Request("));
            Assert.That(surfaceBinder, Does.Contain("QuitConfirmFlow.Request("));
            Assert.That(underground, Does.Contain("QuitConfirmFlow.Request("));
            Assert.That(surfaceBinder, Does.Not.Contain("runtime.RequestQuit()"));
            Assert.That(underground, Does.Not.Contain("runtime.RequestQuit()"));
            Assert.That(main, Does.Not.Contain("SaveRuntimeController.Instance?.RequestQuit()"));
            Assert.That(mainView, Does.Contain("UiSfx.Attach(quitButton, SfxId.None"));
            Assert.That(surfaceView, Does.Contain("UiSfx.Attach(quitButton, SfxId.None"));
            Assert.That(underground, Does.Contain("\"QuitShortcut\""));
            Assert.That(underground, Does.Contain("SfxId.None"));
        }

        [Test]
        public void InputHandlers_YieldToQuitConfirm()
        {
            var main = ReadProjectScript("App/UI/MainMenu/MainMenuBinder.cs");
            var surfaceBinder = ReadProjectScript("App/UI/SurfaceBase/SurfaceBaseBinder.cs");
            var underground = ReadProjectScript("App/Integration/UndergroundMenuController.cs");
            var flow = ReadProjectScript("App/UI/QuitConfirm/QuitConfirmFlow.cs");
            Assert.That(main, Does.Contain("QuitConfirmFlow.IsBlockingInput"));
            Assert.That(surfaceBinder, Does.Contain("QuitConfirmFlow.IsBlockingInput"));
            Assert.That(underground, Does.Contain("QuitConfirmFlow.IsBlockingInput"));
            Assert.That(flow, Does.Contain("[DefaultExecutionOrder(-400)]"));
            Assert.That(underground, Does.Contain("[DefaultExecutionOrder(-300)]"));
        }

        private QuitRequestResult RequestShown(bool inGame)
        {
            var result = QuitConfirmFlow.Request(inGame);
            if (result == QuitRequestResult.Shown)
            {
                QuitConfirmFlow.Instance.ManualTick = true;
                QuitConfirmFlow.Instance.Popup.ManualTick = true;
            }
            return result;
        }

        private int Count(SfxId id)
        {
            var count = 0;
            foreach (var request in requests)
                if (request.Id == id) count++;
            return count;
        }

        private void Record(SfxId id, float pitch) => requests.Add((id, pitch));

        private static string ReadProjectScript(string relativePath) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "_Project", "Scripts", relativePath));

        private sealed class FakeTarget : IQuitTarget
        {
            public bool IsDirty { get; set; }
            public bool IsSaveInProgress { get; set; }
            public int SaveCount { get; private set; }
            public int ShutdownCount { get; private set; }
            public readonly List<string> Order = new List<string>();

            public void SaveBeforeQuit()
            {
                SaveCount++;
                Order.Add("save");
            }

            public void Shutdown()
            {
                ShutdownCount++;
                Order.Add("shutdown");
            }
        }

        private sealed class FakeMenuOutro : IQuitOutroView
        {
            public readonly MainMenuBootTimeline Timeline;
            public FakeMenuOutro(MainMenuBootTimeline timeline) { Timeline = timeline; }
            public float LineWidth { get; private set; }
            public float QuitOutroDuration => QuitOutroTimeline.MenuDuration(Timeline);
            public void BeginQuitOutro(bool reducedMotion) { }
            public void ApplyQuitOutro(float progress)
            {
                Timeline.SetElapsed(QuitOutroTimeline.BootTimeAt(Timeline.TotalDuration, progress));
                LineWidth = Timeline.Sample().LineWidth;
            }
        }
    }
}
