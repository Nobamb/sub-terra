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
        private const string SupportPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Buildings/SupportPillar.prefab";
        private const string EmergencyEscapePortalPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Buildings/EmergencyEscapePortal.prefab";
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
            var supportPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SupportPrefabPath)
                ?? prefab;
            var emergencyEscapePortalPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(EmergencyEscapePortalPrefabPath)
                ?? prefab;
            var icon = EnsurePlaceholderIcon();
            var minerals = BuildMinerals(icon);
            var buildings = BuildBuildings(
                prefab,
                supportPrefab,
                emergencyEscapePortalPrefab,
                icon);
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

        private static List<BuildingData> BuildBuildings(
            GameObject prefab,
            GameObject supportPrefab,
            GameObject emergencyEscapePortalPrefab,
            Sprite icon)
        {
            return new List<BuildingData>
            {
                EnsureBuilding("Building_Support_Basic.asset", DataIds.Buildings.SupportBasic, "기본 버팀목",
                    "주변 지형을 보강해 구조 위험을 낮춥니다.", supportPrefab, 0,
                    icon, new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Copper, 2) }),
                EnsureBuilding("Building_Light_Basic.asset", DataIds.Buildings.LightBasic, "기본 조명",
                    "전력이 연결된 지하 구역을 밝힙니다.", prefab, 1,
                    icon, new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Iron, 1) }),
                EnsureBuilding("Building_Charger_Basic.asset", DataIds.Buildings.ChargerBasic, "기본 충전기",
                    "전력망에 연결되면 플레이어 장비를 충전합니다.", prefab, 3,
                    icon, new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Copper, 3) }),
                EnsureBuilding("Building_Storage_Basic.asset", DataIds.Buildings.StorageBasic, "기본 보관함",
                    "탐사 중 수집한 광물을 임시 보관합니다.", prefab, 0,
                    icon, new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Iron, 2) }),
                EnsureBuilding("Building_Settlement_Basic.asset", DataIds.Buildings.SettlementBasic, "정산 콘솔",
                    "보관한 광물을 정산해 골드로 전환합니다.", prefab, 1,
                    icon, new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Lithium, 1) }),
                EnsureBuilding("Building_OutpostCore_Basic.asset", DataIds.Buildings.OutpostCoreBasic, "전진기지 코어",
                    "연결된 시설에 전력을 공급하고 유독 가스 정화 안전지대를 형성하며 탐사 체크포인트 역할을 합니다.", prefab, 5,
                    icon, new List<ItemCostEntry>
                    {
                        new ItemCostEntry(DataIds.Minerals.Copper, 5),
                        new ItemCostEntry(DataIds.Minerals.Iron, 5)
                    }),
                EnsureBuilding("Building_EmergencyEscapePortal.asset", DataIds.Buildings.EmergencyEscapePortal,
                    "긴급 탈출 포탈",
                    "E키로 사용합니다. 100G와 최대 전력의 10%를 소모해 최근 전진기지 코어 또는 엘리베이터로 이동합니다.",
                    emergencyEscapePortalPrefab, 30,
                    icon, new List<ItemCostEntry>
                    {
                        new ItemCostEntry(DataIds.Minerals.Iron, 3),
                        new ItemCostEntry(DataIds.Minerals.Lithium, 3)
                    })
            };
        }

        private static BuildingData EnsureBuilding(
            string file,
            string id,
            string name,
            string description,
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

            asset.EditorSet(id, name, description, prefab, icon, power, costs);
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
                EnsureUpgrade("Upgrade_Drill_Speed.asset", DataIds.Upgrades.DrillSpeed, "드릴 속도", 3, 0.1f),
                EnsureUpgrade("Upgrade_Drill_Efficiency.asset", DataIds.Upgrades.DrillEfficiency, "드릴 전력 효율", 3, 0.05f),
                EnsureUpgrade("Upgrade_Maximum_Energy.asset", DataIds.Upgrades.MaximumEnergy, "최대 전력", 3, 20f),
                EnsureHealthUpgrade("Upgrade_Maximum_Health.asset", DataIds.Upgrades.MaximumHealth, "최대 체력", new[] { 30f, 60f, 100f }),
                EnsureHealthUpgrade("Upgrade_Health_Regeneration.asset", DataIds.Upgrades.HealthRegeneration, "초당 체력 재생", new[] { 0.3f, 0.6f, 1f }),
                EnsureUpgrade("Upgrade_Maximum_Cargo.asset", DataIds.Upgrades.MaximumCargo, "최대 화물 중량", 3, 10f),
                EnsureUpgrade("Upgrade_Drone_Scan.asset", DataIds.Upgrades.DroneScan, "드론 스캔 범위", 2, 2f),
                EnsureUpgrade("Upgrade_Drone_Rescue.asset", DataIds.Upgrades.DroneRescue, "드론 구조 보존", 2, 0.15f),
                EnsureUpgrade("Upgrade_Gas_Resistance.asset", DataIds.Upgrades.GasResistance, "가스 저항", 3, 0.1f)
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

        private static UpgradeData EnsureHealthUpgrade(
            string file,
            string id,
            string name,
            IReadOnlyList<float> effects)
        {
            var path = Root + "/Upgrades/" + file;
            var asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<UpgradeData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EditorSet(
                id,
                name,
                3,
                new List<UpgradeLevelDefinition>
                {
                    new UpgradeLevelDefinition(1, effects[0], new List<ItemCostEntry>
                    {
                        new ItemCostEntry(DataIds.Minerals.Copper, 1)
                    }),
                    new UpgradeLevelDefinition(2, effects[1], new List<ItemCostEntry>
                    {
                        new ItemCostEntry(DataIds.Minerals.Copper, 2),
                        new ItemCostEntry(DataIds.Minerals.Iron, 1)
                    }),
                    new UpgradeLevelDefinition(3, effects[2], new List<ItemCostEntry>
                    {
                        new ItemCostEntry(DataIds.Minerals.Copper, 3),
                        new ItemCostEntry(DataIds.Minerals.Iron, 2)
                    })
                });
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static List<DialogueTemplateData> BuildDialogues()
        {
            return new List<DialogueTemplateData>
            {
                EnsureDialogue(
                    "Dialogue_Drone_Emergency.asset",
                    DataIds.Dialogue.DroneEmergency,
                    "긴급 생존 경고",
                    "survival_emergency",
                    700,
                    "{reason} {action} 가능 여부를 즉시 확인하세요."),
                EnsureDialogue(
                    "Dialogue_Drone_Structural_Warning.asset",
                    DataIds.Dialogue.DroneStructuralWarning,
                    "붕괴 임박 경고",
                    "structural_critical",
                    600,
                    "{safetyGuidance}"),
                EnsureDialogue(
                    "Dialogue_Drone_Gas_Warning.asset",
                    DataIds.Dialogue.DroneGasWarning,
                    "가스 위험 경고",
                    "gas_risk",
                    500,
                    "{safetyGuidance}"),
                EnsureDialogue(
                    "Dialogue_LowPower_Warning.asset",
                    DataIds.Dialogue.LowPowerWarning,
                    "전력 부족 경고",
                    "low_power",
                    400,
                    "현재 전력 {currentEnergy}, 귀환 예상 {returnEnergyEstimate}. {action}을 권장합니다."),
                EnsureDialogue(
                    "Dialogue_Drone_Cargo_Full.asset",
                    DataIds.Dialogue.DroneCargoFull,
                    "인벤토리 가득 참",
                    "cargo_full",
                    350,
                    "화물 {cargoWeight}/{maxCargoWeight}. 더 담을 수 없어 기지 귀환을 권장합니다."),
                EnsureDialogue(
                    "Dialogue_Drone_Return.asset",
                    DataIds.Dialogue.DroneReturn,
                    "귀환 추천",
                    "return",
                    300,
                    "미정산 가치 {unsettledCargoValue}. 기지 귀환을 권장합니다."),
                EnsureDialogue(
                    "Dialogue_Drone_Lithium.asset",
                    DataIds.Dialogue.DroneLithium,
                    "희귀 광물 발견",
                    "rare_mineral",
                    200,
                    "인근에서 {mineralId} 신호를 확인했습니다."),
                EnsureDialogue(
                    "Dialogue_Drone_Outpost.asset",
                    DataIds.Dialogue.DroneOutpost,
                    "전진기지 추천",
                    "outpost",
                    100,
                    "기지 거리 {nearestBaseDistance}m. 전진기지 설치를 검토하세요."),
                EnsureDialogue(
                    "Dialogue_Drone_Explore.asset",
                    DataIds.Dialogue.DroneExplore,
                    "일반 탐사",
                    "explore",
                    0,
                    "현재 심도 {depth}. 확인된 즉시 위험이 없어 하강을 계속할 수 있습니다.")
            };
        }

        private static DialogueTemplateData EnsureDialogue(
            string file,
            string id,
            string name,
            string situation,
            int priority,
            string template)
        {
            var path = Root + "/Dialogue/" + file;
            var asset = AssetDatabase.LoadAssetAtPath<DialogueTemplateData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DialogueTemplateData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EditorSet(
                id,
                name,
                situation,
                priority,
                template);
            EditorUtility.SetDirty(asset);
            return asset;
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
