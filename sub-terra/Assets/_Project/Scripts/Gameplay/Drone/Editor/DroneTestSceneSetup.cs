using SubTerra.Gameplay.Mining;
using SubTerra.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Drone.Editor
{
    public static class DroneTestSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Test/Gameplay/Gameplay_Drone_Test.unity";
        private const string TileFolder = "Assets/_Project/Tilemaps/DroneTest";
        private const string DronePrefabPath = "Assets/_Project/Prefabs/Gameplay/Drone/DiggerBot_Runtime.prefab";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";

        [MenuItem("Tools/SubTerra/Setup Drone Test Scene")]
        public static void CreateScene()
        {
            EnsureFolder("Assets/_Project/Scenes/Test/Gameplay");
            EnsureFolder(TileFolder);
            EnsureFolder("Assets/_Project/Prefabs/Gameplay/Drone");
            Tile rock = CreateTile("Rock", new Color(0.28f, 0.3f, 0.35f));
            Tile copper = CreateTile("Copper", new Color(0.88f, 0.42f, 0.18f));
            GameObject dronePrefab = CreateDronePrefab();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new("GameplayRoot");
            Tilemap tilemap = CreateTilemap(root.transform);
            for (int x = -12; x <= 12; x++) tilemap.SetTile(new Vector3Int(x, -2, 0), rock);
            tilemap.SetTile(new Vector3Int(2, 0, 0), copper);
            tilemap.SetTile(new Vector3Int(3, 0, 0), copper);

            GameObject systems = new("DroneSystems"); systems.transform.SetParent(root.transform);
            MiningTileResolver resolver = systems.AddComponent<MiningTileResolver>();
            resolver.EditorSetEntries(
                new TileBase[] { rock, copper },
                new[]
                {
                    new MiningTileDto("tile.rock.normal", string.Empty, 0, true, 1f, 0.35f, 0.05f, false),
                    new MiningTileDto("tile.copper", "mineral.copper", 1, true, 1f, 0.6f, 0.1f, false)
                });

            Transform player = CreatePlayer(root.transform);
            Transform baseCore = CreateBaseCore(root.transform);
            GameObject drone = (GameObject)PrefabUtility.InstantiatePrefab(dronePrefab);
            drone.name = "DiggerBot_Runtime"; drone.transform.SetParent(root.transform); drone.transform.position = new Vector3(-2f, 1f, 0f);
            DroneFollower follower = drone.GetComponent<DroneFollower>(); follower.SetTarget(player);
            DroneSensor sensor = drone.GetComponent<DroneSensor>();
            SetReference(sensor, "playerTransform", player);
            SetReference(sensor, "foregroundTilemap", tilemap);
            SetReference(sensor, "tileResolver", resolver);
            SetObjectArray(sensor, "outpostCores", new Object[] { baseCore });
            CreateCamera(player);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {ScenePath}. The drone follows the player and scans the nearby copper tiles.");
        }

        private static Tilemap CreateTilemap(Transform root)
        {
            GameObject grid = new("Grid"); grid.transform.SetParent(root); grid.AddComponent<Grid>();
            GameObject map = new("ForegroundTilemap"); map.transform.SetParent(grid.transform);
            Tilemap tilemap = map.AddComponent<Tilemap>(); map.AddComponent<TilemapRenderer>();
            return tilemap;
        }

        private static Transform CreatePlayer(Transform root)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab != null)
            {
                GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                player.name = "Player"; player.transform.SetParent(root); player.transform.position = new Vector3(-1f, 0f, 0f);
                return player.transform;
            }
            GameObject fallback = new("Player"); fallback.transform.SetParent(root); fallback.transform.position = new Vector3(-1f, 0f, 0f);
            return fallback.transform;
        }

        private static Transform CreateBaseCore(Transform root)
        {
            GameObject core = new("OutpostCore"); core.transform.SetParent(root); core.transform.position = new Vector3(-7f, 0f, 0f);
            SpriteRenderer renderer = core.AddComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); renderer.color = new Color(0.2f, 0.72f, 1f);
            return core.transform;
        }

        private static GameObject CreateDronePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DronePrefabPath);
            if (prefab != null) return prefab;
            GameObject source = new("DiggerBot_Runtime");
            SpriteRenderer renderer = source.AddComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); renderer.color = new Color(0.32f, 0.9f, 0.95f);
            source.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            source.AddComponent<DroneFollower>(); source.AddComponent<DroneSensor>();
            PrefabUtility.SaveAsPrefabAsset(source, DronePrefabPath);
            Object.DestroyImmediate(source);
            return AssetDatabase.LoadAssetAtPath<GameObject>(DronePrefabPath);
        }

        private static void CreateCamera(Transform player)
        {
            GameObject cameraObject = new("Main Camera"); cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(0f, 1f, -10f);
            if (player != null) cameraObject.AddComponent<SubTerra.Gameplay.Player.PlayerCameraFollow>().SetTarget(player);
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
