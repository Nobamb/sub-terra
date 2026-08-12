using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SubTerra.App.Editor
{
    /// <summary>
    /// 모든 작업자가 Play 시 Bootstrap부터 시작하도록 고정한다.
    /// Integration/SurfaceBase를 연 채 Play하면 HUD·입력이 영구 비활성 되는 환경 차이를 막는다.
    /// </summary>
    [InitializeOnLoad]
    public static class BootstrapPlayModeStartScene
    {
        private const string BootstrapScenePath =
            "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";

        private const string MenuPath = "SubTerra/Play Mode/Use Bootstrap Start Scene";
        private const string PrefKey = "SubTerra.BootstrapPlayModeStartScene.Enabled";

        static BootstrapPlayModeStartScene()
        {
            EditorApplication.delayCall += ApplyPreference;
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

        private static void ApplyPreference()
        {
            if (!IsEnabled())
            {
                if (EditorSceneManager.playModeStartScene != null
                    && AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)
                        == BootstrapScenePath)
                {
                    EditorSceneManager.playModeStartScene = null;
                }

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
    }
}
