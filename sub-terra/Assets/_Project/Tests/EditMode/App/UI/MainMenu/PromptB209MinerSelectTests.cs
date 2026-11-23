using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using SubTerra.Shared.Localization;
using UnityEngine;

namespace SubTerra.App.Tests.UI.MainMenu
{
    /// <summary>B-209 새 게임 전 광부 선택 화면: 선택 상태, 새 게임 진입점 호출, 연출 시간표, 인트로 위치.</summary>
    public sealed class PromptB209MinerSelectTests
    {
        private readonly List<SfxId> sounds = new List<SfxId>();
        private readonly List<GameObject> spawned = new List<GameObject>();
        private bool previousReduceMotion;

        [SetUp]
        public void SetUp()
        {
            sounds.Clear();
            previousReduceMotion = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
            SfxRequests.PlayRequested += Record;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            AccessibilityPreferences.ReduceMotion = previousReduceMotion;
            foreach (var go in spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            spawned.Clear();
        }

        private void Record(SfxId id, float pitch) => sounds.Add(id);

        // ── 순수 상태 ──

        [Test]
        public void Flow_StartIsDisabledAndRejectedBeforeSelection()
        {
            var flow = new MinerSelectFlow();

            Assert.That(flow.CanConfirm, Is.False);
            Assert.That(flow.HintVisible, Is.True);
            Assert.That(flow.TryConfirm(), Is.EqualTo(MinerSelectResult.Rejected));
            Assert.That(flow.IsConfirmed, Is.False);
        }

        [Test]
        public void Flow_SelectEnablesStart_SwitchKeepsHintHidden()
        {
            var flow = new MinerSelectFlow();

            Assert.That(flow.Select(1), Is.EqualTo(MinerSelectResult.Selected));
            Assert.That(flow.Selected, Is.EqualTo(PlayerGender.Female));
            Assert.That(flow.CanConfirm, Is.True);
            Assert.That(flow.HintVisible, Is.False);

            Assert.That(flow.Select(0), Is.EqualTo(MinerSelectResult.Switched));
            Assert.That(flow.Selected, Is.EqualTo(PlayerGender.Male));
            Assert.That(flow.HintVisible, Is.False, "바꿔 고를 때 안내 글자는 다시 나오지 않는다");
            Assert.That(flow.Select(0), Is.EqualTo(MinerSelectResult.Reselected));
        }

        [Test]
        public void Flow_KeyboardMovesFocus_SubmitSelectsThenConfirms()
        {
            var flow = new MinerSelectFlow();

            Assert.That(flow.MoveFocus(1), Is.EqualTo(MinerSelectResult.Focused));
            Assert.That(flow.FocusIndex, Is.EqualTo(0));
            Assert.That(flow.MoveFocus(1), Is.EqualTo(MinerSelectResult.Focused));
            Assert.That(flow.FocusIndex, Is.EqualTo(1));
            Assert.That(flow.MoveFocus(1), Is.EqualTo(MinerSelectResult.Ignored), "끝에서 넘어가지 않는다");

            Assert.That(flow.Submit(), Is.EqualTo(MinerSelectResult.Selected));
            Assert.That(flow.Selected, Is.EqualTo(PlayerGender.Female));
            Assert.That(flow.Submit(), Is.EqualTo(MinerSelectResult.Confirmed));
            Assert.That(flow.IsLocked, Is.True);
            Assert.That(flow.Select(0), Is.EqualTo(MinerSelectResult.Ignored), "확정 뒤 입력 무시");
            Assert.That(flow.Back(), Is.EqualTo(MinerSelectResult.Ignored));
        }

        [Test]
        public void Flow_BackOnlyOnce_ResetReturnsToInitialState()
        {
            var flow = new MinerSelectFlow();
            flow.Select(1);

            Assert.That(flow.Back(), Is.EqualTo(MinerSelectResult.Back));
            Assert.That(flow.Back(), Is.EqualTo(MinerSelectResult.Ignored), "연타해도 한 번만");
            Assert.That(flow.TryConfirm(), Is.EqualTo(MinerSelectResult.Ignored));

            flow.Reset();
            Assert.That(flow.HasSelection, Is.False);
            Assert.That(flow.HintVisible, Is.True);
            Assert.That(flow.IsLocked, Is.False);
        }

        [Test]
        public void Session_ConfirmCallsEntryOnceWithSelectedGenderAndOverwrite()
        {
            var calls = new List<(int slot, bool overwrite, PlayerGender gender)>();
            var session = new MinerSelectSession(2, (slot, overwrite, gender) =>
            {
                calls.Add((slot, overwrite, gender));
                return true;
            });

            Assert.That(session.TryStart(), Is.False, "확정 전에는 부르지 않는다");
            session.Flow.Select(1);
            session.Flow.TryConfirm();

            Assert.That(session.TryStart(), Is.True);
            Assert.That(session.TryStart(), Is.False, "한 번만");
            Assert.That(calls, Is.EqualTo(new[] { (2, true, PlayerGender.Female) }));
        }

        [Test]
        public void Session_BackNeverCallsEntry()
        {
            var called = 0;
            var session = new MinerSelectSession(1, (slot, overwrite, gender) =>
            {
                called++;
                return true;
            });
            session.Flow.Select(0);
            session.Flow.Back();

            Assert.That(session.TryStart(), Is.False);
            Assert.That(called, Is.Zero, "돌아가면 슬롯·저장이 그대로다(StartNewGame 미호출)");
        }

        [Test]
        public void Session_FailedStartCanRetryAfterUnlock()
        {
            var results = new Queue<bool>(new[] { false, true });
            var session = new MinerSelectSession(1, (slot, overwrite, gender) => results.Dequeue());
            session.Flow.Select(0);
            session.Flow.TryConfirm();

            Assert.That(session.TryStart(), Is.False);
            Assert.That(session.HasStarted, Is.False);
            session.Flow.Unlock();
            session.Flow.TryConfirm();
            Assert.That(session.TryStart(), Is.True);
        }

        // ── 시간표 ──

        [Test]
        public void Motion_ReduceMotionHasNoGlitchOnlyShortFade()
        {
            Assert.That(MinerSelectMotion.TransitionSeconds(true), Is.EqualTo(MinerSelectMotion.ReducedFadeSeconds));
            for (var t = 0f; t <= MinerSelectMotion.ReducedFadeSeconds + 0.05f; t += 1f / 60f)
            {
                var frame = MinerSelectMotion.EvaluateTransition(t, true);
                Assert.That(frame.NoiseAlpha, Is.Zero);
                Assert.That(frame.SliceOffset, Is.Zero);
            }

            Assert.That(MinerSelectMotion.EvaluateTransition(MinerSelectMotion.ReducedFadeSeconds, true).BlackAlpha, Is.EqualTo(1f));
        }

        [Test]
        public void Motion_GlitchShakesThenEndsFullyBlack()
        {
            var sawNoise = false;
            var sawSlice = false;
            var lastBlack = 0f;
            for (var t = 0f; t < MinerSelectMotion.GlitchSeconds; t += 1f / 60f)
            {
                var frame = MinerSelectMotion.EvaluateTransition(t, false);
                sawNoise |= frame.NoiseAlpha > 0.1f;
                sawSlice |= Mathf.Abs(frame.SliceOffset) > 1f;
                Assert.That(frame.BlackAlpha, Is.GreaterThanOrEqualTo(lastBlack - 0.0001f));
                lastBlack = frame.BlackAlpha;
            }

            var end = MinerSelectMotion.EvaluateTransition(MinerSelectMotion.GlitchSeconds, false);
            Assert.That(sawNoise && sawSlice, Is.True);
            Assert.That(end.IsComplete, Is.True);
            Assert.That(end.BlackAlpha, Is.GreaterThanOrEqualTo(0.999f), "끝 프레임은 완전한 검정");
        }

        [Test]
        public void Motion_HintGlitchesOutWithinQuarterSecond()
        {
            Assert.That(MinerSelectMotion.EvaluateHint(0.01f).Alpha, Is.GreaterThan(0f));
            var end = MinerSelectMotion.EvaluateHint(MinerSelectMotion.HintGlitchOutSeconds);
            Assert.That(end.IsComplete, Is.True);
            Assert.That(end.Alpha, Is.Zero);
            Assert.That(MinerSelectMotion.HintGlitchOutSeconds, Is.EqualTo(0.25f).Within(0.0001f));
        }

        // ── 화면 ──

        [Test]
        public void Panel_StartDisabledUntilSelection_RejectPlaysFailOnly()
        {
            var panel = OpenPanel(out var session);

            Assert.That(panel.StartInteractable, Is.False);
            Assert.That(panel.IsHintShown, Is.True);
            Assert.That(panel.Card(0).Label.text, Is.Not.Empty);

            sounds.Clear();
            panel.PressStart();
            Assert.That(sounds, Is.EqualTo(new[] { SfxId.UiCancelFail }));
            Assert.That(panel.Phase, Is.EqualTo(MinerSelectPhase.Choosing));
            Assert.That(session.HasStarted, Is.False);
        }

        [Test]
        public void Panel_SelectRevealsChosenTopDownDimsOther_SwitchMovesHighlight()
        {
            var panel = OpenPanel(out _);

            panel.PressCard(1);
            Assert.That(sounds, Does.Contain(SfxId.UiSlotSelect));
            Assert.That(panel.StartInteractable, Is.True);
            panel.Tick(MinerSelectMotion.SilhouetteRevealSeconds + 0.05f);
            Assert.That(panel.CardReveal(1), Is.EqualTo(1f));
            Assert.That(panel.CardReveal(0), Is.Zero);
            Assert.That(panel.CardBrightness(0), Is.EqualTo(MinerSelectMotion.DimmedBrightness).Within(0.001f));
            Assert.That(panel.IsHintShown, Is.False, "안내 글자는 처음 고를 때 사라진다");

            panel.PressCard(0);
            panel.Tick(MinerSelectMotion.SilhouetteRevealSeconds + 0.05f);
            Assert.That(panel.CardReveal(0), Is.EqualTo(1f));
            Assert.That(panel.CardReveal(1), Is.Zero);
            Assert.That(panel.IsHintShown, Is.False, "바꿔 골라도 다시 나타나지 않는다");
        }

        [Test]
        public void Panel_BackRaisesOnceAndReopenStartsFresh()
        {
            var panel = OpenPanel(out var session);
            var backs = 0;
            panel.BackRequested += () => backs++;
            panel.PressCard(1);
            sounds.Clear();

            panel.PressBack();
            panel.PressBack();
            Assert.That(panel.Phase, Is.EqualTo(MinerSelectPhase.Closing), "TV 꺼지듯 접힌 뒤 돌아간다");
            Assert.That(backs, Is.Zero);
            for (var i = 0; i < 60 && backs == 0; i++) panel.Tick(1f / 30f);
            panel.PressBack();
            Assert.That(backs, Is.EqualTo(1));
            Assert.That(sounds, Is.EqualTo(new[] { SfxId.UiClick }), "UiBack은 B-158에서 없어져 취소음 UiClick");
            Assert.That(session.HasStarted, Is.False);

            panel.Close();
            panel.Open(new MinerSelectSession(1, (s, o, g) => true));
            FinishAppear(panel);
            panel.Tick(1f);
            Assert.That(panel.StartInteractable, Is.False);
            Assert.That(panel.IsHintShown, Is.True);
            Assert.That(panel.CardReveal(0), Is.Zero);
            Assert.That(panel.CardReveal(1), Is.Zero);
            Assert.That(panel.CardBrightness(0), Is.EqualTo(1f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Panel_ConfirmFillsColorThenClosesToBlack(bool reduceMotion)
        {
            AccessibilityPreferences.ReduceMotion = reduceMotion;
            var panel = OpenPanel(out var session);
            MinerSelectSession launched = null;
            panel.LaunchReady += s => launched = s;
            panel.PressCard(1);
            sounds.Clear();

            panel.PressStart();
            Assert.That(sounds, Is.EqualTo(new[] { SfxId.UiConfirm }));
            Assert.That(panel.Phase, Is.EqualTo(MinerSelectPhase.Filling));
            panel.PressCard(0);
            Assert.That(session.Flow.Selected, Is.EqualTo(PlayerGender.Female), "확정 뒤 바꿔 고를 수 없다");

            panel.Tick(MinerSelectMotion.ColorFillSeconds * 0.5f);
            Assert.That(panel.Card(1).AppliedFill, Is.GreaterThan(0f).And.LessThan(1f), "모션 감소여도 색 차오름은 그대로");
            Assert.That(panel.Card(0).AppliedFill, Is.Zero);
            panel.Tick(MinerSelectMotion.ColorFillSeconds);
            Assert.That(panel.Phase, Is.EqualTo(MinerSelectPhase.Exiting));
            Assert.That(launched, Is.Null);

            for (var i = 0; i < 120 && launched == null; i++) panel.Tick(1f / 60f);
            Assert.That(launched, Is.Not.Null);
            Assert.That(launched, Is.SameAs(session));
            Assert.That(panel.Phase, Is.EqualTo(MinerSelectPhase.Black));
            Assert.That(panel.BlackAlpha, Is.GreaterThanOrEqualTo(0.999f));
            Assert.That(sounds.Contains(SfxId.BriefingGlitchShort), Is.EqualTo(!reduceMotion), "모션 감소면 글리치 없음");
            Assert.That(session.HasStarted, Is.False, "새 게임 시작은 바인더가 인트로 오버레이에서 한다");
        }

        [Test]
        public void Panel_RecoverAfterFailedStartAllowsChoosingAgain()
        {
            var panel = OpenPanel(out var session);
            panel.PressCard(0);
            panel.PressStart();
            panel.Tick(MinerSelectMotion.ColorFillSeconds + 0.01f);
            for (var i = 0; i < 120 && panel.Phase != MinerSelectPhase.Black; i++) panel.Tick(1f / 60f);
            Assert.That(panel.Phase, Is.EqualTo(MinerSelectPhase.Black));

            panel.RecoverAfterFailedStart();
            Assert.That(panel.Phase, Is.EqualTo(MinerSelectPhase.Choosing));
            Assert.That(panel.BlackAlpha, Is.Zero);
            Assert.That(panel.StartInteractable, Is.True);
            Assert.That(session.Flow.Selected, Is.EqualTo(PlayerGender.Male));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Panel_AppearsLikeTvTurningOn_InputBlockedUntilOpen(bool reduceMotion)
        {
            AccessibilityPreferences.ReduceMotion = reduceMotion;
            var root = new GameObject("B209 Appear", typeof(RectTransform));
            spawned.Add(root);
            ((RectTransform)root.transform).sizeDelta = new Vector2(1920f, 1080f);
            var panel = MinerSelectPanel.Create((RectTransform)root.transform, null);
            sounds.Clear();
            panel.Open(new MinerSelectSession(1, (s, o, g) => true));

            Assert.That(panel.Phase, Is.EqualTo(MinerSelectPhase.Appearing));
            Assert.That(sounds.Contains(SfxId.UiCctvOn), Is.EqualTo(!reduceMotion), "TV 켜짐 소리는 TV 연출 때만");
            if (!reduceMotion) Assert.That(panel.StageOpen, Is.LessThan(0.01f), "처음엔 가운데 가로선뿐");
            panel.PressCard(0);
            Assert.That(panel.Flow.HasSelection, Is.False, "켜지는 동안 입력 무시");

            var lastOpen = 0f;
            for (var i = 0; i < 120 && panel.Phase == MinerSelectPhase.Appearing; i++)
            {
                panel.Tick(1f / 60f);
                Assert.That(panel.StageOpen, Is.GreaterThanOrEqualTo(lastOpen - 0.0001f));
                lastOpen = panel.StageOpen;
            }

            Assert.That(panel.Phase, Is.EqualTo(MinerSelectPhase.Choosing));
            Assert.That(panel.StageOpen, Is.EqualTo(1f));
        }

        [Test]
        public void Panel_SelectScanMovesTopToBottom_ConfirmColorScanToo()
        {
            var panel = OpenPanel(out _);
            panel.PressCard(0);
            var card = panel.Card(0);
            var lastY = float.MaxValue;
            var sawScan = false;
            for (var i = 0; i < 60 && panel.CardReveal(0) < 1f; i++)
            {
                panel.Tick(1f / 60f);
                if (!card.ScanVisible) continue;
                sawScan = true;
                Assert.That(card.ScanY, Is.LessThan(lastY + 0.001f), "고를 때 스캔 줄은 위에서 아래로");
                lastY = card.ScanY;
            }

            Assert.That(sawScan, Is.True);
            Assert.That(card.AppliedReveal, Is.EqualTo(1f));

            panel.PressStart();
            lastY = float.MaxValue;
            sawScan = false;
            for (var i = 0; i < 60 && panel.Phase == MinerSelectPhase.Filling; i++)
            {
                panel.Tick(1f / 60f);
                if (!card.ScanVisible) continue;
                sawScan = true;
                Assert.That(card.ScanY, Is.LessThan(lastY + 0.001f), "확정 때 색 입히는 스캔 줄도 위에서 아래로");
                lastY = card.ScanY;
            }

            Assert.That(sawScan, Is.True);
            Assert.That(MinerSelectCardView.ScanLineY(0f), Is.GreaterThan(MinerSelectCardView.ScanLineY(1f)));
        }

        [Test]
        public void Card_GlowOnlyWhenSelected_HoverGrowsOverThreeTenths()
        {
            Assert.That(MinerSelectMotion.HoverSeconds, Is.EqualTo(0.3f));
            var panel = OpenPanel(out _);
            var card = panel.Card(0);
            var other = panel.Card(1);
            TickFor(panel, 0.1f);
            Assert.That(card.GlowAlpha, Is.Zero, "고르기 전에는 바깥 빛이 없다");
            Assert.That(card.BodyScale, Is.EqualTo(1f).Within(0.0001f));

            card.OnPointerEnter(null);
            TickFor(panel, 0.15f);
            Assert.That(card.BodyScale, Is.GreaterThan(1f));
            Assert.That(card.BodyScale, Is.LessThan(MinerSelectMotion.HoverScale - 0.005f), "0.15초에는 아직 다 커지지 않는다");
            Assert.That(card.GlowAlpha, Is.Zero, "마우스만 올려도 바깥 빛은 없다");
            TickFor(panel, 0.2f);
            Assert.That(card.BodyScale, Is.EqualTo(MinerSelectMotion.HoverScale).Within(0.0001f));

            panel.PressCard(0);
            TickFor(panel, 0.5f);
            Assert.That(card.GlowAlpha, Is.GreaterThan(0.5f), "고른 카드만 바깥 빛");
            Assert.That(other.GlowAlpha, Is.Zero);
        }

        [Test]
        public void Buttons_UseRoundedOutlineAndGreyWhenOff()
        {
            var panel = OpenPanel(out _);
            var buttons = panel.GetComponentsInChildren<MinerSelectButtonView>(true);
            Assert.That(buttons.Length, Is.EqualTo(2));
            foreach (var button in buttons)
            {
                Assert.That(button.Frame.sprite, Is.SameAs(MinerSelectArt.RoundedOutline()));
                Assert.That(button.Frame.type, Is.EqualTo(UnityEngine.UI.Image.Type.Sliced));
                Assert.That(button.GetComponentInChildren<UnityEngine.UI.Mask>(true), Is.Not.Null, "빛은 둥근 바탕 안에서만");
            }

            Assert.That(panel.StartLit, Is.False);
            var start = System.Array.Find(buttons, b => b.name == "StartButton");
            Assert.That(start.Frame.color.b, Is.LessThan(0.7f), "꺼진 탐사 시작은 회색 테두리");
            panel.PressCard(0);
            Assert.That(start.Frame.color, Is.EqualTo(MinerSelectArt.Cyan));
        }

        [Test]
        public void Motion_CloseFoldsToLineThenEnds()
        {
            var start = MinerSelectMotion.EvaluateClose(0f, false);
            Assert.That(start.Open, Is.EqualTo(1f));
            var end = MinerSelectMotion.EvaluateClose(MinerSelectMotion.CloseSeconds, false);
            Assert.That(end.IsComplete, Is.True);
            Assert.That(end.Open, Is.Zero);
            var reduced = MinerSelectMotion.EvaluateClose(MinerSelectMotion.ReducedFadeSeconds * 0.5f, true);
            Assert.That(reduced.Open, Is.EqualTo(1f), "모션 감소면 접지 않고 페이드만");
            Assert.That(reduced.Alpha, Is.LessThan(1f));
        }

        [Test]
        public void Motion_GlitchCollapsesLikeTvOffBeforeBlack()
        {
            var sawCollapse = false;
            for (var t = 0f; t < MinerSelectMotion.GlitchSeconds; t += 1f / 120f)
            {
                var frame = MinerSelectMotion.EvaluateTransition(t, false);
                if (frame.Collapse > 0.5f && frame.BlackAlpha < 1f) sawCollapse = true;
            }

            Assert.That(sawCollapse, Is.True);
            Assert.That(MinerSelectMotion.EvaluateTransition(0f, true).Collapse, Is.Zero);
        }

        [Test]
        public void Art_LoadsFromResources()
        {
            Assert.That(MinerSelectArt.CardFrame(), Is.Not.Null);
            Assert.That(MinerSelectArt.PedestalArt(), Is.Not.Null);
            Assert.That(MinerSelectArt.ScreenFrame(), Is.Not.Null);
            Assert.That(MinerSelectArt.RoundedOutline().border.x, Is.GreaterThan(MinerSelectArt.ButtonLineInset + MinerSelectArt.ButtonCornerRadius),
                "버튼 둥근 테두리는 모서리가 늘어나지 않는 9분할");
            Assert.That(MinerSelectArt.RoundedFill().border.x, Is.EqualTo(MinerSelectArt.RoundedOutline().border.x), "바탕·마스크는 테두리와 같은 모양");
            Assert.That(MinerSelectArt.CardGlow().border.x, Is.GreaterThan(MinerSelectArt.CardGlowPad + MinerSelectArt.CardChamfer),
                "카드 빛의 깎인 모서리는 9분할 모서리 안에 있다");
            Assert.That(MinerSelectArt.ScreenFrame().border.x, Is.GreaterThan(0f), "화면 테두리는 9분할");
            Assert.That(MinerSelectArt.HologramShader(), Is.Not.Null);
            Assert.That(MinerSelectArt.GlitchShader(), Is.Not.Null);
        }

        // ── 문구·인트로 위치 ──

        [Test]
        public void Localization_HasKoreanAndEnglishKeys()
        {
            var previous = LocalizationService.Current;
            try
            {
                LocalizationService.SetLanguage(GameLanguage.English);
                Assert.That(LocalizationService.Get("miner_select.title"), Is.EqualTo("Select Miner"));
                Assert.That(LocalizationService.Get("miner_select.male"), Is.EqualTo("Male"));
                Assert.That(LocalizationService.Get("miner_select.female"), Is.EqualTo("Female"));
                Assert.That(LocalizationService.Get("miner_select.start"), Is.EqualTo("Start Expedition"));
                Assert.That(LocalizationService.Get("miner_select.back"), Is.EqualTo("Back"));
                Assert.That(LocalizationService.Get("miner_select.hint"), Is.EqualTo("Select a miner"));
                LocalizationService.SetLanguage(GameLanguage.Korean);
                Assert.That(LocalizationService.Get("miner_select.title"), Is.EqualTo("광부 선택"));
                Assert.That(LocalizationService.Get("miner_select.hint"), Is.EqualTo("광부를 선택해 주세요"));
            }
            finally
            {
                LocalizationService.SetLanguage(previous);
            }
        }

        [Test]
        public void IntroMovedFromFirstMineEntryToMenuLaunch()
        {
            var binder = Source("Scripts", "App", "Integration", "IntegrationRuntimeBinder.cs");
            Assert.That(binder, Does.Not.Contain("NewGameIntroPlayer"), "첫 광산 진입에서 영상을 틀지 않는다");
            Assert.That(binder, Does.Contain("BindTutorialDirector();"), "시작 브리핑 바인딩은 그대로");

            var menu = Source("Scripts", "App", "UI", "MainMenu", "MainMenuBinder.cs");
            Assert.That(menu, Does.Contain("RunNewGameLaunch"));
            Assert.That(menu, Does.Contain("session.TryStart"));
        }

        private MinerSelectPanel OpenPanel(out MinerSelectSession session)
        {
            var root = new GameObject("B209 Root", typeof(RectTransform));
            spawned.Add(root);
            ((RectTransform)root.transform).sizeDelta = new Vector2(1920f, 1080f);
            var panel = MinerSelectPanel.Create((RectTransform)root.transform, null);
            Assert.That(panel, Is.Not.Null);
            session = new MinerSelectSession(1, (s, o, g) => true);
            panel.Open(session);
            FinishAppear(panel);
            sounds.Clear();
            return panel;
        }

        private static void TickFor(MinerSelectPanel panel, float seconds)
        {
            for (var t = 0f; t < seconds - 0.0001f; t += 1f / 100f) panel.Tick(1f / 100f);
        }

        private static void FinishAppear(MinerSelectPanel panel)
        {
            for (var i = 0; i < 120 && panel.Phase == MinerSelectPhase.Appearing; i++) panel.Tick(1f / 30f);
            Assert.That(panel.Phase, Is.EqualTo(MinerSelectPhase.Choosing));
        }

        private static string Source(params string[] parts)
        {
            var path = Path.Combine(Application.dataPath, "_Project");
            foreach (var part in parts) path = Path.Combine(path, part);
            return File.ReadAllText(path);
        }
    }
}
