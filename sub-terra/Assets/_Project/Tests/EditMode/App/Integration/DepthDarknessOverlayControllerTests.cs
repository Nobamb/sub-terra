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

        [Test]
        public void EvaluateOpacity_InsideLight_ClearsAtDeepDepth()
        {
            Assert.That(
                DepthDarknessOverlayController.EvaluateOpacity(40, true),
                Is.Zero);
        }

        [Test]
        public void OverlayShader_IsIncludedAsResource()
        {
            Assert.That(
                Resources.Load<Shader>(DepthDarknessOverlayController.ShaderResourceName),
                Is.Not.Null);
        }
    }
}
