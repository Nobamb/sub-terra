using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Economy;
using SubTerra.App.UI.Economy;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.Economy
{
    /// <summary>E-S01~S05 정적/구조 검증.</summary>
    public sealed class EconomyStaticStructureTests
    {
        [Test]
        public void E_S01_IResourceWallet_CanAffordReadOnly_TrySpendMutatesOnlyOnSuccess()
        {
            Assert.That(typeof(IResourceWallet).IsAssignableFrom(typeof(EconomyService)), Is.True);

            var canAfford = typeof(IResourceWallet).GetMethod(nameof(IResourceWallet.CanAfford));
            var trySpend = typeof(IResourceWallet).GetMethod(nameof(IResourceWallet.TrySpend));
            Assert.That(canAfford, Is.Not.Null);
            Assert.That(trySpend, Is.Not.Null);
            Assert.That(canAfford.ReturnType, Is.EqualTo(typeof(bool)));
            Assert.That(trySpend.ReturnType, Is.EqualTo(typeof(bool)));

            var walletPath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "Shared",
                "Contracts",
                "IResourceWallet.cs");
            Assert.That(File.Exists(walletPath), Is.True);
            var text = File.ReadAllText(walletPath);
            Assert.That(text, Does.Contain("bool CanAfford"));
            Assert.That(text, Does.Contain("bool TrySpend"));
            Assert.That(text, Does.Contain("상태를 바꾸지 않"));
        }

        [Test]
        public void E_S02_SellUsesCatalogPrice_NoUiPriceParameter()
        {
            var sell = typeof(EconomyService).GetMethod(nameof(EconomyService.TrySellMineral));
            Assert.That(sell, Is.Not.Null);
            var parameters = sell.GetParameters();
            Assert.That(parameters.Length, Is.EqualTo(2));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string)));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(int)));
            // 단가/가격 파라미터 없음
            Assert.That(parameters.All(p => p.Name != "price" && p.Name != "unitPrice"), Is.True);

            var servicePath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "Economy",
                "EconomyService.cs");
            var text = File.ReadAllText(servicePath);
            Assert.That(text, Does.Contain("UnitPrice"));
            Assert.That(text, Does.Contain("카탈로그"));
        }

        [Test]
        public void E_S03_NoPartialApplyPath_UsesTryReduceManyOrSingleCommit()
        {
            var servicePath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "Economy",
                "EconomyService.cs");
            var text = File.ReadAllText(servicePath);
            Assert.That(text, Does.Contain("TryReduceMany"));
            Assert.That(text, Does.Contain("사전"));
            // 루프 안에서 개별 TryReduceMineral을 호출하는 부분 차감 패턴이 없어야 한다.
            Assert.That(text, Does.Not.Contain("for (var i = 0; i < normalized.Count; i++)\r\n            {\r\n                inventory.TryReduceMineral"));

            var invPath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "Inventory",
                "InventoryService.cs");
            var invText = File.ReadAllText(invPath);
            Assert.That(invText, Does.Contain("TryReduceMany"));
            Assert.That(invText, Does.Contain("전 항목 검증"));
        }

        [Test]
        public void E_S04_CostAggregator_ExistsAndDocumentsMerge()
        {
            var path = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "Economy",
                "CostAggregator.cs");
            Assert.That(File.Exists(path), Is.True);
            var text = File.ReadAllText(path);
            Assert.That(text, Does.Contain("TryNormalize"));
            Assert.That(text, Does.Contain("합산"));
        }

        [Test]
        public void E_S05_UiPresenter_DoesNotMutateStateOrInventoryDirectly()
        {
            var presenterPath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "UI",
                "Economy",
                "EconomyPanelPresenter.cs");
            Assert.That(File.Exists(presenterPath), Is.True);
            var text = File.ReadAllText(presenterPath);

            // State/Inventory 직접 쓰기 API 호출 금지 — 서비스 호출만.
            // 표시용 Credits 접두/접미는 View API에서 허용 (SetCreditsLabel / SetPreviewCredits).
            Assert.That(text, Does.Not.Contain("SetGold"));
            Assert.That(text, Does.Not.Contain("AddGold"));
            Assert.That(text, Does.Not.Contain("TryReduceMineral"));
            Assert.That(text, Does.Not.Contain("TryAddMineral"));
            Assert.That(text, Does.Not.Contain("SetQuantity"));
            Assert.That(text, Does.Contain("TrySellMineral"));
            Assert.That(text, Does.Contain("TryCraftBuilding"));
            Assert.That(text, Does.Contain("busy"));
            Assert.That(text, Does.Contain("suppressListRebuildFromInventory"));
            Assert.That(text, Does.Contain("suppressStatusFromTransactions"));

            // View 계약: mutation 토큰 금지. Credits 표시 API는 허용.
            var methods = typeof(IEconomyPanelView).GetMethods();
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (var method in methods)
            {
                names.Add(method.Name);
                Assert.That(method.Name, Does.Not.Contain("Gold"));
                Assert.That(method.Name, Does.Not.Contain("Inventory"));
                Assert.That(method.Name, Does.Not.Contain("Spend"));
            }

            Assert.That(names, Does.Contain("SetSellRows"));
            Assert.That(names, Does.Contain("SetPreviewCredits"));
            Assert.That(names, Does.Contain("SetCreditsLabel"));
            Assert.That(names, Does.Contain("SetSellActionsEnabled"));
            Assert.That(names, Does.Contain("SetEmptySellState"));
        }

        [Test]
        public void E_S05_RequiredSellScripts_Exist()
        {
            var ecoDir = Path.Combine(Application.dataPath, "_Project", "Scripts", "App", "Economy");
            var uiDir = Path.Combine(Application.dataPath, "_Project", "Scripts", "App", "UI", "Economy");
            Assert.That(File.Exists(Path.Combine(ecoDir, "ISellGate.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(ecoDir, "EconomyPricing.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(uiDir, "SellMineralRowReadModel.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(uiDir, "EconomySellRowView.cs")), Is.True);
        }

        [Test]
        public void RequiredEconomyScripts_ExistUnderAppOwnership()
        {
            var ecoDir = Path.Combine(Application.dataPath, "_Project", "Scripts", "App", "Economy");
            var uiDir = Path.Combine(Application.dataPath, "_Project", "Scripts", "App", "UI", "Economy");
            var sharedContracts = Path.Combine(Application.dataPath, "_Project", "Scripts", "Shared", "Contracts");
            var sharedDto = Path.Combine(Application.dataPath, "_Project", "Scripts", "Shared", "DTO");

            var requiredEco = new[]
            {
                "EconomyService.cs",
                "CraftingService.cs",
                "CostAggregator.cs",
                "EconomyTransactionResult.cs",
                "IBuildingPlacementGate.cs",
                "ItemCostMapping.cs"
            };
            var requiredUi = new[]
            {
                "IEconomyPanelView.cs",
                "EconomyPanelPresenter.cs",
                "EconomyPanelView.cs",
                "EconomyPanelBinder.cs"
            };

            foreach (var name in requiredEco)
            {
                Assert.That(File.Exists(Path.Combine(ecoDir, name)), Is.True, name);
            }

            foreach (var name in requiredUi)
            {
                Assert.That(File.Exists(Path.Combine(uiDir, name)), Is.True, name);
            }

            Assert.That(File.Exists(Path.Combine(sharedContracts, "IResourceWallet.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(sharedDto, "ItemCostDto.cs")), Is.True);
            Assert.That(typeof(EconomyService).Namespace, Is.EqualTo("SubTerra.App.Economy"));
            Assert.That(typeof(IResourceWallet).Namespace, Is.EqualTo("SubTerra.Shared"));
            Assert.That(typeof(ItemCostDto).Namespace, Is.EqualTo("SubTerra.Shared"));
        }

        [Test]
        public void CraftingService_PlaceBeforeSpend_OrderDocumented()
        {
            var path = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "Economy",
                "CraftingService.cs");
            var text = File.ReadAllText(path);
            Assert.That(text, Does.Contain("CanAfford"));
            Assert.That(text, Does.Contain("TryPlace"));
            Assert.That(text, Does.Contain("TrySpend"));

            // 소스 상 CanAfford → TryPlace → TrySpend 순서
            var iAfford = text.IndexOf("wallet.CanAfford");
            var iPlace = text.IndexOf("placement.TryPlace");
            var iSpend = text.IndexOf("wallet.TrySpend");
            Assert.That(iAfford, Is.GreaterThanOrEqualTo(0));
            Assert.That(iPlace, Is.GreaterThan(iAfford));
            Assert.That(iSpend, Is.GreaterThan(iPlace));
        }

        [Test]
        public void SharedItemCostDto_HasNoUnityEngineDependency()
        {
            var dtoPath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "Shared",
                "DTO",
                "ItemCostDto.cs");
            var text = File.ReadAllText(dtoPath);
            Assert.That(text, Does.Not.Contain("UnityEngine"));
            Assert.That(text, Does.Contain("ItemId"));
            Assert.That(text, Does.Contain("Quantity"));
        }
    }
}
