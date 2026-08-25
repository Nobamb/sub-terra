using SubTerra.App.UI.Outpost;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>prompt-B 70: 시설 상호작용 패널 우측 상단에 X 닫기 버튼만 추가한다.</summary>
    public static class PromptB70FacilityPanelCloseBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/OutpostPanel.prefab";
        private const float TitleWidth = 660f;
        private const float CloseSize = 36f;

        [MenuItem("SubTerra/UI/Build Prompt-B 70 Facility Panel Close")]
        public static string Build()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ApplyTo(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                return "Prompt-B 70 facility panel close built: " + PrefabPath;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>기존 또는 재생성된 OutpostPanel 루트에 X 닫기 버튼만 반영한다.</summary>
        public static void ApplyTo(GameObject prefabRoot)
        {
            if (prefabRoot == null)
            {
                throw new System.InvalidOperationException("OutpostPanel 루트가 없습니다.");
            }

            var panelRoot = prefabRoot.transform.Find("PanelRoot");
            if (panelRoot == null)
            {
                throw new System.InvalidOperationException(
                    "OutpostPanel의 PanelRoot를 찾을 수 없습니다.");
            }

            ShrinkTitleForCloseButton(panelRoot);
            var close = EnsureCloseButton(panelRoot);
            Wire(prefabRoot, close);
        }

        private static void ShrinkTitleForCloseButton(Transform panelRoot)
        {
            var title = panelRoot.Find("Title") as RectTransform;
            if (title == null)
            {
                return;
            }

            title.sizeDelta = new Vector2(TitleWidth, title.sizeDelta.y);
        }

        private static Button EnsureCloseButton(Transform panelRoot)
        {
            var existing = panelRoot.Find("CloseButton");
            var root = existing != null
                ? existing.gameObject
                : new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            if (existing == null)
            {
                root.transform.SetParent(panelRoot, false);
            }

            root.transform.SetAsLastSibling();
            var image = root.GetComponent<Image>();
            image.color = new Color(0.22f, 0.18f, 0.18f, 0.95f);
            image.raycastTarget = true;

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-12f, -10f);
            rect.sizeDelta = new Vector2(CloseSize, CloseSize);

            var labelTransform = root.transform.Find("Label");
            var labelObject = labelTransform != null
                ? labelTransform.gameObject
                : new GameObject("Label", typeof(RectTransform));
            if (labelTransform == null)
            {
                labelObject.transform.SetParent(root.transform, false);
            }

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                label = labelObject.AddComponent<TextMeshProUGUI>();
            }

            var font = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (font != null)
            {
                label.font = font;
            }

            label.text = "×";
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return root.GetComponent<Button>();
        }

        private static void Wire(GameObject prefabRoot, Button close)
        {
            var view = prefabRoot.GetComponent<OutpostPanelView>();
            if (view == null)
            {
                return;
            }

            var viewObject = new SerializedObject(view);
            viewObject.FindProperty("closeButton").objectReferenceValue = close;
            viewObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
