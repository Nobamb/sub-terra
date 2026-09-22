#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>prompt-B 105 메뉴 연결과 전환을 검증한다.</summary>
    public static class PromptB105SideMenuTestRunner
    {
        [InitializeOnLoadMethod]
        private static void WatchRequest()
        {
            EditorApplication.update += () =>
            {
                const string flag = "Temp/subterra-run-prompt-b105.flag";
                if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(flag)) return;
                bool play = File.ReadAllText(flag).Trim() == "play";
                File.Delete(flag);
                Run(play);
            };
        }

        private static TestRunnerApi api;
        private static ResultWriter receiver;
        private static SceneAsset previousStartScene;

        [MenuItem("SubTerra/Tests/Run Prompt-B 105 Side Menu Tests")]
        public static void RunFromMenu()
        {
            Run(false);
        }

        private static void Run(bool runPlayMode)
        {
            ReleaseRunner();
            if (runPlayMode)
            {
                previousStartScene = EditorSceneManager.playModeStartScene;
                EditorSceneManager.playModeStartScene = null;
            }
            var path = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "Temp", runPlayMode ? "prompt-b105-playmode-results.txt" : "prompt-b105-editmode-results.txt"));
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
            receiver = new ResultWriter(path, runPlayMode ? "Prompt-B 105 PlayMode" : "Prompt-B 105 EditMode");
            api.RegisterCallbacks(receiver);
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = runPlayMode ? TestMode.PlayMode : TestMode.EditMode,
                assemblyNames = new[] { runPlayMode ? "SubTerra.App.Tests.PlayMode" : "SubTerra.App.Tests.EditMode" },
                groupNames = runPlayMode ? new[] { "SubTerra.App.Tests.PlayMode.PromptB105SideMenuPlayModeTests" } : new[] { "SubTerra.App.Tests.UI.PromptB105SideMenuTests", "SubTerra.App.Tests.UI.PromptB89UndergroundMenuTests" }
            }));
            Debug.Log("[SubTerra] Prompt-B 105 tests requested → " + path);
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
                if (previousStartScene != null)
                {
                    EditorSceneManager.playModeStartScene = previousStartScene;
                    previousStartScene = null;
                }
                Debug.Log($"[SubTerra] Prompt-B 105 finished Pass={passed} Fail={failed} Skip={skipped}");
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

