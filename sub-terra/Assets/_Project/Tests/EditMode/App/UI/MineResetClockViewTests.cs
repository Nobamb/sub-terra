using NUnit.Framework;
using System.Reflection;
using SubTerra.App.Save;
using SubTerra.App.UI.HUD;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    public sealed class MineResetClockViewTests
    {
        private GameObject host;
        private MineResetClockView view;
        private RectTransform clock;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("ClockViewTestHost", typeof(RectTransform));
            var overlay = MineResetClockOverlay.Create(host.transform);
            clock = (RectTransform)overlay.transform.Find("ClockRoot");
            clock.gameObject.SetActive(true);
            view = clock.GetComponent<MineResetClockView>();
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(host); }

        [Test]
        public void Overlay_PreservesTopPlacementAndCanvas_AndUsesSmallLocalizedTitle()
        {
            Assert.That(clock.anchorMin, Is.EqualTo(new Vector2(0.5f, 1f)));
            Assert.That(clock.anchorMax, Is.EqualTo(clock.anchorMin));
            Assert.That(clock.pivot, Is.EqualTo(clock.anchorMin));
            Assert.That(clock.anchoredPosition, Is.EqualTo(new Vector2(0f, -12f)));
            Assert.That(clock.sizeDelta, Is.EqualTo(new Vector2(280f, 78f)));
            Assert.That(clock.GetComponentInParent<Canvas>().sortingOrder, Is.EqualTo(900));
            Assert.That(clock.GetComponentInParent<Canvas>().pixelPerfect, Is.True);
            Assert.That(clock.Find("Label").GetComponent<TMP_Text>().text,
                Is.EqualTo(LocalizationService.Get("mine_reset.clock.label")));
            Assert.That(clock.Find("Digits").GetComponentsInChildren<UnityEngine.UI.Image>(), Has.Length.EqualTo(46));
            foreach (var graphic in clock.GetComponentsInChildren<UnityEngine.UI.Graphic>())
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
        }

        [TestCase("00:00:00")]
        [TestCase("03:00:00")]
        [TestCase("02:59:59")]
        [TestCase("00:09:09")]
        public void Display_HasCorrectLitMasksAndFixedPositions(string value)
        {
            view.SetFormattedClock("03:00:00");
            var rects = clock.Find("Digits").GetComponentsInChildren<RectTransform>();
            var positions = new Vector2[rects.Length];
            for (var i = 0; i < rects.Length; i++) positions[i] = rects[i].anchoredPosition;
            view.SetFormattedClock(value);
            for (var i = 0; i < 6; i++)
            {
                var digit = clock.Find("Digits/Digit" + i);
                var mask = SevenSegmentGlyph.GetMask(value[i + i / 2] - '0');
                for (var j = 0; j < 7; j++)
                {
                    var image = digit.Find("Segment" + j).GetComponent<UnityEngine.UI.Image>();
                    Assert.That(image.color.a, Is.EqualTo((mask & (1 << j)) != 0 ? 1f : 0.08f));
                    Assert.That(image.sprite, Is.Not.Null);
                }
            }
            for (var i = 0; i < rects.Length; i++) Assert.That(rects[i].anchoredPosition, Is.EqualTo(positions[i]));
            for (var i = 0; i < 4; i++)
                Assert.That(clock.Find("Digits/Colon" + i).GetComponent<UnityEngine.UI.Image>().color.a, Is.EqualTo(1f));
        }

        [Test]
        public void RepeatedRefresh_DoesNotAdvanceTransition_AndDisableClearsIt()
        {
            view.SetFormattedClock(MineResetService.FormatClock(1801));
            view.SetFormattedClock(MineResetService.FormatClock(1800));
            var segment = clock.Find("Digits/Digit0/Segment0").GetComponent<UnityEngine.UI.Image>();
            var before = segment.color;
            for (var i = 0; i < 20; i++) view.SetFormattedClock("00:30:00");
            Assert.That(segment.color, Is.EqualTo(before));
            view.Advance(0.75f);
            Assert.That(segment.color, Is.Not.EqualTo(before));
            clock.gameObject.SetActive(false);
            InvokeLifecycle("OnDisable");
            Assert.That(view.IsTransitioning, Is.False);
            Assert.That(view.PulseFactor, Is.EqualTo(1f));
            for (var i = 0; i < 5; i++)
            {
                clock.gameObject.SetActive(true);
                InvokeLifecycle("OnEnable");
                view.SetFormattedClock("00:30:00");
                Assert.That(view.IsTransitioning, Is.False);
                Assert.That(segment.color, Is.EqualTo(MineResetClockStyle.WarningColor));
                clock.gameObject.SetActive(false);
                InvokeLifecycle("OnDisable");
            }
        }

        [Test]
        public void Pulse_ChangesOnlyFrameAndGlow_NotSegmentsOrColons()
        {
            view.SetFormattedClock("00:01:00");
            var images = clock.Find("Digits").GetComponentsInChildren<UnityEngine.UI.Image>();
            var colors = new Color[images.Length];
            for (var i = 0; i < images.Length; i++) colors[i] = images[i].color;
            view.Advance(0.875f);
            for (var i = 0; i < images.Length; i++) Assert.That(images[i].color, Is.EqualTo(colors[i]));
            Assert.That(clock.Find("AccentTop").GetComponent<UnityEngine.UI.Image>().color.a,
                Is.EqualTo(0.45f).Within(1e-5f));
            Assert.That(clock.Find("DigitGlow").GetComponent<UnityEngine.UI.Image>().color.a, Is.LessThanOrEqualTo(0.15f));
            clock.gameObject.SetActive(false);
            InvokeLifecycle("OnDisable");
            clock.gameObject.SetActive(true);
            InvokeLifecycle("OnEnable");
            view.SetFormattedClock("00:00:59");
            Assert.That(view.PulseFactor, Is.EqualTo(1f));
            view.SetFormattedClock("03:00:00");
            Assert.That(view.Band, Is.EqualTo(MineResetClockBand.Normal));
            Assert.That(view.PulseFactor, Is.EqualTo(1f));
        }

        [TestCase("00:10:00", "00:09:59")]
        [TestCase("00:01:00", "00:00:59")]
        public void DigitCarry_UsesExistingClockFloor(string before, string after)
        {
            view.SetFormattedClock(before);
            var seconds = view.DisplaySeconds;
            view.SetFormattedClock(MineResetService.FormatClock(seconds - 0.01));
            Assert.That(view.DisplaySeconds, Is.EqualTo(seconds - 1));
            view.SetFormattedClock(after);
            Assert.That(view.DisplaySeconds, Is.EqualTo(seconds - 1));
        }

        [TestCase(null)] [TestCase("")] [TestCase("0:00:00")]
        [TestCase("00:60:00")] [TestCase("00:00:60")] [TestCase("xx:00:00")]
        public void InvalidClock_PreservesPreviousDisplay(string value)
        {
            view.SetFormattedClock("00:09:09");
            view.SetFormattedClock(value);
            Assert.That(view.DisplaySeconds, Is.EqualTo(549));
        }

        private void InvokeLifecycle(string method)
        {
            typeof(MineResetClockView).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
        }
    }
}
