using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(0f)] private float acceleration = 45f;
        [SerializeField, Min(0f)] private float deceleration = 55f;
        [SerializeField, Min(0f)] private float jumpImpulse = 11f;
        [SerializeField, Min(0f)] private float ladderSpeed = 4f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.16f;
        [SerializeField] private LayerMask groundLayers = ~0;
        /// <summary>
        /// 점프 직후 이 시간 동안은 지면 오버랩이 남아도 착지로 보지 않는다.
        /// (점프 직후 groundCheck가 여전히 지면과 겹쳐 무한 점프가 나는 것을 막는다.)
        /// </summary>
        [SerializeField, Min(0.01f)] private float jumpAirLockDuration = 0.18f;

        private Rigidbody2D body;
        private Collider2D[] ownColliders;
        private readonly Collider2D[] groundHits = new Collider2D[8];
        private float moveInput;
        private float verticalMoveInput;
        private bool jumpRequested;
        /// <summary>착지(또는 사다리 재진입) 전까지 점프를 1회만 허용한다.</summary>
        private bool hasUsedAirJump;
        /// <summary>점프 후 지면을 한 번이라도 떠난 적이 있는지.</summary>
        private bool hasLeftGroundSinceJump;
        /// <summary>점프 직후 착지 판정·점프 회복을 막는 남은 시간.</summary>
        private float jumpAirLockRemaining;
        private float gravityBeforeClimbing;
        private float cargoSpeedMultiplier = 1f;
        private float hazardSpeedMultiplier = 1f;

        public Vector2 Position => body != null ? body.position : (Vector2)transform.position;
        public float FacingDirection { get; private set; } = 1f;
        public bool IsGrounded { get; private set; }
        public bool CanMove { get; private set; } = true;
        public bool IsClimbing { get; private set; }
        public bool IsMovementRequested => Mathf.Abs(moveInput) > 0.01f
            || Mathf.Abs(verticalMoveInput) > 0.01f;
        public float CurrentSpeedMultiplier => cargoSpeedMultiplier * hazardSpeedMultiplier;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ownColliders = GetComponentsInChildren<Collider2D>();
        }

        private void Update()
        {
            if (jumpAirLockRemaining > 0f)
            {
                jumpAirLockRemaining = Mathf.Max(0f, jumpAirLockRemaining - Time.deltaTime);
            }

            UpdateGroundedState();
        }

        private void FixedUpdate()
        {
            ApplyHorizontalMovement();
            // 점프는 지면·사다리 모두에서 처리한다. 사다리 점프 시 등반은 그 프레임 생략.
            var jumped = TryApplyJump();
            if (IsClimbing && !jumped)
            {
                ApplyVerticalMovement();
            }
        }

        private void OnDisable()
        {
            ExitLadder();
        }

        public void SetMoveInput(float horizontal)
        {
            moveInput = Mathf.Clamp(horizontal, -1f, 1f);

            if (Mathf.Abs(moveInput) > 0.01f)
            {
                FacingDirection = Mathf.Sign(moveInput);
            }
        }

        public void RequestJump()
        {
            jumpRequested = true;
        }

        public void SetVerticalMoveInput(float vertical)
        {
            verticalMoveInput = Mathf.Clamp(vertical, -1f, 1f);
        }

        public void EnterLadder()
        {
            if (IsClimbing || body == null)
            {
                return;
            }

            gravityBeforeClimbing = body.gravityScale;
            body.gravityScale = 0f;
            body.linearVelocity = new Vector2(body.linearVelocityX, 0f);
            IsClimbing = true;
            // 사다리에 붙으면 다시 점프 1회를 허용한다.
            ResetJumpCharge();
        }

        public void ExitLadder()
        {
            if (!IsClimbing || body == null)
            {
                return;
            }

            body.gravityScale = gravityBeforeClimbing;
            verticalMoveInput = 0f;
            IsClimbing = false;
        }

        public void SetCanMove(bool canMove)
        {
            CanMove = canMove;
            if (!canMove)
            {
                moveInput = 0f;
            }
        }

        public void SetCargoSpeedMultiplier(float multiplier)
        {
            cargoSpeedMultiplier = Mathf.Max(0f, multiplier);
        }

        public void SetHazardSpeedMultiplier(float multiplier)
        {
            hazardSpeedMultiplier = Mathf.Max(0f, multiplier);
        }

        private void ApplyHorizontalMovement()
        {
            float targetVelocity = CanMove ? moveInput * moveSpeed * CurrentSpeedMultiplier : 0f;
            float rate = Mathf.Abs(targetVelocity) > 0.01f ? acceleration : deceleration;
            float nextVelocityX = Mathf.MoveTowards(body.linearVelocityX, targetVelocity, rate * Time.fixedDeltaTime);
            body.linearVelocity = new Vector2(nextVelocityX, body.linearVelocityY);
        }

        /// <summary>
        /// 지면 착지 전 점프 1회. 사다리 탑승 중에도 점프 가능(사다리 탈출 후 상승 임펄스).
        /// </summary>
        /// <returns>이번 FixedUpdate에서 점프를 적용했으면 true.</returns>
        private bool TryApplyJump()
        {
            if (!jumpRequested)
            {
                return false;
            }

            jumpRequested = false;
            if (!CanMove || body == null)
            {
                return false;
            }

            // 공중 연타·점프 직후 재점프 차단.
            if (hasUsedAirJump || jumpAirLockRemaining > 0f)
            {
                return false;
            }

            // 사다리 중: 탈출 후 점프.
            if (IsClimbing)
            {
                ExitLadder();
                body.linearVelocity = new Vector2(body.linearVelocityX, 0f);
                body.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
                ConsumeJumpCharge();
                IsGrounded = false;
                return true;
            }

            // 지면 점프: 물리적으로 지면에 있을 때만.
            if (!IsGrounded)
            {
                return false;
            }

            body.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
            ConsumeJumpCharge();
            IsGrounded = false;
            return true;
        }

        private void ConsumeJumpCharge()
        {
            hasUsedAirJump = true;
            hasLeftGroundSinceJump = false;
            jumpAirLockRemaining = jumpAirLockDuration;
        }

        private void ResetJumpCharge()
        {
            hasUsedAirJump = false;
            hasLeftGroundSinceJump = false;
            jumpAirLockRemaining = 0f;
        }

        private void ApplyVerticalMovement()
        {
            var verticalVelocity = CanMove
                ? verticalMoveInput * ladderSpeed * CurrentSpeedMultiplier
                : 0f;
            body.linearVelocity = new Vector2(body.linearVelocityX, verticalVelocity);
        }

        private void UpdateGroundedState()
        {
            if (groundCheck == null)
            {
                IsGrounded = false;
                return;
            }

            int hitCount = Physics2D.OverlapCircleNonAlloc(
                groundCheck.position,
                groundCheckRadius,
                groundHits,
                groundLayers);

            bool physicallyGrounded = false;
            for (int index = 0; index < hitCount; index++)
            {
                Collider2D hit = groundHits[index];
                if (hit != null && !IsOwnCollider(hit))
                {
                    physicallyGrounded = true;
                    break;
                }
            }

            // 점프 직후 에어 락 동안은 지면 오버랩이 있어도 착지로 취급하지 않는다.
            if (jumpAirLockRemaining > 0f)
            {
                IsGrounded = false;
                if (!physicallyGrounded)
                {
                    hasLeftGroundSinceJump = true;
                }

                return;
            }

            if (!physicallyGrounded)
            {
                IsGrounded = false;
                if (hasUsedAirJump)
                {
                    hasLeftGroundSinceJump = true;
                }

                return;
            }

            IsGrounded = true;

            // 착지 회복 조건:
            // 1) 점프 후 실제로 지면을 떠났다가 다시 닿았고 하강/정지 중일 때
            // 2) 사다리가 아닐 때
            // (점프 직후 지면 체크가 남아 있어도 1번이 충족되기 전에는 회복하지 않음)
            if (hasUsedAirJump
                && !IsClimbing
                && hasLeftGroundSinceJump
                && body != null
                && body.linearVelocityY <= 0.05f)
            {
                ResetJumpCharge();
            }
        }

        private bool IsOwnCollider(Collider2D candidate)
        {
            if (ownColliders == null)
            {
                return false;
            }

            foreach (Collider2D ownCollider in ownColliders)
            {
                if (candidate == ownCollider)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null)
            {
                return;
            }

            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
