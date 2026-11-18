using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Hazards.Tests
{
    /// <summary>B-204: 전진기지·조명 범위 퍼짐은 시각만 바꾸고 판정 반지름은 처음부터 최종값이어야 한다.</summary>
    public sealed class RangeSpreadTests
    {
        private readonly List<GameObject> spawned = new();
        private bool previousReduceMotion;

        [SetUp]
        public void SetUp()
        {
            previousReduceMotion = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
            LightHaloVisual.ResetBudgetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = spawned.Count - 1; i >= 0; i--)
            {
                if (spawned[i] != null)
                {
                    Object.DestroyImmediate(spawned[i]);
                }
            }

            spawned.Clear();
            AccessibilityPreferences.ReduceMotion = previousReduceMotion;
            LightHaloVisual.ResetBudgetForTests();
        }

        [Test]
        public void SpreadIsFasterThanGasSpawnAndMath_IsMonotonic()
        {
            Assert.That(RangeSpreadMath.CoreSpreadSeconds, Is.LessThan(GasVisualRules.SpawnDurationSeconds));
            Assert.That(RangeSpreadMath.LightSpreadSeconds, Is.LessThan(GasVisualRules.SpawnDurationSeconds));

            var previousRadius = 0f;
            var previousOpacity = -1f;
            for (var step = 0; step <= 10; step++)
            {
                var progress = step / 10f;
                var radius = RangeSpreadMath.RadiusFraction(progress);
                var opacity = RangeSpreadMath.OpacityFraction(progress);
                Assert.That(radius, Is.GreaterThanOrEqualTo(previousRadius));
                Assert.That(opacity, Is.GreaterThanOrEqualTo(previousOpacity));
                previousRadius = radius;
                previousOpacity = opacity;
            }

            Assert.That(RangeSpreadMath.RadiusFraction(1f), Is.EqualTo(1f));
            Assert.That(RangeSpreadMath.OpacityFraction(1f), Is.EqualTo(1f));
            Assert.That(RangeSpreadMath.OpacityFraction(0f), Is.EqualTo(0f));
            // 처음에 빠르게 커진다(ease-out): 진행 30%에서 이미 반지름 절반을 넘는다.
            Assert.That(RangeSpreadMath.RadiusFraction(0.3f), Is.GreaterThan(0.5f));
        }

        [Test]
        public void JudgementRadiiAreUnchanged()
        {
            Assert.That(PowerSupplyRangeIndicator.DefaultRadius, Is.EqualTo(10f));
            Assert.That(GasVisualRules.LightClearRadiusBlocks, Is.EqualTo(5f));
        }

        [Test]
        public void Core_SpreadGrowsVisualCircleButContainsUsesFinalRadius()
        {
            var indicator = CreateCore();

            indicator.BeginSpread();
            Assert.That(indicator.IsSpreading, Is.True);
            Assert.That(indicator.VisualRadius, Is.LessThan(1f));
            // 연출 시작 직후에도 최종 가장자리가 전력 범위 안이다.
            Assert.That(indicator.Contains(new Vector3(10f, 0f)), Is.True);
            Assert.That(indicator.Contains(new Vector3(10.01f, 0f)), Is.False);

            indicator.AdvanceSpread(RangeSpreadMath.CoreSpreadSeconds * 0.3f);
            var mid = indicator.VisualRadius;
            Assert.That(mid, Is.GreaterThan(1f).And.LessThan(10f));
            Assert.That(indicator.RangeLine.GetPosition(0).x, Is.EqualTo(mid).Within(0.001f));
            Assert.That(indicator.Contains(new Vector3(10f, 0f)), Is.True);

            indicator.AdvanceSpread(RangeSpreadMath.CoreSpreadSeconds);
            Assert.That(indicator.IsSpreading, Is.False);
            Assert.That(indicator.VisualRadius, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(indicator.RangeLine.GetPosition(0).x, Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void Core_ConfigureWithoutSpreadIsFinalCircleImmediately()
        {
            var indicator = CreateCore();

            Assert.That(indicator.IsSpreading, Is.False);
            Assert.That(indicator.VisualRadius, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(indicator.RangeLine.GetPosition(0).x, Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void Core_ReduceMotionSkipsSpread()
        {
            var indicator = CreateCore();
            AccessibilityPreferences.ReduceMotion = true;

            indicator.BeginSpread();

            Assert.That(indicator.IsSpreading, Is.False);
            Assert.That(indicator.VisualRadius, Is.EqualTo(10f).Within(0.0001f));
        }

        [Test]
        public void Core_ReduceMotionTurnedOnMidSpreadFinishesAtOnce()
        {
            var indicator = CreateCore();
            indicator.BeginSpread();
            indicator.AdvanceSpread(0.05f);
            Assert.That(indicator.IsSpreading, Is.True);

            AccessibilityPreferences.ReduceMotion = true;
            indicator.AdvanceSpread(0.001f);

            Assert.That(indicator.IsSpreading, Is.False);
            Assert.That(indicator.VisualRadius, Is.EqualTo(10f).Within(0.0001f));
        }

        [Test]
        public void Light_RevealGrowsVisualRadiusButJudgementStaysFinal()
        {
            var light = CreateLight(Vector3.zero);
            var edge = new Vector2(4.9f, 0f);

            light.BeginReveal();

            Assert.That(light.IsRevealing, Is.True);
            Assert.That(light.Radius, Is.EqualTo(5f));
            Assert.That(light.VisualRadius, Is.LessThan(1f));
            // 켠 직후에도 가스 시야 판정은 최종 5칸이다.
            Assert.That(GasVisionClearanceSource.IsCleared(edge), Is.True);
            Assert.That(GasVisionClearanceSource.IsCleared(new Vector2(5.1f, 0f)), Is.False);
            Assert.That(light.Contains(edge), Is.True);
            // 시각 구멍은 아직 가장자리까지 열리지 않았다.
            Assert.That(GasVisionHoleEvaluator.SampleCover(edge, 0.35f), Is.EqualTo(0f));

            light.AdvanceReveal(RangeSpreadMath.LightSpreadSeconds * 0.3f);
            var mid = light.VisualRadius;
            Assert.That(mid, Is.GreaterThan(1f).And.LessThan(5f));
            Assert.That(GasVisionHoleEvaluator.SampleCover(new Vector2(mid * 0.5f, 0f), 0.35f), Is.GreaterThan(0f));

            light.AdvanceReveal(RangeSpreadMath.LightSpreadSeconds);
            Assert.That(light.IsRevealing, Is.False);
            Assert.That(light.VisualRadius, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(GasVisionHoleEvaluator.SampleCover(new Vector2(3f, 0f), 0.35f), Is.GreaterThan(0.2f));
        }

        [Test]
        public void Light_RevealMakesRangeSpriteAndHaloOpaqueGradually()
        {
            var light = CreateLight(Vector3.zero);
            var clearance = light.GetComponent<SpriteRenderer>();
            var halo = LightHaloVisual.EnsureFor(light);
            Assert.That(clearance, Is.Not.Null);
            Assert.That(halo, Is.Not.Null);

            light.BeginReveal();
            halo.Tick(1f, 1f, 1, null);
            var startSprite = clearance.color.a;
            var startHalo = halo.CurrentAlpha;
            var startScale = clearance.transform.localScale.x;

            light.AdvanceReveal(RangeSpreadMath.LightSpreadSeconds * 0.5f);
            LightHaloVisual.ResetBudgetForTests();
            halo.Tick(1f, 1f, 2, null);
            var midSprite = clearance.color.a;
            var midHalo = halo.CurrentAlpha;
            var midScale = clearance.transform.localScale.x;

            light.AdvanceReveal(RangeSpreadMath.LightSpreadSeconds);
            LightHaloVisual.ResetBudgetForTests();
            halo.Tick(1f, 1f, 3, null);

            Assert.That(midSprite, Is.GreaterThan(startSprite));
            Assert.That(clearance.color.a, Is.GreaterThan(midSprite));
            Assert.That(clearance.color.a, Is.EqualTo(GasVisualRules.LightClearRedOpacity).Within(0.0001f));
            Assert.That(midHalo, Is.GreaterThan(startHalo));
            Assert.That(halo.CurrentAlpha, Is.GreaterThan(midHalo));
            Assert.That(midScale, Is.GreaterThan(startScale));
            Assert.That(clearance.transform.localScale.x, Is.GreaterThan(midScale));
        }

        [Test]
        public void Light_AlreadyLitAndRestoredLightsStayFinalWithoutReveal()
        {
            var light = CreateLight(Vector3.zero);

            // 복원·이미 켜진 조명은 BeginReveal을 거치지 않아 처음부터 최종 범위다.
            Assert.That(light.IsRevealing, Is.False);
            Assert.That(light.VisualRadius, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(light.VisualOpacity, Is.EqualTo(1f));
            Assert.That(GasVisionHoleEvaluator.SampleCover(new Vector2(3f, 0f), 0.35f), Is.GreaterThan(0.2f));

            var halo = LightHaloVisual.EnsureFor(light);
            halo.Tick(1f, 1f, 1, null);
            Assert.That(halo.CurrentAlpha, Is.GreaterThan(0.3f));
        }

        [Test]
        public void Light_ReEnableAfterRevealShowsFinalRangeImmediately()
        {
            var light = CreateLight(Vector3.zero);
            light.BeginReveal();
            Assert.That(light.IsRevealing, Is.True);

            // EditMode에서는 OnEnable이 자동 호출되지 않아 직접 부른다. 정적 Active 목록이 남지 않게 OnDisable까지 짝을 맞춘다.
            Invoke(light, "OnEnable");
            try
            {
                Assert.That(light.IsRevealing, Is.False);
                Assert.That(light.VisualRadius, Is.EqualTo(5f).Within(0.0001f));
            }
            finally
            {
                Invoke(light, "OnDisable");
            }
        }

        private static void Invoke(GasVisionClearanceSource source, string methodName)
        {
            var method = typeof(GasVisionClearanceSource).GetMethod(
                methodName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(source, null);
        }

        [Test]
        public void Light_ReduceMotionSkipsReveal()
        {
            var light = CreateLight(Vector3.zero);
            AccessibilityPreferences.ReduceMotion = true;

            light.BeginReveal();

            Assert.That(light.IsRevealing, Is.False);
            Assert.That(light.VisualRadius, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(light.VisualOpacity, Is.EqualTo(1f));
        }

        [Test]
        public void Light_RevealCopiesGrowingRadiusToHoleBuffers()
        {
            var light = CreateLight(Vector3.zero);
            var buffer = new Vector4[GasVisionHoleEvaluator.MaxLights];

            light.BeginReveal();
            GasVisionHoleEvaluator.CopyActiveLights(buffer);
            var startRadius = buffer[0].w;

            light.AdvanceReveal(RangeSpreadMath.LightSpreadSeconds * 0.5f);
            GasVisionHoleEvaluator.CopyActiveLights(buffer);
            var midRadius = buffer[0].w;

            light.AdvanceReveal(RangeSpreadMath.LightSpreadSeconds);
            GasVisionHoleEvaluator.CopyActiveLights(buffer);

            Assert.That(midRadius, Is.GreaterThan(startRadius));
            Assert.That(buffer[0].w, Is.EqualTo(5f).Within(0.0001f));
        }

        private PowerSupplyRangeIndicator CreateCore()
        {
            var root = Track(new GameObject("TestCore"));
            var indicator = root.AddComponent<PowerSupplyRangeIndicator>();
            indicator.Configure(PowerSupplyRangeIndicator.DefaultRadius);
            return indicator;
        }

        private GasVisionClearanceSource CreateLight(Vector3 position)
        {
            var root = Track(new GameObject("TestLight"));
            root.transform.position = position;
            var clearance = new GameObject("GasClearance");
            clearance.transform.SetParent(root.transform, false);
            clearance.AddComponent<SpriteRenderer>();
            var source = clearance.AddComponent<GasVisionClearanceSource>();
            source.SetRadius(GasVisualRules.LightClearRadiusBlocks);
            return source;
        }

        private GameObject Track(GameObject go)
        {
            spawned.Add(go);
            return go;
        }
    }
}
