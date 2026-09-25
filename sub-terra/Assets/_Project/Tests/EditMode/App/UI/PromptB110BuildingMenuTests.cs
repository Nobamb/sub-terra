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
        public void Prefab_EntriesStackWithoutOverlapInsidePanel()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            var rects = menu.GetComponentsInChildren<BuildingMenuEntryVisual>(true)
                .Select(v => (RectTransform)v.transform)
                .OrderByDescending(r => r.anchoredPosition.y)
                .ToList();

            Assert.That(rects[0].anchoredPosition.y, Is.EqualTo(PromptB110BuildingMenuBuilder.FirstEntryY).Within(0.1f));
            for (var i = 0; i < rects.Count; i++)
            {
                Assert.That(rects[i].anchoredPosition.x, Is.EqualTo(PromptB110BuildingMenuBuilder.LeftColumnX).Within(0.1f));
                Assert.That(rects[i].sizeDelta.y, Is.EqualTo(PromptB110BuildingMenuBuilder.EntryHeight).Within(0.1f));
                if (i > 0)
                {
                    var previousBottom = rects[i - 1].anchoredPosition.y - rects[i - 1].sizeDelta.y;
                    Assert.That(rects[i].anchoredPosition.y, Is.LessThanOrEqualTo(previousBottom), rects[i].name);
                }
            }

            var last = rects[rects.Count - 1];
            Assert.That(-last.anchoredPosition.y + last.sizeDelta.y,
                Is.LessThanOrEqualTo(PromptB110BuildingMenuBuilder.PanelHeight - 16f));
        }

        [Test]
        public void Prefab_DetailColumnStaysRightOfListAndInsidePanel()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            var panel = menu.transform.Find("PanelRoot");
            var names = new[]
            {
                "DetailIconFrame", "DetailName", "DetailPowerChip", "SelectionText", "CostSection",
                "AvailabilityBanner", "AvailabilityText", "StatusText", "ControlsHint"
            };
            var listRight = PromptB110BuildingMenuBuilder.LeftColumnX + PromptB110BuildingMenuBuilder.EntryWidth;
            foreach (var name in names)
            {
                var rect = panel.Find(name) as RectTransform;
                Assert.That(rect, Is.Not.Null, name);
                Assert.That(rect.anchoredPosition.x, Is.GreaterThan(listRight), name);
                Assert.That(rect.anchoredPosition.x + rect.sizeDelta.x,
                    Is.LessThanOrEqualTo(PromptB110BuildingMenuBuilder.PanelWidth - 8f + 0.5f), name);
                Assert.That(-rect.anchoredPosition.y + rect.sizeDelta.y,
                    Is.LessThanOrEqualTo(PromptB110BuildingMenuBuilder.PanelHeight), name);
            }

            // 설치 상태 문구는 배너 안에 놓인다.
            var banner = (RectTransform)panel.Find("AvailabilityBanner");
            var text = (RectTransform)panel.Find("AvailabilityText");
            Assert.That(text.anchoredPosition.y, Is.LessThanOrEqualTo(banner.anchoredPosition.y));
            Assert.That(text.anchoredPosition.y - text.sizeDelta.y,
                Is.GreaterThanOrEqualTo(banner.anchoredPosition.y - banner.sizeDelta.y));
        }

        [Test]
        public void Prefab_DecorationsDoNotBlockEntryClicks()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            foreach (var graphic in menu.GetComponentsInChildren<Graphic>(true))
            {
                var isButton = graphic.GetComponent<Button>() != null;
                var isPanel = graphic.name == "PanelRoot";
                if (!isButton && !isPanel)
                {
                    Assert.That(graphic.raycastTarget, Is.False, graphic.name);
                }
            }
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
            Assert.That(BuildingMenuDisplayFormatter.Detail(BuildingAvailabilityDisplayKind.Ready, string.Empty),
                Does.Contain("좌클릭"));
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
            Assert.That(BuildingMenuDisplayFormatter.CostState(costs[0]), Does.Contain("충분"));
            Assert.That(BuildingMenuDisplayFormatter.CostState(costs[1]), Is.EqualTo("× 3 부족"));
            Assert.That(BuildingMenuDisplayFormatter.CostFill(costs[0]), Is.EqualTo(1f));
            Assert.That(BuildingMenuDisplayFormatter.CostFill(costs[1]), Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(BuildingMenuDisplayFormatter.ShortageDetail(costs), Is.EqualTo("철 3개가 더 필요합니다."));
            Assert.That(BuildingMenuDisplayFormatter.ShortageDetail(new[] { costs[0] }), Is.Empty);
            Assert.That(BuildingMenuDisplayFormatter.PowerLabel(0), Is.EqualTo("전력 불필요"));
            Assert.That(BuildingMenuDisplayFormatter.PowerLabel(3), Is.EqualTo("전력 소비 3"));
        }

        [Test]
        public void View_UpdatesEntriesDetailAndCostsFromReadModels()
        {
            var menu = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath));
            try
            {
                var view = menu.GetComponent<BuildingMenuView>();
                var panel = menu.transform.Find("PanelRoot");
                var item = new BuildingMenuItemReadModel(
                    DataIds.Buildings.OutpostCoreBasic,
                    "전진기지 코어",
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

                var entry = panel.Find("Select_" + DataIds.Buildings.OutpostCoreBasic)
                    .GetComponent<BuildingMenuEntryVisual>();
                Assert.That(entry.IsSelected, Is.True);
                Assert.That(entry.transform.Find("EntryState").GetComponent<TMP_Text>().text, Is.EqualTo("부족"));
                Assert.That(panel.Find("DetailName").GetComponent<TMP_Text>().text, Is.EqualTo("전진기지 코어"));
                Assert.That(panel.Find("DetailPowerChip/DetailPowerText").GetComponent<TMP_Text>().text, Is.EqualTo("전력 소비 5"));
                Assert.That(panel.Find("CostSection").gameObject.activeSelf, Is.True);
                Assert.That(panel.Find("CostSection/CostRow_1").gameObject.activeSelf, Is.True);
                Assert.That(panel.Find("CostSection/CostRow_2").gameObject.activeSelf, Is.False);

                view.SetAvailability(new BuildingAvailabilityReadModel(BuildingPlacementState.Previewing, false, "자원이 부족합니다."));
                var availabilityText = panel.Find("AvailabilityText").GetComponent<TMP_Text>().text;
                Assert.That(availabilityText, Does.Contain("자원 부족"));
                Assert.That(availabilityText, Does.Contain("철 5개가 더 필요합니다."));

                view.ClearSelection();
                Assert.That(entry.IsSelected, Is.False);
                Assert.That(panel.Find("CostSection").gameObject.activeSelf, Is.False);
                Assert.That(panel.Find("DetailPowerChip").gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(menu);
            }
        }
    }
}
