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
            Transform groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(playerObject.transform);
            groundCheck.localPosition = new Vector3(0f, -0.9f, 0f);

            FieldInfo groundCheckField = typeof(PlayerMovement).GetField(
                "groundCheck",
                BindingFlags.Instance | BindingFlags.NonPublic);
            groundCheckField.SetValue(movement, groundCheck);

            groundObject = new GameObject("Ground");
            groundObject.transform.position = new Vector3(0f, -1.2f, 0f);
            BoxCollider2D groundCollider = groundObject.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2(4f, 0.5f);

            yield return null;
            Assert.IsTrue(movement.IsGrounded);

            movement.RequestJump();
            yield return new WaitForFixedUpdate();

            Assert.Greater(body.linearVelocityY, 0f);
        }

        [UnityTest]
        public IEnumerator Jump_IsLimitedToOnceUntilLanding()
        {
            body.gravityScale = 0f;
            Transform groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(playerObject.transform);
            groundCheck.localPosition = new Vector3(0f, -0.9f, 0f);
            typeof(PlayerMovement)
                .GetField("groundCheck", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(movement, groundCheck);

            groundObject = new GameObject("Ground");
            groundObject.transform.position = new Vector3(0f, -1.2f, 0f);
            groundObject.AddComponent<BoxCollider2D>().size = new Vector2(4f, 0.5f);

            yield return null;
            Assert.IsTrue(movement.IsGrounded);

            movement.RequestJump();
            yield return new WaitForFixedUpdate();
            float firstJumpVy = body.linearVelocityY;
            Assert.Greater(firstJumpVy, 0f);

            // 공중(또는 점프 직후)에서 연타해도 추가 임펄스가 적용되지 않아야 한다.
            body.linearVelocity = new Vector2(0f, 2f);
            movement.RequestJump();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(2f, body.linearVelocityY, 0.01f);

            // 지면 오버랩이 남아 있어도 에어 락 동안 재점프되면 안 된다 (31-3 무한 점프 회귀).
            body.linearVelocity = new Vector2(0f, 0f);
            for (int i = 0; i < 3; i++)
            {
                movement.RequestJump();
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(0f, body.linearVelocityY, 0.01f);
            }
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
