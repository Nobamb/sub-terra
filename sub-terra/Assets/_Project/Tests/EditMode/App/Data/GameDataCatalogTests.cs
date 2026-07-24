using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.State;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.Data
{
    /// <summary>
    /// B-F01~B-F04 및 잘못된 ID/수치 케이스. 실제 카탈로그·검증기를 구동한다.
    /// </summary>
    public sealed class GameDataCatalogTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < created.Count; i++)
            {
                if (created[i] != null)
                {
                    Object.DestroyImmediate(created[i]);
                }
            }

            created.Clear();
            GameBootstrapper.ResetInstanceForTests();
        }

        [Test]
        public void B_F01_TryGet_ResolvesRequiredIdsToSingleDefinition()
        {
            var catalog = CreateValidMvpCatalog();

            Assert.That(catalog.TryGetMineral(DataIds.Minerals.Copper, out var copper), Is.True);
            Assert.That(copper.Id, Is.EqualTo(DataIds.Minerals.Copper));
            Assert.That(copper.UnitWeight, Is.EqualTo(1.5f));
            Assert.That(copper.UnitPrice, Is.EqualTo(10));

            Assert.That(catalog.TryGetMineral(DataIds.Minerals.Iron, out var iron), Is.True);
            Assert.That(iron.UnitWeight, Is.EqualTo(2f));
            Assert.That(catalog.TryGetMineral(DataIds.Minerals.Lithium, out var lithium), Is.True);
            Assert.That(lithium.UnitPrice, Is.EqualTo(40));

            Assert.That(catalog.TryGetBuilding(DataIds.Buildings.SupportBasic, out var support), Is.True);
            Assert.That(support.RuntimePrefab, Is.Not.Null);
            Assert.That(support.PowerDraw, Is.EqualTo(0));

            Assert.That(catalog.TryGetBuilding(DataIds.Buildings.OutpostCoreBasic, out var core), Is.True);
            Assert.That(core.Id, Is.EqualTo(DataIds.Buildings.OutpostCoreBasic));

            Assert.That(catalog.TryGetUpgrade(DataIds.Upgrades.DrillSpeed, out var drill), Is.True);
            Assert.That(drill.MaxLevel, Is.EqualTo(3));
            Assert.That(drill.Levels.Count, Is.GreaterThanOrEqualTo(1));

            Assert.That(catalog.TryGetUpgrade(DataIds.Upgrades.DroneScan, out var scan), Is.True);
            Assert.That(scan.Id, Is.EqualTo(DataIds.Upgrades.DroneScan));

            Assert.That(catalog.TryGetRecipe(DataIds.Recipes.SupportBasic, out var recipe), Is.True);
            Assert.That(recipe.Inputs.Count, Is.GreaterThan(0));

            Assert.That(catalog.TryGetDialogue(DataIds.Dialogue.LowPowerWarning, out var dialogue), Is.True);
            Assert.That(dialogue.SituationKey, Is.EqualTo("low_power"));
        }

        [Test]
        public void B_F01_UnknownId_ReturnsFalse()
        {
            var catalog = CreateValidMvpCatalog();
            Assert.That(catalog.TryGetMineral("mineral.unknown", out _), Is.False);
            Assert.That(catalog.TryGetBuilding("building.missing", out _), Is.False);
            Assert.That(catalog.TryGetUpgrade("upgrade.none", out _), Is.False);
        }

        [Test]
        public void ProjectCatalog_ContainsRequiredAssetsAndReferences()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(
                "Assets/_Project/Data/Catalog/GameDataCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            var result = catalog.ValidateAll();
            Assert.That(result.IsValid, Is.True, result.FormatAll());
            Assert.That(catalog.TryGetMineral(DataIds.Minerals.Copper, out var copper), Is.True);
            Assert.That(copper.Icon, Is.Not.Null);
            Assert.That(catalog.TryGetBuilding(DataIds.Buildings.SupportBasic, out var support), Is.True);
            Assert.That(support.Icon, Is.Not.Null);
            Assert.That(support.RuntimePrefab, Is.Not.Null);
        }

        [Test]
        public void EditorValidation_ReportsDataAssetMissingFromCatalogRegistration()
        {
            const string path = "Assets/_Project/Data/Minerals/Mineral_Unregistered_Test.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(
                "Assets/_Project/Data/Catalog/GameDataCatalog.asset");
            var unregistered = ScriptableObject.CreateInstance<MineralData>();
            unregistered.EditorSet(
                "mineral.unregistered_test",
                "Unregistered",
                1f,
                1,
                CreateTestIcon());
            AssetDatabase.CreateAsset(unregistered, path);

            try
            {
                LogAssert.Expect(
                    LogType.Error,
                    new System.Text.RegularExpressions.Regex("Catalog validation failed"));
                var result = DataCatalogValidationMenu.ValidateCatalog(catalog);

                Assert.That(result.IsValid, Is.False);
                Assert.That(
                    result.Issues.Any(i =>
                        i.AssetPath == path && i.FieldName == "catalogRegistration"),
                    Is.True);
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void B_F02_DuplicateId_ReportsBothAssetsAndDoesNotInitDictionaryAsSuccess()
        {
            var a = CreateMineral("Mineral_A", DataIds.Minerals.Copper, "Copper A", 1f, 1);
            var b = CreateMineral("Mineral_B", DataIds.Minerals.Copper, "Copper B", 2f, 2);
            var catalog = ScriptableObject.CreateInstance<GameDataCatalog>();
            Track(catalog);
            catalog.EditorSetLists(
                new List<MineralData> { a, b },
                new List<BuildingData>(),
                new List<RecipeData>(),
                new List<UpgradeData>(),
                new List<DialogueTemplateData>());

            var result = catalog.ValidateAll();

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.DictionaryInitialized, Is.False);
            var messages = result.Issues.Select(i => i.ToString()).ToList();
            Assert.That(messages.Any(m => m.Contains("Mineral_A")), Is.True);
            Assert.That(messages.Any(m => m.Contains("Mineral_B")), Is.True);
            Assert.That(messages.Any(m => m.Contains("id") || m.Contains("Duplicate")), Is.True);

            // 중복 시 조회 딕셔너리는 성공으로 취급하지 않는다.
            Assert.That(catalog.TryGetMineral(DataIds.Minerals.Copper, out _), Is.False);
        }

        [Test]
        public void CrossTypeDuplicateId_IsRejected()
        {
            var sharedId = DataIds.Minerals.Copper;
            var mineral = CreateMineral("CrossTypeMineral", sharedId, "Copper", 1f, 1);
            var building = CreateBuilding(
                "CrossTypeBuilding",
                sharedId,
                "Bad Building",
                Track(new GameObject("CrossTypePrefab")),
                0,
                new List<ItemCostEntry> { new ItemCostEntry(sharedId, 1) });
            var catalog = ScriptableObject.CreateInstance<GameDataCatalog>();
            Track(catalog);
            catalog.EditorSetLists(
                new List<MineralData> { mineral },
                new List<BuildingData> { building },
                new List<RecipeData>(),
                new List<UpgradeData>(),
                new List<DialogueTemplateData>());

            var result = catalog.ValidateAll();

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.DictionaryInitialized, Is.False);
            Assert.That(result.Issues.Any(i => i.Message.Contains("across data types")), Is.True);
        }

        [Test]
        public void B_F03_MissingRequiredPrefab_IdentifiesAssetAndField()
        {
            var building = CreateBuilding(
                "Building_MissingPrefab",
                DataIds.Buildings.SupportBasic,
                "Support",
                prefab: null,
                power: 0,
                costs: new List<ItemCostEntry>());
            var catalog = ScriptableObject.CreateInstance<GameDataCatalog>();
            Track(catalog);
            catalog.EditorSetLists(
                new List<MineralData>(),
                new List<BuildingData> { building },
                new List<RecipeData>(),
                new List<UpgradeData>(),
                new List<DialogueTemplateData>());

            var result = catalog.ValidateAll();

            Assert.That(result.IsValid, Is.False);
            var issue = result.Issues.FirstOrDefault(i =>
                i.FieldName == "runtimePrefab" && i.AssetPath.Contains("Building_MissingPrefab"));
            Assert.That(issue, Is.Not.Null, "Expected runtimePrefab issue with asset name.");
            Assert.That(issue.Message, Does.Contain("prefab").IgnoreCase);
        }

        [Test]
        public void MissingRequiredIcon_IdentifiesAssetAndField()
        {
            var mineral = ScriptableObject.CreateInstance<MineralData>();
            mineral.name = "Mineral_MissingIcon";
            mineral.EditorSet(DataIds.Minerals.Copper, "Copper", 1f, 1, null);
            Track(mineral);

            var catalog = ScriptableObject.CreateInstance<GameDataCatalog>();
            Track(catalog);
            catalog.EditorSetLists(
                new List<MineralData> { mineral },
                new List<BuildingData>(),
                new List<RecipeData>(),
                new List<UpgradeData>(),
                new List<DialogueTemplateData>());

            var result = catalog.ValidateAll();

            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Issues.Any(i =>
                    i.AssetPath.Contains("Mineral_MissingIcon") && i.FieldName == "icon"),
                Is.True);
            Assert.That(result.DictionaryInitialized, Is.False);
        }

        [Test]
        public void UnknownRecipeReference_IsReportedAndLookupRemainsUnavailable()
        {
            var catalog = CreateValidMvpCatalog();
            var badRecipe = CreateRecipe(
                "Recipe_BadReference",
                "recipe.bad.reference",
                "Broken Recipe",
                "building.not_registered",
                new List<ItemCostEntry> { new ItemCostEntry("mineral.not_registered", 1) },
                new List<ItemCostEntry> { new ItemCostEntry("building.not_registered", 1) });

            catalog.EditorSetLists(
                catalog.Minerals.ToList(),
                catalog.Buildings.ToList(),
                new List<RecipeData> { badRecipe },
                catalog.Upgrades.ToList(),
                catalog.Dialogues.ToList());

            var result = catalog.ValidateAll();

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.DictionaryInitialized, Is.False);
            Assert.That(result.Issues.Any(i => i.FieldName == "resultBuildingId"), Is.True);
            Assert.That(result.Issues.Any(i => i.FieldName.Contains("inputs[0].itemId")), Is.True);
            Assert.That(catalog.TryGetRecipe("recipe.bad.reference", out _), Is.False);
        }

        [Test]
        public void EmptyBuildingCost_AndBrokenUpgradeLevels_AreRejected()
        {
            var catalog = CreateValidMvpCatalog();
            var prefab = Track(new GameObject("ValidationPrefab"));
            var building = CreateBuilding(
                "Building_EmptyCost",
                DataIds.Buildings.SupportBasic,
                "Support",
                prefab,
                0,
                new List<ItemCostEntry>());

            var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
            upgrade.name = "Upgrade_BrokenLevels";
            upgrade.EditorSet(
                DataIds.Upgrades.DrillSpeed,
                "Drill Speed",
                2,
                new List<UpgradeLevelDefinition>
                {
                    new UpgradeLevelDefinition(2, 0f, new List<ItemCostEntry>())
                });
            Track(upgrade);

            catalog.EditorSetLists(
                catalog.Minerals.ToList(),
                new List<BuildingData> { building },
                catalog.Recipes.ToList(),
                new List<UpgradeData> { upgrade },
                catalog.Dialogues.ToList());

            var result = catalog.ValidateAll();

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.Any(i => i.FieldName == "buildCosts"), Is.True);
            Assert.That(result.Issues.Any(i => i.FieldName == "levels"), Is.True);
            Assert.That(result.Issues.Any(i => i.FieldName.Contains("effectValue")), Is.True);
            Assert.That(result.Issues.Any(i => i.FieldName.Contains("costs")), Is.True);
        }

        [Test]
        public void B_F04_DisplayNameChange_DoesNotChangeIdOrLookup()
        {
            var catalog = CreateValidMvpCatalog();
            Assert.That(catalog.TryGetMineral(DataIds.Minerals.Copper, out var copper), Is.True);
            var permanentId = copper.Id;
            Assert.That(permanentId, Is.EqualTo(DataIds.Minerals.Copper));

            copper.EditorSet(permanentId, "Renamed Copper Display", copper.UnitWeight, copper.UnitPrice);

            Assert.That(catalog.TryGetMineral(DataIds.Minerals.Copper, out var again), Is.True);
            Assert.That(again, Is.SameAs(copper));
            Assert.That(again.Id, Is.EqualTo(DataIds.Minerals.Copper));
            Assert.That(again.DisplayName, Is.EqualTo("Renamed Copper Display"));
            Assert.That(catalog.TryGetMineral("Renamed Copper Display", out _), Is.False);
        }

        [Test]
        public void InvalidId_AndNegativeNumeric_AreAggregated()
        {
            var badId = CreateMineral("BadId", "Mineral.Copper", "Bad", 1f, 1);
            var negWeight = CreateMineral("NegWeight", "mineral.negweight.test", "Neg", -1f, 5);
            var negPrice = CreateMineral("NegPrice", "mineral.negprice.test", "NegP", 1f, -3);
            var catalog = ScriptableObject.CreateInstance<GameDataCatalog>();
            Track(catalog);
            catalog.EditorSetLists(
                new List<MineralData> { badId, negWeight, negPrice },
                new List<BuildingData>(),
                new List<RecipeData>(),
                new List<UpgradeData>(),
                new List<DialogueTemplateData>());

            var result = catalog.ValidateAll();

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorCount, Is.GreaterThanOrEqualTo(3));
            Assert.That(result.Issues.Any(i => i.AssetPath.Contains("BadId") && i.FieldName == "id"), Is.True);
            Assert.That(result.Issues.Any(i => i.FieldName == "unitWeight"), Is.True);
            Assert.That(result.Issues.Any(i => i.FieldName == "unitPrice"), Is.True);
        }

        [Test]
        public void IDataCatalogPort_Validate_ValidCatalog_ReturnsTrue()
        {
            IDataCatalogPort port = CreateValidMvpCatalog();
            Assert.That(port.Validate(out var reason), Is.True);
            Assert.That(reason, Is.Null.Or.Empty);
        }

        [Test]
        public void EmptyCatalog_FailsRequiredMvpIds_AndBlocksBootstrap()
        {
            // 빈 등록은 Bootstrap을 통과시키면 안 된다(필수 광물·시설·업그레이드 ID 누락).
            var catalog = ScriptableObject.CreateInstance<GameDataCatalog>();
            Track(catalog);
            catalog.EditorSetLists(
                new List<MineralData>(),
                new List<BuildingData>(),
                new List<RecipeData>(),
                new List<UpgradeData>(),
                new List<DialogueTemplateData>());

            var result = catalog.ValidateAll();
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorCount, Is.GreaterThanOrEqualTo(3));
            Assert.That(
                result.Issues.Any(i => i.FieldName == "requiredIds" && i.Message.Contains(DataIds.Minerals.Copper)),
                Is.True);
            Assert.That(
                result.Issues.Any(i => i.FieldName == "requiredIds" && i.Message.Contains(DataIds.Recipes.SupportBasic)),
                Is.True);
            Assert.That(
                result.Issues.Any(i =>
                    i.FieldName == "requiredIds" && i.Message.Contains(DataIds.Dialogue.LowPowerWarning)),
                Is.True);

            IDataCatalogPort port = catalog;
            Assert.That(port.Validate(out var reason), Is.False);
            Assert.That(reason, Is.Not.Null.And.Not.Empty);

            var scenes = new FakeScenes();
            var bootstrapper = new GameObject("BootEmptyCatalog").AddComponent<GameBootstrapper>();
            Track(bootstrapper.gameObject);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Catalog validation failed"));
            Assert.That(bootstrapper.Initialize(catalog, new EmptySave(), scenes), Is.False);
            Assert.That(bootstrapper.InitializationFailed, Is.True);
            Assert.That(scenes.LoadCallCount, Is.Zero);
        }

        [Test]
        public void IDataCatalogPort_Validate_InvalidCatalog_ReturnsFalseWithReason()
        {
            var building = CreateBuilding(
                "BrokenBuilding",
                DataIds.Buildings.LightBasic,
                "Light",
                prefab: null,
                power: 1,
                costs: new List<ItemCostEntry>());
            var catalog = ScriptableObject.CreateInstance<GameDataCatalog>();
            Track(catalog);
            catalog.EditorSetLists(
                new List<MineralData>(),
                new List<BuildingData> { building },
                new List<RecipeData>(),
                new List<UpgradeData>(),
                new List<DialogueTemplateData>());

            IDataCatalogPort port = catalog;
            Assert.That(port.Validate(out var reason), Is.False);
            Assert.That(reason, Is.Not.Null.And.Not.Empty);
            Assert.That(reason, Does.Contain("catalog validation failed").IgnoreCase);
        }

        [Test]
        public void Bootstrap_WithValidRealCatalog_ReachesMainMenuPath()
        {
            var catalog = CreateValidMvpCatalog();
            var scenes = new FakeScenes();
            var bootstrapper = new GameObject("BootCatalogOk").AddComponent<GameBootstrapper>();
            Track(bootstrapper.gameObject);

            Assert.That(bootstrapper.Initialize(catalog, new EmptySave(), scenes), Is.True);
            Assert.That(bootstrapper.InitializationFailed, Is.False);
            Assert.That(bootstrapper.State, Is.Not.Null);
            Assert.That(GameState.IsComplete(bootstrapper.State), Is.True);
            Assert.That(scenes.LoadedName, Is.EqualTo(SceneNames.MainMenu));
        }

        [Test]
        public void Bootstrap_WithInvalidRealCatalog_BlocksMainMenuAndExposesReason()
        {
            var building = CreateBuilding(
                "BootBroken",
                DataIds.Buildings.ChargerBasic,
                "Charger",
                prefab: null,
                power: 2,
                costs: new List<ItemCostEntry>());
            var catalog = ScriptableObject.CreateInstance<GameDataCatalog>();
            Track(catalog);
            catalog.EditorSetLists(
                new List<MineralData>(),
                new List<BuildingData> { building },
                new List<RecipeData>(),
                new List<UpgradeData>(),
                new List<DialogueTemplateData>());

            var scenes = new FakeScenes();
            var bootstrapper = new GameObject("BootCatalogBad").AddComponent<GameBootstrapper>();
            Track(bootstrapper.gameObject);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Catalog validation failed"));
            Assert.That(bootstrapper.Initialize(catalog, new EmptySave(), scenes), Is.False);
            Assert.That(bootstrapper.InitializationFailed, Is.True);
            Assert.That(scenes.LoadCallCount, Is.Zero);
            Assert.That(scenes.LoadedName, Is.Null);
        }

        [Test]
        public void StaticDefinitions_HaveNoPlayerStateFields()
        {
            // B-S03: 보유량·골드·현재 레벨 필드가 정적 SO에 없어야 한다.
            AssertNoField(typeof(MineralData), "quantity", "amount", "gold", "inventory", "currentLevel");
            AssertNoField(typeof(BuildingData), "quantity", "amount", "gold", "isPowered", "currentLevel");
            AssertNoField(typeof(UpgradeData), "currentLevel", "ownedLevel", "gold");
            AssertNoField(typeof(RecipeData), "quantity", "gold");
            AssertNoField(typeof(DialogueTemplateData), "cooldownRemaining", "gold");
        }

        private static void AssertNoField(System.Type type, params string[] forbidden)
        {
            var names = type
                .GetFields(System.Reflection.BindingFlags.Instance |
                           System.Reflection.BindingFlags.Public |
                           System.Reflection.BindingFlags.NonPublic)
                .Select(f => f.Name.ToLowerInvariant())
                .ToList();
            foreach (var bad in forbidden)
            {
                Assert.That(names.Any(n => n.Contains(bad.ToLowerInvariant())), Is.False,
                    $"{type.Name} should not contain player-state field matching '{bad}'.");
            }
        }

        private GameDataCatalog CreateValidMvpCatalog()
        {
            var prefab = Track(new GameObject("BuildingPrefabPlaceholder"));

            var minerals = new List<MineralData>
            {
                CreateMineral("Mineral_Copper", DataIds.Minerals.Copper, "Copper", 1.5f, 10),
                CreateMineral("Mineral_Iron", DataIds.Minerals.Iron, "Iron", 2f, 15),
                CreateMineral("Mineral_Lithium", DataIds.Minerals.Lithium, "Lithium", 0.8f, 40)
            };

            var buildings = new List<BuildingData>
            {
                CreateBuilding("Bld_Support", DataIds.Buildings.SupportBasic, "Basic Support", prefab, 0,
                    new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Copper, 2) }),
                CreateBuilding("Bld_Light", DataIds.Buildings.LightBasic, "Basic Light", prefab, 1,
                    new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Iron, 1) }),
                CreateBuilding("Bld_Charger", DataIds.Buildings.ChargerBasic, "Basic Charger", prefab, 3,
                    new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Copper, 3) }),
                CreateBuilding("Bld_Storage", DataIds.Buildings.StorageBasic, "Basic Storage", prefab, 0,
                    new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Iron, 2) }),
                CreateBuilding("Bld_Settlement", DataIds.Buildings.SettlementBasic, "Settlement Console", prefab, 1,
                    new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Lithium, 1) }),
                CreateBuilding("Bld_Outpost", DataIds.Buildings.OutpostCoreBasic, "Outpost Core", prefab, 5,
                    new List<ItemCostEntry>
                    {
                        new ItemCostEntry(DataIds.Minerals.Copper, 5),
                        new ItemCostEntry(DataIds.Minerals.Iron, 5)
                    })
            };

            var recipes = new List<RecipeData>
            {
                CreateRecipe(
                    "Recipe_Support",
                    DataIds.Recipes.SupportBasic,
                    "Craft Basic Support",
                    DataIds.Buildings.SupportBasic,
                    new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Copper, 2) },
                    new List<ItemCostEntry> { new ItemCostEntry(DataIds.Buildings.SupportBasic, 1) })
            };

            var upgrades = new List<UpgradeData>
            {
                CreateUpgrade("Upg_DrillSpeed", DataIds.Upgrades.DrillSpeed, "Drill Speed", 3, 0.1f),
                CreateUpgrade("Upg_DrillEff", DataIds.Upgrades.DrillEfficiency, "Drill Efficiency", 3, 0.05f),
                CreateUpgrade("Upg_DroneScan", DataIds.Upgrades.DroneScan, "Drone Scan", 2, 1f),
                CreateUpgrade("Upg_DroneRescue", DataIds.Upgrades.DroneRescue, "Drone Rescue", 2, 1f)
            };

            var dialogues = new List<DialogueTemplateData>
            {
                CreateDialogue(
                    "Dlg_LowPower",
                    DataIds.Dialogue.LowPowerWarning,
                    "Low Power Warning",
                    "low_power",
                    100,
                    "Power is low. Return or recharge.")
            };

            var catalog = ScriptableObject.CreateInstance<GameDataCatalog>();
            Track(catalog);
            catalog.EditorSetLists(minerals, buildings, recipes, upgrades, dialogues);

            var validation = catalog.ValidateAll();
            Assert.That(validation.IsValid, Is.True, validation.FormatAll());
            return catalog;
        }

        private MineralData CreateMineral(string name, string id, string display, float weight, int price)
        {
            var data = ScriptableObject.CreateInstance<MineralData>();
            data.name = name;
            data.EditorSet(id, display, weight, price, CreateTestIcon());
            return Track(data);
        }

        private BuildingData CreateBuilding(
            string name,
            string id,
            string display,
            GameObject prefab,
            int power,
            List<ItemCostEntry> costs)
        {
            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.name = name;
            data.EditorSet(id, display, prefab, CreateTestIcon(), power, costs);
            return Track(data);
        }

        private Sprite CreateTestIcon()
        {
            var icon = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));
            return Track(icon);
        }

        private RecipeData CreateRecipe(
            string name,
            string id,
            string display,
            string buildingId,
            List<ItemCostEntry> inputs,
            List<ItemCostEntry> outputs)
        {
            var data = ScriptableObject.CreateInstance<RecipeData>();
            data.name = name;
            data.EditorSet(id, display, buildingId, inputs, outputs);
            return Track(data);
        }

        private UpgradeData CreateUpgrade(string name, string id, string display, int maxLevel, float effect)
        {
            var levels = new List<UpgradeLevelDefinition>();
            for (var i = 1; i <= maxLevel; i++)
            {
                levels.Add(new UpgradeLevelDefinition(
                    i,
                    effect * i,
                    new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Copper, i) }));
            }

            var data = ScriptableObject.CreateInstance<UpgradeData>();
            data.name = name;
            data.EditorSet(id, display, maxLevel, levels);
            return Track(data);
        }

        private DialogueTemplateData CreateDialogue(
            string name,
            string id,
            string display,
            string situation,
            int priority,
            string template)
        {
            var data = ScriptableObject.CreateInstance<DialogueTemplateData>();
            data.name = name;
            data.EditorSet(id, display, situation, priority, template);
            return Track(data);
        }

        private T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        private sealed class FakeScenes : ISceneLoader
        {
            public string LoadedName;
            public int LoadCallCount;

            public bool Load(string sceneName)
            {
                LoadCallCount++;
                LoadedName = sceneName;
                return true;
            }
        }
    }
}
