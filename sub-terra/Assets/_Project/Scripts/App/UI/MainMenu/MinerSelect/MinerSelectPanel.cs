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
        /// <summary>확정: 고른 카드에 원래 색이 차오르는 중(입력 막힘).</summary>
        Filling = 2,
        /// <summary>화면 전체 글리치(모션 감소면 짧은 암전).</summary>
        Exiting = 3,
        /// <summary>완전히 검게 닫힘. 인트로 오버레이가 이어서 덮는다.</summary>
        Black = 4
    }

    /// <summary>
    /// B-209 새 게임 전 광부 선택 화면. 메인 메뉴 패널 안에 코드로 만든다(Scene/프리팹 YAML 변경 없음).
    /// 상태는 MinerSelectFlow, 시간표는 MinerSelectMotion이 맡고 이 컴포넌트는 입력·표시·효과음만 한다.
    /// 새 게임 시작은 하지 않는다: 검게 닫히면 LaunchReady로 바인더에 넘긴다.
    /// </summary>
    public sealed class MinerSelectPanel : MonoBehaviour
    {
        public const string ObjectName = "MinerSelectPanel";
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;
        public const float CardSpacing = 251f;
        private const int NoiseBarCount = 10;
        private const float FlickerStepSeconds = 0.08f;

        private static readonly Color StartFillOn = new Color(0.03f, 0.2f, 0.25f, 0.94f);
        private static readonly Color StartFillOff = new Color(0.13f, 0.16f, 0.17f, 0.88f);
        private static readonly Color StartTextOff = new Color(0.47f, 0.52f, 0.54f, 1f);

        private RectTransform content;
        private MinerSelectCardView[] cards;
        private CanvasGroup hintGroup;
        private RectTransform hintRect;
        private TMP_Text hintText;
        private TMP_Text titleText;
        private TMP_Text subtitleText;
        private UnityEngine.UI.Button startButton;
        private UnityEngine.UI.Image startFill;
        private UnityEngine.UI.Image startOutline;
        private TMP_Text startLabel;
        private UnityEngine.UI.Button backButton;
        private TMP_Text backLabel;
        private UnityEngine.UI.Image blackout;
        private RectTransform[] noiseBars;
        private UnityEngine.UI.Image[] noiseImages;

        private MinerSelectSession session;
        private MinerSelectPhase phase = MinerSelectPhase.Hidden;
        private int openedFrame = -1;
        private readonly float[] hover = new float[MinerSelectFlow.CardCount];
        private readonly float[] detail = new float[MinerSelectFlow.CardCount];
        private readonly float[] brightness = { 1f, 1f };
        private float colorFill;
        private float hintElapsed = -1f;
        private float transitionElapsed;
        private bool transitionReduced;
        private float clock;

        public MinerSelectPhase Phase => phase;
        public bool IsOpen => phase != MinerSelectPhase.Hidden && gameObject.activeSelf;
        public MinerSelectSession Session => session;
        public MinerSelectFlow Flow => session != null ? session.Flow : null;
        public bool IsHintShown => hintGroup != null && hintGroup.gameObject.activeSelf && hintGroup.alpha > 0.001f;
        public bool StartInteractable => startButton != null && startButton.interactable;
        public float BlackAlpha => blackout != null ? blackout.color.a : 0f;
        public MinerSelectCardView Card(int index) => cards != null && index >= 0 && index < cards.Length ? cards[index] : null;
        public float CardDetail(int index) => index >= 0 && index < detail.Length ? detail[index] : 0f;
        public float CardBrightness(int index) => index >= 0 && index < brightness.Length ? brightness[index] : 1f;
        public float ColorFill => colorFill;

        /// <summary>뒤로(Esc·돌아가기 버튼). 바인더가 화면을 닫고 메뉴로 돌아간다.</summary>
        public event Action BackRequested;
        /// <summary>색 차오름 → 전환이 끝나 화면이 완전히 검게 닫혔다. 바인더가 인트로·새 게임을 시작한다.</summary>
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
                detail[i] = 0f;
                brightness[i] = 1f;
            }

            colorFill = 0f;
            hintElapsed = -1f;
            transitionElapsed = 0f;
            phase = MinerSelectPhase.Choosing;
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

            ApplyTransition(new MinerSelectTransitionFrame(0f, 0f, 0f, 0, false));
            Fit();
            RefreshStartButton();
            ApplyCards(0f);
            // 메뉴의 새 게임 버튼 선택이 남아 있으면 같은 Enter가 다시 눌린다.
            UiKeyboardSubmitGuard.ClearSelection();
        }

        /// <summary>즉시 숨긴다(돌아가기·메뉴 비활성).</summary>
        public void Close()
        {
            phase = MinerSelectPhase.Hidden;
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        /// <summary>새 게임 시작이 실패하면 검은 화면을 걷고 다시 고를 수 있게 한다.</summary>
        public void RecoverAfterFailedStart()
        {
            if (session == null) return;
            session.Flow.Unlock();
            colorFill = 0f;
            transitionElapsed = 0f;
            phase = MinerSelectPhase.Choosing;
            ApplyTransition(new MinerSelectTransitionFrame(0f, 0f, 0f, 0, false));
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
                    break;
                case MinerSelectResult.Switched:
                    SfxPlayer.Play(SfxId.UiSlotSelect);
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
            BackRequested?.Invoke();
        }

        public void MoveFocus(int direction)
        {
            if (!CanInput) return;
            if (session.Flow.MoveFocus(direction) == MinerSelectResult.Focused) SfxPlayer.Play(SfxId.UiHover);
        }

        public void Submit()
        {
            if (!CanInput) return;
            var before = session.Flow.SelectedIndex;
            var focus = session.Flow.FocusIndex;
            if (focus >= 0 && focus != before)
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
            RefreshStartButton();
            UiKeyboardSubmitGuard.ClearSelection();
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
            var dt = Time.unscaledDeltaTime;
            clock += dt;
            if (phase == MinerSelectPhase.Choosing && Time.frameCount != openedFrame) HandleKeyboard();
            Tick(dt);
        }

        /// <summary>시간을 진행한다. 확인용으로 바깥에서 큰 dt를 넣어도 된다.</summary>
        public void Tick(float dt)
        {
            if (phase == MinerSelectPhase.Hidden || session == null) return;
            UpdateHint(dt);

            if (phase == MinerSelectPhase.Filling)
            {
                colorFill = Mathf.Clamp01(colorFill + Mathf.Max(0f, dt) / MinerSelectMotion.ColorFillSeconds);
                if (colorFill >= 1f)
                {
                    transitionReduced = AccessibilityPreferences.ReduceMotion;
                    transitionElapsed = 0f;
                    phase = MinerSelectPhase.Exiting;
                    if (!transitionReduced) SfxPlayer.Play(SfxId.BriefingGlitchShort);
                }
            }
            else if (phase == MinerSelectPhase.Exiting)
            {
                transitionElapsed += Mathf.Max(0f, dt);
                var frame = MinerSelectMotion.EvaluateTransition(transitionElapsed, transitionReduced);
                ApplyTransition(frame);
                if (frame.IsComplete)
                {
                    phase = MinerSelectPhase.Black;
                    LaunchReady?.Invoke(session);
                }
            }

            ApplyCards(dt);
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
            hintElapsed += Mathf.Max(0f, dt);
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
            for (var i = 0; i < cards.Length; i++)
            {
                var focused = flow.FocusIndex == i && phase == MinerSelectPhase.Choosing;
                var selected = flow.SelectedIndex == i;
                var dimmed = flow.HasSelection && !selected;
                hover[i] = MinerSelectMotion.Approach(hover[i], focused ? 1f : 0f, dt, MinerSelectMotion.HoverSeconds);
                detail[i] = MinerSelectMotion.Approach(detail[i], selected ? 1f : 0f, dt, MinerSelectMotion.SilhouetteSharpenSeconds);
                brightness[i] = MinerSelectMotion.Approach(brightness[i], dimmed ? MinerSelectMotion.DimmedBrightness : 1f,
                    dt, MinerSelectMotion.SilhouetteSharpenSeconds);
                var fill = selected && phase >= MinerSelectPhase.Filling ? colorFill : 0f;
                var step = (int)(clock / FlickerStepSeconds);
                var flicker = 0.88f + 0.12f * MinerSelectMotion.Hash(step * 3 + i * 101);
                cards[i].Apply(hover[i], detail[i], brightness[i], fill, flicker);
            }
        }

        private void RefreshStartButton()
        {
            if (startButton == null || session == null) return;
            var enabled = session.Flow.CanConfirm && phase == MinerSelectPhase.Choosing;
            // 확정 뒤에는 켜진 모양을 유지하되 다시 눌리지 않는다.
            var lit = enabled || session.Flow.IsConfirmed;
            startButton.interactable = enabled;
            startFill.color = lit ? StartFillOn : StartFillOff;
            var outlineColor = lit ? MinerSelectArt.Cyan : new Color(0.5f, 0.56f, 0.58f, 0.55f);
            startOutline.color = outlineColor;
            startLabel.color = lit ? Color.white : StartTextOff;
        }

        private void ApplyTransition(MinerSelectTransitionFrame frame)
        {
            if (blackout != null) blackout.color = new Color(0f, 0f, 0f, frame.BlackAlpha);
            if (content != null) content.anchoredPosition = new Vector2(frame.SliceOffset, 0f);
            if (noiseBars == null) return;
            var random = new System.Random(frame.Seed * 7919 + 17);
            for (var i = 0; i < noiseBars.Length; i++)
            {
                if (frame.NoiseAlpha <= 0f)
                {
                    noiseImages[i].color = Color.clear;
                    continue;
                }

                var y = (float)random.NextDouble();
                var h = 0.006f + (float)random.NextDouble() * 0.05f;
                var w = 0.3f + (float)random.NextDouble() * 0.7f;
                var x = (float)random.NextDouble() * (1f - w);
                noiseBars[i].anchorMin = new Vector2(x, y);
                noiseBars[i].anchorMax = new Vector2(x + w, Mathf.Min(1f, y + h));
                var tint = Color.Lerp(MinerSelectArt.Cyan, Color.white, (float)random.NextDouble() * 0.7f);
                tint.a = frame.NoiseAlpha * (0.3f + (float)random.NextDouble() * 0.7f);
                noiseImages[i].color = tint;
            }
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
            if (startLabel != null) startLabel.text = LocalizationService.Get("miner_select.start", "탐사 시작");
            if (backLabel != null) backLabel.text = "←  " + LocalizationService.Get("miner_select.back", "돌아가기");
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

            // 메뉴 배경(광산 그림)은 비치게 두고 어둡게 덮는다. 아래 메뉴로 클릭이 새지 않게 막는다.
            NewImage(root, "Backdrop", null, new Color(0.004f, 0.02f, 0.03f, 0.66f), true).rectTransform.StretchFull();
            var vignette = NewImage(root, "Vignette", CoreCctvArt.Vignette(), Color.white, false);
            vignette.rectTransform.StretchFull();

            var frame = NewImage(root, "ScreenFrame", ResourceSellArt.ChamferOutline(), new Color(0.42f, 0.94f, 1f, 0.55f), false);
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            frame.rectTransform.StretchFull();
            frame.rectTransform.offsetMin = new Vector2(14f, 14f);
            frame.rectTransform.offsetMax = new Vector2(-14f, -14f);

            content = NewRect(root, "Content", Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight));

            // 머리: SUB-TERRA OPS / 광부 선택 / MINER REGISTRATION / 구분선 / 설명.
            var ops = NewText(content, "OpsLabel", font, LocalizationService.Get("miner_select.ops", "SUB-TERRA OPS"), 24f,
                MinerSelectArt.Cyan, new Vector2(0f, 502f), new Vector2(400f, 36f));
            ops.characterSpacing = 8f;
            OpsLine(content, -238f, 502f);
            OpsLine(content, 238f, 502f);
            titleText = NewText(content, "Title", font, string.Empty, 68f, new Color(0.9f, 1f, 1f, 1f), new Vector2(0f, 450f), new Vector2(900f, 84f));
            titleText.fontStyle = FontStyles.Bold;
            var registration = NewText(content, "Registration", font, LocalizationService.Get("miner_select.registration", "MINER REGISTRATION"),
                24f, MinerSelectArt.Cyan, new Vector2(0f, 396f), new Vector2(700f, 36f));
            registration.characterSpacing = 14f;
            var divider = NewImage(content, "Divider", null, new Color(0.42f, 0.94f, 1f, 0.45f), false);
            divider.rectTransform.anchoredPosition = new Vector2(0f, 370f);
            divider.rectTransform.sizeDelta = new Vector2(680f, 2f);
            var dot = NewImage(content, "DividerDot", MinerSelectArt.SoftDisc(), MinerSelectArt.Cyan, false);
            dot.rectTransform.anchoredPosition = new Vector2(0f, 370f);
            dot.rectTransform.sizeDelta = new Vector2(14f, 14f);
            subtitleText = NewText(content, "Subtitle", font, string.Empty, 26f, new Color(0.88f, 0.95f, 0.97f, 1f),
                new Vector2(0f, 338f), new Vector2(1000f, 36f));

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

            BuildStartButton(font);
            BuildBackButton(font);

            // 글리치 전환 층: 노이즈 막대 → 검은 막. 맨 위에서 화면 전체를 덮는다.
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

            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(startButton);
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(backButton);
            RefreshTexts();
        }

        private void BuildStartButton(TMP_FontAsset font)
        {
            var rect = NewRect(content, "StartButton", new Vector2(0f, -440f), new Vector2(560f, 88f));
            startFill = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            startFill.sprite = ResourceSellArt.ChamferFill();
            startFill.type = UnityEngine.UI.Image.Type.Sliced;
            startButton = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            startButton.transition = UnityEngine.UI.Selectable.Transition.None;
            startButton.targetGraphic = startFill;
            startButton.onClick.AddListener(PressStart);

            var glow = NewImage(rect, "Hover", ResourceSellArt.ChamferFill(), new Color(0.42f, 0.94f, 1f, 0f), false);
            glow.type = UnityEngine.UI.Image.Type.Sliced;
            glow.rectTransform.StretchFull();
            var skin = rect.gameObject.AddComponent<MenuSpriteButtonSkin>();
            skin.SetOverlay(glow);
            skin.SetMaxAlpha(0.28f);

            startOutline = NewImage(rect, "Outline", ResourceSellArt.ChamferOutline(), MinerSelectArt.Cyan, false);
            startOutline.type = UnityEngine.UI.Image.Type.Sliced;
            startOutline.rectTransform.StretchFull();
            startLabel = NewText(rect, "Label", font, string.Empty, 38f, Color.white, Vector2.zero, new Vector2(540f, 80f));
            startLabel.fontStyle = FontStyles.Bold;
            startLabel.characterSpacing = 4f;
            // 꺼진 상태에서 누르면 실패음만 난다(UiSfxButton). 켜진 클릭음은 PressStart가 낸다.
            UiSfx.Attach(startButton, SfxId.None, hover: true, failWhenDisabled: true);
        }

        private void BuildBackButton(TMP_FontAsset font)
        {
            var rect = NewRect(content, "BackButton", new Vector2(-ReferenceWidth * 0.5f + 190f, -458f), new Vector2(240f, 66f));
            var fill = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            fill.sprite = ResourceSellArt.ChamferFill();
            fill.type = UnityEngine.UI.Image.Type.Sliced;
            fill.color = new Color(0.02f, 0.1f, 0.13f, 0.9f);
            backButton = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            backButton.transition = UnityEngine.UI.Selectable.Transition.None;
            backButton.targetGraphic = fill;
            backButton.onClick.AddListener(PressBack);

            var glow = NewImage(rect, "Hover", ResourceSellArt.ChamferFill(), new Color(0.42f, 0.94f, 1f, 0f), false);
            glow.type = UnityEngine.UI.Image.Type.Sliced;
            glow.rectTransform.StretchFull();
            var skin = rect.gameObject.AddComponent<MenuSpriteButtonSkin>();
            skin.SetOverlay(glow);
            skin.SetMaxAlpha(0.3f);

            var outline = NewImage(rect, "Outline", ResourceSellArt.ChamferOutline(), MinerSelectArt.Cyan, false);
            outline.type = UnityEngine.UI.Image.Type.Sliced;
            outline.rectTransform.StretchFull();
            backLabel = NewText(rect, "Label", font, string.Empty, 28f, new Color(0.9f, 0.98f, 1f, 1f), Vector2.zero, new Vector2(230f, 60f));
            UiSfx.Attach(backButton, SfxId.None, hover: true);
        }

        private static void OpsLine(Transform parent, float x, float y, float width = 150f)
        {
            var line = NewImage(parent, "Line", null, new Color(0.42f, 0.94f, 1f, 0.6f), false);
            line.rectTransform.anchoredPosition = new Vector2(x, y);
            line.rectTransform.sizeDelta = new Vector2(width, 2f);
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
