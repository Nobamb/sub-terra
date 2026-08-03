using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SubTerra.App.Tests.Integration
{
    public sealed class PhaseHGasStaticTests
    {
        [Test]
        public void H_S03_IntegrationSceneWiresEffectControllerAndVisionOverlay()
        {
            var path = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scenes",
                "App",
                "Mine_Demo_Integration.unity");
            var scene = File.ReadAllText(path);

            Assert.That(scene, Does.Contain("SubTerra.App.Integration.GasExposureEffectController"));
            Assert.That(scene, Does.Contain("m_Name: GasVisionOverlay"));
            Assert.That(scene, Does.Contain("gasSystem: {fileID: 279480561}"));
            Assert.That(scene, Does.Contain("playerMovement: {fileID: 72969612}"));
            Assert.That(scene, Does.Not.Contain("visionOverlay: {fileID: 0}"));
        }

        [Test]
        public void H_S04_HazardHudUsesTextSymbolAndColorTogether()
        {
            var path = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "UI",
                "Hazards",
                "HazardHudView.cs");
            var source = File.ReadAllText(path);

            Assert.That(source, Does.Contain("var symbol"));
            Assert.That(source, Does.Contain("text.text = symbol"));
            Assert.That(source, Does.Contain("text.color = color"));
            Assert.That(source, Does.Contain("icon.color = color"));
        }
    }
}
