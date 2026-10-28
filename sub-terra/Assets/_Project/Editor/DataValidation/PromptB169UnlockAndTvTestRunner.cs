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
    /// B-169 검사만 돌린다. 메뉴나 Temp 플래그로 실행한다.
    /// 플래그 첫 줄: edit 또는 play. 둘째 줄(선택): 검사 이름 정규식(비우면 B-169 묶음). 이웃 묶음을 함께 돌릴 때 쓴다.
    /// </summary>
    public static class PromptB169UnlockAndTvTestRunner
    {
        private const string FlagPath = "Temp/subterra-prompt-b169.flag";
        private const string DonePath = "Temp/subterra-prompt-b169.done";
        private const string EditFixture = "SubTerra.App.Tests.UI.PromptB169UnlockAndTvTests";
        private const string PlayFixture = "SubTerra.App.Tests.PlayMode.Audio.PromptB169UnlockAndTvPlayModeTests";
        private const string BootstrapPrefKey = "SubTerra.BootstrapPlayModeStartScene.Enabled";
        private static TestRunnerApi activeApi;

        [InitializeOnLoadMethod]
        private static void WatchFlag()
        {
            EditorApplication.update -= PollFlag;
            EditorApplication.update += PollFlag;
        }

        [MenuItem("SubTerra/Tests/Run Prompt-B 169 Unlock And TV EditMode Tests")]
        public static void RunEditFromMenu() => Run(false, null);

        [MenuItem("SubTerra/Tests/Run Prompt-B 169 Unlock And TV PlayMode Tests")]
        public static void RunPlayFromMenu() => Run(true, null);

        private static void PollFlag()
        {
            if (!File.Exists(FlagPath) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            try
            {
                var lines = File.ReadAllText(FlagPath).Replace("\r", string.Empty).Split('\n');
                File.Delete(FlagPath);
                var play = lines.Length > 0 && lines[0].Trim() == "play";
                var group = lines.Length > 1 ? lines[1].Trim() : string.Empty;
                Run(play, string.IsNullOrEmpty(group) ? null : group);
            }
            catch (Exception exception)
            {
                File.WriteAllText(DonePath, "FAIL " + exception.GetType().Name + " " + exception.Message);
            }
        }

        private static void Run(bool play, string groupPattern)
        {
            if (activeApi != null)
            {
                File.WriteAllText(DonePath, "BUSY");
                return;
            }

            if (File.Exists(DonePath)) File.Delete(DonePath);
            var previousStartScene = EditorSceneManager.playModeStartScene;
            var previousBootstrapPref = EditorPrefs.GetBool(BootstrapPrefKey, true);
            if (play)
            {
                EditorPrefs.SetBool(BootstrapPrefKey, false);
                EditorSceneManager.playModeStartScene = null;
            }

            activeApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            activeApi.RegisterCallbacks(new ResultWriter(() =>
            {
                if (play)
                {
                    EditorPrefs.SetBool(BootstrapPrefKey, previousBootstrapPref);
                    EditorSceneManager.playModeStartScene = previousStartScene;
                }

                if (activeApi != null)
                {
                    UnityEngine.Object.DestroyImmediate(activeApi);
                    activeApi = null;
                }
            }));
            var fixture = play ? PlayFixture : EditFixture;
            var group = groupPattern ?? "^" + fixture.Replace(".", "\\.") + "\\.";
            activeApi.Execute(new ExecutionSettings(new Filter
            {
                testMode = play ? TestMode.PlayMode : TestMode.EditMode,
                assemblyNames = new[] { play ? "SubTerra.App.Tests.PlayMode" : "SubTerra.App.Tests.EditMode" },
                groupNames = new[] { group }
            }));
        }

        private sealed class ResultWriter : ICallbacks
        {
            private readonly Action onFinished;
            private readonly StringBuilder lines = new StringBuilder();
            private int pass;
            private int fail;

            public ResultWriter(Action finished)
            {
                onFinished = finished;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                lines.AppendLine("Started " + DateTime.Now.ToString("o"));
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                lines.AppendLine("Result: " + result.TestStatus);
                lines.AppendLine("Pass: " + pass + " Fail: " + fail);
                File.WriteAllText(DonePath, lines.ToString());
                onFinished?.Invoke();
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren) return;
                if (result.TestStatus == TestStatus.Passed)
                {
                    pass++;
                    lines.AppendLine("PASS: " + result.FullName + " (" + result.Duration.ToString("F1") + "s)");
                    return;
                }

                if (result.TestStatus == TestStatus.Failed) fail++;
                lines.AppendLine(result.TestStatus.ToString().ToUpperInvariant() + ": " + result.FullName);
                lines.AppendLine("  " + result.Message);
            }
        }
    }
}
