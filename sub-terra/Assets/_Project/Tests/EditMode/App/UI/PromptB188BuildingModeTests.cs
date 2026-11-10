using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.Building;
using SubTerra.Shared;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 188 (#269): 아이콘 레일 건설 모드 · `>>` 목록 · 검색 · 상세 · 뒤로가기.</summary>
    public sealed class PromptB188BuildingModeTests
    {
        private const string MenuPath = "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        [Test]
        public void Navigation_ExpandDetailBackAndReset()
        {
            var navigation = new BuildingModeNavigation();
            Assert.That(navigation.Page, Is.EqualTo(BuildingModePage.Rail));
            Assert.That(navigation.ExpandLabel, Is.EqualTo(">>"));

            navigation.ToggleExpanded();
            Assert.That(navigation.Page, Is.EqualTo(BuildingModePage.List));
            Assert.That(navigation.ExpandLabel, Is.EqualTo("<<"));

            navigation.SetQuery("사다");
            navigation.ShowDetail(DataIds.Buildings.LadderBasic);
            Assert.That(navigation.Page, Is.EqualTo(BuildingModePage.Detail));
            Assert.That(navigation.DetailBuildingId, Is.EqualTo(DataIds.Buildings.LadderBasic));

            // 뒤로가기는 목록으로만 돌아가고 검색어를 유지한다.
            navigation.BackToList();
            Assert.That(navigation.Page, Is.EqualTo(BuildingModePage.List));
            Assert.That(navigation.Query, Is.EqualTo("사다"));
            navigation.BackToList();
            Assert.That(navigation.Page, Is.EqualTo(BuildingModePage.List), "목록에서 뒤로가기는 아무것도 닫지 않는다.");

            // `<<`는 목록·상세 어디서든 레일+카드로 접는다.
            navigation.ShowDetail(DataIds.Buildings.ChargerBasic);
            navigation.ToggleExpanded();
            Assert.That(navigation.Page, Is.EqualTo(BuildingModePage.Rail));

            navigation.Reset();
            Assert.That(navigation.Page, Is.EqualTo(BuildingModePage.Rail));
            Assert.That(navigation.Query, Is.Empty);
            Assert.That(navigation.DetailBuildingId, Is.Empty);
        }

        [Test]
        public void Formatter_ShortPurposeSearchAndCostLines()
        {
            Assert.That(BuildingMenuDisplayFormatter.ShortPurpose("첫 문장입니다. 둘째 문장입니다."), Is.EqualTo("첫 문장입니다."));
            Assert.That(BuildingMenuDisplayFormatter.ShortPurpose("마침표 없음"), Is.EqualTo("마침표 없음"));
            Assert.That(BuildingMenuDisplayFormatter.ShortPurpose("100G와 10%를 씁니다."), Is.EqualTo("100G와 10%를 씁니다."));
            var longText = "연결된 시설에 전력을 공급하고 유독 가스 정화 안전지대를 형성하며 탐사 체크포인트 역할을 합니다.";
            var shortened = BuildingMenuDisplayFormatter.ShortPurpose(longText);
            Assert.That(shortened.Length, Is.LessThanOrEqualTo(BuildingMenuDisplayFormatter.ShortPurposeMaxLength + 1));
            Assert.That(shortened, Does.EndWith("…"));
            Assert.That(longText, Does.StartWith(shortened.TrimEnd('…')));

            Assert.That(BuildingMenuDisplayFormatter.MatchesSearch("기본 사다리", "오르내립니다.", "사다리"), Is.True);
            Assert.That(BuildingMenuDisplayFormatter.MatchesSearch("기본 사다리", "오르내립니다.", "기본사다"), Is.True, "공백 무시");
            Assert.That(BuildingMenuDisplayFormatter.MatchesSearch("기본 사다리", "오르내립니다.", "오르내"), Is.True, "짧은 용도 검색");
            Assert.That(BuildingMenuDisplayFormatter.MatchesSearch("기본 사다리", "오르내립니다.", "충전"), Is.False);
            Assert.That(BuildingMenuDisplayFormatter.MatchesSearch("기본 사다리", "오르내립니다.", "  "), Is.True, "빈 검색어는 전체");

            var lines = BuildingMenuDisplayFormatter.CostLines(new[]
            {
                new BuildingCostReadModel(DataIds.Minerals.Iron, 5, 3),
                new BuildingCostReadModel(DataIds.Minerals.Copper, 4, 10)
            });
            var plain = System.Text.RegularExpressions.Regex.Replace(lines, "<[^>]+>", string.Empty).Split('\n');
            Assert.That(plain[0], Is.EqualTo("철 3 / 5  2 부족"));
            Assert.That(plain[1], Is.EqualTo("구리 10 / 4  충분"));
            Assert.That(BuildingMenuDisplayFormatter.OperatingCondition(DataIds.Buildings.ClinicBasic), Is.EqualTo("전진기지 범위 내"));
            Assert.That(BuildingMenuDisplayFormatter.OperatingCondition(DataIds.Buildings.LadderBasic), Is.Empty);
        }

        [Test]
        public void Prefab_DefaultIsRailWithHintAndNoControlGuideText()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            var view = menu.GetComponent<BuildingMenuView>();
            Assert.That(view.UsesBuildingMode, Is.True);
            Assert.That(view.HasBuildingModeReferences(), Is.True);
            Assert.That(view.CloseButton.gameObject.activeSelf, Is.False, "레일 모드는 X 대신 B로 닫는다.");
            Assert.That(Find(menu.transform, "CatalogWindow").gameObject.activeSelf, Is.False);
            Assert.That(Find(menu.transform, "Card").gameObject.activeSelf, Is.False);
            Assert.That(Find(menu.transform, "ExpandButton/Label").GetComponent<TMP_Text>().text, Is.EqualTo(">>"));
            Assert.That(Find(menu.transform, "HintText").GetComponent<TMP_Text>().text, Is.EqualTo("시설을 고르세요"));
            Assert.That(Find(menu.transform, "Placeholder").GetComponent<TMP_Text>().text, Is.EqualTo("시설명 검색"));
            Assert.That(Find(menu.transform, "BackButton/Label").GetComponent<TMP_Text>().text, Does.Contain("시설 목록"));

            // 이전 500×560 창의 머리말·DETAILS·조작 안내는 남지 않는다.
            foreach (var removed in new[] { "HeaderBand", "DetailSectionLabel", "ControlsHint", "HotkeyHint", "ColumnDivider" })
            {
                Assert.That(Find(menu.transform, removed), Is.Null, removed);
            }

            foreach (var text in menu.GetComponentsInChildren<TMP_Text>(true))
            {
                Assert.That(text.text, Does.Not.Contain("좌클릭"), text.name);
                Assert.That(text.text, Does.Not.Contain("근접 설치"), text.name);
                Assert.That(text.text, Does.Not.Contain("B 닫기"), text.name);
                Assert.That(text.text, Does.Not.Contain("[B]"), text.name);
            }
        }

        [Test]
        public void Prefab_CatalogRowsMatchRailAndSelectSameBuilding()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            var binder = menu.GetComponent<BuildingMenuBinder>();
            var rows = menu.GetComponentsInChildren<BuildingMenuCatalogRowView>(true);
            var railIds = menu.GetComponentsInChildren<BuildingMenuEntryVisual>(true).Select(v => v.BuildingId).ToList();
            Assert.That(rows.Select(r => r.BuildingId), Is.EqualTo(railIds), "목록 순서는 레일과 같다.");
            Assert.That(rows.Select(r => r.BuildingId), Is.EquivalentTo(catalog.Buildings.Where(b => b != null).Select(b => b.Id)));
            foreach (var row in rows)
            {
                Assert.That(row.HasRequiredReferences(), Is.True, row.name);
                var entry = new SerializedObject(row.GetComponent<BuildingMenuEntryButton>());
                Assert.That(entry.FindProperty("binder").objectReferenceValue, Is.SameAs(binder), row.name);
            }

            var scroll = Find(menu.transform, "CatalogScroll").GetComponent<UnityEngine.UI.ScrollRect>();
            Assert.That(scroll.content, Is.Not.Null);
            Assert.That(scroll.viewport, Is.Not.Null);
            Assert.That(scroll.horizontal, Is.False);
            Assert.That(scroll.content.GetComponent<UnityEngine.UI.ContentSizeFitter>(), Is.Not.Null);
        }

        [Test]
        public void View_ExpandSearchDetailBackAndCloseReset()
        {
            var menu = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath));
            try
            {
                var view = menu.GetComponent<BuildingMenuView>();
                var items = CatalogItems(copper: 4, iron: 3);
                view.SetBuildingList(items);
                view.ClearSelection();

                var catalogWindow = Find(menu.transform, "CatalogWindow").gameObject;
                var listPage = Find(menu.transform, "ListPage").gameObject;
                var detailPage = Find(menu.transform, "DetailPage").gameObject;
                var card = Find(menu.transform, "Card").gameObject;
                var hint = Find(menu.transform, "HintChip").gameObject;
                var empty = Find(menu.transform, "EmptyResult").gameObject;
                var expand = Find(menu.transform, "ExpandButton/Label").GetComponent<TMP_Text>();
                Assert.That(hint.activeSelf, Is.True);
                Assert.That(catalogWindow.activeSelf, Is.False);

                // `>>`: 레일은 그대로, 옆에 목록. 작은 카드·힌트는 접힌다.
                view.ToggleExpanded();
                Assert.That(Find(menu.transform, "Rail").gameObject.activeSelf, Is.True);
                Assert.That(catalogWindow.activeSelf && listPage.activeSelf, Is.True);
                Assert.That(detailPage.activeSelf, Is.False);
                Assert.That(card.activeSelf || hint.activeSelf, Is.False);
                Assert.That(expand.text, Is.EqualTo("<<"));
                Assert.That(VisibleRowIds(menu), Has.Count.EqualTo(items.Count));
                var ladderRow = Row(menu, DataIds.Buildings.LadderBasic);
                Assert.That(ladderRow.CostText, Does.Contain("부족").Or.Contain("충분"));
                Assert.That(ladderRow.CostText, Does.Contain(" / "));

                // 검색: 이름(및 짧은 용도)이 맞는 시설만. 지우면 전체, 없으면 짧은 안내.
                view.SetSearchQuery("사다리");
                Assert.That(VisibleRowIds(menu), Is.EqualTo(new[] { DataIds.Buildings.LadderBasic }));
                Assert.That(empty.activeSelf, Is.False);
                view.SetSearchQuery("존재하지않는시설");
                Assert.That(VisibleRowIds(menu), Is.Empty);
                Assert.That(empty.activeSelf, Is.True);
                view.SetSearchQuery(string.Empty);
                Assert.That(VisibleRowIds(menu), Has.Count.EqualTo(items.Count));
                Assert.That(empty.activeSelf, Is.False);

                // 목록에서 충전기 → 상세(썸네일·기능·조건·비용·상태). 설치 대상 선택은 Presenter가 맡으므로 여기선 SetSelection으로 흉내 낸다.
                view.SetSearchQuery("충전");
                view.OpenDetail(DataIds.Buildings.ChargerBasic);
                view.SetSelection(items.First(i => i.BuildingId == DataIds.Buildings.ChargerBasic));
                view.SetAvailability(new BuildingAvailabilityReadModel(BuildingPlacementState.Valid, true, string.Empty));
                Assert.That(detailPage.activeSelf, Is.True);
                Assert.That(listPage.activeSelf, Is.False);
                Assert.That(Find(menu.transform, "DetailPageName").GetComponent<TMP_Text>().text,
                    Is.EqualTo(items.First(i => i.BuildingId == DataIds.Buildings.ChargerBasic).DisplayName));
                Assert.That(Find(menu.transform, "DetailThumb").GetComponent<UnityEngine.UI.Image>().sprite, Is.Not.Null);
                var condition = Find(menu.transform, "DetailPageCondition").GetComponent<TMP_Text>();
                Assert.That(condition.gameObject.activeSelf, Is.True);
                Assert.That(condition.text, Does.Contain("전진기지 범위 내"));
                Assert.That(Find(menu.transform, "DetailPageFunction").GetComponent<TMP_Text>().text, Is.Not.Empty);
                Assert.That(Find(menu.transform, "DetailCostRow_0").gameObject.activeSelf, Is.True);
                Assert.That(Find(menu.transform, "DetailPageStatus").GetComponent<TMP_Text>().text, Does.Contain("설치 가능"));
                Assert.That(Row(menu, DataIds.Buildings.ChargerBasic).IsSelected, Is.True);

                // 레일에서 다른 시설을 고르면 상세도 그 시설로 바뀐다.
                view.SetSelection(items.First(i => i.BuildingId == DataIds.Buildings.LadderBasic));
                Assert.That(view.Navigation.DetailBuildingId, Is.EqualTo(DataIds.Buildings.LadderBasic));
                Assert.That(Find(menu.transform, "DetailPageCondition").gameObject.activeSelf, Is.False);

                // 뒤로가기: 목록으로만, 검색어 유지. 건설 모드는 열린 채.
                view.BackToList();
                Assert.That(listPage.activeSelf, Is.True);
                Assert.That(catalogWindow.activeSelf, Is.True);
                Assert.That(view.SearchField.text, Is.EqualTo("충전"));
                Assert.That(VisibleRowIds(menu), Is.EqualTo(new[] { DataIds.Buildings.ChargerBasic }));
                Assert.That(menu.activeSelf, Is.True);

                // `<<`: 레일+카드로 접힌다. B-191: 큰 창은 TV 꺼짐 연출이 끝난 뒤 꺼진다.
                view.ToggleExpanded();
                catalogWindow.GetComponent<SubTerra.App.UI.TerminalWindowOpenMotion>().Advance(1f);
                Assert.That(catalogWindow.activeSelf, Is.False);
                Assert.That(card.activeSelf, Is.True);

                // 닫으면(B) 다음 열기는 레일·검색 초기 상태.
                view.ToggleExpanded();
                view.SetVisible(false);
                var motion = menu.GetComponent<BuildingModeSlideMotion>();
                if (motion != null) motion.Advance(1f);
                Assert.That(menu.activeSelf, Is.False);
                Assert.That(view.Navigation.Page, Is.EqualTo(BuildingModePage.Rail));
                Assert.That(view.SearchField.text, Is.Empty);
                Assert.That(catalogWindow.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(menu);
            }
        }

        [Test]
        public void View_StatusMessageIsSeparateFromAvailability()
        {
            var menu = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath));
            try
            {
                var view = menu.GetComponent<BuildingMenuView>();
                var chip = Find(menu.transform, "StatusChip").gameObject;
                view.SetBuildingList(CatalogItems(0, 0));
                view.ClearSelection();
                Assert.That(chip.activeSelf, Is.False);
                // B-191: 취소·설치 완료 문구는 없어졌고, 설치 불가 이유는 그대로 이 칸에 뜬다.
                view.SetStatusMessage("자원이 부족합니다.");
                Assert.That(chip.activeSelf, Is.True);
                Assert.That(Find(menu.transform, "StatusText").GetComponent<TMP_Text>().text, Is.EqualTo("자원이 부족합니다."));
                Assert.That(Find(menu.transform, "AvailabilityText").GetComponent<TMP_Text>().text, Does.Not.Contain("자원이 부족"));
                view.SetStatusMessage(string.Empty);
                Assert.That(chip.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(menu);
            }
        }

        private static List<BuildingMenuItemReadModel> CatalogItems(int copper, int iron)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            var items = new List<BuildingMenuItemReadModel>();
            foreach (var data in catalog.Buildings.Where(b => b != null))
            {
                var costs = data.BuildCosts.Select(c => new BuildingCostReadModel(
                    c.ItemId,
                    c.Quantity,
                    c.ItemId == DataIds.Minerals.Copper ? copper : c.ItemId == DataIds.Minerals.Iron ? iron : 0)).ToList();
                items.Add(new BuildingMenuItemReadModel(data.Id, data.DisplayName, data.Description, data.Icon, data.PowerDraw, costs));
            }

            return items;
        }

        private static BuildingMenuCatalogRowView Row(GameObject menu, string buildingId)
        {
            return menu.GetComponentsInChildren<BuildingMenuCatalogRowView>(true).Single(r => r.BuildingId == buildingId);
        }

        private static List<string> VisibleRowIds(GameObject menu)
        {
            return menu.GetComponentsInChildren<BuildingMenuCatalogRowView>(true)
                .Where(r => r.gameObject.activeSelf)
                .Select(r => r.BuildingId)
                .ToList();
        }

        private static Transform Find(Transform root, string path)
        {
            var parts = path.Split('/');
            var first = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == parts[0]);
            if (first == null || parts.Length == 1)
            {
                return first;
            }

            return first.Find(string.Join("/", parts.Skip(1)));
        }
    }
}
