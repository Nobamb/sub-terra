using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 화물 버리기 창(B-153). 기존 QuestRewardDumpPanel 위에 화물 부족 창(B-152)과 같은 프레임·헤더·게이지를 입힌다.
    /// 씬 오브젝트(DumpTitle, DumpCopper/DumpIron/DumpLithium의 Label·DumpOne·DumpAll, DumpCloseButton)는 이름과 onClick을
    /// 그대로 두고 위치·모양만 바꾼다. 표시·연출·중복 클릭 차단만 하며 버리기·지급 판정은 Presenter와 QuestRewardService가 한다.
    /// </summary>
    public sealed class CargoDiscardPopupView : MonoBehaviour
    {
        public enum PopupState
        {
            Hidden = 0,
            Opening = 1,
            Open = 2,
            Closing = 3
        }

        public enum CloseMode
        {
            /// <summary>B-152와 같이 가운데 선으로 접힌다.</summary>
            Fold = 0,
            /// <summary>화물 부족 창으로 넘어간다. 프레임은 두고 내용만 빠진다.</summary>
            Page = 1,
            /// <summary>공간 확보 완료를 잠깐 보여 준 뒤 접힌다.</summary>
            Complete = 2
        }

        public const string CopperRowName = "DumpCopper";
        public const string IronRowName = "DumpIron";
        public const string LithiumRowName = "DumpLithium";
        public const string TitleName = "DumpTitle";
        public const string SummaryName = "DumpSummary";
        public const string BackName = "DumpCloseButton";

        private const float Pad = CargoPopupChrome.Pad;
        private const float GaugeY = 116f;
        private const float RowsY = 258f;
        private const float RowH = 76f;
        private const float RowGap = 6f;
        private const float BackY = 508f;
        private const float BackH = 64f;
        private const float BackW = 360f;
        private const float FooterY = 586f;
        private const float ButtonW = 96f;
        private const float ButtonGap = 8f;
        private const float ButtonH = 38f;
        private const float RowRightPad = 14f;
        private const float NameX = 84f;
        private const float ChipIcon = 30f;
        private const float MaxStep = 0.05f;
        private const float DimmedRowAlpha = 0.38f;

        [SerializeField] private Vector2 panelSize = new Vector2(1000f, 660f);
        [SerializeField, Range(4, CargoGaugeLayout.MaxCells)] private int gaugeCells = 12;
        [SerializeField, Range(1, CargoGaugeLayout.MaxOverflowCells)] private int maxOverflowCells = 4;
        [SerializeField] private CargoFullPopupTiming openTiming = new CargoFullPopupTiming();
        [SerializeField] private CargoDiscardTiming timing = new CargoDiscardTiming();

        private sealed class Row
        {
            public string MineralId;
            public RectTransform Root;
            public CanvasGroup Group;
            public Image Icon;
            public TMP_Text Name;
            public TMP_Text Quantity;
            public TMP_Text Detail;
            public readonly Button[] Buttons = new Button[5];
            public readonly CargoFullChoiceButton[] Feedback = new CargoFullChoiceButton[5];
            public readonly TMP_Text[] Freed = new TMP_Text[5];
            public string DisplayName = string.Empty;
            public int Count = -1;
            public float UnitWeight;
            public float BlinkClock = -1f;
        }

        private sealed class Chip
        {
            public Image Icon;
            public TMP_Text Amount;
        }

        private readonly CargoFullChoiceGate gate = new CargoFullChoiceGate();
        private CargoFullPopupSkin skin;
        private MineResetTimedPopupSkin frameSkin;
        private TMP_FontAsset font;
        private bool built;
        private bool rendered;
        private PopupState state;
        private CloseMode closeMode;
        private bool paging;
        private float clock;
        private CargoFullPopupTiming activeOpen;
        private CargoDiscardTiming activeTiming;
        private CargoFullPopupTimeline.Pose pose;

        private CargoPopupFrame frame;
        private CargoGaugeView gauge;
        private CargoPopupFooter footer;
        private TMP_Text titleText;
        private TMP_Text rewardCaption;
        private Chip[] chips;
        private Row[] rows;
        private Button backButton;
        private CargoFullChoiceButton backFeedback;

        private CargoShortfallModel model;
        private QuestReward reward;
        private float shownWeight;
        private float fromWeight;
        private float gaugeClock = -1f;
        private bool cleared;
        private float clearedClock = -1f;
        private float holdClock;

        public PopupState State => state;
        public bool IsVisible => state != PopupState.Hidden;
        /// <summary>닫히는 중(접기·페이지 넘김·완료 표시 포함). 다음 창은 이게 끝난 뒤 뜬다.</summary>
        public bool IsClosing => state == PopupState.Closing;
        public bool IsPaging => paging && (state == PopupState.Opening || state == PopupState.Closing);
        public bool LastCloseWasPage { get; private set; }
        public CloseMode ActiveCloseMode => closeMode;
        public bool IsCleared => cleared;
        public bool IsDumpReady => state == PopupState.Open && !cleared && gate.IsReady;
        public bool ManualTick { get; set; }
        public float ContentAlpha => frame != null ? frame.ContentGroup.alpha : 0f;
        public float ShellReveal => frame != null ? frame.ShellMask.sizeDelta.y / ResolvePanelSize().y : 0f;
        public float DisplayedWeight => shownWeight;
        public CargoShortfallModel Model => model;
        public CargoDiscardTiming Timing => ResolveTiming();
        public string TitleString => titleText != null ? titleText.text : string.Empty;
        public string LoadString => gauge != null ? gauge.LoadText.text : string.Empty;
        public string ShortfallString => gauge != null ? gauge.ShortfallText.text : string.Empty;
        public string HeaderRewardString => CargoFullPopupText.RewardLabel(reward);
        public Button BackButton => backButton;

        /// <summary>닫기 연출이 끝나 창이 꺼진 직후. 다음 창(화물 부족·클리어)을 이어 띄우는 데 쓴다.</summary>
        public event Action Closed;

        /// <summary>씬 오브젝트를 찾고(없으면 만들고) 장식을 입힌다. 버튼 연결 전에 부른다.</summary>
        public void EnsureReady()
        {
            EnsureBuilt();
        }

        public Button GetButton(string mineralId, CargoDiscardKind kind)
        {
            var row = FindRow(mineralId);
            return row != null ? row.Buttons[(int)kind] : null;
        }

        public string QuantityString(string mineralId) => TextOf(FindRow(mineralId)?.Quantity);
        public string NameString(string mineralId) => TextOf(FindRow(mineralId)?.Name);
        public string DetailString(string mineralId) => TextOf(FindRow(mineralId)?.Detail);
        public string FreedString(string mineralId, CargoDiscardKind kind) => TextOf(FindRow(mineralId)?.Freed[(int)kind]);
        public float RowAlpha(string mineralId) => FindRow(mineralId)?.Group.alpha ?? 0f;
        public float QuantityBlink(string mineralId)
        {
            var row = FindRow(mineralId);
            return row != null && row.BlinkClock >= 0f ? CargoDiscardTimeline.QuantityBlink(row.BlinkClock, ResolveTiming()) : 0f;
        }

        /// <summary>버튼이 요청할 수량. 1·5·10·전부는 고정(Presenter가 보유 수로 자른다), 필요는 부족을 0으로 만드는 최소 개수.</summary>
        public int RequestCount(string mineralId, CargoDiscardKind kind)
        {
            if (kind != CargoDiscardKind.Needed)
            {
                return CargoDiscardText.FixedCount(kind);
            }

            var row = FindRow(mineralId);
            return row != null ? CargoDiscardMath.Needed(model.Shortfall, row.UnitWeight, Math.Max(0, row.Count)).Count : 0;
        }

        public void Render(CargoShortfallModel shortfall, QuestReward questReward, IReadOnlyList<QuestDumpRow> dumpRows)
        {
            EnsureBuilt();
            var wasShort = rendered && model.IsShort;
            var animate = rendered && state != PopupState.Hidden;
            model = shortfall;
            reward = questReward;

            if (animate && Mathf.Abs(shownWeight - model.CurrentWeight) > 0.0001f)
            {
                fromWeight = shownWeight;
                gaugeClock = 0f;
            }
            else if (!animate)
            {
                shownWeight = model.CurrentWeight;
                gaugeClock = -1f;
            }

            if (wasShort && !model.IsShort && state != PopupState.Hidden)
            {
                // 버린 결과 부족이 0이 됐다. 로직은 이미 지급을 마쳤으므로 표시만 하고 버튼은 잠근다.
                cleared = true;
                gate.Disarm();
            }

            for (var i = 0; i < rows.Length; i++)
            {
                RenderRow(rows[i], dumpRows, animate);
            }

            rendered = true;
            if (titleText != null)
            {
                titleText.text = CargoDiscardText.Title;
            }

            ApplyGauge();
            LayoutHeader();
            footer.Layout(ResolvePanelSize().x);
            ApplyInteractable();
        }

        public void Show(bool page)
        {
            EnsureBuilt();
            if (state == PopupState.Opening || state == PopupState.Open)
            {
                return;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            transform.SetAsLastSibling();
            if (rendered)
            {
                // 꺼진 상태에서 잰 글자 폭은 0일 수 있어 켜진 뒤 다시 잰다.
                LayoutHeader();
                for (var i = 0; i < rows.Length; i++)
                {
                    LayoutRowText(rows[i]);
                }

                footer.Layout(ResolvePanelSize().x);
            }

            gate.Reset();
            cleared = false;
            clearedClock = -1f;
            gaugeClock = -1f;
            shownWeight = model.CurrentWeight;
            activeOpen = ResolveOpenTiming();
            activeTiming = ResolveTiming();
            state = PopupState.Opening;
            paging = page;
            clock = 0f;
            if (!Application.isPlaying && !ManualTick)
            {
                SettleOpen();
                return;
            }

            ApplyOpen(0f);
        }

        public void Close(CloseMode mode)
        {
            if (state == PopupState.Hidden)
            {
                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            if (state == PopupState.Closing)
            {
                // 완료 표시는 다른 닫기 요청에 끊기지 않는다.
                if (closeMode != CloseMode.Complete && mode == CloseMode.Complete)
                {
                    closeMode = mode;
                }

                return;
            }

            gate.Disarm();
            state = PopupState.Closing;
            closeMode = mode;
            paging = mode == CloseMode.Page;
            clock = 0f;
            holdClock = 0f;
            activeOpen = ResolveOpenTiming();
            activeTiming = ResolveTiming();
            ApplyInteractable();
            if (!gameObject.activeInHierarchy || (!Application.isPlaying && !ManualTick))
            {
                FinishClose();
                return;
            }

            if (mode == CloseMode.Complete)
            {
                cleared = true;
                if (clearedClock < 0f && gaugeClock < 0f)
                {
                    clearedClock = 0f;
                }

                ApplyGauge();
                return;
            }

            ApplyClose(0f);
        }

        /// <summary>페이지 넘김으로 닫기 시작했는데 화물 부족 창이 이어지지 않으면(지급 완료 등) 평소처럼 접는다.</summary>
        public void FoldInsteadOfPage()
        {
            if (state != PopupState.Closing || closeMode != CloseMode.Page)
            {
                return;
            }

            closeMode = CloseMode.Fold;
            paging = false;
            clock = Mathf.Min(clock, activeOpen.closeDuration * 0.4f);
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

        /// <summary>수량 버튼 onClick 입구. 열려 있고 처리 중이 아닐 때 한 번만 true.</summary>
        public bool TryAcceptDump()
        {
            if (state != PopupState.Open || cleared || !gate.TryConsume())
            {
                return false;
            }

            return true;
        }

        /// <summary>Presenter 처리가 끝난 뒤 다음 클릭을 받는다. 닫히는 중이거나 공간을 다 비웠으면 잠근 채 둔다.</summary>
        public void ReleaseDump()
        {
            if (state != PopupState.Open || cleared)
            {
                return;
            }

            gate.Reset();
            gate.Arm();
        }

        /// <summary>돌아가기 onClick 입구. 열려 있을 때 한 번만 true.</summary>
        public bool TryAcceptBack()
        {
            return state == PopupState.Open && gate.TryConsume();
        }

        private void Update()
        {
            if (ManualTick || state == PopupState.Hidden)
            {
                return;
            }

            if (state == PopupState.Open && !HasRunningEffects())
            {
                return;
            }

            Tick(Mathf.Min(Time.unscaledDeltaTime, MaxStep));
        }

        /// <summary>테스트와 Update가 같은 경로로 시간을 진행한다.</summary>
        public void Tick(float dt)
        {
            if (state == PopupState.Hidden)
            {
                return;
            }

            StepEffects(dt);
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
                    if (closeMode == CloseMode.Complete)
                    {
                        holdClock += dt;
                        if (holdClock < activeTiming.completeHold)
                        {
                            return;
                        }

                        // 완료 표시가 끝나면 B-152와 같이 접는다.
                        closeMode = CloseMode.Fold;
                        clock = 0f;
                    }

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

        private float OpenDuration => paging ? activeTiming.pageInDuration : activeOpen.OpenDuration;

        private float CloseDuration => closeMode == CloseMode.Page ? activeTiming.pageOutDuration : activeOpen.closeDuration;

        private bool HasRunningEffects()
        {
            if (gaugeClock >= 0f || clearedClock >= 0f)
            {
                return true;
            }

            for (var i = 0; i < rows.Length; i++)
            {
                if (rows[i].BlinkClock >= 0f)
                {
                    return true;
                }
            }

            return false;
        }

        private void StepEffects(float dt)
        {
            var timingNow = activeTiming ?? ResolveTiming();
            if (gaugeClock >= 0f)
            {
                gaugeClock += dt;
                var p = CargoDiscardTimeline.GaugeTween(gaugeClock, timingNow);
                var wasShort = CargoShortfallModel.From(shownWeight, model.MaxCapacity, model.RewardWeight).IsShort;
                shownWeight = Mathf.Lerp(fromWeight, model.CurrentWeight, p);
                if (gaugeClock >= timingNow.gaugeTweenDuration)
                {
                    shownWeight = model.CurrentWeight;
                    gaugeClock = -1f;
                }

                var nowShort = CargoShortfallModel.From(shownWeight, model.MaxCapacity, model.RewardWeight).IsShort;
                if (cleared && wasShort && !nowShort && clearedClock < 0f)
                {
                    clearedClock = 0f;
                }
            }
            else if (clearedClock >= 0f)
            {
                clearedClock += dt;
                if (clearedClock >= timingNow.clearedFlashDuration)
                {
                    clearedClock = -1f;
                }
            }

            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (row.BlinkClock < 0f)
                {
                    continue;
                }

                row.BlinkClock += dt;
                if (row.BlinkClock >= timingNow.quantityBlinkDuration)
                {
                    row.BlinkClock = -1f;
                }

                ApplyBlink(row);
            }

            ApplyGauge();
        }

        private void OnDisable()
        {
            if (state == PopupState.Hidden)
            {
                return;
            }

            var wasClosing = state == PopupState.Closing;
            LastCloseWasPage = wasClosing && closeMode == CloseMode.Page;
            ResetToHidden();
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
            ApplyInteractable();
        }

        private void FinishClose()
        {
            var notify = state != PopupState.Hidden;
            LastCloseWasPage = state == PopupState.Closing && closeMode == CloseMode.Page;
            ResetToHidden();
            ApplyInteractable();
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            if (notify)
            {
                Closed?.Invoke();
            }
        }

        private void ResetToHidden()
        {
            state = PopupState.Hidden;
            closeMode = CloseMode.Fold;
            paging = false;
            clock = 0f;
            holdClock = 0f;
            gaugeClock = -1f;
            clearedClock = -1f;
            cleared = false;
            shownWeight = model.CurrentWeight;
            gate.Reset();
            if (rows == null)
            {
                return;
            }

            for (var i = 0; i < rows.Length; i++)
            {
                rows[i].BlinkClock = -1f;
                ApplyBlink(rows[i]);
            }
        }

        private CargoFullPopupTiming ResolveOpenTiming()
        {
            if (openTiming == null)
            {
                openTiming = new CargoFullPopupTiming();
            }

            return openTiming.Sanitized();
        }

        private CargoDiscardTiming ResolveTiming()
        {
            if (timing == null)
            {
                timing = new CargoDiscardTiming();
            }

            return timing.Sanitized();
        }

        // ---------------- 연출 ----------------

        private void ApplyOpen(float t)
        {
            if (paging)
            {
                pose = CargoFullPopupTimeline.EvaluateOpen(activeOpen.OpenDuration, activeOpen);
                frame.ApplyPage(CargoDiscardTimeline.EvaluatePageIn(t, activeTiming));
            }
            else
            {
                pose = CargoFullPopupTimeline.EvaluateOpen(t, activeOpen);
                frame.ApplyOpen(pose);
            }

            ApplyGauge();
            ApplyInteractable();
        }

        private void ApplyClose(float t)
        {
            if (closeMode == CloseMode.Page)
            {
                frame.ApplyPage(CargoDiscardTimeline.EvaluatePageOut(t, activeTiming));
                return;
            }

            pose = CargoFullPopupTimeline.EvaluateClose(t, activeOpen);
            frame.ApplyOpen(pose);
        }

        private void ApplyGauge()
        {
            if (gauge == null)
            {
                return;
            }

            var shown = CargoShortfallModel.From(shownWeight, model.MaxCapacity, model.RewardWeight);
            gauge.SetModel(shown, gaugeCells, maxOverflowCells);
            var opening = state == PopupState.Opening && !paging;
            var flash = clearedClock >= 0f ? CargoDiscardTimeline.ClearedFlash(clearedClock, activeTiming ?? ResolveTiming()) : 0f;
            gauge.Apply(opening ? pose.Gauge : 1f, opening ? pose.OverflowFlash : 0f, flash);
        }

        private void ApplyBlink(Row row)
        {
            var blink = row.BlinkClock >= 0f ? CargoDiscardTimeline.QuantityBlink(row.BlinkClock, activeTiming ?? ResolveTiming()) : 0f;
            row.Quantity.color = Color.Lerp(CargoPopupChrome.Teal(skin), Color.white, 0.75f * blink);
        }

        private void ApplyInteractable()
        {
            if (rows == null)
            {
                return;
            }

            var open = state == PopupState.Open;
            var dumpAllowed = open && !cleared;
            for (var r = 0; r < rows.Length; r++)
            {
                var row = rows[r];
                var owned = Math.Max(0, row.Count);
                for (var k = 0; k < row.Buttons.Length; k++)
                {
                    var button = row.Buttons[k];
                    if (button == null)
                    {
                        continue;
                    }

                    var enabled = owned > 0;
                    if ((CargoDiscardKind)k == CargoDiscardKind.Needed)
                    {
                        enabled = enabled && model.IsShort && row.UnitWeight > 0f;
                    }

                    button.interactable = dumpAllowed && enabled;
                }
            }

            if (backButton != null)
            {
                backButton.interactable = open;
            }

            if (frame != null)
            {
                frame.ContentGroup.interactable = open;
                frame.ContentGroup.blocksRaycasts = state != PopupState.Hidden;
            }
        }

        // ---------------- 표시값 ----------------

        private void RenderRow(Row row, IReadOnlyList<QuestDumpRow> dumpRows, bool animate)
        {
            var quantity = 0;
            var unit = 0f;
            var display = QuestRewardService.DisplayNameOf(row.MineralId);
            if (dumpRows != null)
            {
                for (var i = 0; i < dumpRows.Count; i++)
                {
                    if (dumpRows[i].MineralId != row.MineralId)
                    {
                        continue;
                    }

                    quantity = dumpRows[i].Quantity;
                    unit = dumpRows[i].UnitWeight;
                    if (!string.IsNullOrEmpty(dumpRows[i].DisplayName))
                    {
                        display = dumpRows[i].DisplayName;
                    }

                    break;
                }
            }

            if (animate && row.Count >= 0 && row.Count != quantity)
            {
                row.BlinkClock = 0f;
            }

            if (unit <= 0f && row.UnitWeight > 0f)
            {
                // 0개가 되면 스냅샷에서 줄이 빠져 무게를 모른다. 마지막 값을 그대로 보여 준다.
                unit = row.UnitWeight;
            }

            row.Count = quantity;
            row.UnitWeight = unit;
            row.DisplayName = display;
            row.Name.text = display;
            row.Quantity.text = CargoDiscardText.Quantity(quantity);
            row.Detail.text = CargoDiscardText.Detail(unit, quantity);
            row.Group.alpha = quantity > 0 ? 1f : DimmedRowAlpha;

            var amber = CargoPopupChrome.Amber(skin);
            var muted = CargoPopupChrome.TextMuted(skin);
            for (var k = 0; k < CargoDiscardText.Kinds.Length; k++)
            {
                var kind = CargoDiscardText.Kinds[k];
                var label = row.Freed[k];
                if (kind == CargoDiscardKind.Needed)
                {
                    var need = model.IsShort ? CargoDiscardMath.Needed(model.Shortfall, unit, quantity) : default;
                    label.text = CargoDiscardText.NeededLabel(need);
                    label.color = need.Covers ? CargoPopupChrome.Teal(skin) : amber;
                    continue;
                }

                label.text = CargoDiscardText.Freed(CargoDiscardMath.Clamp(CargoDiscardText.FixedCount(kind), quantity), unit);
                label.color = muted;
            }

            LayoutRowText(row);
            ApplyBlink(row);
        }

        // ---------------- 배치 ----------------

        private void LayoutHeader()
        {
            var entries = new (Sprite sprite, int amount)[]
            {
                (skin != null ? skin.copperIcon : null, reward.Copper),
                (skin != null ? skin.ironIcon : null, reward.Iron),
                (skin != null ? skin.lithiumIcon : null, reward.Lithium),
                (skin != null ? skin.goldIcon : null, reward.Gold)
            };

            // 오른쪽 끝에서부터 [아이콘 ×N] 칩을 왼쪽으로 쌓는다.
            var right = ResolvePanelSize().x - Pad - 4f;
            const float chipY = CargoPopupChrome.TitleY + 10f;
            var slot = chips.Length - 1;
            var any = false;
            for (var i = entries.Length - 1; i >= 0; i--)
            {
                if (entries[i].amount <= 0)
                {
                    continue;
                }

                var chip = chips[slot--];
                chip.Amount.text = "×" + entries[i].amount;
                var textW = CargoPopupChrome.PreferredWidth(chip.Amount, 22f) + 2f;
                CargoPopupChrome.SetRect(chip.Amount.rectTransform, right - textW, chipY, textW, 36f);
                CargoPopupChrome.SetRect(chip.Icon.rectTransform, right - textW - ChipIcon - 4f, chipY + 3f, ChipIcon, ChipIcon);
                chip.Icon.sprite = entries[i].sprite;
                chip.Icon.enabled = entries[i].sprite != null;
                chip.Amount.gameObject.SetActive(true);
                chip.Icon.gameObject.SetActive(true);
                right -= textW + ChipIcon + 4f + 16f;
                any = true;
            }

            for (var i = slot; i >= 0; i--)
            {
                chips[i].Amount.gameObject.SetActive(false);
                chips[i].Icon.gameObject.SetActive(false);
            }

            rewardCaption.gameObject.SetActive(any);
            if (any)
            {
                var captionW = CargoPopupChrome.PreferredWidth(rewardCaption, 15f) + 2f;
                CargoPopupChrome.SetRect(rewardCaption.rectTransform, right + 16f - 8f - captionW, chipY, captionW, 36f);
            }
        }

        private void LayoutRowText(Row row)
        {
            var columnEnd = RowWidth - RowRightPad - ButtonsWidth - 12f;
            var nameW = Mathf.Min(140f, CargoPopupChrome.PreferredWidth(row.Name, 22f) + 2f);
            CargoPopupChrome.FitWidth(row.Name, 22f, 15f, nameW);
            CargoPopupChrome.SetRect(row.Name.rectTransform, NameX, 6f, nameW, 34f);
            var qtyX = NameX + nameW + 8f;
            var qtyW = Mathf.Max(20f, columnEnd - qtyX);
            CargoPopupChrome.SetRect(row.Quantity.rectTransform, qtyX, 6f, qtyW, 34f);
            CargoPopupChrome.FitWidth(row.Quantity, 22f, 14f, qtyW);
            CargoPopupChrome.SetRect(row.Detail.rectTransform, NameX, 42f, columnEnd - NameX, 24f);
            CargoPopupChrome.FitWidth(row.Detail, 15f, 11f, columnEnd - NameX);
        }

        private static float ButtonsWidth => ButtonW * 5f + ButtonGap * 4f;

        private float RowWidth => ResolvePanelSize().x - Pad * 2f;

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
            titleText = FindChild<TMP_Text>(transform, TitleName);
            font = CargoPopupChrome.ResolveFont(titleText, frameSkin);
            if (titleText == null)
            {
                titleText = ResourceSellUi.Label(transform, TitleName, 0f, 0f, 10f, 10f, font, 40f, FontStyles.Bold, Color.white,
                    TextAlignmentOptions.Left);
            }

            var summary = transform.Find(SummaryName);
            if (summary != null)
            {
                // 요약 한 줄("현재 화물 … · 보상 중량 …")은 게이지가 대신한다. 참조 유지를 위해 끄기만 한다.
                summary.gameObject.SetActive(false);
            }

            var size = ResolvePanelSize();
            frame = CargoPopupFrame.Build((RectTransform)transform, "CargoDiscard", size, skin, frameSkin);
            BuildContent(size);
            frame.BringScanToFront();
            ApplyInteractable();
        }

        private void BuildContent(Vector2 size)
        {
            var content = frame.ContentRoot;
            var innerW = size.x - Pad * 2f;
            var teal = CargoPopupChrome.Teal(skin);
            CargoPopupChrome.BuildHeader(content, titleText, CargoDiscardText.Title, CargoFullPopupArt.ListIcon(), teal, innerW,
                frameSkin);

            rewardCaption = ResourceSellUi.Label(content, "RewardCaption", 0f, 0f, 10f, 36f, font, 15f, FontStyles.Normal,
                CargoPopupChrome.TextMuted(skin), TextAlignmentOptions.Right);
            rewardCaption.text = CargoDiscardText.RewardCaption;
            chips = new Chip[4];
            for (var i = 0; i < chips.Length; i++)
            {
                var icon = ResourceSellUi.AddImage(ResourceSellUi.Place(content, "RewardIcon" + i, 0f, 0f, ChipIcon, ChipIcon), null,
                    Color.white);
                icon.preserveAspect = true;
                var amount = ResourceSellUi.Label(content, "RewardAmount" + i, 0f, 0f, 10f, 36f, font, 22f, FontStyles.Bold,
                    CargoPopupChrome.TextMain(skin), TextAlignmentOptions.Left);
                chips[i] = new Chip { Icon = icon, Amount = amount };
            }

            gauge = CargoGaugeView.Build(content, Pad, GaugeY, innerW, skin, font, CargoDiscardText.Cleared);
            rows = new[]
            {
                BuildRow(content, CopperRowName, DataIds.Minerals.Copper, skin != null ? skin.copperIcon : null, 0),
                BuildRow(content, IronRowName, DataIds.Minerals.Iron, skin != null ? skin.ironIcon : null, 1),
                BuildRow(content, LithiumRowName, DataIds.Minerals.Lithium, skin != null ? skin.lithiumIcon : null, 2)
            };

            backButton = EnsureButton(transform, BackName, CargoDiscardText.BackTitle);
            var neutral = Color.Lerp(teal, CargoPopupChrome.TextMuted(skin), 0.45f);
            backFeedback = CargoPopupChrome.BuildChoice(backButton, content, (size.x - BackW) * 0.5f, BackY, BackW, BackH, neutral,
                CargoFullPopupArt.BackIcon(), 28f, CargoDiscardText.BackTitle, CargoDiscardText.BackSubtitle, true, font, skin,
                out _);
            footer = CargoPopupFooter.Build(content, FooterY, innerW, font, skin);
        }

        private Row BuildRow(RectTransform content, string rowName, string mineralId, Sprite iconSprite, int index)
        {
            var existing = transform.Find(rowName);
            RectTransform root;
            if (existing != null)
            {
                root = (RectTransform)existing;
                root.SetParent(content, false);
            }
            else
            {
                root = ResourceSellUi.Place(content, rowName, 0f, 0f, 10f, 10f);
            }

            var width = RowWidth;
            CargoPopupChrome.SetRect(root, Pad, RowsY + index * (RowH + RowGap), width, RowH);
            var group = root.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = root.gameObject.AddComponent<CanvasGroup>();
            }

            var row = new Row { MineralId = mineralId, Root = root, Group = group };
            CargoPopupChrome.Plate(root, skin, 0.22f);
            root.Find("Plate").SetAsFirstSibling();
            root.Find("PlateLine").SetSiblingIndex(1);

            var teal = CargoPopupChrome.Teal(skin);
            var box = ResourceSellUi.Place(root, "IconBox", 14f, (RowH - 56f) * 0.5f, 56f, 56f);
            ResourceSellUi.Image(box, "Face", ResourceSellArt.ChamferFill(), new Color(0.015f, 0.03f, 0.045f, 1f));
            ResourceSellUi.Image(box, "Line", ResourceSellArt.ChamferOutline(), CargoPopupChrome.WithAlpha(teal, 0.55f));
            row.Icon = ResourceSellUi.AddImage(ResourceSellUi.Centered(box, "Icon", Vector2.zero, new Vector2(44f, 44f)), iconSprite,
                Color.white);
            row.Icon.preserveAspect = true;
            row.Icon.enabled = iconSprite != null;

            row.Name = FindChild<TMP_Text>(root, "Label");
            if (row.Name == null)
            {
                row.Name = ResourceSellUi.Label(root, "Label", 0f, 0f, 10f, 10f, font, 22f, FontStyles.Bold, Color.white,
                    TextAlignmentOptions.Left);
            }

            StyleText(row.Name, 22f, FontStyles.Bold, CargoPopupChrome.TextMain(skin), TextAlignmentOptions.Left);
            row.Quantity = ResourceSellUi.Label(root, "Quantity", 0f, 0f, 10f, 10f, font, 22f, FontStyles.Bold, teal,
                TextAlignmentOptions.Left);
            row.Detail = ResourceSellUi.Label(root, "Detail", 0f, 0f, 10f, 10f, font, 15f, FontStyles.Normal,
                CargoPopupChrome.TextMuted(skin), TextAlignmentOptions.Left);

            var start = width - RowRightPad - ButtonsWidth;
            var neutral = Color.Lerp(teal, CargoPopupChrome.TextMuted(skin), 0.45f);
            var danger = Color.Lerp(CargoPopupChrome.Danger(skin), Color.black, 0.15f);
            for (var k = 0; k < CargoDiscardText.Kinds.Length; k++)
            {
                var kind = CargoDiscardText.Kinds[k];
                var label = CargoDiscardText.ButtonLabel(kind);
                var button = EnsureButton(root, ButtonName(kind), label);
                var x = start + k * (ButtonW + ButtonGap);
                CargoPopupChrome.SetRect((RectTransform)button.transform, x, 6f, ButtonW, ButtonH);
                Color accent;
                float tint;
                switch (kind)
                {
                    case CargoDiscardKind.Needed:
                        accent = teal;
                        tint = 0.16f;
                        break;
                    case CargoDiscardKind.All:
                        accent = danger;
                        tint = 0.04f;
                        break;
                    default:
                        accent = neutral;
                        tint = 0.06f;
                        break;
                }

                row.Buttons[k] = button;
                row.Feedback[k] = CargoPopupChrome.BuildCompactButton(button, label, accent, tint, font, skin);
                // 필요는 "전부 버려도 N 부족"까지 들어가도록 이웃 칸 쪽으로 조금 넓게 쓴다.
                var freedW = kind == CargoDiscardKind.Needed ? 140f : ButtonW;
                var freed = ResourceSellUi.Label(root, "Freed" + kind, x + (ButtonW - freedW) * 0.5f, 6f + ButtonH + 2f, freedW, 24f,
                    font, 13f, FontStyles.Normal, CargoPopupChrome.TextMuted(skin), TextAlignmentOptions.Center);
                freed.enableAutoSizing = true;
                freed.fontSizeMin = 10f;
                freed.fontSizeMax = 13f;
                row.Freed[k] = freed;
            }

            return row;
        }

        public static string ButtonName(CargoDiscardKind kind)
        {
            switch (kind)
            {
                case CargoDiscardKind.One:
                    return "DumpOne";
                case CargoDiscardKind.Five:
                    return "DumpFive";
                case CargoDiscardKind.Ten:
                    return "DumpTen";
                case CargoDiscardKind.Needed:
                    return "DumpNeeded";
                default:
                    return "DumpAll";
            }
        }

        public static string RowName(string mineralId)
        {
            if (mineralId == DataIds.Minerals.Copper)
            {
                return CopperRowName;
            }

            if (mineralId == DataIds.Minerals.Iron)
            {
                return IronRowName;
            }

            return mineralId == DataIds.Minerals.Lithium ? LithiumRowName : string.Empty;
        }

        private Button EnsureButton(Transform parent, string buttonName, string label)
        {
            var existing = parent.Find(buttonName);
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null)
            {
                // 빌더를 다시 돌리지 않은 씬에서도 쓰도록 빠진 버튼만 만든다. 연결은 DemoObjectiveView가 한다.
                var rect = ResourceSellUi.Place(parent, buttonName, 0f, 0f, 10f, 10f);
                var hit = rect.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                button = rect.gameObject.AddComponent<Button>();
                ResourceSellUi.Label(rect, "Label", 0f, 0f, 10f, 10f, font, 18f, FontStyles.Bold, Color.white,
                    TextAlignmentOptions.Center).text = label;
            }

            return button;
        }

        private static void StyleText(TMP_Text text, float size, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
        }

        private Row FindRow(string mineralId)
        {
            EnsureBuilt();
            for (var i = 0; i < rows.Length; i++)
            {
                if (rows[i].MineralId == mineralId)
                {
                    return rows[i];
                }
            }

            return null;
        }

        private static T FindChild<T>(Transform parent, string childName) where T : Component
        {
            var child = parent.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static string TextOf(TMP_Text text)
        {
            return text != null ? text.text : string.Empty;
        }

        private Vector2 ResolvePanelSize()
        {
            return panelSize.x > 100f && panelSize.y > 100f ? panelSize : CargoFullPopupView.DefaultPanelSize;
        }
    }
}
