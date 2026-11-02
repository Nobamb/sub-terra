using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    // SpriteRenderer를 직접 갱신한다. Animator 클립은 에셋 미리보기/확장용으로 보존하되,
    // 런타임 표시가 Animator 평가 순서에 의존하지 않도록 한다.
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        public const int WalkFrameCount = SubTerra.Shared.PlayerWalkCycle.FrameCount;
        public const float WalkFrameRate = SubTerra.Shared.PlayerWalkCycle.FrameRate;
        // B-176: 다침 8장 × 20fps. 마지막 4-idle 장까지 보이게 한다.
        private const float DamageDuration = 0.4f;
        private const float WalkGroundVisualGraceDuration = 0.06f;
        private const float LadderMovementEpsilon = 0.0001f;
        private const float LadderVisualGraceDuration = 0.1f;
        // 중립 → 오른쪽 중간/상승/중간 → 중립 → 왼쪽 중간/상승/중간.
        private static readonly int[] FiveFrameLadderSequence = { 0, 1, 2, 1, 0, 3, 4, 3 };
        // B-164: 등반 순서에서 자세가 꼭대기에 닿는 인덱스(5프레임: 2·6, 3프레임: 1·3).
        private const int FiveFrameLadderTopA = 2;
        private const int FiveFrameLadderTopB = 6;
        private const float FootstepMinHorizontalSpeed = 0.1f;
        private const float FootstepAlternatePitch = 0.96f;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [Tooltip("B-175 이전 점프 10장. 새 단계 그림 3묶음이 없을 때만 쓴다.")]
        [SerializeField] private Sprite[] jumpFrames;
        [SerializeField] private Sprite[] jumpReadyFrames;
        [SerializeField] private Sprite[] jumpRiseFrames;
        [SerializeField] private Sprite[] jumpFallFrames;
        [SerializeField] private Sprite[] ladderFrames;
        [SerializeField] private Sprite[] miningFrames;
        [SerializeField] private Sprite[] ladderMiningFrames;
        [SerializeField] private Sprite[] damageFrames;
        [SerializeField] private Sprite[] knockoutFrames;
        [SerializeField] private PlayerMiningAnimationSettings miningAnimation = new();
        [SerializeField] private PlayerLadderMiningAnimationSettings ladderMiningAnimation = new();
        [SerializeField] private PlayerJumpAnimationSettings jumpAnimation = new();
        [SerializeField, Min(0.01f)] private float damageFps = 20f;
        [SerializeField, Min(0.01f)] private float knockoutFps = 24f;
        [SerializeField, Min(0.01f)] private float knockoutBreathingFps = 12f;
        [SerializeField, Min(0.01f)] private float ladderDistancePerFrame = 0.5f;
        [SerializeField, Min(0f)] private float ladderSettleDelay = 0.08f;
        [Tooltip("B-164: 걷기 그림에서 발이 닿는 프레임. 이 프레임으로 넘어갈 때 발소리를 낸다.")]
        [SerializeField] private int[] footstepFrames = { 1, 21 };

        [SerializeField] private PlayerMovement movement;
        private bool isMining;
        private PlayerMiningAnimationSequence miningSequence;
        private PlayerLadderMiningAnimationSequence ladderMiningSequence;
        private bool miningOnLadder;
        private SubTerra.Shared.RunFailureCause knockoutCause;
        private bool wasIncapacitated;
        private PlayerSurvivalController survival;
        private float previousHealth;
        private float damageUntil;
        private int ladderDamageDirection = 1;
        private string currentState;
        private float stateStartedAt;
        private float lastGroundedAt = float.NegativeInfinity;
        private bool survivalEventsBound;
        private float ladderTravelDistance;
        private int ladderSequenceIndex;
        private float previousLadderY;
        private float ladderStillTime;
        private bool ladderPositionCaptured;
        private float ladderVisualGraceUntil;
        private int lastWalkFrame = -1;
        private bool footstepAlternate;
        private PlayerJumpAnimationSequence jumpSequence;
        private int observedJumpCount;
        private bool jumpBobApplied;
        private float jumpBobBaseY;

        public bool IsShowingLadderVisual => IsLadderState(currentState);
        /// <summary>B-175: 점프 그림 단계. #265 연출은 PhaseChanged를 구독한다.</summary>
        public PlayerJumpAnimationSequence JumpAnimation => JumpSequence;
        public bool IsShowingLadderMiningVisual => currentState == "LadderMining";
        public int LadderMiningDirection => LadderMiningSequence.Direction;
        /// <summary>B-176: 사다리를 잡은 채 다치는 중. 이동 방향 대신 LadderDamageDirection으로 고정한다.</summary>
        public bool IsShowingLadderDamageVisual => currentState == "Damage" && IsHoldingLadder;
        public int LadderDamageDirection => ladderDamageDirection;
        public bool CanMineOnLadder => movement != null && !movement.IsJumpInProgress
            && !movement.IsApproachingLadderFromAbove
            && (movement.IsClimbing || (movement.IsTouchingLadder
                && (IsShowingLadderVisual || IsShowingLadderMiningVisual || miningOnLadder)));
        private PlayerMiningAnimationSequence MiningSequence =>
            miningSequence ??= new PlayerMiningAnimationSequence(miningAnimation);
        private PlayerLadderMiningAnimationSequence LadderMiningSequence =>
            ladderMiningSequence ??= new PlayerLadderMiningAnimationSequence(ladderMiningAnimation);
        private PlayerJumpAnimationSequence JumpSequence =>
            jumpSequence ??= new PlayerJumpAnimationSequence(jumpAnimation);
        private bool IsHoldingLadder => movement != null
            && (movement.IsClimbing || (movement.IsTouchingLadder && !movement.IsJumpInProgress));
        private bool HasJumpPhaseFrames => HasFrames(jumpReadyFrames) && HasFrames(jumpRiseFrames)
            && HasFrames(jumpFallFrames);

        private void OnValidate()
        {
            miningAnimation.miningReturnFrame = Mathf.Clamp(miningAnimation.miningReturnFrame, 5, 12);
        }

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
            if (movement != null)
            {
                observedJumpCount = movement.JumpCount;
            }
        }

        private void OnDisable()
        {
            UnsubscribeSurvivalEvents();
            ResetLadderPlayback();
            MiningSequence.Reset();
            LadderMiningSequence.Reset();
            JumpSequence.Reset();
            ApplyJumpBob(0f);
            miningOnLadder = false;
            isMining = false;
            knockoutCause = SubTerra.Shared.RunFailureCause.Unknown;
            wasIncapacitated = false;
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

            if (movement.IsGrounded)
            {
                lastGroundedAt = Time.unscaledTime;
            }

            bool incapacitated = survival != null && survival.State != null && !survival.State.CanAct;
            if (incapacitated)
            {
                if (!wasIncapacitated && knockoutCause == SubTerra.Shared.RunFailureCause.Unknown)
                    knockoutCause = survival.State.LastDamageCause;
                MiningSequence.Reset();
                LadderMiningSequence.Reset();
                isMining = false;
            }
            else if (wasIncapacitated)
            {
                knockoutCause = SubTerra.Shared.RunFailureCause.Unknown;
                damageUntil = 0f;
                MiningSequence.Reset();
                LadderMiningSequence.Reset();
                isMining = false;
                SetCurrentState("Idle");
            }
            wasIncapacitated = incapacitated;

            if (!incapacitated && (movement.IsMovementRequested || movement.IsJumpInProgress
                || (LadderMiningSequence.IsActive && !CanMineOnLadder)))
            {
                MiningSequence.Interrupt();
                LadderMiningSequence.Interrupt();
                isMining = false;
            }
            if (Time.unscaledTime >= damageUntil)
            {
                if (currentState == "Damage")
                {
                    if (isMining)
                    {
                        if (miningOnLadder) LadderMiningSequence.ResumeAfterDamage();
                        else MiningSequence.ResumeAfterDamage();
                    }
                    else
                    {
                        MiningSequence.Reset();
                        LadderMiningSequence.Reset();
                    }
                }
                MiningSequence.Tick(Time.unscaledDeltaTime);
                LadderMiningSequence.Tick(Time.unscaledDeltaTime);
            }

            UpdateJumpSequence(incapacitated);
            var stateName = ResolveStateName();
            if (stateName == "Mining" || stateName == "LadderMining")
            {
                JumpSequence.Reset();
            }

            ApplyJumpBob(stateName == "Jump" && HasJumpPhaseFrames ? JumpSequence.BobOffsetPixels() : 0f);
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
            if (value == isMining) return;
            isMining = value;
            if (value)
            {
                miningOnLadder = false;
                LadderMiningSequence.Reset();
                MiningSequence.Start();
            }
            else if (miningOnLadder) LadderMiningSequence.Stop();
            else MiningSequence.Stop();
        }

        public void SetLadderMining(int direction)
        {
            if (isMining && miningOnLadder) return;
            isMining = true;
            miningOnLadder = true;
            MiningSequence.Reset();
            LadderMiningSequence.Start(direction);
        }

        public void InterruptMining()
        {
            isMining = false;
            MiningSequence.Interrupt();
            LadderMiningSequence.Interrupt();
        }

        public void NotifyMiningCompleted()
        {
            if (miningOnLadder) LadderMiningSequence.Complete();
            else MiningSequence.Complete();
            // 다음 블록의 SetMining(true)는 마무리 중에도 새 시작으로 구분한다.
            isMining = false;
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

            if (LadderMiningSequence.IsActive)
            {
                return "LadderMining";
            }

            if (MiningSequence.IsActive)
            {
                return "Mining";
            }

            if (movement.IsApproachingLadderFromAbove)
            {
                return "LadderDrop";
            }

            // Trigger 접촉이 아직 남아 있는 동안에는 물리 상태 전환 한 틱 때문에
            // 일반 Walk/Jump 프레임이 사다리 표시를 덮어쓰지 않게 한다.
            if (IsHoldingLadder)
            {
                if (!movement.IsMovingOnLadder)
                {
                    return "LadderIdle";
                }

                return movement.IsDescendingLadder ? "LadderDown" : "Ladder";
            }

            if (!movement.IsGrounded)
            {
                return IsWalkGroundGrace() ? "Walk" : "Jump";
            }

            // 착지 그림(ready 8→1)은 이동 입력이 없을 때만 이어 간다.
            if (HasJumpPhaseFrames && JumpSequence.Phase == PlayerJumpPhase.Land
                && !movement.IsMovementRequested)
            {
                return "Jump";
            }

            return movement.IsMovementRequested ? "Walk" : "Idle";
        }

        // 타일 경계의 한 틱 접지 누락이 걷기 순서를 처음부터 재시작하지 않게 한다.
        private bool IsWalkGroundGrace()
        {
            return currentState == "Walk"
                && movement.IsMovementRequested
                && !movement.IsJumpInProgress
                && Time.unscaledTime - lastGroundedAt <= WalkGroundVisualGraceDuration;
        }

        private void UpdateJumpSequence(bool incapacitated)
        {
            // 착지 틱에 버퍼 점프가 바로 실행되면 공중 상태가 끊기지 않으므로 실행 횟수로 본다.
            bool started = movement.JumpCount != observedJumpCount;
            observedJumpCount = movement.JumpCount;

            if (incapacitated || IsHoldingLadder)
            {
                JumpSequence.Reset();
                return;
            }

            if (started)
            {
                JumpSequence.StartJump(movement.LastJumpLaunchSpeed);
            }

            bool airborne = !movement.IsGrounded && !IsWalkGroundGrace();
            JumpSequence.Tick(started ? 0f : Time.unscaledDeltaTime, airborne,
                movement.VerticalSpeed, movement.IsMovementRequested);
        }

        private void PlayJumpPhase()
        {
            var frames = JumpSequence.Clip switch
            {
                PlayerJumpClip.Ready => jumpReadyFrames,
                PlayerJumpClip.Rise => jumpRiseFrames,
                _ => jumpFallFrames
            };
            var frame = frames[Mathf.Clamp(JumpSequence.FrameIndex, 0, frames.Length - 1)];
            if (frame != null)
            {
                spriteRenderer.sprite = frame;
            }
        }

        // 사다리 위에서 내려오기 직전 표시는 낙하 반복 그림을 시간으로 돌린다.
        private void PlayLadderDrop()
        {
            int first = Mathf.Clamp(jumpAnimation.fallLoopFirstFrame, 1, jumpFallFrames.Length) - 1;
            int last = Mathf.Clamp(jumpAnimation.fallLoopLastFrame, first + 1, jumpFallFrames.Length) - 1;
            var elapsed = Mathf.Max(0f, Time.unscaledTime - stateStartedAt);
            int step = Mathf.FloorToInt(elapsed * Mathf.Max(0.01f, jumpAnimation.fallLoopFps)) % (last - first + 1);
            var frame = jumpFallFrames[first + step];
            if (frame != null)
            {
                spriteRenderer.sprite = frame;
            }
        }

        // 설정에서 켠 경우에만 fall 1~15 그림 안의 출렁임을 반대로 옮긴다. 기본은 끔.
        private void ApplyJumpBob(float pixels)
        {
            if (spriteRenderer == null || (!jumpBobApplied && pixels == 0f))
            {
                return;
            }

            var visual = spriteRenderer.transform;
            if (!jumpBobApplied)
            {
                jumpBobBaseY = visual.localPosition.y;
            }

            var pixelsPerUnit = spriteRenderer.sprite != null ? spriteRenderer.sprite.pixelsPerUnit : 256f;
            var position = visual.localPosition;
            position.y = jumpBobBaseY - pixels / pixelsPerUnit * Mathf.Abs(visual.localScale.y);
            visual.localPosition = position;
            jumpBobApplied = pixels != 0f;
        }

        private static bool HasFrames(Sprite[] frames) => frames != null && frames.Length > 0;

        private void HandleHealthChanged(SubTerra.Shared.PlayerHealthReadModel health)
        {
            if (health.Current < previousHealth)
            {
                damageUntil = Time.unscaledTime + DamageDuration;
                // 사다리 채굴 중 맞으면 그 방향, 아니면 사다리 그림처럼 오른쪽을 본다.
                ladderDamageDirection = LadderMiningSequence.IsActive ? LadderMiningSequence.Direction : 1;
            }

            previousHealth = health.Current;
        }

        private void HandleFailureRequested(SubTerra.Shared.RunFailureInputDto dto)
        {
            damageUntil = 0f;
            knockoutCause = dto != null && dto.cause != SubTerra.Shared.RunFailureCause.Unknown
                ? dto.cause
                : survival != null && survival.State != null
                    ? survival.State.LastDamageCause : SubTerra.Shared.RunFailureCause.Unknown;
            MiningSequence.Reset();
            LadderMiningSequence.Reset();
            isMining = false;
            SetCurrentState("Knockout");
            stateStartedAt = Time.unscaledTime;
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
            if (HasJumpPhaseFrames && stateName == "Jump")
            {
                PlayJumpPhase();
                return;
            }

            if (HasJumpPhaseFrames && stateName == "LadderDrop")
            {
                PlayLadderDrop();
                return;
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

            if (stateName == "Mining")
                frameIndex = Mathf.Min(MiningSequence.FrameIndex, frames.Length - 1);
            else if (stateName == "LadderMining")
                frameIndex = Mathf.Min(LadderMiningSequence.FrameIndex, frames.Length - 1);
            else if (stateName == "Knockout" && frames.Length >= 48
                && elapsed >= 48f / Mathf.Max(0.01f, knockoutFps)
                && knockoutCause == SubTerra.Shared.RunFailureCause.GasExposure)
                frameIndex = 36 + Mathf.FloorToInt((elapsed - 48f / Mathf.Max(0.01f, knockoutFps))
                    * knockoutBreathingFps) % 12;

            if (frames[frameIndex] != null)
            {
                spriteRenderer.sprite = frames[frameIndex];
            }

            if (stateName == "Walk")
            {
                TryPlayFootstep(frameIndex);
            }
        }

        private void TryPlayFootstep(int frameIndex)
        {
            if (frameIndex == lastWalkFrame)
            {
                return;
            }

            lastWalkFrame = frameIndex;
            if (footstepFrames == null
                || System.Array.IndexOf(footstepFrames, frameIndex) < 0
                || !movement.IsGrounded
                || movement.IsClimbing
                || movement.HorizontalSpeed <= FootstepMinHorizontalSpeed
                || Time.timeScale <= 0f)
            {
                return;
            }

            // 같은 소리가 기계적으로 들리지 않게 피치를 번갈아 쓴다. 시설 바닥도 같은 소리다.
            SfxRequests.Play(SfxId.Footstep, footstepAlternate ? FootstepAlternatePitch : 1f);
            footstepAlternate = !footstepAlternate;
        }

        private (Sprite[] Frames, float FrameRate, bool Loop) ResolveFrames(string stateName)
        {
            return stateName switch
            {
                "Walk" => (walkFrames, WalkFrameRate, true),
                "Jump" or "LadderDrop" => (jumpFrames, 12f, false),
                "Mining" => (miningFrames, miningAnimation.workFps, false),
                "LadderMining" => (ladderMiningFrames, ladderMiningAnimation.workFps, false),
                "Damage" => (damageFrames, damageFps, false),
                "Knockout" => (knockoutFrames, knockoutFps, false),
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
            var sequenceLength = ladderFrames.Length >= 5 ? FiveFrameLadderSequence.Length : 4;
            var steps = Mathf.FloorToInt(Mathf.Abs(ladderTravelDistance) / distancePerFrame);
            if (steps > 0)
            {
                var direction = ladderTravelDistance > 0f ? 1 : -1;
                ladderSequenceIndex += direction * steps;
                ladderSequenceIndex = ((ladderSequenceIndex % sequenceLength) + sequenceLength)
                    % sequenceLength;
                ladderTravelDistance -= direction * steps * distancePerFrame;
                if (IsLadderTopIndex(ladderSequenceIndex, ladderFrames.Length >= 5))
                {
                    SfxRequests.Play(movement.LadderSfx.OnClimbStep());
                }
            }

            if (ladderFrames.Length >= 5)
            {
                return FiveFrameLadderSequence[ladderSequenceIndex];
            }

            // 기존 3프레임을 사용하는 테스트/프리팹도 안전하게 유지한다.
            return ladderSequenceIndex switch
            {
                1 => 1,
                3 => 2,
                _ => 0
            };
        }

        private static bool IsLadderTopIndex(int sequenceIndex, bool fiveFrames)
        {
            return fiveFrames
                ? sequenceIndex == FiveFrameLadderTopA || sequenceIndex == FiveFrameLadderTopB
                : sequenceIndex == 1 || sequenceIndex == 3;
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
                && nextStateName != "LadderMining"
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
            lastWalkFrame = -1;
        }

        private static bool IsLadderState(string stateName)
        {
            return stateName == "Ladder" || stateName == "LadderDown" || stateName == "LadderIdle";
        }
    }
}
