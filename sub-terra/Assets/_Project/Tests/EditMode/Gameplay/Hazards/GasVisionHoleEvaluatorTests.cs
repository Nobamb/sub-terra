using NUnit.Framework;
using UnityEngine;

namespace SubTerra.Gameplay.Hazards.Tests
{
    public sealed class GasVisionHoleEvaluatorTests
    {
        [Test]
        public void PromptB50_1_LightHoleUsesFivePercentRedInsideFiveBlocks()
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

                var inside = veilComponent.SampleAt(new Vector2(14.9f, -4f));
                var outside = veilComponent.SampleAt(new Vector2(15.1f, -4f));

                Assert.That(inside.r, Is.GreaterThan(0.8f));
                Assert.That(inside.a, Is.EqualTo(GasVisualRules.LightClearRedOpacity).Within(0.001f));
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
