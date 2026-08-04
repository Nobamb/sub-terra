using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.Editor
{
    /// <summary>MVP2 Phase M Edit Mode 테스트 실행 및 결과 파일 기록.</summary>
    public static class PhaseMTestRunner
    {
        private const string Scratch =
            @"C:\Users\USER\AppData\Local\Temp\grok-goal-c9a3ec143937\implementer";

        [MenuItem("SubTerra/MVP2/Run Phase M EditMode Tests")]
        public static void Run()
        {
            Directory.CreateDirectory(Scratch);
            var outPath = Path.Combine(Scratch, "phase-m-test-results.txt");
            var logPath = Path.Combine(Scratch, "phase-m-editmode.log");
            File.WriteAllText(outPath, "Phase M EditMode starting...\n");

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks(outPath, logPath));
            var filter = new Filter
            {
                testMode = TestMode.EditMode,
                groupNames = new[]
                {
                    "SubTerra.App.Tests.Save.PhaseMWorldSaveTests",
                    "SubTerra.App.Tests.Save.SaveServiceTests",
                    "SubTerra.Gameplay.Snapshot.Tests.WorldSnapshotSystemTests",
                    "SubTerra.App.Tests.WorldSnapshotDtoTests"
                }
            };
            api.Execute(new ExecutionSettings(filter));
            Debug.Log("[PhaseM] EditMode tests requested -> " + outPath);
        }

        private sealed class Callbacks : ICallbacks
        {
            private readonly string outPath;
            private readonly string logPath;
            private readonly StringBuilder sb = new StringBuilder();

            public Callbacks(string resultsPath, string editModeLogPath)
            {
                outPath = resultsPath;
                logPath = editModeLogPath;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                sb.AppendLine("RunStarted " + DateTime.UtcNow.ToString("o"));
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                sb.AppendLine("=== SUMMARY ===");
                sb.AppendLine("Result: " + result.TestStatus);
                sb.AppendLine("Pass: " + result.PassCount);
                sb.AppendLine("Fail: " + result.FailCount);
                sb.AppendLine("Skip: " + result.SkipCount);
                sb.AppendLine("Inconclusive: " + result.InconclusiveCount);
                sb.AppendLine("Duration: " + result.Duration);
                File.WriteAllText(outPath, sb.ToString());
                File.WriteAllText(logPath, sb.ToString());
                Debug.Log(
                    "[PhaseM] finished Pass="
                    + result.PassCount
                    + " Fail="
                    + result.FailCount
                    + " Status="
                    + result.TestStatus);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren)
                {
                    return;
                }

                sb.AppendLine(result.TestStatus + ": " + result.FullName);
                if (result.TestStatus == TestStatus.Failed)
                {
                    sb.AppendLine("  MSG: " + result.Message);
                    if (!string.IsNullOrEmpty(result.StackTrace))
                    {
                        sb.AppendLine("  STACK: " + result.StackTrace);
                    }
                }
            }
        }
    }
}
