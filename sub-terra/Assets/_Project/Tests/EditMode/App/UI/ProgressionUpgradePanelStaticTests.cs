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
            Assert.That(File.ReadAllText(entryPath), Does.Contain("IPointerClickHandler"));
            Assert.That(File.ReadAllText(entryPath), Does.Contain("SelectUpgradeEntry"));
            Assert.That(File.ReadAllText(viewPath), Does.Contain("selectedCanAfford"));
            Assert.That(File.ReadAllText(viewPath), Does.Contain("SelectCategoryTab"));
            var binderSource = File.ReadAllText(binderPath);
            Assert.That(binderSource, Does.Contain("presenter?.Refresh()")
                .Or.Contain("presenter.Refresh()"));
            // 비활성 패널에서 BindTo → Awake 순이어도 바인딩이 유지되어야 한다.
            Assert.That(binderSource, Does.Contain("if (presenter == null)"));
            Assert.That(binderSource, Does.Contain("IsBound"));
            Assert.That(File.ReadAllText(builderPath), Does.Contain("CreateUpgradeEntries"));

            var categoryPath = Path.Combine(
                root,
                "Assets",
                "_Project",
                "Scripts",
                "App",
                "Progression",
                "UpgradeCategory.cs");
            Assert.That(File.Exists(categoryPath), Is.True);
            var categorySource = File.ReadAllText(categoryPath);
            Assert.That(categorySource, Does.Contain("UpgradeCategory"));
            Assert.That(categorySource, Does.Contain("Drone"));
            // prompt-B 33-3: 심층 구역 전용 탭.
            Assert.That(categorySource, Does.Contain("DeepZone"));
            Assert.That(categorySource, Does.Contain("심층 구역"));

            var viewSource = File.ReadAllText(viewPath);
            Assert.That(viewSource, Does.Contain("EntryListStartY"));
            Assert.That(viewSource, Does.Contain("levelsOnlySummary"));
            Assert.That(viewSource, Does.Contain("hideDeepZoneTab"));
            // prompt-B 33-4/후속: 하위 탭 선택·설명 카드·심층 단일 텍스트.
            Assert.That(viewSource, Does.Contain("EnsureInteractable"));
            Assert.That(viewSource, Does.Contain("UpgradeDescription"));
            Assert.That(viewSource, Does.Contain("detailText.gameObject.SetActive(false)"));
            Assert.That(viewSource, Does.Contain("ApplyDeepZoneDisplay"));
            Assert.That(viewSource, Does.Contain("WireCategoryTabsRuntime"));
            Assert.That(viewSource, Does.Contain("RebuildEntryButtonCacheIfNeeded"));
            Assert.That(viewSource, Does.Contain("SelectUpgradeEntry"));
            Assert.That(viewSource, Does.Contain("BringToFront"));
            Assert.That(viewSource, Does.Contain("ModalPanel"));

            var namesPath = Path.Combine(
                root,
                "Assets",
                "_Project",
                "Scripts",
                "App",
                "Core",
                "Data",
                "ItemDisplayNames.cs");
            Assert.That(File.Exists(namesPath), Is.True);
            Assert.That(File.ReadAllText(namesPath), Does.Contain("UpgradeDescription"));

            var entrySource = File.ReadAllText(entryPath);
            Assert.That(entrySource, Does.Contain("EnsureInteractable"));
            Assert.That(entrySource, Does.Contain("GetComponentInParent"));
            Assert.That(entrySource, Does.Contain("RemoveListener(OnButtonClicked)"));
            Assert.That(entrySource, Does.Contain("AddListener(OnButtonClicked)"));
            Assert.That(entrySource, Does.Contain("IPointerClickHandler"));

            var presenterPath = Path.Combine(
                root,
                "Assets",
                "_Project",
                "Scripts",
                "App",
                "UI",
                "Progression",
                "ProgressionPanelPresenter.cs");
            var presenterSource = File.ReadAllText(presenterPath);
            Assert.That(presenterSource, Does.Contain("UpgradeCategoryRules.Resolve"));
            Assert.That(presenterSource, Does.Contain("SetActiveCategory"));
        }
    }
}
