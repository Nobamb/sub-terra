using System.Collections;
using NUnit.Framework;
using SubTerra.App.UI.HUD;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class PromptB106HudPlayModeTests
    {
        [UnityTest]
        public IEnumerator HealthTrail_RemainsVisibleDuringRegenerationAndResetsOnDisable()
        {
#if UNITY_EDITOR
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/BasicHUD.prefab"));
            try
            {
                var hud = root.GetComponent<BasicHudView>();
                hud.SetHealthLevel(100, 100);
                hud.SetHealthLevel(60, 100);
                var gauge = root.transform.Find("HealthGauge");
                var fill = gauge.Find("Fill").GetComponent<UnityEngine.UI.Image>();
                var trail = gauge.Find("DamageTrail").GetComponent<UnityEngine.UI.Image>();
                var color = trail.transform.Find("TrailColor").GetComponent<UnityEngine.UI.Image>();
                Assert.That(fill.fillAmount, Is.EqualTo(0.6f));
                Assert.That(color.color.g, Is.GreaterThan(0.6f));
                Assert.That(trail.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic, Is.False);
                for (int i = 1; i <= 3; i++)
                {
                    yield return new WaitForSecondsRealtime(0.08f);
                    hud.SetHealthLevel(60 + i, 100);
                    Assert.That(trail.fillAmount, Is.GreaterThan(fill.fillAmount));
                }
                yield return new WaitForSecondsRealtime(0.65f);
                Assert.That(trail.fillAmount, Is.EqualTo(fill.fillAmount).Within(0.001f));
                hud.SetHealthLevel(40, 100);
                root.SetActive(false);
                Assert.That(trail.fillAmount, Is.EqualTo(0.4f));
            }
            finally { Object.Destroy(root); }
#else
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator EnergyGauge_DecreasesOverHalfSecond_RetargetsAndResets()
        {
#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/BasicHUD.prefab");
            var root = Object.Instantiate(prefab);
            try
            {
                var gauge = root.transform.Find("EnergyGauge").GetComponent<HudGaugeView>();
                gauge.SetValue(100, 100);
                Assert.That(gauge.DisplayedFraction, Is.EqualTo(1f));
                gauge.SetValue(0, 100);
                Assert.That(gauge.DisplayedFraction, Is.EqualTo(1f), "Decrease must not snap.");
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(gauge.DisplayedFraction, Is.InRange(0.1f, 0.95f));
                float before = gauge.DisplayedFraction;
                gauge.SetValue(10, 100);
                Assert.That(gauge.DisplayedFraction, Is.EqualTo(before), "Retarget must start at displayed value.");
                yield return new WaitForSecondsRealtime(0.6f);
                Assert.That(gauge.DisplayedFraction, Is.EqualTo(0.1f).Within(0.001f));
                gauge.SetValue(200, 100);
                Assert.That(gauge.DisplayedFraction, Is.EqualTo(1f));
                gauge.SetValue(0, 100);
                root.SetActive(false);
                root.SetActive(true);
                gauge.SetValue(25, 100);
                Assert.That(gauge.DisplayedFraction, Is.EqualTo(0.25f));
                gauge.SetValue(0, 0);
                yield return new WaitForSecondsRealtime(0.6f);
                Assert.That(gauge.DisplayedFraction, Is.Zero);
            }
            finally { Object.Destroy(root); }
#else
            yield return null;
#endif
        }
    }
}
