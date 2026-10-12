using System.Collections.Generic;
using SubTerra.App.UI.Guide;
using SubTerra.App.UI.Sell;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.ControlTerminal
{
    /// <summary>
    /// 조작 방식 선택 창의 '조작 단말' 표시. 홀로그램 프레임 조립·키보드 착지·키 눌림·연결선·시연 화면·자동 시연을 맡는다.
    /// 조작 방식 선택·저장은 ControlSchemePanel이 그대로 맡고, 여기서는 아무것도 저장하거나 게임 입력을 호출하지 않는다.
    /// 등장·페이지 교체·키 눌림·자동 시연은 서로 다른 속성(카드/키보드 위치·그룹 알파·키 눌림·시연 눌림)을 소유한다.
    /// 모든 시간은 unscaledDeltaTime을 받은 Tick으로 진행하며 정적인 상태에서는 갱신하지 않는다.
    /// </summary>
    public sealed class ControlTerminalView
    {
        public enum Phase
        {
            Hidden,
            Intro,
            Idle,
            Outro
        }

        private const float MaxStep = 0.05f;
        private const float LineThickness = 2f;
        private const float LineCoreThickness = 2.5f;
        private const float GlowMargin = 22f;
        private const float GlowStripThickness = 54f;
        private const float FlareLength = 150f;
        private const float FitMargin = 48f;

        private static readonly Color CardFill = new Color(0.02f, 0.06f, 0.10f, 0.94f);
        private static readonly Color Accent = new Color(0.55f, 1f, 0.97f, 1f);
        private static readonly Color EdgeTeal = new Color(0.25f, 0.95f, 0.92f, 1f);
        private static readonly Color PlateFill = new Color(0.03f, 0.07f, 0.12f, 0.9f);
        private static readonly Color TitleColor = new Color(0.86f, 1f, 0.99f, 1f);

        private readonly RectTransform card;
        private readonly Image cardFill;
        private readonly TMP_FontAsset font;
        private readonly IGuideSprites sprites;
        private readonly ControlTerminalHoverState hover = new ControlTerminalHoverState();
        private readonly ControlTerminalAutoDemo autoDemo = new ControlTerminalAutoDemo();
        private readonly List<ControlTerminalKeyCap> caps = new List<ControlTerminalKeyCap>();
        private readonly RectTransform[] ghostRects = new RectTransform[ControlTerminalTimeline.GhostCount];
        private readonly Image[] ghostImages = new Image[ControlTerminalTimeline.GhostCount];
        private readonly RectTransform[] flareRects = new RectTransform[8];
        private readonly Image[] flareImages = new Image[8];
        private readonly Vector2[] points = new Vector2[4];
        private readonly Vector2[] segStarts = new Vector2[3];
        private readonly Vector2[] segEnds = new Vector2[3];
        private readonly RectTransform[] segRects = new RectTransform[3];
        private readonly Image[] segImages = new Image[3];

        private RectTransform chrome;
        private CanvasGroup chromeGroup;
        private RectTransform holoBack;
        private RectTransform holoFront;
        private RectTransform revealRect;
        private readonly Image[] glowImages = new Image[4];
        private Image revealCopy;
        private Image flashFrame;
        private Image flashEdge;
        private RectTransform edgeRect;
        private Image edgeImage;
        private RectTransform lineRect;
        private Image lineImage;
        private RectTransform lineGlowRect;
        private Image lineGlowImage;
        private RectTransform beamRect;
        private Image beamImage;
        private RectTransform beamGlowRect;
        private Image beamGlowImage;
        private Sprite frameSprite;
        private bool holoShown;
        private RectTransform keyboardRoot;
        private CanvasGroup keyboardGroup;
        private CanvasGroup keysGroup;
        private RectTransform dotRect;
        private Image dotImage;
        private RectTransform headRect;
        private Image headImage;
        private RectTransform opener;
        private Image openerBackground;
        private RectTransform lightRect;
        private Image lightImage;
        private RectTransform scanRect;
        private Image scanImage;
        private RectTransform screenFrameRect;
        private Image screenFrameImage;
        private Image screenGlow;
        private CanvasGroup stageGroup;
        private GameGuideStageView stage;
        private TMP_Text title;
        private TMP_Text caption;
        private TMP_Text description;
        private TMP_Text functionCaption;
        private TMP_Text hint;

        private Phase phase = Phase.Hidden;
        private ControlScheme scheme = ControlScheme.Classic;
        private bool schemeApplied;
        private float introTime;
        private float outroTime;
        private float swapTime = ControlTerminalTimeline.PageSwapDuration;
        private string boundKey;
        private int externalBlocks;
        private bool screenShown;

        public ControlTerminalView(RectTransform card, Image cardFill, TMP_FontAsset font, IGuideSprites sprites)
        {
            this.card = card;
            this.cardFill = cardFill;
            this.font = font;
            this.sprites = sprites;
            Build();
        }

        public Phase CurrentPhase => phase;
        public bool IsIntroPlaying => phase == Phase.Intro;
        public bool IsOutroPlaying => phase == Phase.Outro;
        public bool IsHidden => phase == Phase.Hidden;
        public float IntroTime => introTime;
        public ControlScheme Scheme => scheme;
        public RectTransform Chrome => chrome;
        public ControlTerminalHoverState HoverState => hover;
        public ControlTerminalAutoDemo AutoDemo => autoDemo;
        public IReadOnlyList<ControlTerminalKeyCap> Caps => caps;
        public GameGuideStageView Stage => stage;
        public string BoundKey => boundKey;
        public bool ScreenVisible => screenShown;
        public RectTransform KeyboardRoot => keyboardRoot;
        public string TitleText => title != null ? title.text : string.Empty;
        public string DescriptionText => description != null ? description.text : string.Empty;
        public string FunctionCaptionText => functionCaption != null ? functionCaption.text : string.Empty;
        public float SwapAlpha => ControlTerminalTimeline.PageSwapAlpha(swapTime);

        public ControlTerminalKeyCap GetCap(string id)
        {
            for (int i = 0; i < caps.Count; i++)
            {
                if (caps[i].Id == id) return caps[i];
            }

            return null;
        }

        // ------------------------------------------------------------------ 조립

        private void Build()
        {
            var layer = card.gameObject.layer;
            BuildFrame(layer);
            BuildKeyboard(layer);
            BuildScreen(layer);
            BuildConnector(layer);
            BuildChrome(layer);
            holoFront.SetAsLastSibling();
            ApplyIntro(ControlTerminalTimeline.IntroDuration);
            screenShown = false;
            ApplyScreen();
        }

        /// <summary>
        /// 생성 이미지 프레임을 카드에 입히고, 등장·퇴장에 쓰는 홀로그램 연출 층을 미리 만든다.
        /// 뒤쪽 층(번짐·열리는 창)은 키보드 아래에, 앞쪽 층(빛줄기·테두리·잔상·스캔·반짝임)은 맨 위에 둔다.
        /// 연출이 끝나면 두 층 모두 꺼지고 완성된 프레임만 남는다.
        /// </summary>
        private void BuildFrame(int layer)
        {
            frameSprite = ControlTerminalArt.Frame();
            StyleFrame(cardFill);
            var cardSize = new Vector2(ControlTerminalLayout.CardWidth, ControlTerminalLayout.CardHeight);

            holoBack = NewRect("HoloBack", card, layer);
            Stretch(holoBack);
            holoBack.SetAsFirstSibling();

            // 바깥 번짐: 속이 빈 틀이어야 해서 가장자리마다 길쭉하게 늘인 부드러운 띠 네 개로 만든다.
            var glowRoot = NewRect("Glow", holoBack, layer);
            Stretch(glowRoot);
            for (int i = 0; i < 4; i++)
            {
                var strip = NewRect("Strip" + i, glowRoot, layer);
                var horizontal = i < 2;
                Center(strip, horizontal ? new Vector2(cardSize.x + GlowMargin * 2f, GlowStripThickness) : new Vector2(GlowStripThickness, cardSize.y + GlowMargin * 2f));
                var sign = (i & 1) == 0 ? 1f : -1f;
                strip.anchoredPosition = horizontal ? new Vector2(0f, sign * cardSize.y * 0.5f) : new Vector2(sign * cardSize.x * 0.5f, 0f);
                glowImages[i] = AddImage(strip, ResourceSellArt.SoftRect(), Color.clear);
            }

            // 열리는 창: 가운데를 기준으로 크기가 변하고, 안쪽의 프레임 그림은 카드 크기로 고정된다.
            revealRect = NewRect("Reveal", holoBack, layer);
            Center(revealRect, new Vector2(3f, 3f));
            revealRect.gameObject.AddComponent<RectMask2D>();
            var copy = NewRect("FrameCopy", revealRect, layer);
            Center(copy, cardSize);
            revealCopy = AddImage(copy, null, Color.clear);
            StyleFrame(revealCopy);
            var flashRect = NewRect("FlashFrame", revealRect, layer);
            Center(flashRect, cardSize);
            flashFrame = AddImage(flashRect, null, Color.clear);
            StyleFrame(flashFrame);
            var edgeFlashRect = NewRect("FlashEdge", revealRect, layer);
            Center(edgeFlashRect, cardSize);
            flashEdge = AddImage(edgeFlashRect, ResourceSellArt.ChamferOutline(), Color.clear);
            flashEdge.pixelsPerUnitMultiplier = 0.4f;

            holoFront = NewRect("HoloFront", card, layer);
            Stretch(holoFront);

            // 고스트 프레임: 카드보다 큰 얇은 청록 테두리가 안쪽으로 모여든다(퇴장 땐 바깥으로 퍼진다).
            for (int i = 0; i < ControlTerminalTimeline.GhostCount; i++)
            {
                var ghost = NewRect("Ghost" + i, holoFront, layer);
                Center(ghost, cardSize);
                ghostRects[i] = ghost;
                ghostImages[i] = AddImage(ghost, ResourceSellArt.ChamferOutline(), Color.clear);
            }

            // 열리는 창을 따라다니는 사각 테두리.
            edgeRect = NewRect("OpenEdge", holoFront, layer);
            Center(edgeRect, new Vector2(3f, 3f));
            edgeImage = AddImage(edgeRect, ResourceSellArt.ChamferOutline(), Color.clear);

            // 수평 빛줄기: 흰 심 + 청록 번짐.
            lineGlowRect = NewRect("LineGlow", holoFront, layer);
            Center(lineGlowRect, new Vector2(3f, 18f));
            lineGlowImage = AddImage(lineGlowRect, ResourceSellArt.Soft(), Color.clear);
            lineRect = NewRect("Line", holoFront, layer);
            Center(lineRect, new Vector2(3f, 2.5f));
            lineImage = AddImage(lineRect, null, Color.clear);

            // 스캔 빔: 카드 안에서만 보이게 마스크 안에 둔다.
            var beamMask = NewRect("BeamMask", holoFront, layer);
            Center(beamMask, cardSize);
            beamMask.gameObject.AddComponent<RectMask2D>();
            beamGlowRect = NewRect("BeamGlow", beamMask, layer);
            Center(beamGlowRect, new Vector2(cardSize.x, 70f));
            beamGlowImage = AddImage(beamGlowRect, ResourceSellArt.Soft(), Color.clear);
            beamRect = NewRect("Beam", beamMask, layer);
            Center(beamRect, new Vector2(cardSize.x, 3f));
            beamImage = AddImage(beamRect, null, Color.clear);

            // 네 모서리의 십자 반짝임(가로·세로 한 쌍씩).
            for (int i = 0; i < 4; i++)
            {
                for (int axis = 0; axis < 2; axis++)
                {
                    var flare = NewRect("Flare" + i + (axis == 0 ? "H" : "V"), holoFront, layer);
                    Center(flare, axis == 0 ? new Vector2(FlareLength, 8f) : new Vector2(8f, FlareLength));
                    flareRects[i * 2 + axis] = flare;
                    flareImages[i * 2 + axis] = AddImage(flare, ResourceSellArt.Soft(), Color.clear);
                }
            }

            holoBack.gameObject.SetActive(false);
            holoFront.gameObject.SetActive(false);
            holoShown = false;
        }

        private Color FrameColor => frameSprite != null ? Color.white : CardFill;

        /// <summary>프레임 그림(없으면 기존 각진 면)과 9-slice 배율을 입힌다.</summary>
        private void StyleFrame(Image image)
        {
            if (frameSprite != null)
            {
                image.sprite = frameSprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = ControlTerminalArt.SliceMultiplier(frameSprite, ControlTerminalArt.FrameCornerPixels);
            }
            else
            {
                image.sprite = ResourceSellArt.ChamferFill();
                image.type = Image.Type.Sliced;
            }

            image.raycastTarget = false;
        }

        private void BuildKeyboard(int layer)
        {
            var maskRect = NewRect("Keyboard", card, layer);
            maskRect.anchorMin = maskRect.anchorMax = maskRect.pivot = new Vector2(0.5f, 0.5f);
            maskRect.anchoredPosition = ControlTerminalLayout.PlateCenter;
            maskRect.sizeDelta = ControlTerminalLayout.PlateSize;
            maskRect.gameObject.AddComponent<RectMask2D>();

            keyboardRoot = NewRect("KeyboardRoot", maskRect, layer);
            keyboardRoot.anchorMin = keyboardRoot.anchorMax = keyboardRoot.pivot = new Vector2(0.5f, 0.5f);
            keyboardRoot.sizeDelta = ControlTerminalLayout.PlateSize;
            keyboardGroup = keyboardRoot.gameObject.AddComponent<CanvasGroup>();
            keyboardGroup.blocksRaycasts = true;

            // 카드 중앙을 원점으로 쓰는 좌표계를 맞추는 안쪽 루트.
            var origin = NewRect("Origin", keyboardRoot, layer);
            origin.anchorMin = origin.anchorMax = origin.pivot = new Vector2(0.5f, 0.5f);
            origin.anchoredPosition = -ControlTerminalLayout.PlateCenter;
            origin.sizeDelta = Vector2.zero;

            var plate = NewRect("Plate", origin, layer);
            plate.anchorMin = plate.anchorMax = plate.pivot = new Vector2(0.5f, 0.5f);
            plate.anchoredPosition = ControlTerminalLayout.PlateCenter;
            plate.sizeDelta = ControlTerminalLayout.PlateSize - new Vector2(12f, 12f);
            AddImage(plate, ResourceSellArt.ChamferFill(), PlateFill);
            var plateLine = NewRect("PlateLine", plate, layer);
            Stretch(plateLine);
            AddImage(plateLine, ResourceSellArt.ChamferOutline(), ResourceSellUi.WithAlpha(EdgeTeal, 0.35f));

            var keys = NewRect("Keys", origin, layer);
            keys.anchorMin = keys.anchorMax = keys.pivot = new Vector2(0.5f, 0.5f);
            keys.sizeDelta = Vector2.zero;
            keysGroup = keys.gameObject.AddComponent<CanvasGroup>();
            var defs = ControlTerminalLayout.Keys;
            for (int i = 0; i < defs.Count; i++)
            {
                var cap = ControlTerminalKeyCap.Create(keys, defs[i], font);
                cap.HoverEntered += OnKeyEntered;
                cap.HoverExited += OnKeyExited;
                caps.Add(cap);
            }
        }

        private void BuildScreen(int layer)
        {
            var size = ControlTerminalLayout.ScreenSize;
            var root = NewRect("DemoScreen", card, layer);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = ControlTerminalLayout.ScreenCenter;
            root.sizeDelta = size;

            var glow = NewRect("Glow", root, layer);
            Stretch(glow);
            glow.offsetMin = new Vector2(-26f, -26f);
            glow.offsetMax = new Vector2(26f, 26f);
            screenGlow = AddImage(glow, ResourceSellArt.SoftRect(), Color.clear);

            opener = NewRect("Opener", root, layer);
            opener.anchorMin = opener.anchorMax = opener.pivot = new Vector2(0.5f, 0.5f);
            opener.sizeDelta = size;
            opener.gameObject.AddComponent<RectMask2D>();
            var background = NewRect("Background", opener, layer);
            Stretch(background);
            openerBackground = AddImage(background, null, new Color(0.02f, 0.045f, 0.075f, 1f));

            // 시연 영상은 크기가 고정된 틀 안에 두고 Opener만 열고 닫는다.
            var stageFrame = NewRect("StageFrame", opener, layer);
            stageFrame.anchorMin = stageFrame.anchorMax = stageFrame.pivot = new Vector2(0.5f, 0.5f);
            stageFrame.sizeDelta = size;
            stage = GameGuideStageView.Create(stageFrame, "Stage", 0f, 0f, size.x, size.y, font, sprites);
            stageGroup = stage.gameObject.AddComponent<CanvasGroup>();
            stageGroup.blocksRaycasts = false;
            stageGroup.interactable = false;

            scanRect = NewRect("Scan", opener, layer);
            scanRect.anchorMin = scanRect.anchorMax = scanRect.pivot = new Vector2(0.5f, 0.5f);
            scanRect.sizeDelta = new Vector2(size.x, 3f);
            scanImage = AddImage(scanRect, null, Color.clear);

            lightRect = NewRect("HorizontalLight", root, layer);
            lightRect.anchorMin = lightRect.anchorMax = lightRect.pivot = new Vector2(0.5f, 0.5f);
            lightRect.sizeDelta = new Vector2(0f, 2f);
            lightImage = AddImage(lightRect, null, Color.clear);

            screenFrameRect = NewRect("ScreenFrame", root, layer);
            screenFrameRect.anchorMin = screenFrameRect.anchorMax = screenFrameRect.pivot = new Vector2(0.5f, 0.5f);
            screenFrameRect.sizeDelta = size;
            screenFrameImage = AddImage(screenFrameRect, ResourceSellArt.ChamferOutline(), Color.clear);

            var captionRect = NewRect("FunctionCaption", root, layer);
            captionRect.anchorMin = captionRect.anchorMax = new Vector2(0.5f, 0f);
            captionRect.pivot = new Vector2(0.5f, 1f);
            captionRect.anchoredPosition = new Vector2(0f, -10f);
            captionRect.sizeDelta = new Vector2(size.x, 30f);
            functionCaption = ResourceSellUi.Text(captionRect, font, 21f, FontStyles.Bold, Accent, TextAlignmentOptions.Center);
        }

        private void BuildConnector(int layer)
        {
            var root = NewRect("Connector", card, layer);
            Stretch(root);
            for (int i = 0; i < 3; i++)
            {
                segRects[i] = NewRect("Segment" + i, root, layer);
                segRects[i].anchorMin = segRects[i].anchorMax = segRects[i].pivot = new Vector2(0.5f, 0.5f);
                segImages[i] = AddImage(segRects[i], null, Color.clear);
            }

            dotRect = NewRect("StartDot", root, layer);
            dotRect.anchorMin = dotRect.anchorMax = dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(6f, 6f);
            dotImage = AddImage(dotRect, null, Color.clear);
            headRect = NewRect("Head", root, layer);
            headRect.anchorMin = headRect.anchorMax = headRect.pivot = new Vector2(0.5f, 0.5f);
            headRect.sizeDelta = new Vector2(9f, 9f);
            headImage = AddImage(headRect, ResourceSellArt.Soft(), Color.clear);
        }

        private void BuildChrome(int layer)
        {
            chrome = NewRect("Chrome", card, layer);
            Stretch(chrome);
            chromeGroup = chrome.gameObject.AddComponent<CanvasGroup>();
            title = Label("SchemeTitle", 0f, 300f, 760f, 46f, 30f, FontStyles.Bold, TitleColor);
            caption = Label("KeyboardCaption", 0f, 262f, 700f, 30f, 20f, FontStyles.Normal, ResourceSellUi.TextMuted);
            caption.text = ControlTerminalBindings.Caption;
            description = Label("SchemeDescription", 0f, -205f, 1000f, 84f, 21f, FontStyles.Normal, ResourceSellUi.TextMain);
            var notice = Label("SaveNotice", 0f, -250f, 1000f, 26f, 17f, FontStyles.Normal, ResourceSellUi.TextMuted);
            notice.text = ControlTerminalBindings.SaveNotice;
            hint = Label("HoverHint", ControlTerminalLayout.ScreenCenter.x, -62f, 420f, 24f, 16f, FontStyles.Normal, ResourceSellUi.TextDim);
            hint.text = ControlTerminalBindings.HoverHint;
        }

        private TMP_Text Label(string name, float x, float y, float width, float height, float size, FontStyles style, Color color)
        {
            var rect = NewRect(name, chrome, card.gameObject.layer);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            var text = ResourceSellUi.Text(rect, font, size, style, color, TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        // ------------------------------------------------------------------ 외부 제어

        /// <summary>카드를 열린 영역 안에 들어가게 줄인다(확대하지 않는다).</summary>
        public float FitTo(Vector2 available)
        {
            var scale = 1f;
            if (available.x > 1f && available.y > 1f)
            {
                scale = Mathf.Min(1f,
                    (available.x - FitMargin) / ControlTerminalLayout.CardWidth,
                    (available.y - FitMargin) / ControlTerminalLayout.CardHeight);
                scale = Mathf.Max(0.3f, scale);
            }

            card.localScale = new Vector3(scale, scale, 1f);
            return scale;
        }

        /// <summary>조작 방식에 맞춰 키 기능·글자·설명을 갱신한다. 키·시연 화면 오브젝트는 다시 만들지 않는다.</summary>
        public void SetScheme(ControlScheme next, bool animate)
        {
            bool changed = !schemeApplied || next != scheme;
            scheme = next;
            schemeApplied = true;
            title.text = ControlTerminalBindings.Title(next);
            description.text = ControlTerminalBindings.Description(next);
            if (!changed) return;
            ClearHover();
            for (int i = 0; i < caps.Count; i++)
            {
                caps[i].ResetImmediate();
                caps[i].Bind(next);
            }

            autoDemo.SetKeys(ControlTerminalBindings.DemoKeys(next));
            swapTime = animate ? 0f : ControlTerminalTimeline.PageSwapDuration;
            ApplySwap();
        }

        /// <summary>프레임 조립 → 키보드 착지 → 점등 연출을 처음부터 시작한다.</summary>
        public void PlayIntro()
        {
            ClearHover();
            for (int i = 0; i < caps.Count; i++) caps[i].ResetImmediate();
            phase = Phase.Intro;
            introTime = 0f;
            outroTime = 0f;
            swapTime = ControlTerminalTimeline.PageSwapDuration;
            ApplySwap();
            autoDemo.Restart();
            ApplyIntro(0f);
            ApplyIntroKeys(0f);
        }

        /// <summary>연출 없이 완성된 모습으로 즉시 보인다(에디터 테스트·즉시 열기).</summary>
        public void ShowSettled()
        {
            ClearHover();
            for (int i = 0; i < caps.Count; i++) caps[i].ResetImmediate();
            phase = Phase.Idle;
            introTime = ControlTerminalTimeline.IntroDuration;
            swapTime = ControlTerminalTimeline.PageSwapDuration;
            ApplySwap();
            ApplyIntro(introTime);
            autoDemo.Restart();
        }

        /// <summary>약 0.18초 소등·프레임 분해. 끝나면 Hidden이 된다. 설정 저장은 이 연출과 무관하다.</summary>
        public void PlayOutro()
        {
            if (phase == Phase.Hidden || phase == Phase.Outro) return;
            ClearHover();
            for (int i = 0; i < caps.Count; i++) caps[i].ResetImmediate();
            phase = Phase.Outro;
            outroTime = 0f;
            ApplyOutro(0f);
        }

        /// <summary>닫힘·disable·재열기 전에 호버·선·영상·눌림을 모두 즉시 정리한다.</summary>
        public void ResetImmediate()
        {
            ClearHover();
            for (int i = 0; i < caps.Count; i++) caps[i].ResetImmediate();
            autoDemo.Restart();
            externalBlocks = 0;
            phase = Phase.Hidden;
            introTime = 0f;
            outroTime = 0f;
            swapTime = ControlTerminalTimeline.PageSwapDuration;
            ApplySwap();
        }

        /// <summary>하단 버튼에 호버·포커스가 있으면 자동 시연을 멈춘다.</summary>
        public void AddExternalBlock(int delta)
        {
            externalBlocks = Mathf.Max(0, externalBlocks + delta);
            autoDemo.SetBlocked(externalBlocks > 0 || hover.HoverKey != null);
        }

        public void Tick(float deltaSeconds)
        {
            if (phase == Phase.Hidden) return;
            var dt = Mathf.Clamp(deltaSeconds, 0f, MaxStep);
            if (phase == Phase.Outro)
            {
                outroTime += dt;
                ApplyOutro(outroTime);
                if (outroTime >= ControlTerminalTimeline.OutroDuration) phase = Phase.Hidden;
                return;
            }

            if (phase == Phase.Intro)
            {
                introTime += dt;
                ApplyIntro(introTime);
                ApplyIntroKeys(introTime);
                if (introTime >= ControlTerminalTimeline.IntroDuration)
                {
                    phase = Phase.Idle;
                    autoDemo.Restart();
                }
            }

            if (swapTime < ControlTerminalTimeline.PageSwapDuration)
            {
                swapTime += dt;
                ApplySwap();
            }

            hover.Tick(dt);
            if (phase == Phase.Idle)
            {
                autoDemo.SetBlocked(externalBlocks > 0 || hover.HoverKey != null);
                autoDemo.Tick(dt);
                var current = autoDemo.CurrentKey;
                for (int i = 0; i < caps.Count; i++)
                {
                    var cap = caps[i];
                    cap.SetDemoDown(current != null && cap.Id == current);
                }
            }

            for (int i = 0; i < caps.Count; i++) caps[i].Tick(dt);
            ApplyHover(dt);
        }

        // ------------------------------------------------------------------ 호버

        private void OnKeyEntered(ControlTerminalKeyCap cap)
        {
            if (phase == Phase.Hidden || phase == Phase.Outro) return;
            hover.Enter(cap.Id);
            autoDemo.SetBlocked(true);
        }

        private void OnKeyExited(ControlTerminalKeyCap cap)
        {
            hover.Exit(cap.Id);
        }

        private void ClearHover()
        {
            hover.Reset();
            ClearStage();
            ApplyHover(0f);
        }

        private void ClearStage()
        {
            if (stage != null) stage.Clear();
            boundKey = null;
            if (functionCaption != null) functionCaption.text = string.Empty;
        }

        private void ApplyHover(float dt)
        {
            var activeKey = hover.ActiveKey;
            if (activeKey == null)
            {
                if (boundKey != null) ClearStage();
            }
            else if (activeKey != boundKey)
            {
                var cap = GetCap(activeKey);
                var function = cap != null ? cap.Function : ControlKeyFunction.None;
                var clip = ControlTerminalDemoClips.Get(function);
                stage.Bind(clip, false);
                stage.Play();
                boundKey = activeKey;
                functionCaption.text = ControlTerminalBindings.FunctionName(function);
            }

            if (hover.VideoPlaying && dt > 0f) stage.Tick(dt);
            ApplyConnector(activeKey);
            ApplyScreen();
        }

        private void ApplyConnector(string activeKey)
        {
            var cap = activeKey != null ? GetCap(activeKey) : null;
            if (cap == null || hover.LineProgress <= 0f)
            {
                SetConnectorVisible(false);
                return;
            }

            var start = ControlTerminalConnector.StartOf(cap.Def, cap.PressPixels);
            ControlTerminalConnector.Build(start, ControlTerminalLayout.ConnectorExitX, ControlTerminalLayout.ScreenLeftMiddle, points);
            var head = ControlTerminalConnector.Evaluate(points, hover.LineProgress, segStarts, segEnds);
            var color = Color.Lerp(EdgeTeal, Color.white, 0.35f);
            for (int i = 0; i < 3; i++)
            {
                var a = segStarts[i];
                var b = segEnds[i];
                var length = Vector2.Distance(a, b);
                var visible = length > 0.5f;
                if (segImages[i].enabled != visible) segImages[i].enabled = visible;
                if (!visible) continue;
                var horizontal = Mathf.Abs(b.x - a.x) >= Mathf.Abs(b.y - a.y);
                segRects[i].anchoredPosition = (a + b) * 0.5f;
                segRects[i].sizeDelta = horizontal ? new Vector2(length + LineThickness, LineThickness) : new Vector2(LineThickness, length + LineThickness);
                segImages[i].color = color;
            }

            dotRect.anchoredPosition = start;
            dotImage.color = Color.white;
            dotImage.enabled = true;
            headRect.anchoredPosition = head;
            headImage.color = new Color(EdgeTeal.r, EdgeTeal.g, EdgeTeal.b, hover.LineProgress < 1f ? 0.9f : 0f);
            headImage.enabled = true;
        }

        private void SetConnectorVisible(bool visible)
        {
            if (segImages[0].enabled == visible && dotImage.enabled == visible) return;
            for (int i = 0; i < 3; i++) segImages[i].enabled = visible;
            dotImage.enabled = visible;
            headImage.enabled = visible;
        }

        private void ApplyScreen()
        {
            var level = hover.ScreenLevel;
            var frame = ControlTerminalTimeline.Screen(level);
            var size = ControlTerminalLayout.ScreenSize;
            var visible = level > 0f;
            if (!visible)
            {
                // 꺼져 있는 동안은 아무것도 건드리지 않는다(매 프레임 갱신 방지).
                if (screenShown || opener.gameObject.activeSelf)
                {
                    screenShown = false;
                    opener.gameObject.SetActive(false);
                    // 꺼진 화면 자리는 흐린 대기 테두리로 남겨 시연 영역을 알린다.
                    screenFrameRect.sizeDelta = size;
                    screenFrameImage.enabled = true;
                    screenFrameImage.color = ResourceSellUi.WithAlpha(EdgeTeal, 0.18f);
                    lightImage.enabled = false;
                    functionCaption.alpha = 0f;
                    screenGlow.color = Color.clear;
                }

                return;
            }

            screenShown = true;
            var width = size.x * frame.WidthFraction;
            var height = Mathf.Max(2f, size.y * frame.HeightFraction);
            if (!opener.gameObject.activeSelf) opener.gameObject.SetActive(true);
            screenFrameImage.enabled = true;
            lightImage.enabled = true;

            opener.sizeDelta = new Vector2(width, height);
            screenFrameRect.sizeDelta = new Vector2(width + 2f, height + 2f);
            screenFrameImage.color = ResourceSellUi.WithAlpha(EdgeTeal, 0.75f);
            lightRect.sizeDelta = new Vector2(width, 2f);
            lightImage.color = new Color(1f, 1f, 1f, frame.LightAlpha);
            scanRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(height * 0.5f, -height * 0.5f, frame.ScanPosition));
            scanImage.color = new Color(EdgeTeal.r, EdgeTeal.g, EdgeTeal.b, frame.ScanAlpha * 0.8f);
            var content = frame.ContentAlpha * hover.ContentAlpha;
            stageGroup.alpha = content;
            functionCaption.alpha = content;
            screenGlow.color = ResourceSellUi.WithAlpha(EdgeTeal, 0.16f * frame.WidthFraction * frame.HeightFraction);
        }

        // ------------------------------------------------------------------ 등장·퇴장·교체

        /// <summary>
        /// 홀로그램 연출 층이 프레임을 그리는 동안은 실제 프레임을 숨기고, 창이 다 열리면 같은 모습의 실제 프레임이 이어받는다.
        /// 키캡은 ApplyIntroKeys가 따로 튀어나오게 한다.
        /// </summary>
        private void ApplyIntro(float time)
        {
            var frame = ControlTerminalTimeline.Intro(time);
            if (frame.HoloActive) ApplyHolo(ControlTerminalTimeline.IntroHolo(time), time, true);
            else SetHoloShown(false);
            if (!frame.HoloActive) cardFill.color = FrameColor;
            keyboardGroup.alpha = frame.KeyboardAlpha;
            chromeGroup.alpha = frame.TextAlpha;
        }

        private void ApplyIntroKeys(float time)
        {
            var left = ControlTerminalLayout.PlateLeft;
            var width = ControlTerminalLayout.PlateRight - left;
            for (int i = 0; i < caps.Count; i++)
            {
                var cap = caps[i];
                cap.SetIntroPop(ControlTerminalTimeline.IntroKeyPop(
                    time, cap.Def.Row, (cap.Def.Center.x - left) / width, cap.Id == ControlTerminalBindings.SpaceKey));
            }
        }

        private void ApplyOutro(float time)
        {
            var frame = ControlTerminalTimeline.Outro(time);
            chromeGroup.alpha = frame.ContentAlpha;
            keyboardGroup.alpha = frame.ContentAlpha;
            if (frame.HoloActive) ApplyHolo(ControlTerminalTimeline.OutroHolo(time), time, false);
            else
            {
                SetHoloShown(false);
                cardFill.color = Color.clear;
            }
        }

        /// <summary>한 시점의 홀로그램 층 값을 이미 만들어 둔 그래픽에 적용한다(오브젝트·머티리얼을 새로 만들지 않는다).</summary>
        private void ApplyHolo(ControlTerminalHoloFrame holo, float time, bool intro)
        {
            SetHoloShown(true);
            cardFill.color = holo.RealFrame ? FrameColor : Color.clear;
            var cardW = ControlTerminalLayout.CardWidth;
            var cardH = ControlTerminalLayout.CardHeight;
            var w = Mathf.Max(3f, cardW * holo.Width);
            var h = Mathf.Max(3f, cardH * holo.Height);

            // 열리는 창과 안쪽의 프레임 그림·흰빛.
            revealRect.sizeDelta = new Vector2(w, h);
            revealCopy.color = frameSprite != null ? new Color(1f, 1f, 1f, holo.CopyAlpha) : new Color(CardFill.r, CardFill.g, CardFill.b, holo.CopyAlpha);
            flashFrame.color = new Color(0.86f, 1f, 1f, 0.26f * holo.Flash);
            flashEdge.color = new Color(1f, 1f, 1f, holo.Flash);

            // 번짐.
            var glowColor = new Color(EdgeTeal.r, EdgeTeal.g, EdgeTeal.b, 0.44f * holo.Glow);
            for (int i = 0; i < glowImages.Length; i++) glowImages[i].color = glowColor;

            // 창을 따라가는 사각 테두리: 얇은 두께에 흰색에서 청록색으로 가라앉는다.
            var edgeVisible = holo.EdgeAlpha > 0.001f && h > 14f;
            edgeImage.enabled = edgeVisible;
            if (edgeVisible)
            {
                edgeRect.sizeDelta = new Vector2(w + 2f, h + 2f);
                var edgeColor = Color.Lerp(EdgeTeal, Color.white, Mathf.Clamp01(holo.Flash * 1.2f));
                edgeImage.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b, holo.EdgeAlpha);
            }

            // 수평 빛줄기.
            var lineVisible = holo.LineAlpha > 0.001f;
            lineImage.enabled = lineVisible;
            lineGlowImage.enabled = lineVisible;
            if (lineVisible)
            {
                lineRect.sizeDelta = new Vector2(w, LineCoreThickness);
                lineGlowRect.sizeDelta = new Vector2(w + 40f, 22f);
                lineImage.color = new Color(1f, 1f, 1f, holo.LineAlpha);
                lineGlowImage.color = new Color(EdgeTeal.r, EdgeTeal.g, EdgeTeal.b, 0.8f * holo.LineAlpha);
            }

            // 위에서 아래로 훑는 스캔 빔.
            var beamVisible = holo.BeamAlpha > 0.001f;
            beamImage.enabled = beamVisible;
            beamGlowImage.enabled = beamVisible;
            if (beamVisible)
            {
                var y = Mathf.Lerp(cardH * 0.5f, -cardH * 0.5f, holo.BeamPosition);
                beamRect.anchoredPosition = new Vector2(0f, y);
                beamGlowRect.anchoredPosition = new Vector2(0f, y);
                beamImage.color = new Color(1f, 1f, 1f, 0.9f * holo.BeamAlpha);
                beamGlowImage.color = new Color(EdgeTeal.r, EdgeTeal.g, EdgeTeal.b, 0.5f * holo.BeamAlpha);
            }

            // 잔상 테두리.
            for (int i = 0; i < ghostRects.Length; i++)
            {
                var ghost = intro ? ControlTerminalTimeline.IntroGhost(i, time) : ControlTerminalTimeline.OutroGhost(i, time);
                var visible = ghost.Alpha > 0.001f;
                ghostImages[i].enabled = visible;
                if (!visible) continue;
                ghostRects[i].sizeDelta = new Vector2(cardW * (1f + ghost.Spread), cardH * (1f + ghost.Spread));
                var tint = Color.Lerp(EdgeTeal, Color.white, i == 0 ? 0.55f : 0.15f);
                ghostImages[i].color = new Color(tint.r, tint.g, tint.b, ghost.Alpha);
            }

            // 모서리 반짝임.
            var flareVisible = holo.Flare > 0.001f;
            for (int i = 0; i < flareRects.Length; i++) flareImages[i].enabled = flareVisible;
            if (flareVisible)
            {
                var halfW = cardW * 0.5f - 4f;
                var halfH = cardH * 0.5f - 4f;
                var size = holo.Flare;
                for (int corner = 0; corner < 4; corner++)
                {
                    var position = new Vector2((corner & 1) == 1 ? halfW : -halfW, (corner & 2) == 2 ? halfH : -halfH);
                    var horizontal = flareRects[corner * 2];
                    var vertical = flareRects[corner * 2 + 1];
                    horizontal.anchoredPosition = position;
                    vertical.anchoredPosition = position;
                    horizontal.sizeDelta = new Vector2(FlareLength * size, 7f);
                    vertical.sizeDelta = new Vector2(7f, FlareLength * size);
                    var color = new Color(0.9f, 1f, 1f, holo.Flare);
                    flareImages[corner * 2].color = color;
                    flareImages[corner * 2 + 1].color = color;
                }
            }
        }

        private void SetHoloShown(bool shown)
        {
            if (holoShown == shown) return;
            holoShown = shown;
            holoBack.gameObject.SetActive(shown);
            holoFront.gameObject.SetActive(shown);
        }

        private void ApplySwap()
        {
            var alpha = ControlTerminalTimeline.PageSwapAlpha(swapTime);
            keysGroup.alpha = alpha;
            title.alpha = alpha;
            description.alpha = alpha;
        }

        // ------------------------------------------------------------------ UI 도우미

        private static RectTransform NewRect(string name, Transform parent, int layer)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>카드 가운데를 기준으로 크기를 가진 사각을 만든다.</summary>
        private static void Center(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Image AddImage(RectTransform rect, Sprite sprite, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            return image;
        }
    }

    /// <summary>하단 버튼 같은 일반 UI에 붙어 호버·포커스 동안 자동 시연을 멈춘다. 입력은 가로채지 않는다.</summary>
    public sealed class ControlTerminalFocusRelay : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private ControlTerminalView view;
        private bool pointer;
        private bool selected;

        public void Bind(ControlTerminalView target)
        {
            view = target;
        }

        public void OnPointerEnter(PointerEventData eventData) => Set(ref pointer, true);
        public void OnPointerExit(PointerEventData eventData) => Set(ref pointer, false);
        public void OnSelect(BaseEventData eventData) => Set(ref selected, true);
        public void OnDeselect(BaseEventData eventData) => Set(ref selected, false);
        private void OnDisable()
        {
            Set(ref pointer, false);
            Set(ref selected, false);
        }

        private void Set(ref bool flag, bool value)
        {
            if (flag == value) return;
            flag = value;
            if (view != null) view.AddExternalBlock(value ? 1 : -1);
        }
    }
}
