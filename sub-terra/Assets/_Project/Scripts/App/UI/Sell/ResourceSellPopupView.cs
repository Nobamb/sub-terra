using System;
using System.Collections.Generic;
using SubTerra.App.UI.HUD;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Sell
{
    /// <summary>
    /// 지상 판매창과 정산 콘솔이 같이 쓰는 자원 판매 팝업(B-136).
    /// 표시·입력 위임·연출만 한다. 수량·예상 결과·확정 재검증은 ResourceSellSession, 거래는 각 화면 서비스가 한다.
    /// 프레임과 배경은 5개 구간으로 나뉜 껍데기(Shell)만 아코디언처럼 압축·펼침되고, 글자·아이콘·버튼은
    /// 별도 Content 레이어라 찌그러지지 않는다. 시간은 unscaledDeltaTime이다.
    /// </summary>
    public sealed class ResourceSellPopupView : MonoBehaviour
    {
        public enum PopupState
        {
            Hidden = 0,
            Opening = 1,
            Open = 2,
            Closing = 3
        }

        public const string Title = "자원 판매";
        public const string EmptyLabel = "판매할 자원이 없습니다";
        public const string SelectAllLabel = "최대 선택";
        public const string ResetLabel = "선택 초기화";
        public const string SellLabel = "판매";
        public static readonly Vector2 CardSize = new Vector2(1400f, 840f);

        // 좌우 여백. 프레임 모서리 장식(약 120x130, 안쪽 대각선은 모서리에서 약 100 이내)과 겹치지 않는 값이다.
        private const float Pad = 72f;
        private const float TitleY = 48f;
        private const float HeaderRowY = 56f;
        private const float CloseSize = 48f;
        private const float ColumnHeaderY = 190f;
        private const float ListY = 242f;
        private const float RowPitch = ResourceSellRowView.Columns.RowHeight + ResourceSellRowView.Columns.RowGap;
        private const float ListH = 4f * RowPitch - ResourceSellRowView.Columns.RowGap;
        private const float ToolsY = 590f;
        private const float SummaryY = 668f;
        private const float SummaryH = 124f;
        private const float SellButtonW = 280f;
        private const float SellButtonH = 88f;
        private const float MaxStep = 0.05f;
        private const float PollSeconds = 0.5f;
        private const float BackdropAlpha = 0.62f;
        private const float FrameCorner = 120f;
        private const float CapWidth = FrameCorner;
        private const float StackSpacing = 17f;

        private static float MaxGap => CardSize.x - CapWidth * 2f;

        private static readonly Color BackdropColor = new Color(0.01f, 0.015f, 0.025f, 1f);
        private static readonly Color TitleColor = new Color(0.84f, 0.98f, 1f, 1f);

        // 마스크로 잘린 패널 한 조각. 양쪽 끝(Cap)은 바깥으로 밀려나고 가운데(Interior)는 폭이 늘어난다.
        private sealed class Piece
        {
            public RectTransform Mask;
            public Image Shade;
        }

        private sealed class BurstPiece
        {
            public Image Image;
            public Vector2 Direction;
            public float Distance;
            public float Size;
        }

        private sealed class Streak
        {
            public Image Image;
            public Vector2 From;
            public Vector2 To;
            public bool Active;
        }

        private readonly List<ResourceSellRowView> rows = new List<ResourceSellRowView>();
        private readonly List<KeyValuePair<string, Vector2>> pendingOrigins = new List<KeyValuePair<string, Vector2>>();
        private readonly HashSet<string> flashIds = new HashSet<string>();

        private TMP_FontAsset font;
        private Sprite goldSprite;
        private Sprite cargoSprite;
        private Canvas canvas;
        private Image backdrop;
        private RectTransform card;
        private Image cardBlocker;
        private RectTransform shell;
        private CanvasGroup shellGroup;
        private Piece capLeft;
        private Piece capRight;
        private Piece interior;
        private CanvasGroup interiorGroup;
        private RectTransform hologramScan;
        private Image hologramScanImage;
        private Image hologramWash;
        private RectTransform whole;
        private Image beamLeft;
        private Image beamRight;
        private RectTransform contentRoot;
        private CanvasGroup contentGroup;
        private CanvasGroup headerGroup;
        private CanvasGroup listGroup;
        private CanvasGroup footerGroup;

        private TMP_Text hintText;
        private Image goldIcon;
        private Image goldIconGlow;
        private TMP_Text goldLabel;
        private TMP_Text goldValue;
        private ResourceSellButton closeButton;
        private TMP_Text cargoNumbers;
        private RectTransform cargoAfterFill;
        private RectTransform cargoRemoved;
        private RectTransform cargoRemovedLine;
        private ScrollRect scroll;
        private RectTransform listContent;
        private Scrollbar scrollbar;
        private TMP_Text emptyText;
        private ResourceSellButton selectAllButton;
        private ResourceSellButton resetButton;
        private TMP_Text noticeText;
        private TMP_Text statusText;
        private TMP_Text totalQuantityText;
        private TMP_Text totalGoldText;
        private TMP_Text bonusText;
        private TMP_Text projectionText;
        private Image summaryGlow;
        private RectTransform summaryGoldAnchor;
        private ResourceSellButton sellButton;
        private TMP_Text sellLabel;

        private RectTransform fx;
        private Image[] coins;
        private BurstPiece[] burstCoins;
        private BurstPiece[] burstSparks;
        private Image impactGlow;
        private Streak[] streaks;

        private ResourceSellSession session;
        private PopupState state;
        private float clock;
        private float pollClock;
        private bool saleActive;
        private float saleClock;
        private int countFrom;
        private int goldTarget;
        private int shownGold = int.MinValue;
        private int lastSellFrame = -1;
        private bool shellSegmented = true;

        public PopupState State => state;
        public bool IsVisible => state != PopupState.Hidden;
        public bool IsOpen => state == PopupState.Opening || state == PopupState.Open;
        public bool IsClosing => state == PopupState.Closing;
        public bool IsInteractive => contentGroup != null && contentGroup.interactable;
        public float Clock => clock;
        public Canvas Canvas => canvas;
        public ResourceSellSession Session => session;
        public RectTransform CardRect => card;
        public IReadOnlyList<ResourceSellRowView> Rows => rows;
        public int VisibleRowCount => CountActiveRows();
        public ResourceSellButton SellButton => sellButton;
        public ResourceSellButton SelectAllButton => selectAllButton;
        public ResourceSellButton ResetButton => resetButton;
        public ResourceSellButton CloseButton => closeButton;
        public ScrollRect Scroll => scroll;
        public bool ScrollbarVisible => scrollbar != null && scrollbar.gameObject.activeSelf;
        public bool EmptyVisible => emptyText != null && emptyText.gameObject.activeSelf;
        public string SellLabelString => sellLabel != null ? sellLabel.text : string.Empty;
        public string GoldValueString => goldValue != null ? goldValue.text : string.Empty;
        public string TotalQuantityString => totalQuantityText != null ? totalQuantityText.text : string.Empty;
        public string TotalGoldString => totalGoldText != null ? totalGoldText.text : string.Empty;
        public string ProjectionString => projectionText != null ? projectionText.text : string.Empty;
        public string CargoString => cargoNumbers != null ? cargoNumbers.text : string.Empty;
        public string NoticeString => noticeText != null ? noticeText.text : string.Empty;
        public string StatusString => statusText != null ? statusText.text : string.Empty;
        public string HintString => hintText != null ? hintText.text : string.Empty;
        public float ContentAlpha => contentGroup != null ? contentGroup.alpha : 0f;
        public float CardScale => card != null ? card.localScale.x : 0f;
        public bool IsShellSegmented => shellSegmented;
        public bool IsSaleEffectActive => saleActive;
        public int DisplayedGold => shownGold;
        public bool AnyEffectVisible
        {
            get
            {
                if (impactGlow != null && impactGlow.enabled)
                {
                    return true;
                }

                for (var i = 0; coins != null && i < coins.Length; i++)
                {
                    if (coins[i].enabled)
                    {
                        return true;
                    }
                }

                for (var i = 0; burstCoins != null && i < burstCoins.Length; i++)
                {
                    if (burstCoins[i].Image.enabled)
                    {
                        return true;
                    }
                }

                for (var i = 0; burstSparks != null && i < burstSparks.Length; i++)
                {
                    if (burstSparks[i].Image.enabled)
                    {
                        return true;
                    }
                }

                for (var i = 0; streaks != null && i < streaks.Length; i++)
                {
                    if (streaks[i].Image.enabled)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>X 버튼. 실제 닫기 경로(조건·후처리)는 각 화면이 정한다.</summary>
        public event Action CloseRequested;

        /// <summary>확인용: true면 Update가 시간을 진행하지 않고 테스트가 Tick으로 직접 진행한다.</summary>
        public bool ManualTick { get; set; }

        /// <summary>
        /// canvasRoot 아래에 전체 화면 루트(자체 Canvas)를 만들고 창을 짓는다.
        /// blockBackground가 true면 뒤쪽 클릭을 막고, false면 창 영역만 입력을 받는다.
        /// </summary>
        public static ResourceSellPopupView Create(Transform canvasRoot, bool blockBackground)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var root = new GameObject(
                "ResourceSellPopup",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Canvas),
                typeof(GraphicRaycaster));
            root.layer = canvasRoot.gameObject.layer;
            root.transform.SetParent(canvasRoot, false);
            ResourceSellUi.Stretch((RectTransform)root.transform);
            var popupCanvas = root.GetComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 32_100;
            var dim = root.GetComponent<Image>();
            dim.raycastTarget = blockBackground;

            var view = root.AddComponent<ResourceSellPopupView>();
            view.Build();
            return view;
        }

        public void Attach(ResourceSellSession target)
        {
            if (session == target)
            {
                return;
            }

            if (session != null)
            {
                session.Changed -= Render;
            }

            session = target;
            if (session != null)
            {
                session.Changed += Render;
            }

            if (session == null)
            {
                HideImmediate();
            }
        }

        /// <summary>
        /// 연다. 숨김·닫는 중이면 최신 보유량·가격을 읽고 수량 0으로 등장 연출을 처음부터 재생한다.
        /// 이미 열려 있으면 아무것도 하지 않는다(스냅샷마다 불려도 안전).
        /// </summary>
        public bool Show()
        {
            if (state == PopupState.Opening || state == PopupState.Open)
            {
                return true;
            }

            ClearSaleEffect();
            SetStatus(string.Empty, ResourceSellUi.TextMuted);
            gameObject.SetActive(true);
            state = PopupState.Opening;
            clock = 0f;
            pollClock = 0f;
            if (scroll != null)
            {
                scroll.verticalNormalizedPosition = 1f;
            }

            if (session != null)
            {
                session.Open();
            }
            else
            {
                Render();
            }

            SnapGold();
            for (var i = 0; i < rows.Count; i++)
            {
                rows[i].SnapActive();
            }

            transform.SetAsLastSibling();
            PopupWindowSorting.BringToFront(canvas);
            ApplyOpen(0f);
            return true;
        }

        /// <summary>아코디언 종료 연출 뒤 숨긴다. 남은 금화·빛·입력 차단은 끝날 때 모두 정리된다.</summary>
        public bool BeginClose()
        {
            if (state == PopupState.Hidden || state == PopupState.Closing)
            {
                return false;
            }

            ClearSaleEffect();
            SnapGold();
            state = PopupState.Closing;
            clock = 0f;
            if (!isActiveAndEnabled)
            {
                FinishClose();
                return true;
            }

            ApplyClose(0f);
            return true;
        }

        public void HideImmediate()
        {
            if (state == PopupState.Hidden && !gameObject.activeSelf)
            {
                return;
            }

            FinishClose();
        }

        private void Update()
        {
            if (ManualTick || state == PopupState.Hidden)
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
                    if (clock >= ResourceSellTimeline.OpenDuration)
                    {
                        state = PopupState.Open;
                        clock = 0f;
                        ApplyOpen(ResourceSellTimeline.OpenDuration);
                        SettleShell();
                    }
                    else
                    {
                        ApplyOpen(clock);
                    }

                    break;
                case PopupState.Open:
                    pollClock += dt;
                    if (pollClock >= PollSeconds)
                    {
                        // 이벤트가 없는 변화(다른 경로의 골드 변화 등)도 놓치지 않는 안전망. 값이 같으면 다시 그리지 않는다.
                        pollClock = 0f;
                        if (session != null)
                        {
                            session.Refresh();
                        }
                    }

                    break;
                case PopupState.Closing:
                    clock += dt;
                    if (clock >= ResourceSellTimeline.CloseDuration)
                    {
                        FinishClose();
                        return;
                    }

                    ApplyClose(clock);
                    break;
            }

            if (saleActive)
            {
                saleClock += dt;
                ApplySale(saleClock);
            }
        }

        private void OnDisable()
        {
            // 씬 전환·부모 비활성화로 꺼져도 다음 표시가 깨끗하게 시작되도록 되돌린다.
            if (state != PopupState.Hidden)
            {
                state = PopupState.Hidden;
            }

            ClearSaleEffect();
            HideIntroEffects();
            PopupWindowSorting.Remove(canvas);
        }

        private void OnDestroy()
        {
            if (session != null)
            {
                session.Changed -= Render;
                session = null;
            }
        }

        private void FinishClose()
        {
            state = PopupState.Hidden;
            clock = 0f;
            ClearSaleEffect();
            HideIntroEffects();
            PopupWindowSorting.Remove(canvas);
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        // ---------------- 입력 ----------------

        private void OnAdjust(string itemId, int delta)
        {
            if (!CanAcceptInput())
            {
                return;
            }

            SetStatus(string.Empty, ResourceSellUi.TextMuted);
            session.Adjust(itemId, delta);
        }

        private void OnMax(string itemId)
        {
            if (!CanAcceptInput())
            {
                return;
            }

            SetStatus(string.Empty, ResourceSellUi.TextMuted);
            session.SetMax(itemId);
        }

        private void OnSelectAll()
        {
            if (!CanAcceptInput())
            {
                return;
            }

            SetStatus(string.Empty, ResourceSellUi.TextMuted);
            session.SelectAllBulk();
        }

        private void OnReset()
        {
            if (!CanAcceptInput())
            {
                return;
            }

            SetStatus(string.Empty, ResourceSellUi.TextMuted);
            session.ResetSelection();
        }

        /// <summary>판매 버튼. 같은 프레임 중복 입력과 거래 중 재진입을 막는다.</summary>
        public ResourceSellConfirmOutcome RequestSell()
        {
            if (!CanAcceptInput() || session.IsCommitting)
            {
                return new ResourceSellConfirmOutcome(ResourceSellConfirmStatus.Busy, string.Empty);
            }

            if (lastSellFrame == Time.frameCount && Application.isPlaying)
            {
                return new ResourceSellConfirmOutcome(ResourceSellConfirmStatus.Busy, string.Empty);
            }

            lastSellFrame = Time.frameCount;
            CaptureOrigins();
            var outcome = session.Confirm();
            switch (outcome.Status)
            {
                case ResourceSellConfirmStatus.Sold:
                    SetStatus(outcome.Message, ResourceSellUi.Ok);
                    StartSaleEffect(outcome);
                    break;
                case ResourceSellConfirmStatus.Adjusted:
                    SetStatus(outcome.Message, ResourceSellUi.Gold);
                    break;
                case ResourceSellConfirmStatus.Failed:
                    SetStatus(outcome.Message, ResourceSellUi.Error);
                    break;
                case ResourceSellConfirmStatus.Empty:
                    SetStatus(outcome.Message, ResourceSellUi.TextMuted);
                    break;
            }

            return outcome;
        }

        private bool CanAcceptInput()
        {
            return session != null && IsInteractive;
        }

        // ---------------- 표시 ----------------

        private void Render()
        {
            if (card == null)
            {
                return;
            }

            var snapshot = session != null ? session.Snapshot : ResourceSellSnapshot.Empty;
            var quote = session != null ? session.Quote : default;
            hintText.text = session != null ? session.Hint : string.Empty;

            EnsureRows(snapshot.Lines.Count);
            var interactable = session != null && !session.IsCommitting;
            for (var i = 0; i < snapshot.Lines.Count; i++)
            {
                var line = snapshot.Lines[i];
                rows[i].Bind(line, session != null ? session.GetQuantity(line.ItemId) : 0, interactable);
            }

            var count = snapshot.Lines.Count;
            var listHeight = count * RowPitch - ResourceSellRowView.Columns.RowGap;
            listContent.sizeDelta = new Vector2(listContent.sizeDelta.x, Mathf.Max(0f, listHeight));
            var overflow = listHeight > ListH + 0.5f;
            scroll.vertical = overflow;
            if (scrollbar.gameObject.activeSelf != overflow)
            {
                scrollbar.gameObject.SetActive(overflow);
            }

            if (!overflow)
            {
                listContent.anchoredPosition = Vector2.zero;
            }

            emptyText.gameObject.SetActive(count == 0);

            goldTarget = snapshot.Gold;
            if (!saleActive)
            {
                SetGoldDisplay(goldTarget);
            }

            ApplyCargo(quote, snapshot);

            totalQuantityText.text = ResourceSellUi.Number(quote.TotalQuantity) + "개";
            totalGoldText.text = "+" + ResourceSellUi.GoldText(quote.TotalGold);
            bonusText.text = quote.BonusGold > 0
                ? "기본 " + ResourceSellUi.GoldText(quote.BaseGold) + " + 보너스 " + ResourceSellUi.GoldText(quote.BonusGold)
                : string.Empty;
            projectionText.text = "<color=#E6F7FF>" + ResourceSellUi.Number(snapshot.Gold) + "</color>  →  <color=#FFCC4D>"
                + ResourceSellUi.GoldText(quote.GoldAfter) + "</color>";
            sellLabel.text = quote.TotalGold > 0 || quote.TotalQuantity > 0
                ? SellLabel + " · +" + ResourceSellUi.GoldText(quote.TotalGold)
                : SellLabel;

            sellButton.Button.interactable = interactable && quote.CanSell;
            selectAllButton.Button.interactable = interactable && session.HasBulkTargets;
            resetButton.Button.interactable = interactable && !session.Selection.IsEmpty;
            noticeText.text = session != null ? session.BulkRuleNotice : string.Empty;
        }

        private void ApplyCargo(ResourceSellQuote quote, ResourceSellSnapshot snapshot)
        {
            var capacity = snapshot.CargoCapacity;
            var current = snapshot.CargoWeight;
            var after = quote.TotalQuantity > 0 ? quote.CargoAfter : current;
            var currentFraction = capacity > 0f ? Mathf.Clamp01(current / capacity) : 0f;
            var afterFraction = capacity > 0f ? Mathf.Clamp01(after / capacity) : 0f;
            SetHorizontal(cargoAfterFill, 0f, afterFraction);
            SetHorizontal(cargoRemoved, afterFraction, currentFraction);
            SetHorizontal(cargoRemovedLine, afterFraction, currentFraction);
            var removing = currentFraction - afterFraction > 0.0001f;
            cargoRemoved.gameObject.SetActive(removing);
            cargoRemovedLine.gameObject.SetActive(removing);
            cargoNumbers.text = removing
                ? ResourceSellUi.Cargo(current) + " → " + ResourceSellUi.Cargo(after) + " / " + ResourceSellUi.Cargo(capacity)
                : ResourceSellUi.Cargo(current) + " / " + ResourceSellUi.Cargo(capacity);
        }

        private static void SetHorizontal(RectTransform rect, float from, float to)
        {
            rect.anchorMin = new Vector2(from, 0f);
            rect.anchorMax = new Vector2(Mathf.Max(from, to), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void SetGoldDisplay(int value)
        {
            if (value == shownGold)
            {
                return;
            }

            shownGold = value;
            goldValue.text = ResourceSellUi.GoldText(value);
            // 숫자 폭에 맞춰 '골드' 라벨과 아이콘을 왼쪽으로 붙인다(오른쪽 끝은 X 버튼에서 28 떨어진 곳에 고정).
            var width = Mathf.Max(24f, goldValue.GetPreferredValues(goldValue.text).x + 4f);
            var right = CardSize.x - Pad - CloseSize - 28f;
            Place(goldValue.rectTransform, right - width, HeaderRowY, width, 48f);
            Place(goldLabel.rectTransform, right - width - 58f, HeaderRowY, 52f, 48f);
            var iconX = right - width - 104f;
            Place(goldIcon.rectTransform, iconX, HeaderRowY + 4f, 40f, 40f);
            Place(goldIconGlow.rectTransform, iconX - 20f, HeaderRowY - 16f, 80f, 80f);
        }

        private void SnapGold()
        {
            if (session != null)
            {
                goldTarget = session.Snapshot.Gold;
            }

            SetGoldDisplay(goldTarget);
        }

        private void SetStatus(string message, Color color)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message ?? string.Empty;
            statusText.color = color;
        }

        private void EnsureRows(int count)
        {
            while (rows.Count < count)
            {
                var row = ResourceSellRowView.Create(listContent, font, goldSprite);
                row.AdjustRequested += OnAdjust;
                row.MaxRequested += OnMax;
                rows.Add(row);
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var visible = i < count;
                if (rows[i].gameObject.activeSelf != visible)
                {
                    rows[i].gameObject.SetActive(visible);
                }

                if (visible)
                {
                    var rect = (RectTransform)rows[i].transform;
                    rect.anchoredPosition = new Vector2(
                        ResourceSellRowView.Columns.RowWidth * 0.5f,
                        -(i * RowPitch + ResourceSellRowView.Columns.RowHeight * 0.5f));
                }
            }
        }

        private int CountActiveRows()
        {
            var count = 0;
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].gameObject.activeSelf)
                {
                    count++;
                }
            }

            return count;
        }

        // ---------------- 연출 ----------------

        private void ApplyOpen(float t)
        {
            backdrop.color = ResourceSellUi.WithAlpha(BackdropColor, BackdropAlpha * ResourceSellTimeline.OpenBackdrop(t));
            var scale = ResourceSellTimeline.OpenCardScale(t);
            card.localScale = new Vector3(scale, scale, 1f);
            shellGroup.alpha = ResourceSellTimeline.OpenShellAlpha(t);

            var settled = t >= ResourceSellTimeline.OpenDuration;
            SetSegmented(!settled);
            if (!settled)
            {
                ApplyShell(
                    ResourceSellTimeline.Gap(t),
                    ResourceSellTimeline.InteriorHeight(t),
                    ResourceSellTimeline.OpenBrightness(t),
                    ResourceSellTimeline.EdgeGlow(t),
                    ResourceSellTimeline.HologramAmount(t),
                    ResourceSellTimeline.HologramSolid(t),
                    t);
            }

            var header = ResourceSellTimeline.Reveal(t, ResourceSellTimeline.HeaderReveal);
            var list = ResourceSellTimeline.Reveal(t, ResourceSellTimeline.ListReveal);
            var footer = ResourceSellTimeline.Reveal(t, ResourceSellTimeline.FooterReveal);
            SetGroup(headerGroup, header);
            SetGroup(listGroup, list);
            SetGroup(footerGroup, footer);
            for (var i = 0; i < rows.Count; i++)
            {
                rows[i].Group.alpha = ResourceSellTimeline.RowReveal(t, i);
            }

            contentGroup.alpha = 1f;
            var interactive = settled || t >= ResourceSellTimeline.FooterReveal;
            contentGroup.interactable = interactive;
            contentGroup.blocksRaycasts = true;
            cardBlocker.raycastTarget = true;
            goldIcon.color = Color.white;
            ApplyIntroEffects(t);
        }

        private void ApplyClose(float t)
        {
            SetSegmented(true);
            backdrop.color = ResourceSellUi.WithAlpha(BackdropColor, BackdropAlpha * ResourceSellTimeline.CloseBackdrop(t));
            var scale = ResourceSellTimeline.CloseCardScale(t);
            card.localScale = new Vector3(scale, scale, 1f);
            shellGroup.alpha = ResourceSellTimeline.CloseShellAlpha(t);
            var fold = ResourceSellTimeline.CloseGap(t);
            ApplyShell(
                fold,
                ResourceSellTimeline.CloseInteriorHeight(t),
                ResourceSellTimeline.CloseBrightness(t),
                ResourceSellTimeline.CloseEdgeGlow(t),
                1f - fold,
                fold,
                t);
            contentGroup.alpha = ResourceSellTimeline.CloseContentAlpha(t);
            contentGroup.interactable = false;
            contentGroup.blocksRaycasts = false;
            cardBlocker.raycastTarget = false;
            HideIntroEffects();
            goldIcon.color = Color.white;
        }

        /// <summary>
        /// 껍데기 한 장면. gap 0이면 양쪽 끝이 맞닿은 닫힌 창, 1이면 최종 폭이다.
        /// 양쪽 끝은 같은 속도로 좌우로 밀려나고, 그 사이(Interior)는 같은 자리에 고정된 홀로그램 패널이
        /// 가운데에서부터 폭·높이가 늘어나며 드러난다.
        /// </summary>
        private void ApplyShell(float gap, float heightFraction, float brightness, float glow, float hologram, float solid, float time)
        {
            var width = Mathf.Clamp01(gap) * MaxGap;
            var half = width * 0.5f;
            var capCenter = half + CapWidth * 0.5f;
            capLeft.Mask.anchoredPosition = new Vector2(-capCenter, 0f);
            capRight.Mask.anchoredPosition = new Vector2(capCenter, 0f);

            var showInterior = width > 0.5f;
            if (interior.Mask.gameObject.activeSelf != showInterior)
            {
                interior.Mask.gameObject.SetActive(showInterior);
            }

            interior.Mask.sizeDelta = new Vector2(width, CardSize.y * Mathf.Clamp01(heightFraction));
            interiorGroup.alpha = Mathf.Lerp(0.4f, 1f, solid);

            // 멀리 있을수록 어둡다. 가운데 조각은 홀로그램 줄무늬·번쩍임이 더해진다.
            var dark = ResourceSellUi.WithAlpha(Color.black, Mathf.Clamp01((1f - brightness) * 0.8f));
            capLeft.Shade.color = dark;
            capRight.Shade.color = dark;
            interior.Shade.color = dark;
            var flicker = 0.78f + 0.22f * Mathf.Sin(time * 53f) * Mathf.Sin(time * 17f + 1f);
            hologramScanImage.color = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.16f * hologram * flicker);
            hologramWash.color = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.08f * hologram * flicker);
            hologramScan.anchoredPosition = new Vector2(0f, (time * 70f) % 6f);

            PlaceBeam(beamLeft, -half, glow);
            PlaceBeam(beamRight, half, glow);
        }

        private static void PlaceBeam(Image beam, float x, float alpha)
        {
            beam.rectTransform.anchoredPosition = new Vector2(x, 0f);
            beam.color = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, Mathf.Clamp01(alpha));
            beam.enabled = alpha > 0.001f;
        }

        /// <summary>정착: 마스크 조각을 걷고 한 장짜리 패널만 남긴다.</summary>
        private void SettleShell()
        {
            SetSegmented(false);
            card.localScale = Vector3.one;
            shellGroup.alpha = 1f;
            SetGroup(headerGroup, 1f);
            SetGroup(listGroup, 1f);
            SetGroup(footerGroup, 1f);
            for (var i = 0; i < rows.Count; i++)
            {
                rows[i].Group.alpha = 1f;
            }

            contentGroup.alpha = 1f;
            contentGroup.interactable = true;
            HideIntroEffects();
            goldIcon.color = Color.white;
            goldIconGlow.color = Color.clear;
        }

        private void SetSegmented(bool segmented)
        {
            if (shellSegmented == segmented)
            {
                return;
            }

            shellSegmented = segmented;
            capLeft.Mask.gameObject.SetActive(segmented);
            capRight.Mask.gameObject.SetActive(segmented);
            interior.Mask.gameObject.SetActive(segmented);
            whole.gameObject.SetActive(!segmented);
            if (!segmented)
            {
                beamLeft.enabled = false;
                beamRight.enabled = false;
            }
        }

        private static void SetGroup(CanvasGroup group, float reveal)
        {
            group.alpha = reveal;
            var rect = (RectTransform)group.transform;
            rect.anchoredPosition = new Vector2(0f, -10f * (1f - reveal));
        }

        // 도는 금화가 하나씩 쌓이고, 닫힌 창이 다가와 부딪히면 금화가 터지며 사라진다.
        // 표시 연출일 뿐 골드 값은 바꾸지 않는다.
        private void ApplyIntroEffects(float t)
        {
            var windup = ResourceSellTimeline.ImpactWindupAmount(t);
            var stack = ResourceSellTimeline.StackAlpha(t);
            for (var i = 0; i < coins.Length; i++)
            {
                var drop = ResourceSellTimeline.CoinDropProgress(t, i);
                var rest = new Vector2(0f, (i - (coins.Length - 1) * 0.5f) * StackSpacing);
                var size = Mathf.Lerp(0.7f, 1f, drop) * (1f + 0.18f * windup);
                coins[i].rectTransform.anchoredPosition = rest + new Vector2(0f, 150f * (1f - drop));
                coins[i].rectTransform.localScale = new Vector3(size * ResourceSellTimeline.CoinSpinWidth(t, i), size, 1f);
                var alpha = Mathf.Clamp01(drop * 2f) * stack;
                coins[i].color = ResourceSellUi.WithAlpha(Color.white, alpha);
                coins[i].enabled = alpha > 0.001f;
            }

            var flash = ResourceSellTimeline.ImpactFlash(t);
            var progress = ResourceSellTimeline.BurstProgress(t);
            impactGlow.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(200f, 460f, progress);
            impactGlow.color = ResourceSellUi.WithAlpha(ResourceSellUi.GoldGlow, 0.7f * flash);
            impactGlow.enabled = flash > 0.001f;

            var active = ResourceSellTimeline.BurstActive(t);
            var burstAlpha = ResourceSellTimeline.BurstAlpha(t);
            for (var i = 0; i < burstCoins.Length; i++)
            {
                var piece = burstCoins[i];
                var shrink = Mathf.Lerp(1f, 0.45f, progress);
                piece.Image.rectTransform.anchoredPosition =
                    piece.Direction * (piece.Distance * progress) + new Vector2(0f, -46f * progress * progress);
                piece.Image.rectTransform.localScale =
                    new Vector3(shrink * ResourceSellTimeline.CoinSpinWidth(t, i), shrink, 1f);
                piece.Image.color = ResourceSellUi.WithAlpha(Color.white, burstAlpha);
                piece.Image.enabled = active && burstAlpha > 0.001f;
            }

            for (var i = 0; i < burstSparks.Length; i++)
            {
                var spark = burstSparks[i];
                var radius = Mathf.Lerp(26f, spark.Distance, progress);
                spark.Image.rectTransform.anchoredPosition = spark.Direction * radius;
                spark.Image.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(spark.Size, 12f, progress), 8f);
                spark.Image.color = ResourceSellUi.WithAlpha(Color.Lerp(Color.white, ResourceSellUi.Gold, progress), 0.9f * burstAlpha);
                spark.Image.enabled = active && burstAlpha > 0.001f;
            }
        }

        private void HideIntroEffects()
        {
            for (var i = 0; coins != null && i < coins.Length; i++)
            {
                coins[i].enabled = false;
            }

            for (var i = 0; burstCoins != null && i < burstCoins.Length; i++)
            {
                burstCoins[i].Image.enabled = false;
            }

            for (var i = 0; burstSparks != null && i < burstSparks.Length; i++)
            {
                burstSparks[i].Image.enabled = false;
            }

            if (impactGlow != null)
            {
                impactGlow.enabled = false;
            }
        }

        private void CaptureOrigins()
        {
            pendingOrigins.Clear();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.gameObject.activeSelf && row.Quantity > 0 && IsInsideViewport(row))
                {
                    pendingOrigins.Add(new KeyValuePair<string, Vector2>(row.ItemId, FxPoint(row.GoldAnchor)));
                }
            }
        }

        private bool IsInsideViewport(ResourceSellRowView row)
        {
            var viewport = scroll.viewport;
            var local = viewport.InverseTransformPoint(row.GoldAnchor.position);
            return viewport.rect.Contains(local);
        }

        private void StartSaleEffect(ResourceSellConfirmOutcome outcome)
        {
            ClearSaleEffect();
            saleActive = true;
            saleClock = 0f;
            countFrom = outcome.GoldBefore;
            goldTarget = outcome.GoldAfter;
            flashIds.Clear();
            var destination = FxPoint(summaryGoldAnchor);
            var used = 0;
            for (var i = 0; i < pendingOrigins.Count && used < streaks.Length; i++)
            {
                var id = pendingOrigins[i].Key;
                if (!Contains(outcome.SoldItemIds, id))
                {
                    continue;
                }

                flashIds.Add(id);
                var streak = streaks[used++];
                streak.From = pendingOrigins[i].Value;
                streak.To = destination;
                streak.Active = true;
            }

            SetGoldDisplay(countFrom);
            ApplySale(0f);
        }

        private void ApplySale(float t)
        {
            for (var i = 0; i < streaks.Length; i++)
            {
                var streak = streaks[i];
                if (!streak.Active)
                {
                    streak.Image.enabled = false;
                    continue;
                }

                var p = ResourceSellTimeline.StreakProgress(t, i);
                var alpha = ResourceSellTimeline.StreakAlpha(t, i);
                var position = Vector2.Lerp(streak.From, streak.To, p);
                var direction = streak.To - streak.From;
                streak.Image.rectTransform.anchoredPosition = position;
                streak.Image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                streak.Image.color = ResourceSellUi.WithAlpha(Color.Lerp(ResourceSellUi.Teal, ResourceSellUi.Gold, p), 0.85f * alpha);
                streak.Image.enabled = alpha > 0.001f;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                rows[i].SetFlash(flashIds.Contains(rows[i].ItemId) ? ResourceSellTimeline.RowFlash(t) : 0f);
            }

            summaryGlow.color = ResourceSellUi.WithAlpha(ResourceSellUi.GoldGlow, 0.32f * ResourceSellTimeline.SummaryGlow(t));
            goldIconGlow.color = ResourceSellUi.WithAlpha(ResourceSellUi.GoldGlow, 0.6f * ResourceSellTimeline.TopGoldGlow(t));
            var count = ResourceSellTimeline.CountProgress(t);
            SetGoldDisplay(Mathf.RoundToInt(Mathf.Lerp(countFrom, goldTarget, count)));

            if (t >= ResourceSellTimeline.SaleDuration)
            {
                ClearSaleEffect();
            }
        }

        /// <summary>판매 연출을 즉시 끝내고 최신 골드 값을 보인다(연속 거래·닫기).</summary>
        private void ClearSaleEffect()
        {
            saleActive = false;
            saleClock = 0f;
            flashIds.Clear();
            for (var i = 0; streaks != null && i < streaks.Length; i++)
            {
                streaks[i].Active = false;
                streaks[i].Image.enabled = false;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                rows[i].SetFlash(0f);
            }

            if (summaryGlow != null)
            {
                summaryGlow.color = Color.clear;
                goldIconGlow.color = Color.clear;
                SetGoldDisplay(goldTarget);
            }
        }

        private Vector2 FxPoint(RectTransform target)
        {
            var world = target.TransformPoint(target.rect.center);
            return fx.InverseTransformPoint(world);
        }

        private static bool Contains(IReadOnlyList<string> ids, string id)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h * 0.5f));
            rect.sizeDelta = new Vector2(w, h);
        }

        // ---------------- 생성 ----------------

        private void Build()
        {
            var popupSkin = Resources.Load<MineResetTimedPopupSkin>(MineResetTimedPopupSkin.ResourcePath);
            var iconSkin = Resources.Load<ResourceSellSkin>(ResourceSellSkin.ResourcePath);
            font = popupSkin != null ? popupSkin.font : null;
            if (font == null)
            {
                font = TMP_Settings.defaultFontAsset;
            }

            goldSprite = iconSkin != null && iconSkin.goldIcon != null ? iconSkin.goldIcon : ResourceSellArt.Coin();
            cargoSprite = iconSkin != null ? iconSkin.cargoIcon : null;

            canvas = GetComponent<Canvas>();
            backdrop = GetComponent<Image>();
            backdrop.color = ResourceSellUi.WithAlpha(BackdropColor, 0f);
            backdrop.canvasRenderer.cullTransparentMesh = false;

            card = ResourceSellUi.Centered(transform, "Card", Vector2.zero, CardSize);
            cardBlocker = ResourceSellUi.Image(card, "InputBlocker", null, Color.clear);
            cardBlocker.raycastTarget = true;

            BuildShell(popupSkin);
            BuildContent(popupSkin);
            BuildFx();

            gameObject.SetActive(false);
        }

        private void BuildShell(MineResetTimedPopupSkin skin)
        {
            shell = ResourceSellUi.Centered(card, "Shell", Vector2.zero, CardSize);
            shellGroup = shell.gameObject.AddComponent<CanvasGroup>();
            shellGroup.interactable = false;
            shellGroup.blocksRaycasts = false;

            // 양쪽 끝: 프레임 모서리 장식이 든 왼쪽·오른쪽 끝 조각. 처음엔 가운데에서 맞닿아 있다.
            var capOffset = CardSize.x * 0.5f - CapWidth * 0.5f;
            capLeft = BuildCap("CapLeft", skin, capOffset);
            capRight = BuildCap("CapRight", skin, -capOffset);

            // 가운데: 같은 자리에 고정된 홀로그램 패널. 마스크 폭이 늘어나는 만큼 드러난다.
            var mask = ResourceSellUi.Centered(shell, "Interior", Vector2.zero, new Vector2(0f, CardSize.y));
            mask.gameObject.AddComponent<RectMask2D>();
            var inner = ResourceSellUi.Centered(mask, "Inner", Vector2.zero, CardSize);
            interiorGroup = inner.gameObject.AddComponent<CanvasGroup>();
            BuildPanelLayers(inner, skin);
            hologramWash = ResourceSellUi.Image(mask, "HologramWash", null, Color.clear);
            hologramScan = ResourceSellUi.Centered(mask, "HologramScan", Vector2.zero, CardSize + new Vector2(0f, 12f));
            hologramScanImage = ResourceSellUi.AddImage(hologramScan, ResourceSellArt.ScanTile(), Color.clear);
            hologramScanImage.type = Image.Type.Tiled;
            interior = new Piece { Mask = mask, Shade = ResourceSellUi.Image(mask, "Shade", null, Color.clear) };
            mask.gameObject.SetActive(false);

            whole = ResourceSellUi.Centered(shell, "Whole", Vector2.zero, CardSize);
            BuildPanelLayers(whole, skin);
            whole.gameObject.SetActive(false);

            beamLeft = ResourceSellUi.AddImage(
                ResourceSellUi.Centered(shell, "BeamLeft", Vector2.zero, new Vector2(14f, CardSize.y * 0.96f)),
                ResourceSellArt.SoftRect(), Color.clear);
            beamRight = ResourceSellUi.AddImage(
                ResourceSellUi.Centered(shell, "BeamRight", Vector2.zero, new Vector2(14f, CardSize.y * 0.96f)),
                ResourceSellArt.SoftRect(), Color.clear);
            beamLeft.enabled = false;
            beamRight.enabled = false;
            shellSegmented = true;
        }

        // innerOffset: 조각 안에서 전체 패널을 어디에 놓아야 최종 위치와 같은 그림이 보이는지(조각 중심 기준).
        private Piece BuildCap(string name, MineResetTimedPopupSkin skin, float innerOffset)
        {
            var mask = ResourceSellUi.Centered(shell, name, Vector2.zero, new Vector2(CapWidth, CardSize.y));
            mask.gameObject.AddComponent<RectMask2D>();
            var inner = ResourceSellUi.Centered(mask, "Inner", new Vector2(innerOffset, 0f), CardSize);
            BuildPanelLayers(inner, skin);
            var shade = ResourceSellUi.Image(mask, "Shade", null, Color.clear);
            return new Piece { Mask = mask, Shade = shade };
        }

        private static void BuildPanelLayers(RectTransform parent, MineResetTimedPopupSkin skin)
        {
            BuildPanelLayers(parent, skin, CardSize);
        }

        // 어두운 남색 패널 + 절제된 육각형 무늬 + 위쪽 옅은 청록 번짐 + 각진 금속 모서리 프레임.
        // 보관함 팝업(B-138)도 같은 껍데기를 쓴다.
        internal static void BuildPanelLayers(RectTransform parent, MineResetTimedPopupSkin skin, Vector2 cardSize)
        {
            var inset = ResourceSellUi.Image(parent, "PanelBase", null, ResourceSellUi.Card);
            inset.rectTransform.offsetMin = new Vector2(10f, 10f);
            inset.rectTransform.offsetMax = new Vector2(-10f, -10f);
            if (skin != null && skin.panel != null)
            {
                var panel = ResourceSellUi.Image(parent, "Panel", skin.panel, ResourceSellUi.Card);
                panel.rectTransform.offsetMin = new Vector2(10f, 10f);
                panel.rectTransform.offsetMax = new Vector2(-10f, -10f);
            }

            var hex = ResourceSellUi.Image(parent, "Hex", ResourceSellArt.HexTile(),
                ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.045f));
            hex.type = Image.Type.Tiled;
            hex.rectTransform.offsetMin = new Vector2(24f, 24f);
            hex.rectTransform.offsetMax = new Vector2(-24f, -24f);

            var wash = ResourceSellUi.AddImage(
                ResourceSellUi.Centered(parent, "TopWash", new Vector2(0f, cardSize.y * 0.5f - 120f), new Vector2(cardSize.x * 0.8f, 240f)),
                ResourceSellArt.Soft(), ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.06f));
            wash.raycastTarget = false;

            var frameSprite = skin != null ? ResourceSellArt.Sliced(skin.frame, 0.135f, 0.19f) : null;
            if (frameSprite != null)
            {
                var frame = ResourceSellUi.Image(parent, "Frame", frameSprite, Color.white);
                frame.type = Image.Type.Sliced;
                frame.pixelsPerUnitMultiplier = frameSprite.border.x * 100f / (frameSprite.pixelsPerUnit * FrameCorner);
            }
            else
            {
                var outline = ResourceSellUi.Image(parent, "FrameFallback", ResourceSellArt.ChamferOutline(),
                    ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.85f));
                outline.type = Image.Type.Sliced;
            }
        }

        private void BuildContent(MineResetTimedPopupSkin skin)
        {
            contentRoot = ResourceSellUi.Centered(card, "Content", Vector2.zero, CardSize);
            contentGroup = contentRoot.gameObject.AddComponent<CanvasGroup>();
            headerGroup = Group("Header");
            listGroup = Group("List");
            footerGroup = Group("Footer");
            var header = headerGroup.transform;
            var list = listGroup.transform;
            var footer = footerGroup.transform;

            // 상단: 제목·안내 / 골드·X / 화물 게이지.
            ResourceSellUi.AddImage(ResourceSellUi.Place(header, "TitleAccent", Pad, TitleY + 14f, 4f, 36f), null,
                ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.9f));
            var title = ResourceSellUi.Label(header, "Title", Pad + 18f, TitleY, 420f, 64f, font, 40f, FontStyles.Bold,
                TitleColor, TextAlignmentOptions.Left);
            title.text = Title;
            title.characterSpacing = 3f;
            if (skin != null && skin.titleDivider != null)
            {
                var divider = ResourceSellUi.AddImage(ResourceSellUi.Place(header, "TitleDivider", Pad, TitleY + 68f, 560f, 14f),
                    skin.titleDivider, ResourceSellUi.WithAlpha(Color.white, 0.75f));
                divider.preserveAspect = false;
            }

            hintText = ResourceSellUi.Label(header, "Hint", Pad + 18f, TitleY + 90f, 620f, 30f, font, 20f, FontStyles.Normal,
                ResourceSellUi.TextMuted, TextAlignmentOptions.Left);

            goldIconGlow = ResourceSellUi.AddImage(ResourceSellUi.Place(header, "GoldIconGlow", 0f, 0f, 80f, 80f),
                ResourceSellArt.Soft(), Color.clear);
            goldIcon = ResourceSellUi.AddImage(ResourceSellUi.Place(header, "GoldIcon", 0f, 0f, 40f, 40f), goldSprite, Color.white);
            goldIcon.preserveAspect = true;
            goldLabel = ResourceSellUi.Label(header, "GoldLabel", 0f, 0f, 52f, 48f, font, 20f, FontStyles.Normal,
                ResourceSellUi.TextMain, TextAlignmentOptions.Center);
            goldLabel.text = "골드";
            goldValue = ResourceSellUi.Label(header, "GoldValue", 0f, 0f, 120f, 48f, font, 30f, FontStyles.Bold,
                ResourceSellUi.Gold, TextAlignmentOptions.Right);
            goldValue.overflowMode = TextOverflowModes.Overflow;
            closeButton = ResourceSellButton.Create(header, "CloseButton", CardSize.x - Pad - CloseSize, HeaderRowY, CloseSize, CloseSize, font,
                null, 20f, ResourceSellButton.Variant.Close, ResourceSellArt.Cross(), 22f);
            closeButton.Button.onClick.AddListener(() => CloseRequested?.Invoke());

            BuildCargo(header);

            // 열 머리글(고정).
            var headerStrip = ResourceSellUi.Place(list, "ColumnHeader", Pad, ColumnHeaderY, ResourceSellRowView.Columns.RowWidth, 40f);
            ResourceSellUi.Image(headerStrip, "Strip", ResourceSellArt.ChamferFill(), ResourceSellUi.Strip);
            ResourceSellUi.Image(headerStrip, "StripLine", ResourceSellArt.ChamferOutline(),
                ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.18f));
            ColumnLabel(headerStrip, "자원", ResourceSellRowView.Columns.IconX + 8f, 200f, TextAlignmentOptions.Left);
            ColumnLabel(headerStrip, "보유", ResourceSellRowView.Columns.OwnedX, ResourceSellRowView.Columns.OwnedW,
                TextAlignmentOptions.Center);
            ColumnLabel(headerStrip, "개당 가격", ResourceSellRowView.Columns.PriceX, ResourceSellRowView.Columns.PriceW,
                TextAlignmentOptions.Center);
            ColumnLabel(headerStrip, "판매 수량", ResourceSellRowView.Columns.ControlsX, ResourceSellRowView.Columns.ControlsW,
                TextAlignmentOptions.Center);
            ColumnLabel(headerStrip, "받을 골드", ResourceSellRowView.Columns.GoldX, ResourceSellRowView.Columns.GoldW,
                TextAlignmentOptions.Right);

            BuildList(list);

            // 보조 버튼·규칙 안내·결과 문구.
            selectAllButton = ResourceSellButton.Create(footer, "SelectAllButton", Pad, ToolsY, 200f, 50f, font,
                SelectAllLabel, 20f, ResourceSellButton.Variant.Normal, ResourceSellArt.FillDown(), 20f);
            resetButton = ResourceSellButton.Create(footer, "ResetButton", Pad + 216f, ToolsY, 214f, 50f, font,
                ResetLabel, 20f, ResourceSellButton.Variant.Normal, ResourceSellArt.ResetArc(), 20f);
            selectAllButton.Button.onClick.AddListener(OnSelectAll);
            resetButton.Button.onClick.AddListener(OnReset);
            noticeText = ResourceSellUi.Label(footer, "BulkNotice", 560f, ToolsY - 2f, CardSize.x - Pad - 560f, 26f, font, 16f,
                FontStyles.Normal, ResourceSellUi.TextMuted, TextAlignmentOptions.Right);
            statusText = ResourceSellUi.Label(footer, "Status", 560f, ToolsY + 24f, CardSize.x - Pad - 560f, 28f, font, 18f,
                FontStyles.Normal, ResourceSellUi.TextMuted, TextAlignmentOptions.Right);

            BuildSummary(footer);
        }

        private CanvasGroup Group(string name)
        {
            var rect = ResourceSellUi.Centered(contentRoot, name, Vector2.zero, CardSize);
            return rect.gameObject.AddComponent<CanvasGroup>();
        }

        private void ColumnLabel(Transform parent, string text, float x, float w, TextAlignmentOptions alignment)
        {
            var label = ResourceSellUi.Label(parent, "Col_" + text, x, 0f, w, 40f, font, 18f, FontStyles.Normal,
                ResourceSellUi.TextMuted, alignment);
            label.text = text;
        }

        private void BuildCargo(Transform header)
        {
            const float x = 740f;
            const float y = 124f;
            var icon = ResourceSellUi.AddImage(ResourceSellUi.Place(header, "CargoIcon", x, y + 2f, 50f, 50f), cargoSprite,
                cargoSprite != null ? Color.white : ResourceSellUi.Teal);
            icon.preserveAspect = true;
            icon.enabled = cargoSprite != null;
            var label = ResourceSellUi.Label(header, "CargoLabel", x + 62f, y - 2f, 200f, 26f, font, 17f, FontStyles.Normal,
                ResourceSellUi.TextMuted, TextAlignmentOptions.Left);
            label.text = "화물 적재량";
            cargoNumbers = ResourceSellUi.Label(header, "CargoNumbers", x + 230f, y - 2f, CardSize.x - Pad - x - 230f, 26f,
                font, 17f, FontStyles.Normal, ResourceSellUi.TextMain, TextAlignmentOptions.Right);

            var bar = ResourceSellUi.Place(header, "CargoGauge", x + 62f, y + 30f, CardSize.x - Pad - x - 62f, 18f);
            ResourceSellUi.Image(bar, "Back", null, new Color(0.01f, 0.03f, 0.045f, 1f));
            // 판매로 비워질 부분: 옅은 채움 + 윤곽. 판매 후 남는 부분: 진한 채움.
            cargoRemoved = ResourceSellUi.Image(bar, "Removed", null, ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.16f)).rectTransform;
            cargoAfterFill = ResourceSellUi.Image(bar, "After", null, ResourceSellUi.WithAlpha(ResourceSellUi.TealDeep, 0.95f)).rectTransform;
            var removedLine = ResourceSellUi.Image(bar, "RemovedOutline", ResourceSellArt.ChamferOutline(),
                ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.9f));
            removedLine.type = Image.Type.Sliced;
            removedLine.pixelsPerUnitMultiplier = 2f;
            cargoRemovedLine = removedLine.rectTransform;
            for (var i = 1; i < 10; i++)
            {
                var tick = ResourceSellUi.Image(bar, "Tick" + i, null, new Color(0.01f, 0.03f, 0.045f, 0.85f)).rectTransform;
                tick.anchorMin = new Vector2(i / 10f, 0f);
                tick.anchorMax = new Vector2(i / 10f, 1f);
                tick.sizeDelta = new Vector2(2f, 0f);
                tick.offsetMin = new Vector2(-1f, 0f);
                tick.offsetMax = new Vector2(1f, 0f);
            }

            var outline = ResourceSellUi.Image(bar, "Outline", ResourceSellArt.ChamferOutline(),
                ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.35f));
            outline.type = Image.Type.Sliced;
            outline.pixelsPerUnitMultiplier = 2f;
        }

        private void BuildList(Transform list)
        {
            var width = CardSize.x - Pad * 2f;
            var view = ResourceSellUi.Place(list, "ListView", Pad, ListY, width, ListH);
            scroll = view.gameObject.AddComponent<ScrollRect>();
            var viewport = ResourceSellUi.Place(view, "Viewport", 0f, 0f, ResourceSellRowView.Columns.RowWidth + 12f, ListH);
            viewport.gameObject.AddComponent<RectMask2D>();
            // 빈 영역에서도 휠 스크롤을 받는다.
            var catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = Color.clear;
            catcher.raycastTarget = true;

            listContent = ResourceSellUi.Place(viewport, "Content", 0f, 0f, ResourceSellRowView.Columns.RowWidth, 0f);
            listContent.pivot = new Vector2(0f, 1f);
            listContent.anchoredPosition = Vector2.zero;

            var barRect = ResourceSellUi.Place(view, "Scrollbar", width - 10f, 0f, 8f, ListH);
            ResourceSellUi.Image(barRect, "Track", null, ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.08f));
            var area = ResourceSellUi.Centered(barRect, "SlidingArea", Vector2.zero, Vector2.zero);
            ResourceSellUi.Stretch(area);
            var handle = ResourceSellUi.Image(area, "Handle", null, ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.75f));
            handle.raycastTarget = true;
            scrollbar = barRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            var navigation = scrollbar.navigation;
            navigation.mode = Navigation.Mode.None;
            scrollbar.navigation = navigation;

            scroll.viewport = viewport;
            scroll.content = listContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            scroll.inertia = false;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollbar.gameObject.SetActive(false);

            emptyText = ResourceSellUi.Label(list, "Empty", Pad, ListY, width, ListH, font, 24f, FontStyles.Normal,
                ResourceSellUi.TextMuted, TextAlignmentOptions.Center);
            emptyText.text = EmptyLabel;
            emptyText.gameObject.SetActive(false);
        }

        private void BuildSummary(Transform footer)
        {
            const float top = SummaryY;
            const float gap = 32f;
            var barWidth = CardSize.x - Pad * 2f;
            var bar = ResourceSellUi.Place(footer, "Summary", Pad, top, barWidth, SummaryH);
            ResourceSellUi.Image(bar, "Back", ResourceSellArt.ChamferFill(), new Color(0.012f, 0.028f, 0.042f, 0.97f));
            ResourceSellUi.Image(bar, "Line", ResourceSellArt.ChamferOutline(), ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.38f));

            // 구역 사이 간격은 모두 같다(구분선 양쪽 gap). 판매 버튼과 예상 골드 사이는 gap보다 넓게 떼어 붙어 보이지 않게 한다.
            var sellX = Pad + barWidth - 28f - SellButtonW;
            var x = Pad + gap;

            Caption(footer, "QuantityCaption", "판매 예정", x, top + 16f);
            var cube = ResourceSellUi.AddImage(ResourceSellUi.Place(footer, "QuantityIcon", x, top + 54f, 34f, 34f),
                cargoSprite, cargoSprite != null ? Color.white : Color.clear);
            cube.preserveAspect = true;
            totalQuantityText = ResourceSellUi.Label(footer, "TotalQuantity", x + 46f, top + 46f, 144f, 50f, font, 34f,
                FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            x += 190f + gap;
            Divider(footer, x, top);
            x += 2f + gap;

            Caption(footer, "GoldCaption", "받을 골드", x, top + 16f);
            summaryGlow = ResourceSellUi.AddImage(ResourceSellUi.Place(footer, "GoldGlow", x - 50f, top - 4f, 340f, SummaryH + 8f),
                ResourceSellArt.Soft(), Color.clear);
            var coin = ResourceSellUi.AddImage(ResourceSellUi.Place(footer, "GoldCoin", x, top + 52f, 38f, 38f),
                goldSprite, Color.white);
            coin.preserveAspect = true;
            totalGoldText = ResourceSellUi.Label(footer, "TotalGold", x + 48f, top + 46f, 210f, 50f, font, 34f,
                FontStyles.Bold, ResourceSellUi.Gold, TextAlignmentOptions.Left);
            summaryGoldAnchor = ResourceSellUi.Place(footer, "GoldAnchor", x + 48f, top + 46f, 120f, 50f);
            bonusText = ResourceSellUi.Label(footer, "Bonus", x, top + 94f, 260f, 22f, font, 14f,
                FontStyles.Normal, ResourceSellUi.TextMuted, TextAlignmentOptions.Left);
            x += 260f + gap;
            Divider(footer, x, top);
            x += 2f + gap;

            Caption(footer, "ProjectionCaption", "예상 골드", x, top + 16f);
            var projectionWidth = sellX - 40f - x;
            projectionText = ResourceSellUi.Label(footer, "Projection", x, top + 50f, projectionWidth, 44f, font, 26f,
                FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            projectionText.richText = true;
            projectionText.enableAutoSizing = true;
            projectionText.fontSizeMin = 18f;
            projectionText.fontSizeMax = 26f;

            sellButton = ResourceSellButton.Create(footer, "SellButton", sellX, top + (SummaryH - SellButtonH) * 0.5f,
                SellButtonW, SellButtonH, font, SellLabel, 28f, ResourceSellButton.Variant.Primary, goldSprite, 34f, false);
            // 금화 아이콘은 원래 색을 유지한다(호버 시 글자만 남색으로 바뀜).
            sellLabel = sellButton.GetComponentInChildren<TMP_Text>(true);
            if (sellLabel != null)
            {
                sellLabel.enableAutoSizing = true;
                sellLabel.fontSizeMin = 20f;
                sellLabel.fontSizeMax = 28f;
            }

            sellButton.Button.onClick.AddListener(() => RequestSell());
        }

        private void Caption(Transform parent, string name, string text, float x, float y)
        {
            var label = ResourceSellUi.Label(parent, name, x, y, 220f, 26f, font, 17f, FontStyles.Normal,
                ResourceSellUi.TextMuted, TextAlignmentOptions.Left);
            label.text = text;
        }

        private static void Divider(Transform parent, float x, float top)
        {
            ResourceSellUi.AddImage(ResourceSellUi.Place(parent, "Divider", x, top + 26f, 2f, 72f), null,
                ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.18f));
        }

        private void BuildFx()
        {
            fx = ResourceSellUi.Centered(transform, "Fx", Vector2.zero, Vector2.zero);
            var coinSprite = ResourceSellArt.CoinEmoji();
            impactGlow = ResourceSellUi.AddImage(ResourceSellUi.Centered(fx, "ImpactGlow", Vector2.zero, new Vector2(200f, 200f)),
                ResourceSellArt.Soft(), Color.clear);
            impactGlow.enabled = false;

            coins = new Image[ResourceSellTimeline.CoinCount];
            for (var i = 0; i < coins.Length; i++)
            {
                coins[i] = ResourceSellUi.AddImage(ResourceSellUi.Centered(fx, "Coin" + i, Vector2.zero, new Vector2(64f, 64f)),
                    coinSprite, Color.clear);
                coins[i].enabled = false;
            }

            // 폭발 조각은 방향·거리가 고정된 값이라 같은 상태에서 늘 같은 모양이다.
            burstSparks = new BurstPiece[ResourceSellTimeline.BurstSparkCount];
            for (var i = 0; i < burstSparks.Length; i++)
            {
                var angle = (i + 0.5f) / burstSparks.Length * Mathf.PI * 2f;
                var rect = ResourceSellUi.Centered(fx, "Spark" + i, Vector2.zero, new Vector2(54f, 8f));
                rect.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
                var image = ResourceSellUi.AddImage(rect, ResourceSellArt.SoftRect(), Color.clear);
                image.enabled = false;
                burstSparks[i] = new BurstPiece
                {
                    Image = image,
                    Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)),
                    Distance = 230f + (i % 3) * 36f,
                    Size = 54f + (i % 2) * 18f
                };
            }

            burstCoins = new BurstPiece[ResourceSellTimeline.BurstCoinCount];
            for (var i = 0; i < burstCoins.Length; i++)
            {
                var angle = i / (float)burstCoins.Length * Mathf.PI * 2f + 0.26f + (i * 37 % 11) / 11f * 0.22f;
                var size = 34f + (i % 3) * 8f;
                var image = ResourceSellUi.AddImage(ResourceSellUi.Centered(fx, "BurstCoin" + i, Vector2.zero, new Vector2(size, size)),
                    coinSprite, Color.clear);
                image.enabled = false;
                burstCoins[i] = new BurstPiece
                {
                    Image = image,
                    Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)),
                    Distance = 150f + (i * 7 % 5) / 4f * 100f,
                    Size = size
                };
            }

            streaks = new Streak[ResourceSellTimeline.MaxStreaks];
            for (var i = 0; i < streaks.Length; i++)
            {
                var image = ResourceSellUi.AddImage(ResourceSellUi.Centered(fx, "Streak" + i, Vector2.zero, new Vector2(70f, 8f)),
                    ResourceSellArt.SoftRect(), Color.clear);
                image.enabled = false;
                streaks[i] = new Streak { Image = image };
            }
        }
    }
}
