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
    /// <summary>
    /// 프롬프트 B-140 가이드 개편 전용 테스트 러너.
    /// 메뉴 또는 Temp/subterra-run-prompt-b140.flag(내용: edit | play)로 실행하고 Temp/prompt-b140-*-results.txt에 결과를 쓴다.
    /// </summary>
    public static class PromptB140GameGuideTestRunner
    {
        private static readonly string[] EditGroups =
        {
            "SubTerra.App.Tests.UI.PromptB140GameGuideTests",
            "SubTerra.App.Tests.UI.GameGuidePanelTests"
        };

        private static TestRunnerApi api;
        private static ResultWriter receiver;
        private static SceneAsset previousStartScene;

        [InitializeOnLoadMethod]
        private static void WatchRequest()
        {
            EditorApplication.update += () =>
            {
                const string flag = "Temp/subterra-run-prompt-b140.flag";
                if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(flag)) return;
                var mode = File.ReadAllText(flag).Trim();
                File.Delete(flag);
                Run(mode == "play" || mode == "playall", mode == "playall");
            };
        }

        [MenuItem("SubTerra/Tests/Run Prompt-B 140 Game Guide Edit Mode Tests")]
        public static void RunEditFromMenu()
        {
            Run(false, false);
        }

        [MenuItem("SubTerra/Tests/Run Prompt-B 140 Game Guide Play Mode Tests")]
        public static void RunPlayFromMenu()
        {
            Run(true, false);
        }

        private static void Run(bool runPlayMode, bool allPlayMode)
        {
            Release();
            if (runPlayMode)
            {
                previousStartScene = EditorSceneManager.playModeStartScene;
                EditorSceneManager.playModeStartScene = null;
            }

            var path = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "Temp", runPlayMode ? "prompt-b140-playmode-results.txt" : "prompt-b140-editmode-results.txt"));
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".done")) File.Delete(path + ".done");

            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            receiver = new ResultWriter(path, runPlayMode ? "Prompt-B 140 PlayMode" : "Prompt-B 140 EditMode");
            api.RegisterCallbacks(receiver);
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = runPlayMode ? TestMode.PlayMode : TestMode.EditMode,
                assemblyNames = new[] { runPlayMode ? "SubTerra.App.Tests.PlayMode" : "SubTerra.App.Tests.EditMode" },
                groupNames = runPlayMode
                    ? (allPlayMode ? null : new[] { "SubTerra.App.Tests.PlayMode.PromptB140GameGuidePlayModeTests" })
                    : EditGroups
            }));
            Debug.Log("[SubTerra] Prompt-B 140 tests requested → " + path);
        }

        private static void Release(ResultWriter completed = null)
        {
            if (completed != null && completed != receiver) return;
            if (api != null && receiver != null) api.UnregisterCallbacks(receiver);
            if (api != null) UnityEngine.Object.DestroyImmediate(api);
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

            public ResultWriter(string resultPath, string label)
            {
                path = resultPath;
                output.AppendLine(label);
                output.AppendLine("Started: " + DateTime.Now.ToString("o"));
            }

            public void RunStarted(ITestAdaptor testsToRun) => output.AppendLine("RunStarted: " + testsToRun.Name);

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

                Debug.Log($"[SubTerra] Prompt-B 140 finished Pass={passed} Fail={failed} Skip={skipped}");
                EditorApplication.delayCall += () => Release(this);
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren) return;
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
