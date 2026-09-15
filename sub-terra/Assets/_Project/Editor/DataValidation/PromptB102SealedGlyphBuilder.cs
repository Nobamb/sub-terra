using SubTerra.App.Core.Data;
using SubTerra.App.UI.Inventory;
using SubTerra.Gameplay.Drone;
using SubTerra.Gameplay.Mining;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>prompt-B 102: X 그레이박스를 문양 석재로 교체하고 엔진 연료 채굴 정의를 연결한다.</summary>
    public static class PromptB102SealedGlyphBuilder
    {
        private const string StoneSpritePath =
            "Assets/_Project/Art/Tiles/SealedGlyph/sealed_glyph_stone_01.png";
        private const string GlyphSpritePath =
            "Assets/_Project/Art/Tiles/SealedGlyph/sealed_glyph_awakened_01.png";
        private const string LockedSignalTilePath =
            "Assets/_Project/Tilemaps/DemoWorld/LockedSignal.asset";
        private const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string DemoWorldScenePath =
            "Assets/_Project/Scenes/Test/Gameplay/Gameplay_DemoWorld_Test.unity";
        private const string EngineFuelId = DataIds.RareItems.EngineFuel;

        [MenuItem("SubTerra/Data/Build Prompt-B 102 Sealed Glyph Engine Fuel")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            RegisterEngineFuelItem();
            Sprite stone = LoadSprite(StoneSpritePath);
            Sprite glyph = LoadSprite(GlyphSpritePath);
            if (stone == null || glyph == null)
            {
                throw new System.InvalidOperationException(
                    "문양 석재 스프라이트가 없습니다. " + StoneSpritePath);
            }

            ApplyLockedSignalTile(stone);
            AddInventoryRareRow();
            string previous = SceneManager.GetActiveScene().path;
            WireScene(IntegrationScenePath, glyph);

            if (!string.IsNullOrEmpty(previous) && previous != DemoWorldScenePath)
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Prompt-B 102 sealed glyph stone/glyph sprites, engine fuel catalog, and mining wiring applied.";
        }

        private static void RegisterEngineFuelItem()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Data/RareItems"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Data", "RareItems");
            }

            const string path = "Assets/_Project/Data/RareItems/RareItem_EngineFuel.asset";
            var item = AssetDatabase.LoadAssetAtPath<MineralData>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<MineralData>();
                AssetDatabase.CreateAsset(item, path);
            }

            item.EditorSet(
                EngineFuelId,
                "엔진 연료",
                1f,
                100,
                LoadSprite("Assets/_Project/Art/Icons/icon_engine_fuel.png"));
            EditorUtility.SetDirty(item);

            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(
                "Assets/_Project/Data/Catalog/GameDataCatalog.asset");
            if (catalog == null)
            {
                throw new System.InvalidOperationException("GameDataCatalog missing.");
            }

            var rareItems = new System.Collections.Generic.List<MineralData>();
            if (catalog.RareItems != null)
            {
                for (var i = 0; i < catalog.RareItems.Count; i++)
                {
                    if (catalog.RareItems[i] != null)
                    {
                        rareItems.Add(catalog.RareItems[i]);
                    }
                }
            }

            var found = false;
            for (var i = 0; i < rareItems.Count; i++)
            {
                if (rareItems[i] != null && rareItems[i].Id == EngineFuelId)
                {
                    rareItems[i] = item;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                rareItems.Add(item);
            }

            catalog.EditorSetLists(
                catalog.Minerals != null
                    ? new System.Collections.Generic.List<MineralData>(catalog.Minerals)
                    : new System.Collections.Generic.List<MineralData>(),
                catalog.MiningTiles != null
                    ? new System.Collections.Generic.List<MiningTileData>(catalog.MiningTiles)
                    : new System.Collections.Generic.List<MiningTileData>(),
                catalog.Buildings != null
                    ? new System.Collections.Generic.List<BuildingData>(catalog.Buildings)
                    : new System.Collections.Generic.List<BuildingData>(),
                catalog.Recipes != null
                    ? new System.Collections.Generic.List<RecipeData>(catalog.Recipes)
                    : new System.Collections.Generic.List<RecipeData>(),
                catalog.Upgrades != null
                    ? new System.Collections.Generic.List<UpgradeData>(catalog.Upgrades)
                    : new System.Collections.Generic.List<UpgradeData>(),
                catalog.Dialogues != null
                    ? new System.Collections.Generic.List<DialogueTemplateData>(catalog.Dialogues)
                    : new System.Collections.Generic.List<DialogueTemplateData>(),
                rareItems);
            EditorUtility.SetDirty(catalog);
        }

        private static void AddInventoryRareRow()
        {
            const string prefabPath = "Assets/_Project/Prefabs/UI/InventoryPanel.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var view = root.GetComponent<InventoryPanelView>();
                var rowsRoot = root.transform.Find("PanelRoot/StackRows");
                if (view == null || rowsRoot == null)
                {
                    throw new System.InvalidOperationException("InventoryPanel StackRows missing.");
                }

                if (rowsRoot.Find("Stack_" + EngineFuelId) != null)
                {
                    return;
                }

                InventoryStackRowView template = rowsRoot.GetComponentInChildren<InventoryStackRowView>(true);
                if (template == null)
                {
                    throw new System.InvalidOperationException("Inventory stack row template missing.");
                }

                var clone = Object.Instantiate(template.gameObject, rowsRoot);
                clone.name = "Stack_" + EngineFuelId;
                var rect = clone.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(0f, -3 * 58f);
                var icon = clone.transform.Find("Icon")?.GetComponent<Image>();
                var name = clone.transform.Find("Name")?.GetComponent<TMP_Text>();
                var quantity = clone.transform.Find("Quantity")?.GetComponent<TMP_Text>();
                if (icon != null)
                {
                    icon.sprite = LoadSprite("Assets/_Project/Art/Icons/icon_engine_fuel.png");
                    icon.enabled = icon.sprite != null;
                }

                if (name != null)
                {
                    name.text = ItemDisplayNames.Inventory(EngineFuelId);
                }

                if (quantity != null)
                {
                    quantity.text = "x0";
                }

                var row = clone.GetComponent<InventoryStackRowView>();
                row.EditorSetReferences(EngineFuelId, icon, name, quantity);

                var viewSo = new SerializedObject(view);
                var rowProperty = viewSo.FindProperty("stackRows");
                rowProperty.arraySize += 1;
                rowProperty.GetArrayElementAtIndex(rowProperty.arraySize - 1).objectReferenceValue = row;
                viewSo.ApplyModifiedPropertiesWithoutUndo();

                var rootRect = root.GetComponent<RectTransform>();
                if (rootRect.sizeDelta.y < 390f)
                {
                    rootRect.sizeDelta = new Vector2(rootRect.sizeDelta.x, 390f);
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyLockedSignalTile(Sprite stone)
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(LockedSignalTilePath);
            if (tile == null)
            {
                throw new System.InvalidOperationException("LockedSignal tile missing.");
            }

            tile.sprite = stone;
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
        }

        private static void WireScene(string scenePath, Sprite glyph)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var resolver = FindInScene<MiningTileResolver>(scene);
            var tilemap = FindNamed<Tilemap>(scene, "ForegroundTilemap");
            var sensor = FindInScene<DroneSensor>(scene);
            if (resolver != null)
            {
                UpdateLockedSignalDefinition(resolver);
                EditorUtility.SetDirty(resolver);
            }

            if (tilemap != null && resolver != null && sensor != null)
            {
                var host = sensor.gameObject;
                var visual = host.GetComponent<SealedGlyphScanVisual>()
                    ?? host.AddComponent<SealedGlyphScanVisual>();
                visual.Configure(tilemap, resolver, sensor, glyph);
                EditorUtility.SetDirty(visual);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void UpdateLockedSignalDefinition(MiningTileResolver resolver)
        {
            var serialized = new SerializedObject(resolver);
            var entries = serialized.FindProperty("entries");
            for (var index = 0; index < entries.arraySize; index++)
            {
                var definition = entries.GetArrayElementAtIndex(index).FindPropertyRelative("definition");
                if (definition.FindPropertyRelative("tileId").stringValue != "tile.locked.signal")
                {
                    continue;
                }

                definition.FindPropertyRelative("mineralId").stringValue = EngineFuelId;
                definition.FindPropertyRelative("quantity").intValue = 1;
                definition.FindPropertyRelative("isMineable").boolValue = true;
                definition.FindPropertyRelative("miningTime").floatValue = 1.2f;
                definition.FindPropertyRelative("requiredDrillLevel").intValue = 2;
                definition.FindPropertyRelative("energyCost").intValue = 3;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite LoadSprite(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    return sprite;
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static T FindNamed<T>(Scene scene, string objectName) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<T>(true))
                {
                    if (component.name == objectName)
                    {
                        return component;
                    }
                }
            }

            return null;
        }
    }
}
