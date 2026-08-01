using SubTerra.App.Integration;
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
    /// <summary>MVP2 E 채굴 Runtime 경계와 진행 HUD를 통합 Scene에 연결합니다.</summary>
    public static class PhaseEMiningSetup
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string HudPrefabPath =
            "Assets/_Project/Prefabs/UI/HUDCanvas.prefab";
        private const string FontPath =
            "Assets/_Project/Fonts/NotoSansKR-Regular_SDF.asset";

        [MenuItem("SubTerra/MVP2/Build Phase E Mining Loop")]
        public static string BuildAll()
        {
            BuildHudPrefab();
            ConfigureIntegrationScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Phase E mining transaction, cargo speed, and progress HUD wired.";
        }

        private static void BuildHudPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HudPrefabPath);
            try
            {
                var hud = root.GetComponent<MiningProgressHud>()
                    ?? root.AddComponent<MiningProgressHud>();
                Transform existing = root.transform.Find("MiningProgressStatus");
                GameObject status = existing != null
                    ? existing.gameObject
                    : CreateStatusObject(root.transform);
                var label = status.GetComponentInChildren<TextMeshProUGUI>(true);

                var serializedHud = new SerializedObject(hud);
                serializedHud.FindProperty("statusRoot").objectReferenceValue = status;
                serializedHud.FindProperty("statusText").objectReferenceValue = label;
                serializedHud.ApplyModifiedPropertiesWithoutUndo();
                status.SetActive(false);
                EditorUtility.SetDirty(hud);
                PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject CreateStatusObject(Transform parent)
        {
            var status = new GameObject(
                "MiningProgressStatus",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            status.transform.SetParent(parent, false);
            var rect = status.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 32f);
            rect.sizeDelta = new Vector2(360f, 44f);
            status.GetComponent<Image>().color = new Color(0.03f, 0.06f, 0.1f, 0.88f);

            var labelObject = new GameObject(
                "StatusText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(status.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 4f);
            labelRect.offsetMax = new Vector2(-12f, -4f);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.9f, 0.95f, 1f, 1f);
            label.text = "채굴 0%";
            label.raycastTarget = false;
            return status;
        }

        private static void ConfigureIntegrationScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var binder = FindInScene<IntegrationRuntimeBinder>(scene);
            var mining = FindInScene<MiningSystem>(scene);
            var movement = FindInScene<PlayerMovement>(scene);
            var hud = FindInScene<MiningProgressHud>(scene);
            var resolver = FindInScene<MiningTileResolver>(scene);
            if (binder == null || mining == null || movement == null || hud == null || resolver == null)
            {
                throw new System.InvalidOperationException(
                    "Phase E 필수 Integration Scene 참조가 없습니다.");
            }

            var serializedBinder = new SerializedObject(binder);
            serializedBinder.FindProperty("miningSystem").objectReferenceValue = mining;
            serializedBinder.FindProperty("playerMovement").objectReferenceValue = movement;
            serializedBinder.FindProperty("miningProgressHud").objectReferenceValue = hud;
            serializedBinder.ApplyModifiedPropertiesWithoutUndo();

            var serializedMining = new SerializedObject(mining);
            serializedMining.FindProperty("miningTransactionBehaviour").objectReferenceValue = binder;
            serializedMining.ApplyModifiedPropertiesWithoutUndo();

            ApplyMiningCosts(resolver);
            EditorUtility.SetDirty(binder);
            EditorUtility.SetDirty(mining);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ApplyMiningCosts(MiningTileResolver resolver)
        {
            var serialized = new SerializedObject(resolver);
            var entries = serialized.FindProperty("entries");
            for (var index = 0; index < entries.arraySize; index++)
            {
                var definition = entries.GetArrayElementAtIndex(index).FindPropertyRelative("definition");
                var tileId = definition.FindPropertyRelative("tileId").stringValue;
                var level = 0;
                var energy = 1;
                if (tileId == "tile.iron")
                {
                    level = 1;
                    energy = 2;
                }
                else if (tileId == "tile.lithium")
                {
                    level = 2;
                    energy = 3;
                }
                else if (tileId == "tile.gas-pocket")
                {
                    level = 1;
                    energy = 2;
                }
                else if (tileId == "tile.locked.signal")
                {
                    energy = 0;
                }

                definition.FindPropertyRelative("requiredDrillLevel").intValue = level;
                definition.FindPropertyRelative("energyCost").intValue = energy;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(resolver);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
