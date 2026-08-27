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

        // 세로 기둥 너비/높이. 가로 캡 높이는 기둥 너비와 같고, 캡 너비는 일반 블록(1칸) 너비.
        private const float PostWidth = 0.4f;
        private const float PostHeight = 1.8f;
        private const float BlockWidth = 1f;

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

                // 루트에 남은 구형 단일 스프라이트는 VisualRoot 하위로 이관한다.
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

                // VisualRoot 자체의 단일 스프라이트는 Post/Cap 자식으로 대체한다.
                var visualRenderer = visual.GetComponent<SpriteRenderer>();
                if (visualRenderer != null)
                {
                    Object.DestroyImmediate(visualRenderer);
                }

                // 가로 캡: 일반 블록 너비 × 세로 기둥 너비, 기둥 맨 위에 올려 T자 형성.
                float capHeight = PostWidth;
                float capLocalY = (PostHeight * 0.5f) + (capHeight * 0.5f);

                ConfigureSlicedSprite(
                    EnsureChild(visual, "Post"),
                    sprite,
                    color,
                    new Vector2(PostWidth, PostHeight),
                    Vector3.zero,
                    sortingOrder: 3);
                ConfigureSlicedSprite(
                    EnsureChild(visual, "Cap"),
                    sprite,
                    color,
                    new Vector2(BlockWidth, capHeight),
                    new Vector3(0f, capLocalY, 0f),
                    sortingOrder: 4);

                // 기둥은 통행을 막지 않으며 상단 캡만 아래에서 통과 가능한 발판으로 사용한다.
                foreach (var existing in root.GetComponents<BoxCollider2D>())
                {
                    Object.DestroyImmediate(existing);
                }

                // 가로 캡 콜라이더 — 아래에서는 통과하고 위에서는 밟을 수 있는 단방향 발판.
                var capCollider = root.AddComponent<BoxCollider2D>();
                capCollider.isTrigger = false;
                capCollider.offset = new Vector2(0f, capLocalY);
                capCollider.size = new Vector2(BlockWidth, capHeight);
                capCollider.usedByEffector = true;

                var platformEffector = root.GetComponent<PlatformEffector2D>();
                if (platformEffector == null)
                {
                    platformEffector = root.AddComponent<PlatformEffector2D>();
                }

                platformEffector.useOneWay = true;
                platformEffector.useSideFriction = false;
                platformEffector.useSideBounce = false;
                platformEffector.surfaceArc = 180f;

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

        private static Transform EnsureChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            var childObject = new GameObject(name);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static void ConfigureSlicedSprite(
            Transform target,
            Sprite sprite,
            Color color,
            Vector2 size,
            Vector3 localPosition,
            int sortingOrder)
        {
            target.localPosition = localPosition;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;

            var renderer = target.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = target.gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = sprite;
            renderer.color = color;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.sortingOrder = sortingOrder;
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
