using System.IO;
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
            var stacks = CreateTmp(panelRoot.transform, "StacksText", new Vector2(0f, 1f), new Vector2(1f, 0f),
                new Vector2(12f, -84f), new Vector2(-24f, -12f), 18, string.Empty);
            stacks.alignment = TextAlignmentOptions.TopLeft;
            stacks.textWrappingMode = TextWrappingModes.Normal;

            var view = root.AddComponent<InventoryPanelView>();
            var binder = root.AddComponent<InventoryPanelBinder>();

            // SerializeField 연결
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            viewSo.FindProperty("cargoSummaryText").objectReferenceValue = cargo;
            viewSo.FindProperty("unsettledValueText").objectReferenceValue = value;
            viewSo.FindProperty("stacksText").objectReferenceValue = stacks;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            var binderSo = new SerializedObject(binder);
            binderSo.FindProperty("panelView").objectReferenceValue = view;
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
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            return tmp;
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
