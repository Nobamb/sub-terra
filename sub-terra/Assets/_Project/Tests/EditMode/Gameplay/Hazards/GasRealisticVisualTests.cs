using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SubTerra.Gameplay.Hazards.Tests
{
    /// <summary>B-196: 가스·조명 연출은 부드러워졌지만 판정 수치는 그대로여야 한다.</summary>
    public sealed class GasRealisticVisualTests
    {
        private readonly List<GameObject> spawned = new();

        [SetUp]
        public void SetUp()
        {
            GasMotePool.ResetForTests();
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
            GasMotePool.ResetForTests();
            GasInteriorHaze.Reset();
        }

        [Test]
        public void JudgementConstantsAreUnchanged()
        {
            Assert.That(GasVisualRules.GasRadiusBlocks, Is.EqualTo(5f));
            Assert.That(GasVisualRules.LightClearRadiusBlocks, Is.EqualTo(5f));
            Assert.That(GasVisualRules.SpawnDurationSeconds, Is.EqualTo(1f));
            Assert.That(GasVisualRules.ApproachFadeSeconds, Is.EqualTo(1f));
            Assert.That(GasVisualRules.GasVisualOpacity, Is.EqualTo(0.70f));
            Assert.That(GasVisualRules.InitialApproachOpacity, Is.EqualTo(0.35f));
            Assert.That(GasVisualRules.FullApproachOpacity, Is.EqualTo(0.95f));
        }

        [Test]
        public void LightCover_IsOneAtCenterFadesInsideRadiusAndZeroAtOrBeyondIt()
        {
            const float radius = 5f;
            var inner = GasVisualRules.LightHoleInnerFraction;
            Assert.That(GasVisualMath.LightCover(0f, radius, inner), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(GasVisualMath.LightCover(radius * inner, radius, inner), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(GasVisualMath.LightCover(radius, radius, inner), Is.EqualTo(0f));
            Assert.That(GasVisualMath.LightCover(radius + 0.01f, radius, inner), Is.EqualTo(0f));

            var previous = 1f;
            for (var d = 0f; d <= radius; d += 0.1f)
            {
                var cover = GasVisualMath.LightCover(d, radius, inner);
                Assert.That(cover, Is.LessThanOrEqualTo(previous + 0.0001f), "감쇠는 단조 감소여야 한다: d=" + d);
                previous = cover;
            }

            Assert.That(GasVisualMath.LightCover(3.2f, radius, inner), Is.GreaterThan(0f).And.LessThan(1f));
        }

        [Test]
        public void SoftVisualNeverExtendsPastJudgementRadius()
        {
            var light = Track(new GameObject("Light"));
            light.transform.position = Vector3.zero;
            var source = light.AddComponent<GasVisionClearanceSource>();
            source.SetRadius(GasVisualRules.LightClearRadiusBlocks);

            for (var x = 0f; x <= 8f; x += 0.1f)
            {
                var position = new Vector2(x, 0f);
                var cleared = GasVisionClearanceSource.IsCleared(position);
                var cover = GasVisionHoleEvaluator.SampleCover(position, GasVisualRules.LightHoleInnerFraction);
                Assert.That(cleared, Is.EqualTo(x <= GasVisualRules.LightClearRadiusBlocks), "판정은 예전과 같다: x=" + x);
                if (!cleared)
                {
                    Assert.That(cover, Is.EqualTo(0f), "판정 밖에서는 시각도 걷히지 않는다: x=" + x);
                }
            }
        }

        [Test]
        public void ClearanceSprite_IsSoftAndFitsExactlyTheJudgementDiameter()
        {
            var light = Track(new GameObject("Light"));
            var renderer = light.AddComponent<SpriteRenderer>();
            var source = light.AddComponent<GasVisionClearanceSource>();
            source.SetRadius(GasVisualRules.LightClearRadiusBlocks);

            Assert.That(renderer.sprite, Is.Not.Null);
            Assert.That(renderer.sprite.name, Is.Not.EqualTo("Knob"));
            var worldWidth = renderer.sprite.bounds.size.x * renderer.transform.lossyScale.x;
            Assert.That(worldWidth, Is.EqualTo(GasVisualRules.LightClearRadiusBlocks * 2f).Within(0.01f));
            Assert.That(renderer.color.a, Is.EqualTo(GasVisualRules.LightClearRedOpacity).Within(0.001f));
        }

        [Test]
        public void CloudDensity_IsSoftSettlesOnFloorAndIsZeroAtRadius()
        {
            var core = 0.2f;
            Assert.That(GasVisualMath.CloudDensity(1f, 0f, core, 0.65f, 0.3f), Is.EqualTo(0f));
            Assert.That(GasVisualMath.CloudDensity(1.2f, 0.5f, core, 0.65f, 0.3f), Is.EqualTo(0f));
            Assert.That(GasVisualMath.CloudDensity(0.96f, 0.5f, core, 0.65f, 0.3f), Is.LessThan(0.05f));

            // 중심이 가장자리보다 진하고, 중간 반지름은 둘 사이다(단계가 아니라 연속).
            var centerDensity = GasVisualMath.CloudDensity(0f, 0f, core, 0.65f, 0.3f);
            var midDensity = GasVisualMath.CloudDensity(0.6f, 0f, core, 0.65f, 0.3f);
            var edgeDensity = GasVisualMath.CloudDensity(0.9f, 0f, core, 0.65f, 0.3f);
            Assert.That(centerDensity, Is.GreaterThan(midDensity));
            Assert.That(midDensity, Is.GreaterThan(edgeDensity));
            Assert.That(edgeDensity, Is.GreaterThan(0f));

            // 바닥(y=0)이 천장(y=1)보다 진하다. 편향이 0이면 같다.
            var floor = GasVisualMath.CloudDensity(0.3f, 0f, core, 0.65f, 0.3f);
            var ceiling = GasVisualMath.CloudDensity(0.3f, 1f, core, 0.65f, 0.3f);
            Assert.That(floor, Is.GreaterThan(ceiling));
            Assert.That(
                GasVisualMath.CloudDensity(0.3f, 0f, core, 0f, 0.3f),
                Is.EqualTo(GasVisualMath.CloudDensity(0.3f, 1f, core, 0f, 0.3f)).Within(0.0001f));
        }

        [Test]
        public void ZoneVisual_KeepsRadiusColliderAndSpawnTimingAndStaysSoftInsideIt()
        {
            var root = Track(new GameObject("Gas"));
            var collider = root.AddComponent<CircleCollider2D>();
            var visual = root.AddComponent<GasZoneVisual>();
            var zone = root.GetComponent<GasZone>();
            zone.Activate("b196-zone", GasType.Toxic, 0.8f, GasVisualRules.GasRadiusBlocks, 12f);

            Assert.That(zone.Radius, Is.EqualTo(5f));
            Assert.That(collider.radius, Is.EqualTo(5f));
            Assert.That(zone.Contains(root.transform.position), Is.False, "생성 연출 중에는 접근 판정이 닫혀 있다");

            zone.Tick(0.5f);
            visual.ApplyVisual();
            Assert.That(zone.SpawnProgress, Is.EqualTo(0.5f).Within(0.001f));
            var renderer = visual.CloudRenderer;
            Assert.That(renderer, Is.Not.Null);
            var halfDiameter = renderer.sprite.bounds.size.x * renderer.transform.lossyScale.x;
            Assert.That(halfDiameter, Is.EqualTo(GasVisualRules.GasRadiusBlocks * 2f * 0.5f).Within(0.01f));

            zone.Tick(0.5f);
            visual.ApplyVisual();
            Assert.That(zone.IsSpawnComplete, Is.True);
            Assert.That(zone.Contains(root.transform.position + new Vector3(4.9f, 0f, 0f)), Is.True);
            Assert.That(zone.Contains(root.transform.position + new Vector3(5.1f, 0f, 0f)), Is.False);

            var fullDiameter = renderer.sprite.bounds.size.x * renderer.transform.lossyScale.x;
            Assert.That(fullDiameter, Is.LessThanOrEqualTo(GasVisualRules.GasRadiusBlocks * 2f + 0.01f), "시각이 판정 반지름보다 커지면 안 된다");
            Assert.That(renderer.color.a, Is.LessThanOrEqualTo(GasVisualRules.GasVisualOpacity + 0.0001f));
            Assert.That(renderer.sprite.name, Is.Not.EqualTo("Knob"));
        }

        [Test]
        public void CloudColorKeepsGreenIdentityWithLowerSaturationThanBefore()
        {
            Color.RGBToHSV(new Color(0.22f, 0.82f, 0.34f), out var oldHue, out var oldSaturation, out _);
            Color.RGBToHSV(GasVisualRules.GasTint, out var hue, out var saturation, out _);
            Assert.That(hue, Is.EqualTo(oldHue).Within(0.08f), "녹색 정체성 유지");
            Assert.That(saturation, Is.LessThan(oldSaturation));

            Color.RGBToHSV(new Color(1f, 0.12f, 0.08f), out var oldRedHue, out var oldRedSaturation, out _);
            Color.RGBToHSV(GasVisualRules.LightClearTint, out var redHue, out var redSaturation, out _);
            Assert.That(redHue, Is.EqualTo(oldRedHue).Within(0.05f), "붉은색 정체성 유지");
            Assert.That(redSaturation, Is.LessThan(oldRedSaturation));
        }

        [Test]
        public void CloudMaterialIsSharedAcrossZonesNotCloned()
        {
            var a = CreateZone("shared-a", Vector3.zero);
            var b = CreateZone("shared-b", new Vector3(30f, 0f, 0f));

            var materialA = a.CloudRenderer.sharedMaterial;
            var materialB = b.CloudRenderer.sharedMaterial;
            Assert.That(materialA, Is.Not.Null);
            Assert.That(materialA, Is.SameAs(materialB));
            Assert.That(materialA.shader.name, Is.EqualTo(GasVisualRules.CloudShaderName));
        }

        [Test]
        public void MotesRespectPerZoneAndGlobalCapsAndReturnToPoolWhenOffscreen()
        {
            var zones = new List<GasZoneVisual>();
            for (var i = 0; i < 10; i++)
            {
                zones.Add(CreateZone("mote-" + i, new Vector3(i * 20f, 0f, 0f)));
            }

            foreach (var zone in zones)
            {
                zone.UpdateMotes(true, 3f);
                Assert.That(zone.ActiveMoteCount, Is.LessThanOrEqualTo(zone.Tuning.MotesCapped));
            }

            Assert.That(GasMotePool.ActiveCount, Is.LessThanOrEqualTo(GasCloudTuning.GlobalMoteCap));
            Assert.That(GasMotePool.ActiveCount, Is.GreaterThan(0));

            // 입자는 판정 반지름 안쪽에만 있다.
            var first = zones[0];
            for (var t = 0f; t < 20f; t += 0.5f)
            {
                first.UpdateMotes(true, t);
                foreach (Transform child in first.transform)
                {
                    if (child.name != "GasMote" || !child.gameObject.activeSelf)
                    {
                        continue;
                    }

                    Assert.That(((Vector2)child.localPosition).magnitude, Is.LessThanOrEqualTo(GasVisualRules.GasRadiusBlocks * 0.9f + 0.001f));
                }
            }

            foreach (var zone in zones)
            {
                zone.UpdateMotes(false, 3f);
                Assert.That(zone.ActiveMoteCount, Is.EqualTo(0), "화면 밖에서는 입자를 풀에 돌려준다");
            }

            Assert.That(GasMotePool.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void GlobalMoteCapBlocksRentBeyondLimit()
        {
            GasMotePool.Cap = 3;
            var parent = Track(new GameObject("Parent")).transform;
            var rented = new List<SpriteRenderer>();
            for (var i = 0; i < 6; i++)
            {
                var renderer = GasMotePool.Rent(parent, GasSoftSprites.Radial);
                if (renderer != null)
                {
                    rented.Add(renderer);
                }
            }

            Assert.That(rented.Count, Is.EqualTo(3));
            Assert.That(GasMotePool.ActiveCount, Is.EqualTo(3));
            foreach (var renderer in rented)
            {
                GasMotePool.Return(renderer);
            }

            Assert.That(GasMotePool.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void MoteMath_StaysInsideNinetyPercentOfRadiusAndAlphaIsBounded()
        {
            for (var index = 0; index < 16; index++)
            {
                for (var t = 0f; t < 60f; t += 0.7f)
                {
                    GasVisualMath.EvaluateMote(index, 12.3f, t, 0.65f, out var local, out var alpha, out var scale);
                    Assert.That(local.magnitude, Is.LessThanOrEqualTo(0.9f + 0.0001f));
                    Assert.That(alpha, Is.InRange(0f, 1f));
                    Assert.That(scale, Is.GreaterThan(0f));
                }
            }
        }

        [Test]
        public void Haze_RisesSlowlyFallsQuicklyAndNeverFlashes()
        {
            var haze = 0f;
            // 60fps로 진입: 한 프레임에 크게 뛰지 않고 약 1.2초에 걸쳐 1이 된다.
            var maxStep = 0f;
            for (var i = 0; i < 30; i++)
            {
                var next = GasVisualMath.StepHaze(haze, 1f, 1f / 60f, 1.2f, 0.4f);
                maxStep = Mathf.Max(maxStep, next - haze);
                haze = next;
            }

            Assert.That(maxStep, Is.LessThan(0.02f));
            Assert.That(haze, Is.InRange(0.3f, 0.6f));

            for (var i = 0; i < 60; i++)
            {
                haze = GasVisualMath.StepHaze(haze, 1f, 1f / 60f, 1.2f, 0.4f);
            }

            Assert.That(haze, Is.EqualTo(1f).Within(0.0001f));

            // 나가면 0.4초 안에 걷힌다.
            for (var i = 0; i < 30; i++)
            {
                haze = GasVisualMath.StepHaze(haze, 0f, 1f / 60f, 1.2f, 0.4f);
            }

            Assert.That(haze, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void HazeTintOnlyAddsALittleGreenAndLeavesNoHazeUnchanged()
        {
            var clean = GasVisualMath.VeilDark(0.95f, 0f, 0.5f);
            Assert.That(clean.r, Is.EqualTo(GasVisualRules.VeilDarkColor.r).Within(0.0001f));
            Assert.That(clean.a, Is.EqualTo(0.95f).Within(0.0001f));

            var hazy = GasVisualMath.VeilDark(0.95f, 1f, 0.5f);
            Assert.That(hazy.g, Is.GreaterThan(clean.g));
            Assert.That(hazy.a, Is.EqualTo(0.95f).Within(0.0001f), "불투명도(접근 판정 연출)는 건드리지 않는다");

            var hole = GasVisualMath.LightHole(0f, 0.5f, 0.08f);
            Assert.That(hole.a, Is.EqualTo(GasVisualRules.LightClearRedOpacity).Within(0.0001f));
            var hazyHole = GasVisualMath.LightHole(1f, 0.5f, 0.08f);
            Assert.That(hazyHole.a, Is.GreaterThan(hole.a));
            Assert.That(hazyHole.a, Is.LessThan(0.2f));
        }

        private GasZoneVisual CreateZone(string id, Vector3 position)
        {
            var root = Track(new GameObject(id));
            root.transform.position = position;
            root.AddComponent<CircleCollider2D>();
            var visual = root.AddComponent<GasZoneVisual>();
            var zone = root.GetComponent<GasZone>();
            zone.Activate(id, GasType.Toxic, 0.8f, GasVisualRules.GasRadiusBlocks, 12f, false);
            visual.ApplyVisual();
            return visual;
        }

        private GameObject Track(GameObject go)
        {
            spawned.Add(go);
            return go;
        }
    }
}
