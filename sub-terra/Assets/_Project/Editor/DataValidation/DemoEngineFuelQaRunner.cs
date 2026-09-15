using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>엔진 연료 집중 검증 동안만 Bootstrap 시작 씬 강제를 해제하고 원래 설정을 복원한다.</summary>
    public static class DemoEngineFuelQaRunner
    {
        private const string BootstrapPreference = "SubTerra.BootstrapPlayModeStartScene.Enabled";
        private static TestRunnerApi api;
        private static SceneAsset previousStartScene;
        private static bool previousPreference;
        private static bool hadPreference;
        private static bool preferencesCaptured;
        private static double deadline;
        private static string resultPath;

        public static void RunPlayMode()
        {
            try
            {
                var testNames = new[]
                {
                    "SubTerra.Gameplay.Mining.Tests.MiningSystemPlayModeTests",
                    "SubTerra.App.Tests.PlayMode.MineDemo.PromptB79MiningFailureHudPlayModeTests"
                };
                resultPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "engine-fuel-playmode.xml"));
                var args = Environment.GetCommandLineArgs();
                for (var i = 0; i + 1 < args.Length; i++)
                {
                    if (args[i] == "-fuelQaResults") resultPath = Path.GetFullPath(args[i + 1]);
                    if (args[i] == "-fuelQaTest") testNames = new[] { args[i + 1] };
                }
                previousStartScene = EditorSceneManager.playModeStartScene;
                hadPreference = EditorPrefs.HasKey(BootstrapPreference);
                previousPreference = EditorPrefs.GetBool(BootstrapPreference, true);
                preferencesCaptured = true;
                EditorApplication.quitting += RestorePreferences;
                EditorPrefs.SetBool(BootstrapPreference, false);
                EditorSceneManager.playModeStartScene = null;
                deadline = EditorApplication.timeSinceStartup + 300d;
                EditorApplication.update += CheckTimeout;
                api = ScriptableObject.CreateInstance<TestRunnerApi>();
                api.RegisterCallbacks(new ResultWriter());
                api.Execute(new ExecutionSettings(new Filter
                {
                    testMode = TestMode.PlayMode,
                    testNames = testNames
                }));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish(2);
            }
        }

        private static void CheckTimeout()
        {
            if (EditorApplication.timeSinceStartup < deadline) return;
            Debug.LogError("[SubTerra QA] Engine fuel PlayMode tests timed out.");
            Finish(2);
        }

        private static void RestorePreferences()
        {
            if (!preferencesCaptured) return;
            preferencesCaptured = false;
            if (hadPreference) EditorPrefs.SetBool(BootstrapPreference, previousPreference);
            else EditorPrefs.DeleteKey(BootstrapPreference);
            EditorSceneManager.playModeStartScene = previousStartScene;
        }

        private static void Finish(int exitCode)
        {
            EditorApplication.update -= CheckTimeout;
            EditorApplication.quitting -= RestorePreferences;
            RestorePreferences();
            EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
        }

        private sealed class ResultWriter : ICallbacks
        {
            private int passed;
            private int failed;
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren) return;
                if (result.TestStatus == TestStatus.Passed) passed++;
                if (result.TestStatus == TestStatus.Failed) failed++;
            }
            public void RunFinished(ITestResultAdaptor result)
            {
                var exitCode = failed == 0 && passed > 0 ? 0 : 2;
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
                    TestRunnerApi.SaveResultToFile(result, resultPath);
                    Debug.Log($"[SubTerra QA] Engine fuel PlayMode: passed={passed}, failed={failed}.");
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    exitCode = 2;
                }
                finally
                {
                    Finish(exitCode);
                }
            }
        }
    }
}
