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
            Assert.That(root.GetComponentsInChildren<UnityEngine.UI.RawImage>().Length, Is.EqualTo(7));
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
