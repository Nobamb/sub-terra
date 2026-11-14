using NUnit.Framework;
using UnityEngine;

namespace SubTerra.Gameplay.Hazards.Tests
{
    public sealed class GasVisionHoleEvaluatorTests
    {
        [Test]
        public void PromptB196_LightHoleFadesSoftlyInsideFiveBlocksAndNeverPastIt()
        {
            var light = new GameObject("HoleLight");
            var veil = new GameObject("HoleVeil");
            try
            {
                var source = light.AddComponent<GasVisionClearanceSource>();
                source.SetRadius(GasVisualRules.LightClearRadiusBlocks);
                light.transform.position = new Vector3(10f, -4f, 0f);

                var veilComponent = veil.AddComponent<GasVisionWorldVeil>();
                veilComponent.SetOpacity(GasVisualRules.FullApproachOpacity);

                var center = veilComponent.SampleAt(new Vector2(10f, -4f));
                var middle = veilComponent.SampleAt(new Vector2(12.9f, -4f));
                var nearEdge = veilComponent.SampleAt(new Vector2(14.9f, -4f));
                var outside = veilComponent.SampleAt(new Vector2(15.1f, -4f));

                // 중심은 예전처럼 약한 붉은 빛 5%, 경계로 갈수록 베일로 스며들고, 반지름 밖은 베일 그대로다.
                Assert.That(center.r, Is.GreaterThan(0.8f));
                Assert.That(center.a, Is.EqualTo(GasVisualRules.LightClearRedOpacity).Within(0.001f));
                Assert.That(middle.a, Is.GreaterThan(center.a).And.LessThan(nearEdge.a));
                Assert.That(nearEdge.a, Is.LessThanOrEqualTo(GasVisualRules.FullApproachOpacity));
                Assert.That(outside.a, Is.EqualTo(GasVisualRules.FullApproachOpacity).Within(0.001f));
                Assert.That(outside.r, Is.LessThan(0.1f));
            }
            finally
            {
                Object.DestroyImmediate(light);
                Object.DestroyImmediate(veil);
            }
        }
    }
}
