using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SubTerra.App.Drone;
using SubTerra.App.UI.Drone;
using SubTerra.Shared;
using TMPro;
using UnityEngine;

namespace SubTerra.App.Tests.Drone
{
    /// <summary>prompt-B 149: 드론 분석 창 터미널 연출과 점수 표시 제거.</summary>
    public sealed class DroneTerminalFxTests
    {
        private const float Step = 1f / 120f;

        [Test]
        public void B149_Open_LineThenExpand_WithinAboutPointThreeSeconds()
        {
            var settings = new DroneTerminalFxSettings();
            Assert.That(settings.openDuration, Is.EqualTo(0.3f).Within(0.05f));

            var start = DroneTerminalTimeline.Open(0f, settings);
            Assert.That(start.Open, Is.EqualTo(0f));
            Assert.That(start.LineWidth, Is.EqualTo(0f));

            // 가로선이 먼저 다 펼쳐진 뒤에 세로로 열린다.
            var lineDone = DroneTerminalTimeline.Open(settings.openDuration * 0.3f, settings);
            Assert.That(lineDone.LineWidth, Is.EqualTo(1f).Within(0.001f));
            Assert.That(lineDone.Open, Is.EqualTo(0f).Within(0.001f));
            Assert.That(lineDone.LineAlpha, Is.GreaterThan(0.5f));

            var opened = DroneTerminalTimeline.Open(settings.openDuration, settings);
            Assert.That(opened.Open, Is.EqualTo(1f).Within(0.001f));

            // 펼쳐진 직후 스캔라인이 위에서 아래로 한 번 지나간다.
            var midScan = DroneTerminalTimeline.Open(settings.openDuration + settings.scanSweepDuration * 0.5f, settings);
            Assert.That(midScan.ScanAlpha, Is.GreaterThan(0.5f));
            Assert.That(midScan.ScanPosition, Is.EqualTo(0.5f).Within(0.01f));
            var afterScan = DroneTerminalTimeline.Open(settings.openDuration + settings.scanSweepDuration + 0.01f, settings);
            Assert.That(afterScan.ScanAlpha, Is.EqualTo(0f));
            Assert.That(afterScan.LineAlpha, Is.EqualTo(0f));
        }

        [Test]
        public void B149_WindowState_OpenTypingClose_OrderAndDurations()
        {
            var settings = new DroneTerminalFxSettings();
            var window = new DroneTerminalWindowState(settings);
            window.Open();
            Assert.That(window.Phase, Is.EqualTo(DroneTerminalPhase.Opening));

            var openTime = Advance(window, DroneTerminalPhase.Opening);
            Assert.That(window.Phase, Is.EqualTo(DroneTerminalPhase.Shown));
            Assert.That(openTime, Is.EqualTo(0.3f).Within(0.02f));

            // 글자는 열기가 거의 끝날 때 시작해 1초 안에 모두 나온다.
            var plan = DroneTerminalTypingPlan.ForOpen(settings, 40, 14, 3);
            Assert.That(plan.HeaderStart, Is.LessThan(settings.openDuration));
            Assert.That(plan.HeaderEnd, Is.LessThanOrEqualTo(plan.DialogueStart));
            Assert.That(plan.DialogueEnd, Is.LessThanOrEqualTo(plan.ActionStart));
            Assert.That(plan.ActionEnd, Is.LessThanOrEqualTo(plan.ReasonStart));
            Assert.That(plan.End, Is.LessThanOrEqualTo(1f));
            Assert.That(plan.ReasonInterval, Is.InRange(0.05f, 0.1f));

            window.Close();
            Assert.That(window.Phase, Is.EqualTo(DroneTerminalPhase.Closing));
            var closeTime = 0f;
            var closed = false;
            while (!closed && closeTime < 2f)
            {
                closed = window.Tick(Step);
                closeTime += Step;
            }

            Assert.That(closed, Is.True);
            Assert.That(window.Phase, Is.EqualTo(DroneTerminalPhase.Hidden));
            Assert.That(closeTime, Is.EqualTo(0.25f).Within(0.02f));
        }

