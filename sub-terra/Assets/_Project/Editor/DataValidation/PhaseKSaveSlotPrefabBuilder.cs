using SubTerra.App.UI.Save;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    public static class PhaseKSaveSlotPrefabBuilder
    {
        public const string PrefabPath =
            "Assets/_Project/Prefabs/UI/SaveSlotPanel.prefab";

        [MenuItem("SubTerra/UI/Build Phase K Save Slot Panel")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            var root = new GameObject("SaveSlotPanel", typeof(RectTransform), typeof(Image));
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(680f, 520f);
            root.GetComponent<Image>().color = new Color(0.03f, 0.055f, 0.08f, 0.98f);

            CreateText(root.transform, "Title", new Vector2(0f, -36f), new Vector2(620f, 50f), 30f, "Save Slots");
            var slotButtons = new Button[3];
            var slotTexts = new TMP_Text[3];
            for (var i = 0; i < 3; i++)
            {
                slotButtons[i] = CreateButton(
                    root.transform,
                    "Slot" + (i + 1),
                    new Vector2(0f, -115f - (i * 82f)),
                    new Vector2(580f, 62f),
                    "Slot " + (i + 1) + "  Empty",
                    out slotTexts[i]);
            }

            var continueButton = CreateButton(
                root.transform,
                "ContinueButton",
                new Vector2(-190f, -380f),
                new Vector2(180f, 54f),
                "Continue",
                out _);
            var retryButton = CreateButton(
                root.transform,
                "RetryButton",
                new Vector2(0f, -380f),
                new Vector2(160f, 54f),
                "Retry",
                out _);
            var newGameButton = CreateButton(
                root.transform,
                "NewGameButton",
                new Vector2(190f, -380f),
                new Vector2(180f, 54f),
                "New Game",
                out _);
            var message = CreateText(
                root.transform,
                "MessageText",
                new Vector2(0f, -455f),
                new Vector2(600f, 44f),
                17f,
                string.Empty);

            var view = root.AddComponent<SaveSlotPanelView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("panelRoot").objectReferenceValue = root;
            AssignArray(serialized.FindProperty("slotButtons"), slotButtons);
            AssignArray(serialized.FindProperty("slotTexts"), slotTexts);
            serialized.FindProperty("continueButton").objectReferenceValue = continueButton;
            serialized.FindProperty("retryButton").objectReferenceValue = retryButton;
            serialized.FindProperty("newGameButton").objectReferenceValue = newGameButton;
            serialized.FindProperty("messageText").objectReferenceValue = message;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var binder = root.AddComponent<SaveSlotPanelBinder>();
            var binderSerialized = new SerializedObject(binder);
            binderSerialized.FindProperty("view").objectReferenceValue = view;
            binderSerialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var prefabView = prefab != null ? prefab.GetComponent<SaveSlotPanelView>() : null;
            var prefabBinder = prefab != null
                ? prefab.GetComponent<SaveSlotPanelBinder>()
                : null;
            return "SaveSlotPanel exists=" + (prefab != null)
                + " refs=" + (prefabView != null && prefabView.HasRequiredReferences())
                + " binder=" + (prefabBinder != null
                    && prefabBinder.HasRequiredReferences());
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            string label,
            out TMP_Text text)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            root.GetComponent<Image>().color = new Color(0.12f, 0.24f, 0.32f, 1f);
            text = CreateText(root.transform, "Label", Vector2.zero, size, 18f, label);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            text.alignment = TextAlignmentOptions.Center;
            return root.GetComponent<Button>();
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
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
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
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static void AssignArray<T>(SerializedProperty property, T[] values)
            where T : Object
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
