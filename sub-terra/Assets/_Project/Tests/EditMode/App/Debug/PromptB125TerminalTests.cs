using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
using SubTerra.App.UI.Tutorial;
using SubTerra.Gameplay.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Debug
{
    public sealed class PromptB125TerminalTests
    {
        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            UiPauseGate.Release(SaveRuntimeController.DebugTerminalPauseOwner);
            if (Time.timeScale <= 0f)
            {
                Time.timeScale = 1f;
            }

            GameBootstrapper.ResetInstanceForTests();
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(created[i]);
                }
            }

            created.Clear();
        }

        [Test]
        public void Gold_AddsAndClampsAtZero()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var state = GameState.CreateNew();
            var context = Context(state, null, null, null);

            Assert.That(session.Execute("gold 100", context), Is.EqualTo("gold 0 -> 100"));
            Assert.That(state.Player.Gold, Is.EqualTo(100));
            Assert.That(session.Execute("골드 -1000", context), Is.EqualTo("gold 100 -> 0"));
            Assert.That(state.Player.Gold, Is.EqualTo(0));
            Assert.That(session.Execute("GOLD", context), Does.Contain("인자 오류"));
            Assert.That(state.Player.Gold, Is.EqualTo(0));
        }

        [Test]
        public void Cargo_AddsPartial_AndClampsRemoval()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var catalog = Catalog();
            var inventory = new InventoryService(catalog, 3f);
            var context = Context(null, inventory, null, null);

            var added = session.Execute("copper 5", context);
            Assert.That(added, Is.EqualTo("copper 담음 3, 버림 2, 보유 3"));
            Assert.That(inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(3));

            var wide = new InventoryService(catalog, 100f);
            wide.TryAddMineral(DataIds.Minerals.Iron, 10);
            var wideContext = Context(null, wide, null, null);
            Assert.That(session.Execute("철 -20", wideContext), Is.EqualTo("iron 차감 10, 보유 0"));
            Assert.That(wide.State.GetQuantity(DataIds.Minerals.Iron), Is.EqualTo(0));

            Assert.That(session.Execute("lithium 2", wideContext), Does.Contain("담음 2"));
            Assert.That(wide.State.GetQuantity(DataIds.Minerals.Lithium), Is.EqualTo(2));
            Assert.That(
                session.Execute("engine_fuel 1", wideContext),
                Does.Contain("담음 1"));
            Assert.That(wide.State.GetQuantity(DataIds.RareItems.EngineFuel), Is.EqualTo(1));
            Assert.That(
                session.Execute(DataIds.RareItems.EngineFuel + " 2", wideContext),
                Does.Contain("담음 2"));
            Assert.That(wide.State.GetQuantity(DataIds.RareItems.EngineFuel), Is.EqualTo(3));
        }

        [Test]
        public void Cargo_UnknownIdOrBadArgs_DoNotChangeStacks()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(DataIds.Minerals.Iron, 1f, 1, "Iron");
            var inventory = new InventoryService(catalog, 100f);
            inventory.TryAddMineral(DataIds.Minerals.Iron, 4);
            var context = Context(null, inventory, null, null);

            Assert.That(session.Execute("copper 1", context), Does.Contain("알 수 없는 ID"));
            Assert.That(inventory.State.GetQuantity(DataIds.Minerals.Iron), Is.EqualTo(4));
            Assert.That(session.Execute("iron 0", context), Does.Contain("인자 오류"));
            Assert.That(session.Execute("iron", context), Does.Contain("인자 오류"));
            Assert.That(inventory.State.GetQuantity(DataIds.Minerals.Iron), Is.EqualTo(4));
            Assert.That(session.Execute("zinc 3", context), Does.Contain("알 수 없는 명령"));
            Assert.That(inventory.State.GetQuantity(DataIds.Minerals.Iron), Is.EqualTo(4));
        }

        [Test]
        public void Energy_FullAndNumber_ClampToPlayerRange()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var state = GameState.CreateNew();
            state.SetCurrentEnergy(10);
            var context = Context(state, null, null, null);

            Assert.That(session.Execute("전력 full", context), Is.EqualTo("energy 100/100"));
            Assert.That(state.Player.Energy, Is.EqualTo(100));
            Assert.That(session.Execute("energy 1000", context), Is.EqualTo("energy 100/100"));
            Assert.That(session.Execute("ENERGY -5", context), Is.EqualTo("energy 0/100"));
            Assert.That(state.Player.Energy, Is.EqualTo(0));
        }

        [Test]
        public void Health_NumberUsesAbsoluteSetter_FullRestores_MissingPlayerErrors()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var host = new GameObject("PromptB125TerminalHealth");
            created.Add(host);
            var controller = host.AddComponent<PlayerSurvivalController>();
            TrackSettings(controller);
            var failures = 0;
            controller.FailureRequested += _ => failures++;
            controller.SetHealthAbsolute(1);

            var context = Context(null, null, () => controller, () => null);
            Assert.That(session.Execute("체력 가득", context), Does.Contain("hp "));
            Assert.That(controller.State.Health, Is.EqualTo(controller.State.MaximumHealth));
            Assert.That(failures, Is.Zero);

            var zero = session.Execute("hp 0", context);
            Assert.That(zero, Does.StartWith("hp 0/"));
            Assert.That(controller.State.Health, Is.EqualTo(0f));
            Assert.That(controller.State.CanAct, Is.False);
            Assert.That(failures, Is.Zero);

            Assert.That(session.Execute("hp 4", Context(null, null, () => null, () => null)), Is.EqualTo("플레이어 없음"));
        }

        [Test]
        public void Quest_AdvancesOnce_AndCompleteIsNoOp()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var host = new GameObject("PromptB125TerminalQuest");
            created.Add(host);
            var binder = host.AddComponent<TutorialDirectorBinder>();
            Wake(binder);
            Assert.That(binder.Director, Is.Not.Null);
            var before = binder.Director.CurrentObjectiveId;
            var context = Context(null, null, null, () => binder);

            var advanced = session.Execute("qc", context);
            Assert.That(advanced, Does.Contain("퀘스트 진행"));
            Assert.That(binder.Director.CurrentObjectiveId, Is.Not.EqualTo(before));

            binder.Director.RestoreFromProgress(new ProgressState(
                binder.Director.CompletedCount,
                false,
                binder.Director.CurrentObjectiveId,
                true));
            var frozen = binder.Director.CurrentObjectiveId;
            var noop = session.Execute("퀘스트클리어", context);
            Assert.That(noop, Is.EqualTo("데모 완료 상태라 진행하지 않음"));
            Assert.That(binder.Director.IsDemoComplete, Is.True);
            Assert.That(binder.Director.CurrentObjectiveId, Is.EqualTo(frozen));
            Assert.That(session.Execute("퀘클", Context(null, null, null, () => null)), Is.EqualTo("퀘스트 디렉터 없음"));
        }

        [Test]
        public void Enter_ConfirmsCandidateBeforeExecuting()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            session.NotifyTextChanged("go");
            Assert.That(session.CandidateCount, Is.EqualTo(1));
            Assert.That(session.FormatCandidates(), Is.EqualTo("> gold <n>"));
            Assert.That(session.TryConfirm(out var gold), Is.True);
            Assert.That(gold, Is.EqualTo("gold "));
            Assert.That(session.CandidateCount, Is.EqualTo(0));
            Assert.That(session.TryConfirm(out _), Is.False);

            session.NotifyTextChanged("h");
            Assert.That(session.SelectedName, Is.EqualTo("hp"));
            session.Move(1);
            Assert.That(session.SelectedName, Is.EqualTo("help"));
            session.Move(1);
            Assert.That(session.SelectedName, Is.EqualTo("hp"));
            Assert.That(session.FormatCandidates(), Does.Not.Contain("별칭"));
            Assert.That(session.FormatCandidates(), Does.Contain("hp <n|full|가득>"));

            session.NotifyTextChanged("hel");
            Assert.That(session.TryConfirm(out var helpLine), Is.True);
            Assert.That(helpLine, Is.EqualTo("help"));
            var help = session.Execute(helpLine, null);
            Assert.That(help, Does.Contain("gold <n>"));
            Assert.That(help, Does.Contain("copper <n>"));
            Assert.That(help, Does.Contain("iron <n>"));
            Assert.That(help, Does.Contain("lithium <n>"));
            Assert.That(help, Does.Contain("engine_fuel <n>"));
            Assert.That(help, Does.Contain("energy <n|full|가득>"));
            Assert.That(help, Does.Contain("hp <n|full|가득>"));
            Assert.That(help, Does.Contain("questclear"));
            Assert.That(help, Does.Contain("골드"));
            Assert.That(help, Does.Contain("엔진연료"));
            Assert.That(help, Does.Contain("퀘클"));
            Assert.That(help, Does.Contain("예: gold 100"));
        }

        [Test]
        public void UnknownCommandAndThrow_DoNotEscapeTheSession()
        {
            var registry = DeveloperDebugCommandRegistry.CreateIsolated();
            var session = new DeveloperDebugCommandSession(registry);
            Assert.That(session.Execute("nope", null), Is.EqualTo("알 수 없는 명령: nope"));
            Assert.That(
                registry.Register(new DeveloperDebugCommandSpec(
                    "gold",
                    new[] { "gold" },
                    "<n>",
                    "gold 1",
                    (_, __) => "replaced")),
                Is.False);

            var added = registry.Register(new DeveloperDebugCommandSpec(
                "boom",
                new[] { "boom" },
                string.Empty,
                "boom",
                (_, __) => throw new InvalidOperationException("boom-secret")));
            Assert.That(added, Is.True);
            var failed = session.Execute("boom", null);
            Assert.That(failed, Is.EqualTo("명령 실행 실패"));
            Assert.That(failed, Does.Not.Contain("boom-secret"));
            Assert.That(session.Execute("ping", null), Does.Contain("알 수 없는 명령"));

            Assert.That(
                registry.Register(new DeveloperDebugCommandSpec(
                    "ping",
                    new[] { "ping", "핑" },
                    string.Empty,
                    "ping",
                    (_, __) => "pong")),
                Is.True);
            Assert.That(session.Execute("핑", null), Is.EqualTo("pong"));
        }

        [Test]
        public void Terminal_TogglesPause_AndRunsSimpleCommands()
        {
            Time.timeScale = 1f;
            var state = GameState.CreateNew();
            var terminal = CreateTerminal();
            terminal.BindContext(Context(state, null, null, null));

            var canvas = terminal.GetComponentInChildren<Canvas>(true);
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.gameObject.activeSelf, Is.False);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));

            var panel = canvas.transform.Find("Panel");
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.GetComponent<Image>().color, Is.EqualTo(Color.black));
            var input = panel.GetComponentInChildren<TMP_InputField>(true);
            Assert.That(input, Is.Not.Null);
            Assert.That(input.onFocusSelectAll, Is.False);
            Assert.That(panel.GetComponentInChildren<ScrollRect>(true), Is.Not.Null);
            if (TMP_Settings.defaultFontAsset != null)
            {
                var label = panel.GetComponentInChildren<TextMeshProUGUI>(true);
                Assert.That(label.font, Is.EqualTo(TMP_Settings.defaultFontAsset));
            }

            terminal.HandleShortcutChord(false, true);
            Assert.That(terminal.IsOpen, Is.False);

            terminal.SetInput("gold 20");
            terminal.Submit();
            Assert.That(state.Player.Gold, Is.EqualTo(0));

            terminal.HandleShortcutChord(true, true);
            Assert.That(terminal.IsOpen, Is.True);
            Assert.That(canvas.gameObject.activeSelf, Is.True);
            Assert.That(DeveloperDebugTerminal.SortingOrder, Is.GreaterThan(PopupWindowSorting.SettingsSortOrder));
            Assert.That(DeveloperDebugTerminal.SortingOrder, Is.LessThanOrEqualTo(short.MaxValue));
            Assert.That(canvas.sortingOrder, Is.EqualTo(DeveloperDebugTerminal.SortingOrder));
            Assert.That(UiPauseGate.IsHeldBy(SaveRuntimeController.DebugTerminalPauseOwner), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            terminal.Submit();
            Assert.That(state.Player.Gold, Is.EqualTo(20));
            Assert.That(terminal.Transcript, Does.Contain("> gold 20"));
            Assert.That(terminal.Transcript, Does.Contain("gold 0 -> 20"));

            terminal.SetInput("go");
            Assert.That(terminal.CandidateText, Does.Contain("gold <n>"));
            terminal.Submit();
            Assert.That(terminal.CurrentInput, Is.EqualTo("gold "));
            Assert.That(state.Player.Gold, Is.EqualTo(20));
            terminal.Submit();
            Assert.That(terminal.Transcript, Does.Contain("인자 오류"));
            Assert.That(state.Player.Gold, Is.EqualTo(20));

            terminal.SetInput("hel");
            terminal.Submit();
            Assert.That(terminal.CurrentInput, Is.EqualTo("help"));
            terminal.Submit();
            Assert.That(terminal.Transcript, Does.Contain("questclear"));

            terminal.Toggle();
            Assert.That(terminal.IsOpen, Is.False);
            Assert.That(canvas.gameObject.activeSelf, Is.False);
            Assert.That(UiPauseGate.IsHeldBy(SaveRuntimeController.DebugTerminalPauseOwner), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void Terminal_ParentsUnderBootstrapper()
        {
            GameBootstrapper.ResetInstanceForTests();
            var root = new GameObject("PromptB125Bootstrap");
            created.Add(root);
            var bootstrap = root.AddComponent<GameBootstrapper>();
            Wake(bootstrap);
            var terminal = CreateTerminal();

            terminal.AttachToRuntimeRoot();

            Assert.That(GameBootstrapper.Instance, Is.SameAs(bootstrap));
            Assert.That(terminal.transform.parent == root.transform, Is.True);
        }

        [Test]
        public void Sources_AreDevelopmentGated_AndDoNotCallLaterApis()
        {
            AssertGated("_Project/Scripts/App/Integration/Debug/DeveloperDebugCommandRegistry.cs");
            AssertGated("_Project/Scripts/App/Integration/Debug/DeveloperDebugTerminal.cs");

            var registry = File.ReadAllText(SourcePath(
                "_Project/Scripts/App/Integration/Debug/DeveloperDebugCommandRegistry.cs"));
            var terminal = File.ReadAllText(SourcePath(
                "_Project/Scripts/App/Integration/Debug/DeveloperDebugTerminal.cs"));
            Assert.That(registry, Does.Contain("SetHealthAbsolute"));
            Assert.That(registry, Does.Contain("RestoreFull()"));
            Assert.That(registry, Does.Contain("DebugForceAdvanceObjective()"));
            Assert.That(registry, Does.Not.Contain("TrySpend"));
            Assert.That(terminal, Does.Contain("leftCtrlKey"));
            Assert.That(terminal, Does.Contain("rightCtrlKey"));
            Assert.That(terminal, Does.Contain("backquoteKey"));
            Assert.That(terminal, Does.Not.Contain("f9Key"));
            AssertForbidden(registry);
            AssertForbidden(terminal);
        }

        [Test]
        public void History_PreservesOrderDuplicatesAndErrors_AndRestoresDraft()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            Assert.That(session.TryRecallHistory(-1, "draft", out _), Is.False);
            session.Execute("  help  ", null);
            session.Execute("nope", null);
            session.Execute("help", null);
            session.Execute("   ", null);
            session.NotifyTextChanged("go");

            Assert.That(session.TryRecallHistory(-1, "go", out var latest), Is.True);
            Assert.That(latest, Is.EqualTo("help"));
            Assert.That(session.CandidateCount, Is.Zero);
            Assert.That(session.TryRecallHistory(-1, latest, out var previous), Is.True);
            Assert.That(previous, Is.EqualTo("nope"));
            Assert.That(session.TryRecallHistory(-1, previous, out var oldest), Is.True);
            Assert.That(oldest, Is.EqualTo("help"));
            Assert.That(session.TryRecallHistory(-1, oldest, out _), Is.False);
            Assert.That(session.TryRecallHistory(1, oldest, out previous), Is.True);
            Assert.That(previous, Is.EqualTo("nope"));
            Assert.That(session.TryRecallHistory(1, previous, out latest), Is.True);
            Assert.That(latest, Is.EqualTo("help"));
            Assert.That(session.TryRecallHistory(1, latest, out var draft), Is.True);
            Assert.That(draft, Is.EqualTo("go"));
            Assert.That(session.CandidateCount, Is.EqualTo(1));
            Assert.That(session.TryRecallHistory(1, draft, out _), Is.False);
        }

        [Test]
        public void History_EditingAndExecutionRestartAtLatest()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            session.Execute("gold 1", null);
            session.Execute("gold 2", null);
            session.TryRecallHistory(-1, string.Empty, out _);
            session.TryRecallHistory(-1, "gold 2", out _);
            session.NotifyTextChanged("gold 3");

            Assert.That(session.TryRecallHistory(-1, "gold 3", out var latest), Is.True);
            Assert.That(latest, Is.EqualTo("gold 2"));
            Assert.That(session.TryRecallHistory(1, latest, out var draft), Is.True);
            Assert.That(draft, Is.EqualTo("gold 3"));
            session.Execute(draft, null);
            Assert.That(session.TryRecallHistory(-1, string.Empty, out latest), Is.True);
            Assert.That(latest, Is.EqualTo("gold 3"));
            Assert.That(session.TryRecallHistory(1, latest, out draft), Is.True);
            Assert.That(draft, Is.Empty);
        }

        [TestCase("go", "gold ")]
        [TestCase("골", "gold ")]
        [TestCase("hel", "help")]
        public void Terminal_CompletesCandidateWithoutExecuting(string prefix, string completed)
        {
            var terminal = CreateTerminal();
            terminal.SetInput(prefix);
            Assert.That(terminal.CompleteCandidate(), Is.False);
            terminal.Toggle();
            var transcript = terminal.Transcript;

            Assert.That(terminal.CompleteCandidate(), Is.True);
            Assert.That(terminal.CurrentInput, Is.EqualTo(completed));
            Assert.That(terminal.CandidateText, Is.Empty);
            Assert.That(terminal.Transcript, Is.EqualTo(transcript));
            Assert.That(terminal.CompleteCandidate(), Is.False);
        }

        [Test]
        public void Terminal_FirstOpenShowsHelpAndControlsOnce_WithoutAddingHistory()
        {
            var terminal = CreateTerminal();
            Assert.That(terminal.Transcript, Is.Empty);
            terminal.Toggle();
            var guide = terminal.Transcript;

            Assert.That(guide, Does.Contain("help 또는 도움말"));
            Assert.That(guide, Does.Contain("↑ / ↓: 이전 / 이후 명령어 히스토리 탐색"));
            Assert.That(guide, Does.Contain("Shift + ↑ / ↓: 추천 명령어 선택"));
            Assert.That(guide, Does.Contain("Tab: 선택한 명령어 자동완성"));
            Assert.That(guide, Does.Contain("Enter:"));
            Assert.That(guide, Does.Contain("Ctrl + `:"));
            terminal.SetInput("draft");
            terminal.HandleArrowKey(-1, false);
            Assert.That(terminal.CurrentInput, Is.EqualTo("draft"));
            terminal.Toggle();
            terminal.Toggle();
            Assert.That(terminal.Transcript, Is.EqualTo(guide));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Terminal_ShiftArrowsSelectAndWrapCandidates_ForTabOrEnter(bool useTab)
        {
            var terminal = CreateTerminal();
            terminal.Toggle();
            var guide = terminal.Transcript;
            terminal.SetInput("h");
            Assert.That(terminal.CandidateText, Does.StartWith("> hp "));

            terminal.HandleArrowKey(1, true);
            Assert.That(terminal.CandidateText, Does.Contain("\n> help"));
            Assert.That(terminal.CurrentInput, Is.EqualTo("h"));
            terminal.HandleArrowKey(1, true);
            Assert.That(terminal.CandidateText, Does.StartWith("> hp "));
            terminal.HandleArrowKey(-1, true);
            Assert.That(terminal.CandidateText, Does.Contain("\n> help"));
            Assert.That(terminal.Transcript, Is.EqualTo(guide));

            if (useTab)
            {
                Assert.That(terminal.CompleteCandidate(), Is.True);
            }
            else
            {
                terminal.Submit();
            }

            Assert.That(terminal.CurrentInput, Is.EqualTo("help"));
            Assert.That(terminal.CandidateText, Is.Empty);
            Assert.That(terminal.Transcript, Is.EqualTo(guide));
            terminal.Submit();
            Assert.That(terminal.Transcript, Does.Contain("> help\n"));
        }

        [Test]
        public void Terminal_ShiftArrowsPreserveHistoryAndDraft_AndIgnoreClosedTerminal()
        {
            var terminal = CreateTerminal();
            terminal.Toggle();
            terminal.SetInput("help ");
            terminal.Submit();
            terminal.SetInput("h");
            terminal.HandleArrowKey(1, true);
            terminal.HandleArrowKey(-1, false);
            Assert.That(terminal.CurrentInput, Is.EqualTo("help"));
            Assert.That(terminal.CandidateText, Is.Empty);
            terminal.HandleArrowKey(-1, true);
            Assert.That(terminal.CurrentInput, Is.EqualTo("help"));
            terminal.HandleArrowKey(1, false);
            Assert.That(terminal.CurrentInput, Is.EqualTo("h"));
            Assert.That(terminal.CandidateText, Does.StartWith("> hp "));

            terminal.Toggle();
            terminal.HandleArrowKey(1, true);
            terminal.HandleArrowKey(-1, false);
            Assert.That(terminal.CurrentInput, Is.EqualTo("h"));
            Assert.That(terminal.CandidateText, Does.StartWith("> hp "));
        }

        [Test]
        public void Terminal_RecallsCommandWithArguments_AndExecutesRecalledHelpOnce()
        {
            var state = GameState.CreateNew();
            var terminal = CreateTerminal();
            terminal.BindContext(Context(state, null, null, null));
            terminal.Toggle();
            terminal.SetInput("gold 20");
            terminal.Submit();
            terminal.SetInput("hel");
            terminal.CompleteCandidate();
            terminal.Submit();
            terminal.SetInput("draft");

            terminal.RecallHistory(-1);
            Assert.That(terminal.CurrentInput, Is.EqualTo("help"));
            terminal.RecallHistory(-1);
            Assert.That(terminal.CurrentInput, Is.EqualTo("gold 20"));
            terminal.RecallHistory(1);
            Assert.That(terminal.CurrentInput, Is.EqualTo("help"));
            terminal.RecallHistory(1);
            Assert.That(terminal.CurrentInput, Is.EqualTo("draft"));
            terminal.RecallHistory(-1);
            terminal.Submit();
            Assert.That(terminal.CurrentInput, Is.Empty);
            Assert.That(terminal.Transcript.Split(new[] { "> help" }, StringSplitOptions.None).Length, Is.EqualTo(3));
            Assert.That(state.Player.Gold, Is.EqualTo(20));
            terminal.Toggle();
            terminal.RecallHistory(-1);
            Assert.That(terminal.CurrentInput, Is.Empty);
            terminal.Toggle();
            terminal.RecallHistory(-1);
            Assert.That(terminal.CurrentInput, Is.EqualTo("help"));
        }

        [Test]
        public void Terminal_OutputUsesFullHeight_AndCandidatesOnlyReserveTheirTextHeight()
        {
            var terminal = CreateTerminal();
            terminal.Toggle();
            terminal.SetInput("hel");
            terminal.CompleteCandidate();
            terminal.Submit();
            Canvas.ForceUpdateCanvases();
            var panel = terminal.GetComponentInChildren<Canvas>(true).transform.Find("Panel");
            var scroll = panel.Find("OutputScroll").GetComponent<RectTransform>();
            var input = panel.Find("InputLine").GetComponent<RectTransform>();
            var candidates = panel.Find("Candidates").GetComponent<RectTransform>();
            var output = scroll.GetComponent<ScrollRect>();

            Assert.That(candidates.gameObject.activeSelf, Is.False);
            Assert.That(scroll.offsetMin.y - input.offsetMax.y, Is.EqualTo(10f));
            Assert.That(scroll.rect.height, Is.GreaterThan(panel.GetComponent<RectTransform>().rect.height * 0.8f));
            Assert.That(output.viewport.rect.height, Is.EqualTo(scroll.rect.height).Within(0.01f));
            Assert.That(output.content.rect.height, Is.GreaterThan(output.viewport.rect.height));
            Assert.That(output.verticalNormalizedPosition, Is.EqualTo(0f).Within(0.01f));

            terminal.SetInput("h");
            Canvas.ForceUpdateCanvases();
            Assert.That(candidates.gameObject.activeSelf, Is.True);
            Assert.That(candidates.rect.height, Is.EqualTo(candidates.GetComponentInChildren<TMP_Text>().preferredHeight).Within(0.01f));
            Assert.That(candidates.offsetMin.y - input.offsetMax.y, Is.EqualTo(10f));
            Assert.That(scroll.offsetMin.y - candidates.offsetMax.y, Is.EqualTo(10f).Within(0.01f));
            terminal.SetInput(string.Empty);
            Assert.That(scroll.offsetMin.y - input.offsetMax.y, Is.EqualTo(10f));
        }

        private DeveloperDebugTerminal CreateTerminal()
        {
            var go = new GameObject("PromptB125Terminal");
            created.Add(go);
            var terminal = go.AddComponent<DeveloperDebugTerminal>();
            terminal.Initialize();
            return terminal;
        }

        private static void Wake(MonoBehaviour behaviour)
        {
            var awake = behaviour.GetType().GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (awake != null)
            {
                awake.Invoke(behaviour, null);
            }
        }

        private static DeveloperDebugCommandContext Context(
            GameState state,
            InventoryService inventory,
            Func<PlayerSurvivalController> player,
            Func<TutorialDirectorBinder> tutorial)
        {
            return new DeveloperDebugCommandContext(state, inventory, player, tutorial);
        }

        private static InMemoryMineralCatalog Catalog()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(DataIds.Minerals.Copper, 1f, 1, "Copper");
            catalog.Register(DataIds.Minerals.Iron, 1f, 1, "Iron");
            catalog.Register(DataIds.Minerals.Lithium, 1f, 1, "Lithium");
            catalog.Register(DataIds.RareItems.EngineFuel, 1f, 1, "Fuel");
            return catalog;
        }

        private void TrackSettings(PlayerSurvivalController controller)
        {
            var settings = typeof(PlayerSurvivalController)
                .GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(controller) as PlayerSurvivalSettings;
            if (settings != null)
            {
                created.Add(settings);
            }
        }

        private static void AssertForbidden(string text)
        {
            Assert.That(text, Does.Not.Contain("SetUpgradeLevelAbsolute"));
            Assert.That(text, Does.Not.Contain("ForceUnlockDeepZone"));
            Assert.That(text, Does.Not.Contain("SetNextPaidResetFeeOverride"));
            Assert.That(text, Does.Not.Contain("SetMineResetElapsedSeconds"));
            Assert.That(text, Does.Not.Contain("ResetMineWithoutPopup"));
            Assert.That(text, Does.Not.Contain("Debug.isDebugBuild"));
            var stripped = text.Replace("SUBTERRA_BUILD_DEVELOPMENT", string.Empty);
            Assert.That(stripped, Does.Not.Contain("DEVELOPMENT_BUILD"));
            Assert.That(stripped, Does.Not.Contain("SUBTERRA_BUILD_QA"));
        }

        private static void AssertGated(string assetRelativePath)
        {
            var text = File.ReadAllText(SourcePath(assetRelativePath));
            var marker = "namespace SubTerra.App.Integration";
            var index = text.IndexOf(marker, StringComparison.Ordinal);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), assetRelativePath);
            var ifIndex = text.LastIndexOf("#if", index, StringComparison.Ordinal);
            var endIndex = text.LastIndexOf("#endif", index, StringComparison.Ordinal);
            Assert.That(ifIndex, Is.GreaterThan(endIndex), assetRelativePath);
            var lineEnd = text.IndexOf('\n', ifIndex);
            if (lineEnd < 0)
            {
                lineEnd = text.Length;
            }

            var line = text.Substring(ifIndex, lineEnd - ifIndex).Trim().TrimEnd('\r');
            Assert.That(line, Is.EqualTo("#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT"));
        }

        private static string SourcePath(string assetRelativePath)
        {
            return Path.Combine(Application.dataPath, assetRelativePath);
        }
    }
}
