using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Outpost;
using SubTerra.App.UI;
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
    /// B-138 보관함 팝업을 실제 Integration Scene에서 검증한다.
    /// 버튼·행·드롭다운은 실제 마우스 이벤트로 누르고, 결과는 실제 OutpostService·인벤토리 값과 비교한다.
    /// </summary>
    public sealed class PromptB138StoragePopupPlayModeTests
    {
        private const string EvidenceDirectory = "Temp/b138-storage-evidence";
        private const float Frame = 1f / 30f;
        private const string Copper = DataIds.Minerals.Copper;
        private const string Iron = DataIds.Minerals.Iron;

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

        [UnityTest]
        public IEnumerator Storage_ShowboxLocksInput_ThenRealClicksDepositWithdrawThroughService()
        {
            var scene = default(StorageScene);
            yield return Load(s => scene = s);
            Add(Copper, 13);
            Add(Iron, 4);
            var inventory = env.Save.InventoryService;
            var results = new List<OutpostOperationResult>();
            scene.Service.OperationCompleted += results.Add;

            scene.Binder.Presenter.ToggleInteractionPanel();
            var popup = scene.View.StoragePopup;
            Assert.That(popup, Is.Not.Null, "보관함은 전용 팝업을 쓴다");
            Assert.That(scene.View.PanelRoot.activeSelf, Is.False, "예전 큰 보관함 패널은 뜨지 않는다");
            Assert.That(popup.State, Is.EqualTo(StoragePopupView.PopupState.Opening));
            Assert.That(popup.PopCount, Is.EqualTo(2), "보유 자원 종류만큼 광물이 튀어나온다");

            // 등장 중: 상자만 보이고 입력은 잠겨 있다.
            yield return UiTestWait.Delay(0.25f, "box rising");
            Assert.That(popup.State, Is.EqualTo(StoragePopupView.PopupState.Opening));
            Assert.That(popup.AnyEffectVisible, Is.True, "상자가 보인다");
            Assert.That(popup.IsInteractive, Is.False, "등장 중 입력 잠금");
            Assert.That(popup.CardAlpha, Is.Zero, "패널은 아직 없다");
            yield return UiTestWait.Delay(0.4f, "panel growing");
            if (popup.State == StoragePopupView.PopupState.Opening)
            {
                Assert.That(popup.IsInteractive, Is.EqualTo(StorageBoxTimeline.IsInteractive(popup.Clock)),
                    "조작 영역 공개 전에는 입력 잠금");
            }
            yield return UiTestWait.Until(() => popup.State == StoragePopupView.PopupState.Open, "storage open");
            Assert.That(popup.IsInteractive, Is.True, "정착 후 입력 가능");
            Assert.That(popup.AnyEffectVisible, Is.False, "정착 후 상자·광물 잔존 없음");
            Assert.That(popup.CardScale, Is.EqualTo(1f));

            Assert.That(Visible(popup.PlayerRows), Is.EqualTo(2));
            Assert.That(popup.PlayerTotalsString, Does.StartWith("2종 · 17개 · "));
            Assert.That(popup.StorageEmptyVisible, Is.True);
            Assert.That(popup.SelectionString, Does.Contain("자원을 선택하세요"));
            Assert.That(popup.DepositButton.Button.interactable, Is.False, "미선택이면 보관 불가");
            Assert.That(popup.WithdrawButton.Button.interactable, Is.False);

            // 검색 + 드롭다운으로 철 선택.
            var search = Find<TMP_InputField>(popup, "Card/Content/Controls/MineralPicker/SearchInput");
            search.text = "철";
            yield return null;
            var ironOption = Find<Button>(popup, "Card/Content/Controls/MineralPicker/OptionsPanel/Viewport/Content/Option_" + Iron);
            yield return ClickCenter(ironOption);
            Assert.That(popup.SelectedMineralId, Is.EqualTo(Iron));
            Assert.That(popup.Picker.IsOptionsOpen, Is.False);
            Assert.That(search.text, Is.Empty, "선택하면 검색어를 비운다");

            // 목록 행으로 구리 선택.
            yield return ClickCenter(Row(popup.PlayerRows, Copper).Button);
            Assert.That(popup.SelectedMineralId, Is.EqualTo(Copper));
            Assert.That(Row(popup.PlayerRows, Copper).IsSelected, Is.True);
            Assert.That(popup.SelectionString, Does.Contain("보유 13 · 보관 0"));
            Assert.That(popup.Quantity, Is.EqualTo(1));

            // 빠른 버튼은 지정이 아니라 누적: 1 + 5 = 6.
            yield return ClickCenter(popup.QuickButtons[1].Button);
            Assert.That(popup.Quantity, Is.EqualTo(6));
            Assert.That(popup.QuantityInput.text, Is.EqualTo("6"));
            // - 는 1개 감소, + 는 1개 증가.
            yield return ClickCenter(popup.MinusButton.Button);
            Assert.That(popup.Quantity, Is.EqualTo(5));
            yield return ClickCenter(popup.QuickButtons[0].Button);
            Assert.That(popup.Quantity, Is.EqualTo(6));
            Assert.That(popup.QuantityInput.text, Is.EqualTo("6"));
            Assert.That(popup.PreviewString, Does.Contain("보관  <b>6개</b>"));
            Assert.That(popup.WithdrawButton.Button.interactable, Is.False, "보관 0이면 꺼내기 불가");
            yield return Capture("selected-1920x1080");
            yield return ClickCenter(popup.DepositButton.Button);
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(7));
            Assert.That(scene.Service.State.GetStorageQuantity(Copper), Is.EqualTo(6));
            Assert.That(results[results.Count - 1].Kind, Is.EqualTo(OutpostOperationKind.Deposit));
            Assert.That(popup.StatusString, Does.Contain("6개"));
            Assert.That(popup.StorageEmptyVisible, Is.False);
            Assert.That(Row(popup.StorageRows, Copper).QuantityString, Is.EqualTo("6개"));

            // 직접 입력은 max(보유, 보관)까지만. 꺼내기는 요청이 많으면 보관 전부.
            popup.QuantityInput.text = "99";
            yield return null;
            Assert.That(popup.QuantityInput.text, Is.EqualTo("7"));
            Assert.That(popup.PreviewString, Does.Contain("꺼내기  <b>6개</b>"));
            yield return ClickCenter(popup.WithdrawButton.Button);
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(13));
            Assert.That(scene.Service.State.GetStorageQuantity(Copper), Is.Zero, "요청 7 > 보관 6 → 전량 꺼냄");

            // 0에서 +10 = 10개 보관 후 다시 +10: 범위를 넘으면 상한으로 맞춰지고 남은 3개 전부.
            popup.QuantityInput.text = "0";
            yield return null;
            Assert.That(popup.Quantity, Is.Zero);
            Assert.That(popup.MinusButton.Button.interactable, Is.False, "수량 0이면 - 비활성");
            yield return ClickCenter(popup.QuickButtons[2].Button);
            Assert.That(popup.Quantity, Is.EqualTo(10));
            yield return ClickCenter(popup.DepositButton.Button);
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(3));
            yield return ClickCenter(popup.QuickButtons[2].Button);
            Assert.That(popup.PreviewString, Does.Contain("보관  <b>3개</b>"));
            yield return ClickCenter(popup.DepositButton.Button);
            Assert.That(inventory.State.GetQuantity(Copper), Is.Zero, "요청 10 > 보유 3 → 전량 보관");
            Assert.That(scene.Service.State.GetStorageQuantity(Copper), Is.EqualTo(13));
            Assert.That(popup.DepositButton.Button.interactable, Is.False, "보유 0이면 보관 버튼 비활성");

            // 무게 한도: 실패하고 값은 그대로.
            inventory.SetMaximumCapacity(inventory.CurrentWeight + 5f);
            yield return ClickCenter(popup.QuickButtons[2].Button);
            yield return ClickCenter(popup.WithdrawButton.Button);
            Assert.That(results[results.Count - 1].Status, Is.EqualTo(OutpostOperationStatus.CapacityExceeded));
            Assert.That(scene.Service.State.GetStorageQuantity(Copper), Is.EqualTo(13));
            Assert.That(inventory.State.GetQuantity(Copper), Is.Zero);
            Assert.That(popup.StatusString, Does.Contain("한도"));
            yield return Capture("after-transfers-1920x1080");
            inventory.SetMaximumCapacity(120f);

            // 비활성 버튼은 호버해도 밝아지지 않는다.
            var disabled = popup.DepositButton;
            disabled.OnPointerEnter(null);
            disabled.Step(0.5f);
            Assert.That(disabled.HoverLevel, Is.Zero);
            disabled.OnPointerExit(null);
        }

        [UnityTest]
        public IEnumerator Storage_CloseDuringIntro_XKey_Interact_CloseButton_RangeExit_Repeat_LeaveNothing()
        {
            var scene = default(StorageScene);
            yield return Load(s => scene = s);
            Add(Copper, 3);
            var presenter = scene.Binder.Presenter;

            // 상자가 올라오는 중 X 키(최상위 팝업 닫기).
            presenter.ToggleInteractionPanel();
            var popup = scene.View.StoragePopup;
            yield return UiTestWait.Delay(0.15f, "rising");
            Assert.That(popup.State, Is.EqualTo(StoragePopupView.PopupState.Opening));
            yield return UiTestWait.Press(env.Keyboard, Key.X);
            Assert.That(popup.IsClosing || !popup.IsVisible, Is.True);
            yield return UiTestWait.Until(() => !popup.IsVisible, "closed by X during rise");
            AssertClosedClean(popup);
            Assert.That(presenter.IsInteractionPanelOpen, Is.False);

            // 광물이 튀어나오는 중 다시 상호작용(토글).
            presenter.ToggleInteractionPanel();
            yield return UiTestWait.Delay(0.52f, "popping");
            Assert.That(popup.State, Is.EqualTo(StoragePopupView.PopupState.Opening));
            presenter.ToggleInteractionPanel();
            Assert.That(popup.IsClosing, Is.True, "연출 중 재상호작용은 즉시 종료 연출");
            var started = Time.realtimeSinceStartup;
            yield return UiTestWait.Until(() => !popup.IsVisible, "closed by toggle");
            Assert.That(Time.realtimeSinceStartup - started, Is.LessThan(0.7f));
            AssertClosedClean(popup);

            // 창의 X 버튼, 이후 범위 안에서 저절로 열리지 않음.
            presenter.ToggleInteractionPanel();
            yield return UiTestWait.Until(() => popup.State == StoragePopupView.PopupState.Open, "open for button");
            yield return ClickCenter(popup.CloseButton.Button);
            yield return UiTestWait.Until(() => !popup.IsVisible, "closed by button");
            AssertClosedClean(popup);
            for (var i = 0; i < 20; i++)
            {
                yield return null;
                Assert.That(popup.IsVisible, Is.False);
            }

            // 열린 상태에서 범위 이탈.
            presenter.ToggleInteractionPanel();
            yield return UiTestWait.Until(() => popup.State == StoragePopupView.PopupState.Open, "open for leave");
            scene.MovePlayer(new Vector3(30f, 0f, 0f));
            yield return UiTestWait.Until(() => !popup.IsVisible, "closed after leaving");
            AssertClosedClean(popup);
            yield return scene.ReturnToStorage();

            // 반복 열기·닫기: 매번 미선택·수량 1로 다시 시작한다.
            for (var i = 0; i < 3; i++)
            {
                presenter.ToggleInteractionPanel();
                yield return UiTestWait.Until(() => popup.State == StoragePopupView.PopupState.Open, "reopen " + i);
                Assert.That(popup.SelectedMineralId, Is.Empty);
                yield return ClickCenter(Row(popup.PlayerRows, Copper).Button);
                yield return UiTestWait.Press(env.Keyboard, Key.X);
                yield return UiTestWait.Until(() => !popup.IsVisible, "close " + i);
                AssertClosedClean(popup);
            }
        }

        [UnityTest]
        public IEnumerator Storage_EmptyCargoAndStorage_KeepsBoxFlowWithoutPops()
        {
            var scene = default(StorageScene);
            yield return Load(s => scene = s);
            scene.Binder.Presenter.ToggleInteractionPanel();
            var popup = scene.View.StoragePopup;
            Assert.That(popup.PopCount, Is.Zero, "빈 화물·빈 보관은 광물 팝 생략");
            yield return UiTestWait.Delay(0.5f, "lid open");
            Assert.That(popup.BoxLid, Is.GreaterThan(0.5f), "상자 개봉 흐름은 유지");
            yield return UiTestWait.Until(() => popup.State == StoragePopupView.PopupState.Open, "empty open");
            Assert.That(popup.PlayerEmptyVisible, Is.True);
            Assert.That(popup.StorageEmptyVisible, Is.True);
            Assert.That(popup.PlayerTotalsString, Is.EqualTo("0종 · 0개 · 0kg"));
            AssertTextInside(popup, "Content/Controls/Preview", "미선택 안내");
            yield return Capture("empty-1920x1080");
            scene.Binder.ClosePanel();
            yield return UiTestWait.Until(() => !popup.IsVisible, "closed");
            AssertClosedClean(popup);
        }

        [UnityTest]
        public IEnumerator Storage_Resolutions_CardOnScreen_ColumnsAligned_NoOverlap_TextReadable()
        {
            var scene = default(StorageScene);
            yield return Load(s => scene = s);
            Add(Copper, 13);
            Add(Iron, 4);
            scene.Binder.Presenter.ToggleInteractionPanel();
            var popup = scene.View.StoragePopup;
            yield return UiTestWait.Until(() => popup.State == StoragePopupView.PopupState.Open, "open");
            scene.Binder.SelectMineral(Iron);
            scene.Binder.DepositQuantity(2);
            scene.Binder.SelectMineral(Copper);
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) })
            {
                yield return env.Resolution.Set(size.x, size.y);
                yield return null;
                Canvas.ForceUpdateCanvases();
                AssertInsideScreen(popup.CardRect, "card " + size);
                AssertInsideScreen((RectTransform)popup.WithdrawButton.transform, "withdraw " + size);
                AssertInsideScreen((RectTransform)popup.CloseButton.transform, "close " + size);

                // 두 목록의 열이 머리글과 같은 위치에 정렬된다.
                foreach (var list in new[] { "PlayerCargo", "StorageCargo" })
                {
                    var strip = popup.CardRect.Find("Content/Lists/" + list + "/ColumnHeader");
                    var rows = list == "PlayerCargo" ? popup.PlayerRows : popup.StorageRows;
                    var row = rows[0];
                    Assert.That(ScreenRect((RectTransform)row.transform.Find("Quantity")).xMax,
                        Is.EqualTo(ScreenRect((RectTransform)strip.Find("Col_수량")).xMax).Within(1f), list + " 수량 열 " + size);
                    Assert.That(ScreenRect((RectTransform)row.transform.Find("Weight")).xMax,
                        Is.EqualTo(ScreenRect((RectTransform)strip.Find("Col_무게")).xMax).Within(1f), list + " 무게 열 " + size);
                    var name = row.transform.Find("Name").GetComponent<TMP_Text>();
                    Assert.That(name.fontSize * ScreenScale(name.rectTransform), Is.GreaterThanOrEqualTo(14f - 0.01f), "행 글자 " + size);
                }

                var picker = ScreenRect((RectTransform)popup.CardRect.Find("Content/Controls/MineralPicker/CaptionButton"));
                var selection = ScreenRect((RectTransform)popup.CardRect.Find("Content/Controls/Selection"));
                var preview = ScreenRect((RectTransform)popup.CardRect.Find("Content/Controls/Preview"));
                var deposit = ScreenRect((RectTransform)popup.DepositButton.transform);
                var withdraw = ScreenRect((RectTransform)popup.WithdrawButton.transform);
                var lastQuick = ScreenRect((RectTransform)popup.QuickButtons[2].transform);
                Assert.That(picker.xMax, Is.LessThan(selection.xMin), "드롭다운과 선택 카드 " + size);
                Assert.That(lastQuick.xMax, Is.LessThan(preview.xMin), "수량 버튼과 미리보기 " + size);
                Assert.That(preview.xMax, Is.LessThan(deposit.xMin), "미리보기와 보관 버튼 " + size);
                Assert.That(deposit.xMax, Is.LessThan(withdraw.xMin), "보관과 꺼내기 " + size);
                AssertTextInside(popup, "Content/Controls/Preview", "미리보기 " + size);
                AssertTextInside(popup, "Content/Controls/Selection/Counts", "선택 수량 " + size);
                var label = popup.WithdrawButton.GetComponentInChildren<TMP_Text>();
                Assert.That(label.fontSize * ScreenScale(label.rectTransform), Is.GreaterThanOrEqualTo(12f), "버튼 글자 " + size);
                yield return Capture("open-" + size.x + "x" + size.y);
            }
        }

        [UnityTest]
        public IEnumerator Capture_Storage_Open_And_Close_Frames()
        {
            var scene = default(StorageScene);
            yield return Load(s => scene = s);
            Add(Copper, 13);
            Add(Iron, 4);
            scene.Binder.Presenter.ToggleInteractionPanel();
            var popup = scene.View.StoragePopup;
            popup.ManualTick = true;
            popup.HideImmediate();
            popup.Show();
            var i = 0;
            for (var t = 0f; t <= StorageBoxTimeline.OpenDuration + Frame; t += Frame)
            {
                yield return Capture("flow-open-" + (i++).ToString("D3"));
                popup.Tick(Frame);
            }

            Assert.That(popup.State, Is.EqualTo(StoragePopupView.PopupState.Open));
            scene.Binder.ClosePanel();
            i = 0;
            while (popup.IsVisible && i < 30)
            {
                yield return Capture("flow-close-" + (i++).ToString("D3"));
                popup.Tick(Frame);
            }

            Assert.That(popup.IsVisible, Is.False);
            popup.ManualTick = false;
            AssertClosedClean(popup);
        }

        // ================= helpers =================

        private IEnumerator Load(System.Action<StorageScene> found)
        {
            env = new UiTestEnvironment();
            yield return env.LoadIntegration();
            env.Save.InventoryService.SetMaximumCapacity(120f);
            var scene = new StorageScene(spawned);
            yield return scene.Prepare();
            found(scene);
        }

        private void Add(string id, int quantity)
        {
            env.Save.InventoryService.AddMineral(id, quantity);
        }

        private static int Visible(IReadOnlyList<StorageCargoRowView> rows)
        {
            var count = 0;
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].gameObject.activeSelf)
                {
                    count++;
                }
            }

            return count;
        }

        private static StorageCargoRowView Row(IReadOnlyList<StorageCargoRowView> rows, string id)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].gameObject.activeSelf && rows[i].MineralId == id)
                {
                    return rows[i];
                }
            }

            Assert.Fail("row not found: " + id);
            return null;
        }

        private static T Find<T>(StoragePopupView popup, string path) where T : Component
        {
            var target = popup.transform.Find(path);
            Assert.That(target, Is.Not.Null, path);
            var component = target.GetComponent<T>();
            Assert.That(component, Is.Not.Null, path);
            return component;
        }

        /// <summary>글자가 자기 칸 밖(이웃 버튼·프레임)으로 넘치지 않는다.</summary>
        private static void AssertTextInside(StoragePopupView popup, string path, string label)
        {
            var text = popup.CardRect.Find(path).GetComponent<TMP_Text>();
            text.ForceMeshUpdate();
            var rect = text.rectTransform.rect;
            var bounds = text.textBounds;
            Assert.That(bounds.max.x, Is.LessThanOrEqualTo(rect.xMax + 1f), label + " 오른쪽 넘침");
            Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(rect.xMin - 1f), label + " 왼쪽 넘침");
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(rect.yMin - 1f), label + " 아래 넘침");
        }

        private static void AssertClosedClean(StoragePopupView popup)
        {
            Assert.That(popup.State, Is.EqualTo(StoragePopupView.PopupState.Hidden));
            Assert.That(popup.gameObject.activeSelf, Is.False, "닫힌 뒤 입력 차단 영역이 남지 않는다");
            Assert.That(popup.AnyEffectVisible, Is.False, "남은 상자·광물·발광 없음");
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
                    Assert.That(hit.gameObject.transform.IsChildOf(popup.transform), Is.False, "보관함 팝업이 입력을 막는다: " + hit.gameObject.name);
                }
            }
        }

        private IEnumerator ClickCenter(Button button)
        {
            yield return UiTestWait.FocusGameView();
            Canvas.ForceUpdateCanvases();
            var position = ScreenRect((RectTransform)button.transform).center;
            Assert.That(button.IsInteractable(), Is.True, button.name + " is not interactable");
            var pointer = new PointerEventData(EventSystem.current) { position = position };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, button.name + " 아래에 레이캐스트 대상이 없다.");
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button),
                button.name + " 위를 다른 UI가 가리고 있다: " + hits[0].gameObject.name);

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

        /// <summary>실제 전진기지 코어·보관함 프리팹을 플레이어 옆에 설치하고 실제 브리지로 접근 상태를 만든다.</summary>
        private sealed class StorageScene
        {
            private readonly List<GameObject> spawned;
            private int sequence;

            public StorageScene(List<GameObject> spawned)
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
                Assert.That(View.StoragePopup, Is.Not.Null, "보관함 팝업이 바인딩 때 만들어진다");
                Origin = Player.transform.position;
                Spawn(DataIds.Buildings.OutpostCoreBasic, Vector3.zero);
                Spawn(DataIds.Buildings.StorageBasic, new Vector3(3f, 0f, 0f));
                yield return ReturnToStorage();
            }

            public IEnumerator ReturnToStorage()
            {
                MovePlayer(new Vector3(3f, 0f, 0f));
                Network.RequestRebuild();
                yield return UiTestWait.Until(() => Service != null
                    && Service.IsFacilityInteraction
                    && Service.InteractionFacilityBuildingId == DataIds.Buildings.StorageBasic, "near storage");
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
                var id = buildingId + "-b138-" + (++sequence).ToString("D3");
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
    }
}
