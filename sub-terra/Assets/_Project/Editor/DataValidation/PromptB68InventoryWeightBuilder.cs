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
                var cargo = panelRoot != null
                    ? panelRoot.Find("CargoSummaryText")?.GetComponent<TextMeshProUGUI>()
                    : null;
                if (panelRoot == null || cargo == null)
                {
                    throw new System.InvalidOperationException(
                        "InventoryPanel의 PanelRoot/CargoSummaryText를 찾을 수 없습니다.");
                }

                RemoveChild(panelRoot, "WeightHelpIcon");
                RemoveChild(panelRoot, "WeightTooltip");
                cargo.rectTransform.sizeDelta = new Vector2(344f, cargo.rectTransform.sizeDelta.y);

                var hover = CreateWeightHelpIcon(panelRoot, cargo);
                var tooltip = CreateWeightTooltip(panelRoot, cargo);
                var serialized = new SerializedObject(hover);
                serialized.FindProperty("tooltipRoot").objectReferenceValue = tooltip;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, InventoryPanelPrefabPath);
                return "Prompt-B 68 inventory weight help built: " + InventoryPanelPrefabPath;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static InventoryWeightTooltip CreateWeightHelpIcon(
            Transform parent,
            TextMeshProUGUI styleSource)
        {
            var root = new GameObject(
                "WeightHelpIcon",
                typeof(RectTransform),
                typeof(Image),
                typeof(InventoryWeightTooltip));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-12f, -12f);
            rect.sizeDelta = new Vector2(30f, 30f);

            var image = root.GetComponent<Image>();
            image.color = new Color(0.18f, 0.24f, 0.32f, 1f);
            image.raycastTarget = true;

            var label = CreateText(root.transform, "Label", styleSource, "?", 21f);
            StretchFull(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            return root.GetComponent<InventoryWeightTooltip>();
        }

        private static GameObject CreateWeightTooltip(
            Transform parent,
            TextMeshProUGUI styleSource)
        {
            var root = new GameObject("WeightTooltip", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-12f, -48f);
            rect.sizeDelta = new Vector2(382f, 164f);

            var image = root.GetComponent<Image>();
            image.color = new Color(0.035f, 0.05f, 0.075f, 0.98f);
            image.raycastTarget = false;

            var description = CreateText(
                root.transform,
                "Description",
                styleSource,
                "화물 무게 안내\n"
                + "• 무게가 무거울수록 이동 속도가 감소합니다.\n"
                + "• 최대 적재량에 도달하면 더 이상 자원을 채굴할 수 없습니다.\n"
                + "• 적재율에 비례해 점프력이 기본의 75%까지 감소합니다.\n"
                + "• 적재율에 비례해 낙하 충격이 최대 1.5배까지 증가합니다.",
                15f);
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
            label.font = styleSource.font;
            label.fontSharedMaterial = styleSource.fontSharedMaterial;
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static void RemoveChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
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
