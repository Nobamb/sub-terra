using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>Phase C 물리 테스트만 분리 실행해 전체 회귀 결과와 구분해 기록한다.</summary>
    public static class PhaseCTestRunner
    {
        private static TestRunnerApi api;
        private static ResultWriter receiver;

        [MenuItem("SubTerra/Tests/Run Phase C Play Mode")]
        public static void RunPhaseCPlayMode()
        {
            var path = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "Temp",
                "phase-c-playmode-results.txt"));
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            receiver = new ResultWriter(path);
            api.RegisterCallbacks(receiver);
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.PlayMode,
                assemblyNames = new[]
                {
                    "SubTerra.Gameplay.Player.PlayModeTests",
                    "SubTerra.App.Tests.PlayMode"
                },
                groupNames = new[]
                {
                    "SubTerra.Gameplay.Player.Tests.PlayerMovementPlayModeTests",
                    "SubTerra.Gameplay.Player.Tests.ElevatorControllerPlayModeTests",
                    "SubTerra.App.Tests.PlayMode.Traversal.ElevatorRoundTripPlayModeTests"
                }
            }));
            Debug.Log("[SubTerra] Phase C Play Mode tests requested.");
        }

        private sealed class ResultWriter : ICallbacks
        {
            private readonly string path;
            private readonly StringBuilder output = new();
            private int passed;
            private int failed;
            private int skipped;

            public ResultWriter(string resultPath)
            {
                path = resultPath;
                output.AppendLine("Phase C Play Mode Test Run");
                output.AppendLine("Started: " + DateTime.Now.ToString("o"));
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                output.AppendLine("RunStarted: " + testsToRun.Name);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                output.AppendLine("RunFinished");
                output.AppendLine("Result: " + result.TestStatus);
                output.AppendLine($"Pass: {passed} Fail: {failed} Skip: {skipped}");
                output.AppendLine("DurationSec: " + result.Duration);
                File.WriteAllText(path, output.ToString());
                Debug.Log($"[SubTerra] Phase C Play Mode finished Pass={passed} Fail={failed}");
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

                switch (result.TestStatus)
                {
                    case TestStatus.Passed:
                        passed++;
                        break;
                    case TestStatus.Failed:
                        failed++;
                        output.AppendLine("FAIL: " + result.FullName);
                        output.AppendLine(result.Message);
                        output.AppendLine(result.StackTrace);
                        break;
                    default:
                        skipped++;
                        break;
                }
            }
        }
    }
}
