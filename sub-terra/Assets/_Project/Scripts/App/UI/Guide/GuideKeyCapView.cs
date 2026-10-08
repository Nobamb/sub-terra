using System.Collections.Generic;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Guide
{
    /// <summary>키캡 한 개. 눌림(0~1)에 따라 면·글자 색만 바꾸고 크기는 건드리지 않는다.</summary>
    public sealed class GuideKeyCapView : MonoBehaviour
    {
        private static readonly Color Face = new Color(0.07f, 0.12f, 0.17f, 1f);
        private static readonly Color Edge = new Color(0.62f, 0.74f, 0.80f, 0.95f);
        private static readonly Color Down = new Color(0.42f, 0.94f, 1f, 1f);

        private Image face;
        private Image border;
        private Graphic content;
        private float lastPress = -1f;

        public RectTransform Rect => (RectTransform)transform;
        public string Label { get; private set; } = string.Empty;
        public float PressLevel => lastPress < 0f ? 0f : lastPress;

        public void SetPress(float press)
        {
            press = Mathf.Clamp01(press);
            if (Mathf.Approximately(press, lastPress))
            {
                return;
            }

            lastPress = press;
            face.color = Color.Lerp(Face, Down, press);
            border.color = Color.Lerp(Edge, Color.white, press);
            if (content != null)
            {
                content.color = Color.Lerp(Color.white, ResourceSellUi.Navy, press);
            }
        }

        /// <summary>키캡 너비(글자 수에 맞춰 늘어난다).</summary>
        public static float WidthFor(string label, float height)
        {
            if (string.IsNullOrEmpty(label))
            {
                return height;
            }

            return Mathf.Max(height, label.Length * height * 0.46f + height * 0.55f);
        }

        /// <summary>왼쪽 위 (x, y) 기준으로 키캡 하나를 만든다.</summary>
        internal static GuideKeyCapView Create(Transform parent, string label, bool mouse, float x, float y, float height,
            TMP_FontAsset font)
        {
            var width = mouse ? height * 0.9f : WidthFor(label, height);
            var rect = ResourceSellUi.Place(parent, mouse ? "KeyMouse" : "Key_" + label, x, y, width, height);
            var view = rect.gameObject.AddComponent<GuideKeyCapView>();
            view.Label = mouse ? "MOUSE" : label;
            view.face = ResourceSellUi.Image(rect, "Face", ResourceSellArt.ChamferFill(), Face);
            view.border = ResourceSellUi.Image(rect, "Border", ResourceSellArt.ChamferOutline(), Edge);
            if (mouse)
            {
                var iconRect = ResourceSellUi.Centered(rect, "Mouse", Vector2.zero, new Vector2(height * 0.5f, height * 0.66f));
                var icon = ResourceSellUi.AddImage(iconRect, GameGuideArt.Mouse(), Color.white);
                icon.preserveAspect = true;
                view.content = icon;
            }
            else
            {
                var labelRect = ResourceSellUi.Centered(rect, "Label", Vector2.zero, new Vector2(width, height));
                var text = ResourceSellUi.Text(labelRect, font, height * 0.52f, FontStyles.Bold, Color.white,
                    TextAlignmentOptions.Center);
                text.text = label;
                view.content = text;
            }

            view.lastPress = -1f;
            view.SetPress(0f);
            return view;
        }

        /// <summary>
        /// "A D / ← →" 같은 표기를 왼쪽부터 이어 붙인다. 사용한 폭을 돌려준다.
        /// 구분 기호는 어두운 글자, 키캡 사이 간격은 6이다.
        /// </summary>
        internal static float BuildRow(Transform parent, string spec, float x, float y, float height, TMP_FontAsset font,
            List<GuideKeyCapView> caps)
        {
            var tokens = GuideKeyTokens.Parse(spec);
            var cursor = x;
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                switch (token.Kind)
                {
                    case GuideKeyTokenKind.Key:
                    case GuideKeyTokenKind.Mouse:
                        var cap = Create(parent, token.Label, token.Kind == GuideKeyTokenKind.Mouse, cursor, y, height, font);
                        if (caps != null)
                        {
                            caps.Add(cap);
                        }

                        cursor += cap.Rect.sizeDelta.x + 6f;
                        break;
                    default:
                        var glyph = token.Kind == GuideKeyTokenKind.Slash ? "/" : token.Label;
                        var width = height * 0.6f;
                        var text = ResourceSellUi.Label(parent, "Sep", cursor, y, width, height, font, height * 0.55f,
                            FontStyles.Bold, ResourceSellUi.TextMuted, TextAlignmentOptions.Center);
                        text.text = glyph;
                        cursor += width + 6f;
                        break;
                }
            }

            return Mathf.Max(0f, cursor - x - 6f);
        }

        /// <summary>BuildRow가 만들 폭을 미리 계산한다(오른쪽 정렬·가운데 정렬용).</summary>
        internal static float MeasureRow(string spec, float height)
        {
            var tokens = GuideKeyTokens.Parse(spec);
            var width = 0f;
            for (var i = 0; i < tokens.Count; i++)
            {
                switch (tokens[i].Kind)
                {
                    case GuideKeyTokenKind.Key:
                        width += WidthFor(tokens[i].Label, height) + 6f;
                        break;
                    case GuideKeyTokenKind.Mouse:
                        width += height * 0.9f + 6f;
                        break;
                    default:
                        width += height * 0.6f + 6f;
                        break;
                }
            }

            return Mathf.Max(0f, width - 6f);
        }
    }
}
