using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>Phase L Edit/Play Mode 검증을 분리 실행하고 Temp에 근거를 기록한다.</summary>
    public static class PhaseLTestRunner
    {
        private static TestRunnerApi api;
        private static ResultWriter receiver;

        [MenuItem("SubTerra/Tests/Run Phase L Edit Mode")]
        public static void RunEditMode()
        {
            Run(
                TestMode.EditMode,
                new[] { "SubTerra.App.Tests.EditMode" },
                new[]
                {
                    "SubTerra.App.Tests.Run.RunFailureServiceTests",
                    "SubTerra.App.Tests.Integration.PhaseLRunFailureStaticTests"
                },
                "phase-l-editmode-results.txt");
        }

        [MenuItem("SubTerra/Tests/Run Phase L Play Mode")]
        public static void RunPlayMode()
        {
            Run(
                TestMode.PlayMode,
                new[]
                {
                    "SubTerra.Gameplay.Player.PlayModeTests",
                    "SubTerra.App.Tests.PlayMode"
                },
                new[]
                {
                    "SubTerra.Gameplay.Player.Tests.PlayerSurvivalControllerPlayModeTests",
                    "SubTerra.App.Tests.PlayMode.RunFailure.RunFailureRuntimePlayModeTests"
                },
                "phase-l-playmode-results.txt");
        }

        public static void RunAllAppEditMode()
        {
            Run(
                TestMode.EditMode,
                new[] { "SubTerra.App.Tests.EditMode" },
                null,
                "phase-l-regression-editmode-results.txt");
        }

        public static void RunAllAppPlayMode()
        {
            Run(
                TestMode.PlayMode,
                new[] { "SubTerra.App.Tests.PlayMode" },
                null,
                "phase-l-regression-playmode-results.txt");
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
            receiver = new ResultWriter(path, "Phase L " + mode);
            api.RegisterCallbacks(receiver);
            var filter = new Filter
            {
                testMode = mode,
                assemblyNames = assemblies
            };
            if (groups != null && groups.Length > 0)
            {
                filter.groupNames = groups;
            }

            api.Execute(new ExecutionSettings(filter));
            Debug.Log("[SubTerra] Phase L tests requested: " + fileName);
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
            private readonly StringBuilder output = new StringBuilder();
            private int passed;
            private int failed;
            private int skipped;
            private bool finished;

            public ResultWriter(string resultPath, string label)
            {
                path = resultPath;
                output.AppendLine(label);
                output.AppendLine("Started: " + DateTime.Now.ToString("o"));
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                if (finished)
                {
                    return;
                }

                output.AppendLine("RunStarted: " + testsToRun.Name);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                if (finished)
                {
                    return;
                }

                finished = true;
                output.AppendLine("RunFinished: " + result.TestStatus);
                output.AppendLine("Pass: " + passed + " Fail: " + failed + " Skip: " + skipped);
                output.AppendLine("DurationSec: " + result.Duration);
                File.WriteAllText(path, output.ToString());
                File.WriteAllText(path + ".done", result.TestStatus.ToString());
                Debug.Log("[SubTerra] Phase L finished Pass=" + passed + " Fail=" + failed);
                EditorApplication.delayCall += () => ReleaseRunner(this);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (finished || result.HasChildren)
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
