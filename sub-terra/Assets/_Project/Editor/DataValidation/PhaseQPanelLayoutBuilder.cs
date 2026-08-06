using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.UI;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.Inventory;
using SubTerra.App.UI.Progression;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>탐사 HUD의 패널 영역과 재열기 버튼을 일관된 위치로 조립한다.</summary>
    public static class PhaseQPanelLayoutBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string BuildingPrefabPath = "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        private const string InventoryPrefabPath = "Assets/_Project/Prefabs/UI/InventoryPanel.prefab";
        private const string DronePrefabPath = "Assets/_Project/Prefabs/UI/DroneAnalysisUI.prefab";
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        [MenuItem("SubTerra/UI/Build Phase Q Panel Layout")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            var previousScene = SceneManager.GetActiveScene().path;
            InventoryPanelPrefabBuilder.BuildPrefab();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("HUDCanvas");
            if (canvas == null)
            {
                throw new InvalidOperationException("HUDCanvas missing.");
            }

            var oldLayout = canvas.transform.Find("PanelLayout");
            if (oldLayout != null)
            {
                UnityEngine.Object.DestroyImmediate(oldLayout.gameObject);
            }

            RemoveLegacyDirectBuildingPanels(canvas.transform);

            PositionExistingHud(canvas.transform);
            var layout = new GameObject("PanelLayout", typeof(RectTransform));
            layout.transform.SetParent(canvas.transform, false);
            Stretch(layout.GetComponent<RectTransform>());

            var building = InstantiatePanel(BuildingPrefabPath, layout.transform, "BuildingPanel", new Vector2(0f, 1f), new Vector2(24f, -470f));
            SetSize(building, new Vector2(440f, 500f));
            var inventory = InstantiatePanel(InventoryPrefabPath, layout.transform, "InventoryPanel", new Vector2(0.5f, 0.5f), Vector2.zero);
            var drone = InstantiatePanel(DronePrefabPath, layout.transform, "DiggerBotPanel", new Vector2(0.5f, 0f), new Vector2(0f, 112f));
            var upgrade = CreateUpgradePanel(layout.transform);
            var guide = CreateGuidePanel(layout.transform);

            var buildingBinder = building.GetComponent<BuildingMenuBinder>();
            var inventoryBinder = inventory.GetComponent<InventoryPanelBinder>();
            var upgradeBinder = upgrade.GetComponent<ProgressionPanelBinder>();
            var controller = layout.AddComponent<PanelToggleController>();
            AssignController(controller, building, inventory, upgrade, guide, drone, buildingBinder);
            AddCloseButton(building.transform, controller, "CloseBuilding", new Vector2(-18f, -18f));
            AddCloseButton(inventory.transform, controller, "CloseInventory", new Vector2(-18f, -18f));
            AddCloseButton(upgrade.transform, controller, "CloseUpgrade", new Vector2(-18f, -18f));
            AddCloseButton(guide.transform, controller, "CloseGameGuide", new Vector2(-18f, -18f));
            AddCloseButton(drone.transform, controller, "CloseDiggerBot", new Vector2(-18f, -18f));
            CreateShortcutBar(layout.transform, controller);
            WireRuntimeBinders(buildingBinder, inventoryBinder, upgradeBinder, drone.GetComponent<SubTerra.App.UI.Drone.DroneUiBinder>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previousScene) && previousScene != ScenePath)
            {
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
            }

            return "Phase Q panel layout built: B/I/U/G toggles, close buttons, and HUD regions wired.";
        }

        private static GameObject InstantiatePanel(string path, Transform parent, string name, Vector2 anchor, Vector2 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Prefab missing: " + path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            var rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            return instance;
        }

        // Phase Q 이전 씬에 직접 배치된 BuildingMenu는 새 PanelLayout과 중복되며,
        // BuildingUiIntegrationBinder가 새 패널 하나만 바인딩하므로 여기서 제거한다.
        private static void RemoveLegacyDirectBuildingPanels(Transform canvas)
        {
            var legacyPanels = new List<GameObject>();
            for (var i = 0; i < canvas.childCount; i++)
            {
                var child = canvas.GetChild(i);
                if (child.GetComponent<BuildingMenuBinder>() != null)
                {
                    legacyPanels.Add(child.gameObject);
                }
            }

            for (var i = 0; i < legacyPanels.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(legacyPanels[i]);
            }
        }

        private static GameObject CreateUpgradePanel(Transform parent)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            if (catalog == null || catalog.Upgrades == null || catalog.Upgrades.Count == 0)
            {
                throw new InvalidOperationException("GameDataCatalog upgrades are missing.");
            }

            var root = CreatePanel(parent, "UpgradePanel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 500f));
            CreateText(root.transform, "Title", new Vector2(22f, -18f), new Vector2(600f, 34f), 26f, "장비 업그레이드 [U]");
            var list = CreateText(root.transform, "UpgradeList", new Vector2(22f, -70f), new Vector2(280f, 300f), 18f, string.Empty);
            var detail = CreateText(root.transform, "UpgradeDetail", new Vector2(326f, -70f), new Vector2(330f, 175f), 18f, "업그레이드를 선택하세요.");
            var result = CreateText(root.transform, "UpgradeResult", new Vector2(326f, -260f), new Vector2(330f, 60f), 16f, string.Empty);
            var deep = CreateText(root.transform, "DeepZone", new Vector2(326f, -330f), new Vector2(330f, 55f), 16f, string.Empty);
            var purchase = CreateButton(root.transform, "PurchaseButton", new Vector2(326f, -415f), new Vector2(200f, 42f), "업그레이드 구매");
            var view = root.AddComponent<ProgressionPanelView>();
            var binder = root.AddComponent<ProgressionPanelBinder>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("upgradeListText").objectReferenceValue = list;
            viewSo.FindProperty("detailText").objectReferenceValue = detail;
            viewSo.FindProperty("resultText").objectReferenceValue = result;
            viewSo.FindProperty("deepZoneText").objectReferenceValue = deep;
            viewSo.FindProperty("purchaseButton").objectReferenceValue = purchase;
            viewSo.FindProperty("panelRoot").objectReferenceValue = root;
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            var binderSo = new SerializedObject(binder);
            binderSo.FindProperty("view").objectReferenceValue = view;
            binderSo.ApplyModifiedPropertiesWithoutUndo();
            var entries = CreateUpgradeEntries(root.transform, binder, catalog);
            var viewEntries = viewSo.FindProperty("upgradeButtons");
            viewEntries.arraySize = entries.Count;
            for (var i = 0; i < entries.Count; i++)
            {
                viewEntries.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            }
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(purchase.onClick, binder.PurchaseSelected);
            return root;
        }

        private static List<ProgressionUpgradeEntryButton> CreateUpgradeEntries(
            Transform parent,
            ProgressionPanelBinder binder,
            GameDataCatalog catalog)
        {
            var entries = new List<ProgressionUpgradeEntryButton>(catalog.Upgrades.Count);
            for (var i = 0; i < catalog.Upgrades.Count; i++)
            {
                var upgrade = catalog.Upgrades[i];
                if (upgrade == null || string.IsNullOrEmpty(upgrade.Id))
                {
                    continue;
                }

                var button = CreateButton(
                    parent,
                    "UpgradeEntry_" + i,
                    new Vector2(22f, -108f - (i * 48f)),
                    new Vector2(280f, 40f),
                    upgrade.DisplayName);
                var entry = button.gameObject.AddComponent<ProgressionUpgradeEntryButton>();
                entry.EditorSet(upgrade.Id, binder, button.GetComponentInChildren<TMP_Text>());
                entries.Add(entry);
            }

            return entries;
        }

        private static GameObject CreateGuidePanel(Transform parent)
        {
            var root = CreatePanel(parent, "GameGuidePanel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 740f));
            CreateText(root.transform, "Title", new Vector2(28f, -24f), new Vector2(950f, 44f), 30f, "게임 가이드 [G]");
            CreateText(root.transform, "GuideText", new Vector2(28f, -88f), new Vector2(1100f, 580f), 18f,
                "기본 조작\n이동/점프/채굴 후 B 시설 건설, I 화물, U 장비 업그레이드를 사용합니다.\n\n핵심 메커니즘\n전력·구조 위험·가스 위험을 확인하고, Digger-Bot 권고를 참고하세요.\n\n세부 안내 내용은 다음 UI 작업에서 확장합니다.");
            return root;
        }

        private static void CreateShortcutBar(Transform parent, PanelToggleController controller)
        {
            var root = new GameObject("PanelShortcutBar", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -100f);
            rect.sizeDelta = new Vector2(180f, 250f);
            AddShortcut(root.transform, controller, "시설 [B]", "ToggleBuilding", 0);
            AddShortcut(root.transform, controller, "화물 [I]", "ToggleInventory", 1);
            AddShortcut(root.transform, controller, "업그레이드 [U]", "ToggleUpgrade", 2);
            AddShortcut(root.transform, controller, "게임 가이드 [G]", "ToggleGameGuide", 3);
        }

        private static void AddShortcut(Transform parent, PanelToggleController controller, string label, string method, int index)
        {
            var button = CreateButton(parent, "Open" + index, new Vector2(0f, -index * 56f), new Vector2(180f, 44f), label);
            AddControllerListener(button, controller, method);
        }

        private static void AddCloseButton(Transform parent, PanelToggleController controller, string method, Vector2 position)
        {
            var button = CreateButton(parent, "CloseButton", position, new Vector2(36f, 36f), "×");
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            AddControllerListener(button, controller, method);
        }

        private static void AddControllerListener(Button button, PanelToggleController controller, string method)
        {
            switch (method)
            {
                case "ToggleBuilding": UnityEventTools.AddPersistentListener(button.onClick, controller.ToggleBuilding); break;
                case "ToggleInventory": UnityEventTools.AddPersistentListener(button.onClick, controller.ToggleInventory); break;
                case "ToggleUpgrade": UnityEventTools.AddPersistentListener(button.onClick, controller.ToggleUpgrade); break;
                case "ToggleGameGuide": UnityEventTools.AddPersistentListener(button.onClick, controller.ToggleGameGuide); break;
                case "CloseBuilding": UnityEventTools.AddPersistentListener(button.onClick, controller.CloseBuilding); break;
                case "CloseInventory": UnityEventTools.AddPersistentListener(button.onClick, controller.CloseInventory); break;
                case "CloseUpgrade": UnityEventTools.AddPersistentListener(button.onClick, controller.CloseUpgrade); break;
                case "CloseGameGuide": UnityEventTools.AddPersistentListener(button.onClick, controller.CloseGameGuide); break;
                case "CloseDiggerBot": UnityEventTools.AddPersistentListener(button.onClick, controller.CloseDiggerBot); break;
                default: throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported panel action.");
            }
        }

        private static void AssignController(PanelToggleController controller, GameObject building, GameObject inventory, GameObject upgrade, GameObject guide, GameObject drone, BuildingMenuBinder buildingBinder)
        {
            var so = new SerializedObject(controller);
            var panels = so.FindProperty("panels");
            panels.arraySize = 5;
            AssignPanel(panels.GetArrayElementAtIndex(0), RuntimePanelId.Building, building);
            AssignPanel(panels.GetArrayElementAtIndex(1), RuntimePanelId.Inventory, inventory);
            AssignPanel(panels.GetArrayElementAtIndex(2), RuntimePanelId.Upgrade, upgrade);
            AssignPanel(panels.GetArrayElementAtIndex(3), RuntimePanelId.GameGuide, guide);
            AssignPanel(panels.GetArrayElementAtIndex(4), RuntimePanelId.DiggerBot, drone);
            so.FindProperty("buildingMenu").objectReferenceValue = buildingBinder;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignPanel(SerializedProperty panel, RuntimePanelId id, GameObject root)
        {
            panel.FindPropertyRelative("panelId").enumValueIndex = (int)id;
            panel.FindPropertyRelative("panelRoot").objectReferenceValue = root;
            panel.FindPropertyRelative("visibleOnStart").boolValue = false;
        }

        private static void WireRuntimeBinders(
            BuildingMenuBinder building,
            InventoryPanelBinder inventory,
            ProgressionPanelBinder progression,
            SubTerra.App.UI.Drone.DroneUiBinder drone)
        {
            var runtime = UnityEngine.Object.FindFirstObjectByType<IntegrationRuntimeBinder>();
            if (runtime != null)
            {
                var so = new SerializedObject(runtime);
                so.FindProperty("inventoryPanelBinder").objectReferenceValue = inventory;
                so.FindProperty("progressionPanelBinder").objectReferenceValue = progression;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var buildingIntegration = UnityEngine.Object.FindFirstObjectByType<BuildingUiIntegrationBinder>();
            if (buildingIntegration != null)
            {
                var so = new SerializedObject(buildingIntegration);
                so.FindProperty("buildingMenu").objectReferenceValue = building;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var tutorial = UnityEngine.Object.FindFirstObjectByType<SubTerra.App.UI.Tutorial.TutorialDirectorBinder>();
            if (tutorial != null)
            {
                var so = new SerializedObject(tutorial);
                so.FindProperty("droneUiBinder").objectReferenceValue = drone;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void PositionExistingHud(Transform canvas)
        {
            var hud = canvas.GetComponent<SubTerra.App.UI.HUD.HudBinder>();
            if (hud != null && hud.BasicHud != null)
            {
                var fields = new[] { hud.BasicHud.EnergyText, hud.BasicHud.DepthText, hud.BasicHud.GoldText, hud.BasicHud.CargoText, hud.BasicHud.UnsettledValueText, hud.BasicHud.BuildingSelectionText };
                for (var i = 0; i < fields.Length; i++)
                {
                    if (fields[i] == null) continue;
                    var rect = fields[i].rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(24f, -28f - (i * 30f));
                    rect.sizeDelta = new Vector2(360f, 26f);
                    fields[i].alignment = TextAlignmentOptions.TopLeft;
                    fields[i].textWrappingMode = TextWrappingModes.NoWrap;
                }
            }

            var objective = canvas.Find("DemoObjectiveRoot");
            if (objective == null) return;
            PositionObjectiveText(objective, "ObjectiveTitle", 0.72f, 0.76f, 20f);
            PositionObjectiveText(objective, "ObjectiveBody", 0.64f, 0.69f, 16f);
            PositionObjectiveText(objective, "NextAction", 0.58f, 0.62f, 15f);
        }

        private static void PositionObjectiveText(Transform root, string name, float minY, float maxY, float fontSize)
        {
            var target = root.Find(name) as RectTransform;
            if (target == null) return;
            target.anchorMin = new Vector2(0.02f, minY);
            target.anchorMax = new Vector2(0.34f, maxY);
            target.offsetMin = target.offsetMax = Vector2.zero;
            var text = target.GetComponent<TMP_Text>();
            if (text != null) text.fontSize = fontSize;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            root.GetComponent<Image>().color = new Color(0.035f, 0.065f, 0.095f, 0.97f);
            return root;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 position, Vector2 size, string label)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            root.GetComponent<Image>().color = new Color(0.14f, 0.28f, 0.34f, 1f);
            var text = CreateText(root.transform, "Label", Vector2.zero, size, 17f, label);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            text.alignment = TextAlignmentOptions.Center;
            return root.GetComponent<Button>();
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, Vector2 position, Vector2 size, float fontSize, string value)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = root.AddComponent<TextMeshProUGUI>();
            var font = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (font != null) text.font = font;
            text.text = value; text.fontSize = fontSize; text.color = Color.white; text.alignment = TextAlignmentOptions.TopLeft; text.raycastTarget = false;
            return text;
        }

        private static void SetSize(GameObject root, Vector2 size) => root.GetComponent<RectTransform>().sizeDelta = size;
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
