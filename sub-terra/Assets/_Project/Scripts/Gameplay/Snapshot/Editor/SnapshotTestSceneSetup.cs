#if UNITY_EDITOR
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Power;
using SubTerra.Gameplay.Structural;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Snapshot.Editor
{
    public static class SnapshotTestSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Test/Gameplay/Gameplay_Snapshot_Test.unity";

        [MenuItem("Tools/SubTerra/Setup Snapshot Test Scene")]
        public static void CreateScene()
        {
            EnsureFolder("Assets/_Project/Scenes/Test/Gameplay");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new("GameplayRoot");
            Tilemap tilemap = CreateTilemap(root.transform);
            GameObject systems = new("WorldSystems"); systems.transform.SetParent(root.transform);

            MiningSystem mining = systems.AddComponent<MiningSystem>();
            StructuralIntegritySystem structural = systems.AddComponent<StructuralIntegritySystem>();
            GasHazardSystem gas = systems.AddComponent<GasHazardSystem>();
            BuildingPlacementSystem building = systems.AddComponent<BuildingPlacementSystem>();
            PowerNetworkSystem power = systems.AddComponent<PowerNetworkSystem>();
            WorldSnapshotSystem snapshot = systems.AddComponent<WorldSnapshotSystem>();
            SetReference(mining, "foregroundTilemap", tilemap);
            SetReference(structural, "foregroundTilemap", tilemap);
            SetReference(gas, "foregroundTilemap", tilemap);
            SetReference(building, "terrainTilemap", tilemap);
            SetReference(snapshot, "foregroundTilemap", tilemap);
            SetReference(snapshot, "miningSystem", mining);
            SetReference(snapshot, "structuralSystem", structural);
            SetReference(snapshot, "gasHazardSystem", gas);
            SetReference(snapshot, "buildingPlacementSystem", building);
            SetReference(snapshot, "powerNetworkSystem", power);
            CreateCamera();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {ScenePath}. WorldSnapshotSystem is wired to the A gameplay systems.");
        }

        private static Tilemap CreateTilemap(Transform root)
        {
            GameObject grid = new("Grid"); grid.transform.SetParent(root); grid.AddComponent<Grid>();
            GameObject map = new("ForegroundTilemap"); map.transform.SetParent(grid.transform);
            Tilemap tilemap = map.AddComponent<Tilemap>(); map.AddComponent<TilemapRenderer>();
            return tilemap;
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new("Main Camera"); cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
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
