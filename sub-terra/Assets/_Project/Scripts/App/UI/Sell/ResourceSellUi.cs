using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Sell
{
    /// <summary>판매 창 색·숫자 형식·코드 UI 생성 도우미. 좌표는 부모 왼쪽 위 기준(x 오른쪽, y 아래)이다.</summary>
    internal static class ResourceSellUi
    {
        public static readonly Color Card = new Color(0.02f, 0.035f, 0.05f, 0.98f);
        public static readonly Color Teal = new Color(0.42f, 0.94f, 1f, 1f);
        public static readonly Color TealDeep = new Color(0.10f, 0.78f, 0.85f, 1f);
        public static readonly Color Gold = new Color(1f, 0.80f, 0.30f, 1f);
        public static readonly Color GoldGlow = new Color(1f, 0.72f, 0.20f, 1f);
        public static readonly Color TextMain = new Color(0.90f, 0.97f, 1f, 1f);
        public static readonly Color TextMuted = new Color(0.62f, 0.76f, 0.82f, 1f);
        public static readonly Color TextDim = new Color(0.42f, 0.52f, 0.58f, 1f);
        public static readonly Color Error = new Color(1f, 0.50f, 0.42f, 1f);
        public static readonly Color Ok = new Color(0.50f, 1f, 0.78f, 1f);
        public static readonly Color Navy = new Color(0.02f, 0.07f, 0.11f, 1f);
        public static readonly Color Strip = new Color(0.04f, 0.09f, 0.13f, 0.92f);
        public static readonly Color RowIdle = new Color(0.035f, 0.07f, 0.10f, 0.92f);
        public static readonly Color RowActive = new Color(0.05f, 0.17f, 0.21f, 0.95f);

        public static string Number(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        public static string GoldText(int value)
        {
            return Number(value) + "G";
        }

        public static string Cargo(float value)
        {
            return value < 0f ? "0" : value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>부모 중앙 기준 RectTransform.</summary>
        public static RectTransform Centered(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = NewRect(parent, name);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>부모 왼쪽 위 기준 (x, y, w, h). 피벗은 가운데라 눌림·확대가 제자리에서 일어난다.</summary>
        public static RectTransform Place(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = NewRect(parent, name);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h * 0.5f));
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color)
        {
            var rect = NewRect(parent, name);
            Stretch(rect);
            return AddImage(rect, sprite, color);
        }

        public static Image AddImage(RectTransform rect, Sprite sprite, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
            }

            return image;
        }

        public static TMP_Text Text(
            RectTransform rect,
            TMP_FontAsset font,
            float size,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment)
        {
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            // Ellipsis는 줄 높이가 칸보다 크면 한 글자도 안 그린다. 폭이 좁은 칸만 개별로 Ellipsis를 쓴다.
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = false;
            return text;
        }

        public static TMP_Text Label(
            Transform parent,
            string name,
            float x,
            float y,
            float w,
            float h,
            TMP_FontAsset font,
            float size,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment)
        {
            return Text(Place(parent, name, x, y, w, h), font, size, style, color, alignment);
        }

        private static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : go.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
    }
}
