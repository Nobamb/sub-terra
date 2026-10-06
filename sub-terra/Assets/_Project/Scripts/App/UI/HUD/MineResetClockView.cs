using System;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    public sealed class MineResetClockView : MonoBehaviour
    {
        private readonly MineResetClockStyle style = new MineResetClockStyle();
        private readonly UnityEngine.UI.Image[] segments = new UnityEngine.UI.Image[42];
        private readonly UnityEngine.UI.Image[] dots = new UnityEngine.UI.Image[4];
        private readonly UnityEngine.UI.Image[] accents = new UnityEngine.UI.Image[2];
        private readonly byte[] masks = new byte[6];
        private UnityEngine.UI.Image glow;
        private RectTransform window;
        private UnityEngine.UI.RectMask2D windowMask;
        private CanvasGroup frameGroup, readoutGroup;
        private UnityEngine.UI.Image powerBeam, powerSpark, edgeTop, edgeBottom, frameFlash;
        private float powerProgress;
        private bool poweredOn;

        private int displaySeconds = -1;
        private bool built;

        public float PowerProgress => powerProgress;
        public bool IsPoweredOn => poweredOn;
        public bool IsPowerAnimating => poweredOn ? powerProgress < 1f : powerProgress > 0f;
        public int DisplaySeconds => displaySeconds;
        public MineResetClockBand Band => style.Band;
        public bool IsTransitioning => style.IsTransitioning;
        public float PulseFactor => style.PulseFactor;

        public void Build()
        {
            if (built) return;
            try
            {
                var root = (RectTransform)transform;
                window = CreateRect(root, "Window", Vector2.zero, new Vector2(280f, 78f));
                windowMask = window.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                var content = CreateRect(window, "Content", Vector2.zero, new Vector2(280f, 78f));
                var frame = CreateRect(content, "Frame", Vector2.zero, new Vector2(280f, 78f));
                frameGroup = CreateGroup(frame);
                var readout = CreateRect(content, "Readout", Vector2.zero, new Vector2(280f, 78f));
                readoutGroup = CreateGroup(readout);
                accents[0] = CreateImage(frame, "FrameGlow", Vector2.zero, new Vector2(292f, 90f),
                    Color.clear, SubTerra.App.UI.Sell.ResourceSellArt.SoftRect());
                CreateImage(frame, "Plate", Vector2.zero, new Vector2(280f, 78f),
                    new Color(0.035f, 0.055f, 0.075f, 1f), MineResetClockFrameArt.PlateFill());
                CreateImage(frame, "Bezel", Vector2.zero, new Vector2(280f, 78f),
                    new Color(0.30f, 0.38f, 0.43f, 1f), MineResetClockFrameArt.Rim());
                CreateImage(frame, "Display", Vector2.zero, new Vector2(270f, 68f),
                    new Color(0.015f, 0.03f, 0.06f, 0.96f), MineResetClockFrameArt.PlateFill());
                accents[1] = CreateImage(frame, "AccentTop", Vector2.zero, new Vector2(280f, 78f),
                    Color.white, MineResetClockFrameArt.Inner());
                glow = CreateImage(readout, "DigitGlow", new Vector2(0f, -13f), new Vector2(250f, 58f),
                    Color.clear, MineResetPopupArt.Soft("clock-digits", 0.1f));
                for (var i = 0; i < 4; i++)
                {
                    var bracket = CreateImage(frame, "Bracket" + i,
                        new Vector2(i % 2 == 0 ? -130f : 130f, i < 2 ? 32f : -32f),
                        new Vector2(20f, 14f), new Color(0.54f, 0.64f, 0.70f, 1f), MineResetClockFrameArt.CornerPlate());
                    bracket.rectTransform.localScale = new Vector3(i % 2 == 0 ? 1f : -1f, i < 2 ? 1f : -1f, 1f);
                }

                var digits = CreateRect(readout, "Digits", new Vector2(0f, -13f), new Vector2(220f, 36f));
                for (var i = 0; i < 6; i++)
                {
                    var x = -96f + i * 32f + (i / 2) * 16f;
                    var digit = CreateRect(digits, "Digit" + i, new Vector2(x, 0f), new Vector2(24f, 36f));
                    for (var j = 0; j < 7; j++)
                    {
                        var horizontal = j == 0 || j == 3 || j == 6;
                        Vector2 position;
                        if (horizontal) position = new Vector2(0f, j == 0 ? 16f : j == 3 ? -16f : 0f);
                        else position = new Vector2(j == 1 || j == 2 ? 10f : -10f, j == 1 || j == 5 ? 8f : -8f);
                        segments[i * 7 + j] = CreateImage(digit, "Segment" + j, position,
                            horizontal ? new Vector2(20f, 4f) : new Vector2(4f, 12f),
                            Color.clear, MineResetPopupArt.Segment(!horizontal));
                    }
                }
                for (var i = 0; i < 4; i++)
                    dots[i] = CreateImage(digits, "Colon" + i, new Vector2(i < 2 ? -40f : 40f, i % 2 == 0 ? 7f : -7f),
                        new Vector2(4f, 4f), Color.white);
                powerBeam = CreateImage(root, "PowerBeam", Vector2.zero, Vector2.zero, Color.clear);
                powerSpark = CreateImage(root, "PowerSpark", Vector2.zero, new Vector2(12f, 12f),
                    Color.clear, MineResetPopupArt.Soft("clock-power", 0.1f));
                edgeTop = CreateImage(root, "EdgeGlowTop", Vector2.zero, Vector2.zero,
                    Color.clear, MineResetPopupArt.Soft("clock-power", 0.1f));
                edgeBottom = CreateImage(root, "EdgeGlowBottom", Vector2.zero, Vector2.zero,
                    Color.clear, MineResetPopupArt.Soft("clock-power", 0.1f));
                frameFlash = CreateImage(root, "FrameFlash", Vector2.zero, new Vector2(280f, 78f),
                    Color.clear, MineResetClockFrameArt.Inner());
                built = true;
                SetFormattedClock("03:00:00");
            }
            catch (Exception exception)
            {
                built = false;
                Debug.LogError("[SubTerra] Mine reset clock view initialization failed: " + exception);
            }
        }

        public void SetFormattedClock(string clock)
        {
            if (!built || clock == null || clock.Length != 8 || clock[2] != ':' || clock[5] != ':') return;
            for (var i = 0; i < 8; i++)
                if (i != 2 && i != 5 && (clock[i] < '0' || clock[i] > '9')) return;
            var hours = (clock[0] - '0') * 10 + clock[1] - '0';
            var minutes = (clock[3] - '0') * 10 + clock[4] - '0';
            var seconds = (clock[6] - '0') * 10 + clock[7] - '0';
            if (minutes >= 60 || seconds >= 60) return;
            var total = hours * 3600 + minutes * 60 + seconds;
            if (!style.Observe(total)) return;
            displaySeconds = total;
            for (var i = 0; i < 6; i++)
                masks[i] = SevenSegmentGlyph.GetMask(clock[i + i / 2] - '0');
            Apply();
        }

        public void SetPowered(bool on, bool immediate = false)
        {
            if (!built) return;
            if (!on && powerProgress == 0f)
            {
                poweredOn = false;
                HidePoweredOff();
                return;
            }
            if (poweredOn == on && !immediate) return;
            poweredOn = on;
            if (immediate) powerProgress = on ? 1f : 0f;
            Apply();
            if (!on && powerProgress == 0f) HidePoweredOff();
        }

        public void Advance(float dt)
        {
            if (!built || !isActiveAndEnabled || dt <= 0f) return;
            if (displaySeconds >= 0) style.Advance(dt);
            if (IsPowerAnimating)
                powerProgress = MineResetClockPowerTimeline.Advance(powerProgress, poweredOn, Mathf.Min(dt, 1f / 20f));
            Apply();
            if (!poweredOn && powerProgress == 0f) HidePoweredOff();
        }

        private void HidePoweredOff()
        {
            if (!gameObject.activeSelf) return;
            ResetPower();
            ResetObservation();
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (IsPowerAnimating || style.IsTransitioning || style.IsPulsing) Advance(Mathf.Min(Time.unscaledDeltaTime, 1f / 20f));
        }

        private void OnEnable() { ResetObservation(); }
        private void OnDisable() { ResetPower(); ResetObservation(); }
        private void OnDestroy()
        {
            poweredOn = false;
            powerProgress = 0f;
            built = false;
            style.Reset();
            displaySeconds = -1;
        }

        private void ResetPower()
        {
            poweredOn = false;
            powerProgress = 0f;
            if (built) ApplyPower();
        }

        public void ResetObservation()
        {
            style.Reset();
            displaySeconds = -1;
            if (built) Apply();
        }

        private void Apply()
        {
            if (!built || glow == null || accents[0] == null || accents[1] == null) return;
            var color = style.DisplayColor;
            for (var i = 0; i < segments.Length; i++)
            {
                color.a = (masks[i / 7] & (1 << (i % 7))) != 0 ? 1f : 0.08f;
                if (segments[i] != null) segments[i].color = color;
            }
            color.a = 1f;
            for (var i = 0; i < dots.Length; i++)
                if (dots[i] != null) dots[i].color = color;
            color.a = style.PulseFactor;
            accents[1].color = color;
            color.a = 0.18f * style.PulseFactor;
            accents[0].color = color;
            color.a = 0.12f * style.PulseFactor;
            glow.color = color;
            ApplyPower();
        }

        private void ApplyPower()
        {
            if (!built || window == null || windowMask == null || frameGroup == null || readoutGroup == null
                || powerBeam == null || powerSpark == null || edgeTop == null || edgeBottom == null || frameFlash == null) return;
            var pose = MineResetClockPowerTimeline.Evaluate(powerProgress);
            window.sizeDelta = new Vector2(280f * pose.WindowWidth01, 78f * pose.WindowHeight01);
            var padding = -6f * Mathf.Clamp01((powerProgress - 0.4f) / 0.45f);
            windowMask.padding = new Vector4(padding, padding, padding, padding);
            frameGroup.alpha = pose.FrameAlpha;
            readoutGroup.alpha = pose.ReadoutAlpha;
            var color = Color.Lerp(style.DisplayColor, Color.white, 0.6f);
            powerBeam.rectTransform.sizeDelta = new Vector2(280f * pose.BeamWidth01, pose.BeamHeightPx);
            color.a = pose.BeamAlpha;
            powerBeam.color = color;
            color.a = pose.SparkAlpha;
            powerSpark.color = color;
            var edgeSize = new Vector2(window.sizeDelta.x, 8f);
            edgeTop.rectTransform.sizeDelta = edgeBottom.rectTransform.sizeDelta = edgeSize;
            edgeTop.rectTransform.anchoredPosition = new Vector2(0f, window.sizeDelta.y * 0.5f);
            edgeBottom.rectTransform.anchoredPosition = new Vector2(0f, -window.sizeDelta.y * 0.5f);
            color.a = pose.EdgeGlow;
            edgeTop.color = edgeBottom.color = color;
            color.a = pose.SettleFlash;
            frameFlash.color = color;
        }

        private static CanvasGroup CreateGroup(RectTransform rect)
        {
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static UnityEngine.UI.Image CreateImage(Transform parent, string name, Vector2 position,
            Vector2 size, Color color, Sprite sprite = null)
        {
            var image = CreateRect(parent, name, position, size).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.sprite = sprite;
            if (sprite != null && sprite.border != Vector4.zero) image.type = UnityEngine.UI.Image.Type.Sliced;
            image.raycastTarget = false;
            return image;
        }
    }
}
