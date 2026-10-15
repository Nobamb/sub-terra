using SubTerra.App.UI.Outpost;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.FacilityNameTag
{
    /// <summary>이름표 색 묶음. 시설 이름표는 Teal, 플레이어 머리 위 경고 문구는 Warning을 쓴다.</summary>
    public readonly struct FacilityNameTagPalette
    {
        public readonly Color Fill;
        public readonly Color Glow;
        public readonly Color Edge;
        public readonly Color Accent;
        public readonly Color Label;

        public FacilityNameTagPalette(Color fill, Color glow, Color edge, Color accent, Color label)
        {
            Fill = fill;
            Glow = glow;
            Edge = edge;
            Accent = accent;
            Label = label;
        }

        /// <summary>시설 이름표·플레이어 정보 문구(청록).</summary>
        public static FacilityNameTagPalette Teal => new FacilityNameTagPalette(
            new Color(0.02f, 0.10f, 0.12f, 0.78f),
            new Color(0.10f, 0.85f, 0.85f, 0.30f),
            new Color(0.25f, 0.95f, 0.92f, 0.5f),
            new Color(0.55f, 1f, 0.97f, 1f),
            new Color(0.86f, 1f, 0.99f, 1f));

        /// <summary>플레이어 머리 위 경고 문구(노랑).</summary>
        public static FacilityNameTagPalette Warning => new FacilityNameTagPalette(
            new Color(0.20f, 0.14f, 0.02f, 0.82f),
            new Color(1f, 0.78f, 0.20f, 0.32f),
            new Color(1f, 0.82f, 0.28f, 0.55f),
            new Color(1f, 0.90f, 0.45f, 1f),
            new Color(1f, 0.95f, 0.78f, 1f));
    }

    /// <summary>
    /// 시설 이름표 하나의 모양과 연출. 일반 화면(월드 캔버스)과 CCTV 오버레이가 같은 클래스를 쓰고,
    /// 표시 여부와 위치는 각 소유자가 정한다. 시설 이름 외에는 아무것도 그리지 않는다.
    /// 피벗은 하단 중앙이라 시설 윗면 위에 그대로 올려 둘 수 있다.
    /// 플레이어 머리 위 문구(PlayerHeadMessageStack)도 같은 클래스를 쓰고, 아이콘·게이지·그룹 알파만 추가로 켠다.
    /// </summary>
    public sealed class FacilityNameTagVisual
    {
        public const float Height = 40f;
        public const float TwoLineHeight = 56f;
        public const float MinWidth = 104f;
        public const float MaxWidth = 340f;
        public const float SidePadding = 22f;
        /// <summary>월드 캔버스에서 디자인 픽셀 하나가 차지하는 월드 길이.</summary>
        public const float WorldPerPixel = 0.01f;
        public const int WorldSortingOrder = 80;
        /// <summary>아이콘을 켰을 때 가로에 더하는 여백(아이콘 폭 + 좌우 간격).</summary>
        public const float IconSpace = 30f;

        private const float FontSize = 22f;
        private const float EdgeThickness = 1.5f;
        private const float BracketLength = 10f;
        private const float BracketThickness = 2.5f;
        private const float ScanWidth = 2f;
        private const float MaxStep = 0.05f;
        // 아이콘 스프라이트에 글로우 여백이 들어 있어 세모 본체는 이보다 작게 보인다.
        private const float IconSize = 20f;
        private const float IconLeft = 8f;
        private const float GaugeHeight = 3f;

        private readonly FacilityNameTagPalette palette;
        private readonly float maxWidth;
        private readonly GameObject root;
        private readonly RectTransform rect;
        private readonly RectTransform frame;
        private readonly RectTransform glow;
        private readonly Image glowImage;
        private readonly Image fillImage;
        private readonly Image[] edges;
        private readonly Image[] brackets;
        private readonly RectTransform mask;
        private readonly RectTransform textRect;
        private readonly TextMeshProUGUI text;
        private readonly Image scan;

        private Image icon;
        private RectTransform gaugeRoot;
        private Image gaugeTrack;
        private Image gaugeBar;
        private RectTransform gaugeBarRect;

        private string label = string.Empty;
        private float width = MinWidth;
        private float height = Height;
        private float level;
        private float clock;
        private bool wanted;
        private bool settled;
        private bool built;

        public RectTransform Rect => rect;
        public GameObject Root => root;
        public string LabelText => label;
        public bool Wanted => wanted;
        public float Level => level;
        public float Width => width;
        public float CurrentHeight => height;
        public bool IsAlive => root != null;
        /// <summary>화면에 무언가 그려지는 중(등장·표시·퇴장).</summary>
        public bool IsVisible => root != null && root.activeSelf && (wanted || level > 0f);
        public bool IsSettled => wanted && level >= 1f;
        public FacilityNameTagFrame Frame { get; private set; }
        public TMP_Text TextComponent => text;

        private FacilityNameTagVisual(GameObject root, TMP_FontAsset font, FacilityNameTagPalette palette, float fontSize, float maxWidth)
        {
            this.root = root;
            this.palette = palette;
            this.maxWidth = maxWidth;
            rect = (RectTransform)root.transform;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(MinWidth, Height);

            frame = NewRect("Frame", rect);
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            glow = NewRect("Glow", frame);
            Stretch(glow, 9f);
            glowImage = AddImage(glow, CoreCctvArt.Glow(), palette.Glow);
            glowImage.type = Image.Type.Sliced;
            var fill = NewRect("Fill", frame);
            Stretch(fill, 0f);
            fillImage = AddImage(fill, null, palette.Fill);

            edges = new Image[4];
            for (var i = 0; i < 4; i++)
            {
                var edgeRect = NewRect("Edge" + i, frame);
                switch (i)
                {
                    case 0: // 위
                        SetEdge(edgeRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, EdgeThickness));
                        break;
                    case 1: // 아래
                        SetEdge(edgeRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, EdgeThickness));
                        break;
                    case 2: // 왼쪽
                        SetEdge(edgeRect, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(EdgeThickness, 0f));
                        break;
                    default: // 오른쪽
                        SetEdge(edgeRect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(EdgeThickness, 0f));
                        break;
                }

                edges[i] = AddImage(edgeRect, null, palette.Edge);
            }

            // 모서리 브래킷 8개: 모서리마다 가로·세로 한 쌍.
            brackets = new Image[8];
            for (var c = 0; c < 4; c++)
            {
                var right = (c & 1) == 1;
                var top = (c & 2) == 2;
                brackets[c * 2] = NewBracket(frame, right, top, true, palette.Accent);
                brackets[c * 2 + 1] = NewBracket(frame, right, top, false, palette.Accent);
            }

            mask = NewRect("TextMask", rect);
            mask.anchorMin = new Vector2(0f, 0f);
            mask.anchorMax = new Vector2(0f, 1f);
            mask.pivot = new Vector2(0f, 0.5f);
            mask.gameObject.AddComponent<RectMask2D>();
            textRect = NewRect("Text", mask);
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(0f, 1f);
            textRect.pivot = new Vector2(0f, 0.5f);
            text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = palette.Label;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = false;
            SetFont(font);

            var scanRect = NewRect("Scan", rect);
            scanRect.anchorMin = scanRect.anchorMax = new Vector2(0f, 0.5f);
            scanRect.pivot = new Vector2(0.5f, 0.5f);
            scanRect.sizeDelta = new Vector2(ScanWidth, Height - 6f);
            scan = AddImage(scanRect, null, palette.Accent);

            root.SetActive(false);
            built = true;
            Apply();
        }

        /// <summary>월드 캔버스 이름표. 월드 1칸당 100디자인픽셀 비율로 줄여 시설 위에 놓는다.</summary>
        public static FacilityNameTagVisual CreateWorld(
            Transform parent,
            TMP_FontAsset font,
            int layer,
            string name,
            FacilityNameTagPalette? palette = null,
            float fontSize = FontSize,
            float maxWidth = MaxWidth)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = layer;
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = WorldSortingOrder;
            canvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
            go.transform.localScale = Vector3.one * WorldPerPixel;

            var visual = new FacilityNameTagVisual(go, font, palette ?? FacilityNameTagPalette.Teal, fontSize, maxWidth);
            SetLayerRecursive(go.transform, layer);
            return visual;
        }

        /// <summary>CCTV 화면 같은 uGUI 영역 안에 놓는 이름표. 위치와 배율은 소유자가 정한다.</summary>
        public static FacilityNameTagVisual CreateOverlay(RectTransform parent, TMP_FontAsset font, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : 0;
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            return new FacilityNameTagVisual(go, font, FacilityNameTagPalette.Teal, FontSize, MaxWidth);
        }

        /// <summary>라벨에 줄바꿈이 있으면 두 줄 높이를 쓴다.</summary>
        public static float HeightFor(string value)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf('\n') >= 0 ? TwoLineHeight : Height;
        }

        public void SetFont(TMP_FontAsset font)
        {
            if (text == null || font == null)
            {
                return;
            }

            text.font = font;
            Relayout();
        }

        public void SetLabel(string value)
        {
            value = value ?? string.Empty;
            if (value == label)
            {
                return;
            }

            label = value;
            text.text = value;
            Relayout();
        }

        /// <summary>왼쪽에 아이콘을 붙인다. null이면 떼어 낸다. 아이콘이 있으면 가로 여백이 늘어난다.</summary>
        public void SetIcon(Sprite sprite)
        {
            if (icon == null)
            {
                if (sprite == null)
                {
                    return;
                }

                var iconRect = NewRect("Icon", frame);
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.sizeDelta = new Vector2(IconSize, IconSize);
                iconRect.anchoredPosition = new Vector2(IconLeft, 0f);
                icon = AddImage(iconRect, sprite, Color.white);
                icon.preserveAspect = true;
            }

            icon.sprite = sprite;
            icon.gameObject.SetActive(sprite != null);
            Relayout();
        }

        /// <summary>하단 진행 게이지. 0~1은 게이지 표시, 음수는 숨김.</summary>
        public void SetGauge(float progress)
        {
            if (progress < 0f)
            {
                if (gaugeRoot != null)
                {
                    gaugeRoot.gameObject.SetActive(false);
                }

                return;
            }

            if (gaugeRoot == null)
            {
                gaugeRoot = NewRect("Gauge", frame);
                gaugeRoot.anchorMin = new Vector2(0f, 0f);
                gaugeRoot.anchorMax = new Vector2(1f, 0f);
                gaugeRoot.pivot = new Vector2(0.5f, 0f);
                gaugeRoot.sizeDelta = new Vector2(0f, GaugeHeight);
                gaugeRoot.anchoredPosition = new Vector2(0f, EdgeThickness + 1f);
                gaugeTrack = AddImage(gaugeRoot, null, palette.Edge);
                var bar = NewRect("GaugeBar", gaugeRoot);
                bar.anchorMin = new Vector2(0f, 0f);
                bar.anchorMax = new Vector2(0f, 1f);
                bar.pivot = new Vector2(0f, 0.5f);
                bar.sizeDelta = Vector2.zero;
                bar.anchoredPosition = Vector2.zero;
                gaugeBarRect = bar;
                gaugeBar = AddImage(bar, null, palette.Accent);
            }

            gaugeRoot.gameObject.SetActive(true);
            gaugeBarRect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        }

        /// <summary>
        /// 루트 전체의 알파. 경고 문구의 깜박임에만 쓴다. 이름표는 호출하지 않으므로 CanvasGroup이 생기지 않는다.
        /// </summary>
        public void SetGroupAlpha(float alpha)
        {
            if (root == null)
            {
                return;
            }

            var group = root.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = root.AddComponent<CanvasGroup>();
            }

            group.alpha = Mathf.Clamp01(alpha);
        }

        /// <summary>표시 방향을 바꾼다. 같은 방향이면 아무것도 하지 않아 연출이 다시 시작되지 않는다.</summary>
        public void SetWanted(bool value)
        {
            if (value == wanted)
            {
                return;
            }

            wanted = value;
            settled = false;
            if (value && root != null && !root.activeSelf)
            {
                root.SetActive(true);
            }
        }

        /// <summary>unscaled 시간 기준으로 연출을 진행한다. 완전히 사라지면 오브젝트를 끈다.</summary>
        public void Tick(float deltaSeconds)
        {
            if (root == null)
            {
                return;
            }

            deltaSeconds = Mathf.Clamp(deltaSeconds, 0f, MaxStep);
            if (!wanted && level <= 0f)
            {
                if (root.activeSelf)
                {
                    root.SetActive(false);
                }

                return;
            }

            clock += deltaSeconds;
            var before = level;
            level = FacilityNameTagTimeline.Advance(level, wanted, deltaSeconds);
            if (wanted && level >= 1f)
            {
                // 표시 중에는 레이아웃을 건드리지 않고 테두리 발광만 천천히 숨 쉰다.
                if (!settled || before < 1f)
                {
                    settled = true;
                    Apply();
                }
                else
                {
                    ApplyPulse();
                }

                return;
            }

            settled = false;
            Apply();
            if (!wanted && level <= 0f)
            {
                root.SetActive(false);
            }
        }

        /// <summary>연출 없이 즉시 숨긴다. 재열기·씬 전환·시설 제거 뒤에 아무것도 남기지 않는다.</summary>
        public void ResetImmediate()
        {
            wanted = false;
            level = 0f;
            clock = 0f;
            settled = false;
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        public void Destroy()
        {
            if (root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }

        private bool HasIcon => icon != null && icon.gameObject.activeSelf;

        private void Relayout()
        {
            var preferred = text.font != null && !string.IsNullOrEmpty(label)
                ? text.GetPreferredValues(label).x
                : label.Length * text.fontSize;
            var extra = HasIcon ? IconSpace : 0f;
            width = Mathf.Clamp(Mathf.Ceil(preferred) + SidePadding * 2f + extra, MinWidth, maxWidth);
            height = HeightFor(label);
            rect.sizeDelta = new Vector2(width, height);
            textRect.sizeDelta = new Vector2(width, 0f);
            Apply();
        }

        private void Apply()
        {
            if (!built)
            {
                return;
            }

            var frameState = FacilityNameTagTimeline.Evaluate(level, wanted);
            Frame = frameState;
            var frameWidth = Mathf.Lerp(height, width, frameState.Spread);
            frame.sizeDelta = new Vector2(frameWidth, height);

            var alpha = frameState.FrameAlpha;
            fillImage.color = Scale(palette.Fill, alpha);
            for (var i = 0; i < edges.Length; i++)
            {
                edges[i].color = Scale(palette.Edge, alpha);
            }

            var flare = frameState.Flare;
            var bracketColor = Color.Lerp(palette.Edge, Color.white, Mathf.Clamp01(flare - FacilityNameTagTimeline.SteadyFlare) * 1.6f);
            bracketColor.a = alpha;
            for (var i = 0; i < brackets.Length; i++)
            {
                brackets[i].color = bracketColor;
            }

            ApplyPulse();

            // 글자는 크기를 바꾸지 않고, 프레임 안쪽을 왼쪽부터 마스크로 드러낸다.
            var left = (width - frameWidth) * 0.5f;
            mask.anchoredPosition = new Vector2(left, 0f);
            mask.sizeDelta = new Vector2(frameWidth * frameState.Reveal, 0f);
            textRect.anchoredPosition = new Vector2(-left, 0f);
            var textColor = palette.Label;
            textColor.a = frameState.TextAlpha;
            text.color = textColor;

            var scanRect = (RectTransform)scan.transform;
            scanRect.anchoredPosition = new Vector2(left + frameWidth * frameState.ScanPosition, 0f);
            scanRect.sizeDelta = new Vector2(ScanWidth, height - 6f);
            scan.color = Scale(palette.Accent, frameState.ScanAlpha);

            if (HasIcon)
            {
                icon.color = Scale(Color.white, frameState.TextAlpha);
            }

            if (gaugeRoot != null && gaugeRoot.gameObject.activeSelf)
            {
                gaugeTrack.color = Scale(palette.Edge, alpha);
                gaugeBar.color = Scale(palette.Accent, alpha);
            }
        }

        private void ApplyPulse()
        {
            var state = Frame;
            var pulse = wanted && level >= 1f ? FacilityNameTagTimeline.Pulse(clock) : 1f;
            var glowColor = palette.Glow;
            glowColor.a = palette.Glow.a * state.FrameAlpha * Mathf.Clamp01(state.Flare * 1.4f) * pulse;
            glowImage.color = glowColor;
        }

        private static Color Scale(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }

        private static void SetEdge(RectTransform target, Vector2 min, Vector2 max, Vector2 pivot, Vector2 size)
        {
            target.anchorMin = min;
            target.anchorMax = max;
            target.pivot = pivot;
            target.sizeDelta = size;
            target.anchoredPosition = Vector2.zero;
        }

        private static Image NewBracket(RectTransform parent, bool right, bool top, bool horizontal, Color accent)
        {
            var bar = NewRect(horizontal ? "BracketH" : "BracketV", parent);
            var anchor = new Vector2(right ? 1f : 0f, top ? 1f : 0f);
            bar.anchorMin = bar.anchorMax = anchor;
            bar.pivot = anchor;
            bar.sizeDelta = horizontal
                ? new Vector2(BracketLength, BracketThickness)
                : new Vector2(BracketThickness, BracketLength);
            bar.anchoredPosition = Vector2.zero;
            return AddImage(bar, null, accent);
        }

        private static RectTransform NewRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform target, float inset)
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.offsetMin = new Vector2(-inset, -inset);
            target.offsetMax = new Vector2(inset, inset);
        }

        private static Image AddImage(RectTransform target, Sprite sprite, Color color)
        {
            var image = target.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void SetLayerRecursive(Transform target, int layer)
        {
            target.gameObject.layer = layer;
            for (var i = 0; i < target.childCount; i++)
            {
                SetLayerRecursive(target.GetChild(i), layer);
            }
        }
    }
}
