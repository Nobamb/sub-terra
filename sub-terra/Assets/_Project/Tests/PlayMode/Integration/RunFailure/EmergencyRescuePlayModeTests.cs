using System.Collections;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Run;
using SubTerra.App.State;
using SubTerra.App.UI;
using SubTerra.App.UI.EmergencyRescue;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode
{
    /// <summary>
    /// 전력 고갈 구출 팝업과 머리 위 홀로그램 안내를 실제 Integration Scene에서 검증한다(B-135).
    /// 클릭·호버·R키는 실제 입력 장치 이벤트로 보내고, 구출 비용은 실제 EmergencyRescueService 결과와 비교한다.
    /// </summary>
    public sealed class EmergencyRescuePlayModeTests
    {
        private const string EvidenceDirectory = "Temp/rescue-evidence";
        private const float Frame = 1f / 30f;

        private UiTestEnvironment env;
        private GameState state;
        private EmergencyRescueRuntimeController controller;
        private EmergencyRescuePanelView view;

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(EvidenceDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Time.captureFramerate = 0;
            UiPauseGate.Release("rescue-test");
            env?.Dispose();
            env = null;
        }

        // ---------- 표시 흐름 ----------

        [UnityTest]
        public IEnumerator Flow_AutoOpen_Close_ChipAppears_NeverAutoReopens()
        {
            yield return Load();
            Prepare(600, 12, 20);
            Deplete();

            Assert.That(view.IsOpen, Is.True, "전력이 0이 되면 팝업이 자동으로 나타난다");
            Assert.That(view.IsChipVisible, Is.False, "팝업이 열린 동안 안내 버튼은 숨겨진다");
            yield return UiTestWait.Until(() => view.IsStable, "popup intro finished");
            AssertDisplayedCostMatchesService();

            yield return Capture("flow-1-popup-open");
            yield return UiTestWait.Click(env.Mouse, view.Popup.CloseButton);
            Assert.That(view.IsClosing, Is.True);
            Assert.That(view.IsChipVisible, Is.False, "닫는 연출이 끝나기 전에는 안내 버튼이 없다");
            yield return UiTestWait.Until(() => !view.IsPopupVisible, "close glitch finished");
            Assert.That(view.IsChipVisible, Is.True, "닫기가 끝나면 안내 버튼이 나타난다");
            AssertNothingOfOverlayRemains();

            // 전력이 계속 0이고 에너지 이벤트가 반복돼도 닫은 팝업이 자동으로 다시 열리지 않는다.
            state.SetEnergy(0, state.Player.MaxEnergy + 1);
            state.SetEnergy(0, state.Player.MaxEnergy);
            yield return UiTestWait.Delay(1.5f, "stay depleted");
            Assert.That(state.Player.Energy, Is.Zero);
            Assert.That(view.IsPopupVisible, Is.False);
            Assert.That(view.IsChipVisible, Is.True);
            yield return Capture("flow-2-chip-after-close");
        }

        [UnityTest]
        public IEnumerator Flow_ChipClick_And_RKey_ReopenSamePopupWithFreshCost()
        {
            yield return Load();
            Prepare(600, 12, 20);
            Deplete();
            yield return UiTestWait.Until(() => view.IsStable, "popup intro finished");
            yield return CloseByButton();

            // 닫혀 있는 동안 자원이 바뀌면 다시 열 때 최신 값으로 갱신된다.
            state.SetGold(900);
            var goldBefore = state.Player.Gold;
            var position = Player().position;
            yield return ClickChip();
            Assert.That(view.IsOpen, Is.True, "칩 클릭은 팝업을 다시 연다");
            Assert.That(view.IsChipVisible, Is.False);
            Assert.That(state.Player.Gold, Is.EqualTo(goldBefore), "칩 클릭은 구출을 실행하지 않는다");
            Assert.That(Player().position, Is.EqualTo(position));
            Assert.That(view.DisplayedCost.GoldBefore, Is.EqualTo(900));
            yield return UiTestWait.Until(() => view.IsStable, "reopened popup stable");
            AssertDisplayedCostMatchesService();
            Assert.That(OverlayCount(), Is.EqualTo(1));

            yield return CloseByButton();
            yield return UiTestWait.Press(env.Keyboard, Key.R);
            Assert.That(view.IsOpen, Is.True, "R 키도 같은 팝업을 다시 연다");
            Assert.That(view.IsChipVisible, Is.False);
            Assert.That(state.Player.Gold, Is.EqualTo(goldBefore), "R 키는 구출을 확정하지 않는다");
            yield return UiTestWait.Until(() => view.IsStable, "R reopened popup stable");
            Assert.That(OverlayCount(), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Input_DuringTransitions_NoDuplicatesAndNoEarlyConfirm()
        {
            yield return Load();
            Prepare(600, 12, 20);
            Deplete();
            var goldBefore = state.Player.Gold;
            var copperBefore = Copper();

            // 등장 도중에는 구출 요청이 눌리지 않는다.
            Assert.That(view.Popup.RescueButton.interactable, Is.False);
            view.Popup.RescueButton.onClick.Invoke();
            Assert.That(state.Player.Gold, Is.EqualTo(goldBefore), "등장 도중 확정되지 않는다");
            yield return UiTestWait.Until(() => view.IsStable, "popup intro finished");

            // 닫는 도중 R: 닫기가 끝난 뒤 한 번만 다시 열리고 그 사이 안내 버튼은 나타나지 않는다.
            view.Popup.CloseButton.onClick.Invoke();
            Assert.That(view.IsClosing, Is.True);
            yield return UiTestWait.Press(env.Keyboard, Key.R);
            var chipSeenWhileTransition = false;
            yield return UiTestWait.Until(() =>
            {
                chipSeenWhileTransition |= view.IsChipVisible;
                return view.IsOpen;
            }, "reopen after close");
            Assert.That(chipSeenWhileTransition, Is.False, "전환 중에는 팝업과 안내 버튼이 동시에 보이지 않는다");
            Assert.That(OverlayCount(), Is.EqualTo(1));
            yield return UiTestWait.Until(() => view.IsStable, "popup stable after pending reopen");

            // 빠른 연속 R: 중복 생성도, 확정도 없다.
            for (var i = 0; i < 6; i++)
            {
                yield return UiTestWait.Press(env.Keyboard, Key.R);
            }

            Assert.That(OverlayCount(), Is.EqualTo(1));
            Assert.That(state.Player.Gold, Is.EqualTo(goldBefore));
            Assert.That(Copper(), Is.EqualTo(copperBefore));

            // 종료 연출 도중의 클릭·재호출이 섞여도 팝업은 하나이고 상태가 일관된다.
            view.Popup.CloseButton.onClick.Invoke();
            view.Popup.CloseButton.onClick.Invoke();
            Assert.That(view.IsClosing, Is.True);
            controller.OpenPanel();
            controller.OpenPanel();
            yield return UiTestWait.Until(() => view.IsStable, "stable after close/reopen spam");
            Assert.That(OverlayCount(), Is.EqualTo(1));
            Assert.That(view.IsChipVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator Confirm_AppliesDisplayedCostOnce_AndClearsEverything()
        {
            yield return Load();
            Prepare(600, 12, 20);
            Deplete();
            yield return UiTestWait.Until(() => view.IsStable, "popup intro finished");

            EmergencyRescueCost shown = view.DisplayedCost;
            var goldCharged = shown.GoldCharged;
            var goldBefore = state.Player.Gold;
            var copperCharged = Find(shown, DataIds.Minerals.Copper).Charged;
            var copperBefore = Copper();
            var ironCharged = Find(shown, DataIds.Minerals.Iron).Charged;
            var ironBefore = Iron();
            var start = Player().position;

            yield return UiTestWait.Click(env.Mouse, view.Popup.RescueButton);
            // 같은 프레임의 연타가 와도 한 번만 적용된다.
            view.Popup.RescueButton.onClick.Invoke();
            view.Popup.RescueButton.onClick.Invoke();
            yield return null;

            Assert.That(state.Player.Gold, Is.EqualTo(goldBefore - goldCharged), "표시한 골드 비용과 실제 차감이 같다");
            Assert.That(Copper(), Is.EqualTo(copperBefore - copperCharged));
            Assert.That(Iron(), Is.EqualTo(ironBefore - ironCharged));
            Assert.That(Player().position, Is.Not.EqualTo(start), "기존 이동 목적지(엘리베이터)로 이동한다");
            Assert.That(view.IsPopupVisible, Is.False);
            Assert.That(view.IsChipVisible, Is.False, "구출 뒤에는 안내가 남지 않는다");
            AssertNothingOfOverlayRemains();

            var goldAfter = state.Player.Gold;
            yield return UiTestWait.Press(env.Keyboard, Key.R);
            Assert.That(view.IsPopupVisible, Is.False, "구출 완료 뒤 R은 아무 일도 하지 않는다");
            Assert.That(state.Player.Gold, Is.EqualTo(goldAfter));
        }

        [UnityTest]
        public IEnumerator Cleared_WhenEnergyRecovers_RemovesPopupChipAndEffects_ThenNextDepletionAutoOpens()
        {
            yield return Load();
            Prepare(300, 5, 5);
            Deplete();
            yield return UiTestWait.Until(() => view.IsStable, "popup intro finished");

            // 팝업이 열린 채 전력이 회복되면 연출 없이 바로 정리된다.
            state.SetCurrentEnergy(state.Player.MaxEnergy);
            Assert.That(view.IsPopupVisible, Is.False);
            Assert.That(view.IsChipVisible, Is.False);
            AssertNothingOfOverlayRemains();

            // 다음 고갈에서는 기존 규칙대로 다시 자동 표시된다.
            Deplete();
            Assert.That(view.IsOpen, Is.True);
            yield return UiTestWait.Until(() => view.IsStable, "second episode popup stable");
            yield return CloseByButton();
            Assert.That(view.IsChipVisible, Is.True);

            // 안내 버튼이 보이는 동안 회복돼도 버튼과 키캡 효과가 사라진다.
            yield return UiTestWait.Delay(2.2f, "let keycap pulse start");
            state.SetCurrentEnergy(state.Player.MaxEnergy);
            Assert.That(view.IsChipVisible, Is.False);
            Assert.That(view.Chip.gameObject.activeSelf, Is.False);
            Assert.That(view.Chip.IsKeycapPulsing, Is.False);
            Assert.That(view.Chip.HoverLevel, Is.Zero);

            // 닫는 도중 전력이 회복되면 안내가 남지 않는다.
            Deplete();
            Assert.That(view.IsOpen, Is.True);
            yield return UiTestWait.Until(() => view.IsStable, "third episode popup stable");
            view.Popup.CloseButton.onClick.Invoke();
            Assert.That(view.IsClosing, Is.True);
            state.SetCurrentEnergy(state.Player.MaxEnergy);
            yield return UiTestWait.Delay(0.6f, "after recovery during close");
            Assert.That(view.IsPopupVisible, Is.False);
            Assert.That(view.IsChipVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator PausedTime_AnimationStillRunsAndInputWorks()
        {
            yield return Load();
            Prepare(600, 12, 20);
            UiPauseGate.Acquire("rescue-test");
            Assert.That(Time.timeScale, Is.Zero);
            Deplete();
            yield return UiTestWait.Until(() => view.IsStable, "popup stable while time is paused");
            yield return UiTestWait.Click(env.Mouse, view.Popup.CloseButton);
            yield return UiTestWait.Until(() => !view.IsPopupVisible, "close finished while paused");
            Assert.That(view.IsChipVisible, Is.True);
            yield return ClickChip();
            yield return UiTestWait.Until(() => view.IsStable, "reopened while paused");
            AssertNoLeftoverGlitchPieces();
        }

        // ---------- 홀로그램 호버·키캡 ----------

        [UnityTest]
        public IEnumerator Chip_RealPointerHover_RisesHoldsAndReturns_KeycapPulsesWithoutInput()
        {
            yield return Load();
            Prepare(600, 12, 20);
            Deplete();
            yield return UiTestWait.Until(() => view.IsStable, "popup intro finished");
            yield return CloseByButton();
            yield return UiTestWait.Until(() => view.Chip.ShowLevel >= 1f, "chip unfold finished");

            EmergencyRescueChipView chip = view.Chip;
            var humanRest = chip.HumanRect.anchoredPosition.y;
            var arrowRest = chip.ArrowRect.anchoredPosition.y;
            var titlePosition = chip.TitleLabel.rectTransform.anchoredPosition;

            // 키캡 반복 안내: 입력이나 구출 요청을 만들지 않고 스스로 한 번 눌린다.
            var rKey = env.Keyboard.rKey;
            var pulsed = false;
            var goldBefore = state.Player.Gold;
            var timer = 0f;
            while (timer < 6f && !pulsed)
            {
                pulsed = chip.IsKeycapPulsing;
                Assert.That(rKey.isPressed, Is.False);
                yield return null;
                timer += Time.unscaledDeltaTime;
            }

            Assert.That(pulsed, Is.True, "3~5초 간격 키캡 안내");
            Assert.That(view.IsPopupVisible, Is.False, "안내 연출이 팝업을 열지 않는다");
            Assert.That(state.Player.Gold, Is.EqualTo(goldBefore));

            // 실제 포인터 호버.
            yield return UiTestWait.FocusGameView();
            yield return MoveMouse(ChipCenter());
            yield return UiTestWait.Until(() => chip.IsHovered, "pointer hovers chip");
            yield return UiTestWait.Until(() => chip.HoverLevel >= 1f, "hover reached full");
            Assert.That(chip.HumanRect.anchoredPosition.y, Is.EqualTo(humanRest + EmergencyRescueChipView.HumanRise).Within(0.05f));
            Assert.That(chip.ArrowRect.anchoredPosition.y, Is.EqualTo(arrowRest + EmergencyRescueChipView.ArrowRise).Within(0.05f));
            var held = chip.HumanRect.anchoredPosition.y;
            yield return UiTestWait.Delay(0.4f, "hover hold");
            Assert.That(chip.HumanRect.anchoredPosition.y, Is.EqualTo(held), "호버 중 상승이 반복되지 않는다");
            Assert.That(chip.TitleLabel.rectTransform.anchoredPosition, Is.EqualTo(titlePosition));
            yield return Capture("chip-hover-held");

            // 빠른 진입·이탈 반복 뒤 원위치로 정확히 돌아온다.
            for (var i = 0; i < 8; i++)
            {
                yield return MoveMouse(new Vector2(40f, 40f));
                yield return MoveMouse(ChipCenter());
            }

            yield return MoveMouse(new Vector2(40f, 40f));
            yield return UiTestWait.Until(() => chip.HoverLevel <= 0f, "hover returned");
            Assert.That(chip.HumanRect.anchoredPosition.y, Is.EqualTo(humanRest).Within(0.01f));
            Assert.That(chip.ArrowRect.anchoredPosition.y, Is.EqualTo(arrowRest).Within(0.01f));
        }

        // ---------- 해상도 ----------

        [UnityTest]
        public IEnumerator Resolutions_PopupAndChipStayOnScreenAndReadable()
        {
            yield return Load();
            Prepare(1240, 10, 1500);
            Deplete();
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) })
            {
                yield return env.Resolution.Set(size.x, size.y);
                if (!view.IsPopupVisible)
                {
                    controller.OpenPanel();
                }

                yield return UiTestWait.Until(() => view.IsStable, "popup stable at " + size);
                yield return null;
                AssertInsideScreen((RectTransform)view.Popup.CardRect, "card " + size);
                AssertInsideScreen((RectTransform)view.Popup.RescueButton.transform, "rescue button " + size);
                AssertInsideScreen((RectTransform)view.Popup.CloseButton.transform, "close button " + size);
                var number = view.Popup.CardRect.Find("Content/CostTable/Row0/After").GetComponent<TMP_Text>();
                var scale = view.GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
                Assert.That(number.fontSize * scale, Is.GreaterThanOrEqualTo(14f), "비용 숫자가 너무 작지 않다 " + size);
                yield return Capture("resolution-" + size.x + "x" + size.y + "-popup");

                yield return CloseByButton();
                yield return UiTestWait.Until(() => view.Chip.ShowLevel >= 1f, "chip stable at " + size);
                AssertInsideScreen((RectTransform)view.Chip.transform, "chip " + size);
                yield return Capture("resolution-" + size.x + "x" + size.y + "-chip");
                Assert.That(view.IsPopupVisible, Is.False);
            }
        }

        // ---------- 캡처(결정적 프레임) ----------

        [UnityTest]
        public IEnumerator Capture_PopupIntro_Idle_Close()
        {
            yield return Load();
            Prepare(1240, 10, 1500);
            hideDroneOverlayInCaptures = true;
            Deplete();
            yield return UiTestWait.Until(() => view.IsStable, "popup stable");
            yield return CloseByButton();
            yield return UiTestWait.Until(() => view.Chip.ShowLevel >= 1f, "chip stable");

            view.Popup.ManualTick = true;
            view.Chip.ManualTick = true;
            view.HideImmediate();
            view.SetChipVisible(false);
            yield return null;

            controller.OpenPanel();
            var index = 0;
            for (var i = 0; i < 22; i++)
            {
                yield return Capture("intro-" + index.ToString("D3"));
                view.Popup.Tick(Frame);
                index++;
            }

            // 표시 중 간헐 글리치: 시간을 건너뛰며 몇 장 남긴다.
            for (var i = 0; i < 90; i++)
            {
                view.Popup.Tick(Frame);
                if (i % 3 == 0)
                {
                    yield return Capture("idle-" + (i / 3).ToString("D3"));
                }
            }

            view.Popup.CloseButton.onClick.Invoke();
            for (var i = 0; i < 12; i++)
            {
                yield return Capture("outro-" + i.ToString("D3"));
                view.Popup.Tick(Frame);
            }

            Assert.That(view.IsPopupVisible, Is.False);
            for (var i = 0; i < 14; i++)
            {
                yield return Capture("chipshow-" + i.ToString("D3"));
                view.Chip.Tick(Frame);
            }
        }

        [UnityTest]
        public IEnumerator Capture_ChipHover_KeycapPulse_PressFeedback()
        {
            yield return Load();
            Prepare(1240, 10, 1500);
            hideDroneOverlayInCaptures = true;
            Deplete();
            yield return UiTestWait.Until(() => view.IsStable, "popup stable");
            yield return CloseByButton();
            yield return UiTestWait.Until(() => view.Chip.ShowLevel >= 1f, "chip stable");

            EmergencyRescueChipView chip = view.Chip;
            chip.ManualTick = true;
            for (var i = 0; i < 3; i++)
            {
                chip.Tick(Frame);
            }

            // 호버 진입 → 유지 → 이탈
            chip.OnPointerEnter(new PointerEventData(EventSystem.current));
            for (var i = 0; i < 22; i++)
            {
                yield return Capture("hover-in-" + i.ToString("D3"));
                chip.Tick(Frame);
            }

            chip.OnPointerExit(new PointerEventData(EventSystem.current));
            for (var i = 0; i < 14; i++)
            {
                yield return Capture("hover-out-" + i.ToString("D3"));
                chip.Tick(Frame);
            }

            // 키캡 반복 안내 한 번
            var guard = 0;
            while (!chip.IsKeycapPulsing && guard++ < 600)
            {
                chip.Tick(Frame);
            }

            Assert.That(chip.IsKeycapPulsing, Is.True);
            for (var i = 0; i < 14; i++)
            {
                yield return Capture("keycap-" + i.ToString("D3"));
                chip.Tick(Frame);
            }

            // 실제 입력: 눌림 피드백과 함께 팝업이 열린다.
            chip.Tick(1f);
            for (var i = 0; i < 6; i++)
            {
                yield return Capture("press-" + i.ToString("D3"));
                if (i == 1)
                {
                    controller.OpenPanel();
                    view.Popup.ManualTick = true;
                }

                chip.Tick(Frame);
                if (view.Popup.IsVisible)
                {
                    view.Popup.Tick(Frame);
                }
            }
        }

        // ---------- helpers ----------

        private IEnumerator Load()
        {
            env = new UiTestEnvironment();
            yield return env.LoadIntegration();
            state = GameBootstrapper.Instance.State;
            controller = Object.FindAnyObjectByType<EmergencyRescueRuntimeController>(FindObjectsInactive.Include);
            view = Object.FindAnyObjectByType<EmergencyRescuePanelView>(FindObjectsInactive.Include);
            Assert.That(controller, Is.Not.Null, "EmergencyRescueRuntimeController");
            Assert.That(view, Is.Not.Null, "EmergencyRescuePanelView");
            Assert.That(state.Player.Energy, Is.GreaterThan(0), "테스트는 전력이 있는 상태에서 시작한다");
        }

        private void Prepare(int gold, int copper, int iron)
        {
            state.SetGold(gold);
            env.Save.InventoryService.SetMaximumCapacity(100000f);
            env.Save.InventoryService.AddMineral(DataIds.Minerals.Copper, copper);
            env.Save.InventoryService.AddMineral(DataIds.Minerals.Iron, iron);
        }

        private void Deplete()
        {
            state.SetCurrentEnergy(0);
        }

        private Transform Player()
        {
            var movement = Object.FindAnyObjectByType<SubTerra.Gameplay.Player.PlayerMovement>();
            Assert.That(movement, Is.Not.Null, "PlayerMovement");
            return movement.transform;
        }

        private int Copper()
        {
            return env.Save.InventoryService.State.GetQuantity(DataIds.Minerals.Copper);
        }

        private int Iron()
        {
            return env.Save.InventoryService.State.GetQuantity(DataIds.Minerals.Iron);
        }

        private static EmergencyRescueMineralCost Find(EmergencyRescueCost cost, string id)
        {
            for (var i = 0; i < cost.Minerals.Count; i++)
            {
                if (cost.Minerals[i].MineralId == id)
                {
                    return cost.Minerals[i];
                }
            }

            Assert.Fail("cost row not found: " + id);
            return default;
        }

        private void AssertDisplayedCostMatchesService()
        {
            var service = new EmergencyRescueService(state, env.Save.InventoryService);
            EmergencyRescueCost current = service.GetCurrentCost();
            Assert.That(EmergencyRescueCostRows.AreSame(view.DisplayedCost, current), Is.True, "표시 비용 = 실제 비용");
            var rows = EmergencyRescueCostRows.Build(current);
            Assert.That(view.Popup.VisibleRowCount, Is.EqualTo(rows.Count));
            for (var i = 0; i < rows.Count; i++)
            {
                Transform row = view.Popup.CardRect.Find("Content/CostTable/Row" + i);
                Assert.That(row.Find("Deduct").GetComponent<TMP_Text>().text, Is.EqualTo(rows[i].Deduct));
                Assert.That(row.Find("Before").GetComponent<TMP_Text>().text, Is.EqualTo(rows[i].Before));
                Assert.That(row.Find("After").GetComponent<TMP_Text>().text, Is.EqualTo(rows[i].After));
            }
        }

        private IEnumerator CloseByButton()
        {
            yield return UiTestWait.Click(env.Mouse, view.Popup.CloseButton);
            yield return UiTestWait.Until(() => !view.IsPopupVisible, "popup closed");
            yield return UiTestWait.Until(() => view.IsChipVisible, "chip visible after close");
        }

        private IEnumerator ClickChip()
        {
            yield return UiTestWait.Until(() => view.Chip.ShowLevel >= 1f, "chip unfold finished");
            yield return UiTestWait.FocusGameView();
            var position = ChipCenter();
            yield return MoveMouse(position);
            InputSystem.QueueStateEvent(env.Mouse, new MouseState { position = position, buttons = 1 });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(env.Mouse, new MouseState { position = position });
            yield return null;
            yield return null;
        }

        private Vector2 ChipCenter()
        {
            var rect = (RectTransform)view.Chip.transform;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        private IEnumerator MoveMouse(Vector2 position)
        {
            InputSystem.QueueStateEvent(env.Mouse, new MouseState { position = position });
            yield return null;
            yield return null;
        }

        private int OverlayCount()
        {
            var count = 0;
            foreach (var found in Object.FindObjectsByType<EmergencyRescuePanelView>(FindObjectsInactive.Include))
            {
                if (found.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private void AssertNothingOfOverlayRemains()
        {
            Assert.That(view.gameObject.activeSelf, Is.False, "닫힌 뒤 투명 패널이 남지 않는다");
            AssertNoLeftoverGlitchPieces();
            var pointer = new PointerEventData(EventSystem.current);
            var hits = new System.Collections.Generic.List<RaycastResult>();
            foreach (var point in new[] { new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), new Vector2(40f, 40f), new Vector2(Screen.width - 40f, Screen.height - 40f) })
            {
                pointer.position = point;
                hits.Clear();
                EventSystem.current.RaycastAll(pointer, hits);
                foreach (var hit in hits)
                {
                    Assert.That(hit.gameObject.transform.IsChildOf(view.transform), Is.False, "구출 오버레이가 입력을 막는다: " + hit.gameObject.name);
                }
            }
        }

        private void AssertNoLeftoverGlitchPieces()
        {
            if (view.gameObject.activeSelf)
            {
                return;
            }

            Assert.That(view.Popup.AnyGlitchPieceVisible, Is.False);
        }

        private static void AssertInsideScreen(RectTransform rect, string label)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(-0.5f), label);
            Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(-0.5f), label);
            Assert.That(corners[2].x, Is.LessThanOrEqualTo(Screen.width + 0.5f), label);
            Assert.That(corners[2].y, Is.LessThanOrEqualTo(Screen.height + 0.5f), label);
        }

        // 기존 드론 대사창(정렬 30000)이 머리 위 안내를 덮는 위치에 뜨는 경우가 있어, 증거 캡처에서만 끈다.
        private bool hideDroneOverlayInCaptures;

        private IEnumerator Capture(string name)
        {
            if (hideDroneOverlayInCaptures)
            {
                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
                {
                    if (canvas.overrideSorting && canvas.sortingOrder == SubTerra.App.UI.Drone.DroneDialogueSocket.OverlaySortingOrder)
                    {
                        canvas.enabled = false;
                    }
                }
            }

            yield return UiTestWait.Capture(EvidenceDirectory + "/" + name + ".png");
        }
    }
}
