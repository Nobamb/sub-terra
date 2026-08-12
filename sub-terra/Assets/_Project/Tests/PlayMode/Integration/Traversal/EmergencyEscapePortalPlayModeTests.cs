using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.State;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SubTerra.App.Tests.PlayMode.Traversal
{
    public sealed class EmergencyEscapePortalPlayModeTests
    {
        [UnityTest]
        public IEnumerator PromptB47_2_Portal_OpensPanelWhenNetworkBoundEvenIfCapacityUnpowered()
        {
            var networkObject = new GameObject("PromptB47_2_PowerNetwork");
            var network = networkObject.AddComponent<PowerNetworkSystem>();
            // 전진기지급 공급 5만 두고 케이블 없이 수요 30 포탈을 등록한다.
            var sourceObject = new GameObject("PromptB47_2_Source");
            sourceObject.AddComponent<PowerNode>().Configure(
                network, true, 5, 0, PowerPriority.Critical);

            var portalObject = new GameObject("PromptB47_2_Portal");
            portalObject.AddComponent<BoxCollider2D>().isTrigger = true;
            var portalPower = portalObject.AddComponent<PowerNode>();
            portalPower.Configure(network, false, 0, 30, PowerPriority.Critical);
            var portal = portalObject.AddComponent<EmergencyEscapePortal>();
            network.Rebuild();

            // 용량 기반 PowerNode.IsPowered 는 false여야 한다(공급 5 < 수요 30 + 미연결).
            Assert.That(portalPower.IsPowered, Is.False);

            var player = new GameObject("PromptB47_2_Rider");
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var movement = player.AddComponent<PlayerMovement>();
            var portObject = new GameObject("PromptB47_2_FakePort");
            var fakePort = portObject.AddComponent<FakeEscapePort>();
            SetField(portal, "rider", movement);
            SetField(portal, "escapePort", fakePort);

            Assert.That(portalPower.Demand, Is.EqualTo(30));
            Assert.That(portal.IsPowered, Is.True, "망 등록만으로 사용 가능으로 본다.");
            Assert.That(portal.RequestEscape(), Is.True);
            Assert.That(fakePort.OpenCount, Is.EqualTo(1));
            yield return null;

            portalPower.SetNetwork(null);
            Assert.That(portal.IsPowered, Is.False);
            Assert.That(portal.RequestEscape(), Is.False);
            Assert.That(fakePort.OpenCount, Is.EqualTo(1));

            Object.Destroy(portObject);
            Object.Destroy(player);
            Object.Destroy(portalObject);
            Object.Destroy(sourceObject);
            Object.Destroy(networkObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PromptB47_1_Portal_RequiresRiderAndPowerNetworkBeforeOpenPanel()
        {
            var networkObject = new GameObject("PromptB47_PowerNetwork");
            var network = networkObject.AddComponent<PowerNetworkSystem>();
            var sourceObject = new GameObject("PromptB47_PowerSource");
            var source = sourceObject.AddComponent<PowerNode>();
            source.Configure(network, true, 50, 0, PowerPriority.Critical);

            var portalObject = new GameObject("PromptB47_Portal");
            portalObject.AddComponent<BoxCollider2D>().isTrigger = true;
            var portalPower = portalObject.AddComponent<PowerNode>();
            portalPower.Configure(network, false, 0, 30, PowerPriority.Critical);
            var portal = portalObject.AddComponent<EmergencyEscapePortal>();
            var cableObject = new GameObject("PromptB47_Cable");
            var cable = cableObject.AddComponent<PowerCable>();
            cable.Configure(network, source, portalPower);
            network.RegisterCable(cable);
            network.Rebuild();

            var player = new GameObject("PromptB47_Rider");
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var movement = player.AddComponent<PlayerMovement>();
            var portObject = new GameObject("PromptB47_FakePort");
            var fakePort = portObject.AddComponent<FakeEscapePort>();
            SetField(portal, "rider", movement);
            SetField(portal, "escapePort", fakePort);

            Assert.That(portalPower.Demand, Is.EqualTo(30));
            Assert.That(portal.IsPowered, Is.True);
            Assert.That(portal.RequestEscape(), Is.True);
            Assert.That(fakePort.OpenCount, Is.EqualTo(1));
            yield return null;

            portalPower.SetNetwork(null);
            Assert.That(portal.IsPowered, Is.False);
            Assert.That(portal.RequestEscape(), Is.False);
            Assert.That(fakePort.OpenCount, Is.EqualTo(1));

            Object.Destroy(portObject);
            Object.Destroy(player);
            Object.Destroy(cableObject);
            Object.Destroy(portalObject);
            Object.Destroy(sourceObject);
            Object.Destroy(networkObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PromptB47_1_EscapeTo_SelectedDestination_PaysOnceAndTeleports()
        {
            var state = GameState.CreateNew();
            state.SetGold(300);
            var player = new GameObject("PromptB47_Player");
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var elevator = new GameObject("PromptB47_ElevatorCenter");
            elevator.transform.position = new Vector3(5f, -2f, 0f);
            var host = new GameObject("PromptB47_Bridge");
            var bridge = host.AddComponent<EmergencyEscapePortalRuntimeBridge>();
            bridge.Bind(state, player.transform, elevator.transform);

            var panelHost = new GameObject("PromptB47_Panel", typeof(RectTransform));
            var view = panelHost.AddComponent<EmergencyEscapePanelView>();
            var binder = panelHost.AddComponent<EmergencyEscapePanelBinder>();
            var confirm = panelHost.AddComponent<Button>();
            var close = new GameObject("Close").AddComponent<Button>();
            SetField(view, "panelRoot", panelHost);
            SetField(binder, "view", view);
            SetField(binder, "confirmButton", confirm);
            SetField(binder, "closeButton", close);
            bridge.BindPanel(binder);

            var options = bridge.GetDestinationOptions();
            Assert.That(options.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(options[0].Kind, Is.EqualTo(EmergencyEscapeDestination.Elevator));
            Assert.That(options[0].DisplayName, Is.EqualTo("엘리베이터"));

            Assert.That(bridge.TryOpenEscapePanel(out _), Is.True);
            Assert.That(
                bridge.TryEscapeTo(EmergencyEscapeDestination.Elevator, string.Empty, out _),
                Is.True);
            Assert.That(player.transform.position, Is.EqualTo(elevator.transform.position));
            Assert.That(state.Player.Gold, Is.EqualTo(200));
            Assert.That(state.Player.Energy, Is.EqualTo(90));

            var outpost = new GameObject("PromptB47_Outpost");
            outpost.transform.position = new Vector3(12f, -10f, 0f);
            outpost.AddComponent<BuildingInstance>().Initialize(
                "building.outpost_core.basic-0002",
                DataIds.Buildings.OutpostCoreBasic);

            options = bridge.GetDestinationOptions();
            Assert.That(options.Count, Is.EqualTo(2));
            Assert.That(options[1].Kind, Is.EqualTo(EmergencyEscapeDestination.OutpostCore));
            Assert.That(
                bridge.TryEscapeTo(
                    EmergencyEscapeDestination.OutpostCore,
                    "building.outpost_core.basic-0002",
                    out _),
                Is.True);
            Assert.That(
                player.transform.position,
                Is.EqualTo(outpost.transform.position + Vector3.up));
            Assert.That(state.Player.Gold, Is.EqualTo(100));
            Assert.That(state.Player.Energy, Is.EqualTo(80));

            Object.Destroy(panelHost);
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
            public int OpenCount { get; private set; }

            public bool TryOpenEscapePanel(out string reason)
            {
                OpenCount++;
                reason = string.Empty;
                return true;
            }

            public IReadOnlyList<EmergencyEscapeDestinationOption> GetDestinationOptions()
            {
                return new[]
                {
                    new EmergencyEscapeDestinationOption(
                        EmergencyEscapeDestination.Elevator,
                        string.Empty,
                        "엘리베이터")
                };
            }

            public bool TryEscapeTo(
                EmergencyEscapeDestination kind,
                string outpostInstanceId,
                out string reason)
            {
                reason = string.Empty;
                return true;
            }
        }
    }
}
