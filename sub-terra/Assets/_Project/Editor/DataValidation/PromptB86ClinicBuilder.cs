#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.UI.Building;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>prompt-B 86 보건소 데이터·프리팹·건설 메뉴·Integration 배선을 한정 갱신한다.</summary>
    public static class PromptB86ClinicBuilder
    {
        public const string BuildingMenuPrefabPath =
            "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string ClinicPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Power/ClinicFacility.prefab";
        public const string ClinicDataPath =
            "Assets/_Project/Data/Buildings/Building_Clinic_Basic.asset";
        public const string ClinicPlacementPath =
            "Assets/_Project/Data/Buildings/Placement/clinic_basicPlacement.asset";

        private const string CatalogPath =
            "Assets/_Project/Data/Catalog/GameDataCatalog.asset";
        private const string ChargerDataPath =
            "Assets/_Project/Data/Buildings/Building_Charger_Basic.asset";
        private const float FirstButtonY = -246f;
        private const float LastButtonY = -498f;
        private const float ButtonX = 20f;
        private static readonly Vector2 ButtonSize = new Vector2(132f, 29f);

        private static readonly string[] ButtonOrder =
        {
            DataIds.Buildings.SupportBasic,
            DataIds.Buildings.LadderBasic,
            DataIds.Buildings.LightBasic,
            DataIds.Buildings.ChargerBasic,
            DataIds.Buildings.ClinicBasic,
            DataIds.Buildings.StorageBasic,
            DataIds.Buildings.SettlementBasic,
            DataIds.Buildings.OutpostCoreBasic,
            DataIds.Buildings.EmergencyEscapePortal
        };

        private static readonly string[] ButtonLabels =
        {
            "버팀목",
            "사다리",
            "조명",
            "충전기",
            "보건소",
            "보관함",
            "정산 콘솔",
            "전진기지 코어",
            "긴급 탈출 포탈"
        };

        [MenuItem("SubTerra/UI/Build Prompt-B 86 Clinic")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            var prefab = BuildClinicPrefab();
            var data = BuildClinicData(prefab);
            var placement = BuildPlacement(data);
            RegisterCatalog(data);
            UpdateBuildingMenuPrefab();
            WireIntegrationScene(placement);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Prompt-B 86 clinic data, menu, prefab, and Integration wiring ready.";
        }

        private static GameObject BuildClinicPrefab()
        {
            var root = new GameObject("ClinicFacility");
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var body = root.AddComponent<SpriteRenderer>();
            body.sprite = sprite;
            body.color = Color.white;
            body.sortingOrder = 4;
            root.transform.localScale = Vector3.one;
            root.AddComponent<BuildingInstance>();

            var node = root.AddComponent<PowerNode>();
            node.Configure(null, false, 0, 3, PowerPriority.Normal);

            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);
            CreateVisual(visualRoot.transform, "WhiteBody", sprite, Color.white,
                new Vector3(5.5f, 5.5f, 1f), 4);
            CreateVisual(visualRoot.transform, "RedCrossHorizontal", sprite,
                new Color(0.85f, 0.08f, 0.1f, 1f), new Vector3(3.2f, 0.85f, 1f), 6);
            CreateVisual(visualRoot.transform, "RedCrossVertical", sprite,
                new Color(0.85f, 0.08f, 0.1f, 1f), new Vector3(0.85f, 3.2f, 1f), 6);
            body.enabled = false;

            var poweredRoot = new GameObject("PoweredVisualRoot");
            poweredRoot.transform.SetParent(root.transform, false);
            CreateVisual(poweredRoot.transform, "PowerGlow", sprite,
                new Color(0.35f, 1f, 0.75f, 0.22f), new Vector3(6.1f, 6.1f, 1f), 3);
            poweredRoot.SetActive(false);

            var facility = root.AddComponent<PowerFacility>();
            var serialized = new SerializedObject(facility);
            serialized.FindProperty("powerNode").objectReferenceValue = node;
            var visuals = serialized.FindProperty("poweredVisuals");
            visuals.arraySize = 1;
            visuals.GetArrayElementAtIndex(0).objectReferenceValue = poweredRoot;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var saved = PrefabUtility.SaveAsPrefabAsset(root, ClinicPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (saved == null)
            {
                throw new InvalidOperationException("Failed to create clinic runtime prefab.");
            }

            return saved;
        }

        private static void CreateVisual(
            Transform parent,
            string name,
            Sprite sprite,
            Color color,
            Vector3 scale,
            int sortingOrder)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localScale = scale;
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        private static BuildingData BuildClinicData(GameObject prefab)
        {
            var charger = AssetDatabase.LoadAssetAtPath<BuildingData>(ChargerDataPath);
            if (charger == null)
            {
                throw new InvalidOperationException("Charger BuildingData is missing.");
            }

            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(ClinicDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<BuildingData>();
                AssetDatabase.CreateAsset(data, ClinicDataPath);
            }

            data.EditorSet(
                DataIds.Buildings.ClinicBasic,
                "보건소",
                "전력망에 연결되면 플레이어 체력을 최대치까지 회복합니다.",
                prefab,
                charger.Icon,
                3,
                new List<ItemCostEntry>
                {
                    new ItemCostEntry(DataIds.Minerals.Copper, 3)
                });
            EditorUtility.SetDirty(data);
            return data;
        }

        private static BuildingPlacementDefinition BuildPlacement(BuildingData data)
        {
            var placement = AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(
                ClinicPlacementPath);
            if (placement == null)
            {
                placement = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
                AssetDatabase.CreateAsset(placement, ClinicPlacementPath);
            }

            placement.EditorSet(data.Id, data.RuntimePrefab, Vector2Int.one, true);
            placement.EditorSetCosts(new ItemCostDto(DataIds.Minerals.Copper, 3));
            EditorUtility.SetDirty(placement);
            return placement;
        }

        private static void RegisterCatalog(BuildingData clinic)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException("GameDataCatalog is missing.");
            }

            var buildings = new List<BuildingData>(catalog.Buildings.Count + 1);
            var inserted = false;
            for (var i = 0; i < catalog.Buildings.Count; i++)
            {
                var current = catalog.Buildings[i];
                if (current == null || current.Id == DataIds.Buildings.ClinicBasic)
                {
                    continue;
                }

                buildings.Add(current);
                if (!inserted && current.Id == DataIds.Buildings.ChargerBasic)
                {
                    buildings.Add(clinic);
                    inserted = true;
                }
            }

            if (!inserted)
            {
                buildings.Add(clinic);
            }

            catalog.EditorSetLists(
                new List<MineralData>(catalog.Minerals),
                new List<MiningTileData>(catalog.MiningTiles),
                buildings,
                new List<RecipeData>(catalog.Recipes),
                new List<UpgradeData>(catalog.Upgrades),
                new List<DialogueTemplateData>(catalog.Dialogues));
            EditorUtility.SetDirty(catalog);
        }

        private static void UpdateBuildingMenuPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(BuildingMenuPrefabPath);
            try
            {
                var binder = root.GetComponent<BuildingMenuBinder>();
                var charger = FindChild(
                    root.transform,
                    "Select_" + DataIds.Buildings.ChargerBasic);
                if (binder == null || charger == null)
                {
                    throw new InvalidOperationException("BuildingMenu charger button is missing.");
                }

                var clinicName = "Select_" + DataIds.Buildings.ClinicBasic;
                var clinic = FindChild(root.transform, clinicName);
                if (clinic == null)
                {
                    var clone = UnityEngine.Object.Instantiate(charger.gameObject, charger.parent);
                    clone.name = clinicName;
                    clinic = clone.transform;
                }

                var step = (LastButtonY - FirstButtonY) / (ButtonOrder.Length - 1);
                for (var i = 0; i < ButtonOrder.Length; i++)
                {
                    var entry = FindChild(root.transform, "Select_" + ButtonOrder[i]);
                    if (entry == null)
                    {
                        throw new InvalidOperationException(
                            "Missing building button: " + ButtonOrder[i]);
                    }

                    var rect = entry.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(ButtonX, FirstButtonY + step * i);
                    rect.sizeDelta = ButtonSize;

                    var label = entry.GetComponentInChildren<TMP_Text>(true);
                    if (label != null)
                    {
                        label.text = ButtonLabels[i];
                    }

                    var button = entry.GetComponent<BuildingMenuEntryButton>();
                    if (button != null)
                    {
                        button.EditorSet(ButtonOrder[i], binder);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, BuildingMenuPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WireIntegrationScene(BuildingPlacementDefinition clinic)
        {
            var scene = SceneManager.GetSceneByPath(IntegrationScenePath);
            var wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded)
            {
                scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Additive);
            }

            try
            {
                var placement = FindInScene<BuildingPlacementSystem>(scene);
                var bridge = FindInScene<GameplayBuildingPlacementBridge>(scene);
                if (placement == null || bridge == null)
                {
                    throw new InvalidOperationException(
                        "Integration BuildingPlacementSystem/Bridge is missing.");
                }

                MergeDefinition(new SerializedObject(placement), clinic);
                MergeBinding(new SerializedObject(bridge), clinic);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (!wasLoaded && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void MergeDefinition(
            SerializedObject targetObject,
            BuildingPlacementDefinition definition)
        {
            var definitions = targetObject.FindProperty("restoreDefinitions");
            for (var i = 0; i < definitions.arraySize; i++)
            {
                var current = definitions.GetArrayElementAtIndex(i).objectReferenceValue
                    as BuildingPlacementDefinition;
                if (current != null && current.BuildingId == definition.BuildingId)
                {
                    definitions.GetArrayElementAtIndex(i).objectReferenceValue = definition;
                    targetObject.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }

            definitions.InsertArrayElementAtIndex(definitions.arraySize);
            definitions.GetArrayElementAtIndex(definitions.arraySize - 1).objectReferenceValue =
                definition;
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

        private static Transform FindChild(Transform root, string name)
        {
            var children = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i].name == name)
                {
                    return children[i];
                }
            }

            return null;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var component = roots[i].GetComponentInChildren<T>(true);
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
