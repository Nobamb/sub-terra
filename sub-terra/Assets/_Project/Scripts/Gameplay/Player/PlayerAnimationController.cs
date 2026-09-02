using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    // SpriteRenderer를 직접 갱신한다. Animator 클립은 에셋 미리보기/확장용으로 보존하되,
    // 런타임 표시가 Animator 평가 순서에 의존하지 않도록 한다.
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        private const float DamageDuration = 0.35f;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] jumpFrames;
        [SerializeField] private Sprite[] ladderFrames;
        [SerializeField] private Sprite[] ladderDownFrames;
        [SerializeField] private Sprite[] miningFrames;
        [SerializeField] private Sprite[] damageFrames;
        [SerializeField] private Sprite[] knockoutFrames;

        [SerializeField] private PlayerMovement movement;
        private bool isMining;
        private PlayerSurvivalController survival;
        private float previousHealth;
        private float damageUntil;
        private string currentState;
        private float stateStartedAt;
        private bool survivalEventsBound;

        private void Awake()
        {
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            animator ??= GetComponent<Animator>();
            // Animator가 같은 SpriteRenderer에 매 프레임 Idle을 덮어쓰지 않게 한다.
            if (animator != null)
            {
                animator.enabled = false;
            }

            movement = GetComponentInParent<PlayerMovement>();
            survival = GetComponentInParent<PlayerSurvivalController>();
            previousHealth = survival?.State?.Health ?? 0f;
            stateStartedAt = Time.unscaledTime;
        }

        private void OnEnable()
        {
            if (survival == null)
            {
                survival = GetComponentInParent<PlayerSurvivalController>();
            }

            SubscribeSurvivalEvents();
        }

        private void OnDisable()
        {
            UnsubscribeSurvivalEvents();
        }

        public void BindSurvival(PlayerSurvivalController survivalController)
        {
            if (survival == survivalController)
            {
                return;
            }

            UnsubscribeSurvivalEvents();
            survival = survivalController;
            previousHealth = survival?.State?.Health ?? previousHealth;
            SubscribeSurvivalEvents();
        }

        private void LateUpdate()
        {
            if (spriteRenderer == null || movement == null)
            {
                return;
            }

            Play(ResolveStateName());
        }

        public void ConfigureFrames(
            SpriteRenderer renderer,
            PlayerMovement playerMovement,
            Sprite[] idle,
            Sprite[] walk,
            Sprite[] jump,
            Sprite[] ladder,
            Sprite[] ladderDown,
            Sprite[] mining,
            Sprite[] damage,
            Sprite[] knockout)
        {
            spriteRenderer = renderer;
            movement = playerMovement;
            idleFrames = idle;
            walkFrames = walk;
            jumpFrames = jump;
            ladderFrames = ladder;
            ladderDownFrames = ladderDown;
            miningFrames = mining;
            damageFrames = damage;
            knockoutFrames = knockout;
        }

        public void SetMining(bool value)
        {
            isMining = value;
        }

        private string ResolveStateName()
        {
            if (survival?.State != null && !survival.State.CanAct)
            {
                return "Knockout";
            }

            if (Time.unscaledTime < damageUntil)
            {
                return "Damage";
            }

            if (isMining)
            {
                return "Mining";
            }

            if (movement.IsClimbing)
            {
                if (!movement.IsMovingOnLadder)
                {
                    return "LadderIdle";
                }

                return movement.IsDescendingLadder ? "LadderDown" : "Ladder";
            }

            if (!movement.IsGrounded)
            {
                return "Jump";
            }

            return movement.IsMovementRequested ? "Walk" : "Idle";
        }

        private void HandleHealthChanged(SubTerra.Shared.PlayerHealthReadModel health)
        {
            if (health.Current < previousHealth)
            {
                damageUntil = Time.unscaledTime + DamageDuration;
            }

            previousHealth = health.Current;
        }

        private void HandleFailureRequested(SubTerra.Shared.RunFailureInputDto _)
        {
            damageUntil = 0f;
            Play("Knockout");
        }

        private void SubscribeSurvivalEvents()
        {
            if (survival == null || survivalEventsBound || !isActiveAndEnabled)
            {
                return;
            }

            survival.HealthChanged += HandleHealthChanged;
            survival.FailureRequested += HandleFailureRequested;
            previousHealth = survival.State?.Health ?? previousHealth;
            survivalEventsBound = true;
        }

        private void UnsubscribeSurvivalEvents()
        {
            if (survival == null || !survivalEventsBound)
            {
                return;
            }

            survival.HealthChanged -= HandleHealthChanged;
            survival.FailureRequested -= HandleFailureRequested;
            survivalEventsBound = false;
        }

        private void Play(string stateName)
        {
            if (currentState != stateName)
            {
                currentState = stateName;
                stateStartedAt = Time.unscaledTime;
            }

            var (frames, frameRate, loop) = ResolveFrames(stateName);
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            var elapsed = Mathf.Max(0f, Time.unscaledTime - stateStartedAt);
            var frameIndex = Mathf.FloorToInt(elapsed * frameRate);
            frameIndex = loop
                ? frameIndex % frames.Length
                : Mathf.Min(frameIndex, frames.Length - 1);

            if (frames[frameIndex] != null)
            {
                spriteRenderer.sprite = frames[frameIndex];
            }
        }

        private (Sprite[] Frames, float FrameRate, bool Loop) ResolveFrames(string stateName)
        {
            return stateName switch
            {
                "Walk" => (walkFrames, 10f, true),
                "Jump" => (jumpFrames, 12f, false),
                "Ladder" => (ladderFrames, 8f, true),
                "LadderDown" => (ladderDownFrames, 8f, true),
                "LadderIdle" => (ladderFrames, 0f, false),
                "Mining" => (miningFrames, 10f, true),
                "Damage" => (damageFrames, 10f, false),
                "Knockout" => (knockoutFrames, 8f, false),
                _ => (idleFrames, 4f, true)
            };
        }
    }
}
