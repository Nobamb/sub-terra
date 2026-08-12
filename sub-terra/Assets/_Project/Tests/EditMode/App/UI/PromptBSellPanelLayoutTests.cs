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
        public void SurfaceBasePrefab_MatchesAuthorityCoordinates_AndSellChildren()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PromptB_SellPanelLayoutBuilder.SurfaceBasePrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var content = prefab.transform.Find("SurfaceBaseContent") ?? prefab.transform;
            var economy = content.Find("EconomyPanel");
            Assert.That(economy, Is.Not.Null, "EconomyPanel missing");

            var ecoRect = economy as RectTransform;
            Assert.That(ecoRect, Is.Not.Null);
            Assert.That(ecoRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(ecoRect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(ecoRect.sizeDelta, Is.EqualTo(Vector2.zero));

            var openSell = content.Find("OpenSellButton") as RectTransform;
            Assert.That(openSell, Is.Not.Null, "OpenSellButton missing");
            Assert.That(
                economy.GetSiblingIndex(),
                Is.GreaterThan(openSell.GetSiblingIndex()),
                "sell modal must remain after its opener; the runtime open path raises it above sibling panels");

            var canvasGroup = economy.GetComponent<CanvasGroup>();
            Assert.That(canvasGroup, Is.Not.Null);
            Assert.That(canvasGroup.alpha, Is.Zero);
            Assert.That(canvasGroup.blocksRaycasts, Is.False);
            Assert.That(economy.GetComponent<Image>().color.a, Is.EqualTo(1f), "modal backdrop must be opaque");

            Assert.That(openSell.anchoredPosition.x, Is.EqualTo(0f).Within(0.5f));
            Assert.That(openSell.anchoredPosition.y, Is.EqualTo(PromptB_SellPanelLayoutBuilder.SellButtonY).Within(0.5f));

            var card = economy.Find("SellModalCard") as RectTransform;
            Assert.That(card, Is.Not.Null, "SellModalCard missing");
            Assert.That(card.sizeDelta.x, Is.EqualTo(PromptB_SellPanelLayoutBuilder.EconomyW).Within(0.5f));
            Assert.That(card.sizeDelta.y, Is.EqualTo(PromptB_SellPanelLayoutBuilder.EconomyH).Within(0.5f));
            Assert.That(card.anchoredPosition.y, Is.EqualTo(PromptB_SellPanelLayoutBuilder.EconomyY).Within(0.5f));

            Assert.That(FindDeep(card, "SellListViewport"), Is.Not.Null);
            Assert.That(FindDeep(card, "SellListContent"), Is.Not.Null);
            Assert.That(FindDeep(card, "QtyMinusButton"), Is.Not.Null);
            Assert.That(FindDeep(card, "QtyPlusButton"), Is.Not.Null);
            Assert.That(FindDeep(card, "QtyMaxButton"), Is.Not.Null);
            Assert.That(FindDeep(card, "SellSelectedButton"), Is.Not.Null);
            Assert.That(FindDeep(card, "SellAllButton"), Is.Not.Null);
            Assert.That(FindDeep(card, "CreditsLabel"), Is.Not.Null);
            Assert.That(FindDeep(card, "PreviewText"), Is.Not.Null);
            Assert.That(FindDeep(card, "CloseSellButton"), Is.Not.Null);
            Assert.That(FindDeep(card, "SellDescription"), Is.Not.Null);

            // Progression 축소
            var progression = content.Find("ProgressionPanel");
            if (progression != null)
            {
                var pRect = progression as RectTransform;
                Assert.That(pRect.sizeDelta.x, Is.EqualTo(PromptB_SellPanelLayoutBuilder.ProgressionW).Within(0.5f));
                Assert.That(pRect.sizeDelta.y, Is.EqualTo(PromptB_SellPanelLayoutBuilder.ProgressionH).Within(0.5f));
                Assert.That(pRect.anchoredPosition.y, Is.EqualTo(PromptB_SellPanelLayoutBuilder.ProgressionY).Within(0.5f));

                var upgradeList = FindDeep(progression, "UpgradeList") as RectTransform;
                if (upgradeList != null)
                {
                    Assert.That(upgradeList.sizeDelta.y, Is.LessThanOrEqualTo(200f));
                    Assert.That(upgradeList.sizeDelta.y, Is.GreaterThanOrEqualTo(160f));
                    Assert.That(
                        upgradeList.GetComponent<TMP_Text>().alignment,
                        Is.EqualTo(TextAlignmentOptions.Top),
                        "level summary must be horizontally centered");
                }
            }

            // 기본 화면 gap: OpenSell 아래와 Message 위가 겹치지 않는다.
            var message = FindDeep(content, "MessageText") as RectTransform;
            if (message != null)
            {
                var sellYMin = openSell.anchoredPosition.y - openSell.sizeDelta.y * 0.5f;
                var msgYMax = message.anchoredPosition.y + message.sizeDelta.y * 0.5f;
                Assert.That(sellYMin - msgYMax, Is.GreaterThanOrEqualTo(16f));
            }

            var title = FindDeep(content, "Title") as RectTransform;
            var goals = FindDeep(content, "GoalsText") as RectTransform;
            var energy = FindDeep(content, "EnergyText") as RectTransform;
            var deepZone = FindDeep(content, "DeepZoneText") as RectTransform;
            var recentRun = FindDeep(content, "RecentRunText") as RectTransform;
            var explore = FindDeep(content, "ExploreButton") as RectTransform;
            Assert.That(title, Is.Not.Null, "Surface Base title missing");
            Assert.That(title.gameObject.activeSelf, Is.True, "Surface Base title must be visible");
            Assert.That(title.anchoredPosition.y,
                Is.EqualTo(PromptB_SellPanelLayoutBuilder.TitleY).Within(0.5f));
            Assert.That(title.GetComponent<TMP_Text>().text, Is.EqualTo("Surface Base"));
            AssertBelowWithGap(title, goals, 16f);
            AssertBelowWithGap(goals, energy, 16f);
            AssertBelowWithGap(energy, deepZone, 16f);
            AssertBelowWithGap(deepZone, recentRun, 16f);
            AssertBelowWithGap(recentRun, explore, 16f);
            AssertBelowWithGap(explore, openSell, 16f);
            AssertBelowWithGap(openSell, message, 16f);
            AssertBelowWithGap(message, progression as RectTransform, 16f);

            foreach (var centered in new[]
                     {
                         goals,
                         energy,
                         deepZone,
                         recentRun,
                         openSell,
                         message,
                         progression as RectTransform
                     })
            {
                Assert.That(centered.anchoredPosition.x, Is.EqualTo(0f).Within(0.5f), centered.name + " must be centered");
            }

            var settings = FindDeep(content, "SettingsButton") as RectTransform;
            var quit = FindDeep(content, "QuitButton") as RectTransform;
            var actionLeft = explore.anchoredPosition.x - explore.sizeDelta.x * 0.5f;
            var actionRight = quit.anchoredPosition.x + quit.sizeDelta.x * 0.5f;
            Assert.That((actionLeft + actionRight) * 0.5f, Is.EqualTo(0f).Within(0.5f), "action row must be centered");
            Assert.That(settings.anchoredPosition.y, Is.EqualTo(explore.anchoredPosition.y).Within(0.5f));
            Assert.That(quit.anchoredPosition.y, Is.EqualTo(explore.anchoredPosition.y).Within(0.5f));
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

                var progression = FindDeep(instance.transform, "ProgressionPanel");
                progression.SetAsLastSibling();
                Assert.That(economy.GetSiblingIndex(), Is.Not.EqualTo(economy.parent.childCount - 1));

                open.onClick.Invoke();
                Assert.That(group.alpha, Is.EqualTo(1f));
                Assert.That(group.interactable, Is.True);
                Assert.That(group.blocksRaycasts, Is.True);
                Assert.That(economy.GetSiblingIndex(), Is.EqualTo(economy.parent.childCount - 1));
                Assert.That(progression.gameObject.activeSelf, Is.False,
                    "level summary must be hidden while the sell modal is open");

                close.onClick.Invoke();
                Assert.That(group.alpha, Is.Zero);
                Assert.That(group.interactable, Is.False);
                Assert.That(group.blocksRaycasts, Is.False);
                Assert.That(progression.gameObject.activeSelf, Is.True,
                    "level summary must be restored after the sell modal closes");
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
