using SubTerra.App.UI.MainMenu;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.QuitConfirm
{
    /// <summary>
    /// B-170 지상·광산 종료 연출. 부팅 화면 요소가 없으므로 부팅 시간표의 화면 부분(가로선 → 펼침)만 거꾸로 써서
    /// 화면을 위아래에서 가로선으로 접고 선을 지운다. 메인 화면 BootOverlay와 같은 셔터·가로선 모양이다.
    /// </summary>
    public sealed class QuitOutroOverlay : MonoBehaviour, IQuitOutroView
    {
        private static readonly Color ShutterColor = new Color32(0x01, 0x04, 0x06, 0xFF);
        private static readonly Color LineColor = new Color32(0xC8, 0xFB, 0xFF, 0xFF);
        private static readonly Color LineGlow = new Color(0x39 / 255f, 0xD8 / 255f, 0xEA / 255f, 0.8f);

        private readonly MainMenuBootTimeline boot = new MainMenuBootTimeline();
        private RectTransform top;
        private RectTransform bottom;
        private RectTransform line;
        private Image lineImage;

        public float QuitOutroDuration => QuitOutroTimeline.OverlayDuration;
        public float Open { get; private set; } = 1f;
        public float LineWidth { get; private set; }
        public float LineAlpha { get; private set; }

        public static QuitOutroOverlay Create(Transform parent)
        {
            var root = new GameObject("QuitOutroOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            // 투명 면으로 연출 중 모든 클릭을 막는다.
            var blocker = root.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0f);
            blocker.raycastTarget = true;
            var overlay = root.AddComponent<QuitOutroOverlay>();
            overlay.Build();
            root.SetActive(false);
            return overlay;
        }

        private void Build()
        {
            top = Part("TopShutter", ShutterColor).rectTransform;
            bottom = Part("BottomShutter", ShutterColor).rectTransform;
            lineImage = Part("ScanLine", new Color(LineColor.r, LineColor.g, LineColor.b, 0f));
            line = lineImage.rectTransform;
            line.sizeDelta = new Vector2(0f, 3f);
            var shadow = lineImage.gameObject.AddComponent<Shadow>();
            shadow.effectColor = LineGlow;
        }

        private Image Part(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public void BeginQuitOutro(bool reducedMotion)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            // 연출 감소: 접힘 없이 바로 검은 화면.
            if (reducedMotion) Apply(0f, 0f, 0f);
            else ApplyQuitOutro(0f);
        }

        public void ApplyQuitOutro(float progress)
        {
            boot.SetElapsed(QuitOutroTimeline.BootTimeAt(boot.ScreenDuration, progress));
            var frame = boot.Sample();
            Apply(frame.Open, frame.LineWidth, frame.LineAlpha);
        }

        private void Apply(float open, float lineWidth, float lineAlpha)
        {
            Open = open;
            LineWidth = lineWidth;
            LineAlpha = lineAlpha;
            var half = 0.5f * Mathf.Clamp01(open);
            top.anchorMin = new Vector2(0f, 0.5f + half);
            top.anchorMax = Vector2.one;
            top.offsetMin = top.offsetMax = Vector2.zero;
            bottom.anchorMin = Vector2.zero;
            bottom.anchorMax = new Vector2(1f, 0.5f - half);
            bottom.offsetMin = bottom.offsetMax = Vector2.zero;
            var w = Mathf.Clamp01(lineWidth);
            line.anchorMin = new Vector2(0.5f - w * 0.5f, 0.5f);
            line.anchorMax = new Vector2(0.5f + w * 0.5f, 0.5f);
            line.sizeDelta = new Vector2(0f, 3f);
            var c = lineImage.color;
            c.a = Mathf.Clamp01(lineAlpha);
            lineImage.color = c;
        }
    }
}
