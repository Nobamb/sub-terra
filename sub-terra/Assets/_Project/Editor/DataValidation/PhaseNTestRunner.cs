using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>Phase N 목표 전이 Edit/Play Mode 테스트 실행 및 결과 기록.</summary>
    public static class PhaseNTestRunner
    {
        private static TestRunnerApi api;
        private static PhaseNResultWriter receiver;

        [MenuItem("SubTerra/Tests/Run Phase N Edit Mode")]
        public static void RunPhaseNEditMode()
        {
            Run(
                TestMode.EditMode,
                new[] { "SubTerra.App.Tests.EditMode" },
                new[]
                {
                    "SubTerra.App.Tests.Tutorial.DemoObjectiveTransitionTests",
                    "SubTerra.App.Tests.Tutorial.DemoObjectiveSaveRoundTripTests"
                },
                "Temp/phase-n-editmode-results.txt",
                "Phase N Edit Mode");
        }

        [MenuItem("SubTerra/Tests/Run Phase N Play Mode")]
        public static void RunPhaseNPlayMode()
        {
            Run(
                TestMode.PlayMode,
                new[] { "SubTerra.App.Tests.PlayMode" },
                new[] { "SubTerra.App.Tests.PlayMode.DemoFlow.DemoFlowPlayModeTests" },
                "Temp/phase-n-playmode-results.txt",
                "Phase N Play Mode");
        }

        private static void Run(
            TestMode mode,
            string[] assemblies,
            string[] groupNames,
            string relativePath,
            string label)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var path = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (api != null && receiver != null)
            {
                api.UnregisterCallbacks(receiver);
                UnityEngine.Object.DestroyImmediate(api);
            }

            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            receiver = new PhaseNResultWriter(path, label);
            api.RegisterCallbacks(receiver);
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = mode,
                assemblyNames = assemblies,
                groupNames = groupNames
            }));
            Debug.Log("[SubTerra] " + label + " requested → " + path);
        }

        private sealed class PhaseNResultWriter : ICallbacks
        {
            private readonly string path;
            private readonly string label;
            private readonly StringBuilder sb = new StringBuilder();
            private int pass;
            private int fail;
            private int skip;

            public PhaseNResultWriter(string path, string label)
            {
                this.path = path;
                this.label = label;
                sb.AppendLine(label);
                sb.AppendLine("Started: " + DateTime.Now.ToString("o"));
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                sb.AppendLine("RunStarted: " + testsToRun.Name);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                sb.AppendLine("RunFinished");
                sb.AppendLine("Result: " + result.TestStatus);
                sb.AppendLine("Pass: " + pass + " Fail: " + fail + " Skip: " + skip);
                sb.AppendLine("DurationSec: " + result.Duration);
                try
                {
                    File.WriteAllText(path, sb.ToString());
                }
                catch (Exception ex)
                {
                    Debug.LogError("[SubTerra] Phase N write failed: " + ex.Message);
                }

                Debug.Log("[SubTerra] " + label + " finished Pass=" + pass + " Fail=" + fail);
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
                        pass++;
                        break;
                    case TestStatus.Failed:
                        fail++;
                        sb.AppendLine("FAIL " + result.FullName);
                        sb.AppendLine("  " + result.Message);
                        break;
                    default:
                        skip++;
                        break;
                }
            }
        }
    }
}
