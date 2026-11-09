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
                Assert.That(panel.Find("OwnedKindsText").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("1종"));
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
                "cargoFillImage", "unsettledAmountText", "ownedKindsText", "emptyHintText", "columnHeaderRoot"
            })
            {
                Assert.That(so.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            }

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

            // 도움말 ?는 적재 현황 제목 옆, 도움말 본문은 창 아래(숫자 위를 덮지 않음).
            var help = (RectTransform)panel.Find("WeightHelpIcon");
            Assert.That(help.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(help.anchoredPosition.y, Is.InRange(
                card.anchoredPosition.y - card.sizeDelta.y, card.anchoredPosition.y));
            Assert.That(help.anchoredPosition.x + help.sizeDelta.x,
                Is.LessThan(card.anchoredPosition.x + amount.anchoredPosition.x + amount.sizeDelta.x));
            var tooltip = panel.Find("WeightTooltip");
            Assert.That(((RectTransform)tooltip).anchoredPosition.y, Is.LessThanOrEqualTo(-PromptB112InventoryPanelBuilder.PanelHeight));
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
    }
}
