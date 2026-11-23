using SubTerra.App.Audio;
using SubTerra.App.UI.Sell;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// 광부 선택 화면 버튼. Codex 홀로그램 테두리(9분할) + 어두운 바탕 + 호버 빛 + 지나가는 빛줄기.
    /// 꺼진 상태는 회색 테두리 그림으로 바꾸고 빛을 끈다. 클릭 소리는 호출자가 낸다(호버음만 붙임).
    /// </summary>
    public sealed class MinerSelectButtonView : MonoBehaviour
    {
        private static readonly Color FillOn = new Color(0.02f, 0.16f, 0.2f, 0.9f);
        private static readonly Color FillOff = new Color(0.09f, 0.11f, 0.12f, 0.85f);
        private static readonly Color TextOff = new Color(0.5f, 0.55f, 0.57f, 1f);
        private const float ShinePeriod = 1.8f;

        private UnityEngine.UI.Button button;
        private UnityEngine.UI.Image fill;
        private UnityEngine.UI.Image frame;
        private UnityEngine.UI.Image hoverGlow;
        private RectTransform shine;
        private UnityEngine.UI.Image shineImage;
        private MenuSpriteButtonSkin skin;
        private TMP_Text label;
        private Sprite frameOn;
        private Sprite frameOff;
        private bool lit = true;
        private float shineTime;
        private float width;

        public UnityEngine.UI.Button Button => button;
        public TMP_Text Label => label;
        public bool IsLit => lit;

        public static MinerSelectButtonView Create(Transform parent, string name, Vector2 position, Vector2 size, TMP_FontAsset font,
            float fontSize, UnityAction onClick, bool failWhenDisabled)
        {
            var rect = MinerSelectPanel.NewRect(parent, name, position, size);
            var view = rect.gameObject.AddComponent<MinerSelectButtonView>();
            view.Build(size, font, fontSize, onClick, failWhenDisabled);
            return view;
        }

        private void Build(Vector2 size, TMP_FontAsset font, float fontSize, UnityAction onClick, bool failWhenDisabled)
        {
            width = size.x;
            var rect = (RectTransform)transform;
            frameOn = MinerSelectArt.ButtonFrame();
            frameOff = MinerSelectArt.ButtonFrameOff();
            var multiplier = MinerSelectArt.ButtonArtHeight / Mathf.Max(1f, size.y);

            // 바탕은 테두리 안쪽으로 조금 들인다(각진 모서리 밖으로 안 나가게).
            fill = MinerSelectPanel.NewImage(rect, "Fill", ResourceSellArt.ChamferFill(), FillOn, true);
            fill.type = UnityEngine.UI.Image.Type.Sliced;
            Inset(fill.rectTransform, 6f, 5f);

            // 빛 효과는 버튼 안에서만 보이게 자른다.
            var clip = MinerSelectPanel.NewRect(rect, "Clip", Vector2.zero, size);
            Inset(clip, 8f, 6f);
            clip.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();

            hoverGlow = MinerSelectPanel.NewImage(clip, "Hover", MinerSelectArt.VerticalFade(), new Color(0.42f, 0.94f, 1f, 0f), false);
            Inset(hoverGlow.rectTransform, 0f, 0f);
            skin = rect.gameObject.AddComponent<MenuSpriteButtonSkin>();
            skin.SetOverlay(hoverGlow);
            skin.SetMaxAlpha(0.3f);

            shineImage = MinerSelectPanel.NewImage(clip, "Shine", MinerSelectArt.Shine(), new Color(1f, 1f, 1f, 0f), false);
            shine = shineImage.rectTransform;
            shine.sizeDelta = new Vector2(size.y * 1.6f, size.y * 1.4f);

            frame = MinerSelectPanel.NewImage(rect, "Frame", frameOn != null ? frameOn : ResourceSellArt.ChamferOutline(),
                frameOn != null ? Color.white : MinerSelectArt.Cyan, false);
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = frameOn != null ? multiplier : 1f;
            Inset(frame.rectTransform, 0f, 0f);

            label = MinerSelectPanel.NewText(rect, "Label", font, string.Empty, fontSize, Color.white, Vector2.zero,
                new Vector2(size.x - 20f, size.y - 8f));

            button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            button.targetGraphic = fill;
            if (onClick != null) button.onClick.AddListener(onClick);
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(button);
            UiSfx.Attach(button, SfxId.None, hover: true, failWhenDisabled: failWhenDisabled);
        }

        /// <summary>켜짐(청록·빛) / 꺼짐(회색). interactable과 따로 둘 수 있다(확정 뒤에는 켜진 모양으로 잠금).</summary>
        public void SetState(bool interactable, bool showLit)
        {
            if (button != null) button.interactable = interactable;
            lit = showLit;
            if (fill != null) fill.color = showLit ? FillOn : FillOff;
            if (frame != null)
            {
                if (frameOn != null)
                {
                    frame.sprite = showLit || frameOff == null ? frameOn : frameOff;
                    frame.color = Color.white;
                }
                else
                {
                    frame.color = showLit ? MinerSelectArt.Cyan : new Color(0.5f, 0.56f, 0.58f, 0.55f);
                }
            }

            if (label != null) label.color = showLit ? Color.white : TextOff;
            if (skin != null) skin.enabled = interactable;
            if (!interactable && hoverGlow != null) hoverGlow.color = new Color(0.42f, 0.94f, 1f, 0f);
        }

        private void Update()
        {
            if (shine == null) return;
            var hovered = skin != null && skin.enabled && skin.IsHighlighted;
            // 켜진 버튼은 가끔, 올리면 자주 빛줄기가 지나간다.
            var period = hovered ? ShinePeriod * 0.55f : ShinePeriod * 2.2f;
            shineTime += Time.unscaledDeltaTime;
            if (shineTime > period) shineTime -= period;
            var t = Mathf.Clamp01(shineTime / 0.6f);
            var span = width + shine.sizeDelta.x;
            shine.anchoredPosition = new Vector2(-span * 0.5f + span * t, 0f);
            var alpha = lit ? (hovered ? 0.35f : 0.16f) * Mathf.Sin(t * Mathf.PI) : 0f;
            shineImage.color = new Color(1f, 1f, 1f, alpha);
        }

        private static void Inset(RectTransform rect, float x, float y)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(x, y);
            rect.offsetMax = new Vector2(-x, -y);
        }
    }
}
