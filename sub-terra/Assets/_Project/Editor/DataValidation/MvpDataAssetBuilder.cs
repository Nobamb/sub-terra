using System.Collections.Generic;
using System.IO;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// MVP 필수 데이터 에셋·카탈로그·Bootstrap 연결을 Editor에서 생성한다.
    /// 암묵 Resources 검색이 아니라 명시 경로에 에셋을 만들고 카탈로그 목록에 등록한다.
    /// </summary>
    public static class MvpDataAssetBuilder
    {
        private const string Root = "Assets/_Project/Data";
        private const string CatalogPath = Root + "/Catalog/GameDataCatalog.asset";
        private const string PrefabPath = Root + "/Prefabs/Buildings/BuildingPlaceholder.prefab";
        private const string IconPath = Root + "/Icons/DataPlaceholder.asset";
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";

        [MenuItem("SubTerra/Data/Build MVP Data Assets")]
        public static void BuildFromMenu()
        {
            var report = BuildAll();
            Debug.Log("[SubTerra] " + report);
        }

        [InitializeOnLoadMethod]
        private static void AutoBuildIfMissing()
        {
            // 도메인 리로드마다 실행되지만, 카탈로그가 이미 있으면 즉시 반환한다.
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath) != null)
                {
                    return;
                }

                try
                {
                    var report = BuildAll();
                    Debug.Log("[SubTerra] Auto-created MVP data assets. " + report);
                }
                catch (System.Exception ex)
                {
                    // 예외 메시지에는 로컬 경로가 포함될 수 있어 타입명만 기록한다.
                    Debug.LogError("[SubTerra] MVP data auto-build failed: " + ex.GetType().Name);
                }
            };
        }

        public static string BuildAll()
        {
            EnsureFolders();

            var prefab = EnsurePlaceholderPrefab();
            var icon = EnsurePlaceholderIcon();
            var minerals = BuildMinerals(icon);
            var buildings = BuildBuildings(prefab, icon);
            var recipes = BuildRecipes();
            var upgrades = BuildUpgrades();
            var dialogues = BuildDialogues();

            var catalog = EnsureCatalog();
            catalog.EditorSetLists(minerals, buildings, recipes, upgrades, dialogues);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            var validation = catalog.ValidateAll();
            WireBootstrap(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return
                $"Catalog={CatalogPath}; valid={validation.IsValid}; errors={validation.ErrorCount}; " +
                $"minerals={minerals.Count}; buildings={buildings.Count}; recipes={recipes.Count}; " +
                $"upgrades={upgrades.Count}; dialogues={dialogues.Count}; dictInit={validation.DictionaryInitialized}";
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project", "Data");
            EnsureFolder(Root, "Minerals");
            EnsureFolder(Root, "Buildings");
            EnsureFolder(Root, "Recipes");
            EnsureFolder(Root, "Upgrades");
            EnsureFolder(Root, "Dialogue");
            EnsureFolder(Root, "Catalog");
            EnsureFolder(Root, "Icons");
            EnsureFolder(Root, "Prefabs");
            EnsureFolder(Root + "/Prefabs", "Buildings");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static GameObject EnsurePlaceholderPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("BuildingPlaceholder");
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        private static Sprite EnsurePlaceholderIcon()
        {
            var existing = AssetDatabase.LoadAllAssetsAtPath(IconPath);
            for (var i = 0; i < existing.Length; i++)
            {
                if (existing[i] is Sprite sprite)
                {
                    return sprite;
                }
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (texture == null)
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = "DataPlaceholderTexture"
                };
                var color = new Color(0.2f, 0.75f, 0.8f, 1f);
                texture.SetPixels(new[] { color, color, color, color });
                texture.Apply();
                AssetDatabase.CreateAsset(texture, IconPath);
            }

            var created = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                2f);
            created.name = "DataPlaceholder";
            AssetDatabase.AddObjectToAsset(created, texture);
            EditorUtility.SetDirty(texture);
            AssetDatabase.SaveAssets();
            return created;
        }

        private static GameDataCatalog EnsureCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            if (catalog != null)
            {
                return catalog;
            }

            catalog = ScriptableObject.CreateInstance<GameDataCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        private static List<MineralData> BuildMinerals(Sprite icon)
        {
            return new List<MineralData>
            {
                EnsureMineral("Mineral_Copper.asset", DataIds.Minerals.Copper, "Copper", 1.5f, 10, icon),
                EnsureMineral("Mineral_Iron.asset", DataIds.Minerals.Iron, "Iron", 2f, 15, icon),
                EnsureMineral("Mineral_Lithium.asset", DataIds.Minerals.Lithium, "Lithium", 0.8f, 40, icon)
            };
        }

        private static MineralData EnsureMineral(
            string file,
            string id,
            string name,
            float weight,
            int price,
            Sprite icon)
        {
            var path = Root + "/Minerals/" + file;
            var asset = AssetDatabase.LoadAssetAtPath<MineralData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MineralData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EditorSet(id, name, weight, price, icon);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static List<BuildingData> BuildBuildings(GameObject prefab, Sprite icon)
        {
            return new List<BuildingData>
            {
                EnsureBuilding("Building_Support_Basic.asset", DataIds.Buildings.SupportBasic, "Basic Support", prefab, 0,
                    icon, new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Copper, 2) }),
                EnsureBuilding("Building_Light_Basic.asset", DataIds.Buildings.LightBasic, "Basic Light", prefab, 1,
                    icon, new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Iron, 1) }),
                EnsureBuilding("Building_Charger_Basic.asset", DataIds.Buildings.ChargerBasic, "Basic Charger", prefab, 3,
                    icon, new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Copper, 3) }),
                EnsureBuilding("Building_Storage_Basic.asset", DataIds.Buildings.StorageBasic, "Basic Storage", prefab, 0,
                    icon, new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Iron, 2) }),
                EnsureBuilding("Building_Settlement_Basic.asset", DataIds.Buildings.SettlementBasic, "Settlement Console", prefab, 1,
                    icon, new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Lithium, 1) }),
                EnsureBuilding("Building_OutpostCore_Basic.asset", DataIds.Buildings.OutpostCoreBasic, "Outpost Core", prefab, 5,
                    icon, new List<ItemCostEntry>
                    {
                        new ItemCostEntry(DataIds.Minerals.Copper, 5),
                        new ItemCostEntry(DataIds.Minerals.Iron, 5)
                    })
            };
        }

        private static BuildingData EnsureBuilding(
            string file,
            string id,
            string name,
            GameObject prefab,
            int power,
            Sprite icon,
            List<ItemCostEntry> costs)
        {
            var path = Root + "/Buildings/" + file;
            var asset = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<BuildingData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EditorSet(id, name, prefab, icon, power, costs);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static List<RecipeData> BuildRecipes()
        {
            var path = Root + "/Recipes/Recipe_Support_Basic.asset";
            var asset = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RecipeData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EditorSet(
                DataIds.Recipes.SupportBasic,
                "Craft Basic Support",
                DataIds.Buildings.SupportBasic,
                new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Copper, 2) },
                new List<ItemCostEntry> { new ItemCostEntry(DataIds.Buildings.SupportBasic, 1) });
            EditorUtility.SetDirty(asset);
            return new List<RecipeData> { asset };
        }

        private static List<UpgradeData> BuildUpgrades()
        {
            return new List<UpgradeData>
            {
                EnsureUpgrade("Upgrade_Drill_Speed.asset", DataIds.Upgrades.DrillSpeed, "Drill Speed", 3, 0.1f),
                EnsureUpgrade("Upgrade_Drill_Efficiency.asset", DataIds.Upgrades.DrillEfficiency, "Drill Efficiency", 3, 0.05f),
                EnsureUpgrade("Upgrade_Drone_Scan.asset", DataIds.Upgrades.DroneScan, "Drone Scan", 2, 1f),
                EnsureUpgrade("Upgrade_Drone_Rescue.asset", DataIds.Upgrades.DroneRescue, "Drone Rescue", 2, 1f)
            };
        }

        private static UpgradeData EnsureUpgrade(string file, string id, string name, int maxLevel, float effect)
        {
            var path = Root + "/Upgrades/" + file;
            var asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<UpgradeData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var levels = new List<UpgradeLevelDefinition>();
            for (var i = 1; i <= maxLevel; i++)
            {
                levels.Add(new UpgradeLevelDefinition(
                    i,
                    effect * i,
                    new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Copper, i) }));
            }

            asset.EditorSet(id, name, maxLevel, levels);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static List<DialogueTemplateData> BuildDialogues()
        {
            var path = Root + "/Dialogue/Dialogue_LowPower_Warning.asset";
            var asset = AssetDatabase.LoadAssetAtPath<DialogueTemplateData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DialogueTemplateData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EditorSet(
                DataIds.Dialogue.LowPowerWarning,
                "Low Power Warning",
                "low_power",
                100,
                "Power is low. Return or recharge.");
            EditorUtility.SetDirty(asset);
            return new List<DialogueTemplateData> { asset };
        }

        private static void WireBootstrap(GameDataCatalog catalog)
        {
            if (!File.Exists(BootstrapScenePath))
            {
                Debug.LogWarning("[SubTerra] Bootstrap scene missing; skip wiring.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            GameBootstrapper bootstrapper = null;
            for (var i = 0; i < roots.Length; i++)
            {
                bootstrapper = roots[i].GetComponentInChildren<GameBootstrapper>(true);
                if (bootstrapper != null)
                {
                    break;
                }
            }

            if (bootstrapper == null)
            {
                Debug.LogWarning("[SubTerra] GameBootstrapper not found in Bootstrap scene.");
                return;
            }

            var so = new SerializedObject(bootstrapper);
            var prop = so.FindProperty("gameDataCatalog");
            if (prop == null)
            {
                Debug.LogWarning("[SubTerra] gameDataCatalog property not found on GameBootstrapper.");
                return;
            }

            prop.objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
