using NUnit.Framework;
using SubTerra.App.Core.Data;

namespace SubTerra.App.Tests.Data
{
    public sealed class DataIdRulesTests
    {
        [TestCase("mineral.copper")]
        [TestCase("building.outpost_core.basic")]
        [TestCase("upgrade.drill.speed")]
        public void ValidIds_Pass(string id)
        {
            Assert.That(DataIdRules.IsValidPermanentId(id), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("Mineral.Copper")]
        [TestCase("mineral copper")]
        [TestCase("mineral")]
        [TestCase(" mineral.copper")]
        [TestCase("mineral.copper ")]
        public void InvalidIds_Fail(string id)
        {
            Assert.That(DataIdRules.IsValidPermanentId(id), Is.False);
        }

        [Test]
        public void Normalize_TrimsAndLowercases()
        {
            Assert.That(DataIdRules.Normalize("  Mineral.Copper  "), Is.EqualTo("mineral.copper"));
        }
    }
}
