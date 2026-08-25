using System.Collections.Generic;
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
        private float moveInput;
        private float verticalMoveInput;
        private bool jumpRequested;
        private float jumpAirLockRemaining;
        /// <summary>
        /// true면 이번 공중 구간에서 이미 점프를 썼다.
        /// 착지(물리 접촉 + 비상승) 전까지 절대 회복하지 않는다.
        /// </summary>
        private bool jumpUsedUntilLand;
        private float gravityBeforeClimbing;
        private readonly HashSet<LadderZone> activeLadders = new HashSet<LadderZone>();
        private float cargoSpeedMultiplier = 1f;
        private float hazardSpeedMultiplier = 1f;

        public Vector2 Position => body != null ? body.position : (Vector2)transform.position;
        public float FacingDirection { get; private set; } = 1f;
        public bool IsGrounded { get; private set; }
        public bool CanMove { get; private set; } = true;
        public bool IsClimbing { get; private set; }
        public bool IsDescendingLadder => IsClimbing && verticalMoveInput < -0.01f;
        public bool IsMovementRequested => Mathf.Abs(moveInput) > 0.01f
            || Mathf.Abs(verticalMoveInput) > 0.01f;
        public float CurrentSpeedMultiplier => cargoSpeedMultiplier * hazardSpeedMultiplier;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
        }

        private void FixedUpdate()
        {
            if (jumpAirLockRemaining > 0f)
            {
                jumpAirLockRemaining = Mathf.Max(0f, jumpAirLockRemaining - Time.fixedDeltaTime);
            }

            // 점프 판정과 같은 물리 틱에서 착지를 갱신한다.
            UpdateGroundedState();
            ApplyHorizontalMovement();

            var jumped = TryApplyJump();
            if (IsClimbing && !jumped)
            {
                ApplyVerticalMovement();
            }
            else if (!jumped)
            {
                TryResumeLadderAfterJump();
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
            body.gravityScale = 0f;
            body.linearVelocity = new Vector2(body.linearVelocityX, 0f);
            IsClimbing = true;
            // 사다리 탑승 시 탈출 점프 1회 허용.
            jumpAirLockRemaining = 0f;
            jumpUsedUntilLand = false;
        }

        public void ExitLadder()
        {
            activeLadders.Clear();
            ExitLadderMode();
        }

        public void ExitLadder(LadderZone ladder)
        {
            if (ladder == null || !activeLadders.Remove(ladder) || activeLadders.Count > 0)
            {
                return;
            }

            ExitLadderMode();
        }

        private void ExitLadderMode()
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
            float nextVelocityX = Mathf.MoveTowards(
                body.linearVelocityX,
                targetVelocity,
                rate * Time.fixedDeltaTime);
            body.linearVelocity = new Vector2(nextVelocityX, body.linearVelocityY);
        }

        /// <summary>
        /// 지면에서만 점프 1회. 공중 연타 불가. 착지 후 다시 1회.
        /// </summary>
        private bool TryApplyJump()
        {
            if (!jumpRequested)
            {
                return false;
            }

            // 입력을 소모한다. 실패해도 버퍼를 붙잡지 않아 공중 연타가 쌓이지 않는다.
            jumpRequested = false;

            if (!CanMove || body == null)
            {
                return false;
            }

            if (jumpAirLockRemaining > 0f)
            {
                return false;
            }

            // 핵심: 점프 후 착지 확정 전까지는 IsGrounded 와 무관하게 차단.
            if (jumpUsedUntilLand && !IsClimbing)
            {
                return false;
            }

            if (IsClimbing)
            {
                // Trigger 안에 남아 있으면 접촉 정보를 보존해, 공중에서도 다시 사다리를 잡을 수 있다.
                ExitLadderMode();
                body.linearVelocity = new Vector2(body.linearVelocityX, 0f);
                ApplyJumpImpulse();
                return true;
            }

            // 같은 물리 틱에서 갱신한 접점/발밑 보조 판정을 사용한다.
            // 공중 재점프는 jumpUsedUntilLand가 별도로 차단한다.
            if (!IsGrounded)
            {
                return false;
            }

            ApplyJumpImpulse();
            return true;
        }

        private void ApplyJumpImpulse()
        {
            // 하강 중 점프 시 속도 상쇄 후 일정한 상승을 준다.
            if (body.linearVelocityY < 0f)
            {
                body.linearVelocity = new Vector2(body.linearVelocityX, 0f);
            }

            body.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
            jumpAirLockRemaining = jumpAirLockDuration;
            jumpUsedUntilLand = true;
            IsGrounded = false;
        }

        private void ApplyVerticalMovement()
        {
            var verticalVelocity = CanMove
                ? verticalMoveInput * ladderSpeed * CurrentSpeedMultiplier
                : 0f;
            body.linearVelocity = new Vector2(body.linearVelocityX, verticalVelocity);
        }

        private void TryResumeLadderAfterJump()
        {
            if (body == null
                || IsClimbing
                || activeLadders.Count == 0
                || jumpAirLockRemaining > 0f
                || Mathf.Abs(verticalMoveInput) <= 0.01f)
            {
                return;
            }

            EnterLadderMode();
        }

        private void UpdateGroundedState()
        {
            // 에어 락 동안은 무조건 공중 취급 → 연타/즉시 재점프 불가.
            if (jumpAirLockRemaining > 0f)
            {
                IsGrounded = false;
                return;
            }

            // 표시/기타용 착지: 접점 또는 짧은 발 밑 레이.
            bool floorContact = HasFloorContact();
            bool onFloor = floorContact || HasFloorProbeHit();

            // 강하게 상승 중이면 착지로 보지 않는다.
            if (onFloor && body != null && body.linearVelocityY > 0.4f)
            {
                IsGrounded = false;
                return;
            }

            IsGrounded = onFloor;

            // 접점이 한 틱 누락돼도 발밑 보조 판정으로 착지를 확정한다.
            // 상승 중에는 위에서 반환하므로 공중에서 차지가 회복되지 않는다.
            if (jumpUsedUntilLand
                && onFloor
                && body != null
                && body.linearVelocityY <= 0.1f)
            {
                jumpUsedUntilLand = false;
            }
        }

        /// <summary>
        /// Rigidbody 실제 충돌 접점 중 위쪽 노멀(바닥)만 착지로 인정.
        /// 공중에서는 접점이 없어 무한 점프 오탐을 막는다.
        /// </summary>
        private bool HasFloorContact()
        {
            if (body == null)
            {
                return false;
            }

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
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 접점이 한 프레임 누락될 때 보조. 발 아래 짧은 레이만 사용.
        /// </summary>
        private bool HasFloorProbeHit()
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
                return false;
            }

            int hitCount = Physics2D.RaycastNonAlloc(
                origin,
                Vector2.down,
                probeHits,
                groundProbeDistance,
                groundLayers);

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

                if (hit.normal.y >= minGroundNormalY)
                {
                    return true;
                }
            }

            return false;
        }

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
