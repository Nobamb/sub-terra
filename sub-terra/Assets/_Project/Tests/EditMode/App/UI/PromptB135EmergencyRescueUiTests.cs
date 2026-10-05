using NUnit.Framework;
using SubTerra.App.Run;
using SubTerra.App.UI.EmergencyRescue;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB135EmergencyRescueUiTests
    {
        private GameObject canvasObject;
        private EmergencyRescuePanelView view;
        private EmergencyRescueCost sampleCost;

        [SetUp]
        public void SetUp()
        {
            canvasObject = new GameObject("B135Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            view = EmergencyRescuePanelView.Create(canvasObject.transform, null);
            sampleCost = new EmergencyRescueCost(
                1240,
                250,
                new[]
                {
                    new EmergencyRescueMineralCost("mineral.copper", "구리", 10, 8),
                    new EmergencyRescueMineralCost("mineral.iron", "철", 1500, 1200)
                });
        }

        [TearDown]
        public void TearDown()
        {
            if (view != null)
            {
                Object.DestroyImmediate(view.gameObject);
            }

            Object.DestroyImmediate(canvasObject);
        }

        // ---------- 시간표 ----------

        [Test]
        public void Intro_BurstsTwoOrThreeTimes_ThenSettlesClean()
        {
            var bursts = 0;
            var inBurst = false;
            for (var t = 0f; t <= EmergencyRescueTimeline.IntroDuration; t += 0.005f)
            {
                var high = EmergencyRescueTimeline.Intro(t).Intensity > 0.45f;
                if (high && !inBurst)
                {
                    bursts++;
                }

                inBurst = high;
            }

            Assert.That(bursts, Is.InRange(2, 3));
            Assert.That(EmergencyRescueTimeline.IntroDuration, Is.InRange(0.4f, 0.6f));
            var end = EmergencyRescueTimeline.Intro(EmergencyRescueTimeline.IntroDuration);
            Assert.That(end.Intensity, Is.Zero);
            Assert.That(end.VisualAlpha, Is.EqualTo(1f));
            Assert.That(end.ContentAlpha, Is.EqualTo(1f));
            Assert.That(end.BackdropAlpha, Is.EqualTo(1f));
        }

        [Test]
        public void Intro_UsesBothTealAndRedBursts()
        {
            var seenTeal = false;
            var seenRed = false;
            for (var t = 0f; t <= EmergencyRescueTimeline.IntroDuration; t += 0.01f)
            {
                var frame = EmergencyRescueTimeline.Intro(t);
                if (frame.Intensity < 0.4f)
                {
                    continue;
                }

                seenTeal |= frame.Tint == 0;
                seenRed |= frame.Tint == 1;
            }

            Assert.That(seenTeal && seenRed, Is.True);
        }

        [Test]
        public void Outro_IsShortStrongerThanIntroAndEndsInvisible()
        {
            Assert.That(EmergencyRescueTimeline.OutroDuration, Is.InRange(0.2f, 0.35f));
            var introPeak = 0f;
            var outroPeak = 0f;
            for (var t = 0f; t <= 0.5f; t += 0.005f)
            {
                introPeak = Mathf.Max(introPeak, EmergencyRescueTimeline.Intro(t).Intensity);
            }

            for (var t = 0f; t < EmergencyRescueTimeline.OutroDuration; t += 0.005f)
            {
                outroPeak = Mathf.Max(outroPeak, EmergencyRescueTimeline.Outro(t).Intensity);
            }

            Assert.That(outroPeak, Is.GreaterThan(introPeak - 0.01f));
            Assert.That(EmergencyRescueTimeline.Outro(0.01f).Intensity, Is.GreaterThan(0.6f));
            var end = EmergencyRescueTimeline.Outro(EmergencyRescueTimeline.OutroDuration);
            Assert.That(end.VisualAlpha, Is.Zero);
            Assert.That(end.Intensity, Is.Zero);
            Assert.That(end.BackdropAlpha, Is.Zero);
        }

        [Test]
        public void Idle_IsIntermittentAndWeakerThanTransitions()
        {
            var active = 0;
            var total = 0;
            var peak = 0f;
            for (var t = 0f; t < 60f; t += 0.02f)
            {
                var value = EmergencyRescueTimeline.Idle(t, out _);
                total++;
                if (value > 0f)
                {
                    active++;
                }

                peak = Mathf.Max(peak, value);
            }

            Assert.That(active, Is.GreaterThan(0));
            Assert.That(active / (float)total, Is.LessThan(0.15f), "표시 중 글리치는 간헐적이어야 한다");
            Assert.That(peak, Is.LessThanOrEqualTo(0.8f));
        }

        [Test]
        public void Hover_ArrowLagsHuman_BouncesOnceAndSettles_AndIsStateless()
        {
            EmergencyRescueHoverFrame rest = EmergencyRescueTimeline.Hover(0f);
            EmergencyRescueHoverFrame full = EmergencyRescueTimeline.Hover(1f);
            Assert.That(rest.Glow + rest.Human + rest.Arrow, Is.Zero);
            Assert.That(full.Glow, Is.EqualTo(1f).Within(0.001f));
            Assert.That(full.Human, Is.EqualTo(1f).Within(0.001f));
            Assert.That(full.Arrow, Is.EqualTo(1f).Within(0.001f));

            // 청록빛이 먼저, 사람, 화살표 순으로 시작한다.
            Assert.That(EmergencyRescueTimeline.Hover(0.12f).Glow, Is.GreaterThan(0.2f));
            Assert.That(EmergencyRescueTimeline.Hover(0.12f).Human, Is.Zero);
            Assert.That(EmergencyRescueTimeline.Hover(0.2f).Human, Is.GreaterThan(EmergencyRescueTimeline.Hover(0.2f).Arrow - 0.001f));

            var peak = 0f;
            var overshootFrames = 0;
            for (var level = 0f; level <= 1f; level += 0.01f)
            {
                var arrow = EmergencyRescueTimeline.Hover(level).Arrow;
                peak = Mathf.Max(peak, arrow);
                if (arrow > 1.005f)
                {
                    overshootFrames++;
                }
            }

            Assert.That(peak, Is.InRange(1.05f, 1.25f), "화살표만 작게 튄다");
            Assert.That(overshootFrames, Is.GreaterThan(0));
            Assert.That(EmergencyRescueTimeline.Hover(0.5f).Human, Is.LessThanOrEqualTo(1f), "사람은 넘치지 않는다");

            // 레벨만으로 값이 정해지므로 같은 레벨은 항상 같은 값이다(이동값이 누적되지 않는다).
            Assert.That(EmergencyRescueTimeline.Hover(0.37f).Arrow, Is.EqualTo(EmergencyRescueTimeline.Hover(0.37f).Arrow));
            var total = EmergencyRescueTimeline.HoverRiseSeconds;
            Assert.That(total, Is.InRange(0.4f, 0.6f));
        }

        [Test]
        public void KeycapPulse_IsShortAndSpacedThreeToFiveSeconds()
        {
            Assert.That(EmergencyRescueTimeline.KeycapPulseDuration, Is.InRange(0.3f, 0.4f));
            for (var cycle = 0; cycle < 50; cycle++)
            {
                Assert.That(EmergencyRescueTimeline.KeycapInterval(cycle), Is.InRange(3f, 5f));
            }

            var peak = 0f;
            for (var t = 0f; t <= EmergencyRescueTimeline.KeycapPulseDuration; t += 0.005f)
            {
                peak = Mathf.Max(peak, EmergencyRescueTimeline.KeycapPulse(t, EmergencyRescueTimeline.KeycapPulseDuration));
            }

            Assert.That(peak, Is.InRange(0.95f, 1f));
            Assert.That(EmergencyRescueTimeline.KeycapPulse(0f, 0.34f), Is.Zero);
            Assert.That(EmergencyRescueTimeline.KeycapPulse(0.34f, 0.34f), Is.Zero);
        }

        // ---------- 비용 표 ----------

        [Test]
        public void CostRows_CopyExistingCalculation_WithUnitsAndSeparators()
        {
            var rows = EmergencyRescueCostRows.Build(sampleCost);
            Assert.That(rows.Count, Is.EqualTo(3));
            Assert.That(rows[0].IsGold, Is.True);
            Assert.That(rows[0].Deduct, Is.EqualTo("-250G"));
            Assert.That(rows[0].Before, Is.EqualTo("1,240G"));
            Assert.That(rows[0].After, Is.EqualTo("990G"));
            Assert.That(rows[2].Name, Is.EqualTo("철"));
            Assert.That(rows[2].Deduct, Is.EqualTo("-1,200개"));
            Assert.That(rows[2].Before, Is.EqualTo("1,500개"));
            Assert.That(rows[2].After, Is.EqualTo("300개"));
        }

        [Test]
        public void CostRows_FreeCost_HasNoRows_AndAreSameDetectsChanges()
        {
            var free = new EmergencyRescueCost(0, 0, null);
            Assert.That(EmergencyRescueCostRows.Build(free), Is.Empty);
            Assert.That(EmergencyRescueCostRows.BuildFooter(free), Is.EqualTo(EmergencyRescueCostRows.FreeMessage));

            var same = new EmergencyRescueCost(
                sampleCost.GoldBefore,
                sampleCost.GoldCharged,
                new[]
                {
                    new EmergencyRescueMineralCost("mineral.copper", "구리", 10, 8),
                    new EmergencyRescueMineralCost("mineral.iron", "철", 1500, 1200)
                });
            var changed = new EmergencyRescueCost(
                sampleCost.GoldBefore,
                sampleCost.GoldCharged,
                new[]
                {
                    new EmergencyRescueMineralCost("mineral.copper", "구리", 11, 9),
                    new EmergencyRescueMineralCost("mineral.iron", "철", 1500, 1200)
                });
            Assert.That(EmergencyRescueCostRows.AreSame(sampleCost, same), Is.True);
            Assert.That(EmergencyRescueCostRows.AreSame(sampleCost, changed), Is.False);
            Assert.That(EmergencyRescueCostRows.AreSame(sampleCost, null), Is.False);
        }

        [Test]
        public void CostTable_AllRowsShareColumnPositions()
        {
            view.Show(sampleCost, null);
            Transform table = view.transform.Find("Card/Content/CostTable");
            Assert.That(table, Is.Not.Null);
            Assert.That(view.Popup.VisibleRowCount, Is.EqualTo(3));

            foreach (var column in new[] { "Name", "Deduct", "Before", "Arrow", "After" })
            {
                var x0 = Cell(table, 0, column).anchoredPosition.x;
                for (var row = 1; row < 3; row++)
                {
                    Assert.That(Cell(table, row, column).anchoredPosition.x, Is.EqualTo(x0), column);
                    Assert.That(Cell(table, row, column).sizeDelta.x, Is.EqualTo(Cell(table, 0, column).sizeDelta.x), column);
                }
            }

            foreach (var numeric in new[] { "Deduct", "Before", "After" })
            {
                for (var row = 0; row < 3; row++)
                {
                    var text = Cell(table, row, numeric).GetComponent<TMP_Text>();
                    Assert.That(text.alignment, Is.EqualTo(TextAlignmentOptions.MidlineRight), numeric);
                }
            }

            // 열 순서: 이름 < 차감량 < 보유 < 화살표 < 잔량.
            var order = new[] { "Name", "Deduct", "Before", "Arrow", "After" };
            for (var i = 1; i < order.Length; i++)
            {
                Assert.That(
                    Cell(table, 0, order[i]).anchoredPosition.x,
                    Is.GreaterThan(Cell(table, 0, order[i - 1]).anchoredPosition.x));
            }

            Assert.That(Cell(table, 0, "Arrow").GetComponent<TMP_Text>().text, Is.EqualTo("→"));
        }

        [Test]
        public void CostTable_ManyRowsShrinkToFit_AndFreeShowsNote()
        {
            var many = new EmergencyRescueMineralCost[8];
            for (var i = 0; i < many.Length; i++)
            {
                many[i] = new EmergencyRescueMineralCost("mineral.m" + i, "광물" + i, 100 + i, 80 + i);
            }

            view.Show(new EmergencyRescueCost(500, 250, many), null);
            Assert.That(view.Popup.VisibleRowCount, Is.EqualTo(9));
            Transform table = view.transform.Find("Card/Content/CostTable");
            var first = (RectTransform)table.Find("Row0");
            var last = (RectTransform)table.Find("Row8");
            Assert.That(first.sizeDelta.y, Is.LessThan(40f));
            Assert.That(last.anchoredPosition.y - last.sizeDelta.y * 0.5f, Is.GreaterThanOrEqualTo(-144f), "표 영역 밖으로 넘치지 않는다");
            view.HideImmediate();

            view.Show(new EmergencyRescueCost(0, 0, null), null);
            Assert.That(view.Popup.VisibleRowCount, Is.Zero);
            Transform note = view.transform.Find("Card/Content/CostTable/EmptyNote");
            Assert.That(note.gameObject.activeSelf, Is.True);
            Assert.That(note.GetComponent<TMP_Text>().text, Does.Contain("무료"));
        }

        // ---------- 팝업 상태기계 ----------

        [Test]
        public void Popup_OpensWithIntro_ThenAcceptsInput()
        {
            Assert.That(view.IsPopupVisible, Is.False);
            Assert.That(view.Show(sampleCost, null), Is.True);
            EmergencyRescuePopupView popup = view.Popup;
            Assert.That(popup.State, Is.EqualTo(EmergencyRescuePopupView.PopupState.Opening));
            Assert.That(view.IsOpen, Is.True);
            Assert.That(popup.RescueButton.interactable, Is.False, "등장 중에는 확정할 수 없다");
            Assert.That(popup.CloseButton.interactable, Is.False);

            popup.Tick(0.1f);
            Assert.That(popup.State, Is.EqualTo(EmergencyRescuePopupView.PopupState.Opening));
            popup.Tick(EmergencyRescueTimeline.IntroDuration);
            Assert.That(popup.State, Is.EqualTo(EmergencyRescuePopupView.PopupState.Open));
            Assert.That(popup.RescueButton.interactable, Is.True);
            Assert.That(popup.CloseButton.interactable, Is.True);
            Assert.That(popup.ContentAlpha, Is.EqualTo(1f));
            Assert.That(popup.VisualAlpha, Is.EqualTo(1f));
            Assert.That(popup.VisualOffset, Is.EqualTo(Vector2.zero));
            Assert.That(popup.AnyGlitchPieceVisible, Is.False, "정착 뒤에는 글리치 조각이 남지 않는다");
        }

        [Test]
        public void Popup_ShowWhileOpen_RefreshesContentWithoutRestartingIntro()
        {
            view.Show(sampleCost, null);
            view.Popup.Tick(1f);
            Assert.That(view.IsStable, Is.True);

            var cheaper = new EmergencyRescueCost(100, 100, null);
            Assert.That(view.Show(cheaper, "화물 상태가 변경되었습니다."), Is.True);
            Assert.That(view.Popup.State, Is.EqualTo(EmergencyRescuePopupView.PopupState.Open));
            Assert.That(view.Popup.VisibleRowCount, Is.EqualTo(1));
            Assert.That(view.Popup.MessageString, Does.Contain("변경"));
            Assert.That(view.DisplayedCost, Is.SameAs(cheaper));
        }

        [Test]
        public void Popup_Close_PlaysStrongGlitchThenCallsBackOnceAndLeavesNothing()
        {
            view.Show(sampleCost, null);
            view.Popup.Tick(1f);

            var closed = 0;
            Assert.That(view.BeginClose(() => closed++), Is.True);
            Assert.That(view.IsClosing, Is.True);
            Assert.That(view.IsOpen, Is.False);
            Assert.That(view.BeginClose(() => closed += 100), Is.False, "닫는 중 중복 닫기는 무시한다");
            Assert.That(view.Popup.RescueButton.interactable, Is.False);
            Assert.That(view.Popup.AnyGlitchPieceVisible, Is.True);

            view.Popup.Tick(0.1f);
            Assert.That(closed, Is.Zero);
            view.Popup.Tick(EmergencyRescueTimeline.OutroDuration);
            Assert.That(closed, Is.EqualTo(1));
            Assert.That(view.IsPopupVisible, Is.False);
            Assert.That(view.gameObject.activeSelf, Is.False, "투명 패널이 입력을 막지 않도록 루트가 꺼진다");
        }

        [Test]
        public void Popup_ShowDuringClosing_IsRejectedAndReopenWorksAfter()
        {
            view.Show(sampleCost, null);
            view.Popup.Tick(1f);
            view.BeginClose(null);
            view.Popup.Tick(0.05f);

            Assert.That(view.Show(sampleCost, null), Is.False);
            Assert.That(view.Popup.State, Is.EqualTo(EmergencyRescuePopupView.PopupState.Closing));

            view.Popup.Tick(1f);
            Assert.That(view.IsPopupVisible, Is.False);
            Assert.That(view.Show(sampleCost, null), Is.True);
            Assert.That(view.Popup.State, Is.EqualTo(EmergencyRescuePopupView.PopupState.Opening));
            Assert.That(view.Popup.Clock, Is.Zero, "다시 열면 같은 등장 연출을 처음부터 재생한다");
        }

        [Test]
        public void Popup_HideImmediate_DropsPendingCloseCallback()
        {
            view.Show(sampleCost, null);
            view.Popup.Tick(1f);
            var called = false;
            view.BeginClose(() => called = true);
            view.HideImmediate();

            Assert.That(view.IsPopupVisible, Is.False);
            Assert.That(view.gameObject.activeSelf, Is.False);
            view.Popup.Tick(1f);
            Assert.That(called, Is.False);
        }

        [Test]
        public void Popup_ExternalInteractable_TogglesRescueButtonOnlyWhenStable()
        {
            view.Show(sampleCost, null);
            view.Popup.Tick(1f);
            view.SetInteractable(false);
            Assert.That(view.Popup.RescueButton.interactable, Is.False);
            view.SetInteractable(true);
            Assert.That(view.Popup.RescueButton.interactable, Is.True);
        }

        [Test]
        public void Popup_Layout_UsesWarningTitleAndTwoButtons()
        {
            Transform content = view.transform.Find("Card/Content");
            Assert.That(content.Find("Title").GetComponent<TMP_Text>().text, Is.EqualTo("전력이 바닥났습니다"));
            Assert.That(content.Find("RescueButton/Label").GetComponent<TMP_Text>().text, Is.EqualTo("구출 요청"));
            Assert.That(content.Find("CloseButton/Label").GetComponent<TMP_Text>().text, Is.EqualTo("닫기"));
            var rescue = (RectTransform)content.Find("RescueButton");
            var close = (RectTransform)content.Find("CloseButton");
            Assert.That(rescue.sizeDelta.x, Is.GreaterThan(close.sizeDelta.x), "구출 요청이 더 강조된다");
        }

        // ---------- 머리 위 홀로그램 ----------

        [Test]
        public void Chip_PartsAreSeparateAnimatableObjects()
        {
            Transform chip = canvasObject.transform.Find("EmergencyRescueChip");
            Assert.That(chip.Find("Content/Label").GetComponent<TMP_Text>().text, Is.EqualTo("구출 요청"));
            Assert.That(chip.Find("Content/Pictogram/Human"), Is.Not.Null);
            Assert.That(chip.Find("Content/Pictogram/Arrow"), Is.Not.Null);
            Assert.That(chip.Find("Content/Pictogram/PictoGlow"), Is.Not.Null);
            Assert.That(chip.Find("Content/InputHint/KeycapBody/KeyLabel").GetComponent<TMP_Text>().text, Is.EqualTo("R"));
            Assert.That(chip.Find("Content/InputHint/KeycapGlow"), Is.Not.Null);

            // 클릭 영역은 루트 하나이고 장식은 입력을 받지 않는다.
            var graphics = chip.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                if (graphic.gameObject == chip.gameObject)
                {
                    Assert.That(graphic.raycastTarget, Is.True);
                }
                else
                {
                    Assert.That(graphic.raycastTarget, Is.False, graphic.name);
                }
            }

            var size = ((RectTransform)chip).sizeDelta;
            Assert.That(size, Is.EqualTo(EmergencyRescueChipView.Size));
        }

        [Test]
        public void Chip_ShowIsIdempotent_AndHideClearsEveryEffect()
        {
            EmergencyRescueChipView chip = view.Chip;
            view.SetChipVisible(true);
            chip.Tick(0.1f);
            var level = chip.ShowLevel;
            view.SetChipVisible(true);
            Assert.That(chip.ShowLevel, Is.EqualTo(level), "반복 호출이 등장 연출을 다시 시작하지 않는다");

            chip.Tick(1f);
            chip.OnPointerEnter(new PointerEventData(null));
            chip.Tick(1f);
            Assert.That(chip.HoverLevel, Is.EqualTo(1f));

            view.SetChipVisible(false);
            Assert.That(view.IsChipVisible, Is.False);
            Assert.That(chip.gameObject.activeSelf, Is.False);
            Assert.That(chip.HoverLevel, Is.Zero);
            Assert.That(chip.IsKeycapPulsing, Is.False);
        }

        [Test]
        public void Chip_Hover_RisesOnceHoldsAndReturns_WithoutAccumulating()
        {
            EmergencyRescueChipView chip = view.Chip;
            view.SetChipVisible(true);
            chip.Tick(1f);
            var humanRest = chip.HumanRect.anchoredPosition.y;
            var arrowRest = chip.ArrowRect.anchoredPosition.y;
            var titleBefore = chip.TitleLabel.rectTransform.anchoredPosition;
            var keyBefore = chip.KeycapRect.anchoredPosition;

            // 빠르게 들어왔다 나가기를 반복해도 이동값이 기준 범위를 벗어나지 않는다.
            for (var i = 0; i < 30; i++)
            {
                chip.OnPointerEnter(new PointerEventData(null));
                chip.Tick(0.03f);
                chip.OnPointerExit(new PointerEventData(null));
                chip.Tick(0.02f);
                Assert.That(chip.HumanRect.anchoredPosition.y, Is.InRange(humanRest, humanRest + EmergencyRescueChipView.HumanRise + 0.01f));
                Assert.That(
                    chip.ArrowRect.anchoredPosition.y,
                    Is.InRange(arrowRest, arrowRest + EmergencyRescueChipView.ArrowRise * 1.25f));
            }

            chip.OnPointerEnter(new PointerEventData(null));
            chip.Tick(1f);
            Assert.That(chip.HumanRect.anchoredPosition.y, Is.EqualTo(humanRest + EmergencyRescueChipView.HumanRise).Within(0.01f));
            Assert.That(chip.ArrowRect.anchoredPosition.y, Is.EqualTo(arrowRest + EmergencyRescueChipView.ArrowRise).Within(0.01f));
            var held = chip.HumanRect.anchoredPosition.y;
            chip.Tick(1f);
            Assert.That(chip.HumanRect.anchoredPosition.y, Is.EqualTo(held), "호버 중에는 상승을 반복하지 않는다");
            Assert.That(chip.PictoGlowAlpha, Is.GreaterThan(0.5f));

            chip.OnPointerExit(new PointerEventData(null));
            chip.Tick(1f);
            Assert.That(chip.HumanRect.anchoredPosition.y, Is.EqualTo(humanRest).Within(0.01f));
            Assert.That(chip.ArrowRect.anchoredPosition.y, Is.EqualTo(arrowRest).Within(0.01f));
            Assert.That(chip.TitleLabel.rectTransform.anchoredPosition, Is.EqualTo(titleBefore), "문구는 고정");
            Assert.That(chip.KeycapRect.anchoredPosition, Is.EqualTo(keyBefore));
        }

        [Test]
        public void Chip_KeycapPulse_RunsOnItsOwn_WithoutClickingOrMovingPictogram()
        {
            EmergencyRescueChipView chip = view.Chip;
            var clicks = 0;
            view.Bind(null, null, () => clicks++);
            view.SetChipVisible(true);
            chip.Tick(1f);

            var pulsed = false;
            var minScale = 1f;
            var humanRest = chip.HumanRect.anchoredPosition;
            for (var t = 0f; t < 6f; t += 0.02f)
            {
                chip.Tick(0.02f);
                if (chip.IsKeycapPulsing)
                {
                    pulsed = true;
                    minScale = Mathf.Min(minScale, chip.KeycapRect.localScale.x);
                    Assert.That(chip.KeycapRect.anchoredPosition.y, Is.LessThanOrEqualTo(0f));
                }
            }

            Assert.That(pulsed, Is.True);
            Assert.That(minScale, Is.LessThan(1f));
            Assert.That(clicks, Is.Zero, "키캡 안내 연출은 입력이나 구출 요청을 만들지 않는다");
            Assert.That(chip.HumanRect.anchoredPosition, Is.EqualTo(humanRest), "호버 값과 분리되어 있다");
        }

        [Test]
        public void Chip_DismissWithPress_BlocksInputAtOnceAndHidesShortly()
        {
            EmergencyRescueChipView chip = view.Chip;
            view.SetChipVisible(true);
            chip.Tick(1f);
            chip.OnPointerEnter(new PointerEventData(null));
            chip.Tick(0.2f);

            view.DismissChip(true);
            Assert.That(view.IsChipVisible, Is.False, "논리적으로는 즉시 숨김 상태");
            Assert.That(chip.IsDismissing, Is.True);
            Assert.That(chip.IsPressFeedbackPlaying, Is.True);
            Assert.That(chip.IsHovered, Is.False);
            Assert.That(chip.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);

            chip.Tick(0.08f);
            Assert.That(chip.KeycapRect.anchoredPosition.y, Is.LessThan(-EmergencyRescueChipView.KeycapIdleDrop), "실제 입력은 반복 안내보다 깊게 눌린다");
            chip.Tick(0.2f);
            Assert.That(chip.gameObject.activeSelf, Is.False);
            Assert.That(chip.GetComponent<CanvasGroup>().blocksRaycasts, Is.True, "다음 표시를 위해 상태가 복원된다");
            Assert.That(chip.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
        }

        [Test]
        public void Chip_ShowAfterDismiss_StartsFreshUnfold()
        {
            EmergencyRescueChipView chip = view.Chip;
            view.SetChipVisible(true);
            chip.Tick(1f);
            view.DismissChip(false);
            chip.Tick(0.05f);
            view.SetChipVisible(true);
            Assert.That(chip.IsShown, Is.True);
            Assert.That(chip.IsDismissing, Is.False);
            Assert.That(chip.ShowLevel, Is.Zero);
            Assert.That(chip.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
        }

        [Test]
        public void ChipView_InputHint_CanBeSwappedWithoutTouchingInputHandling()
        {
            view.Chip.SetInputHint("A");
            Assert.That(view.Chip.KeycapLabel.text, Is.EqualTo("A"));
            view.Chip.SetInputHint(null);
            Assert.That(view.Chip.KeycapLabel.text, Is.EqualTo("R"));
        }

        private static RectTransform Cell(Transform table, int row, string column)
        {
            return (RectTransform)table.Find("Row" + row + "/" + column);
        }
    }
}
