using SubTerra.App.UI.Progression;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>prompt-B 56: 업그레이드 창의 공통 제목만 제거한다.</summary>
    public static class PromptB56UpgradePanelBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [MenuItem("SubTerra/UI/Apply Prompt-B 56 Upgrade Panel")]
        public static void Apply()
        {
            var previousScenePath = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Single);
            var removedCount = 0;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var view in root.GetComponentsInChildren<ProgressionPanelView>(true))
                {
                    foreach (var label in view.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (label.text != "장비 업그레이드 [U]")
                        {
                            continue;
                        }

                        Object.DestroyImmediate(label.gameObject);
                        removedCount++;
                    }
                }
            }

            if (removedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (!string.IsNullOrEmpty(previousScenePath)
                && previousScenePath != IntegrationScenePath
                && AssetDatabase.LoadAssetAtPath<SceneAsset>(previousScenePath) != null)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }

            Debug.Log($"[SubTerra] prompt-B 56 upgrade title removed: {removedCount}");
        }
    }
}
