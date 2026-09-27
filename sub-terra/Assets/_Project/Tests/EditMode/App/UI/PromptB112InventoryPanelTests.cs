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
            Assert.That(InventoryPanelDisplayFormatter.StateLine(42.5f, 50f), Does.Contain("거의 가득").And.Contain("7.5"));
            Assert.That(InventoryPanelDisplayFormatter.StateLine(50f, 50f), Does.Contain("가득 참"));
            Assert.That(InventoryPanelDisplayFormatter.UnsettledAmount(98765f), Is.EqualTo("98,765"));
            Assert.That(InventoryPanelDisplayFormatter.QuantityState(0), Is.EqualTo("보유 없음"));
            Assert.That(InventoryPanelDisplayFormatter.OwnedKinds(3, 4), Is.EqualTo("보유 3 / 4종"));
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
                "cargoFillImage", "unsettledAmountText", "ownedKindsText"
            })
            {
                Assert.That(so.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            }

            var scroll = prefab.transform.Find("PanelRoot/StackScroll").GetComponent<ScrollRect>();
            Assert.That(scroll.content, Is.SameAs(prefab.transform.Find("PanelRoot/StackScroll/Viewport/StackRows")));
            Assert.That(scroll.horizontal, Is.False);

            // B-68 빌더가 다시 실행돼도 도움말 아이콘은 적재량 카드 안에 있어야 한다.
            var help = (RectTransform)prefab.transform.Find("PanelRoot/WeightHelpIcon");
            Assert.That(help.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(help.anchoredPosition.x, Is.InRange(
                PromptB112InventoryPanelBuilder.Margin,
                PromptB112InventoryPanelBuilder.Margin + PromptB112InventoryPanelBuilder.CargoCardWidth - help.sizeDelta.x));

            var tooltip = prefab.transform.Find("PanelRoot/WeightTooltip");
            Assert.That(tooltip.GetSiblingIndex(), Is.EqualTo(tooltip.parent.childCount - 1));
        }

        [Test]
        public void Prefab_HasOneRowPerCatalogItemWithStateText()
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
                Assert.That(row.transform.Find("State"), Is.Not.Null, row.MineralId);
                Assert.That(row.transform.Find("UnitWeight"), Is.Not.Null, row.MineralId);
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
