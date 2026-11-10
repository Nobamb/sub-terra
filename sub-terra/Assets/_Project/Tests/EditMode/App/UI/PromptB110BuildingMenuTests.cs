using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.Building;
using SubTerra.Shared;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 110: 시설 건설창 재구성 구조와 표시 규칙.</summary>
    public sealed class PromptB110BuildingMenuTests
    {
        private const string MenuPath = "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        [Test]
        public void WorldRightClick_StartsAndCancelsMoveWithoutOpeningBuildingMenu()
        {
            var host = new GameObject("RightClickMove");
            host.SetActive(false);
            var prefab = new GameObject("StorageTemplate");
            var definition = ScriptableObject.CreateInstance<SubTerra.Gameplay.Building.BuildingPlacementDefinition>();
            try
            {
                definition.EditorSet(DataIds.Buildings.StorageBasic, prefab, Vector2Int.one, false);
                var system = host.AddComponent<SubTerra.Gameplay.Building.BuildingPlacementSystem>();
                var systemSettings = new SerializedObject(system);
                var definitions = systemSettings.FindProperty("restoreDefinitions");
                definitions.arraySize = 1;
                definitions.GetArrayElementAtIndex(0).objectReferenceValue = definition;
                systemSettings.ApplyModifiedPropertiesWithoutUndo();
                var bridge = host.AddComponent<SubTerra.App.Integration.GameplayBuildingPlacementBridge>();
                var bridgeSettings = new SerializedObject(bridge);
                bridgeSettings.FindProperty("placementSystem").objectReferenceValue = system;
                bridgeSettings.ApplyModifiedPropertiesWithoutUndo();
                host.SetActive(true);
                Assert.That(system.TryRestoreBuilding(new BuildingSnapshotDto
                {
                    instanceId = "storage.right-click", buildingTypeId = DataIds.Buildings.StorageBasic,
                    footprintWidth = 1, footprintHeight = 1, level = 2, health = 0.75f
                }), Is.True);
                var instance = host.GetComponentInChildren<SubTerra.Gameplay.Building.BuildingInstance>();
                var original = instance.transform.position;
                Assert.That(system.TryGetMovableAt(Vector3.zero, out var hoverTarget, out var ground), Is.True);
                Assert.That(hoverTarget, Is.SameAs(instance));
                Assert.That(ground.x, Is.EqualTo(original.x).Within(0.001f));
                Assert.That(ground.y, Is.EqualTo(-0.5f).Within(0.001f), "단위 셀의 바닥은 중심보다 반 칸 아래다.");
                Assert.That(system.Selection, Is.Null, "호버 조회는 이동 선택을 시작하지 않는다.");
                Assert.That(system.IsMoving, Is.False);
                Assert.That(instance.transform.position, Is.EqualTo(original));
                instance.gameObject.SetActive(false);
                Assert.That(system.TryGetMovableAt(Vector3.zero, out _, out _), Is.False);
                instance.gameObject.SetActive(true);
                Assert.That(bridge.HandleWorldRightClick(new Vector3(20f, 20f, 0f)), Is.False);
                Assert.That(bridge.HandleWorldRightClick(Vector3.zero), Is.True);
                Assert.That(system.IsMoving, Is.True);
                Assert.That(bridge.HandleWorldRightClick(Vector3.zero), Is.True);
                Assert.That(system.IsMoving, Is.False);
                Assert.That(system.Selection, Is.Null);
                Assert.That(instance.transform.position, Is.EqualTo(original));
                Assert.That(instance.InstanceId, Is.EqualTo("storage.right-click"));
                Assert.That(bridge.HandleWorldRightClick(Vector3.zero), Is.True);
                Assert.That(system.TryMoveAt(new Vector3Int(2, 0, 0)).IsSuccess, Is.True);
                Assert.That(bridge.IsMoveMode, Is.False);
                Assert.That(instance.InstanceId, Is.EqualTo("storage.right-click"));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void Issue216_BuildingMenuDoesNotAddMoveButtonOrReserveLayoutSpace()
        {
            var host = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath));
            try
            {
                var view = host.GetComponent<BuildingMenuView>();
                host.SetActive(true);
                Assert.That(host.GetComponentsInChildren<Transform>(true).Any(t => t.name == "MoveFacilityButton"), Is.False);
                Assert.That(host.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text == "이동 (우클릭)"), Is.False);
                Assert.That(view.HasStructuredReferences(), Is.True);
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void Prefab_HasStructuredReferencesAndKeepsLegacyFields()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            var view = menu.GetComponent<BuildingMenuView>();
            Assert.That(view.HasRequiredReferences(), Is.True);
            Assert.That(view.HasStructuredReferences(), Is.True);
            Assert.That(view.CloseButton, Is.Not.Null);
            Assert.That(view.CloseButton.transform.parent, Is.SameAs(menu.transform));
        }

        [Test]
        public void Prefab_HasOneVisualEntryPerCatalogBuilding()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            var ids = menu.GetComponentsInChildren<BuildingMenuEntryVisual>(true)
                .Select(v => v.BuildingId)
                .ToList();
            var catalogIds = catalog.Buildings.Where(b => b != null).Select(b => b.Id).ToList();

            Assert.That(ids, Is.Unique);
            Assert.That(ids, Is.EquivalentTo(catalogIds));
            foreach (var visual in menu.GetComponentsInChildren<BuildingMenuEntryVisual>(true))
            {
                Assert.That(visual.GetComponent<BuildingMenuEntryButton>(), Is.Not.Null, visual.name);
                Assert.That(visual.HasRequiredReferences(), Is.True, visual.name);
                Assert.That(visual.IsSelected, Is.False, visual.name + " 기본 상태는 미선택");
            }
        }

        [Test]
        public void Prefab_RailIconsStackWithoutOverlapInsideRail()
        {
            // B-188: 시설 아이콘은 왼쪽 레일 안에 세로로 겹치지 않게 쌓이고, 맨 아래에 `>>`가 온다.
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            var rail = (RectTransform)Find(menu.transform, "Rail");
            var rects = menu.GetComponentsInChildren<BuildingMenuEntryVisual>(true)
                .Select(v => (RectTransform)v.transform)
                .OrderByDescending(r => r.anchoredPosition.y)
                .ToList();

            Assert.That(rects[0].anchoredPosition.y, Is.EqualTo(PromptB188BuildingModeBuilder.IconY(0)).Within(0.1f));
            for (var i = 0; i < rects.Count; i++)
            {
                Assert.That(rects[i].parent, Is.SameAs(rail), rects[i].name);
                Assert.That(rects[i].anchoredPosition.x, Is.EqualTo(PromptB188BuildingModeBuilder.Padding).Within(0.1f));
                Assert.That(rects[i].sizeDelta, Is.EqualTo(Vector2.one * PromptB188BuildingModeBuilder.IconSize));
                if (i > 0)
                {
                    var previousBottom = rects[i - 1].anchoredPosition.y - rects[i - 1].sizeDelta.y;
                    Assert.That(rects[i].anchoredPosition.y, Is.LessThanOrEqualTo(previousBottom), rects[i].name);
                }
            }

            var expand = (RectTransform)rail.Find("ExpandButton");
            var last = rects[rects.Count - 1];
            Assert.That(expand.anchoredPosition.y, Is.LessThan(last.anchoredPosition.y - last.sizeDelta.y));
            Assert.That(-expand.anchoredPosition.y + expand.sizeDelta.y, Is.LessThanOrEqualTo(rail.sizeDelta.y));
            Assert.That(rail.sizeDelta.y, Is.LessThanOrEqualTo(560f), "Scene 배치(16,-360) 아래 1080 안에 들어간다.");
        }

        [Test]
        public void Prefab_CardAndCatalogStayRightOfRail()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            var rail = (RectTransform)Find(menu.transform, "Rail");
            var railRight = rail.anchoredPosition.x + rail.sizeDelta.x;
            foreach (var name in new[] { "SidePane", "CatalogWindow" })
            {
                var rect = (RectTransform)Find(menu.transform, name);
                Assert.That(rect.anchoredPosition.x, Is.GreaterThan(railRight), name);
                Assert.That(rect.anchoredPosition.y, Is.EqualTo(rail.anchoredPosition.y).Within(0.1f), name);
            }

            // 카드 읽는 순서: 아이콘·시설명 → 용도 → 운영 조건 → 건설 비용 → 설치 상태.
            var card = Find(menu.transform, "Card");
            var order = new[] { "CardHeader", "SelectionText", "DetailOperatingCondition", "CostSection", "AvailabilityBanner" }
                .Select(n => card.Find(n).GetSiblingIndex())
                .ToList();
            Assert.That(order, Is.Ordered.Ascending);
            Assert.That(card.Find("CardHeader/DetailName"), Is.Not.Null);
            Assert.That(Find(menu.transform, "AvailabilityText").parent.name, Is.EqualTo("AvailabilityBanner"));
        }

        [Test]
        public void Prefab_OnlyRailCardAndCatalogReceiveClicks()
        {
            // B-188: 빈 PanelRoot(500×560)가 지형 클릭을 삼키지 않고, 레일·카드·큰 창만 입력을 받는다.
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            // B-191: 정렬 드롭다운의 펼친 목록(Template·항목 배경)도 큰 창 안에서 클릭을 받는다.
            var allowed = new[]
            {
                "Rail", "HintChip", "Card", "StatusChip", "CatalogWindow", "SearchField", "Viewport", "Scrollbar", "Handle",
                "Template", "Item Background"
            };
            foreach (var graphic in menu.GetComponentsInChildren<Graphic>(true))
            {
                var isButton = graphic.GetComponent<Selectable>() != null;
                if (!isButton && !allowed.Contains(graphic.name))
                {
                    Assert.That(graphic.raycastTarget, Is.False, graphic.name);
                }
            }

            var panelImage = menu.transform.Find("PanelRoot").GetComponent<Image>();
            Assert.That(panelImage == null || !panelImage.enabled || !panelImage.raycastTarget, Is.True);
            Assert.That(menu.transform.Find("PanelRoot").GetComponent<SubTerra.App.UI.PopupWindowDrag>(), Is.Null);
        }

        [Test]
        public void Formatter_ClassifiesAvailabilityWithTextAndGlyph()
        {
            var idle = new BuildingAvailabilityReadModel(BuildingPlacementState.None, false, string.Empty);
            var ready = new BuildingAvailabilityReadModel(BuildingPlacementState.Valid, true, string.Empty);
            var poor = new BuildingAvailabilityReadModel(BuildingPlacementState.Valid, false, "자원이 부족합니다.");
            var blocked = new BuildingAvailabilityReadModel(BuildingPlacementState.Invalid, true, "시설을 지지할 지면이 없습니다.");

            Assert.That(BuildingMenuDisplayFormatter.Classify(idle), Is.EqualTo(BuildingAvailabilityDisplayKind.Idle));
            Assert.That(BuildingMenuDisplayFormatter.Classify(ready), Is.EqualTo(BuildingAvailabilityDisplayKind.Ready));
            Assert.That(BuildingMenuDisplayFormatter.Classify(poor), Is.EqualTo(BuildingAvailabilityDisplayKind.NeedResources));
            Assert.That(BuildingMenuDisplayFormatter.Classify(blocked), Is.EqualTo(BuildingAvailabilityDisplayKind.CheckPlacement));

            // 색상 외에도 제목·기호·사유 문구가 서로 달라야 한다.
            var kinds = new[]
            {
                BuildingAvailabilityDisplayKind.Idle, BuildingAvailabilityDisplayKind.Ready,
                BuildingAvailabilityDisplayKind.NeedResources, BuildingAvailabilityDisplayKind.CheckPlacement
            };
            Assert.That(kinds.Select(BuildingMenuDisplayFormatter.Title), Is.Unique);
            Assert.That(kinds.Select(BuildingMenuDisplayFormatter.Glyph), Is.Unique);
            Assert.That(BuildingMenuDisplayFormatter.AvailabilityText(BuildingAvailabilityDisplayKind.CheckPlacement, blocked.Message),
                Does.Contain("지면이 없습니다"));
            // B-188: 설치 가능 옆에는 좌클릭·C·B 조작 안내를 붙이지 않는다.
            Assert.That(BuildingMenuDisplayFormatter.Detail(BuildingAvailabilityDisplayKind.Ready, string.Empty), Is.Empty);
            Assert.That(BuildingMenuDisplayFormatter.AvailabilityText(BuildingAvailabilityDisplayKind.Ready, string.Empty),
                Is.EqualTo("<b>설치 가능</b>"));
            Assert.That(BuildingMenuDisplayFormatter.AvailabilityText(BuildingAvailabilityDisplayKind.CheckPlacement, string.Empty),
                Does.Contain("설치 가능한 위치로 커서를 옮기세요"));
        }

        [Test]
        public void Formatter_MarksOnlyMissingCosts()
        {
            var costs = new[]
            {
                new BuildingCostReadModel(DataIds.Minerals.Copper, 5, 6),
                new BuildingCostReadModel(DataIds.Minerals.Iron, 5, 2)
            };

            var summary = BuildingMenuDisplayFormatter.CostSummary(costs);
            Assert.That(summary, Does.StartWith("구리 5"));
            Assert.That(summary, Does.Contain("<color=" + BuildingMenuDisplayFormatter.MissingColorHex + ">철 5</color>"));
            Assert.That(BuildingMenuDisplayFormatter.HasAllCosts(costs), Is.False);
            Assert.That(BuildingMenuDisplayFormatter.EntryStateLabel(costs), Is.EqualTo("부족"));
            Assert.That(BuildingMenuDisplayFormatter.CostState(costs[0]), Is.EqualTo("충분"));
            Assert.That(BuildingMenuDisplayFormatter.CostState(costs[1]), Is.EqualTo("3 부족"));
            // B-188: `보유 / 필요` 순서, 부족한 행만 보유 수량을 경고색으로.
            Assert.That(BuildingMenuDisplayFormatter.CostAmount(costs[0]), Is.EqualTo("<b>6</b> / 5"));
            Assert.That(BuildingMenuDisplayFormatter.CostAmount(costs[1]),
                Is.EqualTo("<color=" + BuildingMenuDisplayFormatter.MissingColorHex + "><b>2</b></color> / 5"));
            Assert.That(BuildingMenuDisplayFormatter.CostFill(costs[0]), Is.EqualTo(1f));
            Assert.That(BuildingMenuDisplayFormatter.CostFill(costs[1]), Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(BuildingMenuDisplayFormatter.ShortageDetail(costs), Is.EqualTo("철 3개가 더 필요합니다."));
            Assert.That(BuildingMenuDisplayFormatter.ShortageDetail(new[] { costs[0] }), Is.Empty);
        }

        [Test]
        public void View_UpdatesEntriesDetailAndCostsFromReadModels()
        {
            var menu = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath));
            try
            {
                var view = menu.GetComponent<BuildingMenuView>();
                var panel = menu.transform;
                var item = new BuildingMenuItemReadModel(
                    DataIds.Buildings.OutpostCoreBasic,
                    "전진기지",
                    "설명",
                    null,
                    5,
                    new[]
                    {
                        new BuildingCostReadModel(DataIds.Minerals.Copper, 5, 5),
                        new BuildingCostReadModel(DataIds.Minerals.Iron, 5, 0)
                    });

                view.SetBuildingList(new[] { item });
                view.SetSelection(item);

                var entry = Find(panel, "Select_" + DataIds.Buildings.OutpostCoreBasic)
                    .GetComponent<BuildingMenuEntryVisual>();
                Assert.That(entry.IsSelected, Is.True);
                Assert.That(entry.transform.Find("EntryState").GetComponent<TMP_Text>().text, Is.EqualTo("부족"));
                Assert.That(Find(panel, "DetailName").GetComponent<TMP_Text>().text, Is.EqualTo("전진기지"));
                Assert.That(Find(panel, "DetailPowerChip"), Is.Null);
                Assert.That(Find(panel, "DetailOperatingCondition").gameObject.activeSelf, Is.False);
                Assert.That(Find(panel, "CostSection").gameObject.activeSelf, Is.True);
                Assert.That(Find(panel, "CostRow_1").gameObject.activeSelf, Is.True);
                Assert.That(Find(panel, "CostRow_2").gameObject.activeSelf, Is.False);
                Assert.That(Find(panel, "Card").gameObject.activeSelf, Is.True);
                Assert.That(Find(panel, "HintChip").gameObject.activeSelf, Is.False);

                view.SetAvailability(new BuildingAvailabilityReadModel(BuildingPlacementState.Previewing, false, "자원이 부족합니다."));
                var availabilityText = Find(panel, "AvailabilityText").GetComponent<TMP_Text>().text;
                Assert.That(availabilityText, Does.Contain("자원 부족"));
                Assert.That(availabilityText, Does.Contain("철 5개가 더 필요합니다."));

                view.ClearSelection();
                Assert.That(entry.IsSelected, Is.False);
                Assert.That(Find(panel, "CostSection").gameObject.activeSelf, Is.False);
                Assert.That(Find(panel, "DetailOperatingCondition").gameObject.activeSelf, Is.False);
                // 미선택: 카드 대신 짧은 힌트만.
                Assert.That(Find(panel, "Card").gameObject.activeSelf, Is.False);
                Assert.That(Find(panel, "HintChip").gameObject.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(menu);
            }
        }

        [Test]
        public void View_ShowsCoreAreaConditionOnlyForRestrictedFacilitiesAndClearsIt()
        {
            var menu = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath));
            try
            {
                var view = menu.GetComponent<BuildingMenuView>();
                var panel = menu.transform;
                var condition = Find(panel, "DetailOperatingCondition").GetComponent<TMP_Text>();
                var header = Find(panel, "CardHeader");
                // B-188: 운영 조건은 카드 세로 순서에서 이름 줄 아래(용도 다음)에 온다.
                Assert.That(condition.transform.parent, Is.SameAs(header.parent));
                Assert.That(condition.transform.GetSiblingIndex(), Is.GreaterThan(header.GetSiblingIndex()));
                Assert.That(condition.raycastTarget, Is.False);

                var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
                foreach (var data in catalog.Buildings.Where(b => b != null))
                {
                    var restricted = data.Id == DataIds.Buildings.ChargerBasic
                        || data.Id == DataIds.Buildings.ClinicBasic
                        || data.Id == DataIds.Buildings.SettlementBasic;
                    view.SetSelection(new BuildingMenuItemReadModel(
                        data.Id, data.DisplayName, data.Description, data.Icon, data.PowerDraw, null));
                    Assert.That(condition.gameObject.activeSelf, Is.EqualTo(restricted), data.Id);
                    Assert.That(condition.text, Is.EqualTo(restricted
                        ? "전진기지 범위 내"
                        : string.Empty), data.Id);
                }

                view.ClearSelection();
                Assert.That(condition.gameObject.activeSelf, Is.False);
                Assert.That(condition.text, Is.Empty);
                Assert.That(Find(panel, "SelectionText").GetComponent<TMP_Text>().text, Does.Not.Contain("전력"));
            }
            finally
            {
                Object.DestroyImmediate(menu);
            }
        }

        [Test]
        public void LegacyView_ShowsConditionBelowNameWithoutPowerConsumption()
        {
            var host = new GameObject("LegacyBuildingMenu", typeof(RectTransform));
            try
            {
                var view = host.AddComponent<BuildingMenuView>();
                var textObject = new GameObject("SelectionText", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(host.transform, false);
                var text = textObject.GetComponent<TMP_Text>();
                var so = new SerializedObject(view);
                so.FindProperty("selectionText").objectReferenceValue = text;
                so.ApplyModifiedPropertiesWithoutUndo();

                view.SetSelection(new BuildingMenuItemReadModel(
                    DataIds.Buildings.ChargerBasic, "충전기", "설명", null, 3, null));
                Assert.That(text.text.Replace("\r\n", "\n"), Does.StartWith(
                    "충전기\n전진기지 범위 내\n설명"));
                Assert.That(text.text, Does.Not.Contain("전력 소비"));

                view.SetSelection(new BuildingMenuItemReadModel(
                    DataIds.Buildings.OutpostCoreBasic, "전진기지", "설명", null, 5, null));
                Assert.That(text.text, Does.Not.Contain("범위 내"));
                Assert.That(text.text, Does.Not.Contain("전력 소비"));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static Transform Find(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        }
    }
}
