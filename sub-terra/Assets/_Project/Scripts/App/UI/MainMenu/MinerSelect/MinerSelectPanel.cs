using System;
using SubTerra.App.Audio;
using SubTerra.App.UI.Guide;
using SubTerra.App.UI.Outpost;
using SubTerra.App.UI.Sell;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.App.UI.MainMenu
{
    public enum MinerSelectPhase
    {
        Hidden = 0,
        Choosing = 1,
        /// <summary>확정: 고른 카드에 위→아래 스캔으로 원래 색이 입혀지는 중(입력 막힘).</summary>
        Filling = 2,
        /// <summary>닫힘 글리치(모션 감소면 짧은 암전).</summary>
        Exiting = 3,
        /// <summary>완전히 검게 닫힘. 인트로 오버레이가 이어서 덮는다.</summary>
        Black = 4,
        /// <summary>TV 켜지듯 나타나는 중(입력 막힘).</summary>
        Appearing = 5,
        /// <summary>돌아가기: TV 꺼지듯 접히는 중(입력 막힘).</summary>
        Closing = 6
    }

    /// <summary>
    /// B-209 새 게임 전 광부 선택 화면. 메인 메뉴 패널 안에 코드로 만든다(Scene/프리팹 YAML 변경 없음).
    /// 상태는 MinerSelectFlow, 시간표는 MinerSelectMotion, 카드 그리기는 MinerSelectCardView가 맡고
    /// 이 컴포넌트는 입력·연출 단계·효과음만 한다. 새 게임 시작은 하지 않는다: 검게 닫히면 LaunchReady로 넘긴다.
    /// </summary>
    public sealed class MinerSelectPanel : MonoBehaviour
    {
        public const string ObjectName = "MinerSelectPanel";
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;
        public const float CardSpacing = 251f;
        private const int NoiseBarCount = 12;
        private const float FlickerStepSeconds = 0.08f;
        private const float BackdropAlpha = 0.66f;
        private const float SelectEmphasisSeconds = 0.25f;
        /// <summary>캡처가 늦으면 이 프레임 수 뒤에 막대 글리치로 넘어간다.</summary>
        private const int CaptureWaitFrames = 4;
        /// <summary>프레임이 크게 끊겨도 연출이 한 번에 건너뛰지 않게 하는 한 프레임 최대 진행(초).</summary>
        private const float MaxPhaseStep = 0.1f;

        private CanvasGroup group;
        private UnityEngine.UI.Image backdrop;
        private RectTransform stage;
        private RectTransform content;
        private MinerSelectCardView[] cards;
        private MinerSelectParticleField dust;
        private CanvasGroup hintGroup;
        private RectTransform hintRect;
        private TMP_Text hintText;
        private TMP_Text titleText;
        private TMP_Text subtitleText;
        private MinerSelectButtonView startButton;
        private MinerSelectButtonView backButton;
        private RectTransform tvLine;
        private UnityEngine.UI.Image tvLineImage;
        private UnityEngine.UI.Image flash;
        private UnityEngine.UI.Image blackout;
        private RectTransform[] noiseBars;
        private UnityEngine.UI.Image[] noiseImages;
        private MinerSelectScreenGlitch screenGlitch;

        private MinerSelectSession session;
        private MinerSelectPhase phase = MinerSelectPhase.Hidden;
        private int openedFrame = -1;
        private readonly float[] hover = new float[MinerSelectFlow.CardCount];
        private readonly float[] emphasis = new float[MinerSelectFlow.CardCount];
        private readonly float[] reveal = new float[MinerSelectFlow.CardCount];
        private readonly float[] brightness = { 1f, 1f };
        private float colorFill;
        private float hintElapsed = -1f;
        private float phaseElapsed;
        private bool phaseReduced;
        private int captureWait;
        private float clock;
        private float dustCarry;

        public MinerSelectPhase Phase => phase;
        public bool IsOpen => phase != MinerSelectPhase.Hidden && gameObject.activeSelf;
        public MinerSelectSession Session => session;
        public MinerSelectFlow Flow => session != null ? session.Flow : null;
        public bool IsHintShown => hintGroup != null && hintGroup.gameObject.activeSelf && hintGroup.alpha > 0.001f;
        public bool StartInteractable => startButton != null && startButton.Button.interactable;
        public bool StartLit => startButton != null && startButton.IsLit;
        public float BlackAlpha => blackout != null ? blackout.color.a : 0f;
        public float StageOpen => stage != null ? stage.localScale.y : 0f;
        public MinerSelectCardView Card(int index) => cards != null && index >= 0 && index < cards.Length ? cards[index] : null;
        public float CardReveal(int index) => index >= 0 && index < reveal.Length ? reveal[index] : 0f;
        public float CardBrightness(int index) => index >= 0 && index < brightness.Length ? brightness[index] : 1f;
        public float ColorFill => colorFill;
        public bool UsesScreenGlitch => screenGlitch != null && screenGlitch.HasCapture;

        /// <summary>돌아가기(Esc·버튼): TV 꺼짐이 끝난 뒤 한 번. 바인더가 화면을 닫고 메뉴로 돌아간다.</summary>
        public event Action BackRequested;
        /// <summary>색 입힘 → 닫힘 글리치가 끝나 화면이 완전히 검게 닫혔다. 바인더가 인트로·새 게임을 시작한다.</summary>
        public event Action<MinerSelectSession> LaunchReady;

        public static MinerSelectPanel Create(RectTransform parent, TMP_FontAsset font)
        {
            if (parent == null) return null;
            var existing = parent.Find(ObjectName);
            if (existing != null && existing.TryGetComponent(out MinerSelectPanel found)) return found;

            var rect = NewRect(parent, ObjectName, Vector2.zero, Vector2.zero);
            rect.StretchFull();
            var panel = rect.gameObject.AddComponent<MinerSelectPanel>();
            try
            {
                panel.Build(font != null ? font : TMP_Settings.defaultFontAsset);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, panel);
            }

            rect.gameObject.SetActive(false);
            return panel;
        }

        public void Open(MinerSelectSession newSession)
        {
            session = newSession;
            if (session == null) return;
            session.Flow.Reset();
            for (var i = 0; i < MinerSelectFlow.CardCount; i++)
            {
                hover[i] = 0f;
                emphasis[i] = 0f;
                reveal[i] = 0f;
                brightness[i] = 1f;
                var card = Card(i);
                if (card != null && card.Particles != null) card.Particles.Clear();
            }

            DestroyScreenGlitch();
            colorFill = 0f;
            hintElapsed = -1f;
            phaseElapsed = 0f;
            phaseReduced = AccessibilityPreferences.ReduceMotion;
            phase = MinerSelectPhase.Appearing;
            openedFrame = Time.frameCount;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            RefreshTexts();
            if (hintGroup != null)
            {
                hintGroup.gameObject.SetActive(true);
                hintGroup.alpha = 1f;
                hintRect.anchoredPosition = new Vector2(0f, hintRect.anchoredPosition.y);
            }

            ApplyTransition(MinerSelectTransitionFrame.Clear);
            if (content != null) content.anchoredPosition = Vector2.zero;
            ApplyTv(MinerSelectMotion.EvaluateAppear(0f, phaseReduced));
            Fit();
            RefreshStartButton();
            ApplyCards(0f);
            if (!phaseReduced) SfxPlayer.Play(SfxId.UiCctvOn);
            // 메뉴의 새 게임 버튼 선택이 남아 있으면 같은 Enter가 다시 눌린다.
            UiKeyboardSubmitGuard.ClearSelection();
        }

        /// <summary>즉시 숨긴다(돌아가기 완료·메뉴 비활성).</summary>
        public void Close()
        {
            phase = MinerSelectPhase.Hidden;
            DestroyScreenGlitch();
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        /// <summary>새 게임 시작이 실패하면 검은 화면을 걷고 다시 고를 수 있게 한다.</summary>
        public void RecoverAfterFailedStart()
        {
            if (session == null) return;
            session.Flow.Unlock();
            DestroyScreenGlitch();
            colorFill = 0f;
            phaseElapsed = 0f;
            phase = MinerSelectPhase.Choosing;
            ApplyTransition(MinerSelectTransitionFrame.Clear);
            if (content != null) content.anchoredPosition = Vector2.zero;
            ApplyTv(new MinerSelectTvFrame(1f, 1f, 0f, 0f, 0f, 1f, true));
            RefreshStartButton();
        }

        // ── 입력(마우스·키보드·확인용 호출 공통) ──

        public void PressCard(int index)
        {
            if (!CanInput) return;
            var result = session.Flow.Select(index);
            switch (result)
            {
                case MinerSelectResult.Selected:
                    hintElapsed = 0f;
                    SfxPlayer.Play(SfxId.UiSlotSelect);
                    BurstCard(index, 18, false);
                    break;
                case MinerSelectResult.Switched:
                    SfxPlayer.Play(SfxId.UiSlotSelect);
                    BurstCard(index, 18, false);
                    break;
            }

            RefreshStartButton();
        }

        public void PressStart()
        {
            if (!CanInput) return;
            HandleConfirm(session.Flow.TryConfirm());
        }

        public void PressBack()
        {
            if (!CanInput) return;
            if (session.Flow.Back() != MinerSelectResult.Back) return;
            // 프롬프트의 UiBack은 B-158에서 없어졌다(칸 비움). 취소·돌아가기는 UiClick을 쓴다.
            SfxPlayer.Play(SfxId.UiClick);
            phase = MinerSelectPhase.Closing;
            phaseElapsed = 0f;
            phaseReduced = AccessibilityPreferences.ReduceMotion;
            RefreshStartButton();
            UiKeyboardSubmitGuard.ClearSelection();
        }

        public void MoveFocus(int direction)
        {
            if (!CanInput) return;
            if (session.Flow.MoveFocus(direction) == MinerSelectResult.Focused) SfxPlayer.Play(SfxId.UiHover);
        }

        public void Submit()
        {
            if (!CanInput) return;
            var focus = session.Flow.FocusIndex;
            if (focus >= 0 && focus != session.Flow.SelectedIndex)
            {
                PressCard(focus);
                return;
            }

            HandleConfirm(session.Flow.TryConfirm());
        }

        private bool CanInput => phase == MinerSelectPhase.Choosing && session != null && !session.Flow.IsLocked;

        private void HandleConfirm(MinerSelectResult result)
        {
            if (result == MinerSelectResult.Rejected)
            {
                SfxPlayer.Play(SfxId.UiCancelFail);
                return;
            }

            if (result != MinerSelectResult.Confirmed) return;
            SfxPlayer.Play(SfxId.UiConfirm);
            phase = MinerSelectPhase.Filling;
            colorFill = 0f;
            BurstCard(session.Flow.SelectedIndex, 30, true);
            RefreshStartButton();
            UiKeyboardSubmitGuard.ClearSelection();
        }

        private void BurstCard(int index, int count, bool warm)
        {
            var card = Card(index);
            if (card != null && Application.isPlaying) card.Burst(count, warm);
        }

        private void OnCardEntered(int index)
        {
            if (!CanInput) return;
            session.Flow.Hover(index);
            SfxPlayer.Play(SfxId.UiHover);
        }

        private void OnCardExited(int index)
        {
            if (!CanInput) return;
            session.Flow.Unhover(index);
        }

        private void Update()
        {
            if (phase == MinerSelectPhase.Hidden || session == null) return;
            if (phase == MinerSelectPhase.Choosing && Time.frameCount != openedFrame) HandleKeyboard();
            Tick(Time.unscaledDeltaTime);
        }

        /// <summary>시간을 진행한다. 확인용으로 바깥에서 dt를 넣어도 된다(한 번에 1/30초까지만 반영되는 단계가 있다).</summary>
        public void Tick(float dt)
        {
            if (phase == MinerSelectPhase.Hidden || session == null) return;
            dt = Mathf.Max(0f, dt);
            clock += dt;
            UpdateHint(dt);

            switch (phase)
            {
                case MinerSelectPhase.Appearing:
                {
                    phaseElapsed += Mathf.Min(dt, MaxPhaseStep);
                    var frame = MinerSelectMotion.EvaluateAppear(phaseElapsed, phaseReduced);
                    ApplyTv(frame);
                    if (frame.IsComplete) phase = MinerSelectPhase.Choosing;
                    break;
                }
                case MinerSelectPhase.Closing:
                {
                    phaseElapsed += Mathf.Min(dt, MaxPhaseStep);
                    var frame = MinerSelectMotion.EvaluateClose(phaseElapsed, phaseReduced);
                    ApplyTv(frame);
                    if (frame.IsComplete)
                    {
                        phase = MinerSelectPhase.Hidden;
                        BackRequested?.Invoke();
                        return;
                    }

                    break;
                }
                case MinerSelectPhase.Filling:
                    colorFill = Mathf.Clamp01(colorFill + dt / MinerSelectMotion.ColorFillSeconds);
                    if (colorFill >= 1f) BeginExit();
                    break;
                case MinerSelectPhase.Exiting:
                    TickExit(dt);
                    break;
            }

            ApplyCards(dt);
            TickDust(dt);
        }

        private void BeginExit()
        {
            phaseReduced = AccessibilityPreferences.ReduceMotion;
            phaseElapsed = 0f;
            captureWait = 0;
            phase = MinerSelectPhase.Exiting;
            if (phaseReduced) return;
            SfxPlayer.Play(SfxId.BriefingGlitchShort);
            screenGlitch = MinerSelectScreenGlitch.Create();
            if (screenGlitch != null) screenGlitch.Begin();
        }

        private void TickExit(float dt)
        {
            // 화면 캡처가 준비될 때까지(보통 한 프레임) 시간을 멈춘다. 늦으면 막대 글리치로 간다.
            var waitingCapture = screenGlitch != null && !screenGlitch.HasCapture && !screenGlitch.Failed
                && captureWait < CaptureWaitFrames;
            if (waitingCapture)
            {
                captureWait++;
                return;
            }

            phaseElapsed += Mathf.Min(dt, MaxPhaseStep);
            var frame = MinerSelectMotion.EvaluateTransition(phaseElapsed, phaseReduced);
            if (screenGlitch != null && screenGlitch.HasCapture)
            {
                screenGlitch.Apply(frame);
                if (blackout != null) blackout.color = new Color(0f, 0f, 0f, frame.IsComplete ? 1f : 0f);
            }
            else
            {
                ApplyTransition(frame);
            }

            if (!frame.IsComplete) return;
            if (blackout != null) blackout.color = Color.black;
            phase = MinerSelectPhase.Black;
            LaunchReady?.Invoke(session);
        }

        private void HandleKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) PressBack();
            else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) MoveFocus(-1);
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) MoveFocus(1);
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame
                || keyboard.spaceKey.wasPressedThisFrame) Submit();
        }

        private void UpdateHint(float dt)
        {
            if (hintGroup == null || hintElapsed < 0f) return;
            hintElapsed += dt;
            var frame = MinerSelectMotion.EvaluateHint(hintElapsed);
            hintGroup.alpha = frame.Alpha;
            hintRect.anchoredPosition = new Vector2(frame.OffsetX, hintRect.anchoredPosition.y);
            if (frame.IsComplete)
            {
                hintElapsed = -1f;
                hintGroup.gameObject.SetActive(false);
            }
        }

        private void ApplyCards(float dt)
        {
            if (cards == null || session == null) return;
            var flow = session.Flow;
            var colorPhase = phase == MinerSelectPhase.Filling || phase == MinerSelectPhase.Exiting || phase == MinerSelectPhase.Black;
            for (var i = 0; i < cards.Length; i++)
            {
                var focused = flow.FocusIndex == i && phase == MinerSelectPhase.Choosing;
                var selected = flow.SelectedIndex == i;
                var dimmed = flow.HasSelection && !selected;
                hover[i] = MinerSelectMotion.Approach(hover[i], focused ? 1f : 0f, dt, MinerSelectMotion.HoverSeconds);
                emphasis[i] = MinerSelectMotion.Approach(emphasis[i], selected ? 1f : 0f, dt, SelectEmphasisSeconds);
                // 고르면 스캔 줄이 일정한 속도로 내려가며 드러내고, 바꿔 고르면 더 빨리 올라가며 실루엣으로 돌아간다.
                reveal[i] = MinerSelectMotion.Approach(reveal[i], selected ? 1f : 0f, dt,
                    selected ? MinerSelectMotion.SilhouetteRevealSeconds : MinerSelectMotion.SilhouetteHideSeconds);
                brightness[i] = MinerSelectMotion.Approach(brightness[i], dimmed ? MinerSelectMotion.DimmedBrightness : 1f,
                    dt, SelectEmphasisSeconds);
                var step = (int)(clock / FlickerStepSeconds);
                cards[i].Apply(new MinerCardVisual
                {
                    Hover = hover[i],
                    Selected = emphasis[i],
                    Reveal = reveal[i],
                    Fill = selected && colorPhase ? colorFill : 0f,
                    Brightness = brightness[i],
                    Flicker = 0.86f + 0.14f * MinerSelectMotion.Hash(step * 3 + i * 101)
                }, dt);
            }
        }

        private void TickDust(float dt)
        {
            if (dust == null) return;
            if (Application.isPlaying)
            {
                dustCarry += dt * 7f;
                while (dustCarry >= 1f)
                {
                    dustCarry -= 1f;
                    var pos = new Vector2(UnityEngine.Random.Range(-940f, 940f), UnityEngine.Random.Range(-560f, -200f));
                    var vel = new Vector2(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(12f, 30f));
                    var c = MinerSelectArt.Cyan;
                    c.a = UnityEngine.Random.Range(0.18f, 0.45f);
                    dust.Emit(pos, vel, UnityEngine.Random.Range(2.5f, 5.5f), UnityEngine.Random.Range(5f, 9f), c);
                }
            }

            dust.Tick(dt);
        }

        private void RefreshStartButton()
        {
            if (startButton == null || session == null) return;
            var enabled = session.Flow.CanConfirm && phase == MinerSelectPhase.Choosing;
            // 확정 뒤에는 켜진 모양을 유지하되 다시 눌리지 않는다.
            startButton.SetState(enabled, enabled || session.Flow.IsConfirmed);
            if (backButton != null) backButton.SetState(!session.Flow.IsLocked, true);
        }

        /// <summary>TV 켜짐·꺼짐: 무대를 세로로 펼치거나 접고, 가운데 가로선·번쩍임·노이즈를 얹는다.</summary>
        private void ApplyTv(MinerSelectTvFrame frame)
        {
            if (group != null) group.alpha = frame.Alpha;
            if (stage != null) stage.localScale = new Vector3(1f, Mathf.Max(0.002f, frame.Open), 1f);
            if (backdrop != null) backdrop.color = new Color(0.004f, 0.02f, 0.03f, BackdropAlpha * Mathf.Clamp01(0.6f + 0.4f * frame.Open));
            if (tvLine != null)
            {
                tvLine.anchorMin = new Vector2(0.5f - frame.LineWidth * 0.5f, 0.5f);
                tvLine.anchorMax = new Vector2(0.5f + frame.LineWidth * 0.5f, 0.5f);
                var c = Color.Lerp(Color.white, MinerSelectArt.Cyan, 0.35f);
                c.a = frame.LineAlpha;
                tvLineImage.color = c;
            }

            if (flash != null) flash.color = new Color(0.8f, 1f, 1f, frame.Flash);
            ApplyNoise(frame.NoiseAlpha, (int)(clock / MinerSelectMotion.GlitchStepSeconds));
        }

        /// <summary>캡처 글리치를 못 쓸 때의 닫힘: 노이즈 막대 + 내용 밀림 + 세로 접힘 + 검정.</summary>
        private void ApplyTransition(MinerSelectTransitionFrame frame)
        {
            if (blackout != null) blackout.color = new Color(0f, 0f, 0f, frame.BlackAlpha);
            if (content != null) content.anchoredPosition = new Vector2(frame.SliceOffset, 0f);
            if (stage != null && phase == MinerSelectPhase.Exiting)
            {
                stage.localScale = new Vector3(Mathf.Max(0.002f, frame.LineWidth), Mathf.Max(0.002f, 1f - frame.Collapse * 0.995f), 1f);
            }

            if (flash != null && phase == MinerSelectPhase.Exiting) flash.color = new Color(0.8f, 1f, 1f, frame.Flash);
            ApplyNoise(frame.NoiseAlpha, frame.Seed);
        }

        private void ApplyNoise(float alpha, int seed)
        {
            if (noiseBars == null) return;
            var random = new System.Random(seed * 7919 + 17);
            for (var i = 0; i < noiseBars.Length; i++)
            {
                if (alpha <= 0f)
                {
                    noiseImages[i].color = Color.clear;
                    continue;
                }

                var y = (float)random.NextDouble();
                var h = 0.002f + (float)random.NextDouble() * 0.014f;
                var w = 0.15f + (float)random.NextDouble() * 0.6f;
                var x = (float)random.NextDouble() * (1f - w);
                noiseBars[i].anchorMin = new Vector2(x, y);
                noiseBars[i].anchorMax = new Vector2(x + w, Mathf.Min(1f, y + h));
                var tint = Color.Lerp(MinerSelectArt.Cyan, Color.white, (float)random.NextDouble() * 0.7f);
                tint.a = alpha * (0.25f + (float)random.NextDouble() * 0.75f);
                noiseImages[i].color = tint;
            }
        }

        private void DestroyScreenGlitch()
        {
            if (screenGlitch == null) return;
            if (Application.isPlaying) Destroy(screenGlitch.gameObject);
            else DestroyImmediate(screenGlitch.gameObject);
            screenGlitch = null;
        }

        private void OnRectTransformDimensionsChange() => Fit();

        /// <summary>1920×1080 기준 내용을 화면 안에 맞춘다(다른 비율에서도 카드·버튼이 잘리지 않음).</summary>
        private void Fit()
        {
            if (content == null) return;
            var rect = ((RectTransform)transform).rect;
            if (rect.width <= 0f || rect.height <= 0f) return;
            var scale = Mathf.Min(rect.width / ReferenceWidth, rect.height / ReferenceHeight);
            content.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        }

        private void OnEnable()
        {
            LocalizationService.LanguageChanged += RefreshTexts;
        }

        private void OnDisable()
        {
            LocalizationService.LanguageChanged -= RefreshTexts;
        }

        private void OnDestroy()
        {
            DestroyScreenGlitch();
            if (cards == null) return;
            for (var i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;
                cards[i].PointerEntered -= OnCardEntered;
                cards[i].PointerExited -= OnCardExited;
                cards[i].Clicked -= PressCard;
            }
        }

        private void RefreshTexts()
        {
            if (titleText != null) titleText.text = LocalizationService.Get("miner_select.title", "광부 선택");
            if (subtitleText != null)
                subtitleText.text = LocalizationService.Get("miner_select.subtitle", "탐사를 시작할 광부를 선택하세요.");
            if (hintText != null) hintText.text = LocalizationService.Get("miner_select.hint", "광부를 선택해 주세요");
            if (startButton != null) startButton.Label.text = LocalizationService.Get("miner_select.start", "탐사 시작");
            if (backButton != null) backButton.Label.text = "←  " + LocalizationService.Get("miner_select.back", "돌아가기");
            if (cards != null)
            {
                cards[0].SetLabel(LocalizationService.Get("miner_select.male", "남자"));
                cards[1].SetLabel(LocalizationService.Get("miner_select.female", "여자"));
            }
        }

        // ── 화면 조립 ──

        private void Build(TMP_FontAsset font)
        {
            var root = (RectTransform)transform;
            group = root.gameObject.AddComponent<CanvasGroup>();

            // 메뉴 배경(광산 그림)은 비치게 두고 어둡게 덮는다. 아래 메뉴로 클릭이 새지 않게 막는다.
            backdrop = NewImage(root, "Backdrop", null, new Color(0.004f, 0.02f, 0.03f, BackdropAlpha), true);
            backdrop.rectTransform.StretchFull();
            var vignette = NewImage(root, "Vignette", CoreCctvArt.Vignette(), Color.white, false);
            vignette.rectTransform.StretchFull();

            // TV 켜짐·꺼짐 때 세로로 펼쳐지고 접히는 무대.
            stage = NewRect(root, "Stage", Vector2.zero, Vector2.zero);
            stage.StretchFull();

            var frameArt = MinerSelectArt.ScreenFrame();
            var frame = NewImage(stage, "ScreenFrame", frameArt != null ? frameArt : ResourceSellArt.ChamferOutline(),
                frameArt != null ? Color.white : new Color(0.42f, 0.94f, 1f, 0.55f), false);
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            if (frameArt != null) frame.pixelsPerUnitMultiplier = 1.35f;
            frame.rectTransform.StretchFull();
            frame.rectTransform.offsetMin = new Vector2(10f, 10f);
            frame.rectTransform.offsetMax = new Vector2(-10f, -10f);

            content = NewRect(stage, "Content", Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight));
            dust = MinerSelectParticleField.Create(content, "Dust", new Vector2(ReferenceWidth, ReferenceHeight));

            BuildHeader(font);

            // 카드 두 장(왼쪽 남자, 오른쪽 여자). 그림은 B-208 가이드 스킨의 서 있는 프레임을 쓴다.
            var sprites = GuideSprites.Load(null);
            cards = new MinerSelectCardView[MinerSelectFlow.CardCount];
            cards[0] = MinerSelectCardView.Create(content, 0, "MaleCard", sprites.Get("player.idle"), font);
            cards[1] = MinerSelectCardView.Create(content, 1, "FemaleCard", sprites.Get(GuidePlayerArt.FemaleKeyPrefix + "idle"), font);
            for (var i = 0; i < cards.Length; i++)
            {
                ((RectTransform)cards[i].transform).anchoredPosition = new Vector2(i == 0 ? -CardSpacing : CardSpacing, -2f);
                cards[i].PointerEntered += OnCardEntered;
                cards[i].PointerExited += OnCardExited;
                cards[i].Clicked += PressCard;
            }

            // 안내 글자(양옆 선 포함). 처음 고를 때 통째로 지직거리며 사라진다.
            hintRect = NewRect(content, "Hint", new Vector2(0f, -352f), new Vector2(900f, 44f));
            hintGroup = hintRect.gameObject.AddComponent<CanvasGroup>();
            hintGroup.blocksRaycasts = false;
            hintText = NewText(hintRect, "Text", font, string.Empty, 28f, new Color(0.9f, 0.97f, 1f, 1f), Vector2.zero, new Vector2(400f, 44f));
            OpsLine(hintRect, -300f, 0f, 190f);
            OpsLine(hintRect, 300f, 0f, 190f);

            startButton = MinerSelectButtonView.Create(content, "StartButton", new Vector2(0f, -440f), new Vector2(580f, 92f), font, 38f,
                PressStart, true);
            startButton.Label.fontStyle = FontStyles.Bold;
            startButton.Label.characterSpacing = 6f;
            backButton = MinerSelectButtonView.Create(content, "BackButton", new Vector2(-ReferenceWidth * 0.5f + 200f, -458f),
                new Vector2(250f, 70f), font, 28f, PressBack, false);

            // TV 선·번쩍임·노이즈·검은 막. 맨 위에서 화면 전체를 덮는다.
            tvLineImage = NewImage(root, "TvLine", MinerSelectArt.ScanGlow(), Color.clear, false);
            tvLine = tvLineImage.rectTransform;
            tvLine.offsetMin = new Vector2(0f, -22f);
            tvLine.offsetMax = new Vector2(0f, 22f);
            flash = NewImage(root, "Flash", null, Color.clear, false);
            flash.rectTransform.StretchFull();
            noiseBars = new RectTransform[NoiseBarCount];
            noiseImages = new UnityEngine.UI.Image[NoiseBarCount];
            for (var i = 0; i < NoiseBarCount; i++)
            {
                noiseImages[i] = NewImage(root, "Noise" + i, null, Color.clear, false);
                noiseBars[i] = noiseImages[i].rectTransform;
                noiseBars[i].offsetMin = noiseBars[i].offsetMax = Vector2.zero;
            }

            blackout = NewImage(root, "Blackout", null, Color.clear, false);
            blackout.rectTransform.StretchFull();
            RefreshTexts();
        }

        private void BuildHeader(TMP_FontAsset font)
        {
            var ops = NewText(content, "OpsLabel", font, LocalizationService.Get("miner_select.ops", "SUB-TERRA OPS"), 24f,
                MinerSelectArt.Cyan, new Vector2(0f, 502f), new Vector2(400f, 36f));
            ops.characterSpacing = 8f;
            OpsLine(content, -238f, 502f);
            OpsLine(content, 238f, 502f);

            var titleGlow = NewImage(content, "TitleGlow", MinerSelectArt.SoftDisc(), new Color(0.42f, 0.94f, 1f, 0.16f), false);
            titleGlow.rectTransform.anchoredPosition = new Vector2(0f, 448f);
            titleGlow.rectTransform.sizeDelta = new Vector2(720f, 150f);
            titleText = NewText(content, "Title", font, string.Empty, 70f, new Color(0.92f, 1f, 1f, 1f), new Vector2(0f, 450f),
                new Vector2(900f, 86f));
            titleText.fontStyle = FontStyles.Bold;
            titleText.characterSpacing = 6f;

            var registration = NewText(content, "Registration", font,
                LocalizationService.Get("miner_select.registration", "MINER REGISTRATION"), 24f, MinerSelectArt.Cyan,
                new Vector2(0f, 396f), new Vector2(700f, 36f));
            registration.characterSpacing = 14f;

            var divider = NewImage(content, "Divider", MinerSelectArt.ScanGlow(), new Color(0.42f, 0.94f, 1f, 0.7f), false);
            divider.rectTransform.anchoredPosition = new Vector2(0f, 370f);
            divider.rectTransform.sizeDelta = new Vector2(760f, 10f);
            var diamondGlow = NewImage(content, "DividerGlow", MinerSelectArt.SoftDisc(), MinerSelectArt.Cyan, false);
            diamondGlow.rectTransform.anchoredPosition = new Vector2(0f, 370f);
            diamondGlow.rectTransform.sizeDelta = new Vector2(34f, 34f);
            var diamond = NewImage(content, "DividerDiamond", null, new Color(0.75f, 1f, 1f, 1f), false);
            diamond.rectTransform.anchoredPosition = new Vector2(0f, 370f);
            diamond.rectTransform.sizeDelta = new Vector2(9f, 9f);
            diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            subtitleText = NewText(content, "Subtitle", font, string.Empty, 26f, new Color(0.88f, 0.95f, 0.97f, 1f),
                new Vector2(0f, 338f), new Vector2(1000f, 36f));
        }

        private static void OpsLine(Transform parent, float x, float y, float width = 150f)
        {
            var line = NewImage(parent, "Line", MinerSelectArt.ScanGlow(), new Color(0.42f, 0.94f, 1f, 0.75f), false);
            line.rectTransform.anchoredPosition = new Vector2(x, y);
            line.rectTransform.sizeDelta = new Vector2(width, 8f);
        }

        internal static RectTransform NewRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        internal static UnityEngine.UI.Image NewImage(Transform parent, string name, Sprite sprite, Color color, bool raycast)
        {
            var rect = NewRect(parent, name, Vector2.zero, new Vector2(100f, 100f));
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        internal static TMP_Text NewText(Transform parent, string name, TMP_FontAsset font, string text, float size, Color color,
            Vector2 position, Vector2 box)
        {
            var rect = NewRect(parent, name, position, box);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }
    }

    internal static class MinerSelectRectExtensions
    {
        public static void StretchFull(this RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
