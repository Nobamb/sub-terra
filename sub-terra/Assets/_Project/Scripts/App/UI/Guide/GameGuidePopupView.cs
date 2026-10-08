using System;
using System.Collections.Generic;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Guide
{
    /// <summary>
    /// 게임 가이드 창(B-140). 코드로 만든 팝업이며 기존 가이드의 열기·닫기 경로(G 키·오른쪽 메뉴·X)를 그대로 쓴다.
    /// 등장은 홀로그램 책이 떠올라 펼쳐지고 페이지가 넘어간 뒤 같은 외곽이 가이드 창으로 변형되는 하나의 동작이고,
    /// 퇴장은 내용이 먼저 사라지고 프레임이 책 크기로 줄어든 뒤 책이 닫히며 사라진다.
    /// 시간은 unscaledDeltaTime, 게임의 timeScale·카메라는 건드리지 않는다.
    /// 콘텐츠는 GameGuideCatalog, 선택 상태는 GameGuideState, 시간표는 GameGuideTimeline이 맡고 이 클래스는 표시만 한다.
    /// </summary>
    public sealed partial class GameGuidePopupView : MonoBehaviour
    {
        public enum PopupState
        {
            Hidden = 0,
            Opening = 1,
            Open = 2,
            Closing = 3
        }

        public const string Title = "SUB-TERRA 게임 가이드";
        public const float CardWidth = 1500f;
        public const float CardHeight = 900f;
        public static readonly Vector2 CardSize = new Vector2(CardWidth, CardHeight);

        // 프레임 모서리 장식과 겹치지 않는 안쪽 여백.
        private const float Pad = 72f;
        private const float TabY = 128f;
        private const float TabHeight = 56f;
        private const float BodyY = 200f;
        private const float BodyHeight = 628f;
        private const float BodyWidth = CardWidth - Pad * 2f;
        private const float ListWidth = 804f;
        private const float ColumnGap = 12f;
        private const float DetailWidth = BodyWidth - ListWidth - ColumnGap;
        private const float FirstCollapsed = 76f;
        private const float FirstExpanded = 258f;
        private const float FilterRowHeight = 48f;
        private const float MaxStep = 0.05f;
        private const float BackdropAlpha = 0.34f;
        private const float FirstAnimSeconds = 0.2f;
        private const float DetailFadeSeconds = 0.14f;

        private static readonly Color BackdropColor = new Color(0.01f, 0.015f, 0.025f, 1f);
        private static readonly Color TitleColor = new Color(0.84f, 0.98f, 1f, 1f);

        private TMP_FontAsset font;
        private IGuideSprites sprites;
        private IGuideData data;
        private MineResetTimedPopupSkin skin;
        private GameGuideState guideState;

        private Canvas canvas;
        private Image backdrop;
        private RectTransform card;
        private Image blocker;
        private GameGuideBookFx book;
        private RectTransform contentRoot;
        private CanvasGroup tabsGroup;
        private CanvasGroup listGroup;
        private CanvasGroup detailGroup;
        private RectTransform tabsRect;
        private RectTransform listRect;
        private RectTransform detailRect;

        private PopupState state;
        private bool fullIntro;
        private float clock;
        private float closeClock;
        private GuideFrame frame;
        private GuideFrame closeFrom;
        private bool settled;

        private float firstT = 1f;
        private float firstTarget = 1f;
        private float detailFade = 1f;
        private float fitScale = 1f;
        private Vector2 lastCanvasSize;

        public PopupState State => state;
        public bool IsVisible => state != PopupState.Hidden;
        public bool IsOpen => state == PopupState.Opening || state == PopupState.Open;
        public bool IsClosing => state == PopupState.Closing;
        public bool IsFullIntro => fullIntro;
        public float Clock => state == PopupState.Closing ? closeClock : clock;
        public GuideFrame Frame => frame;
        public Canvas Canvas => canvas;
        public RectTransform CardRect => card;
        public GameGuideState GuideState => guideState;
        public bool IsInteractive => contentRoot != null && tabsGroup != null && tabsGroup.interactable;
        public bool BlocksInput => blocker != null && blocker.raycastTarget && gameObject.activeSelf;
        public float CardScale => card != null ? card.localScale.x : 0f;
        public float FirstExploreProgress => firstT;

        /// <summary>X 버튼. 실제 닫기 경로(chrome 상태 갱신 포함)는 가이드 호스트가 정한다.</summary>
        public event Action CloseRequested;

        /// <summary>확인용: true면 Update가 시간을 진행하지 않고 테스트가 Tick으로 직접 진행한다.</summary>
        public bool ManualTick { get; set; }

        /// <summary>canvasRoot 아래에 창 루트(자체 Canvas)를 만든다. 뒤쪽 클릭은 창 영역만 막는다.</summary>
        public static GameGuidePopupView Create(Transform canvasRoot, IGuideData guideData)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var root = new GameObject(
                "GameGuidePopup",
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
            root.GetComponent<Image>().raycastTarget = false;

            var view = root.AddComponent<GameGuidePopupView>();
            view.data = guideData;
            view.guideState = GameGuideState.Session;
            view.Build();
            return view;
        }

        /// <summary>선택 상태를 바꿔 끼운다(테스트용). 기본은 세션 공유 상태다.</summary>
        public void UseState(GameGuideState value)
        {
            if (guideState != null)
            {
                guideState.Changed -= OnStateChanged;
            }

            guideState = value ?? GameGuideState.Session;
            if (state != PopupState.Hidden)
            {
                guideState.Changed += OnStateChanged;
                RefreshAll(true);
            }
        }

        /// <summary>
        /// 연다. 첫 열기는 책이 떠올라 페이지 세 장이 넘어가는 전체 연출, 이후 재열기는 페이지 한 장의 짧은 연출이다.
        /// 이미 열려 있거나 열리는 중이면 아무것도 하지 않아 책·창이 겹쳐 생기지 않는다.
        /// 닫는 중이면 현재 모양에서 이어서 다시 연다.
        /// </summary>
        public bool Show()
        {
            if (state == PopupState.Opening || state == PopupState.Open)
            {
                return true;
            }

            var resume = state == PopupState.Closing;
            var previous = frame;
            fullIntro = !GameGuideState.IntroPlayed;
            GameGuideState.IntroPlayed = true;

            gameObject.SetActive(true);
            state = PopupState.Opening;
            settled = false;
            clock = resume ? GameGuideTimeline.OpenTimeForExpand(previous.Expand, fullIntro) : 0f;

            guideState.Changed -= OnStateChanged;
            guideState.Changed += OnStateChanged;
            if (!resume)
            {
                RefreshAll(true);
            }

            transform.SetAsLastSibling();
            PopupWindowSorting.BringToFront(canvas);
            lastCanvasSize = Vector2.zero;
            FitToCanvas();
            ApplyFrame(GameGuideTimeline.Open(clock, fullIntro));
            return true;
        }

        /// <summary>닫는 연출을 시작한다. 등장 도중에도 받아들이며 연출이 끝나면 루트가 꺼져 입력 차단이 남지 않는다.</summary>
        public bool BeginClose()
        {
            if (state == PopupState.Hidden || state == PopupState.Closing)
            {
                return false;
            }

            StopDemo();
            closeFrom = frame;
            closeClock = 0f;
            state = PopupState.Closing;
            tabsGroup.interactable = false;
            listGroup.interactable = false;
            detailGroup.interactable = false;
            UiKeyboardSubmitGuard.ClearSelection();
            if (!isActiveAndEnabled)
            {
                FinishClose();
                return true;
            }

            ApplyFrame(GameGuideTimeline.Close(0f, closeFrom));
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
            FitToCanvas();
            switch (state)
            {
                case PopupState.Opening:
                    clock += dt;
                    var total = GameGuideTimeline.OpenDuration(fullIntro);
                    if (clock >= total)
                    {
                        clock = total;
                        ApplyFrame(GameGuideTimeline.Open(total, fullIntro));
                        Settle();
                    }
                    else
                    {
                        ApplyFrame(GameGuideTimeline.Open(clock, fullIntro));
                    }

                    TickUi(dt);
                    break;
                case PopupState.Open:
                    TickUi(dt);
                    break;
                case PopupState.Closing:
                    closeClock += dt;
                    if (closeClock >= GameGuideTimeline.CloseDuration)
                    {
                        FinishClose();
                        return;
                    }

                    ApplyFrame(GameGuideTimeline.Close(closeClock, closeFrom));
                    break;
            }
        }

        private void OnDisable()
        {
            // 씬 전환·부모 비활성화로 꺼져도 다음 표시가 깨끗하게 시작되도록 되돌린다.
            state = PopupState.Hidden;
            StopDemo();
            if (guideState != null)
            {
                guideState.Changed -= OnStateChanged;
            }

            PopupWindowSorting.Remove(canvas);
        }

        private void OnDestroy()
        {
            if (guideState != null)
            {
                guideState.Changed -= OnStateChanged;
            }
        }

        private void FinishClose()
        {
            state = PopupState.Hidden;
            clock = 0f;
            closeClock = 0f;
            StopDemo();
            if (guideState != null)
            {
                guideState.Changed -= OnStateChanged;
            }

            PopupWindowSorting.Remove(canvas);
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void Settle()
        {
            state = PopupState.Open;
            settled = true;
            detailFade = 1f;
            ApplyFrame(GameGuideTimeline.Open(GameGuideTimeline.OpenDuration(fullIntro), fullIntro));
            tabsGroup.interactable = true;
            listGroup.interactable = true;
            detailGroup.interactable = true;
            PlayDemo();
        }

        /// <summary>연출 한 프레임의 값을 모든 층에 반영한다.</summary>
        private void ApplyFrame(GuideFrame f)
        {
            frame = f;
            backdrop.color = ResourceSellUi.WithAlpha(BackdropColor, BackdropAlpha * f.Backdrop);
            book.Apply(f, state == PopupState.Closing ? closeClock + 0.6f : clock);
            SetReveal(tabsGroup, tabsRect, f.Tabs);
            SetReveal(listGroup, listRect, f.List);
            SetReveal(detailGroup, detailRect, f.Detail);
            blocker.raycastTarget = true;
            var interactive = state == PopupState.Open || (state == PopupState.Opening && f.Detail >= 0.999f);
            if (state != PopupState.Closing)
            {
                tabsGroup.interactable = interactive;
                listGroup.interactable = interactive;
                detailGroup.interactable = interactive;
            }
        }

        private static void SetReveal(CanvasGroup group, RectTransform rect, float reveal)
        {
            group.alpha = reveal;
            group.blocksRaycasts = reveal > 0.02f;
            rect.anchoredPosition = new Vector2(0f, -10f * (1f - reveal));
        }

        private void TickUi(float dt)
        {
            if (!Mathf.Approximately(firstT, firstTarget))
            {
                firstT = Mathf.MoveTowards(firstT, firstTarget, dt / FirstAnimSeconds);
                LayoutBody();
            }

            if (detailFade < 1f)
            {
                detailFade = Mathf.Min(1f, detailFade + dt / DetailFadeSeconds);
                ApplyDetailFade();
            }

            if (settled)
            {
                detailStage?.Tick(dt);
            }
        }

        /// <summary>카드가 캔버스보다 크면 창 전체를 한 번에 줄여 맞춘다(화면 비율이 달라도 같은 모양).</summary>
        private void FitToCanvas()
        {
            var rootRect = (RectTransform)transform;
            var size = rootRect.rect.size;
            if (size == lastCanvasSize || size.x <= 1f || size.y <= 1f)
            {
                return;
            }

            lastCanvasSize = size;
            fitScale = Mathf.Min(1f, size.x * 0.97f / CardSize.x, size.y * 0.96f / CardSize.y);
            card.localScale = new Vector3(fitScale, fitScale, 1f);
        }

        private void PlayDemo()
        {
            detailStage?.Play();
        }

        private void StopDemo()
        {
            if (detailStage != null)
            {
                detailStage.Stop();
            }
        }

        /// <summary>자세히 보기에서 재생 중인 시연이 있는지(닫은 뒤 반복 효과 점검용).</summary>
        public bool IsDemoPlaying => detailStage != null && detailStage.IsPlaying;

        public bool OwnsCanvas(Canvas other)
        {
            return other != null && canvas != null && other == canvas;
        }

        private void RequestClose()
        {
            if (CloseRequested != null)
            {
                CloseRequested.Invoke();
            }
            else
            {
                BeginClose();
            }
        }
    }
}
