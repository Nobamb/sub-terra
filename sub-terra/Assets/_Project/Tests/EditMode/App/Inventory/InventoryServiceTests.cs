using NUnit.Framework;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Tests.Inventory
{
    /// <summary>
    /// D-F01~F03: 복수 광물 합산, 적재 경계 부분 수락, 잘못된 입력 원자성, 성공 시 이벤트 1회.
    /// 실제 InventoryService + InMemory 카탈로그를 사용한다(유닛 목 없음).
    /// </summary>
    public sealed class InventoryServiceTests
    {
        private const string Copper = "mineral.copper";
        private const string Iron = "mineral.iron";
        private const string Lithium = "mineral.lithium";

        private static InMemoryMineralCatalog CreateMvpCatalog()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(Copper, 1.5f, 10, "Copper");
            catalog.Register(Iron, 2f, 15, "Iron");
            catalog.Register(Lithium, 0.8f, 40, "Lithium");
            return catalog;
        }

        [Test]
        public void D_F01_MultiMineral_QuantitiesWeightValue_MatchFormula()
        {
            var catalog = CreateMvpCatalog();
            var service = new InventoryService(catalog, maxCapacity: 100f);

            // copper 3 → weight 4.5 value 30
            // iron 2 → weight 4 value 30
            // lithium 5 → weight 4 value 200
            var r1 = service.TryAddMineral(Copper, 3);
            var r2 = service.TryAddMineral(Iron, 2);
            var r3 = service.TryAddMineral(Lithium, 5);

            Assert.That(r1.Status, Is.EqualTo(InventoryMutationStatus.Success));
            Assert.That(r2.Status, Is.EqualTo(InventoryMutationStatus.Success));
            Assert.That(r3.Status, Is.EqualTo(InventoryMutationStatus.Success));

            Assert.That(service.State.GetQuantity(Copper), Is.EqualTo(3));
            Assert.That(service.State.GetQuantity(Iron), Is.EqualTo(2));
            Assert.That(service.State.GetQuantity(Lithium), Is.EqualTo(5));

            var expectedWeight = 3 * 1.5f + 2 * 2f + 5 * 0.8f;
            var expectedValue = 3 * 10 + 2 * 15 + 5 * 40;
            Assert.That(service.CurrentWeight, Is.EqualTo(expectedWeight).Within(0.0001f));
            Assert.That(service.UnsettledValue, Is.EqualTo(expectedValue).Within(0.0001f));

            // 계산 단일화: 스냅샷 수치 = 서비스 합산 = 수식
            var snap = service.GetSnapshot();
            Assert.That(snap.CurrentWeight, Is.EqualTo(service.CurrentWeight).Within(0.0001f));
            Assert.That(snap.UnsettledValue, Is.EqualTo(service.UnsettledValue).Within(0.0001f));
            Assert.That(snap.GetQuantity(Copper), Is.EqualTo(3));
            Assert.That(snap.GetQuantity(Iron), Is.EqualTo(2));
            Assert.That(snap.GetQuantity(Lithium), Is.EqualTo(5));
        }

        [Test]
        public void D_F02_CapacityBoundary_PartialAccept_NeverExceedsMax()
        {
            // max 10, copper unit 1.5 → floor(10/1.5)=6 units full
            // fill with 5 copper = 7.5, remaining 2.5 → max fit 1 unit (1.5)
            var catalog = CreateMvpCatalog();
            var service = new InventoryService(catalog, maxCapacity: 10f);

            Assert.That(service.TryAddMineral(Copper, 5).AcceptedQuantity, Is.EqualTo(5));
            Assert.That(service.CurrentWeight, Is.EqualTo(7.5f).Within(0.0001f));

            var over = service.TryAddMineral(Copper, 5);
            Assert.That(over.Status, Is.EqualTo(InventoryMutationStatus.PartialAccept));
            Assert.That(over.AcceptedQuantity, Is.EqualTo(1));
            Assert.That(over.RejectedQuantity, Is.EqualTo(4));
            Assert.That(service.State.GetQuantity(Copper), Is.EqualTo(6));
            Assert.That(service.CurrentWeight, Is.LessThanOrEqualTo(10f + 0.0001f));
            Assert.That(service.CurrentWeight, Is.EqualTo(9f).Within(0.0001f));

            // 더 이상 한 단위도 불가
            var full = service.TryAddMineral(Copper, 1);
            Assert.That(full.Status, Is.EqualTo(InventoryMutationStatus.CapacityFull));
            Assert.That(full.AcceptedQuantity, Is.Zero);
            Assert.That(full.RejectedQuantity, Is.EqualTo(1));
            Assert.That(service.State.GetQuantity(Copper), Is.EqualTo(6));
        }

        [Test]
        public void D_F03_InvalidInputs_LeaveStateAndEventsUnchanged()
        {
            var catalog = CreateMvpCatalog();
            var gameState = GameState.CreateNew();
            var service = new InventoryService(catalog, maxCapacity: 50f, gameState);
            service.TryAddMineral(Copper, 2);

            var before = service.State.CaptureFingerprint();
            var serviceEvents = 0;
            var gameEvents = 0;
            service.InventoryChanged += _ => serviceEvents++;
            gameState.InventoryChanged += _ => gameEvents++;

            // unknown id
            var unknown = service.TryAddMineral("mineral.unknown", 1);
            Assert.That(unknown.Status, Is.EqualTo(InventoryMutationStatus.InvalidId));
            Assert.That(unknown.DidChange, Is.False);

            // zero
            var zero = service.TryAddMineral(Copper, 0);
            Assert.That(zero.Status, Is.EqualTo(InventoryMutationStatus.InvalidQuantity));

            // negative
            var neg = service.TryAddMineral(Copper, -3);
            Assert.That(neg.Status, Is.EqualTo(InventoryMutationStatus.InvalidQuantity));

            // overflow risk: fill near max qty then add MaxValue
            var overflowCatalog = new InMemoryMineralCatalog();
            overflowCatalog.Register("mineral.bulk", 0.0001f, 1, "Bulk");
            var overflowService = new InventoryService(overflowCatalog, maxCapacity: 1e9f);
            // seed almost max int stack via internal set path isn't public — use many adds carefully
            // Instead: put existing high via repeated? too slow. Use reflection-free approach:
            // Add int.MaxValue/2 twice of a tiny weight mineral when capacity allows.
            // Simpler: force overflow by adding MaxValue when existing is 1.
            overflowService.TryAddMineral("mineral.bulk", 1);
            var overflow = overflowService.TryAddMineral("mineral.bulk", int.MaxValue);
            Assert.That(overflow.Status, Is.EqualTo(InventoryMutationStatus.OverflowRisk));
            Assert.That(overflowService.State.GetQuantity("mineral.bulk"), Is.EqualTo(1));

            Assert.That(service.State.CaptureFingerprint().Equals(before), Is.True);
            Assert.That(serviceEvents, Is.Zero);
            Assert.That(gameEvents, Is.Zero);
            Assert.That(service.State.GetQuantity(Copper), Is.EqualTo(2));
        }

        [Test]
        public void SuccessfulMutation_RaisesInventoryChangedExactlyOnce()
        {
            var catalog = CreateMvpCatalog();
            var gameState = GameState.CreateNew();
            var service = new InventoryService(catalog, maxCapacity: 50f, gameState);

            var serviceEvents = 0;
            var gameEvents = 0;
            InventorySnapshot lastSnap = null;
            InventoryReadModel lastHud = default;
            service.InventoryChanged += s =>
            {
                serviceEvents++;
                lastSnap = s;
            };
            gameState.InventoryChanged += m =>
            {
                gameEvents++;
                lastHud = m;
            };

            service.AddMineral(Copper, 2);

            Assert.That(serviceEvents, Is.EqualTo(1));
            Assert.That(gameEvents, Is.EqualTo(1));
            Assert.That(lastSnap.GetQuantity(Copper), Is.EqualTo(2));
            Assert.That(lastHud.CargoWeight, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(lastHud.UnsettledValue, Is.EqualTo(20f).Within(0.0001f));
            Assert.That(lastSnap.CurrentWeight, Is.EqualTo(lastHud.CargoWeight).Within(0.0001f));
            Assert.That(lastSnap.UnsettledValue, Is.EqualTo(lastHud.UnsettledValue).Within(0.0001f));
        }

        [Test]
        public void IMiningRewardReceiver_AddMineral_UsesSharedSignature()
        {
            var catalog = CreateMvpCatalog();
            IMiningRewardReceiver receiver = new InventoryService(catalog, 50f);

            receiver.AddMineral(Iron, 4);

            var concrete = (InventoryService)receiver;
            Assert.That(concrete.State.GetQuantity(Iron), Is.EqualTo(4));
            Assert.That(concrete.LastResult.Status, Is.EqualTo(InventoryMutationStatus.Success));
            Assert.That(concrete.LastResult.AcceptedQuantity, Is.EqualTo(4));
        }

        [Test]
        public void TryReduceMineral_AtomicSuccessAndInsufficient()
        {
            var catalog = CreateMvpCatalog();
            var service = new InventoryService(catalog, 50f);
            service.TryAddMineral(Copper, 5);

            var ok = service.TryReduceMineral(Copper, 2);
            Assert.That(ok.DidChange, Is.True);
            Assert.That(service.State.GetQuantity(Copper), Is.EqualTo(3));

            var before = service.State.CaptureFingerprint();
            var events = 0;
            service.InventoryChanged += _ => events++;
            var fail = service.TryReduceMineral(Copper, 10);
            Assert.That(fail.Status, Is.EqualTo(InventoryMutationStatus.Insufficient));
            Assert.That(service.State.CaptureFingerprint().Equals(before), Is.True);
            Assert.That(events, Is.Zero);
            Assert.That(service.State.GetQuantity(Copper), Is.EqualTo(3));
        }

        [Test]
        public void Snapshot_DoesNotExposeMutableDictionary()
        {
            var catalog = CreateMvpCatalog();
            var service = new InventoryService(catalog, 50f);
            service.TryAddMineral(Copper, 1);
            var snap = service.GetSnapshot();

            Assert.That(snap.Stacks.Count, Is.EqualTo(1));
            Assert.That(snap.GetQuantity(Copper), Is.EqualTo(1));
            // Stacks is IReadOnlyList — no public mutable dict on service
            Assert.That(typeof(InventoryService).GetProperty("Quantities"), Is.Null);
            Assert.That(typeof(InventoryService).GetProperty("Stacks"), Is.Null);
        }

        [Test]
        public void ZeroQuantityStacks_AreNotStored()
        {
            var catalog = CreateMvpCatalog();
            var service = new InventoryService(catalog, 50f);
            service.TryAddMineral(Copper, 2);
            service.TryReduceMineral(Copper, 2);

            Assert.That(service.State.GetQuantity(Copper), Is.Zero);
            Assert.That(service.GetSnapshot().Stacks.Count, Is.Zero);
            Assert.That(service.CurrentWeight, Is.EqualTo(0f).Within(0.0001f));
        }
    }
}
