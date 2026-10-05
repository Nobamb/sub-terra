using SubTerra.App.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyRescue
{
    /// <summary>
    /// 플레이어 머리 위 구출 홀로그램 안내. 클릭하면 구출 팝업을 다시 여는 버튼이며 구출을 직접 실행하지 않는다.
    /// 위 문구·가운데 픽토그램(사람·화살표·발광)·아래 키캡이 각각 별도 오브젝트라 따로 움직이고,
    /// 실제 입력 처리는 이 클래스 밖(컨트롤러)에 있어 입력 안내만 바꿔 끼울 수 있다.
    /// 시간은 모두 unscaledDeltaTime이라 게임 시간이 멈춰도 재생된다.
    /// </summary>
    [DefaultExecutionOrder(210)]
    public sealed class EmergencyRescueChipView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const string ObjectName = "EmergencyRescueChip";
        public const string TitleText = "구출 요청";
        public const string DefaultKeyLabel = "R";
        public static readonly Vector2 Size = new Vector2(132f, 168f);

        public const float HumanRise = 10f;
        public const float ArrowRise = 12f;
        public const float KeycapIdleDrop = 3f;
        public const float KeycapPressDrop = 6f;

        private const float MaxStep = 0.05f;
        private const float HumanBaseY = -8f;
        private const float ArrowBaseY = 26f;

        private static readonly Color Fill = new Color(0.02f, 0.10f, 0.12f, 0.78f);
        private static readonly Color Edge = new Color(0.25f, 0.95f, 0.92f, 0.55f);
        private static readonly Color Accent = new Color(0.62f, 1f, 0.97f, 1f);
        private static readonly Color HumanColor = new Color(0.55f, 0.95f, 1f, 0.95f);
        private static readonly Color KeyTop = new Color(0.06f, 0.17f, 0.22f, 1f);
        private static readonly Color KeySide = new Color(0.02f, 0.07f, 0.10f, 1f);

        private CanvasGroup rootGroup;
        private CanvasGroup holoGroup;
        private CanvasGroup contentGroup;
        private RectTransform holo;
        private Image glow;
        private Image[] brackets;
        private Image[] edges;
        private Image[] redTicks;
        private Image pictoGlow;
        private Image[] rails;
        private Image platform;
        private RectTransform human;
        private Image humanImage;
        private RectTransform arrow;
        private Image arrowImage;
        private RectTransform keycapBody;
        private Image keycapGlow;
        private TMP_Text keycapText;
        private TMP_Text titleLabel;

        private bool shown;
        private bool dismissing;
        private bool hovered;
        private float showLevel;
        private float hoverLevel;
        private float dismissClock;
        private float clock;
        private float idleClock;
        private int idleCycle;
        private float idleInterval = EmergencyRescueTimeline.KeycapFirstDelay;
        private float pulseClock = -1f;
        private float pressClock = -1f;

        /// <summary>화면에 표시 중(등장·유지)이고 사라지는 중이 아님.</summary>
        public bool IsShown => shown && !dismissing;
        public bool IsDismissing => dismissing;
        public bool IsHovered => hovered;
        public float ShowLevel => showLevel;
        public float HoverLevel => hoverLevel;
        public bool IsKeycapPulsing => pulseClock >= 0f;
        public bool IsPressFeedbackPlaying => pressClock >= 0f;
        public RectTransform HumanRect => human;
        public RectTransform ArrowRect => arrow;
        public RectTransform KeycapRect => keycapBody;
        public TMP_Text TitleLabel => titleLabel;
        public TMP_Text KeycapLabel => keycapText;
        public float PictoGlowAlpha => pictoGlow != null ? pictoGlow.color.a : 0f;

        public static EmergencyRescueChipView Create(Transform canvasRoot, TMP_FontAsset font)
        {
            Transform parent = canvasRoot;
            RectTransform rect = EmergencyRescueUi.Rect(parent, ObjectName, Vector2.zero, Size);
            rect.pivot = new Vector2(0.5f, 0f);

            var rootImage = rect.gameObject.AddComponent<Image>();
            // 홀로그램 전체를 클릭 영역으로 쓴다. 보이지 않는 아주 옅은 면이며 자식 장식은 입력을 받지 않는다.
            rootImage.color = new Color(0f, 0f, 0f, 0.004f);
            rootImage.raycastTarget = true;

            var canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = UiLayerPriority.CriticalHazard;
            rect.gameObject.AddComponent<GraphicRaycaster>();
            rect.gameObject.AddComponent<EmergencyRescueChipFollow>();
            var group = rect.gameObject.AddComponent<CanvasGroup>();

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rootImage;
            button.transition = Selectable.Transition.None;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            var view = rect.gameObject.AddComponent<EmergencyRescueChipView>();
            view.rootGroup = group;
            view.Build(font);
            rect.gameObject.SetActive(false);
            return view;
        }

        public Button Button => GetComponent<Button>();

        /// <summary>입력 안내 키 표시만 교체한다. 실제 입력 처리는 컨트롤러가 맡는다.</summary>
        public void SetInputHint(string keyLabel)
        {
            if (keycapText != null)
            {
                keycapText.text = string.IsNullOrEmpty(keyLabel) ? DefaultKeyLabel : keyLabel;
            }
        }

        public void Show()
        {
            if (IsShown)
            {
                return;
            }

            ResetState();
            shown = true;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            transform.SetAsLastSibling();
            var follow = GetComponent<EmergencyRescueChipFollow>();
            if (follow != null)
            {
                follow.Tick();
            }

            Apply();
        }

        /// <summary>연출 없이 즉시 숨기고 모든 반복 효과를 정리한다.</summary>
        public void HideImmediate()
        {
            ResetState();
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 입력을 받아 팝업이 열리는 순간 호출한다. 호버·반복 안내를 끝내고 즉시 입력을 막은 뒤
        /// (withPress면 키캡이 분명히 눌렸다가) 짧게 사라진다. 팝업 열기를 기다리게 하지 않는다.
        /// </summary>
        public void Dismiss(bool withPress)
        {
            if (!IsShown)
            {
                return;
            }

            if (!isActiveAndEnabled)
            {
                HideImmediate();
                return;
            }

            dismissing = true;
            dismissClock = 0f;
            hovered = false;
            pulseClock = -1f;
            pressClock = withPress ? 0f : -1f;
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsShown)
            {
                hovered = true;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
        }

        private void OnDisable()
        {
            ResetState();
        }

        /// <summary>확인용: true면 Update가 시간을 진행하지 않아 테스트가 Tick으로 프레임을 직접 진행한다.</summary>
        public bool ManualTick { get; set; }

        private void Update()
        {
            if (ManualTick)
            {
                return;
            }

            Tick(Mathf.Min(Time.unscaledDeltaTime, MaxStep));
        }

        /// <summary>테스트와 Update가 같은 경로로 시간을 진행한다.</summary>
        public void Tick(float dt)
        {
            if (!shown && !dismissing)
            {
                return;
            }

            clock += dt;
            if (dismissing)
            {
                dismissClock += dt;
                if (pressClock >= 0f)
                {
                    pressClock += dt;
                    if (pressClock >= EmergencyRescueTimeline.PressFeedbackDuration)
                    {
                        pressClock = -1f;
                    }
                }

                if (dismissClock >= EmergencyRescueTimeline.ChipDismissDuration)
                {
                    HideImmediate();
                    return;
                }

                Apply();
                return;
            }

            showLevel = Mathf.MoveTowards(showLevel, 1f, dt / EmergencyRescueTimeline.ChipShowDuration);
            hoverLevel = EmergencyRescueTimeline.AdvanceHover(hoverLevel, hovered && showLevel >= 1f, dt);
            TickKeycap(dt);
            Apply();
        }

        // 반복 안내는 등장이 끝난 뒤 3~5초 간격으로 한 번씩만 시각적으로 눌린다. 입력은 만들지 않는다.
        private void TickKeycap(float dt)
        {
            if (pulseClock >= 0f)
            {
                pulseClock += dt;
                if (pulseClock >= EmergencyRescueTimeline.KeycapPulseDuration)
                {
                    pulseClock = -1f;
                    idleClock = 0f;
                    idleCycle++;
                    idleInterval = EmergencyRescueTimeline.KeycapInterval(idleCycle);
                }

                return;
            }

            if (showLevel < 1f)
            {
                return;
            }

            idleClock += dt;
            if (idleClock >= idleInterval)
            {
                pulseClock = 0f;
            }
        }

        private void ResetState()
        {
            shown = false;
            dismissing = false;
            hovered = false;
            showLevel = 0f;
            hoverLevel = 0f;
            dismissClock = 0f;
            clock = 0f;
            idleClock = 0f;
            idleCycle = 0;
            idleInterval = EmergencyRescueTimeline.KeycapFirstDelay;
            pulseClock = -1f;
            pressClock = -1f;
            if (rootGroup != null)
            {
                rootGroup.alpha = 1f;
                rootGroup.blocksRaycasts = true;
                rootGroup.interactable = true;
            }
        }

        private void Apply()
        {
            EmergencyRescueChipShowFrame show = EmergencyRescueTimeline.ChipShow(showLevel);
            EmergencyRescueHoverFrame hover = EmergencyRescueTimeline.Hover(hoverLevel);
            var breathe = 0.925f + 0.075f * Mathf.Sin(clock * 2.6f);

            holo.localScale = new Vector3(1f, Mathf.Lerp(0.15f, 1f, show.Unfold), 1f);
            holoGroup.alpha = show.FrameAlpha;
            contentGroup.alpha = show.ContentAlpha;
            rootGroup.alpha = dismissing
                ? 1f - Mathf.SmoothStep(0f, 1f, dismissClock / EmergencyRescueTimeline.ChipDismissDuration)
                : 1f;

            var idleDepth = EmergencyRescueTimeline.KeycapPulse(
                pulseClock, EmergencyRescueTimeline.KeycapPulseDuration);
            var pressDepth = EmergencyRescueTimeline.KeycapPulse(
                pressClock, EmergencyRescueTimeline.PressFeedbackDuration);

            var glowAlpha = (0.26f + 0.3f * pressDepth) * breathe;
            glow.color = EmergencyRescueUi.WithAlpha(EmergencyRescueUi.TealDeep, glowAlpha);
            var bracketAlpha = Mathf.Clamp01(show.Corner * breathe + 0.25f * pressDepth);
            for (var i = 0; i < brackets.Length; i++)
            {
                brackets[i].color = EmergencyRescueUi.WithAlpha(Accent, bracketAlpha);
            }

            for (var i = 0; i < edges.Length; i++)
            {
                edges[i].color = EmergencyRescueUi.WithAlpha(Edge, Edge.a * breathe + 0.3f * pressDepth);
            }

            // 픽토그램: 발광 → 사람 → 화살표. 호버 레벨 하나로 값이 정해져 연타해도 쌓이지 않는다.
            pictoGlow.color = EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Teal, 0.14f + 0.5f * hover.Glow);
            pictoGlow.rectTransform.localScale = Vector3.one * (0.8f + 0.45f * hover.Glow);
            human.anchoredPosition = new Vector2(0f, HumanBaseY + HumanRise * hover.Human);
            arrow.anchoredPosition = new Vector2(0f, ArrowBaseY + ArrowRise * hover.Arrow);
            humanImage.color = Color.Lerp(HumanColor, Color.white, 0.55f * hover.Glow);
            arrowImage.color = Color.Lerp(HumanColor, Color.white, 0.55f * hover.Glow);
            for (var i = 0; i < rails.Length; i++)
            {
                rails[i].color = EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Teal, 0.38f + 0.3f * hover.Glow);
            }

            platform.color = EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Teal, 0.55f + 0.3f * hover.Glow);

            // 키캡: 반복 안내(약하게)와 실제 입력(분명하게)을 한 몸체에 합치되 큰 쪽만 따른다.
            var drop = Mathf.Max(KeycapIdleDrop * idleDepth, KeycapPressDrop * pressDepth);
            var scale = Mathf.Min(1f - 0.08f * idleDepth, 1f - 0.16f * pressDepth);
            keycapBody.anchoredPosition = new Vector2(0f, -drop);
            keycapBody.localScale = new Vector3(scale, scale, 1f);
            keycapGlow.color = EmergencyRescueUi.WithAlpha(
                EmergencyRescueUi.Teal, Mathf.Max(0.85f * idleDepth, pressDepth));

            for (var i = 0; i < redTicks.Length; i++)
            {
                redTicks[i].color = EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Red, 0.75f * show.FrameAlpha);
            }
        }

        private void Build(TMP_FontAsset font)
        {
            var w = Size.x;
            var h = Size.y;

            holo = EmergencyRescueUi.Rect(transform, "Holo", Vector2.zero, Size);
            holoGroup = holo.gameObject.AddComponent<CanvasGroup>();
            holoGroup.interactable = false;
            holoGroup.blocksRaycasts = false;
            glow = EmergencyRescueUi.Image(
                holo, "Glow", EmergencyRescueArt.Soft(), Vector2.zero, new Vector2(w + 44f, h + 44f), EmergencyRescueUi.TealDeep);
            EmergencyRescueUi.Image(holo, "Fill", null, Vector2.zero, Size, Fill);

            edges = new Image[4];
            edges[0] = EmergencyRescueUi.Image(holo, "EdgeTop", null, new Vector2(0f, h * 0.5f - 0.75f), new Vector2(w, 1.5f), Edge);
            edges[1] = EmergencyRescueUi.Image(holo, "EdgeBottom", null, new Vector2(0f, -h * 0.5f + 0.75f), new Vector2(w, 1.5f), Edge);
            edges[2] = EmergencyRescueUi.Image(holo, "EdgeLeft", null, new Vector2(-w * 0.5f + 0.75f, 0f), new Vector2(1.5f, h), Edge);
            edges[3] = EmergencyRescueUi.Image(holo, "EdgeRight", null, new Vector2(w * 0.5f - 0.75f, 0f), new Vector2(1.5f, h), Edge);

            // 모서리 브래킷 8개: 모서리마다 가로·세로 한 쌍.
            brackets = new Image[8];
            const float length = 16f;
            const float thick = 3f;
            for (var c = 0; c < 4; c++)
            {
                var sx = (c & 1) == 1 ? 1f : -1f;
                var sy = (c & 2) == 2 ? 1f : -1f;
                var cx = sx * (w * 0.5f - length * 0.5f);
                var cy = sy * (h * 0.5f - thick * 0.5f);
                brackets[c * 2] = EmergencyRescueUi.Image(
                    holo, "BracketH" + c, null, new Vector2(cx, cy), new Vector2(length, thick), Accent);
                var vx = sx * (w * 0.5f - thick * 0.5f);
                var vy = sy * (h * 0.5f - length * 0.5f);
                brackets[c * 2 + 1] = EmergencyRescueUi.Image(
                    holo, "BracketV" + c, null, new Vector2(vx, vy), new Vector2(thick, length), Accent);
            }

            // 붉은 경고색은 윗모서리 안쪽의 작은 눈금 둘뿐이다.
            redTicks = new Image[2];
            redTicks[0] = EmergencyRescueUi.Image(
                holo, "RedTickL", null, new Vector2(-w * 0.5f + 30f, h * 0.5f - 8f), new Vector2(12f, 3f), EmergencyRescueUi.Red);
            redTicks[1] = EmergencyRescueUi.Image(
                holo, "RedTickR", null, new Vector2(w * 0.5f - 30f, h * 0.5f - 8f), new Vector2(12f, 3f), EmergencyRescueUi.Red);

            RectTransform content = EmergencyRescueUi.Rect(transform, "Content", Vector2.zero, Size);
            contentGroup = content.gameObject.AddComponent<CanvasGroup>();
            contentGroup.interactable = false;
            contentGroup.blocksRaycasts = false;

            titleLabel = EmergencyRescueUi.Text(
                content, "Label", font, 21f, FontStyles.Bold, EmergencyRescueUi.TextMain,
                TextAlignmentOptions.Center, new Vector2(0f, 66f), new Vector2(w - 8f, 30f));
            titleLabel.text = TitleText;

            // 픽토그램(가운데): 엘리베이터 레일·발판 위의 사람과 위쪽 화살표.
            RectTransform picto = EmergencyRescueUi.Rect(content, "Pictogram", Vector2.zero, new Vector2(96f, 96f));
            pictoGlow = EmergencyRescueUi.Image(
                picto, "PictoGlow", EmergencyRescueArt.Soft(), new Vector2(0f, 2f), new Vector2(110f, 110f), EmergencyRescueUi.Teal);
            rails = new Image[2];
            rails[0] = EmergencyRescueUi.Image(picto, "RailL", null, new Vector2(-28f, 6f), new Vector2(2f, 76f), EmergencyRescueUi.Teal);
            rails[1] = EmergencyRescueUi.Image(picto, "RailR", null, new Vector2(28f, 6f), new Vector2(2f, 76f), EmergencyRescueUi.Teal);
            platform = EmergencyRescueUi.Image(picto, "Platform", null, new Vector2(0f, -31f), new Vector2(60f, 3f), EmergencyRescueUi.Teal);
            humanImage = EmergencyRescueUi.Image(
                picto, "Human", EmergencyRescueArt.Human(), new Vector2(0f, HumanBaseY), new Vector2(42f, 42f), HumanColor);
            human = humanImage.rectTransform;
            arrowImage = EmergencyRescueUi.Image(
                picto, "Arrow", EmergencyRescueArt.Arrow(), new Vector2(0f, ArrowBaseY), new Vector2(18f, 24f), HumanColor);
            arrow = arrowImage.rectTransform;

            // 입력 안내(아래): 키캡. 본체와 발광이 따로라 눌림·발광이 호버 연출과 섞이지 않는다.
            RectTransform key = EmergencyRescueUi.Rect(content, "InputHint", new Vector2(0f, -56f), new Vector2(60f, 46f));
            keycapGlow = EmergencyRescueUi.Image(
                key, "KeycapGlow", EmergencyRescueArt.Soft(), Vector2.zero, new Vector2(76f, 62f), EmergencyRescueUi.Teal);
            keycapBody = EmergencyRescueUi.Rect(key, "KeycapBody", Vector2.zero, new Vector2(40f, 34f));
            Image side = EmergencyRescueUi.Image(
                keycapBody, "KeycapSide", EmergencyRescueArt.RoundedRect(), new Vector2(0f, -3f), new Vector2(40f, 32f), KeySide);
            side.type = Image.Type.Sliced;
            Image top = EmergencyRescueUi.Image(
                keycapBody, "KeycapTop", EmergencyRescueArt.RoundedRect(), Vector2.zero, new Vector2(40f, 32f), KeyTop);
            top.type = Image.Type.Sliced;
            Image rim = EmergencyRescueUi.Image(
                keycapBody, "KeycapRim", EmergencyRescueArt.RoundedRect(), Vector2.zero, new Vector2(40f, 32f),
                EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Teal, 0.22f));
            rim.type = Image.Type.Sliced;
            keycapText = EmergencyRescueUi.Text(
                keycapBody, "KeyLabel", font, 22f, FontStyles.Bold, Color.white,
                TextAlignmentOptions.Center, Vector2.zero, new Vector2(40f, 32f));
            keycapText.text = DefaultKeyLabel;
            Apply();
        }
    }
}
