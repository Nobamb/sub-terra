#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>prompt-B 95 퀘스트 보상 테스트를 실행한다.</summary>
    public static class PromptB95QuestRewardTestRunner
    {
        [InitializeOnLoadMethod]
        private static void WatchRequest()
        {
            EditorApplication.update += () =>
            {
                const string flag = "Temp/subterra-run-prompt-b95.flag";
                if (EditorApplication.isCompiling
                    || EditorApplication.isPlayingOrWillChangePlaymode
                    || !File.Exists(flag))
                {
                    return;
                }

                File.Delete(flag);
                RunEditMode();
            };
        }

        private static TestRunnerApi api;
        private static ResultWriter receiver;

        [MenuItem("SubTerra/Tests/Run Prompt-B 95 Quest Reward Tests")]
        public static void RunFromMenu()
        {
            RunEditMode();
        }

        public static void RunEditMode()
        {
            ReleaseRunner();
            var path = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "Temp", "prompt-b95-editmode-results.txt"));
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
            receiver = new ResultWriter(path, "Prompt-B 95 EditMode");
            api.RegisterCallbacks(receiver);
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "SubTerra.App.Tests.EditMode" },
                groupNames = new[]
                {
                    "SubTerra.App.Tests.Tutorial.PromptB95QuestRewardTests",
                    "SubTerra.App.Tests.Tutorial.PromptB53QuestUiTests",
                    "SubTerra.App.Tests.Tutorial.DemoObjectiveTransitionTests",
                    "SubTerra.App.Tests.Tutorial.DemoObjectiveSaveRoundTripTests"
                }
            }));
            Debug.Log("[SubTerra] Prompt-B 95 EditMode tests requested → " + path);
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
                Debug.Log($"[SubTerra] Prompt-B 95 finished Pass={passed} Fail={failed} Skip={skipped}");
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
                    output.AppendLine("PASS: " + result.FullName);
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
                    output.AppendLine("SKIP: " + result.FullName);
                }
            }
        }
    }
}
#endif
