using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Mining.Editor
{
    public static class MiningTestSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Test/Gameplay/Gameplay_Mining_Test.unity";
        private const string TileFolder = "Assets/_Project/Tilemaps/MiningTest";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";
        private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        [MenuItem("Tools/SubTerra/Setup Mining Test Scene")]
        public static void CreateScene()
        {
            EnsureFolder("Assets/_Project/Scenes/Test/Gameplay");
            EnsureFolder(TileFolder);

            Tile rock = CreateTile("Rock", new Color(0.28f, 0.3f, 0.35f));
            Tile copper = CreateTile("Copper", new Color(0.85f, 0.42f, 0.18f));
            Tile iron = CreateTile("Iron", new Color(0.62f, 0.68f, 0.74f));
            Tile lithium = CreateTile("Lithium", new Color(0.3f, 0.88f, 0.84f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new("GameplayRoot");
            GameObject gridObject = new("Grid"); gridObject.transform.SetParent(root.transform); gridObject.AddComponent<Grid>();
            GameObject mapObject = new("ForegroundTilemap"); mapObject.transform.SetParent(gridObject.transform);
            Tilemap tilemap = mapObject.AddComponent<Tilemap>();
            mapObject.AddComponent<TilemapRenderer>();
            TilemapCollider2D collider = mapObject.AddComponent<TilemapCollider2D>();
            collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            Rigidbody2D body = mapObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            mapObject.AddComponent<CompositeCollider2D>();

            for (int x = -12; x <= 12; x++) tilemap.SetTile(new Vector3Int(x, -2, 0), rock);
            tilemap.SetTile(new Vector3Int(1, 0, 0), copper);
            tilemap.SetTile(new Vector3Int(3, 0, 0), iron);
            tilemap.SetTile(new Vector3Int(5, 0, 0), lithium);

            // Tilemap collider seams cannot make the player fall through this test floor.
            // Mining targets stay on the Tilemap; only collision uses this continuous surface.
            GameObject safetyGround = new("SafetyGround");
            safetyGround.transform.SetParent(root.transform);
            safetyGround.transform.position = new Vector3(0f, -1.5f, 0f);
            BoxCollider2D safetyCollider = safetyGround.AddComponent<BoxCollider2D>();
            safetyCollider.size = new Vector2(30f, 1f);

            GameObject systems = new("MineSystems"); systems.transform.SetParent(root.transform);
            MiningTileResolver resolver = systems.AddComponent<MiningTileResolver>();
            resolver.EditorSetEntries(
                new TileBase[] { rock, copper, iron, lithium },
                new[]
                {
                    new MiningTileDto("tile.rock.normal", string.Empty, 0, true, 1f, 0.35f, 0.05f, false),
                    new MiningTileDto("tile.copper", "mineral.copper", 1, true, 1f, 0.6f, 0.1f, false),
                    new MiningTileDto("tile.iron", "mineral.iron", 1, true, 1f, 0.9f, 0.18f, false),
                    new MiningTileDto("tile.lithium", "mineral.lithium", 1, true, 1f, 1.25f, 0.3f, true)
                });
            MiningSystem miningSystem = systems.AddComponent<MiningSystem>();
            SetReference(miningSystem, "foregroundTilemap", tilemap);
            SetReference(miningSystem, "tileResolver", resolver);

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.SetParent(root.transform); player.transform.position = new Vector3(-4f, 0f, 0f);
            PlayerMiningController controller = player.GetComponent<PlayerMiningController>() ?? player.AddComponent<PlayerMiningController>();
            SetReference(controller, "miningSystem", miningSystem);
            SetReference(controller, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));

            GameObject cameraObject = new("Main Camera"); cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(-4f, 1f, -10f);
            PlayerCameraFollow follow = cameraObject.AddComponent<PlayerCameraFollow>(); follow.SetTarget(player.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {ScenePath}");
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
            for (int i = 1; i < parts.Length; i++) { string next = $"{current}/{parts[i]}"; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; }
        }
    }
}
