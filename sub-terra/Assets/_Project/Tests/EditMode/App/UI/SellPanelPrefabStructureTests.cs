using System.IO;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>
    /// Structural check: SurfaceBase sell chrome hierarchy + layout components.
    /// Proves VLG/ScrollRect/EcoStatus coords without requiring Unity layout builder run.
    /// </summary>
    public sealed class SellPanelPrefabStructureTests
    {
        public void BuildSurfaceBaseSellPanelPrefab()
        {
            var report = PromptB_SellPanelLayoutBuilder.Build();
            Assert.That(report, Does.Contain("Sell").Or.Contain("SurfaceBase"));
        }

        private static string PrefabText(string fileName)
        {
            var path = Path.Combine(Application.dataPath, "_Project", "Prefabs", "UI", fileName);
            Assert.That(File.Exists(path), Is.True, path);
            return File.ReadAllText(path);
        }

        [Test]
        public void SurfaceBasePanel_ContainsSellChromeHierarchyAndViewBindings()
        {
            var text = PrefabText("SurfaceBasePanel.prefab");

            var requiredNames = new[]
            {
                "SellListViewport",
                "SellListContent",
                "OpenSellButton",
                "SellModalCard",
                "CloseSellButton",
                "SellDescription",
                "SellSelectedButton",
                "SellAllButton",
                "CreditsLabel",
                "QtyMinusButton",
                "QtyPlusButton",
                "QtyMaxButton",
                "PreviewText",
                "EmptySellText",
                "SellTitle",
                "QtyText"
            };
            foreach (var name in requiredNames)
            {
                Assert.That(text, Does.Contain("m_Name: " + name), name);
            }

            Assert.That(text, Does.Contain("creditsLabelText:"));
            Assert.That(text, Does.Contain("sellSelectedButton:"));
            Assert.That(text, Does.Contain("sellListContent:"));
            Assert.That(text, Does.Contain("qtyMinusButton:"));
            Assert.That(text, Does.Contain("previewText:"));
            Assert.That(text, Does.Contain("openSellButton:"));
            Assert.That(text, Does.Contain("closeSellButton:"));
            Assert.That(text, Does.Contain("m_SizeDelta: {x: 760, y: 520}"));
            Assert.That(text, Does.Contain("m_SizeDelta: {x: 760, y: 220}"));
            Assert.That(text, Does.Contain("m_AnchoredPosition: {x: 0, y: -146}"));
        }

        [Test]
        public void SurfaceBasePanel_SellListHasVerticalLayoutAndScrollRect()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab");
            Assert.That(prefab, Is.Not.Null);

            var transforms = prefab.GetComponentsInChildren<Transform>(true);
            var content = transforms.FirstOrDefault(item => item.name == "SellListContent");
            var viewport = transforms.FirstOrDefault(item => item.name == "SellListViewport");
            var modal = transforms.FirstOrDefault(item => item.name == "SellModalCard");
            Assert.That(content, Is.Not.Null);
            Assert.That(viewport, Is.Not.Null);
            Assert.That(modal, Is.Not.Null);

            // SellListContent must drive stacked rows (not all at y=0).
            Assert.That(content.GetComponent<VerticalLayoutGroup>(), Is.Not.Null);
            var fitter = content.GetComponent<ContentSizeFitter>();
            Assert.That(fitter, Is.Not.Null);
            Assert.That(fitter.verticalFit, Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize));

            // SellModalCard ScrollRect wires viewport + content.
            var scroll = modal.GetComponent<ScrollRect>();
            Assert.That(scroll, Is.Not.Null);
            Assert.That(scroll.content, Is.SameAs(content as RectTransform));
            Assert.That(scroll.viewport, Is.SameAs(viewport as RectTransform));

            // Status band stays inside the opaque modal card.
            var status = transforms.FirstOrDefault(item => item.name == "EcoStatus") as RectTransform;
            var detail = transforms.FirstOrDefault(item => item.name == "EcoDetail") as RectTransform;
            Assert.That(status, Is.Not.Null);
            Assert.That(detail, Is.Not.Null);
            Assert.That(status.anchoredPosition.y, Is.EqualTo(-145f).Within(0.5f));
            Assert.That(detail.anchoredPosition.y, Is.EqualTo(-181f).Within(0.5f));
        }

        [Test]
        public void EconomySellRowPrefab_ExistsWithRowView()
        {
            var text = PrefabText("EconomySellRow.prefab");
            Assert.That(text, Does.Contain("EconomySellRowView"));
            Assert.That(text, Does.Contain("nameText:"));
            Assert.That(text, Does.Contain("ownedText:"));
            Assert.That(text, Does.Contain("selectButton:"));
            Assert.That(text, Does.Contain("m_Name: Name"));
            Assert.That(text, Does.Contain("m_Name: Owned"));
            Assert.That(text, Does.Contain("m_Name: UnitPrice"));
        }
    }
}
