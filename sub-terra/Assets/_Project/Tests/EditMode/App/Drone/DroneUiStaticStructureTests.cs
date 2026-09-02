using System.Linq;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.Integration;
using SubTerra.App.UI.Drone;
using SubTerra.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Drone
{
    public sealed class DroneUiStaticStructureTests
    {
        [OneTimeSetUp]
        public void BuildAssets()
        {
            PhaseKDroneDialogueBuilder.BuildAll();
        }

        [Test]
        public void RequiredDroneUiPrefabs_HaveWiredViewsAndCompositeBinder()
        {
            var dialogue = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/DroneDialoguePanel.prefab");
            var reason = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/DroneReasonPanel.prefab");
            var composite = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/DroneAnalysisUI.prefab");

            Assert.That(
                dialogue.GetComponent<DroneDialoguePanelView>().HasRequiredReferences(),
                Is.True);
            Assert.That(
                reason.GetComponent<DroneReasonPanelView>().HasRequiredReferences(),
                Is.True);
            Assert.That(composite.GetComponent<DroneUiBinder>().HasRequiredReferences(), Is.True);
        }

        [Test]
        public void IntegrationAdapter_ImplementsSharedProvider_WithoutChangingGameplay()
        {
            Assert.That(
                typeof(IDroneContextProvider).IsAssignableFrom(
                    typeof(DroneContextProviderAdapter)),
                Is.True);
        }

        [Test]
        public void K_F04_AppReadings_AreCopiedToSharedContextWithoutRecalculation()
        {
            var sensorType = System.Type.GetType(
                "SubTerra.Gameplay.Drone.DroneSensor, SubTerra.Gameplay.Drone");
            var root = new GameObject("DroneSensor");
            try
            {
                Assert.That(sensorType, Is.Not.Null);
                var sensor = root.AddComponent(sensorType) as MonoBehaviour;
                sensorType.GetMethod("SetAppStateReadings")?.Invoke(
                    sensor,
                    new object[] { 37, 120, 50f, 50f });

                var context = ((IDroneContextProvider)sensor).CreateContext();

                Assert.That(context.currentEnergy, Is.EqualTo(37));
                Assert.That(context.unsettledCargoValue, Is.EqualTo(120));
                Assert.That(context.cargoWeight, Is.EqualTo(50f));
                Assert.That(context.maxCargoWeight, Is.EqualTo(50f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void K_S01_S03_WorldViewPrefab_HasSocketAndNeverBlocksRaycasts()
        {
            var world = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/ViewSocket.prefab");
            var socket = world != null ? world.GetComponent<DroneDialogueSocket>() : null;

            Assert.That(world, Is.Not.Null);
            Assert.That(world.name, Is.EqualTo("ViewSocket"));
            Assert.That(socket, Is.Not.Null);
            Assert.That(socket.HasRequiredReferences(), Is.True);
            Assert.That(
                world.GetComponentsInChildren<Graphic>(true).All(item => !item.raycastTarget),
                Is.True);
            Assert.That(world.GetComponentInChildren<Canvas>().renderMode, Is.EqualTo(RenderMode.WorldSpace));
        }

        [Test]
        public void WorldDialogue_ExposesItsViewportAreaToDarknessOverlayWhileVisible()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/ViewSocket.prefab");
            var instance = Object.Instantiate(prefab);
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            try
            {
                var socket = instance.GetComponent<DroneDialogueSocket>();
                socket.SetVisible(true);
                socket.SetDialogue(new DroneDialogueResult(
                    "dialogue.test",
                    "스캔 결과",
                    false,
                    false,
                    false));
                socket.RefreshPosition();

                Vector4 bypassRect = Shader.GetGlobalVector(
                    DroneDialogueSocket.DarknessBypassShaderProperty);
                Assert.That(bypassRect.z, Is.GreaterThan(bypassRect.x));
                Assert.That(bypassRect.w, Is.GreaterThan(bypassRect.y));

                socket.SetVisible(false);
                bypassRect = Shader.GetGlobalVector(
                    DroneDialogueSocket.DarknessBypassShaderProperty);
                Assert.That(bypassRect.z, Is.LessThanOrEqualTo(bypassRect.x));
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void K_S01_IntegrationScene_WiresWorldAndOverlayViewsToActualProvider()
        {
            const string path = "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
            var scene = SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }

            var transforms = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Transform>(true))
                .ToArray();
            var drone = transforms.Single(item => item.name == "DiggerBot_Runtime");
            var binder = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<DroneUiBinder>(true))
                .Single();

            Assert.That(drone.Find("ViewSocket"), Is.Not.Null);
            Assert.That(binder.HasWorldDialogueSocket, Is.True);
            Assert.That(binder.HasRequiredReferences(), Is.True);
        }
    }
}
