using System;
using System.IO;
using System.Text;
using SubTerra.App.Progression;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>데모 퀘스트 순서·스킵 방지 게이트를 Editor에서 즉시 검증한다.</summary>
    public static class PhaseNVerificationCommand
    {
        [MenuItem("SubTerra/Tests/Verify Phase N Gates")]
        public static void VerifyFromMenu()
        {
            var report = RunAll();
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var editPath = Path.Combine(projectRoot, "Temp", "phase-n-editmode-results.txt");
            var playPath = Path.Combine(projectRoot, "Temp", "phase-n-playmode-results.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(editPath));
            File.WriteAllText(editPath, report.EditLog);
            File.WriteAllText(playPath, report.PlayLog);
            Debug.Log("[SubTerra] Phase N verification pass=" + report.Pass + " fail=" + report.Fail);
            if (report.Fail > 0)
            {
                Debug.LogError("[SubTerra] Phase N verification failures:\n" + report.FailDetails);
            }
        }

        public static Report RunAll()
        {
            var edit = new StringBuilder();
            var play = new StringBuilder();
            var failures = new StringBuilder();
            var pass = 0;
            var fail = 0;

            void Check(StringBuilder log, string name, bool ok, string detail = "")
            {
                if (ok)
                {
                    pass++;
                    log.AppendLine("PASS " + name);
                    return;
                }

                fail++;
                log.AppendLine("FAIL " + name + " " + detail);
                failures.AppendLine(name + ": " + detail);
            }

            edit.AppendLine("Prompt-B 60 quest gates");
            edit.AppendLine(DateTime.Now.ToString("o"));
            Check(edit, "quest-count", DemoObjectiveCatalog.All.Count == 17
                && DemoObjectiveIds.Ordered.Length == 17
                && DemoObjectiveIds.RequiredCount == 17);

            var chainIsValid = true;
            for (var i = 0; i < DemoObjectiveCatalog.All.Count - 1; i++)
            {
                chainIsValid &= DemoObjectiveCatalog.All[i].Id == DemoObjectiveIds.Ordered[i]
                    && DemoObjectiveCatalog.All[i].NextObjectiveId == DemoObjectiveIds.Ordered[i + 1];
            }

            Check(edit, "quest-chain", chainIsValid);
            Check(edit, "deep-zone-boundary", DeepZoneUnlockRule.Mvp.RequiredCompletedObjectives == 12);

            var engine = new DemoObjectiveTransitionEngine();
            var fullSequence = true;
            for (var i = 0; i < DemoObjectiveCatalog.All.Count; i++)
            {
                var current = DemoObjectiveCatalog.GetRequired(engine.CurrentObjectiveId);
                if (current == null || !engine.TryAdvance(current.RequiredSignal).Advanced)
                {
                    fullSequence = false;
                    break;
                }
            }

            Check(edit, "full-sequence", fullSequence
                && engine.IsDemoComplete
                && engine.CompletedCount == 17);

            var noSkip = new DemoObjectiveTransitionEngine();
            Check(edit, "reject-future-signal",
                !noSkip.TryAdvance(DemoProgressSignal.MineralSoldAtSettlement).Advanced
                && noSkip.CurrentObjectiveId == DemoObjectiveIds.MineBlock);
            noSkip.TryAdvance(DemoProgressSignal.BlockMined);
            Check(edit, "one-signal-one-step",
                noSkip.CurrentObjectiveId == DemoObjectiveIds.MineCopper
                && noSkip.CompletedCount == 1);

            var directorSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "Tutorial",
                "DemoObjectiveDirector.cs"));
            Check(edit, "no-remembered-auto-chain",
                !directorSource.Contains("AdvanceRememberedObjectives")
                && directorSource.Contains("GasPurified")
                && directorSource.Contains("EmergencyEscapeSucceeded"));

            play.AppendLine("Prompt-B 60 persisted transition gates");
            play.AppendLine(DateTime.Now.ToString("o"));
            var state = GameState.CreateNew();
            state.SetDemoProgress(DemoObjectiveIds.TravelToSurface, 3, false);
            Check(play, "wrong-elevator-direction-rejected",
                !DemoObjectiveDirector.AdvancePersistedState(
                    state,
                    DemoProgressSignal.MineReachedByElevator).Advanced);
            Check(play, "surface-travel-advances",
                DemoObjectiveDirector.AdvancePersistedState(
                    state,
                    DemoProgressSignal.SurfaceReachedByElevator).Advanced
                && state.Progress.CurrentObjectiveId == DemoObjectiveIds.ReturnToMine);

            edit.AppendLine("SUMMARY pass=" + pass + " fail=" + fail);
            play.AppendLine("SUMMARY pass=" + pass + " fail=" + fail);
            return new Report
            {
                Pass = pass,
                Fail = fail,
                EditLog = edit.ToString(),
                PlayLog = play.ToString(),
                FailDetails = failures.ToString()
            };
        }

        public sealed class Report
        {
            public int Pass;
            public int Fail;
            public string EditLog;
            public string PlayLog;
            public string FailDetails;
        }
    }
}
