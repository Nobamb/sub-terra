using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>B-175 점프 관련 PlayMode 검사(이동 물리·코요테·버퍼, 그림 교체 동작, 사다리 위 내려오기)만 돌린다.</summary>
    public static class PromptB175JumpTestRunner
    {
        private const string FlagPath = "Temp/subterra-prompt-b175-playmode.flag";
        private const string DonePath = "Temp/subterra-prompt-b175-playmode.done";
        private const string BootstrapPrefKey = "SubTerra.BootstrapPlayModeStartScene.Enabled";
        private static readonly string[] Fixtures =
        {
            "SubTerra.Gameplay.Player.Tests.PlayerMovementPlayModeTests",
            "SubTerra.App.Tests.PlayMode.MineDemo.PlayerRedrawnActionPlayModeTests",
            "SubTerra.App.Tests.PlayMode.MineDemo.LadderBackBootstrapPlayModeTests"
        };
        private static TestRunnerApi activeApi;

        [InitializeOnLoadMethod]
        private static void WatchFlag()
        {
            EditorApplication.update -= PollFlag;
            EditorApplication.update += PollFlag;
        }

        [MenuItem("SubTerra/Tests/Run Prompt-B 175 Jump PlayMode Tests")]
        public static void RunFromMenu() => Run();

        private static void PollFlag()
        {
            if (!File.Exists(FlagPath) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            try
            {
                File.Delete(FlagPath);
                Run();
            }
            catch (Exception exception)
            {
                File.WriteAllText(DonePath, "FAIL " + exception.GetType().Name + " " + exception.Message);
            }
        }

        private static void Run()
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
            var groups = new string[Fixtures.Length];
            for (int i = 0; i < Fixtures.Length; i++) groups[i] = "^" + Fixtures[i].Replace(".", "\\.") + "\\.";
            activeApi.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.PlayMode,
                assemblyNames = new[] { "SubTerra.Gameplay.Player.PlayModeTests", "SubTerra.App.Tests.PlayMode" },
                groupNames = groups
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
