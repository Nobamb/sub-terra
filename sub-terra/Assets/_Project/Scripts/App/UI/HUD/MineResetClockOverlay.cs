using System.Globalization;
using SubTerra.App.Save;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.App.UI.Tutorial;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 광산 초기화 전자시계(화면 최상단 중앙)와 3시간 만료 팝업.
    /// Bootstrap 수명에 붙어 Scene이 바뀌어도 유지되며, T 키로 시계만 열고 닫는다.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class MineResetClockOverlay : MonoBehaviour
    {
        private const int ClockSortingOrder = 900;
        private const int PopupSortingOrder = 32_000;
        private static readonly Color ClockPanelColor = new Color(0.02f, 0.04f, 0.03f, 0.92f);
        private static readonly Color ClockBorderColor = new Color(0.12f, 0.28f, 0.16f, 1f);
        private static readonly Color DigitColor = new Color(0.25f, 1f, 0.42f, 1f);
        private static readonly Color LabelColor = new Color(0.45f, 0.85f, 0.55f, 1f);
        private static readonly Color CardColor = new Color(0.02f, 0.035f, 0.05f, 0.98f);
        private static readonly Color PopupTitleColor = new Color(0.84f, 0.98f, 1f, 1f);
        private static readonly Color PopupBodyColor = new Color(0.8f, 0.88f, 0.92f, 1f);
        private static readonly Color PopupBackdropColor = new Color(0.01f, 0.015f, 0.025f, 1f);
        private const float PopupBackdropAlpha = 0.78f;

        // 알림 카드는 확인창(1332x1021)과 같은 비율로 줄인 크기다. 광산·육각형 좌표는 확인창 값을 균일 배율로 줄여 쓴다.
        private static readonly Vector2 PopupCardSize = new Vector2(960f, 736f);
        private const float CaveSourceY = 152.2f;
        private static readonly Vector2 CaveSourceSize = new Vector2(1272.3f, 337.6f);
        private static readonly Vector4[] CaveGlowSourceRects =
        {
            new Vector4(361.7f, 51.7f, 147f, 136.7f),
            new Vector4(-418.6f, 29.3f, 79.2f, 89.6f),
            new Vector4(-404.8f, 66f, 67.8f, 75.8f)
        };
        // 광산 배경은 프레임 안쪽을 채운다. 원본 가로세로 비율 그대로 폭 기준으로 키우고 좌우 가장자리만 살짝 잘라낸다.
        // 한 장으로는 위아래를 다 못 채우므로 아래에는 원본, 위에는 위아래를 뒤집은 천장 쪽 광물을 붙인다.
        private static readonly Vector2 BackgroundClipSize = new Vector2(936f, 696f);
        private const float CaveScale = 0.9f;
        private const float CaveStripEdgeY = 350f;
        // 육각형은 확인창 대비 약 1.4배(0.52 → 0.728)로 키워 상단 중앙에 둔다.
        private const float HexRootScale = 0.728f;
        private const float HexCenterY = 100f;
        private const float TitleY = 306f;
        private const float TitleDividerY = 272f;
        private const float HighlightY = -94f;
        private const float BodyY = -160f;
        private const float FooterY = -234f;
        private const float OkButtonY = -296f;
        private static readonly Color PopupHighlightColor = new Color(0.45f, 0.96f, 1f, 1f);
        private static readonly Color PopupFooterColor = new Color(0.62f, 0.8f, 0.86f, 1f);
        private static readonly Color ShadeColor = new Color(0.004f, 0.01f, 0.018f, 1f);
        private static readonly Color ClockColor = new Color(0.45f, 0.96f, 1f, 1f);
        private const float ClockSize = 220f;
        private const float SideLightX = 455f;
        private const float SideLightY = 20f;
        private static readonly Vector2 SideLightSize = new Vector2(300f, 560f);
        private static readonly Color SideLightColor = new Color(0.08f, 0.5f, 0.6f, 1f);
        private static readonly Vector2 HexMineSize = new Vector2(427.2f, 323.8f);
        private static readonly Vector4 HexBorderRect = new Vector4(-0.6f, 0.6f, 556.9f, 453.6f);
        private static readonly Vector4 HexRingsRect = new Vector4(0f, 16.1f, 643.1f, 321.5f);
        private const int MoteCount = 8;

        private GameObject clockRoot;
        private TMP_Text labelText;
        private TMP_Text clockText;
        private GameObject popupRoot;
        private TMP_Text popupTitle;
        private TMP_Text popupHighlight;
        private TMP_Text popupBody;
        private TMP_Text popupFooter;
        private Button popupOkButton;
        private MineResetPopupMotion popupMotion;
        private MineResetClockIntro clockIntro;
        private RectTransform popupCard;
        private Image popupBlocker;
        private bool sessionVisible;

        public static MineResetClockOverlay Create(Transform parent)
        {
            var root = new GameObject(
                "MineResetClockOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }
            else if (Application.isPlaying)
            {
                DontDestroyOnLoad(root);
            }

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ClockSortingOrder;
            canvas.pixelPerfect = true;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var overlay = root.AddComponent<MineResetClockOverlay>();
            overlay.Build();
            overlay.sessionVisible = true;
            return overlay;
        }

        public void SetSessionVisible(bool visible)
        {
            sessionVisible = visible;
            if (!visible && popupRoot != null)
            {
                CloseImmediately();
            }

            RefreshFromState();
        }

        public bool TryClosePopup(Canvas canvas)
        {
            if (popupRoot == null || !popupRoot.activeSelf
                || popupRoot.GetComponent<Canvas>() != canvas) return false;
            // 시계 연출 중에는 닫히지 않는다. 입력만 소비해 뒤 화면으로 새지 않게 한다.
            if (clockIntro != null && clockIntro.IsPlaying) return true;
            HidePopup();
            return true;
        }

        public void RefreshFromState()
        {
            if (clockRoot == null)
            {
                return;
            }

            var runtime = SaveRuntimeController.Instance;
            var clockOn = sessionVisible
                && runtime != null
                && runtime.ActiveSlot > 0
                && runtime.IsMineResetClockVisible;
            clockRoot.SetActive(clockOn);
            if (!clockOn)
            {
                return;
            }

            if (labelText != null)
            {
                labelText.text = LocalizationService.Get(
                    "mine_reset.clock.label",
                    "광산 초기화");
            }

            if (clockText != null)
            {
                clockText.text = MineResetService.FormatClock(
                    runtime.MineResetRemainingSeconds);
            }
        }

        public void ShowTimedResetPopup(bool fromMine)
        {
            if (popupRoot == null)
            {
                return;
            }

            RefreshPopupTexts(fromMine);

            // 이전 연출(닫는 중·시계 포함)이 남아 있으면 정리하고 처음부터 다시 재생한다.
            StopClockIntro();
            if (popupMotion != null)
            {
                popupMotion.ResetHidden();
            }

            popupRoot.SetActive(true);
            popupRoot.transform.SetAsLastSibling();
            PopupWindowSorting.BringToFront(popupRoot.GetComponent<Canvas>());

            if (clockIntro != null && popupCard != null && Application.isPlaying)
            {
                // 시계가 도는 동안 카드는 숨기고, 딤 블로커만 켜 두어 뒤 화면 입력을 막는다.
                popupCard.gameObject.SetActive(false);
                SetBackdropDim(0f);
                clockIntro.Play(() => OpenPopupCard(MineResetClockIntroTimeline.DimFloor));
                return;
            }

            OpenPopupCard(0f);
        }

        // 시계 연출이 끝나는 순간(또는 연출을 건너뛸 때) TV 켜짐 등장으로 넘어간다. 초기화 로직과는 무관하다.
        private void OpenPopupCard(float backdropFloor)
        {
            if (popupRoot == null || !popupRoot.activeSelf)
            {
                return;
            }

            if (popupCard != null)
            {
                popupCard.gameObject.SetActive(true);
            }

            if (popupMotion != null)
            {
                popupMotion.SetBackdropFloor(backdropFloor);
                popupMotion.PlayOpen();
            }
        }

        private void RefreshPopupTexts(bool fromMine)
        {
            if (popupTitle != null)
            {
                popupTitle.text = LocalizationService.Get("mine_reset.timed.title", "탐사 시간 종료");
            }

            if (popupHighlight != null)
            {
                popupHighlight.text = LocalizationService.Get("mine_reset.timed.highlight");
            }

            // 시간과 비용은 실제 설정(MineResetService)·현재 상태에서 읽는다.
            var hours = Mathf.RoundToInt((float)(MineResetService.CycleDurationSeconds / 3600d));
            if (popupBody != null)
            {
                popupBody.text = string.Format(
                    CultureInfo.InvariantCulture,
                    LocalizationService.Get(fromMine ? "mine_reset.timed.body.mine" : "mine_reset.timed.body.surface"),
                    hours);
            }

            if (popupFooter != null)
            {
                var runtime = SaveRuntimeController.Instance;
                var fee = runtime != null ? runtime.CurrentMineResetFee : MineResetService.BaseFeeGold;
                popupFooter.text = string.Format(
                    CultureInfo.InvariantCulture,
                    LocalizationService.Get("mine_reset.timed.footer"),
                    fee);
            }

            if (popupOkButton != null)
            {
                var okLabel = popupOkButton.GetComponentInChildren<TMP_Text>(true);
                if (okLabel != null)
                {
                    okLabel.text = LocalizationService.Get("mine_reset.timed.ok", "확인");
                }
            }
        }

        private void SetBackdropDim(float ratio)
        {
            if (popupBlocker == null)
            {
                return;
            }

            var color = popupBlocker.color;
            color.a = PopupBackdropAlpha * Mathf.Clamp01(ratio);
            popupBlocker.color = color;
        }

        private void StopClockIntro()
        {
            if (clockIntro != null)
            {
                clockIntro.Stop();
            }
        }

        public void DestroyOverlay()
        {
            if (this == null || gameObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void Update()
        {
            if (!sessionVisible || UiPauseGate.IsHeld)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null
                && keyboard.tKey.wasPressedThisFrame
                && !IsTypingInInputField())
            {
                SaveRuntimeController.Instance?.ToggleMineResetClock();
            }

            if (clockRoot != null && clockRoot.activeSelf)
            {
                RefreshFromState();
            }
        }

        private void Build()
        {
            var font = TMP_Settings.defaultFontAsset;
            BuildClock(font);
            BuildPopup(font);
            RefreshFromState();
        }

        private void BuildClock(TMP_FontAsset font)
        {
            clockRoot = new GameObject("ClockRoot", typeof(RectTransform), typeof(Image));
            clockRoot.transform.SetParent(transform, false);
            var rootRect = clockRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -12f);
            rootRect.sizeDelta = new Vector2(280f, 78f);

            var border = clockRoot.GetComponent<Image>();
            border.color = ClockBorderColor;
            border.raycastTarget = false;

            var inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(clockRoot.transform, false);
            var innerRect = inner.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(3f, 3f);
            innerRect.offsetMax = new Vector2(-3f, -3f);
            var innerImage = inner.GetComponent<Image>();
            innerImage.color = ClockPanelColor;
            innerImage.raycastTarget = false;

            labelText = CreateText(
                inner.transform,
                "Label",
                new Vector2(0f, -6f),
                new Vector2(260f, 22f),
                16f,
                font,
                LabelColor,
                FontStyles.Normal);
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.text = LocalizationService.Get("mine_reset.clock.label", "광산 초기화");

            clockText = CreateText(
                inner.transform,
                "Digits",
                new Vector2(0f, -32f),
                new Vector2(260f, 42f),
                34f,
                font,
                DigitColor,
                FontStyles.Bold);
            clockText.alignment = TextAlignmentOptions.Center;
            clockText.characterSpacing = 6f;
            clockText.fontStyle = FontStyles.Bold;
            clockText.text = "03:00:00";
        }

        private void BuildPopup(TMP_FontAsset defaultFont)
        {
            var skin = Resources.Load<MineResetTimedPopupSkin>(MineResetTimedPopupSkin.ResourcePath);
            if (skin == null)
            {
                Debug.LogWarning("[SubTerra] MineResetTimedPopupSkin missing: timed reset popup falls back to flat art.");
            }

            var font = skin != null && skin.font != null ? skin.font : defaultFont;

            popupRoot = new GameObject(
                "TimedResetPopup",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup),
                typeof(Image));
            popupRoot.transform.SetParent(transform, false);
            Stretch(popupRoot.GetComponent<RectTransform>());
            var popupCanvas = popupRoot.GetComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = Mathf.Max(PopupSortingOrder, UiLayerPriority.EmergencyRescueModal + 500);
            var rootGroup = popupRoot.GetComponent<CanvasGroup>();

            // 딤은 뒤 클릭만 막는다(눌러서 닫지 않는다). 알파는 시계 연출과 모션이 키운다.
            // 알파 0이어도 입력을 막도록 투명 메시 컬링을 끈다.
            var blocker = popupRoot.GetComponent<Image>();
            blocker.color = PopupBackdropColor;
            blocker.raycastTarget = true;
            blocker.canvasRenderer.cullTransparentMesh = false;
            popupBlocker = blocker;

            var card = CreateRect(popupRoot.transform, "Card", Vector2.zero, PopupCardSize);
            popupCard = card;

            // 1) 펼쳐지는 본체: RectMask2D 높이만 바뀌고 안의 그림은 제자리·제크기다.
            var body = CreateRect(card, "Body", Vector2.zero, PopupCardSize);
            body.gameObject.AddComponent<RectMask2D>();
            // 패널 그림이 살짝 비치므로 불투명 바탕을 한 겹 깔아 뒤쪽 화면 글자가 카드 안으로 비치지 않게 한다.
            CreateImage(body, "PanelBase", null, Vector2.zero, PopupCardSize, CardColor);
            CreateImage(body, "Panel", skin != null ? skin.panel : null, Vector2.zero, PopupCardSize, CardColor);

            // 프레임 안쪽 전체를 채우는 광산 배경. 팝업 안에서만 잘리고 화면 전체로 번지지 않는다.
            var background = CreateRect(body, "Background", Vector2.zero, BackgroundClipSize);
            background.gameObject.AddComponent<RectMask2D>();
            var caveBottom = CreateRect(background, "CaveBottom",
                new Vector2(0f, -CaveStripEdgeY + CaveSourceSize.y * CaveScale * 0.5f), CaveSourceSize);
            caveBottom.localScale = new Vector3(CaveScale, CaveScale, 1f);
            var caveTop = CreateRect(background, "CaveTop",
                new Vector2(0f, CaveStripEdgeY - CaveSourceSize.y * CaveScale * 0.5f), CaveSourceSize);
            // 위아래만 뒤집어 천장 쪽에도 같은 광물이 보이게 한다. 가로세로 비율은 그대로다.
            caveTop.localScale = new Vector3(CaveScale, -CaveScale, 1f);
            var caveGlows = new Image[CaveGlowSourceRects.Length * 2 + 2];
            var strips = new[] { caveBottom, caveTop };
            for (var strip = 0; strip < strips.Length; strip++)
            {
                CreateImage(strips[strip], "Cave", skin != null ? skin.cave : null, Vector2.zero, CaveSourceSize, Color.white);
                for (var i = 0; i < CaveGlowSourceRects.Length; i++)
                {
                    var r = CaveGlowSourceRects[i];
                    var sprite = skin != null && skin.caveGlows != null && i < skin.caveGlows.Length ? skin.caveGlows[i] : null;
                    caveGlows[strip * CaveGlowSourceRects.Length + i] = CreateImage(strips[strip], "CaveGlow" + i, sprite,
                        new Vector2(r.x, r.y - CaveSourceY), new Vector2(r.z, r.w), Color.white);
                }
            }

            // 양옆 광물 빛. 광물 발광과 같은 별도 레이어로 호흡하며(알파는 모션이 정한다) 본문·버튼은 건드리지 않는다.
            var sideRight = CreateImage(background, "SideLightRight", MineResetPopupArt.Soft("glow", 0.1f),
                new Vector2(SideLightX, SideLightY), SideLightSize, SideLightColor);
            var sideLeft = CreateImage(background, "SideLightLeft", MineResetPopupArt.Soft("glow", 0.1f),
                new Vector2(-SideLightX, SideLightY), SideLightSize, SideLightColor);
            caveGlows[CaveGlowSourceRects.Length * 2] = sideRight;
            caveGlows[CaveGlowSourceRects.Length * 2 + 1] = sideLeft;

            // 본문·버튼 뒤 어두운 그라데이션. 좌우 가장자리는 비워 광물 빛이 보이게 한다.
            CreateImage(background, "TitleShade", MineResetPopupArt.Soft("shade", 0.35f),
                new Vector2(0f, 300f), new Vector2(720f, 190f), new Color(ShadeColor.r, ShadeColor.g, ShadeColor.b, 0.55f));
            CreateImage(background, "TextShade", MineResetPopupArt.Soft("shade", 0.35f),
                new Vector2(0f, -215f), new Vector2(780f, 440f), new Color(ShadeColor.r, ShadeColor.g, ShadeColor.b, 0.88f));

            CreateImage(body, "Frame", skin != null ? skin.frame : null, Vector2.zero, PopupCardSize, Color.white);
            var frameFlash = CreateImage(body, "FrameFlash", skin != null ? skin.frameGlow : null, Vector2.zero, PopupCardSize, Color.white);

            // 2) TV 켜짐용 가로 빛과 펼쳐지는 위아래 가장자리 빛
            var scan = CreateImage(card, "ScanLine", skin != null ? skin.scanLine : null,
                Vector2.zero, new Vector2(PopupCardSize.x, 24f), Color.white);
            var edgeTop = CreateImage(card, "EdgeTop", skin != null ? skin.scanLine : null,
                new Vector2(0f, PopupCardSize.y / 2f), new Vector2(PopupCardSize.x * 0.96f, 20f), Color.white);
            var edgeBottom = CreateImage(card, "EdgeBottom", skin != null ? skin.scanLine : null,
                new Vector2(0f, -PopupCardSize.y / 2f), new Vector2(PopupCardSize.x * 0.96f, 20f), Color.white);

            // 3) 광산 입구 육각형: 루트만 균일 배율로 키우므로 비율이 유지된다. HexScale만 모션이 키운다.
            var hex = CreateRect(card, "Hex", new Vector2(0f, HexCenterY), new Vector2(HexRingsRect.z, HexBorderRect.w));
            hex.localScale = new Vector3(HexRootScale, HexRootScale, 1f);
            var rings = CreateImage(hex, "HexRings", skin != null ? skin.hexRings : null,
                new Vector2(HexRingsRect.x, HexRingsRect.y), new Vector2(HexRingsRect.z, HexRingsRect.w), Color.white);
            var hexScale = CreateRect(hex, "HexScale", Vector2.zero, new Vector2(HexBorderRect.z, HexBorderRect.w));
            var hexMine = CreateImage(hexScale, "HexMine", skin != null ? skin.hexMine : null, Vector2.zero, HexMineSize, Color.white);
            var tunnelGlow = CreateImage(hexScale, "HexTunnelGlow", skin != null ? skin.hexTunnelGlow : null, Vector2.zero, HexMineSize, Color.white);
            var crystalGlow = CreateImage(hexScale, "HexCrystalGlow", skin != null ? skin.hexCrystalGlow : null, Vector2.zero, HexMineSize, Color.white);
            var borderGlow = CreateImage(hexScale, "HexBorderGlow", skin != null ? skin.hexBorderGlow : null,
                new Vector2(HexBorderRect.x, HexBorderRect.y), new Vector2(HexBorderRect.z, HexBorderRect.w), Color.white);
            var border = CreateImage(hexScale, "HexBorder", skin != null ? skin.hexBorder : null,
                new Vector2(HexBorderRect.x, HexBorderRect.y), new Vector2(HexBorderRect.z, HexBorderRect.w), Color.white);
            var core = CreateImage(hex, "CoreGlow", skin != null ? skin.coreGlow : null, Vector2.zero, new Vector2(240f, 120f), Color.white);
            var motes = new Image[MoteCount];
            for (var i = 0; i < motes.Length; i++)
            {
                var size = 9f + (i % 3) * 3f;
                motes[i] = CreateImage(hex, "Mote" + i, skin != null ? skin.mote : null,
                    Vector2.zero, new Vector2(size, size), new Color(0.55f, 0.95f, 1f, 0f));
            }

            // 4) 글자·장식·버튼: 알파만 페이드한다(찌그러뜨리지 않는다).
            var contentRect = CreateRect(card, "Content", Vector2.zero, PopupCardSize);
            var contentGroup = contentRect.gameObject.AddComponent<CanvasGroup>();

            popupTitle = CreatePopupText(contentRect, "Title", new Vector2(0f, TitleY), new Vector2(600f, 60f),
                44f, font, PopupTitleColor, FontStyles.Bold);
            popupTitle.alignment = TextAlignmentOptions.Center;
            popupTitle.characterSpacing = 4f;
            popupTitle.textWrappingMode = TextWrappingModes.NoWrap;
            popupTitle.enableAutoSizing = true;
            popupTitle.fontSizeMin = 28f;
            popupTitle.fontSizeMax = 44f;
            if (skin != null && skin.titleGlowMaterial != null && popupTitle.font == skin.font)
            {
                popupTitle.fontSharedMaterial = skin.titleGlowMaterial;
            }

            CreateImage(contentRect, "TitleDivider", skin != null ? skin.titleDivider : null,
                new Vector2(0f, TitleDividerY), new Vector2(600f, 16.4f), Color.white);

            popupHighlight = CreatePopupText(contentRect, "Highlight", new Vector2(0f, HighlightY), new Vector2(760f, 44f),
                30f, font, PopupHighlightColor, FontStyles.Bold);
            popupHighlight.alignment = TextAlignmentOptions.Center;
            popupHighlight.textWrappingMode = TextWrappingModes.NoWrap;
            popupHighlight.enableAutoSizing = true;
            popupHighlight.fontSizeMin = 20f;
            popupHighlight.fontSizeMax = 30f;

            popupBody = CreatePopupText(contentRect, "Body", new Vector2(0f, BodyY), new Vector2(820f, 84f),
                22f, font, PopupBodyColor, FontStyles.Normal);
            popupBody.alignment = TextAlignmentOptions.Center;
            popupBody.textWrappingMode = TextWrappingModes.Normal;
            popupBody.enableAutoSizing = true;
            popupBody.fontSizeMin = 15f;
            popupBody.fontSizeMax = 22f;
            popupBody.lineSpacing = 10f;
            popupBody.overflowMode = TextOverflowModes.Truncate;

            popupFooter = CreatePopupText(contentRect, "Footer", new Vector2(0f, FooterY), new Vector2(700f, 30f),
                19f, font, PopupFooterColor, FontStyles.Normal);
            popupFooter.alignment = TextAlignmentOptions.Center;
            popupFooter.textWrappingMode = TextWrappingModes.NoWrap;
            popupFooter.enableAutoSizing = true;
            popupFooter.fontSizeMin = 13f;
            popupFooter.fontSizeMax = 19f;

            popupOkButton = CreateOkButton(contentRect, skin, font);
            popupOkButton.onClick.AddListener(HidePopup);

            popupMotion = popupRoot.AddComponent<MineResetPopupMotion>();
            popupMotion.Bind(new MineResetPopupMotion.Layers
            {
                RootGroup = rootGroup,
                Backdrop = blocker,
                BackdropAlpha = PopupBackdropAlpha,
                Card = card,
                Body = body,
                ScanLine = scan,
                EdgeTop = edgeTop,
                EdgeBottom = edgeBottom,
                FrameFlash = frameFlash,
                Content = contentGroup,
                HexScale = hexScale,
                HexMine = hexMine,
                HexBorder = border,
                HexBorderGlow = borderGlow,
                HexCrystalGlow = crystalGlow,
                HexTunnelGlow = tunnelGlow,
                HexRings = rings,
                CoreGlow = core,
                CaveGlows = caveGlows,
                Motes = motes
            });

            BuildClockIntro();
            popupRoot.SetActive(false);
        }

        // 카드 위에 겹치는 시계 연출. 숫자 없는 테두리와 시침 하나뿐이고 모두 입력을 받지 않는다.
        private void BuildClockIntro()
        {
            var introRect = CreateRect(popupRoot.transform, "ClockIntro", Vector2.zero, new Vector2(ClockSize * 1.6f, ClockSize * 1.6f));
            var visual = CreateRect(introRect, "Clock", Vector2.zero, new Vector2(ClockSize, ClockSize));
            var group = visual.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            CreateImage(visual, "ClockGlow", MineResetPopupArt.Soft("glow", 0.2f), Vector2.zero,
                new Vector2(ClockSize * 1.5f, ClockSize * 1.5f), new Color(ClockColor.r, ClockColor.g, ClockColor.b, 0.28f));
            CreateImage(visual, "ClockRing", MineResetPopupArt.ClockRing(), Vector2.zero,
                new Vector2(ClockSize, ClockSize), ClockColor);
            // 시침은 정사각형 이미지의 정중앙이 회전축이라, 같은 크기·같은 중심의 테두리와 축이 정확히 일치한다.
            var hand = CreateImage(visual, "ClockHand", MineResetPopupArt.ClockHand(), Vector2.zero,
                new Vector2(ClockSize, ClockSize), ClockColor);

            clockIntro = introRect.gameObject.AddComponent<MineResetClockIntro>();
            clockIntro.Bind(visual, group, hand.rectTransform, SetBackdropDim);
            introRect.gameObject.SetActive(false);
        }

        private Button CreateOkButton(Transform parent, MineResetTimedPopupSkin skin, TMP_FontAsset font)
        {
            var rect = CreateRect(parent, "OkButton", new Vector2(0f, OkButtonY), new Vector2(300f, 64f));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = skin != null ? skin.buttonConfirm : null;
            image.color = image.sprite != null ? Color.white : new Color(0.12f, 0.36f, 0.31f, 1f);
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            var overlay = CreateImage(rect, "HoverOverlay", skin != null ? skin.buttonConfirmHover : null,
                Vector2.zero, rect.sizeDelta, new Color(1f, 1f, 1f, 0f));
            Stretch(overlay.rectTransform);
            var buttonSkin = rect.gameObject.AddComponent<MenuSpriteButtonSkin>();
            buttonSkin.SetOverlay(overlay);

            var label = CreatePopupText(rect, "Label", Vector2.zero, Vector2.zero, 28f, font, Color.white, FontStyles.Bold);
            Stretch(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = 28f;
            label.text = LocalizationService.Get("mine_reset.timed.ok", "확인");
            return button;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        // 장식·발광 레이어는 입력을 가로채지 않는다. 그림이 없으면 발광은 끄고 면은 단색으로 대신한다.
        private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, Color color)
        {
            var rect = CreateRect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            if (sprite == null && color != CardColor)
            {
                image.enabled = false;
            }

            return image;
        }

        // 확인·Esc가 이 경로로 들어온다. 창만 닫고 월드·골드·시드·요금·씬은 건드리지 않는다.
        private void HidePopup()
        {
            if (popupRoot == null || !popupRoot.activeSelf)
            {
                return;
            }

            // 시계 연출 중에는 닫기 경로가 들어와도 연출을 끊지 않는다.
            if (clockIntro != null && clockIntro.IsPlaying)
            {
                return;
            }

            if (popupMotion != null && Application.isPlaying && popupMotion.isActiveAndEnabled)
            {
                // 이미 닫는 중이면 PlayClose가 무시한다.
                popupMotion.PlayClose(CloseImmediately);
                return;
            }

            CloseImmediately();
        }

        private void CloseImmediately()
        {
            if (popupRoot == null)
            {
                return;
            }

            StopClockIntro();
            if (popupMotion != null)
            {
                popupMotion.ResetHidden();
                popupMotion.SetBackdropFloor(0f);
            }

            if (popupCard != null)
            {
                popupCard.gameObject.SetActive(true);
            }

            PopupWindowSorting.Remove(popupRoot.GetComponent<Canvas>());
            popupRoot.SetActive(false);
        }

        private static bool IsTypingInInputField()
        {
            var selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            if (selected == null)
            {
                return false;
            }

            return selected.GetComponent<TMP_InputField>() != null
                || selected.GetComponent<InputField>() != null;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize,
            TMP_FontAsset font,
            Color color,
            FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        // 알림 카드는 중앙 기준 좌표를 쓴다(시계용 CreateText는 상단 앵커).
        private static TMP_Text CreatePopupText(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize,
            TMP_FontAsset font,
            Color color,
            FontStyles style)
        {
            var text = CreateText(parent, name, anchoredPosition, size, fontSize, font, color, style);
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
