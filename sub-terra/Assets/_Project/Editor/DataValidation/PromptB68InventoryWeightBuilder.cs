using SubTerra.App.UI.Inventory;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>prompt-B 68번 인벤토리 중량 도움말만 추가한다.</summary>
    public static class PromptB68InventoryWeightBuilder
    {
        private const string InventoryPanelPrefabPath =
            "Assets/_Project/Prefabs/UI/InventoryPanel.prefab";

        [MenuItem("SubTerra/UI/Build Prompt-B 68 Inventory Weight Help")]
        public static string Build()
        {
            var root = PrefabUtility.LoadPrefabContents(InventoryPanelPrefabPath);
            try
            {
                var panelRoot = root.transform.Find("PanelRoot");
                ApplyTo(panelRoot);

                PrefabUtility.SaveAsPrefabAsset(root, InventoryPanelPrefabPath);
                return "Prompt-B 68 inventory weight help built: " + InventoryPanelPrefabPath;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>기본 인벤토리 빌더가 프리팹을 재생성해도 중량 도움말을 유지한다.</summary>
        public static void ApplyTo(Transform panelRoot)
        {
            var cargo = panelRoot != null
                ? panelRoot.Find("CargoSummaryText")?.GetComponent<TextMeshProUGUI>()
                : null;
            if (panelRoot == null || cargo == null)
            {
                throw new System.InvalidOperationException(
                    "InventoryPanel의 PanelRoot/CargoSummaryText를 찾을 수 없습니다.");
            }

            cargo.rectTransform.sizeDelta = new Vector2(300f, cargo.rectTransform.sizeDelta.y);

            var hover = CreateWeightHelpIcon(panelRoot, cargo);
            var tooltip = CreateWeightTooltip(panelRoot, cargo);
            var serialized = new SerializedObject(hover);
            serialized.FindProperty("tooltipRoot").objectReferenceValue = tooltip;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static InventoryWeightTooltip CreateWeightHelpIcon(
            Transform parent,
            TextMeshProUGUI styleSource)
        {
            var existing = parent.Find("WeightHelpIcon");
            var root = existing != null
                ? existing.gameObject
                : new GameObject("WeightHelpIcon", typeof(RectTransform));
            if (existing == null)
            {
                root.transform.SetParent(parent, false);
            }

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            // 닫기 버튼이 우측 상단 -12 위치를 사용하므로 그 왼쪽에 배치한다.
            rect.anchoredPosition = new Vector2(-56f, -11f);
            rect.sizeDelta = new Vector2(34f, 34f);

            var image = GetOrAddComponent<Image>(root);
            image.color = new Color(1f, 0.72f, 0.2f, 1f);
            image.raycastTarget = true;

            var outline = GetOrAddComponent<Outline>(root);
            outline.effectColor = new Color(0.05f, 0.075f, 0.11f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);

            var labelRoot = root.transform.Find("Label");
            var label = labelRoot != null
                ? labelRoot.GetComponent<TextMeshProUGUI>()
                : null;
            if (label == null)
            {
                label = CreateText(root.transform, "Label", styleSource, "?", 23f);
            }

            ApplyTextStyle(label, styleSource, "?", 23f);
            StretchFull(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.04f, 0.055f, 0.08f, 1f);
            root.SetActive(true);
            return GetOrAddComponent<InventoryWeightTooltip>(root);
        }

        private static GameObject CreateWeightTooltip(
            Transform parent,
            TextMeshProUGUI styleSource)
        {
            var existing = parent.Find("WeightTooltip");
            var root = existing != null
                ? existing.gameObject
                : new GameObject("WeightTooltip", typeof(RectTransform));
            if (existing == null)
            {
                root.transform.SetParent(parent, false);
            }

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-56f, -48f);
            rect.sizeDelta = new Vector2(382f, 164f);

            var image = GetOrAddComponent<Image>(root);
            image.color = new Color(0.035f, 0.05f, 0.075f, 0.98f);
            image.raycastTarget = false;

            const string tooltipText =
                "화물 무게 안내\n"
                + "• 무게가 무거울수록 이동 속도가 감소합니다.\n"
                + "• 최대 적재량에 도달하면 더 이상 자원을 채굴할 수 없습니다.\n"
                + "• 적재율에 비례해 점프력이 기본의 75%까지 감소합니다.\n"
                + "• 적재율에 비례해 낙하 충격이 최대 1.5배까지 증가합니다.";
            var descriptionRoot = root.transform.Find("Description");
            var description = descriptionRoot != null
                ? descriptionRoot.GetComponent<TextMeshProUGUI>()
                : null;
            if (description == null)
            {
                description = CreateText(root.transform, "Description", styleSource, tooltipText, 15f);
            }

            ApplyTextStyle(description, styleSource, tooltipText, 15f);
            StretchFull(description.rectTransform);
            description.rectTransform.offsetMin = new Vector2(12f, 10f);
            description.rectTransform.offsetMax = new Vector2(-12f, -10f);
            description.textWrappingMode = TextWrappingModes.Normal;
            description.alignment = TextAlignmentOptions.TopLeft;

            root.SetActive(false);
            root.transform.SetAsLastSibling();
            return root;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            TextMeshProUGUI styleSource,
            string text,
            float fontSize)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var label = root.AddComponent<TextMeshProUGUI>();
            ApplyTextStyle(label, styleSource, text, fontSize);
            return label;
        }

        private static void ApplyTextStyle(
            TextMeshProUGUI label,
            TextMeshProUGUI styleSource,
            string text,
            float fontSize)
        {
            label.font = styleSource.font;
            label.fontSharedMaterial = styleSource.fontSharedMaterial;
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.raycastTarget = false;
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
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
