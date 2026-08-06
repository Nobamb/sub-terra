using SubTerra.App.Core.Data;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.Hazards;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// Phase G 건설 메뉴와 위험 HUD 연결을 Editor API로 생성한다.
    /// Scene/Prefab YAML을 직접 편집하지 않고 기존 B 소유 HUD Prefab을 보존한다.
    /// </summary>
    public static class PhaseGBuildingUiPrefabBuilder
    {
        private const string BuildingMenuPath = "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        private const string HudCanvasPath = "Assets/_Project/Prefabs/UI/HUDCanvas.prefab";
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        [MenuItem("SubTerra/UI/Build Phase G Building UI")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + BuildAll());
        }

        public static string BuildAll()
        {
            var buildingReport = BuildBuildingMenu();
            var hudReport = WireHazardHud();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return buildingReport + "; " + hudReport;
        }

        private static string BuildBuildingMenu()
        {
            var root = new GameObject("BuildingMenu", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(1f, 0.5f);
            rootRect.anchoredPosition = new Vector2(-24f, 0f);
            rootRect.sizeDelta = new Vector2(620f, 560f);

            var panelRoot = new GameObject("PanelRoot", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(root.transform, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());
            panelRoot.GetComponent<Image>().color = new Color(0.055f, 0.075f, 0.105f, 0.96f);

            var title = CreateText(panelRoot.transform, "Title", new Vector2(20f, -16f),
                new Vector2(580f, 38f), 28, "시설 건설");
            title.fontStyle = FontStyles.Bold;

            var listText = CreateText(panelRoot.transform, "BuildingListText", new Vector2(20f, -64f),
                new Vector2(220f, 170f), 18, string.Empty);
            var selectionText = CreateText(panelRoot.transform, "SelectionText", new Vector2(260f, -64f),
                new Vector2(340f, 250f), 18, "시설을 선택하세요.");
            selectionText.textWrappingMode = TextWrappingModes.Normal;

            var selectedIconObject = new GameObject("SelectedIcon", typeof(RectTransform), typeof(Image));
            selectedIconObject.transform.SetParent(panelRoot.transform, false);
            var selectedIconRect = selectedIconObject.GetComponent<RectTransform>();
            selectedIconRect.anchorMin = selectedIconRect.anchorMax = new Vector2(1f, 1f);
            selectedIconRect.pivot = new Vector2(1f, 1f);
            selectedIconRect.anchoredPosition = new Vector2(-20f, -18f);
            selectedIconRect.sizeDelta = new Vector2(52f, 52f);
            var selectedIcon = selectedIconObject.GetComponent<Image>();
            selectedIcon.enabled = false;
            selectedIcon.raycastTarget = false;

            var binder = root.AddComponent<BuildingMenuBinder>();
            var view = root.AddComponent<BuildingMenuView>();
            var ids = new[]
            {
                DataIds.Buildings.SupportBasic,
                DataIds.Buildings.LightBasic,
                DataIds.Buildings.ChargerBasic,
                DataIds.Buildings.StorageBasic,
                DataIds.Buildings.SettlementBasic,
                DataIds.Buildings.OutpostCoreBasic
            };
            var names = new[]
            {
                "버팀목", "조명", "충전기", "보관함", "정산 콘솔", "전진기지 코어"
            };

            for (var i = 0; i < ids.Length; i++)
            {
                var button = CreateButton(
                    panelRoot.transform,
                    "Select_" + ids[i],
                    new Vector2(20f, -246f - (i * 42f)),
                    new Vector2(220f, 34f),
                    names[i]);
                var entry = button.gameObject.AddComponent<BuildingMenuEntryButton>();
                entry.EditorSet(ids[i], binder);
            }

            var availability = CreateText(panelRoot.transform, "AvailabilityText",
                new Vector2(260f, -330f), new Vector2(340f, 74f), 20, string.Empty);
            var status = CreateText(panelRoot.transform, "StatusText",
                new Vector2(260f, -414f), new Vector2(340f, 52f), 17, string.Empty);
            var cancel = CreateButton(panelRoot.transform, "CancelButton",
                new Vector2(420f, -492f), new Vector2(180f, 44f), "건설 취소");
            UnityEventTools.AddPersistentListener(cancel.onClick, binder.CancelSelection);

            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("buildingListText").objectReferenceValue = listText;
            viewSo.FindProperty("selectionText").objectReferenceValue = selectionText;
            viewSo.FindProperty("availabilityText").objectReferenceValue = availability;
            viewSo.FindProperty("statusText").objectReferenceValue = status;
            viewSo.FindProperty("selectedIcon").objectReferenceValue = selectedIcon;
            viewSo.FindProperty("cancelButton").objectReferenceValue = cancel;
            viewSo.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            var binderSo = new SerializedObject(binder);
            binderSo.FindProperty("view").objectReferenceValue = view;
            binderSo.FindProperty("catalog").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            binderSo.ApplyModifiedPropertiesWithoutUndo();

            // 같은 경로에 덮어써 기존 .meta GUID와 Scene/Prefab 참조를 보존한다.
            PrefabUtility.SaveAsPrefabAsset(root, BuildingMenuPath);
            Object.DestroyImmediate(root);

            var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(BuildingMenuPath);
            var loadedView = loaded != null ? loaded.GetComponent<BuildingMenuView>() : null;
            var loadedBinder = loaded != null ? loaded.GetComponent<BuildingMenuBinder>() : null;
            return "BuildingMenu viewRefs=" + (loadedView != null && loadedView.HasRequiredReferences())
                + " binderRefs=" + (loadedBinder != null && loadedBinder.HasRequiredReferences());
        }

        private static string WireHazardHud()
        {
            var root = PrefabUtility.LoadPrefabContents(HudCanvasPath);
            if (root == null)
            {
                return "HUDCanvas missing";
            }

            try
            {
                var oldView = root.GetComponent<HazardHudView>();
                if (oldView != null)
                {
                    Object.DestroyImmediate(oldView);
                }
                var oldBinder = root.GetComponent<HazardHudBinder>();
                if (oldBinder != null)
                {
                    Object.DestroyImmediate(oldBinder);
                }

                var structuralText = FindText(root.transform, "StructuralRiskText");
                var gasText = FindText(root.transform, "GasRiskText");
                var gasRoot = FindTransform(root.transform, "WarningRoot")?.gameObject;
                var powerText = FindText(root.transform, "PowerConnectionText");
                if (powerText == null)
                {
                    powerText = CreateText(root.transform, "PowerConnectionText",
                        new Vector2(20f, -238f), new Vector2(440f, 60f), 19, "전력 X 미연결");
                }

                var view = root.AddComponent<HazardHudView>();
                var binder = root.AddComponent<HazardHudBinder>();
                var viewSo = new SerializedObject(view);
                viewSo.FindProperty("structuralText").objectReferenceValue = structuralText;
                viewSo.FindProperty("gasText").objectReferenceValue = gasText;
                viewSo.FindProperty("gasWarningRoot").objectReferenceValue = gasRoot;
                viewSo.FindProperty("powerText").objectReferenceValue = powerText;
                viewSo.ApplyModifiedPropertiesWithoutUndo();

                var binderSo = new SerializedObject(binder);
                binderSo.FindProperty("view").objectReferenceValue = view;
                binderSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, HudCanvasPath);
                return "HUDCanvas hazardRefs=" + view.HasRequiredReferences()
                    + " binderRefs=" + binder.HasRequiredReferences();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize,
            string value)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
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

        private static Button CreateButton(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.15f, 0.22f, 0.3f, 1f);

            var labelText = CreateText(go.transform, "Label", Vector2.zero, size, 17, label);
            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelText.alignment = TextAlignmentOptions.Center;
            return go.GetComponent<Button>();
        }

        private static TMP_Text FindText(Transform root, string name)
        {
            var found = FindTransform(root, name);
            return found != null ? found.GetComponent<TMP_Text>() : null;
        }

        private static Transform FindTransform(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
