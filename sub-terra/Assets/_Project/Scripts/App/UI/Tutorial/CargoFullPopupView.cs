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
    /// 화물 버리기 창(B-153)과는 같은 단말의 두 페이지라, 그 사이에서는 접지 않고 내용만 바꾼다(page).
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

        private const float Pad = CargoPopupChrome.Pad;
        private const float GuideY = 120f;
        private const float CardY = 164f;
        private const float CardH = 108f;
        private const float GaugeY = 288f;
        private const float ButtonsY = 440f;
        private const float ButtonH = 108f;
        private const float ButtonGap = 16f;
        private const float FooterY = 578f;
        private const float RewardBox = 72f;
        private const float RewardIcon = 56f;
        private const float MaxStep = 0.05f;
        private const float RewardFontSize = 32f;

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text guideText;
        [SerializeField] private Button dumpButton;
        [SerializeField] private Button forfeitButton;
        [SerializeField] private Vector2 panelSize = new Vector2(1000f, 660f);
        [SerializeField, Range(4, CargoGaugeLayout.MaxCells)] private int gaugeCells = 12;
        [SerializeField, Range(1, CargoGaugeLayout.MaxOverflowCells)] private int maxOverflowCells = 4;
        [SerializeField] private CargoFullPopupTiming timing = new CargoFullPopupTiming();
        [SerializeField] private CargoDiscardTiming pageTiming = new CargoDiscardTiming();

        private sealed class RewardSlot
        {
            public RectTransform Root;
            public Image Icon;
        }

        private readonly CargoFullChoiceGate gate = new CargoFullChoiceGate();
        private CargoFullPopupSkin skin;
        private MineResetTimedPopupSkin frameSkin;
        private TMP_FontAsset font;
        private bool built;
        private bool rendered;
        private PopupState state;
        private float clock;
        private bool paging;
        private CargoFullPopupTiming activeTiming;
        private CargoDiscardTiming activePageTiming;
        private CargoFullPopupTimeline.Pose pose;

        private CargoPopupFrame frame;
        private CargoGaugeView gauge;
        private CargoPopupFooter footer;

        private RectTransform rewardCard;
        private RewardSlot[] rewardSlots;
        private TMP_Text rewardCaption;
        private TMP_Text rewardAmount;

        private CargoFullChoiceButton dumpFeedback;
        private CargoFullChoiceButton forfeitFeedback;
        private TMP_Text forfeitSubtitle;

        private CargoShortfallModel model;
        private QuestReward reward;

        public PopupState State => state;
        public bool IsVisible => state != PopupState.Hidden;
        public bool IsClosing => state == PopupState.Closing;
        /// <summary>접지 않고 내용만 바꾸는 페이지 전환 중인지(열기·닫기 모두).</summary>
        public bool IsPaging => paging && (state == PopupState.Opening || state == PopupState.Closing);
        /// <summary>마지막 닫기가 페이지 넘김(접기 없음)으로 끝났는지. 다음 창이 같은 방식으로 들어올지 정한다.</summary>
        public bool LastCloseWasPage { get; private set; }
        public bool IsChoiceReady => state == PopupState.Open && gate.IsReady;
        public bool ManualTick { get; set; }
        public float Clock => clock;
        public float ContentAlpha => frame != null ? frame.ContentGroup.alpha : 0f;
        public float ShellReveal => frame != null ? frame.ShellMask.sizeDelta.y / ResolvePanelSize().y : 0f;
        public float OverflowFlash => pose.OverflowFlash;
        public float GaugeProgress => pose.Gauge;
        public CargoGaugeLayout GaugeLayout => gauge != null ? gauge.Layout : default;
        public CargoFullPopupTiming Timing => ResolveTiming();
        public string TitleString => titleText != null ? titleText.text : string.Empty;
        public string LoadString => gauge != null ? gauge.LoadText.text : string.Empty;
        public string ShortfallString => gauge != null ? gauge.ShortfallText.text : string.Empty;
        public string RewardString => rewardAmount != null ? rewardAmount.text : string.Empty;
        public string ForfeitSubtitleString => forfeitSubtitle != null ? forfeitSubtitle.text : string.Empty;
        public Button DumpButton => dumpButton;
        public Button ForfeitButton => forfeitButton;
        public int LitCellCount => gauge != null ? gauge.LitCellCount : 0;
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

            if (titleText != null)
            {
                titleText.text = CargoFullPopupText.Title;
            }

            if (guideText != null)
            {
                guideText.text = CargoFullPopupText.Guide;
            }

            gauge.SetModel(model, gaugeCells, maxOverflowCells);
            forfeitSubtitle.text = CargoFullPopupText.ForfeitSubtitle(reward);
            LayoutReward();
            footer.Layout(ResolvePanelSize().x);
            var opening = state == PopupState.Opening && !paging;
            gauge.Apply(opening ? pose.Gauge : 1f, opening ? pose.OverflowFlash : 0f);
        }

        /// <summary>연다. 이미 열려 있으면 내용만 유지하고, 한 번 고른 뒤 다시 불리면 새 선택을 받는다.</summary>
        public void Show()
        {
            Show(false);
        }

        /// <summary>page: 화물 버리기 창에서 돌아올 때. 프레임은 펼친 채로 두고 스캔선이 지나가며 내용만 들어온다.</summary>
        public void Show(bool page)
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
                footer.Layout(ResolvePanelSize().x);
            }

            gate.Reset();
            activeTiming = ResolveTiming();
            activePageTiming = ResolvePageTiming();
            state = PopupState.Opening;
            paging = page;
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
            Close(false);
        }

        /// <summary>page: 화물 버리기 창으로 넘어갈 때. 접지 않고 내용만 빠진 뒤 꺼진다.</summary>
        public void Close(bool page)
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
            paging = page;
            clock = 0f;
            activeTiming = ResolveTiming();
            activePageTiming = ResolvePageTiming();
            if (!gameObject.activeInHierarchy || (!Application.isPlaying && !ManualTick))
            {
                FinishClose();
                return;
            }

            ApplyClose(0f);
        }

        /// <summary>페이지 넘김으로 닫기 시작했는데 이어질 창이 없으면 평소처럼 접어서 닫는다.</summary>
        public void FoldInsteadOfPage()
        {
            if (state != PopupState.Closing || !paging)
            {
                return;
            }

            paging = false;
            clock = Mathf.Min(clock, activeTiming.closeDuration * 0.4f);
            ApplyClose(clock);
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
                    if (clock >= OpenDuration)
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
                    if (clock >= CloseDuration)
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

        private float OpenDuration => paging ? activePageTiming.pageInDuration : activeTiming.OpenDuration;

        private float CloseDuration => paging ? activePageTiming.pageOutDuration : activeTiming.closeDuration;

        private void OnDisable()
        {
            // 부모가 꺼져 연출이 끊겨도 다음 표시가 처음부터 시작되게 한다.
            if (state == PopupState.Hidden)
            {
                return;
            }

            var wasClosing = state == PopupState.Closing;
            LastCloseWasPage = wasClosing && paging;
            state = PopupState.Hidden;
            paging = false;
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
            clock = OpenDuration;
            ApplyOpen(clock);
            paging = false;
            gate.Arm();
            ApplyInteractable(true);
        }

        private void FinishClose()
        {
            var notify = state != PopupState.Hidden;
            LastCloseWasPage = state == PopupState.Closing && paging;
            state = PopupState.Hidden;
            paging = false;
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

        private CargoDiscardTiming ResolvePageTiming()
        {
            if (pageTiming == null)
            {
                pageTiming = new CargoDiscardTiming();
            }

            return pageTiming.Sanitized();
        }

        // ---------------- 연출 ----------------

        private void ApplyOpen(float t)
        {
            if (paging)
            {
                pose = CargoFullPopupTimeline.EvaluateOpen(activeTiming.OpenDuration, activeTiming);
                frame.ApplyPage(CargoDiscardTimeline.EvaluatePageIn(t, activePageTiming));
                gauge.Apply(1f, 0f);
            }
            else
            {
                pose = CargoFullPopupTimeline.EvaluateOpen(t, activeTiming);
                frame.ApplyOpen(pose);
                gauge.Apply(pose.Gauge, pose.OverflowFlash);
            }

            ApplyInteractable(state == PopupState.Open && gate.IsReady);
        }

        private void ApplyClose(float t)
        {
            if (paging)
            {
                frame.ApplyPage(CargoDiscardTimeline.EvaluatePageOut(t, activePageTiming));
                return;
            }

            pose = CargoFullPopupTimeline.EvaluateClose(t, activeTiming);
            frame.ApplyOpen(pose);
            gauge.Apply(pose.Gauge, pose.OverflowFlash);
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

            if (frame != null)
            {
                frame.ContentGroup.interactable = interactable;
                frame.ContentGroup.blocksRaycasts = state != PopupState.Hidden;
            }
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
            var textW = Mathf.Max(CargoPopupChrome.PreferredWidth(rewardCaption, 16f),
                CargoPopupChrome.PreferredWidth(rewardAmount, RewardFontSize)) + 8f;
            var cardW = Mathf.Min(maxCard, 24f + boxes + (count > 0 ? 20f : 0f) + textW + 28f);
            textW = cardW - 24f - boxes - (count > 0 ? 20f : 0f) - 28f;
            // 긴 보상은 칸 폭에 맞춰 글자만 줄인다(말줄임은 줄 높이가 넘치면 아무것도 안 그린다).
            CargoPopupChrome.FitWidth(rewardAmount, RewardFontSize, 18f, textW);
            CargoPopupChrome.SetRect(rewardCard, (size.x - cardW) * 0.5f, CardY, cardW, CardH);

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
                CargoPopupChrome.SetRect(target.Root, x, (CardH - RewardBox) * 0.5f, RewardBox, RewardBox);
                target.Icon.sprite = entries[i].sprite;
                target.Icon.enabled = entries[i].sprite != null;
                x += RewardBox + 10f;
            }

            for (var i = slot; i < rewardSlots.Length; i++)
            {
                rewardSlots[i].Root.gameObject.SetActive(false);
            }

            var textX = count > 0 ? x - 10f + 20f : 24f;
            CargoPopupChrome.SetRect(rewardCaption.rectTransform, textX, 18f, textW, 24f);
            CargoPopupChrome.SetRect(rewardAmount.rectTransform, textX, 44f, textW, 48f);
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
            font = CargoPopupChrome.ResolveFont(titleText, frameSkin);

            var size = ResolvePanelSize();
            frame = CargoPopupFrame.Build((RectTransform)transform, "CargoFull", size, skin, frameSkin);
            BuildContent(size);
            frame.BringScanToFront();
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

        private void BuildContent(Vector2 size)
        {
            var content = frame.ContentRoot;
            var innerW = size.x - Pad * 2f;
            var amber = CargoPopupChrome.Amber(skin);

            // 제목: 경고 아이콘 + 제목 + 구분선.
            CargoPopupChrome.BuildHeader(content, titleText, CargoFullPopupText.Title,
                skin != null ? skin.warningIcon : null, skin != null && skin.warningIcon != null ? Color.white : amber,
                innerW, frameSkin);

            if (guideText != null)
            {
                guideText.transform.SetParent(content, false);
                CargoPopupChrome.SetRect(guideText.rectTransform, Pad, GuideY, innerW, 32f);
                guideText.fontSize = 20f;
                guideText.fontStyle = FontStyles.Normal;
                guideText.color = CargoPopupChrome.TextMain(skin);
                guideText.alignment = TextAlignmentOptions.Center;
                guideText.textWrappingMode = TextWrappingModes.NoWrap;
                guideText.enableAutoSizing = true;
                guideText.fontSizeMin = 15f;
                guideText.fontSizeMax = 20f;
            }

            BuildRewardCard();
            gauge = CargoGaugeView.Build(content, Pad, GaugeY, innerW, skin, font, null);
            var buttonW = (innerW - ButtonGap) * 0.5f;
            dumpFeedback = CargoPopupChrome.BuildChoice(dumpButton, content, Pad, ButtonsY, buttonW, ButtonH,
                CargoPopupChrome.Teal(skin), CargoFullPopupArt.ListIcon(), 32f,
                CargoFullPopupText.DumpTitle, CargoFullPopupText.DumpSubtitle, false, font, skin, out _);
            forfeitFeedback = CargoPopupChrome.BuildChoice(forfeitButton, content, Pad + buttonW + ButtonGap, ButtonsY, buttonW,
                ButtonH, CargoPopupChrome.Danger(skin), skin != null ? skin.trashIcon : null, 36f,
                CargoFullPopupText.ForfeitTitle, string.Empty, false, font, skin, out forfeitSubtitle);
            footer = CargoPopupFooter.Build(content, FooterY, innerW, font, skin);
        }

        private void BuildRewardCard()
        {
            var content = frame.ContentRoot;
            var teal = CargoPopupChrome.Teal(skin);
            rewardCard = ResourceSellUi.Place(content, "RewardCard", 0f, CardY, 400f, CardH);
            CargoPopupChrome.Plate(rewardCard, skin, 0.4f);
            rewardSlots = new RewardSlot[4];
            for (var i = 0; i < rewardSlots.Length; i++)
            {
                var box = ResourceSellUi.Place(rewardCard, "RewardSlot" + i, 0f, 0f, RewardBox, RewardBox);
                ResourceSellUi.Image(box, "Face", ResourceSellArt.ChamferFill(), new Color(0.015f, 0.03f, 0.045f, 1f));
                ResourceSellUi.Image(box, "Line", ResourceSellArt.ChamferOutline(), CargoPopupChrome.WithAlpha(teal, 0.65f));
                var icon = ResourceSellUi.AddImage(ResourceSellUi.Centered(box, "Icon", Vector2.zero, new Vector2(RewardIcon, RewardIcon)),
                    null, Color.white);
                icon.preserveAspect = true;
                rewardSlots[i] = new RewardSlot { Root = box, Icon = icon };
            }

            rewardCaption = ResourceSellUi.Label(rewardCard, "Caption", 0f, 0f, 100f, 24f, font, 16f, FontStyles.Normal,
                CargoPopupChrome.TextMuted(skin), TextAlignmentOptions.Left);
            rewardCaption.text = CargoFullPopupText.RewardCaption;
            rewardAmount = ResourceSellUi.Label(rewardCard, "Amount", 0f, 0f, 100f, 48f, font, 32f, FontStyles.Bold,
                CargoPopupChrome.TextMain(skin), TextAlignmentOptions.Left);
        }

        private Vector2 ResolvePanelSize()
        {
            return panelSize.x > 100f && panelSize.y > 100f ? panelSize : DefaultPanelSize;
        }
    }
}
