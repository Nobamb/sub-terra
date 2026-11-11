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
    /// B-193 낙석 피해·착지 가스·디버그 명령 검사만 돌린다.
    /// 메뉴나 Temp 플래그(내용: 비우거나 검사 이름 정규식)로 실행하고 결과는 Temp/*.done에 쓴다.
    /// </summary>
    public static class PromptB193CollapseDamageTestRunner
    {
        private const string PlayFlagPath = "Temp/subterra-prompt-b193-playmode.flag";
        private const string PlayDonePath = "Temp/subterra-prompt-b193-playmode.done";
        private const string GameplayFlagPath = "Temp/subterra-prompt-b193-gameplay.flag";
        private const string GameplayDonePath = "Temp/subterra-prompt-b193-gameplay.done";
        private const string EditFlagPath = "Temp/subterra-prompt-b193-editmode.flag";
        private const string EditDonePath = "Temp/subterra-prompt-b193-editmode.done";
        private static readonly string[] PlayFixtures =
        {
            "SubTerra.App.Tests.PlayMode.Issue289CollapseDamagePlayModeTests"
        };
        private static readonly string[] EditFixtures =
        {
            "SubTerra.App.Tests.Debug.PromptB193DebugCommandTests",
            "SubTerra.App.Tests.Debug.PromptB125SpawnCommandTests",
            "SubTerra.App.Tests.Integration.Issue289LandingGasTests",
            "SubTerra.App.Tests.Integration.IntegrationWiringTests"
        };
        private const string BootstrapPrefKey = "SubTerra.BootstrapPlayModeStartScene.Enabled";
        private static TestRunnerApi activeApi;

        [InitializeOnLoadMethod]
        private static void WatchFlag()
        {
            EditorApplication.update -= PollFlag;
            EditorApplication.update += PollFlag;
        }

        [MenuItem("SubTerra/Tests/Run Prompt-B 193 Collapse Damage PlayMode Tests")]
        public static void RunPlayFromMenu() => Run(TestMode.PlayMode, null);

        [MenuItem("SubTerra/Tests/Run Prompt-B 193 Gameplay PlayMode Regression Tests")]
        public static void RunGameplayFromMenu() => Run(TestMode.PlayMode, null, true);

        [MenuItem("SubTerra/Tests/Run Prompt-B 193 Debug Command Edit Mode Tests")]
        public static void RunEditFromMenu() => Run(TestMode.EditMode, null);

        private static void PollFlag()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Poll(PlayFlagPath, PlayDonePath, TestMode.PlayMode);
            Poll(GameplayFlagPath, GameplayDonePath, TestMode.PlayMode, true);
            Poll(EditFlagPath, EditDonePath, TestMode.EditMode);
        }

        private static void Poll(string flagPath, string donePath, TestMode mode, bool gameplay = false)
        {
            if (!File.Exists(flagPath))
            {
                return;
            }

            try
            {
                var filter = File.ReadAllText(flagPath).Trim();
                File.Delete(flagPath);
                Run(mode, string.IsNullOrEmpty(filter) ? null : filter, gameplay);
            }
            catch (Exception exception)
            {
                File.WriteAllText(donePath, "FAIL " + exception.GetType().Name + " " + exception.Message);
            }
        }

        private static void Run(TestMode mode, string testPattern, bool gameplay = false)
        {
            var donePath = gameplay ? GameplayDonePath : mode == TestMode.PlayMode ? PlayDonePath : EditDonePath;
            if (activeApi != null)
            {
                File.WriteAllText(donePath, "BUSY");
                return;
            }

            if (File.Exists(donePath)) File.Delete(donePath);
            var previousStartScene = EditorSceneManager.playModeStartScene;
            var previousBootstrapPref = EditorPrefs.GetBool(BootstrapPrefKey, true);
            if (mode == TestMode.PlayMode)
            {
                EditorPrefs.SetBool(BootstrapPrefKey, false);
                EditorSceneManager.playModeStartScene = null;
            }

            activeApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            activeApi.RegisterCallbacks(new ResultWriter(donePath, () =>
            {
                if (mode == TestMode.PlayMode)
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
            var fixtures = mode == TestMode.PlayMode ? PlayFixtures : EditFixtures;
            string[] groups = null;
            string[] assemblies;
            if (gameplay)
            {
                // 기존 붕괴·생존·채굴 PlayMode 회귀. 어셈블리 전체를 돌린다.
                assemblies = new[]
                {
                    "SubTerra.Gameplay.Structural.PlayModeTests",
                    "SubTerra.Gameplay.Player.PlayModeTests",
                    "SubTerra.Gameplay.Mining.PlayModeTests"
                };
            }
            else
            {
                assemblies = new[]
                {
                    mode == TestMode.PlayMode ? "SubTerra.App.Tests.PlayMode" : "SubTerra.App.Tests.EditMode"
                };
                groups = new string[fixtures.Length];
                for (var i = 0; i < fixtures.Length; i++)
                {
                    groups[i] = "^" + fixtures[i].Replace(".", "\\.") + "\\." + (testPattern ?? string.Empty);
                }
            }

            activeApi.Execute(new ExecutionSettings(new Filter
            {
                testMode = mode,
                assemblyNames = assemblies,
                groupNames = groups
            }));
        }

        private sealed class ResultWriter : ICallbacks
        {
            private readonly string donePath;
            private readonly Action onFinished;
            private readonly StringBuilder lines = new StringBuilder();
            private int pass;
            private int fail;

            public ResultWriter(string path, Action finished)
            {
                donePath = path;
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
                File.WriteAllText(donePath, lines.ToString());
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
                if (!string.IsNullOrEmpty(result.StackTrace))
                {
                    var stack = result.StackTrace.Split('\n');
                    for (var i = 0; i < stack.Length && i < 6; i++)
                    {
                        lines.AppendLine("    " + stack[i].Trim());
                    }
                }
            }
        }
    }
}
