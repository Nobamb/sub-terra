using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Snapshot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.DemoWorld.Editor
{
    /// <summary>MVP2 B 지층 데이터와 런타임 생성기를 실제 Mine Scene에 연결합니다.</summary>
    public static class PhaseBMineLayerSetup
    {
        private const string DistributionPath =
            "Assets/_Project/Data/World/MineLayerDistribution.asset";
        private const string BoundaryTilePath =
            "Assets/_Project/Tilemaps/DemoWorld/BoundaryRock.asset";
        private static readonly string[] ScenePaths =
        {
            "Assets/_Project/Scenes/Test/Gameplay/Gameplay_DemoWorld_Test.unity",
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity"
        };

        [MenuItem("Tools/SubTerra/MVP2/Apply Phase B Mine Layers")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new System.InvalidOperationException(
                    "지층 Scene 연결은 Play Mode 밖에서 실행해야 합니다.");
            }

            EnsureDistribution();
            EnsureBoundaryTile();
            AssetDatabase.SaveAssets();
            foreach (string scenePath in ScenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                // Single Scene 전환은 아직 Scene이 참조하지 않는 에셋을 언로드할 수 있어 전환 뒤 다시 읽는다.
                MineLayerDistribution distribution =
                    AssetDatabase.LoadAssetAtPath<MineLayerDistribution>(DistributionPath);
                TileBase boundary =
                    AssetDatabase.LoadAssetAtPath<TileBase>(BoundaryTilePath);
                ApplyToScene(scene, distribution, boundary);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("MVP2 Phase B mine layers were applied to the test and Integration scenes.");
        }

        internal static void ApplyToOpenScene(Scene scene)
        {
            EnsureDistribution();
            EnsureBoundaryTile();
            AssetDatabase.SaveAssets();
            ApplyToScene(
                scene,
                AssetDatabase.LoadAssetAtPath<MineLayerDistribution>(DistributionPath),
                AssetDatabase.LoadAssetAtPath<TileBase>(BoundaryTilePath));
        }

        private static void ApplyToScene(
            Scene scene,
            MineLayerDistribution distribution,
            TileBase boundary)
        {
            Tilemap tilemap = FindInScene<Tilemap>(scene, "ForegroundTilemap");
            MiningTileResolver resolver = FindInScene<MiningTileResolver>(scene);
            WorldSnapshotSystem snapshot = FindInScene<WorldSnapshotSystem>(scene);
            if (tilemap == null || resolver == null || snapshot == null)
            {
                throw new System.InvalidOperationException(
                    $"{scene.path}: 지층 생성에 필요한 Tilemap/Resolver/Snapshot 참조가 없습니다.");
            }

            GameObject host = snapshot.gameObject;
            MineLayerTilemapGenerator generator =
                host.GetComponent<MineLayerTilemapGenerator>()
                ?? host.AddComponent<MineLayerTilemapGenerator>();
            generator.EditorConfigure(
                tilemap,
                distribution,
                LoadTile("Rock"),
                boundary,
                LoadTile("Copper"),
                LoadTile("Iron"),
                LoadTile("Lithium"),
                LoadTile("GasPocket"),
                LoadTile("LockedSignal"),
                resolver,
                snapshot,
                20260731L);

            var snapshotSerialized = new SerializedObject(snapshot);
            SetObject(snapshotSerialized, "baseWorldGeneratorBehaviour", generator);
            snapshotSerialized.ApplyModifiedPropertiesWithoutUndo();

            if (!generator.Regenerate(20260731L, distribution.GeneratorVersion))
            {
                throw new System.InvalidOperationException(
                    $"{scene.path}: 기본 지층 생성에 실패했습니다.");
            }

            // Demo/tutorial path markers expected by Integration wiring tests.
            // Applied after regenerate so BoundaryRock edges stay intact.
            RestoreAuthoredDemoMarkers(tilemap);

            EditorUtility.SetDirty(generator);
            EditorUtility.SetDirty(snapshot);
            EditorUtility.SetDirty(tilemap);
        }

        /// <summary>
        /// Places fixed tutorial mineral markers without clearing the Phase B layer.
        /// </summary>
        internal static void RestoreAuthoredDemoMarkers(Tilemap tilemap)
        {
            if (tilemap == null)
            {
                return;
            }

            tilemap.SetTile(new Vector3Int(-8, -2, 0), LoadTile("Copper"));
            tilemap.SetTile(new Vector3Int(-7, -3, 0), LoadTile("Copper"));
            tilemap.SetTile(new Vector3Int(-3, -3, 0), LoadTile("Iron"));
            tilemap.SetTile(new Vector3Int(2, -5, 0), LoadTile("Lithium"));
            tilemap.SetTile(new Vector3Int(8, -4, 0), LoadTile("GasPocket"));
            tilemap.SetTile(new Vector3Int(14, -7, 0), LoadTile("LockedSignal"));
            tilemap.RefreshAllTiles();
            EditorUtility.SetDirty(tilemap);
        }

        private static MineLayerDistribution EnsureDistribution()
        {
            EnsureFolder("Assets/_Project/Data/World");
            MineLayerDistribution distribution =
                AssetDatabase.LoadAssetAtPath<MineLayerDistribution>(DistributionPath);
            if (distribution != null)
            {
                return distribution;
            }

            distribution = ScriptableObject.CreateInstance<MineLayerDistribution>();
            AssetDatabase.CreateAsset(distribution, DistributionPath);
            return distribution;
        }

        private static TileBase EnsureBoundaryTile()
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(BoundaryTilePath);
            if (tile != null)
            {
                return tile;
            }

            Tile rock = AssetDatabase.LoadAssetAtPath<Tile>(
                "Assets/_Project/Tilemaps/DemoWorld/Rock.asset");
            tile = ScriptableObject.CreateInstance<Tile>();
            if (rock != null)
            {
                tile.sprite = rock.sprite;
                tile.color = rock.color * 0.65f;
            }

            tile.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.CreateAsset(tile, BoundaryTilePath);
            return tile;
        }

        private static TileBase LoadTile(string name)
        {
            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(
                $"Assets/_Project/Tilemaps/DemoWorld/{name}.asset");
            if (tile == null)
            {
                throw new System.InvalidOperationException(
                    $"DemoWorld tile is missing: {name}");
            }

            return tile;
        }

        private static T FindInScene<T>(Scene scene, string objectName = null)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (T component in root.GetComponentsInChildren<T>(true))
                {
                    if (string.IsNullOrEmpty(objectName)
                        || component.gameObject.name == objectName)
                    {
                        return component;
                    }
                }
            }

            return null;
        }

        private static void SetObject(
            SerializedObject serialized,
            string propertyName,
            Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new System.InvalidOperationException(
                    $"Serialized property is missing: {propertyName}");
            }

            property.objectReferenceValue = value;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
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
