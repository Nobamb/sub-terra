using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.State;
using UnityEngine;

namespace SubTerra.App.Tests.Integration
{
    /// <summary>prompt-B 37: 플레이어 승하강에 따른 HUD 깊이 실시간 반영.</summary>
    public sealed class GameplayDepthStatusBridgeTests
    {
        [Test]
        public void BindGameState_PublishesDepthFromPlayerY()
        {
            var player = new GameObject("DepthTestPlayer");
            var host = new GameObject("DepthBridgeHost");
            try
            {
                player.transform.position = new Vector3(0f, -12f, 0f);
                var bridge = host.AddComponent<GameplayDepthStatusBridge>();
                var state = GameState.CreateNew();
                var received = -1;
                state.DepthChanged += depth => received = depth;

                bridge.SetSurfaceY(0f);
                bridge.SetPlayer(player.transform);
                bridge.BindGameState(state);

                Assert.That(state.Run.Depth, Is.EqualTo(12));
                Assert.That(received, Is.EqualTo(12));
                Assert.That(bridge.LastPublishedDepth, Is.EqualTo(12));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Refresh_UpdatesDepthWhenPlayerMovesUpAndDown()
        {
            var player = new GameObject("DepthTestPlayer");
            var host = new GameObject("DepthBridgeHost");
            try
            {
                player.transform.position = new Vector3(0f, -20f, 0f);
                var bridge = host.AddComponent<GameplayDepthStatusBridge>();
                var state = GameState.CreateNew();
                bridge.SetSurfaceY(0f);
                bridge.SetPlayer(player.transform);
                bridge.BindGameState(state);
                Assert.That(state.Run.Depth, Is.EqualTo(20));

                // 하강 → 깊이 증가
                player.transform.position = new Vector3(0f, -35f, 0f);
                bridge.Refresh();
                Assert.That(state.Run.Depth, Is.EqualTo(35));
                Assert.That(state.Run.MaximumDepth, Is.EqualTo(35));

                // 상승 → 현재 깊이는 줄고, 최고 깊이는 유지
                player.transform.position = new Vector3(0f, -3f, 0f);
                bridge.Refresh();
                Assert.That(state.Run.Depth, Is.EqualTo(3));
                Assert.That(state.Run.MaximumDepth, Is.EqualTo(35));

                // 지표면 이상 → 0m
                player.transform.position = new Vector3(0f, 2f, 0f);
                bridge.Refresh();
                Assert.That(state.Run.Depth, Is.EqualTo(0));
                Assert.That(state.Run.MaximumDepth, Is.EqualTo(35));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Refresh_DoesNotRaiseEventWhenDepthUnchanged()
        {
            var player = new GameObject("DepthTestPlayer");
            var host = new GameObject("DepthBridgeHost");
            try
            {
                player.transform.position = new Vector3(0f, -8f, 0f);
                var bridge = host.AddComponent<GameplayDepthStatusBridge>();
                var state = GameState.CreateNew();
                bridge.SetSurfaceY(0f);
                bridge.SetPlayer(player.transform);
                bridge.BindGameState(state);

                var eventCount = 0;
                state.DepthChanged += _ => eventCount++;
                bridge.Refresh();
                bridge.Refresh();

                Assert.That(eventCount, Is.EqualTo(0));
                Assert.That(state.Run.Depth, Is.EqualTo(8));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(host);
            }
        }
    }
}
