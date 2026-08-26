using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.State;
using SubTerra.Gameplay.Building;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.BuildingUI
{
    public sealed class BuildingUiPlayModeTests
    {
        [UnityTest]
        public IEnumerator G_F01_GameplayBridgeStartsAndCancelsActualASelection()
        {
            var host = new GameObject("PhaseGPlacementBridge");
            host.SetActive(false);
            var placement = host.AddComponent<BuildingPlacementSystem>();
            var sceneReferences = host.AddComponent<BuildingPlacementSceneReferences>();
            var bridge = host.AddComponent<GameplayBuildingPlacementBridge>();

            var previewObject = new GameObject("Preview");
            previewObject.transform.SetParent(host.transform);
            previewObject.AddComponent<SpriteRenderer>();
            var preview = previewObject.AddComponent<BuildingPlacementPreview>();

            var runtimePrefab = new GameObject("RuntimeBuildingPrefab");
            var definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
            SetField(definition, "buildingId", "building.support.basic");
            SetField(definition, "runtimePrefab", runtimePrefab);
            SetField(placement, "resourceWalletBehaviour", bridge);

            var binding = new BuildingPlacementBinding();
            SetField(binding, "buildingId", "building.support.basic");
            SetField(binding, "definition", definition);
            SetField(bridge, "placementSystem", placement);
            SetField(bridge, "preview", preview);
            SetField(bridge, "sceneReferences", sceneReferences);
            SetField(bridge, "bindings", new[] { binding });

            var events = new List<BuildingPlacementState>();
            bridge.PlacementChanged += result => events.Add(result.state);
            host.SetActive(true);
            yield return null;

            Assert.That(bridge.BeginPreview("building.support.basic"), Is.True);
            Assert.That(placement.Selection, Is.SameAs(definition));
            bridge.CancelPreview();

            Assert.That(placement.Selection, Is.Null);
            Assert.That(preview.gameObject.activeSelf, Is.False);
            Assert.That(events, Does.Contain(BuildingPlacementState.Previewing));
            Assert.That(events, Does.Contain(BuildingPlacementState.Cancelled));

            UnityEngine.Object.Destroy(host);
            UnityEngine.Object.Destroy(runtimePrefab);
            UnityEngine.Object.Destroy(definition);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PromptB71_CPlacesBuildingWhileEnterDoesNot()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var host = new GameObject("PromptB71PlacementBridge");
            host.SetActive(false);
            var placement = host.AddComponent<BuildingPlacementSystem>();
            var bridge = host.AddComponent<GameplayBuildingPlacementBridge>();
            var buildingRoot = new GameObject("RuntimeBuildings");
            buildingRoot.transform.SetParent(host.transform);
            var runtimePrefab = new GameObject("RuntimeBuildingPrefab");
            runtimePrefab.SetActive(false);
            var definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
            var wallet = new RecordingWallet();

            try
            {
                SetField(definition, "buildingId", "building.support.basic");
                SetField(definition, "runtimePrefab", runtimePrefab);
                SetField(definition, "requiresGround", false);
                SetField(placement, "buildingRoot", buildingRoot.transform);
                placement.SetResourceWallet(wallet);

                var binding = new BuildingPlacementBinding();
                SetField(binding, "buildingId", "building.support.basic");
                SetField(binding, "definition", definition);
                SetField(bridge, "placementSystem", placement);
                SetField(bridge, "bindings", new[] { binding });

                host.SetActive(true);
                yield return null;
                Assert.That(bridge.BeginPreview("building.support.basic"), Is.True);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                InputSystem.Update();
                InvokePrivate(bridge, "Update");

                Assert.That(placement.Selection, Is.SameAs(definition));
                Assert.That(wallet.SpendCount, Is.EqualTo(0));
                Assert.That(buildingRoot.transform.childCount, Is.EqualTo(0));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.C));
                InputSystem.Update();
                InvokePrivate(bridge, "Update");

                Assert.That(placement.Selection, Is.Null);
                Assert.That(wallet.SpendCount, Is.EqualTo(1));
                Assert.That(buildingRoot.transform.childCount, Is.EqualTo(1));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                UnityEngine.Object.Destroy(host);
                UnityEngine.Object.Destroy(runtimePrefab);
                UnityEngine.Object.Destroy(definition);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator G_F05_OutpostEventUsesActualPowerAndInteractionStatus()
        {
            var host = new GameObject("PhaseGHazardBridge");
            var bridge = host.AddComponent<GameplayHazardStatusBridge>();
            var state = GameState.CreateNew();
            bridge.BindGameState(state);

            bridge.Publish(new GameplayEventDto
            {
                type = GameplayEventType.OutpostStatusChanged,
                outpostStatus = new OutpostStatusDto
                {
                    isActive = true,
                    isInInteractionRange = true,
                    totalPowerSupply = 7.5f,
                    totalPowerConsumption = 3.25f,
                    connectedFacilities = new List<ConnectedFacilityStatusDto>
                    {
                        new()
                        {
                            buildingId = "building.charger.basic",
                            isActive = true
                        }
                    }
                }
            });

            Assert.That(bridge.PowerStatus.IsConnected, Is.True);
            Assert.That(bridge.PowerStatus.Supply, Is.EqualTo(7.5f));
            Assert.That(bridge.PowerStatus.Demand, Is.EqualTo(3.25f));
            Assert.That(state.InteractionPrompt, Is.Empty);
            UnityEngine.Object.Destroy(host);
            yield return null;
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            var methodInfo = target.GetType().GetMethod(
                method,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(methodInfo, Is.Not.Null, "Missing method: " + method);
            methodInfo.Invoke(target, null);
        }

        private sealed class RecordingWallet : IResourceWallet
        {
            public int SpendCount { get; private set; }

            public bool CanAfford(IReadOnlyList<ItemCostDto> costs) => true;

            public bool TrySpend(IReadOnlyList<ItemCostDto> costs)
            {
                SpendCount++;
                return true;
            }
        }
    }
}
