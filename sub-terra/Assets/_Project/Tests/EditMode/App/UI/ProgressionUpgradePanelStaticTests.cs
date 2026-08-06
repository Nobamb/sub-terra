using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    /// <summary>U 업그레이드 창의 선택·구매 가능 표시 연결을 회귀 방지한다.</summary>
    public sealed class ProgressionUpgradePanelStaticTests
    {
        [Test]
        public void UpgradePanel_HasSelectableEntriesAndAffordabilityGate()
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            var viewPath = Path.Combine(
                root,
                "Assets",
                "_Project",
                "Scripts",
                "App",
                "UI",
                "Progression",
                "ProgressionPanelView.cs");
            var entryPath = Path.Combine(
                root,
                "Assets",
                "_Project",
                "Scripts",
                "App",
                "UI",
                "Progression",
                "ProgressionUpgradeEntryButton.cs");
            var binderPath = Path.Combine(
                root,
                "Assets",
                "_Project",
                "Scripts",
                "App",
                "UI",
                "Progression",
                "ProgressionPanelBinder.cs");
            var builderPath = Path.Combine(
                root,
                "Assets",
                "_Project",
                "Editor",
                "DataValidation",
                "PhaseQPanelLayoutBuilder.cs");

            Assert.That(File.Exists(entryPath), Is.True);
            Assert.That(File.ReadAllText(entryPath), Does.Contain("binder?.SelectUpgrade(upgradeId)"));
            Assert.That(File.ReadAllText(viewPath), Does.Contain("selectedCanAfford"));
            Assert.That(File.ReadAllText(binderPath), Does.Contain("presenter?.Refresh()"));
            Assert.That(File.ReadAllText(builderPath), Does.Contain("CreateUpgradeEntries"));
        }
    }
}
