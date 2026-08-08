using System.IO;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    /// <summary>
    /// PR-2: Surface Base sell chrome 권위 좌표 + Sell 자식 존재 + 빌더 경로 범위.
    /// </summary>
    public sealed class PromptBSellPanelLayoutTests
    {
        [Test]
        public void Builder_AppliesAuthorityCoordinates_AndSellChildren()
        {
            var report = PromptB_SellPanelLayoutBuilder.Build();
            Assert.That(report, Does.Contain("Sell").Or.Contain("SurfaceBase"));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PromptB_SellPanelLayoutBuilder.SurfaceBasePrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var content = prefab.transform.Find("SurfaceBaseContent") ?? prefab.transform;
            var economy = content.Find("EconomyPanel");
            Assert.That(economy, Is.Not.Null, "EconomyPanel missing");

            var ecoRect = economy as RectTransform;
            Assert.That(ecoRect, Is.Not.Null);
            Assert.That(ecoRect.sizeDelta.x, Is.EqualTo(PromptB_SellPanelLayoutBuilder.EconomyW).Within(0.5f));
            Assert.That(ecoRect.sizeDelta.y, Is.EqualTo(PromptB_SellPanelLayoutBuilder.EconomyH).Within(0.5f));
            Assert.That(ecoRect.anchoredPosition.y, Is.EqualTo(PromptB_SellPanelLayoutBuilder.EconomyY).Within(0.5f));

            Assert.That(FindDeep(economy, "SellListViewport"), Is.Not.Null);
            Assert.That(FindDeep(economy, "SellListContent"), Is.Not.Null);
            Assert.That(FindDeep(economy, "QtyMinusButton"), Is.Not.Null);
            Assert.That(FindDeep(economy, "QtyPlusButton"), Is.Not.Null);
            Assert.That(FindDeep(economy, "QtyMaxButton"), Is.Not.Null);
            Assert.That(FindDeep(economy, "SellSelectedButton"), Is.Not.Null);
            Assert.That(FindDeep(economy, "SellAllButton"), Is.Not.Null);
            Assert.That(FindDeep(economy, "CreditsLabel"), Is.Not.Null);
            Assert.That(FindDeep(economy, "PreviewText"), Is.Not.Null);

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
                }
            }

            // gap 산술: Message.yMin - Economy.yMax >= 16
            var message = FindDeep(content, "MessageText") as RectTransform;
            if (message != null)
            {
                var msgYMin = message.anchoredPosition.y - message.sizeDelta.y * 0.5f;
                var ecoYMax = ecoRect.anchoredPosition.y + ecoRect.sizeDelta.y * 0.5f;
                Assert.That(msgYMin - ecoYMax, Is.GreaterThanOrEqualTo(15.5f));
            }
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
