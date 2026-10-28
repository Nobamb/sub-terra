using SubTerra.App.UI.Inventory;
using SubTerra.App.UI.SurfaceBase;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    public static class Issue249SurfaceInventoryBuilder
    {
        private const string InventoryPath = "Assets/_Project/Prefabs/UI/InventoryPanel.prefab";

        [MenuItem("SubTerra/UI/Build Issue 249 Surface Inventory")]
        public static void Build()
        {
            var root = PrefabUtility.LoadPrefabContents(PromptB117SurfaceBaseBuilder.PrefabPath);
            try
            {
                PromptB117SurfaceBaseBuilder.ApplyMainControls(root);
                var content = root.transform.Find("SurfaceBaseContent");
                var modal = content.Find("InventoryModal");
                if (modal == null)
                {
                    var go = new GameObject("InventoryModal", typeof(RectTransform), typeof(Canvas),
                        typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.GraphicRaycaster));
                    modal = go.transform;
                    modal.SetParent(content, false);
                    var rect = (RectTransform)modal;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    go.GetComponent<Canvas>().overrideSorting = true;
                    go.GetComponent<UnityEngine.UI.Image>().color = new Color(.01f, .025f, .04f, .8f);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPath);
                    var inventory = (GameObject)PrefabUtility.InstantiatePrefab(prefab, modal);
                    inventory.name = "InventoryPanel";
                    var card = (RectTransform)inventory.transform;
                    card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f);
                    card.anchoredPosition = Vector2.zero;
                    card.localScale = Vector3.one;
                    var canvas = inventory.GetComponent<Canvas>();
                    if (canvas == null) canvas = inventory.AddComponent<Canvas>();
                    canvas.overrideSorting = true;
                    if (inventory.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                        inventory.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
                modal.gameObject.SetActive(false);
                var panel = modal.GetComponentInChildren<InventoryPanelView>(true);
                var view = new SerializedObject(root.GetComponent<SurfaceBaseView>());
                view.FindProperty("inventoryButton").objectReferenceValue = content.Find("InventoryButton").GetComponent<UnityEngine.UI.Button>();
                view.FindProperty("inventoryRoot").objectReferenceValue = modal.gameObject;
                view.FindProperty("inventoryPanel").objectReferenceValue = panel;
                view.FindProperty("upgradeCard").objectReferenceValue = content.Find("UpgradeModal/ProgressionPanel");
                view.ApplyModifiedPropertiesWithoutUndo();
                var binder = new SerializedObject(root.GetComponent<SurfaceBaseBinder>());
                binder.FindProperty("inventoryBinder").objectReferenceValue = modal.GetComponentInChildren<InventoryPanelBinder>(true);
                binder.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PromptB117SurfaceBaseBuilder.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("Issue 249 SurfaceBasePanel inventory and two-by-two actions built.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
