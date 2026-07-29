using SubTerra.App.Core.Data;
using SubTerra.App.Drone;
using SubTerra.App.UI.Drone;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>Phase I 설정과 두 드론 패널을 Editor API로 생성하고 조립한다.</summary>
    public static class PhaseIDroneUiPrefabBuilder
    {
        private const string DialoguePath =
            "Assets/_Project/Prefabs/UI/DroneDialoguePanel.prefab";
        private const string ReasonPath =
            "Assets/_Project/Prefabs/UI/DroneReasonPanel.prefab";
        private const string CompositePath =
            "Assets/_Project/Prefabs/UI/DroneAnalysisUI.prefab";
        private const string SettingsPath =
            "Assets/_Project/Data/Drone/DroneAnalysisSettings.asset";
        private const string CatalogPath =
            "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        [MenuItem("SubTerra/UI/Build Phase I Drone UI")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + BuildAll());
        }

        public static string BuildAll()
        {
            MvpDataAssetBuilder.BuildAll();
            var settings = EnsureSettings();
            BuildDialoguePanel();
            BuildReasonPanel();
            var report = BuildComposite(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return report;
        }

        private static DroneAnalysisSettings EnsureSettings()
        {
            EnsureFolder("Assets/_Project/Data", "Drone");
            var settings = AssetDatabase.LoadAssetAtPath<DroneAnalysisSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<DroneAnalysisSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            settings.EditorSetDefaults();
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static void BuildDialoguePanel()
        {
            var root = new GameObject("DroneDialoguePanel", typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 28f);
            rect.sizeDelta = new Vector2(720f, 130f);

            var panel = CreatePanelRoot(root.transform);
            var title = CreateText(
                panel.transform,
                "SpeakerText",
                new Vector2(20f, -14f),
                new Vector2(680f, 30f),
                20,
                "Digger-Bot");
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.35f, 0.9f, 1f);
            var dialogue = CreateText(
                panel.transform,
                "DialogueText",
                new Vector2(20f, -48f),
                new Vector2(680f, 66f),
                19,
                "분석 대기 중");
            dialogue.textWrappingMode = TextWrappingModes.Normal;

            var view = root.AddComponent<DroneDialoguePanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("panelRoot").objectReferenceValue = panel;
            so.FindProperty("dialogueText").objectReferenceValue = dialogue;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, DialoguePath);
            Object.DestroyImmediate(root);
        }

        private static void BuildReasonPanel()
        {
            var root = new GameObject("DroneReasonPanel", typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-24f, 0f);
            rect.sizeDelta = new Vector2(420f, 230f);

            var panel = CreatePanelRoot(root.transform);
            var title = CreateText(
                panel.transform,
                "TitleText",
                new Vector2(18f, -14f),
                new Vector2(384f, 30f),
                18,
                "드론 추천");
            title.fontStyle = FontStyles.Bold;
            var action = CreateText(
                panel.transform,
                "ActionText",
                new Vector2(18f, -48f),
                new Vector2(384f, 42f),
                23,
                "분석 대기 중");
            action.color = new Color(0.45f, 1f, 0.7f);
            var reason = CreateText(
                panel.transform,
                "ReasonText",
                new Vector2(18f, -96f),
                new Vector2(384f, 116f),
                17,
                "상태 정보 없음");
            reason.textWrappingMode = TextWrappingModes.Normal;

            var view = root.AddComponent<DroneReasonPanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("panelRoot").objectReferenceValue = panel;
            so.FindProperty("actionText").objectReferenceValue = action;
            so.FindProperty("reasonText").objectReferenceValue = reason;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ReasonPath);
            Object.DestroyImmediate(root);
        }

        private static string BuildComposite(DroneAnalysisSettings settings)
        {
            var dialoguePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialoguePath);
            var reasonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReasonPath);
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);

            var root = new GameObject("DroneAnalysisUI", typeof(RectTransform));
            StretchFull(root.GetComponent<RectTransform>());

            var dialogue = PrefabUtility.InstantiatePrefab(dialoguePrefab) as GameObject;
            var reason = PrefabUtility.InstantiatePrefab(reasonPrefab) as GameObject;
            dialogue.transform.SetParent(root.transform, false);
            reason.transform.SetParent(root.transform, false);

            var binder = root.AddComponent<DroneUiBinder>();
            var so = new SerializedObject(binder);
            so.FindProperty("dialogueView").objectReferenceValue =
                dialogue.GetComponent<DroneDialoguePanelView>();
            so.FindProperty("reasonView").objectReferenceValue =
                reason.GetComponent<DroneReasonPanelView>();
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.FindProperty("settings").objectReferenceValue = settings;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, CompositePath);
            Object.DestroyImmediate(root);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CompositePath);
            var prefabBinder = prefab != null ? prefab.GetComponent<DroneUiBinder>() : null;
            var dialogueView = dialoguePrefab != null
                ? dialoguePrefab.GetComponent<DroneDialoguePanelView>()
                : null;
            var reasonView = reasonPrefab != null
                ? reasonPrefab.GetComponent<DroneReasonPanelView>()
                : null;
            return "Drone UI composite=" + (prefab != null)
                + " binderRefs=" + (prefabBinder != null && prefabBinder.HasRequiredReferences())
                + " dialogueRefs=" + (dialogueView != null && dialogueView.HasRequiredReferences())
                + " reasonRefs=" + (reasonView != null && reasonView.HasRequiredReferences());
        }

        private static GameObject CreatePanelRoot(Transform parent)
        {
            var panel = new GameObject("PanelRoot", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            StretchFull(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0.035f, 0.065f, 0.095f, 0.96f);
            return panel;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            float fontSize,
            string value)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = root.AddComponent<TextMeshProUGUI>();
            var fontAsset = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (fontAsset != null)
            {
                text.font = fontAsset;
            }
            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
            return text;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
