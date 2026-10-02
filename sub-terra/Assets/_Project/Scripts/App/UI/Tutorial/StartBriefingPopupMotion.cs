using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 시작 브리핑 팝업 연출: 화면 글리치 → 신호 집결·확산 → 프레임 개방 → 본문 페이드인,
    /// 표시 중 약한 가장자리 간섭, 닫을 때 프레임 축소·신호 수축·화면 글리치.
    /// 글리치는 프레임/장식 레이어에만 적용하고 본문(content)은 알파만 바꾼다.
    /// 모든 시간은 비스케일이라 정지(UiPauseGate) 중에도 재생된다.
    /// </summary>
    public sealed class StartBriefingPopupMotion : MonoBehaviour
    {
        public const string PauseOwner = "start-briefing";

        private static readonly Color Cyan = new Color(0.20f, 1f, 0.96f, 1f);
        private static readonly Color DimFrame = new Color(0.22f, 0.32f, 0.36f, 1f);

        [Header("Layout")]
        [SerializeField] private RectTransform window;
        [SerializeField] private Vector2 windowSize = new Vector2(1700f, 850f);
        [SerializeField] private Vector2 frameHalfExtent = new Vector2(783f, 311f);
        [SerializeField] private float backdropAlpha = 0.78f;

        [Header("Frame layers (글리치 적용 대상)")]
        [SerializeField] private RectTransform frame;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image frameGlow;
        [SerializeField] private Image[] ghostFrames;
        [SerializeField] private Image[] edgeBars;

        [Header("Signal")]
        [SerializeField] private Image signalLine;
        [SerializeField] private Image[] signalMotes;

        [Header("Screen glitch")]
        [SerializeField] private RectTransform screenGlitchRoot;
        [SerializeField] private Image[] screenBars;
        [SerializeField] private RawImage[] tearSlices;

        [Header("Stable layers")]
        [SerializeField] private Image backdrop;
        [SerializeField] private CanvasGroup content;
        [SerializeField] private CanvasGroup rootGroup;

        private struct EdgeState
        {
            public int Edge;
            public float Position;
            public float Speed;
            public float Length;
            public float Lateral;
            public float Seed;
        }

        private readonly BriefingLifecycle lifecycle = new BriefingLifecycle();
        private BriefingGlitchPlanner planner;
        private EdgeState[] edges;
        private float[] burstAlpha;
        private Vector2[] moteStart;
        private Action onClosed;
        private RenderTexture snapshot;
        private bool captureStarted;
        private bool openGlitchDone;
        private bool holdsPause;
        private int activeBurst = -1;
        private float phaseTime;
        private float clock;
        private float glitchTimer;
        private float ghostUntil;
        private Vector2 ghostShift;
        private float frameShiftUntil;
        private float frameShiftX;
        private float flickerUntil;
        private float flickerNextRoll;
        private float flickerLevel = 1f;
        private float burstUntil;
        private Vector2 lastRootSize;

        public BriefingPhase Phase => lifecycle.Phase;
        public bool IsClosing => lifecycle.Phase == BriefingPhase.Closing;
        public bool IsShown => lifecycle.Phase == BriefingPhase.Shown;

        private void Awake()
        {
            var random = new System.Random(Environment.TickCount ^ GetHashCode());
            planner = new BriefingGlitchPlanner(() => (float)random.NextDouble());
        }

        private void OnEnable()
        {
            if (planner == null)
            {
                Awake();
            }

            BuildEdgeStates();
            ResetVisuals();
            lifecycle.Begin();
            captureStarted = false;
            openGlitchDone = false;
            phaseTime = 0f;
            clock = 0f;
            glitchTimer = 0f;
            FitWindow(true);
        }

        private void OnDisable()
        {
            // 화면 전환·비활성화 시 연출과 정지 소유권을 모두 정리한다. 종료 콜백은 부르지 않는다.
            lifecycle.Reset();
            onClosed = null;
            StopAllCoroutines();
            ReleaseSnapshot();
            ReleasePause();
            ResetVisuals();
        }

        /// <summary>닫기 연출을 시작한다. Shown 상태에서 한 번만 받아들이고 완료 뒤 콜백을 한 번 호출한다.</summary>
        public bool RequestClose(Action closed)
        {
            if (!lifecycle.TryBeginClose())
            {
                return false;
            }

            onClosed = closed;
            phaseTime = 0f;
            glitchTimer = 0f;
            TriggerGlitch(BriefingGlitchKind.FrameShift, 1f);
            TriggerGlitch(BriefingGlitchKind.EdgeBurst, 1f);
            // 선택이 남은 버튼에 Enter/Submit이 다시 들어가 중복 종료되지 않게 한다.
            UiKeyboardSubmitGuard.ClearSelection();
            if (content != null)
            {
                content.interactable = false;
                content.blocksRaycasts = false;
            }

            return true;
        }

        private void Update()
        {
            // 화면 복사·씬 로드 직후의 긴 프레임에 연출이 건너뛰어지지 않도록 한 프레임 진행량을 제한한다.
            var dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            clock += dt;
            FitWindow(false);

            switch (lifecycle.Phase)
            {
                case BriefingPhase.Waiting:
                    if (!captureStarted && IsPresentable())
                    {
                        captureStarted = true;
                        StartCoroutine(CaptureThenStart());
                    }

                    break;
                case BriefingPhase.Intro:
                    phaseTime += dt;
                    UpdateIntro(phaseTime, dt);
                    if (phaseTime >= StartBriefingTimeline.IntroDuration)
                    {
                        FinishIntro();
                    }

                    break;
                case BriefingPhase.Shown:
                    phaseTime += dt;
                    UpdateFrameEffects(1f, 1f, StartBriefingTimeline.SustainIntensity, dt);
                    break;
                case BriefingPhase.Closing:
                    phaseTime += dt;
                    UpdateClosing(phaseTime, dt);
                    if (phaseTime >= StartBriefingTimeline.CloseDuration)
                    {
                        CompleteClose();
                    }

                    break;
            }
        }

        // 부모 HUD가 아직 가려져 있으면(월드 복원 대기) 연출을 시작하지 않는다.
        private bool IsPresentable()
        {
            for (var current = transform.parent; current != null; current = current.parent)
            {
                var group = current.GetComponent<CanvasGroup>();
                if (group != null && group.enabled && group.alpha < 0.99f)
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerator CaptureThenStart()
        {
            // 글리치 어긋남에 쓸 현재 화면을 오버레이가 그려지기 전에 GPU 복사로 확보한다.
            yield return new WaitForEndOfFrame();
            if (lifecycle.Phase != BriefingPhase.Waiting)
            {
                yield break;
            }

            try
            {
                if (tearSlices != null && tearSlices.Length > 0 && Screen.width > 0 && Screen.height > 0)
                {
                    snapshot = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32)
                    {
                        wrapMode = TextureWrapMode.Repeat
                    };
                    ScreenCapture.CaptureScreenshotIntoRenderTexture(snapshot);
                }
            }
            catch (Exception)
            {
                ReleaseSnapshot();
            }

            if (lifecycle.TryStartIntro())
            {
                phaseTime = 0f;
                UiPauseGate.Acquire(PauseOwner);
                holdsPause = true;
            }
        }

        private void FinishIntro()
        {
            lifecycle.TryFinishIntro();
            // 정지된 배경 복사본을 닫기 화면 글리치에도 재사용한다.
            HideScreenGlitch();
            if (content != null)
            {
                content.alpha = 1f;
                content.interactable = true;
                content.blocksRaycasts = true;
            }

            ApplyBackdrop(1f);
            ApplySignal(StartBriefingTimeline.IntroDuration);
        }

        private void CompleteClose()
        {
            if (!lifecycle.TryCompleteClose())
            {
                return;
            }

            HideScreenGlitch();
            ReleaseSnapshot();
            ReleasePause();
            var callback = onClosed;
            onClosed = null;
            try
            {
                if (callback != null)
                {
                    callback();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[SubTerra] Start briefing close callback failed: " + ex);
            }

            // 종료 처리가 패널을 끄지 않았더라도 투명한 입력 차단막이 남지 않게 한다.
            if (this != null && gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void ReleasePause()
        {
            if (!holdsPause)
            {
                return;
            }

            holdsPause = false;
            UiPauseGate.Release(PauseOwner);
        }

        private void ReleaseSnapshot()
        {
            if (snapshot == null)
            {
                return;
            }

            snapshot.Release();
            Destroy(snapshot);
            snapshot = null;
        }

        // ── 등장 ────────────────────────────────────────────────────────

        private void UpdateIntro(float t, float dt)
        {
            ApplyBackdrop(StartBriefingTimeline.BackdropAmount(t));
            UpdateScreenBursts(t);
            ApplySignal(t);

            var open = StartBriefingTimeline.FrameOpen(t);
            if (!openGlitchDone && t >= StartBriefingTimeline.FrameOpenStart)
            {
                // 프레임이 열리는 순간 가장자리에 확실한 글리치를 한 번 넣는다.
                openGlitchDone = true;
                TriggerGlitch(BriefingGlitchKind.FrameShift, 1f);
                TriggerGlitch(BriefingGlitchKind.EdgeBurst, 1f);
            }

            if (content != null)
            {
                content.alpha = StartBriefingTimeline.ContentAlphaIntro(t);
            }

            var intensity = t >= StartBriefingTimeline.FrameOpenStart
                ? StartBriefingTimeline.IntroIntensity(t)
                : 0f;
            UpdateFrameEffects(open, 1f, intensity, dt);
        }

        private void UpdateScreenBursts(float t)
        {
            var burst = StartBriefingTimeline.ActiveBurst(t);
            if (burst == activeBurst)
            {
                return;
            }

            activeBurst = burst;
            if (burst < 0)
            {
                HideScreenGlitch();
                return;
            }

            RerollScreenBurst();
        }

        private void RerollScreenBurst()
        {
            if (screenGlitchRoot == null)
            {
                return;
            }

            screenGlitchRoot.gameObject.SetActive(true);
            var width = Mathf.Max(1f, screenGlitchRoot.rect.width);
            var height = Mathf.Max(1f, screenGlitchRoot.rect.height);

            var barCount = screenBars != null ? screenBars.Length : 0;
            var visibleBars = Mathf.Min(barCount, Mathf.RoundToInt(planner.Range(5f, 8.4f)));
            for (var i = 0; i < barCount; i++)
            {
                var bar = screenBars[i];
                if (bar == null)
                {
                    continue;
                }

                bar.gameObject.SetActive(i < visibleBars);
                if (i >= visibleBars)
                {
                    continue;
                }

                var barWidth = width * planner.Range(0.25f, 1f);
                var rect = bar.rectTransform;
                rect.sizeDelta = new Vector2(barWidth, planner.Range(2f, 9f));
                rect.anchoredPosition = new Vector2(
                    planner.Range(-(width - barWidth) * 0.5f, (width - barWidth) * 0.5f),
                    planner.Range(-height * 0.5f, height * 0.5f));
                var tint = planner.Range(0f, 1f) < 0.35f ? Color.white : Cyan;
                tint.a = planner.Range(0.30f, 0.70f);
                bar.color = tint;
            }

            var sliceCount = tearSlices != null ? tearSlices.Length : 0;
            for (var i = 0; i < sliceCount; i++)
            {
                var slice = tearSlices[i];
                if (slice == null)
                {
                    continue;
                }

                var bandHeight = height * planner.Range(0.03f, 0.12f);
                var centerY = planner.Range(-height * 0.5f + bandHeight * 0.5f, height * 0.5f - bandHeight * 0.5f);
                var shift = planner.Range(0.02f, 0.08f) * (planner.Range(0f, 1f) < 0.5f ? -1f : 1f);
                var rect = slice.rectTransform;
                rect.sizeDelta = new Vector2(width, bandHeight);
                rect.anchoredPosition = new Vector2(0f, centerY);
                slice.gameObject.SetActive(true);
                if (snapshot != null)
                {
                    var v = (centerY - bandHeight * 0.5f + height * 0.5f) / height;
                    var v1 = bandHeight / height;
                    // 화면 복사는 UV 원점이 위인 그래픽스 API(D3D 등)에서 위아래가 뒤집혀 나온다.
                    if (SystemInfo.graphicsUVStartsAtTop)
                    {
                        v = 1f - v - v1;
                    }

                    slice.texture = snapshot;
                    slice.color = Color.white;
                    slice.uvRect = new Rect(shift, v, 1f, v1);
                }
                else
                {
                    // 화면 복사가 없으면 청록 띠로 어긋남을 대신한다.
                    slice.texture = null;
                    slice.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.14f);
                }
            }
        }

        private void HideScreenGlitch()
        {
            activeBurst = -1;
            if (screenGlitchRoot != null)
            {
                screenGlitchRoot.gameObject.SetActive(false);
            }
        }

        private void ApplyBackdrop(float amount)
        {
            if (backdrop != null)
            {
                var color = backdrop.color;
                color.a = backdropAlpha * Mathf.Clamp01(amount);
                backdrop.color = color;
            }
        }

        private void ApplySignal(float t)
        {
            if (signalLine == null)
            {
                return;
            }

            var alpha = StartBriefingTimeline.SignalAlpha(t);
            var gather = StartBriefingTimeline.Gather(t);
            var spread = StartBriefingTimeline.Spread(t);
            signalLine.enabled = alpha > 0.001f;
            if (alpha <= 0.001f)
            {
                SetMotes(0f, 0f);
                return;
            }

            var size = Mathf.Lerp(6f, 16f, gather);
            var lineWidth = Mathf.Lerp(size, frameHalfExtent.x * 2f, spread);
            var lineHeight = Mathf.Lerp(size, 4f, spread);
            signalLine.rectTransform.sizeDelta = new Vector2(lineWidth, lineHeight);
            var color = Color.Lerp(Cyan, Color.white, spread * 0.6f);
            color.a = alpha;
            signalLine.color = color;
            SetMotes(gather, alpha * (1f - spread));
        }

        private void SetMotes(float gather, float alpha)
        {
            if (signalMotes == null)
            {
                return;
            }

            for (var i = 0; i < signalMotes.Length; i++)
            {
                var mote = signalMotes[i];
                if (mote == null)
                {
                    continue;
                }

                mote.enabled = alpha > 0.001f && gather < 1f;
                if (!mote.enabled || moteStart == null || i >= moteStart.Length)
                {
                    continue;
                }

                var pull = gather * gather;
                mote.rectTransform.anchoredPosition = moteStart[i] * (1f - pull);
                mote.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, pull);
                var color = Cyan;
                color.a = alpha * Mathf.Clamp01(0.3f + gather);
                mote.color = color;
            }
        }

        // ── 닫기 ────────────────────────────────────────────────────────

        private void UpdateClosing(float t, float dt)
        {
            ApplyBackdrop(1f - StartBriefingTimeline.Progress(t,
                StartBriefingTimeline.CloseContentFadeEnd, StartBriefingTimeline.CloseSignalEnd));
            if (t >= StartBriefingTimeline.CloseGlitchStart)
            {
                UpdateScreenBursts(StartBriefingTimeline.CloseScreenTime(t));
            }

            if (t >= StartBriefingTimeline.CloseFrameEnd && t < StartBriefingTimeline.CloseSignalEnd)
            {
                ApplySignal(StartBriefingTimeline.CloseSignalTime(t));
            }
            else
            {
                ApplySignal(StartBriefingTimeline.IntroDuration);
            }

            if (content != null)
            {
                content.alpha = StartBriefingTimeline.ContentAlphaClose(t);
            }

            if (rootGroup != null)
            {
                rootGroup.alpha = StartBriefingTimeline.CloseOverallAlpha(t);
            }

            UpdateFrameEffects(
                StartBriefingTimeline.CloseFrameOpen(t),
                StartBriefingTimeline.CloseLight(t),
                StartBriefingTimeline.CloseIntensity(t),
                dt);
        }

        // ── 프레임·장식 글리치 (본문에는 적용하지 않는다) ─────────────────

        private void UpdateFrameEffects(float open, float light, float intensity, float dt)
        {
            if (frame != null)
            {
                frame.localScale = new Vector3(1f, Mathf.Max(0.01f, open), 1f);
                frame.anchoredPosition = new Vector2(clock < frameShiftUntil ? frameShiftX : 0f, 0f);
            }

            if (frameImage != null)
            {
                frameImage.enabled = open > 0.001f;
                frameImage.color = Color.Lerp(DimFrame, Color.white, light);
            }

            if (intensity > 0.001f)
            {
                glitchTimer -= dt;
                if (glitchTimer <= 0f)
                {
                    TriggerGlitch(planner.PickKind(), intensity);
                    if (intensity > 0.6f && planner.Range(0f, 1f) < 0.5f)
                    {
                        TriggerGlitch(planner.PickKind(), intensity);
                    }

                    glitchTimer = planner.NextDelay(intensity);
                }
            }
            else
            {
                glitchTimer = 0f;
            }

            ApplyGlow(open, light, intensity);
            ApplyGhosts(open, light, intensity);
            UpdateEdgeBars(open, light, intensity, dt);
        }

        private void TriggerGlitch(BriefingGlitchKind kind, float intensity)
        {
            var strength = 0.5f + 0.5f * Mathf.Clamp01(intensity);
            switch (kind)
            {
                case BriefingGlitchKind.FrameShift:
                    frameShiftUntil = clock + planner.Range(0.04f, 0.10f);
                    frameShiftX = planner.Range(2f, 7f) * strength * (planner.Range(0f, 1f) < 0.5f ? -1f : 1f);
                    ghostUntil = frameShiftUntil + 0.03f;
                    ghostShift = new Vector2(
                        planner.Range(5f, 14f) * strength * (planner.Range(0f, 1f) < 0.5f ? -1f : 1f),
                        planner.Range(-3f, 3f));
                    break;
                case BriefingGlitchKind.EdgeBurst:
                    burstUntil = clock + planner.Range(0.06f, 0.16f);
                    if (burstAlpha != null)
                    {
                        for (var i = 0; i < burstAlpha.Length; i++)
                        {
                            var chosen = planner.Range(0f, 1f) < 0.5f;
                            burstAlpha[i] = chosen ? planner.Range(0.5f, 0.95f) * strength : 0f;
                            if (chosen && edges != null && i < edges.Length)
                            {
                                edges[i].Position = planner.Range(0f, 1f);
                            }
                        }
                    }

                    break;
                case BriefingGlitchKind.GlowFlicker:
                    flickerUntil = clock + planner.Range(0.08f, 0.22f);
                    flickerNextRoll = 0f;
                    break;
            }
        }

        private void ApplyGlow(float open, float light, float intensity)
        {
            if (frameGlow == null)
            {
                return;
            }

            if (clock < flickerUntil)
            {
                if (clock >= flickerNextRoll)
                {
                    flickerLevel = planner.Range(0f, 1f) < 0.5f ? planner.Range(0.1f, 0.4f) : planner.Range(1.5f, 2.2f);
                    flickerNextRoll = clock + 0.03f;
                }
            }
            else
            {
                flickerLevel = 1f;
            }

            var alpha = 0.26f * light * Mathf.Clamp01(open * 2f) * flickerLevel;
            frameGlow.enabled = open > 0.001f && alpha > 0.002f;
            var color = Cyan;
            color.a = Mathf.Clamp01(alpha);
            frameGlow.color = color;
            frameGlow.rectTransform.localScale = new Vector3(1.035f, 1.035f * Mathf.Max(0.01f, open), 1f);
        }

        private void ApplyGhosts(float open, float light, float intensity)
        {
            if (ghostFrames == null)
            {
                return;
            }

            var active = clock < ghostUntil && open > 0.001f;
            for (var i = 0; i < ghostFrames.Length; i++)
            {
                var ghost = ghostFrames[i];
                if (ghost == null)
                {
                    continue;
                }

                ghost.enabled = active;
                if (!active)
                {
                    continue;
                }

                var direction = i % 2 == 0 ? 1f : -1f;
                var rect = ghost.rectTransform;
                rect.anchoredPosition = new Vector2(ghostShift.x * direction, ghostShift.y * direction);
                rect.localScale = new Vector3(1f, Mathf.Max(0.01f, open), 1f);
                var color = i % 2 == 0 ? Cyan : Color.Lerp(Cyan, Color.white, 0.55f);
                color.a = 0.5f * light * Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(intensity));
                ghost.color = color;
            }
        }

        private void BuildEdgeStates()
        {
            var count = edgeBars != null ? edgeBars.Length : 0;
            edges = new EdgeState[count];
            burstAlpha = new float[count];
            for (var i = 0; i < count; i++)
            {
                edges[i] = new EdgeState
                {
                    Edge = i % 4,
                    Position = planner.Range(0f, 1f),
                    Speed = planner.Range(0.05f, 0.18f) * (planner.Range(0f, 1f) < 0.5f ? -1f : 1f),
                    Length = planner.Range(60f, 220f),
                    Lateral = planner.Range(-5f, 7f),
                    Seed = planner.Range(0f, 100f)
                };
            }

            var motes = signalMotes != null ? signalMotes.Length : 0;
            moteStart = new Vector2[motes];
            for (var i = 0; i < motes; i++)
            {
                var angle = planner.Range(0f, Mathf.PI * 2f);
                var radius = planner.Range(380f, 760f);
                moteStart[i] = new Vector2(Mathf.Cos(angle) * radius * 1.4f, Mathf.Sin(angle) * radius * 0.6f);
            }
        }

        private void UpdateEdgeBars(float open, float light, float intensity, float dt)
        {
            if (edgeBars == null || edges == null)
            {
                return;
            }

            var halfX = frameHalfExtent.x;
            var halfY = frameHalfExtent.y * Mathf.Max(0.01f, open);
            var flow = Mathf.Lerp(0.15f, 1f, Mathf.Clamp01(intensity)) * 0.85f;
            var bursting = clock < burstUntil;
            for (var i = 0; i < edgeBars.Length; i++)
            {
                var bar = edgeBars[i];
                if (bar == null)
                {
                    continue;
                }

                ref var state = ref edges[i];
                state.Position += state.Speed * dt;
                if (state.Position > 1f || state.Position < 0f)
                {
                    state.Position -= Mathf.Floor(state.Position);
                    state.Lateral = planner.Range(-5f, 7f);
                }

                // 퍼린 노이즈를 문턱값으로 잘라 불규칙하게 나타났다 사라지는 간섭을 만든다.
                var noise = Mathf.PerlinNoise(state.Seed, clock * 2.2f);
                var alpha = Mathf.Pow(Mathf.Clamp01((noise - 0.5f) * 2.4f), 1.4f) * 0.55f * flow;
                var color = Cyan;
                if (bursting && burstAlpha[i] > 0f)
                {
                    alpha = Mathf.Max(alpha, burstAlpha[i]);
                    color = Color.Lerp(Cyan, Color.white, 0.5f);
                }

                alpha *= light * Mathf.Clamp01(open * 3f);
                bar.enabled = alpha > 0.004f;
                if (!bar.enabled)
                {
                    continue;
                }

                var rect = bar.rectTransform;
                if (state.Edge < 2)
                {
                    rect.sizeDelta = new Vector2(state.Length, 3f);
                    rect.anchoredPosition = new Vector2(
                        Mathf.Lerp(-halfX, halfX, state.Position),
                        (state.Edge == 0 ? halfY : -halfY) + state.Lateral);
                }
                else
                {
                    rect.sizeDelta = new Vector2(3f, state.Length * 0.5f);
                    rect.anchoredPosition = new Vector2(
                        (state.Edge == 2 ? -halfX : halfX) + state.Lateral,
                        Mathf.Lerp(-halfY, halfY, state.Position));
                }

                color.a = Mathf.Clamp01(alpha);
                bar.color = color;
            }
        }

        // ── 초기화·배치 ──────────────────────────────────────────────────

        private void ResetVisuals()
        {
            activeBurst = -1;
            ghostUntil = 0f;
            frameShiftUntil = 0f;
            flickerUntil = 0f;
            burstUntil = 0f;
            flickerLevel = 1f;
            if (rootGroup != null)
            {
                rootGroup.alpha = 1f;
            }

            ApplyBackdrop(0f);
            if (content != null)
            {
                content.alpha = 0f;
                content.interactable = false;
                content.blocksRaycasts = false;
            }

            if (frame != null)
            {
                frame.localScale = new Vector3(1f, 0.01f, 1f);
                frame.anchoredPosition = Vector2.zero;
            }

            if (frameImage != null)
            {
                frameImage.enabled = false;
                frameImage.color = Color.white;
            }

            SetEnabled(frameGlow, false);
            SetEnabled(ghostFrames, false);
            SetEnabled(edgeBars, false);
            SetEnabled(signalLine, false);
            SetEnabled(signalMotes, false);
            if (screenGlitchRoot != null)
            {
                screenGlitchRoot.gameObject.SetActive(false);
            }
        }

        private static void SetEnabled(Image image, bool enabled)
        {
            if (image != null)
            {
                image.enabled = enabled;
            }
        }

        private static void SetEnabled(Image[] images, bool enabled)
        {
            if (images == null)
            {
                return;
            }

            for (var i = 0; i < images.Length; i++)
            {
                SetEnabled(images[i], enabled);
            }
        }

        // 화면 비율이 달라져도 프레임·효과가 같은 기준으로 맞도록 창 전체를 한 번에 스케일한다.
        private void FitWindow(bool force)
        {
            var rootRect = transform as RectTransform;
            if (rootRect == null || window == null)
            {
                return;
            }

            var size = rootRect.rect.size;
            if (!force && size == lastRootSize)
            {
                return;
            }

            lastRootSize = size;
            if (size.x <= 1f || size.y <= 1f)
            {
                return;
            }

            var scale = Mathf.Min(1f, size.x * 0.94f / windowSize.x, size.y * 0.92f / windowSize.y);
            window.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
