#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Building.Editor
{
    public static class BuildingTestSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Test/Gameplay/Gameplay_Building_Test.unity";
        private const string TileFolder = "Assets/_Project/Tilemaps/BuildingTest";
        private const string PrefabPath = "Assets/_Project/Prefabs/Gameplay/Buildings/SupportPillar.prefab";
        private const string DefinitionPath = "Assets/_Project/Data/Buildings/SupportPillarPlacement.asset";

        [MenuItem("Tools/SubTerra/Setup Building Test Scene")]
        public static void CreateScene()
        {
            EnsureFolder("Assets/_Project/Scenes/Test/Gameplay");
            EnsureFolder(TileFolder);
            EnsureFolder("Assets/_Project/Prefabs/Gameplay/Buildings");
            EnsureFolder("Assets/_Project/Data/Buildings");
            Tile groundTile = CreateTile("Ground", new Color(0.29f, 0.31f, 0.35f));
            GameObject supportPrefab = CreateSupportPrefab();
            BuildingPlacementDefinition definition = CreateDefinition(supportPrefab);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new("GameplayRoot");
            Tilemap tilemap = CreateTilemap(root.transform);
            for (int x = -12; x <= 12; x++) tilemap.SetTile(new Vector3Int(x, -1, 0), groundTile);
            CreateCamera();

            GameObject systems = new("BuildingSystems"); systems.transform.SetParent(root.transform);
            BuildingTestResourceWallet wallet = systems.AddComponent<BuildingTestResourceWallet>();
            GameObject buildings = new("PlacedBuildings"); buildings.transform.SetParent(root.transform);
            BuildingPlacementSystem placement = systems.AddComponent<BuildingPlacementSystem>();
            SetReference(placement, "terrainTilemap", tilemap);
            SetReference(placement, "buildingRoot", buildings.transform);
            SetReference(placement, "resourceWalletBehaviour", wallet);
            placement.Select(definition);

            BuildingPlacementSceneReferences sceneReferences = systems.AddComponent<BuildingPlacementSceneReferences>();
            SetReference(sceneReferences, "terrainTilemap", tilemap);
            BuildingPlacementPreview preview = CreatePreview(root.transform);
            BuildingPlacementInput input = systems.AddComponent<BuildingPlacementInput>();
            SetReference(input, "placementSystem", placement);
            SetReference(input, "preview", preview);
            SetReference(input, "targetCamera", Camera.main);

            // UI Canvas 및 EventSystem 생성
            GameObject canvasObject = new("UICanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject menuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/BuildingMenu.prefab");
            if (menuPrefab != null)
            {
                GameObject menuInstance = (GameObject)PrefabUtility.InstantiatePrefab(menuPrefab, canvasObject.transform);
                SubTerra.App.UI.Building.BuildingMenuBinder menuBinder = menuInstance.GetComponent<SubTerra.App.UI.Building.BuildingMenuBinder>();
                BuildingTestAutoBinder autoBinder = systems.AddComponent<BuildingTestAutoBinder>();
                SetReference(autoBinder, "menuBinder", menuBinder);
                SetReference(autoBinder, "catalog", AssetDatabase.LoadAssetAtPath<SubTerra.App.Core.Data.GameDataCatalog>("Assets/_Project/Data/Catalog/GameDataCatalog.asset"));
                SetReference(autoBinder, "placementSystem", placement);
                SetReference(autoBinder, "preview", preview);
                SetReference(autoBinder, "sceneReferences", sceneReferences);
            }

            GameObject eventSystemObject = new("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
            {
                eventSystemObject.AddComponent(inputModuleType);
            }
            else
            {
                eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {ScenePath}. Move the mouse over empty ground cells to preview the support pillar.");
        }

        private static Tilemap CreateTilemap(Transform root)
        {
            GameObject gridObject = new("Grid"); gridObject.transform.SetParent(root); gridObject.AddComponent<Grid>();
            GameObject mapObject = new("TerrainTilemap"); mapObject.transform.SetParent(gridObject.transform);
            Tilemap tilemap = mapObject.AddComponent<Tilemap>(); mapObject.AddComponent<TilemapRenderer>();
            TilemapCollider2D collider = mapObject.AddComponent<TilemapCollider2D>(); collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            Rigidbody2D body = mapObject.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Static;
            mapObject.AddComponent<CompositeCollider2D>();
            return tilemap;
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new("Main Camera"); cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(0f, 1f, -10f);
        }

        private static BuildingPlacementPreview CreatePreview(Transform root)
        {
            GameObject previewObject = new("BuildingPreview"); previewObject.transform.SetParent(root);
            SpriteRenderer renderer = previewObject.AddComponent<SpriteRenderer>(); renderer.sortingOrder = 10;
            return previewObject.AddComponent<BuildingPlacementPreview>();
        }

        private static GameObject CreateSupportPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null) return prefab;
            GameObject source = new("SupportPillar");
            SpriteRenderer renderer = source.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); renderer.color = new Color(0.95f, 0.7f, 0.2f);
            source.transform.localScale = new Vector3(0.35f, 1.8f, 1f);
            source.AddComponent<BoxCollider2D>();
            source.AddComponent<SubTerra.Gameplay.Structural.StructuralSupport>();
            PrefabUtility.SaveAsPrefabAsset(source, PrefabPath);
            Object.DestroyImmediate(source);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        private static BuildingPlacementDefinition CreateDefinition(GameObject prefab)
        {
            BuildingPlacementDefinition definition = AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }
            definition.EditorSet("building.support", prefab, Vector2Int.one, true);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static Tile CreateTile(string name, Color color)
        {
            string path = $"{TileFolder}/{name}.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); tile.color = color; tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile); return tile;
        }

        private static void SetReference(Object target, string name, Object value)
        {
            SerializedObject serialized = new(target); serialized.FindProperty(name).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/'); string current = parts[0];
            for (int index = 1; index < parts.Length; index++) { string next = $"{current}/{parts[index]}"; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]); current = next; }
        }
    }
}
#endif
