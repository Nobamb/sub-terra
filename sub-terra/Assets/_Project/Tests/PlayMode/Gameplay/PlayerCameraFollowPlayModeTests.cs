using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.Gameplay.Player.Tests
{
    public sealed class PlayerCameraFollowPlayModeTests
    {
        private GameObject cameraObject;
        private GameObject targetObject;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(targetObject);
        }

        [UnityTest]
        public IEnumerator D_F01_Follow_ClampsEntireViewportAtEveryEdge()
        {
            PlayerCameraFollow follow = CreateFollow(out Camera camera);
            camera.orthographicSize = 5f;
            camera.aspect = 16f / 9f;

            targetObject.transform.position = new Vector3(100f, -100f, 0f);
            follow.SnapToTarget();
            yield return null;

            Assert.That(camera.transform.position.x, Is.EqualTo(20f - 5f * camera.aspect).Within(0.001f));
            Assert.That(camera.transform.position.y, Is.EqualTo(-10f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator D_F04_Teleport_SnapsImmediatelyWithoutResidualCatchUp()
        {
            PlayerCameraFollow follow = CreateFollow(out Camera camera);
            targetObject.transform.position = Vector3.zero;
            follow.SnapToTarget();

            targetObject.transform.position = new Vector3(1f, 0f, 0f);
            yield return null;
            Assert.That(camera.transform.position.x, Is.GreaterThan(0f).And.LessThan(1f));

            targetObject.transform.position = new Vector3(10f, -8f, 0f);
            yield return null;
            Assert.That(camera.transform.position.x, Is.EqualTo(10f).Within(0.001f));
            Assert.That(camera.transform.position.y, Is.EqualTo(-7f).Within(0.001f));

            yield return null;
            Assert.That(camera.transform.position.x, Is.EqualTo(10f).Within(0.001f));
            Assert.That(camera.transform.position.y, Is.EqualTo(-7f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator D_F02_FortyMeterDescent_KeepsTargetInsideViewport()
        {
            PlayerCameraFollow follow = CreateFollow(out Camera camera);
            CameraBounds2D bounds = camera.GetComponent<CameraBounds2D>();
            bounds.SetWorldBounds(new Vector2(0.5f, -17.5f), new Vector2(81f, 47f));
            targetObject.transform.position = Vector3.zero;
            follow.SnapToTarget();

            const int samples = 40;
            for (int frame = 1; frame <= samples; frame++)
            {
                targetObject.transform.position = new Vector3(
                    0f,
                    Mathf.Lerp(0f, -40f, frame / (float)samples),
                    0f);
                yield return null;

                Vector3 viewport = camera.WorldToViewportPoint(targetObject.transform.position);
                Assert.That(viewport.x, Is.InRange(0f, 1f));
                Assert.That(viewport.y, Is.InRange(0f, 1f));
            }
        }

        [UnityTest]
        public IEnumerator D_F03_CommonAspectRatios_KeepViewportCornersInsideBounds()
        {
            PlayerCameraFollow follow = CreateFollow(out Camera camera);
            float[] aspects = { 16f / 9f, 16f / 10f, 4f / 3f };
            Vector3[] outsideCorners =
            {
                new(-100f, -100f, 0f),
                new(-100f, 100f, 0f),
                new(100f, -100f, 0f),
                new(100f, 100f, 0f)
            };

            foreach (float aspect in aspects)
            {
                camera.aspect = aspect;
                foreach (Vector3 targetPosition in outsideCorners)
                {
                    targetObject.transform.position = targetPosition;
                    follow.SnapToTarget();
                    yield return null;

                    float planeDistance = Mathf.Abs(camera.transform.position.z);
                    Vector3 bottomLeft = camera.ViewportToWorldPoint(
                        new Vector3(0f, 0f, planeDistance));
                    Vector3 topRight = camera.ViewportToWorldPoint(
                        new Vector3(1f, 1f, planeDistance));
                    Assert.That(bottomLeft.x, Is.GreaterThanOrEqualTo(-20.001f));
                    Assert.That(bottomLeft.y, Is.GreaterThanOrEqualTo(-15.001f));
                    Assert.That(topRight.x, Is.LessThanOrEqualTo(20.001f));
                    Assert.That(topRight.y, Is.LessThanOrEqualTo(15.001f));
                }
            }
        }

        private PlayerCameraFollow CreateFollow(out Camera camera)
        {
            targetObject = new GameObject("CameraTarget");
            cameraObject = new GameObject("Camera", typeof(Camera));
            camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;

            CameraBounds2D bounds = cameraObject.AddComponent<CameraBounds2D>();
            bounds.SetWorldBounds(Vector2.zero, new Vector2(40f, 30f));
            PlayerCameraFollow follow = cameraObject.AddComponent<PlayerCameraFollow>();
            follow.SetBoundsProvider(bounds);
            follow.SetTarget(targetObject.transform);
            return follow;
        }
    }
}
