using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// B-150 메인화면 켜짐/꺼짐 연출 적용기. 시간 계산은 MainMenuBootTimeline·MainMenuExitTimeline이 하고
    /// 여기서는 셔터·문구·카드·버튼에 값만 적용한다. 연출 중에는 메뉴 입력을 막는다.
    /// </summary>
    public sealed class MainMenuIntroDirector : MonoBehaviour
    {
        [SerializeField] private MainMenuBootSettings bootSettings = new MainMenuBootSettings();
        [SerializeField] private MainMenuExitSettings exitSettings = new MainMenuExitSettings();

        [Header("Screen")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private RectTransform topShutter;
        [SerializeField] private RectTransform bottomShutter;
        [SerializeField] private RectTransform scanLine;
        [SerializeField] private UnityEngine.UI.Graphic scanLineGraphic;
        [SerializeField] private CanvasGroup menuGroup;

        [Header("Content")]
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private string headerConnecting = "SUB-TERRA OPS // 신호 연결 중...";
        [SerializeField] private string headerConnected = "SUB-TERRA OPS // 신호 연결됨";
        [SerializeField] private CanvasGroup logoGroup;
        [SerializeField] private RectTransform logoGlitchTarget;
        [SerializeField] private float logoGlitchOffset = 9f;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private SaveSlotCardView[] cards = new SaveSlotCardView[3];
        [SerializeField] private CanvasGroup continueGroup;
        [SerializeField] private CanvasGroup[] secondaryGroups = new CanvasGroup[3];

        private MainMenuBootTimeline boot;
        private MainMenuExitTimeline exit;
        private bool booting;
        private bool exiting;
        private Action exitClosed;
        private int exitCardIndex = -1;
        private int inputResumeFrame = -1;
        private Vector2 logoHome;
        private bool hasLogoHome;
        private float glitchStep;
        private readonly bool[] revealStarted = new bool[3];

        public bool IsBooting => booting;
        public bool IsExiting => exiting;
        /// <summary>건너뛰기 입력이 같은 프레임에 메뉴 버튼까지 누르지 않도록 다음 프레임까지 막는다.</summary>
        public bool BlocksInput => booting || exiting || Time.frameCount <= inputResumeFrame;
        public MainMenuBootTimeline Boot => boot ?? (boot = new MainMenuBootTimeline(bootSettings));
        public event Action BootSkipped;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                ApplyFinal();
                return;
            }

            try
            {
                StartBoot();
            }
            catch (Exception ex)
            {
                // 연출 실패가 메뉴 사용을 막지 않도록 끝 상태로 둔다.
                Debug.LogError("[MainMenuIntroDirector] Boot start failed: " + ex);
                ApplyFinal();
            }
        }

        private void OnDisable()
        {
            booting = false;
            exiting = false;
            exitClosed = null;
        }

        public void StartBoot()
        {
            Boot.Restart();
            exiting = false;
            booting = true;
            for (var i = 0; i < revealStarted.Length; i++) revealStarted[i] = false;
            SetMenuInput(false);
            ApplyBoot();
        }

        /// <summary>아무 키나 클릭: 즉시 끝 상태로 건너뛴다.</summary>
        public void SkipBoot()
        {
            if (!booting) return;
            Boot.Skip();
            for (var i = 0; i < cards.Length; i++)
                if (cards[i] != null) cards[i].FinishThumbnailReveal();
            ApplyBoot();
            FinishBoot();
            BootSkipped?.Invoke();
        }

        /// <summary>선택 카드가 번쩍인 뒤 화면이 가로선으로 접히고, 다 접히면 onClosed를 한 번 호출한다.</summary>
        public void PlayExit(int selectedCardIndex, Action onClosed)
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                onClosed?.Invoke();
                return;
            }

            if (booting) SkipBoot();
            exit = exit ?? new MainMenuExitTimeline(exitSettings);
            exit.Restart();
            exitCardIndex = selectedCardIndex;
            exitClosed = onClosed;
            exiting = true;
            SetMenuInput(false);
            ApplyExit();
        }

        /// <summary>이어하기·새 게임이 실패하면 접힌 화면을 다시 연다.</summary>
        public void CancelExit()
        {
            exiting = false;
            exitClosed = null;
            if (exitCardIndex >= 0 && exitCardIndex < cards.Length && cards[exitCardIndex] != null)
                cards[exitCardIndex].SetExitFlash(0f);
            SetShutters(1f, 0f, 0f);
            SetMenuInput(true);
        }

        /// <summary>카드 index가 충분히 나타났는지. 프레임은 카드가 보인 뒤에 붙는다.</summary>
        public bool IsCardShown(int index)
        {
            return !booting || Boot.CardAlpha(index) >= 0.6f;
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            if (booting)
            {
                if (SkipPressed())
                {
                    SkipBoot();
                    return;
                }

                Boot.Advance(dt);
                ApplyBoot();
                if (Boot.IsComplete) FinishBoot();
            }
            else if (exiting)
            {
                exit.Advance(dt);
                ApplyExit();
                if (exit.IsComplete)
                {
                    exiting = false;
                    var closed = exitClosed;
                    exitClosed = null;
                    try
                    {
                        closed?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[MainMenuIntroDirector] Exit callback failed: " + ex);
                        CancelExit();
                    }
                }
            }
        }

        private static bool SkipPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) return true;
            var mouse = Mouse.current;
            return mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame);
        }

        private void FinishBoot()
        {
            booting = false;
            inputResumeFrame = Time.frameCount;
            SetShutters(1f, 0f, 0f);
            SetMenuInput(true);
        }

        private void ApplyFinal()
        {
            booting = false;
            Boot.Skip();
            ApplyBoot();
            SetShutters(1f, 0f, 0f);
            if (menuGroup != null) menuGroup.blocksRaycasts = true;
        }

        private void ApplyBoot()
        {
            var frame = Boot.Sample();
            SetShutters(frame.Open, frame.LineWidth, frame.LineAlpha);
            if (headerText != null)
            {
                headerText.text = frame.HeaderConnected ? headerConnected : headerConnecting;
                headerText.maxVisibleCharacters = frame.HeaderConnected
                    ? 99999
                    : Mathf.CeilToInt(headerConnecting.Length * frame.HeaderTyping);
            }

            ApplyLogo(frame);
            if (subtitleText != null)
            {
                var length = subtitleText.text != null ? subtitleText.text.Length : 0;
                subtitleText.maxVisibleCharacters = frame.IsComplete ? 99999 : Mathf.CeilToInt(length * frame.SubtitleTyping);
            }

            for (var i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;
                var alpha = Boot.CardAlpha(i);
                cards[i].SetIntro(alpha, Boot.CardOffset(i));
                if (alpha > 0f && i < revealStarted.Length && !revealStarted[i])
                {
                    revealStarted[i] = true;
                    cards[i].BeginThumbnailReveal();
                }
            }

            if (continueGroup != null) continueGroup.alpha = Boot.ContinueAlpha();
            for (var i = 0; i < secondaryGroups.Length; i++)
                if (secondaryGroups[i] != null) secondaryGroups[i].alpha = Boot.SecondaryAlpha(i);
        }

        private void ApplyLogo(MainMenuBootFrame frame)
        {
            if (logoGroup != null)
            {
                // 글리치 중에는 짧게 깜빡이며 또렷해진다.
                var flicker = frame.LogoGlitch > 0f && Mathf.Repeat(Boot.Elapsed * 37f, 1f) < 0.3f ? 0.55f : 1f;
                logoGroup.alpha = frame.LogoAlpha * flicker;
            }

            if (logoGlitchTarget == null) return;
            if (!hasLogoHome)
            {
                logoHome = logoGlitchTarget.anchoredPosition;
                hasLogoHome = true;
            }

            if (frame.LogoGlitch <= 0f)
            {
                logoGlitchTarget.anchoredPosition = logoHome;
                return;
            }

            glitchStep += Time.unscaledDeltaTime;
            if (glitchStep < 0.03f) return;
            glitchStep = 0f;
            var jitter = UnityEngine.Random.Range(-1f, 1f) * logoGlitchOffset * frame.LogoGlitch;
            logoGlitchTarget.anchoredPosition = logoHome + new Vector2(jitter, 0f);
        }

        private void ApplyExit()
        {
            var frame = exit.Sample();
            if (exitCardIndex >= 0 && exitCardIndex < cards.Length && cards[exitCardIndex] != null)
                cards[exitCardIndex].SetExitFlash(frame.CardFlash);
            SetShutters(frame.Open, frame.LineWidth, frame.LineAlpha);
        }

        private void SetShutters(float open, float lineWidth, float lineAlpha)
        {
            var closed = open < 0.999f || lineAlpha > 0.001f;
            if (overlayRoot != null && overlayRoot.activeSelf != closed) overlayRoot.SetActive(closed);
            if (!closed) return;
            var half = 0.5f * Mathf.Clamp01(open);
            if (topShutter != null)
            {
                topShutter.anchorMin = new Vector2(0f, 0.5f + half);
                topShutter.anchorMax = Vector2.one;
                topShutter.offsetMin = topShutter.offsetMax = Vector2.zero;
            }

            if (bottomShutter != null)
            {
                bottomShutter.anchorMin = Vector2.zero;
                bottomShutter.anchorMax = new Vector2(1f, 0.5f - half);
                bottomShutter.offsetMin = bottomShutter.offsetMax = Vector2.zero;
            }

            if (scanLine != null)
            {
                var w = Mathf.Clamp01(lineWidth);
                scanLine.anchorMin = new Vector2(0.5f - w * 0.5f, 0.5f);
                scanLine.anchorMax = new Vector2(0.5f + w * 0.5f, 0.5f);
            }

            if (scanLineGraphic != null)
            {
                var c = scanLineGraphic.color;
                c.a = Mathf.Clamp01(lineAlpha);
                scanLineGraphic.color = c;
            }
        }

        private void SetMenuInput(bool enabled)
        {
            if (menuGroup != null) menuGroup.blocksRaycasts = enabled;
        }
    }
}
