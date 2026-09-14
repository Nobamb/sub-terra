using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    // SpriteRenderer를 직접 갱신한다. Animator 클립은 에셋 미리보기/확장용으로 보존하되,
    // 런타임 표시가 Animator 평가 순서에 의존하지 않도록 한다.
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        private const float DamageDuration = 0.35f;
        private const float LadderMovementEpsilon = 0.0001f;
        private const float LadderVisualGraceDuration = 0.1f;
        private const int LadderSequenceLength = 4;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] jumpFrames;
        [SerializeField] private Sprite[] ladderFrames;
        [SerializeField] private Sprite[] miningFrames;
        [SerializeField] private Sprite[] damageFrames;
        [SerializeField] private Sprite[] knockoutFrames;
        [SerializeField, Min(0.01f)] private float ladderDistancePerFrame = 0.5f;
        [SerializeField, Min(0f)] private float ladderSettleDelay = 0.08f;

        [SerializeField] private PlayerMovement movement;
        private bool isMining;
        private PlayerSurvivalController survival;
        private float previousHealth;
        private float damageUntil;
        private string currentState;
        private float stateStartedAt;
        private bool survivalEventsBound;
        private float ladderTravelDistance;
        private int ladderSequenceIndex;
        private float previousLadderY;
        private float ladderStillTime;
        private bool ladderPositionCaptured;
        private float ladderVisualGraceUntil;

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
            ResetLadderPlayback();
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

            var stateName = ResolveStateName();
            if (IsLadderState(stateName))
            {
                ladderVisualGraceUntil = Time.unscaledTime + LadderVisualGraceDuration;
                PlayLadder(stateName);
                return;
            }

            if (ShouldHoldLadderVisual(stateName))
            {
                HoldLadderVisual();
                return;
            }

            ResetLadderPlayback();
            Play(stateName);
        }

        public void ConfigureFrames(
            SpriteRenderer renderer,
            PlayerMovement playerMovement,
            Sprite[] idle,
            Sprite[] walk,
            Sprite[] jump,
            Sprite[] ladder,
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
            miningFrames = mining;
            damageFrames = damage;
            knockoutFrames = knockout;
            ResetLadderPlayback();
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

            // Trigger 접촉이 아직 남아 있는 동안에는 물리 상태 전환 한 틱 때문에
            // 일반 Walk/Jump 프레임이 사다리 표시를 덮어쓰지 않게 한다.
            if (movement.IsClimbing
                || (movement.IsTouchingLadder && !movement.IsJumpInProgress))
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
            SetCurrentState(stateName);

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
                "Mining" => (miningFrames, 10f, true),
                "Damage" => (damageFrames, 10f, false),
                "Knockout" => (knockoutFrames, 8f, false),
                _ => (idleFrames, 4f, true)
            };
        }

        private void PlayLadder(string stateName)
        {
            SetCurrentState(stateName);
            if (ladderFrames == null || ladderFrames.Length < 3)
            {
                return;
            }

            var currentY = movement.Position.y;
            if (!ladderPositionCaptured)
            {
                previousLadderY = currentY;
                ladderPositionCaptured = true;
                SetLadderFrame(0);
                return;
            }

            var verticalDistance = currentY - previousLadderY;
            previousLadderY = currentY;
            if (stateName == "LadderIdle")
            {
                ResetLadderPhase();
                ladderStillTime = 0f;
                SetLadderFrame(0);
                return;
            }

            if (Mathf.Abs(verticalDistance) > LadderMovementEpsilon)
            {
                ladderTravelDistance += verticalDistance;
                ladderStillTime = 0f;
                SetLadderFrame(ResolveLadderFrameIndex());
                return;
            }

            ladderStillTime += Time.unscaledDeltaTime;
            if (ladderStillTime >= ladderSettleDelay)
            {
                ResetLadderPhase();
                SetLadderFrame(0);
            }
        }

        private int ResolveLadderFrameIndex()
        {
            var distancePerFrame = Mathf.Max(0.01f, ladderDistancePerFrame);
            var steps = Mathf.FloorToInt(Mathf.Abs(ladderTravelDistance) / distancePerFrame);
            if (steps > 0)
            {
                var direction = ladderTravelDistance > 0f ? 1 : -1;
                ladderSequenceIndex += direction * steps;
                ladderSequenceIndex = ((ladderSequenceIndex % LadderSequenceLength) + LadderSequenceLength)
                    % LadderSequenceLength;
                ladderTravelDistance -= direction * steps * distancePerFrame;
            }

            return ladderSequenceIndex switch
            {
                1 => 1,
                3 => 2,
                _ => 0
            };
        }

        private void SetLadderFrame(int frameIndex)
        {
            var frame = ladderFrames[frameIndex];
            if (frame != null)
            {
                spriteRenderer.sprite = frame;
            }
        }

        private void ResetLadderPlayback()
        {
            ResetLadderPhase();
            ladderStillTime = 0f;
            ladderPositionCaptured = false;
            ladderVisualGraceUntil = 0f;
        }

        private bool ShouldHoldLadderVisual(string nextStateName)
        {
            if (!IsLadderState(currentState)
                || movement.IsTouchingLadder
                || movement.IsJumpInProgress
                || Time.unscaledTime >= ladderVisualGraceUntil)
            {
                return false;
            }

            return nextStateName != "Mining"
                && nextStateName != "Damage"
                && nextStateName != "Knockout";
        }

        private void HoldLadderVisual()
        {
            // 접촉이 한 틱 끊겨도 낙하 거리를 등반 프레임 진행량으로 누적하지 않는다.
            previousLadderY = movement.Position.y;
            ladderPositionCaptured = true;
        }

        private void ResetLadderPhase()
        {
            ladderTravelDistance = 0f;
            ladderSequenceIndex = 0;
        }

        private void SetCurrentState(string stateName)
        {
            if (currentState == stateName)
            {
                return;
            }

            currentState = stateName;
            stateStartedAt = Time.unscaledTime;
        }

        private static bool IsLadderState(string stateName)
        {
            return stateName == "Ladder" || stateName == "LadderDown" || stateName == "LadderIdle";
        }
    }
}
