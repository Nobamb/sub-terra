using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Structural.Editor
{
    public static class StructuralTestSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Test/Gameplay/Gameplay_Structural_Test.unity";
        private const string TileFolder = "Assets/_Project/Tilemaps/MiningTest";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";
        private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        [MenuItem("Tools/SubTerra/Setup Structural Test Scene")]
        public static void CreateScene()
        {
            EnsureFolder("Assets/_Project/Scenes/Test/Gameplay");
            EnsureFolder(TileFolder);

            Tile rock = CreateTile("Rock", new Color(0.28f, 0.3f, 0.35f));
            Tile lithium = CreateTile("Lithium", new Color(0.3f, 0.88f, 0.84f));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new("GameplayRoot");

            Tilemap tilemap = CreateTilemap(root.transform);
            for (int x = -12; x <= 12; x++) tilemap.SetTile(new Vector3Int(x, -2, 0), rock);
            tilemap.SetTile(new Vector3Int(0, 0, 0), lithium);
            for (int x = -2; x <= 2; x++) tilemap.SetTile(new Vector3Int(x, 3, 0), rock);
            CreateSafetyGround(root.transform);

            GameObject systems = new("StructuralSystems");
            systems.transform.SetParent(root.transform);
            MiningTileResolver resolver = systems.AddComponent<MiningTileResolver>();
            resolver.EditorSetEntries(
                new TileBase[] { rock, lithium },
                new[]
                {
                    new MiningTileDto("tile.rock.normal", string.Empty, 0, true, 1f, 0.35f, 0.05f, false),
                    new MiningTileDto("tile.lithium", "mineral.lithium", 1, true, 1f, 0.5f, 0.3f, false)
                });
            MiningSystem miningSystem = systems.AddComponent<MiningSystem>();
            SetReference(miningSystem, "foregroundTilemap", tilemap);
            SetReference(miningSystem, "tileResolver", resolver);

            StructuralIntegritySystem integritySystem = systems.AddComponent<StructuralIntegritySystem>();
            SetReference(integritySystem, "foregroundTilemap", tilemap);
            MiningStructuralBridge bridge = systems.AddComponent<MiningStructuralBridge>();
            SetReference(bridge, "miningSystem", miningSystem);
            SetReference(bridge, "structuralIntegritySystem", integritySystem);

            StructuralSupport optionalSupport = CreateSupport(root.transform, new Vector3(5f, -1f, 0f));
            integritySystem.RegisterSupport(optionalSupport);
            EditorUtility.SetDirty(integritySystem);
            CreatePlayer(root.transform, miningSystem);
            CreateCamera(root.transform.Find("Player"));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {ScenePath}. Mine the lithium tile under the ceiling to trigger a partial collapse.");
        }

        private static Tilemap CreateTilemap(Transform root)
        {
            GameObject gridObject = new("Grid"); gridObject.transform.SetParent(root); gridObject.AddComponent<Grid>();
            GameObject mapObject = new("ForegroundTilemap"); mapObject.transform.SetParent(gridObject.transform);
            Tilemap tilemap = mapObject.AddComponent<Tilemap>();
            mapObject.AddComponent<TilemapRenderer>();
            TilemapCollider2D collider = mapObject.AddComponent<TilemapCollider2D>();
            collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            Rigidbody2D body = mapObject.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Static;
            mapObject.AddComponent<CompositeCollider2D>();
            return tilemap;
        }

        private static void CreateSafetyGround(Transform root)
        {
            GameObject ground = new("SafetyGround"); ground.transform.SetParent(root); ground.transform.position = new Vector3(0f, -1.5f, 0f);
            ground.AddComponent<BoxCollider2D>().size = new Vector2(30f, 1f);
        }

        private static StructuralSupport CreateSupport(Transform root, Vector3 position)
        {
            GameObject support = new("SupportPillar_Optional"); support.transform.SetParent(root); support.transform.position = position;
            StructuralSupport structuralSupport = support.AddComponent<StructuralSupport>();
            SpriteRenderer renderer = support.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.color = new Color(0.92f, 0.68f, 0.18f); support.transform.localScale = new Vector3(0.35f, 2f, 1f);
            return structuralSupport;
        }

        private static void CreatePlayer(Transform root, MiningSystem miningSystem)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null) return;
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Player"; player.transform.SetParent(root); player.transform.position = new Vector3(-2f, 0f, 0f);
            PlayerMiningController controller = player.GetComponent<PlayerMiningController>() ?? player.AddComponent<PlayerMiningController>();
            SetReference(controller, "miningSystem", miningSystem);
            SetReference(controller, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
        }

        private static void CreateCamera(Transform player)
        {
            GameObject cameraObject = new("Main Camera"); cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(0f, 1f, -10f);
            if (player != null) cameraObject.AddComponent<PlayerCameraFollow>().SetTarget(player);
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
