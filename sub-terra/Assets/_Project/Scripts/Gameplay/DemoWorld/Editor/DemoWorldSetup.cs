using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Drone;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Integration;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Power;
using SubTerra.Gameplay.Snapshot;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.DemoWorld.Editor
{
    /// <summary>Creates a disposable A-owned demo world; the final Integration Scene is never modified here.</summary>
    public static class DemoWorldSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Test/Gameplay/Gameplay_DemoWorld_Test.unity";
        private const string TileFolder = "Assets/_Project/Tilemaps/DemoWorld";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";
        private const string DronePrefabPath = "Assets/_Project/Prefabs/Gameplay/Drone/DiggerBot_Runtime.prefab";
        private const string SupportDefinitionPath = "Assets/_Project/Data/Buildings/SupportPillarPlacement.asset";
        private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        [MenuItem("Tools/SubTerra/Setup Demo World Test Scene")]
        public static void CreateScene()
        {
            EnsureFolder("Assets/_Project/Scenes/Test/Gameplay");
            EnsureFolder(TileFolder);
            Tile rock = CreateTile("Rock", new Color(0.28f, 0.3f, 0.35f));
            Tile copper = CreateTile("Copper", new Color(0.88f, 0.42f, 0.18f));
            Tile iron = CreateTile("Iron", new Color(0.62f, 0.68f, 0.74f));
            Tile lithium = CreateTile("Lithium", new Color(0.3f, 0.88f, 0.84f));
            Tile gasPocket = CreateTile("GasPocket", new Color(0.35f, 0.92f, 0.45f));
            Tile lockedSignal = CreateTile("LockedSignal", new Color(0.9f, 0.32f, 0.95f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new("GameplayRoot");
            Tilemap tilemap = CreateTilemap(root.transform);
            PopulateDemoRoute(tilemap, rock, copper, iron, lithium, gasPocket, lockedSignal);

            GameObject systems = new("DemoSystems"); systems.transform.SetParent(root.transform);
            MiningTileResolver resolver = systems.AddComponent<MiningTileResolver>();
            resolver.EditorSetEntries(
                new TileBase[] { rock, copper, iron, lithium, gasPocket, lockedSignal },
                new[]
                {
                    new MiningTileDto("tile.rock.normal", string.Empty, 0, true, 1f, 0.35f, 0.05f, false),
                    new MiningTileDto("tile.copper", "mineral.copper", 1, true, 1f, 0.6f, 0.1f, false),
                    new MiningTileDto("tile.iron", "mineral.iron", 1, true, 1f, 0.8f, 0.18f, false),
                    new MiningTileDto("tile.lithium", "mineral.lithium", 1, true, 1f, 1.2f, 0.3f, true),
                    new MiningTileDto("tile.gas-pocket", string.Empty, 0, true, 1f, 0.5f, 0.8f, true),
                    new MiningTileDto("tile.locked.signal", string.Empty, 0, false, 1f, 1f, 0f, false)
                });
            MiningSystem mining = systems.AddComponent<MiningSystem>();
            SetReference(mining, "foregroundTilemap", tilemap); SetReference(mining, "tileResolver", resolver);
            StructuralIntegritySystem structural = systems.AddComponent<StructuralIntegritySystem>(); SetReference(structural, "foregroundTilemap", tilemap);
            MiningStructuralBridge structuralBridge = systems.AddComponent<MiningStructuralBridge>(); SetReference(structuralBridge, "miningSystem", mining); SetReference(structuralBridge, "structuralIntegritySystem", structural);
            GasHazardSystem gas = systems.AddComponent<GasHazardSystem>(); SetReference(gas, "foregroundTilemap", tilemap);
            MiningGasBridge gasBridge = systems.AddComponent<MiningGasBridge>(); SetReference(gasBridge, "miningSystem", mining); SetReference(gasBridge, "gasHazardSystem", gas);
            PowerNetworkSystem power = systems.AddComponent<PowerNetworkSystem>();

            Transform player = CreatePlayer(root.transform, mining);
            SetReference(gas, "playerTransform", player);
            BuildingPlacementSystem building = systems.AddComponent<BuildingPlacementSystem>();
            BuildingTestResourceWallet wallet = systems.AddComponent<BuildingTestResourceWallet>();
            SetReference(building, "terrainTilemap", tilemap); SetReference(building, "resourceWalletBehaviour", wallet); SetReference(building, "structuralIntegritySystem", structural);
            BuildingPlacementDefinition supportDefinition = AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(SupportDefinitionPath);
            if (supportDefinition != null) building.Select(supportDefinition);
            Transform core = CreateOutpostCore(root.transform, power);
            CreateDrone(root.transform, player, tilemap, resolver, structural, gas, power, core);
            WorldSnapshotSystem snapshot = systems.AddComponent<WorldSnapshotSystem>();
            SetReference(snapshot, "foregroundTilemap", tilemap); SetReference(snapshot, "miningSystem", mining); SetReference(snapshot, "structuralSystem", structural);
            SetReference(snapshot, "gasHazardSystem", gas); SetReference(snapshot, "buildingPlacementSystem", building); SetReference(snapshot, "powerNetworkSystem", power);
            GameplayEventRecorder recorder = systems.AddComponent<GameplayEventRecorder>();
            GameplayEventBridge eventBridge = systems.AddComponent<GameplayEventBridge>();
            SetReference(eventBridge, "eventSinkBehaviour", recorder); SetReference(eventBridge, "miningSystem", mining); SetReference(eventBridge, "structuralSystem", structural);
            SetReference(eventBridge, "gasHazardSystem", gas); SetReference(eventBridge, "buildingPlacementSystem", building); SetReference(eventBridge, "powerNetworkSystem", power);
            CreateCamera(player);

            EditorSceneManager.SaveScene(scene, ScenePath);
            PhaseBMineLayerSetup.ApplyToOpenScene(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {ScenePath}. Follow the documented route from safe start to the locked rare-mineral signal.");
        }

        private static void PopulateDemoRoute(Tilemap tilemap, Tile rock, Tile copper, Tile iron, Tile lithium, Tile gasPocket, Tile lockedSignal)
        {
            for (int y = -2; y >= -41; y--)
                for (int x = -40; x <= 40; x++)
                    tilemap.SetTile(new Vector3Int(x, y, 0), rock);

            for (int y = -1; y <= 5; y++)
            {
                tilemap.SetTile(new Vector3Int(-40, y, 0), lockedSignal);
                tilemap.SetTile(new Vector3Int(40, y, 0), lockedSignal);
            }

            tilemap.SetTile(new Vector3Int(-8, -2, 0), copper);
            tilemap.SetTile(new Vector3Int(-7, -3, 0), copper);
            tilemap.SetTile(new Vector3Int(-3, -3, 0), iron);
            tilemap.SetTile(new Vector3Int(2, -5, 0), lithium);
            tilemap.SetTile(new Vector3Int(8, -4, 0), gasPocket);
            tilemap.SetTile(new Vector3Int(14, -7, 0), lockedSignal);
        }

        private static Tilemap CreateTilemap(Transform root)
        {
            GameObject grid = new("Grid"); grid.transform.SetParent(root); grid.AddComponent<Grid>();
            GameObject map = new("ForegroundTilemap"); map.transform.SetParent(grid.transform);
            Tilemap tilemap = map.AddComponent<Tilemap>(); map.AddComponent<TilemapRenderer>();
            TilemapCollider2D collider = map.AddComponent<TilemapCollider2D>();
            collider.compositeOperation = Collider2D.CompositeOperation.None;
            Rigidbody2D body = map.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            return tilemap;
        }

        private static Transform CreatePlayer(Transform root, MiningSystem mining)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null) return new GameObject("Player").transform;
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Player"; player.transform.SetParent(root); player.transform.position = new Vector3(-9.5f, -0.65f, 0f);
            PlayerMiningController controller = player.GetComponent<PlayerMiningController>() ?? player.AddComponent<PlayerMiningController>();
            SetReference(controller, "miningSystem", mining); SetReference(controller, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
            return player.transform;
        }

        private static Transform CreateOutpostCore(Transform root, PowerNetworkSystem power)
        {
            GameObject core = new("OutpostCore_Demo"); core.transform.SetParent(root); core.transform.position = new Vector3(-8f, 0f, 0f);
            SpriteRenderer renderer = core.AddComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); renderer.color = new Color(0.2f, 0.72f, 1f);
            core.AddComponent<PowerNode>().Configure(power, true, 5, 0, PowerPriority.Critical);
            return core.transform;
        }

        private static void CreateDrone(Transform root, Transform player, Tilemap map, MiningTileResolver resolver, StructuralIntegritySystem structural, GasHazardSystem gas, PowerNetworkSystem power, Transform core)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DronePrefabPath);
            if (prefab == null) return;
            GameObject drone = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            drone.name = "DiggerBot_Runtime"; drone.transform.SetParent(root); drone.transform.position = player.position + new Vector3(-0.8f, 0.55f, 0f);
            drone.transform.localScale = Vector3.one;
            var renderer = drone.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = new Vector2(0.45f, 0.35f);
            }
            drone.GetComponent<DroneFollower>()?.SetTarget(player);
            DroneSensor sensor = drone.GetComponent<DroneSensor>();
            if (sensor == null) return;
            SetReference(sensor, "playerTransform", player); SetReference(sensor, "foregroundTilemap", map); SetReference(sensor, "tileResolver", resolver);
            SetReference(sensor, "structuralSystem", structural); SetReference(sensor, "gasHazardSystem", gas); SetReference(sensor, "powerNetworkSystem", power);
            SetObjectArray(sensor, "outpostCores", new Object[] { core });
        }

        private static void CreateCamera(Transform player)
        {
            GameObject cameraObject = new("Main Camera"); cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(-9.5f, 0.35f, -10f);
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

        private static void SetObjectArray(Object target, string name, Object[] values)
        {
            SerializedObject serialized = new(target); SerializedProperty property = serialized.FindProperty(name); property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/'); string current = parts[0];
            for (int index = 1; index < parts.Length; index++) { string next = $"{current}/{parts[index]}"; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]); current = next; }
        }
    }
}
