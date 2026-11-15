using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Hazards.Tests
{
    /// <summary>B-196-1: 켜진 조명 후광·암전 구멍·베일 중심 알파는 시각만 바꾸고 판정은 그대로여야 한다.</summary>
    public sealed class LightVisibilityTests
    {
        private readonly List<GameObject> spawned = new();
        private bool previousReduceMotion;

        [SetUp]
        public void SetUp()
        {
            previousReduceMotion = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
            LightHaloVisual.ResetBudgetForTests();
            GasInteriorHaze.Reset();
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
            GasInteriorHaze.Reset();
        }

        [Test]
        public void JudgementValuesAreUnchanged()
        {
            Assert.That(GasVisualRules.GasRadiusBlocks, Is.EqualTo(5f));
            Assert.That(GasVisualRules.LightClearRadiusBlocks, Is.EqualTo(5f));
            Assert.That(GasVisualRules.SpawnDurationSeconds, Is.EqualTo(1f));
            Assert.That(GasVisualRules.LightHoleInnerFraction, Is.EqualTo(0.35f));
            Assert.That(GasVisualRules.LightClearRedOpacity, Is.EqualTo(0.05f));
        }

        [Test]
        public void IsClearedIgnoresPowerButVisualsFollowIt()
        {
            var light = CreateLight(new Vector3(0f, 0f, 0f), powered: false);
            var inside = new Vector2(3f, 0f);

            // 판정은 전원과 무관하게 5칸 안에서 켜진다(예전과 같다).
            Assert.That(GasVisionClearanceSource.IsCleared(inside), Is.True);
            Assert.That(GasVisionClearanceSource.IsCleared(new Vector2(5.1f, 0f)), Is.False);

            // 시각 구멍은 전원이 꺼지면 생기지 않는다.
            Assert.That(light.IsLit, Is.False);
            Assert.That(GasVisionHoleEvaluator.SampleCover(inside, 0.35f), Is.EqualTo(0f));

            SetPower(light, true);
            Assert.That(light.IsLit, Is.True);
            Assert.That(GasVisionHoleEvaluator.SampleCover(inside, 0.35f), Is.GreaterThan(0.2f));
            Assert.That(GasVisionHoleEvaluator.SampleCover(new Vector2(5f, 0f), 0.35f), Is.EqualTo(0f));
            Assert.That(GasVisionHoleEvaluator.SampleCover(new Vector2(6f, 0f), 0.35f), Is.EqualTo(0f));
        }

        [Test]
        public void VeilHoleCenterIsNoticeablyStrongerThanTheOldFivePercent()
        {
            CreateLight(new Vector3(10f, -4f, 0f), powered: true);
            var veilObject = Track(new GameObject("Veil"));
            var veil = veilObject.AddComponent<GasVisionWorldVeil>();
            veil.SetOpacity(GasVisualRules.FullApproachOpacity);

            var center = veil.SampleAt(new Vector2(10f, -4f));
            var nearEdge = veil.SampleAt(new Vector2(14.9f, -4f));
            var outside = veil.SampleAt(new Vector2(15.1f, -4f));

            Assert.That(center.a, Is.InRange(0.25f, 0.4501f));
            Assert.That(center.a, Is.GreaterThan(GasVisualRules.LightClearRedOpacity * 4f));
            Assert.That(nearEdge.a, Is.GreaterThan(center.a), "가장자리는 여전히 부드럽게 베일로 이어진다");
            Assert.That(outside.a, Is.EqualTo(GasVisualRules.FullApproachOpacity).Within(0.001f));
        }

        [Test]
        public void CopyActiveLightsSkipsUnpoweredOffscreenAndCapsTheCount()
        {
            var on = CreateLight(new Vector3(0f, 0f, 0f), powered: true);
            var off = CreateLight(new Vector3(2f, 0f, 0f), powered: false);
            var far = CreateLight(new Vector3(500f, 0f, 0f), powered: true);
            var buffer = new Vector4[GasVisionHoleEvaluator.MaxLights];

            Assert.That(GasVisionHoleEvaluator.CopyActiveLights(buffer), Is.EqualTo(2), "꺼진 조명은 제외");
            var view = new Rect(-10f, -6f, 20f, 12f);
            Assert.That(GasVisionHoleEvaluator.CopyActiveLights(buffer, view), Is.EqualTo(1), "화면 밖 조명은 제외");
            Assert.That(buffer[0].w, Is.EqualTo(GasVisualRules.LightClearRadiusBlocks));
            Assert.That(on, Is.Not.Null);
            Assert.That(off, Is.Not.Null);
            Assert.That(far, Is.Not.Null);

            for (var i = 0; i < 40; i++)
            {
                CreateLight(new Vector3(i * 0.1f, 1f, 0f), powered: true);
            }

            Assert.That(
                GasVisionHoleEvaluator.CopyActiveLights(buffer),
                Is.EqualTo(GasVisionHoleEvaluator.MaxLights),
                "상한 32개");
        }

        [Test]
        public void HaloAppearsOnlyWhilePoweredAndNeverExceedsTheJudgementRadius()
        {
            var light = CreateLight(Vector3.zero, powered: true);
            var halo = LightHaloVisual.EnsureFor(light);
            Assert.That(halo, Is.Not.Null);
            Assert.That(LightHaloVisual.EnsureFor(light), Is.SameAs(halo), "중복 생성하지 않는다");

            halo.Tick(1f, 1f, 1, null);
            Assert.That(halo.Renderer.enabled, Is.True);
            Assert.That(halo.Renderer.color.a, Is.GreaterThan(0.2f), "5% 틴트보다 훨씬 진하다");
            Assert.That(halo.Renderer.color.a, Is.LessThan(GasVisualRules.LightHaloOpacity + 0.0001f));

            var diameter = halo.Renderer.bounds.size.x;
            Assert.That(diameter, Is.LessThanOrEqualTo(GasVisualRules.LightClearRadiusBlocks * 2f + 0.01f));

            SetPower(light, false);
            for (var frame = 2; frame < 30; frame++)
            {
                halo.Tick(1f / 60f, frame / 60f, frame, null);
            }

            Assert.That(halo.Renderer.enabled, Is.False, "전원이 꺼지면 후광이 사라진다");

            SetPower(light, true);
            halo.Tick(1f, 2f, 100, null);
            Assert.That(halo.Renderer.enabled, Is.True, "다시 켜면 돌아온다");
        }

        [Test]
        public void UnwiredLightStillGlowsDimlyBecauseItClearsGasWithoutPower()
        {
            // 실제 게임의 조명은 전원 없이도 가스 시야를 뚫는다(IsCleared). 시각도 완전히 꺼지지 않는다.
            var light = CreateLight(Vector3.zero, powered: false, unpoweredLevel: GasVisualRules.LightUnpoweredLevel);
            Assert.That(GasVisionClearanceSource.IsCleared(new Vector2(3f, 0f)), Is.True);
            Assert.That(light.IsLit, Is.True);
            Assert.That(light.LightLevel, Is.EqualTo(GasVisualRules.LightUnpoweredLevel).Within(0.0001f));

            var halo = LightHaloVisual.EnsureFor(light);
            halo.Tick(1f, 1f, 1, null);
            var dim = halo.CurrentAlpha;
            Assert.That(dim, Is.GreaterThan(0.3f), "전원이 없어도 5% 틴트보다 훨씬 진하다");

            SetPower(light, true);
            halo.Tick(1f, 1f, 2, null);
            Assert.That(halo.CurrentAlpha, Is.GreaterThan(dim), "전원이 연결되면 더 밝다");
        }

        [Test]
        public void HaloBudgetStopsAtTheCap()
        {
            var taken = 0;
            for (var i = 0; i < GasVisualRules.MaxVisibleLightHalos + 10; i++)
            {
                if (LightHaloVisual.TryTakeBudget(7))
                {
                    taken++;
                }
            }

            Assert.That(taken, Is.EqualTo(GasVisualRules.MaxVisibleLightHalos));
            Assert.That(LightHaloVisual.TryTakeBudget(8), Is.True, "다음 프레임에는 다시 센다");
        }

        [Test]
        public void OffscreenHaloIsDisabledToSaveCost()
        {
            var light = CreateLight(new Vector3(500f, 0f, 0f), powered: true);
            var halo = LightHaloVisual.EnsureFor(light);
            var cameraObject = Track(new GameObject("HaloCamera"));
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;

            halo.Tick(1f, 1f, 1, camera);
            Assert.That(halo.Renderer.enabled, Is.False);

            cameraObject.transform.position = new Vector3(500f, 0f, -10f);
            halo.Tick(1f, 1f, 2, camera);
            Assert.That(halo.Renderer.enabled, Is.True);
        }

        [Test]
        public void ReduceMotionRemovesFlickerButKeepsTheLight()
        {
            var light = CreateLight(Vector3.zero, powered: true);
            var halo = LightHaloVisual.EnsureFor(light);

            AccessibilityPreferences.ReduceMotion = true;
            var seen = new HashSet<float>();
            for (var i = 0; i < 40; i++)
            {
                halo.Tick(1f, i * 0.37f, 1000 + i, null);
                seen.Add(Mathf.Round(halo.CurrentAlpha * 10000f));
            }

            Assert.That(seen.Count, Is.EqualTo(1), "모션 감소에서는 알파가 고정된다");
            Assert.That(halo.Renderer.enabled, Is.True, "빛은 그대로 보인다");
            Assert.That(halo.CurrentAlpha, Is.EqualTo(GasVisualRules.LightHaloOpacity).Within(0.0001f));

            Assert.That(GasVisualMath.HaloFlicker(3.3f, 0.4f, 0.06f, true), Is.EqualTo(1f));
            AccessibilityPreferences.ReduceMotion = false;
            for (var i = 0; i < 200; i++)
            {
                var multiplier = GasVisualMath.HaloFlicker(i * 0.13f, 0.4f, GasVisualRules.LightHaloFlickerStrength, false);
                Assert.That(multiplier, Is.InRange(1f - GasVisualRules.LightHaloFlickerStrength - 0.0001f, 1f));
            }
        }

        [Test]
        public void DepthDarknessShaderHasPerLightHolesAndOldCenterAlphaStillExists()
        {
            var shader = Resources.Load<Shader>("DepthDarknessOverlayUI");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            try
            {
                Assert.That(material.HasProperty("_LightHoleInner"), Is.True);
                Assert.That(material.HasProperty("_LightHoleStrength"), Is.True);
                Assert.That(GasVisualRules.DarknessHoleStrength, Is.InRange(0.5f, 1f));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }

            Assert.That(Resources.Load<Shader>(GasVisualRules.LightHaloShaderResourceName), Is.Not.Null);
        }

        private GasVisionClearanceSource CreateLight(Vector3 position, bool powered, float unpoweredLevel = 0f)
        {
            var root = Track(new GameObject("TestLight"));
            root.transform.position = position;
            root.AddComponent<PowerNode>();
            var facility = root.AddComponent<PowerFacility>();
            var clearance = new GameObject("GasClearance");
            clearance.transform.SetParent(root.transform, false);
            var source = clearance.AddComponent<GasVisionClearanceSource>();
            source.SetRadius(GasVisualRules.LightClearRadiusBlocks);
            source.SetUnpoweredLevel(unpoweredLevel);
            SetPower(source, powered);
            Assert.That(facility, Is.Not.Null);
            return source;
        }

        private static void SetPower(GasVisionClearanceSource source, bool powered)
        {
            var facility = source.GetComponentInParent<PowerFacility>();
            var method = typeof(PowerFacility).GetMethod(
                "ApplyPowerState",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(bool) },
                null);
            Assert.That(method, Is.Not.Null);
            method.Invoke(facility, new object[] { powered });
            source.RefreshPowerBinding();
        }

        private GameObject Track(GameObject go)
        {
            spawned.Add(go);
            return go;
        }
    }
}
