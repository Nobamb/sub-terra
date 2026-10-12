using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.UI.ControlTerminal;
using SubTerra.App.UI.Guide;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>조작 단말(키 조작 방식 창)의 순수 계산과 View 배선. PlayMode 없이 EditMode에서만 검증한다.</summary>
    public sealed class ControlTerminalTests
    {
        private readonly List<Object> spawned = new List<Object>();
        private ControlScheme originalScheme;
        private bool originalBlocked;
        private bool hadPref;
        private int originalPref;

        [SetUp]
        public void SetUp()
        {
            originalScheme = ControlPreferences.Scheme;
            originalBlocked = ControlPreferences.IsSettingsOpen;
            hadPref = PlayerPrefs.HasKey(SettingsRuntimeApplier.PrefControls);
            originalPref = PlayerPrefs.GetInt(SettingsRuntimeApplier.PrefControls, 0);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = spawned.Count - 1; i >= 0; i--)
                if (spawned[i] != null) Object.DestroyImmediate(spawned[i]);
            spawned.Clear();
            ControlPreferences.Scheme = originalScheme;
            ControlPreferences.IsSettingsOpen = originalBlocked;
            if (hadPref) PlayerPrefs.SetInt(SettingsRuntimeApplier.PrefControls, originalPref);
            else PlayerPrefs.DeleteKey(SettingsRuntimeApplier.PrefControls);
            PlayerPrefs.Save();
        }

        // ---------------------------------------------------------------- 바인딩·문구

        [TestCase(ControlScheme.Classic, "W", ControlKeyFunction.MoveUp)]
        [TestCase(ControlScheme.Classic, "→", ControlKeyFunction.MoveRight)]
        [TestCase(ControlScheme.Classic, "Enter", ControlKeyFunction.MineFacing)]
        [TestCase(ControlScheme.Classic, "Space", ControlKeyFunction.Jump)]
        [TestCase(ControlScheme.Classic, "마우스 클릭", ControlKeyFunction.MineMouse)]
        [TestCase(ControlScheme.Classic, "Q", ControlKeyFunction.None)]
        [TestCase(ControlScheme.WasdMove, "A", ControlKeyFunction.MoveLeft)]
        [TestCase(ControlScheme.WasdMove, "↑", ControlKeyFunction.MineUp)]
        [TestCase(ControlScheme.WasdMove, "↓", ControlKeyFunction.MineDown)]
        [TestCase(ControlScheme.ArrowsMove, "←", ControlKeyFunction.MoveLeft)]
        [TestCase(ControlScheme.ArrowsMove, "S", ControlKeyFunction.MineDown)]
        [TestCase(ControlScheme.ArrowsMove, "D", ControlKeyFunction.MineRight)]
        public void Bindings_MapKeysToFunctions(ControlScheme scheme, string key, ControlKeyFunction expected)
        {
            Assert.That(ControlTerminalBindings.FunctionOf(scheme, key), Is.EqualTo(expected));
        }

        [Test]
        public void Bindings_RolesAndLabelsFollowTheScheme()
        {
            Assert.That(ControlTerminalBindings.RoleLabel(ControlScheme.Classic, "W"), Is.EqualTo("↑ 이동"));
            Assert.That(ControlTerminalBindings.RoleLabel(ControlScheme.WasdMove, "←"), Is.EqualTo("← 채굴"));
            Assert.That(ControlTerminalBindings.RoleLabel(ControlScheme.ArrowsMove, "D"), Is.EqualTo("→ 채굴"));
            Assert.That(ControlTerminalBindings.RoleLabel(ControlScheme.Classic, "Space"), Is.EqualTo("점프"));
            Assert.That(ControlTerminalBindings.RoleLabel(ControlScheme.Classic, "Enter"), Is.EqualTo("채굴"));
            Assert.That(ControlTerminalBindings.RoleLabel(ControlScheme.Classic, "Q"), Is.Empty);
            Assert.That(ControlTerminalBindings.RoleOf(ControlKeyFunction.Jump), Is.EqualTo(ControlKeyRole.Jump));
            Assert.That(ControlTerminalBindings.RoleOf(ControlKeyFunction.MoveLeft), Is.EqualTo(ControlKeyRole.Move));
            Assert.That(ControlTerminalBindings.RoleOf(ControlKeyFunction.MineMouse), Is.EqualTo(ControlKeyRole.Mine));
        }

        [Test]
        public void Bindings_KeepTheExistingTexts()
        {
            Assert.That(ControlTerminalBindings.Title(ControlScheme.Classic), Is.EqualTo("키 조작 방식 1 / 3"));
            Assert.That(ControlTerminalBindings.Title(ControlScheme.ArrowsMove), Is.EqualTo("키 조작 방식 3 / 3"));
            Assert.That(ControlTerminalBindings.Description(ControlScheme.Classic), Does.Contain("위·아래는 사다리"));
            Assert.That(ControlTerminalBindings.Description(ControlScheme.WasdMove), Does.StartWith("WASD: 이동 · 방향키: 상하좌우 채굴"));
            Assert.That(ControlTerminalBindings.Description(ControlScheme.ArrowsMove), Does.StartWith("방향키: 이동 · WASD: 상하좌우 채굴"));
            Assert.That(ControlTerminalBindings.Description(ControlScheme.WasdMove), Does.Contain("마우스 / Enter도 사용 가능"));
            Assert.That(ControlTerminalBindings.Caption, Is.EqualTo("키보드 · 이동 / 채굴"));
        }

        [TestCase(ControlScheme.Classic)]
        [TestCase(ControlScheme.WasdMove)]
        [TestCase(ControlScheme.ArrowsMove)]
        public void DemoKeys_ListEachFunctionOnce(ControlScheme scheme)
        {
            var keys = ControlTerminalBindings.DemoKeys(scheme);
            var functions = new HashSet<ControlKeyFunction>();
            foreach (var key in keys) Assert.That(functions.Add(ControlTerminalBindings.FunctionOf(scheme, key)), Is.True, key);
            Assert.That(functions.Contains(ControlKeyFunction.Jump), Is.True);
            Assert.That(functions.Contains(ControlKeyFunction.MineMouse), Is.True);
            Assert.That(keys.Count, Is.EqualTo(functions.Count));
        }

        // ---------------------------------------------------------------- 배치

        [Test]
        public void Layout_KeysAreUniqueInsideTheCardAndDoNotOverlap()
        {
            var keys = ControlTerminalLayout.Keys;
            var ids = new HashSet<string>();
            for (int i = 0; i < keys.Count; i++)
            {
                Assert.That(ids.Add(keys[i].Id), Is.True, keys[i].Id);
                Assert.That(Mathf.Abs(keys[i].Center.x) + keys[i].Size.x * 0.5f, Is.LessThan(ControlTerminalLayout.CardWidth * 0.5f));
                Assert.That(Mathf.Abs(keys[i].Center.y) + keys[i].Size.y * 0.5f, Is.LessThan(ControlTerminalLayout.CardHeight * 0.5f));
                Assert.That(keys[i].Right, Is.LessThan(ControlTerminalLayout.PlateRight));
                for (int j = i + 1; j < keys.Count; j++)
                {
                    var a = new Rect(keys[i].Center - keys[i].Size * 0.5f, keys[i].Size);
                    var b = new Rect(keys[j].Center - keys[j].Size * 0.5f, keys[j].Size);
                    Assert.That(a.Overlaps(b), Is.False, keys[i].Id + " vs " + keys[j].Id);
                }
            }

            Assert.That(ControlTerminalLayout.TryGet("Space", out _), Is.True);
            Assert.That(ControlTerminalLayout.TryGet(ControlTerminalBindings.MouseKey, out var mouse), Is.True);
            Assert.That(mouse.IsMouse, Is.True);
        }

        [Test]
        public void Layout_ScreenAndButtonsFitWithoutCoveringTheKeyboard()
        {
            var screenLeft = ControlTerminalLayout.ScreenLeftMiddle.x;
            Assert.That(ControlTerminalLayout.PlateRight, Is.LessThan(ControlTerminalLayout.ConnectorExitX));
            Assert.That(ControlTerminalLayout.ConnectorExitX, Is.LessThan(screenLeft));
            var screenRight = ControlTerminalLayout.ScreenCenter.x + ControlTerminalLayout.ScreenSize.x * 0.5f;
            Assert.That(screenRight, Is.LessThan(ControlTerminalLayout.NextButtonX - 28f));
            Assert.That(ControlTerminalLayout.ScreenSize.x / ControlTerminalLayout.ScreenSize.y, Is.EqualTo(16f / 9f).Within(0.01f));
        }

        // ---------------------------------------------------------------- 시간표

        [Test]
        public void Intro_SettlesToExactBaseValues()
        {
            var end = ControlTerminalTimeline.Intro(ControlTerminalTimeline.IntroDuration);
            Assert.That(end.Settled, Is.True);
            Assert.That(end.HoloActive, Is.False);
            Assert.That(end.KeyboardAlpha, Is.EqualTo(1f));
            Assert.That(end.TextAlpha, Is.EqualTo(1f));
            Assert.That(ControlTerminalTimeline.Intro(ControlTerminalTimeline.IntroDuration + 1f).Settled, Is.True);

            // 연출 층은 끝에서 모두 사라지고 완성된 프레임만 남는다.
            var holo = ControlTerminalTimeline.IntroHolo(ControlTerminalTimeline.HoloEnd);
            Assert.That(holo.Active, Is.False);
            Assert.That(holo.RealFrame, Is.True);
            Assert.That(holo.Flash, Is.EqualTo(0f));
            Assert.That(holo.Glow, Is.EqualTo(0f));
            Assert.That(holo.EdgeAlpha, Is.EqualTo(0f));
            for (int i = 0; i < ControlTerminalTimeline.GhostCount; i++)
                Assert.That(ControlTerminalTimeline.IntroGhost(i, ControlTerminalTimeline.HoloEnd).Alpha, Is.EqualTo(0f));

            var pop = ControlTerminalTimeline.IntroKeyPop(ControlTerminalTimeline.IntroDuration, 4, 1f, true);
            Assert.That(pop.Scale, Is.EqualTo(1f));
            Assert.That(pop.Lift, Is.EqualTo(0f));
            Assert.That(pop.Alpha, Is.EqualTo(1f));
        }

        [Test]
        public void Intro_LineThenOpenFrameThenFlashFadesAndKeysFollow()
        {
            var start = ControlTerminalTimeline.Intro(0f);
            Assert.That(start.HoloActive, Is.True);
            Assert.That(start.KeyboardAlpha, Is.EqualTo(0f));
            Assert.That(start.TextAlpha, Is.EqualTo(0f));

            // 0.05초: 수평 빛이 뻗는 중이고 창 높이는 아직 0.
            var line = ControlTerminalTimeline.IntroHolo(0.05f);
            Assert.That(line.Width, Is.GreaterThan(0.2f));
            Assert.That(line.Width, Is.LessThan(1f));
            Assert.That(line.Height, Is.EqualTo(0f));
            Assert.That(line.RealFrame, Is.False);
            // 폭은 0.13초에 가득 차고, 그 뒤 창이 위아래로 열려 0.36초에 정확히 가득 찬다.
            Assert.That(ControlTerminalTimeline.IntroHolo(ControlTerminalTimeline.LineEnd).Width, Is.EqualTo(1f).Within(0.001f));
            var mid = ControlTerminalTimeline.IntroHolo(0.24f);
            Assert.That(mid.Height, Is.InRange(0.2f, 0.95f));
            Assert.That(mid.CopyAlpha, Is.EqualTo(1f));
            Assert.That(mid.RealFrame, Is.False);
            var open = ControlTerminalTimeline.IntroHolo(ControlTerminalTimeline.OpenEnd);
            Assert.That(open.Height, Is.EqualTo(1f).Within(0.001f));
            Assert.That(open.RealFrame, Is.True);
            Assert.That(open.CopyAlpha, Is.EqualTo(0f));
            // 흰빛은 열림이 끝나는 순간 가장 강하고 이후 서서히 사라진다.
            Assert.That(open.Flash, Is.EqualTo(1f).Within(0.001f));
            float previous = 1f;
            for (float t = ControlTerminalTimeline.OpenEnd; t < ControlTerminalTimeline.HoloEnd; t += 0.01f)
            {
                var flash = ControlTerminalTimeline.IntroHolo(t).Flash;
                Assert.That(flash, Is.LessThanOrEqualTo(previous + 0.0001f), "t=" + t);
                previous = flash;
            }

            Assert.That(ControlTerminalTimeline.IntroHolo(0.60f).Flash, Is.InRange(0.05f, 0.9f));
            // 키캡은 프레임이 다 열린 뒤에 시작한다.
            Assert.That(ControlTerminalTimeline.KeyPopStart, Is.GreaterThanOrEqualTo(ControlTerminalTimeline.OpenEnd));
            var beforeKeys = ControlTerminalTimeline.IntroKeyPop(ControlTerminalTimeline.KeyPopStart - 0.01f, 0, 0f, false);
            Assert.That(beforeKeys.Scale, Is.EqualTo(0f));
            Assert.That(beforeKeys.Alpha, Is.EqualTo(0f));
            Assert.That(ControlTerminalTimeline.Intro(ControlTerminalTimeline.KeyPopStart).KeyboardAlpha, Is.EqualTo(1f).Within(0.001f));
            Assert.That(ControlTerminalTimeline.Intro(0.2f).TextAlpha, Is.EqualTo(0f));
            Assert.That(ControlTerminalTimeline.Intro(0.45f).TextAlpha, Is.InRange(0.01f, 0.99f));
            Assert.That(ControlTerminalTimeline.IntroDuration, Is.LessThanOrEqualTo(1.1f));
        }

        [Test]
        public void Intro_HoloLayersStayInRangeFlickerAndGhostsConverge()
        {
            bool sawGhost = false;
            bool sawDip = false;
            bool sawBeam = false;
            bool sawFlare = false;
            for (float t = 0f; t <= ControlTerminalTimeline.HoloEnd; t += 0.004f)
            {
                var holo = ControlTerminalTimeline.IntroHolo(t);
                foreach (var value in new[] { holo.Width, holo.Height, holo.CopyAlpha, holo.LineAlpha, holo.EdgeAlpha, holo.Flash, holo.Glow, holo.BeamAlpha, holo.Flare })
                    Assert.That(value, Is.InRange(0f, 1f), "t=" + t);
                if (ControlTerminalTimeline.Flicker(t) < 1f) sawDip = true;
                if (holo.BeamAlpha > 0.3f) sawBeam = true;
                if (holo.Flare > 0.5f) sawFlare = true;
                for (int i = 0; i < ControlTerminalTimeline.GhostCount; i++)
                {
                    var ghost = ControlTerminalTimeline.IntroGhost(i, t);
                    Assert.That(ghost.Alpha, Is.InRange(0f, 1f));
                    Assert.That(ghost.Spread, Is.GreaterThanOrEqualTo(0f));
                    if (ghost.Alpha > 0.4f) sawGhost = true;
                }
            }

            Assert.That(sawGhost, Is.True);
            Assert.That(sawDip, Is.True, "열리는 동안 홀로그램이 깜빡인다");
            Assert.That(sawBeam, Is.True);
            Assert.That(sawFlare, Is.True);
            // 같은 시각에는 항상 같은 값이고, 열린 뒤에는 깜빡이지 않는다.
            Assert.That(ControlTerminalTimeline.Flicker(0.1f), Is.EqualTo(ControlTerminalTimeline.Flicker(0.1f)));
            Assert.That(ControlTerminalTimeline.Flicker(ControlTerminalTimeline.OpenEnd), Is.EqualTo(1f));
            // 잔상은 바깥에서 시작해 카드와 같은 크기로 모인다.
            var early = ControlTerminalTimeline.IntroGhost(0, 0.14f);
            var late = ControlTerminalTimeline.IntroGhost(0, 0.40f);
            Assert.That(early.Spread, Is.GreaterThan(late.Spread));
        }

        [Test]
        public void Intro_KeyPopsBigThenReturnsAndStaggersByRowAndSpaceLater()
        {
            // 없던 키캡이 크게 튀어나왔다가(1보다 큼) 한 번 눌린 듯 작아지고(1보다 작음) 기준 크기로 돌아온다.
            float peak = 0f;
            float low = 99f;
            float maxLift = 0f;
            bool afterPeak = false;
            for (float t = 0f; t <= ControlTerminalTimeline.IntroDuration; t += 0.002f)
            {
                var pop = ControlTerminalTimeline.IntroKeyPop(t, 1, 0.5f, false);
                Assert.That(pop.Alpha, Is.InRange(0f, 1f));
                Assert.That(pop.Scale, Is.InRange(0f, ControlTerminalTimeline.KeyPopPeak + 0.001f));
                Assert.That(pop.Lift, Is.GreaterThanOrEqualTo(0f));
                if (pop.Scale > peak) peak = pop.Scale;
                if (peak > 1.3f) afterPeak = true;
                if (afterPeak && pop.Scale < low) low = pop.Scale;
                if (pop.Lift > maxLift) maxLift = pop.Lift;
            }

            Assert.That(peak, Is.GreaterThan(1.3f));
            Assert.That(low, Is.LessThan(1f));
            Assert.That(maxLift, Is.GreaterThan(4f));

            // 줄마다 시차: 같은 시각에 윗줄이 먼저 나타난다. Space는 같은 줄 키보다 늦다.
            var t0 = ControlTerminalTimeline.KeyPopStart + 0.03f;
            Assert.That(ControlTerminalTimeline.IntroKeyPop(t0, 0, 0f, false).Scale, Is.GreaterThan(0f));
            Assert.That(ControlTerminalTimeline.IntroKeyPop(t0, 3, 0f, false).Scale, Is.EqualTo(0f));
            var rowStart = ControlTerminalTimeline.KeyPopStart + 3 * ControlTerminalTimeline.KeyStagger + 0.01f;
            Assert.That(ControlTerminalTimeline.IntroKeyPop(rowStart, 3, 0f, false).Scale, Is.GreaterThan(0f));
            Assert.That(ControlTerminalTimeline.IntroKeyPop(rowStart, 3, 0f, true).Scale, Is.EqualTo(0f));
            // 가장 늦은 키도 등장 시간 안에 기준값으로 정착한다.
            Assert.That(ControlTerminalTimeline.IntroKeyPop(ControlTerminalTimeline.IntroDuration, 4, 1f, true).Scale, Is.EqualTo(1f));
        }

        [Test]
        public void Outro_EndsDarkAndSwapEndsAtFullAlpha()
        {
            var end = ControlTerminalTimeline.Outro(ControlTerminalTimeline.OutroDuration);
            Assert.That(end.ContentAlpha, Is.EqualTo(0f));
            Assert.That(end.HoloActive, Is.False);
            Assert.That(ControlTerminalTimeline.Outro(0f).HoloActive, Is.True);

            // 퇴장: 시작은 완성된 프레임, 흰빛이 달아오르고 창이 닫히며 수평 빛이 되었다가 사라진다.
            var begin = ControlTerminalTimeline.OutroHolo(0f);
            Assert.That(begin.RealFrame, Is.True);
            Assert.That(begin.Height, Is.EqualTo(1f));
            Assert.That(begin.Width, Is.EqualTo(1f));
            Assert.That(begin.Flash, Is.EqualTo(0f));
            var bright = ControlTerminalTimeline.OutroHolo(0.10f);
            Assert.That(bright.Flash, Is.EqualTo(1f).Within(0.001f));
            Assert.That(bright.RealFrame, Is.False);
            var closing = ControlTerminalTimeline.OutroHolo(0.18f);
            Assert.That(closing.Height, Is.InRange(0.05f, 0.95f));
            var lineOnly = ControlTerminalTimeline.OutroHolo(0.26f);
            Assert.That(lineOnly.Height, Is.EqualTo(0f).Within(0.001f));
            Assert.That(lineOnly.LineAlpha, Is.GreaterThan(0.5f));
            var last = ControlTerminalTimeline.OutroHolo(ControlTerminalTimeline.OutroDuration);
            Assert.That(last.Active, Is.False);
            Assert.That(last.Flash, Is.EqualTo(0f));
            for (float t = 0f; t <= ControlTerminalTimeline.OutroDuration; t += 0.004f)
            {
                var holo = ControlTerminalTimeline.OutroHolo(t);
                foreach (var value in new[] { holo.Width, holo.Height, holo.CopyAlpha, holo.LineAlpha, holo.EdgeAlpha, holo.Flash, holo.Glow, holo.Flare })
                    Assert.That(value, Is.InRange(0f, 1f), "t=" + t);
                for (int i = 0; i < ControlTerminalTimeline.GhostCount; i++)
                    Assert.That(ControlTerminalTimeline.OutroGhost(i, t).Alpha, Is.InRange(0f, 1f));
            }

            for (int i = 0; i < ControlTerminalTimeline.GhostCount; i++)
                Assert.That(ControlTerminalTimeline.OutroGhost(i, ControlTerminalTimeline.OutroDuration).Alpha, Is.EqualTo(0f));

            Assert.That(ControlTerminalTimeline.Outro(0f).ContentAlpha, Is.EqualTo(1f));
            Assert.That(ControlTerminalTimeline.PageSwapAlpha(0f), Is.EqualTo(1f));
            Assert.That(ControlTerminalTimeline.PageSwapAlpha(ControlTerminalTimeline.PageSwapDuration), Is.EqualTo(1f));
            Assert.That(ControlTerminalTimeline.PageSwapAlpha(ControlTerminalTimeline.PageSwapDuration * 0.4f), Is.LessThan(0.5f));
            Assert.That(ControlTerminalTimeline.PageSwapDuration, Is.InRange(0.18f, 0.25f));
        }

        [Test]
        public void Screen_OpensLightThenHeightThenScan()
        {
            var off = ControlTerminalTimeline.Screen(0f);
            Assert.That(off.WidthFraction, Is.EqualTo(0f));
            Assert.That(off.HeightFraction, Is.EqualTo(0f));
            var light = ControlTerminalTimeline.Screen(0.3f);
            Assert.That(light.WidthFraction, Is.EqualTo(1f).Within(0.001f));
            Assert.That(light.HeightFraction, Is.EqualTo(0f).Within(0.001f));
            Assert.That(light.LightAlpha, Is.GreaterThan(0.5f));
            var on = ControlTerminalTimeline.Screen(1f);
            Assert.That(on.HeightFraction, Is.EqualTo(1f).Within(0.001f));
            Assert.That(on.ContentAlpha, Is.EqualTo(1f));
            Assert.That(on.ScanAlpha, Is.EqualTo(0f));
            Assert.That(ControlTerminalTimeline.Screen(0.85f).ScanAlpha, Is.GreaterThan(0f));
        }

        // ---------------------------------------------------------------- 키 눌림

        [Test]
        public void KeyMotion_PressesInEightyMsAndReleasesToExactZero()
        {
            var motion = new ControlTerminalKeyMotion();
            motion.SetDown(true);
            motion.Tick(0.04f);
            Assert.That(motion.Level, Is.EqualTo(0.5f).Within(0.001f));
            motion.Tick(0.04f);
            Assert.That(motion.Level, Is.EqualTo(1f).Within(0.001f));
            // 호버를 유지해도 흔들리지 않는다.
            for (int i = 0; i < 30; i++) motion.Tick(0.016f);
            Assert.That(motion.Level, Is.EqualTo(1f));

            motion.SetDown(false);
            bool wentBelowZero = false;
            for (int i = 0; i < 20; i++)
            {
                motion.Tick(0.01f);
                if (motion.Level < 0f) wentBelowZero = true;
                Assert.That(motion.Level, Is.GreaterThan(-0.3f));
            }

            Assert.That(wentBelowZero, Is.True, "약한 탄성이 한 번 있어야 한다");
            Assert.That(motion.Level, Is.EqualTo(0f));
            Assert.That(motion.IsSettled, Is.True);
        }

        [Test]
        public void KeyMotion_ReleaseTakesAbout120ms()
        {
            var motion = new ControlTerminalKeyMotion();
            motion.SetDown(true);
            motion.Tick(0.1f);
            motion.SetDown(false);
            motion.Tick(ControlTerminalKeyMotion.ReleaseSeconds * 0.5f);
            Assert.That(motion.IsSettled, Is.False);
            motion.Tick(ControlTerminalKeyMotion.ReleaseSeconds * 0.6f);
            Assert.That(motion.Level, Is.EqualTo(0f));
            motion.SetDown(false);
            Assert.That(motion.Level, Is.EqualTo(0f));
        }

        // ---------------------------------------------------------------- 호버 상태

        private static void Run(ControlTerminalHoverState state, float seconds)
        {
            for (float t = 0f; t < seconds - 0.0001f; t += 0.01f) state.Tick(0.01f);
        }

        [Test]
        public void Hover_WaitsDwellBeforeLineAndCancelsOnExit()
        {
            var state = new ControlTerminalHoverState();
            state.Enter("A");
            Run(state, 0.10f);
            Assert.That(state.ActiveKey, Is.Null);
            Assert.That(state.CandidateKey, Is.EqualTo("A"));
            state.Exit("A");
            Run(state, 0.20f);
            Assert.That(state.ActiveKey, Is.Null, "대기 중 벗어나면 지연 요청이 취소된다");
            Assert.That(state.IsIdle, Is.True);

            state.Enter("A");
            Run(state, 0.16f);
            Assert.That(state.ActiveKey, Is.EqualTo("A"));
            Assert.That(state.LineProgress, Is.GreaterThan(0f));
        }

        [Test]
        public void Hover_LineThenScreenOpenThenVideoPlays()
        {
            var state = new ControlTerminalHoverState();
            state.Enter("D");
            Run(state, ControlTerminalHoverState.DwellSeconds + 0.02f);
            Assert.That(state.ScreenLevel, Is.EqualTo(0f));
            Run(state, ControlTerminalHoverState.LineSeconds);
            Assert.That(state.LineProgress, Is.EqualTo(1f));
            Run(state, ControlTerminalHoverState.ScreenOnSeconds + 0.03f);
            Assert.That(state.ScreenLevel, Is.EqualTo(1f));
            Assert.That(state.VideoPlaying, Is.True);
        }

        [Test]
        public void Hover_SwitchingKeysKeepsTheScreenAndReplacesOnlyContent()
        {
            var state = new ControlTerminalHoverState();
            state.Enter("A");
            Run(state, 0.6f);
            Assert.That(state.ScreenLevel, Is.EqualTo(1f));
            state.Exit("A");
            state.Enter("D");
            Run(state, 0.05f);
            Assert.That(state.ActiveKey, Is.EqualTo("A"));
            Assert.That(state.ScreenLevel, Is.EqualTo(1f), "TV 켜짐을 다시 시작하지 않는다");
            Assert.That(state.ContentAlpha, Is.LessThan(1f), "대기 중 이전 내용은 흐려진다");
            Assert.That(state.VideoPlaying, Is.False, "대기 중 이전 영상은 멈춘다");
            Run(state, 0.12f);
            Assert.That(state.ActiveKey, Is.EqualTo("D"));
            Assert.That(state.ScreenLevel, Is.EqualTo(1f));
            Assert.That(state.LineProgress, Is.LessThan(1f).Or.EqualTo(1f));
            Run(state, 0.3f);
            Assert.That(state.ContentAlpha, Is.EqualTo(1f));
            Assert.That(state.VideoPlaying, Is.True);
        }

        [Test]
        public void Hover_FastKeyPassingKeepsOnlyTheLastCandidate()
        {
            var state = new ControlTerminalHoverState();
            foreach (var key in new[] { "A", "S", "D", "W" })
            {
                state.Enter(key);
                Run(state, 0.05f);
                state.Exit(key);
            }

            state.Enter("Space");
            Run(state, 0.16f);
            Assert.That(state.ActiveKey, Is.EqualTo("Space"));
        }

        [Test]
        public void Hover_LeavingAllKeysClosesAfterGraceThenRetractsLine()
        {
            var state = new ControlTerminalHoverState();
            state.Enter("A");
            Run(state, 0.6f);
            state.Exit("A");
            Run(state, 0.05f);
            Assert.That(state.IsClosing, Is.False);
            Assert.That(state.ScreenLevel, Is.EqualTo(1f));
            Run(state, 0.06f);
            Assert.That(state.IsClosing, Is.True);
            Run(state, 0.12f);
            Assert.That(state.ScreenLevel, Is.EqualTo(0f));
            Run(state, 0.2f);
            Assert.That(state.ActiveKey, Is.Null);
            Assert.That(state.LineProgress, Is.EqualTo(0f));
            Assert.That(state.IsIdle, Is.True);
        }

        [Test]
        public void Hover_ReenteringDuringGraceDoesNotClose()
        {
            var state = new ControlTerminalHoverState();
            state.Enter("A");
            Run(state, 0.6f);
            state.Exit("A");
            Run(state, 0.07f);
            state.Enter("A");
            Run(state, 0.3f);
            Assert.That(state.IsClosing, Is.False);
            Assert.That(state.ScreenLevel, Is.EqualTo(1f));
        }

        [Test]
        public void Hover_ResetClearsEverythingImmediately()
        {
            var state = new ControlTerminalHoverState();
            state.Enter("A");
            Run(state, 0.5f);
            state.Reset();
            Assert.That(state.IsIdle, Is.True);
            Assert.That(state.ScreenLevel, Is.EqualTo(0f));
            Assert.That(state.LineProgress, Is.EqualTo(0f));
            Assert.That(state.ContentAlpha, Is.EqualTo(1f));
            Assert.That(state.VideoPlaying, Is.False);
        }

        // ---------------------------------------------------------------- 자동 시연

        [Test]
        public void AutoDemo_PressesOneKeyAtATimeWithRests()
        {
            var demo = new ControlTerminalAutoDemo();
            demo.SetKeys(new[] { "A", "D" });
            var order = new List<string>();
            string last = null;
            for (float t = 0f; t < 12f; t += 0.02f)
            {
                demo.Tick(0.02f);
                var current = demo.CurrentKey;
                if (current != last && current != null) order.Add(current);
                last = current;
            }

            Assert.That(order.Count, Is.GreaterThanOrEqualTo(4));
            Assert.That(order[0], Is.EqualTo("A"));
            Assert.That(order[1], Is.EqualTo("D"));
            Assert.That(order[2], Is.EqualTo("A"));
        }

        [Test]
        public void AutoDemo_WaitsIdleFirstAndStopsWhileBlocked()
        {
            var demo = new ControlTerminalAutoDemo();
            demo.SetKeys(new[] { "A", "D" });
            demo.Tick(ControlTerminalAutoDemo.IdleSeconds * 0.5f);
            Assert.That(demo.CurrentKey, Is.Null);
            demo.Tick(ControlTerminalAutoDemo.IdleSeconds * 0.6f);
            Assert.That(demo.CurrentKey, Is.EqualTo("A"));
            demo.SetBlocked(true);
            Assert.That(demo.CurrentKey, Is.Null);
            demo.Tick(5f);
            Assert.That(demo.CurrentKey, Is.Null);
            demo.SetBlocked(false);
            demo.Tick(ControlTerminalAutoDemo.IdleSeconds * 0.5f);
            Assert.That(demo.CurrentKey, Is.Null, "해제 후 유휴 시간을 처음부터 다시 센다");
            demo.Tick(ControlTerminalAutoDemo.IdleSeconds * 0.6f);
            Assert.That(demo.CurrentKey, Is.EqualTo("A"));
        }

        // ---------------------------------------------------------------- 연결선

        [Test]
        public void Connector_BendsTwiceReachesTheScreenAndFollowsTheKey()
        {
            Assert.That(ControlTerminalLayout.TryGet("A", out var key), Is.True);
            var rest = ControlTerminalConnector.StartOf(key, 0f);
            var pressed = ControlTerminalConnector.StartOf(key, 2.5f);
            Assert.That(pressed.y, Is.EqualTo(rest.y - 2.5f).Within(0.001f));
            Assert.That(pressed.x, Is.EqualTo(rest.x));

            var points = new Vector2[4];
            ControlTerminalConnector.Build(rest, ControlTerminalLayout.ConnectorExitX, ControlTerminalLayout.ScreenLeftMiddle, points);
            Assert.That(points[3], Is.EqualTo(ControlTerminalLayout.ScreenLeftMiddle));
            Assert.That(points[1].y, Is.EqualTo(points[0].y));
            Assert.That(points[2].x, Is.EqualTo(points[1].x));
            Assert.That(points[3].y, Is.EqualTo(points[2].y));

            var starts = new Vector2[3];
            var ends = new Vector2[3];
            Assert.That(ControlTerminalConnector.Evaluate(points, 0f, starts, ends), Is.EqualTo(rest));
            Assert.That(ControlTerminalConnector.Evaluate(points, 1f, starts, ends), Is.EqualTo(points[3]));
            var half = ControlTerminalConnector.Evaluate(points, 0.5f, starts, ends);
            Assert.That(Vector2.Distance(rest, half), Is.GreaterThan(1f));
            Assert.That(half, Is.Not.EqualTo(points[3]));
        }

        [Test]
        public void Connector_HorizontalRunStaysInTheGapAboveEveryKey()
        {
            // 시작 높이는 윗면 위 4px 틈이라 같은 줄의 글자(중심)와 겹치지 않는다.
            var keys = ControlTerminalLayout.Keys;
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i].IsMouse) continue;
                var start = ControlTerminalConnector.StartOf(keys[i], 0f);
                Assert.That(start.y, Is.GreaterThan(keys[i].Top));
                for (int j = 0; j < keys.Count; j++)
                {
                    if (j == i || keys[j].Center.x < keys[i].Center.x) continue;
                    var rect = new Rect(keys[j].Center - keys[j].Size * 0.5f, keys[j].Size);
                    Assert.That(rect.Contains(new Vector2(keys[j].Center.x, start.y)), Is.False, keys[i].Id + " → " + keys[j].Id);
                    Assert.That(start.y < rect.yMin || start.y > rect.yMax, Is.True, keys[i].Id + " → " + keys[j].Id);
                }
            }
        }

        // ---------------------------------------------------------------- 시연 장면

        [Test]
        public void DemoClips_EveryFunctionHasASceneWithRealGameSprites()
        {
            var skin = Resources.Load<GameGuideSkin>(GameGuideSkin.ResourcePath);
            Assert.That(skin, Is.Not.Null);
            foreach (ControlKeyFunction function in System.Enum.GetValues(typeof(ControlKeyFunction)))
            {
                if (function == ControlKeyFunction.None)
                {
                    Assert.That(ControlTerminalDemoClips.Get(function), Is.Null);
                    continue;
                }

                var clip = ControlTerminalDemoClips.Get(function);
                Assert.That(clip, Is.Not.Null, function.ToString());
                Assert.That(clip.Duration, Is.InRange(1f, 4f), function.ToString());
                Assert.That(ControlTerminalDemoClips.Get(function), Is.SameAs(clip), "같은 기능은 장면을 재사용한다");
                foreach (var key in clip.UsedSpriteKeys())
                {
                    if (key.StartsWith("art.")) Assert.That(GuideSprites.Art(key), Is.Not.Null, key);
                    else Assert.That(skin.Get(key), Is.Not.Null, function + ": " + key);
                }
            }
        }

        // ---------------------------------------------------------------- 패널 배선

        private ControlSchemePanel CreatePanel(out GameObject root)
        {
            root = new GameObject("Settings", typeof(RectTransform), typeof(Canvas));
            spawned.Add(root);
            ((RectTransform)root.transform).sizeDelta = new Vector2(1920, 1080);
            return ControlSchemePanel.Attach(root);
        }

        private static void Run(ControlTerminalView view, float seconds)
        {
            for (float t = 0f; t < seconds - 0.0001f; t += 0.01f) view.Tick(0.01f);
        }

        [Test]
        public void Art_GeneratedFrameAndButtonSpritesAreSlicedAssets()
        {
            foreach (var name in new[] { "control-frame", "control-button-normal", "control-button-hover", "control-arrow-normal", "control-arrow-hover" })
            {
                var sprite = Resources.Load<Sprite>("UI/ControlTerminal/" + name);
                Assert.That(sprite != null, Is.True, name);
                Assert.That(sprite.border.x, Is.GreaterThan(0f), name);
                Assert.That(sprite.border.y, Is.GreaterThan(0f), name);
            }
        }

        [Test]
        public void Panel_IntroOpensHoloFrameThenKeysPopOutAndSettleExactly()
        {
            var panel = CreatePanel(out _);
            panel.SetDraft(ControlScheme.Classic);
            var terminal = panel.Terminal;
            terminal.PlayIntro();
            var back = panel.transform.Find("ControlSchemeOverlay/KeyboardCard/HoloBack");
            var front = panel.transform.Find("ControlSchemeOverlay/KeyboardCard/HoloFront");
            Assert.That(back != null && back.gameObject.activeSelf, Is.True, "열리는 프레임 연출이 먼저 보인다");
            Assert.That(front != null && front.gameObject.activeSelf, Is.True);
            Assert.That(front.GetSiblingIndex(), Is.EqualTo(front.parent.childCount - 1), "빛 연출은 맨 위에 그린다");
            foreach (var cap in terminal.Caps) Assert.That(cap.PopScale, Is.EqualTo(0f), cap.Id);

            // 프레임이 다 나타나기 전에는 키캡이 없다.
            Run(terminal, ControlTerminalTimeline.KeyPopStart - 0.03f);
            foreach (var cap in terminal.Caps) Assert.That(cap.PopScale, Is.EqualTo(0f), cap.Id);

            // 이후 키캡이 기준 크기보다 크게 튀어나왔다가
            bool bigger = false;
            for (int i = 0; i < 60; i++)
            {
                terminal.Tick(0.01f);
                if (terminal.GetCap("W").PopScale > 1.2f) bigger = true;
            }

            Assert.That(bigger, Is.True);
            Run(terminal, ControlTerminalTimeline.IntroDuration);
            Assert.That(back.gameObject.activeSelf || front.gameObject.activeSelf, Is.False, "완성된 프레임이 이어받는다");
            Assert.That(terminal.CurrentPhase, Is.EqualTo(ControlTerminalView.Phase.Idle));
            foreach (var cap in terminal.Caps)
            {
                Assert.That(cap.IsPopSettled, Is.True, cap.Id);
                Assert.That(cap.PopScale, Is.EqualTo(1f), cap.Id);
            }
        }

        [Test]
        public void Panel_BuildsKeysOnceAndOnlyFunctionKeysTakeHover()
        {
            var panel = CreatePanel(out _);
            var terminal = panel.Terminal;
            Assert.That(terminal, Is.Not.Null);
            Assert.That(terminal.Caps.Count, Is.EqualTo(ControlTerminalLayout.Keys.Count));
            panel.SetDraft(ControlScheme.Classic);
            Assert.That(terminal.GetCap("W").RaycastEnabled, Is.True);
            Assert.That(terminal.GetCap("Q").RaycastEnabled, Is.False);
            Assert.That(terminal.GetCap("Space").Role, Is.EqualTo(ControlKeyRole.Jump));

            var first = terminal.GetCap("W");
            panel.SetDraft(ControlScheme.ArrowsMove);
            Assert.That(terminal.GetCap("W"), Is.SameAs(first), "배열을 다시 만들지 않는다");
            Assert.That(first.Function, Is.EqualTo(ControlKeyFunction.MineUp));
            Assert.That(first.RoleText, Is.EqualTo("↑ 채굴"));
            Assert.That(first.Role, Is.EqualTo(ControlKeyRole.Mine));
            Assert.That(terminal.TitleText, Is.EqualTo("키 조작 방식 3 / 3"));
            Assert.That(terminal.DescriptionText, Does.StartWith("방향키: 이동"));
        }

        [Test]
        public void Panel_HoverPressesImmediatelyThenShowsLineAndSingleScreenAfterDwell()
        {
            var panel = CreatePanel(out _);
            panel.Open();
            var terminal = panel.Terminal;
            Assert.That(terminal.IsHidden, Is.False);
            var cap = terminal.GetCap("D");
            var rectBefore = cap.HitRect.sizeDelta;
            var positionBefore = cap.HitRect.anchoredPosition;
            cap.OnPointerEnter(null);
            Run(terminal, 0.09f);
            Assert.That(cap.Level, Is.EqualTo(1f).Within(0.001f), "호버 즉시 눌린다");
            Assert.That(terminal.HoverState.ActiveKey, Is.Null);
            Run(terminal, 0.2f);
            Assert.That(terminal.HoverState.ActiveKey, Is.EqualTo("D"));
            Assert.That(terminal.BoundKey, Is.EqualTo("D"));
            Run(terminal, 0.5f);
            Assert.That(terminal.ScreenVisible, Is.True);
            Assert.That(terminal.FunctionCaptionText, Is.EqualTo("오른쪽 이동"));
            Assert.That(terminal.Stage.IsPlaying, Is.True);
            Assert.That(panel.GetComponentsInChildren<GameGuideStageView>(true).Length, Is.EqualTo(1), "시연 화면은 하나만 쓴다");
            // 판정 Rect는 눌림과 무관하게 고정이다.
            Assert.That(cap.HitRect.sizeDelta, Is.EqualTo(rectBefore));
            Assert.That(cap.HitRect.anchoredPosition, Is.EqualTo(positionBefore));
            Assert.That(cap.TopRect.anchoredPosition.y, Is.EqualTo(-ControlTerminalLayout.PressTravel).Within(0.01f));

            cap.OnPointerExit(null);
            Run(terminal, 0.2f);
            Assert.That(cap.Level, Is.EqualTo(0f));
            Assert.That(cap.TopRect.anchoredPosition.y, Is.EqualTo(0f));
            Run(terminal, 0.6f);
            Assert.That(terminal.HoverState.ActiveKey, Is.Null);
            Assert.That(terminal.ScreenVisible, Is.False);
            Assert.That(terminal.BoundKey, Is.Null);
        }

        [Test]
        public void Panel_HoverDoesNotChangeSelectionOrSavedScheme()
        {
            PlayerPrefs.SetInt(SettingsRuntimeApplier.PrefControls, 1);
            ControlPreferences.Scheme = ControlScheme.WasdMove;
            var panel = CreatePanel(out _);
            panel.SetDraft(ControlScheme.WasdMove);
            panel.Open();
            var terminal = panel.Terminal;
            terminal.GetCap("↑").OnPointerEnter(null);
            Run(terminal, 0.5f);
            terminal.GetCap("Space").OnPointerEnter(null);
            Run(terminal, 0.5f);
            Assert.That(panel.Selected, Is.EqualTo(ControlScheme.WasdMove));
            Assert.That(ControlPreferences.Scheme, Is.EqualTo(ControlScheme.WasdMove));
            Assert.That(SettingsRuntimeApplier.LoadControlScheme(), Is.EqualTo(ControlScheme.WasdMove));
        }

        [Test]
        public void Panel_PageChangeClearsHoverLineScreenAndStaleVideo()
        {
            var panel = CreatePanel(out _);
            panel.SetDraft(ControlScheme.Classic);
            panel.Open();
            var terminal = panel.Terminal;
            terminal.GetCap("A").OnPointerEnter(null);
            Run(terminal, 0.6f);
            Assert.That(terminal.ScreenVisible, Is.True);
            Assert.That(terminal.BoundKey, Is.EqualTo("A"));
            panel.Cycle(1);
            Assert.That(panel.Selected, Is.EqualTo(ControlScheme.WasdMove));
            Assert.That(terminal.HoverState.IsIdle, Is.True);
            Assert.That(terminal.BoundKey, Is.Null);
            Assert.That(terminal.ScreenVisible, Is.False);
            Assert.That(terminal.Stage.Clip, Is.Null);
            Assert.That(terminal.GetCap("A").IsHovered, Is.False);
            Assert.That(SettingsRuntimeApplier.LoadControlScheme(), Is.EqualTo(ControlScheme.WasdMove), "변경 즉시 저장");
        }

        [Test]
        public void Panel_RapidCyclingShowsTheLastSelectedScheme()
        {
            var panel = CreatePanel(out _);
            panel.SetDraft(ControlScheme.Classic);
            panel.Open();
            for (int i = 0; i < 7; i++) panel.Cycle(1);
            Assert.That(panel.Selected, Is.EqualTo(ControlScheme.WasdMove));
            Assert.That(panel.Terminal.TitleText, Is.EqualTo("키 조작 방식 2 / 3"));
            Assert.That(panel.Terminal.GetCap("A").Function, Is.EqualTo(ControlKeyFunction.MoveLeft));
            Assert.That(panel.Terminal.GetCap("←").Function, Is.EqualTo(ControlKeyFunction.MineLeft));
            panel.Cycle(-1);
            panel.Cycle(-1);
            Assert.That(panel.Selected, Is.EqualTo(ControlScheme.ArrowsMove));
            Assert.That(panel.Terminal.GetCap("←").Function, Is.EqualTo(ControlKeyFunction.MoveLeft));
        }

        [TestCase("CloseControls")]
        [TestCase("CloseControlsX")]
        public void Panel_CloseSavesAtOnceAndReopenStartsClean(string buttonName)
        {
            var panel = CreatePanel(out var root);
            panel.SetDraft(ControlScheme.ArrowsMove);
            panel.Open();
            var terminal = panel.Terminal;
            terminal.GetCap("←").OnPointerEnter(null);
            Run(terminal, 0.5f);
            root.transform.Find("ControlSchemeOverlay/KeyboardCard/Chrome/" + buttonName).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(panel.Close(), Is.False);
            Assert.That(SettingsRuntimeApplier.LoadControlScheme(), Is.EqualTo(ControlScheme.ArrowsMove));
            Assert.That(terminal.HoverState.IsIdle, Is.True);
            Assert.That(terminal.ScreenVisible, Is.False);
            Assert.That(terminal.Stage.Clip, Is.Null);
            Assert.That(terminal.GetCap("←").Level, Is.EqualTo(0f));

            panel.Open();
            Assert.That(panel.IsOpen, Is.True);
            Assert.That(terminal.HoverState.IsIdle, Is.True);
            Assert.That(terminal.BoundKey, Is.Null);
            Assert.That(root.transform.Find("ControlSchemeOverlay").gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Panel_KeepsCardInsideSmallOverlaysAndNeverEnlarges()
        {
            var panel = CreatePanel(out var root);
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(1280, 720);
            var overlay = (RectTransform)root.transform.Find("ControlSchemeOverlay");
            overlay.anchorMin = overlay.anchorMax = new Vector2(0.5f, 0.5f);
            overlay.sizeDelta = new Vector2(1280, 720);
            panel.Open();
            var card = (RectTransform)overlay.Find("KeyboardCard");
            Assert.That(card.localScale.x, Is.LessThanOrEqualTo(1f));
            Assert.That(card.rect.width * card.localScale.x, Is.LessThanOrEqualTo(1280f));
            Assert.That(card.rect.height * card.localScale.y, Is.LessThanOrEqualTo(720f));
            overlay.sizeDelta = new Vector2(3840, 2160);
            panel.Open();
            Assert.That(card.localScale.x, Is.EqualTo(1f));
        }

        [Test]
        public void Panel_DecorationsNeverInterceptRaycasts()
        {
            var panel = CreatePanel(out var root);
            panel.Open();
            var card = root.transform.Find("ControlSchemeOverlay/KeyboardCard");
            var allowed = new HashSet<string> { "KeyboardCard", "PreviousScheme", "NextScheme", "CloseControls", "CloseControlsX" };
            foreach (var graphic in card.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.raycastTarget) continue;
                bool isKey = graphic.GetComponent<ControlTerminalKeyCap>() != null;
                bool isButton = allowed.Contains(graphic.gameObject.name);
                Assert.That(isKey || isButton, Is.True, "raycast on " + graphic.transform.name);
            }

            var stage = panel.Terminal.Stage;
            foreach (var graphic in stage.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.False);
        }
    }
}
