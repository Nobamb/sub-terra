using System.Collections.Generic;
using SubTerra.App.UI.FacilityNameTag;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 골드 획득 홀로그램 팝업의 얇은 View. 시설 이름표와 같은 월드 캔버스 비율·레이어를 쓰고,
    /// 값은 GoldPickupPopupTimeline / GoldPickupPopupState 에서 받아 그리기만 한다.
    /// 프레임·바깥 오른쪽 입체 레버·슬롯 창 안의 낙하 숫자(잔상 포함)·팝업 금화를 직접 소유한다.
    /// </summary>
    public sealed class GoldPickupPopupVisual
    {
        private const float SlotWidth = 188f;
        private const float MainSlotHeight = 36f;
        private const float BonusSlotHeight = 22f;
        private const float SlotTopMargin = 4f;
        private const float SlotBottomMargin = 3f;
        private const float MainStartOffset = 40f;
        private const float BonusStartOffset = 24f;
        private const float MainFontMax = 28f;
        private const float MainFontMin = 14f;
        private const float BonusFontMax = 17f;
        private const float BonusFontMin = 10f;
        private const float CoinSize = 22f;
        private const float BracketLength = 8f;
        private const float BracketThickness = 2f;
        private const float SweepBarWidth = 26f;
        private const float ScanlineAlpha = 0.07f;

        // 레버: 프레임 오른쪽 가장자리 바깥에 매단다. 값은 모두 디자인 px.
        private const float LeverOffsetX = 18f;
        private const float LeverPlateWidth = 22f;
        private const float LeverPlateHeight = 52f;
        private const float LeverPivotY = -8f;
        private const float LeverRodLength = 20f;
        private const float LeverRodWidth = 5f;
        private const float LeverBallSize = 15f;

        private static readonly Color Fill = new Color(0.12f, 0.07f, 0.02f, 0.82f);
        private static readonly Color Glow = new Color(1f, 0.74f, 0.24f, 0.22f);
        private static readonly Color Edge = new Color(1f, 0.78f, 0.3f, 0.85f);
        private static readonly Color EdgeBright = new Color(1f, 0.95f, 0.7f, 1f);
        private static readonly Color Accent = new Color(1f, 0.9f, 0.55f, 1f);
        private static readonly Color Sweep = new Color(1f, 0.9f, 0.55f, 0.55f);
        private static readonly Color PlateFill = new Color(0.17f, 0.1f, 0.03f, 0.96f);
        private static readonly Color PlateEdge = new Color(1f, 0.78f, 0.3f, 0.95f);
        private static readonly Color Groove = new Color(0.03f, 0.018f, 0.005f, 0.98f);
        private static readonly Color BracketBrass = new Color(0.62f, 0.4f, 0.1f, 1f);
        private static readonly Color WindowFill = new Color(0.04f, 0.024f, 0.006f, 0.55f);
        private static readonly Color WindowEdge = new Color(1f, 0.78f, 0.3f, 0.28f);
        private static readonly Color WindowShade = new Color(0.07f, 0.04f, 0.01f, 0.9f);
        private static readonly Color ShadowTint = GoldPickupPresentation.ShadowColor;

        private readonly GameObject root;
        private readonly CanvasGroup rootGroup;
        private readonly RectTransform body;
        private readonly RectTransform frame;
        private readonly Image glowImage;
        private readonly Image fillImage;
        private readonly Image scanlineImage;
        private readonly Image[] edges = new Image[4];
        private readonly Image[] brackets = new Image[8];
        private readonly RectTransform bootRect;
        private readonly Image bootImage;
        private readonly RectTransform sweepRect;
        private readonly Image sweepImage;
        private readonly Line mainLine;
        private readonly Line bonusLine;
        private readonly RectTransform leverMount;
        private readonly CanvasGroup leverGroup;
        private readonly Image leverGlow;
        private readonly Image plateShadow;
        private readonly RectTransform leverRod;
        private readonly RectTransform leverBall;
        private readonly Image leverBallImage;
        private readonly RectTransform leverBallShadow;
        private readonly Image leverBallShadowImage;
        private readonly RectTransform coinLayer;
        private readonly Sprite coinSprite;
        private readonly float coinSize;
        private readonly List<CoinSlot> coins = new List<CoinSlot>(GoldPickupPopupTimeline.MaxLiveCoins);

        private float height = GoldPickupPopupTimeline.BaseHeight;
        private float frameWidth;
        private float baseLandedAt = -1f;
        private float bonusLandedAt = -1f;
        private float sweepStartedAt = -1f;
        private bool bonusRowShown;

        public GameObject Root => root;
        public bool IsAlive => root != null;
        public int ActiveCoinCount { get; private set; }
        public float CurrentHeight => height;
        public float LeverAngleDegrees { get; private set; }
        public float CurrentSlide { get; private set; }
        public string MainLabel => mainLine.Label;
        public string BonusLabel => bonusLine.Label;
        public RectTransform BodyRect => body;
        public RectTransform FrameRect => frame;
        public RectTransform LeverMount => leverMount;
        public RectTransform LeverRod => leverRod;
        public RectTransform LeverBall => leverBall;
        public RectTransform MainLineRect => mainLine.Container;
        public RectTransform BonusLineRect => bonusLine.Container;
        public int ActiveMainGhostCount => mainLine.ActiveGhosts;
        public int ActiveBonusGhostCount => bonusLine.ActiveGhosts;
        public bool BonusRowActive => bonusRowShown;
        public Vector3 MainLineWorldPosition => mainLine.Container != null
            ? mainLine.Container.position
            : root.transform.position;

        private GoldPickupPopupVisual(GameObject root, TMP_FontAsset font, Sprite coinSprite, float coinScale)
        {
            this.root = root;
            this.coinSprite = coinSprite;
            coinSize = CoinSize * (coinScale > 0f ? coinScale : 1f);
            var rect = (RectTransform)root.transform;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(GoldPickupPopupTimeline.FrameWidth, GoldPickupPopupTimeline.FullHeight);
            rootGroup = root.AddComponent<CanvasGroup>();
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;

            // 프레임·슬롯·레버는 Body 아래에 모아 등장 상승과 착지 충격을 한 번에 적용한다. 금화는 그 위 별도 층이다.
            body = NewRect("Body", rect);
            body.anchorMin = body.anchorMax = new Vector2(0.5f, 0f);
            body.pivot = new Vector2(0.5f, 0f);
            body.sizeDelta = Vector2.zero;

            frame = NewRect("Frame", body);
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0f);
            frame.pivot = new Vector2(0.5f, 0f);
            var glow = NewRect("Glow", frame);
            Stretch(glow, 9f);
            glowImage = AddImage(glow, GoldPickupPopupSprites.Glow(), Glow);
            glowImage.type = Image.Type.Sliced;
            var fill = NewRect("Fill", frame);
            Stretch(fill, 0f);
            fillImage = AddImage(fill, null, Fill);

            // 홀로그램 스캔라인과 켜질 때 훑는 밝은 선은 프레임 안쪽에서만 보이도록 마스크 안에 둔다.
            var scanMask = NewRect("ScanMask", frame);
            Stretch(scanMask, 0f);
            scanMask.gameObject.AddComponent<RectMask2D>();
            var scanlines = NewRect("Scanlines", scanMask);
            Stretch(scanlines, 0f);
            scanlineImage = AddImage(scanlines, GoldPickupPopupSprites.Scanline(), new Color(1f, 0.85f, 0.4f, ScanlineAlpha));
            scanlineImage.type = Image.Type.Tiled;
            bootRect = NewRect("BootLine", scanMask);
            bootRect.anchorMin = new Vector2(0f, 0f);
            bootRect.anchorMax = new Vector2(1f, 0f);
            bootRect.pivot = new Vector2(0.5f, 0.5f);
            bootRect.sizeDelta = new Vector2(0f, 3f);
            bootImage = AddImage(bootRect, null, EdgeBright);
            sweepRect = NewRect("Sweep", scanMask);
            sweepRect.anchorMin = new Vector2(0f, 0f);
            sweepRect.anchorMax = new Vector2(0f, 1f);
            sweepRect.pivot = new Vector2(0.5f, 0.5f);
            sweepRect.sizeDelta = new Vector2(SweepBarWidth, 0f);
            sweepImage = AddImage(sweepRect, GoldPickupPopupSprites.Glow(), Sweep);
            sweepImage.type = Image.Type.Sliced;
            sweepRect.gameObject.SetActive(false);

            for (var i = 0; i < edges.Length; i++)
            {
                var edge = NewRect("Edge" + i, frame);
                float t = GoldPickupPopupTimeline.EdgeThickness;
                switch (i)
                {
                    case 0:
                        SetEdge(edge, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, t));
                        break;
                    case 1:
                        SetEdge(edge, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, t));
                        break;
                    case 2:
                        SetEdge(edge, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(t, 0f));
                        break;
                    default:
                        SetEdge(edge, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(t, 0f));
                        break;
                }

                edges[i] = AddImage(edge, null, Edge);
            }

            // 모서리 브래킷 8개: 모서리마다 가로·세로 한 쌍.
            for (var c = 0; c < 4; c++)
            {
                bool right = (c & 1) == 1;
                bool top = (c & 2) == 2;
                brackets[c * 2] = NewBracket(frame, right, top, true);
                brackets[c * 2 + 1] = NewBracket(frame, right, top, false);
            }

            TMP_FontAsset resolved = font != null ? font : TMP_Settings.defaultFontAsset;
            mainLine = new Line(body, "MainSlot", resolved, new Vector2(SlotWidth, MainSlotHeight), MainFontMax, MainFontMin,
                GoldPickupPopupTimeline.MainGhostCount, true);
            bonusLine = new Line(body, "BonusSlot", resolved, new Vector2(SlotWidth, BonusSlotHeight), BonusFontMax, BonusFontMin,
                GoldPickupPopupTimeline.BonusGhostCount, false);
            bonusLine.Slot.gameObject.SetActive(false);

            // ---------- 프레임 바깥 오른쪽의 입체 레버 ----------
            leverMount = NewRect("LeverMount", body);
            leverMount.anchorMin = leverMount.anchorMax = new Vector2(0.5f, 0f);
            leverMount.sizeDelta = new Vector2(1f, 1f);
            leverGroup = leverMount.gameObject.AddComponent<CanvasGroup>();
            leverGroup.interactable = false;
            leverGroup.blocksRaycasts = false;

            var leverGlowRect = NewRect("LeverGlow", leverMount);
            leverGlowRect.sizeDelta = new Vector2(LeverPlateWidth + 18f, LeverPlateHeight + 18f);
            leverGlow = AddImage(leverGlowRect, GoldPickupPopupSprites.Glow(), new Color(1f, 0.74f, 0.24f, 0.16f));
            leverGlow.type = Image.Type.Sliced;

            // 프레임에서 떨어진 판 아래에 그림자를 깔아 떠 있는 느낌을 낸다.
            var shadowRect = NewRect("PlateShadow", leverMount);
            shadowRect.sizeDelta = new Vector2(LeverPlateWidth + 10f, LeverPlateHeight + 8f);
            shadowRect.anchoredPosition = new Vector2(-1.5f, -4f);
            plateShadow = AddImage(shadowRect, GoldPickupPopupSprites.Glow(), new Color(0f, 0f, 0f, 0.5f));
            plateShadow.type = Image.Type.Sliced;

            var bracket = NewRect("Bracket", leverMount);
            bracket.sizeDelta = new Vector2(14f, 8f);
            bracket.anchoredPosition = new Vector2(-LeverPlateWidth * 0.5f - 3f, 0f);
            AddImage(bracket, null, BracketBrass);
            var bracketHighlight = NewRect("BracketHighlight", bracket);
            bracketHighlight.anchorMin = new Vector2(0f, 1f);
            bracketHighlight.anchorMax = new Vector2(1f, 1f);
            bracketHighlight.pivot = new Vector2(0.5f, 1f);
            bracketHighlight.sizeDelta = new Vector2(0f, 2f);
            AddImage(bracketHighlight, null, new Color(1f, 0.9f, 0.55f, 0.9f));

            var plate = NewRect("Plate", leverMount);
            plate.sizeDelta = new Vector2(LeverPlateWidth, LeverPlateHeight);
            var plateImage = AddImage(plate, GoldPickupPopupSprites.RoundRect(5, 0f), PlateFill);
            plateImage.type = Image.Type.Sliced;
            var plateRim = NewRect("PlateRim", plate);
            Stretch(plateRim, 0f);
            var plateRimImage = AddImage(plateRim, GoldPickupPopupSprites.RoundRect(5, 1.6f), PlateEdge);
            plateRimImage.type = Image.Type.Sliced;

            var groove = NewRect("Groove", leverMount);
            groove.sizeDelta = new Vector2(8f, LeverPlateHeight - 12f);
            var grooveImage = AddImage(groove, GoldPickupPopupSprites.RoundRect(3, 0f), Groove);
            grooveImage.type = Image.Type.Sliced;
            var grooveRim = NewRect("GrooveRim", groove);
            Stretch(grooveRim, 0f);
            var grooveRimImage = AddImage(grooveRim, GoldPickupPopupSprites.RoundRect(3, 1f), new Color(1f, 0.78f, 0.3f, 0.35f));
            grooveRimImage.type = Image.Type.Sliced;

            var pivotCap = NewRect("PivotCap", leverMount);
            pivotCap.sizeDelta = new Vector2(9f, 9f);
            pivotCap.anchoredPosition = new Vector2(0f, LeverPivotY);
            AddImage(pivotCap, GoldPickupPopupSprites.Sphere(), new Color(0.75f, 0.6f, 0.3f, 1f)).preserveAspect = true;

            leverBallShadow = NewRect("BallShadow", leverMount);
            leverBallShadow.sizeDelta = new Vector2(LeverBallSize + 4f, LeverBallSize * 0.5f);
            leverBallShadowImage = AddImage(leverBallShadow, GoldPickupPopupSprites.SoftDisc(), new Color(0f, 0f, 0f, 0.4f));

            leverRod = NewRect("Rod", leverMount);
            leverRod.pivot = new Vector2(0.5f, 0f);
            leverRod.sizeDelta = new Vector2(LeverRodWidth, LeverRodLength);
            leverRod.anchoredPosition = new Vector2(0f, LeverPivotY);
            AddImage(leverRod, GoldPickupPopupSprites.Cylinder(), Color.white);

            leverBall = NewRect("Ball", leverMount);
            leverBall.sizeDelta = new Vector2(LeverBallSize, LeverBallSize);
            leverBallImage = AddImage(leverBall, GoldPickupPopupSprites.Sphere(), Color.white);
            leverBallImage.preserveAspect = true;

            coinLayer = NewRect("CoinLayer", rect);
            coinLayer.anchorMin = coinLayer.anchorMax = new Vector2(0.5f, 0f);
            coinLayer.pivot = new Vector2(0.5f, 0f);
            coinLayer.sizeDelta = Vector2.zero;
        }

        /// <summary>월드 캔버스 팝업을 만든다. 시설 이름표와 같은 1px=0.01 비율, MainScreen 레이어, 이름표보다 위 정렬.</summary>
        public static GoldPickupPopupVisual Create(Transform parent, TMP_FontAsset font, Sprite coinSprite, float coinScale)
        {
            var go = new GameObject("GoldPickupPopup", typeof(RectTransform));
            go.layer = FacilityNameTagLayers.MainScreen;
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = GoldPickupVfx.WorldSortingOrder + 20;
            canvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
            go.transform.localScale = Vector3.one * FacilityNameTagVisual.WorldPerPixel;
            return new GoldPickupPopupVisual(go, font, coinSprite, coinScale);
        }

        /// <summary>플레이어 머리 위 추적 위치. 등장·퇴장의 상승/하강은 Body 안에서 따로 적용한다.</summary>
        public void SetPose(Vector3 trackedWorld)
        {
            if (root != null)
            {
                root.transform.position = trackedWorld;
            }
        }

        public void Destroy()
        {
            if (root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>상태가 알려 준 착지·합치기 사건에 맞춰 금화 분출과 빛 효과를 시작한다.</summary>
        public void Handle(GoldPickupPopupEvents events, GoldPickupPopupState state)
        {
            if ((events & GoldPickupPopupEvents.BaseLanded) != 0)
            {
                baseLandedAt = state.Elapsed;
                EmitCoins(GoldPickupPopupTimeline.BaseLandCoins(state.TotalBase), TopEdge(), 0);
            }

            if ((events & GoldPickupPopupEvents.BonusLanded) != 0)
            {
                bonusLandedAt = state.Elapsed;
                sweepStartedAt = state.Elapsed;
                EmitCoins(GoldPickupPopupTimeline.BonusLandCoins(state.TotalBonus), TopEdge(), 1);
            }
        }

        public void OnMerged(GoldPickupMergeResult result)
        {
            EmitCoins(GoldPickupPopupTimeline.MergeCoins(result.EffectiveTotal), TopEdge(), 2);
        }

        /// <summary>팝업 위쪽 가장자리 중앙에서 부채꼴로 금화를 분출한다. 살아 있는 금화가 상한이면 초과분은 만들지 않는다.</summary>
        public int EmitCoins(int count, Vector2 origin, int salt)
        {
            if (root == null || coinSprite == null || count <= 0)
            {
                return 0;
            }

            int emitted = 0;
            for (var index = 0; index < count; index++)
            {
                CoinSlot slot = AcquireCoin();
                if (slot == null)
                {
                    break;
                }

                GoldPickupPopupTimeline.CoinLaunch(index, count, salt, out float vx, out float vy, out float delay);
                slot.Active = true;
                slot.Age = 0f;
                slot.Delay = delay;
                slot.Origin = origin;
                slot.Vx = vx;
                slot.Vy = vy;
                slot.SpinIndex = index + salt * 3;
                slot.Image.enabled = false;
                slot.Rect.gameObject.SetActive(true);
                slot.Rect.anchoredPosition = origin;
                ActiveCoinCount++;
                emitted++;
            }

            return emitted;
        }

        public void Tick(float deltaSeconds)
        {
            if (root == null || ActiveCoinCount == 0)
            {
                return;
            }

            for (var i = 0; i < coins.Count; i++)
            {
                CoinSlot slot = coins[i];
                if (!slot.Active)
                {
                    continue;
                }

                slot.Age += deltaSeconds;
                float local = slot.Age - slot.Delay;
                if (local < 0f)
                {
                    continue;
                }

                if (local >= GoldPickupPopupTimeline.CoinLifetime)
                {
                    DeactivateCoin(slot);
                    continue;
                }

                slot.Image.enabled = true;
                slot.Rect.anchoredPosition = GoldPickupPopupTimeline.CoinPosition(slot.Origin, slot.Vx, slot.Vy, local);
                float pop = GoldPickupPopupTimeline.CoinPopScale(local);
                slot.Rect.localScale = new Vector3(GoldPickupPresentation.CoinSpinScaleX(slot.SpinIndex, local) * pop, pop, 1f);
                Color color = slot.Image.color;
                color.a = Mathf.Clamp01(local / 0.04f) * GoldPickupPopupTimeline.CoinAlpha(local);
                slot.Image.color = color;
            }
        }

        public void ClearCoins()
        {
            for (var i = 0; i < coins.Count; i++)
            {
                if (coins[i].Active)
                {
                    DeactivateCoin(coins[i]);
                }
            }
        }

        /// <summary>상태 값을 화면에 반영한다. 숫자 문자열은 표시값이 바뀔 때만 다시 만든다.</summary>
        public void Apply(GoldPickupPopupState state)
        {
            if (root == null)
            {
                return;
            }

            float t = state.Elapsed;
            float exit = state.ExitProgress;
            float level = GoldPickupPopupTimeline.FrameLevel(t, exit);
            FacilityNameTagFrame frameState = GoldPickupPopupTimeline.Frame(level);

            height = GoldPickupPopupTimeline.Height(state.ExpandProgress);
            frameWidth = Mathf.Lerp(GoldPickupPopupTimeline.BaseHeight, GoldPickupPopupTimeline.FrameWidth, frameState.Spread);
            frame.sizeDelta = new Vector2(frameWidth, height);
            frame.anchoredPosition = Vector2.zero;

            // 아래에서 떠오르며 등장하고 퇴장 때는 같은 경로로 가라앉는다. 착지마다 살짝 눌린다.
            CurrentSlide = GoldPickupPopupTimeline.Slide(level);
            float kick = (baseLandedAt >= 0f ? GoldPickupPopupTimeline.Kick(t - baseLandedAt, GoldPickupPopupTimeline.MainKickPx) : 0f)
                + (bonusLandedAt >= 0f ? GoldPickupPopupTimeline.Kick(t - bonusLandedAt, GoldPickupPopupTimeline.BonusKickPx) : 0f);
            body.anchoredPosition = new Vector2(0f, -CurrentSlide - kick);

            bool showBonus = state.HasBonusRow;
            if (showBonus != bonusRowShown)
            {
                bonusRowShown = showBonus;
                bonusLine.Slot.gameObject.SetActive(showBonus);
            }

            // 본문 줄은 프레임 위쪽에 붙어 높이가 늘면 함께 올라가고, 추가 줄은 아래쪽 고정이다.
            float mainCenterY = height - SlotTopMargin - MainSlotHeight * 0.5f;
            mainLine.Slot.anchoredPosition = new Vector2(0f, mainCenterY);
            bonusLine.Slot.anchoredPosition = new Vector2(0f, SlotBottomMargin + BonusSlotHeight * 0.5f);
            // 슬롯 창은 프레임이 펼쳐지는 만큼만 보인다. 프레임보다 먼저 전체 폭으로 나타나지 않게 폭과 알파를 맞춘다.
            float slotWidth = Mathf.Clamp(frameWidth - 12f, 0f, SlotWidth);
            float slotAlpha = frameState.FrameAlpha * Mathf.Clamp01((frameState.Spread - 0.35f) / 0.5f);
            mainLine.Slot.sizeDelta = new Vector2(slotWidth, MainSlotHeight);
            bonusLine.Slot.sizeDelta = new Vector2(slotWidth, BonusSlotHeight);
            mainLine.SetAlpha(slotAlpha);
            bonusLine.SetAlpha(slotAlpha);
            mainLine.SetFlash(baseLandedAt >= 0f ? GoldPickupPopupTimeline.EdgeFlash(t - baseLandedAt) : 0f);
            bonusLine.SetFlash(bonusLandedAt >= 0f ? GoldPickupPopupTimeline.EdgeFlash(t - bonusLandedAt) : 0f);

            ApplyFrameColors(frameState, level, t);
            ApplyLever(level, t);

            mainLine.SetValue(state.DisplayBase);
            mainLine.Move(LineMotion(state, false), GoldPickupPopupTimeline.MainGhostGap);
            if (showBonus)
            {
                bonusLine.SetValue(state.DisplayBonus);
                bonusLine.Move(LineMotion(state, true), GoldPickupPopupTimeline.BonusGhostGap);
            }

            ApplySweep(t);
        }

        // 퇴장 중에는 낙하의 역순으로 위로 빠지고, 그 밖에는 낙하·정착 모션이다.
        private static GoldPickupLineMotion LineMotion(GoldPickupPopupState state, bool bonus)
        {
            float x = GoldPickupPopupTimeline.TextExit(state.ExitProgress, bonus);
            if (x > 0f)
            {
                return GoldPickupPopupTimeline.LineRise(x, bonus ? BonusStartOffset : MainStartOffset);
            }

            return bonus
                ? GoldPickupPopupTimeline.Line(
                    state.Elapsed, state.BonusFallStart, BonusStartOffset, GoldPickupPopupTimeline.BonusOvershootPx)
                : GoldPickupPopupTimeline.Line(
                    state.Elapsed, state.BaseFallStart, MainStartOffset, GoldPickupPopupTimeline.MainOvershootPx);
        }

        // 팝업 바깥(위쪽)으로 튀어나오는 금화의 시작점. 프레임 위쪽 가장자리 중앙에서 솟는다.
        private Vector2 TopEdge()
        {
            return new Vector2(0f, height - CurrentSlide - SlotTopMargin);
        }

        private void ApplyFrameColors(FacilityNameTagFrame frameState, float level, float t)
        {
            float alpha = frameState.FrameAlpha * GoldPickupPopupTimeline.Flicker(t);
            float flash = Mathf.Max(
                baseLandedAt >= 0f ? GoldPickupPopupTimeline.EdgeFlash(t - baseLandedAt) : 0f,
                bonusLandedAt >= 0f ? GoldPickupPopupTimeline.EdgeFlash(t - bonusLandedAt) : 0f);
            fillImage.color = Scale(Fill, alpha);
            Color edgeColor = Color.Lerp(Edge, EdgeBright, flash);
            for (var i = 0; i < edges.Length; i++)
            {
                edges[i].color = Scale(edgeColor, alpha);
            }

            Color bracketColor = Color.Lerp(Accent, Color.white, Mathf.Clamp01(frameState.Flare - FacilityNameTagTimeline.SteadyFlare) * 1.6f);
            bracketColor.a = alpha;
            for (var i = 0; i < brackets.Length; i++)
            {
                brackets[i].color = bracketColor;
            }

            Color glowColor = Glow;
            glowColor.a = Glow.a * alpha * Mathf.Clamp01(frameState.Flare * 1.4f) * (1f + flash * 1.2f);
            glowImage.color = glowColor;

            Color scan = scanlineImage.color;
            scan.a = ScanlineAlpha * alpha;
            scanlineImage.color = scan;

            // 켜질 때 프레임 아래에서 위로 훑는 밝은 선. 퇴장 때는 레벨이 거꾸로 가므로 위에서 아래로 돌아간다.
            float bootAlpha = GoldPickupPopupTimeline.BootScanAlpha(level);
            bootRect.gameObject.SetActive(bootAlpha > 0.001f);
            if (bootAlpha > 0.001f)
            {
                bootRect.anchoredPosition = new Vector2(0f, GoldPickupPopupTimeline.BootScanPosition(level) * height);
                Color boot = EdgeBright;
                boot.a = bootAlpha * 0.9f;
                bootImage.color = boot;
            }
        }

        private void ApplyLever(float level, float t)
        {
            float angle = GoldPickupPopupTimeline.LeverAngle(t);
            LeverAngleDegrees = angle;
            // 프레임 오른쪽 가장자리 바깥에 매달려 프레임이 펼쳐지는 동안 같이 밀려 나온다.
            leverMount.anchoredPosition = new Vector2(frameWidth * 0.5f + LeverOffsetX, height * 0.5f);
            float alpha = GoldPickupPopupTimeline.LeverAlpha(level);
            leverGroup.alpha = alpha;

            // 정면 원근: 당길수록 막대는 짧아지다 아래로 뒤집히고 손잡이는 관객 쪽으로 다가와 커진다.
            float rodScale = GoldPickupPopupTimeline.LeverRodScale(angle);
            float ballScale = GoldPickupPopupTimeline.LeverBallScale(angle);
            float pull = Mathf.Clamp01(angle / GoldPickupPopupTimeline.LeverPullDegrees);
            leverRod.localScale = new Vector3(1f, rodScale, 1f);
            float ballY = LeverPivotY + LeverRodLength * rodScale;
            leverBall.anchoredPosition = new Vector2(0f, ballY);
            leverBall.localScale = new Vector3(ballScale, ballScale, 1f);
            leverBallImage.color = Color.Lerp(Color.white, new Color(1f, 0.97f, 0.8f, 1f), pull);

            // 가까워질수록 그림자는 판 위에서 커지고 손잡이에서 더 멀어진다.
            leverBallShadow.anchoredPosition = new Vector2(2f * ballScale, ballY - LeverBallSize * 0.5f * ballScale - 1f * pull);
            leverBallShadow.localScale = new Vector3(ballScale * (1f + 0.3f * pull), ballScale, 1f);
            Color shadow = leverBallShadowImage.color;
            shadow.a = 0.4f * (1f - 0.35f * pull);
            leverBallShadowImage.color = shadow;

            Color glow = leverGlow.color;
            glow.a = 0.16f + 0.4f * pull;
            leverGlow.color = glow;
            Color plate = plateShadow.color;
            plate.a = 0.5f;
            plateShadow.color = plate;
        }

        private void ApplySweep(float t)
        {
            if (sweepStartedAt < 0f)
            {
                return;
            }

            float progress = GoldPickupPopupTimeline.SweepProgress(t - sweepStartedAt);
            bool active = progress > 0f && progress < 1f;
            if (sweepRect.gameObject.activeSelf != active)
            {
                sweepRect.gameObject.SetActive(active);
            }

            if (!active)
            {
                return;
            }

            sweepRect.anchoredPosition = new Vector2(Mathf.Lerp(-SweepBarWidth * 0.5f, frameWidth + SweepBarWidth * 0.5f, progress), 0f);
            Color color = Sweep;
            color.a *= Mathf.Sin(Mathf.PI * progress);
            sweepImage.color = color;
        }

        private CoinSlot AcquireCoin()
        {
            for (var i = 0; i < coins.Count; i++)
            {
                if (!coins[i].Active)
                {
                    return coins[i];
                }
            }

            if (coins.Count >= GoldPickupPopupTimeline.MaxLiveCoins)
            {
                return null;
            }

            var rect = NewRect("Coin", coinLayer);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(coinSize, coinSize);
            var image = AddImage(rect, coinSprite, Color.white);
            image.preserveAspect = true;
            var slot = new CoinSlot { Rect = rect, Image = image };
            rect.gameObject.SetActive(false);
            coins.Add(slot);
            return slot;
        }

        private void DeactivateCoin(CoinSlot slot)
        {
            slot.Active = false;
            slot.Rect.gameObject.SetActive(false);
            ActiveCoinCount--;
        }

        private static Color Scale(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }

        private static void SetEdge(RectTransform target, Vector2 min, Vector2 max, Vector2 pivot, Vector2 size)
        {
            target.anchorMin = min;
            target.anchorMax = max;
            target.pivot = pivot;
            target.sizeDelta = size;
            target.anchoredPosition = Vector2.zero;
        }

        private static Image NewBracket(RectTransform parent, bool right, bool top, bool horizontal)
        {
            var bar = NewRect(horizontal ? "BracketH" : "BracketV", parent);
            var anchor = new Vector2(right ? 1f : 0f, top ? 1f : 0f);
            bar.anchorMin = bar.anchorMax = anchor;
            bar.pivot = anchor;
            bar.sizeDelta = horizontal
                ? new Vector2(BracketLength, BracketThickness)
                : new Vector2(BracketThickness, BracketLength);
            bar.anchoredPosition = Vector2.zero;
            return AddImage(bar, null, Accent);
        }

        private static RectTransform NewRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform target, float inset)
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.offsetMin = new Vector2(-inset, -inset);
            target.offsetMax = new Vector2(inset, inset);
        }

        private static Image AddImage(RectTransform target, Sprite sprite, Color color)
        {
            var image = target.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private sealed class CoinSlot
        {
            public RectTransform Rect;
            public Image Image;
            public bool Active;
            public float Age;
            public float Delay;
            public Vector2 Origin;
            public float Vx;
            public float Vy;
            public int SpinIndex;
        }

        /// <summary>슬롯 창(RectMask2D) 안에서 떨어지는 숫자 한 줄. 잔상은 같은 문구를 복제한 TMP다.</summary>
        private sealed class Line
        {
            private readonly TextMeshProUGUI[] ghosts;
            private readonly TextMeshProUGUI[] labels;
            private readonly CanvasGroup group;
            private readonly Image flash;
            private readonly bool isMain;
            private int shownValue = -1;
            private int activeGhosts;

            public RectTransform Slot { get; }
            public RectTransform Container { get; }
            public string Label { get; private set; } = string.Empty;
            public int ActiveGhosts => activeGhosts;

            public Line(
                RectTransform parent, string name, TMP_FontAsset font, Vector2 slotSize,
                float fontMax, float fontMin, int ghostCount, bool isMain)
            {
                this.isMain = isMain;
                Slot = NewRect(name, parent);
                Slot.anchorMin = Slot.anchorMax = new Vector2(0.5f, 0f);
                Slot.pivot = new Vector2(0.5f, 0.5f);
                Slot.sizeDelta = slotSize;
                Slot.gameObject.AddComponent<RectMask2D>();
                group = Slot.gameObject.AddComponent<CanvasGroup>();
                group.interactable = false;
                group.blocksRaycasts = false;

                // 슬롯 머신 릴 창: 어두운 바탕과 가는 금테, 위·아래로 짙어지는 음영이 숫자를 창 안에 가둔다.
                var window = NewRect("Window", Slot);
                Stretch(window, 0f);
                AddImage(window, null, WindowFill);
                var windowRim = NewRect("WindowRim", Slot);
                Stretch(windowRim, 0f);
                var rimImage = AddImage(windowRim, GoldPickupPopupSprites.RoundRect(2, 1f), WindowEdge);
                rimImage.type = Image.Type.Sliced;

                // 착지 순간 글자 뒤가 번쩍이는 금빛 섬광.
                var flashRect = NewRect("LandFlash", Slot);
                Stretch(flashRect, 0f);
                flash = AddImage(flashRect, GoldPickupPopupSprites.Glow(), new Color(1f, 0.85f, 0.4f, 0f));
                flash.type = Image.Type.Sliced;

                Container = NewRect("Line", Slot);
                Container.anchorMin = Container.anchorMax = new Vector2(0.5f, 0.5f);
                Container.pivot = new Vector2(0.5f, 0.5f);
                Container.sizeDelta = slotSize;

                ghosts = new TextMeshProUGUI[ghostCount];
                labels = new TextMeshProUGUI[ghostCount + 2];
                for (var i = 0; i < ghostCount; i++)
                {
                    ghosts[i] = NewLabel(Container, "Ghost" + i, font, fontMax, fontMin, Vector2.zero);
                    ghosts[i].gameObject.SetActive(false);
                    labels[i] = ghosts[i];
                }

                // 외곽선 머티리얼 인스턴스를 만들지 않도록 어두운 복제 글자를 그림자로 깐다.
                var shadow = NewLabel(Container, "Shadow", font, fontMax, fontMin, new Vector2(1.5f, -2f));
                shadow.color = ShadowTint;
                var fill = NewLabel(Container, "Fill", font, fontMax, fontMin, Vector2.zero);
                labels[ghostCount] = shadow;
                labels[ghostCount + 1] = fill;
                if (isMain)
                {
                    fill.color = Color.white;
                    fill.enableVertexGradient = true;
                    fill.colorGradient = new VertexGradient(
                        GoldPickupPresentation.MainTopColor,
                        GoldPickupPresentation.MainTopColor,
                        GoldPickupPresentation.MainBottomColor,
                        GoldPickupPresentation.MainBottomColor);
                    for (var i = 0; i < ghosts.Length; i++)
                    {
                        ghosts[i].enableVertexGradient = true;
                        ghosts[i].colorGradient = fill.colorGradient;
                    }
                }
                else
                {
                    fill.color = GoldPickupPresentation.BonusColor;
                }

                float shadeHeight = Mathf.Max(5f, slotSize.y * 0.2f);
                NewShade(Slot, "ShadeTop", true, shadeHeight);
                NewShade(Slot, "ShadeBottom", false, shadeHeight);
            }

            public void SetAlpha(float alpha)
            {
                group.alpha = alpha;
            }

            public void SetFlash(float amount)
            {
                Color color = flash.color;
                color.a = 0.24f * Mathf.Clamp01(amount);
                flash.color = color;
            }

            public void SetValue(int value)
            {
                if (value == shownValue)
                {
                    return;
                }

                shownValue = value;
                Label = !isMain
                    ? GoldPickupPresentation.FormatBonus(value)
                    : GoldPickupPresentation.FormatBase(value);
                for (var i = 0; i < labels.Length; i++)
                {
                    labels[i].text = Label;
                }
            }

            public void Move(GoldPickupLineMotion motion, float ghostGap)
            {
                Container.anchoredPosition = new Vector2(0f, motion.OffsetY);
                Container.localScale = new Vector3(motion.ScaleX, motion.ScaleY, 1f);

                int active = 0;
                for (var i = 0; i < ghosts.Length; i++)
                {
                    bool on = motion.Blur > 0.001f && motion.Speed > 0.001f;
                    if (ghosts[i].gameObject.activeSelf != on)
                    {
                        ghosts[i].gameObject.SetActive(on);
                    }

                    if (!on)
                    {
                        continue;
                    }

                    active++;
                    ghosts[i].rectTransform.anchoredPosition =
                        new Vector2(0f, motion.GhostDir * GoldPickupPopupTimeline.GhostOffset(motion.Speed, i, ghostGap));
                    float alpha = GoldPickupPopupTimeline.GhostAlpha(motion.Speed, i);
                    ghosts[i].color = isMain
                        ? new Color(1f, 1f, 1f, alpha)
                        : new Color(
                            GoldPickupPresentation.BonusColor.r,
                            GoldPickupPresentation.BonusColor.g,
                            GoldPickupPresentation.BonusColor.b,
                            alpha);
                }

                activeGhosts = active;
            }

            private static void NewShade(RectTransform slot, string name, bool top, float shadeHeight)
            {
                var shade = NewRect(name, slot);
                shade.anchorMin = new Vector2(0f, top ? 1f : 0f);
                shade.anchorMax = new Vector2(1f, top ? 1f : 0f);
                shade.pivot = new Vector2(0.5f, 0.5f);
                shade.sizeDelta = new Vector2(0f, shadeHeight);
                shade.anchoredPosition = new Vector2(0f, top ? -shadeHeight * 0.5f : shadeHeight * 0.5f);
                // 스프라이트는 위가 진하므로 아래 음영은 세로로 뒤집어 쓴다.
                shade.localScale = new Vector3(1f, top ? 1f : -1f, 1f);
                AddImage(shade, GoldPickupPopupSprites.VerticalFade(), WindowShade);
            }

            private static TextMeshProUGUI NewLabel(
                RectTransform parent, string name, TMP_FontAsset font, float fontMax, float fontMin, Vector2 offset)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                go.layer = parent.gameObject.layer;
                go.transform.SetParent(parent, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = offset;
                rect.offsetMax = offset;
                var text = go.GetComponent<TextMeshProUGUI>();
                if (font != null)
                {
                    text.font = font;
                }

                text.enableAutoSizing = true;
                text.fontSizeMin = fontMin;
                text.fontSizeMax = fontMax;
                text.fontSize = fontMax;
                text.alignment = TextAlignmentOptions.Center;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;
                text.richText = false;
                text.raycastTarget = false;
                text.outlineWidth = 0f;
                return text;
            }
        }
    }
}
