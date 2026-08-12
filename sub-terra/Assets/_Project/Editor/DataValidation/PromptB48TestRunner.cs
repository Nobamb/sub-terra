#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>prompt-B 48 사다리 시설 건설 창 테스트만 실행한다.</summary>
    public static class PromptB48TestRunner
    {
        private static TestRunnerApi api;
        private static ResultWriter receiver;

        [MenuItem("SubTerra/Tests/Run Prompt-B 48 Ladder Building Menu Tests")]
        public static void RunFromMenu()
        {
            RunEditMode();
        }

        public static void RunEditMode()
        {
            ReleaseRunner();
            var path = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "Temp", "prompt-b48-editmode-results.txt"));
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
                assemblyNames = new[] { "SubTerra.App.Tests.EditMode" },
                groupNames = new[]
                {
                    "SubTerra.App.Tests.UI.PromptB48LadderBuildingMenuTests"
                }
            }));
            Debug.Log("[SubTerra] Prompt-B 48 EditMode tests requested → " + path);
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

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var sb = new StringBuilder();
                sb.AppendLine("pass=" + result.PassCount);
                sb.AppendLine("fail=" + result.FailCount);
                sb.AppendLine("skip=" + result.SkipCount);
                Append(result, sb, 0);
                File.WriteAllText(path, sb.ToString());
                File.WriteAllText(path + ".done", DateTime.Now.ToString("o"));
                Debug.Log(
                    "[SubTerra] Prompt-B 48 tests finished pass="
                    + result.PassCount
                    + " fail="
                    + result.FailCount);
                ReleaseRunner(this);
            }

            private static void Append(ITestResultAdaptor result, StringBuilder sb, int depth)
            {
                if (!result.HasChildren)
                {
                    sb.AppendLine(new string(' ', depth * 2) + result.ResultState + " " + result.FullName);
                    if (result.ResultState != "Passed" && result.ResultState != "Skipped")
                    {
                        sb.AppendLine(result.Message ?? string.Empty);
                    }

                    return;
                }

                foreach (var child in result.Children)
                {
                    Append(child, sb, depth + 1);
                }
            }
        }
    }
}
#endif
