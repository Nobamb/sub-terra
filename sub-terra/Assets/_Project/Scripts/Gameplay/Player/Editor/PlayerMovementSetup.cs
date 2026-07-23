using System.IO;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Player.Editor
{
    public static class PlayerMovementSetup
    {
        private const string PlayerPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";
        private const string ScenePath =
            "Assets/_Project/Scenes/Test/Gameplay/Gameplay_Player_Test.unity";
        private const string TilePath =
            "Assets/_Project/Tilemaps/PlayerTestGround.asset";
        private const string InputActionsPath =
            "Assets/Settings/InputSystem_Actions.inputactions";

        [MenuItem("Tools/SubTerra/Setup Player Test Scene")]
        public static void CreateAssets()
        {
            EnsureFolder(Path.GetDirectoryName(PlayerPrefabPath));
            EnsureFolder(Path.GetDirectoryName(ScenePath));
            EnsureFolder(Path.GetDirectoryName(TilePath));

            Tile groundTile = CreateGroundTile();
            GameObject playerPrefab = CreatePlayerPrefab();
            CreateTestScene(playerPrefab, groundTile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created {PlayerPrefabPath} and {ScenePath}.");
        }

        private static GameObject CreatePlayerPrefab()
        {
            GameObject root = new("Player");
            root.transform.position = new Vector3(-6f, -1f, 0f);

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CapsuleCollider2D collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.8f, 1.8f);

            PlayerMovement movement = root.AddComponent<PlayerMovement>();
            PlayerController controller = root.AddComponent<PlayerController>();
            PlayerFacing facing = root.AddComponent<PlayerFacing>();

            Transform interactionOrigin = new GameObject("PlayerInteractionOrigin").transform;
            interactionOrigin.SetParent(root.transform, false);
            interactionOrigin.localPosition = new Vector3(0.7f, 0f, 0f);

            Transform groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(root.transform, false);
            groundCheck.localPosition = new Vector3(0f, -0.94f, 0f);

            Transform visualRoot = new GameObject("VisualRoot").transform;
            visualRoot.SetParent(root.transform, false);

            SpriteRenderer renderer = visualRoot.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.color = new Color(0.2f, 0.75f, 0.95f);
            renderer.size = new Vector2(0.8f, 1.8f);

            SetObjectReference(movement, "groundCheck", groundCheck);
            SetObjectReference(facing, "visualRoot", visualRoot);

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            SetObjectReference(controller, "inputActions", actions);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Tile CreateGroundTile()
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, TilePath);
            }

            tile.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            tile.color = new Color(0.22f, 0.25f, 0.3f);
            tile.colliderType = Tile.ColliderType.Sprite;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static void CreateTestScene(GameObject playerPrefab, Tile groundTile)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Gameplay_Player_Test";

            GameObject gameplayRoot = new("GameplayRoot");
            GameObject gridObject = new("Grid");
            gridObject.transform.SetParent(gameplayRoot.transform);
            gridObject.AddComponent<Grid>();

            GameObject tilemapObject = new("ForegroundTilemap");
            tilemapObject.transform.SetParent(gridObject.transform);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            tilemapObject.AddComponent<TilemapRenderer>();
            TilemapCollider2D tilemapCollider = tilemapObject.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            Rigidbody2D tilemapBody = tilemapObject.AddComponent<Rigidbody2D>();
            tilemapBody.bodyType = RigidbodyType2D.Static;
            tilemapObject.AddComponent<CompositeCollider2D>();

            for (int x = -12; x <= 12; x++)
            {
                tilemap.SetTile(new Vector3Int(x, -2, 0), groundTile);
                if (x is -12 or 12)
                {
                    for (int y = -1; y <= 3; y++)
                    {
                        tilemap.SetTile(new Vector3Int(x, y, 0), groundTile);
                    }
                }
            }

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.SetParent(gameplayRoot.transform);
            player.transform.position = new Vector3(-6f, 0f, 0f);

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f);
            cameraObject.transform.position = new Vector3(-6f, 1f, -10f);
            PlayerCameraFollow follow = cameraObject.AddComponent<PlayerCameraFollow>();
            follow.SetTarget(player.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void SetObjectReference(
            Object target,
            string propertyName,
            Object value)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string assetPath)
        {
            string normalizedPath = assetPath.Replace('\\', '/');
            string[] parts = normalizedPath.Split('/');
            string current = parts[0];

            for (int index = 1; index < parts.Length; index++)
            {
                string next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
    }
}
