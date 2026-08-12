#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.UI.Building;
using SubTerra.Gameplay.Building;
using SubTerra.Shared;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 48: 기존 사다리 에셋을 시설 건설 창에 추가하고
    /// 버튼 영역이 창 밖으로 나가지 않도록 좌측 버튼 Y를 재배치한다.
    /// 대상: BuildingMenu 프리팹, GameDataCatalog, Integration Scene 배치 바인딩.
    /// </summary>
    public static class PromptB48LadderBuildingMenuBuilder
    {
        public const string BuildingMenuPrefabPath =
            "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string LadderDataPath =
            "Assets/_Project/Data/Buildings/Building_Ladder_Basic.asset";
        public const string LadderPlacementPath =
            "Assets/_Project/Data/Buildings/LadderPlacement.asset";
        /// <summary>씬 기본/데모 사다리. 세로 1유닛 기준(스케일로 연장).</summary>
        public const string LadderPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Traversal/Ladder.prefab";
        /// <summary>시설 건설 설치용. 세로 5칸 footprint.</summary>
        public const string LadderBuildablePrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Traversal/Ladder_Buildable.prefab";

        private const string CatalogPath =
            "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        // 기존 7버튼 기준 상단/하단을 유지하고 8버튼으로 간격을 균등 분배한다.
        // 첫 버튼 y=-246, 마지막 y=-498, 하단 여백(28px)을 그대로 활용.
        private static readonly string[] ButtonOrder =
        {
            DataIds.Buildings.SupportBasic,
            DataIds.Buildings.LadderBasic,
            DataIds.Buildings.LightBasic,
            DataIds.Buildings.ChargerBasic,
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
            "보관함",
            "정산 콘솔",
            "전진기지 코어",
            "긴급 탈출 포탈"
        };

        private const float FirstButtonY = -246f;
        private const float LastButtonY = -498f;
        private const float ButtonX = 20f;
        private static readonly Vector2 ButtonSize = new Vector2(132f, 34f);

        [MenuItem("SubTerra/UI/Build Prompt-B 48 Ladder Building Menu")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            EnsureLadderAssets();
            RegisterCatalog();
            UpdateBuildingMenuPrefab();
            EnsureIntegrationPlacementBinding();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Prompt-B 48 ladder added to BuildingMenu, catalog, and placement bindings.";
        }

        private static void EnsureLadderAssets()
        {
            // 1) 씬 기본 사다리: 이전 세로 1유닛 높이로 롤백.
            EnsureLadderVisualPrefab(
                LadderPrefabPath,
                "Ladder",
                height: 1f,
                createIfMissing: false);

            // 2) 시설 건설용: 세로 5칸 전용 Prefab (기본 Ladder와 분리).
            var buildable = EnsureLadderVisualPrefab(
                LadderBuildablePrefabPath,
                "Ladder_Buildable",
                height: 5f,
                createIfMissing: true);
            if (buildable == null)
            {
                throw new InvalidOperationException(
                    "Buildable ladder prefab is missing: " + LadderBuildablePrefabPath);
            }

            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(LadderDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<BuildingData>();
                AssetDatabase.CreateAsset(data, LadderDataPath);
            }

            data.EditorSet(
                DataIds.Buildings.LadderBasic,
                "기본 사다리",
                "깊은 수직 갱도에서 중력 없이 오르내릴 수 있습니다. 철 1개·구리 3개로 세로 5칸 설치합니다.",
                buildable,
                AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                0,
                new List<ItemCostEntry>
                {
                    new(DataIds.Minerals.Iron, 1),
                    new(DataIds.Minerals.Copper, 3)
                });
            EditorUtility.SetDirty(data);

            var placement = AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(LadderPlacementPath);
            if (placement == null)
            {
                placement = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
                AssetDatabase.CreateAsset(placement, LadderPlacementPath);
            }

            // 수직 갱도 빈 칸 5개에 설치하므로 지면 요구 없음. footprint = 가로 1 × 세로 5.
            placement.EditorSet(
                DataIds.Buildings.LadderBasic,
                buildable,
                new Vector2Int(1, 5),
                needsGround: false);
            placement.EditorSetCosts(
                new ItemCostDto(DataIds.Minerals.Iron, 1),
                new ItemCostDto(DataIds.Minerals.Copper, 3));
            EditorUtility.SetDirty(placement);
        }

        /// <summary>
        /// 사다리 시각 Prefab의 높이를 맞춘다.
        /// 기본 Ladder는 반드시 존재해야 하며, 설치용은 없으면 기본에서 복제한다.
        /// </summary>
        private static GameObject EnsureLadderVisualPrefab(
            string path,
            string rootName,
            float height,
            bool createIfMissing)
        {
            const float ladderWidth = 0.7f;
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing == null)
            {
                if (!createIfMissing)
                {
                    throw new InvalidOperationException("Ladder prefab is missing: " + path);
                }

                var source = AssetDatabase.LoadAssetAtPath<GameObject>(LadderPrefabPath);
                if (source == null)
                {
                    throw new InvalidOperationException(
                        "Source ladder prefab is missing: " + LadderPrefabPath);
                }

                EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
                if (!AssetDatabase.CopyAsset(LadderPrefabPath, path))
                {
                    throw new InvalidOperationException("Failed to copy ladder prefab to " + path);
                }
            }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                contents.name = rootName;
                contents.transform.localScale = Vector3.one;

                var renderer = contents.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.drawMode = SpriteDrawMode.Sliced;
                    renderer.size = new Vector2(0.65f, height);
                }

                var zone = contents.GetComponent<BoxCollider2D>();
                if (zone != null)
                {
                    zone.isTrigger = true;
                    zone.size = new Vector2(ladderWidth, height);
                    zone.offset = Vector2.zero;
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static void RegisterCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            var ladder = AssetDatabase.LoadAssetAtPath<BuildingData>(LadderDataPath);
            if (catalog == null || ladder == null)
            {
                throw new InvalidOperationException("GameDataCatalog or ladder BuildingData is missing.");
            }

            var buildings = new List<BuildingData>(catalog.Buildings.Count + 1);
            var inserted = false;
            for (var i = 0; i < catalog.Buildings.Count; i++)
            {
                var current = catalog.Buildings[i];
                if (current == null)
                {
                    continue;
                }

                // 기존 사다리 슬롯은 건너뛰고, 버팀목 바로 다음에 한 번만 삽입한다.
                if (current.Id == DataIds.Buildings.LadderBasic)
                {
                    continue;
                }

                buildings.Add(current);
                if (!inserted && current.Id == DataIds.Buildings.SupportBasic)
                {
                    buildings.Add(ladder);
                    inserted = true;
                }
            }

            if (!inserted)
            {
                buildings.Add(ladder);
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
                var support = FindChild(root.transform, "Select_" + DataIds.Buildings.SupportBasic);
                if (binder == null || support == null)
                {
                    throw new InvalidOperationException("BuildingMenu support button is missing.");
                }

                // 사다리 버튼이 없으면 버팀목 버튼을 복제해 만든다.
                var ladderName = "Select_" + DataIds.Buildings.LadderBasic;
                var ladderObject = FindChild(root.transform, ladderName)?.gameObject;
                if (ladderObject == null)
                {
                    ladderObject = UnityEngine.Object.Instantiate(support.gameObject, support.parent);
                    ladderObject.name = ladderName;
                }

                ladderObject.GetComponent<BuildingMenuEntryButton>()
                    .EditorSet(DataIds.Buildings.LadderBasic, binder);
                var ladderLabel = ladderObject.GetComponentInChildren<TMP_Text>(true);
                if (ladderLabel != null)
                {
                    ladderLabel.text = "사다리";
                }

                // 첫·마지막 버튼 Y를 유지한 채 8개 버튼을 균등 배치한다.
                var step = ButtonOrder.Length > 1
                    ? (LastButtonY - FirstButtonY) / (ButtonOrder.Length - 1)
                    : 0f;

                for (var i = 0; i < ButtonOrder.Length; i++)
                {
                    var buildingId = ButtonOrder[i];
                    var entry = FindChild(root.transform, "Select_" + buildingId);
                    if (entry == null)
                    {
                        throw new InvalidOperationException("Missing building button: " + buildingId);
                    }

                    var rect = entry.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(ButtonX, FirstButtonY + step * i);
                    rect.sizeDelta = ButtonSize;

                    var label = entry.GetComponentInChildren<TMP_Text>(true);
                    if (label != null && i < ButtonLabels.Length)
                    {
                        label.text = ButtonLabels[i];
                    }

                    var entryButton = entry.GetComponent<BuildingMenuEntryButton>();
                    if (entryButton != null)
                    {
                        entryButton.EditorSet(buildingId, binder);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, BuildingMenuPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureIntegrationPlacementBinding()
        {
            var placementDefinition =
                AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(LadderPlacementPath);
            if (placementDefinition == null)
            {
                throw new InvalidOperationException("LadderPlacement definition is missing.");
            }

            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Additive);
            try
            {
                var placement = FindInScene<BuildingPlacementSystem>(scene);
                var bridge = FindInScene<GameplayBuildingPlacementBridge>(scene);
                if (placement == null || bridge == null)
                {
                    throw new InvalidOperationException(
                        "Integration BuildingPlacementSystem/Bridge is missing.");
                }

                MergeDefinition(new SerializedObject(placement), "restoreDefinitions", placementDefinition);
                MergeBinding(new SerializedObject(bridge), placementDefinition);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
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
