using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.RunFailure;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.Integration
{
    public sealed class PhaseLRunFailureStaticTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        private sealed class UpgradeCatalog : IUpgradeCatalog
        {
            private readonly UpgradeData upgrade;

            public UpgradeCatalog(UpgradeData upgrade)
            {
                this.upgrade = upgrade;
            }

            public IReadOnlyList<UpgradeData> Upgrades => new[] { upgrade };

            public bool TryGetUpgrade(string upgradeId, out UpgradeData data)
            {
                data = upgrade != null && upgrade.Id == upgradeId ? upgrade : null;
                return data != null;
            }
        }

        private sealed class AffordableWallet : IResourceWallet
        {
            public bool CanAfford(IReadOnlyList<ItemCostDto> costs)
            {
                return true;
            }

            public bool TrySpend(IReadOnlyList<ItemCostDto> costs)
            {
                return true;
            }
        }

        [Test]
        public void PromptB84_MaximumHealthPurchase_AppliesImmediately()
        {
            if (SaveRuntimeController.Instance != null)
            {
                Object.DestroyImmediate(SaveRuntimeController.Instance.gameObject);
            }

            var state = GameState.CreateNew();
            var runtimeObject = new GameObject("PromptB84_Save");
            var runtime = runtimeObject.AddComponent<SaveRuntimeController>();
            var inventoryState = new InventoryState(100f);
            var inventory = new InventoryService(
                new InMemoryMineralCatalog(),
                inventoryState,
                state);
            var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
            upgrade.EditorSet(
                DataIds.Upgrades.MaximumHealth,
                "최대 체력",
                1,
                new List<UpgradeLevelDefinition>
                {
                    new UpgradeLevelDefinition(
                        1,
                        50f,
                        new List<ItemCostEntry>
                        {
                            new ItemCostEntry(DataIds.Minerals.Copper, 1)
                        })
                });
            var progression = new ProgressionService(
                new UpgradeState(),
                new UpgradeCatalog(upgrade),
                new AffordableWallet());
            SetField(runtime, "inventory", inventoryState);
            SetField(runtime, "inventoryService", inventory);
            SetField(runtime, "progression", progression);

            var player = new GameObject("PromptB84_Player");
            var settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            var host = new GameObject("PromptB84_RunFailure");
            var survival = host.AddComponent<PlayerSurvivalController>();
            survival.Configure(settings, player.transform);
            var controller = host.AddComponent<RunFailureRuntimeController>();
            SetField(controller, "survivalController", survival);
            SetField(controller, "playerTransform", player.transform);
            controller.Bind(runtime, state);

            var healthEvents = 0;
            survival.HealthChanged += _ => healthEvents++;
            var result = progression.TryPurchase(DataIds.Upgrades.MaximumHealth);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(survival.State.MaximumHealth, Is.EqualTo(150));
            Assert.That(survival.State.Health, Is.EqualTo(150f));
            Assert.That(healthEvents, Is.EqualTo(1));

            controller.Unbind();
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(upgrade);
            Object.DestroyImmediate(runtimeObject);
        }

        [Test]
        public void L_S03_IntegrationScene_WiresSurvivalFailureUiAndAllGameplayInput()
        {
            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                var roots = scene.GetRootGameObjects();
                var controller = Find<RunFailureRuntimeController>(roots);
                var survival = Find<PlayerSurvivalController>(roots);
                var view = Find<RunFailurePanelView>(roots);
                var binder = Find<IntegrationRuntimeBinder>(roots);
                Assert.That(controller, Is.Not.Null);
                Assert.That(survival, Is.Not.Null);
                Assert.That(view, Is.Not.Null);
                Assert.That(view.HasRequiredReferences(), Is.True);

                var controllerObject = new SerializedObject(controller);
                Assert.That(controllerObject.FindProperty("survivalController").objectReferenceValue,
                    Is.SameAs(survival));
                Assert.That(controllerObject.FindProperty("failureView").objectReferenceValue,
                    Is.SameAs(view));
                Assert.That(controllerObject.FindProperty("playerMovement").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(controllerObject.FindProperty("localSurfaceFallback").objectReferenceValue,
                    Is.Not.Null);

                var inputs = controllerObject.FindProperty("gameplayInputBehaviours");
                Assert.That(Contains<PlayerController>(inputs), Is.True, "movement input must lock");
                Assert.That(Contains<PlayerMiningController>(inputs), Is.True, "mining input must lock");
                Assert.That(
                    Contains<BuildingPlacementInput>(inputs)
                    || Contains<GameplayBuildingPlacementBridge>(inputs),
                    Is.True,
                    "building input must lock");

                var blocker = view.GetComponent<UnityEngine.UI.Image>();
                var blockerRect = view.GetComponent<RectTransform>();
                Assert.That(blocker, Is.Not.Null);
                Assert.That(blocker.raycastTarget, Is.True, "failure overlay must block HUD clicks");
                Assert.That(blockerRect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(blockerRect.anchorMax, Is.EqualTo(Vector2.one));

                var binderObject = new SerializedObject(binder);
                Assert.That(binderObject.FindProperty("runFailureController").objectReferenceValue,
                    Is.SameAs(controller));
            }
            finally
            {
                if (!string.IsNullOrEmpty(previous))
                {
                    EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
                }
            }
        }

        private static T Find<T>(GameObject[] roots) where T : Component
        {
            return roots.SelectMany(root => root.GetComponentsInChildren<T>(true)).FirstOrDefault();
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }

        private static bool Contains<T>(SerializedProperty array) where T : Behaviour
        {
            for (var i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue is T)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
