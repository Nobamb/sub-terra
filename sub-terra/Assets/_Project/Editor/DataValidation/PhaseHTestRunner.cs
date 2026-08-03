using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>Phase H 관련 Edit/Play Mode 테스트를 분리 실행하고 결과를 기록한다.</summary>
    public static class PhaseHTestRunner
    {
        private static TestRunnerApi api;
        private static ResultWriter receiver;

        [MenuItem("SubTerra/Tests/Run Phase H Edit Mode")]
        public static void RunEditMode()
        {
            Run(
                TestMode.EditMode,
                new[]
                {
                    "SubTerra.Gameplay.Hazards.EditModeTests",
                    "SubTerra.App.Tests.EditMode"
                },
                new[]
                {
                    "SubTerra.Gameplay.Hazards.Tests",
                    "SubTerra.App.Tests.Integration.GasExposureEffectControllerTests",
                    "SubTerra.App.Tests.Integration.PhaseHGasStaticTests"
                },
                "phase-h-editmode-results.txt");
        }

        [MenuItem("SubTerra/Tests/Run Phase H Play Mode")]
        public static void RunPlayMode()
        {
            Run(
                TestMode.PlayMode,
                new[] { "SubTerra.App.Tests.PlayMode" },
                new[] { "SubTerra.App.Tests.PlayMode.Hazards.GasExposurePlayModeTests" },
                "phase-h-playmode-results.txt");
        }

        private static void Run(
            TestMode mode,
            string[] assemblies,
            string[] groups,
            string fileName)
        {
            ReleaseRunner();
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", fileName));
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            receiver = new ResultWriter(path, "Phase H " + mode);
            api.RegisterCallbacks(receiver);
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = mode,
                assemblyNames = assemblies,
                groupNames = groups
            }));
            Debug.Log("[SubTerra] Phase H tests requested: " + fileName);
        }

        private static void ReleaseRunner(ResultWriter completed = null)
        {
            if (completed != null && completed != receiver)
            {
                return;
            }

            if (api != null && receiver != null)
            {
                api.UnregisterCallbacks(receiver);
            }

            if (api != null)
            {
                UnityEngine.Object.DestroyImmediate(api);
            }

            api = null;
            receiver = null;
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
                output.AppendLine("RunFinished: " + result.TestStatus);
                output.AppendLine($"Pass: {passed} Fail: {failed} Skip: {skipped}");
                output.AppendLine("DurationSec: " + result.Duration);
                File.WriteAllText(path, output.ToString());
                File.WriteAllText(path + ".done", result.TestStatus.ToString());
                Debug.Log($"[SubTerra] Phase H finished Pass={passed} Fail={failed} Skip={skipped}");
                EditorApplication.delayCall += () => ReleaseRunner(this);
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

                if (result.TestStatus == TestStatus.Passed)
                {
                    passed++;
                }
                else if (result.TestStatus == TestStatus.Failed)
                {
                    failed++;
                    output.AppendLine("FAIL: " + result.FullName);
                    output.AppendLine(result.Message);
                    output.AppendLine(result.StackTrace);
                }
                else
                {
                    skipped++;
                }
            }
        }
    }
}
