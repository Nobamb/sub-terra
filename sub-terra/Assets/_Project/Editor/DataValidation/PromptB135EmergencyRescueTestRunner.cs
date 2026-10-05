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
    /// <summary>prompt-B 135 전력 고갈 구출 팝업·홀로그램 안내의 구조·연출·실제 Scene 동작을 검증한다.</summary>
    public static class PromptB135EmergencyRescueTestRunner
    {
        [InitializeOnLoadMethod]
        private static void WatchRequest()
        {
            EditorApplication.update += () =>
            {
                const string flag = "Temp/subterra-run-prompt-b135.flag";
                if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(flag)) return;
                var mode = File.ReadAllText(flag).Trim();
                File.Delete(flag);
                Run(mode == "play" || mode == "playall", mode == "playall");
            };
        }

        private static TestRunnerApi api;
        private static ResultWriter receiver;
        private static SceneAsset previousStartScene;

        [MenuItem("SubTerra/Tests/Run Prompt-B 135 Emergency Rescue Edit Mode Tests")]
        public static void RunEditFromMenu()
        {
            Run(false);
        }

        [MenuItem("SubTerra/Tests/Run Prompt-B 135 Emergency Rescue Play Mode Tests")]
        public static void RunPlayFromMenu()
        {
            Run(true);
        }

        private static void Run(bool runPlayMode, bool allPlayMode = false)
        {
            ReleaseRunner();
            if (runPlayMode)
            {
                previousStartScene = EditorSceneManager.playModeStartScene;
                EditorSceneManager.playModeStartScene = null;
            }

            var path = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "Temp", runPlayMode ? "prompt-b135-playmode-results.txt" : "prompt-b135-editmode-results.txt"));
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
            receiver = new ResultWriter(path, runPlayMode ? "Prompt-B 135 PlayMode" : "Prompt-B 135 EditMode");
            api.RegisterCallbacks(receiver);
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = runPlayMode ? TestMode.PlayMode : TestMode.EditMode,
                assemblyNames = new[] { runPlayMode ? "SubTerra.App.Tests.PlayMode" : "SubTerra.App.Tests.EditMode" },
                groupNames = runPlayMode && allPlayMode
                    ? null
                    : runPlayMode
                    ? new[] { "SubTerra.App.Tests.PlayMode.EmergencyRescuePlayModeTests" }
                    : new[]
                    {
                        "SubTerra.App.Tests.UI.PromptB135EmergencyRescueUiTests",
                        "SubTerra.App.Tests.UI.PromptB85EmergencyRescueChipTests",
                        "SubTerra.App.Tests.Run.EmergencyRescueServiceTests"
                    }
            }));
            Debug.Log("[SubTerra] Prompt-B 135 tests requested → " + path);
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

                Debug.Log($"[SubTerra] Prompt-B 135 finished Pass={passed} Fail={failed} Skip={skipped}");
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
