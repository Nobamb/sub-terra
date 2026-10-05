using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Outpost;
using SubTerra.App.State;
using SubTerra.App.UI;
using SubTerra.App.UI.Economy;
using SubTerra.App.UI.Outpost;
using SubTerra.App.UI.Sell;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Power;
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
    /// B-136 지상 판매창(SurfaceBase)과 정산 콘솔(Integration)이 같은 공통 판매 팝업을 쓰는지 실제 Scene에서 검증한다.
    /// 버튼은 실제 마우스 이벤트로 누르고, 결과는 실제 EconomyService·OutpostService·인벤토리 값과 비교한다.
    /// </summary>
    public sealed class PromptB136ResourceSellPlayModeTests
    {
        private const string EvidenceDirectory = "Temp/b136-sell-evidence";
        private const float Frame = 1f / 30f;
        private const string Copper = DataIds.Minerals.Copper;
        private const string Iron = DataIds.Minerals.Iron;
        private const string Lithium = DataIds.Minerals.Lithium;
        private const string Fuel = DataIds.RareItems.EngineFuel;

        private UiTestEnvironment env;
        private readonly List<GameObject> spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(EvidenceDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null)
                {
                    Object.Destroy(spawned[i]);
                }
            }

            spawned.Clear();
            env?.Dispose();
            env = null;
        }

        // ================= 지상 판매창 =================

        [UnityTest]
        public IEnumerator Surface_RealClicks_AccumulateClampAndSellMatchesServices()
        {
            var binder = default(EconomyPanelBinder);
            yield return LoadSurface(b => binder = b);
            var state = GameBootstrapper.Instance.State;
            state.SetGold(10335);
            Add(Copper, 30);
            Add(Iron, 13);
            Add(Lithium, 7);
            Add(Fuel, 1);
            var saves = 0;
            env.Save.Economy.AutoSaveRequested += _ => saves++;

            var popup = binder.View.SellPopup;
            Assert.That(popup, Is.Not.Null, "지상 판매창이 공통 판매 팝업을 쓴다");
            yield return ClickCenter(FindButton("OpenSellButton"));
            Assert.That(popup.State, Is.EqualTo(ResourceSellPopupView.PopupState.Opening));
            yield return UiTestWait.Until(() => popup.State == ResourceSellPopupView.PopupState.Open, "surface popup open");
            Assert.That(popup.VisibleRowCount, Is.EqualTo(4));
            Assert.That(popup.GoldValueString, Is.EqualTo("10,335G"));
            AllZero(popup);

            var copper = Row(popup, Copper);
            yield return ClickCenter(copper.PlusButton.Button);
            yield return ClickCenter(copper.PlusButton.Button);
            Assert.That(copper.QuantityString, Is.EqualTo("2"));
            for (var i = 0; i < 3; i++)
            {
                yield return ClickCenter(copper.PlusFiveButton.Button);
            }

            Assert.That(copper.QuantityString, Is.EqualTo("17"), "+5는 누적: 2 → 17");

            var iron = Row(popup, Iron);
            yield return ClickCenter(iron.MaxButton.Button);
            Assert.That(iron.QuantityString, Is.EqualTo("13"));
            Assert.That(iron.PlusButton.Button.interactable, Is.False, "최대에서는 증가 버튼 비활성");
            Assert.That(iron.MaxButton.Button.interactable, Is.False);
            yield return ClickCenter(iron.MinusButton.Button);
            yield return ClickCenter(iron.PlusFiveButton.Button);
            Assert.That(iron.QuantityString, Is.EqualTo("13"), "12에서 +5를 눌러도 보유 13까지만");

            var lithium = Row(popup, Lithium);
            yield return ClickCenter(lithium.PlusTenButton.Button);
            Assert.That(lithium.QuantityString, Is.EqualTo("7"));

            var expected = 17 * 10 + 13 * 15 + 7 * 40;
            Assert.That(popup.SellLabelString, Is.EqualTo("판매 · +" + expected.ToString("N0") + "G"));
            Assert.That(popup.TotalQuantityString, Is.EqualTo("37개"));
            Assert.That(popup.ProjectionString, Does.Contain((10335 + expected).ToString("N0")));
            var inventory = env.Save.InventoryService;
            var weightBefore = inventory.CurrentWeight;
            var weightAfter = weightBefore - (17 * 1.5f + 13 * 2f + 7 * 0.8f);
            StringAssert.Contains(Cargo(weightAfter), popup.CargoString, "판매 후 예상 화물 = 실제 무게 계산");
            yield return Capture("surface-selected-1920x1080");

            yield return ClickCenter(popup.SellButton.Button);
            Assert.That(state.Player.Gold, Is.EqualTo(10335 + expected), "화면 금액 = 실제 지급");
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(13));
            Assert.That(inventory.State.GetQuantity(Iron), Is.Zero);
            Assert.That(inventory.State.GetQuantity(Lithium), Is.Zero);
            Assert.That(inventory.State.GetQuantity(Fuel), Is.EqualTo(1), "직접 지정하지 않은 희귀 품목은 그대로");
            Assert.That(inventory.CurrentWeight, Is.EqualTo(weightAfter).Within(0.01f));
            Assert.That(saves, Is.EqualTo(1), "여러 자원 판매도 자동 저장 요청은 1회");
            Assert.That(popup.IsVisible, Is.True, "판매 후 지상 판매창은 기존처럼 열린 채 유지");
            Assert.That(popup.SellButton.Button.interactable, Is.False, "판매 예정 수량이 0으로 초기화");
            Assert.That(popup.VisibleRowCount, Is.EqualTo(2));
            yield return UiTestWait.Until(() => !popup.IsSaleEffectActive, "sale effect done");
            Assert.That(popup.GoldValueString, Is.EqualTo((10335 + expected).ToString("N0") + "G"));
            StringAssert.Contains(Cargo(inventory.CurrentWeight), popup.CargoString);
        }

        [UnityTest]
        public IEnumerator Surface_SelectAllExcludesFuel_ResetKeepsGold_AndEmptyState()
        {
            var binder = default(EconomyPanelBinder);
            yield return LoadSurface(b => binder = b);
            var state = GameBootstrapper.Instance.State;
            state.SetGold(50);
            Add(Copper, 4);
            Add(Iron, 3);
            Add(Fuel, 2);
            var popup = binder.View.SellPopup;
            yield return OpenSurface(popup);

            Assert.That(popup.NoticeString, Does.Contain("희귀"), "제외 규칙을 최대 선택 근처에서 안내");
            Assert.That(Row(popup, Fuel).NoteString, Is.Not.Empty);
            yield return ClickCenter(popup.SelectAllButton.Button);
            Assert.That(Row(popup, Copper).QuantityString, Is.EqualTo("4"));
            Assert.That(Row(popup, Iron).QuantityString, Is.EqualTo("3"));
            Assert.That(Row(popup, Fuel).QuantityString, Is.EqualTo("0"), "최대 선택에서 엔진 연료 제외");
            Assert.That(state.Player.Gold, Is.EqualTo(50), "최대 선택은 판매하지 않는다");
            Assert.That(env.Save.InventoryService.State.GetQuantity(Copper), Is.EqualTo(4));

            yield return ClickCenter(popup.ResetButton.Button);
            AllZero(popup);

            yield return ClickCenter(Row(popup, Fuel).PlusButton.Button);
            yield return ClickCenter(popup.SellButton.Button);
            Assert.That(state.Player.Gold, Is.EqualTo(150), "엔진 연료 개별 판매 100G");
            Assert.That(env.Save.InventoryService.State.GetQuantity(Fuel), Is.EqualTo(1));

            yield return ClickCenter(popup.SelectAllButton.Button);
            yield return ClickCenter(Row(popup, Fuel).MaxButton.Button);
            yield return ClickCenter(popup.SellButton.Button);
            Assert.That(state.Player.Gold, Is.EqualTo(150 + 40 + 45 + 100));
            Assert.That(popup.EmptyVisible, Is.True, "보유 자원이 없으면 안내 문구");
            Assert.That(popup.VisibleRowCount, Is.Zero);
            Assert.That(popup.SellButton.Button.interactable, Is.False);
            yield return UiTestWait.Until(() => !popup.IsSaleEffectActive, "sale effect done");
            yield return Capture("surface-empty-1920x1080");
        }

        [UnityTest]
        public IEnumerator Surface_LiveInventoryChange_ClampsSelection_AndDoubleSubmitSellsOnce()
        {
            var binder = default(EconomyPanelBinder);
            yield return LoadSurface(b => binder = b);
            var state = GameBootstrapper.Instance.State;
            Add(Copper, 10);
            var saves = 0;
            env.Save.Economy.AutoSaveRequested += _ => saves++;
            var popup = binder.View.SellPopup;
            yield return OpenSurface(popup);

            var copper = Row(popup, Copper);
            yield return ClickCenter(copper.MaxButton.Button);
            yield return ClickCenter(copper.MinusButton.Button);
            yield return ClickCenter(copper.MinusButton.Button);
            Assert.That(copper.QuantityString, Is.EqualTo("8"));

            // 표시 중 보유량이 다른 경로로 줄면 수량과 예상 결과가 즉시 맞춰진다.
            env.Save.InventoryService.TryReduceMineral(Copper, 5);
            Assert.That(copper.OwnedString, Is.EqualTo("5"));
            Assert.That(copper.QuantityString, Is.EqualTo("5"));
            Assert.That(popup.SellLabelString, Is.EqualTo("판매 · +50G"));
            Add(Iron, 3);
            Assert.That(popup.VisibleRowCount, Is.EqualTo(2), "새로 생긴 자원 행도 바로 보인다");

            // 같은 프레임 연속 입력: 거래는 한 번만.
            popup.SellButton.Button.onClick.Invoke();
            popup.SellButton.Button.onClick.Invoke();
            yield return null;
            popup.SellButton.Button.onClick.Invoke();
            Assert.That(state.Player.Gold, Is.EqualTo(50));
            Assert.That(env.Save.InventoryService.State.GetQuantity(Copper), Is.Zero);
            Assert.That(saves, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Surface_CloseByXKeyDuringIntro_ByButtonDuringSale_AndRepeatedOpen_LeaveNothing()
        {
            var binder = default(EconomyPanelBinder);
            yield return LoadSurface(b => binder = b);
            Add(Copper, 6);
            var popup = binder.View.SellPopup;
            var open = FindButton("OpenSellButton");

            // 등장 도중 X 키: 기존 지상 기지 X(최상위 창 닫기) 경로로 닫힌다.
            yield return ClickCenter(open);
            yield return UiTestWait.Delay(0.2f, "mid intro");
            Assert.That(popup.State, Is.EqualTo(ResourceSellPopupView.PopupState.Opening));
            yield return UiTestWait.Press(env.Keyboard, Key.X);
            Assert.That(popup.IsClosing || !popup.IsVisible, Is.True);
            yield return UiTestWait.Until(() => !popup.IsVisible, "closed after X");
            AssertClosedClean(popup);
            Assert.That(binder.IsModalVisible, Is.False);

            // 반복 열기·닫기.
            for (var i = 0; i < 3; i++)
            {
                yield return ClickCenter(open);
                yield return UiTestWait.Until(() => popup.State == ResourceSellPopupView.PopupState.Open, "reopen " + i);
                AllZero(popup);
                yield return ClickCenter(popup.CloseButton.Button);
                yield return UiTestWait.Until(() => !popup.IsVisible, "close " + i);
                AssertClosedClean(popup);
            }

            // 판매 연출 도중 닫기: 숫자 연출을 정리하고 최신 값으로 끝난다.
            yield return OpenSurface(popup);
            yield return ClickCenter(Row(popup, Copper).MaxButton.Button);
            yield return ClickCenter(popup.SellButton.Button);
            Assert.That(popup.IsSaleEffectActive, Is.True);
            yield return ClickCenter(popup.CloseButton.Button);
            Assert.That(popup.IsSaleEffectActive, Is.False);
            Assert.That(popup.GoldValueString, Is.EqualTo("60G"));
            var closeTimer = Time.realtimeSinceStartup;
            yield return UiTestWait.Until(() => !popup.IsVisible, "close during sale");
            Assert.That(Time.realtimeSinceStartup - closeTimer, Is.LessThan(0.6f));
            AssertClosedClean(popup);
        }

        [UnityTest]
        public IEnumerator Surface_Resolutions_KeepCardOnScreen_ColumnsAligned_TextReadable()
        {
            var binder = default(EconomyPanelBinder);
            yield return LoadSurface(b => binder = b);
            Add(Copper, 13);
            Add(Lithium, 7);
            Add(Fuel, 1);
            var popup = binder.View.SellPopup;
            yield return OpenSurface(popup);
            popup.Session.Adjust(Copper, 5);
            popup.Session.Adjust(Lithium, 2);
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) })
            {
                yield return env.Resolution.Set(size.x, size.y);
                yield return null;
                AssertInsideScreen(popup.CardRect, "card " + size);
                AssertInsideScreen((RectTransform)popup.SellButton.transform, "sell " + size);
                var row = Row(popup, Copper);
                var gold = row.transform.Find("Gold").GetComponent<TMP_Text>();
                var px = gold.fontSize * ScreenScale(gold.rectTransform);
                Assert.That(px, Is.GreaterThanOrEqualTo(14f), "받을 골드 글자 크기 " + size);
                var note = Row(popup, Fuel).transform.Find("Note").GetComponent<TMP_Text>();
                Assert.That(note.fontSize * ScreenScale(note.rectTransform), Is.GreaterThanOrEqualTo(9.5f), "보조 안내 " + size);
                var maxRight = ScreenRect((RectTransform)row.MaxButton.transform).xMax;
                var coinLeft = ScreenRect((RectTransform)row.transform.Find("GoldCoin")).xMin;
                Assert.That(maxRight, Is.LessThan(coinLeft), "수량 버튼과 금액이 겹치지 않는다 " + size);
                yield return Capture("surface-" + size.x + "x" + size.y);
            }
        }

        [UnityTest]
        public IEnumerator Surface_LongList_ScrollsListOnly_HeaderAndSummaryStayFixed()
        {
            var binder = default(EconomyPanelBinder);
            yield return LoadSurface(b => binder = b);
            var popup = binder.View.SellPopup;
            // 현재 데이터에는 판매 품목이 4종뿐이라, 같은 팝업에 긴 목록 데이터를 넣어 스크롤만 확인한다.
            var stub = new ResourceSellSession(new ManyLinesBackend(9));
            popup.Attach(stub);
            popup.Show();
            yield return UiTestWait.Until(() => popup.State == ResourceSellPopupView.PopupState.Open, "stub popup open");
            Assert.That(popup.ScrollbarVisible, Is.True);
            var header = ScreenRect((RectTransform)popup.CardRect.Find("Content/List/ColumnHeader"));
            var summary = ScreenRect((RectTransform)popup.SellButton.transform);
            var firstRow = ScreenRect((RectTransform)popup.Rows[0].transform);
            popup.Scroll.verticalNormalizedPosition = 0f;
            yield return null;
            Assert.That(ScreenRect((RectTransform)popup.CardRect.Find("Content/List/ColumnHeader")).y, Is.EqualTo(header.y).Within(0.5f));
            Assert.That(ScreenRect((RectTransform)popup.SellButton.transform).y, Is.EqualTo(summary.y).Within(0.5f));
            Assert.That(ScreenRect((RectTransform)popup.Rows[0].transform).y, Is.GreaterThan(firstRow.y + 100f), "목록만 위로 스크롤");
            var viewport = ScreenRect(popup.Scroll.viewport);
            var last = ScreenRect((RectTransform)popup.Rows[8].transform);
            Assert.That(last.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 1f), "마지막 행까지 볼 수 있다");
            yield return Capture("surface-long-list-scrolled");
            popup.HideImmediate();
            popup.Attach(binder.SellSession);
            stub.Dispose();
        }

        [UnityTest]
        public IEnumerator Capture_Surface_Open_Adjust_Sell_Close()
        {
            var binder = default(EconomyPanelBinder);
            yield return LoadSurface(b => binder = b);
            GameBootstrapper.Instance.State.SetGold(10335);
            Add(Copper, 13);
            Add(Lithium, 7);
            Add(Iron, 4);
            var popup = binder.View.SellPopup;
            yield return CaptureFlow(popup, () => binder.View.SetVisible(true), () => binder.CloseModal(), "surface");
        }

        // ================= 정산 콘솔 =================

        [UnityTest]
        public IEnumerator Console_SameSellUi_RareBlocked_BatchSettles_AndStaysOpen()
        {
            var scene = default(ConsoleScene);
            yield return LoadConsole(s => scene = s);
            var state = GameBootstrapper.Instance.State;
            state.SetGold(500);
            Add(Copper, 6);
            Add(Iron, 4);
            Add(Fuel, 1);
            var saves = 0;
            var results = new List<OutpostOperationResult>();
            scene.Service.AutoSaveRequested += _ => saves++;
            scene.Service.OperationCompleted += results.Add;

            scene.Binder.Presenter.ToggleInteractionPanel();
            var popup = scene.View.SellPopup;
            Assert.That(popup, Is.Not.Null, "정산 콘솔도 공통 판매 팝업을 쓴다");
            Assert.That(scene.View.PanelRoot.activeSelf, Is.False, "예전 큰 정산 패널은 뜨지 않는다");
            yield return UiTestWait.Until(() => popup.State == ResourceSellPopupView.PopupState.Open, "console popup open");
            Assert.That(popup.CardRect.sizeDelta, Is.EqualTo(ResourceSellPopupView.CardSize), "지상과 같은 레이아웃");
            Assert.That(popup.HintString, Does.Contain("정산 콘솔"));
            Assert.That(popup.CardRect.Find("Content/Header/CargoGauge"), Is.Not.Null);

            var fuel = Row(popup, Fuel);
            Assert.That(fuel.PlusButton.Button.interactable, Is.False, "정산 콘솔은 희귀 품목을 팔 수 없다");
            Assert.That(fuel.MaxButton.Button.interactable, Is.False);
            Assert.That(fuel.NoteString, Does.Contain("지상"));
            Assert.That(popup.NoticeString, Does.Contain("정산 콘솔"));

            yield return ClickCenter(popup.SelectAllButton.Button);
            Assert.That(Row(popup, Copper).QuantityString, Is.EqualTo("6"));
            Assert.That(Row(popup, Iron).QuantityString, Is.EqualTo("4"));
            Assert.That(popup.SellLabelString, Is.EqualTo("판매 · +120G"));
            yield return Capture("console-selected-1920x1080");
            yield return ClickCenter(popup.SellButton.Button);

            Assert.That(state.Player.Gold, Is.EqualTo(620));
            Assert.That(env.Save.InventoryService.State.GetQuantity(Copper), Is.Zero);
            Assert.That(env.Save.InventoryService.State.GetQuantity(Fuel), Is.EqualTo(1));
            Assert.That(saves, Is.EqualTo(1));
            Assert.That(results.Count, Is.EqualTo(1), "정산은 한 번의 거래");
            Assert.That(results[0].Kind, Is.EqualTo(OutpostOperationKind.SettlePlayerCargo), "퀘스트 16 판정 결과 형식 유지");
            Assert.That(results[0].Quantity, Is.EqualTo(10));
            Assert.That(results[0].GoldDelta, Is.EqualTo(120));
            Assert.That(popup.IsVisible, Is.True, "정산 후에도 창은 열린 채 유지");
            yield return UiTestWait.Until(() => !popup.IsSaleEffectActive, "sale effect done");
            Assert.That(popup.GoldValueString, Is.EqualTo("620G"));
        }

        [UnityTest]
        public IEnumerator Console_RangeExit_XKey_AndCloseButton_CloseCleanly_EvenDuringIntro()
        {
            var scene = default(ConsoleScene);
            yield return LoadConsole(s => scene = s);
            Add(Copper, 3);
            var popup = default(ResourceSellPopupView);

            // 등장 도중 범위 이탈.
            scene.Binder.Presenter.ToggleInteractionPanel();
            popup = scene.View.SellPopup;
            yield return UiTestWait.Delay(0.2f, "mid intro");
            Assert.That(popup.State, Is.EqualTo(ResourceSellPopupView.PopupState.Opening));
            scene.MovePlayer(new Vector3(30f, 0f, 0f));
            yield return UiTestWait.Until(() => !scene.Service.IsFacilityInteraction, "left range");
            Assert.That(popup.IsClosing || !popup.IsVisible, Is.True, "범위 이탈 시 기존 조건대로 닫힌다");
            yield return UiTestWait.Until(() => !popup.IsVisible, "closed after leaving");
            AssertClosedClean(popup);

            // 열린 상태에서 범위 이탈.
            yield return scene.ReturnToSettlement();
            scene.Binder.Presenter.ToggleInteractionPanel();
            yield return UiTestWait.Until(() => popup.State == ResourceSellPopupView.PopupState.Open, "open again");
            AllZero(popup);
            scene.MovePlayer(new Vector3(30f, 0f, 0f));
            yield return UiTestWait.Until(() => !popup.IsVisible, "closed after leaving while open");
            AssertClosedClean(popup);

            // X 키(최상위 팝업 닫기).
            yield return scene.ReturnToSettlement();
            scene.Binder.Presenter.ToggleInteractionPanel();
            yield return UiTestWait.Until(() => popup.State == ResourceSellPopupView.PopupState.Open, "open for X");
            yield return UiTestWait.Press(env.Keyboard, Key.X);
            yield return UiTestWait.Until(() => !popup.IsVisible, "closed by X key");
            Assert.That(scene.Binder.Presenter.IsInteractionPanelOpen, Is.False);
            AssertClosedClean(popup);

            // 창의 X 버튼. 범위 안에 있어도 다시 상호작용하면 열린다.
            scene.Binder.Presenter.ToggleInteractionPanel();
            yield return UiTestWait.Until(() => popup.State == ResourceSellPopupView.PopupState.Open, "open for button");
            yield return ClickCenter(popup.CloseButton.Button);
            yield return UiTestWait.Until(() => !popup.IsVisible, "closed by button");
            AssertClosedClean(popup);
            for (var i = 0; i < 20; i++)
            {
                yield return null;
                Assert.That(popup.IsVisible, Is.False, "범위 안에서 저절로 다시 열리지 않는다");
            }
        }

        [UnityTest]
        public IEnumerator Capture_Console_Open_Adjust_Sell_Close()
        {
            var scene = default(ConsoleScene);
            yield return LoadConsole(s => scene = s);
            GameBootstrapper.Instance.State.SetGold(10335);
            Add(Copper, 13);
            Add(Lithium, 7);
            Add(Fuel, 1);
            var popup = scene.View.SellPopup;
            yield return CaptureFlow(popup, () => scene.Binder.Presenter.ToggleInteractionPanel(), () => scene.Binder.ClosePanel(), "console");
        }

        // ================= helpers =================

        private IEnumerator CaptureFlow(ResourceSellPopupView popup, System.Action open, System.Action close, string prefix)
        {
            popup.ManualTick = true;
            open();
            var i = 0;
            for (var t = 0f; t <= ResourceSellTimeline.OpenDuration + Frame; t += Frame)
            {
                yield return Capture(prefix + "-open-" + (i++).ToString("D3"));
                popup.Tick(Frame);
            }

            popup.Tick(0.1f);
            var copper = Row(popup, Copper);
            var lithium = Row(popup, Lithium);
            i = 0;
            foreach (var button in new[] { copper.PlusButton, copper.PlusButton, copper.PlusFiveButton, copper.PlusFiveButton, lithium.PlusButton, lithium.PlusButton })
            {
                yield return ClickCenter(button.Button, false);
                yield return Capture(prefix + "-adjust-" + (i++).ToString("D3"));
            }

            yield return ClickCenter(popup.SellButton.Button, false);
            i = 0;
            for (var t = 0f; t <= ResourceSellTimeline.SaleDuration; t += Frame)
            {
                yield return Capture(prefix + "-sale-" + (i++).ToString("D3"));
                popup.Tick(Frame);
            }

            close();
            i = 0;
            while (popup.IsVisible && i < 30)
            {
                yield return Capture(prefix + "-close-" + (i++).ToString("D3"));
                popup.Tick(Frame);
            }

            Assert.That(popup.IsVisible, Is.False);
            popup.ManualTick = false;
        }

        private IEnumerator LoadSurface(System.Action<EconomyPanelBinder> found)
        {
            env = new UiTestEnvironment();
            EconomyPanelBinder binder = null;
            yield return env.LoadSurfaceBase(() =>
            {
                binder = Object.FindAnyObjectByType<EconomyPanelBinder>(FindObjectsInactive.Include);
                return binder != null && binder.IsBound && binder.SellSession != null && binder.View.SellPopup != null;
            });
            env.Save.InventoryService.SetMaximumCapacity(120f);
            found(binder);
        }

        private IEnumerator OpenSurface(ResourceSellPopupView popup)
        {
            yield return ClickCenter(FindButton("OpenSellButton"));
            yield return UiTestWait.Until(() => popup.State == ResourceSellPopupView.PopupState.Open, "surface popup open");
        }

        private IEnumerator LoadConsole(System.Action<ConsoleScene> found)
        {
            env = new UiTestEnvironment();
            yield return env.LoadIntegration();
            env.Save.InventoryService.SetMaximumCapacity(120f);
            var scene = new ConsoleScene(spawned);
            yield return scene.Prepare();
            found(scene);
        }

        private void Add(string id, int quantity)
        {
            env.Save.InventoryService.AddMineral(id, quantity);
        }

        private static ResourceSellRowView Row(ResourceSellPopupView popup, string id)
        {
            for (var i = 0; i < popup.Rows.Count; i++)
            {
                if (popup.Rows[i].gameObject.activeSelf && popup.Rows[i].ItemId == id)
                {
                    return popup.Rows[i];
                }
            }

            Assert.Fail("row not found: " + id);
            return null;
        }

        private static void AllZero(ResourceSellPopupView popup)
        {
            for (var i = 0; i < popup.Rows.Count; i++)
            {
                if (popup.Rows[i].gameObject.activeSelf)
                {
                    Assert.That(popup.Rows[i].QuantityString, Is.EqualTo("0"), "열 때 판매 수량은 0");
                }
            }
        }

        private static string Cargo(float value)
        {
            return value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static Button FindButton(string name)
        {
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude))
            {
                if (button.name == name)
                {
                    return button;
                }
            }

            Assert.Fail("button not found: " + name);
            return null;
        }

        private static void AssertClosedClean(ResourceSellPopupView popup)
        {
            Assert.That(popup.State, Is.EqualTo(ResourceSellPopupView.PopupState.Hidden));
            Assert.That(popup.gameObject.activeSelf, Is.False, "닫힌 뒤 입력 차단 영역이 남지 않는다");
            Assert.That(popup.AnyEffectVisible, Is.False, "남은 금화·빛 없음");
            Assert.That(popup.IsSaleEffectActive, Is.False);
            Assert.That(PopupWindowSorting.Top == popup.Canvas, Is.False);
            var pointer = new PointerEventData(EventSystem.current);
            var hits = new List<RaycastResult>();
            foreach (var point in new[] { new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), new Vector2(Screen.width * 0.3f, Screen.height * 0.3f) })
            {
                pointer.position = point;
                hits.Clear();
                EventSystem.current.RaycastAll(pointer, hits);
                foreach (var hit in hits)
                {
                    Assert.That(hit.gameObject.transform.IsChildOf(popup.transform), Is.False, "판매 팝업이 입력을 막는다: " + hit.gameObject.name);
                }
            }
        }

        private IEnumerator ClickCenter(Button button, bool requireTop = true)
        {
            yield return UiTestWait.FocusGameView();
            Canvas.ForceUpdateCanvases();
            var position = ScreenRect((RectTransform)button.transform).center;
            if (requireTop)
            {
                Assert.That(button.IsInteractable(), Is.True, button.name + " is not interactable");
                var pointer = new PointerEventData(EventSystem.current) { position = position };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits, Is.Not.Empty, button.name + " 아래에 레이캐스트 대상이 없다.");
                Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button),
                    button.name + " 위를 다른 UI가 가리고 있다: " + hits[0].gameObject.name);
            }

            env.Mouse.MakeCurrent();
            InputSystem.QueueStateEvent(env.Mouse, new MouseState { position = position });
            yield return null;
            InputSystem.QueueStateEvent(env.Mouse, new MouseState { position = position, buttons = 1 });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(env.Mouse, new MouseState { position = position });
            yield return null;
            yield return null;
        }

        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(EvidenceDirectory, name + ".png"), texture.EncodeToPNG());
            Object.Destroy(texture);
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static float ScreenScale(RectTransform rect)
        {
            var screen = ScreenRect(rect);
            return rect.rect.width > 0f ? screen.width / rect.rect.width : 1f;
        }

        private static void AssertInsideScreen(RectTransform rect, string label)
        {
            var screen = ScreenRect(rect);
            Assert.That(screen.xMin, Is.GreaterThanOrEqualTo(-0.5f), label);
            Assert.That(screen.yMin, Is.GreaterThanOrEqualTo(-0.5f), label);
            Assert.That(screen.xMax, Is.LessThanOrEqualTo(Screen.width + 0.5f), label);
            Assert.That(screen.yMax, Is.LessThanOrEqualTo(Screen.height + 0.5f), label);
        }

        /// <summary>실제 전진기지 코어·정산 콘솔 프리팹을 플레이어 옆에 설치하고 실제 브리지로 접근 상태를 만든다.</summary>
        private sealed class ConsoleScene
        {
            private readonly List<GameObject> spawned;
            private int sequence;

            public ConsoleScene(List<GameObject> spawned)
            {
                this.spawned = spawned;
            }

            public PlayerMovement Player { get; private set; }
            public PowerNetworkSystem Network { get; private set; }
            public OutpostPanelBinder Binder { get; private set; }
            public OutpostPanelView View { get; private set; }
            public Vector3 Origin { get; private set; }

            public OutpostService Service
            {
                get
                {
                    var binder = Object.FindAnyObjectByType<IntegrationRuntimeBinder>();
                    var field = typeof(IntegrationRuntimeBinder).GetField("outpostService", BindingFlags.Instance | BindingFlags.NonPublic);
                    return field != null ? field.GetValue(binder) as OutpostService : null;
                }
            }

            public IEnumerator Prepare()
            {
                Player = Object.FindAnyObjectByType<PlayerMovement>();
                Network = Object.FindAnyObjectByType<PowerNetworkSystem>();
                Binder = Object.FindAnyObjectByType<OutpostPanelBinder>(FindObjectsInactive.Include);
                View = Binder.GetComponent<OutpostPanelView>();
                Assert.That(Player, Is.Not.Null);
                Assert.That(Network, Is.Not.Null);
                Assert.That(View.SellPopup, Is.Not.Null, "정산 판매 팝업이 바인딩 때 만들어진다");
                Origin = Player.transform.position;
                Spawn(DataIds.Buildings.OutpostCoreBasic, Vector3.zero);
                Spawn(DataIds.Buildings.SettlementBasic, new Vector3(3f, 0f, 0f));
                yield return ReturnToSettlement();
                Assert.That(Service.IsFacilityInteraction, Is.True);
                Assert.That(Service.InteractionFacilityBuildingId, Is.EqualTo(DataIds.Buildings.SettlementBasic));
            }

            public IEnumerator ReturnToSettlement()
            {
                MovePlayer(new Vector3(3f, 0f, 0f));
                Network.RequestRebuild();
                yield return UiTestWait.Until(() => Service != null
                    && Service.IsFacilityInteraction
                    && Service.InteractionFacilityBuildingId == DataIds.Buildings.SettlementBasic, "near settlement console");
            }

            public void MovePlayer(Vector3 offset)
            {
                var body = Player.GetComponent<Rigidbody2D>();
                var target = Origin + offset;
                Player.transform.position = target;
                if (body != null)
                {
                    body.position = target;
                    body.linearVelocity = Vector2.zero;
                }
            }

            private void Spawn(string buildingId, Vector3 offset)
            {
                var catalog = GameBootstrapper.Instance.AssignedCatalog as GameDataCatalog;
                Assert.That(catalog.TryGetBuilding(buildingId, out var data), Is.True, buildingId);
                var instance = Object.Instantiate(data.RuntimePrefab, Origin + offset, Quaternion.identity);
                var id = buildingId + "-b136-" + (++sequence).ToString("D3");
                var building = instance.GetComponent<BuildingInstance>() ?? instance.AddComponent<BuildingInstance>();
                building.Initialize(id, buildingId);
                var node = instance.GetComponent<PowerNode>();
                if (node != null)
                {
                    node.SetEntityId(id);
                    node.SetNetwork(Network);
                }

                spawned.Add(instance);
            }
        }

        /// <summary>긴 목록 스크롤 확인용 표시 데이터. 거래는 하지 않는다.</summary>
        private sealed class ManyLinesBackend : IResourceSellBackend
        {
            private readonly ResourceSellLine[] lines;

            public ManyLinesBackend(int count)
            {
                lines = new ResourceSellLine[count];
                for (var i = 0; i < count; i++)
                {
                    lines[i] = new ResourceSellLine("test.item." + i, "자원 " + (i + 1), 5 + i, 10 + i, 1f, null);
                }
            }

            public event System.Action Changed
            {
                add { }
                remove { }
            }

            public string Hint => SurfaceSellBackend.HintText;
            public string BulkRuleNotice => string.Empty;

            public ResourceSellSnapshot Read(bool fresh)
            {
                return new ResourceSellSnapshot(lines, 1000, 20f, 60f, 0, ResourceSellBonusMode.PerLine);
            }

            public ResourceSellCommitResult Commit(IReadOnlyList<KeyValuePair<string, int>> items)
            {
                return new ResourceSellCommitResult(false, 0, "test");
            }

            public void Dispose()
            {
            }
        }
    }
}
