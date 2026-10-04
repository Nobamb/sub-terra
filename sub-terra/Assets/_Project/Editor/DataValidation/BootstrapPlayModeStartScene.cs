using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Editor
{
    /// <summary>
    /// 모든 작업자가 Play 시 Bootstrap부터 시작하도록 고정한다.
    /// Integration/SurfaceBase를 연 채 Play하면 HUD·입력이 영구 비활성 되는 환경 차이를 막는다.
    /// UTF PlayMode 실행 중에는 playModeStartScene이 InitTestScene을 Bootstrap으로 바꾸지 않게 한다.
    /// 그 교체가 일어나면 PlaymodeTestsController가 깨어나지 않아 Run All이 멈추거나,
    /// 정리 중 사라진 InitTestScene을 복원하려다 씬이 0개가 된다.
    /// </summary>
    [InitializeOnLoad]
    public static class BootstrapPlayModeStartScene
    {
        private const string BootstrapScenePath =
            "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";

        private const string MenuPath = "SubTerra/Play Mode/Use Bootstrap Start Scene";
        private const string PrefKey = "SubTerra.BootstrapPlayModeStartScene.Enabled";
        private const string InitTestSceneToken = "InitTestScene";

        private static bool callbackRequested;
        private static bool startSceneSuspended;
        private static MethodInfo utfRunActiveMethod;

        static BootstrapPlayModeStartScene()
        {
            EditorApplication.delayCall += RecoverIdleEditor;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
            TestRunnerApi.RegisterTestCallback(new TestRunnerCallbacks());
            utfRunActiveMethod = typeof(TestRunnerApi).GetMethod(
                "IsRunActive",
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                // Unity는 이 콜백 이후에 playModeStartScene을 연다.
                // 여기서 비우지 않으면 테스트 씬 대신 Bootstrap이 로드된다.
                if (ShouldSuspendStartScene() || IsInitTestSceneLoaded())
                {
                    startSceneSuspended = true;
                    ClearStartScene();
                }
            }
            else if (change == PlayModeStateChange.EnteredEditMode && !ShouldSuspendStartScene())
            {
                callbackRequested = false;
                startSceneSuspended = false;
                ApplyPreference();
            }
        }

        private static void OnEditorUpdate()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (ShouldSuspendStartScene() || IsInitTestSceneLoaded())
                {
                    startSceneSuspended = true;
                    ClearStartScene();
                }

                return;
            }

            if (ShouldSuspendStartScene())
            {
                startSceneSuspended = true;
                ClearStartScene();
                return;
            }

            if (EditorSceneManager.sceneCount == 0 || IsInitTestSceneLoaded())
            {
                RecoverIdleEditor();
                return;
            }

            if (!startSceneSuspended)
            {
                return;
            }

            startSceneSuspended = false;
            callbackRequested = false;
            ApplyPreference();
        }

        private static void RecoverIdleEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || ShouldSuspendStartScene())
            {
                return;
            }

            if (EditorSceneManager.sceneCount == 0 || IsInitTestSceneLoaded())
            {
                OpenBootstrapScene();
            }

            startSceneSuspended = false;
            callbackRequested = false;
            ApplyPreference();
        }

        [MenuItem(MenuPath)]
        private static void TogglePreference()
        {
            var enabled = !IsEnabled();
            EditorPrefs.SetBool(PrefKey, enabled);
            ApplyPreference();
            Debug.Log(
                enabled
                    ? "[SubTerra] Play Mode Start Scene = Bootstrap (team default)."
                    : "[SubTerra] Play Mode Start Scene cleared (current open scene).");
        }

        [MenuItem(MenuPath, true)]
        private static bool TogglePreferenceValidate()
        {
            Menu.SetChecked(MenuPath, IsEnabled());
            return true;
        }

        private static bool IsEnabled()
        {
            // 기본값 true: 새 클론/새 머신에서도 Bootstrap 경로를 강제한다.
            return EditorPrefs.GetBool(PrefKey, true);
        }

        private static bool ShouldSuspendStartScene()
        {
            // InitTestScene이 열려 있다는 이유만으로 잠시 두지 않는다.
            // 실패한 런이 남긴 씬이면 일반 Play가 Bootstrap으로 돌아가야 한다.
            return callbackRequested
                || DataValidation.TestValidationRunner.IsRunning
                || IsUtfRunActive();
        }

        private static bool IsUtfRunActive()
        {
            if (utfRunActiveMethod == null)
            {
                return false;
            }

            try
            {
                return (bool)utfRunActiveMethod.Invoke(null, null);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsInitTestSceneLoaded()
        {
            var count = EditorSceneManager.sceneCount;
            for (var i = 0; i < count; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (IsInitTestScene(scene.path) || IsInitTestScene(scene.name))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInitTestScene(string value)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(InitTestSceneToken, StringComparison.Ordinal) >= 0;
        }

        private static void ClearStartScene()
        {
            if (EditorSceneManager.playModeStartScene == null)
            {
                return;
            }

            EditorSceneManager.playModeStartScene = null;
        }

        private static void ApplyPreference()
        {
            if (ShouldSuspendStartScene())
            {
                ClearStartScene();
                return;
            }

            if (!IsEnabled())
            {
                ClearStartScene();
                return;
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
            if (sceneAsset == null)
            {
                Debug.LogWarning(
                    "[SubTerra] Bootstrap scene not found at " + BootstrapScenePath
                    + ". Play Mode Start Scene was not set.");
                return;
            }

            if (EditorSceneManager.playModeStartScene != sceneAsset)
            {
                EditorSceneManager.playModeStartScene = sceneAsset;
            }
        }

        private static void OpenBootstrapScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath) == null)
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                return;
            }

            EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
        }

        private sealed class TestRunnerCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                callbackRequested = true;
                startSceneSuspended = true;
                ClearStartScene();
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                callbackRequested = false;
                EditorApplication.delayCall += OnEditorUpdate;
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }
    }
}
