using System;
using SubTerra.App.Core.Data;
using SubTerra.App.Outpost;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 재사용 대기 안내 팝업. 남은 시간은 저장된 쿨타임을 매 프레임 읽기만 하고, 팝업이 시간을 따로 세지 않는다.
    /// 상태는 TV 진행값(progress) 하나와 목표 on/off로 표현하며 코루틴·콜백을 쓰지 않는다.
    /// 입력은 받지 않는다(모든 Graphic raycastTarget 해제, Stage CanvasGroup blocksRaycasts 해제).
    /// </summary>
    public sealed class FacilityCooldownPopupView : MonoBehaviour, IFacilityCooldownPopupView
    {
        /// <summary>HUD 위, 모달 팝업(EmergencyRescue 31,000) 아래.</summary>
        public const int SortingOrder = 30_600;

        private const float WindowWidth = 400f;
        private const float WindowHeight = 210f;
        private const float HalfWidth = WindowWidth * 0.5f;
        private const float HalfHeight = WindowHeight * 0.5f;
        private const int ParticleCount = 14;
        private const int OuterParticleCount = 20;
        private const int FlowCount = 4;

        // 프레임 바깥으로 번지는 후광의 도달 거리(캔버스 단위)와 바깥 입자가 떠다니는 거리.
        private const float HaloReach = 56f;
        private const float HaloMaxAlpha = 0.85f;
        private const float InnerGlowAlphaScale = 0.5f;
        private const float OuterMoteMinOffset = 3f;
        private const float OuterMoteTravel = 40f;
        private const float GlowRatePerSecond = 6f;
        private const float FrameCornerPx = 26f;

        // 안쪽 가장 바깥 띠 빛의 화면 두께(캔버스 단위). 30초에는 얇고 0초에 가장 두껍다.
        private const float InnerEdgeMinPx = 6f;
        private const float InnerEdgeMaxPx = 30f;

        // 7-segment 시계. 광산 초기화 시계 숫자(24x36)를 1.5배로 키운 모양이다.
        private const int MaxDigits = 6;
        private const int MaxColons = 2;
        private const int MaxDisplaySeconds = 99 * 3600 + 59 * 60 + 59;
        private const float DigitWidth = 36f;
        private const float ColonWidth = 12f;
        private const float DigitGap = 9f;
        private const float DigitCenterY = -5f;
        private const float SegmentOffAlpha = 0.08f;
        private const string Caption = "재사용까지 남은 시간";

        // 단색 반투명 배경. 지형이 살짝 비치되 글자가 읽히는 범위(알파 0.84).
        private const float PlateAlpha = 0.84f;
        private static readonly Color PlateColor = new Color(0.02f, 0.035f, 0.06f, PlateAlpha);
        private static readonly Color FrameBlueWhite = new Color(0.72f, 0.86f, 1f, 1f);
        private static readonly Color FrameRestColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        private static readonly Color DeepBlue = new Color(0.30f, 0.56f, 1f, 1f);
        private static readonly Color BeamTint = new Color(0.80f, 0.98f, 1f, 1f);

        private readonly Image[] particles = new Image[ParticleCount];
        private readonly float[] particlePhase = new float[ParticleCount];
        private readonly float[] particleLane = new float[ParticleCount];
        private readonly Image[] flows = new Image[FlowCount];
        private readonly RectTransform[] digitRoots = new RectTransform[MaxDigits];
        private readonly Image[] digitSegments = new Image[MaxDigits * 7];
        private readonly RectTransform[] colonRoots = new RectTransform[MaxColons];
        private readonly Image[] colonDots = new Image[MaxColons * 2];
        private readonly Image[] outerParticles = new Image[OuterParticleCount];
        private readonly float[] outerPhase = new float[OuterParticleCount];
        private readonly float[] outerLane = new float[OuterParticleCount];

        private Func<string, Sprite> iconResolver;
        private TMP_FontAsset font;
        private bool built;

        private RectTransform stage, window, content, effects;
        private RectMask2D windowMask;
        private CanvasGroup stageGroup, frameGroup, readoutGroup, effectsGroup;
        private Image plate, frame, beam, spark, icon, divider;
        private Image outerGlow, innerGlow, innerEdge, digitGlow, flashEdge, flashCore;
        private TextMeshProUGUI nameText;
        private string timeString = string.Empty;
        private bool particlesLive;
        private bool outerParticlesLive;
        private bool flashLive;

        // 상태
        private float progress;
        private bool targetOn;
        private string instanceId = string.Empty;
        private string buildingId = string.Empty;
        private Func<string, double> remainingProvider;
        private double lastRemaining;
        private bool hasObserved;
        private int displaySeconds = -1;
        private float gather;
        private float gatherTarget;
        private float flowLevel;
        private float outerLevel;
        private float innerLevel;
        private float edgeLevel;
        private float edgeSize;
        private float innerClock;
        private float outerClock;
        private float frameAlpha;
        private float completeT = -1f;
        private bool flashHeld;
        private float elapsed;

        public bool IsOpen => targetOn;
        public bool IsCompleting => completeT >= 0f;
        public string InstanceId => instanceId;
        public string BuildingId => buildingId;
        public float Progress => progress;
        public float GatherValue => gather;
        public int DisplaySeconds => displaySeconds;
        public string TimeText => timeString;
        public string NameText => nameText != null ? nameText.text : string.Empty;
        public CanvasGroup StageGroup => stageGroup;

        /// <summary>플레이 중 최상위 Canvas 아래에 만든다. 프리팹·씬을 바꾸지 않는다.</summary>
        public static FacilityCooldownPopupView Create(Transform canvasRoot, TMP_FontAsset fallbackFont)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var go = new GameObject(
                "FacilityCooldownPopup_Runtime",
                typeof(RectTransform),
                typeof(Canvas));
            go.layer = canvasRoot.gameObject.layer;
            go.transform.SetParent(canvasRoot, false);
            var popupCanvas = go.GetComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = SortingOrder;
            ResourceSellUi.Stretch((RectTransform)go.transform);

            var view = go.AddComponent<FacilityCooldownPopupView>();
            view.font = fallbackFont;
            view.Build();
            go.SetActive(false);
            return view;
        }

        public void SetIconResolver(Func<string, Sprite> resolver)
        {
            iconResolver = resolver;
            if (targetOn && icon != null)
            {
                ApplyIcon();
            }
        }

        public bool ShowFacilityCooldown(string targetBuildingId, string targetInstanceId, Func<string, double> provider)
        {
            if (!built || provider == null || string.IsNullOrEmpty(targetInstanceId))
            {
                return false;
            }

            var sameTarget = instanceId.Length > 0 && instanceId == targetInstanceId;
            remainingProvider = provider;
            if (sameTarget && targetOn && completeT < 0f)
            {
                // 같은 대상이 이미 떠 있으면 연출을 다시 시작하지 않는다.
                return true;
            }

            var remaining = provider(targetInstanceId);
            if (remaining <= 0d)
            {
                HideFacilityCooldown();
                return false;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (!sameTarget)
            {
                instanceId = targetInstanceId;
                buildingId = targetBuildingId ?? string.Empty;
                ApplyIcon();
                if (nameText != null)
                {
                    nameText.text = ItemDisplayNames.Building(buildingId);
                }

                displaySeconds = -1;
            }

            completeT = -1f;
            flashHeld = false;
            targetOn = true;
            lastRemaining = remaining;
            hasObserved = true;
            // 처음 열 때 현재 남은 시간에 맞는 상태로 바로 시작한다(30초 연출을 처음부터 재생하지 않는다).
            gatherTarget = FacilityCooldownPopupTimeline.Gather01(remaining);
            gather = gatherTarget;
            outerLevel = FacilityCooldownPopupTimeline.OuterGlow01(remaining);
            innerLevel = FacilityCooldownPopupTimeline.InnerGlow01(remaining);
            edgeLevel = FacilityCooldownPopupTimeline.InnerEdgeGlow01(remaining);
            edgeSize = FacilityCooldownPopupTimeline.InnerEdgeSize01(remaining);
            SetCountdown(remaining);
            ApplyPose();
            return true;
        }

        public void HideFacilityCooldown()
        {
            if (!built || !targetOn)
            {
                return;
            }

            targetOn = false;
            completeT = -1f;
            flashHeld = false;
            hasObserved = false;
            gatherTarget = 0f;
        }

        /// <summary>테스트와 Update가 같이 쓰는 진행 경로. dt는 실제 시간(unscaled) 기준.</summary>
        public void Tick(float dt)
        {
            if (!built || !gameObject.activeSelf)
            {
                return;
            }

            dt = Mathf.Max(0f, dt);
            elapsed += dt;
            if (targetOn && completeT < 0f)
            {
                Observe();
            }

            if (completeT >= 0f)
            {
                completeT += dt;
                if (completeT >= FacilityCooldownPopupTimeline.CompletionFlashSeconds)
                {
                    // 중앙까지 환해진 직후 TV 꺼짐을 시작한다. 섬광은 꺼지는 동안 그대로 유지된다.
                    completeT = -1f;
                    flashHeld = true;
                    targetOn = false;
                    gatherTarget = 0f;
                }
            }

            progress = FacilityCooldownPopupTimeline.Advance(progress, targetOn, dt);
            gather = Mathf.MoveTowards(gather, gatherTarget, FacilityCooldownPopupTimeline.GatherRatePerSecond * dt);
            var finalTarget = targetOn && FacilityCooldownPopupTimeline.IsFinalWindow(lastRemaining) ? 1f : 0f;
            flowLevel = Mathf.MoveTowards(flowLevel, finalTarget, 2f * dt);
            UpdateGlowLevels(dt);

            ApplyPose();
            UpdateParticles(dt);
            UpdateOuterParticles(dt);
            UpdateFlows();
            UpdateFlash();

            if (!targetOn && progress <= 0f)
            {
                TurnOff();
            }
        }

        private void Update()
        {
            if (!built)
            {
                return;
            }

            Tick(Mathf.Min(Time.unscaledDeltaTime, 0.05f));
        }

        private void OnDisable()
        {
            ClearState();
            SilenceEffects();
        }

        private void OnDestroy()
        {
            ClearState();
            built = false;
            remainingProvider = null;
        }

        /// <summary>
        /// 저장된 쿨타임을 읽는다. 값이 있으면 글자와 응집 목표를 갱신하고,
        /// 사라졌으면(만료·초기화) 직전 관측이 짧았을 때만 완료 빛을 한 번 돌린다.
        /// </summary>
        private void Observe()
        {
            var remaining = remainingProvider != null ? remainingProvider(instanceId) : -1d;
            if (remaining > 0d)
            {
                lastRemaining = remaining;
                hasObserved = true;
                gatherTarget = FacilityCooldownPopupTimeline.Gather01(remaining);
                SetCountdown(remaining);
                return;
            }

            if (hasObserved && lastRemaining <= FacilityCooldownPopupTimeline.CompletionObserveSeconds)
            {
                completeT = 0f;
                gatherTarget = 1f;
                SetDisplaySeconds(0);
            }
            else
            {
                targetOn = false;
                gatherTarget = 0f;
            }

            hasObserved = false;
        }

        private void SetCountdown(double remaining)
        {
            SetDisplaySeconds(OutpostService.GetCooldownDisplaySeconds(remaining));
        }

        private void SetDisplaySeconds(int seconds)
        {
            if (!built || seconds == displaySeconds)
            {
                return;
            }

            displaySeconds = seconds;
            timeString = FacilityCooldownFormat.Format(Math.Min(seconds, MaxDisplaySeconds));
            ApplyTimeDigits(timeString);
        }

        /// <summary>
        /// 시간 문자열(MM:SS 또는 H:MM:SS)을 7-segment 칸에 배치한다. 쓰는 칸만 켜고 가운데 정렬하며,
        /// 켜진 획은 불투명, 꺼진 획은 흐리게 남긴다. 글자가 바뀔 때(초당 한 번)만 호출된다.
        /// </summary>
        private void ApplyTimeDigits(string text)
        {
            var width = DigitGap * (text.Length - 1);
            for (var i = 0; i < text.Length; i++)
            {
                width += text[i] == ':' ? ColonWidth : DigitWidth;
            }

            var x = -width * 0.5f;
            var digit = 0;
            var colon = 0;
            var lit = MineResetClockStyle.NormalColor;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == ':')
                {
                    if (colon < MaxColons && colonRoots[colon] != null)
                    {
                        PlaceSlot(colonRoots[colon], x + ColonWidth * 0.5f);
                        for (var k = 0; k < 2; k++)
                        {
                            colonDots[colon * 2 + k].color = lit;
                        }

                        colon++;
                    }

                    x += ColonWidth + DigitGap;
                    continue;
                }

                if (digit < MaxDigits && digitRoots[digit] != null && c >= '0' && c <= '9')
                {
                    PlaceSlot(digitRoots[digit], x + DigitWidth * 0.5f);
                    var mask = SevenSegmentGlyph.GetMask(c - '0');
                    for (var j = 0; j < 7; j++)
                    {
                        lit.a = (mask & (1 << j)) != 0 ? 1f : SegmentOffAlpha;
                        digitSegments[digit * 7 + j].color = lit;
                    }

                    lit.a = 1f;
                    digit++;
                }

                x += DigitWidth + DigitGap;
            }

            for (var d = digit; d < MaxDigits; d++)
            {
                HideSlot(digitRoots[d]);
            }

            for (var k = colon; k < MaxColons; k++)
            {
                HideSlot(colonRoots[k]);
            }

            if (digitGlow != null)
            {
                lit.a = 0.12f;
                digitGlow.color = lit;
            }
        }

        private static void PlaceSlot(RectTransform slot, float x)
        {
            slot.anchoredPosition = new Vector2(x, 0f);
            if (!slot.gameObject.activeSelf)
            {
                slot.gameObject.SetActive(true);
            }
        }

        private static void HideSlot(RectTransform slot)
        {
            if (slot != null && slot.gameObject.activeSelf)
            {
                slot.gameObject.SetActive(false);
            }
        }

        private void ApplyIcon()
        {
            if (icon == null)
            {
                return;
            }

            var sprite = iconResolver != null && !string.IsNullOrEmpty(buildingId) ? iconResolver(buildingId) : null;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        private void ApplyPose()
        {
            if (!built || window == null || windowMask == null)
            {
                return;
            }

            var pose = FacilityCooldownPopupTimeline.Evaluate(progress);
            window.sizeDelta = new Vector2(WindowWidth * pose.WindowWidth01, WindowHeight * pose.WindowHeight01);
            var padding = -6f * Mathf.Clamp01((progress - 0.4f) / 0.45f);
            windowMask.padding = new Vector4(padding, padding, padding, padding);
            frameGroup.alpha = pose.FrameAlpha;
            readoutGroup.alpha = pose.ReadoutAlpha;
            effectsGroup.alpha = pose.FrameAlpha;
            frameAlpha = pose.FrameAlpha;
            ApplyGlows();

            var color = Color.Lerp(ResourceSellUi.Teal, BeamTint, 0.6f);
            beam.rectTransform.sizeDelta = new Vector2(WindowWidth * pose.BeamWidth01, pose.BeamHeightPx);
            color.a = pose.BeamAlpha;
            beam.color = color;
            color.a = pose.SparkAlpha;
            spark.color = color;

            // 응집 강도는 프레임 자체의 색으로만 표현한다(흐린 번짐 배경 없음). 청록에서 푸른빛 최대 35%.
            var tint = Mathf.Clamp01(gather) * 0.35f;
            if (frame != null)
            {
                var tinted = Color.Lerp(Color.white, FrameBlueWhite, tint);
                frame.color = Color.Lerp(FrameRestColor, tinted, Mathf.Clamp01(gather));
            }
        }

        /// <summary>
        /// 완료·남은 시간에 따라 바깥/안쪽 빛 세기를 목표값으로 옮긴다.
        /// 완료 섬광과 TV 꺼짐 동안에는 둘 다 최대로 유지해 꺼지는 순간까지 빛이 이어지게 한다.
        /// </summary>
        private void UpdateGlowLevels(float dt)
        {
            var outerTarget = 0f;
            var innerTarget = 0f;
            var edgeTarget = 0f;
            var sizeTarget = 0f;
            if (targetOn && completeT < 0f)
            {
                if (hasObserved)
                {
                    outerTarget = FacilityCooldownPopupTimeline.OuterGlow01(lastRemaining);
                    innerTarget = FacilityCooldownPopupTimeline.InnerGlow01(lastRemaining);
                    edgeTarget = FacilityCooldownPopupTimeline.InnerEdgeGlow01(lastRemaining);
                    sizeTarget = FacilityCooldownPopupTimeline.InnerEdgeSize01(lastRemaining);
                }
            }
            else if (completeT >= 0f || flashHeld)
            {
                outerTarget = 1f;
                innerTarget = FacilityCooldownPopupTimeline.InnerGlowMax;
                edgeTarget = FacilityCooldownPopupTimeline.InnerEdgeMax;
                sizeTarget = 1f;
            }

            var step = GlowRatePerSecond * dt;
            outerLevel = Mathf.MoveTowards(outerLevel, outerTarget, step);
            innerLevel = Mathf.MoveTowards(innerLevel, innerTarget, step);
            edgeLevel = Mathf.MoveTowards(edgeLevel, edgeTarget, step);
            edgeSize = Mathf.MoveTowards(edgeSize, sizeTarget, step);
        }

        /// <summary>
        /// 프레임 바깥 후광과 안쪽 가장자리 빛. 바깥 후광은 마스크 밖(Stage)에 있어 창 크기를 따라가고,
        /// 안쪽 빛은 Effects 아래층이라 글자와 프레임 뒤에 깔린다.
        /// </summary>
        private void ApplyGlows()
        {
            if (outerGlow != null)
            {
                outerGlow.rectTransform.sizeDelta = window.sizeDelta + new Vector2(HaloReach * 2f, HaloReach * 2f);
                var color = Color.Lerp(ResourceSellUi.Teal, BeamTint, 0.5f * outerLevel);
                color.a = outerLevel * HaloMaxAlpha * frameAlpha;
                outerGlow.color = color;
            }

            if (innerGlow != null)
            {
                var color = Color.Lerp(ResourceSellUi.Teal, BeamTint, 0.4f);
                color.a = innerLevel * InnerGlowAlphaScale;
                innerGlow.color = color;
            }

            if (innerEdge != null)
            {
                var color = Color.Lerp(ResourceSellUi.Teal, BeamTint, 0.5f);
                color.a = edgeLevel;
                innerEdge.color = color;
                var thickness = Mathf.Lerp(InnerEdgeMinPx, InnerEdgeMaxPx, Mathf.Clamp01(edgeSize));
                innerEdge.pixelsPerUnitMultiplier = FacilityCooldownGlowArt.EdgeBandBorder / thickness;
            }
        }

        private void UpdateParticles(float dt)
        {
            // 안쪽 입자는 마지막 5초에만 켜진다. 30초 구간의 빛은 프레임 바깥에서만 보인다.
            var strength = Mathf.Clamp01(innerLevel / FacilityCooldownPopupTimeline.InnerGlowMax);
            if (strength <= 0.001f)
            {
                if (particlesLive)
                {
                    SetParticlesAlpha(0f);
                    particlesLive = false;
                }

                return;
            }

            particlesLive = true;
            var speed = FacilityCooldownPopupTimeline.ParticleSpeed(lastRemaining);
            // 속도가 바뀌어도 위치가 튀지 않도록 속도를 시간에 따라 누적한다.
            innerClock += dt * speed;
            var tint = Mathf.Clamp01(gather) * 0.35f;
            var baseColor = Color.Lerp(ResourceSellUi.Teal, DeepBlue, tint);
            var inset = new Vector2(HalfWidth - 10f, HalfHeight - 10f);
            for (var i = 0; i < ParticleCount; i++)
            {
                var image = particles[i];
                if (image == null)
                {
                    continue;
                }

                var s = Frac(particlePhase[i] + innerClock * 0.10f);
                var u = Frac(particleLane[i] + innerClock * 0.012f);
                var point = PerimeterPoint(u, inset.x, inset.y) * (1f - 0.24f * s);
                var visible = Mathf.Clamp01(strength * ParticleCount * 0.9f - i);
                var alpha = visible * 0.85f * Smooth(0f, 0.2f, s) * (1f - Smooth(0.7f, 1f, s));
                image.rectTransform.anchoredPosition = point;
                baseColor.a = alpha;
                image.color = baseColor;
            }
        }

        /// <summary>
        /// 프레임 바깥에서 생겨나 바깥쪽으로 번지며 사라지는 입자. 빛 세기가 오를수록 개수·크기·밝기·속도가 커진다.
        /// </summary>
        private void UpdateOuterParticles(float dt)
        {
            if (outerLevel <= 0.001f)
            {
                if (outerParticlesLive)
                {
                    SetAlpha(outerParticles, 0f);
                    outerParticlesLive = false;
                }

                return;
            }

            outerParticlesLive = true;
            var speed = FacilityCooldownPopupTimeline.ParticleSpeed(lastRemaining) * (0.8f + 0.7f * outerLevel);
            outerClock += dt * speed;
            var size = Mathf.Lerp(6f, 12f, outerLevel);
            var baseColor = Color.Lerp(ResourceSellUi.Teal, BeamTint, 0.6f * outerLevel);
            var peak = Mathf.Lerp(0.55f, 0.95f, outerLevel);
            for (var i = 0; i < OuterParticleCount; i++)
            {
                var image = outerParticles[i];
                if (image == null)
                {
                    continue;
                }

                var s = Frac(outerPhase[i] + outerClock * 0.11f);
                var u = Frac(outerLane[i] + outerClock * 0.006f);
                var edge = PerimeterPoint(u, HalfWidth, HalfHeight);
                var offset = OuterMoteMinOffset + OuterMoteTravel * s;
                var visible = Mathf.Clamp01(outerLevel * OuterParticleCount * 1.15f - i);
                var alpha = visible * peak * frameAlpha * Smooth(0f, 0.18f, s) * (1f - Smooth(0.6f, 1f, s));
                image.rectTransform.anchoredPosition = edge + OutwardNormal(edge) * offset;
                image.rectTransform.sizeDelta = new Vector2(size, size);
                baseColor.a = alpha;
                image.color = baseColor;
            }
        }

        private void UpdateFlows()
        {
            var active = flowLevel > 0.001f;
            if (!active)
            {
                for (var k = 0; k < FlowCount; k++)
                {
                    if (flows[k] != null && flows[k].color.a > 0f)
                    {
                        var c = flows[k].color;
                        c.a = 0f;
                        flows[k].color = c;
                    }
                }

                return;
            }

            var color = ResourceSellUi.Teal;
            for (var k = 0; k < FlowCount; k++)
            {
                var image = flows[k];
                if (image == null)
                {
                    continue;
                }

                var f = Frac(elapsed * 0.9f + k * 0.25f);
                var corner = new Vector2((k % 2 == 0 ? -1f : 1f) * HalfWidth, (k < 2 ? 1f : -1f) * HalfHeight);
                var point = corner * (1f - 0.6f * f);
                image.rectTransform.anchoredPosition = point;
                image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-point.y, -point.x) * Mathf.Rad2Deg);
                color.a = flowLevel * 0.7f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(f / 0.7f));
                image.color = color;
            }
        }

        /// <summary>
        /// 0초 섬광. 가장자리부터 차올라 중앙까지 빠르게 환해지고, 이어지는 TV 꺼짐 동안 그대로 유지된다.
        /// 창 마스크 안쪽 레이어라 꺼지며 접히는 창과 함께 사라진다.
        /// </summary>
        private void UpdateFlash()
        {
            var u = 0f;
            if (completeT >= 0f)
            {
                u = Mathf.Clamp01(completeT / FacilityCooldownPopupTimeline.CompletionFlashSeconds);
            }
            else if (flashHeld)
            {
                u = 1f;
            }

            if (u <= 0f)
            {
                if (flashLive)
                {
                    SetAlpha(flashEdge, 0f);
                    SetAlpha(flashCore, 0f);
                    flashLive = false;
                }

                return;
            }

            flashLive = true;
            if (flashEdge != null)
            {
                var color = Color.Lerp(ResourceSellUi.Teal, BeamTint, 0.7f);
                color.a = FacilityCooldownPopupTimeline.FlashEdge01(u) * 0.95f;
                flashEdge.color = color;
            }

            if (flashCore != null)
            {
                var color = Color.Lerp(BeamTint, Color.white, 0.6f);
                color.a = FacilityCooldownPopupTimeline.FlashCore01(u) * 0.9f;
                flashCore.color = color;
            }
        }

        private void SetParticlesAlpha(float alpha)
        {
            SetAlpha(particles, alpha);
        }

        private static void SetAlpha(Image[] images, float alpha)
        {
            for (var i = 0; i < images.Length; i++)
            {
                SetAlpha(images[i], alpha);
            }
        }

        private static void SetAlpha(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            var c = image.color;
            c.a = alpha;
            image.color = c;
        }

        /// <summary>입자·흐름·후광·섬광을 모두 투명하게 만든다. 비활성화·종료 때 잔상이 남지 않게 한다.</summary>
        private void SilenceEffects()
        {
            SetAlpha(particles, 0f);
            SetAlpha(outerParticles, 0f);
            SetAlpha(flows, 0f);
            SetAlpha(outerGlow, 0f);
            SetAlpha(innerGlow, 0f);
            SetAlpha(innerEdge, 0f);
            SetAlpha(flashEdge, 0f);
            SetAlpha(flashCore, 0f);
            particlesLive = false;
            outerParticlesLive = false;
            flashLive = false;
        }

        private void TurnOff()
        {
            ClearState();
            ApplyPose();
            SilenceEffects();
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void ClearState()
        {
            progress = 0f;
            targetOn = false;
            completeT = -1f;
            flashHeld = false;
            gather = 0f;
            gatherTarget = 0f;
            flowLevel = 0f;
            outerLevel = 0f;
            innerLevel = 0f;
            edgeLevel = 0f;
            edgeSize = 0f;
            innerClock = 0f;
            outerClock = 0f;
            frameAlpha = 0f;
            hasObserved = false;
            lastRemaining = 0d;
            displaySeconds = -1;
            timeString = string.Empty;
            instanceId = string.Empty;
            buildingId = string.Empty;
            remainingProvider = null;
            elapsed = 0f;
        }

        private void Build()
        {
            if (built)
            {
                return;
            }

            try
            {
                var skin = Resources.Load<MineResetTimedPopupSkin>(MineResetTimedPopupSkin.ResourcePath);
                if (skin != null && skin.font != null)
                {
                    font = skin.font;
                }

                if (font == null)
                {
                    font = TMP_Settings.defaultFontAsset;
                }

                var root = (RectTransform)transform;
                stage = CreateRect(root, "Stage", Vector2.zero, new Vector2(WindowWidth, WindowHeight));
                stageGroup = stage.gameObject.AddComponent<CanvasGroup>();
                stageGroup.interactable = false;
                stageGroup.blocksRaycasts = false;

                // 창 마스크 바깥 레이어: 후광 → 바깥 입자 → 빔/점. 창(Window)보다 먼저 만들어 창 뒤에 깔린다.
                outerGlow = CreateImage(stage, "OuterGlow", Vector2.zero,
                    new Vector2(WindowWidth + HaloReach * 2f, WindowHeight + HaloReach * 2f),
                    Color.clear, FacilityCooldownGlowArt.Halo());
                outerGlow.fillCenter = false;
                outerGlow.pixelsPerUnitMultiplier = 24f / HaloReach;
                for (var i = 0; i < OuterParticleCount; i++)
                {
                    outerPhase[i] = Hash01(i * 11 + 1);
                    outerLane[i] = Hash01(i * 17 + 9);
                    outerParticles[i] = CreateImage(stage, "MoteOut" + i, Vector2.zero, new Vector2(8f, 8f),
                        Color.clear, MineResetPopupArt.Soft("cooldown-mote", 0.2f));
                }

                beam = CreateImage(stage, "Beam", Vector2.zero, Vector2.zero, Color.clear, null);
                spark = CreateImage(stage, "Spark", Vector2.zero, new Vector2(16f, 16f),
                    Color.clear, MineResetPopupArt.Soft("cooldown-spark", 0.1f));

                window = CreateRect(stage, "Window", Vector2.zero, new Vector2(WindowWidth, WindowHeight));
                windowMask = window.gameObject.AddComponent<RectMask2D>();
                content = CreateRect(window, "Content", Vector2.zero, new Vector2(WindowWidth, WindowHeight));

                plate = CreateImage(content, "Plate", Vector2.zero, new Vector2(WindowWidth, WindowHeight),
                    PlateColor, null);

                effects = CreateRect(content, "Effects", Vector2.zero, new Vector2(WindowWidth, WindowHeight));
                effectsGroup = effects.gameObject.AddComponent<CanvasGroup>();
                effectsGroup.interactable = false;
                effectsGroup.blocksRaycasts = false;
                innerGlow = CreateImage(effects, "InnerGlow", Vector2.zero, new Vector2(WindowWidth, WindowHeight),
                    Color.clear, FacilityCooldownGlowArt.Vignette());
                innerEdge = CreateImage(effects, "InnerEdgeGlow", Vector2.zero, new Vector2(WindowWidth, WindowHeight),
                    Color.clear, FacilityCooldownGlowArt.EdgeBand());
                innerEdge.fillCenter = false;
                for (var i = 0; i < ParticleCount; i++)
                {
                    particlePhase[i] = Hash01(i * 7 + 3);
                    particleLane[i] = Hash01(i * 13 + 5);
                    particles[i] = CreateImage(effects, "MoteIn" + i, Vector2.zero, new Vector2(10f, 10f),
                        Color.clear, MineResetPopupArt.Soft("cooldown-mote", 0.2f));
                }

                for (var k = 0; k < FlowCount; k++)
                {
                    flows[k] = CreateImage(effects, "Flow" + k, Vector2.zero, new Vector2(70f, 8f),
                        Color.clear, MineResetPopupArt.Soft("cooldown-flow", 0.15f));
                }

                var frameRect = CreateRect(content, "Frame", Vector2.zero, new Vector2(WindowWidth, WindowHeight));
                frameGroup = frameRect.gameObject.AddComponent<CanvasGroup>();
                frameGroup.interactable = false;
                frameGroup.blocksRaycasts = false;
                frame = BuildFrame(frameRect, skin);

                var readout = CreateRect(content, "Readout", Vector2.zero, new Vector2(WindowWidth, WindowHeight));
                readoutGroup = readout.gameObject.AddComponent<CanvasGroup>();
                readoutGroup.interactable = false;
                readoutGroup.blocksRaycasts = false;

                icon = CreateImage(readout, "Icon", new Vector2(-52f, 64f), new Vector2(36f, 36f), Color.white, null);
                icon.preserveAspect = true;
                icon.enabled = false;
                nameText = CreateText(readout, "Name", new Vector2(53f, 64f), new Vector2(150f, 32f), 22f,
                    ResourceSellUi.TextMain, TextAlignmentOptions.Left);
                divider = CreateImage(readout, "Divider", new Vector2(0f, 42f), new Vector2(240f, 1.5f),
                    ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.3f), null);
                BuildDigits(readout);
                CreateText(readout, "Caption", new Vector2(0f, -66f), new Vector2(300f, 26f), 18f,
                    ResourceSellUi.TextMuted, TextAlignmentOptions.Center).text = Caption;

                // 0초 섬광: 가장자리 빛 위에 중앙 빛을 겹친다. 글자 위까지 덮는 것은 TV가 꺼지는 순간뿐이다.
                flashEdge = CreateImage(content, "FlashEdge", Vector2.zero, new Vector2(WindowWidth, WindowHeight),
                    Color.clear, FacilityCooldownGlowArt.Vignette());
                flashCore = CreateImage(content, "FlashCore", Vector2.zero,
                    new Vector2(WindowWidth * 1.6f, WindowHeight * 1.6f),
                    Color.clear, MineResetPopupArt.Soft("cooldown-flash", 0.25f));

                built = true;
                ClearState();
                ApplyPose();
                SilenceEffects();
            }
            catch (Exception exception)
            {
                built = false;
                Debug.LogError("[SubTerra] Facility cooldown popup initialization failed: " + exception);
            }
        }

        /// <summary>
        /// 7-segment 숫자 칸(최대 6자리 + 콜론 2개)을 미리 만들어 둔다. 획 배치는 광산 초기화 시계와 같은 순서(위, 오른쪽 위,
        /// 오른쪽 아래, 아래, 왼쪽 아래, 왼쪽 위, 가운데)이며 크기만 1.5배다.
        /// </summary>
        private void BuildDigits(RectTransform readout)
        {
            digitGlow = CreateImage(readout, "DigitGlow", new Vector2(0f, DigitCenterY), new Vector2(310f, 84f),
                Color.clear, MineResetPopupArt.Soft("clock-digits", 0.1f));
            var digits = CreateRect(readout, "Digits", new Vector2(0f, DigitCenterY), new Vector2(DigitWidth * MaxDigits, 54f));
            for (var i = 0; i < MaxDigits; i++)
            {
                var digit = CreateRect(digits, "Digit" + i, Vector2.zero, new Vector2(DigitWidth, 54f));
                digitRoots[i] = digit;
                for (var j = 0; j < 7; j++)
                {
                    var horizontal = j == 0 || j == 3 || j == 6;
                    var position = horizontal
                        ? new Vector2(0f, j == 0 ? 24f : j == 3 ? -24f : 0f)
                        : new Vector2(j == 1 || j == 2 ? 15f : -15f, j == 1 || j == 5 ? 12f : -12f);
                    digitSegments[i * 7 + j] = CreateImage(digit, "Segment" + j, position,
                        horizontal ? new Vector2(30f, 6f) : new Vector2(6f, 18f),
                        Color.clear, MineResetPopupArt.Segment(!horizontal));
                }

                digit.gameObject.SetActive(false);
            }

            for (var c = 0; c < MaxColons; c++)
            {
                var colon = CreateRect(digits, "Colon" + c, Vector2.zero, new Vector2(ColonWidth, 54f));
                colonRoots[c] = colon;
                for (var k = 0; k < 2; k++)
                {
                    colonDots[c * 2 + k] = CreateImage(colon, "Dot" + k, new Vector2(0f, k == 0 ? 11f : -11f),
                        new Vector2(6f, 6f), Color.clear, null);
                }

                colon.gameObject.SetActive(false);
            }
        }

        private Image BuildFrame(RectTransform parent, MineResetTimedPopupSkin skin)
        {
            var sourceSprite = skin != null ? skin.frame : null;
            var frameSprite = sourceSprite != null
                ? ResourceSellArt.Sliced(sourceSprite, 0.135f, 0.19f)
                : null;
            if (frameSprite != null)
            {
                var image = CreateImage(parent, "FrameImage", Vector2.zero, new Vector2(WindowWidth, WindowHeight),
                    Color.white, frameSprite);
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = frameSprite.border.x * 100f / (frameSprite.pixelsPerUnit * FrameCornerPx);
                return image;
            }

            var outline = CreateImage(parent, "FrameFallback", Vector2.zero, new Vector2(WindowWidth, WindowHeight),
                ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.85f),
                ResourceSellArt.ChamferOutline());
            outline.type = outline.sprite != null && outline.sprite.border != Vector4.zero
                ? Image.Type.Sliced
                : Image.Type.Simple;
            return outline;
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, Vector2 position, Vector2 size,
            float fontSize, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.text = string.Empty;
            return text;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size,
            Color color, Sprite sprite)
        {
            var image = CreateRect(parent, name, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                image.type = Image.Type.Sliced;
            }

            image.raycastTarget = false;
            return image;
        }

        /// <summary>사각형 둘레의 u(0~1) 위치. 위 → 오른쪽 → 아래 → 왼쪽 순서.</summary>
        private static Vector2 PerimeterPoint(float u, float hw, float hh)
        {
            var d = Frac(u) * (4f * hw + 4f * hh);
            if (d < 2f * hw)
            {
                return new Vector2(-hw + d, hh);
            }

            d -= 2f * hw;
            if (d < 2f * hh)
            {
                return new Vector2(hw, hh - d);
            }

            d -= 2f * hh;
            if (d < 2f * hw)
            {
                return new Vector2(hw - d, -hh);
            }

            d -= 2f * hw;
            return new Vector2(-hw, -hh + d);
        }

        /// <summary>둘레 위 점에서 바깥을 향하는 축 방향 단위 벡터.</summary>
        private static Vector2 OutwardNormal(Vector2 point)
        {
            if (Mathf.Abs(point.x) * HalfHeight > Mathf.Abs(point.y) * HalfWidth)
            {
                return new Vector2(point.x < 0f ? -1f : 1f, 0f);
            }

            return new Vector2(0f, point.y < 0f ? -1f : 1f);
        }

        private static float Hash01(int n)
        {
            var s = Mathf.Sin(n * 12.9898f) * 43758.5453f;
            return s - Mathf.Floor(s);
        }

        private static float Frac(float value) => value - Mathf.Floor(value);

        private static float Smooth(float edge0, float edge1, float x)
        {
            var t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
    }
}
