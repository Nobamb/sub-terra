using NUnit.Framework;
using SubTerra.App.Integration;
using UnityEngine;

namespace SubTerra.App.Tests.Integration
{
    public sealed class DepthDarknessOverlayControllerTests
    {
        [TestCase(0, 0f)]
        [TestCase(9, 0f)]
        [TestCase(10, 0.5f)]
        [TestCase(20, 0.725f)]
        [TestCase(30, 0.95f)]
        [TestCase(40, 0.95f)]
        public void EvaluateOpacity_FollowsPromptDepthCurve(int depth, float expected)
        {
            Assert.That(
                DepthDarknessOverlayController.EvaluateOpacity(depth, false),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(0, 1f)]
        [TestCase(9, 1f)]
        [TestCase(10, 0.45f)]
        [TestCase(20, 0.225f)]
        [TestCase(30, 0f)]
        [TestCase(40, 0f)]
        public void EvaluateLuminance_DarkensOccupiedTilesByDepth(int depth, float expected)
        {
            Assert.That(
                DepthDarknessOverlayController.EvaluateLuminance(depth, false),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void EvaluateOpacity_InsideLight_ClearsAtDeepDepth()
        {
            Assert.That(
                DepthDarknessOverlayController.EvaluateOpacity(40, true),
                Is.Zero);
            Assert.That(
                DepthDarknessOverlayController.EvaluateLuminance(40, true),
                Is.EqualTo(1f));
        }

        [Test]
        public void OverlayShader_IsIncludedAsResource()
        {
            Assert.That(
                Resources.Load<Shader>(DepthDarknessOverlayController.ShaderResourceName),
                Is.Not.Null);
        }

        [Test]
        public void ShouldDrawOutline_OnlyOnOccupiedDarkCellEdges()
        {
            Assert.That(
                DepthDarknessBlockVisual.ShouldDrawOutline(true, new Vector2(0.02f, 0.5f), true),
                Is.True);
            Assert.That(
                DepthDarknessBlockVisual.ShouldDrawOutline(true, new Vector2(0.5f, 0.5f), true),
                Is.False);
            Assert.That(
                DepthDarknessBlockVisual.ShouldDrawOutline(true, new Vector2(0.02f, 0.5f), false),
                Is.False);
            Assert.That(
                DepthDarknessBlockVisual.ShouldDrawOutline(false, new Vector2(0.02f, 0.5f), true),
                Is.False);
        }

        [TestCase(10, 0.55f)]
        [TestCase(20, 0.775f)]
        [TestCase(30, 1f)]
        [TestCase(40, 1f)]
        public void EvaluateOccupiedDarkAlpha_HidesBlockTypeInDarkArea(int depth, float expected)
        {
            Assert.That(
                DepthDarknessOverlayController.EvaluateOccupiedDarkAlpha(depth, false),
                Is.EqualTo(expected).Within(0.0001f));
            Assert.That(
                DepthDarknessOverlayController.EvaluateOccupiedDarkAlpha(depth, false),
                Is.GreaterThanOrEqualTo(
                    DepthDarknessOverlayController.EvaluateOpacity(depth, false)));
        }

        [Test]
        public void OccupiedDarkAlpha_InsideLight_Clears()
        {
            Assert.That(
                DepthDarknessOverlayController.EvaluateOccupiedDarkAlpha(40, true),
                Is.Zero);
        }

        [TestCase(10, 0.5f)]
        [TestCase(20, 0.275f)]
        [TestCase(30, 0.05f)]
        [TestCase(40, 0.05f)]
        public void EvaluateOutlineBrightness_FollowsScreenVeil(int depth, float expected)
        {
            Assert.That(
                DepthDarknessBlockVisual.EvaluateOutlineBrightness(depth, false),
                Is.EqualTo(expected).Within(0.0001f));
            Assert.That(
                DepthDarknessBlockVisual.EvaluateOutlineBrightness(20, false),
                Is.GreaterThan(DepthDarknessBlockVisual.EvaluateOutlineBrightness(30, false)));
        }
    }
}
