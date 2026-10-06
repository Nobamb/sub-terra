using NUnit.Framework;
using System.Reflection;
using SubTerra.App.Save;
using SubTerra.App.UI.HUD;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

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
            view.SetPowered(true, true);
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
            Assert.That(clock.Find("Window/Content/Readout/Label").GetComponent<TMP_Text>().text,
                Is.EqualTo(LocalizationService.Get("mine_reset.clock.label")));
            Assert.That(clock.Find("Window/Content/Readout/Digits").GetComponentsInChildren<UnityEngine.UI.Image>(), Has.Length.EqualTo(46));
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
            var rects = clock.Find("Window/Content/Readout/Digits").GetComponentsInChildren<RectTransform>();
            var positions = new Vector2[rects.Length];
            for (var i = 0; i < rects.Length; i++) positions[i] = rects[i].anchoredPosition;
            view.SetFormattedClock(value);
            for (var i = 0; i < 6; i++)
            {
                var digit = clock.Find("Window/Content/Readout/Digits/Digit" + i);
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
                Assert.That(clock.Find("Window/Content/Readout/Digits/Colon" + i).GetComponent<UnityEngine.UI.Image>().color.a, Is.EqualTo(1f));
        }

        [Test]
        public void RepeatedRefresh_DoesNotAdvanceTransition_AndDisableClearsIt()
        {
            view.SetFormattedClock(MineResetService.FormatClock(1801));
            view.SetFormattedClock(MineResetService.FormatClock(1800));
            var segment = clock.Find("Window/Content/Readout/Digits/Digit0/Segment0").GetComponent<UnityEngine.UI.Image>();
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
            var images = clock.Find("Window/Content/Readout/Digits").GetComponentsInChildren<UnityEngine.UI.Image>();
            var colors = new Color[images.Length];
            for (var i = 0; i < images.Length; i++) colors[i] = images[i].color;
            view.Advance(0.875f);
            for (var i = 0; i < images.Length; i++) Assert.That(images[i].color, Is.EqualTo(colors[i]));
            Assert.That(clock.Find("Window/Content/Frame/AccentTop").GetComponent<UnityEngine.UI.Image>().color.a,
                Is.EqualTo(0.45f).Within(1e-5f));
            Assert.That(clock.Find("Window/Content/Readout/DigitGlow").GetComponent<UnityEngine.UI.Image>().color.a, Is.LessThanOrEqualTo(0.15f));
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

        [Test]
        public void Power_RepeatedTargetsAndClockValues_DoNotRestart_AndReversalIsContinuous()
        {
            view.SetPowered(false, true);
            clock.gameObject.SetActive(true);
            view.SetFormattedClock("00:30:00");
            view.SetPowered(true);
            view.Advance(0.05f);
            view.Advance(0.05f);
            var p = view.PowerProgress;
            var window = (RectTransform)clock.Find("Window");
            var size = window.sizeDelta;
            for (var i = 0; i < 20; i++)
            {
                view.SetFormattedClock("00:29:59");
                view.SetPowered(true);
            }
            Assert.That(view.PowerProgress, Is.EqualTo(p));
            Assert.That(window.sizeDelta, Is.EqualTo(size));
            view.SetPowered(false);
            Assert.That(view.PowerProgress, Is.EqualTo(p));
            Assert.That(window.sizeDelta, Is.EqualTo(size));
            view.Advance(0.025f);
            Assert.That(view.PowerProgress, Is.LessThan(p));
            p = view.PowerProgress;
            view.SetPowered(true);
            Assert.That(view.PowerProgress, Is.EqualTo(p));
            for (var i = 0; i < 8; i++) view.Advance(0.05f);
            Assert.That(view.PowerProgress, Is.EqualTo(1f));
            Assert.That(view.IsPowerAnimating, Is.False);
            Assert.That(window.sizeDelta, Is.EqualTo(new Vector2(280f, 78f)));
            Assert.That(clock.Find("Window/Content/Readout").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            AssertNoPowerEffects();
        }

        [Test]
        public void Power_DoesNotScaleContentOrTintMetal_AndOffClearsAllEffects()
        {
            var content = (RectTransform)clock.Find("Window/Content");
            var plate = content.Find("Frame/Plate").GetComponent<UnityEngine.UI.Image>();
            var bezel = content.Find("Frame/Bezel").GetComponent<UnityEngine.UI.Image>();
            var plateColor = plate.color;
            var bezelColor = bezel.color;
            view.SetFormattedClock("00:01:00");
            view.SetPowered(false);
            for (var i = 0; i < 6; i++)
            {
                view.Advance(0.05f);
                Assert.That(content.sizeDelta, Is.EqualTo(new Vector2(280f, 78f)));
                Assert.That(content.localScale, Is.EqualTo(Vector3.one));
                Assert.That(plate.color, Is.EqualTo(plateColor));
                Assert.That(bezel.color, Is.EqualTo(bezelColor));
            }
            Assert.That(view.gameObject.activeSelf, Is.False);
            Assert.That(view.isActiveAndEnabled, Is.False);
            Assert.That(view.IsPowerAnimating, Is.False);
            Assert.That(view.IsTransitioning, Is.False);
            Assert.That(view.PulseFactor, Is.EqualTo(1f));
            Assert.That(view.PowerProgress, Is.Zero);
            Assert.That(((RectTransform)clock.Find("Window")).sizeDelta, Is.EqualTo(Vector2.zero));
            Assert.That(content.Find("Readout").GetComponent<CanvasGroup>().alpha, Is.Zero);
            AssertNoPowerEffects();
        }

        [Test]
        public void Power_DisableMidAnimation_ResetsMaskAlphaAndEffects_AndFrameSpikeIsCapped()
        {
            view.SetPowered(false, true);
            clock.gameObject.SetActive(true);
            view.SetFormattedClock("00:00:59");
            view.SetPowered(true);
            view.Advance(5f);
            Assert.That(view.PowerProgress, Is.EqualTo(0.125f).Within(1e-6f));
            clock.gameObject.SetActive(false);
            InvokeLifecycle("OnDisable");
            Assert.That(view.PowerProgress, Is.Zero);
            Assert.That(view.IsPoweredOn, Is.False);
            Assert.That(clock.Find("Window/Content/Readout").GetComponent<CanvasGroup>().alpha, Is.Zero);
            AssertNoPowerEffects();
            clock.gameObject.SetActive(true);
            view.SetFormattedClock("00:09:00");
            view.SetPowered(true, true);
            Assert.That(view.Band, Is.EqualTo(MineResetClockBand.Warning));
            Assert.That(view.PowerProgress, Is.EqualTo(1f));
        }

        private void AssertNoPowerEffects()
        {
            foreach (var name in new[] { "PowerBeam", "PowerSpark", "EdgeGlowTop", "EdgeGlowBottom", "FrameFlash" })
                Assert.That(clock.Find(name).GetComponent<UnityEngine.UI.Image>().color.a, Is.Zero, name);
        }

        [Test]
        public void InitiallyOff_AndRepeatedOffAtZero_DeactivateRoot()
        {
            var overlay = host.GetComponentInChildren<MineResetClockOverlay>();
            overlay.SetSessionVisible(false);
            for (var i = 0; i < 6; i++) view.Advance(0.05f);
            Assert.That(view.gameObject.activeSelf, Is.False);
            clock.gameObject.SetActive(true);
            view.SetPowered(false);
            Assert.That(view.gameObject.activeSelf, Is.False);
            Assert.That(view.isActiveAndEnabled, Is.False);
            view.SetPowered(false);
            Assert.That(view.IsPowerAnimating, Is.False);

            var initial = MineResetClockOverlay.Create(host.transform).GetComponentInChildren<MineResetClockView>(true);
            Assert.That(initial.gameObject.activeSelf, Is.False);
            Assert.That(initial.isActiveAndEnabled, Is.False);
        }

        [Test]
        public void Power_TwoRefreshRequestsPerStep_AndChangingSeconds_AdvanceOnlyByDeltaTime()
        {
            view.SetPowered(false, true);
            clock.gameObject.SetActive(true);
            for (var i = 0; i < 20; i++)
            {
                var before = view.PowerProgress;
                for (var refresh = 0; refresh < 2; refresh++)
                {
                    view.SetFormattedClock(MineResetService.FormatClock(9000 - i));
                    view.SetPowered(true);
                }
                Assert.That(view.PowerProgress, Is.EqualTo(before));
                view.Advance(0.02f);
                Assert.That(view.PowerProgress, Is.EqualTo(System.Math.Min(1f, (i + 1) * 0.02f / MineResetClockPowerTimeline.OnDuration)).Within(1e-6f));
            }
        }

        [Test]
        public void FrameAndReadout_UseSeparateAlpha_WhileMaskContracts()
        {
            view.SetPowered(false);
            for (var i = 0; i < 3; i++) view.Advance(0.05f);
            Assert.That(view.PowerProgress, Is.InRange(0.45f, 0.55f));
            Assert.That(clock.Find("Window/Content/Frame").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(clock.Find("Window/Content/Readout").GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(((RectTransform)clock.Find("Window")).sizeDelta.y, Is.InRange(1f, 77f));
        }

        [Test]
        public void MaskPadding_StaysZeroForBeam_ExpandsWithWindow_AndRewinds()
        {
            var mask = clock.Find("Window").GetComponent<UnityEngine.UI.RectMask2D>();
            Assert.That(mask.padding, Is.EqualTo(new Vector4(-6f, -6f, -6f, -6f)));
            view.SetPowered(false, true);
            clock.gameObject.SetActive(true);
            view.SetPowered(true);
            for (var i = 0; i < 3; i++) view.Advance(0.05f);
            Assert.That(mask.padding, Is.EqualTo(Vector4.zero));
            view.Advance(0.05f);
            Assert.That(mask.padding.x, Is.InRange(-6f, -0.1f));
            view.SetPowered(false);
            view.Advance(0.028f);
            Assert.That(mask.padding, Is.EqualTo(Vector4.zero));
        }

        [Test]
        public void PartialChildDestruction_AndOverlayDestruction_DoNotAccessDestroyedReferences()
        {
            Object.DestroyImmediate(clock.Find("Window").gameObject);
            view.ResetObservation();
            view.SetFormattedClock("00:01:00");
            view.SetPowered(false);
            view.Advance(0.05f);
            clock.gameObject.SetActive(false);
            InvokeLifecycle("OnDisable");
            clock.gameObject.SetActive(true);
            InvokeLifecycle("OnEnable");
            host.GetComponentInChildren<MineResetClockOverlay>().DestroyOverlay();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void BuildFailure_ThenEnableDisableAndDestroy_HasOnlyTheInitializationError()
        {
            var failedHost = new GameObject("ClockBuildFailure");
            try
            {
                var failed = failedHost.AddComponent<MineResetClockView>();
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Mine reset clock view initialization failed:.*InvalidCastException"));
                failed.Build();
                failedHost.SetActive(false);
                failedHost.SetActive(true);
                failed.ResetObservation();
                failed.SetPowered(true);
            }
            finally { Object.DestroyImmediate(failedHost); }
            LogAssert.NoUnexpectedReceived();
        }

        private void InvokeLifecycle(string method)
        {
            typeof(MineResetClockView).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
        }
    }
}
