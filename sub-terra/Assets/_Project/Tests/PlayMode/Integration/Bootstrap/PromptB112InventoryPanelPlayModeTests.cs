using System.Collections;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SubTerra.App.Tests.PlayMode
{
    /// <summary>
    /// prompt-B 112: 실제 Integration Scene에서 재구성한 인벤토리 창의
    /// I 단축키·실시간 갱신·적재 상태·닫기/재열기·해상도별 배치를 확인하고 캡처를 남긴다.
    /// </summary>
    public sealed class PromptB112InventoryPanelPlayModeTests
    {
        private UiTestEnvironment environment;
        private Keyboard keyboard;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (environment != null) environment.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator InventoryPanel_LiveValuesStatesAndLayout()
        {
            yield return VerifyStatesAndInput(false);
        }

        [UnityTest, Category("Visual")]
        public IEnumerator InventoryPanel_StatesAndLayout_AtThreeResolutions()
        {
            yield return VerifyStatesAndInput(true);
        }

        private IEnumerator VerifyStatesAndInput(bool visual)
        {
            environment = new UiTestEnvironment();
            keyboard = environment.Keyboard;
            yield return environment.LoadIntegration();
            var save = environment.Save;

            var chrome = Object.FindAnyObjectByType<HudPanelChromeController>();
            var view = Object.FindAnyObjectByType<InventoryPanelView>(FindObjectsInactive.Include);
            Assert.That(chrome, Is.Not.Null);
            Assert.That(view, Is.Not.Null);
            var inventory = save.InventoryService;
            Assert.That(inventory, Is.Not.Null);

            // I 단축키로 연다.
            chrome.CloseInventoryPanel();
            yield return null;
            yield return PressI();
            Assert.That(chrome.IsInventoryPanelOpen, Is.True);
            Assert.That(view.PanelRoot.activeSelf, Is.True);

            var panel = view.PanelRoot.transform;
            var percent = Text(panel, "CargoCard/CargoPercentText");
            var stateLine = Text(panel, "CargoCard/CargoStateText");
            var value = Text(panel, "ValueCard/UnsettledAmountText");
            var owned = Text(panel, "OwnedKindsText");
            var rows = panel.Find("StackScroll/Viewport/StackRows");

            AssertMatchesService(inventory, percent, stateLine, value);
            Assert.That(owned.text, Is.EqualTo(InventoryPanelDisplayFormatter.OwnedKinds(0, 4)));
            Assert.That(Text(rows, "Stack_" + DataIds.Minerals.Copper + "/State").text, Is.EqualTo("보유 없음"));
            AssertInsideScreenAndClearOfHud(view);
            AssertNotTruncated(panel);
            var evidence = Path.GetFullPath("Temp/visual/prompt-b112");
            if (visual) Directory.CreateDirectory(evidence);
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "inventory-empty-1920x1080.png"));

            // 창이 열린 상태에서 자원이 바뀌면 즉시 반영되어야 한다.
            Assert.That(inventory.TryAddMineralExact(DataIds.Minerals.Copper, 4).DidChange, Is.True);
            Assert.That(inventory.TryAddMineralExact(DataIds.Minerals.Iron, 3).DidChange, Is.True);
            Assert.That(inventory.TryAddMineralExact(DataIds.RareItems.EngineFuel, 1).DidChange, Is.True);
            yield return null;
            AssertMatchesService(inventory, percent, stateLine, value);
            Assert.That(owned.text, Is.EqualTo(InventoryPanelDisplayFormatter.OwnedKinds(3, 4)));
            Assert.That(Text(rows, "Stack_" + DataIds.Minerals.Copper + "/Quantity").text, Does.Contain(">4<"));
            Assert.That(Text(rows, "Stack_" + DataIds.Minerals.Copper + "/State").text, Is.EqualTo("보유 중"));
            Assert.That(Text(rows, "Stack_" + DataIds.Minerals.Lithium + "/State").text, Is.EqualTo("보유 없음"));
            AssertNotTruncated(panel);
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "inventory-normal-1920x1080.png"));

            // 적재율 80% 이상: 색과 함께 "거의 가득" 문구·남은 용량이 표시된다.
            while (inventory.CurrentWeight / inventory.MaxCapacity < 0.85f
                && inventory.TryAddMineralExact(DataIds.Minerals.Iron, 1).DidChange)
            {
            }

            yield return null;
            AssertMatchesService(inventory, percent, stateLine, value);
            Assert.That(stateLine.text, Does.Contain("거의 가득").Or.Contain("가득 참"));
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "inventory-near-full-1920x1080.png"));

            // 가능한 만큼 채운다. 정확히 최대치에 닿으면 "가득 참"을 표시해야 한다.
            while (inventory.TryAddMineralExact(DataIds.Minerals.Iron, 1).DidChange)
            {
            }

            while (inventory.TryAddMineralExact(DataIds.RareItems.EngineFuel, 1).DidChange)
            {
            }

            yield return null;
            AssertMatchesService(inventory, percent, stateLine, value);
            if (inventory.CurrentWeight >= inventory.MaxCapacity - 0.0001f)
            {
                Assert.That(stateLine.text, Does.Contain("가득 참"));
                Assert.That(percent.text, Is.EqualTo("100%"));
            }

            AssertNotTruncated(panel);
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "inventory-full-1920x1080.png"));

            // 무게 도움말 hover 표시.
            var help = panel.Find("WeightHelpIcon").GetComponent<InventoryWeightTooltip>();
            help.OnPointerEnter(null);
            yield return null;
            Assert.That(help.TooltipRoot.activeSelf, Is.True);
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "inventory-weight-help-1920x1080.png"));
            help.OnPointerExit(null);

            if (visual)
            foreach (var resolution in new[] { new Vector2Int(2560, 1440), new Vector2Int(1366, 768), new Vector2Int(1920, 1080) })
            {
                yield return environment.Resolution.Set(resolution.x, resolution.y);
                Assert.That(Screen.width, Is.EqualTo(resolution.x));
                AssertInsideScreenAndClearOfHud(view);
                AssertNotTruncated(panel);
                if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, $"inventory-{resolution.x}x{resolution.y}.png"));
            }

            // I로 닫고, 닫힌 동안 자원이 줄어도 다시 열면 최신 값을 보여야 한다.
            yield return PressI();
            Assert.That(chrome.IsInventoryPanelOpen, Is.False);
            Assert.That(view.PanelRoot.activeSelf, Is.False);
            Assert.That(inventory.TryReduceMineral(DataIds.Minerals.Iron, 2).DidChange, Is.True);
            yield return null;
            chrome.ToggleInventoryPanel();
            yield return null;
            Assert.That(chrome.IsInventoryPanelOpen, Is.True);
            AssertMatchesService(inventory, percent, stateLine, value);

            // X 버튼 닫기.
            yield return UiTestWait.Click(environment.Mouse, view.CloseButton);
            yield return null;
            Assert.That(chrome.IsInventoryPanelOpen, Is.False);
            Assert.That(save.ActiveSlot, Is.Zero);
        }

        private static void AssertMatchesService(InventoryService inventory, TMP_Text percent, TMP_Text stateLine, TMP_Text value)
        {
            Assert.That(percent.text, Is.EqualTo(
                InventoryPanelDisplayFormatter.Percent(inventory.CurrentWeight, inventory.MaxCapacity)));
            Assert.That(stateLine.text, Is.EqualTo(
                InventoryPanelDisplayFormatter.StateLine(inventory.CurrentWeight, inventory.MaxCapacity)));
            Assert.That(value.text, Is.EqualTo(InventoryPanelDisplayFormatter.UnsettledAmount(inventory.UnsettledValue)));
        }

        private static TMP_Text Text(Transform root, string path)
        {
            var found = root.Find(path);
            Assert.That(found, Is.Not.Null, path);
            return found.GetComponent<TMP_Text>();
        }

        private IEnumerator PressI() => UiTestWait.Press(keyboard, Key.I);

        private static void AssertNotTruncated(Transform panel)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(false))
            {
                text.ForceMeshUpdate();
                Assert.That(text.isTextTruncated, Is.False, text.name + " truncated: " + text.text);
            }
        }

        private static void AssertInsideScreenAndClearOfHud(InventoryPanelView view)
        {
            var rect = (RectTransform)view.transform;
            var panel = ScreenRect(rect);
            Assert.That(panel.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(0f), "인벤토리 하단이 화면 밖입니다.");
            Assert.That(panel.xMax, Is.LessThanOrEqualTo(Screen.width));
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(Screen.height));

            var quest = GameObject.Find("QuestSummaryButton");
            if (quest != null && quest.activeInHierarchy)
            {
                Assert.That(panel.Overlaps(ScreenRect((RectTransform)quest.transform)), Is.False, "퀘스트 카드와 겹칩니다.");
            }

            var building = Object.FindAnyObjectByType<SubTerra.App.UI.Building.BuildingMenuView>();
            var buildingPanel = building != null ? building.transform.Find("PanelRoot") : null;
            if (buildingPanel != null && buildingPanel.gameObject.activeInHierarchy)
            {
                Assert.That(panel.Overlaps(ScreenRect((RectTransform)building.transform)), Is.False, "시설 건설창과 겹칩니다.");
            }

            var side = Object.FindAnyObjectByType<GameplaySideMenuController>();
            var open = side != null ? side.transform.Find("OpenMenuRoot") as RectTransform : null;
            if (open != null && open.gameObject.activeInHierarchy)
            {
                Assert.That(panel.Overlaps(ScreenRect(open)), Is.False, "우측 메뉴와 겹칩니다.");
            }
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

    }
}
