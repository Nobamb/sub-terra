using NUnit.Framework;
using SubTerra.App.UI;
using UnityEngine;

namespace SubTerra.App.Tests.EditMode.UI
{
    public sealed class MineBackdropViewportTests
    {
        private MineBackdropSettings settings;
        private Texture2D image;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<MineBackdropSettings>();
            image = new Texture2D(8, 12);
            settings.image = image;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(image);
        }

        [TestCase(16f / 9f)]
        [TestCase(32f / 9f)]
        [TestCase(9f / 16f)]
        public void Viewport_PreservesAspectAndNeverExposesImageEdges(float aspect)
        {
            foreach (var focus in new[] { Vector2.zero, new Vector2(-10000, -10000), new Vector2(10000, 10000) })
            {
                var rect = settings.Viewport(focus, aspect);
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(settings.edgePadding - .00001f));
                Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(settings.edgePadding - .00001f));
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(1f - settings.edgePadding + .00001f));
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(1f - settings.edgePadding + .00001f));
                Assert.That(rect.width * image.width / (rect.height * image.height), Is.EqualTo(aspect).Within(.0001f));
            }
        }

        [Test]
        public void Viewport_DepthAndSidesMoveGraduallyAndReserveFutureMapSpace()
        {
            var top = settings.Viewport(Vector2.zero, 16f / 9f);
            var deep = settings.Viewport(new Vector2(0, -40), 16f / 9f);
            var left = settings.Viewport(new Vector2(-32, -40), 16f / 9f);
            var right = settings.Viewport(new Vector2(32, -40), 16f / 9f);
            Assert.That(deep.y, Is.LessThan(top.y));
            Assert.That(deep.yMin, Is.GreaterThan(settings.edgePadding));
            Assert.That(left.x, Is.LessThan(deep.x));
            Assert.That(right.x, Is.GreaterThan(deep.x));
            Assert.That(left.xMin, Is.GreaterThan(settings.edgePadding));
            Assert.That(right.xMax, Is.LessThan(1f - settings.edgePadding));
        }
    }
}
