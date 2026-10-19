using System;
using System.Collections.Generic;
using System.Linq;
using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.UI.RunFailure;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.UI.Drone
{
    /// <summary>
    /// Digger-Bot 창: 템플릿 대사와 (통합된) 추천 행동·근거를 함께 표시한다.
    /// 드론 머리 위 말풍선보다 큰 글자로 같은 대사를 하단 중앙에 보여 준다.
    /// prompt-B 149: 가로선 펼침 열기, 타이핑, 근거 순차 등장, PRIORITY 글리치, 접힘 닫기.
    /// </summary>
    public sealed class DroneDialoguePanelView : MonoBehaviour,
        IDroneDialogueView,
        IDroneReasonView,
        IDroneOperationalStateView
    {
        /// <summary>하단 digger-bot 창 대사 기본 글자 크기.</summary>
        public const float PanelDialogueFontSize = 20f;
        public const string HeaderPrefix = "DIGGER-BOT  //  AI ANALYSIS  //  ";
        public const string HeaderCursor = "_";

        private const int AllCharacters = 99999;
        private const float OpenLineThickness = 10f;
        private const float ScanSweepHeight = 28f;
        private const float GlitchBandHeight = 16f;
        private const float GlitchEchoHeight = 6f;
        private const float GlitchMaxOffset = 18f;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private Button closeButton;
        [SerializeField] private DroneTerminalFxSettings terminalFx = new DroneTerminalFxSettings();

        private bool terminalSkinApplied;
        private DroneOperationalState operationalState = DroneOperationalState.Idle;
        private TMP_Text speakerText;
        private Image headerBar;

        private readonly DroneTerminalChangeDetector changeDetector = new DroneTerminalChangeDetector();
        private DroneTerminalWindowState window;
        private List<string> reasonLines = new List<string>();
        private Action closeCallback;
        private Image openLine;
        private Image scanSweep;
        private Image glitchBand;
        private Image glitchEcho;

        private bool revealActive;
        private bool revealPlanDirty;
        private bool revealIncludesHeader;
        private DroneTerminalTypingPlan revealPlan;
        private float revealClock;
        private float cursorHeaderEnd;
        private float cursorRevealEnd;

        private bool appendActive;
        private int appendFrom;
        private int appendTo;
        private float appendClock;

        private bool priorityActive;
        private float priorityClock;
        private float appliedContentAlpha = 1f;

        private static readonly Color TerminalBackground = new Color(0.012f, 0.035f, 0.055f, 0.975f);
        private static readonly Color TerminalHeader = new Color(0.018f, 0.12f, 0.16f, 0.99f);
        private static readonly Color AccentCyan = new Color(0.18f, 0.84f, 0.92f, 1f);
        private static readonly Color AlertAmber = new Color(1f, 0.66f, 0.16f, 1f);
        private static readonly Color PriorityFlashText = new Color(1f, 0.96f, 0.86f, 1f);

        /// <summary>닫기(X) 버튼. HudPanelChromeController가 배선한다.</summary>
        public Button CloseButton => closeButton;

        /// <summary>열기·닫기 연출 상태. 연출을 쓰지 않는 환경에서는 Hidden에 머문다.</summary>
        public DroneTerminalPhase TerminalPhase => window != null ? window.Phase : DroneTerminalPhase.Hidden;
        public bool IsTerminalRevealing => revealActive || appendActive;

        private void Awake()
        {
            ApplyTerminalSkin();
        }

        private void OnDisable()
        {
            // 외부에서 창이 꺼지면 연출을 멈추고 정상 표시 값으로 되돌린다.
            if (window == null || window.Phase == DroneTerminalPhase.Hidden)
            {
                return;
            }

            // 비활성화 도중에는 SetActive를 부를 수 없으므로 연출 오브젝트는 다음 프레임 적용 때 정리된다.
            window.HideImmediate();
            closeCallback = null;
            ResetTerminalFx(false);
        }

        private void Update()
        {
            if (window == null || window.Phase == DroneTerminalPhase.Hidden)
            {
                return;
            }

            if (window.Phase != DroneTerminalPhase.Closing && WasSkipRequested())
            {
                SkipTerminalReveal();
            }

            var deltaTime = Time.unscaledDeltaTime;
            if (window.Tick(deltaTime))
            {
                ResetTerminalFx();
                FinishAnimatedClose();
                return;
            }

            if (window.Phase != DroneTerminalPhase.Closing)
            {
                if (revealActive && !revealPlanDirty)
                {
                    revealClock += deltaTime;
                }

                if (appendActive)
                {
                    appendClock += deltaTime;
                }
            }

            if (priorityActive)
            {
                priorityClock += deltaTime;
            }

            BuildRevealPlanIfNeeded();
            ApplyTerminalFrame();
        }

        public bool IsVisible
        {
            get
            {
                var root = panelRoot != null ? panelRoot : gameObject;
                return root != null && root.activeSelf;
            }
        }

        public void SetDialogue(DroneDialogueResult dialogue)
        {
            // 쿨다운으로 억제된 결과는 기존 문구를 유지한다(창을 열었을 때 빈 칸 방지).
            if (dialogueText == null || dialogue == null || dialogue.IsSuppressed)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(dialogue.Text))
            {
                return;
            }

            ApplyTerminalSkin();
            // 같은 추천에서 바뀐 대사는 타이핑 없이 조용히 교체한다.
            SetTextIfChanged(dialogueText, dialogue.Text);
            // 창 제목과 같은 크기의 본문 글자 크기를 유지한다.
            dialogueText.fontSize = PanelDialogueFontSize;
        }

        public void SetAnalysis(DroneAnalysisResult analysis)
        {
            if (analysis == null)
            {
                return;
            }

            // 점수는 화면에 보이지 않는다. 근거 줄에는 실제 수치·단위를 붙인다.
            var lines = DroneReasonDisplayFormatter.FormatReasonLines(analysis.Recommendation.Reasons);
            var change = changeDetector.ObserveAnalysis(analysis.RecommendedAction, lines.Count);
            reasonLines = lines;
            SetTextIfChanged(actionText, DroneReasonDisplayFormatter.FormatAction(analysis.RecommendedAction));
            SetTextIfChanged(reasonText, DroneReasonDisplayFormatter.JoinLines(lines));

            if (!IsTerminalAnimating() || window.Phase == DroneTerminalPhase.Closing || revealActive)
            {
                return;
            }

            if (change.RecommendationChanged)
            {
                BeginReveal(false);
            }
            else if (change.ReasonLinesAdded)
            {
                // 같은 추천에서 근거 줄만 늘면 기존 줄은 두고 새 줄만 차례로 보인다.
                var visibleNow = appendActive
                    ? DroneTerminalTimeline.AppendedReasonLines(
                        appendFrom, appendTo, appendClock, terminalFx.reasonLineInterval)
                    : change.PreviousReasonLineCount;
                appendActive = true;
                appendFrom = Mathf.Min(visibleNow, change.PreviousReasonLineCount);
                appendTo = change.ReasonLineCount;
                appendClock = 0f;
                ApplyTerminalFrame();
            }
        }

        public void SetVisible(bool visible)
        {
            ApplyTerminalSkin();
            var root = panelRoot != null ? panelRoot : gameObject;
            closeCallback = null;
            if (!visible)
            {
                if (window != null)
                {
                    window.HideImmediate();
                }

                ResetTerminalFx();
                root.SetActive(false);
                return;
            }

            root.SetActive(true);
            if (!CanAnimateTerminal())
            {
                ResetTerminalFx();
                return;
            }

            EnsureTerminalState();
            if (window.Phase == DroneTerminalPhase.Opening || window.Phase == DroneTerminalPhase.Shown)
            {
                return;
            }

            // 숨김 상태면 처음부터, 닫히던 중이면 접힌 정도에서 다시 펼친다.
            window.Open();
            EnsureTerminalFx();
            BeginReveal(true);
        }

        /// <summary>
        /// 접힘 닫기 연출을 시작한다. 연출을 쓸 수 없으면 false를 돌려주고 호출자가 즉시 닫는다.
        /// 연출이 끝나면 PanelRoot를 끄고 onClosed를 부른다.
        /// </summary>
        public bool TryBeginAnimatedClose(Action onClosed)
        {
            if (!CanAnimateTerminal() || window == null || window.Phase == DroneTerminalPhase.Hidden)
            {
                return false;
            }

            closeCallback = onClosed;
            window.Close();
            ApplyTerminalFrame();
            return true;
        }

        public bool HasRequiredReferences()
        {
            // 추천 텍스트는 통합 레이아웃에서 주입되며, 없으면 대사만 표시해도 동작한다.
            return panelRoot != null && dialogueText != null;
        }

        public bool HasIntegratedReasonTexts()
        {
            return actionText != null && reasonText != null;
        }

        public void SetOperationalState(DroneOperationalState state)
        {
            operationalState = state;
            var enteredPriority = changeDetector.ObserveState(state);
            ApplyHeaderState();
            // PRIORITY로 들어온 순간에만 한 번 깜빡이고 글리치가 지나간다.
            if (enteredPriority
                && IsTerminalAnimating()
                && window.Phase != DroneTerminalPhase.Closing)
            {
                priorityActive = true;
                priorityClock = 0f;
                ApplyTerminalFrame();
            }
        }

        private void ApplyTerminalSkin()
        {
            if (terminalSkinApplied)
            {
                return;
            }

            var root = panelRoot != null ? panelRoot : gameObject;
            var panelRect = root.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }

            var background = root.GetComponent<Image>();
            if (background == null)
            {
                background = root.AddComponent<Image>();
            }

            background.color = TerminalBackground;
            background.raycastTarget = false;

            var outline = root.GetComponent<Outline>();
            if (outline == null)
            {
                outline = root.AddComponent<Outline>();
            }

            outline.effectColor = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.72f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;

            var shadow = root.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = root.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(3f, -4f);
            shadow.useGraphicAlpha = true;

            var signalLine = FindOrCreateImage("TerminalSignalLine", panelRect);
            SetAnchors(signalLine.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(14f, -3f), new Vector2(-14f, 0f));
            signalLine.color = AccentCyan;
            signalLine.transform.SetAsFirstSibling();

            var header = FindOrCreateImage("TerminalHeader", panelRect);
            SetAnchors(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -36f), Vector2.zero);
            header.color = TerminalHeader;
            header.transform.SetAsFirstSibling();
            headerBar = header;

            speakerText = root.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(text => text.name == "SpeakerText");
            if (speakerText != null)
            {
                speakerText.fontSize = 18f;
                speakerText.fontStyle = FontStyles.Bold;
                speakerText.alignment = TextAlignmentOptions.MidlineLeft;
                SetAnchors(speakerText.rectTransform, new Vector2(0f, 1f), new Vector2(0.82f, 1f),
                    new Vector2(18f, -34f), new Vector2(0f, -2f));
            }

            StyleText(dialogueText, new Color(0.92f, 0.98f, 1f, 1f));
            StyleText(actionText, new Color(0.34f, 0.94f, 0.84f, 1f));
            StyleText(reasonText, new Color(0.7f, 0.86f, 0.9f, 1f));
            StyleCloseButton(header.rectTransform);
            terminalSkinApplied = true;
            ApplyHeaderState();
        }

        private void ApplyHeaderState()
        {
            if (speakerText == null)
            {
                return;
            }

            var label = operationalState switch
            {
                DroneOperationalState.Scanning => "SCANNING",
                DroneOperationalState.Priority => "PRIORITY",
                _ => "IDLE"
            };
            SetTextIfChanged(speakerText, HeaderPrefix + label + HeaderCursor);
            speakerText.color = HeaderTextColor();
        }

        private Color HeaderTextColor()
        {
            return operationalState == DroneOperationalState.Priority
                ? AlertAmber
                : AccentCyan;
        }

        private bool CanAnimateTerminal()
        {
            return Application.isPlaying
                && isActiveAndEnabled
                && panelRoot != null
                && panelRoot != gameObject
                && panelRoot.transform is RectTransform
                && transform is RectTransform;
        }

        private bool IsTerminalAnimating()
        {
            return window != null
                && window.Phase != DroneTerminalPhase.Hidden
                && CanAnimateTerminal();
        }

        private void EnsureTerminalState()
        {
            if (terminalFx == null)
            {
                terminalFx = new DroneTerminalFxSettings();
            }

            if (window == null)
            {
                window = new DroneTerminalWindowState(terminalFx);
            }
        }

        /// <summary>
        /// 타이핑을 시작한다. 열 때는 헤더부터, 추천이 바뀌면 대사부터.
        /// 시간표는 다음 프레임에 그 시점의 글자 수로 만든다(같은 분석 주기에 대사가 뒤이어 바뀌므로).
        /// </summary>
        private void BeginReveal(bool includeHeader)
        {
            revealActive = true;
            revealPlanDirty = true;
            revealIncludesHeader = includeHeader;
            revealClock = includeHeader && window != null ? window.Clock : 0f;
            appendActive = false;
            ApplyTerminalFrame();
        }

        private void BuildRevealPlanIfNeeded()
        {
            if (!revealPlanDirty)
            {
                return;
            }

            revealPlanDirty = false;
            var dialogueLength = dialogueText != null ? dialogueText.text.Length : 0;
            var actionLength = actionText != null ? actionText.text.Length : 0;
            revealPlan = revealIncludesHeader
                ? DroneTerminalTypingPlan.ForOpen(terminalFx, dialogueLength, actionLength, reasonLines.Count)
                : DroneTerminalTypingPlan.ForRetype(terminalFx, dialogueLength, actionLength, reasonLines.Count);
            if (revealIncludesHeader)
            {
                cursorHeaderEnd = revealPlan.HeaderEnd;
                cursorRevealEnd = revealPlan.End;
            }
        }

        private bool WasSkipRequested()
        {
            if (window.Phase != DroneTerminalPhase.Opening && !revealActive && !appendActive)
            {
                return false;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null
                && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return false;
            }

            var rect = panelRoot.transform as RectTransform;
            var canvas = GetComponentInParent<Canvas>();
            var root = canvas != null ? canvas.rootCanvas : null;
            var eventCamera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay
                ? root.worldCamera
                : null;
            return RectTransformUtility.RectangleContainsScreenPoint(
                rect, mouse.position.ReadValue(), eventCamera);
        }

        /// <summary>열기·타이핑을 건너뛰어 끝 상태로 둔다.</summary>
        public void SkipTerminalReveal()
        {
            if (window == null || window.Phase == DroneTerminalPhase.Hidden || window.Phase == DroneTerminalPhase.Closing)
            {
                return;
            }

            BuildRevealPlanIfNeeded();
            window.SkipToShown();
            revealActive = false;
            appendActive = false;
            ApplyTerminalFrame();
        }

        private void ApplyTerminalFrame()
        {
            if (!IsTerminalAnimating())
            {
                return;
            }

            EnsureTerminalFx();
            var panelRect = (RectTransform)panelRoot.transform;
            var size = panelRect.rect.size;
            var frame = window.Frame();

            panelRect.localScale = new Vector3(1f, Mathf.Max(0f, frame.Open), 1f);
            ApplyOpenLine(panelRect, size, frame);
            ApplyScanSweep(size, frame);
            ApplyContentAlpha(frame.ContentAlpha);
            ApplyTyping();
            ApplyPriority(size);
        }

        private void ApplyOpenLine(RectTransform panelRect, Vector2 size, DroneTerminalWindowFrame frame)
        {
            var alpha = frame.LineAlpha;
            var visible = alpha > 0.001f && frame.LineWidth > 0.001f;
            SetActiveIfChanged(openLine, visible);
            if (!visible)
            {
                return;
            }

            var rect = openLine.rectTransform;
            // 창이 접히는 기준점(PanelRoot 피벗)에 선을 둔다.
            rect.localPosition = panelRect.localPosition;
            rect.sizeDelta = new Vector2(size.x * frame.LineWidth, OpenLineThickness);
            var color = terminalFx.lineColor;
            color.a *= Mathf.Clamp01(alpha);
            openLine.color = color;
        }

        private void ApplyScanSweep(Vector2 size, DroneTerminalWindowFrame frame)
        {
            var visible = frame.ScanAlpha > 0.001f;
            SetActiveIfChanged(scanSweep, visible);
            if (!visible)
            {
                return;
            }

            scanSweep.rectTransform.anchoredPosition = new Vector2(0f, -frame.ScanPosition * size.y);
            var color = terminalFx.scanColor;
            color.a *= Mathf.Clamp01(frame.ScanAlpha);
            scanSweep.color = color;
        }

        private void ApplyContentAlpha(float alpha)
        {
            if (Mathf.Abs(alpha - appliedContentAlpha) < 0.001f)
            {
                return;
            }

            appliedContentAlpha = alpha;
            SetTextAlpha(speakerText, alpha);
            SetTextAlpha(dialogueText, alpha);
            SetTextAlpha(actionText, alpha);
            SetTextAlpha(reasonText, alpha);
        }

        private void ApplyTyping()
        {
            var headerLength = speakerText != null ? speakerText.text.Length : 0;
            var dialogueVisible = AllCharacters;
            var actionVisible = AllCharacters;
            var reasonVisible = AllCharacters;
            var headerVisible = headerLength;

            if (revealActive && revealPlanDirty)
            {
                // 시간표를 만들기 전(같은 분석 주기 안)에는 다시 찍을 글자를 숨겨 둔다.
                if (revealIncludesHeader)
                {
                    headerVisible = 0;
                }

                dialogueVisible = 0;
                actionVisible = 0;
                reasonVisible = 0;
            }
            else if (revealActive)
            {
                if (revealPlan.IsComplete(revealClock))
                {
                    revealActive = false;
                }
                else
                {
                    if (revealPlan.IncludesHeader)
                    {
                        var typed = HeaderPrefix.Length;
                        headerVisible = revealPlan.VisibleHeaderCharacters(revealClock, typed);
                        if (revealClock >= revealPlan.HeaderEnd)
                        {
                            // 상태 이름은 헤더가 다 찍힌 뒤 한 번에 붙는다.
                            headerVisible = headerLength - HeaderCursor.Length;
                        }
                    }

                    dialogueVisible = revealPlan.VisibleDialogueCharacters(
                        revealClock, dialogueText != null ? dialogueText.text.Length : 0);
                    actionVisible = revealPlan.VisibleActionCharacters(
                        revealClock, actionText != null ? actionText.text.Length : 0);
                    var lines = Mathf.Min(reasonLines.Count, revealPlan.VisibleReasonLines(revealClock));
                    reasonVisible = DroneReasonDisplayFormatter.CharactersInFirstLines(reasonLines, lines);
                }
            }
            else if (appendActive)
            {
                var lines = DroneTerminalTimeline.AppendedReasonLines(
                    appendFrom, appendTo, appendClock, terminalFx.reasonLineInterval);
                lines = Mathf.Min(lines, reasonLines.Count);
                if (lines >= Mathf.Min(appendTo, reasonLines.Count))
                {
                    appendActive = false;
                }
                else
                {
                    reasonVisible = DroneReasonDisplayFormatter.CharactersInFirstLines(reasonLines, lines);
                }
            }

            // 커서는 헤더가 다 찍힌 뒤에만 깜빡인다.
            if (headerVisible >= headerLength - HeaderCursor.Length
                && !DroneTerminalTimeline.CursorVisible(window.Clock, cursorHeaderEnd, cursorRevealEnd, terminalFx))
            {
                headerVisible = headerLength - HeaderCursor.Length;
            }

            SetMaxVisible(speakerText, headerVisible);
            SetMaxVisible(dialogueText, dialogueVisible);
            SetMaxVisible(actionText, actionVisible);
            SetMaxVisible(reasonText, reasonVisible);
        }

        private void ApplyPriority(Vector2 size)
        {
            var duration = terminalFx.priorityFlashDuration;
            if (!priorityActive || priorityClock >= duration)
            {
                if (priorityActive)
                {
                    priorityActive = false;
                    RestoreHeaderColors();
                }

                SetActiveIfChanged(glitchBand, false);
                SetActiveIfChanged(glitchEcho, false);
                return;
            }

            var flash = DroneTerminalTimeline.PriorityFlash(priorityClock, duration);
            if (headerBar != null)
            {
                headerBar.color = Color.Lerp(TerminalHeader, terminalFx.priorityColor, flash * 0.85f);
            }

            if (speakerText != null)
            {
                speakerText.color = Color.Lerp(HeaderTextColor(), PriorityFlashText, flash);
            }

            var glitch = DroneTerminalTimeline.Glitch(priorityClock, duration);
            var bandVisible = glitch.Active && glitch.BandAlpha > 0.001f;
            SetActiveIfChanged(glitchBand, bandVisible);
            SetActiveIfChanged(glitchEcho, bandVisible);
            if (!bandVisible)
            {
                return;
            }

            var color = terminalFx.glitchColor;
            color.a *= glitch.BandAlpha;
            glitchBand.color = color;
            glitchBand.rectTransform.anchoredPosition = new Vector2(
                glitch.OffsetX * GlitchMaxOffset, -glitch.BandPosition * size.y);
            color.a *= 0.6f;
            glitchEcho.color = color;
            glitchEcho.rectTransform.anchoredPosition = new Vector2(
                -glitch.OffsetX * GlitchMaxOffset * 0.6f,
                -Mathf.Clamp01(glitch.BandPosition - 0.12f) * size.y);
        }

        private void RestoreHeaderColors()
        {
            if (headerBar != null)
            {
                headerBar.color = TerminalHeader;
            }

            if (speakerText != null)
            {
                speakerText.color = HeaderTextColor();
            }
        }

        /// <summary>연출 흔적을 지우고 창을 정상 표시 상태로 둔다.</summary>
        private void ResetTerminalFx(bool hideFxObjects = true)
        {
            revealActive = false;
            revealPlanDirty = false;
            appendActive = false;
            priorityActive = false;
            if (panelRoot != null && panelRoot != gameObject)
            {
                panelRoot.transform.localScale = Vector3.one;
            }

            if (hideFxObjects)
            {
                SetActiveIfChanged(openLine, false);
                SetActiveIfChanged(scanSweep, false);
                SetActiveIfChanged(glitchBand, false);
                SetActiveIfChanged(glitchEcho, false);
            }
            appliedContentAlpha = -1f;
            ApplyContentAlpha(1f);
            SetMaxVisible(speakerText, AllCharacters);
            SetMaxVisible(dialogueText, AllCharacters);
            SetMaxVisible(actionText, AllCharacters);
            SetMaxVisible(reasonText, AllCharacters);
            RestoreHeaderColors();
        }

        private void FinishAnimatedClose()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            var callback = closeCallback;
            closeCallback = null;
            if (callback != null)
            {
                callback();
            }
        }

        private void EnsureTerminalFx()
        {
            var rootRect = transform as RectTransform;
            var panelRect = panelRoot != null ? panelRoot.transform as RectTransform : null;
            if (rootRect == null || panelRect == null)
            {
                return;
            }

            if (openLine == null)
            {
                // 창이 접혀도 보이도록 PanelRoot 밖(형제)에 둔다.
                openLine = CreateFxImage("TerminalOpenLine", rootRect, RunFailureGlitchArt.GetCrtBeamSprite());
                SetAnchors(openLine.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero);
            }

            if (scanSweep == null)
            {
                scanSweep = CreateFxImage("TerminalScanSweep", panelRect, RunFailureGlitchArt.GetCrtBeamSprite());
                SetTopBand(scanSweep.rectTransform, ScanSweepHeight);
            }

            if (glitchBand == null)
            {
                glitchBand = CreateFxImage("TerminalGlitchBand", panelRect, RunFailureGlitchArt.GetGlitchBlockSprite());
                SetTopBand(glitchBand.rectTransform, GlitchBandHeight);
            }

            if (glitchEcho == null)
            {
                glitchEcho = CreateFxImage("TerminalGlitchEcho", panelRect, RunFailureGlitchArt.GetGlitchBlockSprite());
                SetTopBand(glitchEcho.rectTransform, GlitchEchoHeight);
            }
        }

        private Image CreateFxImage(string name, RectTransform parent, Sprite sprite)
        {
            var image = FindOrCreateImage(name, parent);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.transform.SetAsLastSibling();
            image.gameObject.SetActive(false);
            return image;
        }

        private static void SetTopBand(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void SetActiveIfChanged(Image image, bool active)
        {
            if (image != null && image.gameObject.activeSelf != active)
            {
                image.gameObject.SetActive(active);
            }
        }

        private static void SetMaxVisible(TMP_Text text, int count)
        {
            if (text != null && text.maxVisibleCharacters != count)
            {
                text.maxVisibleCharacters = count;
            }
        }

        private static void SetTextAlpha(TMP_Text text, float alpha)
        {
            if (text != null)
            {
                text.alpha = alpha;
            }
        }

        private static void SetTextIfChanged(TMP_Text text, string value)
        {
            if (text != null && !string.Equals(text.text, value, StringComparison.Ordinal))
            {
                text.text = value;
            }
        }

        private void StyleText(TMP_Text text, Color color)
        {
            if (text == null)
            {
                return;
            }

            text.color = color;
            text.raycastTarget = false;
        }

        private void StyleCloseButton(RectTransform header)
        {
            if (closeButton == null || header == null)
            {
                return;
            }

            if (closeButton.transform.parent != header)
            {
                closeButton.transform.SetParent(header, false);
            }

            var image = closeButton.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.color = new Color(0.07f, 0.18f, 0.22f, 0.98f);
            }

            // ColorTint는 전용 피드백의 0.12초 전환과 경쟁하므로 사용하지 않는다.
            closeButton.transition = Selectable.Transition.None;

            var rect = closeButton.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.one;
                rect.anchorMax = Vector2.one;
                rect.pivot = Vector2.one;
                rect.anchoredPosition = new Vector2(-5f, -3f);
                rect.sizeDelta = new Vector2(28f, 28f);
            }

            var label = closeButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "X";
                label.fontSize = 18f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = AccentCyan;
                label.fontStyle = FontStyles.Bold;
                label.raycastTarget = false;
            }

            var feedback = closeButton.GetComponent<DroneTerminalCloseButtonFeedback>();
            if (feedback == null)
            {
                feedback = closeButton.gameObject.AddComponent<DroneTerminalCloseButtonFeedback>();
            }

            feedback.Configure(image, label);
        }

        private Image FindOrCreateImage(string name, RectTransform parent)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null && existing.TryGetComponent<Image>(out var existingImage))
            {
                existingImage.raycastTarget = false;
                return existingImage;
            }

            var element = new GameObject(name, typeof(RectTransform), typeof(Image));
            element.transform.SetParent(parent, false);
            var image = element.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void SetAnchors(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
