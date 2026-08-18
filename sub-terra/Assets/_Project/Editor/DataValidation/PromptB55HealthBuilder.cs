using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Player;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>prompt-B 55의 체력 데이터와 Basic HUD만 갱신하는 범위 제한 빌더.</summary>
    public static class PromptB55HealthBuilder
    {
        private const string FlagPath = "Temp/subterra-build-promptb55-health.flag";
        private const string DonePath = "Temp/subterra-build-promptb55-health.done";
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";
        private const string UpgradeFolder = "Assets/_Project/Data/Upgrades";
        private const string BasicHudPath = "Assets/_Project/Prefabs/UI/BasicHUD.prefab";
        private const string SurvivalSettingsPath =
            "Assets/_Project/Data/Player/PlayerSurvivalSettings.asset";

        [InitializeOnLoadMethod]
        private static void WatchFlag()
        {
            EditorApplication.update += PollFlag;
        }

        [MenuItem("SubTerra/UI/Build Prompt-B 55 Health")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + BuildAll());
        }

        public static string BuildAll()
        {
            var maximumHealth = EnsureUpgrade(
                "Upgrade_Maximum_Health.asset",
                DataIds.Upgrades.MaximumHealth,
                "최대 체력",
                new[] { 30f, 60f, 100f });
            var regeneration = EnsureUpgrade(
                "Upgrade_Health_Regeneration.asset",
                DataIds.Upgrades.HealthRegeneration,
                "초당 체력 재생",
                new[] { 0.3f, 0.6f, 1f });
            RegisterUpgrades(maximumHealth, regeneration);
            ConfigureSurvivalSettings();
            BuildBasicHud();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Prompt-B 55 health data and BasicHUD built";
        }

        private static void PollFlag()
        {
            if (!File.Exists(FlagPath))
            {
                return;
            }

            try
            {
                File.Delete(FlagPath);
                var result = BuildAll();
                File.WriteAllText(DonePath, result);
                Debug.Log("[SubTerra] " + result);
            }
            catch (Exception exception)
            {
                File.WriteAllText(DonePath, "FAIL: " + exception);
                Debug.LogError("[SubTerra] Prompt-B 55 build failed: " + exception);
            }
        }

        private static UpgradeData EnsureUpgrade(
            string fileName,
            string id,
            string displayName,
            IReadOnlyList<float> effects)
        {
            var path = UpgradeFolder + "/" + fileName;
            var asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<UpgradeData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var levels = new List<UpgradeLevelDefinition>
            {
                new UpgradeLevelDefinition(
                    1,
                    effects[0],
                    new List<ItemCostEntry>
                    {
                        new ItemCostEntry(DataIds.Minerals.Copper, 1)
                    }),
                new UpgradeLevelDefinition(
                    2,
                    effects[1],
                    new List<ItemCostEntry>
                    {
                        new ItemCostEntry(DataIds.Minerals.Copper, 2),
                        new ItemCostEntry(DataIds.Minerals.Iron, 1)
                    }),
                new UpgradeLevelDefinition(
                    3,
                    effects[2],
                    new List<ItemCostEntry>
                    {
                        new ItemCostEntry(DataIds.Minerals.Copper, 3),
                        new ItemCostEntry(DataIds.Minerals.Iron, 2)
                    })
            };
            asset.EditorSet(id, displayName, 3, levels);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void RegisterUpgrades(params UpgradeData[] additions)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException("GameDataCatalog is missing.");
            }

            var upgrades = catalog.Upgrades.Where(item => item != null).ToList();
            for (var i = 0; i < additions.Length; i++)
            {
                upgrades.RemoveAll(item => item.Id == additions[i].Id);
                upgrades.Add(additions[i]);
            }

            catalog.EditorSetLists(
                catalog.Minerals.ToList(),
                catalog.MiningTiles.ToList(),
                catalog.Buildings.ToList(),
                catalog.Recipes.ToList(),
                upgrades,
                catalog.Dialogues.ToList());
            EditorUtility.SetDirty(catalog);
        }

        private static void ConfigureSurvivalSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PlayerSurvivalSettings>(SurvivalSettingsPath);
            if (settings == null)
            {
                throw new InvalidOperationException("PlayerSurvivalSettings is missing.");
            }

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("minimumFallDamageHeight").floatValue = 10f;
            serialized.FindProperty("fallDamageAtThreshold").intValue = 10;
            serialized.FindProperty("fallDamagePerAdditionalMeter").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static void BuildBasicHud()
        {
            var root = PrefabUtility.LoadPrefabContents(BasicHudPath);
            try
            {
                var view = root.GetComponent<BasicHudView>();
                if (view == null || view.EnergyText == null)
                {
                    throw new InvalidOperationException("BasicHUD energy binding is missing.");
                }

                var health = root.GetComponentsInChildren<TextMeshProUGUI>(true)
                    .FirstOrDefault(item => item.name == "HealthText");
                if (health == null)
                {
                    var clone = UnityEngine.Object.Instantiate(
                        view.EnergyText.gameObject,
                        view.EnergyText.transform.parent);
                    clone.name = "HealthText";
                    health = clone.GetComponent<TextMeshProUGUI>();

                    foreach (Transform child in root.transform)
                    {
                        if (child == health.transform || child is not RectTransform rect)
                        {
                            continue;
                        }

                        rect.anchoredPosition += Vector2.down * 30f;
                    }

                    var rootRect = root.GetComponent<RectTransform>();
                    rootRect.sizeDelta += Vector2.up * 30f;
                }

                var healthRect = health.rectTransform;
                var energyRect = view.EnergyText.rectTransform;
                healthRect.anchorMin = energyRect.anchorMin;
                healthRect.anchorMax = energyRect.anchorMax;
                healthRect.pivot = energyRect.pivot;
                healthRect.sizeDelta = energyRect.sizeDelta;
                healthRect.anchoredPosition = energyRect.anchoredPosition + Vector2.up * 30f;
                health.text = "체력 100 / 100";

                var serialized = new SerializedObject(view);
                serialized.FindProperty("healthText").objectReferenceValue = health;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);
                PrefabUtility.SaveAsPrefabAsset(root, BasicHudPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
