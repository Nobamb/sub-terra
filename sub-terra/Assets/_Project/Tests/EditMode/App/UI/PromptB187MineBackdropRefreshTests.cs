using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.UI;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.EditMode.UI
{
    public sealed class PromptB187MineBackdropRefreshTests
    {
        private MineBackdropSettings settings;
        private float oldTimeScale;
        private bool oldReduceMotion;

        [SetUp]
        public void SetUp()
        {
            oldTimeScale = Time.timeScale;
            oldReduceMotion = AccessibilityPreferences.ReduceMotion;
            settings = Resources.Load<MineBackdropSettings>(MineBackdropSettings.ResourcePath);
            Assert.That(settings, Is.Not.Null);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = oldTimeScale;
            AccessibilityPreferences.ReduceMotion = oldReduceMotion;
        }

        [Test]
        public void Textures_AreAssignedAndLandscape()
        {
            foreach (var texture in new[] { settings.image, settings.ironImage, settings.lithiumImage })
            {
                Assert.That(texture, Is.Not.Null);
                Assert.That(texture.width, Is.GreaterThanOrEqualTo(texture.height));
            }
        }

        [Test]
        public void Tint_IsBrighterThanPreviousDarkValues()
        {
            Assert.That(settings.tint.r, Is.GreaterThan(.65f));
            Assert.That(settings.tint.g, Is.GreaterThan(.70f));
            Assert.That(settings.tint.b, Is.GreaterThan(.75f));
        }

        [Test]
        public void DepthBrightness_DarkensGraduallyWithDepth()
        {
            float previous = settings.DepthBrightness(0f);
            Assert.That(previous, Is.EqualTo(1f));
            for (float depth = 1f; depth <= 60f; depth += 1f)
            {
                float now = settings.DepthBrightness(depth);
                Assert.That(now, Is.LessThanOrEqualTo(previous + .00001f));
                previous = now;
            }
            Assert.That(settings.DepthBrightness(17f), Is.LessThan(.85f));
            Assert.That(settings.DepthBrightness(40f), Is.LessThanOrEqualTo(settings.deepBrightness + .0001f));
            Assert.That(settings.DepthBrightness(5f), Is.GreaterThan(settings.DepthBrightness(37f)));
        }

        [Test]
        public void LayerWeights_FollowCopperIronLithiumBands()
        {
            Assert.That(settings.LayerWeights(5).x, Is.GreaterThan(.99f));
            Assert.That(settings.LayerWeights(25).y, Is.GreaterThan(.99f));
            Assert.That(settings.LayerWeights(37).z, Is.GreaterThan(.4f));
            Assert.That(settings.LayerWeights(40).z, Is.GreaterThan(.99f));
        }

        [Test]
        public void Glow_ReduceMotionKeepsOnlyWeakFixedGlow()
        {
            var glow = new MineBackdropGlow(1);
            for (int i = 0; i < 400; i++) glow.Advance(.1f, 1f, true, settings);
            Assert.That(glow.LithiumBase(settings), Is.LessThanOrEqualTo(.08f));
            Assert.That(glow.LithiumPulse(settings), Is.EqualTo(0f));
            Assert.That(glow.CopperPulse(settings), Is.EqualTo(0f));
            Assert.That(glow.IronPulse(settings), Is.EqualTo(0f));
            Assert.That(glow.LithiumShimmer(settings), Is.EqualTo(0f));
            Assert.That(glow.CopperShimmer(settings), Is.EqualTo(0f));
            Assert.That(glow.IronShimmer(settings), Is.EqualTo(0f));
        }

        [Test]
        public void Shimmer_LithiumIsStrongestWhenMotionAllowed()
        {
            var glow = new MineBackdropGlow(3);
            glow.Advance(.1f, 1f, false, settings);
            Assert.That(glow.LithiumShimmer(settings), Is.GreaterThan(glow.CopperShimmer(settings)));
            Assert.That(glow.LithiumShimmer(settings), Is.GreaterThan(glow.IronShimmer(settings)));
        }

        [Test]
        public void Glow_PulsesRarelyAndStopsWhilePaused()
        {
            var glow = new MineBackdropGlow(2);
            int lit = 0, total = 0;
            for (int i = 0; i < 1200; i++)
            {
                glow.Advance(.05f, 1f, false, settings);
                total++;
                if (glow.Envelope > .05f) lit++;
            }
            Assert.That(lit, Is.GreaterThan(0), "반짝이 한 번도 없음");
            Assert.That(lit / (float)total, Is.LessThan(.3f), "반짝이 너무 잦음");

            float clock = glow.Clock, envelope = glow.Envelope;
            for (int i = 0; i < 100; i++) glow.Advance(.1f, 0f, false, settings);
            Assert.That(glow.Clock, Is.EqualTo(clock));
            Assert.That(glow.Envelope, Is.EqualTo(envelope));
        }

        [Test]
        public void Controller_PausedClockFreezesAndReduceMotionPinsViewport()
        {
            var cameraObject = new GameObject("B187 Camera");
            var targetObject = new GameObject("B187 Target");
            var ownerObject = new GameObject("B187 Owner");
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 6f;
                var backdrop = MineBackdropController.AttachTo(ownerObject.transform, camera, targetObject.transform, null);
                Assert.That(backdrop, Is.Not.Null);
                Time.timeScale = 1f;
                backdrop.Refresh(1f);
                float clock = backdrop.GlowClock;
                Assert.That(clock, Is.GreaterThan(0f));
                Time.timeScale = 0f;
                backdrop.Refresh(1f);
                Assert.That(backdrop.GlowClock, Is.EqualTo(clock));

                Time.timeScale = 1f;
                AccessibilityPreferences.ReduceMotion = true;
                targetObject.transform.position = new Vector3(60f, -35f, 0f);
                backdrop.Refresh(1f);
                var first = backdrop.CurrentViewport;
                targetObject.transform.position = new Vector3(-60f, -5f, 0f);
                backdrop.Refresh(1f);
                Assert.That(backdrop.CurrentViewport.center, Is.EqualTo(first.center));
            }
            finally
            {
                Object.DestroyImmediate(ownerObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(targetObject);
            }
        }
    }
}
