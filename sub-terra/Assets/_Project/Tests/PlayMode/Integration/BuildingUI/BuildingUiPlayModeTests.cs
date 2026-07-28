using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.State;
using SubTerra.Gameplay.Building;
using SubTerra.Shared;
using UnityEngine;
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
            Assert.That(state.InteractionPrompt, Does.Contain("장비 충전"));
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
    }
}
