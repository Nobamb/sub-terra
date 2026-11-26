using System.Collections.Generic;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>짧은 입력 예약과 발판 이탈 유예. Unity 시간/물리와 분리해 경계 조건을 검사한다.</summary>
    public sealed class JumpGraceWindow
    {
        private float requestUntil = float.NegativeInfinity;
        private float groundUntil = float.NegativeInfinity;
        public void Request(float now, float duration) => requestUntil = now + System.Math.Max(0f, duration);
        public void ObserveGround(float now, float duration) => groundUntil = now + System.Math.Max(0f, duration);
        public void Clear() { requestUntil = groundUntil = float.NegativeInfinity; }
        public void ClearGround() => groundUntil = float.NegativeInfinity;
        public bool TryConsume(float now, bool grounded, bool climbing, bool used, bool airLocked)
        {
            if (now > requestUntil || airLocked || (used && !climbing)
                || (!grounded && !climbing && now > groundUntil)) return false;
            Clear();
            return true;
        }
    }

    /// <summary>
    /// B-211 가변 점프: 오르는 중 손을 떼면 위 속도를 한 번 줄이되, 꼭대기가 최소 높이 아래로 내려가면 아직 자르지 않는다.
    /// 물리 틱(반암시적 오일러)으로 실제 오르는 높이를 계산해 1칸 턱을 항상 넘게 한다.
    /// </summary>
    public static class VariableJumpRule
    {
        /// <summary>지금 위 속도로 물리 틱마다 더 오를 높이. 틱 간격이 0이면 v²/2g.</summary>
        public static float RemainingRise(float upwardSpeed, float gravity, float fixedStep)
        {
            if (upwardSpeed <= 0f || gravity <= 0f) return 0f;
            if (fixedStep <= 0f) return upwardSpeed * upwardSpeed / (2f * gravity);
            float decrement = gravity * fixedStep;
            int steps = (int)System.Math.Floor(upwardSpeed / decrement);
            return fixedStep * (steps * upwardSpeed - decrement * steps * (steps + 1) * 0.5f);
        }

        /// <param name="risenSoFar">뜬 자리에서 지금까지 오른 높이</param>
        public static bool CanCut(float risenSoFar, float upwardSpeed, float multiplier,
            float minApexHeight, float gravity, float fixedStep)
        {
            if (upwardSpeed <= 0f) return false;
            return risenSoFar + RemainingRise(upwardSpeed * multiplier, gravity, fixedStep)
                >= minApexHeight - 0.0001f;
        }
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(0f)] private float acceleration = 45f;
        [SerializeField, Min(0f)] private float deceleration = 55f;
        [SerializeField, Min(0f)] private float jumpImpulse = 11f;
        [SerializeField, Min(0f)] private float ladderSpeed = 4f;
        private float ladderSpeedMultiplier = 1f;
        [SerializeField, Range(0f, 0.2f)] private float coyoteTimeSeconds = 0.1f;
        [SerializeField, Range(0f, 0.2f)] private float jumpBufferSeconds = 0.1f;

        [Header("Variable Jump (B-211)")]
        [SerializeField] private bool variableJumpEnabled = true;
        [Tooltip("오르는 중 점프 키를 떼면 위 속도에 한 번 곱한다.")]
        [SerializeField, Range(0f, 1f)] private float jumpReleaseVelocityMultiplier = 0.5f;
        [Tooltip("짧게 눌러도 꼭대기가 이 높이(칸) 아래로 내려가지 않는다. 1칸 턱 보장.")]
        [SerializeField, Min(0f)] private float minJumpApexHeight = 1.1f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.08f;
        [SerializeField] private LayerMask groundLayers = ~0;
        /// <summary>
        /// 점프 직후 이 시간 동안은 착지·재점프로 보지 않는다.
        /// </summary>
        [SerializeField, Min(0.01f)] private float jumpAirLockDuration = 0.15f;
        /// <summary>발 아래 보조 레이 길이 (접점 누락 대비).</summary>
        [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.12f;
        /// <summary>바닥으로 인정할 최소 노멀 Y (1=완전 위).</summary>
        [SerializeField, Range(0.1f, 1f)] private float minGroundNormalY = 0.6f;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private readonly ContactPoint2D[] contactBuffer = new ContactPoint2D[16];
        private readonly RaycastHit2D[] probeHits = new RaycastHit2D[8];
        private readonly List<RaycastHit2D> platformCeilingHits = new List<RaycastHit2D>(4);
        private float moveInput;
        private float verticalMoveInput;
        private readonly JumpGraceWindow jumpWindow = new JumpGraceWindow();
        private float jumpAirLockRemaining;
        /// <summary>
        /// true면 이번 공중 구간에서 이미 점프를 썼다.
        /// 착지(물리 접촉 + 비상승) 전까지 절대 회복하지 않는다.
        /// </summary>
        private bool jumpUsedUntilLand;
        private bool jumpHeld;
        // 땅 점프 뒤 아직 자르지 않았고 오르는 중이면 true. 사다리 점프는 켜지 않는다.
        private bool jumpCutArmed;
        private float jumpLaunchY;
        private float gravityBeforeClimbing;
        private bool ascendingTowardsLadderExit;
        private readonly HashSet<LadderZone> activeLadders = new HashSet<LadderZone>();
        private float cargoSpeedMultiplier = 1f;
        private float cargoJumpMultiplier = 1f;
        private float hazardSpeedMultiplier = 1f;
        private readonly PlayerMoveSfxGate moveSfx = new PlayerMoveSfxGate();
        private readonly LadderSfxGate ladderSfx = new LadderSfxGate();
        private PlayerSurvivalController hitStunSource;

        public Vector2 Position => body != null ? body.position : (Vector2)transform.position;
        public float FacingDirection { get; private set; } = 1f;
        public bool IsGrounded { get; private set; }
        internal bool IsOnStableLandingPlatform { get; private set; }
        private IAdjustableLandingPlatform standingPlatform;
        /// <summary>완충판 위에 서 있는 동안 true. 위/아래 키는 사다리·수직 이동이 아니라 판 높이 조절에 쓴다.</summary>
        public bool IsOnAdjustablePlatform => IsGrounded && IsPlatformAlive(standingPlatform);
        /// <summary>서 있는 완충판의 높이를 실제로 바꿨을 때 변화량과 함께 알린다.</summary>
        public event System.Action<float> PlatformHeightAdjusted;
        public bool CanMove { get; private set; } = true;
        /// <summary>
        /// B-210: 피격 경직 중. 이동·점프·사다리 오르내리기·뛰어내리기 입력을 무시한다(입력 값은 보관해 끝나면 바로 이어진다).
        /// </summary>
        public bool IsHitStunned => hitStunSource != null && hitStunSource.IsHitStunned;
        private bool AcceptsControl => CanMove && !IsHitStunned;
        private float MoveInput => IsHitStunned ? 0f : moveInput;
        private float VerticalInput => IsHitStunned ? 0f : verticalMoveInput;
        public bool IsClimbing { get; private set; }
        public bool IsTouchingLadder => activeLadders.Count > 0;
        public bool IsApproachingLadderFromAbove => IsClimbing && body != null
            && !ascendingTowardsLadderExit
            && TryGetLadderTop(out var top) && PlayerTop > top + 0.001f;
        public bool IsJumpInProgress => jumpUsedUntilLand && !IsGrounded;
        public bool IsDescendingLadder => IsClimbing && VerticalInput < -0.01f;
        public bool IsMovingOnLadder => IsClimbing && Mathf.Abs(VerticalInput) > 0.01f;
        public bool IsMovementRequested => Mathf.Abs(MoveInput) > 0.01f
            || Mathf.Abs(VerticalInput) > 0.01f;
        public float CurrentSpeedMultiplier => cargoSpeedMultiplier * hazardSpeedMultiplier;
        public float CurrentJumpMultiplier => cargoJumpMultiplier;
        /// <summary>실제 가로 속도 크기. 벽을 밀고 있으면 0에 가깝다.</summary>
        public float HorizontalSpeed => body != null ? Mathf.Abs(body.linearVelocityX) : 0f;
        /// <summary>실제 세로 속도(위 +).</summary>
        public float VerticalSpeed => body != null ? body.linearVelocityY : 0f;
        /// <summary>B-175: 점프가 실행될 때마다 1 증가. 착지 순간 재점프도 구분한다.</summary>
        public int JumpCount { get; private set; }
        /// <summary>B-175: 마지막 점프의 시작 위 속도(화물 배율 반영).</summary>
        public float LastJumpLaunchSpeed { get; private set; }
        /// <summary>B-211: 손을 떼 위 속도를 자를 때마다 1 증가. 점프 그림이 남은 상승을 다시 나눈다.</summary>
        public int JumpCutCount { get; private set; }
        /// <summary>B-211: 점프 키를 누르고 있는가(started~canceled).</summary>
        public bool IsJumpHeld => jumpHeld;
        /// <summary>
        /// B-211: 착지 판정(TrackFall·디버그 falling) 한 번마다 한 번 알린다. 찌그러짐·먼지가 구독한다.
        /// </summary>
        public event System.Action<PlayerLandingInfo> Landed;
        /// <summary>B-164: 점프·착지 소리 판정. 착지는 PlayerSurvivalController가 같은 판정기로 낸다.</summary>
        public PlayerMoveSfxGate MoveSfx => moveSfx;
        /// <summary>B-164: 사다리 잡기·놓기·걸음 소리 판정.</summary>
        public LadderSfxGate LadderSfx => ladderSfx;
        private float PlayerTop => bodyCollider != null ? bodyCollider.bounds.max.y : body.position.y;
        private float PlayerBottom => bodyCollider != null ? bodyCollider.bounds.min.y : body.position.y;

        /// <summary>캐릭터 판정 영역의 월드 Bounds. 콜라이더가 없으면 false.</summary>
        public bool TryGetBodyBounds(out Bounds bounds)
        {
            if (bodyCollider == null)
            {
                bodyCollider = GetComponent<Collider2D>();
            }

            if (bodyCollider == null)
            {
                bounds = default;
                return false;
            }

            bounds = bodyCollider.bounds;
            return true;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            if (hitStunSource == null) hitStunSource = GetComponentInParent<PlayerSurvivalController>();
        }

        /// <summary>B-210: 경직 판단을 읽을 생존 컨트롤러. 통합 바인더가 BindMovement에서 연결한다.</summary>
        public void BindHitStunSource(PlayerSurvivalController source)
        {
            hitStunSource = source;
        }

        private void FixedUpdate()
        {
            if (jumpAirLockRemaining > 0f)
            {
                jumpAirLockRemaining = Mathf.Max(0f, jumpAirLockRemaining - Time.fixedDeltaTime);
            }

            // 점프 판정과 같은 물리 틱에서 착지를 갱신한다.
            UpdateGroundedState();
            if (CanMove && IsGrounded && !jumpUsedUntilLand)
                jumpWindow.ObserveGround(Time.time, coyoteTimeSeconds);
            ApplyHorizontalMovement();

            var jumped = TryApplyJump();
            // 점프한 틱은 Impulse가 아직 속도에 반영되기 전이라 다음 틱부터 본다.
            if (!jumped) TryCutJump();
            if (IsClimbing && !jumped)
            {
                if (CanMove && IsMovingOnLadder)
                {
                    SfxRequests.Play(ladderSfx.OnVerticalMove());
                }

                ApplyVerticalMovement();
            }
            else if (!jumped)
            {
                TryResumeLadderAfterJump();
            }
        }

        private void OnDisable()
        {
            jumpWindow.Clear();
            jumpHeld = false;
            jumpCutArmed = false;
            ExitLadder();
        }

        public void SetMoveInput(float horizontal)
        {
            moveInput = Mathf.Clamp(horizontal, -1f, 1f);

            if (Mathf.Abs(moveInput) > 0.01f && !IsHitStunned)
            {
                FacingDirection = Mathf.Sign(moveInput);
            }
        }

        public void RequestJump()
        {
            jumpHeld = true;
            if (AcceptsControl) jumpWindow.Request(Time.time, jumpBufferSeconds);
        }

        /// <summary>B-211: 점프 키를 뗐다. 오르는 중이면 다음 물리 틱부터 자르기 규칙을 본다.</summary>
        public void ReleaseJump()
        {
            jumpHeld = false;
        }

        /// <summary>B-211: 착지 판정을 구독자에게 알린다. PlayerSurvivalController가 착지마다 한 번 부른다.</summary>
        public void ReportLanding(PlayerLandingInfo landing)
        {
            Landed?.Invoke(landing);
        }

        public void SetVerticalMoveInput(float vertical)
        {
            verticalMoveInput = Mathf.Clamp(vertical, -1f, 1f);
        }

        public void EnterLadder()
        {
            EnterLadderMode();
        }

        public void EnterLadder(LadderZone ladder)
        {
            if (ladder == null || !activeLadders.Add(ladder))
            {
                return;
            }

            EnterLadderMode();
        }

        private void EnterLadderMode()
        {
            if (IsClimbing || body == null)
            {
                return;
            }

            gravityBeforeClimbing = body.gravityScale;
            ascendingTowardsLadderExit = false;
            body.gravityScale = 0f;
            body.linearVelocity = new Vector2(body.linearVelocityX, 0f);
            IsClimbing = true;
            jumpWindow.ClearGround();
            // 사다리 탑승 시 탈출 점프 1회 허용.
            jumpAirLockRemaining = 0f;
            jumpUsedUntilLand = false;
            SfxRequests.Play(ladderSfx.OnEnter(IsGrounded));
        }

        /// <summary>위치 복원·OnDisable처럼 강제로 내리는 경로라 놓음 소리를 내지 않는다.</summary>
        public void ExitLadder()
        {
            activeLadders.Clear();
            ExitLadderMode(silent: true);
        }

        public void ExitLadder(LadderZone ladder)
        {
            if (ladder == null || !activeLadders.Remove(ladder) || activeLadders.Count > 0)
            {
                return;
            }

            ExitLadderMode();
        }

        private void ExitLadderMode(bool silent = false)
        {
            if (!IsClimbing || body == null)
            {
                return;
            }

            SfxRequests.Play(ladderSfx.OnExit(silent));

            body.gravityScale = gravityBeforeClimbing;
            if (ascendingTowardsLadderExit && body.linearVelocityY > 0f)
                body.linearVelocity = new Vector2(body.linearVelocityX, 0f);
            verticalMoveInput = 0f;
            IsClimbing = false;
            ascendingTowardsLadderExit = false;
        }

        public void SetCanMove(bool canMove)
        {
            CanMove = canMove;
            if (!canMove)
            {
                jumpWindow.Clear();
                moveInput = 0f;
            }
        }

        public void SetCargoSpeedMultiplier(float multiplier)
        {
            cargoSpeedMultiplier = Mathf.Max(0f, multiplier);
        }

        public void SetCargoJumpMultiplier(float multiplier)
        {
            cargoJumpMultiplier = Mathf.Clamp01(multiplier);
        }

        public void SetHazardSpeedMultiplier(float multiplier)
        {
            hazardSpeedMultiplier = Mathf.Max(0f, multiplier);
        }

        private void ApplyHorizontalMovement()
        {
            float targetVelocity = AcceptsControl ? moveInput * moveSpeed * CurrentSpeedMultiplier : 0f;
            float rate = Mathf.Abs(targetVelocity) > 0.01f ? acceleration : deceleration;
            float nextVelocityX = Mathf.MoveTowards(
                body.linearVelocityX,
                targetVelocity,
                rate * Time.fixedDeltaTime);
            // 공중에서 벽/발판 옆면을 계속 밀면 마찰과 모서리 반력이 낙하를 받친다.
            // 바닥으로 인정되지 않는 접점에는 밀어 넣지 않고, 반대 방향 이탈은 허용한다.
            if (!IsGrounded && !IsClimbing && IsPushingIntoSide(nextVelocityX))
                nextVelocityX = 0f;
            // B-210: 땅에서 경직이면 미끄러지지 않고 그 자리에 선다. 공중은 좌우 조작만 없이 떨어진다.
            if (IsHitStunned && IsGrounded && !IsClimbing)
                nextVelocityX = 0f;
            body.linearVelocity = new Vector2(nextVelocityX, body.linearVelocityY);
        }

        private bool IsPushingIntoSide(float velocityX)
        {
            int count = body.GetContacts(contactBuffer);
            for (int i = 0; i < count; i++)
            {
                var contact = contactBuffer[i];
                if (contact.normal.y < minGroundNormalY && contact.normal.x * velocityX < -0.001f)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 지면에서만 점프 1회. 공중 연타 불가. 착지 후 다시 1회.
        /// </summary>
        private bool TryApplyJump()
        {
            if (!AcceptsControl || body == null)
            {
                jumpWindow.Clear();
                return false;
            }

            if (!jumpWindow.TryConsume(Time.time, IsGrounded, IsClimbing,
                    jumpUsedUntilLand, jumpAirLockRemaining > 0f))
                return false;

            if (IsClimbing)
            {
                // Trigger 안에 남아 있으면 접촉 정보를 보존해, 공중에서도 다시 사다리를 잡을 수 있다.
                ExitLadderMode();
                body.linearVelocity = new Vector2(body.linearVelocityX, 0f);
                // B-211: 사다리를 벗어나는 높이가 바뀌지 않게 사다리 점프는 자르지 않는다.
                ApplyJumpImpulse(allowReleaseCut: false);
                return true;
            }

            ApplyJumpImpulse(allowReleaseCut: true);
            return true;
        }

        /// <summary>
        /// B-211: 오르는 중 키가 떨어져 있으면 위 속도를 한 번만 줄인다. 내려가기 시작하면 더는 보지 않는다.
        /// 경직 여부와 관계없는 물리 규칙이다.
        /// </summary>
        private void TryCutJump()
        {
            if (!jumpCutArmed || body == null)
            {
                return;
            }

            float upwardSpeed = body.linearVelocityY;
            if (!variableJumpEnabled || IsClimbing || upwardSpeed <= 0f)
            {
                jumpCutArmed = false;
                return;
            }

            if (jumpHeld)
            {
                return;
            }

            float gravity = Mathf.Abs(Physics2D.gravity.y) * body.gravityScale;
            if (!VariableJumpRule.CanCut(body.position.y - jumpLaunchY, upwardSpeed,
                    jumpReleaseVelocityMultiplier, minJumpApexHeight, gravity, Time.fixedDeltaTime))
            {
                return;
            }

            body.linearVelocity = new Vector2(body.linearVelocityX, upwardSpeed * jumpReleaseVelocityMultiplier);
            jumpCutArmed = false;
            JumpCutCount++;
        }

        private void ApplyJumpImpulse(bool allowReleaseCut)
        {
            // 하강 중 점프 시 속도 상쇄 후 일정한 상승을 준다.
            if (body.linearVelocityY < 0f)
            {
                body.linearVelocity = new Vector2(body.linearVelocityX, 0f);
            }

            // B-175: 그림 단계 계산용 기록. 물리 값은 바꾸지 않는다.
            LastJumpLaunchSpeed = body.linearVelocityY
                + jumpImpulse * cargoJumpMultiplier / Mathf.Max(0.0001f, body.mass);
            JumpCount++;
            body.AddForce(
                Vector2.up * jumpImpulse * cargoJumpMultiplier,
                ForceMode2D.Impulse);
            jumpAirLockRemaining = jumpAirLockDuration;
            jumpUsedUntilLand = true;
            jumpCutArmed = allowReleaseCut && variableJumpEnabled;
            jumpLaunchY = body.position.y;
            IsGrounded = false;
            // 땅·코요테·입력 버퍼·사다리 점프가 모두 이곳을 지나므로 한 번만 난다.
            SfxRequests.Play(moveSfx.ResolveJump());
        }

        internal void SetLadderSpeedMultiplier(float multiplier) => ladderSpeedMultiplier = multiplier;

        private void ApplyVerticalMovement()
        {
            float climbSpeed = ladderSpeed * ladderSpeedMultiplier * CurrentSpeedMultiplier;
            // B-210: 경직 중에는 사다리에 매달린 채 멈춘다(중력 0 그대로).
            var verticalVelocity = AcceptsControl
                ? VerticalInput * climbSpeed
                : 0f;
            if (TryGetLadderTop(out var top))
            {
                var playerTop = PlayerTop;
                if (VerticalInput < -0.01f) ascendingTowardsLadderExit = false;
                if (verticalVelocity > 0f && playerTop <= top + 0.001f)
                    ascendingTowardsLadderExit = true;
                if (ascendingTowardsLadderExit && verticalVelocity > 0f)
                {
                    // Clear the ledge with the feet; stopping the head at the top traps the body below it.
                    float remaining = Mathf.Max(0f, top + 0.04f - PlayerBottom);
                    if (remaining <= 0.001f)
                    {
                        body.linearVelocity = new Vector2(body.linearVelocityX, 0f);
                        ExitLadderMode();
                        return;
                    }
                    verticalVelocity = Mathf.Min(verticalVelocity, remaining / Time.fixedDeltaTime);
                }
                else if (!ascendingTowardsLadderExit && playerTop > top + 0.001f)
                {
                    // 위에서 걸친 경우 손을 댈 수 있는 높이까지 입력과 무관하게 내려온다.
                    // 마지막 틱의 이동만 제한해 순간 위치 보정이나 아래쪽 초과 이동을 피한다.
                    verticalVelocity = AcceptsControl
                        ? -Mathf.Min(climbSpeed,
                            (playerTop - top) / Time.fixedDeltaTime)
                        : 0f;
                }
            }
            body.linearVelocity = new Vector2(body.linearVelocityX, verticalVelocity);
        }

        private bool TryGetLadderTop(out float top)
        {
            top = float.NegativeInfinity;
            var found = false;
            foreach (var ladder in activeLadders)
            {
                if (ladder != null && ladder.TryGetTop(out var ladderTop))
                {
                    top = Mathf.Max(top, ladderTop);
                    found = true;
                }
            }
            return found;
        }

        private void TryResumeLadderAfterJump()
        {
            if (body == null
                || IsClimbing
                || activeLadders.Count == 0
                || jumpAirLockRemaining > 0f
                || Mathf.Abs(VerticalInput) <= 0.01f)
            {
                return;
            }

            if (VerticalInput > 0f && TryGetLadderTop(out var top) && PlayerBottom >= top - 0.001f)
                return;
            EnterLadderMode();
        }

        private void UpdateGroundedState()
        {
            IsOnStableLandingPlatform = false;
            standingPlatform = null;
            // 에어 락 동안은 무조건 공중 취급 → 연타/즉시 재점프 불가.
            if (jumpAirLockRemaining > 0f)
            {
                IsGrounded = false;
                return;
            }

            // 표시/기타용 착지: 접점 또는 짧은 발 밑 레이.
            Collider2D floor = FindFloorContact();
            if (floor == null) floor = FindFloorProbeHit();
            bool onFloor = floor != null;

            // 강하게 상승 중이면 착지로 보지 않는다.
            if (onFloor && body != null && body.linearVelocityY > 0.4f)
            {
                IsGrounded = false;
                return;
            }

            IsGrounded = onFloor;
            IsOnStableLandingPlatform = onFloor && IsStableLandingPlatform(floor);
            if (IsOnStableLandingPlatform && floor.TryGetComponent<IAdjustableLandingPlatform>(out var adjustable))
                standingPlatform = adjustable;

            // 접점이 한 틱 누락돼도 발밑 보조 판정으로 착지를 확정한다.
            // 상승 중에는 위에서 반환하므로 공중에서 차지가 회복되지 않는다.
            if (jumpUsedUntilLand
                && onFloor
                && body != null
                && body.linearVelocityY <= 0.1f)
            {
                jumpUsedUntilLand = false;
                jumpCutArmed = false;
            }
        }

        /// <summary>
        /// Rigidbody 실제 충돌 접점 중 위쪽 노멀(바닥)만 착지로 인정.
        /// 공중에서는 접점이 없어 무한 점프 오탐을 막는다.
        /// </summary>
        private Collider2D FindFloorContact()
        {
            if (body == null) return null;

            Collider2D platform = null;
            int count = body.GetContacts(contactBuffer);
            for (int i = 0; i < count; i++)
            {
                ContactPoint2D contact = contactBuffer[i];
                Collider2D other = contact.collider;
                if (other == null || other.isTrigger)
                {
                    continue;
                }

                if (!IsInGroundLayer(other.gameObject.layer))
                {
                    continue;
                }

                // 플레이어 기준 상대 노멀: 바닥은 대략 (0,1).
                if (contact.normal.y >= minGroundNormalY)
                {
                    // When ordinary ground and a platform both support the player, preserve normal damage.
                    if (!IsStableLandingPlatform(other)) return other;
                    platform = other;
                }
            }

            return platform;
        }

        /// <summary>
        /// 접점이 한 프레임 누락될 때 보조. 발 아래 짧은 레이만 사용.
        /// </summary>
        private Collider2D FindFloorProbeHit()
        {
            Vector2 origin;
            if (groundCheck != null)
            {
                origin = groundCheck.position;
            }
            else if (bodyCollider != null)
            {
                Bounds b = bodyCollider.bounds;
                origin = new Vector2(b.center.x, b.min.y + 0.02f);
            }
            else
            {
                return null;
            }

            int hitCount = Physics2D.RaycastNonAlloc(
                origin,
                Vector2.down,
                probeHits,
                groundProbeDistance,
                groundLayers);

            Collider2D nearest = null;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = probeHits[i];
                if (hit.collider == null || hit.collider.isTrigger)
                {
                    continue;
                }

                if (bodyCollider != null && hit.collider == bodyCollider)
                {
                    continue;
                }

                if (hit.normal.y >= minGroundNormalY
                    && (hit.distance < nearestDistance
                        || (hit.distance == nearestDistance && !IsStableLandingPlatform(hit.collider))))
                {
                    nearest = hit.collider;
                    nearestDistance = hit.distance;
                }
            }

            return nearest;
        }

        private static bool IsPlatformAlive(IAdjustableLandingPlatform platform) =>
            platform != null && (platform is not UnityEngine.Object unityObject || unityObject != null);

        /// <summary>
        /// 서 있는 완충판의 높이를 한 단계 바꾸고, 같은 양만큼 몸을 옮겨 낙하·관통 없이 판 위에 남긴다.
        /// 판 위가 아니거나 사다리 중이거나 이미 최소/최대면 아무것도 바꾸지 않는다.
        /// </summary>
        public bool TryAdjustStandingPlatform(int direction)
        {
            if (!AcceptsControl || IsClimbing || body == null || direction == 0 || !IsOnAdjustablePlatform)
                return false;
            if (!standingPlatform.TryStepHeight(direction, out float delta, HasPlatformHeadroom))
                return false;

            body.position += new Vector2(0f, delta);
            body.linearVelocity = new Vector2(body.linearVelocityX, 0f);
            PlatformHeightAdjusted?.Invoke(delta);
            return true;
        }

        private bool HasPlatformHeadroom(float heightDelta)
        {
            if (heightDelta <= 0f) return true;
            if (bodyCollider == null || !bodyCollider.enabled) return false;

            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(groundLayers);
            int count = bodyCollider.Cast(Vector2.up, filter, platformCeilingHits,
                heightDelta + Physics2D.defaultContactOffset);
            for (int i = 0; i < count; i++)
            {
                var hit = platformCeilingHits[i];
                if (hit.normal.y >= 0f) continue;
                if (hit.collider.TryGetComponent<IAdjustableLandingPlatform>(out var platform)
                    && ReferenceEquals(platform, standingPlatform)) continue;
                return false;
            }

            return true;
        }

        private static bool IsStableLandingPlatform(Collider2D collider) =>
            collider.TryGetComponent<IStableLandingPlatform>(out _);

        private bool IsInGroundLayer(int layer)
        {
            return (groundLayers.value & (1 << layer)) != 0;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = groundCheck != null
                ? groundCheck.position
                : transform.position;

            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawLine(origin, origin + Vector3.down * groundProbeDistance);
            Gizmos.DrawWireSphere(origin, 0.04f);
        }
    }
}
