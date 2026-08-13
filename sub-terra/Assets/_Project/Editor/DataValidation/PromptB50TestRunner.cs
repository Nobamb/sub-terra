#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>prompt-B 50 가스 생성/접근 연출 테스트만 실행한다.</summary>
    public static class PromptB50TestRunner
    {
        private static TestRunnerApi api;
        private static ResultWriter receiver;

        [MenuItem("SubTerra/Tests/Run Prompt-B 50 Gas Vision Tests")]
        public static void RunFromMenu()
        {
            RunEditMode();
        }

        public static void RunEditMode()
        {
            ReleaseRunner();
            var path = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "Temp", "prompt-b50-editmode-results.txt"));
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var donePath = path + ".done";
            if (File.Exists(donePath))
            {
                File.Delete(donePath);
            }

            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            receiver = new ResultWriter(path);
            api.RegisterCallbacks(receiver);
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[]
                {
                    "SubTerra.Gameplay.Hazards.EditModeTests",
                    "SubTerra.App.Tests.EditMode"
                },
                groupNames = new[]
                {
                    "SubTerra.Gameplay.Hazards.Tests.GasZoneSpawnAndRangeTests",
                    "SubTerra.Gameplay.Hazards.Tests.GasExposureEffectModelTests",
                    "SubTerra.App.Tests.Integration.PromptB50GasVisionTests"
                }
            }));
            Debug.Log("[SubTerra] Prompt-B 50 EditMode tests requested → " + path);
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

            public ResultWriter(string resultPath)
            {
                path = resultPath;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var builder = new StringBuilder();
                builder.AppendLine(result.TestStatus + " pass=" + result.PassCount
                    + " fail=" + result.FailCount + " skip=" + result.SkipCount);
                WriteNode(result, builder, 0);
                File.WriteAllText(path, builder.ToString());
                File.WriteAllText(path + ".done", result.TestStatus.ToString());
                Debug.Log("[SubTerra] Prompt-B 50 tests finished: " + result.TestStatus
                    + " fail=" + result.FailCount);
                ReleaseRunner(this);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }

            private static void WriteNode(ITestResultAdaptor node, StringBuilder builder, int depth)
            {
                if (!node.HasChildren)
                {
                    builder.Append(' ', depth * 2);
                    builder.Append(node.TestStatus);
                    builder.Append(' ');
                    builder.AppendLine(node.FullName);
                    if (node.TestStatus == TestStatus.Failed)
                    {
                        builder.AppendLine(node.Message);
                    }
                }

                foreach (var child in node.Children)
                {
                    WriteNode(child, builder, depth + 1);
                }
            }
        }
    }
}
#endif
