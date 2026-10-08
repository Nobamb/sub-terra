using System.Collections.Generic;
using SubTerra.App.UI.FacilityNameTag;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 골드 획득 팝업의 얇은 View (prompt-B 142). 시설 이름표와 같은 모양 언어의 금색 홀로그램 프레임,
    /// 오른쪽 장식 레버, RectMask2D 슬롯 안에서 떨어지는 숫자·모션블러 잔상, 팝업 금화를 그린다.
    /// 값과 시간은 모두 GoldPickupPopupState / GoldPickupPopupTimeline에서 받는다.
    /// 월드 캔버스는 FacilityNameTagLayers.MainScreen에 두어 CCTV에 보이지 않고, 모든 그래픽은 raycast를 막지 않는다.
    /// </summary>
    public sealed class GoldPickupPopupVisual
    {
        public const int SortingOrder = GoldPickupVfx.WorldSortingOrder + 20;

        private const float MainFontMax = 28f;
        private const float MainFontMin = 16f;
        private const float BonusFontMax = 17f;
        private const float BonusFontMin = 11f;
        private const float TextInset = 8f;
        private const float MainSlotTop = 4f;
        private const float BonusSlotTop = 46f;
        private const float BracketLength = 8f;
        private const float BracketThickness = 2f;
        private const float SweepLength = 26f;
        private const float SweepThickness = 3f;
        private const float RodLength = 13f;
        private const float RodWidth = 3.5f;
        private const float KnobSize = 11f;
        private const float AxleSize = 7f;
        private const float BodyCenterX = GoldPickupPopupTimeline.BodyWidth * 0.5f - GoldPickupPopupTimeline.FrameWidth * 0.5f;

        private readonly GameObject root;
        private readonly RectTransform rootRect;
        private readonly CanvasGroup group;
        private readonly RectTransform frame;
        private readonly Image glowImage;
        private readonly Image fillImage;
        private readonly Image dividerImage;
        private readonly Image sweepImage;
        private readonly RectTransform sweepRect;
        private readonly Image[] edges = new Image[4];
        private readonly Image[] brackets = new Image[8];
        private readonly RectTransform rodPivot;
        private readonly Image rodImage;
        private readonly Image knobImage;
        private readonly Image axleImage;
        private readonly LineView mainLine;
        private readonly LineView bonusLine;
        private readonly RectTransform coinLayer;
        private readonly Sprite coinSprite;
        private readonly List<CoinAnim> coins = new List<CoinAnim>(GoldPickupPopupTimeline.MaxAliveCoins);

        private float flashClock = -1f;
        private float sweepClock = -1f;
        private float frameWidthNow = GoldPickupPopupTimeline.MinSpreadWidth;
        private float frameHeightNow = GoldPickupPopupTimeline.BaseHeight;
        private int aliveCoins;
        private bool bonusMaskActive;

        public GameObject Root => root;
        public bool IsAlive => root != null;
        public int ActiveCoinCount => aliveCoins;
        public float FrameWidthNow => frameWidthNow;
        public float FrameHeightNow => frameHeightNow;
        public float LeverAngleNow => rodPivot != null ? Mathf.DeltaAngle(0f, rodPivot.localEulerAngles.z) : 0f;
        public RectTransform MainLineRect => mainLine.Line;
        public RectTransform BonusLineRect => bonusLine.Line;
        public Vector3 MainLineWorldPosition => mainLine.Line != null ? mainLine.Line.position : Vector3.zero;
        public TMP_Text MainFill => mainLine.Fill;
        public TMP_Text BonusFill => bonusLine.Fill;
        public int MainGhostActiveCount => mainLine.ActiveGhosts;
        public int BonusGhostActiveCount => bonusLine.ActiveGhosts;
        public int MainGhostCount => mainLine.Ghosts.Length;
        public TMP_Text MainGhost(int index) => mainLine.Ghosts[index];
        public TMP_Text BonusGhost(int index) => bonusLine.Ghosts[index];
        public bool BonusMaskActive => bonusMaskActive;
        public float CanvasAlpha => group != null ? group.alpha : 0f;

        private GoldPickupPopupVisual(GameObject root, TMP_FontAsset font, Sprite coinSprite)
        {
            this.root = root;
            this.coinSprite = coinSprite;
            rootRect = (RectTransform)root.transform;
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.sizeDelta = new Vector2(GoldPickupPopupTimeline.FrameWidth, GoldPickupPopupTimeline.BaseHeight);
            group = root.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            frame = NewRect("Frame", rootRect);
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0f);
            frame.pivot = new Vector2(0.5f, 0f);
            frame.anchoredPosition = Vector2.zero;

            var glow = NewRect("Glow", frame);
            Stretch(glow, 9f);
            glowImage = AddImage(glow, GoldPickupPopupArt.Glow(), GoldPickupPresentation.FrameGlowColor);
            glowImage.type = Image.Type.Sliced;
            var fill = NewRect("Fill", frame);
            Stretch(fill, 0f);
            fillImage = AddImage(fill, null, GoldPickupPresentation.FrameFillColor);

            var thickness = GoldPickupPopupTimeline.EdgeThickness;
            for (var i = 0; i < 4; i++)
            {
                var edgeRect = NewRect("Edge" + i, frame);
                switch (i)
                {
                    case 0:
                        SetEdge(edgeRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, thickness));
                        break;
                    case 1:
                        SetEdge(edgeRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, thickness));
                        break;
                    case 2:
                        SetEdge(edgeRect, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(thickness, 0f));
                        break;
                    default:
                        SetEdge(edgeRect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(thickness, 0f));
                        break;
                }

                edges[i] = AddImage(edgeRect, null, GoldPickupPresentation.FrameEdgeColor);
            }

            for (var c = 0; c < 4; c++)
            {
                var right = (c & 1) == 1;
                var top = (c & 2) == 2;
                brackets[c * 2] = NewBracket(frame, right, top, true);
                brackets[c * 2 + 1] = NewBracket(frame, right, top, false);
            }

            // 본문과 레버 공간을 가르는 가는 구분선.
            var divider = NewRect("LeverDivider", frame);
            divider.anchorMin = new Vector2(1f, 0f);
            divider.anchorMax = new Vector2(1f, 1f);
            divider.pivot = new Vector2(0.5f, 0.5f);
            divider.sizeDelta = new Vector2(1f, -14f);
            divider.anchoredPosition = new Vector2(-GoldPickupPopupTimeline.LeverZoneWidth, 0f);
            dividerImage = AddImage(divider, null, GoldPickupPresentation.FrameEdgeColor);

            sweepRect = NewRect("Sweep", frame);
            sweepRect.anchorMin = sweepRect.anchorMax = new Vector2(0.5f, 0f);
            sweepRect.pivot = new Vector2(0.5f, 0.5f);
            sweepImage = AddImage(sweepRect, null, GoldPickupPresentation.FrameAccentColor);
            sweepImage.enabled = false;

            mainLine = BuildLine(frame, "Main", font, false);
            bonusLine = BuildLine(frame, "Bonus", font, true);
            bonusLine.Mask.gameObject.SetActive(false);

            // 오른쪽 레버: 축은 프레임에 고정되고 막대만 축을 중심으로 회전한다(클릭 버튼 아님).
            var lever = NewRect("Lever", frame);
            lever.anchorMin = lever.anchorMax = new Vector2(1f, 1f);
            lever.pivot = new Vector2(0.5f, 0.5f);
            lever.sizeDelta = new Vector2(GoldPickupPopupTimeline.LeverZoneWidth, GoldPickupPopupTimeline.BaseHeight);
            lever.anchoredPosition = new Vector2(
                -GoldPickupPopupTimeline.LeverZoneWidth * 0.5f,
                -GoldPickupPopupTimeline.BaseHeight * 0.5f);
            rodPivot = NewRect("RodPivot", lever);
            rodPivot.anchorMin = rodPivot.anchorMax = new Vector2(0.5f, 0.5f);
            rodPivot.pivot = new Vector2(0.5f, 0.5f);
            rodPivot.sizeDelta = Vector2.zero;
            var rod = NewRect("Rod", rodPivot);
            rod.anchorMin = rod.anchorMax = new Vector2(0.5f, 0.5f);
            rod.pivot = new Vector2(0.5f, 0f);
            rod.sizeDelta = new Vector2(RodWidth, RodLength);
            rod.anchoredPosition = Vector2.zero;
            rodImage = AddImage(rod, null, GoldPickupPresentation.LeverMetalColor);
            var knob = NewRect("Knob", rodPivot);
            knob.anchorMin = knob.anchorMax = new Vector2(0.5f, 0.5f);
            knob.pivot = new Vector2(0.5f, 0.5f);
            knob.sizeDelta = new Vector2(KnobSize, KnobSize);
            knob.anchoredPosition = new Vector2(0f, RodLength);
            knobImage = AddImage(knob, GoldPickupPopupArt.Knob(), GoldPickupPresentation.LeverKnobColor);
            var axle = NewRect("Axle", lever);
            axle.anchorMin = axle.anchorMax = new Vector2(0.5f, 0.5f);
            axle.pivot = new Vector2(0.5f, 0.5f);
            axle.sizeDelta = new Vector2(AxleSize, AxleSize);
            axle.anchoredPosition = Vector2.zero;
            axleImage = AddImage(axle, GoldPickupPopupArt.Knob(), GoldPickupPresentation.LeverMetalColor);

            // 금화는 슬롯 마스크 밖, 팝업 맨 위 레이어에 둔다.
            coinLayer = NewRect("CoinLayer", rootRect);
            coinLayer.anchorMin = coinLayer.anchorMax = new Vector2(0.5f, 0f);
            coinLayer.pivot = new Vector2(0.5f, 0f);
            coinLayer.sizeDelta = Vector2.zero;
            coinLayer.anchoredPosition = Vector2.zero;
        }

        /// <summary>월드 캔버스 팝업. 시설 이름표와 같은 디자인 px 비율(1칸 = 100px)로 줄여 쓴다.</summary>
        public static GoldPickupPopupVisual Create(Transform parent, TMP_FontAsset font, Sprite coinSprite, string name = "GoldPickupPopup")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            go.layer = FacilityNameTagLayers.MainScreen;
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
            canvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
            go.transform.localScale = Vector3.one * FacilityNameTagVisual.WorldPerPixel;
            return new GoldPickupPopupVisual(go, font, coinSprite);
        }

        /// <summary>플레이어 추적 위치(anchor)와 퇴장 상승 오프셋(rise)을 분리해 적용한다.</summary>
        public void SetWorldPosition(Vector3 anchor, float rise)
        {
            if (root == null)
            {
                return;
            }

            rootRect.position = anchor + Vector3.up * rise;
        }

        /// <summary>기본 착지: 테두리를 짧게 밝힌다.</summary>
        public void FlashEdge()
        {
            flashClock = 0f;
        }

        /// <summary>추가 착지: 프레임을 따라 금빛을 흐르게 한다.</summary>
        public void StartSweep()
        {
            sweepClock = 0f;
        }

        public void Tick(float deltaSeconds, GoldPickupPopupState state)
        {
            if (root == null || state == null)
            {
                return;
            }

            if (flashClock >= 0f)
            {
                flashClock += deltaSeconds;
                if (flashClock >= GoldPickupPopupTimeline.EdgeFlashDuration)
                {
                    flashClock = -1f;
                }
            }

            if (sweepClock >= 0f)
            {
                sweepClock += deltaSeconds;
                if (sweepClock >= GoldPickupPopupTimeline.SweepDuration)
                {
                    sweepClock = -1f;
                }
            }

            TickCoins(deltaSeconds);
            ApplyFrame(state);
            ApplyRows(state);
            ApplyLever(state);
            group.alpha = GoldPickupPopupTimeline.ExitAlpha(state.ExitAmount);
        }

        /// <summary>요청한 수만큼 팝업 위쪽 가장자리에서 금화를 분출한다. 살아 있는 금화 상한을 넘기면 생략한다.</summary>
        public int SpawnBurst(int requested, float xOffset = 0f)
        {
            if (root == null || coinSprite == null)
            {
                return 0;
            }

            int count = GoldPickupPopupTimeline.LimitCoinSpawn(aliveCoins, requested);
            var origin = new Vector2(BodyCenterX + xOffset, frameHeightNow);
            var spawned = 0;
            for (var index = 0; index < count; index++)
            {
                CoinAnim coin = AcquireCoin();
                if (coin == null)
                {
                    break;
                }

                GoldPickupPopupTimeline.CoinLaunch(index, count, out float vx, out float vy, out float delay);
                coin.Index = index;
                coin.Origin = origin;
                coin.Vx = vx;
                coin.Vy = vy;
                coin.Delay = delay;
                coin.Age = 0f;
                coin.Active = true;
                float size = GoldPickupPopupTimeline.CoinSize(index);
                coin.Rect.sizeDelta = new Vector2(size, size);
                coin.Rect.anchoredPosition = origin;
                coin.Rect.localScale = Vector3.one;
                coin.Image.color = new Color(1f, 1f, 1f, 0f);
                coin.Rect.gameObject.SetActive(true);
                aliveCoins++;
                spawned++;
            }

            return spawned;
        }

        public void Destroy()
        {
            if (root == null)
            {
                return;
            }

            aliveCoins = 0;
            coins.Clear();
            if (Application.isPlaying)
            {
                Object.Destroy(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }

        private void ApplyFrame(GoldPickupPopupState state)
        {
            // 시설 이름표의 Spread / FrameAlpha / Flare만 읽기 전용으로 재사용한다.
            var level = FacilityNameTagTimeline.Evaluate(GoldPickupPopupTimeline.FrameLevel(state.Age), true);
            frameWidthNow = Mathf.Lerp(
                GoldPickupPopupTimeline.MinSpreadWidth, GoldPickupPopupTimeline.FrameWidth, level.Spread);
            frameHeightNow = state.FrameHeight;
            rootRect.sizeDelta = new Vector2(GoldPickupPopupTimeline.FrameWidth, frameHeightNow);
            frame.sizeDelta = new Vector2(frameWidthNow, frameHeightNow);

            float alpha = level.FrameAlpha;
            float flash = GoldPickupPopupTimeline.EdgeFlash(flashClock);
            float sweep = GoldPickupPopupTimeline.SweepStrength(sweepClock);
            fillImage.color = Scale(GoldPickupPresentation.FrameFillColor, alpha);
            Color edge = Color.Lerp(GoldPickupPresentation.FrameEdgeColor, GoldPickupPresentation.FrameAccentColor, flash);
            edge.a = Mathf.Lerp(GoldPickupPresentation.FrameEdgeColor.a, 1f, flash);
            for (var i = 0; i < edges.Length; i++)
            {
                edges[i].color = Scale(edge, alpha);
            }

            Color divider = GoldPickupPresentation.FrameEdgeColor;
            divider.a *= 0.45f;
            dividerImage.color = Scale(divider, alpha);

            float flare = Mathf.Clamp01(level.Flare - FacilityNameTagTimeline.SteadyFlare);
            Color bracket = Color.Lerp(GoldPickupPresentation.FrameAccentColor, Color.white, flare * 1.6f);
            bracket.a = alpha;
            for (var i = 0; i < brackets.Length; i++)
            {
                brackets[i].color = bracket;
            }

            Color glow = GoldPickupPresentation.FrameGlowColor;
            glow.a *= alpha * Mathf.Clamp01(level.Flare * 1.4f) * (1f + 0.6f * flash + 0.4f * sweep);
            glowImage.color = glow;

            if (sweep > 0f)
            {
                Vector2 point = GoldPickupPopupTimeline.PerimeterPoint(
                    sweepClock / GoldPickupPopupTimeline.SweepDuration,
                    frameWidthNow,
                    frameHeightNow,
                    out bool horizontal);
                sweepRect.anchoredPosition = point;
                sweepRect.sizeDelta = horizontal
                    ? new Vector2(SweepLength, SweepThickness)
                    : new Vector2(SweepThickness, SweepLength);
                sweepImage.color = Scale(GoldPickupPresentation.FrameAccentColor, sweep);
                sweepImage.enabled = true;
            }
            else if (sweepImage.enabled)
            {
                sweepImage.enabled = false;
            }
        }

        private void ApplyRows(GoldPickupPopupState state)
        {
            ApplyLine(
                mainLine,
                GoldPickupPopupTimeline.EvaluateRow(state.BaseFallTime, GoldPickupPopupTimeline.MainSlotHeight),
                state.BaseDisplay,
                state.BaseFallTime > 0f,
                false);

            if (state.HasBonus)
            {
                if (!bonusMaskActive)
                {
                    bonusMaskActive = true;
                    bonusLine.Mask.gameObject.SetActive(true);
                }

                ApplyLine(
                    bonusLine,
                    GoldPickupPopupTimeline.EvaluateRow(state.BonusFallTime, GoldPickupPopupTimeline.BonusSlotHeight),
                    state.BonusDisplay,
                    state.BonusFallTime > 0f,
                    true);
            }
        }

        private void ApplyLine(LineView view, GoldPickupRowMotion motion, int value, bool visible, bool bonus)
        {
            if (!visible)
            {
                if (view.Active)
                {
                    view.Line.gameObject.SetActive(false);
                    view.Active = false;
                }

                return;
            }

            if (!view.Active)
            {
                view.Line.gameObject.SetActive(true);
                view.Active = true;
            }

            if (view.Value != value)
            {
                view.Value = value;
                string label = bonus
                    ? GoldPickupPresentation.FormatBonusLine(value)
                    : GoldPickupPresentation.FormatBaseLine(value);
                view.Shadow.text = label;
                view.Fill.text = label;
            }

            view.Line.anchoredPosition = new Vector2(0f, motion.OffsetY);
            view.Line.localScale = new Vector3(motion.ScaleX, motion.ScaleY, 1f);

            Color ghostColor = bonus ? GoldPickupPresentation.BonusColor : GoldPickupPresentation.MainTopColor;
            for (var i = 0; i < view.Ghosts.Length; i++)
            {
                float alpha = GoldPickupPopupTimeline.GhostAlpha(i, motion.Blur);
                bool on = alpha > 0f;
                TextMeshProUGUI ghost = view.Ghosts[i];
                if (on != view.GhostOn[i])
                {
                    view.GhostOn[i] = on;
                    ghost.gameObject.SetActive(on);
                    view.ActiveGhosts += on ? 1 : -1;
                }

                if (!on)
                {
                    continue;
                }

                if (view.GhostValue[i] != value)
                {
                    view.GhostValue[i] = value;
                    ghost.text = view.Fill.text;
                }

                ghostColor.a = alpha;
                ghost.color = ghostColor;
                ((RectTransform)ghost.transform).anchoredPosition = new Vector2(
                    0f,
                    GoldPickupPopupTimeline.GhostOffset(i, motion.Blur, view.GhostSpacing));
            }
        }

        private void ApplyLever(GoldPickupPopupState state)
        {
            rodPivot.localRotation = Quaternion.Euler(0f, 0f, state.LeverAngle);
            float alpha = FacilityNameTagTimeline.Evaluate(GoldPickupPopupTimeline.FrameLevel(state.Age), true).FrameAlpha;
            rodImage.color = Scale(GoldPickupPresentation.LeverMetalColor, alpha);
            knobImage.color = Scale(GoldPickupPresentation.LeverKnobColor, alpha);
            axleImage.color = Scale(GoldPickupPresentation.LeverMetalColor, alpha);
        }

        private void TickCoins(float dt)
        {
            for (var i = 0; i < coins.Count; i++)
            {
                CoinAnim coin = coins[i];
                if (!coin.Active)
                {
                    continue;
                }

                coin.Age += dt;
                float local = coin.Age - coin.Delay;
                if (local < 0f)
                {
                    continue;
                }

                if (local >= GoldPickupPopupTimeline.CoinLife)
                {
                    coin.Active = false;
                    coin.Rect.gameObject.SetActive(false);
                    aliveCoins--;
                    continue;
                }

                coin.Rect.anchoredPosition = GoldPickupPopupTimeline.CoinPosition(coin.Origin, coin.Vx, coin.Vy, local);
                coin.Rect.localScale = new Vector3(GoldPickupPresentation.CoinSpinScaleX(coin.Index, local), 1f, 1f);
                float alpha = GoldPickupPresentation.CoinFadeIn(local)
                    * GoldPickupPresentation.CoinAlpha(local / GoldPickupPopupTimeline.CoinLife);
                coin.Image.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        private CoinAnim AcquireCoin()
        {
            for (var i = 0; i < coins.Count; i++)
            {
                if (!coins[i].Active)
                {
                    return coins[i];
                }
            }

            if (coins.Count >= GoldPickupPopupTimeline.MaxAliveCoins)
            {
                return null;
            }

            var go = NewRect("Coin", coinLayer);
            go.anchorMin = go.anchorMax = new Vector2(0.5f, 0.5f);
            go.pivot = new Vector2(0.5f, 0.5f);
            var image = AddImage(go, coinSprite, Color.white);
            image.preserveAspect = true;
            go.gameObject.SetActive(false);
            var coin = new CoinAnim { Rect = go, Image = image };
            coins.Add(coin);
            return coin;
        }

        private LineView BuildLine(RectTransform parent, string name, TMP_FontAsset font, bool bonus)
        {
            float slot = bonus ? GoldPickupPopupTimeline.BonusSlotHeight : GoldPickupPopupTimeline.MainSlotHeight;
            var mask = NewRect(name + "Mask", parent);
            mask.anchorMin = mask.anchorMax = new Vector2(0f, 1f);
            mask.pivot = new Vector2(0f, 1f);
            mask.sizeDelta = new Vector2(GoldPickupPopupTimeline.BodyWidth, slot);
            mask.anchoredPosition = new Vector2(0f, -(bonus ? BonusSlotTop : MainSlotTop));
            mask.gameObject.AddComponent<RectMask2D>();

            var line = NewRect(name + "Line", mask);
            line.anchorMin = line.anchorMax = new Vector2(0.5f, 0.5f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.sizeDelta = new Vector2(GoldPickupPopupTimeline.BodyWidth, slot);
            line.anchoredPosition = new Vector2(0f, slot);

            float max = bonus ? BonusFontMax : MainFontMax;
            float min = bonus ? BonusFontMin : MainFontMin;
            int ghostCount = bonus ? GoldPickupPopupTimeline.BonusGhostCount : GoldPickupPopupTimeline.MainGhostCount;
            var view = new LineView
            {
                Mask = mask,
                Line = line,
                Ghosts = new TextMeshProUGUI[ghostCount],
                GhostOn = new bool[ghostCount],
                GhostValue = new int[ghostCount],
                GhostSpacing = bonus ? GoldPickupPopupTimeline.BonusGhostSpacing : GoldPickupPopupTimeline.MainGhostSpacing,
                Value = -1
            };

            // 같은 폰트·공유 머티리얼의 복제 TMP로 잔상을 만든다. 가장 뒤쪽 잔상부터 그린다.
            for (var i = ghostCount - 1; i >= 0; i--)
            {
                TextMeshProUGUI ghost = NewText(line, "Ghost" + i, font, max, min, Vector2.zero);
                ghost.color = new Color(1f, 1f, 1f, 0f);
                ghost.gameObject.SetActive(false);
                view.Ghosts[i] = ghost;
                view.GhostValue[i] = -1;
            }

            view.Shadow = NewText(line, "Shadow", font, max, min, new Vector2(1.5f, -2f));
            view.Shadow.color = GoldPickupPresentation.ShadowColor;
            view.Fill = NewText(line, "Fill", font, max, min, Vector2.zero);
            if (bonus)
            {
                view.Fill.color = GoldPickupPresentation.BonusColor;
            }
            else
            {
                view.Fill.color = Color.white;
                view.Fill.enableVertexGradient = true;
                view.Fill.colorGradient = new VertexGradient(
                    GoldPickupPresentation.MainTopColor,
                    GoldPickupPresentation.MainTopColor,
                    GoldPickupPresentation.MainBottomColor,
                    GoldPickupPresentation.MainBottomColor);
            }

            // 낙하 시작 전에는 숨겨 둔다.
            line.gameObject.SetActive(false);
            return view;
        }

        private static TextMeshProUGUI NewText(
            RectTransform parent, string name, TMP_FontAsset font, float max, float min, Vector2 offset)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(TextInset, 0f) + offset;
            rect.offsetMax = new Vector2(-TextInset, 0f) + offset;
            var text = go.GetComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = max;
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = false;
            text.raycastTarget = false;
            return text;
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
            return AddImage(bar, null, GoldPickupPresentation.FrameAccentColor);
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

        private sealed class LineView
        {
            public RectTransform Mask;
            public RectTransform Line;
            public TextMeshProUGUI Shadow;
            public TextMeshProUGUI Fill;
            public TextMeshProUGUI[] Ghosts;
            public bool[] GhostOn;
            public int[] GhostValue;
            public float GhostSpacing;
            public int ActiveGhosts;
            public int Value;
            public bool Active;
        }

        private sealed class CoinAnim
        {
            public RectTransform Rect;
            public Image Image;
            public int Index;
            public Vector2 Origin;
            public float Vx;
            public float Vy;
            public float Delay;
            public float Age;
            public bool Active;
        }
    }

    /// <summary>팝업 전용 절차 스프라이트(발광 9분할, 손잡이 원). 한 번만 만들어 재사용한다.</summary>
    internal static class GoldPickupPopupArt
    {
        private static Sprite glow;
        private static Sprite knob;

        public static Sprite Glow()
        {
            if (glow == null)
            {
                const int size = 48;
                const float fade = 16f;
                var pixels = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        float edge = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                        float t = Mathf.Clamp01(edge / fade);
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(t * t * (3f - 2f * t) * 255f));
                    }
                }

                glow = MakeSprite(size, pixels, new Vector4(fade, fade, fade, fade));
            }

            return glow;
        }

        public static Sprite Knob()
        {
            if (knob == null)
            {
                const int size = 32;
                var pixels = new Color32[size * size];
                float radius = size * 0.5f - 1f;
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - size * 0.5f;
                        float dy = y + 0.5f - size * 0.5f;
                        float coverage = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(coverage * 255f));
                    }
                }

                knob = MakeSprite(size, pixels, Vector4.zero);
            }

            return knob;
        }

        private static Sprite MakeSprite(int size, Color32[] pixels, Vector4 border)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                border);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
