using System.IO;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Economy;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.App.UI.Economy;
using SubTerra.Shared;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>
    /// PR-2: Surface Base sell chrome 권위 좌표 + Sell 자식 존재 + 빌더 경로 범위.
    /// </summary>
    public sealed class PromptBSellPanelLayoutTests
    {
        public void BuildSurfaceBaseSellPanelPrefab()
        {
            var report = PromptB_SellPanelLayoutBuilder.Build();
            Assert.That(report, Does.Contain("Sell").Or.Contain("SurfaceBase"));
        }

        [Test]
        public void SurfaceBasePrefab_PreservesSellModalWithPrompt117Actions()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PromptB_SellPanelLayoutBuilder.SurfaceBasePrefabPath);
            var content = prefab.transform.Find("SurfaceBaseContent");
            var economy = content.Find("EconomyPanel");
            Assert.That(economy, Is.Not.Null);
            var group = economy.GetComponent<CanvasGroup>();
            Assert.That(group.alpha, Is.Zero);
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(economy.GetComponent<Image>().color.a, Is.EqualTo(1f));
            var card = economy.Find("SellModalCard") as RectTransform;
            Assert.That(card.sizeDelta, Is.EqualTo(new Vector2(
                PromptB_SellPanelLayoutBuilder.EconomyW, PromptB_SellPanelLayoutBuilder.EconomyH)));
            foreach (var name in new[] { "SellListViewport", "SellListContent", "QtyMinusButton", "QtyPlusButton",
                "QtyMaxButton", "SellSelectedButton", "SellAllButton", "CreditsLabel", "PreviewText", "CloseSellButton" })
                Assert.That(FindDeep(card, name), Is.Not.Null, name);
            var explore = content.Find("ExploreButton") as RectTransform;
            var sell = content.Find("OpenSellButton") as RectTransform;
            var upgrade = content.Find("UpgradeButton") as RectTransform;
            var reset = content.Find("ResetMineButton") as RectTransform;
            var message = content.Find("MessageText") as RectTransform;
            AssertBelowWithGap(explore, sell, 16f);
            AssertBelowWithGap(sell, reset, 16f);
            AssertBelowWithGap(reset, message, 6f);
            Assert.That(sell.anchoredPosition.x, Is.EqualTo(-upgrade.anchoredPosition.x));
            Assert.That(sell.anchoredPosition.y, Is.EqualTo(upgrade.anchoredPosition.y));
            Assert.That(content.Find("UpgradeModal").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Builder_OnlyTouchesDocumentedSurfaceBasePaths()
        {
            var builderPath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Editor",
                "DataValidation",
                "PromptB_SellPanelLayoutBuilder.cs");
            Assert.That(File.Exists(builderPath), Is.True);
            var text = File.ReadAllText(builderPath);

            Assert.That(text, Does.Contain("SurfaceBasePanel.prefab"));
            Assert.That(text, Does.Contain("SurfaceBase.unity"));
            Assert.That(text, Does.Contain("EconomySellRow.prefab"));
            // 다른 패널 경로를 저장하지 않음
            Assert.That(text, Does.Not.Contain("MainMenuPanel.prefab"));
            Assert.That(text, Does.Not.Contain("BuildingMenu.prefab"));
            Assert.That(text, Does.Not.Contain("InventoryPanel.prefab"));
        }

        [Test]
        public void EconomyPanelView_OpenAndCloseButtons_ToggleOpaqueModal()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PromptB_SellPanelLayoutBuilder.SurfaceBasePrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var economy = FindDeep(instance.transform, "EconomyPanel");
                var open = FindDeep(instance.transform, "OpenSellButton").GetComponent<Button>();
                var close = FindDeep(economy, "CloseSellButton").GetComponent<Button>();
                var view = economy.GetComponent<EconomyPanelView>();
                var group = economy.GetComponent<CanvasGroup>();

                Assert.That(view, Is.Not.Null);
                Assert.That(group, Is.Not.Null);

                // EditMode 인스턴스에서도 런타임 버튼 배선을 명시적으로 실행한다.
                InvokeAwake(view);

                var progression = FindDeep(instance.transform, "UpgradeModal");
                progression.SetAsLastSibling();
                Assert.That(economy.GetSiblingIndex(), Is.Not.EqualTo(economy.parent.childCount - 1));

                open.onClick.Invoke();
                Assert.That(group.alpha, Is.EqualTo(1f));
                Assert.That(group.interactable, Is.True);
                Assert.That(group.blocksRaycasts, Is.True);
                Assert.That(economy.GetSiblingIndex(), Is.EqualTo(economy.parent.childCount - 1));
                Assert.That(progression.gameObject.activeSelf, Is.False,
                    "upgrade modal must stay closed while selling");

                close.onClick.Invoke();
                Assert.That(group.alpha, Is.Zero);
                Assert.That(group.interactable, Is.False);
                Assert.That(group.blocksRaycasts, Is.False);
                Assert.That(progression.gameObject.activeSelf, Is.False,
                    "closing sales must not open upgrades");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void EconomyPanelView_RowSelectionQuantityAndSelectedSale_WorkThroughPrefabButtons()
        {
            const string copper = "mineral.copper";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PromptB_SellPanelLayoutBuilder.SurfaceBasePrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var economy = FindDeep(instance.transform, "EconomyPanel");
                var view = economy.GetComponent<EconomyPanelView>();
                var binder = economy.GetComponent<EconomyPanelBinder>();
                InvokeAwake(view);
                InvokeAwake(binder);

                var catalog = new InMemoryMineralCatalog();
                catalog.Register(copper, 1f, 10, "구리");
                var state = GameState.CreateNew();
                var inventory = new InventoryService(catalog, 100f, state);
                Assert.That(
                    inventory.TryAddMineral(copper, 3).Status,
                    Is.EqualTo(InventoryMutationStatus.Success));
                var service = new EconomyService(inventory, catalog, state);
                binder.BindTo(service, null, inventory, state);

                var content = FindDeep(economy, "SellListContent");
                var row = content.GetComponentInChildren<EconomySellRowView>(true);
                Assert.That(row, Is.Not.Null);
                // EditMode Instantiate는 행의 런타임 Awake를 자동 호출하지 않으므로 명시적으로 재현한다.
                InvokeAwake(row);
                var rowButton = row.GetComponent<Button>();
                Assert.That(rowButton.targetGraphic.enabled, Is.True, "unselected row must remain raycastable");

                rowButton.onClick.Invoke();
                Assert.That(binder.Presenter.SelectedMineralId, Is.EqualTo(copper));

                FindDeep(economy, "QtyPlusButton").GetComponent<Button>().onClick.Invoke();
                Assert.That(binder.Presenter.SellQuantity, Is.EqualTo(2));

                FindDeep(economy, "SellSelectedButton").GetComponent<Button>().onClick.Invoke();
                Assert.That(inventory.State.GetQuantity(copper), Is.EqualTo(1));
                Assert.That(state.Player.Gold, Is.EqualTo(20));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void InvokeAwake(MonoBehaviour behaviour)
        {
            behaviour.GetType()
                .GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(behaviour, null);
        }

        private static void AssertBelowWithGap(RectTransform upper, RectTransform lower, float minimumGap)
        {
            Assert.That(upper, Is.Not.Null);
            Assert.That(lower, Is.Not.Null);
            var upperBottom = upper.anchoredPosition.y - upper.sizeDelta.y * 0.5f;
            var lowerTop = lower.anchoredPosition.y + lower.sizeDelta.y * 0.5f;
            Assert.That(upperBottom - lowerTop, Is.GreaterThanOrEqualTo(minimumGap));
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == name)
            {
                return parent;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var found = FindDeep(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
