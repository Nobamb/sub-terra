using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    /// <summary>HUD와 목표 UI가 서로 다른 세로 영역을 사용하도록 회귀 방지한다.</summary>
    public sealed class HudQuestLayoutStaticTests
    {
        [Test]
        public void PhaseQLayout_SeparatesHudRowsFromObjectiveArea()
        {
            var path = Path.Combine(
                Application.dataPath,
                "_Project",
                "Editor",
                "DataValidation",
                "PhaseQPanelLayoutBuilder.cs");
            var source = File.ReadAllText(path);

            Assert.That(source, Does.Contain("-28f - (i * 30f)"));
            Assert.That(source, Does.Contain("0.72f, 0.76f"));
            Assert.That(source, Does.Contain("0.64f, 0.69f"));
            Assert.That(source, Does.Contain("0.58f, 0.62f"));
        }
    }
}
