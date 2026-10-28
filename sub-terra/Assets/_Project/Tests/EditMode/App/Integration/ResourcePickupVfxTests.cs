using System;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.Gameplay.Mining;
using SubTerra.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Integration
{
    public sealed class ResourcePickupVfxTests
    {
        private GameObject host, player;
        private ResourcePickupVfx vfx;
        private MiningSystem mining;
        private Sprite sprite;
        private bool reduce;
        [SetUp]
        public void SetUp()
        {
            reduce = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
            host = new GameObject("PickupTest");
            player = new GameObject("PickupPlayer");
            mining = host.AddComponent<MiningSystem>();
            vfx = host.AddComponent<ResourcePickupVfx>();
            sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
            vfx.BindTo(mining, player.transform, _ => sprite);
        }
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(sprite);
            AccessibilityPreferences.ReduceMotion = reduce;
        }
        private void Mined(string id)
        {
            var handlers = (Action<Vector3Int, MiningTileDto>)typeof(MiningSystem)
                .GetField("TileMined", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(mining);
            handlers?.Invoke(Vector3Int.zero, new MiningTileDto { mineralId = id, quantity = 1 });
        }
        [Test]
        public void OnlyAcceptedMatchingResourcePlaysOnce_AndRebindingDoesNotDuplicate()
        {
            Mined(DataIds.Minerals.Copper);
            Assert.That(vfx.ActiveCount, Is.Zero);
            vfx.BindTo(mining, player.transform, _ => sprite);
            vfx.QueueAccepted(DataIds.Minerals.Copper, 2);
            Mined(DataIds.Minerals.Copper);
            Mined(DataIds.Minerals.Copper);
            Assert.That(vfx.ActiveCount, Is.EqualTo(1));
            vfx.QueueAccepted(DataIds.Minerals.Copper, 1);
            Mined(DataIds.Minerals.Iron);
            Mined(DataIds.Minerals.Copper);
            Assert.That(vfx.ActiveCount, Is.EqualTo(1), "Mismatched pending reward must be consumed, not leaked.");
            vfx.QueueAccepted(DataIds.Minerals.Copper, 0);
            Mined(DataIds.Minerals.Copper);
            Assert.That(vfx.ActiveCount, Is.EqualTo(1));
        }
        [Test]
        public void FailedOrCancelledMiningClearsPendingFeedback()
        {
            foreach (var phase in new[] { MiningPhase.Failed, MiningPhase.Cancelled })
            {
                vfx.QueueAccepted(DataIds.Minerals.Copper, 1);
                var handlers = (Action<MiningProgressState>)typeof(MiningSystem)
                    .GetField("ProgressChanged", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(mining);
                handlers(new MiningProgressState(phase, MiningFailureReason.None, 0f, 0f, 0));
                Mined(DataIds.Minerals.Copper);
                Assert.That(vfx.ActiveCount, Is.Zero);
            }
        }
        [Test]
        public void RareHasDistinctOutlineAndSparks_OrdinaryStaysQuiet_AndPoolIsBounded()
        {
            Assert.That(vfx.PlayAccepted(DataIds.Minerals.Copper, 1, Vector3.right), Is.True);
            var normalVisible = 0;
            foreach (var renderer in vfx.GetComponentsInChildren<SpriteRenderer>()) if (renderer.enabled) normalVisible++;
            Assert.That(normalVisible, Is.EqualTo(1));
            vfx.Unbind();
            vfx.BindTo(mining, player.transform, _ => sprite);
            vfx.PlayAccepted(DataIds.RareItems.EngineFuel, 1, Vector3.right);
            var rareVisible = 0;
            foreach (var renderer in vfx.GetComponentsInChildren<SpriteRenderer>()) if (renderer.enabled) rareVisible++;
            Assert.That(rareVisible, Is.EqualTo(5));
            for (var i = 0; i < 32; i++) vfx.PlayAccepted(DataIds.Minerals.Iron, 1, Vector3.right);
            Assert.That(vfx.AllocatedCount, Is.EqualTo(ResourcePickupVfx.Capacity));
            Assert.That(vfx.ActiveCount, Is.EqualTo(ResourcePickupVfx.Capacity));
            vfx.Tick(2f);
            Assert.That(vfx.ActiveCount, Is.Zero);
            Assert.That(vfx.AllocatedCount, Is.EqualTo(ResourcePickupVfx.Capacity));
        }
        [Test]
        public void ReduceMotionStaysAtPlayer_AndDisableHidesEverything()
        {
            AccessibilityPreferences.ReduceMotion = true;
            vfx.PlayAccepted(DataIds.RareItems.EngineFuel, 1, Vector3.left * 4f);
            vfx.Tick(0.05f);
            var icon = vfx.GetComponentInChildren<SpriteRenderer>();
            Assert.That(icon.transform.position.x, Is.EqualTo(player.transform.position.x));
            vfx.enabled = false;
            // 일반 MonoBehaviour의 EditMode 검사는 실제 비활성화 콜백을 실행하지 않는다.
            typeof(ResourcePickupVfx).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(vfx, null);
            Assert.That(vfx.ActiveCount, Is.Zero);
            Mined(DataIds.Minerals.Copper);
            Assert.That(vfx.ActiveCount, Is.Zero);
        }
    }
}
