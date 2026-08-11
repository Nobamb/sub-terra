#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Inventory;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>prompt-B 46 대상 에셋과 Integration Scene만 수정하는 전용 빌더.</summary>
    public static class PromptB46EmergencyEscapePortalBuilder
    {
        public const string BuildingMenuPrefabPath =
            "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string PortalPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Buildings/EmergencyEscapePortal.prefab";
        public const string PortalDataPath =
            "Assets/_Project/Data/Buildings/Building_EmergencyEscapePortal.asset";
        public const string PortalPlacementPath =
            "Assets/_Project/Data/Buildings/Placement/escape_portal_emergencyPlacement.asset";

        private const string CatalogPath =
            "Assets/_Project/Data/Catalog/GameDataCatalog.asset";
        private const string IconPath =
            "Assets/_Project/Data/Icons/DataPlaceholder.asset";
        private const string InputActionsPath =
            "Assets/Settings/InputSystem_Actions.inputactions";

        [MenuItem("SubTerra/UI/Build Prompt-B 46 Emergency Escape Portal")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            var prefab = BuildPortalPrefab();
            var data = BuildPortalData(prefab);
            RegisterCatalog(data);
            var definition = BuildPlacementDefinition(prefab);
            UpdateBuildingMenuPrefab();
            WireIntegrationScene(definition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Prompt-B 46 portal data, BuildingMenu, and Integration Scene wired.";
        }

        private static GameObject BuildPortalPrefab()
        {
            var root = new GameObject(
                "EmergencyEscapePortal",
                typeof(BoxCollider2D),
                typeof(BuildingInstance),
                typeof(PowerNode),
                typeof(EmergencyEscapePortal));
            try
            {
                var zone = root.GetComponent<BoxCollider2D>();
                zone.isTrigger = true;
                zone.size = new Vector2(1.4f, 2.2f);
                zone.offset = new Vector2(0f, 0.55f);

                var power = root.GetComponent<PowerNode>();
                power.Configure(null, false, 0, 30, PowerPriority.Critical);

                var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                CreateVisual(root.transform, "OuterFrame", sprite,
                    new Vector3(0f, 0.55f, 0f), new Vector2(1.25f, 2f),
                    new Color(0.1f, 0.8f, 0.95f, 0.9f), 5);
                CreateVisual(root.transform, "PortalField", sprite,
                    new Vector3(0f, 0.55f, -0.01f), new Vector2(0.72f, 1.55f),
                    new Color(0.08f, 0.16f, 0.35f, 0.72f), 6);

                var portalSo = new SerializedObject(root.GetComponent<EmergencyEscapePortal>());
                portalSo.FindProperty("inputActions").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
                portalSo.FindProperty("powerNode").objectReferenceValue = power;
                portalSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PortalPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
        }

        private static void CreateVisual(
            Transform parent,
            string name,
            Sprite sprite,
            Vector3 localPosition,
            Vector2 size,
            Color color,
            int sortingOrder)
        {
            var visual = new GameObject(name, typeof(SpriteRenderer));
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        private static BuildingData BuildPortalData(GameObject prefab)
        {
            if (prefab == null)
            {
                throw new InvalidOperationException("Emergency escape portal prefab was not created.");
            }

            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(PortalDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<BuildingData>();
                AssetDatabase.CreateAsset(data, PortalDataPath);
            }

            var icon = LoadFirstSprite(IconPath);
            data.EditorSet(
                DataIds.Buildings.EmergencyEscapePortal,
                "긴급 탈출 포탈",
                "E키로 사용합니다. 100G와 최대 전력의 10%를 소모해 최근 전진기지 코어 또는 엘리베이터로 이동합니다.",
                prefab,
                icon,
                30,
                new List<ItemCostEntry>
                {
                    new(DataIds.Minerals.Iron, 3),
                    new(DataIds.Minerals.Lithium, 3)
                });
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void RegisterCatalog(BuildingData data)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException("GameDataCatalog is missing.");
            }

            var buildings = new List<BuildingData>(catalog.Buildings.Count + 1);
            for (var i = 0; i < catalog.Buildings.Count; i++)
            {
                var current = catalog.Buildings[i];
                if (current != null && current.Id != DataIds.Buildings.EmergencyEscapePortal)
                {
                    buildings.Add(current);
                }
            }
            buildings.Add(data);

            catalog.EditorSetLists(
                new List<MineralData>(catalog.Minerals),
                new List<MiningTileData>(catalog.MiningTiles),
                buildings,
                new List<RecipeData>(catalog.Recipes),
                new List<UpgradeData>(catalog.Upgrades),
                new List<DialogueTemplateData>(catalog.Dialogues));
            EditorUtility.SetDirty(catalog);
        }

        private static BuildingPlacementDefinition BuildPlacementDefinition(GameObject prefab)
        {
            var definition = AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(PortalPlacementPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
                AssetDatabase.CreateAsset(definition, PortalPlacementPath);
            }

            definition.EditorSet(
                DataIds.Buildings.EmergencyEscapePortal,
                prefab,
                new Vector2Int(2, 2),
                true);
            definition.EditorSetCosts(
                new ItemCostDto(DataIds.Minerals.Iron, 3),
                new ItemCostDto(DataIds.Minerals.Lithium, 3));
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void UpdateBuildingMenuPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(BuildingMenuPrefabPath);
            try
            {
                var binder = root.GetComponent<BuildingMenuBinder>();
                var source = FindChild(root.transform, "Select_" + DataIds.Buildings.OutpostCoreBasic);
                if (binder == null || source == null)
                {
                    throw new InvalidOperationException("BuildingMenu source button is missing.");
                }

                var targetName = "Select_" + DataIds.Buildings.EmergencyEscapePortal;
                var entryObject = FindChild(root.transform, targetName)?.gameObject;
                if (entryObject == null)
                {
                    entryObject = UnityEngine.Object.Instantiate(source.gameObject, source.parent);
                    entryObject.name = targetName;
                }

                var rect = entryObject.GetComponent<RectTransform>();
                var sourceRect = source.GetComponent<RectTransform>();
                rect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -42f);
                rect.sizeDelta = sourceRect.sizeDelta;
                entryObject.GetComponent<BuildingMenuEntryButton>()
                    .EditorSet(DataIds.Buildings.EmergencyEscapePortal, binder);
                var label = entryObject.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = "긴급 탈출 포탈";
                }

                PrefabUtility.SaveAsPrefabAsset(root, BuildingMenuPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WireIntegrationScene(BuildingPlacementDefinition definition)
        {
            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Additive);
            try
            {
                var placement = FindInScene<BuildingPlacementSystem>(scene);
                var placementBridge = FindInScene<GameplayBuildingPlacementBridge>(scene);
                var applicationRoot = FindInSceneByName(scene, "ApplicationRoot");
                var player = FindInSceneByName(scene, "Player");
                var fallback = FindInSceneByName(scene, "RunFailureSurfaceFallback");
                var elevator = FindInSceneByName(scene, "StartElevatorStation");
                var elevatorCenter = elevator != null
                    ? FindChild(elevator.transform, "BoardingAnchor")
                    : null;
                if (placement == null || placementBridge == null || applicationRoot == null
                    || player == null || fallback == null || elevatorCenter == null)
                {
                    throw new InvalidOperationException("Integration portal references are incomplete.");
                }

                MergeDefinition(new SerializedObject(placement), "restoreDefinitions", definition);
                MergeBinding(new SerializedObject(placementBridge), definition);

                var escapeBridge = applicationRoot.GetComponent<EmergencyEscapePortalRuntimeBridge>()
                    ?? applicationRoot.AddComponent<EmergencyEscapePortalRuntimeBridge>();
                var bridgeSo = new SerializedObject(escapeBridge);
                bridgeSo.FindProperty("playerTransform").objectReferenceValue = player.transform;
                bridgeSo.FindProperty("elevatorCenter").objectReferenceValue = elevatorCenter;
                bridgeSo.ApplyModifiedPropertiesWithoutUndo();

                player.transform.position = elevatorCenter.position;
                fallback.transform.position = elevatorCenter.position;
                PreserveInventoryPanelReferences(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void PreserveInventoryPanelReferences(Scene scene)
        {
            var controller = FindInScene<HudPanelChromeController>(scene);
            var inventoryView = FindInScene<InventoryPanelView>(scene);
            var inventoryRoot = inventoryView != null ? inventoryView.gameObject : null;
            var close = inventoryRoot != null
                ? FindChild(inventoryRoot.transform, "CloseButton")?.GetComponent<Button>()
                : null;
            if (controller == null || inventoryView == null || close == null)
            {
                throw new InvalidOperationException("Inventory panel references must remain wired.");
            }

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("inventoryPanelView").objectReferenceValue = inventoryView;
            controllerSo.FindProperty("inventoryPanelRoot").objectReferenceValue = inventoryRoot;
            controllerSo.FindProperty("inventoryCloseButton").objectReferenceValue = close;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void MergeDefinition(
            SerializedObject targetObject,
            string propertyName,
            BuildingPlacementDefinition definition)
        {
            var target = targetObject.FindProperty(propertyName);
            for (var i = 0; i < target.arraySize; i++)
            {
                var current = target.GetArrayElementAtIndex(i).objectReferenceValue
                    as BuildingPlacementDefinition;
                if (current != null && current.BuildingId == definition.BuildingId)
                {
                    target.GetArrayElementAtIndex(i).objectReferenceValue = definition;
                    targetObject.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }

            target.InsertArrayElementAtIndex(target.arraySize);
            target.GetArrayElementAtIndex(target.arraySize - 1).objectReferenceValue = definition;
            targetObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void MergeBinding(
            SerializedObject bridgeObject,
            BuildingPlacementDefinition definition)
        {
            var bindings = bridgeObject.FindProperty("bindings");
            for (var i = 0; i < bindings.arraySize; i++)
            {
                var binding = bindings.GetArrayElementAtIndex(i);
                if (binding.FindPropertyRelative("buildingId").stringValue == definition.BuildingId)
                {
                    binding.FindPropertyRelative("definition").objectReferenceValue = definition;
                    bridgeObject.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }

            bindings.InsertArrayElementAtIndex(bindings.arraySize);
            var added = bindings.GetArrayElementAtIndex(bindings.arraySize - 1);
            added.FindPropertyRelative("buildingId").stringValue = definition.BuildingId;
            added.FindPropertyRelative("definition").objectReferenceValue = definition;
            bridgeObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite LoadFirstSprite(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static Transform FindChild(Transform root, string name)
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

        private static GameObject FindInSceneByName(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindChild(root.transform, name);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
#endif
