using System;
using SubTerra.App.UI.Outpost;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// 광부 선택 카드 한 장(청록 홀로그램 테두리, 광부 그림, 받침대, 이름).
    /// 표시 값(호버·또렷함·어두움·색 차오름)은 패널이 매 프레임 넣고, 카드는 그대로 그리기만 한다.
    /// </summary>
    public sealed class MinerSelectCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public const float Width = 414f;
        public const float Height = 610f;

        private static readonly int DetailId = Shader.PropertyToID("_Detail");
        private static readonly int FillId = Shader.PropertyToID("_Fill");
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int UvRectId = Shader.PropertyToID("_UvRect");

        private UnityEngine.UI.Image fill;
        private UnityEngine.UI.Image outline;
        private UnityEngine.UI.Image glow;
        private UnityEngine.UI.Image figure;
        private UnityEngine.UI.Image beam;
        private UnityEngine.UI.Image pedestal;
        private UnityEngine.UI.Image pedestalCore;
        private TMP_Text label;
        private TMP_Text ticks;
        private UnityEngine.UI.Image[] labelLines;
        private Material material;

        public int Index { get; private set; }
        public UnityEngine.UI.Image Figure => figure;
        public TMP_Text Label => label;
        public bool UsesHologramShader => material != null;
        public float AppliedDetail { get; private set; }
        public float AppliedFill { get; private set; }
        public float AppliedBrightness { get; private set; }

        public event Action<int> PointerEntered;
        public event Action<int> PointerExited;
        public event Action<int> Clicked;

        public static MinerSelectCardView Create(Transform parent, int index, string name, Sprite art, TMP_FontAsset font)
        {
            var root = MinerSelectPanel.NewRect(parent, name, Vector2.zero, new Vector2(Width, Height));
            var card = root.gameObject.AddComponent<MinerSelectCardView>();
            card.Index = index;
            card.Build(art, font);
            return card;
        }

        private void Build(Sprite art, TMP_FontAsset font)
        {
            var root = (RectTransform)transform;

            // 바깥으로 번지는 청록 빛(호버·선택 때 밝아짐).
            glow = MinerSelectPanel.NewImage(root, "Glow", CoreCctvArt.Glow(), MinerSelectArt.Cyan, false);
            Stretch(glow.rectTransform, -22f);
            glow.type = UnityEngine.UI.Image.Type.Sliced;

            fill = MinerSelectPanel.NewImage(root, "Fill", ResourceSellArt.ChamferFill(), new Color(0.03f, 0.16f, 0.2f, 0.62f), true);
            fill.type = UnityEngine.UI.Image.Type.Sliced;
            Stretch(fill.rectTransform, 0f);

            ticks = MinerSelectPanel.NewText(root, "Ticks", font, "///", 22f, MinerSelectArt.Cyan, new Vector2(-Width * 0.5f + 46f, Height * 0.5f - 26f), new Vector2(60f, 30f));
            ticks.fontStyle = FontStyles.Bold | FontStyles.Italic;
            ticks.characterSpacing = -8f;

            beam = MinerSelectPanel.NewImage(root, "Beam", MinerSelectArt.Beam(), MinerSelectArt.Cyan, false);
            beam.rectTransform.anchoredPosition = new Vector2(0f, -20f);
            beam.rectTransform.sizeDelta = new Vector2(300f, 360f);

            figure = MinerSelectPanel.NewImage(root, "Figure", art, Color.white, false);
            figure.preserveAspect = true;
            figure.rectTransform.anchoredPosition = new Vector2(0f, 38f);
            figure.rectTransform.sizeDelta = new Vector2(440f, 440f);
            var shader = MinerSelectArt.HologramShader();
            if (shader != null)
            {
                material = new Material(shader) { name = "MinerHologram_" + Index, hideFlags = HideFlags.DontSave };
                material.SetVector(UvRectId, MinerSelectArt.UvRect(art));
                figure.material = material;
            }

            pedestal = MinerSelectPanel.NewImage(root, "Pedestal", MinerSelectArt.PedestalRing(), MinerSelectArt.Cyan, false);
            pedestal.rectTransform.anchoredPosition = new Vector2(0f, -176f);
            pedestal.rectTransform.sizeDelta = new Vector2(276f, 66f);
            pedestalCore = MinerSelectPanel.NewImage(root, "PedestalCore", MinerSelectArt.SoftDisc(), MinerSelectArt.Cyan, false);
            pedestalCore.rectTransform.anchoredPosition = new Vector2(0f, -178f);
            pedestalCore.rectTransform.sizeDelta = new Vector2(220f, 40f);

            label = MinerSelectPanel.NewText(root, "Label", font, string.Empty, 40f, Color.white, new Vector2(0f, -248f), new Vector2(220f, 60f));
            label.fontStyle = FontStyles.Bold;
            labelLines = new[]
            {
                MinerSelectPanel.NewImage(root, "LabelLineLeft", null, MinerSelectArt.Cyan, false),
                MinerSelectPanel.NewImage(root, "LabelLineRight", null, MinerSelectArt.Cyan, false)
            };
            labelLines[0].rectTransform.anchoredPosition = new Vector2(-118f, -248f);
            labelLines[1].rectTransform.anchoredPosition = new Vector2(118f, -248f);
            labelLines[0].rectTransform.sizeDelta = labelLines[1].rectTransform.sizeDelta = new Vector2(60f, 2f);

            outline = MinerSelectPanel.NewImage(root, "Outline", ResourceSellArt.ChamferOutline(), MinerSelectArt.Cyan, false);
            outline.type = UnityEngine.UI.Image.Type.Sliced;
            Stretch(outline.rectTransform, 0f);
        }

        public void SetLabel(string text)
        {
            if (label != null) label.text = text;
        }

        /// <param name="hover">0~1 마우스 올림/키보드 포커스.</param>
        /// <param name="detail">0~1 고른 카드의 또렷함.</param>
        /// <param name="brightness">안 고른 카드는 낮아진다.</param>
        /// <param name="colorFill">0~1 확정 때 원래 색 차오름.</param>
        /// <param name="flicker">0~1 홀로그램의 약한 깜빡임(1이 가장 밝음).</param>
        public void Apply(float hover, float detail, float brightness, float colorFill, float flicker)
        {
            AppliedDetail = detail;
            AppliedFill = colorFill;
            AppliedBrightness = brightness;
            var lit = Mathf.Clamp01(0.3f * hover + 0.25f * detail);

            SetAlpha(outline, (0.5f + 0.5f * lit / 0.55f) * brightness);
            SetAlpha(glow, (0.08f + 0.45f * lit) * brightness);
            SetAlpha(ticks, (0.6f + 0.4f * lit) * brightness);
            SetAlpha(beam, (0.16f + 0.22f * lit + 0.1f * detail) * brightness * flicker);
            SetAlpha(pedestal, (0.55f + 0.45f * lit) * brightness);
            SetAlpha(pedestalCore, (0.35f + 0.5f * lit) * brightness);
            if (label != null) label.color = Color.Lerp(new Color(0.55f, 0.62f, 0.66f, 1f), Color.white, brightness);
            for (var i = 0; labelLines != null && i < labelLines.Length; i++) SetAlpha(labelLines[i], 0.6f * brightness);

            if (figure == null) return;
            var figureBrightness = (0.9f + 0.18f * hover + 0.15f * detail) * brightness;
            if (material != null)
            {
                material.SetFloat(DetailId, detail);
                material.SetFloat(FillId, colorFill);
                material.SetFloat(BrightnessId, figureBrightness);
                figure.color = new Color(1f, 1f, 1f, Mathf.Lerp(flicker, 1f, colorFill));
            }
            else
            {
                // 셰이더가 없을 때: 청록 틴트 → 원래 색.
                var holo = MinerSelectArt.Cyan * Mathf.Min(1f, figureBrightness);
                var tint = Color.Lerp(holo, Color.white, colorFill);
                tint.a = Mathf.Lerp(flicker, 1f, colorFill);
                figure.color = tint;
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => PointerEntered?.Invoke(Index);

        public void OnPointerExit(PointerEventData eventData) => PointerExited?.Invoke(Index);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            Clicked?.Invoke(Index);
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
        }

        private static void Stretch(RectTransform rect, float outset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-outset, -outset);
            rect.offsetMax = new Vector2(outset, outset);
        }

        private static void SetAlpha(UnityEngine.UI.Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            var color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.color = color;
        }
    }
}
