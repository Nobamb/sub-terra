using NUnit.Framework;
using SubTerra.App.Outpost;

namespace SubTerra.App.Tests.Outpost
{
    public sealed class OutpostTransferQuantityTests
    {
        [TestCase(10, 8, 8)]
        [TestCase(5, 8, 5)]
        [TestCase(8, 8, 8)]
        [TestCase(10, 0, 0)]
        [TestCase(0, 8, 0)]
        [TestCase(-1, 8, 0)]
        public void ClampToAvailable_UsesOwnedWhenRequestExceedsStock(
            int requested,
            int available,
            int expected)
        {
            Assert.That(
                OutpostTransferQuantity.ClampToAvailable(requested, available),
                Is.EqualTo(expected));
        }
    }
}
