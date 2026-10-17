using NUnit.Framework;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB106HudTests
    {
        [Test]
        public void DamageTrail_UsesIndependentOrangeInsideOriginalShapeAndResets()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/BasicHUD.prefab"));
            try
            {
                var hud = instance.GetComponent<BasicHudView>();
                hud.SetHealthLevel(100, 100);
                hud.SetHealthLevel(60, 100);
                var gauge = instance.transform.Find("HealthGauge");
                var fill = gauge.Find("Fill").GetComponent<UnityEngine.UI.Image>();
                var trail = gauge.Find("DamageTrail").GetComponent<UnityEngine.UI.Image>();
                var color = trail.transform.Find("TrailColor").GetComponent<UnityEngine.UI.Image>();
                Assert.That(fill.fillAmount, Is.EqualTo(0.6f));
                Assert.That(trail.fillAmount, Is.EqualTo(1f));
                Assert.That(trail.sprite, Is.EqualTo(fill.sprite), "모양은 원본 마스크를 유지한다.");
                Assert.That(trail.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic, Is.False);
                Assert.That(color.sprite, Is.Null, "빨간 원본을 곱하지 않고 주황 단색을 표시한다.");
                Assert.That(color.color, Is.EqualTo(new Color(1f, 0.65f, 0.1f, 1f)));
                Assert.That(color.raycastTarget, Is.False);
                Assert.That(trail.transform.GetSiblingIndex(), Is.LessThan(fill.transform.GetSiblingIndex()));
                Assert.That(gauge.Find("Frame").GetSiblingIndex(), Is.GreaterThan(fill.transform.GetSiblingIndex()));
                hud.SetHealthLevel(80, 100);
                Assert.That(trail.fillAmount, Is.EqualTo(1f), "잔상보다 낮은 회복은 피격 잔상을 지우지 않는다.");
                hud.SetHealthLevel(80, 200);
                Assert.That(trail.fillAmount, Is.EqualTo(0.4f));
                hud.SetHealthLevel(40, 200);
                gauge.GetComponent<HudGaugeView>().enabled = false;
                // 일반 MonoBehaviour의 비활성 콜백은 EditMode에서 자동 실행되지 않는다.
                typeof(HudGaugeView).GetMethod("OnDisable",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(gauge.GetComponent<HudGaugeView>(), null);
                Assert.That(trail.fillAmount, Is.EqualTo(0.2f));
                Assert.That(instance.transform.Find("EnergyGauge/DamageTrail"), Is.Null);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void DamageTrail_HoldsThenFallsToCurrentHealth()
        {
            var trail = new HealthDamageTrail();
            trail.Reset(1f);
            trail.Set(0.6f);
            trail.Advance(0.3f);
            Assert.That(trail.Fraction, Is.EqualTo(1f));
            trail.Advance(0.25f);
            Assert.That(trail.Fraction, Is.EqualTo(0.8f).Within(0.001f));
            trail.Advance(0.3f);
            Assert.That(trail.Fraction, Is.EqualTo(0.6f).Within(0.001f));
        }

        [Test]
        public void DamageTrail_ConsecutiveDamageContinuesFromVisibleAmount()
        {
            var trail = new HealthDamageTrail();
            trail.Reset(1f);
            trail.Set(0.6f);
            trail.Advance(0.55f);
            float visible = trail.Fraction;
            trail.Set(0.4f);
            Assert.That(trail.Fraction, Is.EqualTo(visible));
            trail.Set(0.4f);
            trail.Advance(0.75f);
            Assert.That(trail.Fraction, Is.EqualTo(0.4f).Within(0.001f));
        }

        [Test]
        public void DamageTrail_HealingCatchUpAndResetDoNotLeaveDamageSegment()
        {
            var trail = new HealthDamageTrail();
            trail.Reset(1f);
            trail.Set(0.4f);
            trail.Advance(0.2f);
            trail.Set(1f);
            Assert.That(trail.Fraction, Is.EqualTo(1f));
            trail.Advance(0.1f);
            Assert.That(trail.Fraction, Is.EqualTo(1f));
            trail.Reset(0.5f);
            Assert.That(trail.Fraction, Is.EqualTo(0.5f));
        }

        [Test]
        public void DamageTrail_RegenerationDuringHoldDoesNotEraseOrRestartTrail()
        {
            var trail = new HealthDamageTrail();
            trail.Reset(1f);
            trail.Set(0.6f);
            for (var i = 1; i <= 3; i++)
            {
                trail.Set(0.6f + i * 0.001f);
                trail.Advance(0.1f);
                Assert.That(trail.Fraction, Is.EqualTo(1f));
            }
            trail.Advance(0.25f);
            Assert.That(trail.Fraction, Is.EqualTo(0.8015f).Within(0.001f), "대기 시간은 최초 피격 기준이다.");
        }

        [Test]
        public void DamageTrail_ContinuousRegenerationFinishesOnTimeWithoutGrowing()
        {
            var trail = new HealthDamageTrail();
            trail.Reset(1f);
            trail.Set(0.6f);
            var previous = 1f;
            for (var i = 1; i <= 15; i++)
            {
                var health = 0.6f + i * 0.001f;
                trail.Set(health);
                trail.Advance(0.05f);
                Assert.That(trail.Fraction, Is.LessThanOrEqualTo(previous + 0.0001f));
                Assert.That(trail.Fraction, Is.GreaterThanOrEqualTo(health));
                previous = trail.Fraction;
            }
            Assert.That(trail.Fraction, Is.EqualTo(0.615f).Within(0.001f));
        }

        [Test]
        public void Prefab_HasLayeredGaugesAndRequiredReferences()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/BasicHUD.prefab");
            Assert.That(root.GetComponent<BasicHudView>().HasRequiredReferences(), Is.True);
            foreach (string name in new[] { "HealthGauge", "EnergyGauge" })
            {
                var gauge = root.transform.Find(name);
                Assert.That(gauge.GetComponent<HudGaugeView>(), Is.Not.Null);
                var fill = gauge.Find("Fill").GetComponent<UnityEngine.UI.Image>();
                Assert.That(fill.type, Is.EqualTo(UnityEngine.UI.Image.Type.Filled));
                Assert.That(fill.fillOrigin, Is.Zero);
                Assert.That(fill.fillMethod, Is.EqualTo(UnityEngine.UI.Image.FillMethod.Horizontal));
                Assert.That(fill.raycastTarget, Is.False);
                Assert.That(gauge.Find("Empty").GetSiblingIndex(), Is.LessThan(fill.transform.GetSiblingIndex()));
                Assert.That(gauge.Find("Frame").GetSiblingIndex(), Is.GreaterThan(fill.transform.GetSiblingIndex()));
            }
            // 하단 전력·연결·활성 아이콘은 구조 상태 행(Image)으로 대체되어 RawImage는 6개다.
            Assert.That(root.GetComponentsInChildren<UnityEngine.UI.RawImage>().Length, Is.EqualTo(6));
        }

        [Test]
        public void NumericEnergyBinding_UpdatesOnceAndUnsubscribes()
        {
            int calls = 0;
            EnergyReadModel latest = default;
            var presenter = new HudPresenter(new RecordingHudView(), model => { calls++; latest = model; });
            var state = GameState.CreateNew();
            presenter.Bind(state);
            calls = 0;
            state.SetEnergy(37, 150);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(latest.Current, Is.EqualTo(37));
            Assert.That(latest.Max, Is.EqualTo(150));
            presenter.Unbind();
            state.SetEnergy(10, 150);
            Assert.That(calls, Is.EqualTo(1));
        }
    }
}
