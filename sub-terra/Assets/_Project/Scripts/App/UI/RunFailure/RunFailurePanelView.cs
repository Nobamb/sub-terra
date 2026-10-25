using System;
using System.Collections;
using System.Collections.Generic;
using SubTerra.App.Audio;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.UI.RunFailure
{
    /// <summary>
    /// B-148: 탐사 실패(체력 소진 / 가스 중독) 팝업 연출.
    /// 가스 중독은 초록색, 구조 붕괴/추락 등은 빨간색 전용 팝업 이미지를 사용한다.
    /// 30fps 계단식 밴드 슬라이스·고스트 분리·바 스필 글리치 연출을 유지한다.
    /// </summary>
    public sealed class RunFailurePanelView : MonoBehaviour
    {
        // 전용 패널 이미지(1432x1098)와 같은 비율을 유지해야 프레임이 늘어나지 않는다.
        public static readonly Vector2 CardSize = new Vector2(1100f, 843f);

        private const int WindowBarCount = 12;
        private const int CardBandCount = 4;
        private const int ScreenBarCount = 14;
        private const float EdgeSpill = 64f;
        private const float BackdropAlpha = 0.78f;
        private const float TextJitterScale = 0.15f;
        private const float DecorJitterScale = 1.4f;

        // 카드 중심 기준 세로 배치(위 +). 패널 이미지 안쪽 여백(약 y +326 ~ -308) 안에서 요소 사이를 18px 이상 띄운다.
        private const float TitleCenterY = 284f;
        private const float DividerCenterY = 244f;
        private const float SubtitleCenterY = 212f;
        private const float CargoCenterY = 12f;
        private const float CargoHeaderHeight = 44f;
        private const float CargoRowHeight = 50f;
        private const float CargoRowSpacing = 8f;
        private const float BadgeCenterY = -194f;
        private const float ButtonCenterY = -265f;
        private static readonly Vector2 CargoBoxSize = new Vector2(880f, 328f);
        private static readonly Vector2 BadgeSize = new Vector2(740f, 48f);
        private static readonly Vector2 ConfirmButtonSize = new Vector2(320f, 58f);

        private static readonly Color BackdropBaseColor = new Color(0.01f, 0.015f, 0.028f, BackdropAlpha);

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private CanvasGroup windowCanvasGroup;
        [SerializeField] private Image windowBackgroundImage;
        [SerializeField] private RectTransform rgbSplitLeftRect;
        [SerializeField] private Image rgbSplitLeftImage;
        [SerializeField] private RectTransform rgbSplitRightRect;
        [SerializeField] private Image rgbSplitRightImage;
        [SerializeField] private RectTransform[] windowGlitchBars;
        [SerializeField] private Image[] windowGlitchBarImages;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI detailText;
        [SerializeField] private ScrollRect cargoScrollRect;
        [SerializeField] private RectTransform cargoContentRoot;
        [SerializeField] private TextMeshProUGUI emptyCargoText;
        [SerializeField] private TextMeshProUGUI preservationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI confirmButtonText;
        [SerializeField] private Image confirmButtonImage;
        [SerializeField] private Image blackoutOverlayImage;
        [SerializeField] private RectTransform screenGlitchRoot;
        [SerializeField] private Image screenScanlineImage;
        [SerializeField] private RectTransform[] screenGlitchBars;
        [SerializeField] private Image[] screenGlitchBarImages;
        [SerializeField] private RectTransform tvTopShutterRect;
        [SerializeField] private Image tvTopShutterImage;
        [SerializeField] private RectTransform tvBottomShutterRect;
        [SerializeField] private Image tvBottomShutterImage;
        [SerializeField] private RectTransform tvCenterBeamRect;
        [SerializeField] private Image tvCenterBeamImage;
        [SerializeField] private RunFailureVisualSettings visualSettings = new RunFailureVisualSettings();

        private readonly List<CargoRowWidgets> cargoRows = new List<CargoRowWidgets>();
        private readonly List<Image> panelSurfaces = new List<Image>();
        private Sprite gasPanelSprite;
        private Sprite impactPanelSprite;
        private Sprite activePanelSprite;

        private MineResetTimedPopupSkin popupSkin;
        private Image backdropImage;
        private Canvas panelCanvas;
        private GraphicRaycaster panelRaycaster;
        private Image backdropVignetteImage;
        private Image cardFlashImage;
        private Image cardScanlineImage;
        private RectTransform contentRoot;
        private CanvasGroup contentCanvasGroup;
        private Image warnLeftImage;
        private Image warnRightImage;
        private Image titleDividerImage;
        private Image titleDividerCoreImage;
        private TextMeshProUGUI subtitleText;
        private Image cargoSectionOutlineImage;
        private Image cargoHeaderBarImage;
        private TextMeshProUGUI cargoHeaderLeftText;
        private TextMeshProUGUI cargoHeaderRightText;
        private Image preservationPlateImage;
        private Image preservationOutlineImage;
        private Image preservationDiamondLeft;
        private Image preservationDiamondRight;
        private Image confirmButtonOutlineImage;
        private SliceBandWidgets[] glitchBands;
        private JitterBlock[] jitterBlocks;
        private bool jitterBlocksOffset;
        private Image screenVignetteImage;
        private Image screenCenterFlashImage;
        private Image tvTopSeamGlowImage;
        private Image tvBottomSeamGlowImage;
        private RectTransform tvCenterHaloRect;
        private Image tvCenterHaloImage;

        private GameDataCatalog dataCatalog;
        private RunFailureRevealTimeline revealTimeline;
        private RunFailureTransitionTimeline transitionTimeline;
        private Vector2 baseVisualAnchoredPosition;
        private bool basePositionCaptured;
        private bool isShowing;
        private bool isInTransition;
        private int lastHandledInputFrame = -1;
        private float idleGlitchClock;
        private int lastCardGlitchKey = int.MinValue;

        public event Action ConfirmTriggered;

        public RunFailureRevealTimeline ActiveRevealTimeline => revealTimeline;
        public RunFailureTransitionTimeline ActiveTransitionTimeline => transitionTimeline;
        public RunFailureVisualSettings VisualSettings => visualSettings;
        public bool IsShowing => isShowing;
        public bool IsRevealComplete => revealTimeline != null && revealTimeline.IsComplete;
        public bool IsConfirmed => revealTimeline != null && revealTimeline.IsConfirmed;
        public bool IsInTransition => isInTransition;
        public int ActiveCargoRowCount { get; private set; }
        public string DisplayedCauseTitle => titleText != null ? titleText.text : string.Empty;
        public string DisplayedPreservationText => preservationText != null ? preservationText.text : string.Empty;
        public string DisplayedEmptyCargoText => emptyCargoText != null ? emptyCargoText.text : string.Empty;

        private sealed class JitterBlock
        {
            public RectTransform[] Rects;
            public Vector2[] BasePositions;
            public float Scale;
        }

        private sealed class SliceBandWidgets
        {
            public RectTransform MaskRect;
            public RectTransform InnerRect;
            public Image TintOverlay;
        }

        private sealed class CargoRowWidgets
        {
            public GameObject Root;
            public CanvasGroup Group;
            public Image RowBackground;
            public Image RowOutline;
            public Image LeftAccentBar;
            public Image PulseGlow;
            public Image IconPlate;
            public Image IconImage;
            public TextMeshProUGUI NameText;
            public TextMeshProUGUI MinusPrefixText;
            public TextMeshProUGUI QuantityText;
        }

        private void Awake()
        {
            EnsureHierarchyReady();
            CaptureBasePosition();
            BindConfirmButton();
            Hide();
        }

        private void OnEnable()
        {
            BindConfirmButton();
        }

        private void OnDisable()
        {
            UnbindConfirmButton();
        }

        public void BindCatalog(GameDataCatalog catalog)
        {
            dataCatalog = catalog;
        }

        public bool HasRequiredReferences()
        {
            return canvasGroup != null
                && titleText != null
                && detailText != null;
        }

        public void EnsureHierarchyReady()
        {
            if (cargoScrollRect != null
                && cargoContentRoot != null
                && emptyCargoText != null
                && preservationText != null
                && confirmButton != null
                && glitchBands != null
                && glitchBands.Length == CardBandCount)
            {
                return;
            }

            var existingFont = titleText != null ? titleText.font : null;
            BuildOrUpdateHierarchy(existingFont);
        }

        public void BuildOrUpdateHierarchy(TMP_FontAsset font = null)
        {
            if (popupSkin == null)
            {
                popupSkin = Resources.Load<MineResetTimedPopupSkin>(MineResetTimedPopupSkin.ResourcePath);
            }

            var activeFont = font;
            if (activeFont == null && popupSkin != null && popupSkin.font != null)
            {
                activeFont = popupSkin.font;
            }

            if (activeFont == null)
            {
                activeFont = TMP_Settings.defaultFontAsset;
            }

            var rootRect = GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = gameObject.AddComponent<RectTransform>();
            }

            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            backdropImage = GetComponent<Image>();
            if (backdropImage == null)
            {
                backdropImage = gameObject.AddComponent<Image>();
            }

            backdropImage.color = BackdropBaseColor;
            backdropImage.raycastTarget = true;

            // 드론 말풍선(30_000)보다 앞에 보이도록 독립 정렬 캔버스를 쓴다.
            panelCanvas = GetComponent<Canvas>();
            if (panelCanvas == null)
            {
                panelCanvas = gameObject.AddComponent<Canvas>();
            }

            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = UiLayerPriority.RunFailureModal;
            panelRaycaster = GetComponent<GraphicRaycaster>();
            if (panelRaycaster == null)
            {
                panelRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            }

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }

            cargoRows.Clear();
            panelSurfaces.Clear();
            gasPanelSprite = Resources.Load<Sprite>("UI/RunFailure/gas-death-popup");
            impactPanelSprite = Resources.Load<Sprite>("UI/RunFailure/impact-death-popup");
            activePanelSprite = impactPanelSprite;
            lastCardGlitchKey = int.MinValue;

            // 1. 배경 비네팅
            var vignetteObj = CreateUiObject("BackdropVignette", transform);
            SetStretch(vignetteObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            backdropVignetteImage = vignetteObj.AddComponent<Image>();
            backdropVignetteImage.sprite = RunFailureGlitchArt.GetVignetteSprite();
            backdropVignetteImage.color = new Color(0.02f, 0.01f, 0.02f, 0.72f);
            backdropVignetteImage.raycastTarget = false;

            // 2. 암전 오버레이
            var blackoutObj = CreateUiObject("BlackoutOverlay", transform);
            SetStretch(blackoutObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            blackoutOverlayImage = blackoutObj.AddComponent<Image>();
            blackoutOverlayImage.color = new Color(0f, 0f, 0f, 0f);
            blackoutOverlayImage.raycastTarget = false;
            blackoutObj.SetActive(false);

            // 3. 원인별 전용 이미지로 구성한 메인 팝업 카드
            var visualObj = CreateUiObject("VisualRoot", transform);
            visualRoot = visualObj.GetComponent<RectTransform>();
            visualRoot.anchorMin = new Vector2(0.5f, 0.5f);
            visualRoot.anchorMax = new Vector2(0.5f, 0.5f);
            visualRoot.pivot = new Vector2(0.5f, 0.5f);
            visualRoot.sizeDelta = CardSize;
            visualRoot.anchoredPosition = Vector2.zero;
            baseVisualAnchoredPosition = Vector2.zero;
            basePositionCaptured = true;

            windowCanvasGroup = visualObj.AddComponent<CanvasGroup>();

            // 고스트 프레임 (좌/우 RGB 채널 분리)
            var frameSprite = activePanelSprite;

            var splitLeftObj = CreateUiObject("RgbSplitLeft", visualRoot);
            rgbSplitLeftRect = splitLeftObj.GetComponent<RectTransform>();
            SetCenteredRect(rgbSplitLeftRect, Vector2.zero, CardSize);
            rgbSplitLeftImage = splitLeftObj.AddComponent<Image>();
            rgbSplitLeftImage.sprite = frameSprite;
            rgbSplitLeftImage.color = new Color(1f, 0.28f, 0.24f, 0f);
            rgbSplitLeftImage.raycastTarget = false;
            splitLeftObj.SetActive(false);

            var splitRightObj = CreateUiObject("RgbSplitRight", visualRoot);
            rgbSplitRightRect = splitRightObj.GetComponent<RectTransform>();
            SetCenteredRect(rgbSplitRightRect, Vector2.zero, CardSize);
            rgbSplitRightImage = splitRightObj.AddComponent<Image>();
            rgbSplitRightImage.sprite = frameSprite;
            rgbSplitRightImage.color = new Color(0.24f, 0.86f, 1f, 0f);
            rgbSplitRightImage.raycastTarget = false;
            splitRightObj.SetActive(false);

            // 본체 패널·프레임 레이어
            BuildCardSurfaceLayers(visualRoot, true);

            // 가로 슬라이스 밴드 4개 (EmergencyRescuePopupView Band와 동일하게 패널·프레임 일부를 잘라 가로로 어긋나게 함)
            var bandRootObj = CreateUiObject("GlitchBands", visualRoot);
            var bandRootRect = bandRootObj.GetComponent<RectTransform>();
            SetCenteredRect(bandRootRect, Vector2.zero, CardSize);
            glitchBands = new SliceBandWidgets[CardBandCount];
            for (var i = 0; i < CardBandCount; i++)
            {
                glitchBands[i] = CreateSliceBand(bandRootRect, i);
            }

            // 순간 섬광 레이어: 패널 이미지 자체를 원인색으로 덧입혀 프레임 모양 그대로 번쩍인다(사각 윤곽선 아님).
            cardFlashImage = CreateCenteredImage(
                visualRoot,
                "CardFlash",
                activePanelSprite,
                Vector2.zero,
                CardSize,
                new Color(1f, 0.28f, 0.24f, 0f));
            cardFlashImage.type = Image.Type.Simple;

            // 글리치 중에만 은은하게 깔리는 주사선 질감
            var scanlineLayerObj = CreateUiObject("CardScanlines", visualRoot);
            SetCenteredRect(scanlineLayerObj.GetComponent<RectTransform>(), Vector2.zero, CardSize - new Vector2(150f, 160f));
            cardScanlineImage = scanlineLayerObj.AddComponent<Image>();
            cardScanlineImage.sprite = RunFailureGlitchArt.GetScanlineSprite();
            cardScanlineImage.type = Image.Type.Tiled;
            cardScanlineImage.color = new Color(1f, 1f, 1f, 0f);
            cardScanlineImage.raycastTarget = false;

            // 4. 본문 컨텐츠 영역 (제목, 원인 설명, 유실 자원 테이블, 보존율 배지, 확인 버튼)
            BuildCardContent(visualRoot, activeFont);
            BuildJitterBlocks();

            // 5. 카드 가장자리 스필 글리치 바 12개
            var barContainerObj = CreateUiObject("WindowGlitchBars", visualRoot);
            SetCenteredRect(barContainerObj.GetComponent<RectTransform>(), Vector2.zero, CardSize);
            windowGlitchBars = new RectTransform[WindowBarCount];
            windowGlitchBarImages = new Image[WindowBarCount];
            var barSprite = RunFailureGlitchArt.GetGlitchBlockSprite();
            for (var i = 0; i < WindowBarCount; i++)
            {
                var barObj = CreateUiObject("WindowGlitchBar_" + i, barContainerObj.transform);
                var barRect = barObj.GetComponent<RectTransform>();
                SetCenteredRect(barRect, Vector2.zero, new Vector2(80f, 4f));
                var barImg = barObj.AddComponent<Image>();
                barImg.sprite = barSprite;
                barImg.raycastTarget = false;
                barObj.SetActive(false);
                windowGlitchBars[i] = barRect;
                windowGlitchBarImages[i] = barImg;
            }

            // 6. 전체 화면 강한 글리치 오버레이 (확인 후 전환 2단계)
            BuildScreenGlitchHierarchy();

            // 7. CRT TV 켜짐 셔터 및 중심 형광 빔 (확인 후 전환 3단계)
            BuildTvTurnOnHierarchy();

            BindConfirmButton();
        }

        private void BuildCardSurfaceLayers(RectTransform parent, bool isMainCard)
        {
            var panel = CreateCenteredImage(
                parent,
                isMainCard ? "Panel" : "BandPanel",
                activePanelSprite,
                Vector2.zero,
                CardSize,
                Color.white);
            panel.type = Image.Type.Simple;
            panel.raycastTarget = isMainCard;
            panelSurfaces.Add(panel);
            if (isMainCard)
            {
                windowBackgroundImage = panel;
            }
        }

        private void ApplyPanelArt(RunFailureCause cause)
        {
            var sprite = cause == RunFailureCause.GasExposure ? gasPanelSprite : impactPanelSprite;
            if (activePanelSprite == sprite)
            {
                return;
            }

            activePanelSprite = sprite;
            foreach (var panel in panelSurfaces)
            {
                panel.sprite = sprite;
            }

            rgbSplitLeftImage.sprite = sprite;
            rgbSplitRightImage.sprite = sprite;
            if (cardFlashImage != null)
            {
                cardFlashImage.sprite = sprite;
            }
        }

        /// <summary>글리치 때 본문이 블록 단위로 가로로 어긋나도록 이동 대상과 기준 위치를 모아 둔다.</summary>
        private void BuildJitterBlocks()
        {
            var blocks = new List<JitterBlock>();
            // 글자가 들어 있는 블록은 거의 고정(배율 낮음), 장식만 크게 어긋나 글자는 읽히고 바깥만 일그러져 보이게 한다.
            AddJitterBlock(blocks, TextJitterScale, titleText != null ? titleText.rectTransform : null);
            AddJitterBlock(blocks, DecorJitterScale, warnLeftImage != null ? warnLeftImage.rectTransform : null);
            AddJitterBlock(blocks, DecorJitterScale, warnRightImage != null ? warnRightImage.rectTransform : null);
            AddJitterBlock(blocks, DecorJitterScale, titleDividerImage != null ? titleDividerImage.rectTransform : null, titleDividerCoreImage != null ? titleDividerCoreImage.rectTransform : null);
            AddJitterBlock(blocks, TextJitterScale, subtitleText != null ? subtitleText.rectTransform : null);
            AddJitterBlock(blocks, TextJitterScale, cargoScrollRect != null ? cargoScrollRect.transform.parent as RectTransform : null);
            AddJitterBlock(blocks, TextJitterScale, preservationPlateImage != null ? preservationPlateImage.rectTransform : null);
            AddJitterBlock(blocks, TextJitterScale, confirmButton != null ? confirmButton.transform as RectTransform : null);
            jitterBlocks = blocks.ToArray();
            jitterBlocksOffset = false;
        }

        private static void AddJitterBlock(List<JitterBlock> blocks, float scale, params RectTransform[] rects)
        {
            var valid = new List<RectTransform>();
            for (var i = 0; i < rects.Length; i++)
            {
                if (rects[i] != null)
                {
                    valid.Add(rects[i]);
                }
            }

            if (valid.Count == 0)
            {
                return;
            }

            var basePositions = new Vector2[valid.Count];
            for (var i = 0; i < valid.Count; i++)
            {
                basePositions[i] = valid[i].anchoredPosition;
            }

            blocks.Add(new JitterBlock { Rects = valid.ToArray(), BasePositions = basePositions, Scale = scale });
        }

        private void ApplyJitterBlocks(int seed, float amplitude)
        {
            if (jitterBlocks == null)
            {
                return;
            }

            for (var i = 0; i < jitterBlocks.Length; i++)
            {
                var block = jitterBlocks[i];
                // 일부 블록만 어긋나야 화면이 찢어진 느낌이 난다.
                var moves = RunFailureRevealTimeline.Hash01(seed, 91 + i) > 0.42f;
                var shiftX = moves
                    ? (RunFailureRevealTimeline.Hash01(seed, 101 + i) - 0.5f) * 2f * amplitude * block.Scale
                    : 0f;
                for (var j = 0; j < block.Rects.Length; j++)
                {
                    if (block.Rects[j] != null)
                    {
                        block.Rects[j].anchoredPosition = block.BasePositions[j] + new Vector2(shiftX, 0f);
                    }
                }
            }

            jitterBlocksOffset = true;
        }

        private void ResetJitterBlocks()
        {
            if (!jitterBlocksOffset || jitterBlocks == null)
            {
                return;
            }

            for (var i = 0; i < jitterBlocks.Length; i++)
            {
                var block = jitterBlocks[i];
                for (var j = 0; j < block.Rects.Length; j++)
                {
                    if (block.Rects[j] != null)
                    {
                        block.Rects[j].anchoredPosition = block.BasePositions[j];
                    }
                }
            }

            jitterBlocksOffset = false;
        }

        private SliceBandWidgets CreateSliceBand(RectTransform parent, int index)
        {
            var maskObj = CreateUiObject("Band_" + index, parent);
            var maskRect = maskObj.GetComponent<RectTransform>();
            SetCenteredRect(maskRect, Vector2.zero, new Vector2(CardSize.x + (EdgeSpill * 2f), 28f));
            maskObj.AddComponent<RectMask2D>();

            var innerObj = CreateUiObject("BandInner", maskRect);
            var innerRect = innerObj.GetComponent<RectTransform>();
            SetCenteredRect(innerRect, Vector2.zero, CardSize);
            BuildCardSurfaceLayers(innerRect, false);

            var tintOverlay = CreateCenteredImage(
                innerRect,
                "BandTint",
                RunFailureGlitchArt.GetGlitchBlockSprite(),
                Vector2.zero,
                CardSize,
                new Color(1f, 0.28f, 0.24f, 0.22f));

            maskObj.SetActive(false);
            return new SliceBandWidgets
            {
                MaskRect = maskRect,
                InnerRect = innerRect,
                TintOverlay = tintOverlay
            };
        }

        private void BuildCardContent(RectTransform parent, TMP_FontAsset font)
        {
            var contentObj = CreateUiObject("Content", parent);
            contentRoot = contentObj.GetComponent<RectTransform>();
            SetCenteredRect(contentRoot, Vector2.zero, CardSize);
            contentCanvasGroup = contentObj.AddComponent<CanvasGroup>();

            // 1. 상단 타이틀 + 좌우 경고 마름모 (EmergencyRescuePopupView 레이아웃 계승)
            titleText = CreateTextElement(contentRoot, "FailureTitle", font, 38f, TextAlignmentOptions.Center);
            titleText.fontStyle = FontStyles.Bold;
            titleText.characterSpacing = 3f;
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 28f;
            titleText.fontSizeMax = 42f;
            SetCenteredRect(titleText.rectTransform, new Vector2(0f, TitleCenterY), new Vector2(640f, 60f));

            var diamondSprite = RunFailureGlitchArt.GetDiamondSprite();
            warnLeftImage = CreateCenteredImage(
                contentRoot,
                "WarnL",
                diamondSprite,
                new Vector2(-225f, TitleCenterY),
                new Vector2(22f, 22f),
                new Color(1f, 0.28f, 0.24f, 0.95f));
            warnRightImage = CreateCenteredImage(
                contentRoot,
                "WarnR",
                diamondSprite,
                new Vector2(225f, TitleCenterY),
                new Vector2(22f, 22f),
                new Color(1f, 0.28f, 0.24f, 0.95f));

            // 2. 원인별 색상을 적용하는 타이틀 디바이더
            var dividerSprite = RunFailureGlitchArt.GetCrtBeamSprite();
            var dividerSize = new Vector2(700f, 4f);
            titleDividerImage = CreateCenteredImage(
                contentRoot,
                "TitleDivider",
                dividerSprite,
                new Vector2(0f, DividerCenterY),
                dividerSize,
                new Color(1f, 0.50f, 0.45f, 0.92f));

            titleDividerCoreImage = CreateCenteredImage(
                contentRoot,
                "TitleDividerCore",
                RunFailureGlitchArt.GetCrtBeamSprite(),
                new Vector2(0f, DividerCenterY),
                new Vector2(600f, 4f),
                new Color(1f, 0.35f, 0.30f, 0.85f));

            // 3. 부제 (원인별 상태 메시지)
            subtitleText = CreateTextElement(contentRoot, "FailureSubtitle", font, 21f, TextAlignmentOptions.Center);
            subtitleText.color = new Color(0.86f, 0.92f, 0.98f, 0.94f);
            subtitleText.enableAutoSizing = true;
            subtitleText.fontSizeMin = 15f;
            subtitleText.fontSizeMax = 21f;
            SetCenteredRect(subtitleText.rectTransform, new Vector2(0f, SubtitleCenterY), new Vector2(880f, 36f));

            // 4. 유실 자원 목록 홀로그램 테이블 (팝업 내부 중앙 영역)
            var cargoBoxObj = CreateUiObject("LostCargoSection", contentRoot);
            var cargoBoxRect = cargoBoxObj.GetComponent<RectTransform>();
            SetCenteredRect(cargoBoxRect, new Vector2(0f, CargoCenterY), CargoBoxSize);
            var cargoBoxImg = cargoBoxObj.AddComponent<Image>();
            cargoBoxImg.sprite = RunFailureGlitchArt.GetChamferFillSprite();
            cargoBoxImg.type = Image.Type.Sliced;
            cargoBoxImg.color = new Color(0.016f, 0.026f, 0.044f, 0.90f);
            cargoBoxImg.raycastTarget = true;

            cargoSectionOutlineImage = CreateCenteredImage(
                cargoBoxRect,
                "CargoTableOutline",
                RunFailureGlitchArt.GetChamferOutlineSprite(),
                Vector2.zero,
                CargoBoxSize,
                new Color(1f, 0.28f, 0.24f, 0.48f));

            // 테이블 상단 헤더 바
            var headerObj = CreateUiObject("CargoTableHeader", cargoBoxRect);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector2(8f, -(CargoHeaderHeight + 6f));
            headerRect.offsetMax = new Vector2(-8f, -6f);
            cargoHeaderBarImage = headerObj.AddComponent<Image>();
            cargoHeaderBarImage.sprite = RunFailureGlitchArt.GetChamferFillSprite();
            cargoHeaderBarImage.type = Image.Type.Sliced;
            cargoHeaderBarImage.color = new Color(1f, 0.28f, 0.24f, 0.14f);
            cargoHeaderBarImage.raycastTarget = false;

            cargoHeaderLeftText = CreateTextElement(
                headerRect,
                "HeaderLeft",
                font,
                18f,
                TextAlignmentOptions.MidlineLeft);
            cargoHeaderLeftText.fontStyle = FontStyles.Bold;
            cargoHeaderLeftText.text = "유실 자원 목록";
            SetStretch(cargoHeaderLeftText.rectTransform, new Vector2(24f, 0f), new Vector2(-240f, 0f));

            cargoHeaderRightText = CreateTextElement(
                headerRect,
                "HeaderRight",
                font,
                18f,
                TextAlignmentOptions.MidlineRight);
            cargoHeaderRightText.fontStyle = FontStyles.Bold;
            cargoHeaderRightText.text = "유실 수량";
            SetStretch(cargoHeaderRightText.rectTransform, new Vector2(240f, 0f), new Vector2(-24f, 0f));

            // 스크롤 뷰포트
            var scrollObj = CreateUiObject("CargoScrollView", cargoBoxRect);
            var scrollRectTransform = scrollObj.GetComponent<RectTransform>();
            SetStretch(scrollRectTransform, new Vector2(12f, 12f), new Vector2(-12f, -(CargoHeaderHeight + 14f)));
            cargoScrollRect = scrollObj.AddComponent<ScrollRect>();
            cargoScrollRect.horizontal = false;
            cargoScrollRect.vertical = true;
            cargoScrollRect.movementType = ScrollRect.MovementType.Clamped;
            cargoScrollRect.scrollSensitivity = 26f;

            var viewportObj = CreateUiObject("Viewport", scrollRectTransform);
            var viewportRect = viewportObj.GetComponent<RectTransform>();
            SetStretch(viewportRect, Vector2.zero, Vector2.zero);
            viewportObj.AddComponent<RectMask2D>();
            var viewportImg = viewportObj.AddComponent<Image>();
            viewportImg.color = Color.clear;
            viewportImg.raycastTarget = true;

            var scrollContentObj = CreateUiObject("Content", viewportRect);
            cargoContentRoot = scrollContentObj.GetComponent<RectTransform>();
            cargoContentRoot.anchorMin = new Vector2(0f, 1f);
            cargoContentRoot.anchorMax = new Vector2(1f, 1f);
            cargoContentRoot.pivot = new Vector2(0.5f, 1f);
            cargoContentRoot.anchoredPosition = Vector2.zero;
            cargoContentRoot.sizeDelta = Vector2.zero;
            var vlg = scrollContentObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = CargoRowSpacing;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = scrollContentObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            cargoScrollRect.viewport = viewportRect;
            cargoScrollRect.content = cargoContentRoot;

            // 빈 화물 안내 문구
            emptyCargoText = CreateTextElement(
                cargoBoxRect,
                "EmptyCargoText",
                font,
                22f,
                TextAlignmentOptions.Center);
            emptyCargoText.color = new Color(0.75f, 0.82f, 0.90f, 0.92f);
            emptyCargoText.text = RunFailureRevealTimeline.EmptyCargoMessage;
            SetStretch(emptyCargoText.rectTransform, new Vector2(24f, 20f), new Vector2(-24f, -(CargoHeaderHeight + 14f)));

            // 5. 드론 구조 보존율 하단 배지
            var presPlateObj = CreateUiObject("PreservationBadge", contentRoot);
            var presPlateRect = presPlateObj.GetComponent<RectTransform>();
            SetCenteredRect(presPlateRect, new Vector2(0f, BadgeCenterY), BadgeSize);
            preservationPlateImage = presPlateObj.AddComponent<Image>();
            preservationPlateImage.sprite = RunFailureGlitchArt.GetChamferFillSprite();
            preservationPlateImage.type = Image.Type.Sliced;
            preservationPlateImage.color = new Color(0.03f, 0.05f, 0.08f, 0.90f);
            preservationPlateImage.raycastTarget = false;

            preservationOutlineImage = CreateCenteredImage(
                presPlateRect,
                "PreservationOutline",
                RunFailureGlitchArt.GetChamferOutlineSprite(),
                Vector2.zero,
                BadgeSize,
                new Color(1f, 0.28f, 0.24f, 0.42f));

            preservationDiamondLeft = CreateCenteredImage(
                presPlateRect,
                "PresDiamondL",
                diamondSprite,
                new Vector2((-BadgeSize.x * 0.5f) + 28f, 0f),
                new Vector2(13f, 13f),
                new Color(1f, 0.28f, 0.24f, 0.85f));
            preservationDiamondRight = CreateCenteredImage(
                presPlateRect,
                "PresDiamondR",
                diamondSprite,
                new Vector2((BadgeSize.x * 0.5f) - 28f, 0f),
                new Vector2(13f, 13f),
                new Color(1f, 0.28f, 0.24f, 0.85f));

            preservationText = CreateTextElement(
                presPlateRect,
                "PreservationText",
                font,
                22f,
                TextAlignmentOptions.Center);
            preservationText.fontStyle = FontStyles.Bold;
            preservationText.color = new Color(0.92f, 0.96f, 1f, 0.98f);
            SetStretch(preservationText.rectTransform, new Vector2(48f, 2f), new Vector2(-48f, -2f));

            // 호환용 숨김 detailText (기존 직렬화/정적 테스트 호환성 유지)
            detailText = CreateTextElement(
                contentRoot,
                "FailureDetail",
                font,
                16f,
                TextAlignmentOptions.Center);
            detailText.color = new Color(1f, 1f, 1f, 0f);
            SetCenteredRect(detailText.rectTransform, new Vector2(0f, BadgeCenterY), new Vector2(640f, 40f));

            // 6. 전용 패널에 맞춘 확인 버튼
            var buttonSize = ConfirmButtonSize;
            var buttonObj = CreateUiObject("ConfirmButton", contentRoot);
            var buttonRect = buttonObj.GetComponent<RectTransform>();
            SetCenteredRect(buttonRect, new Vector2(0f, ButtonCenterY), buttonSize);

            confirmButtonOutlineImage = CreateCenteredImage(
                buttonRect,
                "ButtonOutline",
                RunFailureGlitchArt.GetChamferOutlineSprite(),
                Vector2.zero,
                buttonSize + new Vector2(4f, 4f),
                new Color(1f, 0.28f, 0.24f, 0.65f));

            confirmButtonImage = buttonObj.AddComponent<Image>();
            confirmButtonImage.sprite = RunFailureGlitchArt.GetChamferFillSprite();
            confirmButtonImage.type = Image.Type.Sliced;
            confirmButtonImage.color = new Color(0.36f, 0.10f, 0.09f, 1f);

            confirmButtonImage.raycastTarget = true;
            confirmButton = buttonObj.AddComponent<Button>();
            confirmButton.targetGraphic = confirmButtonImage;
            confirmButton.transition = Selectable.Transition.None;
            var nav = confirmButton.navigation;
            nav.mode = Navigation.Mode.None;
            confirmButton.navigation = nav;

            var hoverSprite = RunFailureGlitchArt.GetChamferOutlineSprite();
            var hoverOverlay = CreateCenteredImage(
                buttonRect,
                "HoverOverlay",
                hoverSprite,
                Vector2.zero,
                buttonSize,
                new Color(1f, 1f, 1f, 0f));
            SetStretch(hoverOverlay.rectTransform, Vector2.zero, Vector2.zero);
            buttonObj.AddComponent<MenuSpriteButtonSkin>().SetOverlay(hoverOverlay);

            confirmButtonText = CreateTextElement(
                buttonRect,
                "ConfirmLabel",
                font,
                27f,
                TextAlignmentOptions.Center);
            confirmButtonText.fontStyle = FontStyles.Bold;
            confirmButtonText.text = "확인";
            confirmButtonText.characterSpacing = 4f;
            SetStretch(confirmButtonText.rectTransform, Vector2.zero, Vector2.zero);
        }

        private void BuildScreenGlitchHierarchy()
        {
            var screenGlitchObj = CreateUiObject("ScreenGlitchRoot", transform);
            screenGlitchRoot = screenGlitchObj.GetComponent<RectTransform>();
            SetStretch(screenGlitchRoot, Vector2.zero, Vector2.zero);

            var vignetteObj = CreateUiObject("ScreenVignette", screenGlitchRoot);
            SetStretch(vignetteObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            screenVignetteImage = vignetteObj.AddComponent<Image>();
            screenVignetteImage.sprite = RunFailureGlitchArt.GetVignetteSprite();
            screenVignetteImage.raycastTarget = false;

            var scanlineObj = CreateUiObject("ScreenScanlines", screenGlitchRoot);
            SetStretch(scanlineObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            screenScanlineImage = scanlineObj.AddComponent<Image>();
            screenScanlineImage.sprite = RunFailureGlitchArt.GetScanlineSprite();
            screenScanlineImage.type = Image.Type.Tiled;
            screenScanlineImage.raycastTarget = false;

            var flashObj = CreateUiObject("ScreenCenterFlash", screenGlitchRoot);
            SetStretch(flashObj.GetComponent<RectTransform>(), new Vector2(-80f, -80f), new Vector2(80f, 80f));
            screenCenterFlashImage = flashObj.AddComponent<Image>();
            screenCenterFlashImage.sprite = RunFailureGlitchArt.GetHaloSprite();
            screenCenterFlashImage.raycastTarget = false;

            var barSprite = RunFailureGlitchArt.GetGlitchBlockSprite();
            screenGlitchBars = new RectTransform[ScreenBarCount];
            screenGlitchBarImages = new Image[ScreenBarCount];
            for (var i = 0; i < ScreenBarCount; i++)
            {
                var barObj = CreateUiObject("ScreenGlitchBar_" + i, screenGlitchRoot);
                var barRect = barObj.GetComponent<RectTransform>();
                SetStretch(barRect, Vector2.zero, Vector2.zero);
                var barImg = barObj.AddComponent<Image>();
                barImg.sprite = barSprite;
                barImg.raycastTarget = false;
                screenGlitchBars[i] = barRect;
                screenGlitchBarImages[i] = barImg;
            }

            screenGlitchObj.SetActive(false);
        }

        private void BuildTvTurnOnHierarchy()
        {
            var topShutterObj = CreateUiObject("TvTopShutter", transform);
            tvTopShutterRect = topShutterObj.GetComponent<RectTransform>();
            SetStretch(tvTopShutterRect, Vector2.zero, Vector2.zero);
            tvTopShutterImage = topShutterObj.AddComponent<Image>();
            tvTopShutterImage.color = Color.black;
            tvTopShutterImage.raycastTarget = false;

            var topSeamObj = CreateUiObject("TvTopSeamGlow", tvTopShutterRect);
            var topSeamRect = topSeamObj.GetComponent<RectTransform>();
            topSeamRect.anchorMin = new Vector2(0f, 0f);
            topSeamRect.anchorMax = new Vector2(1f, 0f);
            topSeamRect.pivot = new Vector2(0.5f, 0.5f);
            topSeamRect.sizeDelta = new Vector2(0f, 18f);
            tvTopSeamGlowImage = topSeamObj.AddComponent<Image>();
            tvTopSeamGlowImage.sprite = RunFailureGlitchArt.GetCrtBeamSprite();
            tvTopSeamGlowImage.raycastTarget = false;
            topShutterObj.SetActive(false);

            var bottomShutterObj = CreateUiObject("TvBottomShutter", transform);
            tvBottomShutterRect = bottomShutterObj.GetComponent<RectTransform>();
            SetStretch(tvBottomShutterRect, Vector2.zero, Vector2.zero);
            tvBottomShutterImage = bottomShutterObj.AddComponent<Image>();
            tvBottomShutterImage.color = Color.black;
            tvBottomShutterImage.raycastTarget = false;

            var bottomSeamObj = CreateUiObject("TvBottomSeamGlow", tvBottomShutterRect);
            var bottomSeamRect = bottomSeamObj.GetComponent<RectTransform>();
            bottomSeamRect.anchorMin = new Vector2(0f, 1f);
            bottomSeamRect.anchorMax = new Vector2(1f, 1f);
            bottomSeamRect.pivot = new Vector2(0.5f, 0.5f);
            bottomSeamRect.sizeDelta = new Vector2(0f, 18f);
            tvBottomSeamGlowImage = bottomSeamObj.AddComponent<Image>();
            tvBottomSeamGlowImage.sprite = RunFailureGlitchArt.GetCrtBeamSprite();
            tvBottomSeamGlowImage.raycastTarget = false;
            bottomShutterObj.SetActive(false);

            var haloObj = CreateUiObject("TvCenterHalo", transform);
            tvCenterHaloRect = haloObj.GetComponent<RectTransform>();
            tvCenterHaloRect.anchorMin = new Vector2(0f, 0.5f);
            tvCenterHaloRect.anchorMax = new Vector2(1f, 0.5f);
            tvCenterHaloRect.sizeDelta = new Vector2(0f, 96f);
            tvCenterHaloImage = haloObj.AddComponent<Image>();
            tvCenterHaloImage.sprite = RunFailureGlitchArt.GetHaloSprite();
            tvCenterHaloImage.raycastTarget = false;
            haloObj.SetActive(false);

            var beamObj = CreateUiObject("TvCenterBeam", transform);
            tvCenterBeamRect = beamObj.GetComponent<RectTransform>();
            tvCenterBeamRect.anchorMin = new Vector2(0f, 0.5f);
            tvCenterBeamRect.anchorMax = new Vector2(1f, 0.5f);
            tvCenterBeamRect.sizeDelta = new Vector2(0f, 16f);
            tvCenterBeamImage = beamObj.AddComponent<Image>();
            tvCenterBeamImage.sprite = RunFailureGlitchArt.GetCrtBeamSprite();
            tvCenterBeamImage.raycastTarget = false;
            beamObj.SetActive(false);
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static Image CreateCenteredImage(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            var obj = CreateUiObject(name, parent);
            SetCenteredRect(obj.GetComponent<RectTransform>(), anchoredPosition, sizeDelta);
            var img = obj.AddComponent<Image>();
            img.sprite = sprite;
            if (sprite != null && sprite.border.sqrMagnitude > 0f)
            {
                img.type = Image.Type.Sliced;
            }
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI CreateTextElement(
            Transform parent,
            string name,
            TMP_FontAsset font,
            float size,
            TextAlignmentOptions alignment)
        {
            var obj = CreateUiObject(name, parent);
            var text = obj.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        public void Show(PlayerRescueResultDto rescue)
        {
            if (rescue == null)
            {
                return;
            }

            EnsureHierarchyReady();
            CaptureBasePosition();
            EnsureRuntimeOverlaysReset();
            SetVisible(true);
            transform.SetAsLastSibling();

            isShowing = true;
            isInTransition = false;
            idleGlitchClock = 0f;
            lastCardGlitchKey = int.MinValue;
            lastHandledInputFrame = Time.frameCount;

            revealTimeline = new RunFailureRevealTimeline(rescue, visualSettings);
            transitionTimeline = new RunFailureTransitionTimeline(rescue.cause, visualSettings, false);

            BuildCargoRows(rescue.lostCargo, revealTimeline.GlitchColor);
            ApplyStaticTheme(revealTimeline.GlitchColor, revealTimeline.SecondarySplitColor);

            var initialFrame = revealTimeline.Evaluate(0f);
            ApplyRevealFrame(initialFrame);
            // 창 글리치는 0초에 시작한다.
            SfxPlayer.Play(SfxId.RunFailGlitch);
        }

        public void ShowSurfaceFallback()
        {
            EnsureHierarchyReady();
            CaptureBasePosition();
            EnsureRuntimeOverlaysReset();
            SetVisible(true);
            transform.SetAsLastSibling();

            isShowing = true;
            isInTransition = false;
            revealTimeline = null;
            transitionTimeline = null;

            if (windowCanvasGroup != null)
            {
                windowCanvasGroup.alpha = 1f;
            }

            if (visualRoot != null)
            {
                visualRoot.anchoredPosition = baseVisualAnchoredPosition;
                visualRoot.localScale = Vector3.one;
            }

            if (titleText != null)
            {
                titleText.text = "Scene 전환 실패 — 현장 안전 지점 복귀";
            }

            if (subtitleText != null)
            {
                subtitleText.text = "입력과 생존 상태를 복구했습니다. 엘리베이터에서 다시 귀환하세요.";
            }

            if (detailText != null)
            {
                detailText.text = "입력과 생존 상태를 복구했습니다. 엘리베이터에서 다시 귀환하세요.";
            }

            if (preservationText != null)
            {
                preservationText.text = "입력과 생존 상태를 복구했습니다. 엘리베이터에서 다시 귀환하세요.";
            }
        }

        public void Hide()
        {
            isShowing = false;
            isInTransition = false;
            EnsureRuntimeOverlaysReset();
            SetVisible(false);
        }

        private void Update()
        {
            if (!isShowing || isInTransition || revealTimeline == null)
            {
                return;
            }

            var dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (!revealTimeline.IsComplete && dt > 0f)
            {
                AdvanceReveal(dt);
            }
            else if (revealTimeline.IsComplete && !revealTimeline.IsConfirmed && dt > 0f)
            {
                idleGlitchClock += dt;
                var idleIntensity = RunFailureRevealTimeline.EvaluateIdle(idleGlitchClock, out var idleSeed);
                ApplyCardGlitchVisuals(
                    idleIntensity > 0.001f,
                    idleIntensity,
                    idleSeed,
                    revealTimeline.GlitchColor,
                    revealTimeline.SecondarySplitColor,
                    revealTimeline.GlitchColor,
                    Vector2.zero,
                    0);
            }

            if (Time.frameCount == lastHandledInputFrame)
            {
                return;
            }

            if (!revealTimeline.IsComplete)
            {
                if (WasAnySkipKeyPressedThisFrame())
                {
                    lastHandledInputFrame = Time.frameCount;
                    ConfirmOrSkip();
                }
            }
            else if (!revealTimeline.IsConfirmed)
            {
                if (WasConfirmKeyPressedThisFrame())
                {
                    lastHandledInputFrame = Time.frameCount;
                    ConfirmOrSkip();
                }
            }
        }

        /// <summary>테스트와 Update가 같은 경로로 실패 창 등장 연출을 진행한다.</summary>
        public void AdvanceReveal(float dt)
        {
            if (revealTimeline == null || revealTimeline.IsComplete || dt <= 0f)
            {
                return;
            }

            var previous = revealTimeline.ElapsedSeconds;
            var frame = revealTimeline.Advance(dt);
            PlayRowTicks(previous, revealTimeline.ElapsedSeconds);
            ApplyRevealFrame(frame);
        }

        // 잃은 자원 줄이 보이기 시작할 때마다 한 번. 스킵은 Advance를 거치지 않아 남은 줄은 무음이다.
        private void PlayRowTicks(float previous, float now)
        {
            for (var i = 0; i < revealTimeline.ItemCount; i++)
            {
                if (SfxPlayback.Crossed(previous, now, revealTimeline.GetItemStartTime(i)))
                {
                    SfxPlayer.Play(SfxId.RunFailRowTick);
                }
            }
        }

        /// <summary>
        /// 연출 진행 중이면 즉시 최종 상태로 스킵하고 false를 반환한다.
        /// 이미 최종 상태라면 확인을 확정하고 true를 반환한다.
        /// </summary>
        public bool ConfirmOrSkip()
        {
            if (!isShowing || isInTransition || revealTimeline == null)
            {
                return false;
            }

            if (!revealTimeline.TryConfirmOrSkip())
            {
                ApplyRevealFrame(revealTimeline.Evaluate(revealTimeline.TotalDuration));
                return false;
            }

            ApplyRevealFrame(revealTimeline.Evaluate(revealTimeline.TotalDuration));
            if (transitionTimeline != null)
            {
                transitionTimeline.BeginConfirm();
            }

            if (ConfirmTriggered != null)
            {
                ConfirmTriggered.Invoke();
            }

            return true;
        }

        public IEnumerator PlayTransitionRoutine(bool includeTvTurnOn, Action onMovePlayer)
        {
            return PlayTransitionRoutine(includeTvTurnOn, onMovePlayer, null);
        }

        /// <param name="stepProvider">한 프레임 진행량. null이면 비스케일 dt. 테스트가 큰 dt를 넣을 때 쓴다.</param>
        public IEnumerator PlayTransitionRoutine(bool includeTvTurnOn, Action onMovePlayer, Func<float> stepProvider)
        {
            isInTransition = true;
            if (transitionTimeline == null)
            {
                var fallbackCause = revealTimeline != null ? revealTimeline.Cause : RunFailureCause.Unknown;
                transitionTimeline = new RunFailureTransitionTimeline(fallbackCause, visualSettings, true);
            }
            else
            {
                transitionTimeline.BeginConfirm();
            }

            var targetDuration = includeTvTurnOn
                ? transitionTimeline.TotalDuration
                : transitionTimeline.PreSurfaceLoadDuration;

            ApplyTransitionFrame(transitionTimeline.Evaluate(0f));
            // 암전(Darken) 단계는 0초에 시작한다. 강한 글리치 단계는 암전 소리 꼬리가 이어져 무음이다.
            SfxPlayer.Play(SfxId.RunFailBlackout);

            while (transitionTimeline.ElapsedSeconds < targetDuration - 0.0001f)
            {
                var step = Mathf.Max(0.001f, stepProvider != null ? stepProvider() : Time.unscaledDeltaTime);
                var previous = transitionTimeline.ElapsedSeconds;
                var frame = transitionTimeline.Advance(step);
                if (includeTvTurnOn && transitionTimeline.CrossedTvTurnOn(previous, transitionTimeline.ElapsedSeconds))
                {
                    SfxPlayer.Play(SfxId.UiPopupOpen);
                }

                if (frame.ShouldMovePlayer && onMovePlayer != null)
                {
                    onMovePlayer.Invoke();
                }

                ApplyTransitionFrame(frame);
                yield return null;
            }

            if (!transitionTimeline.HasMovedPlayer && onMovePlayer != null)
            {
                var finalFrame = transitionTimeline.Advance(targetDuration);
                if (finalFrame.ShouldMovePlayer)
                {
                    onMovePlayer.Invoke();
                }
            }

            if (includeTvTurnOn)
            {
                ApplyTransitionFrame(transitionTimeline.Evaluate(transitionTimeline.TotalDuration));
            }

            isInTransition = false;
        }

        public void ApplyRevealFrame(RunFailureRevealFrame frame)
        {
            ApplyPanelArt(frame.Cause);
            ApplyStaticTheme(frame.GlitchColor, frame.SecondarySplitColor);

            if (titleText != null)
            {
                titleText.text = frame.CauseTitle;
                titleText.color = Color.Lerp(frame.GlitchColor, Color.white, 0.22f);
            }

            if (subtitleText != null)
            {
                subtitleText.text = frame.CauseSubtitle;
            }

            if (preservationText != null)
            {
                preservationText.text = frame.PreservationText;
            }

            if (detailText != null)
            {
                detailText.text = frame.HasLostCargo
                    ? frame.PreservationText
                    : frame.EmptyCargoText + "\n" + frame.PreservationText;
            }

            if (emptyCargoText != null)
            {
                emptyCargoText.text = frame.EmptyCargoText;
                emptyCargoText.gameObject.SetActive(!frame.HasLostCargo);
            }

            if (cargoScrollRect != null)
            {
                cargoScrollRect.gameObject.SetActive(frame.HasLostCargo);
            }

            if (visualRoot != null)
            {
                visualRoot.anchoredPosition = baseVisualAnchoredPosition + frame.WindowJitterOffset;
                visualRoot.localScale = Vector3.one;
            }

            if (windowCanvasGroup != null)
            {
                windowCanvasGroup.alpha = frame.WindowFlickerAlpha;
            }

            if (contentRoot != null)
            {
                contentRoot.anchoredPosition = frame.IsWindowGlitching
                    ? new Vector2(frame.WindowJitterOffset.x * 0.25f, 0f)
                    : Vector2.zero;
            }

            var showGlitch = frame.IsWindowGlitching && frame.WindowGlitchIntensity > 0.001f;
            var seed = frame.GlitchSeed != 0
                ? frame.GlitchSeed
                : Mathf.FloorToInt(frame.ElapsedSeconds / RunFailureRevealTimeline.StepSeconds) + 17;

            ApplyCardGlitchVisuals(
                showGlitch,
                frame.WindowGlitchIntensity,
                seed,
                frame.GlitchColor,
                frame.SecondarySplitColor,
                frame.GlitchTint,
                frame.RgbSplitOffset,
                1);

            var count = Mathf.Min(cargoRows.Count, frame.Items.Count);
            var activeCountingIndex = -1;
            for (var i = 0; i < count; i++)
            {
                var row = cargoRows[i];
                var itemFrame = frame.Items[i];
                if (row.Root != null && !row.Root.activeSelf)
                {
                    row.Root.SetActive(true);
                }

                if (row.Group != null)
                {
                    row.Group.alpha = itemFrame.IsVisible ? itemFrame.Alpha : 0f;
                }

                if (row.QuantityText != null)
                {
                    row.QuantityText.text = itemFrame.DisplayQuantity.ToString("N0");
                }

                var isCountingNow = itemFrame.IsVisible && !itemFrame.IsCompleted;
                if (isCountingNow)
                {
                    activeCountingIndex = i;
                }

                if (row.PulseGlow != null)
                {
                    var pulseAlpha = isCountingNow
                        ? Mathf.Sin(itemFrame.Progress * Mathf.PI) * 0.34f
                        : 0f;
                    row.PulseGlow.color = WithAlpha(frame.GlitchColor, pulseAlpha);
                }

                if (row.RowOutline != null)
                {
                    var outlineAlpha = isCountingNow ? 0.72f : 0.26f;
                    row.RowOutline.color = WithAlpha(frame.GlitchColor, outlineAlpha);
                }
            }

            // 항목이 많아 스크롤이 필요한 경우 현재 카운트업 중인 항목이 보이도록 자동 스크롤
            if (activeCountingIndex >= 4 && cargoScrollRect != null && count > 4 && !frame.IsComplete)
            {
                var normalizedScroll = 1f - Mathf.Clamp01((float)activeCountingIndex / Mathf.Max(1, count - 1));
                cargoScrollRect.verticalNormalizedPosition = normalizedScroll;
            }

            if (confirmButtonText != null)
            {
                confirmButtonText.text = "확인";
            }
        }

        public void ApplyTransitionFrame(RunFailureTransitionFrame frame)
        {
            if (windowCanvasGroup != null)
            {
                windowCanvasGroup.alpha = frame.WindowAlpha;
                windowCanvasGroup.interactable = false;
                windowCanvasGroup.blocksRaycasts = false;
            }

            var isDarkenPhase = frame.Phase == RunFailureTransitionPhase.Darken;
            if (visualRoot != null)
            {
                visualRoot.anchoredPosition = baseVisualAnchoredPosition + (isDarkenPhase ? frame.WindowJitterOffset : Vector2.zero);
                var stretchX = isDarkenPhase ? 1f + ((1f - frame.WindowScaleY) * 0.08f) : 1f;
                visualRoot.localScale = new Vector3(stretchX, Mathf.Max(0f, frame.WindowScaleY), 1f);
            }

            if (isDarkenPhase && frame.WindowGlitchIntensity > 0.001f)
            {
                var split = new Vector2(14f * frame.WindowGlitchIntensity, 2f * frame.WindowGlitchIntensity);
                ApplyCardGlitchVisuals(
                    true,
                    frame.WindowGlitchIntensity,
                    frame.GlitchSeed != 0 ? frame.GlitchSeed : 331,
                    frame.GlitchColor,
                    frame.SecondarySplitColor,
                    frame.GlitchColor,
                    split,
                    2);
            }
            else
            {
                ApplyCardGlitchVisuals(
                    false,
                    0f,
                    0,
                    frame.GlitchColor,
                    frame.SecondarySplitColor,
                    frame.GlitchColor,
                    Vector2.zero,
                    0);
            }

            var isTvPhase = frame.Phase == RunFailureTransitionPhase.TvTurnOn;
            var isDone = frame.Phase == RunFailureTransitionPhase.Completed;

            if (blackoutOverlayImage != null)
            {
                blackoutOverlayImage.gameObject.SetActive(!isTvPhase && !isDone && frame.BlackoutAlpha > 0.001f);
                blackoutOverlayImage.color = new Color(0f, 0f, 0f, frame.BlackoutAlpha);
            }

            UpdateHeavyScreenGlitchVisuals(frame);
            UpdateTvTurnOnVisuals(frame, isTvPhase && !isDone);
        }

        private void ApplyStaticTheme(Color accentColor, Color secondaryColor)
        {
            var brightAccent = Color.Lerp(accentColor, Color.white, 0.25f);
            var softAccent = Color.Lerp(accentColor, Color.white, 0.42f);

            if (backdropVignetteImage != null)
            {
                backdropVignetteImage.color = new Color(
                    accentColor.r * 0.22f,
                    accentColor.g * 0.22f,
                    accentColor.b * 0.22f,
                    0.78f);
            }

            if (warnLeftImage != null)
            {
                warnLeftImage.color = brightAccent;
            }

            if (warnRightImage != null)
            {
                warnRightImage.color = brightAccent;
            }

            if (titleDividerImage != null)
            {
                titleDividerImage.color = WithAlpha(softAccent, 0.94f);
            }

            if (titleDividerCoreImage != null)
            {
                titleDividerCoreImage.color = WithAlpha(brightAccent, 0.88f);
            }

            if (cargoSectionOutlineImage != null)
            {
                cargoSectionOutlineImage.color = WithAlpha(accentColor, 0.54f);
            }

            if (cargoHeaderBarImage != null)
            {
                cargoHeaderBarImage.color = WithAlpha(accentColor, 0.16f);
            }

            if (cargoHeaderLeftText != null)
            {
                cargoHeaderLeftText.color = softAccent;
            }

            if (cargoHeaderRightText != null)
            {
                cargoHeaderRightText.color = softAccent;
            }

            if (preservationOutlineImage != null)
            {
                preservationOutlineImage.color = WithAlpha(accentColor, 0.52f);
            }

            if (preservationDiamondLeft != null)
            {
                preservationDiamondLeft.color = brightAccent;
            }

            if (preservationDiamondRight != null)
            {
                preservationDiamondRight.color = brightAccent;
            }

            if (confirmButtonOutlineImage != null)
            {
                confirmButtonOutlineImage.color = WithAlpha(accentColor, 0.65f);
            }

            if (confirmButtonImage != null)
            {
                confirmButtonImage.color = new Color(
                    Mathf.Lerp(0.06f, accentColor.r, 0.28f),
                    Mathf.Lerp(0.06f, accentColor.g, 0.28f),
                    Mathf.Lerp(0.06f, accentColor.b, 0.28f),
                    1f);
            }
        }

        /// <summary>
        /// mode: 0 = 대기 중 가장자리 간헐 글리치, 1 = 창 등장 글리치, 2 = 확인 후 창 닫힘(Darken) 강한 글리치
        /// </summary>
        private void ApplyCardGlitchVisuals(
            bool active,
            float intensity,
            int seed,
            Color primaryColor,
            Color secondaryColor,
            Color flashTint,
            Vector2 rgbSplitOffset,
            int mode)
        {
            var clampedIntensity = active ? Mathf.Clamp01(intensity) : 0f;
            var showGlitch = active && clampedIntensity > 0.001f;

            if (!showGlitch)
            {
                if (rgbSplitLeftRect != null)
                {
                    rgbSplitLeftRect.gameObject.SetActive(false);
                }

                if (rgbSplitRightRect != null)
                {
                    rgbSplitRightRect.gameObject.SetActive(false);
                }

                if (cardFlashImage != null)
                {
                    cardFlashImage.color = WithAlpha(primaryColor, 0f);
                }

                if (cardScanlineImage != null)
                {
                    cardScanlineImage.color = WithAlpha(Color.white, 0f);
                }

                ResetJitterBlocks();

                if (windowGlitchBars != null)
                {
                    for (var i = 0; i < windowGlitchBars.Length; i++)
                    {
                        if (windowGlitchBars[i] != null && windowGlitchBars[i].gameObject.activeSelf)
                        {
                            windowGlitchBars[i].gameObject.SetActive(false);
                        }
                    }
                }

                if (glitchBands != null)
                {
                    for (var i = 0; i < glitchBands.Length; i++)
                    {
                        var band = glitchBands[i];
                        if (band != null && band.MaskRect != null && band.MaskRect.gameObject.activeSelf)
                        {
                            band.MaskRect.gameObject.SetActive(false);
                        }
                    }
                }

                lastCardGlitchKey = int.MinValue;
                return;
            }

            // 1. 좌/우 고스트 프레임 분리
            // 고스트·밴드·섬광은 본문 뒤에 깔리므로 세게 줘도 글자는 그대로 읽힌다.
            var ghostShift = rgbSplitOffset.sqrMagnitude > 0.01f
                ? rgbSplitOffset * 2.4f
                : new Vector2((mode == 0 ? 9f : 20f) * clampedIntensity, 0f);
            var ghostAlphaScale = mode == 0 ? 0.38f : 0.82f;

            if (rgbSplitLeftRect != null && rgbSplitLeftImage != null)
            {
                rgbSplitLeftRect.gameObject.SetActive(true);
                rgbSplitLeftRect.anchoredPosition = ghostShift;
                rgbSplitLeftImage.color = WithAlpha(primaryColor, ghostAlphaScale * clampedIntensity);
            }

            if (rgbSplitRightRect != null && rgbSplitRightImage != null)
            {
                rgbSplitRightRect.gameObject.SetActive(true);
                rgbSplitRightRect.anchoredPosition = -ghostShift;
                rgbSplitRightImage.color = WithAlpha(secondaryColor, (ghostAlphaScale * 0.9f) * clampedIntensity);
            }

            // 패널 이미지에 원인색을 곱해 덧입히므로 프레임·장식 모양대로 번쩍이고 면이 균일한 단색으로 칠해지지 않는다.
            if (cardFlashImage != null)
            {
                var flashScale = mode == 0 ? 0.0f : 0.70f;
                cardFlashImage.color = WithAlpha(flashTint, flashScale * clampedIntensity);
            }

            if (cardScanlineImage != null)
            {
                var scanScale = mode == 0 ? 0.05f : 0.12f;
                cardScanlineImage.color = WithAlpha(Color.Lerp(primaryColor, Color.white, 0.5f), scanScale * clampedIntensity);
            }

            // 30fps 스텝이 동일하면 바/밴드 지오메트리 재계산 생략
            var key = (mode * 1000000) + (seed * 64) + Mathf.RoundToInt(clampedIntensity * 25f);
            if (key == lastCardGlitchKey)
            {
                return;
            }

            lastCardGlitchKey = key;

            // 본문 블록이 가로로 어긋나는 왜곡(대기 중 가장자리 글리치에서는 흔들지 않는다)
            if (mode == 0)
            {
                ResetJitterBlocks();
            }
            else
            {
                ApplyJitterBlocks(seed, (mode == 2 ? 34f : 22f) * clampedIntensity);
            }

            // 2. 카드 가장자리 스필 및 내부 글리치 바 (EmergencyRescuePopupView 방식)
            if (windowGlitchBars != null && windowGlitchBarImages != null)
            {
                var totalBars = Mathf.Min(windowGlitchBars.Length, windowGlitchBarImages.Length);
                var maxActiveBars = mode == 0 ? Mathf.Min(6, totalBars) : totalBars;
                var activeBarCount = Mathf.Clamp(Mathf.CeilToInt(clampedIntensity * maxActiveBars), 1, maxActiveBars);

                for (var i = 0; i < totalBars; i++)
                {
                    var barRect = windowGlitchBars[i];
                    var barImage = windowGlitchBarImages[i];
                    if (barRect == null || barImage == null)
                    {
                        continue;
                    }

                    if (i >= activeBarCount)
                    {
                        if (barRect.gameObject.activeSelf)
                        {
                            barRect.gameObject.SetActive(false);
                        }

                        continue;
                    }

                    var h1 = RunFailureRevealTimeline.Hash01(seed, (i * 5) + 11);
                    var h2 = RunFailureRevealTimeline.Hash01(seed, (i * 5) + 12);
                    var h3 = RunFailureRevealTimeline.Hash01(seed, (i * 5) + 13);
                    var h4 = RunFailureRevealTimeline.Hash01(seed, (i * 5) + 14);
                    var h5 = RunFailureRevealTimeline.Hash01(seed, (i * 5) + 15);
                    var h6 = RunFailureRevealTimeline.Hash01(seed, (i * 5) + 16);
                    var h7 = RunFailureRevealTimeline.Hash01(seed, (i * 5) + 17);

                    // 바는 본문 위에 그려지므로 글자를 가리지 않게 프레임 둘레(좌우 가장자리·위아래 테두리)에만 둔다.
                    // 바깥 끝은 EdgeSpill까지 번지고, 안쪽 끝은 본문 영역(x≈±450) 밖에 머물도록 길이를 제한한다.
                    var width = mode == 0 ? 40f + (h1 * 80f) : 70f + (h1 * 70f);
                    var height = 6f + (h2 * 10f);
                    var side = h3 < 0.5f ? -1f : 1f;
                    var halfW = CardSize.x * 0.5f;
                    var halfH = CardSize.y * 0.5f;
                    float x;
                    float y;
                    if (mode == 0 || h5 < 0.6f)
                    {
                        // 좌우 가장자리: 바깥으로 번지며 프레임 두께 안쪽까지만 들어온다.
                        x = side * (halfW + (EdgeSpill * 0.8f) - (width * 0.5f));
                        y = (h4 - 0.5f) * 2f * (halfH - 40f);
                    }
                    else
                    {
                        // 위아래 테두리: 제목·버튼 영역 밖(프레임 두께 구간)에만 놓는다.
                        width = 130f + (h1 * 300f);
                        var yEdge = halfH - 36f + (EdgeSpill * 0.3f) - (h4 * 64f);
                        y = (h3 < 0.5f ? -1f : 1f) * yEdge;
                        x = (h6 - 0.5f) * 2f * (halfW - (width * 0.5f));
                    }

                    barRect.anchorMin = new Vector2(0.5f, 0.5f);
                    barRect.anchorMax = new Vector2(0.5f, 0.5f);
                    barRect.pivot = new Vector2(0.5f, 0.5f);
                    barRect.anchoredPosition = new Vector2(x, y);
                    barRect.sizeDelta = new Vector2(width, height);

                    var barColor = h6 < 0.68f ? primaryColor : secondaryColor;
                    if (h6 > 0.90f)
                    {
                        barColor = Color.Lerp(primaryColor, Color.white, 0.55f);
                    }

                    var alpha = (0.40f + (0.50f * h7)) * Mathf.Min(1f, clampedIntensity * 1.3f);
                    barImage.color = WithAlpha(barColor, alpha);
                    if (!barRect.gameObject.activeSelf)
                    {
                        barRect.gameObject.SetActive(true);
                    }
                }
            }

            // 3. 가로 슬라이스 밴드 (패널·프레임이 가로로 찢어져 어긋나는 연출)
            if (glitchBands != null)
            {
                var activeBandCount = mode == 0 || clampedIntensity < 0.14f
                    ? 0
                    : Mathf.Clamp(
                        Mathf.RoundToInt(clampedIntensity * CardBandCount),
                        mode == 2 ? 2 : 1,
                        CardBandCount);

                for (var i = 0; i < glitchBands.Length; i++)
                {
                    var band = glitchBands[i];
                    if (band == null || band.MaskRect == null || band.InnerRect == null)
                    {
                        continue;
                    }

                    if (i >= activeBandCount)
                    {
                        if (band.MaskRect.gameObject.activeSelf)
                        {
                            band.MaskRect.gameObject.SetActive(false);
                        }

                        continue;
                    }

                    var b1 = RunFailureRevealTimeline.Hash01(seed, (i * 7) + 31);
                    var b2 = RunFailureRevealTimeline.Hash01(seed, (i * 7) + 32);
                    var b3 = RunFailureRevealTimeline.Hash01(seed, (i * 7) + 33);
                    var b4 = RunFailureRevealTimeline.Hash01(seed, (i * 7) + 34);

                    var bandHeight = 30f + (b1 * (mode == 2 ? 110f : 80f));
                    var bandY = (b2 - 0.5f) * 2f * ((CardSize.y * 0.5f) - 64f);
                    var shiftSign = b3 < 0.5f ? -1f : 1f;
                    var shiftX = shiftSign * (28f + (b4 * (mode == 2 ? 80f : 56f))) * clampedIntensity;

                    band.MaskRect.anchoredPosition = new Vector2(0f, bandY);
                    band.MaskRect.sizeDelta = new Vector2(CardSize.x + (EdgeSpill * 2f), bandHeight);
                    band.InnerRect.anchoredPosition = new Vector2(shiftX, -bandY);

                    if (band.TintOverlay != null)
                    {
                        var bandColor = (i % 2 == 0) ? primaryColor : secondaryColor;
                        band.TintOverlay.color = WithAlpha(bandColor, 0.20f * clampedIntensity);
                    }

                    if (!band.MaskRect.gameObject.activeSelf)
                    {
                        band.MaskRect.gameObject.SetActive(true);
                    }
                }
            }
        }

        private void UpdateHeavyScreenGlitchVisuals(RunFailureTransitionFrame frame)
        {
            var active = frame.Phase == RunFailureTransitionPhase.HeavyGlitch
                && frame.HeavyGlitchIntensity > 0.001f;

            if (screenGlitchRoot != null)
            {
                screenGlitchRoot.gameObject.SetActive(active);
            }

            if (!active)
            {
                return;
            }

            var seed = frame.GlitchSeed != 0
                ? frame.GlitchSeed
                : Mathf.FloorToInt(frame.ElapsedSeconds / RunFailureRevealTimeline.StepSeconds) + 601;

            if (screenVignetteImage != null)
            {
                screenVignetteImage.color = WithAlpha(frame.GlitchColor, 0.55f * frame.HeavyGlitchIntensity);
            }

            if (screenScanlineImage != null)
            {
                if (screenScanlineImage.sprite == null)
                {
                    screenScanlineImage.sprite = RunFailureGlitchArt.GetScanlineSprite();
                }

                screenScanlineImage.color = WithAlpha(frame.GlitchColor, 0.28f * frame.HeavyGlitchIntensity);
            }

            if (screenCenterFlashImage != null)
            {
                var flashRoll = RunFailureRevealTimeline.Hash01(seed, 7);
                var flashAlpha = flashRoll > 0.52f ? 0.36f * frame.HeavyGlitchIntensity : 0.12f * frame.HeavyGlitchIntensity;
                screenCenterFlashImage.color = WithAlpha(frame.GlitchColor, flashAlpha);
            }

            if (screenGlitchBars == null || screenGlitchBarImages == null)
            {
                return;
            }

            var count = Mathf.Min(screenGlitchBars.Length, screenGlitchBarImages.Length);
            for (var i = 0; i < count; i++)
            {
                var barRect = screenGlitchBars[i];
                var barImage = screenGlitchBarImages[i];
                if (barRect == null || barImage == null)
                {
                    continue;
                }

                var h1 = RunFailureRevealTimeline.Hash01(seed, (i * 6) + 19);
                var h2 = RunFailureRevealTimeline.Hash01(seed, (i * 6) + 23);
                var h3 = RunFailureRevealTimeline.Hash01(seed, (i * 6) + 29);
                var h4 = RunFailureRevealTimeline.Hash01(seed, (i * 6) + 31);
                var h5 = RunFailureRevealTimeline.Hash01(seed, (i * 6) + 37);

                barRect.gameObject.SetActive(true);
                var yCenter = 0.04f + (0.92f * h1);
                var halfHeight = 0.004f + (0.032f * h2 * frame.HeavyGlitchIntensity);
                var leftInset = (h3 - 0.5f) * 140f;
                var rightInset = (h4 - 0.5f) * 140f;

                barRect.anchorMin = new Vector2(0f, Mathf.Clamp01(yCenter - halfHeight));
                barRect.anchorMax = new Vector2(1f, Mathf.Clamp01(yCenter + halfHeight));
                barRect.offsetMin = new Vector2(leftInset, 0f);
                barRect.offsetMax = new Vector2(rightInset, 0f);

                Color c;
                if (h5 > 0.86f)
                {
                    c = Color.Lerp(frame.GlitchColor, Color.white, 0.65f);
                }
                else if (h5 < 0.28f)
                {
                    c = frame.SecondarySplitColor;
                }
                else
                {
                    c = frame.GlitchColor;
                }

                c.a = (0.32f + (0.58f * h2)) * frame.HeavyGlitchIntensity;
                barImage.color = c;
            }
        }

        private void UpdateTvTurnOnVisuals(RunFailureTransitionFrame frame, bool active)
        {
            var seamColor = Color.Lerp(frame.GlitchColor, Color.white, 0.45f);
            var seamAlpha = Mathf.Clamp01(1.15f - frame.TvApertureOpenNormalized);

            if (tvTopShutterRect != null && tvTopShutterImage != null)
            {
                tvTopShutterRect.gameObject.SetActive(active);
                if (active)
                {
                    var topMinY = Mathf.Clamp01(0.5f + (frame.TvApertureOpenNormalized * 0.5f));
                    tvTopShutterRect.anchorMin = new Vector2(0f, topMinY);
                    tvTopShutterRect.anchorMax = Vector2.one;
                    tvTopShutterRect.offsetMin = Vector2.zero;
                    tvTopShutterRect.offsetMax = Vector2.zero;
                    tvTopShutterImage.color = Color.black;

                    if (tvTopSeamGlowImage != null)
                    {
                        tvTopSeamGlowImage.color = WithAlpha(seamColor, seamAlpha * 0.85f);
                    }
                }
            }

            if (tvBottomShutterRect != null && tvBottomShutterImage != null)
            {
                tvBottomShutterRect.gameObject.SetActive(active);
                if (active)
                {
                    var bottomMaxY = Mathf.Clamp01(0.5f - (frame.TvApertureOpenNormalized * 0.5f));
                    tvBottomShutterRect.anchorMin = Vector2.zero;
                    tvBottomShutterRect.anchorMax = new Vector2(1f, bottomMaxY);
                    tvBottomShutterRect.offsetMin = Vector2.zero;
                    tvBottomShutterRect.offsetMax = Vector2.zero;
                    tvBottomShutterImage.color = Color.black;

                    if (tvBottomSeamGlowImage != null)
                    {
                        tvBottomSeamGlowImage.color = WithAlpha(seamColor, seamAlpha * 0.85f);
                    }
                }
            }

            var showBeam = active && frame.TvLineAlpha > 0.001f;
            if (tvCenterHaloRect != null && tvCenterHaloImage != null)
            {
                tvCenterHaloRect.gameObject.SetActive(showBeam);
                if (showBeam)
                {
                    var halfWidth = Mathf.Clamp01(frame.TvLineWidthNormalized) * 0.52f;
                    tvCenterHaloRect.anchorMin = new Vector2(0.5f - halfWidth, 0.5f);
                    tvCenterHaloRect.anchorMax = new Vector2(0.5f + halfWidth, 0.5f);
                    tvCenterHaloRect.offsetMin = new Vector2(0f, -54f);
                    tvCenterHaloRect.offsetMax = new Vector2(0f, 54f);
                    tvCenterHaloImage.color = WithAlpha(frame.GlitchColor, frame.TvLineAlpha * 0.72f);
                }
            }

            if (tvCenterBeamRect != null && tvCenterBeamImage != null)
            {
                tvCenterBeamRect.gameObject.SetActive(showBeam);
                if (showBeam)
                {
                    if (tvCenterBeamImage.sprite == null)
                    {
                        tvCenterBeamImage.sprite = RunFailureGlitchArt.GetCrtBeamSprite();
                    }

                    var halfWidth = Mathf.Clamp01(frame.TvLineWidthNormalized) * 0.5f;
                    tvCenterBeamRect.anchorMin = new Vector2(0.5f - halfWidth, 0.5f);
                    tvCenterBeamRect.anchorMax = new Vector2(0.5f + halfWidth, 0.5f);
                    tvCenterBeamRect.offsetMin = new Vector2(0f, -9f);
                    tvCenterBeamRect.offsetMax = new Vector2(0f, 9f);

                    var beamColor = Color.Lerp(frame.GlitchColor, Color.white, 0.68f);
                    beamColor.a = frame.TvLineAlpha;
                    tvCenterBeamImage.color = beamColor;
                }
            }
        }

        private void BuildCargoRows(IReadOnlyList<CargoLossEntryDto> lostCargo, Color accentColor)
        {
            ActiveCargoRowCount = 0;
            if (cargoContentRoot == null)
            {
                return;
            }

            var validEntries = new List<CargoLossEntryDto>();
            if (lostCargo != null)
            {
                for (var i = 0; i < lostCargo.Count; i++)
                {
                    var entry = lostCargo[i];
                    if (entry != null && !string.IsNullOrEmpty(entry.mineralId) && entry.quantity > 0)
                    {
                        validEntries.Add(entry);
                    }
                }
            }

            ActiveCargoRowCount = validEntries.Count;
            EnsureRowPoolSize(validEntries.Count);

            var rowTint = Color.Lerp(accentColor, Color.white, 0.32f);
            for (var i = 0; i < cargoRows.Count; i++)
            {
                var row = cargoRows[i];
                if (i >= validEntries.Count)
                {
                    if (row.Root != null)
                    {
                        row.Root.SetActive(false);
                    }

                    continue;
                }

                var entry = validEntries[i];
                ResolveMineralPresentation(entry.mineralId, out var displayName, out var iconSprite);

                if (row.Root != null)
                {
                    row.Root.SetActive(true);
                }

                if (row.Group != null)
                {
                    row.Group.alpha = 0f;
                }

                if (row.RowBackground != null)
                {
                    var zebra = (i % 2 == 0) ? 0.84f : 0.72f;
                    row.RowBackground.color = new Color(0.042f, 0.062f, 0.102f, zebra);
                }

                if (row.RowOutline != null)
                {
                    row.RowOutline.color = WithAlpha(accentColor, 0.26f);
                }

                if (row.LeftAccentBar != null)
                {
                    row.LeftAccentBar.color = WithAlpha(accentColor, 0.90f);
                }

                if (row.PulseGlow != null)
                {
                    row.PulseGlow.color = WithAlpha(accentColor, 0f);
                }

                if (row.IconPlate != null)
                {
                    row.IconPlate.color = new Color(
                        Mathf.Lerp(0.08f, accentColor.r, 0.22f),
                        Mathf.Lerp(0.11f, accentColor.g, 0.22f),
                        Mathf.Lerp(0.16f, accentColor.b, 0.22f),
                        0.92f);
                }

                if (row.IconImage != null)
                {
                    row.IconImage.sprite = iconSprite;
                    row.IconImage.color = Color.white;
                }

                if (row.NameText != null)
                {
                    row.NameText.text = displayName;
                }

                if (row.MinusPrefixText != null)
                {
                    row.MinusPrefixText.color = WithAlpha(rowTint, 0.88f);
                }

                if (row.QuantityText != null)
                {
                    row.QuantityText.text = "0";
                    row.QuantityText.color = rowTint;
                }
            }

            if (cargoScrollRect != null)
            {
                cargoScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void EnsureRowPoolSize(int requiredCount)
        {
            if (cargoContentRoot == null)
            {
                return;
            }

            var font = titleText != null ? titleText.font : null;
            while (cargoRows.Count < requiredCount)
            {
                var index = cargoRows.Count;
                var rowObject = new GameObject(
                    "LostCargoRow_" + index,
                    typeof(RectTransform),
                    typeof(CanvasGroup),
                    typeof(Image),
                    typeof(HorizontalLayoutGroup),
                    typeof(LayoutElement));
                rowObject.transform.SetParent(cargoContentRoot, false);

                var rowBackground = rowObject.GetComponent<Image>();
                rowBackground.sprite = RunFailureGlitchArt.GetChamferFillSprite();
                rowBackground.type = Image.Type.Sliced;
                rowBackground.color = new Color(0.042f, 0.062f, 0.102f, 0.84f);
                rowBackground.raycastTarget = false;

                var layout = rowObject.GetComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(20, 20, 6, 6);
                layout.spacing = 14f;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                var rowElement = rowObject.GetComponent<LayoutElement>();
                rowElement.preferredHeight = CargoRowHeight;
                rowElement.minHeight = CargoRowHeight;

                // 장식 오버레이 (Layout Ignored): 테두리, 좌측 네온 바, 카운트업 펄스 광원
                var outlineObj = CreateUiObject("RowOutline", rowObject.transform);
                SetStretch(outlineObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
                var outlineLayout = outlineObj.AddComponent<LayoutElement>();
                outlineLayout.ignoreLayout = true;
                var rowOutline = outlineObj.AddComponent<Image>();
                rowOutline.sprite = RunFailureGlitchArt.GetChamferOutlineSprite();
                rowOutline.type = Image.Type.Sliced;
                rowOutline.color = new Color(1f, 0.28f, 0.24f, 0.26f);
                rowOutline.raycastTarget = false;

                var pulseObj = CreateUiObject("RowPulseGlow", rowObject.transform);
                SetStretch(pulseObj.GetComponent<RectTransform>(), new Vector2(4f, 2f), new Vector2(-4f, -2f));
                var pulseLayout = pulseObj.AddComponent<LayoutElement>();
                pulseLayout.ignoreLayout = true;
                var pulseGlow = pulseObj.AddComponent<Image>();
                pulseGlow.sprite = RunFailureGlitchArt.GetSoftGlowSprite();
                pulseGlow.color = new Color(1f, 0.28f, 0.24f, 0f);
                pulseGlow.raycastTarget = false;

                var leftBarObj = CreateUiObject("LeftAccentBar", rowObject.transform);
                var leftBarRect = leftBarObj.GetComponent<RectTransform>();
                leftBarRect.anchorMin = new Vector2(0f, 0f);
                leftBarRect.anchorMax = new Vector2(0f, 1f);
                leftBarRect.pivot = new Vector2(0f, 0.5f);
                leftBarRect.offsetMin = new Vector2(3f, 6f);
                leftBarRect.offsetMax = new Vector2(7f, -6f);
                var leftBarLayout = leftBarObj.AddComponent<LayoutElement>();
                leftBarLayout.ignoreLayout = true;
                var leftAccentBar = leftBarObj.AddComponent<Image>();
                leftAccentBar.color = new Color(1f, 0.28f, 0.24f, 0.90f);
                leftAccentBar.raycastTarget = false;

                // 아이콘 슬롯 플레이트 + 광물 아이콘
                var iconPlateObj = new GameObject(
                    "IconPlate",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(LayoutElement));
                iconPlateObj.transform.SetParent(rowObject.transform, false);
                var iconPlateElement = iconPlateObj.GetComponent<LayoutElement>();
                iconPlateElement.preferredWidth = 38f;
                iconPlateElement.preferredHeight = 38f;
                iconPlateElement.minWidth = 38f;
                iconPlateElement.minHeight = 38f;
                var iconPlateImage = iconPlateObj.GetComponent<Image>();
                iconPlateImage.sprite = RunFailureGlitchArt.GetChamferFillSprite();
                iconPlateImage.type = Image.Type.Sliced;
                iconPlateImage.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);
                iconPlateImage.raycastTarget = false;

                var iconObject = CreateUiObject("MineralIcon", iconPlateObj.transform);
                SetStretch(iconObject.GetComponent<RectTransform>(), new Vector2(4f, 4f), new Vector2(-4f, -4f));
                var iconImage = iconObject.AddComponent<Image>();
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;

                // 자원 이름
                var nameObject = new GameObject(
                    "MineralName",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI),
                    typeof(LayoutElement));
                nameObject.transform.SetParent(rowObject.transform, false);
                var nameElement = nameObject.GetComponent<LayoutElement>();
                nameElement.flexibleWidth = 1f;
                nameElement.minWidth = 140f;
                var nameText = nameObject.GetComponent<TextMeshProUGUI>();
                if (font != null)
                {
                    nameText.font = font;
                }

                nameText.fontSize = 22f;
                nameText.fontStyle = FontStyles.Bold;
                nameText.alignment = TextAlignmentOptions.MidlineLeft;
                nameText.color = new Color(0.93f, 0.96f, 1f, 1f);
                nameText.textWrappingMode = TextWrappingModes.NoWrap;
                nameText.overflowMode = TextOverflowModes.Ellipsis;
                nameText.raycastTarget = false;

                // 유실 부호 배지 (-)
                var minusObj = new GameObject(
                    "MinusPrefix",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI),
                    typeof(LayoutElement));
                minusObj.transform.SetParent(rowObject.transform, false);
                var minusElement = minusObj.GetComponent<LayoutElement>();
                minusElement.preferredWidth = 20f;
                minusElement.minWidth = 20f;
                var minusText = minusObj.GetComponent<TextMeshProUGUI>();
                if (font != null)
                {
                    minusText.font = font;
                }

                minusText.fontSize = 22f;
                minusText.fontStyle = FontStyles.Bold;
                minusText.alignment = TextAlignmentOptions.MidlineRight;
                minusText.text = "-";
                minusText.textWrappingMode = TextWrappingModes.NoWrap;
                minusText.raycastTarget = false;

                // 유실 수량 (카운트업)
                var quantityObject = new GameObject(
                    "LostQuantity",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI),
                    typeof(LayoutElement));
                quantityObject.transform.SetParent(rowObject.transform, false);
                var quantityElement = quantityObject.GetComponent<LayoutElement>();
                quantityElement.preferredWidth = 100f;
                quantityElement.minWidth = 76f;
                var quantityText = quantityObject.GetComponent<TextMeshProUGUI>();
                if (font != null)
                {
                    quantityText.font = font;
                }

                quantityText.fontSize = 24f;
                quantityText.fontStyle = FontStyles.Bold;
                quantityText.alignment = TextAlignmentOptions.MidlineRight;
                quantityText.color = Color.white;
                quantityText.textWrappingMode = TextWrappingModes.NoWrap;
                quantityText.raycastTarget = false;

                cargoRows.Add(new CargoRowWidgets
                {
                    Root = rowObject,
                    Group = rowObject.GetComponent<CanvasGroup>(),
                    RowBackground = rowBackground,
                    RowOutline = rowOutline,
                    LeftAccentBar = leftAccentBar,
                    PulseGlow = pulseGlow,
                    IconPlate = iconPlateImage,
                    IconImage = iconImage,
                    NameText = nameText,
                    MinusPrefixText = minusText,
                    QuantityText = quantityText
                });
            }
        }

        private void ResolveMineralPresentation(string mineralId, out string displayName, out Sprite iconSprite)
        {
            string catalogName = null;
            iconSprite = null;

            var catalog = dataCatalog;
            if (catalog == null && GameBootstrapper.Instance != null)
            {
                catalog = GameBootstrapper.Instance.Catalog as GameDataCatalog;
                if (catalog == null)
                {
                    catalog = GameBootstrapper.Instance.AssignedCatalog as GameDataCatalog;
                }
            }

            if (catalog != null
                && !string.IsNullOrEmpty(mineralId)
                && catalog.TryGetInventoryItem(mineralId, out var mineralData)
                && mineralData != null)
            {
                catalogName = mineralData.DisplayName;
                if (mineralData.Icon != null)
                {
                    iconSprite = mineralData.Icon;
                }
            }

            displayName = ItemDisplayNames.PreferDisplay(mineralId, catalogName);
            if (iconSprite == null)
            {
                iconSprite = RunFailureGlitchArt.GetFallbackMineralSprite();
            }
        }

        private void CaptureBasePosition()
        {
            if (basePositionCaptured || visualRoot == null)
            {
                return;
            }

            baseVisualAnchoredPosition = visualRoot.anchoredPosition;
            basePositionCaptured = true;
        }

        private void EnsureRuntimeOverlaysReset()
        {
            if (visualRoot != null && basePositionCaptured)
            {
                visualRoot.anchoredPosition = baseVisualAnchoredPosition;
                visualRoot.localScale = Vector3.one;
            }

            if (contentRoot != null)
            {
                contentRoot.anchoredPosition = Vector2.zero;
            }

            if (windowCanvasGroup != null)
            {
                windowCanvasGroup.alpha = 1f;
                windowCanvasGroup.interactable = true;
                windowCanvasGroup.blocksRaycasts = true;
            }

            if (contentCanvasGroup != null)
            {
                contentCanvasGroup.alpha = 1f;
            }

            ApplyCardGlitchVisuals(false, 0f, 0, Color.white, Color.white, Color.white, Vector2.zero, 0);

            if (blackoutOverlayImage != null)
            {
                blackoutOverlayImage.gameObject.SetActive(false);
            }

            if (screenGlitchRoot != null)
            {
                screenGlitchRoot.gameObject.SetActive(false);
            }

            if (tvTopShutterRect != null)
            {
                tvTopShutterRect.gameObject.SetActive(false);
            }

            if (tvBottomShutterRect != null)
            {
                tvBottomShutterRect.gameObject.SetActive(false);
            }

            if (tvCenterHaloRect != null)
            {
                tvCenterHaloRect.gameObject.SetActive(false);
            }

            if (tvCenterBeamRect != null)
            {
                tvCenterBeamRect.gameObject.SetActive(false);
            }
        }

        private void BindConfirmButton()
        {
            if (confirmButton == null)
            {
                return;
            }

            confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }

        private void UnbindConfirmButton()
        {
            if (confirmButton == null)
            {
                return;
            }

            confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        }

        private void OnConfirmButtonClicked()
        {
            lastHandledInputFrame = Time.frameCount;
            ConfirmOrSkip();
        }

        private static bool WasAnySkipKeyPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.anyKey.wasPressedThisFrame;
        }

        private static bool WasConfirmKeyPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            return keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame
                || keyboard.spaceKey.wasPressedThisFrame
                || keyboard.eKey.wasPressedThisFrame;
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;

            // 독립 캔버스는 숨긴 동안에도 전체 화면 배경이 클릭을 가로채지 않도록 직접 끈다.
            if (panelRaycaster != null)
            {
                panelRaycaster.enabled = visible;
            }

            if (backdropImage != null)
            {
                backdropImage.raycastTarget = visible;
            }
        }
    }
}
