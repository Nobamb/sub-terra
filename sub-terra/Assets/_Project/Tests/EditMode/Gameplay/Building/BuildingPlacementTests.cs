using NUnit.Framework;
using UnityEngine;

namespace SubTerra.Gameplay.Building.Tests
{
    public sealed class BuildingPlacementTests
    {
        [Test]
        public void TestWallet_DoesNotSpendWhenEmpty()
        {
            GameObject host = new("Wallet");
            BuildingTestResourceWallet wallet = host.AddComponent<BuildingTestResourceWallet>();

            Assert.That(wallet.CanAfford("building.support"), Is.True);
            Assert.That(wallet.TrySpend("building.support"), Is.True);
            Assert.That(wallet.TrySpend("building.support"), Is.True);
            Assert.That(wallet.TrySpend("building.support"), Is.True);
            Assert.That(wallet.TrySpend("building.support"), Is.False);

            Object.DestroyImmediate(host);
        }

        [Test]
        public void PlacementResult_PreservesFailureAndCell()
        {
            var cell = new Vector3Int(4, 2, 0);
            var result = new BuildingPlacementResult(false, BuildingPlacementFailure.Occupied, string.Empty, "building.support", cell);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Failure, Is.EqualTo(BuildingPlacementFailure.Occupied));
            Assert.That(result.Cell, Is.EqualTo(cell));
        }
    }
}
