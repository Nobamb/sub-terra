using SubTerra.App.Core.Data;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>Phase F Support Prefab과 최종 Integration Scene 참조를 Editor API로 보정한다.</summary>
    public static class PhaseFSupportBuilder
    {
        public const string SupportPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Buildings/SupportPillar.prefab";
        public const string SupportDefinitionPath =
            "Assets/_Project/Data/Buildings/SupportPillarPlacement.asset";
        public const string SupportDataPath =
            "Assets/_Project/Data/Buildings/Building_Support_Basic.asset";
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [MenuItem("SubTerra/MVP2/Build Phase F Support")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            var prefab = BuildSupportPrefab();
            var definition = BuildDefinition(prefab);
            WireBuildingData(prefab);
            WireIntegrationScene(definition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Phase F Support prefab/data/integration wired.";
        }

        private static GameObject BuildSupportPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(SupportPrefabPath);
            if (root == null)
            {
                root = new GameObject("SupportPillar");
            }

            try
            {
                root.name = "SupportPillar";
                root.transform.localScale = Vector3.one;

                var oldRenderer = root.GetComponent<SpriteRenderer>();
                var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/UISprite.psd");
                var color = oldRenderer != null
                    ? oldRenderer.color
                    : new Color(0.95f, 0.7f, 0.2f);
                if (oldRenderer != null)
                {
                    Object.DestroyImmediate(oldRenderer);
                }

                var visual = root.transform.Find("VisualRoot");
                if (visual == null)
                {
                    var visualObject = new GameObject("VisualRoot");
                    visualObject.transform.SetParent(root.transform, false);
                    visual = visualObject.transform;
                }

                visual.localPosition = Vector3.zero;
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one;
                var renderer = visual.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    renderer = visual.gameObject.AddComponent<SpriteRenderer>();
                }
                renderer.sprite = sprite;
                renderer.color = color;
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = new Vector2(0.4f, 1.8f);
                renderer.sortingOrder = 3;

                var collider = root.GetComponent<BoxCollider2D>();
                if (collider == null)
                {
                    collider = root.AddComponent<BoxCollider2D>();
                }
                collider.isTrigger = false;
                collider.offset = Vector2.zero;
                collider.size = new Vector2(0.4f, 1.8f);

                var support = root.GetComponent<StructuralSupport>();
                if (support == null)
                {
                    support = root.AddComponent<StructuralSupport>();
                }
                var supportSo = new SerializedObject(support);
                supportSo.FindProperty("radius").floatValue = 3f;
                supportSo.FindProperty("strength").intValue = 35;
                supportSo.ApplyModifiedPropertiesWithoutUndo();

                if (root.GetComponent<BuildingInstance>() == null)
                {
                    root.AddComponent<BuildingInstance>();
                }

                PrefabUtility.SaveAsPrefabAsset(root, SupportPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(SupportPrefabPath);
        }

        private static BuildingPlacementDefinition BuildDefinition(GameObject prefab)
        {
            var definition = AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(
                SupportDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
                AssetDatabase.CreateAsset(definition, SupportDefinitionPath);
            }

            definition.EditorSet(DataIds.Buildings.SupportBasic, prefab, Vector2Int.one, true);
            definition.EditorSetCosts(new ItemCostDto(DataIds.Minerals.Copper, 2));
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void WireBuildingData(GameObject prefab)
        {
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(SupportDataPath);
            if (data == null)
            {
                return;
            }

            data.EditorSet(
                data.Id,
                data.DisplayName,
                data.Description,
                prefab,
                data.Icon,
                data.PowerDraw,
                new System.Collections.Generic.List<ItemCostEntry>(data.BuildCosts));
            EditorUtility.SetDirty(data);
        }

        private static void WireIntegrationScene(BuildingPlacementDefinition definition)
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
                var player = FindInScene<PlayerMovement>(scene);
                if (placement == null || player == null)
                {
                    throw new System.InvalidOperationException(
                        "Phase F requires BuildingPlacementSystem and PlayerMovement in Integration Scene.");
                }

                var placementSo = new SerializedObject(placement);
                var tilemap = placementSo.FindProperty("terrainTilemap").objectReferenceValue as Tilemap;
                if (tilemap == null)
                {
                    throw new System.InvalidOperationException(
                        "Phase F requires the Integration terrain Tilemap reference.");
                }

                var area = tilemap.transform.Find("BuildingPlacementArea");
                if (area == null)
                {
                    var areaObject = new GameObject("BuildingPlacementArea");
                    areaObject.transform.SetParent(tilemap.transform, false);
                    area = areaObject.transform;
                }

                var bounds = tilemap.localBounds;
                area.localPosition = bounds.center;
                area.localRotation = Quaternion.identity;
                area.localScale = Vector3.one;
                var areaCollider = area.GetComponent<BoxCollider2D>();
                if (areaCollider == null)
                {
                    areaCollider = area.gameObject.AddComponent<BoxCollider2D>();
                }
                areaCollider.isTrigger = true;
                areaCollider.offset = Vector2.zero;
                areaCollider.size = new Vector2(bounds.size.x, bounds.size.y);

                placementSo.FindProperty("placementOrigin").objectReferenceValue = player.transform;
                placementSo.FindProperty("maximumPlacementDistance").floatValue = 6f;
                placementSo.FindProperty("allowedPlacementArea").objectReferenceValue = areaCollider;

                var restoreDefinitions = placementSo.FindProperty("restoreDefinitions");
                var found = false;
                for (var i = 0; i < restoreDefinitions.arraySize; i++)
                {
                    if (restoreDefinitions.GetArrayElementAtIndex(i).objectReferenceValue == definition)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    var index = restoreDefinitions.arraySize;
                    restoreDefinitions.InsertArrayElementAtIndex(index);
                    restoreDefinitions.GetArrayElementAtIndex(index).objectReferenceValue = definition;
                }

                placementSo.ApplyModifiedPropertiesWithoutUndo();
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
