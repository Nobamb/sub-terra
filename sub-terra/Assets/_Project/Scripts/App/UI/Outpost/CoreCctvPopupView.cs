using System;
using System.Collections.Generic;
using SubTerra.App.Outpost;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 전진기지 코어 팝업: 왼쪽 ‘연결된 시설’ 목록과 오른쪽 CCTV.
    /// 표시 전용이다. 시설 연결·전력 판정과 접근·이탈 판정은 건드리지 않고, 스냅샷의 연결 시설만 받아 그린다.
    /// 시설을 고르는 동작은 CCTV 관찰 대상만 바꾸며 시설 사용이나 플레이어 이동을 일으키지 않는다.
    /// 모든 시간은 unscaledDeltaTime이라 게임 시간이 멈춘 상태에서도 재생된다.
    /// </summary>
    public sealed class CoreCctvPopupView : MonoBehaviour
    {
        public enum PlayState
        {
            Hidden = 0,
            Intro = 1,
            Live = 2,
            Exiting = 3
        }

        public enum VideoState
        {
            Off = 0,
            PoweringOn = 1,
            On = 2,
            PoweringOff = 3
        }

        [Serializable]
        public struct FacilityIcon
        {
            public string buildingId;
            public Sprite sprite;
        }

        public const string TitleLabel = "전진기지 코어";
        public const string EmptyListLabel = "연결된 시설 없음";
        public const string ConnectedLabel = "연결되었습니다";

        private const float MaxStep = 0.05f;
        private const float NoiseStepSeconds = 1f / 12f;
        private const float BarStepSeconds = 1f / 30f;
        private const float OpenLineHeight = 3f;
        private const float GhostNearDistance = 14f;
        private const float GhostFarDistance = 28f;
        private const float ScanlineAlpha = 0.16f;
        private const float NoiseBaseAlpha = 0.01f;

        private static readonly Color Cyan = new Color(0.42f, 0.94f, 1f, 1f);
        private static readonly Color RecRed = new Color(1f, 0.22f, 0.2f, 1f);

        [Header("Window")]
        [SerializeField] private CanvasGroup windowGroup;
        [SerializeField] private RectTransform panelBackdrop;
        [SerializeField] private CanvasGroup panelBackdropGroup;
        [SerializeField] private Image panelGlow;
        [SerializeField] private RectTransform contentClip;
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private CanvasGroup bodyGroup;
        [SerializeField] private Image openLine;
        [SerializeField] private Image flashGlow;
        [SerializeField] private RawImage hexPattern;
        [SerializeField] private RawImage[] edgeNoise;
        [SerializeField] private Button closeButton;

        [Header("List")]
        [SerializeField] private CanvasGroup listGroup;
        [SerializeField] private ScrollRect listScroll;
        [SerializeField] private RectTransform listViewport;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private Scrollbar listScrollbar;
        [SerializeField] private CoreCctvFacilityItem itemTemplate;
        [SerializeField] private TMP_Text emptyLabel;
        [SerializeField] private FacilityIcon[] fallbackIcons;
        [SerializeField] private Image[] borderPanels;

        [Header("CCTV")]
        [SerializeField] private CanvasGroup screenGroup;
        [SerializeField] private RawImage video;
        [SerializeField] private RawImage ghostNear;
        [SerializeField] private RawImage ghostFar;
        [SerializeField] private RawImage scanlines;
        [SerializeField] private RawImage noise;
        [SerializeField] private Image vignette;
        [SerializeField] private Image[] bars;
        [SerializeField] private CanvasGroup terminalGroup;
        [SerializeField] private TMP_Text[] terminalLines;
        [SerializeField] private TMP_Text connectedText;
        [SerializeField] private CanvasGroup recGroup;
        [SerializeField] private Image recDot;

        private readonly CoreCctvFacilityList list = new CoreCctvFacilityList();
        private readonly List<CoreCctvFacilityItem> items = new List<CoreCctvFacilityItem>();
        private readonly Dictionary<string, float> appearAt = new Dictionary<string, float>();
        private readonly string[] terminalTexts = new string[CoreCctvTimeline.TerminalLineCount];
        private readonly int[] terminalLengths = new int[CoreCctvTimeline.TerminalLineCount];

        private CoreCctvCameraRig rig;
        private IFacilityWorldLocator locator;
        private Func<string, Sprite> iconResolver;

        private PlayState state;
        private VideoState videoState;
        private float clock;
        private float exitClock;
        private float revealClock;
        private float videoClock;
        private float videoLevel;
        private float emptyClock;
        private float introEndTime;
        private bool showRequested;
        private bool revealStarted;
        private bool decided;
        private bool connectedPhase;
        private bool terminalLatched;
        private int activeItemCount;
        private string cameraTargetId = string.Empty;
        private float noiseTimer;
        private int noiseStep;
        private int barStep = -1;
        private bool ghostsActive;
        private Vector2 lastScreenPixels;

        private float lastOpen;
        private float lastLine;
        private float lastBody = 1f;
        private float lastTitle = 1f;
        private float exitStartOpen;
        private float exitStartLine;
        private float exitStartBody;
        private float exitStartTitle;

        public PlayState State => state;
        public VideoState Video => videoState;
        public bool IsShown => state == PlayState.Intro || state == PlayState.Live;
        public float Clock => clock;
        public Button CloseButton => closeButton;
        public CoreCctvFacilityList List => list;
        public CoreCctvCameraRig Rig => rig;
        public int ItemCount => activeItemCount;
        public string SelectedInstanceId => list.SelectedInstanceId;
        public string CameraTargetId => cameraTargetId;
        public bool RevealStarted => revealStarted;
        public float RecAlpha => recGroup != null ? recGroup.alpha : 0f;
        public float ConnectedLabelAlpha => connectedText != null ? connectedText.alpha : 0f;
        public float EmptyLabelAlpha => emptyLabel != null ? emptyLabel.alpha : 0f;
        public float VideoLevel => videoLevel;
        public float ListAlpha => listGroup != null ? listGroup.alpha : 0f;
        public bool ScrollbarVisible => listScrollbar != null && listScrollbar.gameObject.activeSelf;
        public RawImage VideoImage => video;
        public RectTransform ListContent => listContent;
        public RectTransform ListViewport => listViewport;
        public ScrollRect ListScroll => listScroll;
        public bool GhostsVisible => ghostsActive;
        public float TerminalAlpha => terminalGroup != null ? terminalGroup.alpha : 0f;
        public float WindowOpen => lastOpen;

        public CoreCctvFacilityItem ItemAt(int index)
        {
            return index >= 0 && index < activeItemCount ? items[index] : null;
        }

        public void SetFacilityLocator(IFacilityWorldLocator worldLocator)
        {
            locator = worldLocator;
        }

        public void SetIconResolver(Func<string, Sprite> resolver)
        {
            iconResolver = resolver;
        }

        private void Awake()
        {
            EnsureArt();
            if (itemTemplate != null)
            {
                itemTemplate.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // Show를 거치지 않은 활성화(부모 패널이 꺼졌다 켜진 경우)에는 멈춘 화면이 남지 않도록 다시 숨긴다.
            if (!showRequested)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            showRequested = false;
            state = PlayState.Hidden;
            // 씬 전환·비활성화로 꺼져도 다음 표시가 깨끗하게 시작되도록 모든 상태와 렌더링을 되돌린다.
            StopVideoImmediate();
            ResetRuntimeState();
            ResetVisuals();
            if (windowGroup != null)
            {
                windowGroup.alpha = 1f;
                windowGroup.blocksRaycasts = true;
            }
        }

        private void OnDestroy()
        {
            if (rig != null)
            {
                rig.Dispose();
                rig = null;
            }
        }

        /// <summary>확인용: 켜 두면 Update가 시간을 진행하지 않아 Tick을 직접 호출하는 캡처·테스트에서 연출 시각을 고정한다.</summary>
        public bool ManualTick { get; set; }

        private void Update()
        {
            if (state == PlayState.Hidden || ManualTick)
            {
                return;
            }

            Tick(Mathf.Min(Time.unscaledDeltaTime, MaxStep));
        }

        /// <summary>등장 연출을 처음부터 재생한다. 이미 보이는 중이면 아무것도 하지 않는다.</summary>
        public void Show()
        {
            if (IsShown)
            {
                return;
            }

            showRequested = true;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            BeginIntro();
        }

        /// <summary>종료 연출(TV 전원 꺼짐) 후 비활성화한다. 등장 도중에 불려도 지금 모습에서 이어서 닫힌다.</summary>
        public void Hide()
        {
            if (state == PlayState.Hidden || state == PlayState.Exiting)
            {
                return;
            }

            state = PlayState.Exiting;
            exitClock = 0f;
            exitStartOpen = lastOpen;
            exitStartLine = lastLine;
            exitStartBody = lastBody;
            exitStartTitle = lastTitle;
            if (windowGroup != null)
            {
                windowGroup.blocksRaycasts = false;
            }

            SetEdgeEffects(0f, 0f);
            if (!isActiveAndEnabled)
            {
                FinishExit();
            }
        }

        /// <summary>확인용: 연출 없이 즉시 숨긴다.</summary>
        public void HideImmediate()
        {
            if (state == PlayState.Hidden && !gameObject.activeSelf)
            {
                return;
            }

            FinishExit();
        }

        /// <summary>
        /// 스냅샷의 연결 시설. 팝업이 닫혀 있어도 최신 목록을 보관하고, 열린 뒤에만 화면을 갱신한다.
        /// 목록이 달라지지 않았다면 아무것도 하지 않아 등장 연출이 다시 시작되지 않는다.
        /// </summary>
        public void SetFacilities(IReadOnlyList<OutpostFacilityReadModel> facilities)
        {
            if (!list.Update(facilities))
            {
                return;
            }

            if (!revealStarted || state == PlayState.Hidden || state == PlayState.Exiting)
            {
                return;
            }

            OnListChanged();
        }

        /// <summary>목록 항목을 눌렀을 때와 같은 경로. 관찰 대상만 바꾼다.</summary>
        public void SelectFacility(string instanceId)
        {
            if (state == PlayState.Hidden || state == PlayState.Exiting || !revealStarted)
            {
                return;
            }

            if (list.Select(instanceId))
            {
                OnSelectionChanged();
            }
        }

        /// <summary>위(-1)·아래(+1) 입력과 같은 경로.</summary>
        public void StepSelection(int delta)
        {
            if (state == PlayState.Hidden || state == PlayState.Exiting || !revealStarted)
            {
                return;
            }

            if (list.Step(delta))
            {
                OnSelectionChanged();
            }
        }

        /// <summary>테스트와 Update가 같은 경로로 시간을 진행한다. 큰 값도 프레임 한계 단위로 나눠 같은 결과를 낸다.</summary>
        public void Tick(float deltaSeconds)
        {
            while (deltaSeconds > 0f && state != PlayState.Hidden)
            {
                var step = Mathf.Min(deltaSeconds, MaxStep);
                TickStep(step);
                deltaSeconds -= step;
            }
        }

        private void TickStep(float deltaSeconds)
        {
            if (state == PlayState.Hidden)
            {
                return;
            }

            if (state == PlayState.Exiting)
            {
                TickExit(deltaSeconds);
                return;
            }

            if (state == PlayState.Intro)
            {
                clock += deltaSeconds;
                TickIntro();
                if (state == PlayState.Intro && revealStarted && clock >= introEndTime)
                {
                    state = PlayState.Live;
                }
            }

            if (!revealStarted)
            {
                TickNoise(deltaSeconds);
                return;
            }

            revealClock += deltaSeconds;
            TickList(deltaSeconds);
            TickVideo(deltaSeconds);
            if (rig != null)
            {
                rig.Tick(deltaSeconds);
            }

            ApplyGhosts();
            TickRenderSize();
            TickNoise(deltaSeconds);
            HandleKeyboard();
        }

        public bool HasRequiredReferences()
        {
            return windowGroup != null
                && panelBackdrop != null
                && contentClip != null
                && closeButton != null
                && listScroll != null
                && listViewport != null
                && listContent != null
                && itemTemplate != null
                && emptyLabel != null
                && video != null
                && screenGroup != null
                && terminalLines != null
                && terminalLines.Length >= CoreCctvTimeline.TerminalLineCount
                && connectedText != null
                && recGroup != null;
        }

        // ---- 등장 ----

        private void BeginIntro()
        {
            EnsureArt();
            StopVideoImmediate();
            ResetRuntimeState();
            ResetVisuals();
            state = PlayState.Intro;
            if (windowGroup != null)
            {
                windowGroup.alpha = 1f;
                windowGroup.blocksRaycasts = true;
            }

            ApplyWindow(CoreCctvTimeline.Window(0f));
        }

        private void TickIntro()
        {
            ApplyWindow(CoreCctvTimeline.Window(clock));
            ApplyTerminal(clock);

            if (!decided && clock >= CoreCctvTimeline.DecideTime)
            {
                decided = true;
                connectedPhase = list.Count > 0;
            }

            if (decided && !revealStarted && clock >= CoreCctvTimeline.RevealStart(connectedPhase))
            {
                BeginReveal();
            }
        }

        private void ApplyTerminal(float t)
        {
            // 연결된 시설 수는 이 줄을 찍는 순간의 실제 값이다. 이후에 바뀌면 다음 단계가 그 값을 따른다.
            if (!terminalLatched && t >= CoreCctvTimeline.TerminalLineStart(2))
            {
                terminalLatched = true;
                var count = list.Count;
                SetTerminalLine(2, count > 0 ? "> 시설 " + count + "개 확인" : "> 연결된 시설 없음");
                SetTerminalLine(3, count > 0 ? "> 영상 연결" : string.Empty);
            }

            for (var i = 0; i < CoreCctvTimeline.TerminalLineCount; i++)
            {
                if (terminalLines == null || i >= terminalLines.Length || terminalLines[i] == null)
                {
                    continue;
                }

                var visible = Mathf.RoundToInt(terminalLengths[i] * CoreCctvTimeline.TerminalTyping(i, t));
                if (terminalLines[i].maxVisibleCharacters != visible)
                {
                    terminalLines[i].maxVisibleCharacters = visible;
                }
            }

            if (terminalGroup != null)
            {
                terminalGroup.alpha = CoreCctvTimeline.TerminalAlpha(t, !decided || connectedPhase);
            }

            if (connectedText != null)
            {
                connectedText.alpha = decided && connectedPhase ? CoreCctvTimeline.ConnectedAlpha(t) : 0f;
            }
        }

        private void SetTerminalLine(int index, string text)
        {
            terminalTexts[index] = text;
            terminalLengths[index] = text.Length;
            if (terminalLines != null && index < terminalLines.Length && terminalLines[index] != null)
            {
                terminalLines[index].text = text;
                terminalLines[index].maxVisibleCharacters = 0;
            }
        }

        private void BeginReveal()
        {
            revealStarted = true;
            revealClock = 0f;
            emptyClock = 0f;
            list.ResetSelection();
            if (listGroup != null)
            {
                listGroup.alpha = 1f;
            }

            RebuildItems();
            if (list.Count > 0)
            {
                StartVideoSequence();
                introEndTime = clock + CoreCctvTimeline.VideoOnDelay + CoreCctvTimeline.VideoOnDuration;
            }
            else
            {
                introEndTime = clock + CoreCctvTimeline.ItemFade;
            }
        }

        // ---- 목록 변경과 선택 ----

        private void OnListChanged()
        {
            RebuildItems();
            if (list.Count == 0)
            {
                emptyClock = 0f;
                cameraTargetId = string.Empty;
                BeginVideoOff();
                return;
            }

            if (videoState == VideoState.Off || videoState == VideoState.PoweringOff)
            {
                StartVideoSequence();
                return;
            }

            ScrollToSelection();
            if (list.SelectedInstanceId != cameraTargetId)
            {
                // 보고 있던 시설이 사라져 선택이 넘어갔다.
                RetargetCamera(true);
            }
        }

        private void OnSelectionChanged()
        {
            HighlightSelection();
            ScrollToSelection();
            RetargetCamera(videoState == VideoState.PoweringOn || videoState == VideoState.On);
        }

        private void OnItemClicked(string instanceId)
        {
            SelectFacility(instanceId);
        }

        private void HighlightSelection()
        {
            var selected = list.SelectedInstanceId;
            for (var i = 0; i < activeItemCount; i++)
            {
                items[i].SetSelected(items[i].InstanceId == selected);
            }
        }

        private void RebuildItems()
        {
            var facilities = list.Items;
            var count = facilities.Count;
            var added = 0;
            var present = new HashSet<string>();
            for (var i = 0; i < count; i++)
            {
                present.Add(facilities[i].InstanceId);
            }

            // 사라진 항목의 등장 기록은 지워 다시 생기면 새로 나타나게 한다.
            if (appearAt.Count > 0)
            {
                var stale = new List<string>();
                foreach (var key in appearAt.Keys)
                {
                    if (!present.Contains(key))
                    {
                        stale.Add(key);
                    }
                }

                for (var i = 0; i < stale.Count; i++)
                {
                    appearAt.Remove(stale[i]);
                }
            }

            for (var i = 0; i < count; i++)
            {
                var item = EnsureItem(i);
                var facility = facilities[i];
                item.gameObject.SetActive(true);
                item.Bind(facility, ResolveIcon(facility.BuildingId));
                if (!appearAt.ContainsKey(facility.InstanceId))
                {
                    appearAt[facility.InstanceId] = revealClock + CoreCctvTimeline.ItemDelay(added, count);
                    added++;
                }

                var rect = item.Rect;
                rect.anchoredPosition = new Vector2(0f, -CoreCctvListLayout.ItemTop(i));
            }

            for (var i = count; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].gameObject.SetActive(false);
                }
            }

            activeItemCount = count;
            if (listContent != null)
            {
                listContent.sizeDelta = new Vector2(0f, CoreCctvListLayout.ContentHeight(count));
            }

            HighlightSelection();
            TickList(0f);
            ScrollToSelection();
        }

        private CoreCctvFacilityItem EnsureItem(int index)
        {
            while (items.Count <= index)
            {
                var item = Instantiate(itemTemplate, listContent);
                item.gameObject.SetActive(true);
                item.SetArt(CoreCctvArt.Border(), CoreCctvArt.Glow());
                item.Clicked += OnItemClicked;
                items.Add(item);
            }

            return items[index];
        }

        private Sprite ResolveIcon(string buildingId)
        {
            var sprite = iconResolver != null ? iconResolver(buildingId) : null;
            if (sprite != null)
            {
                return sprite;
            }

            if (fallbackIcons != null)
            {
                for (var i = 0; i < fallbackIcons.Length; i++)
                {
                    if (fallbackIcons[i].buildingId == buildingId)
                    {
                        return fallbackIcons[i].sprite;
                    }
                }
            }

            return null;
        }

        private void TickList(float deltaSeconds)
        {
            for (var i = 0; i < activeItemCount; i++)
            {
                var item = items[i];
                appearAt.TryGetValue(item.InstanceId, out var start);
                var alpha = Mathf.Clamp01((revealClock - start) / CoreCctvTimeline.ItemFade);
                alpha = alpha * alpha * (3f - 2f * alpha);
                item.SetAlpha(alpha);
                // 글자·아이콘은 크기를 바꾸지 않고 위치만 살짝 내려앉는다.
                item.Rect.anchoredPosition = new Vector2(0f, -CoreCctvListLayout.ItemTop(i) + 10f * (1f - alpha));
            }

            if (emptyLabel != null)
            {
                if (list.Count == 0 && revealStarted)
                {
                    emptyClock += deltaSeconds;
                    var k = Mathf.Clamp01(emptyClock / CoreCctvTimeline.ItemFade);
                    emptyLabel.alpha = k * k * (3f - 2f * k);
                }
                else
                {
                    emptyLabel.alpha = 0f;
                }
            }
        }

        private void ScrollToSelection()
        {
            if (listContent == null || listViewport == null)
            {
                return;
            }

            var viewport = listViewport.rect.height;
            var current = listContent.anchoredPosition.y;
            var index = list.SelectedIndex;
            var target = index < 0
                ? Mathf.Clamp(current, 0f, CoreCctvListLayout.MaxScroll(list.Count, viewport))
                : CoreCctvListLayout.ScrollToReveal(index, list.Count, viewport, current);
            if (!Mathf.Approximately(target, current))
            {
                if (listScroll != null)
                {
                    listScroll.StopMovement();
                }

                listContent.anchoredPosition = new Vector2(0f, target);
            }
        }

        // ---- CCTV ----

        private bool EnsureRigReady()
        {
            if (!Application.isPlaying || video == null)
            {
                return false;
            }

            if (rig == null)
            {
                rig = new CoreCctvCameraRig();
            }

            rig.EnsureCreated();
            TickRenderSize(true);
            return rig.Texture != null;
        }

        private void StartVideoSequence()
        {
            videoState = VideoState.PoweringOn;
            videoClock = -CoreCctvTimeline.VideoOnDelay;
            // 영상이 켜지기 전에 위치를 먼저 맞춰 두어 첫 화면부터 해당 시설이 보이게 한다.
            if (EnsureRigReady())
            {
                MoveCameraToSelection(false);
                rig.SetRendering(true);
            }

            ApplyVideoFrame(0f, 0f, 0f, 0f, 0f);
        }

        private void BeginVideoOff()
        {
            if (videoState == VideoState.Off || videoState == VideoState.PoweringOff)
            {
                return;
            }

            videoState = VideoState.PoweringOff;
            videoClock = 0f;
            videoLevelAtOff = Mathf.Max(0.01f, videoLevel);
        }

        private void StopVideoImmediate()
        {
            videoState = VideoState.Off;
            videoClock = 0f;
            videoLevel = 0f;
            if (rig != null)
            {
                rig.SetRendering(false);
            }

            ApplyVideoFrame(0f, 0f, 0f, 0f, 0f);
            ClearGhosts();
        }

        private void TickVideo(float deltaSeconds)
        {
            switch (videoState)
            {
                case VideoState.PoweringOn:
                    videoClock += deltaSeconds;
                    if (videoClock < 0f)
                    {
                        break;
                    }

                    if (videoClock >= CoreCctvTimeline.VideoOnDuration)
                    {
                        videoState = VideoState.On;
                        ApplyVideoFrame(1f, 1f, 0f, 0f, 1f);
                        break;
                    }

                    var power = CoreCctvTimeline.Power(videoClock);
                    ApplyVideoFrame(power.Alpha, power.Brightness, power.BarAlpha, power.NoiseBoost, power.Rec);
                    break;
                case VideoState.On:
                    ApplyRecBlink();
                    break;
                case VideoState.PoweringOff:
                    videoClock += deltaSeconds;
                    var level = videoLevelAtOff * CoreCctvTimeline.VideoOff(videoClock);
                    if (videoClock >= CoreCctvTimeline.VideoOffDuration)
                    {
                        videoState = VideoState.Off;
                        if (rig != null)
                        {
                            rig.SetRendering(false);
                        }

                        ApplyVideoFrame(0f, 0f, 0f, 0f, 0f);
                        ClearGhosts();
                    }
                    else
                    {
                        ApplyVideoFrame(level, 1f, 0f, 0f, level);
                    }

                    break;
            }
        }

        private float videoLevelAtOff = 1f;

        private void ApplyVideoFrame(float alpha, float brightness, float barAlpha, float noiseBoost, float rec)
        {
            videoLevel = alpha;
            if (video != null)
            {
                video.color = new Color(brightness, brightness, brightness, alpha);
            }

            if (scanlines != null)
            {
                scanlines.color = WithAlpha(scanlines.color, ScanlineAlpha * alpha);
            }

            if (noise != null)
            {
                noise.color = WithAlpha(noise.color, (NoiseBaseAlpha + 0.10f * noiseBoost) * alpha);
            }

            if (vignette != null)
            {
                vignette.color = WithAlpha(vignette.color, alpha);
            }

            if (recGroup != null)
            {
                recGroup.alpha = rec;
            }

            ApplyBars(barAlpha);
        }

        private void ApplyRecBlink()
        {
            if (recDot == null)
            {
                return;
            }

            // 느린 점멸. 팝업이 보이는 동안에만 돈다.
            var blink = Mathf.Repeat(Time.unscaledTime, 1.2f) < 0.8f ? 1f : 0.3f;
            recDot.color = WithAlpha(RecRed, blink);
        }

        private void ApplyBars(float barAlpha)
        {
            if (bars == null || bars.Length == 0)
            {
                return;
            }

            if (barAlpha <= 0.001f)
            {
                if (barStep != -1)
                {
                    for (var i = 0; i < bars.Length; i++)
                    {
                        if (bars[i] != null)
                        {
                            bars[i].color = WithAlpha(bars[i].color, 0f);
                        }
                    }

                    barStep = -1;
                }

                return;
            }

            var step = (int)(Mathf.Max(0f, videoClock) / BarStepSeconds);
            var screenHeight = screenGroup != null ? ((RectTransform)screenGroup.transform).rect.height : 400f;
            for (var i = 0; i < bars.Length; i++)
            {
                var bar = bars[i];
                if (bar == null)
                {
                    continue;
                }

                if (step != barStep)
                {
                    var y = FacilityServicePopupTimeline.Hash01(step * 31 + i * 7 + 1) * screenHeight;
                    var height = 2f + 10f * FacilityServicePopupTimeline.Hash01(step * 17 + i * 11 + 5);
                    var rect = bar.rectTransform;
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(0f, y);
                    rect.sizeDelta = new Vector2(0f, height);
                }

                bar.color = new Color(0.82f, 1f, 1f, 0.32f * barAlpha);
            }

            barStep = step;
        }

        private void RetargetCamera(bool animated)
        {
            if (EnsureRigReady())
            {
                MoveCameraToSelection(animated);
            }
        }

        private void MoveCameraToSelection(bool animated)
        {
            var id = list.SelectedInstanceId;
            if (rig == null || string.IsNullOrEmpty(id) || locator == null)
            {
                return;
            }

            // 위치를 찾지 못하면(시설이 막 사라졌다면) 카메라는 그 자리에 둔다.
            if (!locator.TryGetWorldCenter(id, out var center))
            {
                return;
            }

            cameraTargetId = id;
            if (animated && rig.HasPosition)
            {
                rig.MoveTo(center);
            }
            else
            {
                rig.SnapTo(center);
            }
        }

        private void ApplyGhosts()
        {
            if (rig == null || !rig.IsMoving || videoLevel <= 0.01f)
            {
                ClearGhosts();
                return;
            }

            // 이동 중에만 지나온 방향으로 이전 화면이 겹쳐 보이는 잔상. CCTV 화면 안에서만 그려진다.
            var speed = rig.MoveSpeed;
            var direction = rig.MoveDirection;
            SetGhost(ghostNear, direction * (GhostNearDistance * speed), 0.28f * speed * videoLevel);
            SetGhost(ghostFar, direction * (GhostFarDistance * speed), 0.14f * speed * videoLevel);
            ghostsActive = true;
        }

        private void ClearGhosts()
        {
            if (!ghostsActive)
            {
                return;
            }

            SetGhost(ghostNear, Vector2.zero, 0f);
            SetGhost(ghostFar, Vector2.zero, 0f);
            ghostsActive = false;
        }

        private static void SetGhost(RawImage ghost, Vector2 offset, float alpha)
        {
            if (ghost == null)
            {
                return;
            }

            ghost.rectTransform.anchoredPosition = offset;
            ghost.color = new Color(1f, 1f, 1f, alpha);
        }

        private void TickRenderSize(bool force = false)
        {
            if (rig == null || !rig.IsCreated || video == null)
            {
                return;
            }

            var rect = video.rectTransform;
            var scale = rect.lossyScale;
            var pixels = new Vector2(
                Mathf.Round(rect.rect.width * Mathf.Abs(scale.x)),
                Mathf.Round(rect.rect.height * Mathf.Abs(scale.y)));
            if (!force && pixels == lastScreenPixels)
            {
                return;
            }

            lastScreenPixels = pixels;
            if (pixels.x < 2f || pixels.y < 2f)
            {
                return;
            }

            if (rig.Resize((int)pixels.x, (int)pixels.y) || force)
            {
                video.texture = rig.Texture;
                if (ghostNear != null)
                {
                    ghostNear.texture = rig.Texture;
                }

                if (ghostFar != null)
                {
                    ghostFar.texture = rig.Texture;
                }
            }

            UpdateScreenTiling(pixels);
        }

        private void UpdateScreenTiling(Vector2 pixels)
        {
            if (scanlines != null)
            {
                scanlines.uvRect = new Rect(0f, 0f, 1f, pixels.y / 4f);
            }
        }

        // ---- 입력 ----

        private void HandleKeyboard()
        {
            if (state == PlayState.Exiting || list.Count == 0 || !IsTopWindow())
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                StepSelection(-1);
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                StepSelection(1);
            }
        }

        private bool IsTopWindow()
        {
            var top = PopupWindowSorting.Top;
            return top == null || top == GetComponent<Canvas>();
        }

        // ---- 종료 ----

        private void TickExit(float deltaSeconds)
        {
            exitClock += deltaSeconds;
            var frame = CoreCctvTimeline.Exit(exitClock);
            var open = Mathf.Min(exitStartOpen, frame.Open);
            var line = Mathf.Min(exitStartLine, frame.LineWidth);
            ApplyOpen(open, line);
            SetGroupAlpha(bodyGroup, Mathf.Min(exitStartBody, frame.Content));
            SetGroupAlpha(titleGroup, Mathf.Min(exitStartTitle, frame.Content));
            if (windowGroup != null)
            {
                windowGroup.alpha = frame.Alpha;
            }

            // 닫히는 동안 영상도 어두워진다.
            if (video != null)
            {
                video.color = new Color(frame.Content, frame.Content, frame.Content, videoLevel * frame.Content);
            }

            if (panelGlow != null)
            {
                panelGlow.color = WithAlpha(Cyan, 0.12f * open);
            }

            if (flashGlow != null)
            {
                flashGlow.color = WithAlpha(Cyan, 0.45f * frame.Glint);
                var rect = flashGlow.rectTransform;
                rect.sizeDelta = new Vector2(WindowWidth() * Mathf.Max(0.15f, line) * 0.9f, 60f);
            }

            if (openLine != null)
            {
                var alpha = Mathf.Clamp01(1f - open * 4f);
                openLine.color = WithAlpha(Color.Lerp(Cyan, Color.white, frame.Glint), alpha);
            }

            ClearGhosts();
            if (exitClock >= CoreCctvTimeline.ExitDuration)
            {
                FinishExit();
            }
        }

        private void FinishExit()
        {
            state = PlayState.Hidden;
            showRequested = false;
            StopVideoImmediate();
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        // ---- 창 ----

        private float WindowWidth()
        {
            return ((RectTransform)transform).rect.width;
        }

        private float WindowHeight()
        {
            return ((RectTransform)transform).rect.height;
        }

        private void ApplyWindow(CoreCctvWindowFrame frame)
        {
            ApplyOpen(frame.Open, frame.LineWidth);
            SetGroupAlpha(titleGroup, frame.TitleAlpha);
            SetGroupAlpha(screenGroup, frame.ScreenAlpha);
            SetGroupAlpha(bodyGroup, 1f);
            lastBody = 1f;
            lastTitle = frame.TitleAlpha;
            SetEdgeEffects(frame.Flash, frame.EdgeNoise);

            if (openLine != null)
            {
                var fade = 1f - Mathf.Clamp01((frame.Open - 0.1f) / 0.5f);
                openLine.color = WithAlpha(Color.Lerp(Cyan, Color.white, frame.Flash), fade * Mathf.Clamp01(frame.LineWidth * 4f));
            }

            if (panelGlow != null)
            {
                panelGlow.color = WithAlpha(Cyan, 0.12f * frame.Open);
            }
        }

        /// <summary>
        /// 프레임(배경)만 가로선 → 세로 확장으로 펼치고, 내부 요소는 크기를 바꾸지 않은 채 같은 영역으로 잘라 보여 준다.
        /// 글자와 아이콘이 찌그러지지 않는다.
        /// </summary>
        private void ApplyOpen(float open, float lineWidth)
        {
            lastOpen = open;
            lastLine = lineWidth;
            var width = WindowWidth();
            var height = WindowHeight();
            if (panelBackdrop != null)
            {
                panelBackdrop.localScale = new Vector3(Mathf.Max(0.0001f, lineWidth), Mathf.Max(0.0001f, open), 1f);
            }

            SetGroupAlpha(panelBackdropGroup, Mathf.Clamp01(open * 4f));
            if (contentClip != null)
            {
                contentClip.sizeDelta = new Vector2(width * lineWidth, height * open);
            }

            if (openLine != null)
            {
                openLine.rectTransform.sizeDelta = new Vector2(width * lineWidth, OpenLineHeight);
            }
        }

        private void SetEdgeEffects(float flash, float edge)
        {
            if (flashGlow != null)
            {
                flashGlow.color = WithAlpha(Cyan, 0.4f * flash);
                flashGlow.rectTransform.sizeDelta = new Vector2(WindowWidth() * 0.9f, WindowHeight() * 0.55f);
            }

            if (edgeNoise != null)
            {
                for (var i = 0; i < edgeNoise.Length; i++)
                {
                    if (edgeNoise[i] != null)
                    {
                        edgeNoise[i].color = new Color(0.45f, 0.95f, 1f, edge);
                    }
                }
            }
        }

        private void TickNoise(float deltaSeconds)
        {
            noiseTimer += deltaSeconds;
            if (noiseTimer < NoiseStepSeconds)
            {
                return;
            }

            noiseTimer -= NoiseStepSeconds;
            noiseStep++;
            if (noise != null && noise.color.a > 0.001f)
            {
                noise.uvRect = RandomUv(noiseStep, 3f, 2f);
            }

            if (edgeNoise != null)
            {
                for (var i = 0; i < edgeNoise.Length; i++)
                {
                    if (edgeNoise[i] != null && edgeNoise[i].color.a > 0.001f)
                    {
                        edgeNoise[i].uvRect = RandomUv(noiseStep + i * 5, 2f, 0.5f);
                    }
                }
            }
        }

        private static Rect RandomUv(int seed, float width, float height)
        {
            return new Rect(
                FacilityServicePopupTimeline.Hash01(seed * 13 + 1),
                FacilityServicePopupTimeline.Hash01(seed * 29 + 7),
                width,
                height);
        }

        // ---- 초기화 ----

        private void ResetRuntimeState()
        {
            clock = 0f;
            exitClock = 0f;
            revealClock = 0f;
            videoClock = 0f;
            emptyClock = 0f;
            introEndTime = 0f;
            revealStarted = false;
            decided = false;
            connectedPhase = false;
            terminalLatched = false;
            cameraTargetId = string.Empty;
            noiseTimer = 0f;
            barStep = -1;
            appearAt.Clear();
            activeItemCount = 0;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].gameObject.SetActive(false);
                }
            }

            for (var i = 0; i < terminalTexts.Length; i++)
            {
                terminalTexts[i] = string.Empty;
                terminalLengths[i] = 0;
            }

            SetTerminalLine(0, "> 코어 단말 접속");
            SetTerminalLine(1, "> 연결된 시설 조회");
            SetTerminalLine(2, string.Empty);
            SetTerminalLine(3, string.Empty);
        }

        private void ResetVisuals()
        {
            lastOpen = 0f;
            lastLine = 0f;
            lastBody = 1f;
            lastTitle = 0f;
            ApplyOpen(0f, 0f);
            SetGroupAlpha(titleGroup, 0f);
            SetGroupAlpha(screenGroup, 0f);
            SetGroupAlpha(bodyGroup, 1f);
            SetGroupAlpha(listGroup, 0f);
            SetGroupAlpha(terminalGroup, 0f);
            SetEdgeEffects(0f, 0f);
            if (openLine != null)
            {
                openLine.color = WithAlpha(Cyan, 0f);
            }

            if (panelGlow != null)
            {
                panelGlow.color = WithAlpha(Cyan, 0f);
            }

            if (connectedText != null)
            {
                connectedText.alpha = 0f;
            }

            if (emptyLabel != null)
            {
                emptyLabel.alpha = 0f;
            }

            if (recDot != null)
            {
                recDot.color = RecRed;
            }

            ApplyVideoFrame(0f, 0f, 0f, 0f, 0f);
            ClearGhosts();
        }

        private void EnsureArt()
        {
            SetSprite(flashGlow, CoreCctvArt.Soft());
            SetSprite(panelGlow, CoreCctvArt.Soft());
            SetSprite(vignette, CoreCctvArt.Vignette());
            SetSprite(recDot, CoreCctvArt.Dot());
            SetTexture(hexPattern, CoreCctvArt.Hex());
            SetTexture(scanlines, CoreCctvArt.Scanline());
            SetTexture(noise, CoreCctvArt.Noise());
            if (edgeNoise != null)
            {
                for (var i = 0; i < edgeNoise.Length; i++)
                {
                    SetTexture(edgeNoise[i], CoreCctvArt.Noise());
                }
            }

            if (borderPanels != null)
            {
                for (var i = 0; i < borderPanels.Length; i++)
                {
                    SetSprite(borderPanels[i], CoreCctvArt.Border());
                }
            }

            if (hexPattern != null && hexPattern.uvRect.width <= 1f)
            {
                // 격자 크기가 화면 크기와 무관하게 일정하도록 창 크기에 맞춰 반복 횟수를 정한다.
                hexPattern.uvRect = new Rect(0f, 0f, WindowWidth() / 90f, WindowHeight() / 51.96f);
            }
        }

        private static void SetSprite(Image image, Sprite sprite)
        {
            if (image != null && image.sprite == null)
            {
                image.sprite = sprite;
            }
        }

        private static void SetTexture(RawImage image, Texture texture)
        {
            if (image != null && image.texture == null)
            {
                image.texture = texture;
            }
        }

        private static void SetGroupAlpha(CanvasGroup group, float alpha)
        {
            if (group != null)
            {
                group.alpha = alpha;
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
