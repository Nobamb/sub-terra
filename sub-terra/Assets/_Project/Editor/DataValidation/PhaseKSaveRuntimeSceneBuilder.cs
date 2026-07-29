using System.Collections.Generic;
using System.Linq;
using SubTerra.App.Core;
using SubTerra.App.Drone;
using SubTerra.App.Save;
using SubTerra.App.UI.Save;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    public static class PhaseKSaveRuntimeSceneBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string IntegrationSourcePath =
            "Assets/_Project/Scenes/Test/Gameplay/Gameplay_Snapshot_Test.unity";
        private const string BootstrapScenePath =
            "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        private const string MainMenuScenePath =
            "Assets/_Project/Scenes/App/MainMenu.unity";
        private const string DroneSettingsPath =
            "Assets/_Project/Data/Drone/DroneAnalysisSettings.asset";

        [MenuItem("SubTerra/Save/Build Phase K Runtime Scenes")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            var previousScene = SceneManager.GetActiveScene().path;
            PhaseKSaveSlotPrefabBuilder.Build();
            EnsureIntegrationScene();
            WireBootstrap();
            WireMainMenu();
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!string.IsNullOrEmpty(previousScene))
            {
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
            }

            return "Phase K runtime scenes wired.";
        }

        private static void EnsureIntegrationScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(IntegrationScenePath) != null)
            {
                return;
            }

            AssetDatabase.CopyAsset(IntegrationSourcePath, IntegrationScenePath);
        }

        private static void WireBootstrap()
        {
            var scene = EditorSceneManager.OpenScene(
                BootstrapScenePath,
                OpenSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<GameBootstrapper>();
            if (bootstrap == null)
            {
                throw new System.InvalidOperationException("GameBootstrapper missing.");
            }

            var runtime = bootstrap.GetComponent<SaveRuntimeController>();
            if (runtime == null)
            {
                runtime = bootstrap.gameObject.AddComponent<SaveRuntimeController>();
            }

            var serialized = new SerializedObject(runtime);
            serialized.FindProperty("droneSettings").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DroneAnalysisSettings>(DroneSettingsPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }

        private static void WireMainMenu()
        {
            var scene = EditorSceneManager.OpenScene(
                MainMenuScenePath,
                OpenSceneMode.Single);
            DestroyOwnedRoot("SaveMenuCanvas");
            DestroyOwnedRoot("SaveMenuEventSystem");
            var gameplayHud = GameObject.Find("HUDCanvas");
            if (gameplayHud != null)
            {
                gameplayHud.SetActive(false);
            }

            var canvasRoot = new GameObject(
                "SaveMenuCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhaseKSaveSlotPrefabBuilder.PrefabPath);
            var panel = (GameObject)PrefabUtility.InstantiatePrefab(
                prefab,
                canvasRoot.transform);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            var eventSystemRoot = new GameObject(
                "SaveMenuEventSystem",
                typeof(EventSystem));
            var inputModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType == null)
            {
                throw new System.InvalidOperationException(
                    "InputSystemUIInputModule missing.");
            }

            eventSystemRoot.AddComponent(inputModuleType);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            if (scenes.All(entry => entry.path != IntegrationScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(IntegrationScenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void DestroyOwnedRoot(string name)
        {
            var root = GameObject.Find(name);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
