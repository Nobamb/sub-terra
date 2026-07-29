#if UNITY_EDITOR
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Hazards.Editor
{
    public static class HazardTestSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Test/Gameplay/Gameplay_Hazard_Test.unity";
        private const string TileFolder = "Assets/_Project/Tilemaps/HazardTest";
        private const string GasZonePrefabPath = "Assets/_Project/Prefabs/Gameplay/Hazards/GasZone.prefab";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";
        private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        [MenuItem("Tools/SubTerra/Setup Hazard Test Scene")]
        public static void CreateScene()
        {
            EnsureFolder("Assets/_Project/Scenes/Test/Gameplay");
            EnsureFolder(TileFolder);
            EnsureFolder("Assets/_Project/Prefabs/Gameplay/Hazards");

            Tile rock = CreateTile("Rock", new Color(0.28f, 0.3f, 0.35f));
            Tile gasPocket = CreateTile("GasPocket", new Color(0.36f, 0.92f, 0.45f));
            GasZone gasZonePrefab = CreateGasZonePrefab();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new("GameplayRoot");
            Tilemap tilemap = CreateTilemap(root.transform);
            for (int x = -12; x <= 12; x++) tilemap.SetTile(new Vector3Int(x, -2, 0), rock);
            tilemap.SetTile(new Vector3Int(0, 0, 0), gasPocket);
            CreateSafetyGround(root.transform);

            GameObject systems = new("HazardSystems"); systems.transform.SetParent(root.transform);
            MiningTileResolver resolver = systems.AddComponent<MiningTileResolver>();
            resolver.EditorSetEntries(
                new TileBase[] { rock, gasPocket },
                new[]
                {
                    new MiningTileDto("tile.rock.normal", string.Empty, 0, true, 1f, 0.35f, 0.05f, false),
                    new MiningTileDto("tile.gas-pocket", string.Empty, 0, true, 1f, 0.5f, 0.8f, true)
                });
            MiningSystem miningSystem = systems.AddComponent<MiningSystem>();
            SetReference(miningSystem, "foregroundTilemap", tilemap);
            SetReference(miningSystem, "tileResolver", resolver);

            Transform player = CreatePlayer(root.transform, miningSystem);
            GasHazardSystem hazardSystem = systems.AddComponent<GasHazardSystem>();
            SetReference(hazardSystem, "foregroundTilemap", tilemap);
            SetReference(hazardSystem, "playerTransform", player);
            SetReference(hazardSystem, "gasZonePrefab", gasZonePrefab);
            MiningGasBridge bridge = systems.AddComponent<MiningGasBridge>();
            SetReference(bridge, "miningSystem", miningSystem);
            SetReference(bridge, "gasHazardSystem", hazardSystem);
            CreateCamera(player);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {ScenePath}. Mine the green gas pocket to activate a timed gas zone.");
        }

        private static Tilemap CreateTilemap(Transform root)
        {
            GameObject gridObject = new("Grid"); gridObject.transform.SetParent(root); gridObject.AddComponent<Grid>();
            GameObject mapObject = new("ForegroundTilemap"); mapObject.transform.SetParent(gridObject.transform);
            Tilemap tilemap = mapObject.AddComponent<Tilemap>(); mapObject.AddComponent<TilemapRenderer>();
            TilemapCollider2D collider = mapObject.AddComponent<TilemapCollider2D>(); collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            Rigidbody2D body = mapObject.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Static;
            mapObject.AddComponent<CompositeCollider2D>();
            return tilemap;
        }

        private static void CreateSafetyGround(Transform root)
        {
            GameObject ground = new("SafetyGround"); ground.transform.SetParent(root); ground.transform.position = new Vector3(0f, -1.5f, 0f);
            ground.AddComponent<BoxCollider2D>().size = new Vector2(30f, 1f);
        }

        private static Transform CreatePlayer(Transform root, MiningSystem miningSystem)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null) return null;
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Player"; player.transform.SetParent(root); player.transform.position = new Vector3(-1f, 0f, 0f);
            PlayerMiningController controller = player.GetComponent<PlayerMiningController>() ?? player.AddComponent<PlayerMiningController>();
            SetReference(controller, "miningSystem", miningSystem);
            SetReference(controller, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
            return player.transform;
        }

        private static void CreateCamera(Transform player)
        {
            GameObject cameraObject = new("Main Camera"); cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(0f, 1f, -10f);
            if (player != null) cameraObject.AddComponent<PlayerCameraFollow>().SetTarget(player);
        }

        private static GasZone CreateGasZonePrefab()
        {
            GasZone prefab = AssetDatabase.LoadAssetAtPath<GasZone>(GasZonePrefabPath);
            if (prefab != null) return prefab;
            GameObject source = new("GasZone");
            source.AddComponent<GasZone>();
            CircleCollider2D trigger = source.AddComponent<CircleCollider2D>(); trigger.isTrigger = true; trigger.radius = 2f;
            SpriteRenderer renderer = source.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.color = new Color(0.2f, 0.95f, 0.4f, 0.35f);
            source.transform.localScale = new Vector3(4f, 4f, 1f);
            PrefabUtility.SaveAsPrefabAsset(source, GasZonePrefabPath);
            Object.DestroyImmediate(source);
            return AssetDatabase.LoadAssetAtPath<GasZone>(GasZonePrefabPath);
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
