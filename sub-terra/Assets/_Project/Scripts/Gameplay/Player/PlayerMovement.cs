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

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.16f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private Rigidbody2D body;
        private Collider2D[] ownColliders;
        private readonly Collider2D[] groundHits = new Collider2D[8];
        private float moveInput;
        private bool jumpRequested;
        private float cargoSpeedMultiplier = 1f;
        private float hazardSpeedMultiplier = 1f;

        public Vector2 Position => body != null ? body.position : (Vector2)transform.position;
        public float FacingDirection { get; private set; } = 1f;
        public bool IsGrounded { get; private set; }
        public bool CanMove { get; private set; } = true;
        public float CurrentSpeedMultiplier => cargoSpeedMultiplier * hazardSpeedMultiplier;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ownColliders = GetComponentsInChildren<Collider2D>();
        }

        private void Update()
        {
            UpdateGroundedState();
        }

        private void FixedUpdate()
        {
            ApplyHorizontalMovement();
            ApplyJump();
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

        private void ApplyJump()
        {
            if (!jumpRequested)
            {
                return;
            }

            jumpRequested = false;
            if (!CanMove || !IsGrounded)
            {
                return;
            }

            body.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
            IsGrounded = false;
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

            IsGrounded = false;
            for (int index = 0; index < hitCount; index++)
            {
                Collider2D hit = groundHits[index];
                if (hit != null && !IsOwnCollider(hit))
                {
                    IsGrounded = true;
                    break;
                }
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
