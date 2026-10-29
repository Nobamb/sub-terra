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
        [Test]
        public void JumpGrace_LeavingGroundAllowsOneLateJumpOnly()
        {
            var window = new JumpGraceWindow();
            window.ObserveGround(0f, 0.1f);
            window.Request(0.08f, 0.1f);
            Assert.That(window.TryConsume(0.09f, false, false, false, false), Is.True);
            window.Request(0.095f, 0.1f);
            Assert.That(window.TryConsume(0.095f, false, false, true, false), Is.False);
            window.Clear();
            window.ObserveGround(0f, 0.1f);
            window.Request(0.11f, 0.1f);
            Assert.That(window.TryConsume(0.11f, false, false, false, false), Is.False);
        }

        [Test]
        public void JumpGrace_BufferWaitsForLandingAndExpires()
        {
            var window = new JumpGraceWindow();
            window.Request(1f, 0.1f);
            Assert.That(window.TryConsume(1.02f, false, false, false, false), Is.False);
            Assert.That(window.TryConsume(1.08f, true, false, false, false), Is.True);
            Assert.That(window.TryConsume(1.08f, true, false, false, false), Is.False);
            window.Request(2f, 0.1f);
            Assert.That(window.TryConsume(2.11f, true, false, false, false), Is.False);
        }

        [Test]
        public void JumpGrace_ClearAndAirLockPreventDeferredEscapeJump()
        {
            var window = new JumpGraceWindow();
            window.Request(0f, 0.1f);
            Assert.That(window.TryConsume(0.02f, false, true, false, true), Is.False);
            window.Clear();
            Assert.That(window.TryConsume(0.03f, true, false, false, false), Is.False);
            window.Request(0.04f, 0.1f);
            Assert.That(window.TryConsume(0.05f, false, true, false, false), Is.True);
        }

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
        public IEnumerator AirborneSideContact_FallsWhileHoldingTowardsPlatform()
        {
            return AssertFallsAtSide(1f, true);
        }

        [UnityTest]
        public IEnumerator AirborneSideContact_FallsAtOppositePlatformEdge()
        {
            return AssertFallsAtSide(-1f, true);
        }

        [UnityTest]
        public IEnumerator AirborneSideContact_FallsWhileHoldingTowardsSolidWall()
        {
            return AssertFallsAtSide(1f, false);
        }

        private IEnumerator AssertFallsAtSide(float direction, bool oneWay)
        {
            var capsule = playerObject.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.6f, 0.7f);
            SetPrivateField(movement, "bodyCollider", capsule);
            body.gravityScale = 3f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            wallObject = new GameObject("SupportCap");
            var cap = wallObject.AddComponent<BoxCollider2D>();
            cap.size = new Vector2(1f, 0.4f);
            cap.offset = new Vector2(0f, 1.1f);
            cap.usedByEffector = oneWay;
            var effector = wallObject.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useSideFriction = false;
            effector.useSideBounce = false;
            effector.surfaceArc = 180f;
            effector.enabled = oneWay;
            body.position = new Vector2(-0.78f * direction, 1.42f);
            movement.SetMoveInput(direction);
            Physics2D.SyncTransforms();

            for (var i = 0; i < 40; i++) yield return new WaitForFixedUpdate();

            Assert.Less(body.position.y, 0.8f, "Holding towards the cap must not support the airborne player.");
            Assert.IsFalse(movement.IsGrounded);
        }

        [UnityTest]
        public IEnumerator WalkAnimation_OneTickGroundGapKeepsWalk_RequestedJumpSwitchesImmediately()
        {
            SetupGroundedPlayerWithContacts();
            animationVisualObject = new GameObject("WalkGapVisual");
            animationVisualObject.transform.SetParent(playerObject.transform, false);
            var renderer = animationVisualObject.AddComponent<SpriteRenderer>();
            var animation = animationVisualObject.AddComponent<PlayerAnimationController>();
            otherAnimationFrames = new[] { CreateTestSprite(), CreateTestSprite(), CreateTestSprite() };
            animation.ConfigureFrames(renderer, movement, new[] { otherAnimationFrames[0] },
                new[] { otherAnimationFrames[1] }, new[] { otherAnimationFrames[2] }, null, null, null, null);
            for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            movement.SetMoveInput(1f);
            yield return null;
            Assert.That(renderer.sprite, Is.SameAs(otherAnimationFrames[1]));

            var ground = groundObject.GetComponent<Collider2D>();
            ground.enabled = false;
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(movement.IsGrounded, Is.False, "실제 물리 접점과 발밑 보조 판정을 한 틱 제거한다");
            Assert.That(renderer.sprite, Is.SameAs(otherAnimationFrames[1]), "접지가 잠깐 빠져도 Walk를 유지한다");
            ground.enabled = true;
            for (int i = 0; i < 2; i++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(movement.IsGrounded, Is.True);
            Assert.That(renderer.sprite, Is.SameAs(otherAnimationFrames[1]));

            movement.RequestJump();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(movement.IsJumpInProgress, Is.True);
            Assert.That(renderer.sprite, Is.SameAs(otherAnimationFrames[2]), "입력으로 점프하면 즉시 Jump로 바뀐다");
        }

        [UnityTest]
        public IEnumerator PromptB175_RealJump_PlaysTakeOffRiseApexFallThenLanding()
        {
            SetupGroundedPlayerWithContacts(airLockDuration: 0.05f);
            animationVisualObject = new GameObject("JumpPhaseVisual");
            animationVisualObject.transform.SetParent(playerObject.transform, false);
            var renderer = animationVisualObject.AddComponent<SpriteRenderer>();
            var animation = animationVisualObject.AddComponent<PlayerAnimationController>();
            var idle = CreateTestSprite();
            animation.ConfigureFrames(renderer, movement, new[] { idle }, new[] { CreateTestSprite() },
                new[] { CreateTestSprite() }, null, null, null, null);
            var ready = new Sprite[8];
            var rise = new Sprite[24];
            var fall = new Sprite[24];
            for (int i = 0; i < 24; i++)
            {
                if (i < 8) ready[i] = CreateTestSprite();
                rise[i] = CreateTestSprite();
                fall[i] = CreateTestSprite();
            }
            SetPrivateField(animation, "jumpReadyFrames", ready);
            SetPrivateField(animation, "jumpRiseFrames", rise);
            SetPrivateField(animation, "jumpFallFrames", fall);
            // 생성 직후 첫 틱은 접점이 없어 짧은 착지 그림이 나올 수 있으므로 대기 그림까지 기다린다.
            for (float waited = 0f; waited < 1f && renderer.sprite != idle; waited += Time.deltaTime)
                yield return null;
            Assert.That(renderer.sprite, Is.SameAs(idle), "점프 전 대기");

            movement.RequestJump();
            yield return new WaitForFixedUpdate();
            Assert.That(movement.LastJumpLaunchSpeed, Is.EqualTo(body.linearVelocityY).Within(0.5f),
                "그림 진행도 기준 속도는 실제 시작 속도와 같다");
            yield return null;
            Assert.That(renderer.sprite, Is.SameAs(rise[0]), "키를 누르면 준비 동작 없이 바로 도약 1");

            int lastRise = 0;
            bool sawApex = false;
            bool sawLanding = false;
            for (float waited = 0f; waited < 4f && renderer.sprite != idle; waited += Time.deltaTime)
            {
                int riseIndex = System.Array.IndexOf(rise, renderer.sprite);
                if (riseIndex >= 0)
                {
                    Assert.That(sawApex, Is.False, "정점 이후 상승 그림으로 되돌아가지 않는다");
                    Assert.That(riseIndex, Is.GreaterThanOrEqualTo(lastRise), "상승 그림은 되돌아가지 않는다");
                    lastRise = riseIndex;
                }
                if (renderer.sprite == fall[0])
                {
                    sawApex = true;
                    Assert.That(lastRise, Is.EqualTo(23), "정점 전에 상승 24에 닿는다");
                }
                if (renderer.sprite == ready[7])
                {
                    sawLanding = true;
                    Assert.That(movement.IsGrounded, Is.True);
                }
                yield return null;
            }
            Assert.That(sawApex && sawLanding, Is.True, "fall 1과 착지 ready 8을 거친다");
            Assert.That(renderer.sprite, Is.SameAs(idle), "착지 그림 뒤 대기로 돌아온다");
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
        public IEnumerator LadderTop_ClearsHeadPastEndButAllowsDescentAndJump()
        {
            var collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.6f, 0.7f);
            SetPrivateField(movement, "bodyCollider", collider);
            var ladder = CreateLadderZone("TopLimitLadder", out firstLadderObject);
            firstLadderObject.GetComponent<BoxCollider2D>().size = new Vector2(0.7f, 5f);
            body.position = new Vector2(0f, 2.1f);
            Physics2D.SyncTransforms();
            movement.EnterLadder(ladder);
            movement.SetVerticalMoveInput(1f);
            for (var i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(collider.bounds.max.y, 2.5f, "The head must clear the edge while climbing from below.");
            Assert.LessOrEqual(collider.bounds.min.y, 2.541f);
            Assert.Greater(body.linearVelocityY, 0f);
            Assert.IsTrue(movement.IsClimbing);
            movement.SetVerticalMoveInput(-1f);
            yield return new WaitForFixedUpdate();
            Assert.Less(body.linearVelocityY, 0f);
            movement.RequestJump();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(movement.IsClimbing);
            Assert.Greater(body.linearVelocityY, 0f);
        }

        [UnityTest]
        public IEnumerator LadderTop_FallingEntryDescendsToSupportBeforeShowingClimb()
        {
            var collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.6f, 0.7f);
            SetPrivateField(movement, "bodyCollider", collider);
            var ladder = CreateLadderZone("FallingEntryLadder", out firstLadderObject);
            firstLadderObject.GetComponent<BoxCollider2D>().size = new Vector2(0.7f, 5f);
            animationVisualObject = new GameObject("EntryVisual");
            var renderer = animationVisualObject.AddComponent<SpriteRenderer>();
            var animation = animationVisualObject.AddComponent<PlayerAnimationController>();
            ladderAnimationFrames = new[] { CreateTestSprite(), CreateTestSprite(), CreateTestSprite() };
            otherAnimationFrames = new[] { CreateTestSprite() };
            animation.ConfigureFrames(renderer, movement, otherAnimationFrames,
                otherAnimationFrames, otherAnimationFrames, ladderAnimationFrames, null, null, null);
            body.position = new Vector2(0f, 2.7f);
            body.linearVelocity = Vector2.down * 5f;
            Physics2D.SyncTransforms();
            movement.EnterLadder(ladder);
            InvokePrivate(animation, "LateUpdate");
            Assert.AreSame(otherAnimationFrames[0], renderer.sprite, "Unsupported entry must not show a grasp pose.");
            yield return new WaitForFixedUpdate();
            Assert.Less(body.linearVelocityY, 0f, "Falling entry must descend even without input.");
            for (var i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
            Assert.That(collider.bounds.max.y, Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(body.linearVelocityY, Is.EqualTo(0f).Within(0.001f));
            InvokePrivate(animation, "LateUpdate");
            Assert.AreSame(ladderAnimationFrames[0], renderer.sprite);
            movement.SetVerticalMoveInput(-1f);
            yield return new WaitForFixedUpdate();
            Assert.Less(body.linearVelocityY, 0f, "Manual descent must resume once supported.");
        }

        [UnityTest]
        public IEnumerator LadderTop_FeetClearEdgeAndResumeHorizontalMovement()
        {
            var collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.6f, 0.7f);
            SetPrivateField(movement, "bodyCollider", collider);
            var ladder = CreateLadderZone("WalkOffLadder", out firstLadderObject);
            firstLadderObject.GetComponent<BoxCollider2D>().size = new Vector2(0.7f, 5f);
            body.position = new Vector2(0f, 2.1f);
            Physics2D.SyncTransforms();
            movement.EnterLadder(ladder);
            movement.SetVerticalMoveInput(1f);
            for (var i = 0; i < 16; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(collider.bounds.min.y, 2.5f);
            Assert.LessOrEqual(collider.bounds.min.y, 2.55f, "Exiting must not launch the player upward.");
            Assert.IsFalse(movement.IsClimbing);
            Assert.That(body.gravityScale, Is.EqualTo(0f), "Original gravity must be restored.");
            movement.SetVerticalMoveInput(1f);
            movement.SetMoveInput(1f);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(movement.IsClimbing, "Holding up must not reattach at the exit.");
            Assert.Greater(body.linearVelocityX, 0f);
        }

        [UnityTest]
        public IEnumerator LadderTop_AutomaticEntryRespectsMovementGateAndJump()
        {
            var collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.6f, 0.7f);
            SetPrivateField(movement, "bodyCollider", collider);
            var ladder = CreateLadderZone("GatedEntryLadder", out firstLadderObject);
            firstLadderObject.GetComponent<BoxCollider2D>().size = new Vector2(0.7f, 5f);
            body.position = new Vector2(0f, 2.7f);
            Physics2D.SyncTransforms();
            movement.EnterLadder(ladder);
            movement.SetVerticalMoveInput(1f);
            movement.SetCanMove(false);
            yield return new WaitForFixedUpdate();
            Assert.That(body.linearVelocityY, Is.EqualTo(0f).Within(0.001f));
            Assert.IsTrue(movement.IsApproachingLadderFromAbove);
            movement.SetCanMove(true);
            yield return new WaitForFixedUpdate();
            Assert.Less(body.linearVelocityY, 0f, "Up input must not interrupt automatic entry.");
            movement.RequestJump();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(movement.IsClimbing);
            Assert.IsFalse(movement.IsApproachingLadderFromAbove);
            Assert.Greater(body.linearVelocityY, 0f);
        }

        [UnityTest]
        public IEnumerator LadderTop_ConnectedZonesAllowCrossingTheirSeam()
        {
            var collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.6f, 0.7f);
            SetPrivateField(movement, "bodyCollider", collider);
            var lower = CreateLadderZone("LowerLadder", out firstLadderObject);
            var upper = CreateLadderZone("UpperLadder", out secondLadderObject);
            firstLadderObject.GetComponent<BoxCollider2D>().size = new Vector2(0.7f, 5f);
            secondLadderObject.GetComponent<BoxCollider2D>().size = new Vector2(0.7f, 5f);
            secondLadderObject.transform.position = Vector3.up * 5f;
            body.position = new Vector2(0f, 2.1f);
            Physics2D.SyncTransforms();
            movement.EnterLadder(lower);
            movement.SetVerticalMoveInput(1f);
            for (var i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(body.position.y, 2.5f, "Adjacent trigger must continue the climb at the seam.");
            Assert.IsTrue(movement.IsClimbing);
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
            // 점프 후에도 사다리 범위 안에 남아 있어야 재탑승을 검사할 수 있다.
            firstLadderObject.GetComponent<BoxCollider2D>().size = new Vector2(0.7f, 5f);
            movement.EnterLadder(ladder);

            movement.RequestJump();
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(movement.IsClimbing);
            Assert.Greater(body.linearVelocityY, 0f);

            yield return new WaitForSeconds(0.2f);
            Assert.That(ladder.TryGetTop(out var top), Is.True);
            Assert.That(body.position.y, Is.LessThan(top));
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

        [TestCase(1)]
        [TestCase(-1)]
        public void LadderAnimation_FiveFrames_UsesApprovedSequenceAndStops(int direction)
        {
            animationVisualObject = new GameObject("FiveFrameLadderVisual");
            animationVisualObject.transform.SetParent(playerObject.transform);
            var renderer = animationVisualObject.AddComponent<SpriteRenderer>();
            var animation = animationVisualObject.AddComponent<PlayerAnimationController>();
            ladderAnimationFrames = new[] { CreateTestSprite(), CreateTestSprite(), CreateTestSprite(), CreateTestSprite(), CreateTestSprite() };
            otherAnimationFrames = new[] { CreateTestSprite() };
            animation.ConfigureFrames(renderer, movement, otherAnimationFrames,
                otherAnimationFrames, null, ladderAnimationFrames, null, null, null);
            SetPrivateField(animation, "ladderDistancePerFrame", 0.1f);
            movement.EnterLadder();
            movement.SetVerticalMoveInput(direction);
            InvokePrivate(animation, "LateUpdate");
            var sequence = direction > 0 ? new[] { 1, 2, 1, 0, 3, 4, 3, 0 } : new[] { 3, 4, 3, 0, 1, 2, 1, 0 };
            for (var i = 0; i < sequence.Length; i++)
            {
                body.position = new Vector2(0f, direction * ((i + 1) * 0.1f + 0.001f));
                InvokePrivate(animation, "LateUpdate");
                Assert.AreSame(ladderAnimationFrames[sequence[i]], renderer.sprite, "Approved sequence step " + i);
            }
            movement.SetVerticalMoveInput(0f);
            InvokePrivate(animation, "LateUpdate");
            Assert.AreSame(ladderAnimationFrames[0], renderer.sprite);
            InvokePrivate(animation, "LateUpdate");
            Assert.AreSame(ladderAnimationFrames[0], renderer.sprite, "Stopped climb must not keep cycling.");
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

        [Test]
        public void LadderAnimation_TransientContactGap_HoldsLadderFrameAndResumes()
        {
            animationVisualObject = new GameObject("LadderContactGapVisual");
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

            LadderZone firstLadder = CreateLadderZone("ContactGapFirst", out firstLadderObject);
            LadderZone secondLadder = CreateLadderZone("ContactGapSecond", out secondLadderObject);
            // 접촉 공백 테스트의 이동 위치가 실제 사다리 범위 안에 있도록 구성한다.
            firstLadderObject.GetComponent<BoxCollider2D>().size = new Vector2(0.7f, 5f);
            secondLadderObject.GetComponent<BoxCollider2D>().size = new Vector2(0.7f, 5f);
            movement.EnterLadder(firstLadder);
            movement.SetVerticalMoveInput(1f);
            InvokePrivate(animation, "LateUpdate");
            body.position = new Vector2(0f, 0.51f);
            InvokePrivate(animation, "LateUpdate");
            var frameBeforeGap = renderer.sprite;

            movement.ExitLadder(firstLadder);
            SetPrivateField(movement, "<IsGrounded>k__BackingField", true);
            movement.SetMoveInput(1f);
            InvokePrivate(animation, "LateUpdate");

            Assert.AreSame(
                frameBeforeGap,
                renderer.sprite,
                "사다리 접촉이 순간적으로 끊겨도 Walk 프레임으로 바뀌면 안 된다.");
            CollectionAssert.DoesNotContain(otherAnimationFrames, renderer.sprite);

            movement.EnterLadder(secondLadder);
            movement.SetVerticalMoveInput(1f);
            InvokePrivate(animation, "LateUpdate");
            CollectionAssert.Contains(
                ladderAnimationFrames,
                renderer.sprite,
                "짧은 접촉 공백 안에 재진입하면 사다리 프레임을 이어가야 한다.");
        }

        [Test]
        public void LadderAnimation_GraceExpires_RestoresNormalAnimation()
        {
            animationVisualObject = new GameObject("LadderGraceExpiryVisual");
            animationVisualObject.transform.SetParent(playerObject.transform);
            var renderer = animationVisualObject.AddComponent<SpriteRenderer>();
            var animation = animationVisualObject.AddComponent<PlayerAnimationController>();
            ladderAnimationFrames = new[]
            {
                CreateTestSprite(),
                CreateTestSprite(),
                CreateTestSprite()
            };
            otherAnimationFrames = new[] { CreateTestSprite() };
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

            LadderZone ladder = CreateLadderZone("GraceExpiryLadder", out firstLadderObject);
            movement.EnterLadder(ladder);
            movement.SetVerticalMoveInput(1f);
            InvokePrivate(animation, "LateUpdate");
            movement.ExitLadder(ladder);
            SetPrivateField(movement, "<IsGrounded>k__BackingField", true);
            movement.SetMoveInput(1f);
            SetPrivateField(animation, "ladderVisualGraceUntil", Time.unscaledTime - 1f);

            InvokePrivate(animation, "LateUpdate");

            Assert.AreSame(
                otherAnimationFrames[0],
                renderer.sprite,
                "유예 시간이 끝나면 일반 Walk 표시가 복원되어야 한다.");
        }

        [Test]
        public void LadderAnimation_ActiveTriggerContact_PrioritizesLadderFrames()
        {
            animationVisualObject = new GameObject("LadderContactPriorityVisual");
            animationVisualObject.transform.SetParent(playerObject.transform);
            var renderer = animationVisualObject.AddComponent<SpriteRenderer>();
            var animation = animationVisualObject.AddComponent<PlayerAnimationController>();
            ladderAnimationFrames = new[]
            {
                CreateTestSprite(),
                CreateTestSprite(),
                CreateTestSprite()
            };
            otherAnimationFrames = new[] { CreateTestSprite() };
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

            LadderZone ladder = CreateLadderZone("ContactPriorityLadder", out firstLadderObject);
            movement.EnterLadder(ladder);
            movement.SetVerticalMoveInput(1f);
            InvokePrivate(animation, "LateUpdate");

            InvokePrivate(movement, "ExitLadderMode");
            SetPrivateField(movement, "<IsGrounded>k__BackingField", true);
            movement.SetMoveInput(1f);
            InvokePrivate(animation, "LateUpdate");

            Assert.IsFalse(movement.IsClimbing);
            Assert.IsTrue(movement.IsTouchingLadder);
            CollectionAssert.Contains(
                ladderAnimationFrames,
                renderer.sprite,
                "사다리 Trigger 접촉이 유지되는 동안 Walk 프레임으로 바뀌면 안 된다.");
        }

        [Test]
        public void LadderAnimation_LadderJump_BypassesVisualGrace()
        {
            animationVisualObject = new GameObject("LadderJumpVisual");
            animationVisualObject.transform.SetParent(playerObject.transform);
            var renderer = animationVisualObject.AddComponent<SpriteRenderer>();
            var animation = animationVisualObject.AddComponent<PlayerAnimationController>();
            ladderAnimationFrames = new[]
            {
                CreateTestSprite(),
                CreateTestSprite(),
                CreateTestSprite()
            };
            otherAnimationFrames = new[] { CreateTestSprite() };
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

            LadderZone ladder = CreateLadderZone("JumpLadder", out firstLadderObject);
            movement.EnterLadder(ladder);
            movement.SetVerticalMoveInput(1f);
            InvokePrivate(animation, "LateUpdate");

            movement.RequestJump();
            InvokePrivate(movement, "FixedUpdate");
            movement.ExitLadder(ladder);
            InvokePrivate(animation, "LateUpdate");

            Assert.IsFalse(movement.IsClimbing);
            Assert.IsTrue(movement.IsJumpInProgress);
            Assert.AreSame(
                otherAnimationFrames[0],
                renderer.sprite,
                "의도적으로 사다리에서 점프하면 유예 없이 Jump 표시로 전환되어야 한다.");
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
