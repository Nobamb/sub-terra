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
        public void OverlayShader_HasFadePropertyForBoundaryBlend()
        {
            var shader = Resources.Load<Shader>(DepthDarknessOverlayController.ShaderResourceName);
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            try
            {
                Assert.That(material.HasProperty("_Fade"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
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

        [Test]
        public void BoundaryFadeSeconds_IsOneSecond()
        {
            Assert.That(DepthDarknessBlockVisual.BoundaryFadeSeconds, Is.EqualTo(1f));
            Assert.That(
                DepthDarknessOverlayController.BoundaryFadeSeconds,
                Is.EqualTo(DepthDarknessBlockVisual.BoundaryFadeSeconds));
        }

        [TestCase(0, 0f)]
        [TestCase(9, 0f)]
        [TestCase(10, 1f)]
        [TestCase(30, 1f)]
        public void TargetBoundaryWeight_SnapsAtTenMeterThreshold(int depth, float expected)
        {
            Assert.That(
                DepthDarknessBlockVisual.TargetBoundaryWeight(depth),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void StepBoundaryWeight_FadesInOverOneSecondAtTenMeters()
        {
            Assert.That(
                DepthDarknessBlockVisual.StepBoundaryWeight(0f, 10, 0.25f),
                Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(
                DepthDarknessBlockVisual.StepBoundaryWeight(0f, 10, 1f),
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(
                DepthDarknessBlockVisual.StepBoundaryWeight(0f, 10, 2f),
                Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void EvaluateDisplayedOpacity_MidFadeIsPartialNotDelayedSnap()
        {
            var quarter = DepthDarknessBlockVisual.StepBoundaryWeight(0f, 10, 0.25f);
            var half = DepthDarknessBlockVisual.StepBoundaryWeight(0f, 10, 0.5f);
            var threeQuarter = DepthDarknessBlockVisual.StepBoundaryWeight(0f, 10, 0.75f);
            Assert.That(
                DepthDarknessOverlayController.EvaluateDisplayedOpacity(10, false, quarter),
                Is.EqualTo(0.125f).Within(0.0001f));
            Assert.That(
                DepthDarknessOverlayController.EvaluateDisplayedOpacity(10, false, half),
                Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(
                DepthDarknessOverlayController.EvaluateDisplayedOpacity(10, false, threeQuarter),
                Is.EqualTo(0.375f).Within(0.0001f));
        }

        [Test]
        public void StepBoundaryWeight_FadesOutOverOneSecondAboveTenMeters()
        {
            Assert.That(
                DepthDarknessBlockVisual.StepBoundaryWeight(1f, 9, 0.5f),
                Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(
                DepthDarknessBlockVisual.StepBoundaryWeight(1f, 9, 1f),
                Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void StepBoundaryWeight_CanReverseMidFade()
        {
            var entering = DepthDarknessBlockVisual.StepBoundaryWeight(0f, 10, 0.4f);
            var leaving = DepthDarknessBlockVisual.StepBoundaryWeight(entering, 9, 0.2f);
            Assert.That(entering, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(leaving, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void EvaluateDisplayedOpacity_FadesFromTenMeterValuesWhenLeaving()
        {
            Assert.That(
                DepthDarknessOverlayController.EvaluateDisplayedOpacity(9, false, 1f),
                Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(
                DepthDarknessOverlayController.EvaluateDisplayedOpacity(9, false, 0.5f),
                Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(
                DepthDarknessOverlayController.EvaluateDisplayedOpacity(10, false, 0.5f),
                Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(
                DepthDarknessOverlayController.EvaluateDisplayedOpacity(9, false, 0f),
                Is.Zero);
        }

        [Test]
        public void EvaluateDisplayedLuminance_FadesOccupiedTilesAtBoundary()
        {
            Assert.That(
                DepthDarknessOverlayController.EvaluateDisplayedLuminance(9, false, 0f),
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(
                DepthDarknessOverlayController.EvaluateDisplayedLuminance(10, false, 0.5f),
                Is.EqualTo(0.725f).Within(0.0001f));
            Assert.That(
                DepthDarknessOverlayController.EvaluateDisplayedOccupiedDarkAlpha(10, false, 0.5f),
                Is.EqualTo(0.275f).Within(0.0001f));
        }

        [Test]
        public void EvaluateDisplayedOpacity_InsideLight_ClearsDuringFade()
        {
            Assert.That(
                DepthDarknessOverlayController.EvaluateDisplayedOpacity(30, true, 1f),
                Is.Zero);
            Assert.That(
                DepthDarknessOverlayController.EvaluateDisplayedLuminance(30, true, 0.4f),
                Is.EqualTo(1f));
        }
    }
}
