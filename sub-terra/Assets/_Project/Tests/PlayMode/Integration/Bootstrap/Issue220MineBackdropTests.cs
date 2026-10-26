using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.UI;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class Issue220MineBackdropTests
    {
        [Test]
        public void Backdrop_CoversCameraSmoothsTeleportAndReusesOwnedRenderer()
        {
            var source = Resources.Load<MineBackdropSettings>(MineBackdropSettings.ResourcePath);
            Assert.That(source, Is.Not.Null);
            Assert.That(source.image, Is.Not.Null);
            var config = Object.Instantiate(source);
            var owner = new GameObject("Issue220_Owner");
            var cameraObject = new GameObject("Issue220_Camera", typeof(Camera));
            var target = new GameObject("Issue220_Focus");
            bool originalReduceMotion = AccessibilityPreferences.ReduceMotion;
            try
            {
                AccessibilityPreferences.ReduceMotion = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = 4f;
                camera.aspect = 2f;
                var backdrop = MineBackdropController.AttachTo(owner.transform, camera, target.transform, null);
                Assert.That(backdrop, Is.Not.Null);
                backdrop.Configure(config, camera, target.transform);
                Assert.That(MineBackdropController.AttachTo(owner.transform, camera, target.transform, null), Is.SameAs(backdrop));
                var start = backdrop.CurrentViewport;
                target.transform.position = new Vector3(30, -40, 0);
                camera.transform.position = new Vector3(25, -35, -10);
                var destination = config.Viewport(target.transform.position, camera.aspect);
                backdrop.Refresh(.016f);
                Assert.That(backdrop.CurrentViewport.center.y, Is.LessThan(start.center.y));
                Assert.That(backdrop.CurrentViewport.center.y, Is.GreaterThan(destination.center.y));
                var renderer = backdrop.GetComponent<MeshRenderer>();
                Assert.That(renderer.bounds.size.x, Is.EqualTo(16f).Within(.001f));
                Assert.That(renderer.bounds.size.y, Is.EqualTo(8f).Within(.001f));
                Assert.That(renderer.bounds.center.x, Is.EqualTo(camera.transform.position.x));
                Assert.That(renderer.bounds.center.y, Is.EqualTo(camera.transform.position.y));
                Assert.That(renderer.sortingOrder, Is.LessThan(-1000));
                Assert.That(renderer.sharedMaterial.mainTexture, Is.SameAs(config.image));
                Assert.That(backdrop.GetComponent<Collider>(), Is.Null);
                Assert.That(backdrop.GetComponent<Collider2D>(), Is.Null);
                Assert.That(backdrop.gameObject.hideFlags & HideFlags.DontSave, Is.EqualTo(HideFlags.DontSave));
                backdrop.enabled = false;
                Assert.That(renderer.enabled, Is.False);
                backdrop.enabled = true;
                Assert.That(renderer.enabled, Is.True);
                AccessibilityPreferences.ReduceMotion = true;
                backdrop.Refresh(.016f);
                var fixedViewport = config.Viewport(config.surfaceOrigin, camera.aspect);
                Assert.That(backdrop.CurrentViewport.x, Is.EqualTo(fixedViewport.x).Within(.00001f));
                Assert.That(backdrop.CurrentViewport.y, Is.EqualTo(fixedViewport.y).Within(.00001f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(config);
                AccessibilityPreferences.ReduceMotion = originalReduceMotion;
            }
        }
    }
}
