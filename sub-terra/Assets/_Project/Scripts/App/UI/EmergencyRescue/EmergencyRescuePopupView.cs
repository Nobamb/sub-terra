using System;
using SubTerra.App.Run;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.MainMenu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyRescue
{
    /// <summary>
    /// 전력 고갈 구출 팝업 본체. 등장(끊겨 나타나는 글리치) → 표시(가장자리 간헐 글리치) → 종료(강한 글리치) 상태기계와
    /// 비용 표·버튼을 소유한다. 표시·비용 계산은 하지 않고 EmergencyRescueCost를 받아 보여 주기만 한다.
    /// 모든 효과는 장식 레이어에만 적용되고 버튼·본문은 제자리에서 읽힌다. 시간은 unscaledDeltaTime이다.
    /// </summary>
    public sealed class EmergencyRescuePopupView : MonoBehaviour
    {
        public enum PopupState
        {
            Hidden = 0,
            Opening = 1,
            Open = 2,
            Closing = 3
        }

        public const string Title = "전력이 바닥났습니다";
        public const string DefaultMessage =
            "전력이 모두 소진되었습니다.\n구출을 요청하면 엘리베이터로 이동하며 아래 자원이 차감됩니다.";
        public const string RescueLabel = "구출 요청";
        public const string CloseLabel = "닫기";
        public static readonly Vector2 CardSize = new Vector2(880f, 675f);

        private const float BackdropAlpha = 0.78f;
        private const float MaxStep = 0.05f;
        private const int BarCount = 12;
        private const int BandCount = 4;
        private const float EdgeSpill = 34f;

        private static readonly Color CardColor = new Color(0.02f, 0.035f, 0.05f, 0.98f);
        private static readonly Color BackdropColor = new Color(0.01f, 0.015f, 0.025f, 1f);
        private static readonly Color DisabledTint = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        private static readonly Color SecondaryTint = new Color(0.62f, 0.74f, 0.84f, 0.92f);

        private sealed class Band
        {
            public RectTransform Mask;
            public RectTransform Inner;
        }

        private TMP_FontAsset font;
        private Image backdrop;
        private Canvas canvas;
        private RectTransform card;
        private RectTransform visualRect;
        private CanvasGroup visualGroup;
        private RectTransform contentRect;
        private CanvasGroup contentGroup;
        private Image ghostTeal;
        private Image ghostRed;
        private Image flash;
        private Image[] bars;
        private Band[] bands;
        private TMP_Text messageText;
        private TMP_Text footerText;
        private EmergencyRescueCostTable table;
        private Button rescueButton;
        private Button closeButton;
        private Image rescueImage;
        private Image closeImage;
        private TMP_Text rescueLabel;
        private TMP_Text closeLabel;
        private Func<string, Sprite> iconResolver;

        private PopupState state;
        private float clock;
        private float idleClock;
        private bool externalInteractable = true;
        private int lastGeometryKey = int.MinValue;
        private EmergencyRescueCost displayedCost;
        private Action closedCallback;

        public PopupState State => state;
        /// <summary>등장 중이거나 열려 있음. 닫는 중은 포함하지 않는다.</summary>
        public bool IsOpen => state == PopupState.Opening || state == PopupState.Open;
        public bool IsClosing => state == PopupState.Closing;
        public bool IsVisible => state != PopupState.Hidden;
        /// <summary>등장이 끝나 입력을 받을 수 있는 상태.</summary>
        public bool IsStable => state == PopupState.Open;
        public EmergencyRescueCost DisplayedCost => displayedCost;
        public Button RescueButton => rescueButton;
        public Button CloseButton => closeButton;
        public float Clock => clock;
        public int VisibleRowCount => table != null ? table.VisibleRowCount : 0;
        public string MessageString => messageText != null ? messageText.text : string.Empty;
        public string FooterString => footerText != null ? footerText.text : string.Empty;
        public float ContentAlpha => contentGroup != null ? contentGroup.alpha : 0f;
        public float VisualAlpha => visualGroup != null ? visualGroup.alpha : 0f;
        public Vector2 VisualOffset => visualRect != null ? visualRect.anchoredPosition : Vector2.zero;
        public RectTransform CardRect => card;
        public bool AnyGlitchPieceVisible
        {
            get
            {
                for (var i = 0; bars != null && i < bars.Length; i++)
                {
                    if (bars[i].enabled)
                    {
                        return true;
                    }
                }

                for (var i = 0; bands != null && i < bands.Length; i++)
                {
                    if (bands[i].Mask.gameObject.activeSelf)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Create가 만든 루트(Image·Canvas)에 팝업 내용을 짓는다.</summary>
        internal void Build(TMP_FontAsset defaultFont)
        {
            var skin = Resources.Load<MineResetTimedPopupSkin>(MineResetTimedPopupSkin.ResourcePath);
            font = skin != null && skin.font != null ? skin.font : defaultFont;
            backdrop = GetComponent<Image>();
            backdrop.color = EmergencyRescueUi.WithAlpha(BackdropColor, 0f);
            backdrop.canvasRenderer.cullTransparentMesh = false;
            canvas = GetComponent<Canvas>();

            card = EmergencyRescueUi.Rect(transform, "Card", Vector2.zero, CardSize);

            visualRect = EmergencyRescueUi.Rect(card, "Visual", Vector2.zero, CardSize);
            visualGroup = visualRect.gameObject.AddComponent<CanvasGroup>();
            visualGroup.interactable = false;
            visualGroup.blocksRaycasts = false;
            BuildLayers(visualRect, skin, true);

            // 프레임 윗부분만 붉은 보조색으로 칠한다(경고). 프레임 그림이 있을 때만.
            if (skin != null && skin.frame != null)
            {
                RectTransform redMask = EmergencyRescueUi.Rect(
                    visualRect, "FrameRedMask", new Vector2(0f, CardSize.y * 0.5f - 55f), new Vector2(CardSize.x, 110f));
                redMask.gameObject.AddComponent<RectMask2D>();
                EmergencyRescueUi.Image(
                    redMask, "FrameRed", skin.frame, new Vector2(0f, -(CardSize.y * 0.5f - 55f)), CardSize,
                    EmergencyRescueUi.WithAlpha(new Color(1f, 0.28f, 0.24f, 1f), 0.85f));
            }

            Sprite frameSprite = skin != null ? skin.frame : null;
            ghostTeal = EmergencyRescueUi.Image(
                card, "GhostTeal", frameSprite, Vector2.zero, CardSize, EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Teal, 0f));
            ghostRed = EmergencyRescueUi.Image(
                card, "GhostRed", frameSprite, Vector2.zero, CardSize, EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Red, 0f));

            ghostTeal.enabled = frameSprite != null;
            ghostRed.enabled = frameSprite != null;

            bands = new Band[BandCount];
            for (var i = 0; i < BandCount; i++)
            {
                RectTransform mask = EmergencyRescueUi.Rect(
                    card, "Band" + i, Vector2.zero, new Vector2(CardSize.x + EdgeSpill * 2f, 30f));
                mask.gameObject.AddComponent<RectMask2D>();
                RectTransform inner = EmergencyRescueUi.Rect(mask, "Inner", Vector2.zero, CardSize);
                BuildLayers(inner, skin, false);
                mask.gameObject.SetActive(false);
                bands[i] = new Band { Mask = mask, Inner = inner };
            }

            flash = EmergencyRescueUi.Image(
                card, "Flash", skin != null ? skin.frameGlow : null, Vector2.zero, CardSize,
                EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Teal, 0f));
            flash.enabled = skin != null && skin.frameGlow != null;

            bars = new Image[BarCount];
            for (var i = 0; i < BarCount; i++)
            {
                bars[i] = EmergencyRescueUi.Image(card, "Bar" + i, null, Vector2.zero, new Vector2(40f, 3f), EmergencyRescueUi.Teal);
                bars[i].enabled = false;
            }

            BuildContent(skin);
            gameObject.SetActive(false);
        }

        public void SetIconResolver(Func<string, Sprite> resolver)
        {
            iconResolver = resolver;
        }

        /// <summary>
        /// 팝업을 연다. 숨겨져 있으면 등장 연출을 처음부터 재생하고, 이미 열려 있으면 내용만 갱신한다.
        /// 닫는 중이면 아무것도 하지 않는다(재열기는 컨트롤러가 닫기 완료 뒤에 처리한다).
        /// </summary>
        public bool Show(EmergencyRescueCost cost, string message)
        {
            if (state == PopupState.Closing)
            {
                return false;
            }

            Refresh(cost, message);
            if (state == PopupState.Hidden)
            {
                StartOpening();
            }
            else
            {
                BringToFront();
            }

            return true;
        }

        public void Refresh(EmergencyRescueCost cost, string message)
        {
            displayedCost = cost;
            SetMessage(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message);
            table.Apply(
                EmergencyRescueCostRows.Build(cost),
                cost == null ? EmergencyRescueCostRows.BuildFooter(null) : EmergencyRescueCostRows.FreeMessage,
                iconResolver);
            footerText.text = cost == null || cost.IsFree ? string.Empty : EmergencyRescueCostRows.BuildFooter(cost);
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
            }
        }

        /// <summary>강한 글리치 종료 연출 뒤 숨기고 onClosed를 부른다. 이미 닫는 중이거나 숨겨져 있으면 무시한다.</summary>
        public bool BeginClose(Action onClosed)
        {
            if (state == PopupState.Hidden || state == PopupState.Closing)
            {
                return false;
            }

            closedCallback = onClosed;
            state = PopupState.Closing;
            clock = 0f;
            lastGeometryKey = int.MinValue;
            ApplyButtons();
            if (!isActiveAndEnabled)
            {
                FinishClose();
                return true;
            }

            ApplyGlitch(EmergencyRescueTimeline.Outro(0f), 2);
            return true;
        }

        /// <summary>연출 없이 즉시 숨기고 대기 중인 닫기 콜백을 버린다. 구출 완료·전력 회복 때 쓴다.</summary>
        public void HideImmediate()
        {
            closedCallback = null;
            if (state == PopupState.Hidden && !gameObject.activeSelf)
            {
                return;
            }

            state = PopupState.Hidden;
            PopupWindowSorting.Remove(canvas);
            gameObject.SetActive(false);
        }

        public void SetInteractable(bool interactable)
        {
            externalInteractable = interactable;
            ApplyButtons();
        }

        private void OnDisable()
        {
            // 씬 전환·비활성화로 꺼져도 다음 표시가 깨끗하게 시작되도록 모두 되돌린다.
            state = PopupState.Hidden;
            closedCallback = null;
            clock = 0f;
            idleClock = 0f;
            lastGeometryKey = int.MinValue;
            PopupWindowSorting.Remove(canvas);
            ClearGlitch();
            if (visualGroup != null)
            {
                visualGroup.alpha = 1f;
            }

            if (contentGroup != null)
            {
                contentGroup.alpha = 1f;
            }
        }

        /// <summary>확인용: true면 Update가 시간을 진행하지 않아 테스트가 Tick으로 프레임을 직접 진행한다.</summary>
        public bool ManualTick { get; set; }

        private void Update()
        {
            if (ManualTick)
            {
                return;
            }

            Tick(Mathf.Min(Time.unscaledDeltaTime, MaxStep));
        }

        /// <summary>테스트와 Update가 같은 경로로 시간을 진행한다.</summary>
        public void Tick(float dt)
        {
            switch (state)
            {
                case PopupState.Opening:
                    clock += dt;
                    if (clock >= EmergencyRescueTimeline.IntroDuration)
                    {
                        state = PopupState.Open;
                        clock = 0f;
                        idleClock = 0f;
                        SettleVisuals();
                        ApplyButtons();
                    }
                    else
                    {
                        ApplyGlitch(EmergencyRescueTimeline.Intro(clock), 1);
                    }

                    break;
                case PopupState.Open:
                    idleClock += dt;
                    ApplyIdle(idleClock);
                    break;
                case PopupState.Closing:
                    clock += dt;
                    if (clock >= EmergencyRescueTimeline.OutroDuration)
                    {
                        FinishClose();
                    }
                    else
                    {
                        ApplyGlitch(EmergencyRescueTimeline.Outro(clock), 2);
                    }

                    break;
            }
        }

        private void StartOpening()
        {
            state = PopupState.Opening;
            clock = 0f;
            idleClock = 0f;
            lastGeometryKey = int.MinValue;
            gameObject.SetActive(true);
            BringToFront();
            ApplyButtons();
            ApplyGlitch(EmergencyRescueTimeline.Intro(0f), 1);
        }

        private void BringToFront()
        {
            transform.SetAsLastSibling();
            PopupWindowSorting.BringToFront(canvas);
        }

        private void FinishClose()
        {
            Action callback = closedCallback;
            closedCallback = null;
            state = PopupState.Hidden;
            PopupWindowSorting.Remove(canvas);
            gameObject.SetActive(false);
            if (callback != null)
            {
                callback();
            }
        }

        // 등장이 끝나면 모든 글리치 조각을 걷어 내고 본문·프레임을 선명한 정상 상태로 둔다.
        private void SettleVisuals()
        {
            ClearGlitch();
            visualGroup.alpha = 1f;
            contentGroup.alpha = 1f;
            visualRect.anchoredPosition = Vector2.zero;
            contentRect.anchoredPosition = Vector2.zero;
            backdrop.color = EmergencyRescueUi.WithAlpha(BackdropColor, BackdropAlpha);
        }

        private void ClearGlitch()
        {
            if (visualRect != null)
            {
                visualRect.anchoredPosition = Vector2.zero;
            }

            if (contentRect != null)
            {
                contentRect.anchoredPosition = Vector2.zero;
            }

            if (ghostTeal != null)
            {
                ghostTeal.color = EmergencyRescueUi.WithAlpha(ghostTeal.color, 0f);
                ghostRed.color = EmergencyRescueUi.WithAlpha(ghostRed.color, 0f);
                flash.color = EmergencyRescueUi.WithAlpha(flash.color, 0f);
            }

            for (var i = 0; bars != null && i < bars.Length; i++)
            {
                bars[i].enabled = false;
            }

            for (var i = 0; bands != null && i < bands.Length; i++)
            {
                if (bands[i].Mask.gameObject.activeSelf)
                {
                    bands[i].Mask.gameObject.SetActive(false);
                }
            }

            lastGeometryKey = int.MinValue;
        }

        private void ApplyIdle(float time)
        {
            var intensity = EmergencyRescueTimeline.Idle(time, out var seed);
            ApplyGlitch(new EmergencyRescueGlitchFrame(intensity, seed, seed % 3 == 0 ? 1 : 0, 1f, 1f, 1f), 0);
        }

        // mode 0 = 표시 중(가장자리 조각만), 1 = 등장, 2 = 종료.
        private void ApplyGlitch(EmergencyRescueGlitchFrame f, int mode)
        {
            var intensity = f.Intensity;
            visualGroup.alpha = f.VisualAlpha;
            contentGroup.alpha = f.ContentAlpha;
            if (mode != 0)
            {
                backdrop.color = EmergencyRescueUi.WithAlpha(BackdropColor, BackdropAlpha * f.BackdropAlpha);
            }

            var key = mode * 1_000_000 + f.Seed * 64 + Mathf.RoundToInt(intensity * 20f);
            if (key == lastGeometryKey)
            {
                return;
            }

            lastGeometryKey = key;
            Color primary = f.Tint == 1 ? EmergencyRescueUi.Red : EmergencyRescueUi.Teal;
            Color other = f.Tint == 1 ? EmergencyRescueUi.Teal : EmergencyRescueUi.Red;
            var seed = f.Seed;

            // 프레임 어긋남: 장식 레이어만 가로로 밀리고 본문·버튼은 (종료 때만 약간) 흔들린다.
            if (mode == 0)
            {
                visualRect.anchoredPosition = Vector2.zero;
                contentRect.anchoredPosition = Vector2.zero;
            }
            else
            {
                var visualJitter = mode == 2 ? 16f : 11f;
                var contentJitter = mode == 2 ? 8f : 3f;
                visualRect.anchoredPosition = new Vector2((Hash(seed, 1) - 0.5f) * 2f * visualJitter * intensity, 0f);
                contentRect.anchoredPosition = new Vector2(
                    (Hash(seed, 2) - 0.5f) * 2f * contentJitter * intensity,
                    mode == 2 ? (Hash(seed, 3) - 0.5f) * 2f * 3f * intensity : 0f);
            }

            var ghostScale = mode == 0 ? 0.3f : 0.6f;
            var ghostShift = mode == 0 ? 3f + 4f * intensity : 5f + 10f * intensity;
            ghostTeal.rectTransform.anchoredPosition = new Vector2(-ghostShift, 0f);
            ghostRed.rectTransform.anchoredPosition = new Vector2(ghostShift, 0f);
            ghostTeal.color = EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Teal, ghostScale * intensity);
            ghostRed.color = EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Red, ghostScale * intensity);
            flash.color = EmergencyRescueUi.WithAlpha(primary, (mode == 0 ? 0.25f : 0.8f) * intensity);

            var maxBars = mode == 2 ? BarCount : mode == 1 ? 8 : 4;
            var barCount = intensity < 0.02f ? 0 : Mathf.Clamp(Mathf.CeilToInt(intensity * maxBars), 1, maxBars);
            for (var i = 0; i < bars.Length; i++)
            {
                if (i >= barCount)
                {
                    bars[i].enabled = false;
                    continue;
                }

                var width = mode == 0 ? 14f + Hash(seed, i * 5 + 11) * 56f : 50f + Hash(seed, i * 5 + 11) * 250f;
                var height = 2f + Hash(seed, i * 5 + 12) * 4f;
                var side = Hash(seed, i * 5 + 13) < 0.5f ? -1f : 1f;
                var yRange = CardSize.y * 0.5f - 40f;
                var y = (Hash(seed, i * 5 + 14) - 0.5f) * 2f * yRange;
                float x;
                if (mode == 0)
                {
                    // 표시 중: 창 가장자리에 걸쳐 바깥으로 살짝 번지는 조각만.
                    x = side * (CardSize.x * 0.5f + EdgeSpill * 0.8f - width * 0.5f);
                }
                else
                {
                    var xRange = CardSize.x * 0.5f - width * 0.5f + EdgeSpill;
                    x = (Hash(seed, i * 5 + 15) - 0.5f) * 2f * xRange;
                }

                bars[i].rectTransform.anchoredPosition = new Vector2(x, y);
                bars[i].rectTransform.sizeDelta = new Vector2(width, height);
                Color color = Hash(seed, i * 5 + 16) < 0.65f ? primary : other;
                bars[i].color = EmergencyRescueUi.WithAlpha(color, (0.4f + 0.5f * Hash(seed, i * 5 + 17)) * Mathf.Min(1f, intensity * 1.3f));
                bars[i].enabled = true;
            }

            // 패널 일부가 가로로 어긋나 분절되는 띠. 표시 중에는 쓰지 않는다.
            var bandCount = mode == 0 || intensity < 0.15f
                ? 0
                : Mathf.Clamp(Mathf.RoundToInt(intensity * (mode == 2 ? BandCount : 2)), mode == 2 ? 2 : 1, mode == 2 ? BandCount : 2);
            for (var i = 0; i < bands.Length; i++)
            {
                Band band = bands[i];
                if (i >= bandCount)
                {
                    if (band.Mask.gameObject.activeSelf)
                    {
                        band.Mask.gameObject.SetActive(false);
                    }

                    continue;
                }

                var height = 16f + Hash(seed, i * 7 + 31) * (mode == 2 ? 56f : 34f);
                var y = (Hash(seed, i * 7 + 32) - 0.5f) * 2f * (CardSize.y * 0.5f - 70f);
                var dx = (Hash(seed, i * 7 + 33) < 0.5f ? -1f : 1f)
                    * (8f + Hash(seed, i * 7 + 34) * (mode == 2 ? 34f : 22f)) * intensity;
                band.Mask.anchoredPosition = new Vector2(0f, y);
                band.Mask.sizeDelta = new Vector2(CardSize.x + EdgeSpill * 2f, height);
                band.Inner.anchoredPosition = new Vector2(dx, -y);
                if (!band.Mask.gameObject.activeSelf)
                {
                    band.Mask.gameObject.SetActive(true);
                }
            }
        }

        private static float Hash(int seed, int salt)
        {
            return EmergencyRescueTimeline.Hash01(seed * 131 + salt * 17 + 5);
        }

        // 버튼은 등장이 끝난 열린 상태에서만 눌린다. 등장·종료 도중의 클릭은 받지 않는다.
        private void ApplyButtons()
        {
            if (rescueButton == null)
            {
                return;
            }

            var stable = state == PopupState.Open;
            rescueButton.interactable = stable && externalInteractable;
            closeButton.interactable = stable;
            rescueImage.color = rescueButton.interactable ? Color.white : DisabledTint;
            closeImage.color = closeButton.interactable ? SecondaryTint : EmergencyRescueUi.WithAlpha(SecondaryTint, 0.45f);
            rescueLabel.alpha = rescueButton.interactable ? 1f : 0.55f;
            closeLabel.alpha = closeButton.interactable ? 1f : 0.55f;
        }

        // 패널·프레임 레이어. 본체와 어긋나는 띠 복사본이 같은 구성을 쓴다.
        private static void BuildLayers(RectTransform parent, MineResetTimedPopupSkin skin, bool decorations)
        {
            EmergencyRescueUi.Image(parent, "PanelBase", null, Vector2.zero, CardSize, CardColor);
            if (skin != null && skin.panel != null)
            {
                EmergencyRescueUi.Image(parent, "Panel", skin.panel, Vector2.zero, CardSize, CardColor);
            }

            if (decorations)
            {
                // 위쪽에 아주 옅은 붉은 번짐 하나. 경고 분위기용 보조색.
                EmergencyRescueUi.Image(
                    parent, "RedWash", EmergencyRescueArt.Soft(), new Vector2(0f, CardSize.y * 0.5f - 112f),
                    new Vector2(CardSize.x * 0.85f, 220f), EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Red, 0.14f));
            }

            if (skin != null && skin.frame != null)
            {
                EmergencyRescueUi.Image(parent, "Frame", skin.frame, Vector2.zero, CardSize, Color.white);
            }
            else if (decorations)
            {
                BuildFallbackFrame(parent);
            }
        }

        private static void BuildFallbackFrame(RectTransform parent)
        {
            var w = CardSize.x;
            var h = CardSize.y;
            Color edge = EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Teal, 0.8f);
            EmergencyRescueUi.Image(parent, "EdgeTop", null, new Vector2(0f, h * 0.5f - 2f), new Vector2(w, 4f), edge);
            EmergencyRescueUi.Image(parent, "EdgeBottom", null, new Vector2(0f, -h * 0.5f + 2f), new Vector2(w, 4f), edge);
            EmergencyRescueUi.Image(parent, "EdgeLeft", null, new Vector2(-w * 0.5f + 2f, 0f), new Vector2(4f, h), edge);
            EmergencyRescueUi.Image(parent, "EdgeRight", null, new Vector2(w * 0.5f - 2f, 0f), new Vector2(4f, h), edge);
        }

        private void BuildContent(MineResetTimedPopupSkin skin)
        {
            contentRect = EmergencyRescueUi.Rect(card, "Content", Vector2.zero, CardSize);
            contentGroup = contentRect.gameObject.AddComponent<CanvasGroup>();

            TMP_Text title = EmergencyRescueUi.Text(
                contentRect, "Title", font, 40f, FontStyles.Bold, EmergencyRescueUi.TitleRed,
                TextAlignmentOptions.Center, new Vector2(0f, 262f), new Vector2(600f, 56f));
            title.text = Title;
            title.characterSpacing = 3f;
            title.enableAutoSizing = true;
            title.fontSizeMin = 28f;
            title.fontSizeMax = 40f;

            // 제목 양옆 붉은 마름모. 경고색은 제목과 이 장식에만 쓴다.
            for (var side = -1; side <= 1; side += 2)
            {
                Image diamond = EmergencyRescueUi.Image(
                    contentRect, side < 0 ? "WarnL" : "WarnR", null, new Vector2(side * 232f, 262f),
                    new Vector2(14f, 14f), EmergencyRescueUi.Red);
                diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            Image divider = EmergencyRescueUi.Image(
                contentRect, "TitleDivider", skin != null ? skin.titleDivider : null,
                new Vector2(0f, 226f), new Vector2(600f, 16.4f),
                skin != null && skin.titleDivider != null
                    ? EmergencyRescueUi.WithAlpha(new Color(1f, 0.55f, 0.5f, 1f), 0.9f)
                    : EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Red, 0.6f));
            if (skin == null || skin.titleDivider == null)
            {
                divider.rectTransform.sizeDelta = new Vector2(600f, 2f);
            }

            messageText = EmergencyRescueUi.Text(
                contentRect, "Message", font, 22f, FontStyles.Normal, EmergencyRescueUi.TextMain,
                TextAlignmentOptions.Center, new Vector2(0f, 176f), new Vector2(720f, 64f));
            messageText.textWrappingMode = TextWrappingModes.Normal;
            messageText.enableAutoSizing = true;
            messageText.fontSizeMin = 16f;
            messageText.fontSizeMax = 22f;
            messageText.lineSpacing = 8f;
            messageText.overflowMode = TextOverflowModes.Truncate;

            table = new EmergencyRescueCostTable(contentRect, font);

            footerText = EmergencyRescueUi.Text(
                contentRect, "Footer", font, 19f, FontStyles.Normal, EmergencyRescueUi.TextMuted,
                TextAlignmentOptions.Center, new Vector2(0f, -176f), new Vector2(720f, 30f));

            rescueButton = CreateButton(
                skin, "RescueButton", RescueLabel, new Vector2(-120f, -250f), new Vector2(300f, 68f), 28f,
                Color.white, out rescueImage, out rescueLabel);
            closeButton = CreateButton(
                skin, "CloseButton", CloseLabel, new Vector2(180f, -250f), new Vector2(190f, 58f), 24f,
                SecondaryTint, out closeImage, out closeLabel);
        }

        private Button CreateButton(
            MineResetTimedPopupSkin skin,
            string name,
            string label,
            Vector2 position,
            Vector2 size,
            float fontSize,
            Color tint,
            out Image image,
            out TMP_Text text)
        {
            RectTransform rect = EmergencyRescueUi.Rect(contentRect, name, position, size);
            image = rect.gameObject.AddComponent<Image>();
            image.sprite = skin != null ? skin.buttonConfirm : null;
            image.color = image.sprite != null ? tint : new Color(0.12f, 0.36f, 0.34f, 1f);
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            Image overlay = EmergencyRescueUi.Image(
                rect, "HoverOverlay", skin != null ? skin.buttonConfirmHover : null, Vector2.zero, size,
                new Color(1f, 1f, 1f, 0f));
            EmergencyRescueUi.Stretch(overlay.rectTransform);
            rect.gameObject.AddComponent<MenuSpriteButtonSkin>().SetOverlay(overlay);

            text = EmergencyRescueUi.Text(
                rect, "Label", font, fontSize, FontStyles.Bold, Color.white,
                TextAlignmentOptions.Center, Vector2.zero, size);
            EmergencyRescueUi.Stretch(text.rectTransform);
            text.text = label;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = fontSize;
            return button;
        }
    }
}