        [Test]
        public void B149_Close_FoldsContentThenCollapsesToLine()
        {
            var settings = new DroneTerminalFxSettings();
            var c = settings.closeDuration;
            var start = DroneTerminalTimeline.Close(0f, settings);
            Assert.That(start.Open, Is.EqualTo(1f));
            Assert.That(start.ContentAlpha, Is.EqualTo(1f));

            var folded = DroneTerminalTimeline.Close(c * 0.7f, settings);
            Assert.That(folded.ContentAlpha, Is.EqualTo(0f).Within(0.001f));
            Assert.That(folded.Open, Is.EqualTo(0f).Within(0.001f));
            Assert.That(folded.LineWidth, Is.EqualTo(1f).Within(0.001f));
            Assert.That(folded.LineAlpha, Is.GreaterThan(0.5f));

            var end = DroneTerminalTimeline.Close(c, settings);
            Assert.That(end.LineWidth, Is.EqualTo(0f).Within(0.001f));
            Assert.That(end.LineAlpha, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void B149_TabDuringOpen_ReversesIntoCloseWithoutJump()
        {
            var settings = new DroneTerminalFxSettings();
            var window = new DroneTerminalWindowState(settings);
            window.Open();
            window.Tick(settings.openDuration * 0.5f);
            var before = window.Frame().Open;

            window.Close();
            Assert.That(window.Phase, Is.EqualTo(DroneTerminalPhase.Closing));
            var after = window.Frame().Open;
            Assert.That(after, Is.LessThanOrEqualTo(before + 0.001f));

            // 남은 시간은 닫기 전체보다 짧다.
            var remaining = 0f;
            while (window.Phase != DroneTerminalPhase.Hidden && remaining < 1f)
            {
                window.Tick(Step);
                remaining += Step;
            }

            Assert.That(remaining, Is.LessThan(settings.closeDuration));

            // 닫히는 중 다시 열면 숨김을 거치지 않고 열기로 돌아간다.
            window.Open();
            window.Tick(settings.openDuration);
            window.Close();
            window.Tick(settings.closeDuration * 0.4f);
            window.Open();
            Assert.That(window.Phase, Is.EqualTo(DroneTerminalPhase.Opening));
            Assert.That(window.Clock, Is.GreaterThan(0f));
        }

        [Test]
        public void B149_RapidToggle_NeverLeavesInvalidState()
        {
            var settings = new DroneTerminalFxSettings();
            var window = new DroneTerminalWindowState(settings);
            for (var i = 0; i < 40; i++)
            {
                if (i % 2 == 0)
                {
                    window.Open();
                }
                else
                {
                    window.Close();
                }

                window.Tick(0.03f);
                var frame = window.Frame();
                Assert.That(frame.Open, Is.InRange(0f, 1f));
                Assert.That(frame.LineWidth, Is.InRange(0f, 1f));
            }

            window.Close();
            window.Tick(1f);
            Assert.That(window.Phase, Is.EqualTo(DroneTerminalPhase.Hidden));
        }

        [Test]
        public void B149_Typing_LongTextStillFinishesWithinBudget()
        {
            var settings = new DroneTerminalFxSettings();
            var plan = DroneTerminalTypingPlan.ForOpen(settings, 400, 60, 8);
            Assert.That(plan.End, Is.LessThanOrEqualTo(settings.revealBudget + 0.001f));
            Assert.That(settings.revealBudget, Is.LessThanOrEqualTo(1f));
            Assert.That(plan.VisibleDialogueCharacters(plan.End, 400), Is.EqualTo(400));

            var retype = DroneTerminalTypingPlan.ForRetype(settings, 400, 60, 8);
            Assert.That(retype.IncludesHeader, Is.False);
            Assert.That(retype.DialogueStart, Is.EqualTo(0f));
            Assert.That(retype.End, Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void B149_Typing_HeaderDialogueActionThenReasonsOneByOne()
        {
            var settings = new DroneTerminalFxSettings();
            var plan = DroneTerminalTypingPlan.ForOpen(settings, 30, 12, 3);
            var header = DroneDialoguePanelView.HeaderPrefix.Length;

            Assert.That(plan.VisibleHeaderCharacters(plan.HeaderStart - 0.01f, header), Is.EqualTo(0));
            var midHeader = (plan.HeaderStart + plan.HeaderEnd) * 0.5f;
            Assert.That(plan.VisibleHeaderCharacters(midHeader, header), Is.InRange(1, header - 1));
            Assert.That(plan.VisibleDialogueCharacters(midHeader, 30), Is.EqualTo(0));

            var midDialogue = (plan.DialogueStart + plan.DialogueEnd) * 0.5f;
            Assert.That(plan.VisibleHeaderCharacters(midDialogue, header), Is.EqualTo(header));
            Assert.That(plan.VisibleDialogueCharacters(midDialogue, 30), Is.InRange(1, 29));
            Assert.That(plan.VisibleActionCharacters(midDialogue, 12), Is.EqualTo(0));

            Assert.That(plan.VisibleReasonLines(plan.ActionEnd - 0.001f), Is.EqualTo(0));
            Assert.That(plan.VisibleReasonLines(plan.ReasonStart), Is.EqualTo(1));
            Assert.That(plan.VisibleReasonLines(plan.ReasonStart + plan.ReasonInterval), Is.EqualTo(2));
            Assert.That(plan.VisibleReasonLines(plan.End), Is.EqualTo(3));
            Assert.That(plan.IsComplete(plan.End), Is.True);
        }

        [Test]
        public void B149_Skip_JumpsToEndStateImmediately()
        {
            var settings = new DroneTerminalFxSettings();
            var window = new DroneTerminalWindowState(settings);
            window.Open();
            window.Tick(0.05f);

            window.SkipToShown();
            Assert.That(window.Phase, Is.EqualTo(DroneTerminalPhase.Shown));
            var frame = window.Frame();
            Assert.That(frame.Open, Is.EqualTo(1f));
            Assert.That(frame.ScanAlpha, Is.EqualTo(0f));
            Assert.That(frame.LineAlpha, Is.EqualTo(0f));

            // 닫는 중에는 건너뛰기가 열기로 되돌리지 않는다.
            window.Close();
            window.SkipToShown();
            Assert.That(window.Phase, Is.EqualTo(DroneTerminalPhase.Closing));
        }

        [Test]
        public void B149_DisplayText_HasNoScore_AndReasonsCarryValues()
        {
            var settings = ScriptableObject.CreateInstance<DroneAnalysisSettings>();
            try
            {
                settings.EditorSetDefaults();
                var context = SafeContext();
                context.currentEnergy = 10;
                context.returnEnergyEstimate = 12;
                context.unsettledCargoValue = 340;
                context.cargoWeight = 50f;
                context.maxCargoWeight = 50f;
                var analysis = new DroneAnalysisService(settings).Analyze(context);
                Assert.That(analysis.RecommendedAction, Is.EqualTo(DroneAction.ReturnToBase));
                Assert.That(analysis.Recommendation.Score, Is.EqualTo(80));

                var action = DroneReasonDisplayFormatter.FormatAction(analysis.RecommendedAction);
                Assert.That(action, Is.EqualTo("AI 추천 // 기지로 귀환"));
                Assert.That(Regex.IsMatch(action, @"\[\d+\]"), Is.False);

                var lines = DroneReasonDisplayFormatter.FormatReasonLines(analysis.Recommendation.Reasons);
                Assert.That(lines.Count, Is.EqualTo(3));
                var joined = DroneReasonDisplayFormatter.JoinLines(lines);
                Assert.That(joined, Does.Not.Contain("(+"));
                Assert.That(joined, Does.Not.Contain("80"));
                Assert.That(lines[0], Is.EqualTo("[01] 현재 전력 10, 귀환 예상 12"));
                Assert.That(lines[1], Is.EqualTo("[02] 미정산 가치 340 · 340G"));
                Assert.That(lines[2], Is.EqualTo("[03] 화물 50/50 · 50kg"));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void B149_ValueFormat_PerUnitRules()
        {
            Assert.That(Value(0.724, "risk"), Is.EqualTo("72%"));
            Assert.That(Value(0.456, "ratio"), Is.EqualTo("무결성 46%"));
            Assert.That(Value(12.5, "gold"), Is.EqualTo("13G"));
            Assert.That(Value(30.26, "weight"), Is.EqualTo("30.3kg"));
            Assert.That(Value(30, "weight"), Is.EqualTo("30kg"));
            // 문구에 같은 값이 이미 있거나 의미 없는 값은 생략한다.
            Assert.That(Value(18, "energy"), Is.Empty);
            Assert.That(Value(12.5, "m"), Is.Empty);
            Assert.That(Value(30, "depth"), Is.Empty);
            Assert.That(Value(1, "detected"), Is.Empty);
            Assert.That(Value(double.NaN, string.Empty), Is.Empty);

            var gas = new DroneReason("gas_risk", "가스 위험 0.72", 0.72, "risk", 50);
            Assert.That(DroneReasonDisplayFormatter.FormatReasonLine(0, gas), Is.EqualTo("[01] 가스 위험 0.72 · 72%"));
            var plain = new DroneReason("context_incomplete", "핵심 안전 상태가 불완전합니다.", double.NaN, string.Empty, 0);
            Assert.That(DroneReasonDisplayFormatter.FormatReasonLine(1, plain), Is.EqualTo("[02] 핵심 안전 상태가 불완전합니다."));

            var empty = DroneReasonDisplayFormatter.FormatReasonLines(new List<DroneReason>());
            Assert.That(empty, Is.EqualTo(new[] { "추가 위험 근거 없음" }));
        }

        [Test]
        public void B149_PanelView_ShowsActionAndReasonsWithoutScore()
        {
            var root = new GameObject("DroneDialoguePanel", typeof(RectTransform));
            var settings = ScriptableObject.CreateInstance<DroneAnalysisSettings>();
            try
            {
                settings.EditorSetDefaults();
                var panel = new GameObject("PanelRoot", typeof(RectTransform));
                panel.transform.SetParent(root.transform, false);
                var dialogue = CreateText(panel.transform, "DialogueText");
                var action = CreateText(panel.transform, "ActionText");
                var reason = CreateText(panel.transform, "ReasonText");
                var view = root.AddComponent<DroneDialoguePanelView>();
                SetField(view, "panelRoot", panel);
                SetField(view, "dialogueText", dialogue);
                SetField(view, "actionText", action);
                SetField(view, "reasonText", reason);

                var context = SafeContext();
                context.gasRisk = 0.72f;
                view.SetAnalysis(new DroneAnalysisService(settings).Analyze(context));

                Assert.That(action.text, Is.EqualTo("AI 추천 // 가스 구역 이탈"));
                Assert.That(reason.text, Is.EqualTo("[01] 가스 위험 0.72 · 72%"));
                Assert.That(reason.text, Does.Not.Contain("(+"));

                // EditMode(재생 아님)에서는 연출 없이 즉시 표시·숨김한다.
                view.SetVisible(true);
                Assert.That(panel.activeSelf, Is.True);
                Assert.That(view.TerminalPhase, Is.EqualTo(DroneTerminalPhase.Hidden));
                Assert.That(view.TryBeginAnimatedClose(null), Is.False);
                view.SetVisible(false);
                Assert.That(panel.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void B149_ChangeDetector_RetypesOnlyWhenRecommendationChanges()
        {
            var detector = new DroneTerminalChangeDetector();
            var first = detector.ObserveAnalysis(DroneAction.ContinueDescending, 1);
            Assert.That(first.RecommendationChanged, Is.True);

            var same = detector.ObserveAnalysis(DroneAction.ContinueDescending, 1);
            Assert.That(same.RecommendationChanged, Is.False);
            Assert.That(same.ReasonLinesAdded, Is.False);

            var added = detector.ObserveAnalysis(DroneAction.ContinueDescending, 3);
            Assert.That(added.RecommendationChanged, Is.False);
            Assert.That(added.ReasonLinesAdded, Is.True);
            Assert.That(added.PreviousReasonLineCount, Is.EqualTo(1));

            var fewer = detector.ObserveAnalysis(DroneAction.ContinueDescending, 2);
            Assert.That(fewer.RecommendationChanged, Is.False);
            Assert.That(fewer.ReasonLinesAdded, Is.False);

            var changed = detector.ObserveAnalysis(DroneAction.LeaveGasZone, 2);
            Assert.That(changed.RecommendationChanged, Is.True);
            Assert.That(changed.ReasonLinesAdded, Is.False);
        }

        [Test]
        public void B149_AppendedReasons_OnlyNewLinesAppearInOrder()
        {
            Assert.That(DroneTerminalTimeline.AppendedReasonLines(1, 3, 0f, 0.07f), Is.EqualTo(2));
            Assert.That(DroneTerminalTimeline.AppendedReasonLines(1, 3, 0.07f, 0.07f), Is.EqualTo(3));
            Assert.That(DroneTerminalTimeline.AppendedReasonLines(1, 3, 5f, 0.07f), Is.EqualTo(3));
            // 줄어들면 바로 줄인다.
            Assert.That(DroneTerminalTimeline.AppendedReasonLines(3, 1, 0f, 0.07f), Is.EqualTo(1));
        }

        [Test]
        public void B149_PriorityEntry_FlashesTwiceAndGlitchesOnce()
        {
            var detector = new DroneTerminalChangeDetector();
            Assert.That(detector.ObserveState(DroneOperationalState.Idle), Is.False);
            Assert.That(detector.ObserveState(DroneOperationalState.Priority), Is.True);
            Assert.That(detector.ObserveState(DroneOperationalState.Priority), Is.False);
            Assert.That(detector.ObserveState(DroneOperationalState.Priority), Is.False);
            Assert.That(detector.ObserveState(DroneOperationalState.Scanning), Is.False);
            Assert.That(detector.ObserveState(DroneOperationalState.Priority), Is.True);

            var settings = new DroneTerminalFxSettings();
            var duration = settings.priorityFlashDuration;
            Assert.That(duration, Is.EqualTo(0.3f).Within(0.05f));
            var pulses = 0;
            var wasOn = false;
            var glitchFrames = 0;
            for (var t = 0f; t < duration + 0.2f; t += Step)
            {
                var on = DroneTerminalTimeline.PriorityFlash(t, duration) > 0.9f;
                if (on && !wasOn)
                {
                    pulses++;
                }

                wasOn = on;
                if (DroneTerminalTimeline.Glitch(t, duration).Active)
                {
                    glitchFrames++;
                    Assert.That(t, Is.LessThan(duration));
                }
            }

            Assert.That(pulses, Is.EqualTo(2));
            Assert.That(glitchFrames, Is.GreaterThan(0));
            Assert.That(DroneTerminalTimeline.PriorityFlash(duration, duration), Is.EqualTo(0f));
            Assert.That(DroneTerminalTimeline.Glitch(duration, duration).Active, Is.False);

            // 글리치 띠는 위에서 아래로 한 번 지나간다.
            var early = DroneTerminalTimeline.Glitch(duration * 0.2f, duration).BandPosition;
            var late = DroneTerminalTimeline.Glitch(duration * 0.8f, duration).BandPosition;
            Assert.That(late, Is.GreaterThan(early));
        }

        [Test]
        public void B149_Cursor_BlinksAfterHeaderThenStaysOn()
        {
            var settings = new DroneTerminalFxSettings();
            const float headerEnd = 0.36f;
            const float revealEnd = 0.9f;
            Assert.That(DroneTerminalTimeline.CursorVisible(headerEnd - 0.01f, headerEnd, revealEnd, settings), Is.False);
            Assert.That(DroneTerminalTimeline.CursorVisible(headerEnd, headerEnd, revealEnd, settings), Is.True);
            Assert.That(
                DroneTerminalTimeline.CursorVisible(headerEnd + settings.cursorBlinkPeriod * 0.75f, headerEnd, revealEnd, settings),
                Is.False);
            var settled = revealEnd + settings.cursorBlinkPeriod * settings.cursorBlinkCount + 0.01f;
            Assert.That(DroneTerminalTimeline.CursorVisible(settled, headerEnd, revealEnd, settings), Is.True);
            Assert.That(DroneTerminalTimeline.CursorVisible(settled + 0.25f, headerEnd, revealEnd, settings), Is.True);
        }

        private static float Advance(DroneTerminalWindowState window, DroneTerminalPhase phase)
        {
            var elapsed = 0f;
            while (window.Phase == phase && elapsed < 2f)
            {
                window.Tick(Step);
                elapsed += Step;
            }

            return elapsed;
        }

        private static string Value(double value, string unit)
        {
            return DroneReasonDisplayFormatter.FormatValue(new DroneReason("test", "msg", value, unit, 0));
        }

        private static TMP_Text CreateText(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<TextMeshProUGUI>();
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(
                name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static DroneContextDto SafeContext()
        {
            return new DroneContextDto
            {
                depth = 10,
                currentEnergy = 100,
                returnEnergyEstimate = 20,
                structuralIntegrity = 1f,
                gasRisk = 0f,
                unsettledCargoValue = 0,
                cargoWeight = 0f,
                maxCargoWeight = 50f,
                nearestBaseDistance = 20f,
                nearbyMineralIds = new List<string>(),
                returnPathAvailable = true
            };
        }
    }
}
