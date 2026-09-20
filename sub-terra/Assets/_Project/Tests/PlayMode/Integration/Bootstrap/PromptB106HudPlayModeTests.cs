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
        public IEnumerator Gauge_DecreasesOverHalfSecond_RetargetsAndResets()
        {
#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/BasicHUD.prefab");
            var root = Object.Instantiate(prefab);
            try
            {
                var gauge = root.transform.Find("HealthGauge").GetComponent<HudGaugeView>();
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
