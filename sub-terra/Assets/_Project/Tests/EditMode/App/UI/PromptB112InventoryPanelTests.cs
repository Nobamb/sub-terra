using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.App.UI.Inventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 112: 인벤토리 창 재구성 구조와 표시 규칙.</summary>
    public sealed class PromptB112InventoryPanelTests
    {
        private const string PrefabPath = PromptB112InventoryPanelBuilder.InventoryPanelPrefabPath;
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        private sealed class DetailRecordingView : IInventoryPanelView, IInventoryPanelDetailView
        {
            public string Cargo;
            public float Current = -1f;
            public float Max = -1f;
            public float Value = -1f;
            public IReadOnlyList<InventoryStackReadModel> Stacks;

            public void SetCargoSummary(string cargoText) { Cargo = cargoText; }
            public void SetUnsettledValue(string valueText) { }
            public void SetStacksText(string stacksText) { }
            public void SetStacks(IReadOnlyList<InventoryStackReadModel> stacks) { Stacks = stacks; }
            public void SetVisible(bool visible) { }

            public void SetCargoLoad(float currentWeight, float maxCapacity)
            {
                Current = currentWeight;
                Max = maxCapacity;
            }

            public void SetUnsettledAmount(float value) { Value = value; }
        }

        [Test]
        public void Formatter_LevelsUseTextAndNumbers()
        {
            Assert.That(InventoryPanelDisplayFormatter.Level(0f, 50f), Is.EqualTo(InventoryCargoLevel.Empty));
            Assert.That(InventoryPanelDisplayFormatter.Level(10f, 50f), Is.EqualTo(InventoryCargoLevel.Normal));
            Assert.That(InventoryPanelDisplayFormatter.Level(40f, 50f), Is.EqualTo(InventoryCargoLevel.NearFull));
            Assert.That(InventoryPanelDisplayFormatter.Level(50f, 50f), Is.EqualTo(InventoryCargoLevel.Full));
            Assert.That(InventoryPanelDisplayFormatter.Level(5f, 0f), Is.EqualTo(InventoryCargoLevel.Empty));

            Assert.That(InventoryPanelDisplayFormatter.Percent(49.9f, 50f), Is.EqualTo("99%"));
            Assert.That(InventoryPanelDisplayFormatter.Percent(50f, 50f), Is.EqualTo("100%"));
            Assert.That(InventoryPanelDisplayFormatter.FillRatio(80f, 50f), Is.EqualTo(1f));
            Assert.That(InventoryPanelDisplayFormatter.StateLine(42.5f, 50f), Does.Contain("적재 한계 임박").And.Contain("7.5"));
            Assert.That(InventoryPanelDisplayFormatter.StateLine(50f, 50f), Does.Contain("가득 참"));
            Assert.That(InventoryPanelDisplayFormatter.StateLine(13f, 50f), Does.StartWith("남은 적재량 37"));
            Assert.That(InventoryPanelDisplayFormatter.UnsettledAmount(98765f), Is.EqualTo("98,765"));
        }

        [Test]
        public void Formatter_ManifestColumnsUseQuantityTimesUnitWeight()
        {
            // prompt-B 190: 개당 무게 열은 숫자만, 이 줄 무게 = 수량 × 개당 무게.
            Assert.That(InventoryPanelDisplayFormatter.UnitWeight(1.5f), Is.EqualTo("1.5"));
            Assert.That(InventoryPanelDisplayFormatter.LineWeight(4, 1.5f), Is.EqualTo("6"));
            Assert.That(InventoryPanelDisplayFormatter.LineWeight(4, 0.8f), Is.EqualTo("3.2"));
            Assert.That(InventoryPanelDisplayFormatter.LineWeight(12, 2f), Is.EqualTo("24"));
            Assert.That(InventoryPanelDisplayFormatter.LineWeight(0, 2f), Is.EqualTo("0"));
            Assert.That(InventoryPanelDisplayFormatter.LoadedKinds(3), Is.EqualTo("3종"));
            Assert.That(InventoryPanelDisplayFormatter.LoadedKinds(0), Is.EqualTo("0종"));
        }

        [Test]
        public void View_HidesZeroQuantityRowsAndShowsEmptyHintOnly()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var view = root.GetComponent<InventoryPanelView>();
                var panel = root.transform.Find("PanelRoot");
                var rows = root.GetComponentsInChildren<InventoryStackRowView>(true);
                var copper = rows.First(r => r.MineralId == "mineral.copper");

                view.SetStacks(new[] { new InventoryStackReadModel("mineral.copper", "구리", null, 4, 1.5f) });
                Assert.That(copper.gameObject.activeSelf, Is.True);
                Assert.That(rows.Where(r => r != copper).All(r => !r.gameObject.activeSelf), Is.True, "수량 0 행이 보입니다.");
                Assert.That(copper.transform.Find("LineWeight").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("6"));
                Assert.That(copper.transform.Find("UnitWeight").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("1.5"));
                Assert.That(panel.Find("OwnedKindsText"), Is.Null, "종류 수 자리는 정렬 드롭다운이 쓴다.");
                Assert.That(panel.Find("EmptyHint").gameObject.activeSelf, Is.False);
                Assert.That(panel.Find("ColumnHeader").gameObject.activeSelf, Is.True);

                view.SetStacks(System.Array.Empty<InventoryStackReadModel>());
                Assert.That(rows.All(r => !r.gameObject.activeSelf), Is.True);
                Assert.That(panel.Find("EmptyHint").gameObject.activeSelf, Is.True);
                Assert.That(panel.Find("EmptyHint").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("적재된 자원이 없습니다"));
                Assert.That(panel.Find("ColumnHeader").gameObject.activeSelf, Is.False);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static InventoryStackReadModel[] CargoOf(int copper, int iron, int lithium, int fuel = 0)
        {
            return new[]
            {
                new InventoryStackReadModel("mineral.copper", "구리", null, copper, 1.5f),
                new InventoryStackReadModel("mineral.iron", "철", null, iron, 2f),
                new InventoryStackReadModel("mineral.lithium", "리튬", null, lithium, 0.8f),
                new InventoryStackReadModel("item.rare.engine_fuel", "엔진 연료 (희귀)", null, fuel, 1f)
            };
        }

        private static string[] Order(InventoryStackReadModel[] stacks, string query, InventoryManifestSort sort)
        {
            return InventoryManifestQuery.Apply(stacks, query, sort).Select(s => s.MineralId).ToArray();
        }

        [Test]
        public void Query_DefaultRaritySortsCopperIronLithiumAndReverse()
        {
            // prompt-B 190: 희귀도 구리 < 철 < 리튬, 희귀 품목(엔진 연료)은 광물보다 높다.
            var cargo = CargoOf(4, 12, 4);
            Assert.That(InventoryManifestQuery.DefaultSort, Is.EqualTo(InventoryManifestSort.RarityAscending));
            Assert.That(Order(cargo, "", InventoryManifestSort.RarityAscending),
                Is.EqualTo(new[] { "mineral.copper", "mineral.iron", "mineral.lithium" }));
            Assert.That(Order(cargo, "", InventoryManifestSort.RarityDescending),
                Is.EqualTo(new[] { "mineral.lithium", "mineral.iron", "mineral.copper" }));
            Assert.That(Order(CargoOf(1, 1, 1, 1), "", InventoryManifestSort.RarityDescending).First(),
                Is.EqualTo("item.rare.engine_fuel"));
        }

        [Test]
        public void Query_WeightSortUsesUnitWeightNotLineWeight()
        {
            // 철 2×1 = 2, 구리 1.5×10 = 15: 이 줄 무게라면 구리가 먼저지만 개당 무게 기준이라 철이 먼저다.
            var cargo = CargoOf(10, 1, 4);
            Assert.That(Order(cargo, "", InventoryManifestSort.WeightDescending),
                Is.EqualTo(new[] { "mineral.iron", "mineral.copper", "mineral.lithium" }));
            Assert.That(Order(cargo, "", InventoryManifestSort.WeightAscending),
                Is.EqualTo(new[] { "mineral.lithium", "mineral.copper", "mineral.iron" }));
        }

        [Test]
        public void Query_QuantityNameAndTieBreakRules()
        {
            var cargo = CargoOf(4, 12, 4);
            Assert.That(Order(cargo, "", InventoryManifestSort.QuantityDescending),
                Is.EqualTo(new[] { "mineral.iron", "mineral.copper", "mineral.lithium" }), "동률(구리·리튬 4개)은 희귀도순");
            Assert.That(Order(cargo, "", InventoryManifestSort.QuantityAscending),
                Is.EqualTo(new[] { "mineral.copper", "mineral.lithium", "mineral.iron" }));
            Assert.That(Order(cargo, "", InventoryManifestSort.NameAscending),
                Is.EqualTo(new[] { "mineral.copper", "mineral.lithium", "mineral.iron" }), "구리 → 리튬 → 철");
            Assert.That(Order(cargo, "", InventoryManifestSort.NameDescending),
                Is.EqualTo(new[] { "mineral.iron", "mineral.lithium", "mineral.copper" }));
            Assert.That(InventoryManifestQuery.AllLabels(), Is.EqualTo(new[]
            {
                "수량 많은 순", "수량 적은 순", "무게 무거운 순", "무게 가벼운 순",
                "희귀도순", "희귀도 역순", "가나다순", "가나다 역순"
            }));
        }

        [Test]
        public void Query_SearchMatchesDisplayNameAndSkipsEmptyStacks()
        {
            var cargo = CargoOf(4, 0, 4, 1);
            Assert.That(Order(cargo, "구", InventoryManifestSort.RarityAscending), Is.EqualTo(new[] { "mineral.copper" }));
            Assert.That(Order(cargo, "  연료 ", InventoryManifestSort.RarityAscending), Is.EqualTo(new[] { "item.rare.engine_fuel" }));
            Assert.That(Order(cargo, "철", InventoryManifestSort.RarityAscending), Is.Empty, "수량 0인 철은 검색돼도 올리지 않는다.");
            Assert.That(Order(cargo, "", InventoryManifestSort.RarityAscending),
                Is.EqualTo(new[] { "mineral.copper", "mineral.lithium", "item.rare.engine_fuel" }));
            Assert.That(InventoryManifestQuery.LoadedCount(cargo), Is.EqualTo(3));
        }

        [Test]
        public void View_SearchSortKeepEachOtherAndResetOnReopen()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var view = root.GetComponent<InventoryPanelView>();
                var panel = root.transform.Find("PanelRoot");
                var content = panel.Find("StackScroll/Viewport/StackRows");
                var hint = panel.Find("EmptyHint").GetComponent<TMPro.TMP_Text>();
                string[] Visible() => content.Cast<Transform>()
                    .Where(t => t.gameObject.activeSelf)
                    .Select(t => t.GetComponent<InventoryStackRowView>().MineralId)
                    .ToArray();

                view.SetStacks(CargoOf(4, 12, 4));
                Assert.That(Visible(), Is.EqualTo(new[] { "mineral.copper", "mineral.iron", "mineral.lithium" }));

                view.SetSort(InventoryManifestSort.RarityDescending);
                Assert.That(Visible(), Is.EqualTo(new[] { "mineral.lithium", "mineral.iron", "mineral.copper" }));

                view.SetSearchQuery("리");
                Assert.That(Visible(), Is.EqualTo(new[] { "mineral.lithium", "mineral.copper" }), "구리·리튬");
                Assert.That(view.Sort, Is.EqualTo(InventoryManifestSort.RarityDescending), "검색해도 정렬 유지");

                view.SetSort(InventoryManifestSort.NameAscending);
                Assert.That(view.SearchQuery, Is.EqualTo("리"), "정렬을 바꿔도 검색어 유지");
                Assert.That(view.SearchField.text, Is.EqualTo("리"));
                Assert.That(Visible(), Is.EqualTo(new[] { "mineral.copper", "mineral.lithium" }));

                // 창을 연 채 채굴: 검색·정렬을 유지하고 표만 갱신.
                view.SetStacks(CargoOf(4, 12, 0));
                Assert.That(Visible(), Is.EqualTo(new[] { "mineral.copper" }));

                view.SetSearchQuery("금");
                Assert.That(Visible(), Is.Empty);
                Assert.That(hint.gameObject.activeSelf, Is.True);
                Assert.That(hint.text, Is.EqualTo("검색 결과가 없습니다"));
                Assert.That(panel.Find("ColumnHeader").gameObject.activeSelf, Is.False);

                // 실린 자원이 없으면 검색어와 상관없이 빈 적재 안내.
                view.SetStacks(CargoOf(0, 0, 0));
                Assert.That(hint.text, Is.EqualTo("적재된 자원이 없습니다"));

                view.SetStacks(CargoOf(4, 12, 4));
                view.ResetManifestControls();
                Assert.That(view.SearchQuery, Is.Empty);
                Assert.That(view.SearchField.text, Is.Empty);
                Assert.That(view.Sort, Is.EqualTo(InventoryManifestSort.RarityAscending));
                Assert.That(view.SortDropdown.value, Is.EqualTo((int)InventoryManifestSort.RarityAscending));
                Assert.That(Visible(), Is.EqualTo(new[] { "mineral.copper", "mineral.iron", "mineral.lithium" }));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void Presenter_ForwardsSnapshotNumbersToDetailView()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1.5f, 10, "Copper");
            var service = new InventoryService(catalog, 50f, GameState.CreateNew());
            service.AddMineral("mineral.copper", 3);

            var view = new DetailRecordingView();
            var presenter = new InventoryPanelPresenter(view);
            presenter.Bind(service);

            Assert.That(view.Current, Is.EqualTo(service.CurrentWeight).Within(0.0001f));
            Assert.That(view.Max, Is.EqualTo(50f).Within(0.0001f));
            Assert.That(view.Value, Is.EqualTo(service.UnsettledValue).Within(0.0001f));
            Assert.That(view.Stacks.Single(s => s.MineralId == "mineral.copper").UnitWeight, Is.EqualTo(1.5f));

            presenter.Unbind();
        }

        [Test]
        public void Prefab_HasReworkedStructureAndKeepsLegacyContract()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var view = prefab.GetComponent<InventoryPanelView>();
            Assert.That(view.HasRequiredReferences(), Is.True);
            Assert.That(prefab.transform.Find("CloseButton"), Is.Not.Null);
            Assert.That(((RectTransform)prefab.transform).sizeDelta,
                Is.EqualTo(new Vector2(PromptB112InventoryPanelBuilder.PanelWidth, PromptB112InventoryPanelBuilder.PanelHeight)));

            var so = new SerializedObject(view);
            foreach (var field in new[]
            {
                "cargoAmountText", "cargoPercentText", "cargoStateText", "cargoFill",
                "cargoFillImage", "unsettledAmountText", "emptyHintText", "columnHeaderRoot",
                "searchField", "sortDropdown", "stackRowsContent", "stackScroll"
            })
            {
                Assert.That(so.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            }

            Assert.That(so.FindProperty("ownedKindsText").objectReferenceValue, Is.Null, "종류 수(N종)는 두지 않는다.");

            var scroll = prefab.transform.Find("PanelRoot/StackScroll").GetComponent<ScrollRect>();
            Assert.That(scroll.content, Is.SameAs(prefab.transform.Find("PanelRoot/StackScroll/Viewport/StackRows")));
            Assert.That(scroll.horizontal, Is.False);

            var panel = prefab.transform.Find("PanelRoot");

            // A. 가로 적재 현황 + 그보다 작은 미정산 스탬프(적재 현황 안쪽 오른쪽 끝).
            var card = (RectTransform)panel.Find("CargoCard");
            var stamp = (RectTransform)panel.Find("ValueCard");
            Assert.That(card.sizeDelta.x, Is.EqualTo(PromptB112InventoryPanelBuilder.ContentWidth));
            Assert.That(stamp.anchoredPosition.x, Is.GreaterThan(card.anchoredPosition.x));
            Assert.That(stamp.anchoredPosition.x + stamp.sizeDelta.x,
                Is.LessThanOrEqualTo(card.anchoredPosition.x + card.sizeDelta.x));
            Assert.That(stamp.anchoredPosition.y - stamp.sizeDelta.y,
                Is.GreaterThanOrEqualTo(card.anchoredPosition.y - card.sizeDelta.y));
            var amount = (RectTransform)card.Find("CargoAmountText");
            var percent = (RectTransform)card.Find("CargoPercentText");
            Assert.That(percent.anchoredPosition.x + percent.sizeDelta.x + card.anchoredPosition.x,
                Is.LessThan(stamp.anchoredPosition.x), "게이지 열이 스탬프에 닿습니다.");
            Assert.That(stamp.Find("UnsettledAmountText").GetComponent<TMPro.TMP_Text>().fontSizeMax,
                Is.LessThan(amount.GetComponent<TMPro.TMP_Text>().fontSize));
            Assert.That(stamp.Find("ValueLabel").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("미정산 가치"));
            Assert.That(stamp.Find("ValueCaption"), Is.Null, "정산 시 받을 금액 문구는 넣지 않는다.");

            // 도움말 ?는 적재 현황 제목 옆, 도움말 본문은 창 아래(숫자 위를 덮지 않음).
            var help = (RectTransform)panel.Find("WeightHelpIcon");
            Assert.That(help.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(help.anchoredPosition.y, Is.InRange(
                card.anchoredPosition.y - card.sizeDelta.y, card.anchoredPosition.y));
            Assert.That(help.anchoredPosition.x + help.sizeDelta.x,
                Is.LessThan(card.anchoredPosition.x + amount.anchoredPosition.x + amount.sizeDelta.x));
            var tooltip = panel.Find("WeightTooltip");
            // 도움말 본문은 ? 아이콘 오른쪽에서 열리는 홀로그램 창이며, 창 폭 안에 머문다.
            var tipRect = (RectTransform)tooltip;
            Assert.That(tipRect.anchoredPosition.x, Is.GreaterThan(help.anchoredPosition.x + help.sizeDelta.x));
            Assert.That(tipRect.anchoredPosition.x + tipRect.sizeDelta.x, Is.LessThan(PromptB112InventoryPanelBuilder.PanelWidth));
            Assert.That(tooltip.Find("Body/Description"), Is.Not.Null);
            Assert.That(help.Find("Glow"), Is.Not.Null, "호버 글로우 층이 없습니다.");
            Assert.That(help.GetComponent<UnityEngine.UI.Image>().sprite, Is.Not.Null, "둥근 ? 아이콘 이미지가 없습니다.");
            Assert.That(tooltip.GetSiblingIndex(), Is.EqualTo(tooltip.parent.childCount - 1));

            // C. 하단 조작 안내 문구는 없다.
            foreach (var removed in new[] { "ControlsHint", "ControlsDivider", "HotkeyHint" })
            {
                Assert.That(panel.Find(removed), Is.Null, removed);
            }

            // B. 적재 내역 표 머리와 행 열 정렬.
            Assert.That(panel.Find("ListSectionLabel").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("적재 내역"));
            Assert.That(panel.Find("EmptyHint").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("적재된 자원이 없습니다"));
            var header = panel.Find("ColumnHeader");
            var cells = new[] { "ColResource", "ColUnitWeight", "ColQuantity", "ColLineWeight" };
            Assert.That(cells.Select(c => header.Find(c).GetComponent<TMPro.TMP_Text>().text),
                Is.EqualTo(new[] { "자원", "개당 무게", "수량", "이 줄 무게" }));
            Assert.That(PromptB112InventoryPanelBuilder.VisibleRowCount(), Is.InRange(3.8f, 4.6f));
            Assert.That(PromptB112InventoryPanelBuilder.RowHeight, Is.LessThan(88f));

            // C. 적재 내역 오른쪽 정렬 드롭다운 + 제목 아래·표 머리 위 검색창.
            var label = (RectTransform)panel.Find("ListSectionLabel");
            var sort = (RectTransform)panel.Find("SortDropdown");
            var search = (RectTransform)panel.Find("SearchField");
            var dropdown = sort.GetComponent<TMPro.TMP_Dropdown>();
            Assert.That(sort.anchoredPosition.y, Is.EqualTo(label.anchoredPosition.y), "정렬은 적재 내역과 같은 줄");
            Assert.That(sort.anchoredPosition.x, Is.GreaterThan(label.anchoredPosition.x + label.sizeDelta.x));
            Assert.That(sort.anchoredPosition.x + sort.sizeDelta.x,
                Is.LessThanOrEqualTo(card.anchoredPosition.x + card.sizeDelta.x));
            Assert.That(dropdown.options.Select(o => o.text), Is.EqualTo(InventoryManifestQuery.AllLabels()));
            Assert.That(dropdown.value, Is.EqualTo((int)InventoryManifestSort.RarityAscending));
            Assert.That(dropdown.captionText.text, Is.EqualTo("희귀도순"));
            Assert.That(search.anchoredPosition.y, Is.LessThan(label.anchoredPosition.y - label.sizeDelta.y + 0.1f));
            Assert.That(search.anchoredPosition.y - search.sizeDelta.y,
                Is.GreaterThanOrEqualTo(((RectTransform)header).anchoredPosition.y), "검색창은 표 머리 위");
            var input = search.GetComponent<TMPro.TMP_InputField>();
            Assert.That(((TMPro.TMP_Text)input.placeholder).text, Is.EqualTo("자원 이름 검색"));
            Assert.That(input.lineType, Is.EqualTo(TMPro.TMP_InputField.LineType.SingleLine));
            Assert.That(search.Find("SearchIcon"), Is.Not.Null, "왼쪽 돋보기");

            // 펼친 정렬 목록은 창 안(아래쪽)으로만 내려오고 위의 적재 숫자를 덮지 않는다.
            var template = dropdown.template;
            Assert.That(template.pivot.y, Is.EqualTo(1f));
            Assert.That(template.anchorMax.y, Is.EqualTo(0f));
            var listBottom = sort.anchoredPosition.y - sort.sizeDelta.y + template.anchoredPosition.y - template.sizeDelta.y;
            Assert.That(listBottom, Is.GreaterThanOrEqualTo(-PromptB112InventoryPanelBuilder.PanelHeight), "정렬 목록이 창 밖으로 나갑니다.");
            Assert.That(template.Find("Scrollbar"), Is.Null, "8개 항목이 스크롤 없이 보인다.");
            // 팝업 정렬(32100+) 창 위로 목록을 올리는 전용 드롭다운이어야 펼친 목록이 창 뒤에 깔리지 않는다.
            Assert.That(dropdown, Is.InstanceOf<InventorySortDropdown>());
            Assert.That(InventorySortDropdown.ListOrderAbove(32100), Is.EqualTo(32102));
            Assert.That(InventorySortDropdown.ListOrderAbove(500), Is.EqualTo(InventorySortDropdown.DefaultListOrder));

            foreach (var row in prefab.GetComponentsInChildren<InventoryStackRowView>(true))
            {
                Assert.That(((RectTransform)row.transform).sizeDelta.y, Is.EqualTo(PromptB112InventoryPanelBuilder.RowHeight));
                Assert.That(((RectTransform)row.transform.Find("Icon")).sizeDelta, Is.EqualTo(new Vector2(40f, 40f)));
                Assert.That(row.transform.Find("State"), Is.Null, row.MineralId);
                var name = (RectTransform)row.transform.Find("Name");
                var unit = (RectTransform)row.transform.Find("UnitWeight");
                Assert.That(name.anchoredPosition.x, Is.EqualTo(PromptB112InventoryPanelBuilder.NameX));
                Assert.That(name.anchoredPosition.x + name.sizeDelta.x, Is.LessThan(unit.anchoredPosition.x),
                    "이름이 숫자 열을 침범합니다.");
                foreach (var pair in new[] { ("UnitWeight", "ColUnitWeight"), ("Quantity", "ColQuantity"), ("LineWeight", "ColLineWeight") })
                {
                    var cell = (RectTransform)row.transform.Find(pair.Item1);
                    var head = (RectTransform)header.Find(pair.Item2);
                    Assert.That(cell.anchoredPosition.x + cell.sizeDelta.x,
                        Is.EqualTo(head.anchoredPosition.x + head.sizeDelta.x), pair.Item1 + " 오른쪽 끝");
                    Assert.That(cell.GetComponent<TMPro.TMP_Text>().alignment, Is.EqualTo(TMPro.TextAlignmentOptions.MidlineRight));
                }
            }
        }

        [Test]
        public void Prefab_HasOneRowPerCatalogItemWithManifestColumns()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var rows = prefab.GetComponentsInChildren<InventoryStackRowView>(true);
            var ids = rows.Select(r => r.MineralId).ToList();
            var expected = catalog.Minerals.Concat(catalog.RareItems)
                .Where(m => m != null)
                .Select(m => m.Id)
                .ToList();
            Assert.That(ids, Is.EquivalentTo(expected));

            foreach (var row in rows)
            {
                Assert.That(row.transform.Find("Icon").GetComponent<Image>().sprite, Is.Not.Null, row.MineralId);
                Assert.That(row.transform.Find("UnitWeight"), Is.Not.Null, row.MineralId);
                Assert.That(row.transform.Find("LineWeight"), Is.Not.Null, row.MineralId);
            }
        }

        [Test]
        public void Builder_TargetsOnlyInventoryPanelPrefab()
        {
            var text = System.IO.File.ReadAllText(System.IO.Path.Combine(
                Application.dataPath, "_Project", "Editor", "DataValidation", "PromptB112InventoryPanelBuilder.cs"));
            Assert.That(text, Does.Not.Contain(".unity\""));
            Assert.That(text, Does.Not.Contain("SurfaceBasePanel.prefab"));
            Assert.That(text, Does.Not.Contain("BuildingMenu.prefab"));
            Assert.That(text, Does.Not.Contain("FindAssets(\"t:Prefab"));
        }

        [Test]
        public void EngineFuelRowBuilder_ReappliesManifestLayout()
        {
            // B-102 엔진 연료 행 추가가 옛 행 스타일을 복제해 명세표를 되돌리지 않아야 한다.
            var text = System.IO.File.ReadAllText(System.IO.Path.Combine(
                Application.dataPath, "_Project", "Editor", "DataValidation", "PromptB102SealedGlyphBuilder.cs"));
            Assert.That(text, Does.Contain("PanelRoot/StackScroll/Viewport/StackRows"));
            Assert.That(text, Does.Contain("PromptB112InventoryPanelBuilder.ApplyTo(root)"));
        }
    }
}
