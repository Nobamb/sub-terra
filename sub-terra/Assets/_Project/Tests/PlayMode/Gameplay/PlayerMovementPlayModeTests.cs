using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Player;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Player.Tests
{
    public sealed class PlayerMovementPlayModeTests
    {
        private GameObject playerObject;
        private PlayerMovement movement;
        private Rigidbody2D body;
        private GameObject groundObject;
        private GameObject wallObject;
        private Tile wallTile;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("PlayerMovementTest");
            body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            movement = playerObject.AddComponent<PlayerMovement>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(groundObject);
            Object.DestroyImmediate(wallObject);
            Object.DestroyImmediate(wallTile);
        }

        [UnityTest]
        public IEnumerator MoveInputAcceleratesPlayerToTheRight()
        {
            movement.SetMoveInput(1f);

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.Greater(body.linearVelocityX, 0f);
            Assert.AreEqual(1f, movement.FacingDirection);
        }

        [UnityTest]
        public IEnumerator ReleasingInputDeceleratesPlayer()
        {
            movement.SetMoveInput(1f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            float movingSpeed = body.linearVelocityX;

            movement.SetMoveInput(0f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.Less(Mathf.Abs(body.linearVelocityX), Mathf.Abs(movingSpeed));
        }

        [Test]
        public void SpeedMultipliersAreCombined()
        {
            movement.SetCargoSpeedMultiplier(0.8f);
            movement.SetHazardSpeedMultiplier(0.5f);

            Assert.AreEqual(0.4f, movement.CurrentSpeedMultiplier, 0.0001f);
        }

        [Test]
        public void NegativeSpeedMultiplierIsClampedToZero()
        {
            movement.SetCargoSpeedMultiplier(-1f);

            Assert.AreEqual(0f, movement.CurrentSpeedMultiplier);
        }

        [TestCase(49f, 100f, CargoSpeedPolicy.LightLoadMultiplier)]
        [TestCase(50f, 100f, CargoSpeedPolicy.MediumLoadMultiplier)]
        [TestCase(79.9f, 100f, CargoSpeedPolicy.MediumLoadMultiplier)]
        [TestCase(80f, 100f, CargoSpeedPolicy.HeavyLoadMultiplier)]
        [TestCase(100f, 100f, CargoSpeedPolicy.HeavyLoadMultiplier)]
        public void E_F04_CargoWeight_UsesThreePrdSpeedSteps(
            float current,
            float maximum,
            float expected)
        {
            Assert.AreEqual(expected, CargoSpeedPolicy.Evaluate(current, maximum), 0.0001f);
        }

        [UnityTest]
        public IEnumerator JumpRequestAddsUpwardVelocityWhenGrounded()
        {
            SetupGroundedPlayerWithContacts(airLockDuration: 0.05f);

            // 물리 접점 생성
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.IsTrue(movement.IsGrounded, "바닥에 올려둔 플레이어는 착지 상태여야 한다.");

            movement.RequestJump();
            yield return new WaitForFixedUpdate();

            Assert.Greater(body.linearVelocityY, 0f);
        }

        [UnityTest]
        public IEnumerator Jump_IsLimitedToOnceUntilLanding()
        {
            SetupGroundedPlayerWithContacts(airLockDuration: 0.05f);

            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.IsTrue(movement.IsGrounded);

            movement.RequestJump();
            yield return new WaitForFixedUpdate();
            Assert.Greater(body.linearVelocityY, 0f);

            // 공중 연타 불가
            body.linearVelocity = new Vector2(0f, 2f);
            movement.RequestJump();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(2f, body.linearVelocityY, 0.01f);

            // 지면 제거 후에도 점프 차지 회복 불가
            yield return new WaitForSeconds(0.1f);
            Object.DestroyImmediate(groundObject);
            groundObject = null;
            Physics2D.SyncTransforms();
            body.linearVelocity = new Vector2(0f, -0.2f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            for (int i = 0; i < 5; i++)
            {
                float before = body.linearVelocityY;
                movement.RequestJump();
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(
                    before,
                    body.linearVelocityY,
                    0.01f,
                    "공중에서는 점프 차지가 회복되면 안 된다.");
            }
        }

        [UnityTest]
        public IEnumerator Jump_CanJumpAgainAfterLandingOnGround()
        {
            SetupGroundedPlayerWithContacts(airLockDuration: 0.05f);

            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.IsTrue(movement.IsGrounded);

            // 1차 점프
            movement.RequestJump();
            yield return new WaitForFixedUpdate();
            Assert.Greater(body.linearVelocityY, 0f);

            // 공중 불가
            body.linearVelocity = new Vector2(0f, 1.5f);
            movement.RequestJump();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(1.5f, body.linearVelocityY, 0.01f);

            // 다시 바닥 위에 올려 착지 접점 생성
            yield return new WaitForSeconds(0.1f);
            body.linearVelocity = Vector2.zero;
            body.position = new Vector2(0f, 0.5f);
            Physics2D.SyncTransforms();
            for (int i = 0; i < 4; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.IsTrue(movement.IsGrounded, "재착지 후 IsGrounded 가 true 여야 한다.");

            // 2차 점프 가능
            movement.RequestJump();
            yield return new WaitForFixedUpdate();
            Assert.Greater(body.linearVelocityY, 0f);

            // 다시 공중 불가
            body.linearVelocity = new Vector2(0f, 1.2f);
            movement.RequestJump();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(1.2f, body.linearVelocityY, 0.01f);
        }

        /// <summary>
        /// Rigidbody 접점 기반 착지 판정을 위해 플레이어 콜라이더 + 정적 바닥을 배치한다.
        /// </summary>
        private void SetupGroundedPlayerWithContacts(float airLockDuration = 0.12f)
        {
            body.gravityScale = 1f;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.position = new Vector2(0f, 0.5f);

            var capsule = playerObject.GetComponent<CapsuleCollider2D>();
            if (capsule == null)
            {
                capsule = playerObject.AddComponent<CapsuleCollider2D>();
            }

            capsule.size = new Vector2(0.5f, 0.9f);

            Transform groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(playerObject.transform, false);
            groundCheck.localPosition = new Vector3(0f, -0.45f, 0f);
            typeof(PlayerMovement)
                .GetField("groundCheck", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(movement, groundCheck);
            typeof(PlayerMovement)
                .GetField("jumpAirLockDuration", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(movement, airLockDuration);

            // Awake 이후 추가된 콜라이더를 바디가 인식하도록 재할당
            typeof(PlayerMovement)
                .GetField("bodyCollider", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(movement, capsule);

            groundObject = new GameObject("Ground");
            groundObject.transform.position = new Vector3(0f, -0.25f, 0f);
            var groundCollider = groundObject.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2(8f, 0.5f);
            var groundBody = groundObject.AddComponent<Rigidbody2D>();
            groundBody.bodyType = RigidbodyType2D.Static;
            Physics2D.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator Jump_WorksWhileClimbingLadder()
        {
            body.gravityScale = 3f;
            movement.EnterLadder();
            Assert.IsTrue(movement.IsClimbing);

            movement.RequestJump();
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(movement.IsClimbing);
            Assert.Greater(body.linearVelocityY, 0f);
            Assert.AreEqual(3f, body.gravityScale);
        }

        [UnityTest]
        public IEnumerator TilemapWallStopsThePlayerInsteadOfAllowingPassThrough()
        {
            body.position = new Vector2(-1f, 0.5f);
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CapsuleCollider2D playerCollider = playerObject.AddComponent<CapsuleCollider2D>();
            playerCollider.size = new Vector2(0.6f, 0.7f);

            wallObject = new GameObject("WallGrid");
            wallObject.AddComponent<Grid>();
            GameObject tilemapObject = new("WallTilemap");
            tilemapObject.transform.SetParent(wallObject.transform);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            tilemapObject.AddComponent<TilemapCollider2D>();
            Rigidbody2D wallBody = tilemapObject.AddComponent<Rigidbody2D>();
            wallBody.bodyType = RigidbodyType2D.Static;
            wallTile = ScriptableObject.CreateInstance<Tile>();
            wallTile.colliderType = Tile.ColliderType.Grid;
            for (int y = -1; y <= 2; y++)
            {
                tilemap.SetTile(new Vector3Int(0, y, 0), wallTile);
            }

            Physics2D.SyncTransforms();
            movement.SetMoveInput(1f);
            for (int frame = 0; frame < 30; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.LessOrEqual(
                body.position.x,
                -0.29f,
                "The player's 0.6-wide capsule must stop at the wall's left face.");
        }

        [UnityTest]
        public IEnumerator LadderMode_ClimbsWithoutGravityAndRestoresPhysicsOnExit()
        {
            body.gravityScale = 3f;
            movement.EnterLadder();
            movement.SetVerticalMoveInput(1f);

            for (int frame = 0; frame < 10; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.IsTrue(movement.IsClimbing);
            Assert.AreEqual(0f, body.gravityScale);
            Assert.Greater(body.position.y, 0.5f);

            movement.ExitLadder();

            Assert.IsFalse(movement.IsClimbing);
            Assert.AreEqual(3f, body.gravityScale);
        }

        [Test]
        public void DisablingMovementWhileClimbing_RestoresGravity()
        {
            body.gravityScale = 2.5f;
            movement.EnterLadder();

            movement.enabled = false;

            Assert.AreEqual(2.5f, body.gravityScale);
            Assert.IsFalse(movement.IsClimbing);
        }
    }
}
