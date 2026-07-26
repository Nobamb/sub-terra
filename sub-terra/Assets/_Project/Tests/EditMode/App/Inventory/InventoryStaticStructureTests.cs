using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Inventory;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.Inventory
{
    /// <summary>D-S01~S05 정적/구조 검증.</summary>
    public sealed class InventoryStaticStructureTests
    {
        [Test]
        public void D_S01_ImplementsIMiningRewardReceiver_WithoutChangingShared()
        {
            Assert.That(typeof(IMiningRewardReceiver).IsAssignableFrom(typeof(InventoryService)), Is.True);

            var method = typeof(IMiningRewardReceiver).GetMethod(nameof(IMiningRewardReceiver.AddMineral));
            Assert.That(method, Is.Not.Null);
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
            var parameters = method.GetParameters();
            Assert.That(parameters.Length, Is.EqualTo(2));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string)));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(int)));

            // Shared 파일에 시그니처 변경 흔적이 없어야 한다.
            var sharedPath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "Shared",
                "Contracts",
                "IMiningRewardReceiver.cs");
            Assert.That(File.Exists(sharedPath), Is.True);
            var text = File.ReadAllText(sharedPath);
            Assert.That(text, Does.Contain("void AddMineral(string mineralId, int quantity)"));
        }

        [Test]
        public void D_S02_WeightValueCalculation_LivesOnlyInInventoryLayer()
        {
            var calcPath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "Inventory",
                "InventoryCalculator.cs");
            Assert.That(File.Exists(calcPath), Is.True);
            var calcText = File.ReadAllText(calcPath);
            Assert.That(calcText, Does.Contain("ComputeTotalWeight"));
            Assert.That(calcText, Does.Contain("ComputeUnsettledValue"));

            // HUD formatter는 포맷만 하고 수량×단가 합산을 하지 않는다.
            var hudFmt = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "UI",
                "HUD",
                "HudFormatter.cs");
            var hudText = File.ReadAllText(hudFmt);
            Assert.That(hudText, Does.Not.Contain("UnitWeight"));
            Assert.That(hudText, Does.Not.Contain("UnitPrice"));
            Assert.That(hudText, Does.Not.Contain("ComputeTotalWeight"));

            var panelPresenter = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "UI",
                "Inventory",
                "InventoryPanelPresenter.cs");
            var panelText = File.ReadAllText(panelPresenter);
            Assert.That(panelText, Does.Not.Contain("ComputeTotalWeight"));
            Assert.That(panelText, Does.Not.Contain("UnitPrice *"));
        }

        [Test]
        public void D_S03_PublicApi_DoesNotExposeMutableDictionary()
        {
            var publicProps = typeof(InventoryService)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var prop in publicProps)
            {
                Assert.That(prop.PropertyType.Name, Does.Not.Contain("Dictionary"));
                if (prop.PropertyType.IsGenericType)
                {
                    var def = prop.PropertyType.GetGenericTypeDefinition();
                    Assert.That(def, Is.Not.EqualTo(typeof(System.Collections.Generic.Dictionary<,>)));
                }
            }

            var publicFields = typeof(InventoryService)
                .GetFields(BindingFlags.Instance | BindingFlags.Public);
            Assert.That(publicFields.Length, Is.Zero);

            // InventoryState.Quantities is internal only
            var qty = typeof(InventoryState).GetProperty(
                "Quantities",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(qty, Is.Null);
        }

        [Test]
        public void D_S04_SuccessPath_DocumentsSingleEvent()
        {
            var servicePath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "Inventory",
                "InventoryService.cs");
            var text = File.ReadAllText(servicePath);
            Assert.That(text, Does.Contain("RaiseChangedOnce"));
            // 서비스 상세 이벤트 발행 지점은 RaiseChangedOnce 한 곳뿐이어야 한다.
            var invokeCount = System.Text.RegularExpressions.Regex.Matches(
                text,
                @"InventoryChanged\?\.Invoke").Count;
            Assert.That(invokeCount, Is.EqualTo(1), "Service should invoke InventoryChanged in one place.");
        }

        [Test]
        public void D_S05_ExceptionPolicy_ResultTypesDefined()
        {
            var names = System.Enum.GetNames(typeof(InventoryMutationStatus));
            Assert.That(names, Does.Contain("Success"));
            Assert.That(names, Does.Contain("PartialAccept"));
            Assert.That(names, Does.Contain("InvalidId"));
            Assert.That(names, Does.Contain("InvalidQuantity"));
            Assert.That(names, Does.Contain("OverflowRisk"));
            Assert.That(names, Does.Contain("CapacityFull"));
            Assert.That(names, Does.Contain("Insufficient"));

            var resultType = typeof(InventoryMutationResult);
            Assert.That(resultType.GetProperty("AcceptedQuantity"), Is.Not.Null);
            Assert.That(resultType.GetProperty("RejectedQuantity"), Is.Not.Null);
            Assert.That(resultType.GetProperty("DidChange"), Is.Not.Null);
            Assert.That(resultType.GetProperty("Diagnostic"), Is.Not.Null);
        }

        [Test]
        public void RequiredInventoryScripts_ExistUnderAppOwnership()
        {
            var invDir = Path.Combine(Application.dataPath, "_Project", "Scripts", "App", "Inventory");
            var uiDir = Path.Combine(Application.dataPath, "_Project", "Scripts", "App", "UI", "Inventory");
            var requiredInv = new[]
            {
                "InventoryService.cs",
                "InventoryState.cs",
                "InventoryCalculator.cs",
                "InventoryMutationResult.cs",
                "InventorySnapshot.cs",
                "IMineralCatalogLookup.cs",
                "InMemoryMineralCatalog.cs",
                "GameDataCatalogMineralLookup.cs"
            };
            var requiredUi = new[]
            {
                "InventoryPanelView.cs",
                "InventoryPanelPresenter.cs",
                "InventoryPanelBinder.cs",
                "IInventoryPanelView.cs"
            };

            foreach (var name in requiredInv)
            {
                Assert.That(File.Exists(Path.Combine(invDir, name)), Is.True, name);
            }

            foreach (var name in requiredUi)
            {
                Assert.That(File.Exists(Path.Combine(uiDir, name)), Is.True, name);
            }
        }

        [Test]
        public void InventoryPanelSources_DoNotPollInUpdate()
        {
            var root = Path.Combine(Application.dataPath, "_Project", "Scripts", "App", "UI", "Inventory");
            foreach (var file in Directory.GetFiles(root, "*.cs"))
            {
                var text = File.ReadAllText(file);
                Assert.That(text, Does.Not.Contain("void Update("));
                Assert.That(text, Does.Not.Contain("void Update ()"));
            }
        }

        [Test]
        public void SharedAndGameplayFolders_NotModifiedByInventoryTypes()
        {
            // 컴파일 타임 소유권: InventoryService는 App 네임스페이스
            Assert.That(typeof(InventoryService).Namespace, Is.EqualTo("SubTerra.App.Inventory"));
            Assert.That(typeof(IMiningRewardReceiver).Namespace, Is.EqualTo("SubTerra.Shared"));
        }
    }
}
