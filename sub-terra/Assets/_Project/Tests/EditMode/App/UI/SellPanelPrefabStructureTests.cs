using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    /// <summary>
    /// Structural check: SurfaceBase sell chrome hierarchy + layout components.
    /// Proves VLG/ScrollRect/EcoStatus coords without requiring Unity layout builder run.
    /// </summary>
    public sealed class SellPanelPrefabStructureTests
    {
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
            var text = PrefabText("SurfaceBasePanel.prefab");

            // SellListContent must drive stacked rows (not all at y=0).
            Assert.That(text, Does.Contain("UnityEngine.UI::UnityEngine.UI.VerticalLayoutGroup"));
            Assert.That(text, Does.Contain("UnityEngine.UI::UnityEngine.UI.ContentSizeFitter"));
            Assert.That(text, Does.Contain("m_VerticalFit: 2"), "ContentSizeFitter vertical preferred");
            Assert.That(
                text,
                Does.Contain("m_GameObject: {fileID: 9100000000000000014}"),
                "VLG/CSF target SellListContent GO");

            // SellModalCard ScrollRect wires viewport + content.
            Assert.That(text, Does.Contain("UnityEngine.UI::UnityEngine.UI.ScrollRect"));
            Assert.That(
                Regex.IsMatch(
                    text,
                    @"m_EditorClassIdentifier: UnityEngine\.UI::UnityEngine\.UI\.ScrollRect\s+m_Content: \{fileID: 9100000000000000015\}"),
                Is.True,
                "ScrollRect.m_Content = SellListContent");
            Assert.That(
                Regex.IsMatch(
                    text,
                    @"m_Viewport: \{fileID: 9100000000000000010\}"),
                Is.True,
                "ScrollRect.m_Viewport = SellListViewport");

            // Status band stays inside the opaque modal card.
            Assert.That(
                Regex.IsMatch(
                    text,
                    @"m_GameObject: \{fileID: 4956128646536738813\}[\s\S]{0,400}m_AnchoredPosition: \{x: 0, y: -145\}"),
                Is.True,
                "EcoStatus @ y=-145");
            Assert.That(
                Regex.IsMatch(
                    text,
                    @"m_GameObject: \{fileID: 4627569984472076596\}[\s\S]{0,400}m_AnchoredPosition: \{x: 0, y: -181\}"),
                Is.True,
                "EcoDetail @ y=-181");
            Assert.That(
                Regex.IsMatch(
                    text,
                    @"m_GameObject: \{fileID: 4956128646536738813\}[\s\S]{0,400}m_AnchoredPosition: \{x: 0, y: 10\}"),
                Is.False,
                "EcoStatus must not remain at y=10");
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
