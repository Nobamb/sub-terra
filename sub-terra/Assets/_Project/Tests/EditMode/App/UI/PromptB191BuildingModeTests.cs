using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.Inventory;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>
    /// prompt-B 191 (#304): 목록 정렬 · 초성/조합 중 검색 · 큰 창 TV 켜짐 · 호버 안내 지연 ·
    /// 말풍선 위 Canvas · 메뉴 버튼/인벤토리 프레임 재사용.
    /// </summary>
    public sealed class PromptB191BuildingModeTests
    {
        private const string MenuPath = "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";
        // PopupWindowSorting의 첫 팝업 정렬값(비공개 상수)과 같은 값.
        private const int FirstPopupSortOrder = 32_100;

        [TestCase("ㅅ", true)]
        [TestCase("ㅅㄷ", true)]
        [TestCase("ㅅㄷㄹ", true)]
        [TestCase("사ㄷ", true)]
        [TestCase("사다", true)]
        [TestCase("사다리", true)]
        [TestCase("ㄷㅅ", false)]
        [TestCase("ㄹㅅ", false)]
        [TestCase("", true)]
        [TestCase("   ", true)]
        [TestCase("ㅅ ㄷ", true)]
        [TestCase(" 사 다 ", true)]
        [TestCase("ㄱㅂ", true)]
        [TestCase("기본ㅅ", true)]
        [TestCase("ㅆ", false)]
        public void KoreanSearch_InitialsAndSyllables(string query, bool expected)
        {
            Assert.That(KoreanSearch.Matches("기본 사다리", query), Is.EqualTo(expected), "'" + query + "'");
        }

        [Test]
        public void KoreanSearch_DoubleInitialsComposingSyllablesAndCase()
        {
            // 쌍자음 초성은 그대로 맞춘다.
            Assert.That(KoreanSearch.Matches("빠른 쌍끌이", "ㅃㄹ"), Is.True);
            Assert.That(KoreanSearch.Matches("빠른 쌍끌이", "ㅆㄲ"), Is.True);
            Assert.That(KoreanSearch.Matches("빠른 쌍끌이", "ㅂㄹ"), Is.False, "ㅂ은 ㅃ과 다르다.");

            // 조합 중: 받침 전(버티 → 버팀), 받침이 다음 초성으로 넘어가기 전(삳 → 사다, 사달 → 사다리).
            Assert.That(KoreanSearch.Matches("버팀목", "버티"), Is.True);
            Assert.That(KoreanSearch.Matches("기본 사다리", "삳"), Is.True);
            Assert.That(KoreanSearch.Matches("기본 사다리", "사달"), Is.True);
            Assert.That(KoreanSearch.Matches("기본 사다리", "사닥"), Is.False);
            Assert.That(KoreanSearch.Matches("닭갈비", "닭ㄱ"), Is.True);
            Assert.That(KoreanSearch.Matches("달걀", "닭"), Is.True, "겹받침 ㄺ은 ㄹ 받침 + ㄱ 초성으로도 본다.");

            Assert.That(KoreanSearch.Matches("LED 조명", "led조"), Is.True, "대소문자·공백 무시");
            Assert.That(KoreanSearch.Matches(null, "ㅅ"), Is.False);
            Assert.That(KoreanSearch.Matches(null, null), Is.True);
        }

        [Test]
        public void Formatter_InitialSearchIsNameOnly()
        {
            Assert.That(BuildingMenuDisplayFormatter.MatchesSearch("기본 사다리", "오르내립니다.", "ㅅㄷ"), Is.True);
            Assert.That(BuildingMenuDisplayFormatter.MatchesSearch("기본 사다리", "오르내립니다.", "사ㄷ"), Is.True);
            Assert.That(BuildingMenuDisplayFormatter.MatchesSearch("기본 사다리", "오르내립니다.", "ㄷㅅ"), Is.False);
            // 짧은 용도는 일반 글자 포함만 본다(초성은 시설명만).
            Assert.That(BuildingMenuDisplayFormatter.MatchesSearch("기본 사다리", "오르내립니다.", "오르내"), Is.True);
            Assert.That(BuildingMenuDisplayFormatter.MatchesSearch("기본 사다리", "오르내립니다.", "ㅇㄹㄴ"), Is.False);
        }

        [Test]
        public void CatalogQuery_FourSortsAndStableTies()
        {
            Assert.That(BuildingCatalogQuery.AllLabels(), Is.EqualTo(new[] { "기본 순", "기본 역순", "가나다순", "가나다 역순" }));
            Assert.That(BuildingCatalogQuery.DefaultSort, Is.EqualTo(BuildingCatalogSort.Default));
            Assert.That(BuildingCatalogQuery.FromIndex(99), Is.EqualTo(BuildingCatalogSort.Default));
            Assert.That(BuildingCatalogQuery.Label(BuildingCatalogSort.NameDescending), Is.EqualTo("가나다 역순"));

            var names = new[] { "충전기", "버팀목", "사다리", "버팀목" };
            Assert.That(BuildingCatalogQuery.Order(names, BuildingCatalogSort.Default), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(BuildingCatalogQuery.Order(names, BuildingCatalogSort.DefaultReverse), Is.EqualTo(new[] { 3, 2, 1, 0 }));
            Assert.That(BuildingCatalogQuery.Order(names, BuildingCatalogSort.NameAscending), Is.EqualTo(new[] { 1, 3, 2, 0 }));
            Assert.That(BuildingCatalogQuery.Order(names, BuildingCatalogSort.NameDescending), Is.EqualTo(new[] { 0, 2, 1, 3 }));
            Assert.That(BuildingCatalogQuery.Order(null, BuildingCatalogSort.NameAscending), Is.Empty);
        }

        [Test]
        public void Navigation_SortKeepsQueryAndResetRestoresDefault()
        {
            var navigation = new BuildingModeNavigation();
            Assert.That(navigation.Sort, Is.EqualTo(BuildingCatalogSort.Default));
            navigation.ToggleExpanded();
            navigation.SetQuery("ㅅㄷ");
            navigation.SetSort(BuildingCatalogSort.NameDescending);
            Assert.That(navigation.Query, Is.EqualTo("ㅅㄷ"), "정렬을 바꿔도 검색어는 유지");
            Assert.That(navigation.Sort, Is.EqualTo(BuildingCatalogSort.NameDescending));

            navigation.Reset();
            Assert.That(navigation.Query, Is.Empty);
            Assert.That(navigation.Sort, Is.EqualTo(BuildingCatalogSort.Default));
        }

        [Test]
        public void SortingOrder_AboveDroneBubbleBelowPopups()
        {
            Assert.That(BuildingMenuView.CanvasSortingOrder,
                Is.EqualTo(DroneDialogueSocket.OverlaySortingOrder + BuildingMenuView.SortingOrderAboveDroneBubble));
            Assert.That(BuildingMenuView.CanvasSortingOrder, Is.GreaterThan(DroneDialogueSocket.OverlaySortingOrder));
            Assert.That(BuildingMenuView.CanvasSortingOrder, Is.LessThan(UiLayerPriority.QuestPopup));
            Assert.That(BuildingMenuView.CanvasSortingOrder, Is.LessThan(UiLayerPriority.EmergencyRescueIndicator));
            Assert.That(BuildingMenuView.CanvasSortingOrder, Is.LessThan(UiLayerPriority.EmergencyRescueModal));
            Assert.That(BuildingMenuView.CanvasSortingOrder, Is.LessThan(UiLayerPriority.RunFailureModal));
            Assert.That(BuildingMenuView.CanvasSortingOrder, Is.LessThan(FirstPopupSortOrder));
            Assert.That(BuildingMenuView.CanvasSortingOrder, Is.LessThan(PopupWindowSorting.SettingsSortOrder));

            // 펼친 정렬 목록(차단막은 목록 - 1)은 건설 UI 위, 퀘스트 팝업 아래.
            var listOrder = InventorySortDropdown.ListOrderAbove(BuildingMenuView.CanvasSortingOrder);
            Assert.That(listOrder - 1, Is.GreaterThan(BuildingMenuView.CanvasSortingOrder));
            Assert.That(listOrder, Is.LessThan(UiLayerPriority.QuestPopup));
        }

        [Test]
        public void Prefab_OwnCanvasHoldsWholeBuildingUi()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            var view = menu.GetComponent<BuildingMenuView>();
            Assert.That(view.HasBuildingModeReferences(), Is.True);

            var canvas = menu.GetComponent<Canvas>();
            Assert.That(canvas, Is.Not.Null, "BuildingMenu 루트에 전용 Canvas");
            Assert.That(view.ModeCanvas, Is.SameAs(canvas));
            Assert.That(menu.GetComponent<GraphicRaycaster>(), Is.Not.Null, "버튼·검색창·드롭다운 클릭용");
            var so = new SerializedObject(canvas);
            Assert.That(so.FindProperty("m_OverrideSorting").boolValue, Is.True);
            Assert.That(so.FindProperty("m_SortingOrder").intValue, Is.EqualTo(BuildingMenuView.CanvasSortingOrder));

            // 레일·카드·큰 창·호버 이름표가 모두 이 Canvas 아래(다른 Canvas 없음)에 있다.
            foreach (var name in new[] { "Rail", "Card", "StatusChip", "CatalogWindow", "RailHoverHologram" })
            {
                var target = menu.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
                var owner = target.GetComponentsInParent<Canvas>(true).First();
                Assert.That(owner, Is.SameAs(canvas), name);
            }

            // 루트 배치값은 Scene 인스턴스가 쓰던 그대로다(가운데 500×560 팝업으로 되돌리지 않음: 내용은 레일 기준 왼쪽 위).
            var rootRect = (RectTransform)menu.transform;
            Assert.That(rootRect.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rootRect.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rootRect.pivot, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rootRect.localScale, Is.EqualTo(Vector3.one));
            Assert.That(new SerializedObject(view).FindProperty("hintReshowDelay"), Is.Null, "B-191-3: 안내 대기 값은 없다.");
        }

        [Test]
        public void Prefab_SortDropdownSitsRightOfSearch()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            var view = menu.GetComponent<BuildingMenuView>();
            var dropdown = view.SortDropdown;
            Assert.That(dropdown, Is.InstanceOf<InventorySortDropdown>(), "펼친 목록을 창 Canvas 위로 올린다.");
            Assert.That(dropdown.options.Select(o => o.text), Is.EqualTo(BuildingCatalogQuery.AllLabels()));
            Assert.That(dropdown.value, Is.EqualTo((int)BuildingCatalogSort.Default));
            Assert.That(dropdown.captionText.text, Is.EqualTo("기본 순"));
            Assert.That(dropdown.template, Is.Not.Null);
            Assert.That(dropdown.template.gameObject.activeSelf, Is.False);
            Assert.That(dropdown.template.rect.height,
                Is.GreaterThanOrEqualTo(BuildingCatalogQuery.SortCount * PromptB188BuildingModeBuilder.SortItemHeight),
                "4개 항목이 스크롤 없이 보인다.");
            Assert.That(dropdown.GetComponentsInParent<RectMask2D>(true), Is.Empty, "펼친 목록이 마스크에 잘리지 않는다.");

            var search = (RectTransform)view.SearchField.transform;
            var sort = (RectTransform)dropdown.transform;
            Assert.That(sort.parent, Is.SameAs(search.parent), "검색창과 같은 목록 페이지");
            Assert.That(sort.anchoredPosition.y, Is.EqualTo(search.anchoredPosition.y).Within(0.01f));
            Assert.That(sort.sizeDelta.y, Is.EqualTo(search.sizeDelta.y).Within(0.01f));
            Assert.That(sort.anchoredPosition.x,
                Is.GreaterThanOrEqualTo(search.anchoredPosition.x + search.sizeDelta.x), "검색창 오른쪽");
            Assert.That(sort.anchoredPosition.x + sort.sizeDelta.x,
                Is.LessThanOrEqualTo(PromptB188BuildingModeBuilder.CatalogWidth - PromptB188BuildingModeBuilder.CatalogMargin + 0.01f));
            Assert.That(view.SearchField.placeholder.GetComponent<TMP_Text>().text, Is.EqualTo("시설명 검색"));
        }

        [Test]
        public void Prefab_ReusesMenuButtonsAndSquareBuildingFrameArt()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            var off = AssetDatabase.LoadAssetAtPath<Sprite>(PromptB188BuildingModeBuilder.MenuButtonOffPath);
            var on = AssetDatabase.LoadAssetAtPath<Sprite>(PromptB188BuildingModeBuilder.MenuButtonOnPath);
            var frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PromptB188BuildingModeBuilder.CardFramePath);
            Assert.That(off, Is.Not.Null);
            Assert.That(on, Is.Not.Null);
            Assert.That(frameSprite, Is.Not.Null);

            // 레일 판·시설 버튼·`>>`: 오른쪽 위 메뉴 버튼 그림(평소 off, 호버·누름 on).
            var rail = menu.GetComponentsInChildren<Transform>(true).First(t => t.name == "Rail");
            Assert.That(rail.Find("RailFrame").GetComponent<Image>().sprite, Is.SameAs(off));
            var buttons = menu.GetComponentsInChildren<BuildingMenuEntryVisual>(true)
                .Select(e => e.GetComponent<Button>())
                .Append(menu.GetComponent<BuildingMenuView>().ExpandButton)
                .ToList();
            Assert.That(buttons, Has.Count.EqualTo(10));
            foreach (var button in buttons)
            {
                var skin = button.transform.Find(PromptB188BuildingModeBuilder.ButtonFrameName).GetComponent<Image>();
                Assert.That(skin.sprite, Is.SameAs(off), button.name);
                Assert.That(skin.type, Is.EqualTo(Image.Type.Sliced), button.name);
                Assert.That(button.targetGraphic, Is.SameAs(skin), button.name);
                Assert.That(button.transition, Is.EqualTo(button.GetComponent<BuildingMenuEntryVisual>() != null
                    ? Selectable.Transition.None : Selectable.Transition.SpriteSwap), button.name);
                Assert.That(button.spriteState.highlightedSprite, Is.SameAs(on), button.name);
                Assert.That(button.GetComponent<Image>().raycastTarget, Is.True, button.name + " 클릭 영역");
            }

            foreach (var entry in menu.GetComponentsInChildren<BuildingMenuEntryVisual>(true))
            {
                // 시설 그림은 기존 아이콘 Image 그대로, 버튼 프레임 위에 그린다.
                var icon = entry.transform.Find("Icon");
                var skin = entry.transform.Find(PromptB188BuildingModeBuilder.ButtonFrameName);
                Assert.That(icon.GetSiblingIndex(), Is.GreaterThan(skin.GetSiblingIndex()), entry.name);
                Assert.That(icon.GetComponent<Image>().preserveAspect, Is.True, entry.name);
                Assert.That(entry.transform.Find("SelectedHighlight/SelectedFrame").GetComponent<Image>().sprite, Is.SameAs(on));
            }

            // 큰 창의 목록·상세는 작은 카드와 같은 프레임 이미지를 쓴다.
            var window = menu.GetComponentsInChildren<Transform>(true).First(t => t.name == "CatalogWindow");
            var frame = window.Find(PromptB188BuildingModeBuilder.WindowFrameName).GetComponent<Image>();
            Assert.That(frame.sprite, Is.SameAs(frameSprite));
            Assert.That(frame.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(frame.raycastTarget, Is.False);
            Assert.That(frame.rectTransform.offsetMin.x, Is.LessThan(0f));
            Assert.That(frame.rectTransform.offsetMin.y, Is.LessThan(0f));
            Assert.That(frame.rectTransform.offsetMax.x, Is.GreaterThan(0f));
            Assert.That(frame.rectTransform.offsetMax.y, Is.GreaterThan(0f));
            Assert.That(frame.transform.GetSiblingIndex(), Is.Zero, "목록·상세보다 뒤에 그린다.");
            Assert.That(window.GetComponent<Image>().raycastTarget, Is.True, "창 안 클릭은 월드로 새지 않는다.");
            Assert.That(((RectTransform)window).sizeDelta.x, Is.EqualTo(PromptB112InventoryPanelBuilder.PanelWidth),
                "검색·정렬과 상세 정보의 기존 창 폭을 유지한다.");
            Assert.That(((RectTransform)window).sizeDelta.y, Is.EqualTo(((RectTransform)window).sizeDelta.x));
            Assert.That(((RectTransform)window).anchorMin, Is.EqualTo(new Vector2(0f, 1f)), "가운데 팝업이 아니라 레일 옆 왼쪽 위에 붙는다.");
        }

        [Test]
        public void View_SortReordersListOnlyKeepsQueryAndResetsOnClose()
        {
            var menu = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath));
            try
            {
                var view = menu.GetComponent<BuildingMenuView>();
                var items = CatalogItems();
                view.SetBuildingList(items);
                view.ClearSelection();
                var railBefore = RailIds(menu);
                var defaultOrder = menu.GetComponentsInChildren<BuildingMenuCatalogRowView>(true).Select(r => r.BuildingId).ToList();
                Assert.That(defaultOrder, Is.EqualTo(railBefore), "기본 순 = 레일 순서");
                var nameOf = items.ToDictionary(i => i.BuildingId, i => i.DisplayName);

                view.ToggleExpanded();
                Assert.That(VisibleRowIds(menu), Is.EqualTo(defaultOrder));

                view.SetSort(BuildingCatalogSort.DefaultReverse);
                Assert.That(VisibleRowIds(menu), Is.EqualTo(Enumerable.Reverse(defaultOrder)));

                view.SetSort(BuildingCatalogSort.NameAscending);
                var ascending = VisibleRowIds(menu);
                Assert.That(ascending.Select(id => nameOf[id]), Is.Ordered.Using<string>(string.CompareOrdinal));
                Assert.That(ascending, Is.EquivalentTo(defaultOrder));

                view.SetSort(BuildingCatalogSort.NameDescending);
                Assert.That(VisibleRowIds(menu).Select(id => nameOf[id]),
                    Is.Ordered.Descending.Using<string>(string.CompareOrdinal));
                Assert.That(view.SortDropdown.value, Is.EqualTo((int)BuildingCatalogSort.NameDescending));

                // 검색으로 걸러진 결과를 정렬하고, 정렬을 바꿔도 검색어는 남는다.
                view.SetSearchQuery("ㅈ");
                var filtered = VisibleRowIds(menu);
                Assert.That(filtered, Is.Not.Empty);
                Assert.That(filtered.Count, Is.LessThan(defaultOrder.Count));
                Assert.That(filtered.All(id => KoreanSearch.Matches(nameOf[id], "ㅈ")), Is.True);
                view.SetSort(BuildingCatalogSort.NameAscending);
                Assert.That(view.SearchField.text, Is.EqualTo("ㅈ"));
                Assert.That(view.Navigation.Query, Is.EqualTo("ㅈ"));
                Assert.That(VisibleRowIds(menu), Is.EquivalentTo(filtered));
                Assert.That(VisibleRowIds(menu).Select(id => nameOf[id]), Is.Ordered.Using<string>(string.CompareOrdinal));

                // 초성 한 글자·섞어 치기·없는 초성.
                view.SetSearchQuery("ㅅㄷ");
                Assert.That(VisibleRowIds(menu), Is.EqualTo(new[] { DataIds.Buildings.LadderBasic }));
                view.SetSearchQuery("사ㄷ");
                Assert.That(VisibleRowIds(menu), Is.EqualTo(new[] { DataIds.Buildings.LadderBasic }));
                view.SetSearchQuery("ㄷㅅ");
                Assert.That(VisibleRowIds(menu), Is.Empty);
                Assert.That(menu.GetComponentsInChildren<Transform>(true).First(t => t.name == "EmptyResult").gameObject.activeSelf, Is.True);

                // 레일 아이콘 순서는 정렬·검색과 무관하다.
                Assert.That(RailIds(menu), Is.EqualTo(railBefore));

                // 닫았다 다시 열면 검색어는 비고 정렬은 기본 순.
                view.SetVisible(false);
                menu.GetComponent<BuildingModeSlideMotion>().Advance(1f);
                Assert.That(view.Navigation.Sort, Is.EqualTo(BuildingCatalogSort.Default));
                Assert.That(view.SortDropdown.value, Is.EqualTo((int)BuildingCatalogSort.Default));
                Assert.That(view.SearchField.text, Is.Empty);
                view.SetVisible(true);
                view.ToggleExpanded();
                Assert.That(VisibleRowIds(menu), Is.EqualTo(defaultOrder));
                Assert.That(RailIds(menu), Is.EqualTo(railBefore));
            }
            finally
            {
                Object.DestroyImmediate(menu);
            }
        }

        [Test]
        public void View_CatalogAndDetailPlayTvOpenButBackDoesNot()
        {
            var menu = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath));
            try
            {
                var view = menu.GetComponent<BuildingMenuView>();
                view.SetBuildingList(CatalogItems());
                view.ClearSelection();
                var catalogWindow = menu.GetComponentsInChildren<Transform>(true).First(t => t.name == "CatalogWindow").gameObject;
                var listPage = menu.GetComponentsInChildren<Transform>(true).First(t => t.name == "ListPage").gameObject;
                var detailPage = menu.GetComponentsInChildren<Transform>(true).First(t => t.name == "DetailPage").gameObject;
                var rail = menu.GetComponentsInChildren<Transform>(true).First(t => t.name == "Rail").gameObject;
                var windowScale = catalogWindow.transform.localScale;

                // `>>`: 큰 창에 TV 켜짐(가로줄 → 위아래로 펼침). 레일에는 걸지 않는다.
                view.ToggleExpanded();
                var windowMotion = catalogWindow.GetComponent<TerminalWindowOpenMotion>();
                Assert.That(windowMotion, Is.Not.Null);
                Assert.That(windowMotion.IsPlaying && !windowMotion.IsClosing, Is.True);
                Assert.That(catalogWindow.transform.localScale.y, Is.LessThan(windowScale.y * 0.1f), "가로줄에서 시작");
                Assert.That(rail.GetComponent<TerminalWindowOpenMotion>(), Is.Null);
                Assert.That(menu.GetComponent<TerminalWindowOpenMotion>(), Is.Null);
                Assert.That(catalogWindow.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
                Assert.That(catalogWindow.GetComponent<CanvasGroup>().interactable, Is.True, "연출 중에도 입력을 막지 않는다.");

                // 연타: 켜지는 중 `<<` → 그 자리에서 접히고, 다시 `>>` → 그 자리에서 되펼쳐진다. 반쯤 남지 않는다.
                windowMotion.Advance(TerminalWindowOpenMotion.Duration * 0.6f);
                var halfway = catalogWindow.transform.localScale.y;
                view.ToggleExpanded();
                Assert.That(windowMotion.IsClosing, Is.True);
                Assert.That(catalogWindow.activeSelf, Is.True, "접히는 연출 동안은 보인다.");
                Assert.That(catalogWindow.transform.localScale.y, Is.EqualTo(halfway).Within(1e-4f), "방향만 바뀌고 튀지 않는다.");
                view.ToggleExpanded();
                Assert.That(windowMotion.IsPlaying && !windowMotion.IsClosing, Is.True);
                Assert.That(catalogWindow.transform.localScale.y, Is.EqualTo(halfway).Within(1e-4f));
                windowMotion.Advance(1f);
                Assert.That(windowMotion.IsPlaying, Is.False);
                Assert.That(catalogWindow.transform.localScale, Is.EqualTo(windowScale));
                Assert.That(catalogWindow.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
                Assert.That(listPage.activeSelf, Is.True);

                // 목록 → 상세: 큰 창은 다시 켜지 않고 상세 페이지에만 짧게.
                view.OpenDetail(DataIds.Buildings.ChargerBasic);
                var detailMotion = detailPage.GetComponent<TerminalWindowOpenMotion>();
                Assert.That(detailMotion, Is.Not.Null);
                Assert.That(detailMotion.IsPlaying && !detailMotion.IsClosing, Is.True);
                Assert.That(windowMotion.IsPlaying, Is.False, "창 전체는 다시 켜지 않는다.");
                Assert.That(BuildingMenuView.DetailMotionSpeed, Is.GreaterThan(1f));
                detailMotion.Advance(TerminalWindowOpenMotion.Duration / BuildingMenuView.DetailMotionSpeed + 0.001f);
                Assert.That(detailMotion.IsPlaying, Is.False, "창보다 짧게 끝난다.");
                Assert.That(detailPage.transform.localScale, Is.EqualTo(Vector3.one));

                // 뒤로가기: 연출 없음.
                view.BackToList();
                Assert.That(listPage.activeSelf, Is.True);
                Assert.That(detailPage.activeSelf, Is.False);
                Assert.That(windowMotion.IsPlaying, Is.False);
                Assert.That(detailMotion.IsPlaying, Is.False);

                // 상세가 켜지는 도중 뒤로가기 → 상세가 반쯤 남지 않는다.
                view.OpenDetail(DataIds.Buildings.LadderBasic);
                Assert.That(detailMotion.IsPlaying, Is.True);
                view.BackToList();
                Assert.That(detailMotion.IsPlaying, Is.False);
                Assert.That(detailPage.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(detailPage.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));

                // `<<`: 거꾸로 접히고 끝나면 꺼진다.
                view.ToggleExpanded();
                Assert.That(windowMotion.IsClosing, Is.True);
                windowMotion.Advance(1f);
                Assert.That(catalogWindow.activeSelf, Is.False);
                Assert.That(catalogWindow.transform.localScale, Is.EqualTo(windowScale));

                // 켜지는 도중 건설 모드를 닫으면(B) 바로 정리된다.
                view.ToggleExpanded();
                Assert.That(windowMotion.IsPlaying, Is.True);
                view.SetVisible(false);
                menu.GetComponent<BuildingModeSlideMotion>().Advance(1f);
                Assert.That(menu.activeSelf, Is.False);
                Assert.That(windowMotion.IsPlaying, Is.False);
                Assert.That(catalogWindow.activeSelf, Is.False);
                Assert.That(catalogWindow.transform.localScale, Is.EqualTo(windowScale));
                Assert.That(view.Navigation.Page, Is.EqualTo(BuildingModePage.Rail));
            }
            finally
            {
                Object.DestroyImmediate(menu);
            }
        }

        [Test]
        public void View_CardTurnsOnAndOffLikeTvAndNoHintAppears()
        {
            var menu = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath));
            try
            {
                var view = menu.GetComponent<BuildingMenuView>();
                var items = CatalogItems();
                view.SetBuildingList(items);
                view.ClearSelection();
                var card = menu.GetComponentsInChildren<Transform>(true).First(t => t.name == "Card").gameObject;
                Assert.That(menu.GetComponentsInChildren<Transform>(true).Any(t => t.name == "HintChip"), Is.False);
                Assert.That(card.activeSelf, Is.False);

                // 레일에서 고르면 카드가 큰 창과 같은 TV 켜짐으로 나타난다.
                var ladderItem = items.First(i => i.BuildingId == DataIds.Buildings.LadderBasic);
                view.SetSelection(ladderItem);
                var motion = card.GetComponent<TerminalWindowOpenMotion>();
                Assert.That(card.activeSelf, Is.True);
                Assert.That(view.IsCardShown, Is.True);
                Assert.That(motion, Is.Not.Null, "카드 TV 켜짐");
                Assert.That(motion.IsPlaying && !motion.IsClosing, Is.True);
                motion.Advance(TerminalWindowOpenMotion.Duration * 0.3f);
                Assert.That(card.transform.localScale.y, Is.LessThan(1f), "가로선에서 세로로 펼쳐지는 중");
                motion.Advance(1f);
                Assert.That(motion.IsPlaying, Is.False);
                Assert.That(card.transform.localScale.y, Is.EqualTo(1f).Within(1e-4f));

                // 같은 프레임에 해제 → 다른 시설 선택(교체)은 카드를 끄지 않고 내용만 바꾼다.
                view.ClearSelection();
                view.SetSelection(items.First(i => i.BuildingId == DataIds.Buildings.ChargerBasic));
                view.FlushPendingCardClose();
                Assert.That(card.activeSelf && view.IsCardShown, Is.True);
                Assert.That(motion.IsPlaying, Is.False, "교체에는 꺼짐·켜짐을 다시 걸지 않는다.");

                // 해제하면 TV 꺼짐 동안 보던 내용을 유지하고, 다 꺼진 뒤 숨긴다.
                var name = menu.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "DetailName");
                var shownName = name.text;
                view.ClearSelection();
                Assert.That(view.IsCardShown, Is.False);
                view.FlushPendingCardClose();
                Assert.That(motion.IsClosing, Is.True, "TV 꺼짐");
                Assert.That(card.activeSelf, Is.True);
                Assert.That(name.text, Is.EqualTo(shownName), "꺼지는 동안 내용은 그대로");
                motion.Advance(TerminalWindowOpenMotion.CloseDuration * 0.5f);
                Assert.That(card.activeSelf, Is.True);
                motion.Advance(1f);
                Assert.That(card.activeSelf, Is.False);
                Assert.That(name.text, Is.EqualTo("시설 미선택"));

                // 꺼지는 도중 다시 고르면 그 자리에서 다시 켜진다.
                view.SetSelection(ladderItem);
                motion.Advance(1f);
                view.ClearSelection();
                view.FlushPendingCardClose();
                motion.Advance(TerminalWindowOpenMotion.CloseDuration * 0.4f);
                view.SetSelection(ladderItem);
                Assert.That(motion.IsClosing, Is.False);
                motion.Advance(1f);
                Assert.That(card.activeSelf, Is.True);
                Assert.That(card.transform.localScale.y, Is.EqualTo(1f).Within(1e-4f));

                // `>>`로 펼치면 카드는 연출 없이 바로 접히고, `<<`로 접으면 다시 켜진다.
                view.ToggleExpanded();
                Assert.That(card.activeSelf, Is.False);
                Assert.That(motion.IsPlaying, Is.False);
                view.ToggleExpanded();
                Assert.That(card.activeSelf, Is.True);
                Assert.That(view.IsCardShown, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(menu);
            }
        }

        [Test]
        public void View_RailSelectionScrollsCatalogRowToCenterOrBottom()
        {
            var menu = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath));
            try
            {
                var view = menu.GetComponent<BuildingMenuView>();
                var items = CatalogItems();
                view.SetBuildingList(items);
                view.ClearSelection();
                view.ToggleExpanded();
                var scroll = view.CatalogScroll;
                Canvas.ForceUpdateCanvases();
                Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(1e-3f), "처음엔 맨 위");

                var viewport = scroll.viewport;
                foreach (var item in items)
                {
                    view.SetSelection(item);
                    Canvas.ForceUpdateCanvases();
                    var row = (RectTransform)menu.GetComponentsInChildren<BuildingMenuCatalogRowView>(true)
                        .Single(r => r.BuildingId == item.BuildingId).transform;
                    var rowCorners = new Vector3[4];
                    var viewCorners = new Vector3[4];
                    row.GetWorldCorners(rowCorners);
                    viewport.GetWorldCorners(viewCorners);
                    Assert.That(rowCorners[0].y, Is.GreaterThanOrEqualTo(viewCorners[0].y - 0.5f), item.BuildingId + " 아래가 잘리지 않는다.");
                    Assert.That(rowCorners[1].y, Is.LessThanOrEqualTo(viewCorners[1].y + 0.5f), item.BuildingId + " 위가 잘리지 않는다.");

                    var rowMid = (rowCorners[0].y + rowCorners[1].y) * 0.5f;
                    var viewMid = (viewCorners[0].y + viewCorners[1].y) * 0.5f;
                    var normalized = scroll.verticalNormalizedPosition;
                    var atEdge = normalized <= 1e-3f || normalized >= 1f - 1e-3f;
                    Assert.That(atEdge || Mathf.Abs(rowMid - viewMid) < 0.5f, Is.True,
                        item.BuildingId + " 가운데 또는 스크롤 끝 (normalized=" + normalized + ")");
                }

                // 맨 위 시설은 맨 위, 끝 쪽 시설(자원 정산기 이후)은 스크롤 맨 아래에 붙는다.
                view.SetSelection(items.First(i => i.BuildingId == DataIds.Buildings.SupportBasic));
                Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(1e-3f));
                view.SetSelection(items.First(i => i.BuildingId == DataIds.Buildings.SettlementBasic));
                Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(0f).Within(1e-3f));
                view.SetSelection(items.First(i => i.BuildingId == DataIds.Buildings.StorageBasic));
                var storageNormalized = scroll.verticalNormalizedPosition;
                Assert.That(storageNormalized, Is.GreaterThan(1e-3f).And.LessThan(1f - 1e-3f), "보관함은 가운데에 둘 수 있다.");

                // 같은 시설의 갱신(인벤토리 변화)은 사용자가 옮긴 스크롤을 되돌리지 않는다.
                scroll.verticalNormalizedPosition = 0.5f;
                view.SetSelection(items.First(i => i.BuildingId == DataIds.Buildings.StorageBasic));
                Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(0.5f).Within(1e-3f));
            }
            finally
            {
                Object.DestroyImmediate(menu);
            }
        }

        [Test]
        public void ScrollFocus_CentersAndClampsToEnds()
        {
            // 목록 624 · 보이는 영역 420 → 움직일 수 있는 범위 204.
            Assert.That(BuildingCatalogScrollFocus.CenteredNormalizedPosition(32f, 624f, 420f), Is.EqualTo(1f), "첫 행은 맨 위");
            Assert.That(BuildingCatalogScrollFocus.CenteredNormalizedPosition(312f, 624f, 420f),
                Is.EqualTo(1f - 102f / 204f).Within(1e-5f), "가운데");
            Assert.That(BuildingCatalogScrollFocus.CenteredNormalizedPosition(452f, 624f, 420f), Is.EqualTo(0f), "자원 정산기부터는 맨 아래");
            Assert.That(BuildingCatalogScrollFocus.CenteredNormalizedPosition(100f, 300f, 420f), Is.EqualTo(1f), "스크롤이 없으면 맨 위");
        }

        [Test]
        public void View_StatusChipClearsBackToRailOnly()
        {
            var menu = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath));
            try
            {
                var view = menu.GetComponent<BuildingMenuView>();
                view.SetBuildingList(CatalogItems());
                view.ClearSelection();
                var chip = menu.GetComponentsInChildren<Transform>(true).First(t => t.name == "StatusChip").gameObject;

                // 취소·설치 완료 뒤 Presenter는 빈 문구를 보낸다 → 상태 칸이 사라지고 레일만 남는다.
                view.SetStatusMessage("자원이 부족합니다.");
                Assert.That(chip.activeSelf, Is.True, "설치 불가 이유는 그대로 뜬다.");
                view.ClearSelection();
                view.SetStatusMessage(string.Empty);
                Assert.That(chip.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(menu);
            }
        }

        private static BuildingRailHoverRelay Relay(GameObject menu, string buildingId)
        {
            return menu.GetComponentsInChildren<BuildingMenuEntryVisual>(true)
                .Single(e => e.BuildingId == buildingId)
                .GetComponent<BuildingRailHoverRelay>();
        }

        private static List<string> RailIds(GameObject menu)
        {
            return menu.GetComponentsInChildren<BuildingMenuEntryVisual>(true)
                .OrderByDescending(e => ((RectTransform)e.transform).anchoredPosition.y)
                .Select(e => e.BuildingId)
                .ToList();
        }

        private static List<string> VisibleRowIds(GameObject menu)
        {
            return menu.GetComponentsInChildren<BuildingMenuCatalogRowView>(true)
                .Where(r => r.gameObject.activeSelf)
                .Select(r => r.BuildingId)
                .ToList();
        }

        private static List<BuildingMenuItemReadModel> CatalogItems()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            return catalog.Buildings
                .Where(b => b != null)
                .Select(b => new BuildingMenuItemReadModel(
                    b.Id, b.DisplayName, b.Description, b.Icon, b.PowerDraw,
                    b.BuildCosts.Select(c => new BuildingCostReadModel(c.ItemId, c.Quantity, 0)).ToList()))
                .ToList();
        }
    }
}
