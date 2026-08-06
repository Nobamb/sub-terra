using System.IO;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.Inventory;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// InventoryPanel.prefab을 Editor API로 생성한다.
    /// Scene/Prefab YAML 직접 편집 대신 PrefabUtility를 사용한다.
    /// </summary>
    public static class InventoryPanelPrefabBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/InventoryPanel.prefab";
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";
        private const string FlagPath = "Temp/subterra-build-inventory-panel.flag";

        [InitializeOnLoadMethod]
        private static void WatchFlag()
        {
            EditorApplication.update += () =>
            {
                if (!File.Exists(FlagPath))
                {
                    return;
                }

                try
                {
                    File.Delete(FlagPath);
                    var report = BuildPrefab();
                    Debug.Log("[SubTerra] " + report);
                    File.WriteAllText("Temp/subterra-inventory-panel-build.txt", report);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[SubTerra] InventoryPanel build failed: " + ex.GetType().Name);
                }
            };
        }

        [MenuItem("SubTerra/UI/Build Inventory Panel Prefab")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + BuildPrefab());
        }

        public static string BuildPrefab()
        {
            EnsureFolder("Assets/_Project/Prefabs", "UI");

            var root = new GameObject("InventoryPanel", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(420f, 320f);
            rootRect.anchoredPosition = Vector2.zero;

            var panelRoot = new GameObject("PanelRoot", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(root.transform, false);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            StretchFull(panelRect);
            var image = panelRoot.GetComponent<Image>();
            image.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);
            image.raycastTarget = true;

            var cargo = CreateTmp(panelRoot.transform, "CargoSummaryText", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(12f, -12f), new Vector2(396f, 32f), 22, "0 / 50");
            var value = CreateTmp(panelRoot.transform, "UnsettledValueText", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(12f, -48f), new Vector2(396f, 28f), 20, "0");
            var rowsRoot = new GameObject("StackRows", typeof(RectTransform));
            rowsRoot.transform.SetParent(panelRoot.transform, false);
            var rowsRect = rowsRoot.GetComponent<RectTransform>();
            rowsRect.anchorMin = new Vector2(0f, 0f);
            rowsRect.anchorMax = new Vector2(1f, 1f);
            rowsRect.offsetMin = new Vector2(12f, 12f);
            rowsRect.offsetMax = new Vector2(-12f, -84f);

            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            var rows = new InventoryStackRowView[catalog != null ? catalog.Minerals.Count : 0];
            for (var i = 0; i < rows.Length; i++)
            {
                var mineral = catalog.Minerals[i];
                rows[i] = CreateStackRow(
                    rowsRoot.transform,
                    mineral != null ? mineral.Id : string.Empty,
                    mineral != null ? mineral.DisplayName : string.Empty,
                    mineral != null ? mineral.Icon : null,
                    i);
            }

            // 기존 텍스트 계약을 보존하되 화면에는 광물 행만 표시한다.
            var stacks = CreateTmp(panelRoot.transform, "StacksText", Vector2.zero, Vector2.zero,
                Vector2.zero, Vector2.zero, 1, string.Empty);
            stacks.gameObject.SetActive(false);

            var view = root.AddComponent<InventoryPanelView>();
            var binder = root.AddComponent<InventoryPanelBinder>();

            // SerializeField 연결
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            viewSo.FindProperty("cargoSummaryText").objectReferenceValue = cargo;
            viewSo.FindProperty("unsettledValueText").objectReferenceValue = value;
            viewSo.FindProperty("stacksText").objectReferenceValue = stacks;
            var rowProperty = viewSo.FindProperty("stackRows");
            rowProperty.arraySize = rows.Length;
            for (var i = 0; i < rows.Length; i++)
            {
                rowProperty.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
            }
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            var binderSo = new SerializedObject(binder);
            binderSo.FindProperty("panelView").objectReferenceValue = view;
            binderSo.FindProperty("catalog").objectReferenceValue = catalog;
            binderSo.ApplyModifiedPropertiesWithoutUndo();

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(PrefabPath);
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var loadedView = loaded != null ? loaded.GetComponent<InventoryPanelView>() : null;
            var ok = loadedView != null && loadedView.HasRequiredReferences();
            var binderOk = loaded != null && loaded.GetComponent<InventoryPanelBinder>() != null
                && loaded.GetComponent<InventoryPanelBinder>().HasRequiredReferences();

            return "InventoryPanel prefab path=" + PrefabPath
                + " viewRefs=" + ok
                + " binderRefs=" + binderOk;
        }

        private static TextMeshProUGUI CreateTmp(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 sizeDelta,
            float fontSize,
            string text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            var fontAsset = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (fontAsset != null)
            {
                tmp.font = fontAsset;
            }
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            return tmp;
        }

        private static InventoryStackRowView CreateStackRow(
            Transform parent,
            string mineralId,
            string displayName,
            Sprite icon,
            int index)
        {
            var root = new GameObject("Stack_" + mineralId, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -index * 58f);
            rect.sizeDelta = new Vector2(0f, 50f);
            root.GetComponent<Image>().color = new Color(0.1f, 0.14f, 0.19f, 0.88f);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(root.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(10f, 0f);
            iconRect.sizeDelta = new Vector2(34f, 34f);
            var iconImage = iconGo.GetComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.enabled = icon != null;

            var name = CreateTmp(root.transform, "Name", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(54f, 0f), new Vector2(210f, 34f), 18, displayName);
            name.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            name.alignment = TextAlignmentOptions.Left;
            var quantity = CreateTmp(root.transform, "Quantity", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-12f, 0f), new Vector2(100f, 34f), 19, "x0");
            quantity.GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);
            quantity.alignment = TextAlignmentOptions.Right;

            var row = root.AddComponent<InventoryStackRowView>();
            row.EditorSetReferences(mineralId, iconImage, name, quantity);
            return row;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
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
