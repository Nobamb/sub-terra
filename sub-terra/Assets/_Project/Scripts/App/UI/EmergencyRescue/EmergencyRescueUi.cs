using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyRescue
{
    /// <summary>구출 팝업·홀로그램이 코드로 UI를 만들 때 쓰는 작은 생성 도우미.</summary>
    internal static class EmergencyRescueUi
    {
        public static readonly Color Teal = new Color(0.42f, 0.94f, 1f, 1f);
        public static readonly Color TealDeep = new Color(0.10f, 0.78f, 0.85f, 1f);
        public static readonly Color Red = new Color(1f, 0.30f, 0.26f, 1f);
        public static readonly Color TitleRed = new Color(1f, 0.46f, 0.40f, 1f);
        public static readonly Color TextMain = new Color(0.86f, 0.95f, 0.98f, 1f);
        public static readonly Color TextMuted = new Color(0.62f, 0.76f, 0.82f, 1f);

        /// <summary>부모 중앙 기준 좌표·크기를 쓰는 빈 RectTransform.</summary>
        public static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>장식 이미지. 입력은 받지 않는다.</summary>
        public static Image Image(
            Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = Rect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static TMP_Text Text(
            Transform parent,
            string name,
            TMP_FontAsset font,
            float fontSize,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 size)
        {
            RectTransform rect = Rect(parent, name, position, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = false;
            return text;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
