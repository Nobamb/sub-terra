#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// TMP 글자가 안 보이는 캔버스에 TexCoord1/Normal/Tangent 채널을 켠다.
    /// 대상: MainMenu, HUDCanvas, MainMenu Settings, SurfaceBase, Integration HUD.
    /// </summary>
    public static class PromptB50TmpVisibilityBuilder
    {
        public const AdditionalCanvasShaderChannels TmpChannels =
            AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;

        private const string MainMenuScene = "Assets/_Project/Scenes/App/MainMenu.unity";
        private const string IntegrationScene = "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string HudPrefab = "Assets/_Project/Prefabs/UI/HUDCanvas.prefab";
        private const string MainMenuPrefab = "Assets/_Project/Prefabs/UI/MainMenuPanel.prefab";
        private const string SurfaceBasePrefab = "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";

        [MenuItem("SubTerra/UI/Build Prompt-B 50 TMP Visibility")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            ApplyPrefab(HudPrefab);
            ApplyPrefab(MainMenuPrefab);
            ApplyPrefab(SurfaceBasePrefab);
            ApplyScene(MainMenuScene);
            ApplyScene(IntegrationScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "TMP canvas shader channels applied.";
        }

        private static void ApplyPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ApplyToCanvases(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyScene(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                for (var i = 0; i < roots.Length; i++)
                {
                    ApplyToCanvases(roots[i]);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded && SceneManager.sceneCount > 1)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ApplyToCanvases(GameObject root)
        {
            var canvases = root.GetComponentsInChildren<Canvas>(true);
            for (var i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas.additionalShaderChannels == TmpChannels)
                {
                    continue;
                }

                canvas.additionalShaderChannels = TmpChannels;
                EditorUtility.SetDirty(canvas);
            }
        }
    }
}
#endif
