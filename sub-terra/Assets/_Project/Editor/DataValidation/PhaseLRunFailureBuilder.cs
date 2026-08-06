using System.Collections.Generic;
using System.Linq;
using SubTerra.App.Integration;
using SubTerra.App.UI.RunFailure;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>Phase L 생존/실패 Orchestrator와 실패 패널을 통합 Scene에 연결한다.</summary>
    public static class PhaseLRunFailureBuilder
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string SettingsFolder = "Assets/_Project/Data/Player";
        private const string SettingsPath = SettingsFolder + "/PlayerSurvivalSettings.asset";

        [MenuItem("SubTerra/MVP2/Build Phase L Run Failure")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + BuildAll());
        }

        public static string BuildAll()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var settings = GetOrCreateSettings();
            var roots = scene.GetRootGameObjects();
            var player = FindTransform(roots, "Player");
            var applicationRoot = FindTransform(roots, "ApplicationRoot");
            var hudCanvas = roots
                .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                .FirstOrDefault(canvas => canvas.name == "HUDCanvas");
            var binder = roots
                .SelectMany(root => root.GetComponentsInChildren<IntegrationRuntimeBinder>(true))
                .FirstOrDefault();
            var movement = player != null ? player.GetComponent<PlayerMovement>() : null;
            if (settings == null
                || player == null
                || applicationRoot == null
                || hudCanvas == null
                || binder == null
                || movement == null)
            {
                return "Phase L wiring failed: settings=" + (settings != null)
                    + " player=" + (player != null)
                    + " app=" + (applicationRoot != null)
                    + " hud=" + (hudCanvas != null)
                    + " binder=" + (binder != null)
                    + " movement=" + (movement != null);
            }

            var host = FindTransform(roots, "RunFailureOrchestrator")?.gameObject;
            if (host == null)
            {
                host = new GameObject("RunFailureOrchestrator");
                SceneManager.MoveGameObjectToScene(host, scene);
                host.transform.SetParent(applicationRoot, false);
            }

            var survival = host.GetComponent<PlayerSurvivalController>()
                ?? host.AddComponent<PlayerSurvivalController>();
            var controller = host.GetComponent<RunFailureRuntimeController>()
                ?? host.AddComponent<RunFailureRuntimeController>();
            var fallback = FindTransform(roots, "RunFailureSurfaceFallback");
            if (fallback == null)
            {
                var fallbackObject = new GameObject("RunFailureSurfaceFallback");
                SceneManager.MoveGameObjectToScene(fallbackObject, scene);
                fallbackObject.transform.SetParent(applicationRoot, false);
                fallbackObject.transform.position = player.position;
                fallback = fallbackObject.transform;
            }

            var view = BuildFailurePanel(hudCanvas.transform);
            WireSurvival(survival, settings, player);
            WireController(controller, survival, movement, player, fallback, view, roots);

            var binderObject = new SerializedObject(binder);
            binderObject.FindProperty("runFailureController").objectReferenceValue = controller;
            binderObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(binder);
            EditorUtility.SetDirty(survival);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Phase L run failure wired: inputs="
                + controller.GetType().Name + " view=" + view.HasRequiredReferences();
        }

        private static PlayerSurvivalSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PlayerSurvivalSettings>(SettingsPath);
            if (settings != null)
            {
                return settings;
            }

            if (!AssetDatabase.IsValidFolder(SettingsFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Player");
            }

            settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(SettingsPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<PlayerSurvivalSettings>(SettingsPath);
        }

        private static RunFailurePanelView BuildFailurePanel(Transform parent)
        {
            var existing = parent.GetComponentsInChildren<RunFailurePanelView>(true).FirstOrDefault();
            var panel = existing != null
                ? existing.gameObject
                : new GameObject(
                    "RunFailurePanel",
                    typeof(RectTransform),
                    typeof(CanvasGroup),
                    typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var blocker = panel.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.42f);
            blocker.raycastTarget = true;

            var visual = panel.transform.Find("VisualRoot");
            if (visual == null)
            {
                var visualObject = new GameObject("VisualRoot", typeof(RectTransform), typeof(Image));
                visualObject.transform.SetParent(panel.transform, false);
                visual = visualObject.transform;
            }

            var visualRect = visual.GetComponent<RectTransform>();
            visualRect.anchorMin = new Vector2(0.5f, 0.5f);
            visualRect.anchorMax = new Vector2(0.5f, 0.5f);
            visualRect.pivot = new Vector2(0.5f, 0.5f);
            visualRect.sizeDelta = new Vector2(680f, 260f);
            visualRect.anchoredPosition = Vector2.zero;
            visual.GetComponent<Image>().color = new Color(0.06f, 0.02f, 0.025f, 0.96f);

            var title = panel.GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(item => item.name == "FailureTitle")
                ?? CreateText(visual, "FailureTitle", 32f, TextAlignmentOptions.Center);
            title.transform.SetParent(visual, false);
            SetRect(title.rectTransform, new Vector2(28f, 154f), new Vector2(-28f, -24f));
            var detail = panel.GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(item => item.name == "FailureDetail")
                ?? CreateText(visual, "FailureDetail", 23f, TextAlignmentOptions.Center);
            detail.transform.SetParent(visual, false);
            SetRect(detail.rectTransform, new Vector2(32f, 24f), new Vector2(-32f, -108f));

            var view = existing ?? panel.AddComponent<RunFailurePanelView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("canvasGroup").objectReferenceValue = panel.GetComponent<CanvasGroup>();
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("detailText").objectReferenceValue = detail;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            float size,
            TextAlignmentOptions alignment)
        {
            var target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            var text = target.AddComponent<TextMeshProUGUI>();
            var font = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void WireSurvival(
            PlayerSurvivalController survival,
            PlayerSurvivalSettings settings,
            Transform player)
        {
            var serialized = new SerializedObject(survival);
            serialized.FindProperty("settings").objectReferenceValue = settings;
            serialized.FindProperty("playerTarget").objectReferenceValue = player;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireController(
            RunFailureRuntimeController controller,
            PlayerSurvivalController survival,
            PlayerMovement movement,
            Transform player,
            Transform fallback,
            RunFailurePanelView view,
            GameObject[] roots)
        {
            var inputs = new List<Behaviour>();
            AddInputs<PlayerController>(roots, inputs);
            AddInputs<PlayerMiningController>(roots, inputs);
            AddInputs<BuildingPlacementInput>(roots, inputs);
            AddInputs<GameplayBuildingPlacementBridge>(roots, inputs);
            AddInputs<ElevatorController>(roots, inputs);

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("survivalController").objectReferenceValue = survival;
            serialized.FindProperty("playerMovement").objectReferenceValue = movement;
            serialized.FindProperty("playerTransform").objectReferenceValue = player;
            serialized.FindProperty("localSurfaceFallback").objectReferenceValue = fallback;
            serialized.FindProperty("failureView").objectReferenceValue = view;
            var inputProperty = serialized.FindProperty("gameplayInputBehaviours");
            inputProperty.arraySize = inputs.Count;
            for (var i = 0; i < inputs.Count; i++)
            {
                inputProperty.GetArrayElementAtIndex(i).objectReferenceValue = inputs[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddInputs<T>(GameObject[] roots, ICollection<Behaviour> output)
            where T : Behaviour
        {
            foreach (var input in roots.SelectMany(root => root.GetComponentsInChildren<T>(true)))
            {
                if (input != null && !output.Contains(input))
                {
                    output.Add(input);
                }
            }
        }

        private static Transform FindTransform(GameObject[] roots, string name)
        {
            return roots
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == name);
        }
    }
}
