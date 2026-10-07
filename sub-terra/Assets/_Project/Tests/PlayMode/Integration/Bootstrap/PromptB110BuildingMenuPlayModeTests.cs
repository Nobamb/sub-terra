using System.Collections;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Building;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SubTerra.App.Tests.PlayMode
{
    /// <summary>
    /// prompt-B 110: 실제 Integration Scene에서 재구성한 시설 건설창의
    /// 선택·비용 갱신·상태 배너·B 단축키·해상도별 배치를 확인하고 캡처를 남긴다.
    /// </summary>
    public sealed class PromptB110BuildingMenuPlayModeTests
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
        public IEnumerator BuildingMenu_SelectionCostsAvailabilityAndLayout()
        {
            yield return VerifyStatesAndInput(false);
        }

        [UnityTest, Category("Visual")]
        public IEnumerator BuildingMenu_StatesAndLayout_AtThreeResolutions()
        {
            yield return VerifyStatesAndInput(true);
        }

        [UnityTest]
        public IEnumerator BuildingMenu_ConstructingPoweredFacilityDoesNotSpendPlayerPower()
        {
            environment = new UiTestEnvironment();
            yield return environment.LoadIntegration();
            var state = GameBootstrapper.Instance.State;
            var inventory = environment.Save.InventoryService;
            Assert.That(inventory.TryAddMineralExact(DataIds.Minerals.Copper, 10).DidChange, Is.True);
            var chrome = Object.FindAnyObjectByType<HudPanelChromeController>();
            chrome.OpenBuildingMenu();
            yield return null;
            var view = Object.FindAnyObjectByType<BuildingMenuView>();
            var binder = view.GetComponent<BuildingMenuBinder>();
            Assert.That(binder.SelectBuilding(DataIds.Buildings.ChargerBasic), Is.True);
            var placement = Object.FindAnyObjectByType<BuildingPlacementSystem>();
            Assert.That(placement.TryFindBestPlacementCell(1f, out var cell, out var failure), Is.True, failure.ToString());
            var energyBefore = state.Player.Energy;
            var copperBefore = inventory.State.GetQuantity(DataIds.Minerals.Copper);
            var copperCost = placement.Selection.Costs[0].Quantity;
            var result = placement.TryPlaceAt(cell);

            Assert.That(result.IsSuccess, Is.True, result.Failure.ToString());
            Assert.That(state.Player.Energy, Is.EqualTo(energyBefore), "건설 자체는 플레이어 전력을 차감하지 않는다.");
            Assert.That(inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(copperBefore - copperCost));
            Assert.That(view.transform.Find("PanelRoot/DetailOperatingCondition").gameObject.activeSelf, Is.False);
            Assert.That(environment.Save.ActiveSlot, Is.Zero);
        }

        private IEnumerator VerifyStatesAndInput(bool visual)
        {
            environment = new UiTestEnvironment();
            keyboard = environment.Keyboard;
            yield return environment.LoadIntegration();
            var save = environment.Save;

            var chrome = Object.FindAnyObjectByType<HudPanelChromeController>();
            Assert.That(chrome, Is.Not.Null);
            chrome.OpenBuildingMenu();
            yield return null;
            var view = Object.FindAnyObjectByType<BuildingMenuView>();
            var binder = view.GetComponent<BuildingMenuBinder>();
            Assert.That(view, Is.Not.Null);
            Assert.That(binder.IsBound, Is.True);
            Assert.That(view.HasStructuredReferences(), Is.True);

            var panel = view.transform.Find("PanelRoot");
            var detailName = panel.Find("DetailName").GetComponent<TMP_Text>();
            var availability = panel.Find("AvailabilityText").GetComponent<TMP_Text>();
            var costSection = panel.Find("CostSection").gameObject;
            Assert.That(detailName.text, Is.EqualTo("시설 미선택"));
            Assert.That(costSection.activeSelf, Is.False);

            var evidence = Path.GetFullPath("Temp/visual/prompt-b110");
            if (visual) Directory.CreateDirectory(evidence);
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "building-idle-1920x1080.png"));

            // 버팀목 선택: 초기 인벤토리는 비어 있어 자원 부족이 문구·기호와 함께 보여야 한다.
            var support = Entry(panel, DataIds.Buildings.SupportBasic);
            yield return UiTestWait.Click(environment.Mouse, support.GetComponent<Button>());
            yield return null;
            Assert.That(binder.Presenter.SelectedBuildingId, Is.EqualTo(DataIds.Buildings.SupportBasic));
            Assert.That(support.GetComponent<BuildingMenuEntryVisual>().IsSelected, Is.True);
            AssertOnlySelected(panel, DataIds.Buildings.SupportBasic);
            Assert.That(detailName.text, Is.Not.EqualTo("시설 미선택"));
            Assert.That(costSection.activeSelf, Is.True);
            Assert.That(availability.text, Does.Contain("자원 부족"));
            Assert.That(panel.Find("AvailabilityBanner/AvailabilityBadge/AvailabilityGlyph")
                .GetComponent<TMP_Text>().text, Is.EqualTo(BuildingMenuDisplayFormatter.BlockedGlyph));
            AssertInsideScreenAndClearOfHud(view);
            AssertNotTruncated(panel);
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "building-need-resources-1920x1080.png"));

            // 자원을 얻으면 비용 행·목록 상태·설치 배너가 즉시 바뀌어야 한다.
            var added = save.InventoryService.TryAddMineralExact(DataIds.Minerals.Copper, 6);
            Assert.That(added.DidChange, Is.True, "copper add");
            yield return null;
            var row0 = panel.Find("CostSection/CostRow_0/State").GetComponent<TMP_Text>();
            Assert.That(row0.text, Does.Contain("충분"));
            Assert.That(support.Find("EntryState").GetComponent<TMP_Text>().text,
                Is.EqualTo(BuildingMenuDisplayFormatter.ReadyGlyph));
            Assert.That(availability.text, Does.Not.Contain("자원 부족"));
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "building-affordable-1920x1080.png"));

            // 비용 2종 중 일부만 부족한 시설: 부족한 광물만 경고 표시.
            var core = Entry(panel, DataIds.Buildings.OutpostCoreBasic);
            core.GetComponent<Button>().onClick.Invoke();
            yield return null;
            AssertOnlySelected(panel, DataIds.Buildings.OutpostCoreBasic);
            Assert.That(panel.Find("CostSection/CostRow_0").gameObject.activeSelf, Is.True);
            Assert.That(panel.Find("CostSection/CostRow_1").gameObject.activeSelf, Is.True);
            Assert.That(panel.Find("CostSection/CostRow_2").gameObject.activeSelf, Is.False);
            Assert.That(panel.Find("CostSection/CostRow_1/State").GetComponent<TMP_Text>().text, Does.Contain("부족"));
            Assert.That(availability.text, Does.Contain("자원 부족"));
            AssertNotTruncated(panel);
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "building-partial-1920x1080.png"));

            var condition = panel.Find("DetailOperatingCondition").GetComponent<TMP_Text>();
            Assert.That(panel.Find("DetailPowerChip"), Is.Null);
            foreach (var id in new[]
            {
                DataIds.Buildings.ChargerBasic, DataIds.Buildings.ClinicBasic, DataIds.Buildings.SettlementBasic
            })
            {
                Entry(panel, id).GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.That(condition.gameObject.activeSelf, Is.True, id);
                Assert.That(condition.text, Is.EqualTo("전진기지 코어 영역 내에서만 작동합니다."), id);
                AssertNotTruncated(panel);
            }

            if (visual)
            foreach (var resolution in new[] { new Vector2Int(2560, 1440), new Vector2Int(1366, 768), new Vector2Int(1920, 1080) })
            {
                yield return environment.Resolution.Set(resolution.x, resolution.y);
                Assert.That(Screen.width, Is.EqualTo(resolution.x));
                AssertInsideScreenAndClearOfHud(view);
                AssertNotTruncated(panel);
                if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, $"building-selected-{resolution.x}x{resolution.y}.png"));
            }

            core.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(condition.gameObject.activeSelf, Is.False);

            // B 단축키: 닫으면 선택이 취소되고, 다시 열면 미선택 상태로 시작한다.
            yield return PressB();
            Assert.That(chrome.IsBuildingMenuOpen, Is.False);
            Assert.That(binder.Presenter.SelectedBuildingId, Is.Empty);
            yield return PressB();
            Assert.That(chrome.IsBuildingMenuOpen, Is.True);
            Assert.That(detailName.text, Is.EqualTo("시설 미선택"));
            Assert.That(condition.gameObject.activeSelf, Is.False);
            AssertOnlySelected(panel, string.Empty);

            // X 버튼 닫기.
            yield return UiTestWait.Click(environment.Mouse, view.CloseButton);
            yield return null;
            Assert.That(chrome.IsBuildingMenuOpen, Is.False);
            Assert.That(save.ActiveSlot, Is.Zero);
        }

        private IEnumerator PressB() => UiTestWait.Press(keyboard, Key.B);

        private static Transform Entry(Transform panel, string buildingId)
        {
            var entry = panel.Find("Select_" + buildingId);
            Assert.That(entry, Is.Not.Null, buildingId);
            return entry;
        }

        private static void AssertOnlySelected(Transform panel, string buildingId)
        {
            foreach (var visual in panel.GetComponentsInChildren<BuildingMenuEntryVisual>(true))
            {
                Assert.That(visual.IsSelected, Is.EqualTo(visual.BuildingId == buildingId), visual.name);
            }
        }

        private static void AssertNotTruncated(Transform panel)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(false))
            {
                text.ForceMeshUpdate();
                Assert.That(text.isTextTruncated, Is.False, text.name + " truncated: " + text.text);
            }
        }

        private static void AssertInsideScreenAndClearOfHud(BuildingMenuView view)
        {
            var rect = (RectTransform)view.transform;
            var menu = ScreenRect(rect);
            Assert.That(menu.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(menu.yMin, Is.GreaterThanOrEqualTo(0f), "건설창 하단이 화면 밖입니다.");
            Assert.That(menu.xMax, Is.LessThanOrEqualTo(Screen.width));
            Assert.That(menu.yMax, Is.LessThanOrEqualTo(Screen.height));

            var quest = GameObject.Find("QuestSummaryButton");
            if (quest != null && quest.activeInHierarchy)
            {
                Assert.That(menu.Overlaps(ScreenRect((RectTransform)quest.transform)), Is.False, "퀘스트 카드와 겹칩니다.");
            }

            var side = Object.FindAnyObjectByType<GameplaySideMenuController>();
            var open = side != null ? side.transform.Find("OpenMenuRoot") as RectTransform : null;
            if (open != null && open.gameObject.activeInHierarchy)
            {
                Assert.That(menu.Overlaps(ScreenRect(open)), Is.False, "우측 메뉴와 겹칩니다.");
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
