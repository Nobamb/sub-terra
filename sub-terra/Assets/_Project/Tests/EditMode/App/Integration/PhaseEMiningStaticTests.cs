using System.IO;
using NUnit.Framework;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.Integration
{
    public sealed class PhaseEMiningStaticTests
    {
        [Test]
        public void E_S01_MiningData_ContainsTimeLevelAndEnergyFields()
        {
            Assert.That(typeof(MiningTileDto).GetField("miningTime"), Is.Not.Null);
            Assert.That(typeof(MiningTileDto).GetField("requiredDrillLevel"), Is.Not.Null);
            Assert.That(typeof(MiningTileDto).GetField("energyCost"), Is.Not.Null);
        }

        [Test]
        public void E_S02_KeyboardAndMouseController_UseStartTickCompleteFlow()
        {
            var source = Read("Scripts", "Gameplay", "Mining", "PlayerMiningController.cs");
            Assert.That(source, Does.Contain("TryStartMiningAtWorldPoint"));
            Assert.That(source, Does.Contain("TryStartMiningFrom"));
            Assert.That(source, Does.Contain("TickMining"));
            Assert.That(source, Does.Not.Contain("TryMineInstant"));
        }

        [Test]
        public void E_S03_CargoSpeed_IsSubscribedToInventoryChanges()
        {
            var source = Read("Scripts", "App", "Integration", "IntegrationRuntimeBinder.cs");
            Assert.That(source, Does.Contain("InventoryChanged += OnInventoryChangedForMovement"));
            Assert.That(source, Does.Contain("CargoSpeedPolicy.Evaluate"));
        }

        [Test]
        public void E_S04_ProgressHudAndRuntimeReferences_AreSerialized()
        {
            var prefab = Read("Prefabs", "UI", "HUDCanvas.prefab");
            var scene = Read("Scenes", "App", "Mine_Demo_Integration.unity");
            Assert.That(prefab, Does.Contain("MiningProgressHud"));
            Assert.That(prefab, Does.Contain("MiningProgressStatus"));
            Assert.That(prefab, Does.Contain("MiningProgressFill"));
            Assert.That(scene, Does.Match(@"miningTransactionBehaviour: \{fileID: [1-9]"));
            Assert.That(scene, Does.Match(@"miningProgressHud: \{fileID: [1-9]"));
            Assert.That(scene, Does.Contain("requiredDrillLevel: 2"));
            Assert.That(scene, Does.Contain("energyCost: 3"));
        }

        [Test]
        public void E_S05_ProgressHud_FollowsPlayerAndUsesMiningProgressAsFill()
        {
            var source = Read("Scripts", "App", "Integration", "MiningProgressHud.cs");
            var binder = Read("Scripts", "App", "Integration", "IntegrationRuntimeBinder.cs");

            Assert.That(source, Does.Contain("float progress = Mathf.Clamp01(state.Progress)"));
            Assert.That(source, Does.Contain("progressFillRect.localScale"));
            Assert.That(source, Does.Contain("state.Phase == MiningPhase.Mining"));
            Assert.That(source, Does.Contain("playerCollider.bounds.max.y"));
            Assert.That(source, Does.Contain("screenOffset = new(0f, 20f)"));
            Assert.That(binder, Does.Contain("playerMovement.transform"));
        }

        [Test]
        public void PromptB65_DeepZoneLockedFailure_UsesRequestedMessage()
        {
            var source = Read("Scripts", "App", "Integration", "MiningProgressHud.cs");

            Assert.That(source, Does.Contain(
                "심층 구역이 해금되어야 채굴할 수 있는 자원입니다."));
        }

        private static string Read(params string[] parts)
        {
            var path = Path.Combine(Application.dataPath, "_Project");
            for (var index = 0; index < parts.Length; index++)
            {
                path = Path.Combine(path, parts[index]);
            }

            return File.ReadAllText(path);
        }
    }
}
