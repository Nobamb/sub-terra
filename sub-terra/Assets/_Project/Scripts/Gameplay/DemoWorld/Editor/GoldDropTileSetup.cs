using SubTerra.Gameplay.Mining;
using SubTerra.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.DemoWorld.Editor
{
    public static class GoldDropTileSetup
    {
        public const string SettingsPath = "Assets/_Project/Data/World/GoldDropSettings.asset";
        private const string ScenePath = "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private static readonly string[] Names = { "Rock", "GasPocket", "Copper", "Iron", "Lithium" };
        private static readonly string[] Fields = { "rockGoldTile", "gasGoldTile", "copperGoldTile", "ironGoldTile", "lithiumGoldTile" };
        private static readonly string[] Images =
        {
            "Ground/ground_normal", "Ground/ground_gas", "Ore/ore_copper", "Ore/ore_iron", "Ore/ore_lithium"
        };

        [MenuItem("SubTerra/World/Apply Prompt B100 Gold Tiles")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("골드 타일 연결은 Play Mode 밖에서 실행합니다.");
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                foreach (var root in scene.GetRootGameObjects())
                foreach (var generator in root.GetComponentsInChildren<MineLayerTilemapGenerator>(true))
                {
                    Configure(generator);
                    var map = new SerializedObject(generator).FindProperty("foregroundTilemap").objectReferenceValue as Tilemap;
                    var settings = AssetDatabase.LoadAssetAtPath<GoldDropSettings>(SettingsPath);
                    var resolver = new SerializedObject(generator).FindProperty("tileResolver").objectReferenceValue as MiningTileResolver;
                    foreach (var cell in map.cellBounds.allPositionsWithin)
                    {
                        if (!resolver.TryResolve(map.GetTile(cell), out var definition) || !definition.isMineable) continue;
                        if ((cell.x == -7 && cell.y == -3) || (cell.x == -3 && cell.y == -3)
                            || (cell.x == 2 && cell.y == -5) || (cell.x == 8 && cell.y == -4)) continue;
                        string id = MineLayerTileIds.BaseTileId(definition.tileId);
                        MineLayerCellKind kind = id switch
                        {
                            MineLayerTileIds.Rock => MineLayerCellKind.Rock,
                            MineLayerTileIds.GasPocket => MineLayerCellKind.GasPocket,
                            MineLayerTileIds.Copper => MineLayerCellKind.Copper,
                            MineLayerTileIds.Iron => MineLayerCellKind.Iron,
                            MineLayerTileIds.Lithium => MineLayerCellKind.Lithium,
                            _ => MineLayerCellKind.BoundaryRock
                        };
                        if (settings.IsGold(generator.WorldSeed, cell.x, cell.y, kind)
                            && resolver.TryFindTileById(id + ".gold", out var gold)) map.SetTile(cell, gold);
                    }
                }
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        public static void Configure(MineLayerTilemapGenerator generator)
        {
            var settings = AssetDatabase.LoadAssetAtPath<GoldDropSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<GoldDropSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            var data = new SerializedObject(settings);
            data.FindProperty("mineralCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                "Assets/_Project/Data/Catalog/GameDataCatalog.asset");
            data.ApplyModifiedPropertiesWithoutUndo();

            var serialized = new SerializedObject(generator);
            serialized.FindProperty("goldDropSettings").objectReferenceValue = settings;
            var resolver = serialized.FindProperty("tileResolver").objectReferenceValue as MiningTileResolver;
            if (resolver == null) throw new System.InvalidOperationException("MiningTileResolver 참조 누락");
            var resolverData = new SerializedObject(resolver);
            var entries = resolverData.FindProperty("entries");
            for (int i = 0; i < Names.Length; i++)
            {
                string imagePath = "Assets/_Project/Art/Tiles/" + Images[i] + "_gold_01.png";
                var importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
                if (importer == null) throw new System.InvalidOperationException("골드 이미지 누락: " + imagePath);
                var originalImporter = AssetImporter.GetAtPath("Assets/_Project/Art/Tiles/" + Images[i] + "_01.png") as TextureImporter;
                var textureSettings = new TextureImporterSettings();
                originalImporter.ReadTextureSettings(textureSettings);
                importer.SetTextureSettings(textureSettings);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 256;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 256;
                importer.SaveAndReimport();
                var original = AssetDatabase.LoadAssetAtPath<Tile>("Assets/_Project/Tilemaps/DemoWorld/" + Names[i] + ".asset");
                string tilePath = "Assets/_Project/Tilemaps/DemoWorld/" + Names[i] + "Gold.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = Object.Instantiate(original);
                    AssetDatabase.CreateAsset(tile, tilePath);
                }
                tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
                EditorUtility.SetDirty(tile);
                serialized.FindProperty(Fields[i]).objectReferenceValue = tile;

                if (!resolver.TryResolve(original, out MiningTileDto definition))
                    throw new System.InvalidOperationException("기반 타일 정의 누락: " + Names[i]);
                var variant = settings.CreateVariant(definition);
                int targetIndex = -1;
                for (int j = 0; j < entries.arraySize; j++)
                {
                    var entry = entries.GetArrayElementAtIndex(j);
                    if (entry.FindPropertyRelative("tile").objectReferenceValue == tile) targetIndex = j;
                }
                if (targetIndex < 0)
                {
                    targetIndex = entries.arraySize++;
                }
                var target = entries.GetArrayElementAtIndex(targetIndex);
                target.FindPropertyRelative("tile").objectReferenceValue = tile;
                var targetDefinition = target.FindPropertyRelative("definition");
                targetDefinition.boxedValue = variant;
                resolver.RegisterRuntime(tile, variant);
            }
            resolverData.ApplyModifiedPropertiesWithoutUndo();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(generator);
            EditorUtility.SetDirty(resolver);
        }
    }
}
