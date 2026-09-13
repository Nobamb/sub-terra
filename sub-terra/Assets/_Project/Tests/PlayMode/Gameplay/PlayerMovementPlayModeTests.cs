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
        private GameObject firstLadderObject;
        private GameObject secondLadderObject;
        private GameObject animationVisualObject;
        private Sprite[] ladderAnimationFrames;
        private Sprite[] otherAnimationFrames;
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
            Object.DestroyImmediate(animationVisualObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(groundObject);
            Object.DestroyImmediate(wallObject);
            Object.DestroyImmediate(firstLadderObject);
            Object.DestroyImmediate(secondLadderObject);
            if (ladderAnimationFrames != null)
            {
                foreach (var frame in ladderAnimationFrames)
                {
                    Object.DestroyImmediate(frame);
                }
            }

            if (otherAnimationFrames != null)
            {
                foreach (var frame in otherAnimationFrames)
                {
                    Object.DestroyImmediate(frame);
                }
            }

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

        [TestCase(0f, 50f, 1f, 1f)]
        [TestCase(10f, 50f, 0.95f, 1.1f)]
        [TestCase(50f, 50f, 0.75f, 1.5f)]
        [TestCase(75f, 50f, 0.75f, 1.5f)]
        public void PromptB68_CargoLoadEffects_ScaleLinearlyAndClamp(
            float current,
            float maximum,
            float expectedJump,
            float expectedFallImpact)
        {
            Assert.That(
                CargoLoadEffectPolicy.EvaluateJumpMultiplier(current, maximum),
                Is.EqualTo(expectedJump).Within(0.0001f));
            Assert.That(
                CargoLoadEffectPolicy.EvaluateFallImpactMultiplier(current, maximum),
                Is.EqualTo(expectedFallImpact).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator PromptB68_FullCargo_AppliesSeventyFivePercentJumpImpulse()
        {
            SetupGroundedPlayerWithContacts(airLockDuration: 0.05f);
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            movement.SetCargoJumpMultiplier(
                CargoLoadEffectPolicy.EvaluateJumpMultiplier(50f, 50f));
            movement.RequestJump();
            yield return new WaitForFixedUpdate();

            Assert.That(movement.CurrentJumpMultiplier, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(body.linearVelocityY, Is.EqualTo(8.25f).Within(0.2f));
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
            Assert.LessOrEqual(body.linearVelocityY, 2f + 0.01f);

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
                Assert.LessOrEqual(
                    body.linearVelocityY,
                    before + 0.01f,
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
            Assert.LessOrEqual(body.linearVelocityY, 1.5f + 0.01f);

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
            Assert.LessOrEqual(body.linearVelocityY, 1.2f + 0.01f);
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
        public IEnumerator JumpingFromLadder_CanGrabSameLadderAgainWithoutLanding()
        {
            body.gravityScale = 3f;
            LadderZone ladder = CreateLadderZone("RegrabbableLadder", out firstLadderObject);
            movement.EnterLadder(ladder);

            movement.RequestJump();
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(movement.IsClimbing);
            Assert.Greater(body.linearVelocityY, 0f);

            yield return new WaitForSeconds(0.2f);
            movement.SetVerticalMoveInput(1f);
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(movement.IsClimbing, "지면에 착지하지 않아도 사다리 입력으로 다시 탑승해야 한다.");
            Assert.AreEqual(0f, body.gravityScale);
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

        [UnityTest]
        public IEnumerator MovingBetweenOverlappingLadders_KeepsClimbingUntilLastExit()
        {
            body.gravityScale = 3f;
            LadderZone firstLadder = CreateLadderZone("FirstLadder", out firstLadderObject);
            LadderZone secondLadder = CreateLadderZone("SecondLadder", out secondLadderObject);

            movement.EnterLadder(firstLadder);
            movement.EnterLadder(secondLadder);
            movement.ExitLadder(firstLadder);
            movement.SetVerticalMoveInput(1f);

            yield return new WaitForFixedUpdate();

            Assert.IsTrue(movement.IsClimbing, "다른 사다리와 접촉 중이면 등반 상태를 유지해야 한다.");
            Assert.AreEqual(0f, body.gravityScale);
            Assert.Greater(body.linearVelocityY, 0f);

            movement.ExitLadder(secondLadder);

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

        [Test]
        public void LadderAnimation_HoldsSingleFrameWhenStationary()
        {
            animationVisualObject = new GameObject("LadderIdleAnimationVisual");
            animationVisualObject.transform.SetParent(playerObject.transform);
            var renderer = animationVisualObject.AddComponent<SpriteRenderer>();
            var animation = animationVisualObject.AddComponent<PlayerAnimationController>();
            ladderAnimationFrames = new[]
            {
                CreateTestSprite(),
                CreateTestSprite(),
                CreateTestSprite()
            };
            animation.ConfigureFrames(
                renderer,
                movement,
                ladderAnimationFrames,
                ladderAnimationFrames,
                ladderAnimationFrames,
                ladderAnimationFrames,
                ladderAnimationFrames,
                ladderAnimationFrames,
                ladderAnimationFrames);

            movement.EnterLadder();
            movement.SetVerticalMoveInput(0f);
            InvokePrivate(animation, "LateUpdate");
            Assert.AreSame(ladderAnimationFrames[0], renderer.sprite);

            SetPrivateField(animation, "stateStartedAt", Time.unscaledTime - 2f);
            InvokePrivate(animation, "LateUpdate");
            Assert.AreSame(
                ladderAnimationFrames[0],
                renderer.sprite,
                "사다리에서 정지하면 상승/하강 프레임을 순환하지 않고 단일 프레임을 유지해야 한다.");
        }

        [Test]
        public void LadderAnimation_UsesSharedFramesAndReversesWithVerticalDistance()
        {
            animationVisualObject = new GameObject("LadderAnimationVisual");
            animationVisualObject.transform.SetParent(playerObject.transform);
            var renderer = animationVisualObject.AddComponent<SpriteRenderer>();
            var animation = animationVisualObject.AddComponent<PlayerAnimationController>();
            ladderAnimationFrames = new[]
            {
                CreateTestSprite(),
                CreateTestSprite(),
                CreateTestSprite()
            };
            animation.ConfigureFrames(
                renderer,
                movement,
                ladderAnimationFrames,
                ladderAnimationFrames,
                ladderAnimationFrames,
                ladderAnimationFrames,
                ladderAnimationFrames,
                ladderAnimationFrames,
                ladderAnimationFrames);
            SetPrivateField(animation, "ladderDistancePerFrame", 0.1f);

            movement.EnterLadder();
            movement.SetVerticalMoveInput(1f);
            InvokePrivate(animation, "LateUpdate");
            Assert.AreSame(ladderAnimationFrames[0], renderer.sprite);

            body.position = new Vector2(0f, 0.09f);
            InvokePrivate(animation, "LateUpdate");
            Assert.AreSame(
                ladderAnimationFrames[0],
                renderer.sprite,
                "설정 거리 미만에서는 다음 사다리 프레임으로 넘어가면 안 된다.");

            body.position = new Vector2(0f, 0.11f);
            InvokePrivate(animation, "LateUpdate");
            Assert.AreSame(
                ladderAnimationFrames[1],
                renderer.sprite,
                "실제 상승 거리가 한 단계 누적되면 왼쪽 동작 프레임을 사용해야 한다.");

            movement.ExitLadder();
            body.position = Vector2.zero;
            movement.EnterLadder();
            movement.SetVerticalMoveInput(-1f);
            InvokePrivate(animation, "LateUpdate");

            body.position = new Vector2(0f, -0.09f);
            InvokePrivate(animation, "LateUpdate");
            Assert.AreSame(
                ladderAnimationFrames[0],
                renderer.sprite,
                "하강도 설정 거리 미만에서는 중립 프레임을 유지해야 한다.");

            body.position = new Vector2(0f, -0.11f);
            InvokePrivate(animation, "LateUpdate");
            Assert.AreSame(
                ladderAnimationFrames[2],
                renderer.sprite,
                "실제 하강 거리가 한 단계 누적되면 같은 배열을 역방향으로 사용해야 한다.");

            movement.SetVerticalMoveInput(0f);
            InvokePrivate(animation, "LateUpdate");
            Assert.AreSame(
                ladderAnimationFrames[0],
                renderer.sprite,
                "사다리에서 멈추면 중립 프레임으로 복원되어야 한다.");
        }

        [Test]
        public void LadderAnimation_DoesNotSelectWalkFramesDuringContinuousClimb()
        {
            animationVisualObject = new GameObject("LadderWalkIsolationVisual");
            animationVisualObject.transform.SetParent(playerObject.transform);
            var renderer = animationVisualObject.AddComponent<SpriteRenderer>();
            var animation = animationVisualObject.AddComponent<PlayerAnimationController>();
            ladderAnimationFrames = new[]
            {
                CreateTestSprite(),
                CreateTestSprite(),
                CreateTestSprite()
            };
            otherAnimationFrames = new[]
            {
                CreateTestSprite(),
                CreateTestSprite()
            };
            animation.ConfigureFrames(
                renderer,
                movement,
                otherAnimationFrames,
                otherAnimationFrames,
                otherAnimationFrames,
                ladderAnimationFrames,
                otherAnimationFrames,
                otherAnimationFrames,
                otherAnimationFrames);

            movement.EnterLadder();
            movement.SetMoveInput(1f);
            movement.SetVerticalMoveInput(1f);
            InvokePrivate(animation, "LateUpdate");

            for (var sample = 1; sample <= 80; sample++)
            {
                body.position = new Vector2(sample * 0.01f, sample * 0.02f);
                InvokePrivate(animation, "LateUpdate");

                CollectionAssert.Contains(
                    ladderAnimationFrames,
                    renderer.sprite,
                    $"연속 등반 중 {sample}번째 표본에서 일반 이동 프레임이 표시되었다.");
                CollectionAssert.DoesNotContain(otherAnimationFrames, renderer.sprite);
            }
        }

        private static LadderZone CreateLadderZone(string name, out GameObject ladderObject)
        {
            ladderObject = new GameObject(name);
            var collider = ladderObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            return ladderObject.AddComponent<LadderZone>();
        }

        private static Sprite CreateTestSprite()
        {
            return Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName + " 메서드를 찾을 수 없습니다.");
            method.Invoke(target, null);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName + " 필드를 찾을 수 없습니다.");
            field.SetValue(target, value);
        }
    }
}
