using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.State;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 92: 긴급 탈출 목적지 선택 시 해당 위치가 화면 중앙에 오도록 시점을 옮긴다.</summary>
    public sealed class PromptB92EmergencyEscapePreviewTests
    {
        private readonly List<GameObject> spawned = new();

        [TearDown]
        public void TearDown()
        {
            for (var i = spawned.Count - 1; i >= 0; i--)
            {
                if (spawned[i] != null)
                {
                    Object.DestroyImmediate(spawned[i]);
                }
            }

            spawned.Clear();
        }

        [Test]
        public void CameraFollow_PreviewWorldPosition_CentersDestinationThenRestoresPlayer()
        {
            var player = Create("Player");
            player.transform.position = new Vector3(2f, -3f, 0f);
            var cameraObject = Create("Camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            var follow = cameraObject.AddComponent<PlayerCameraFollow>();
            follow.SetTarget(player.transform, snapImmediately: true);

            var offset = follow.FollowOffset;
            AssertNear(cameraObject.transform.position, player.transform.position + offset);

            var destination = new Vector3(12f, -18f, 0f);
            follow.PreviewWorldPosition(destination, snapImmediately: true);

            Assert.That(follow.IsPreviewActive, Is.True);
            Assert.That(cameraObject.transform.position.x, Is.EqualTo(destination.x).Within(0.001f));
            Assert.That(cameraObject.transform.position.y, Is.EqualTo(destination.y).Within(0.001f));
            Assert.That(
                cameraObject.transform.position.z,
                Is.EqualTo(destination.z + offset.z).Within(0.001f));
            Assert.That(player.transform.position, Is.EqualTo(new Vector3(2f, -3f, 0f)));

            follow.ClearPreview(snapImmediately: true);
            Assert.That(follow.IsPreviewActive, Is.False);
            AssertNear(cameraObject.transform.position, player.transform.position + offset);
        }

        [Test]
        public void PreviewDestination_CentersElevatorOrOutpostCore_WithoutSpendingOrTeleport()
        {
            var fixture = CreateFixture();
            var playerStart = fixture.Player.transform.position;
            var gold = fixture.State.Player.Gold;
            var energy = fixture.State.Player.Energy;

            Assert.That(
                fixture.Bridge.TryPreviewDestination(
                    EmergencyEscapeDestination.Elevator,
                    string.Empty,
                    out _),
                Is.True);
            AssertCameraCenteredOn(fixture.Camera.transform, fixture.Elevator.transform.position);
            Assert.That(fixture.Follow.IsPreviewActive, Is.True);
            Assert.That(fixture.Player.transform.position, Is.EqualTo(playerStart));
            Assert.That(fixture.State.Player.Gold, Is.EqualTo(gold));
            Assert.That(fixture.State.Player.Energy, Is.EqualTo(energy));

            Assert.That(
                fixture.Bridge.TryPreviewDestination(
                    EmergencyEscapeDestination.OutpostCore,
                    "building.outpost_core.basic-prompt-b92",
                    out _),
                Is.True);
            AssertCameraCenteredOn(fixture.Camera.transform, fixture.Outpost.transform.position);
            Assert.That(fixture.Player.transform.position, Is.EqualTo(playerStart));
            Assert.That(fixture.State.Player.Gold, Is.EqualTo(gold));

            fixture.Bridge.ClearDestinationPreview();
            Assert.That(fixture.Follow.IsPreviewActive, Is.False);
            AssertNear(fixture.Camera.transform.position, playerStart + fixture.Follow.FollowOffset);
        }

        [Test]
        public void OpenPanel_PreviewsDefaultElevator_AndSelectionChangeMovesToOutpostCore()
        {
            var fixture = CreateFixture();
            var playerStart = fixture.Player.transform.position;

            Assert.That(fixture.Bridge.TryOpenEscapePanel(out _), Is.True);
            Assert.That(fixture.Binder.IsOpen, Is.True);
            AssertCameraCenteredOn(fixture.Camera.transform, fixture.Elevator.transform.position);

            var options = fixture.Bridge.GetDestinationOptions();
            var outpostIndex = -1;
            for (var i = 0; i < options.Count; i++)
            {
                if (options[i].InstanceId == "building.outpost_core.basic-prompt-b92")
                {
                    outpostIndex = i;
                    break;
                }
            }

            Assert.That(outpostIndex, Is.GreaterThan(0));
            fixture.Binder.PreviewDestinationAt(outpostIndex);
            AssertCameraCenteredOn(fixture.Camera.transform, fixture.Outpost.transform.position);
            Assert.That(fixture.Player.transform.position, Is.EqualTo(playerStart));
            Assert.That(fixture.State.Player.Gold, Is.EqualTo(300));

            fixture.Binder.Close();
            Assert.That(fixture.Binder.IsOpen, Is.False);
            Assert.That(fixture.Follow.IsPreviewActive, Is.False);
            AssertNear(fixture.Camera.transform.position, playerStart + fixture.Follow.FollowOffset);
        }

        [Test]
        public void PreviewWithoutCamera_StillSucceedsAndLeavesPlayerUnmoved()
        {
            var state = GameState.CreateNew();
            state.SetGold(300);
            var player = Create("PromptB92_PlayerNoCam");
            player.transform.position = new Vector3(1f, -1f, 0f);
            var elevator = Create("PromptB92_ElevatorNoCam");
            elevator.transform.position = new Vector3(8f, 4f, 0f);
            var host = Create("PromptB92_BridgeNoCam");
            var bridge = host.AddComponent<EmergencyEscapePortalRuntimeBridge>();
            bridge.Bind(state, player.transform, elevator.transform);

            Assert.That(
                bridge.TryPreviewDestination(
                    EmergencyEscapeDestination.Elevator,
                    string.Empty,
                    out _),
                Is.True);
            Assert.That(player.transform.position, Is.EqualTo(new Vector3(1f, -1f, 0f)));
            Assert.That(state.Player.Gold, Is.EqualTo(300));
        }

        private Fixture CreateFixture()
        {
            var state = GameState.CreateNew();
            state.SetGold(300);

            var player = Create("PromptB92_Player");
            player.transform.position = new Vector3(-4f, -6f, 0f);

            var elevator = Create("PromptB92_Elevator");
            elevator.transform.position = new Vector3(5f, 2f, 0f);

            var outpost = Create("PromptB92_Outpost");
            outpost.transform.position = new Vector3(14f, -16f, 0f);
            outpost.AddComponent<BuildingInstance>().Initialize(
                "building.outpost_core.basic-prompt-b92",
                DataIds.Buildings.OutpostCoreBasic);

            var cameraObject = Create("PromptB92_Camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            var follow = cameraObject.AddComponent<PlayerCameraFollow>();
            follow.SetTarget(player.transform, snapImmediately: true);

            var host = Create("PromptB92_Bridge");
            var bridge = host.AddComponent<EmergencyEscapePortalRuntimeBridge>();
            bridge.Bind(state, player.transform, elevator.transform);
            bridge.BindCameraFollow(follow);

            var panelHost = Create("PromptB92_Panel");
            var view = panelHost.AddComponent<EmergencyEscapePanelView>();
            var binder = panelHost.AddComponent<EmergencyEscapePanelBinder>();
            SetField(view, "panelRoot", panelHost);
            SetField(binder, "view", view);
            bridge.BindPanel(binder);

            return new Fixture
            {
                State = state,
                Player = player,
                Elevator = elevator,
                Outpost = outpost,
                Camera = camera,
                Follow = follow,
                Bridge = bridge,
                Binder = binder
            };
        }

        private GameObject Create(string name, params System.Type[] components)
        {
            var created = components != null && components.Length > 0
                ? new GameObject(name, components)
                : new GameObject(name);
            spawned.Add(created);
            return created;
        }

        private static void AssertCameraCenteredOn(Transform camera, Vector3 worldPosition)
        {
            Assert.That(camera.position.x, Is.EqualTo(worldPosition.x).Within(0.001f));
            Assert.That(camera.position.y, Is.EqualTo(worldPosition.y).Within(0.001f));
        }

        private static void AssertNear(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.001f));
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(
                name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private sealed class Fixture
        {
            public GameState State;
            public GameObject Player;
            public GameObject Elevator;
            public GameObject Outpost;
            public Camera Camera;
            public PlayerCameraFollow Follow;
            public EmergencyEscapePortalRuntimeBridge Bridge;
            public EmergencyEscapePanelBinder Binder;
        }
    }
}
