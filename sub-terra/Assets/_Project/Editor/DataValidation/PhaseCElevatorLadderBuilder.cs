using System;
using System.Collections.Generic;
using System.Linq;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Editor.DataValidation;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Editor
{
    /// <summary>Phase C 엘리베이터 정거장과 저장 가능한 사다리 에셋을 반복 생성한다.</summary>
    public static class PhaseCElevatorLadderBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string LadderPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Traversal/Ladder.prefab";
        /// <summary>시설 건설 설치용 세로 5칸 사다리. 씬 기본 Ladder와 분리한다.</summary>
        public const string LadderBuildablePrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Traversal/Ladder_Buildable.prefab";
        public const string ElevatorPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Traversal/StartElevator.prefab";
        public const string LadderDataPath =
            "Assets/_Project/Data/Buildings/Building_Ladder_Basic.asset";
        public const string LadderPlacementPath =
            "Assets/_Project/Data/Buildings/LadderPlacement.asset";

        private const string LadderId = "building.ladder.basic";
        private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        [MenuItem("SubTerra/MVP2/Build Phase C Elevator And Ladder")]
        public static string BuildAll()
        {
            EnsureFolder("Assets/_Project/Prefabs/Gameplay/Traversal");
            // 씬 기본 사다리는 세로 1유닛, 설치용은 세로 5칸으로 분리한다.
            var ladderPrefab = BuildLadderPrefab(LadderPrefabPath, "Ladder", height: 1f);
            var buildablePrefab = BuildLadderPrefab(
                LadderBuildablePrefabPath, "Ladder_Buildable", height: 5f);
            var elevatorPrefab = BuildElevatorPrefab();
            var placement = BuildLadderDefinitions(buildablePrefab);
            AddLadderToCatalog();
            WireIntegrationScene(ladderPrefab, elevatorPrefab, placement);
            PhaseLMenuSceneBuilder.BuildSurfaceBasePrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Phase C elevator, ladder, catalog, and Integration Scene wired.";
        }

        private static GameObject BuildLadderPrefab(string path, string name, float height)
        {
            var root = new GameObject(
                name,
                typeof(SpriteRenderer),
                typeof(BoxCollider2D),
                typeof(LadderZone));
            var renderer = root.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.color = new Color(0.95f, 0.67f, 0.18f, 0.9f);
            renderer.size = new Vector2(0.65f, height);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.sortingOrder = 4;

            var zone = root.GetComponent<BoxCollider2D>();
            zone.isTrigger = true;
            zone.size = new Vector2(0.7f, height);

            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject BuildElevatorPrefab()
        {
            var root = new GameObject(
                "StartElevator",
                typeof(SpriteRenderer),
                typeof(BoxCollider2D),
                typeof(ElevatorController));
            // Collider size is local; non-1 scale shrinks the boarding trigger below usable size.
            root.transform.localScale = Vector3.one;
            var renderer = root.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.color = new Color(0.16f, 0.52f, 0.62f, 0.95f);
            renderer.size = new Vector2(2.2f, 2.8f);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.sortingOrder = 3;

            var zone = root.GetComponent<BoxCollider2D>();
            zone.isTrigger = true;
            zone.size = new Vector2(2.2f, 2.8f);

            var boarding = new GameObject("BoardingAnchor").transform;
            boarding.SetParent(root.transform, false);
            boarding.localPosition = new Vector3(0f, -0.65f, 0f);

            var safeExit = new GameObject("SafeExit").transform;
            safeExit.SetParent(root.transform, false);
            safeExit.localPosition = new Vector3(2f, -0.65f, 0f);

            var statusObject = new GameObject("StatusText", typeof(TextMeshPro));
            statusObject.transform.SetParent(root.transform, false);
            statusObject.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            var status = statusObject.GetComponent<TextMeshPro>();
            status.text = "Idle · 탑승 대기";
            status.fontSize = 2f;
            status.alignment = TextAlignmentOptions.Center;
            status.color = new Color(0.75f, 0.98f, 1f);
            status.sortingOrder = 10;
            status.rectTransform.sizeDelta = new Vector2(8f, 1f);

            var controller = root.GetComponent<ElevatorController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("inputActions").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            serialized.FindProperty("boardingAnchor").objectReferenceValue = boarding;
            serialized.FindProperty("safeExitPoint").objectReferenceValue = safeExit;
            // Default(0) 레이어를 쓰면 플레이어/타일이 항상 막아서 이동이 거부된다.
            // 장애물 전용 레이어를 쓰기 전까지는 비활성(0)으로 둔다.
            serialized.FindProperty("exitBlockerLayers").intValue = 0;
            serialized.FindProperty("statusText").objectReferenceValue = status;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var saved = PrefabUtility.SaveAsPrefabAsset(root, ElevatorPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static BuildingPlacementDefinition BuildLadderDefinitions(GameObject prefab)
        {
            var data = LoadOrCreate<BuildingData>(LadderDataPath);
            data.EditorSet(
                LadderId,
                "기본 사다리",
                "깊은 수직 갱도에서 중력 없이 오르내릴 수 있습니다. 철 1개·구리 3개로 세로 5칸 설치합니다.",
                prefab,
                AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                0,
                new List<ItemCostEntry>
                {
                    new("mineral.iron", 1),
                    new("mineral.copper", 3)
                });
            EditorUtility.SetDirty(data);

            var placement = LoadOrCreate<BuildingPlacementDefinition>(LadderPlacementPath);
            placement.EditorSet(LadderId, prefab, new Vector2Int(1, 5), needsGround: false);
            placement.EditorSetCosts(
                new ItemCostDto("mineral.iron", 1),
                new ItemCostDto("mineral.copper", 3));
            EditorUtility.SetDirty(placement);
            return placement;
        }

        private static void AddLadderToCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(
                "Assets/_Project/Data/Catalog/GameDataCatalog.asset");
            var ladder = AssetDatabase.LoadAssetAtPath<BuildingData>(LadderDataPath);
            if (catalog == null || ladder == null)
            {
                throw new InvalidOperationException("GameDataCatalog 또는 사다리 정의가 없습니다.");
            }

            var serialized = new SerializedObject(catalog);
            var buildings = serialized.FindProperty("buildings");
            for (var index = 0; index < buildings.arraySize; index++)
            {
                if (buildings.GetArrayElementAtIndex(index).objectReferenceValue == ladder)
                {
                    return;
                }
            }

            buildings.InsertArrayElementAtIndex(buildings.arraySize);
            buildings.GetArrayElementAtIndex(buildings.arraySize - 1).objectReferenceValue = ladder;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void WireIntegrationScene(
            GameObject ladderPrefab,
            GameObject elevatorPrefab,
            BuildingPlacementDefinition placement)
        {
            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Single);
            DestroySceneObject(scene, "PhaseCTraversal");

            var traversal = new GameObject("PhaseCTraversal");
            SceneManager.MoveGameObjectToScene(traversal, scene);

            var elevator = (GameObject)PrefabUtility.InstantiatePrefab(elevatorPrefab, scene);
            elevator.name = "StartElevatorStation";
            elevator.transform.SetParent(traversal.transform, false);
            elevator.transform.position = new Vector3(-6.5f, 0f, 0f);

            // 시작 세로 통로를 채굴하면 즉시 6칸 이상 사다리를 검증할 수 있는 데모 구간.
            // 기본 Ladder Prefab(높이 1)을 y=7로 스케일해 이전 세로 길이를 유지한다.
            var demoLadder = (GameObject)PrefabUtility.InstantiatePrefab(ladderPrefab, scene);
            demoLadder.name = "LadderTrainingShaft_6m";
            demoLadder.transform.SetParent(traversal.transform, false);
            demoLadder.transform.position = new Vector3(-9.5f, -5f, 0f);
            demoLadder.transform.localScale = new Vector3(1f, 7f, 1f);

            var bridgeHost = new GameObject("ElevatorTravelBridge");
            bridgeHost.transform.SetParent(traversal.transform, false);
            bridgeHost.AddComponent<ElevatorTravelBridge>();

            var placementSystem = FindInScene<BuildingPlacementSystem>(scene);
            AppendObjectReference(
                new SerializedObject(placementSystem),
                "restoreDefinitions",
                placement);

            var placementBridge = FindInScene<GameplayBuildingPlacementBridge>(scene);
            AppendPlacementBinding(placementBridge, placement);
            RestoreAuthoredMineralMarkers(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void RestoreAuthoredMineralMarkers(Scene scene)
        {
            var tilemap = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                .FirstOrDefault(map => map.name == "ForegroundTilemap");
            if (tilemap == null)
            {
                return;
            }

            tilemap.SetTile(
                new Vector3Int(-8, -2, 0),
                AssetDatabase.LoadAssetAtPath<TileBase>(
                    "Assets/_Project/Tilemaps/DemoWorld/ElevatorProtectedBlock.asset"));
            tilemap.SetTile(
                new Vector3Int(-7, -2, 0),
                AssetDatabase.LoadAssetAtPath<TileBase>(
                    "Assets/_Project/Tilemaps/DemoWorld/ElevatorProtectedBlock.asset"));
            tilemap.SetTile(
                new Vector3Int(-6, -2, 0),
                AssetDatabase.LoadAssetAtPath<TileBase>(
                    "Assets/_Project/Tilemaps/DemoWorld/ElevatorProtectedBlock.asset"));
            tilemap.SetTile(
                new Vector3Int(-3, -3, 0),
                AssetDatabase.LoadAssetAtPath<TileBase>(
                    "Assets/_Project/Tilemaps/DemoWorld/Iron.asset"));
            tilemap.SetTile(
                new Vector3Int(2, -5, 0),
                AssetDatabase.LoadAssetAtPath<TileBase>(
                    "Assets/_Project/Tilemaps/DemoWorld/Lithium.asset"));
        }

        private static void AppendPlacementBinding(
            GameplayBuildingPlacementBridge bridge,
            BuildingPlacementDefinition placement)
        {
            if (bridge == null)
            {
                throw new InvalidOperationException("GameplayBuildingPlacementBridge가 없습니다.");
            }

            var serialized = new SerializedObject(bridge);
            var bindings = serialized.FindProperty("bindings");
            for (var index = 0; index < bindings.arraySize; index++)
            {
                if (bindings.GetArrayElementAtIndex(index)
                        .FindPropertyRelative("buildingId").stringValue == LadderId)
                {
                    return;
                }
            }

            bindings.InsertArrayElementAtIndex(bindings.arraySize);
            var element = bindings.GetArrayElementAtIndex(bindings.arraySize - 1);
            element.FindPropertyRelative("buildingId").stringValue = LadderId;
            element.FindPropertyRelative("definition").objectReferenceValue = placement;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bridge);
        }

        private static void AppendObjectReference(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            if (serialized.targetObject == null)
            {
                throw new InvalidOperationException(propertyName + " 대상이 없습니다.");
            }

            var array = serialized.FindProperty(propertyName);
            for (var index = 0; index < array.arraySize; index++)
            {
                if (array.GetArrayElementAtIndex(index).objectReferenceValue == value)
                {
                    return;
                }
            }

            array.InsertArrayElementAtIndex(array.arraySize);
            array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(serialized.targetObject);
        }

        private static T FindInScene<T>(Scene scene) where T : UnityEngine.Object
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .FirstOrDefault();
        }

        private static void DestroySceneObject(Scene scene, string objectName)
        {
            var target = scene.GetRootGameObjects().FirstOrDefault(root => root.name == objectName);
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
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
    }
}
