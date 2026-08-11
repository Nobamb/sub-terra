using System.Linq;
using SubTerra.App.Integration;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>Phase K World Space 대사 Prefab을 만들고 App 소유 통합 Scene에 연결한다.</summary>
    public static class PhaseKDroneDialogueBuilder
    {
        private const string WorldViewPath =
            "Assets/_Project/Prefabs/UI/ViewSocket.prefab";
        private const string LegacyWorldViewPath =
            "Assets/_Project/Prefabs/UI/DroneWorldDialogue.prefab";
        private const string CompositePath =
            "Assets/_Project/Prefabs/UI/DroneAnalysisUI.prefab";
        private const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [MenuItem("SubTerra/UI/Build Phase K Drone Dialogue")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + BuildAll());
        }

        public static string BuildAll()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(LegacyWorldViewPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(WorldViewPath) == null)
            {
                AssetDatabase.MoveAsset(LegacyWorldViewPath, WorldViewPath);
            }

            BuildWorldViewPrefab();
            var report = WireIntegrationScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return report;
        }

        private static void BuildWorldViewPrefab()
        {
            var root = new GameObject("ViewSocket");

            var canvasObject = new GameObject(
                "WorldDialogueCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup));
            canvasObject.transform.SetParent(root.transform, false);
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            // 드론 말풍선을 기존보다 20% 크게 표시한다.
            canvasRect.sizeDelta = new Vector2(456f, 132f);
            canvasRect.localScale = Vector3.one * 0.006f;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            var canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var panel = new GameObject("VisualRoot", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            StretchFull(panel.GetComponent<RectTransform>());
            var image = panel.GetComponent<Image>();
            image.color = new Color(0.025f, 0.07f, 0.1f, 0.96f);
            image.raycastTarget = false;

            var textObject = new GameObject("DialogueText", typeof(RectTransform));
            textObject.transform.SetParent(panel.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 12f);
            textRect.offsetMax = new Vector2(-18f, -12f);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            var font = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (font != null)
            {
                text.font = font;
            }

            text.text = "분석 대기 중";
            text.fontSize = 22f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;

            var socket = root.AddComponent<DroneDialogueSocket>();
            var serialized = new SerializedObject(socket);
            serialized.FindProperty("anchor").objectReferenceValue = root.transform;
            serialized.FindProperty("visualRoot").objectReferenceValue = canvasRect;
            serialized.FindProperty("worldCanvas").objectReferenceValue = canvas;
            serialized.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            serialized.FindProperty("dialogueText").objectReferenceValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, WorldViewPath);
            Object.DestroyImmediate(root);
        }

        private static string WireIntegrationScene()
        {
            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Single);
            var transforms = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Transform>(true))
                .ToArray();
            var drone = transforms.FirstOrDefault(item => item.name == "DiggerBot_Runtime");
            var hudCanvas = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Canvas>(true))
                .FirstOrDefault(item => item.name == "HUDCanvas");
            var provider = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<DroneContextProviderAdapter>(true))
                .FirstOrDefault();
            if (drone == null || hudCanvas == null || provider == null)
            {
                return "Phase K wiring failed: drone=" + (drone != null)
                    + " hud=" + (hudCanvas != null)
                    + " provider=" + (provider != null);
            }

            var socket = drone.GetComponentInChildren<DroneDialogueSocket>(true);
            if (socket == null)
            {
                var worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldViewPath);
                var worldInstance = PrefabUtility.InstantiatePrefab(worldPrefab, scene) as GameObject;
                worldInstance.transform.SetParent(drone, false);
                worldInstance.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                socket = worldInstance.GetComponent<DroneDialogueSocket>();
            }

            socket.gameObject.name = "ViewSocket";

            var binder = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<DroneUiBinder>(true))
                .FirstOrDefault();
            if (binder == null)
            {
                var composite = AssetDatabase.LoadAssetAtPath<GameObject>(CompositePath);
                var uiInstance = PrefabUtility.InstantiatePrefab(composite, scene) as GameObject;
                uiInstance.transform.SetParent(hudCanvas.transform, false);
                if (uiInstance.transform is RectTransform rect)
                {
                    StretchFull(rect);
                }

                binder = uiInstance.GetComponent<DroneUiBinder>();
            }

            var binderObject = new SerializedObject(binder);
            binderObject.FindProperty("worldDialogueSocket").objectReferenceValue = socket;
            binderObject.FindProperty("contextProviderBehaviour").objectReferenceValue = provider;
            binderObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(binder);

            var tutorial = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<TutorialDirectorBinder>(true))
                .FirstOrDefault();
            if (tutorial != null)
            {
                var tutorialObject = new SerializedObject(tutorial);
                tutorialObject.FindProperty("droneUiBinder").objectReferenceValue = binder;
                tutorialObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tutorial);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Phase K worldView=" + (socket != null && socket.HasRequiredReferences())
                + " binder=" + (binder != null && binder.HasRequiredReferences())
                + " provider=true tutorial=" + (tutorial != null);
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
