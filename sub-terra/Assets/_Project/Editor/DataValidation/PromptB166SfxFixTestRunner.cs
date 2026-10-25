using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>B-166 PlayMode 검사만 돌린다. 메뉴나 Temp 플래그(내용: 비우거나 검사 이름 정규식)로 실행한다.</summary>
    public static class PromptB166SfxFixTestRunner
    {
        private const string FlagPath = "Temp/subterra-prompt-b166-playmode.flag";
        private const string DonePath = "Temp/subterra-prompt-b166-playmode.done";
        private const string Fixture = "SubTerra.App.Tests.PlayMode.Traversal.PromptB166SfxFixPlayModeTests";
        private const string BootstrapPrefKey = "SubTerra.BootstrapPlayModeStartScene.Enabled";
        private static TestRunnerApi activeApi;

        [InitializeOnLoadMethod]
        private static void WatchFlag()
        {
            EditorApplication.update -= PollFlag;
            EditorApplication.update += PollFlag;
        }

        [MenuItem("SubTerra/Tests/Run Prompt-B 166 Sfx Fix PlayMode Tests")]
        public static void RunFromMenu() => Run(null);

        private static void PollFlag()
        {
            if (!File.Exists(FlagPath) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            try
            {
                var filter = File.ReadAllText(FlagPath).Trim();
                File.Delete(FlagPath);
                Run(string.IsNullOrEmpty(filter) ? null : filter);
            }
            catch (Exception exception)
            {
                File.WriteAllText(DonePath, "FAIL " + exception.GetType().Name + " " + exception.Message);
            }
        }

        private static void Run(string testPattern)
        {
            if (activeApi != null)
            {
                File.WriteAllText(DonePath, "BUSY");
                return;
            }

            if (File.Exists(DonePath)) File.Delete(DonePath);
            var previousStartScene = EditorSceneManager.playModeStartScene;
            var previousBootstrapPref = EditorPrefs.GetBool(BootstrapPrefKey, true);
            EditorPrefs.SetBool(BootstrapPrefKey, false);
            EditorSceneManager.playModeStartScene = null;
            activeApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            activeApi.RegisterCallbacks(new ResultWriter(() =>
            {
                EditorPrefs.SetBool(BootstrapPrefKey, previousBootstrapPref);
                EditorSceneManager.playModeStartScene = previousStartScene;
                if (activeApi != null)
                {
                    UnityEngine.Object.DestroyImmediate(activeApi);
                    activeApi = null;
                }
            }));
            var group = "^" + Fixture.Replace(".", "\\.") + "\\." + (testPattern ?? string.Empty);
            activeApi.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.PlayMode,
                assemblyNames = new[] { "SubTerra.App.Tests.PlayMode" },
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
                    lines.AppendLine("PASS: " + result.Name + " (" + result.Duration.ToString("F1") + "s)");
                    return;
                }

                if (result.TestStatus == TestStatus.Failed) fail++;
                lines.AppendLine(result.TestStatus.ToString().ToUpperInvariant() + ": " + result.Name);
                lines.AppendLine("  " + result.Message);
            }
        }
    }
}
