using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.State;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB47_1EmergencyEscapePortalTests
    {
        private const string PortalPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Buildings/EmergencyEscapePortal.prefab";
        private const string PanelPrefabPath =
            "Assets/_Project/Prefabs/UI/EmergencyEscapePanel.prefab";
        private const string IntegrationPath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [Test]
        public void PromptB47_1_DestinationList_ElevatorFirstThenOutposts()
        {
            var state = GameState.CreateNew();
            state.SetGold(300);
            var player = new GameObject("PromptB47_Player");
            var elevator = new GameObject("PromptB47_Elevator");
            elevator.transform.position = new Vector3(1f, 2f, 0f);
            var host = new GameObject("PromptB47_Bridge");
            var bridge = host.AddComponent<EmergencyEscapePortalRuntimeBridge>();
            bridge.Bind(state, player.transform, elevator.transform);

            var options = bridge.GetDestinationOptions();
            Assert.That(options.Count, Is.EqualTo(1));
            Assert.That(options[0].Kind, Is.EqualTo(EmergencyEscapeDestination.Elevator));
            Assert.That(options[0].DisplayName, Is.EqualTo("엘리베이터"));

            var outpostA = new GameObject("OutpostA");
            outpostA.transform.position = new Vector3(10f, -4f, 0f);
            outpostA.AddComponent<BuildingInstance>().Initialize(
                "building.outpost_core.basic-0001",
                DataIds.Buildings.OutpostCoreBasic);
            var outpostB = new GameObject("OutpostB");
            outpostB.transform.position = new Vector3(12f, -8f, 0f);
            outpostB.AddComponent<BuildingInstance>().Initialize(
                "building.outpost_core.basic-0002",
                DataIds.Buildings.OutpostCoreBasic);

            options = bridge.GetDestinationOptions();
            Assert.That(options.Count, Is.EqualTo(3));
            Assert.That(options[0].Kind, Is.EqualTo(EmergencyEscapeDestination.Elevator));
            Assert.That(options[1].InstanceId, Is.EqualTo("building.outpost_core.basic-0001"));
            Assert.That(options[2].InstanceId, Is.EqualTo("building.outpost_core.basic-0002"));

            Object.DestroyImmediate(outpostB);
            Object.DestroyImmediate(outpostA);
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(elevator);
            Object.DestroyImmediate(player);
        }

        [Test]
        public void PromptB47_1_EscapeTo_SelectedDestination_PaysAndTeleports()
        {
            var state = GameState.CreateNew();
            state.SetGold(300);
            var player = new GameObject("PromptB47_Player");
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var elevator = new GameObject("PromptB47_Elevator");
            elevator.transform.position = new Vector3(5f, -2f, 0f);
            var host = new GameObject("PromptB47_Bridge");
            var bridge = host.AddComponent<EmergencyEscapePortalRuntimeBridge>();
            bridge.Bind(state, player.transform, elevator.transform);

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

            Object.DestroyImmediate(outpost);
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(elevator);
            Object.DestroyImmediate(player);
        }

        [Test]
        public void PromptB47_1_OpenPanel_RequiresWiredBinder()
        {
            var state = GameState.CreateNew();
            state.SetGold(300);
            var player = new GameObject("PromptB47_Player");
            var elevator = new GameObject("PromptB47_Elevator");
            var host = new GameObject("PromptB47_Bridge");
            var bridge = host.AddComponent<EmergencyEscapePortalRuntimeBridge>();
            bridge.Bind(state, player.transform, elevator.transform);

            Assert.That(bridge.TryOpenEscapePanel(out var missingReason), Is.False);
            Assert.That(missingReason, Does.Contain("선택 창"));

            var panelHost = new GameObject("PromptB47_Panel");
            var view = panelHost.AddComponent<EmergencyEscapePanelView>();
            var binder = panelHost.AddComponent<EmergencyEscapePanelBinder>();
            // dropdown/cost TMP 참조 없이도 Open → SetVisible 경로가 동작하는지 확인한다.
            SetField(view, "panelRoot", panelHost);
            SetField(binder, "view", view);
            bridge.BindPanel(binder);

            Assert.That(bridge.TryOpenEscapePanel(out _), Is.True);

            Object.DestroyImmediate(panelHost);
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(elevator);
            Object.DestroyImmediate(player);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        [Test]
        public void PromptB47_1_PortalPrefab_IsTwoByTwoSquareVisualAndPassable()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<EmergencyEscapePortal>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PowerNode>().Demand, Is.EqualTo(30));

            var collider = prefab.GetComponent<BoxCollider2D>();
            Assert.That(collider, Is.Not.Null);
            Assert.That(collider.isTrigger, Is.True);
            Assert.That(collider.size.x, Is.EqualTo(2f).Within(0.05f));
            Assert.That(collider.size.y, Is.EqualTo(2f).Within(0.05f));

            var outer = prefab.transform.Find("OuterFrame");
            Assert.That(outer, Is.Not.Null);
            var renderer = outer.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sprite, Is.Not.Null);
            var worldSize = Vector2.Scale(renderer.sprite.bounds.size, outer.localScale);
            Assert.That(worldSize.x, Is.EqualTo(2f).Within(0.1f));
            Assert.That(worldSize.y, Is.EqualTo(2f).Within(0.1f));

            Assert.That(
                prefab.GetComponentsInChildren<Collider2D>(true),
                Has.All.Matches<Collider2D>(c => c.isTrigger));
        }

        [Test]
        public void PromptB47_1_PanelPrefab_HasDropdownAndActions()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var view = prefab.GetComponent<EmergencyEscapePanelView>();
            var binder = prefab.GetComponent<EmergencyEscapePanelBinder>();
            Assert.That(view, Is.Not.Null);
            Assert.That(binder, Is.Not.Null);
            Assert.That(view.HasRequiredReferences(), Is.True);
        }

        [Test]
        public void PromptB47_1_Integration_WiresBridgeAndPanel()
        {
            var scene = EditorSceneManager.OpenScene(IntegrationPath, OpenSceneMode.Additive);
            try
            {
                var bridge = FindInScene<EmergencyEscapePortalRuntimeBridge>(scene);
                Assert.That(bridge, Is.Not.Null);
                var bridgeSo = new SerializedObject(bridge);
                Assert.That(
                    bridgeSo.FindProperty("playerTransform").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    bridgeSo.FindProperty("elevatorCenter").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    bridgeSo.FindProperty("panelBinder").objectReferenceValue,
                    Is.Not.Null);

                var panel = FindInScene<EmergencyEscapePanelBinder>(scene);
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.GetComponent<EmergencyEscapePanelView>(), Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
