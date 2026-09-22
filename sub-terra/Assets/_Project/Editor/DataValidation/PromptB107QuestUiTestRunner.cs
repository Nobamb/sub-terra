#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    public static class PromptB107QuestUiTestRunner
    {
        public const string ResultPath = "Temp/prompt-b107-editmode-results.txt";
        public const string FlagPath = "Temp/subterra-run-prompt-b107.flag";

        [InitializeOnLoadMethod]
        private static void WatchRequest()
        {
            EditorApplication.update += () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(FlagPath)) return;
                File.Delete(FlagPath);
                Run();
            };
        }

        private static TestRunnerApi api;
        private static ResultWriter receiver;

        [MenuItem("SubTerra/UI/Run Prompt-B 107 Tests")]
        public static void RunFromMenu()
        {
            Run();
        }

        public static void Run()
        {
            if (api != null && receiver != null)
            {
                api.UnregisterCallbacks(receiver);
                UnityEngine.Object.DestroyImmediate(api);
            }

            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ResultPath));
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (File.Exists(path + ".done"))
            {
                File.Delete(path + ".done");
            }

            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            receiver = new ResultWriter(path);
            api.RegisterCallbacks(receiver);
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "SubTerra.App.Tests.EditMode" },
                groupNames = new[] { "SubTerra.App.Tests.Tutorial.PromptB107QuestUiTests" }
            }));
        }

        private sealed class ResultWriter : ICallbacks
        {
            private readonly string path;
            private readonly StringBuilder output = new StringBuilder();
            private int passed;
            private int failed;

            public ResultWriter(string resultPath)
            {
                path = resultPath;
                output.AppendLine("Prompt-B 107 EditMode");
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                output.AppendLine("RunFinished: " + result.TestStatus);
                output.AppendLine("Pass: " + passed + " Fail: " + failed);
                File.WriteAllText(path, output.ToString());
                File.WriteAllText(path + ".done", result.TestStatus.ToString());
                Debug.Log("[SubTerra] Prompt-B 107 finished Pass=" + passed + " Fail=" + failed);
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
                    output.AppendLine("PASS: " + result.FullName);
                    return;
                }

                failed++;
                output.AppendLine("FAIL: " + result.FullName);
                output.AppendLine(result.Message);
                output.AppendLine(result.StackTrace);
            }
        }
    }
}
#endif
