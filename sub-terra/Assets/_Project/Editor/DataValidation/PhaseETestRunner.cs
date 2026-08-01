using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>Phase E Mining/Player Play Mode 테스트를 분리 실행해 결과를 기록한다.</summary>
    public static class PhaseETestRunner
    {
        private static TestRunnerApi api;
        private static ResultWriter receiver;

        [MenuItem("SubTerra/Tests/Run Phase E Play Mode")]
        public static void RunPhaseEPlayMode()
        {
            Run(
                TestMode.PlayMode,
                new[]
                {
                    "SubTerra.Gameplay.Mining.PlayModeTests",
                    "SubTerra.Gameplay.Player.PlayModeTests"
                },
                new[]
                {
                    "SubTerra.Gameplay.Mining.Tests.MiningSystemPlayModeTests",
                    "SubTerra.Gameplay.Player.Tests.PlayerMovementPlayModeTests"
                },
                "phase-e-playmode-results.txt");
        }

        [MenuItem("SubTerra/Tests/Run Phase E Edit Mode")]
        public static void RunPhaseEEditMode()
        {
            Run(
                TestMode.EditMode,
                new[] { "SubTerra.App.Tests.EditMode" },
                new[] { "SubTerra.App.Tests.Integration.PhaseEMiningStaticTests" },
                "phase-e-editmode-results.txt");
        }

        private static void Run(
            TestMode mode,
            string[] assemblies,
            string[] groups,
            string fileName)
        {
            var path = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "Temp",
                fileName));
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            receiver = new ResultWriter(path, "Phase E " + mode + " Test Run");
            api.RegisterCallbacks(receiver);
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = mode,
                assemblyNames = assemblies,
                groupNames = groups
            }));
            Debug.Log("[SubTerra] Phase E tests requested: " + fileName);
        }

        private sealed class ResultWriter : ICallbacks
        {
            private readonly string path;
            private readonly StringBuilder output = new();
            private int passed;
            private int failed;
            private int skipped;

            public ResultWriter(string resultPath, string label)
            {
                path = resultPath;
                output.AppendLine(label);
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
                File.WriteAllText(path + ".done", result.TestStatus.ToString());
                Debug.Log($"[SubTerra] Phase E Play Mode finished Pass={passed} Fail={failed}");
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
