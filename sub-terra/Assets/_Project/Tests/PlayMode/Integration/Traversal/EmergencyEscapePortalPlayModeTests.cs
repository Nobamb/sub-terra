using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.State;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.Traversal
{
    public sealed class EmergencyEscapePortalPlayModeTests
    {
        [UnityTest]
        public IEnumerator PromptB46_Portal_RequiresRiderAnd30PoweredDemandBeforeRequest()
        {
            var networkObject = new GameObject("PromptB46_PowerNetwork");
            var network = networkObject.AddComponent<PowerNetworkSystem>();
            var sourceObject = new GameObject("PromptB46_PowerSource");
            var source = sourceObject.AddComponent<PowerNode>();
            source.Configure(network, true, 50, 0, PowerPriority.Critical);

            var portalObject = new GameObject("PromptB46_Portal");
            portalObject.AddComponent<BoxCollider2D>().isTrigger = true;
            var portalPower = portalObject.AddComponent<PowerNode>();
            portalPower.Configure(network, false, 0, 30, PowerPriority.Critical);
            var portal = portalObject.AddComponent<EmergencyEscapePortal>();
            var cableObject = new GameObject("PromptB46_Cable");
            var cable = cableObject.AddComponent<PowerCable>();
            cable.Configure(network, source, portalPower);
            network.RegisterCable(cable);
            network.Rebuild();

            var player = new GameObject("PromptB46_Rider");
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var movement = player.AddComponent<PlayerMovement>();
            var portObject = new GameObject("PromptB46_FakePort");
            var fakePort = portObject.AddComponent<FakeEscapePort>();
            SetField(portal, "rider", movement);
            SetField(portal, "escapePort", fakePort);

            Assert.That(portalPower.Demand, Is.EqualTo(30));
            Assert.That(portal.IsPowered, Is.True);
            Assert.That(portal.RequestEscape(), Is.True);
            Assert.That(fakePort.RequestCount, Is.EqualTo(1));
            yield return null;

            portalPower.SetDemand(60);
            Assert.That(portal.IsPowered, Is.False);
            Assert.That(portal.RequestEscape(), Is.False);
            Assert.That(fakePort.RequestCount, Is.EqualTo(1));

            Object.Destroy(portObject);
            Object.Destroy(player);
            Object.Destroy(cableObject);
            Object.Destroy(portalObject);
            Object.Destroy(sourceObject);
            Object.Destroy(networkObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PromptB46_Escape_PrefersLatestOutpostAndFallsBackToElevator()
        {
            var state = GameState.CreateNew();
            state.SetGold(300);
            var player = new GameObject("PromptB46_Player");
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var elevator = new GameObject("PromptB46_ElevatorCenter");
            elevator.transform.position = new Vector3(5f, -2f, 0f);
            var host = new GameObject("PromptB46_Bridge");
            var bridge = host.AddComponent<EmergencyEscapePortalRuntimeBridge>();
            bridge.Bind(state, player.transform, elevator.transform);

            Assert.That(bridge.TryEscape(out var firstDestination, out _), Is.True);
            Assert.That(firstDestination, Is.EqualTo(EmergencyEscapeDestination.Elevator));
            Assert.That(player.transform.position, Is.EqualTo(elevator.transform.position));
            Assert.That(state.Player.Gold, Is.EqualTo(200));
            Assert.That(state.Player.Energy, Is.EqualTo(90));

            var outpost = new GameObject("PromptB46_Outpost");
            outpost.transform.position = new Vector3(12f, -10f, 0f);
            outpost.AddComponent<BuildingInstance>().Initialize(
                "building.outpost_core.basic-0002",
                DataIds.Buildings.OutpostCoreBasic);

            Assert.That(bridge.TryEscape(out var secondDestination, out _), Is.True);
            Assert.That(secondDestination, Is.EqualTo(EmergencyEscapeDestination.OutpostCore));
            Assert.That(player.transform.position,
                Is.EqualTo(outpost.transform.position + Vector3.up));
            Assert.That(state.Player.Gold, Is.EqualTo(100));
            Assert.That(state.Player.Energy, Is.EqualTo(80));

            Object.Destroy(host);
            Object.Destroy(outpost);
            Object.Destroy(elevator);
            Object.Destroy(player);
            yield return null;
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }

        private sealed class FakeEscapePort : MonoBehaviour, IEmergencyEscapePortalPort
        {
            public int RequestCount { get; private set; }

            public bool TryEscape(
                out EmergencyEscapeDestination destination,
                out string reason)
            {
                RequestCount++;
                destination = EmergencyEscapeDestination.Elevator;
                reason = string.Empty;
                return true;
            }
        }
    }
}
