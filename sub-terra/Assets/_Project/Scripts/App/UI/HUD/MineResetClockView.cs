using System;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    public sealed class MineResetClockView : MonoBehaviour
    {
        private readonly MineResetClockStyle style = new MineResetClockStyle();
        private readonly UnityEngine.UI.Image[] segments = new UnityEngine.UI.Image[42];
        private readonly UnityEngine.UI.Image[] dots = new UnityEngine.UI.Image[4];
        private readonly UnityEngine.UI.Image[] accents = new UnityEngine.UI.Image[4];
        private readonly byte[] masks = new byte[6];
        private UnityEngine.UI.Image glow;
        private int displaySeconds = -1;
        private bool built;

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
                var steel = gameObject.AddComponent<UnityEngine.UI.Image>();
                steel.color = new Color(0.16f, 0.22f, 0.27f, 1f);
                steel.raycastTarget = false;
                CreateImage(root, "Bezel", Vector2.zero, new Vector2(276f, 74f),
                    new Color(0.035f, 0.065f, 0.085f, 1f));
                CreateImage(root, "Display", Vector2.zero, new Vector2(270f, 68f),
                    new Color(0.015f, 0.03f, 0.06f, 0.96f));
                glow = CreateImage(root, "DigitGlow", new Vector2(0f, -13f), new Vector2(250f, 58f),
                    Color.clear, MineResetPopupArt.Soft("clock-digits", 0.1f));

                accents[0] = CreateImage(root, "AccentTop", new Vector2(0f, 35f), new Vector2(264f, 1f), Color.white);
                accents[1] = CreateImage(root, "AccentBottom", new Vector2(0f, -35f), new Vector2(264f, 1f), Color.white);
                accents[2] = CreateImage(root, "AccentLeft", new Vector2(-135f, 0f), new Vector2(1f, 62f), Color.white);
                accents[3] = CreateImage(root, "AccentRight", new Vector2(135f, 0f), new Vector2(1f, 62f), Color.white);
                for (var i = 0; i < 4; i++)
                {
                    var bracket = CreateImage(root, "Bracket" + i,
                        new Vector2(i % 2 == 0 ? -133f : 133f, i < 2 ? 32f : -32f),
                        new Vector2(12f, 12f), new Color(0.38f, 0.48f, 0.54f, 1f), MineResetPopupArt.Bracket());
                    bracket.rectTransform.localScale = new Vector3(i % 2 == 0 ? 1f : -1f, i < 2 ? 1f : -1f, 1f);
                }

                var digits = CreateRect(root, "Digits", new Vector2(0f, -13f), new Vector2(220f, 36f));
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
                built = true;
                SetFormattedClock("03:00:00");
            }
            catch (Exception exception)
            {
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

        public void Advance(float dt)
        {
            if (!built || displaySeconds < 0 || !isActiveAndEnabled) return;
            style.Advance(dt);
            Apply();
        }

        private void Update()
        {
            if (style.IsTransitioning || style.IsPulsing) Advance(Time.unscaledDeltaTime);
        }

        private void OnEnable() { ResetObservation(); }
        private void OnDisable() { ResetObservation(); }

        public void ResetObservation()
        {
            style.Reset();
            displaySeconds = -1;
            if (built) Apply();
        }

        private void Apply()
        {
            var color = style.DisplayColor;
            for (var i = 0; i < segments.Length; i++)
            {
                color.a = (masks[i / 7] & (1 << (i % 7))) != 0 ? 1f : 0.08f;
                segments[i].color = color;
            }
            color.a = 1f;
            for (var i = 0; i < dots.Length; i++) dots[i].color = color;
            color.a = style.PulseFactor;
            for (var i = 0; i < accents.Length; i++) accents[i].color = color;
            color.a = 0.12f * style.PulseFactor;
            glow.color = color;
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
            image.raycastTarget = false;
            return image;
        }
    }
}
