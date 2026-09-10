using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.UI.Outpost;

namespace SubTerra.App.Tests.Outpost
{
    public sealed class OutpostMineralPickerFilterTests
    {
        [Test]
        public void Build_IncludesDefaultMineralsAndCargoQuantities()
        {
            var cargo = new InventorySnapshot(
                12f,
                100f,
                80f,
                new[]
                {
                    new InventoryStackEntry(DataIds.Minerals.Copper, "구리", 8, 1.5f, 10)
                });

            var options = OutpostMineralPickerFilter.Build(cargo, null);

            Assert.That(options.Count, Is.EqualTo(3));
            Assert.That(options[0].MineralId, Is.EqualTo(DataIds.Minerals.Copper));
            Assert.That(options[0].OwnedQuantity, Is.EqualTo(8));
            Assert.That(options[0].StoredQuantity, Is.Zero);
            Assert.That(options[1].MineralId, Is.EqualTo(DataIds.Minerals.Iron));
            Assert.That(options[2].MineralId, Is.EqualTo(DataIds.Minerals.Lithium));
        }

        [Test]
        public void Filter_PartialDisplayName_ReturnsMatchingMinerals()
        {
            var options = OutpostMineralPickerFilter.Build(null, null);

            var copper = OutpostMineralPickerFilter.Filter(options, "구");
            var lithium = OutpostMineralPickerFilter.Filter(options, "리튬");
            var ironById = OutpostMineralPickerFilter.Filter(options, "iron");
            var none = OutpostMineralPickerFilter.Filter(options, "xyz");
            var all = OutpostMineralPickerFilter.Filter(options, "  ");

            Assert.That(copper.Count, Is.EqualTo(1));
            Assert.That(copper[0].DisplayName, Is.EqualTo("구리"));
            Assert.That(lithium.Count, Is.EqualTo(1));
            Assert.That(lithium[0].MineralId, Is.EqualTo(DataIds.Minerals.Lithium));
            Assert.That(ironById.Count, Is.EqualTo(1));
            Assert.That(ironById[0].MineralId, Is.EqualTo(DataIds.Minerals.Iron));
            Assert.That(none, Is.Empty);
            Assert.That(all.Count, Is.EqualTo(3));
        }
    }
}
