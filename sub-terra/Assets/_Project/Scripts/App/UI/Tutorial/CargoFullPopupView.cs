using System;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 화물 공간 부족 팝업(B-152). 기존 QuestRewardCapacityPanel 위에 판매창과 같은 철제 껍데기를 코드로 입힌다.
    /// 제목·안내 글자와 두 버튼(이름·onClick)은 씬의 것을 그대로 옮겨 쓰고, 나머지 장식은 실행 중에만 만든다.
    /// 표시·연출·중복 클릭 차단만 하며 지급·포기 판정은 Presenter와 QuestRewardService가 한다.
    /// </summary>
    public sealed class CargoFullPopupView : MonoBehaviour
    {
        public enum PopupState
        {
            Hidden = 0,
            Opening = 1,
            Open = 2,
            Closing = 3
        }

        public static readonly Vector2 DefaultPanelSize = new Vector2(1000f, 660f);

        private const float Pad = 72f;
        private const float TitleY = 42f;
        private const float DividerY = 102f;
        private const float GuideY = 120f;
        private const float CardY = 164f;
        private const float CardH = 108f;
        private const float GaugeY = 288f;
        private const float GaugeH = 132f;
        private const float ButtonsY = 440f;
        private const float ButtonH = 108f;
        private const float ButtonGap = 16f;
        private const float FooterY = 578f;
        private const float GaugeRowX = 128f;
        private const float GaugeRowY = 58f;
        private const float GaugeRowH = 30f;
        private const float CellGap = 4f;
        private const float OverflowGap = 10f;
        private const float RewardBox = 72f;
        private const float RewardIcon = 56f;
        private const float MaxStep = 0.05f;
        private const float RewardFontSize = 32f;

        private static readonly Color TitleColor = new Color(0.84f, 0.98f, 1f, 1f);
        private static readonly Color OverflowBase = new Color(0.13f, 0.08f, 0.02f, 1f);

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text guideText;
        [SerializeField] private Button dumpButton;
        [SerializeField] private Button forfeitButton;
        [SerializeField] private Vector2 panelSize = new Vector2(1000f, 660f);
        [SerializeField, Range(4, CargoGaugeLayout.MaxCells)] private int gaugeCells = 12;
        [SerializeField, Range(1, CargoGaugeLayout.MaxOverflowCells)] private int maxOverflowCells = 4;
        [SerializeField] private CargoFullPopupTiming timing = new CargoFullPopupTiming();

        private sealed class RewardSlot
        {
            public RectTransform Root;
            public Image Icon;
        }

        private sealed class GaugeCell
        {
            public RectTransform Root;
            public Image Fill;
            public Image Line;
            public Image Stripes;
        }

        private readonly CargoFullChoiceGate gate = new CargoFullChoiceGate();
        private CargoFullPopupSkin skin;
        private MineResetTimedPopupSkin frameSkin;
        private TMP_FontAsset font;
        private bool built;
        private bool rendered;
        private PopupState state;
        private float clock;
        private CargoFullPopupTiming activeTiming;
        private CargoFullPopupTimeline.Pose pose;

        private Image backdrop;
        private RectTransform shellMask;
        private CanvasGroup shellGroup;
        private Image frameGlow;
        private Image scanLine;
        private RectTransform contentRoot;
        private CanvasGroup contentGroup;

        private RectTransform rewardCard;
        private RewardSlot[] rewardSlots;
        private TMP_Text rewardCaption;
        private TMP_Text rewardAmount;

        private TMP_Text loadText;
        private TMP_Text shortfallText;
        private RectTransform gaugeRow;
        private GaugeCell[] mainCells;
        private GaugeCell[] overflowCells;
        private Image overflowBracket;
        private Image overflowGlow;

        private CargoFullChoiceButton dumpFeedback;
        private CargoFullChoiceButton forfeitFeedback;
        private TMP_Text forfeitSubtitle;

        private TMP_Text footerText;
        private Image footerLineLeft;
        private Image footerLineRight;

        private CargoShortfallModel model;
        private QuestReward reward;
        private CargoGaugeLayout layout;

        public PopupState State => state;
        public bool IsVisible => state != PopupState.Hidden;
        public bool IsClosing => state == PopupState.Closing;
        public bool IsChoiceReady => state == PopupState.Open && gate.IsReady;
        public bool ManualTick { get; set; }
        public float Clock => clock;
        public float ContentAlpha => contentGroup != null ? contentGroup.alpha : 0f;
        public float OverflowFlash => pose.OverflowFlash;
        public float GaugeProgress => pose.Gauge;
        public CargoGaugeLayout GaugeLayout => layout;
        public CargoFullPopupTiming Timing => ResolveTiming();
        public string TitleString => titleText != null ? titleText.text : string.Empty;
        public string LoadString => loadText != null ? loadText.text : string.Empty;
        public string ShortfallString => shortfallText != null ? shortfallText.text : string.Empty;
        public string RewardString => rewardAmount != null ? rewardAmount.text : string.Empty;
        public string ForfeitSubtitleString => forfeitSubtitle != null ? forfeitSubtitle.text : string.Empty;
        public Button DumpButton => dumpButton;
        public Button ForfeitButton => forfeitButton;
        public int LitCellCount { get; private set; }
        public CargoFullChoiceButton DumpFeedback => dumpFeedback;
        public CargoFullChoiceButton ForfeitFeedback => forfeitFeedback;

        /// <summary>닫기 연출이 끝나 창이 꺼진 직후. 다음 창(선택·클리어)을 이어 띄우는 데 쓴다.</summary>
        public event Action Closed;

        /// <summary>빌더·테스트가 씬 참조를 넣는다.</summary>
        public void Configure(TMP_Text title, TMP_Text guide, Button dump, Button forfeit)
        {
            titleText = title;
            guideText = guide;
            dumpButton = dump;
            forfeitButton = forfeit;
        }

        public void Render(CargoShortfallModel shortfall, QuestReward questReward)
        {
            EnsureBuilt();
            model = shortfall;
            reward = questReward;
            rendered = true;
            layout = model.Layout(gaugeCells, maxOverflowCells);

            if (titleText != null)
            {
                titleText.text = CargoFullPopupText.Title;
            }

            if (guideText != null)
            {
                guideText.text = CargoFullPopupText.Guide;
            }

            loadText.text = CargoFullPopupText.LoadLabel(model);
            shortfallText.text = model.IsShort ? CargoFullPopupText.ShortfallLabel(model) : string.Empty;
            forfeitSubtitle.text = CargoFullPopupText.ForfeitSubtitle(reward);
            LayoutReward();
            LayoutGauge();
            LayoutFooter();
            ApplyGauge(state == PopupState.Opening ? pose.Gauge : 1f, state == PopupState.Opening ? pose.OverflowFlash : 0f);
        }

        /// <summary>연다. 이미 열려 있으면 내용만 유지하고, 한 번 고른 뒤 다시 불리면 새 선택을 받는다.</summary>
        public void Show()
        {
            EnsureBuilt();
            if (state == PopupState.Opening)
            {
                return;
            }

            if (state == PopupState.Open)
            {
                if (gate.IsConsumed)
                {
                    gate.Reset();
                    gate.Arm();
                    ApplyInteractable(true);
                }

                return;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            transform.SetAsLastSibling();
            // 꺼진 상태에서 그린 글자 폭은 글리프가 아직 없어 0으로 나올 수 있다. 켜진 뒤 다시 잰다.
            if (rendered)
            {
                LayoutReward();
                LayoutFooter();
            }

            gate.Reset();
            activeTiming = ResolveTiming();
            state = PopupState.Opening;
            clock = 0f;
            if (!Application.isPlaying && !ManualTick)
            {
                // 에디터 미리보기·씬 검사에서는 연출 없이 완성 상태로 둔다.
                SettleOpen();
                return;
            }

            ApplyOpen(0f);
        }

        /// <summary>약 0.2초 역재생 뒤 꺼진다. 누른 선택은 그 사이 다시 받지 않는다.</summary>
        public void Close()
        {
            if (state == PopupState.Hidden || state == PopupState.Closing)
            {
                if (state == PopupState.Hidden && gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            gate.Disarm();
            ApplyInteractable(false);
            state = PopupState.Closing;
            clock = 0f;
            activeTiming = ResolveTiming();
            if (!gameObject.activeInHierarchy || (!Application.isPlaying && !ManualTick))
            {
                FinishClose();
                return;
            }

            ApplyClose(0f);
        }

        public void HideImmediate()
        {
            if (state == PopupState.Hidden && !gameObject.activeSelf)
            {
                return;
            }

            FinishClose();
        }

        /// <summary>버튼 onClick 입구. 열기 연출이 끝난 뒤 한 번만 true이고, 이후 두 버튼을 모두 잠근다.</summary>
        public bool TryAcceptChoice()
        {
            if (state != PopupState.Open || !gate.TryConsume())
            {
                return false;
            }

            ApplyInteractable(false);
            return true;
        }

        private void Update()
        {
            if (ManualTick || state == PopupState.Hidden || state == PopupState.Open)
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
                    if (clock >= activeTiming.OpenDuration)
                    {
                        SettleOpen();
                    }
                    else
                    {
                        ApplyOpen(clock);
                    }

                    break;
                case PopupState.Closing:
                    clock += dt;
                    if (clock >= activeTiming.closeDuration)
                    {
                        FinishClose();
                    }
                    else
                    {
                        ApplyClose(clock);
                    }

                    break;
            }
        }

        private void OnDisable()
        {
            // 부모가 꺼져 연출이 끊겨도 다음 표시가 처음부터 시작되게 한다.
            if (state == PopupState.Hidden)
            {
                return;
            }

            var wasClosing = state == PopupState.Closing;
            state = PopupState.Hidden;
            clock = 0f;
            gate.Reset();
            if (wasClosing)
            {
                Closed?.Invoke();
            }
        }

        private void SettleOpen()
        {
            state = PopupState.Open;
            clock = activeTiming.OpenDuration;
            ApplyOpen(clock);
            gate.Arm();
            ApplyInteractable(true);
        }

        private void FinishClose()
        {
            var notify = state != PopupState.Hidden;
            state = PopupState.Hidden;
            clock = 0f;
            gate.Reset();
            ApplyInteractable(false);
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            if (notify)
            {
                Closed?.Invoke();
            }
        }

        private CargoFullPopupTiming ResolveTiming()
        {
            if (timing == null)
            {
                timing = new CargoFullPopupTiming();
            }

            return timing.Sanitized();
        }

        // ---------------- 연출 ----------------

        private void ApplyOpen(float t)
        {
            pose = CargoFullPopupTimeline.EvaluateOpen(t, activeTiming);

            ApplyPose();
            ApplyInteractable(state == PopupState.Open && gate.IsReady);
        }

        private void ApplyClose(float t)
        {
            pose = CargoFullPopupTimeline.EvaluateClose(t, activeTiming);
            ApplyPose();
        }

        private void ApplyPose()
        {
            var size = ResolvePanelSize();
            backdrop.color = WithAlpha(skin != null ? skin.backdrop : Color.black, (skin != null ? skin.backdrop.a : 0.55f) * pose.Backdrop);
            shellMask.sizeDelta = new Vector2(size.x, size.y * Mathf.Clamp01(pose.Reveal));
            shellGroup.alpha = pose.Alpha;
            if (frameGlow != null)
            {
                frameGlow.color = WithAlpha(Color.white, 0.55f * pose.FrameFlash);
                frameGlow.enabled = pose.FrameFlash > 0.001f;
            }

            var teal = Teal;
            scanLine.rectTransform.sizeDelta = new Vector2(size.x * Mathf.Clamp01(pose.ScanWidth), scanLine.rectTransform.sizeDelta.y);
            scanLine.color = WithAlpha(teal, pose.ScanAlpha);
            scanLine.enabled = pose.ScanAlpha > 0.001f;
            contentGroup.alpha = pose.Content * pose.Alpha;
            ApplyGauge(pose.Gauge, pose.OverflowFlash);
        }

        private void ApplyInteractable(bool interactable)
        {
            if (dumpButton != null)
            {
                dumpButton.interactable = interactable;
            }

            if (forfeitButton != null)
            {
                forfeitButton.interactable = interactable;
            }

            if (contentGroup != null)
            {
                contentGroup.interactable = interactable;
                contentGroup.blocksRaycasts = state != PopupState.Hidden;
            }
        }

        private void ApplyGauge(float fill, float flash)
        {
            if (mainCells == null)
            {
                return;
            }

            var teal = Teal;
            var empty = skin != null ? skin.cellEmpty : new Color(0.04f, 0.08f, 0.11f, 1f);
            var lit = Mathf.Clamp(Mathf.CeilToInt(layout.FilledCells * Mathf.Clamp01(fill) - 0.001f), 0, layout.FilledCells);
            LitCellCount = lit;
            for (var i = 0; i < mainCells.Length; i++)
            {
                var cell = mainCells[i];
                if (i >= layout.CellCount)
                {
                    continue;
                }

                var on = i < lit;
                cell.Fill.color = on ? teal : empty;
                cell.Line.color = WithAlpha(teal, on ? 0.95f : 0.22f);
            }

            var amber = Amber;
            var bright = Color.Lerp(amber, Color.white, 0.55f * flash);
            for (var i = 0; i < overflowCells.Length; i++)
            {
                var cell = overflowCells[i];
                if (i >= layout.OverflowCells)
                {
                    continue;
                }

                cell.Fill.color = Color.Lerp(OverflowBase, amber * 0.45f, flash);
                cell.Stripes.color = WithAlpha(bright, 0.85f + 0.15f * flash);
                cell.Line.color = WithAlpha(bright, 0.9f);
            }

            overflowBracket.color = WithAlpha(bright, 0.9f);
            overflowGlow.color = WithAlpha(amber, 0.45f * flash);
            overflowGlow.enabled = flash > 0.001f;
        }

        // ---------------- 배치 ----------------

        private void LayoutReward()
        {
            var entries = new (Sprite sprite, int amount)[]
            {
                (skin != null ? skin.copperIcon : null, reward.Copper),
                (skin != null ? skin.ironIcon : null, reward.Iron),
                (skin != null ? skin.lithiumIcon : null, reward.Lithium),
                (skin != null ? skin.goldIcon : null, reward.Gold)
            };

            var count = 0;
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i].amount > 0)
                {
                    count++;
                }
            }

            rewardAmount.text = CargoFullPopupText.RewardLabel(reward);
            var size = ResolvePanelSize();
            var maxCard = size.x - Pad * 2f;
            var boxes = count > 0 ? count * RewardBox + (count - 1) * 10f : 0f;
            var textW = Mathf.Max(PreferredWidth(rewardCaption, 16f),
                PreferredWidth(rewardAmount, RewardFontSize)) + 8f;
            var cardW = Mathf.Min(maxCard, 24f + boxes + (count > 0 ? 20f : 0f) + textW + 28f);
            textW = cardW - 24f - boxes - (count > 0 ? 20f : 0f) - 28f;
            // 긴 보상은 칸 폭에 맞춰 글자만 줄인다(말줄임은 줄 높이가 넘치면 아무것도 안 그린다).
            FitWidth(rewardAmount, RewardFontSize, 18f, textW);
            SetRect(rewardCard, (size.x - cardW) * 0.5f, CardY, cardW, CardH);

            var x = 24f;
            var slot = 0;
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i].amount <= 0)
                {
                    continue;
                }

                var target = rewardSlots[slot++];
                target.Root.gameObject.SetActive(true);
                SetRect(target.Root, x, (CardH - RewardBox) * 0.5f, RewardBox, RewardBox);
                target.Icon.sprite = entries[i].sprite;
                target.Icon.enabled = entries[i].sprite != null;
                x += RewardBox + 10f;
            }

            for (var i = slot; i < rewardSlots.Length; i++)
            {
                rewardSlots[i].Root.gameObject.SetActive(false);
            }

            var textX = count > 0 ? x - 10f + 20f : 24f;
            SetRect(rewardCaption.rectTransform, textX, 18f, textW, 24f);
            SetRect(rewardAmount.rectTransform, textX, 44f, textW, 48f);
        }

        private void LayoutGauge()
        {
            var width = gaugeRow.rect.width > 1f ? gaugeRow.rect.width : gaugeRow.sizeDelta.x;
            var units = layout.CellCount + layout.OverflowCells;
            var extra = layout.OverflowCells > 0 ? OverflowGap : 0f;
            var cellW = (width - extra - (units - 1) * CellGap) / Mathf.Max(1, units);
            for (var i = 0; i < mainCells.Length; i++)
            {
                var visible = i < layout.CellCount;
                mainCells[i].Root.gameObject.SetActive(visible);
                if (visible)
                {
                    SetRect(mainCells[i].Root, i * (cellW + CellGap), 0f, cellW, GaugeRowH);
                }
            }

            var overflowX = layout.CellCount * (cellW + CellGap) - CellGap + extra;
            for (var i = 0; i < overflowCells.Length; i++)
            {
                var visible = i < layout.OverflowCells;
                overflowCells[i].Root.gameObject.SetActive(visible);
                if (visible)
                {
                    SetRect(overflowCells[i].Root, overflowX + i * (cellW + CellGap), 0f, cellW, GaugeRowH);
                }
            }

            var showOverflow = layout.OverflowCells > 0;
            overflowBracket.gameObject.SetActive(showOverflow);
            overflowGlow.gameObject.SetActive(showOverflow);
            if (showOverflow)
            {
                var w = layout.OverflowCells * (cellW + CellGap) - CellGap;
                SetRect(overflowBracket.rectTransform, overflowX - 4f, -4f, w + 8f, GaugeRowH + 8f);
                SetRect(overflowGlow.rectTransform, overflowX - 18f, -18f, w + 36f, GaugeRowH + 36f);
            }
        }

        // ---------------- 생성 ----------------

        private void EnsureBuilt()
        {
            if (built)
            {
                return;
            }

            built = true;
            skin = Resources.Load<CargoFullPopupSkin>(CargoFullPopupSkin.ResourcePath);
            frameSkin = Resources.Load<MineResetTimedPopupSkin>(MineResetTimedPopupSkin.ResourcePath);
            ResolveSceneReferences();
            font = titleText != null && titleText.font != null
                ? titleText.font
                : frameSkin != null && frameSkin.font != null ? frameSkin.font : TMP_Settings.defaultFontAsset;

            var size = ResolvePanelSize();
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            var panelImage = GetComponent<Image>();
            if (panelImage != null)
            {
                // 패널 영역 클릭이 뒤 화면으로 새지 않게 투명하게 남겨 둔다.
                panelImage.sprite = null;
                panelImage.color = Color.clear;
                panelImage.raycastTarget = true;
            }

            backdrop = ResourceSellUi.Image(transform, "CargoFullBackdrop", null, Color.clear);
            backdrop.rectTransform.offsetMin = new Vector2(-4000f, -4000f);
            backdrop.rectTransform.offsetMax = new Vector2(4000f, 4000f);
            backdrop.transform.SetAsFirstSibling();

            BuildShell(size);
            BuildContent(size);

            scanLine = ResourceSellUi.AddImage(
                ResourceSellUi.Centered(transform, "CargoFullScan", Vector2.zero, new Vector2(size.x, 10f)),
                frameSkin != null && frameSkin.scanLine != null ? frameSkin.scanLine : ResourceSellArt.SoftRect(),
                Color.clear);
            scanLine.enabled = false;
            scanLine.transform.SetAsLastSibling();

            ApplyInteractable(false);
        }

        private void ResolveSceneReferences()
        {
            if (titleText == null)
            {
                titleText = FindChild<TMP_Text>("CapacityTitle");
            }

            if (guideText == null)
            {
                guideText = FindChild<TMP_Text>("CapacityBody");
            }

            if (dumpButton == null)
            {
                dumpButton = FindChild<Button>("CapacityDumpButton");
            }

            if (forfeitButton == null)
            {
                forfeitButton = FindChild<Button>("CapacityForfeitButton");
            }
        }

        private T FindChild<T>(string childName) where T : Component
        {
            var child = transform.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private void BuildShell(Vector2 size)
        {
            shellMask = ResourceSellUi.Centered(transform, "CargoFullShell", Vector2.zero, size);
            shellMask.gameObject.AddComponent<RectMask2D>();
            shellGroup = shellMask.gameObject.AddComponent<CanvasGroup>();
            shellGroup.interactable = false;
            shellGroup.blocksRaycasts = false;
            shellMask.SetSiblingIndex(1);
            var inner = ResourceSellUi.Centered(shellMask, "Inner", Vector2.zero, size);
            ResourceSellPopupView.BuildPanelLayers(inner, frameSkin, size);

            var glowSprite = frameSkin != null ? CargoFullPopupArt.Sliced(frameSkin.frameGlow, 0.135f, 0.19f) : null;
            if (glowSprite != null)
            {
                frameGlow = ResourceSellUi.Image(inner, "FrameGlow", glowSprite, Color.clear);
                frameGlow.type = Image.Type.Sliced;
                frameGlow.pixelsPerUnitMultiplier = glowSprite.border.x * 100f / (glowSprite.pixelsPerUnit * 120f);
                frameGlow.enabled = false;
            }
        }

        private void BuildContent(Vector2 size)
        {
            contentRoot = ResourceSellUi.Centered(transform, "CargoFullContent", Vector2.zero, size);
            contentGroup = contentRoot.gameObject.AddComponent<CanvasGroup>();
            contentRoot.SetSiblingIndex(2);
            var innerW = size.x - Pad * 2f;

            // 제목: 경고 아이콘 + 제목 + 구분선.
            var warning = ResourceSellUi.AddImage(ResourceSellUi.Place(contentRoot, "WarningIcon", Pad + 4f, TitleY + 9f, 38f, 38f),
                skin != null ? skin.warningIcon : null, Amber);
            warning.preserveAspect = true;
            warning.color = skin != null && skin.warningIcon != null ? Color.white : Amber;
            if (titleText != null)
            {
                titleText.transform.SetParent(contentRoot, false);
                SetRect(titleText.rectTransform, Pad + 56f, TitleY - 4f, innerW - 56f, 64f);
                titleText.fontSize = 40f;
                titleText.fontStyle = FontStyles.Bold;
                titleText.characterSpacing = 3f;
                titleText.color = TitleColor;
                titleText.alignment = TextAlignmentOptions.Left;
                titleText.textWrappingMode = TextWrappingModes.NoWrap;
                titleText.overflowMode = TextOverflowModes.Overflow;
                titleText.enableAutoSizing = false;
                titleText.text = CargoFullPopupText.Title;
                FitWidth(titleText, 40f, 28f, innerW - 56f);
            }

            if (frameSkin != null && frameSkin.titleDivider != null)
            {
                var divider = ResourceSellUi.AddImage(ResourceSellUi.Place(contentRoot, "TitleDivider", Pad, DividerY, innerW, 12f),
                    frameSkin.titleDivider, WithAlpha(Color.white, 0.75f));
                divider.preserveAspect = false;
            }

            if (guideText != null)
            {
                guideText.transform.SetParent(contentRoot, false);
                SetRect(guideText.rectTransform, Pad, GuideY, innerW, 32f);
                guideText.fontSize = 20f;
                guideText.fontStyle = FontStyles.Normal;
                guideText.color = TextMain;
                guideText.alignment = TextAlignmentOptions.Center;
                guideText.textWrappingMode = TextWrappingModes.NoWrap;
                guideText.enableAutoSizing = true;
                guideText.fontSizeMin = 15f;
                guideText.fontSizeMax = 20f;
            }

            BuildRewardCard();
            BuildGauge(innerW);
            var buttonW = (innerW - ButtonGap) * 0.5f;
            dumpFeedback = BuildChoice(dumpButton, Pad, buttonW, Teal, CargoFullPopupArt.ListIcon(), 32f,
                CargoFullPopupText.DumpTitle, CargoFullPopupText.DumpSubtitle, out _);
            forfeitFeedback = BuildChoice(forfeitButton, Pad + buttonW + ButtonGap, buttonW, Danger,
                skin != null ? skin.trashIcon : null, 36f, CargoFullPopupText.ForfeitTitle, string.Empty, out forfeitSubtitle);
            BuildFooter(innerW);
        }

        private void BuildRewardCard()
        {
            rewardCard = ResourceSellUi.Place(contentRoot, "RewardCard", 0f, CardY, 400f, CardH);
            Plate(rewardCard, 0.4f);
            rewardSlots = new RewardSlot[4];
            for (var i = 0; i < rewardSlots.Length; i++)
            {
                var box = ResourceSellUi.Place(rewardCard, "RewardSlot" + i, 0f, 0f, RewardBox, RewardBox);
                ResourceSellUi.Image(box, "Face", ResourceSellArt.ChamferFill(), new Color(0.015f, 0.03f, 0.045f, 1f));
                ResourceSellUi.Image(box, "Line", ResourceSellArt.ChamferOutline(), WithAlpha(Teal, 0.65f));
                var icon = ResourceSellUi.AddImage(ResourceSellUi.Centered(box, "Icon", Vector2.zero, new Vector2(RewardIcon, RewardIcon)),
                    null, Color.white);
                icon.preserveAspect = true;
                rewardSlots[i] = new RewardSlot { Root = box, Icon = icon };
            }

            rewardCaption = ResourceSellUi.Label(rewardCard, "Caption", 0f, 0f, 100f, 24f, font, 16f, FontStyles.Normal,
                TextMuted, TextAlignmentOptions.Left);
            rewardCaption.text = CargoFullPopupText.RewardCaption;
            rewardAmount = ResourceSellUi.Label(rewardCard, "Amount", 0f, 0f, 100f, 48f, font, 32f, FontStyles.Bold,
                TextMain, TextAlignmentOptions.Left);
        }

        private void BuildGauge(float innerW)
        {
            var panel = ResourceSellUi.Place(contentRoot, "CargoGauge", Pad, GaugeY, innerW, GaugeH);
            Plate(panel, 0.3f);
            var icon = ResourceSellUi.AddImage(ResourceSellUi.Place(panel, "CargoIcon", 20f, 22f, 88f, 88f),
                skin != null ? skin.cargoIcon : null, Color.white);
            icon.preserveAspect = true;
            icon.enabled = skin != null && skin.cargoIcon != null;
            var caption = ResourceSellUi.Label(panel, "Caption", GaugeRowX, 16f, 300f, 32f, font, 20f, FontStyles.Normal,
                TextMain, TextAlignmentOptions.Left);
            caption.text = CargoFullPopupText.LoadCaption;
            loadText = ResourceSellUi.Label(panel, "Load", innerW - 24f - 320f, 10f, 320f, 42f, font, 28f, FontStyles.Bold,
                TextMain, TextAlignmentOptions.Right);
            loadText.enableAutoSizing = true;
            loadText.fontSizeMin = 18f;
            loadText.fontSizeMax = 28f;
            shortfallText = ResourceSellUi.Label(panel, "Shortfall", innerW - 24f - 420f, 96f, 420f, 28f, font, 20f,
                FontStyles.Bold, Amber, TextAlignmentOptions.Right);
            shortfallText.enableAutoSizing = true;
            shortfallText.fontSizeMin = 14f;
            shortfallText.fontSizeMax = 20f;

            gaugeRow = ResourceSellUi.Place(panel, "GaugeRow", GaugeRowX, GaugeRowY, innerW - GaugeRowX - 24f, GaugeRowH);
            overflowGlow = ResourceSellUi.AddImage(ResourceSellUi.Place(gaugeRow, "OverflowGlow", 0f, 0f, 10f, 10f),
                ResourceSellArt.SoftRect(), Color.clear);
            overflowGlow.enabled = false;
            mainCells = new GaugeCell[CargoGaugeLayout.MaxCells];
            for (var i = 0; i < mainCells.Length; i++)
            {
                mainCells[i] = Cell(gaugeRow, "Cell" + i, false);
            }

            overflowCells = new GaugeCell[CargoGaugeLayout.MaxOverflowCells];
            for (var i = 0; i < overflowCells.Length; i++)
            {
                overflowCells[i] = Cell(gaugeRow, "Overflow" + i, true);
            }

            overflowBracket = ResourceSellUi.AddImage(ResourceSellUi.Place(gaugeRow, "OverflowBracket", 0f, 0f, 10f, 10f),
                CargoFullPopupArt.Outline(), Amber);
            overflowBracket.type = Image.Type.Sliced;
            overflowBracket.pixelsPerUnitMultiplier = 0.5f;
        }

        private static GaugeCell Cell(Transform parent, string cellName, bool overflow)
        {
            var root = ResourceSellUi.Place(parent, cellName, 0f, 0f, 10f, GaugeRowH);
            var fill = ResourceSellUi.Image(root, "Fill", overflow ? null : CargoFullPopupArt.Cell(), Color.clear);
            fill.type = overflow ? Image.Type.Simple : Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = 0.5f;
            Image stripes = null;
            if (overflow)
            {
                stripes = ResourceSellUi.Image(root, "Stripes", CargoFullPopupArt.Stripes(), Color.clear);
                stripes.type = Image.Type.Tiled;
                stripes.pixelsPerUnitMultiplier = 0.5f;
                stripes.rectTransform.offsetMin = new Vector2(2f, 2f);
                stripes.rectTransform.offsetMax = new Vector2(-2f, -2f);
            }

            var line = ResourceSellUi.Image(root, "Line", CargoFullPopupArt.Outline(), Color.clear);
            line.type = Image.Type.Sliced;
            line.pixelsPerUnitMultiplier = 0.5f;
            return new GaugeCell { Root = root, Fill = fill, Line = line, Stripes = stripes };
        }

        private CargoFullChoiceButton BuildChoice(
            Button target,
            float x,
            float width,
            Color accent,
            Sprite iconSprite,
            float iconSize,
            string titleLabel,
            string subtitleLabel,
            out TMP_Text subtitle)
        {
            subtitle = null;
            if (target == null)
            {
                return null;
            }

            var rect = (RectTransform)target.transform;
            rect.SetParent(contentRoot, false);
            SetRect(rect, x, ButtonsY, width, ButtonH);
            var hitArea = target.GetComponent<Image>();
            if (hitArea != null)
            {
                hitArea.sprite = null;
                hitArea.color = Color.clear;
                hitArea.raycastTarget = true;
            }

            target.transition = Selectable.Transition.None;
            var glow = ResourceSellUi.Image(rect, "Glow", ResourceSellArt.SoftRect(), Color.clear);
            glow.rectTransform.offsetMin = new Vector2(-14f, -14f);
            glow.rectTransform.offsetMax = new Vector2(14f, 14f);
            var face = ResourceSellUi.Image(rect, "Face", ResourceSellArt.ChamferFill(), Color.clear);
            var border = ResourceSellUi.Image(rect, "Border", ResourceSellArt.ChamferOutline(), accent);
            var icon = ResourceSellUi.AddImage(
                ResourceSellUi.Place(rect, "Icon", 52f - iconSize * 0.5f, (ButtonH - iconSize) * 0.5f, iconSize, iconSize),
                iconSprite, accent);
            icon.preserveAspect = true;
            icon.enabled = iconSprite != null;

            TMP_Text title = null;
            var label = rect.Find("Label");
            if (label != null)
            {
                title = label.GetComponent<TMP_Text>();
            }

            if (title == null)
            {
                title = ResourceSellUi.Text(ResourceSellUi.Place(rect, "Label", 0f, 0f, 10f, 10f), font, 26f, FontStyles.Bold,
                    Color.white, TextAlignmentOptions.Left);
            }

            title.transform.SetAsLastSibling();
            SetRect(title.rectTransform, 92f, 20f, width - 92f - 20f, 40f);
            title.text = titleLabel;
            title.fontSize = 26f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Left;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.enableAutoSizing = true;
            title.fontSizeMin = 18f;
            title.fontSizeMax = 26f;
            title.overflowMode = TextOverflowModes.Ellipsis;

            subtitle = ResourceSellUi.Label(rect, "Subtitle", 92f, 62f, width - 92f - 20f, 28f, font, 17f, FontStyles.Normal,
                TextMuted, TextAlignmentOptions.Left);
            subtitle.text = subtitleLabel;
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 13f;
            subtitle.fontSizeMax = 17f;
            subtitle.overflowMode = TextOverflowModes.Ellipsis;

            var feedback = target.GetComponent<CargoFullChoiceButton>();
            if (feedback == null)
            {
                feedback = target.gameObject.AddComponent<CargoFullChoiceButton>();
            }

            var idle = Color.Lerp(new Color(0.02f, 0.035f, 0.05f, 0.97f), accent, 0.06f);
            var hover = Color.Lerp(new Color(0.02f, 0.035f, 0.05f, 1f), accent, 0.2f);
            feedback.Setup(target, face, border, glow, icon, title, subtitle, accent, idle, hover, TextMain, TextMuted);
            return feedback;
        }

        private void BuildFooter(float innerW)
        {
            var text = ResourceSellUi.Label(contentRoot, "Footer", Pad, FooterY, innerW, 28f, font, 16f, FontStyles.Normal,
                TextMuted, TextAlignmentOptions.Center);
            text.text = CargoFullPopupText.Footer;
            footerText = text;
            footerLineLeft = ResourceSellUi.AddImage(ResourceSellUi.Place(contentRoot, "FooterLineLeft", Pad, FooterY + 13f, 10f, 2f),
                null, WithAlpha(Teal, 0.25f));
            footerLineRight = ResourceSellUi.AddImage(ResourceSellUi.Place(contentRoot, "FooterLineRight", Pad, FooterY + 13f, 10f, 2f),
                null, WithAlpha(Teal, 0.25f));
        }

        // 글자 폭은 글꼴이 준비된 뒤에야 정확하므로 표시할 때마다 양옆 선을 맞춘다.
        private void LayoutFooter()
        {
            var size = ResolvePanelSize();
            var innerW = size.x - Pad * 2f;
            var textW = Mathf.Min(innerW - 120f, PreferredWidth(footerText, 16f));
            var lineW = Mathf.Max(0f, (innerW - textW) * 0.5f - 24f - 60f);
            SetRect(footerLineLeft.rectTransform, Pad + 60f, FooterY + 13f, lineW, 2f);
            SetRect(footerLineRight.rectTransform, size.x - Pad - 60f - lineW, FooterY + 13f, lineW, 2f);
        }

        private static float PreferredWidth(TMP_Text text, float fontSize)
        {
            text.enableAutoSizing = false;
            text.fontSize = fontSize;
            if (text.gameObject.activeInHierarchy)
            {
                text.ForceMeshUpdate(true, true);
            }

            return text.GetPreferredValues(text.text, 4000f, 400f).x;
        }

        /// <summary>한 줄 글자가 available을 넘으면 최소 크기까지 줄인다.</summary>
        private static void FitWidth(TMP_Text text, float maxSize, float minSize, float available)
        {
            var width = PreferredWidth(text, maxSize);
            if (width > available && width > 0f)
            {
                text.fontSize = Mathf.Max(minSize, Mathf.Floor(maxSize * available / width));
            }
        }

        private void Plate(RectTransform parent, float lineAlpha)
        {
            ResourceSellUi.Image(parent, "Plate", ResourceSellArt.ChamferFill(),
                skin != null ? skin.plate : new Color(0.025f, 0.05f, 0.07f, 0.94f));
            ResourceSellUi.Image(parent, "PlateLine", ResourceSellArt.ChamferOutline(), WithAlpha(Teal, lineAlpha));
        }

        private Vector2 ResolvePanelSize()
        {
            return panelSize.x > 100f && panelSize.y > 100f ? panelSize : DefaultPanelSize;
        }

        /// <summary>부모 왼쪽 위 기준 (x, y, w, h). 피벗은 가운데라 눌림이 제자리에서 일어난다.</summary>
        private static void SetRect(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h * 0.5f));
            rect.sizeDelta = new Vector2(w, h);
        }

        private Color Teal => skin != null ? skin.teal : new Color(0.42f, 0.94f, 1f, 1f);
        private Color Amber => skin != null ? skin.amber : new Color(1f, 0.74f, 0.2f, 1f);
        private Color Danger => skin != null ? skin.danger : new Color(0.95f, 0.3f, 0.3f, 1f);
        private Color TextMain => skin != null ? skin.textMain : new Color(0.9f, 0.97f, 1f, 1f);
        private Color TextMuted => skin != null ? skin.textMuted : new Color(0.62f, 0.76f, 0.82f, 1f);

        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
