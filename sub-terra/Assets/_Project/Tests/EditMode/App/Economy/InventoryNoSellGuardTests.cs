using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SubTerra.App.Tests.Economy
{
    /// <summary>
    /// PR-5: Mine 인벤토리 UI에 판매 API·Economy 배선이 없음을 소스 정적 검사.
    /// </summary>
    public sealed class InventoryNoSellGuardTests
    {
        [Test]
        public void InventoryUiSources_HaveNoSellApiOrEconomyServiceWiring()
        {
            var invDir = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "UI",
                "Inventory");
            Assert.That(Directory.Exists(invDir), Is.True, invDir);

            var files = Directory.GetFiles(invDir, "*.cs", SearchOption.AllDirectories);
            Assert.That(files.Length, Is.GreaterThan(0));

            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                var name = Path.GetFileName(file);
                Assert.That(text, Does.Not.Contain("TrySellMineral"), name);
                Assert.That(text, Does.Not.Contain("RequestSell"), name);
                Assert.That(text, Does.Not.Contain("RequestSellSelected"), name);
                Assert.That(text, Does.Not.Contain("RequestSellAll"), name);
                Assert.That(text, Does.Not.Contain("EconomyService"), name);
                Assert.That(text, Does.Not.Contain("ISellGate"), name);
            }
        }

        [Test]
        public void EconomyPanelViewContract_HasSellDisplayApis_NoMutationTokens()
        {
            var methods = typeof(SubTerra.App.UI.Economy.IEconomyPanelView).GetMethods();
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (var m in methods)
            {
                names.Add(m.Name);
                Assert.That(m.Name, Does.Not.Contain("Gold"), m.Name);
                Assert.That(m.Name, Does.Not.Contain("Inventory"), m.Name);
                Assert.That(m.Name, Does.Not.Contain("Spend"), m.Name);
            }

            Assert.That(names, Does.Contain("SetSellRows"));
            Assert.That(names, Does.Contain("SetSelectedMineral"));
            Assert.That(names, Does.Contain("SetSellQuantityControls"));
            Assert.That(names, Does.Contain("SetPreviewCredits"));
            Assert.That(names, Does.Contain("SetCreditsLabel"));
            Assert.That(names, Does.Contain("SetSellActionsEnabled"));
            Assert.That(names, Does.Contain("SetEmptySellState"));
        }
    }
}
