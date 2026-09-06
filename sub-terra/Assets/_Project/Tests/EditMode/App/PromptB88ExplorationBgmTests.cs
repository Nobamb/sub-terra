using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.Shared;
using SubTerra.Gameplay.Structural;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SubTerra.App.Tests
{
    public sealed class PromptB88ExplorationBgmTests
    {
        [Test]
        public void IntegrationBgmUsesPlayerSourcesInsteadOfGlobalRiskEvents()
        {
            var scene = EditorSceneManager.OpenPreviewScene(
                "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity");
            try
            {
                ExplorationBgmController controller = null;
                StructuralRiskFeedback feedback = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (controller == null) controller = root.GetComponentInChildren<ExplorationBgmController>(true);
                    if (feedback == null) feedback = root.GetComponentInChildren<StructuralRiskFeedback>(true);
                }
                Assert.That(controller, Is.Not.Null);
                Assert.That(feedback, Is.Not.Null);
                var data = new SerializedObject(controller);
                Assert.That(data.FindProperty("healthSource").objectReferenceValue, Is.InstanceOf<IPlayerHealthSource>());
                var provider = data.FindProperty("contextProvider").objectReferenceValue;
                Assert.That(provider, Is.InstanceOf<IDroneContextProvider>());
                var sensor = new SerializedObject(provider);
                Assert.That(sensor.FindProperty("playerTransform").objectReferenceValue, Is.Not.Null);
                Assert.That(sensor.FindProperty("structuralSystem").objectReferenceValue, Is.Not.Null);
                Assert.That(data.FindProperty("lowHealthRatio").floatValue, Is.EqualTo(0.3f));
                var exploration = (AudioSource)data.FindProperty("explorationBgmSource").objectReferenceValue;
                var danger = (AudioSource)data.FindProperty("dangerBgmSource").objectReferenceValue;
                Assert.That(exploration.clip.name, Is.EqualTo("bgm3"));
                Assert.That(danger.clip.name, Is.EqualTo("bgm4"));
                Assert.That(exploration.loop && danger.loop, Is.True);
                var old = new SerializedObject(feedback);
                Assert.That(old.FindProperty("explorationBgmSource").objectReferenceValue, Is.Null);
                Assert.That(old.FindProperty("dangerBgmSource").objectReferenceValue, Is.Null);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [TestCase(30f, 100, true)]
        [TestCase(30.01f, 100, false)]
        [TestCase(0f, 100, true)]
        [TestCase(45f, 150, true)]
        [TestCase(45.01f, 150, false)]
        public void HealthThresholdUsesCurrentMaximum(float current, int maximum, bool expected)
        {
            Assert.That(ExplorationBgmController.ShouldPlayDanger(
                new PlayerHealthReadModel(current, maximum), SafeContext(), 0.3f), Is.EqualTo(expected));
        }

        [TestCase(1f, 0f, false)]
        [TestCase(0.65f, 0f, false)]
        [TestCase(0.3f, 0f, true)]
        [TestCase(0f, 0f, true)]
        [TestCase(1f, 0.5f, true)]
        [TestCase(1f, 1f, true)]
        public void TerrainUsesPlayerContext(float integrity, float gas, bool expected)
        {
            var context = new DroneContextDto { structuralIntegrity = integrity, gasRisk = gas };
            Assert.That(ExplorationBgmController.ShouldPlayDanger(
                new PlayerHealthReadModel(100f, 100), context, 0.3f), Is.EqualTo(expected));
        }

        [Test]
        public void LeavingDangerAndRecoveringHealthReturnsToExploration()
        {
            var context = new DroneContextDto { structuralIntegrity = 0f };
            var healthy = new PlayerHealthReadModel(100f, 100);
            Assert.That(ExplorationBgmController.ShouldPlayDanger(healthy, context, 0.3f), Is.True);
            context.structuralIntegrity = 1f;
            Assert.That(ExplorationBgmController.ShouldPlayDanger(healthy, context, 0.3f), Is.False);
            Assert.That(ExplorationBgmController.ShouldPlayDanger(
                new PlayerHealthReadModel(30f, 100), context, 0.3f), Is.True);
            Assert.That(ExplorationBgmController.ShouldPlayDanger(healthy, context, 0.3f), Is.False);
        }

        private static DroneContextDto SafeContext()
        {
            return new DroneContextDto { structuralIntegrity = 1f };
        }
    }
}
