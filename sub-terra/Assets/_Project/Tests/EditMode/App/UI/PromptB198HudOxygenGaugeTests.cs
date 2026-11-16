using NUnit.Framework;
using SubTerra.App.UI.HUD;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB198HudOxygenGaugeTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/BasicHUD.prefab";
        private GameObject instance;
        private BasicHudView hud;

        [SetUp]
        public void SetUp()
        {
            instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            hud = instance.GetComponent<BasicHudView>();
            hud.ApplyCompactLayout();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(instance);
        }

        private RectTransform Rect(string path) => (RectTransform)instance.transform.Find(path);

        private static float Top(RectTransform r) => -r.anchoredPosition.y;
        private static float Bottom(RectTransform r) => Top(r) + r.sizeDelta.y;

        [Test]
        public void OxygenGauge_FillMatchesCurrentOverMaximum()
        {
            var fill = Rect("OxygenGauge/Fill").GetComponent<UnityEngine.UI.Image>();
            hud.SetOxygenLevel(100f, 100f);
            Assert.That(fill.fillAmount, Is.EqualTo(1f));
            hud.SetOxygenLevel(37.5f, 100f);
            // 감소는 전력과 같은 0.5초 보간이라 즉시 줄지 않고 Update에서 목표로 내려간다.
            Assert.That(hud.OxygenGauge.DisplayedFraction, Is.EqualTo(1f));
            typeof(HudGaugeView).GetMethod("Update",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(hud.OxygenGauge, null);
            Assert.That(hud.OxygenGauge.DisplayedFraction, Is.LessThanOrEqualTo(1f));
            Assert.That(hud.OxygenText.text, Is.EqualTo("38 / 100"));
        }

        [Test]
        public void OxygenGauge_ZeroMaximumAndEmptyAreSafe()
        {
            var fill = Rect("OxygenGauge/Fill").GetComponent<UnityEngine.UI.Image>();
            Assert.DoesNotThrow(() => hud.SetOxygenLevel(10f, 0f));
            Assert.That(fill.fillAmount, Is.EqualTo(0f));
            hud.SetOxygenLevel(0f, 100f);
            Assert.That(fill.fillAmount, Is.EqualTo(0f));
            hud.SetOxygenLevel(100f, 100f);
            Assert.That(fill.fillAmount, Is.EqualTo(1f), "증가는 즉시 반영된다.");
        }

        [Test]
        public void OxygenGauge_DoesNotUseDamageTrail()
        {
            hud.SetOxygenLevel(100f, 100f);
            hud.SetOxygenLevel(50f, 100f);
            Assert.That(Rect("OxygenGauge/DamageTrail"), Is.Null);
        }

        [Test]
        public void OxygenFill_UsesGrayTranslucentWhiteNotEnergyArt()
        {
            var fill = Rect("OxygenGauge/Fill").GetComponent<UnityEngine.UI.Image>();
            var energy = Rect("EnergyGauge/Fill").GetComponent<UnityEngine.UI.Image>();
            Assert.That(fill.sprite, Is.Not.Null);
            Assert.That(fill.sprite, Is.Not.EqualTo(energy.sprite));
            Assert.That(fill.color.a, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(fill.color.r, Is.GreaterThan(0.8f));
            Assert.That(fill.type, Is.EqualTo(UnityEngine.UI.Image.Type.Filled));
        }

        [Test]
        public void CompactLayout_PlacesOxygenBelowEnergyInsideFrameWithoutOverlap()
        {
            var frame = Rect("HudFrame");
            var energy = Rect("EnergyGauge");
            var oxygen = Rect("OxygenGauge");
            Assert.That(Top(oxygen), Is.GreaterThanOrEqualTo(Bottom(energy)));
            Assert.That(Top(oxygen) - Top(energy), Is.EqualTo(Top(energy) - Top(Rect("HealthGauge"))), "체력↔전력 간격과 같다.");
            Assert.That(Bottom(oxygen), Is.LessThanOrEqualTo(frame.sizeDelta.y));
            Assert.That(Rect("OxygenLabel").anchoredPosition.y, Is.EqualTo(-(Top(oxygen) + 1f)));
            Assert.That(Rect("OxygenText").anchoredPosition.y, Is.EqualTo(-Top(oxygen)));
            Assert.That(oxygen.GetSiblingIndex(), Is.LessThan(Rect("OxygenText").GetSiblingIndex()));
            Assert.That(frame.sizeDelta.y, Is.EqualTo(BasicHudView.FrameHeight));
            Assert.That(((RectTransform)instance.transform).sizeDelta.y, Is.EqualTo(BasicHudView.FrameHeight));
        }

        [Test]
        public void CompactLayout_LowerRowsMovedBelowOxygenAndStayInsideFrame()
        {
            float oxygenBottom = Bottom(Rect("OxygenGauge"));
            Assert.That(Top(Rect("TopSeparator")), Is.GreaterThan(oxygenBottom));
            float frameHeight = Rect("HudFrame").sizeDelta.y;
            foreach (var name in new[] { "DepthText", "GoldText", "CargoText", "UnsettledValueText", "BottomSeparator", "StructuralStatusRow" })
            {
                var rect = Rect(name);
                Assert.That(Top(rect), Is.GreaterThan(oxygenBottom), name);
                Assert.That(Bottom(rect), Is.LessThanOrEqualTo(frameHeight), name);
            }
            Assert.That(Top(Rect("StructuralStatusRow")), Is.GreaterThan(Top(Rect("BottomSeparator"))));
        }

        [Test]
        public void OxygenIcon_SitsLeftOfOxygenGaugeAlignedWithOtherIcons()
        {
            var icon = Rect("OxygenIcon");
            Assert.That(icon, Is.Not.Null);
            var image = icon.GetComponent<UnityEngine.UI.Image>();
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.raycastTarget, Is.False);
            Assert.That(image.color.a, Is.EqualTo(0.9f).Within(0.001f), "산소 게이지와 같은 탁한 흰색 90%.");
            Assert.That(image.color.r, Is.GreaterThan(0.8f));

            var gauge = Rect("OxygenGauge");
            var label = Rect("OxygenLabel");
            float iconRight = icon.anchoredPosition.x + icon.sizeDelta.x;
            Assert.That(iconRight, Is.LessThanOrEqualTo(label.anchoredPosition.x), "라벨과 겹치지 않는다.");
            Assert.That(iconRight, Is.LessThan(gauge.anchoredPosition.x));
            Assert.That(Top(icon), Is.GreaterThanOrEqualTo(Top(gauge)));
            Assert.That(Bottom(icon), Is.LessThanOrEqualTo(Bottom(gauge)));
            // 체력·전력 아이콘(24px, x=20)과 같은 열 중심, 게이지 y보다 2px 아래.
            var energyIcon = Rect("HudIcon1");
            float center = icon.anchoredPosition.x + icon.sizeDelta.x * 0.5f;
            float energyCenter = energyIcon.anchoredPosition.x + energyIcon.sizeDelta.x * 0.5f;
            Assert.That(center, Is.EqualTo(energyCenter).Within(0.01f));
            Assert.That(Top(icon) - Top(gauge), Is.EqualTo(Top(energyIcon) - Top(Rect("EnergyGauge"))).Within(0.01f));
            Assert.That(Bottom(icon), Is.LessThan(Top(Rect("TopSeparator"))));
            Assert.That(icon.GetSiblingIndex(), Is.GreaterThan(Rect("HudFrame").GetSiblingIndex()));
        }

        [Test]
        public void ExistingGauges_StillWork()
        {
            hud.SetHealthLevel(100, 100);
            hud.SetHealthLevel(60, 100);
            Assert.That(Rect("HealthGauge/DamageTrail"), Is.Not.Null);
            Assert.That(Rect("EnergyGauge/DamageTrail"), Is.Null);
            hud.SetEnergyLevel(50f, 100);
            Assert.That(Rect("EnergyGauge/Fill").GetComponent<UnityEngine.UI.Image>().fillAmount, Is.EqualTo(0.5f));
            Assert.That(instance.transform.Find("StructuralStatusRow"), Is.Not.Null);
        }

        [Test]
        public void FormatOxygen_ClampsValues()
        {
            Assert.That(HudFormatter.FormatOxygen(-5f, 100f), Is.EqualTo("0 / 100"));
            Assert.That(HudFormatter.FormatOxygen(120f, 100f), Is.EqualTo("100 / 100"));
            Assert.That(HudFormatter.FormatOxygen(5f, 0f), Is.EqualTo("1 / 1"));
        }
    }
}
