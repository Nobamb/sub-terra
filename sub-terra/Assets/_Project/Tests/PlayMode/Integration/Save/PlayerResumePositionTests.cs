using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.Save;
using SubTerra.Gameplay.Player;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Tests.PlayMode.Save
{
    public sealed class PlayerResumePositionTests
    {
        private GameObject grid;
        private GameObject playerObject;
        private Tilemap terrain;
        private Tile tile;
        private PlayerMovement player;

        [SetUp]
        public void SetUp()
        {
            grid = new GameObject("ResumeGrid", typeof(Grid));
            var map = new GameObject("Terrain", typeof(Tilemap));
            map.transform.SetParent(grid.transform);
            terrain = map.GetComponent<Tilemap>();
            tile = ScriptableObject.CreateInstance<Tile>();
            for (var x = -5; x <= 5; x++) terrain.SetTile(new Vector3Int(x, -3, 0), tile);
            terrain.SetTile(new Vector3Int(-5, 3, 0), tile);
            terrain.SetTile(new Vector3Int(5, 3, 0), tile);
            playerObject = new GameObject("ResumePlayer");
            playerObject.transform.position = new Vector3(-3.5f, -1.5f, 0f);
            playerObject.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.8f);
            playerObject.AddComponent<Rigidbody2D>().gravityScale = 0f;
            player = playerObject.AddComponent<PlayerMovement>();
            Physics2D.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(grid);
            Object.DestroyImmediate(tile);
        }

        [Test]
        public void ValidPosition_RestoresExactCoordinatesAndResetsVelocity()
        {
            var body = player.GetComponent<Rigidbody2D>();
            body.linearVelocity = new Vector2(3f, -4f);
            var saved = Position(1.25f, -1.6f);
            Assert.That(PlayerResumePosition.TryRestore(player, terrain, saved), Is.True);
            Assert.That(player.Position, Is.EqualTo(new Vector2(saved.x, saved.y)));
            Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
            var captured = PlayerResumePosition.Capture(player);
            Assert.That(captured.x, Is.EqualTo(saved.x));
            Assert.That(captured.y, Is.EqualTo(saved.y));
        }

        [Test]
        public void MissingPosition_DoesNotMoveLegacySave()
        {
            var start = player.Position;
            Assert.That(PlayerResumePosition.TryRestore(player, terrain, null), Is.True);
            Assert.That(player.Position, Is.EqualTo(start));
        }

        [Test]
        public void BlockedPosition_FindsNearbySupportedEmptySpace()
        {
            terrain.SetTile(new Vector3Int(0, -2, 0), tile);
            Assert.That(PlayerResumePosition.TryRestore(player, terrain, Position(0.5f, -1.5f)), Is.True);
            Assert.That(player.Position, Is.Not.EqualTo(new Vector2(0.5f, -1.5f)));
            Assert.That(player.Position, Is.Not.EqualTo(new Vector2(-3.5f, -1.5f)));
            player.TryGetBodyBounds(out var bounds);
            Assert.That(terrain.HasTile(terrain.WorldToCell(bounds.center)), Is.False);
            Assert.That(terrain.HasTile(terrain.WorldToCell(new Vector2(bounds.center.x, bounds.min.y - 0.05f))), Is.True);
        }

        [Test]
        public void CollapsedFloor_ChoosesSupportedSpaceInsteadOfRestoringOverHole()
        {
            terrain.SetTile(new Vector3Int(0, -3, 0), null);
            Assert.That(PlayerResumePosition.TryRestore(player, terrain, Position(0.5f, -1.6f)), Is.True);
            Assert.That(player.Position, Is.Not.EqualTo(new Vector2(0.5f, -1.6f)));
        }

        [Test]
        public void OutsideWorld_FallsBackToExistingSceneSpawn()
        {
            var start = player.Position;
            Assert.That(PlayerResumePosition.TryRestore(player, terrain, Position(5000f, -5000f)), Is.True);
            Assert.That(player.Position, Is.EqualTo(start));
        }

        [Test]
        public void NoSafeSpace_ReportsFailureWithoutMovingOrChangingTerrain()
        {
            terrain.BoxFill(Vector3Int.zero, tile, -5, -3, 5, 3);
            var start = player.Position;
            Assert.That(PlayerResumePosition.TryRestore(player, terrain, Position(0.5f, -1.5f)), Is.False);
            Assert.That(player.Position, Is.EqualTo(start));
            Assert.That(terrain.HasTile(new Vector3Int(0, -2, 0)), Is.True);
        }

        private static PlayerPositionSaveData Position(float x, float y)
            => new PlayerPositionSaveData { hasPosition = true, x = x, y = y };
    }
}
