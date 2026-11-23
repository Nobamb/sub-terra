using System;
using SubTerra.App.UI.Outpost;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>카드 한 장의 한 프레임 표시 값. 패널이 계산해서 넣는다.</summary>
    public struct MinerCardVisual
    {
        /// <summary>0~1 마우스 올림/키보드 포커스.</summary>
        public float Hover;
        /// <summary>0~1 고른 카드 강조(테두리·빛·입자량).</summary>
        public float Selected;
        /// <summary>0~1 위→아래 스캔으로 얼굴·윤곽이 드러난 정도.</summary>
        public float Reveal;
        /// <summary>0~1 위→아래 스캔으로 원래 색이 입혀진 정도.</summary>
        public float Fill;
        /// <summary>안 고른 카드는 낮아진다.</summary>
        public float Brightness;
        /// <summary>0~1 홀로그램의 약한 깜빡임(1이 가장 밝음).</summary>
        public float Flicker;
    }

    /// <summary>
    /// 광부 선택 카드 한 장(홀로그램 테두리, 반투명 안쪽·육각 격자·스캔라인, 광부 그림, 받침대·빛기둥, 입자, 스캔 줄, 이름).
    /// 표시 값은 패널이 매 프레임 넣고, 카드는 그리기와 입자 방출만 한다.
    /// </summary>
    public sealed class MinerSelectCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public const float Width = 414f;
        public const float Height = 610f;
        public const float FigureSize = 440f;
        public const float FigureY = 40f;
        public const float PedestalTopY = -166f;

        private static readonly int RevealId = Shader.PropertyToID("_Reveal");
        private static readonly int FillId = Shader.PropertyToID("_Fill");
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int GlitchId = Shader.PropertyToID("_Glitch");
        private static readonly int UvRectId = Shader.PropertyToID("_UvRect");

        private RectTransform body;
        private UnityEngine.UI.Image glow;
        private UnityEngine.UI.Image inner;
        private UnityEngine.UI.RawImage hex;
        private UnityEngine.UI.RawImage scanlines;
        private UnityEngine.UI.Image frame;
        private UnityEngine.UI.Image figure;
        private UnityEngine.UI.Image beam;
        private UnityEngine.UI.Image pedestal;
        private UnityEngine.UI.Image pedestalGlow;
        private UnityEngine.UI.Image scanBar;
        private MinerSelectParticleField particles;
        private TMP_Text label;
        private UnityEngine.UI.Image[] labelLines;
        private Material material;
        private float time;
        private float ambientCarry;
        private float sparkCarry;
        private float lastReveal;

        public int Index { get; private set; }
        public UnityEngine.UI.Image Figure => figure;
        public TMP_Text Label => label;
        public MinerSelectParticleField Particles => particles;
        public bool UsesHologramShader => material != null;
        public bool UsesFrameArt { get; private set; }
        public float AppliedReveal { get; private set; }
        public float AppliedFill { get; private set; }
        public float AppliedBrightness { get; private set; }
        public float GlowAlpha => glow != null ? glow.color.a : 0f;
        public float BodyScale => body != null ? body.localScale.x : 1f;
        /// <summary>스캔 줄이 보이는지와 위치(카드 안 y).</summary>
        public bool ScanVisible => scanBar != null && scanBar.enabled;
        public float ScanY => scanBar != null ? scanBar.rectTransform.anchoredPosition.y : 0f;

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

        /// <summary>스캔 줄 진행(0 위 ~ 1 아래)에 해당하는 카드 안 y. 셰이더의 줄 위치와 같다.</summary>
        public static float ScanLineY(float progress)
        {
            var localY = Mathf.Lerp(1.04f, -0.04f, Mathf.Clamp01(progress));
            return FigureY - FigureSize * 0.5f + localY * FigureSize;
        }

        private void Build(Sprite art, TMP_FontAsset font)
        {
            var root = (RectTransform)transform;
            // 마우스 판정은 카드 전체(투명).
            var hit = MinerSelectPanel.NewImage(root, "Hit", null, new Color(0f, 0f, 0f, 0f), true);
            Stretch(hit.rectTransform, 0f);

            // 호버 때 커지는 몸통. 판정 영역은 그대로 둔다.
            body = MinerSelectPanel.NewRect(root, "Body", Vector2.zero, new Vector2(Width, Height));

            // 고른 카드만 깎인 테두리를 따라 바깥으로 빛난다(빛 그림의 선 자리가 카드 테두리 선에 맞게 둔다).
            glow = MinerSelectPanel.NewImage(body, "Glow", MinerSelectArt.CardGlow(), MinerSelectArt.Cyan, false);
            glow.type = UnityEngine.UI.Image.Type.Sliced;
            Stretch(glow.rectTransform, -(MinerSelectArt.CardGlowPad - MinerSelectArt.CardOutlineInset));
            SetAlpha(glow, 0f);

            inner = MinerSelectPanel.NewImage(body, "Inner", MinerSelectArt.VerticalFade(), new Color(0.02f, 0.13f, 0.17f, 0.62f), false);
            Stretch(inner.rectTransform, 12f);

            hex = NewRaw(body, "Hex", CoreCctvArt.Hex(), new Color(0.42f, 0.94f, 1f, 0.07f));
            Stretch(hex.rectTransform, 14f);
            hex.uvRect = new Rect(0f, 0f, (Width - 28f) / 90f, (Height - 28f) / 51.96f);

            scanlines = NewRaw(body, "Scanlines", CoreCctvArt.Scanline(), new Color(0.42f, 0.94f, 1f, 0.12f));
            Stretch(scanlines.rectTransform, 14f);
            scanlines.uvRect = new Rect(0f, 0f, 1f, (Height - 28f) / 4f);

            beam = MinerSelectPanel.NewImage(body, "Beam", MinerSelectArt.Beam(), MinerSelectArt.Cyan, false);
            beam.rectTransform.anchoredPosition = new Vector2(0f, PedestalTopY + 160f);
            beam.rectTransform.sizeDelta = new Vector2(300f, 330f);

            pedestalGlow = MinerSelectPanel.NewImage(body, "PedestalGlow", MinerSelectArt.SoftDisc(), MinerSelectArt.Cyan, false);
            pedestalGlow.rectTransform.anchoredPosition = new Vector2(0f, PedestalTopY - 6f);
            pedestalGlow.rectTransform.sizeDelta = new Vector2(330f, 100f);

            var pedestalArt = MinerSelectArt.PedestalArt();
            pedestal = MinerSelectPanel.NewImage(body, "Pedestal", pedestalArt != null ? pedestalArt : MinerSelectArt.PedestalRing(),
                Color.white, false);
            pedestal.preserveAspect = true;
            // 그림의 윗면 타원 중심(높이의 약 37%)이 발 높이에 오게 둔다.
            pedestal.rectTransform.anchoredPosition = new Vector2(0f, PedestalTopY - 17f);
            pedestal.rectTransform.sizeDelta = pedestalArt != null ? new Vector2(270f, 135f) : new Vector2(250f, 60f);
            if (pedestalArt == null) pedestal.color = MinerSelectArt.Cyan;

            figure = MinerSelectPanel.NewImage(body, "Figure", art, Color.white, false);
            figure.preserveAspect = true;
            figure.rectTransform.anchoredPosition = new Vector2(0f, FigureY);
            figure.rectTransform.sizeDelta = new Vector2(FigureSize, FigureSize);
            var shader = MinerSelectArt.HologramShader();
            if (shader != null)
            {
                material = new Material(shader) { name = "MinerHologram_" + Index, hideFlags = HideFlags.DontSave };
                material.SetVector(UvRectId, MinerSelectArt.UvRect(art));
                figure.material = material;
            }

            particles = MinerSelectParticleField.Create(body, "Particles", new Vector2(Width, Height));

            scanBar = MinerSelectPanel.NewImage(body, "ScanLine", MinerSelectArt.ScanGlow(), MinerSelectArt.Cyan, false);
            scanBar.rectTransform.sizeDelta = new Vector2(Width - 30f, 30f);
            scanBar.enabled = false;

            var frameArt = MinerSelectArt.CardFrame();
            UsesFrameArt = frameArt != null;
            frame = MinerSelectPanel.NewImage(body, "Frame", frameArt != null ? frameArt : ResourceSellArt.ChamferOutline(),
                frameArt != null ? Color.white : MinerSelectArt.Cyan, false);
            if (frameArt == null) frame.type = UnityEngine.UI.Image.Type.Sliced;
            Stretch(frame.rectTransform, 0f);

            label = MinerSelectPanel.NewText(body, "Label", font, string.Empty, 40f, Color.white, new Vector2(0f, -266f), new Vector2(220f, 56f));
            label.fontStyle = FontStyles.Bold;
            labelLines = new[]
            {
                MinerSelectPanel.NewImage(body, "LabelLineLeft", null, MinerSelectArt.Cyan, false),
                MinerSelectPanel.NewImage(body, "LabelLineRight", null, MinerSelectArt.Cyan, false)
            };
            labelLines[0].rectTransform.anchoredPosition = new Vector2(-120f, -266f);
            labelLines[1].rectTransform.anchoredPosition = new Vector2(120f, -266f);
            labelLines[0].rectTransform.sizeDelta = labelLines[1].rectTransform.sizeDelta = new Vector2(64f, 2f);
        }

        public void SetLabel(string text)
        {
            if (label != null) label.text = text;
        }

        /// <summary>고를 때·확정할 때 받침대에서 입자를 한 번에 뿜는다.</summary>
        public void Burst(int count, bool warm)
        {
            if (particles == null) return;
            for (var i = 0; i < count; i++)
            {
                var angle = UnityEngine.Random.Range(0.15f, Mathf.PI - 0.15f);
                var speed = UnityEngine.Random.Range(90f, 220f);
                var pos = new Vector2(UnityEngine.Random.Range(-110f, 110f), PedestalTopY + UnityEngine.Random.Range(-6f, 6f));
                var vel = new Vector2(Mathf.Cos(angle) * speed * 0.7f, Mathf.Sin(angle) * speed);
                particles.Emit(pos, vel, UnityEngine.Random.Range(5f, 11f), UnityEngine.Random.Range(0.6f, 1.2f), SparkColor(warm, 1f));
            }
        }

        public void Apply(MinerCardVisual v, float dt)
        {
            time += Mathf.Max(0f, dt);
            AppliedReveal = v.Reveal;
            AppliedFill = v.Fill;
            AppliedBrightness = v.Brightness;
            var b = v.Brightness;
            var lit = Mathf.Clamp01(0.55f * v.Hover + 0.6f * v.Selected);

            if (body != null)
            {
                var grow = Mathf.Clamp01(Mathf.Max(v.Hover, v.Selected * 0.6f));
                grow = grow * grow * (3f - 2f * grow);
                var scale = Mathf.Lerp(1f, MinerSelectMotion.HoverScale, grow);
                body.localScale = new Vector3(scale, scale, 1f);
            }

            if (frame != null)
            {
                var tint = Color.Lerp(new Color(0.78f, 0.9f, 0.95f, 1f), Color.white, lit);
                tint.a = Mathf.Clamp01((0.6f + 0.4f * lit) * b * (0.92f + 0.08f * v.Flicker));
                frame.color = UsesFrameArt ? tint : new Color(MinerSelectArt.Cyan.r, MinerSelectArt.Cyan.g, MinerSelectArt.Cyan.b, tint.a);
            }

            SetAlpha(glow, 0.85f * v.Selected * b);
            SetAlpha(inner, (0.5f + 0.15f * lit) * Mathf.Lerp(0.7f, 1f, b));
            SetAlpha(hex, (0.05f + 0.06f * lit) * b);
            SetAlpha(scanlines, (0.08f + 0.06f * lit) * b);
            if (hex != null)
            {
                var r = hex.uvRect;
                hex.uvRect = new Rect(0f, -time * 0.05f, r.width, r.height);
            }

            if (scanlines != null)
            {
                var r = scanlines.uvRect;
                scanlines.uvRect = new Rect(0f, -time * 1.5f, r.width, r.height);
            }

            SetAlpha(beam, (0.14f + 0.22f * lit + 0.1f * v.Reveal) * b * v.Flicker);
            SetAlpha(pedestal, (0.7f + 0.3f * lit) * b);
            SetAlpha(pedestalGlow, (0.25f + 0.45f * lit) * b);
            if (label != null) label.color = Color.Lerp(new Color(0.5f, 0.58f, 0.62f, 1f), Color.white, b);
            for (var i = 0; labelLines != null && i < labelLines.Length; i++) SetAlpha(labelLines[i], 0.6f * b);

            ApplyFigure(v, b);
            ApplyScan(v, dt, b);
            EmitAmbient(v, dt, b);
            if (particles != null) particles.Tick(dt);
            lastReveal = v.Reveal;
        }

        private void ApplyFigure(MinerCardVisual v, float b)
        {
            if (figure == null) return;
            var figureBrightness = (0.95f + 0.2f * v.Hover + 0.1f * v.Reveal) * b;
            if (material != null)
            {
                material.SetFloat(RevealId, v.Reveal);
                material.SetFloat(FillId, v.Fill);
                material.SetFloat(BrightnessId, figureBrightness);
                // 고르지 않은 실루엣은 가끔 지직거리고 깜빡인다. 고르면(드러나면) 흔들림 없이 또렷하게 선다.
                var steady = Mathf.Max(v.Reveal, v.Fill);
                material.SetFloat(GlitchId, Mathf.Lerp(0.35f, 0f, steady));
                figure.color = new Color(1f, 1f, 1f, Mathf.Lerp(v.Flicker, 1f, steady));
            }
            else
            {
                var holo = MinerSelectArt.Cyan * Mathf.Min(1f, figureBrightness);
                var tint = Color.Lerp(holo, Color.white, v.Fill);
                tint.a = Mathf.Lerp(v.Flicker, 1f, Mathf.Max(v.Reveal, v.Fill));
                figure.color = tint;
            }
        }

        private void ApplyScan(MinerCardVisual v, float dt, float b)
        {
            if (scanBar == null) return;
            var filling = v.Fill > 0.0001f && v.Fill < 0.9999f;
            var revealing = !filling && v.Reveal > 0.0001f && v.Reveal < 0.9999f && Mathf.Abs(v.Reveal - lastReveal) > 0f;
            scanBar.enabled = filling || revealing;
            if (!scanBar.enabled) return;

            var progress = filling ? v.Fill : v.Reveal;
            var y = ScanLineY(progress);
            scanBar.rectTransform.anchoredPosition = new Vector2(0f, y);
            var c = filling ? Color.Lerp(MinerSelectArt.Cyan, Color.white, 0.55f) : MinerSelectArt.Cyan;
            c.a = Mathf.Clamp01(0.95f * Mathf.Max(b, 0.6f));
            scanBar.color = c;

            // 줄을 따라 튀는 불꽃.
            if (particles == null || dt <= 0f) return;
            sparkCarry += dt * (filling ? 70f : 50f);
            while (sparkCarry >= 1f)
            {
                sparkCarry -= 1f;
                var pos = new Vector2(UnityEngine.Random.Range(-150f, 150f), y + UnityEngine.Random.Range(-3f, 3f));
                var vel = new Vector2(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(-15f, 35f));
                particles.Emit(pos, vel, UnityEngine.Random.Range(3f, 6.5f), UnityEngine.Random.Range(0.3f, 0.65f), SparkColor(filling, 1f));
            }
        }

        private void EmitAmbient(MinerCardVisual v, float dt, float b)
        {
            if (particles == null || dt <= 0f) return;
            var rate = (5f + 8f * v.Hover + 12f * v.Selected) * Mathf.Lerp(0.4f, 1f, b);
            ambientCarry += dt * rate;
            while (ambientCarry >= 1f)
            {
                ambientCarry -= 1f;
                var pos = new Vector2(UnityEngine.Random.Range(-105f, 105f), PedestalTopY + UnityEngine.Random.Range(-10f, 6f));
                var vel = new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(35f, 85f));
                var c = Color.Lerp(MinerSelectArt.Cyan, Color.white, UnityEngine.Random.value * 0.4f);
                c.a = UnityEngine.Random.Range(0.45f, 0.9f) * b;
                particles.Emit(pos, vel, UnityEngine.Random.Range(3.5f, 8.5f), UnityEngine.Random.Range(1.2f, 2.4f), c);
            }
        }

        private static Color SparkColor(bool warm, float alpha)
        {
            var c = warm
                ? Color.Lerp(Color.white, new Color(1f, 0.86f, 0.55f, 1f), UnityEngine.Random.value * 0.6f)
                : Color.Lerp(MinerSelectArt.Cyan, Color.white, UnityEngine.Random.Range(0.3f, 0.9f));
            c.a = alpha;
            return c;
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

        private static UnityEngine.UI.RawImage NewRaw(Transform parent, string name, Texture texture, Color color)
        {
            var rect = MinerSelectPanel.NewRect(parent, name, Vector2.zero, new Vector2(100f, 100f));
            var raw = rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            raw.texture = texture;
            raw.color = color;
            raw.raycastTarget = false;
            return raw;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
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
