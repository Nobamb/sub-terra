using SubTerra.App.UI.Outpost;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>prompt-B 69: 보관함 자원 선택을 검색 가능한 드롭다운으로 교체한다.</summary>
    public static class PromptB69StoragePickerBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/OutpostPanel.prefab";

        [MenuItem("SubTerra/UI/Build Prompt-B 69 Storage Mineral Picker")]
        public static string Build()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ApplyTo(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                return "Prompt-B 69 storage mineral picker built: " + PrefabPath;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>기존 또는 재생성된 OutpostPanel 루트에 검색 드롭다운만 반영한다.</summary>
        public static void ApplyTo(GameObject prefabRoot)
        {
            if (prefabRoot == null)
            {
                throw new System.InvalidOperationException("OutpostPanel 루트가 없습니다.");
            }

            var transaction = prefabRoot.transform.Find("PanelRoot/TransactionRoot");
            if (transaction == null)
            {
                throw new System.InvalidOperationException(
                    "OutpostPanel의 PanelRoot/TransactionRoot를 찾을 수 없습니다.");
            }

            RemoveLegacyMineralButtons(transaction);
            var selected = transaction.Find("SelectedMineralText");
            var styleSource = selected != null
                ? selected.GetComponent<TextMeshProUGUI>()
                : null;
            var picker = EnsurePicker(transaction, styleSource);
            Wire(prefabRoot, picker);
        }

        private static void RemoveLegacyMineralButtons(Transform transaction)
        {
            for (var i = transaction.childCount - 1; i >= 0; i--)
            {
                var child = transaction.GetChild(i);
                if (child != null && child.name.StartsWith("Select_mineral."))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static OutpostMineralPickerView EnsurePicker(
            Transform transaction,
            TextMeshProUGUI styleSource)
        {
            var existing = transaction.Find("MineralPicker");
            var root = existing != null
                ? existing.gameObject
                : new GameObject("MineralPicker", typeof(RectTransform));
            if (existing == null)
            {
                root.transform.SetParent(transaction, false);
            }

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -370f);
            rect.sizeDelta = new Vector2(710f, 40f);

            var search = EnsureSearch(root.transform, styleSource);
            var caption = EnsureCaption(root.transform, styleSource);
            var options = EnsureOptions(root.transform, styleSource);

            var picker = GetOrAddComponent<OutpostMineralPickerView>(root);
            var serialized = new SerializedObject(picker);
            serialized.FindProperty("searchInput").objectReferenceValue = search;
            serialized.FindProperty("captionButton").objectReferenceValue = caption.Item1;
            serialized.FindProperty("captionText").objectReferenceValue = caption.Item2;
            serialized.FindProperty("optionsPanel").objectReferenceValue = options.panel;
            serialized.FindProperty("optionsContent").objectReferenceValue = options.content;
            serialized.FindProperty("optionTemplate").objectReferenceValue = options.template;
            serialized.FindProperty("emptyLabel").objectReferenceValue = options.empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            options.panel.SetActive(false);
            return picker;
        }

        private static TMP_InputField EnsureSearch(Transform parent, TextMeshProUGUI styleSource)
        {
            var existing = parent.Find("SearchInput");
            var root = existing != null
                ? existing.gameObject
                : new GameObject("SearchInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            if (existing == null)
            {
                root.transform.SetParent(parent, false);
            }

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(340f, 40f);
            GetOrAddComponent<Image>(root).color = new Color(0.12f, 0.16f, 0.2f, 1f);

            var viewport = EnsureChild(root.transform, "Text Area");
            GetOrAddComponent<RectMask2D>(viewport);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<RectTransform>().offsetMin = new Vector2(8f, 4f);
            viewport.GetComponent<RectTransform>().offsetMax = new Vector2(-8f, -4f);

            var placeholder = EnsureTmp(viewport.transform, "Placeholder", "자원 이름 검색", 17f, styleSource);
            placeholder.color = new Color(1f, 1f, 1f, 0.45f);
            StretchFull(placeholder.rectTransform);
            var text = EnsureTmp(viewport.transform, "Text", string.Empty, 17f, styleSource);
            StretchFull(text.rectTransform);

            var input = GetOrAddComponent<TMP_InputField>(root);
            input.textViewport = viewport.GetComponent<RectTransform>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        private static (Button, TMP_Text) EnsureCaption(Transform parent, TextMeshProUGUI styleSource)
        {
            var existing = parent.Find("CaptionButton");
            var root = existing != null
                ? existing.gameObject
                : new GameObject("CaptionButton", typeof(RectTransform), typeof(Image), typeof(Button));
            if (existing == null)
            {
                root.transform.SetParent(parent, false);
            }

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(354f, 0f);
            rect.sizeDelta = new Vector2(356f, 40f);
            GetOrAddComponent<Image>(root).color = new Color(0.15f, 0.28f, 0.38f, 1f);
            var label = EnsureTmp(root.transform, "Label", "자원 선택", 17f, styleSource);
            StretchFull(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            return (GetOrAddComponent<Button>(root), label);
        }

        private static OptionsRefs EnsureOptions(Transform parent, TextMeshProUGUI styleSource)
        {
            var existing = parent.Find("OptionsPanel");
            var panel = existing != null
                ? existing.gameObject
                : new GameObject(
                    "OptionsPanel",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(ScrollRect));
            if (existing == null)
            {
                panel.transform.SetParent(parent, false);
            }

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -44f);
            panelRect.sizeDelta = new Vector2(710f, 148f);
            GetOrAddComponent<Image>(panel).color = new Color(0.05f, 0.08f, 0.11f, 0.98f);

            var viewport = EnsureChild(panel.transform, "Viewport");
            GetOrAddComponent<RectMask2D>(viewport);
            var viewportRect = viewport.GetComponent<RectTransform>();
            StretchFull(viewportRect);
            viewportRect.offsetMin = new Vector2(6f, 6f);
            viewportRect.offsetMax = new Vector2(-6f, -6f);

            var content = EnsureChild(viewport.transform, "Content");
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);
            var layout = GetOrAddComponent<VerticalLayoutGroup>(content);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 2f;
            var fitter = GetOrAddComponent<ContentSizeFitter>(content);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var empty = EnsureTmp(content.transform, "EmptyLabel", "일치하는 자원이 없습니다.", 16f, styleSource);
            empty.alignment = TextAlignmentOptions.Center;
            var emptyLayout = GetOrAddComponent<LayoutElement>(empty.gameObject);
            emptyLayout.minHeight = 36f;
            emptyLayout.preferredHeight = 36f;

            var template = EnsureOptionTemplate(content.transform, styleSource);
            var scroll = GetOrAddComponent<ScrollRect>(panel);
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return new OptionsRefs
            {
                panel = panel,
                content = content.transform,
                template = template,
                empty = empty
            };
        }

        private static Button EnsureOptionTemplate(Transform parent, TextMeshProUGUI styleSource)
        {
            var existing = parent.Find("OptionTemplate");
            var root = existing != null
                ? existing.gameObject
                : new GameObject("OptionTemplate", typeof(RectTransform), typeof(Image), typeof(Button));
            if (existing == null)
            {
                root.transform.SetParent(parent, false);
            }

            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 36f);
            GetOrAddComponent<Image>(root).color = new Color(0.13f, 0.24f, 0.32f, 1f);
            var layout = GetOrAddComponent<LayoutElement>(root);
            layout.minHeight = 36f;
            layout.preferredHeight = 36f;
            var label = EnsureTmp(root.transform, "Label", "자원", 16f, styleSource);

            StretchFull(label.rectTransform);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.margin = new Vector4(10f, 0f, 10f, 0f);
            root.SetActive(false);
            return GetOrAddComponent<Button>(root);
        }

        private static void Wire(GameObject prefabRoot, OutpostMineralPickerView picker)
        {
            var view = prefabRoot.GetComponent<OutpostPanelView>();
            var binder = prefabRoot.GetComponent<OutpostPanelBinder>();
            if (view != null)
            {
                var viewObject = new SerializedObject(view);
                viewObject.FindProperty("mineralPicker").objectReferenceValue = picker;
                viewObject.ApplyModifiedPropertiesWithoutUndo();
            }

            if (binder != null)
            {
                var binderObject = new SerializedObject(binder);
                binderObject.FindProperty("mineralPicker").objectReferenceValue = picker;
                binderObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static TextMeshProUGUI EnsureTmp(
            Transform parent,
            string name,
            string value,
            float fontSize,
            TextMeshProUGUI styleSource)
        {
            var existing = parent.Find(name);
            var root = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform));
            if (existing == null)
            {
                root.transform.SetParent(parent, false);
            }

            var text = GetOrAddComponent<TextMeshProUGUI>(root);
            if (styleSource != null)
            {
                text.font = styleSource.font;
                text.fontSharedMaterial = styleSource.fontSharedMaterial;
            }

            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            return text;
        }

        private static T GetOrAddComponent<T>(GameObject root) where T : Component
        {
            var existing = root.GetComponent<T>();
            return existing != null ? existing : root.AddComponent<T>();
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private struct OptionsRefs
        {
            public GameObject panel;
            public Transform content;
            public Button template;
            public TMP_Text empty;
        }
    }
}
