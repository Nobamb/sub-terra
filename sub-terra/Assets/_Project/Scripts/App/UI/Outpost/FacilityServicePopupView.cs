using System.Collections.Generic;
using SubTerra.App.UI.HUD;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 보건소·충전기 서비스 팝업. 아이콘 등장 → 시설별 효과 → 패널 펼침을 재생하고 결과·게이지를 보여 준다.
    /// 표시 전용이다. 회복·충전 처리는 이 연출을 기다리지 않고 이미 끝나 있으며, 여기로는 결과 값만 들어온다.
    /// 모든 시간은 unscaledDeltaTime이라 게임 시간이 멈춘 상태에서도 재생된다.
    /// </summary>
    public sealed class FacilityServicePopupView : MonoBehaviour
    {
        public enum PlayState
        {
            Hidden = 0,
            Playing = 1,
            Settled = 2,
            Exiting = 3
        }

        public const string ClinicTitle = "보건소";
        public const string ChargerTitle = "충전기";
        public const string ClinicDescription = "체력을 최대치까지 회복합니다.";
        public const string ChargerDescription = "전력을 최대치까지 충전합니다.";

        private const float IntroIconScale = 1.45f;
        private const float EcgHalfWidth = 250f;
        private const float EcgAmplitude = 80f;
        private const float MaxStep = 0.05f;
        private const int ArcCount = 5;
        private const int ArcPoints = 7;

        private static readonly Color ClinicGlow = new Color(0.30f, 1f, 0.78f, 1f);
        private static readonly Color ChargerGlow = new Color(0.20f, 0.95f, 1f, 1f);
        private static readonly Color EcgColor = new Color(0.55f, 1f, 0.94f, 1f);
        private static readonly Color ArcColor = new Color(0.78f, 1f, 1f, 1f);
        private static readonly Color ErrorColor = new Color(1f, 0.45f, 0.35f);
        private static readonly Color OkColor = new Color(0.45f, 1f, 0.65f);

        [SerializeField] private CanvasGroup windowGroup;
        [SerializeField] private RectTransform panelBackdrop;
        [SerializeField] private CanvasGroup panelBackdropGroup;
        [SerializeField] private Image panelGlow;
        [SerializeField] private CanvasGroup content;
        [SerializeField] private RectTransform iconRect;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite heartSprite;
        [SerializeField] private Sprite boltSprite;
        [SerializeField] private RectTransform iconGlowRect;
        [SerializeField] private Image iconGlow;
        [SerializeField] private RectTransform flashRingRect;
        [SerializeField] private Image flashRing;
        [SerializeField] private RectTransform flashGlowRect;
        [SerializeField] private Image flashGlow;
        [SerializeField] private FacilityServiceLineGraphic ecgCore;
        [SerializeField] private FacilityServiceLineGraphic ecgGlow;
        [SerializeField] private FacilityServiceLineGraphic arcCore;
        [SerializeField] private FacilityServiceLineGraphic arcGlow;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private GameObject healthGauge;
        [SerializeField] private Image healthFill;
        [SerializeField] private GameObject energyGauge;
        [SerializeField] private Image energyFill;
        [SerializeField] private Button closeButton;

        private readonly List<Vector2> ecgPoints = new List<Vector2>(10);
        private readonly Vector2[][] arcStrands = new Vector2[ArcCount][];

        private PlayState state;
        private OutpostPanelMode mode;
        private float clock;
        private float exitClock;
        private float iconSlotY;
        private bool slotCached;
        private bool showRequested;

        private bool hasVital;
        private float vitalBefore;
        private float vitalAfter;
        private float vitalMax = 1f;
        private float gaugeStart;
        private int lastValueKey = int.MinValue;

        public PlayState State => state;
        public OutpostPanelMode Mode => mode;
        public bool IsShown => state == PlayState.Playing || state == PlayState.Settled;
        public float Clock => clock;
        public Button CloseButton => closeButton;
        public CanvasGroup ContentGroup => content;
        public float DisplayedGaugeFraction => ActiveFill() != null ? ActiveFill().fillAmount : 0f;
        public string ResultMessage => resultText != null ? resultText.text : string.Empty;
        public string ValueMessage => valueText != null ? valueText.text : string.Empty;

        private void Awake()
        {
            EnsureBuffers();
            EnsureArt();
        }

        // EditMode 테스트처럼 Awake가 돌지 않는 경로에서도 안전하게 쓰도록 필요할 때 만든다.
        private void EnsureBuffers()
        {
            for (var i = 0; i < ArcCount; i++)
            {
                if (arcStrands[i] == null)
                {
                    arcStrands[i] = new Vector2[ArcPoints];
                }
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
            // 씬 전환·비활성화로 꺼져도 다음 표시가 깨끗하게 시작되도록 모든 상태를 되돌린다.
            state = PlayState.Hidden;
            clock = 0f;
            exitClock = 0f;
            hasVital = false;
            lastValueKey = int.MinValue;
            ZeroEffects();
            if (windowGroup != null)
            {
                windowGroup.alpha = 1f;
                windowGroup.blocksRaycasts = true;
            }

            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (state == PlayState.Hidden)
            {
                return;
            }

            Tick(Mathf.Min(Time.unscaledDeltaTime, MaxStep));
        }

        /// <summary>연출을 처음부터 재생한다. 이미 같은 시설로 보이는 중이면 아무것도 하지 않는다.</summary>
        public void Show(OutpostPanelMode requested)
        {
            if (!FacilityServicePopupTimeline.IsServiceMode(requested))
            {
                Hide();
                return;
            }

            if (IsShown && mode == requested)
            {
                return;
            }

            mode = requested;
            showRequested = true;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            BeginPlay();
        }

        /// <summary>닫기 연출(잔여 발광 즉시 제거 + 패널 퇴장) 후 비활성화한다.</summary>
        public void Hide()
        {
            if (state == PlayState.Hidden || state == PlayState.Exiting)
            {
                return;
            }

            state = PlayState.Exiting;
            exitClock = 0f;
            ZeroEffects();
            if (windowGroup != null)
            {
                windowGroup.blocksRaycasts = false;
            }

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

        public void SetResult(string message, bool isError)
        {
            if (resultText == null)
            {
                return;
            }

            resultText.text = message ?? string.Empty;
            resultText.color = isError ? ErrorColor : OkColor;
        }

        /// <summary>실제 회복·충전 전후 값. 게이지는 펼침이 끝나는 시점부터 이전→이후로 보간한다.</summary>
        public void SetVital(float before, float after, float maximum)
        {
            vitalMax = maximum > 0f ? maximum : 1f;
            vitalBefore = Mathf.Clamp(before, 0f, vitalMax);
            vitalAfter = Mathf.Clamp(after, 0f, vitalMax);
            hasVital = true;
            lastValueKey = int.MinValue;
            var startAt = FacilityServicePopupTimeline.Evaluate(mode, 0f).GaugeStartTime;
            gaugeStart = Mathf.Max(clock, startAt);
            UpdateGauge();
        }

        /// <summary>테스트와 Update가 같은 경로로 시간을 진행한다.</summary>
        public void Tick(float deltaSeconds)
        {
            if (state == PlayState.Hidden)
            {
                return;
            }

            if (state == PlayState.Exiting)
            {
                exitClock += deltaSeconds;
                var alpha = FacilityServicePopupTimeline.ExitAlpha(exitClock);
                if (windowGroup != null)
                {
                    windowGroup.alpha = alpha;
                }

                transform.localScale = Vector3.one * (0.97f + 0.03f * alpha);
                if (exitClock >= FacilityServicePopupTimeline.ExitDuration)
                {
                    FinishExit();
                }

                return;
            }

            clock += deltaSeconds;
            if (state == PlayState.Playing)
            {
                if (clock >= FacilityServicePopupTimeline.TotalDuration(mode))
                {
                    ApplySettled();
                    state = PlayState.Settled;
                }
                else
                {
                    Apply(clock);
                }
            }

            UpdateGauge();
        }

        private void BeginPlay()
        {
            EnsureBuffers();
            EnsureArt();
            CacheSlot();
            clock = 0f;
            exitClock = 0f;
            state = PlayState.Playing;
            hasVital = false;
            lastValueKey = int.MinValue;
            gaugeStart = 0f;

            var window = (RectTransform)transform;
            window.anchoredPosition = Vector2.zero;
            window.localScale = Vector3.one;
            if (windowGroup != null)
            {
                windowGroup.alpha = 1f;
                windowGroup.blocksRaycasts = true;
            }

            ApplyKindContent();
            if (resultText != null)
            {
                resultText.text = string.Empty;
            }

            Apply(0f);
            UpdateGauge();
        }

        private void FinishExit()
        {
            state = PlayState.Hidden;
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void EnsureArt()
        {
            var soft = MineResetPopupArt.Soft("facility-service", 0.05f);
            SetSprite(iconGlow, soft);
            SetSprite(panelGlow, soft);
            SetSprite(flashGlow, soft);
            SetSprite(flashRing, MineResetPopupArt.ClockRing());
        }

        private static void SetSprite(Image image, Sprite sprite)
        {
            if (image != null && image.sprite == null)
            {
                image.sprite = sprite;
            }
        }

        private void CacheSlot()
        {
            if (slotCached || iconRect == null)
            {
                return;
            }

            iconSlotY = iconRect.anchoredPosition.y;
            slotCached = true;
        }

        private bool IsClinic => mode == OutpostPanelMode.Clinic;

        private Image ActiveFill()
        {
            return IsClinic ? healthFill : energyFill;
        }

        private void ApplyKindContent()
        {
            if (titleText != null)
            {
                titleText.text = IsClinic ? ClinicTitle : ChargerTitle;
            }

            if (descriptionText != null)
            {
                descriptionText.text = IsClinic ? ClinicDescription : ChargerDescription;
            }

            if (icon != null)
            {
                icon.sprite = IsClinic ? heartSprite : boltSprite;
            }

            if (healthGauge != null)
            {
                healthGauge.SetActive(IsClinic);
            }

            if (energyGauge != null)
            {
                energyGauge.SetActive(!IsClinic);
            }

            if (valueText != null)
            {
                valueText.text = string.Empty;
            }
        }

        private void Apply(float t)
        {
            var f = FacilityServicePopupTimeline.Evaluate(mode, t);
            var glow = IsClinic ? ClinicGlow : ChargerGlow;

            ApplyIcon(f, glow);
            ApplyPanel(f, glow);
            if (IsClinic)
            {
                ApplyEcg(f);
                ClearArcs();
            }
            else
            {
                ApplyFlash(f, glow);
                ApplyArcs(f);
                ClearEcg();
            }
        }

        private void ApplyIcon(FacilityServiceFrame f, Color glow)
        {
            if (iconRect == null)
            {
                return;
            }

            var pulseAmount = IsClinic ? 0.12f : 0.10f;
            var position = new Vector2(0f, Mathf.Lerp(0f, iconSlotY, f.IconMove));
            var scale = f.IconScale * Mathf.Lerp(IntroIconScale, 1f, f.IconMove) * (1f + pulseAmount * f.Pulse);
            iconRect.anchoredPosition = position;
            iconRect.localScale = Vector3.one * scale;
            if (icon != null)
            {
                SetAlpha(icon, f.IconAlpha);
            }

            if (iconGlowRect != null)
            {
                iconGlowRect.anchoredPosition = position;
                iconGlowRect.localScale = Vector3.one * (scale * (1f + 0.25f * f.Pulse));
            }

            if (iconGlow != null)
            {
                var alpha = 0.14f * f.IconAlpha * (1f - f.IconMove) + 0.40f * f.Pulse * (1f - 0.5f * f.IconMove);
                iconGlow.color = WithAlpha(glow, Mathf.Clamp01(alpha));
            }
        }

        private void ApplyPanel(FacilityServiceFrame f, Color glow)
        {
            var open = f.PanelOpen;
            if (panelBackdrop != null)
            {
                // 패널 바탕만 펼친다. 글자는 별도 레이어(content)라 늘어나거나 찌그러지지 않는다.
                panelBackdrop.localScale = new Vector3(Mathf.Lerp(0.5f, 1f, open), Mathf.Lerp(0.05f, 1f, open), 1f);
            }

            if (panelBackdropGroup != null)
            {
                panelBackdropGroup.alpha = Mathf.Clamp01(open * 3f);
            }

            if (panelGlow != null)
            {
                panelGlow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.15f, open);
                panelGlow.color = WithAlpha(glow, 0.24f * f.PanelGlow);
            }

            if (content != null)
            {
                content.alpha = f.TextAlpha;
            }
        }

        private void ApplyEcg(FacilityServiceFrame f)
        {
            if (ecgCore == null || ecgGlow == null || f.LineProgress <= 0f || f.LineAlpha <= 0.001f)
            {
                ClearEcg();
                return;
            }

            FacilityServicePopupTimeline.BuildEcg(EcgHalfWidth, EcgAmplitude, f.LineProgress, ecgPoints);
            ecgCore.color = WithAlpha(EcgColor, f.LineAlpha);
            ecgGlow.color = WithAlpha(ClinicGlow, 0.30f * f.LineAlpha);
            ecgCore.SetSingle(ecgPoints);
            ecgGlow.SetSingle(ecgPoints);
        }

        private void ClearEcg()
        {
            if (ecgCore != null)
            {
                ecgCore.Clear();
            }

            if (ecgGlow != null)
            {
                ecgGlow.Clear();
            }
        }

        private void ApplyFlash(FacilityServiceFrame f, Color glow)
        {
            var position = iconRect != null ? iconRect.anchoredPosition : Vector2.zero;
            if (flashRingRect != null)
            {
                flashRingRect.anchoredPosition = position;
                flashRingRect.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.55f, f.FlashSpread);
            }

            if (flashRing != null)
            {
                flashRing.color = WithAlpha(new Color(0.45f, 1f, 1f, 1f), 0.9f * f.Flash);
            }

            if (flashGlowRect != null)
            {
                flashGlowRect.anchoredPosition = position;
                flashGlowRect.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.25f, f.FlashSpread);
            }

            if (flashGlow != null)
            {
                flashGlow.color = WithAlpha(glow, 0.45f * f.Flash);
            }
        }

        private void ApplyArcs(FacilityServiceFrame f)
        {
            if (arcCore == null || arcGlow == null || f.ArcAlpha <= 0.001f)
            {
                ClearArcs();
                return;
            }

            arcCore.BeginStrands();
            arcGlow.BeginStrands();
            for (var i = 0; i < ArcCount; i++)
            {
                // 아크마다 조각이 깜빡이며 불규칙하게 튄다.
                if (FacilityServicePopupTimeline.Hash01(f.ArcStep * 31 + i * 7 + 3) < 0.22f)
                {
                    continue;
                }

                BuildArc(arcStrands[i], f.ArcStep, i);
                arcCore.AddStrand(arcStrands[i]);
                arcGlow.AddStrand(arcStrands[i]);
            }

            arcCore.color = WithAlpha(ArcColor, f.ArcAlpha);
            arcGlow.color = WithAlpha(ChargerGlow, 0.35f * f.ArcAlpha);
            arcCore.EndStrands();
            arcGlow.EndStrands();
        }

        private static void BuildArc(Vector2[] strand, int step, int index)
        {
            var seed = step * 97 + index * 13;
            var baseAngle = index * (Mathf.PI * 2f / ArcCount) + 0.5f
                + (FacilityServicePopupTimeline.Hash01(seed) - 0.5f) * 0.7f;
            var radius = 54f + 8f * FacilityServicePopupTimeline.Hash01(seed + 1);
            var tangent = index % 2 == 0;
            var span = 0.5f + 0.35f * FacilityServicePopupTimeline.Hash01(seed + 2);
            var length = 30f + 32f * FacilityServicePopupTimeline.Hash01(seed + 3);
            for (var p = 0; p < strand.Length; p++)
            {
                var k = p / (float)(strand.Length - 1);
                var jitter = (p == 0 || p == strand.Length - 1)
                    ? 0f
                    : (FacilityServicePopupTimeline.Hash01(seed + 10 + p) - 0.5f) * 14f;
                Vector2 point;
                if (tangent)
                {
                    // 아이콘 가장자리를 따라 호를 그리며 반지름 방향으로만 흔들린다.
                    var angle = baseAngle + (k - 0.5f) * span;
                    var r = radius + jitter;
                    point = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                }
                else
                {
                    var dir = new Vector2(Mathf.Cos(baseAngle), Mathf.Sin(baseAngle));
                    var normal = new Vector2(-dir.y, dir.x);
                    point = dir * (radius + length * k) + normal * jitter;
                }

                strand[p] = point;
            }
        }

        private void ClearArcs()
        {
            if (arcCore != null)
            {
                arcCore.Clear();
            }

            if (arcGlow != null)
            {
                arcGlow.Clear();
            }
        }

        private void ZeroEffects()
        {
            ClearEcg();
            ClearArcs();
            if (iconGlow != null)
            {
                iconGlow.color = WithAlpha(iconGlow.color, 0f);
            }

            if (panelGlow != null)
            {
                panelGlow.color = WithAlpha(panelGlow.color, 0f);
            }

            if (flashRing != null)
            {
                flashRing.color = WithAlpha(flashRing.color, 0f);
            }

            if (flashGlow != null)
            {
                flashGlow.color = WithAlpha(flashGlow.color, 0f);
            }
        }

        /// <summary>끝난 상태: 강한 효과는 모두 끄고 패널과 글자만 안정적으로 남긴다.</summary>
        private void ApplySettled()
        {
            Apply(FacilityServicePopupTimeline.TotalDuration(mode));
            ZeroEffects();
            if (content != null)
            {
                content.alpha = 1f;
            }
        }

        private void UpdateGauge()
        {
            var fill = ActiveFill();
            if (!hasVital)
            {
                if (fill != null)
                {
                    fill.fillAmount = 0f;
                }

                return;
            }

            var k = Mathf.Clamp01((clock - gaugeStart) / FacilityServicePopupTimeline.GaugeDuration);
            var eased = 1f - (1f - k) * (1f - k) * (1f - k);
            var value = Mathf.Lerp(vitalBefore, vitalAfter, eased);
            if (fill != null)
            {
                fill.fillAmount = vitalMax > 0f ? Mathf.Clamp01(value / vitalMax) : 0f;
            }

            if (valueText == null)
            {
                return;
            }

            // 숫자가 바뀔 때만 문자열을 다시 만든다.
            var key = IsClinic ? Mathf.CeilToInt(value) : Mathf.RoundToInt(value);
            if (key == lastValueKey)
            {
                return;
            }

            lastValueKey = key;
            var maxInt = Mathf.RoundToInt(vitalMax);
            valueText.text = IsClinic
                ? HudFormatter.FormatHealth(value, maxInt)
                : HudFormatter.FormatEnergy(key, maxInt);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            graphic.color = WithAlpha(graphic.color, alpha);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
