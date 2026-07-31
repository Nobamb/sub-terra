using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.Gameplay.Player;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.EditMode.Readiness
{
    public sealed class PhaseDCameraTests
    {
        [TestCase(16f / 9f)]
        [TestCase(16f / 10f)]
        [TestCase(4f / 3f)]
        public void D_S02_ViewportClamp_AccountsForAspectAndOrthographicSize(float aspect)
        {
            var bounds = new Bounds(Vector3.zero, new Vector3(40f, 30f, 0f));

            CameraClampLimits limits = CameraViewportClamp.Calculate(bounds, 5f, aspect);

            Assert.That(limits.MinX, Is.EqualTo(-20f + 5f * aspect).Within(0.0001f));
            Assert.That(limits.MaxX, Is.EqualTo(20f - 5f * aspect).Within(0.0001f));
            Assert.That(limits.MinY, Is.EqualTo(-10f).Within(0.0001f));
            Assert.That(limits.MaxY, Is.EqualTo(10f).Within(0.0001f));
        }

        [Test]
        public void ViewportLargerThanWorld_CentersThatAxis()
        {
            var bounds = new Bounds(new Vector3(3f, -4f, 0f), new Vector3(8f, 6f, 0f));

            CameraClampLimits limits = CameraViewportClamp.Calculate(bounds, 5f, 16f / 9f);

            Assert.That(limits.MinX, Is.EqualTo(3f));
            Assert.That(limits.MaxX, Is.EqualTo(3f));
            Assert.That(limits.MinY, Is.EqualTo(-4f));
            Assert.That(limits.MaxY, Is.EqualTo(-4f));
        }

        [Test]
        public void D_S01_MineAndSurfaceScenes_HaveIndependentBounds()
        {
            Scene mine = EditorSceneManager.OpenScene(
                PhaseDCameraSetup.MineScenePath,
                OpenSceneMode.Single);
            Camera mineCamera = Find<Camera>(mine);
            CameraBounds2D mineBounds = mineCamera.GetComponent<CameraBounds2D>();
            PlayerCameraFollow follow = mineCamera.GetComponent<PlayerCameraFollow>();
            Assert.NotNull(mineBounds);
            Assert.NotNull(follow);
            Assert.That(
                new SerializedObject(follow)
                    .FindProperty("boundsProvider").objectReferenceValue,
                Is.SameAs(mineBounds));
            Assert.That(mineBounds.WorldBounds.size.y, Is.GreaterThanOrEqualTo(40f));
            float mineHeight = mineBounds.WorldBounds.size.y;

            Scene surface = EditorSceneManager.OpenScene(
                PhaseDCameraSetup.SurfaceScenePath,
                OpenSceneMode.Single);
            CameraBounds2D surfaceBounds = Find<Camera>(surface).GetComponent<CameraBounds2D>();
            Assert.NotNull(surfaceBounds);
            Assert.That(surfaceBounds.WorldBounds.size.y, Is.LessThan(mineHeight));
        }

        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
