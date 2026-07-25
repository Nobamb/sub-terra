using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.Gameplay.Player.Tests
{
    public sealed class PlayerMovementPlayModeTests
    {
        private GameObject playerObject;
        private PlayerMovement movement;
        private Rigidbody2D body;
        private GameObject groundObject;

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
    }
}
