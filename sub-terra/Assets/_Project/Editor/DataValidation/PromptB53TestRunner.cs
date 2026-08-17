#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>prompt-B 53 퀘스트 순서·완료·상세창 관련 테스트만 실행한다.</summary>
    public static class PromptB53TestRunner
    {
        private static TestRunnerApi api;
        private static ResultWriter receiver;

        [MenuItem("SubTerra/Tests/Run Prompt-B 53 Quest EditMode Tests")]
        public static void RunEditMode()
        {
            Run(
                TestMode.EditMode,
                "prompt-b53-editmode-results.txt",
                new[] { "SubTerra.App.Tests.EditMode" },
                new[]
                {
                    "SubTerra.App.Tests.Tutorial.DemoObjectiveTransitionTests",
                    "SubTerra.App.Tests.Tutorial.DemoObjectiveSaveRoundTripTests",
                    "SubTerra.App.Tests.Tutorial.DemoDeepZoneUnlockPathTests",
                    "SubTerra.App.Tests.Tutorial.PromptB53QuestUiTests",
                    "SubTerra.App.Tests.Progression.ProgressionServiceTests"
                });
        }

        [MenuItem("SubTerra/Tests/Run Prompt-B 53 Quest PlayMode Tests")]
        public static void RunPlayMode()
        {
            Run(
                TestMode.PlayMode,
                "prompt-b53-playmode-results.txt",
                new[] { "SubTerra.App.Tests.PlayMode" },
                new[]
                {
                    "SubTerra.App.Tests.PlayMode.DemoFlow.DemoFlowPlayModeTests"
                });
        }

        private static void Run(
            TestMode mode,
            string fileName,
            string[] assemblyNames,
            string[] groupNames)
        {
            ReleaseRunner();
            var path = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "Temp",
                fileName));
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
                testMode = mode,
                assemblyNames = assemblyNames,
                groupNames = groupNames
            }));
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
                Object.DestroyImmediate(api);
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
                var output = new StringBuilder();
                output.AppendLine(result.TestStatus + " pass=" + result.PassCount
                    + " fail=" + result.FailCount + " skip=" + result.SkipCount);
                Append(result, output, 0);
                File.WriteAllText(path, output.ToString());
                File.WriteAllText(path + ".done", result.TestStatus.ToString());
                Debug.Log("[SubTerra] Prompt-B 53 tests finished: " + result.TestStatus
                    + " fail=" + result.FailCount);
                ReleaseRunner(this);
            }

            private static void Append(
                ITestResultAdaptor result,
                StringBuilder output,
                int depth)
            {
                if (!result.HasChildren)
                {
                    output.Append(' ', depth * 2);
                    output.Append(result.TestStatus);
                    output.Append(' ');
                    output.AppendLine(result.FullName);
                    if (result.TestStatus == TestStatus.Failed)
                    {
                        output.AppendLine(result.Message ?? string.Empty);
                    }
                }

                foreach (var child in result.Children)
                {
                    Append(child, output, depth + 1);
                }
            }
        }
    }
}
#endif
