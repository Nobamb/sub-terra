using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>B-210: 다침 그림 종류. 가스 피해·사다리 피격은 경직 시간 안에 압축해 끝까지 보인다.</summary>
    public enum PlayerDamageVisual
    {
        Damage,
        GasDamage,
        LadderHit
    }

    // SpriteRenderer를 직접 갱신한다. Animator 클립은 에셋 미리보기/확장용으로 보존하되,
    // 런타임 표시가 Animator 평가 순서에 의존하지 않도록 한다.
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        public const int WalkFrameCount = SubTerra.Shared.PlayerWalkCycle.FrameCount;
        public const float WalkFrameRate = SubTerra.Shared.PlayerWalkCycle.FrameRate;
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
        // 기절 뒤 가스 숨쉬기 반복이 시작하는 그림 번호(1부터). 끝은 실제 마지막 장이다.
        private const int KnockoutBreathingFirstFrame = 37;

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
        [Tooltip("B-210: 땅에서 가스 피해 경직 그림. 비어 있으면 damageFrames.")]
        [SerializeField] private Sprite[] gasDamageFrames;
        [Tooltip("B-210: 사다리를 잡은 채 다친 뒷모습. 비어 있으면 damageFrames.")]
        [SerializeField] private Sprite[] ladderHitFrames;
        [Tooltip("B-210: 사다리 오르기 한 바퀴(6장 이상)에서 발소리를 내는 그림 번호(1부터).")]
        [SerializeField] private int[] ladderStepFrames;
        [Tooltip("B-210: 그림 세트별 사다리 채굴 구간. 비어 있으면 ladderMiningAnimation 값.")]
        [SerializeField] private PlayerLadderMiningFrameRanges ladderMiningRanges;
        [SerializeField] private PlayerMiningAnimationSettings miningAnimation = new();
        [SerializeField] private PlayerLadderMiningAnimationSettings ladderMiningAnimation = new();
        [SerializeField] private PlayerJumpAnimationSettings jumpAnimation = new();
        [SerializeField, Min(0.01f)] private float damageFps = 20f;
        [SerializeField, Min(0.01f)] private float knockoutFps = 24f;
        [SerializeField, Min(0.01f)] private float knockoutBreathingFps = 12f;
        [Tooltip("옛 3·5장 사다리 순서에서 한 칸당 이동 거리.")]
        [SerializeField, Min(0.01f)] private float ladderDistancePerFrame = 0.5f;
        [Tooltip("B-210: 6장 이상 사다리 세트가 한 바퀴(1→N) 도는 이동 거리. 한 장 = 이 값 ÷ 장수.")]
        [SerializeField, Min(0.01f)] private float ladderCycleDistance = 2f;
        [SerializeField, Min(0f)] private float ladderSettleDelay = 0.08f;
        [Tooltip("B-164: 걷기 그림에서 발이 닿는 프레임. 이 프레임으로 넘어갈 때 발소리를 낸다.")]
        [SerializeField] private int[] footstepFrames = { 1, 21 };
        [Tooltip("B-208: 성별별 그림 묶음. 비어 있으면 위 프레임 배열을 그대로 쓴다.")]
        [SerializeField] private PlayerAnimationSet maleAnimationSet;
        [SerializeField] private PlayerAnimationSet femaleAnimationSet;

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
        private float damageVisualDuration;
        private bool damageIsHitStun;
        private PlayerDamageVisual damageVisual;
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
        private int observedJumpCutCount;
        private bool jumpBobApplied;
        private float jumpBobBaseY;

        public bool IsShowingLadderVisual => IsLadderState(currentState);
        /// <summary>B-208: 지금 그리는 그림 세트의 성별. 프리팹 기본 그림은 남자다.</summary>
        public PlayerGender Gender { get; private set; }
        public PlayerAnimationSet MaleAnimationSet => maleAnimationSet;
        public PlayerAnimationSet FemaleAnimationSet => femaleAnimationSet;
        /// <summary>B-175: 점프 그림 단계. #265 연출은 PhaseChanged를 구독한다.</summary>
        public PlayerJumpAnimationSequence JumpAnimation => JumpSequence;
        public bool IsShowingLadderMiningVisual => currentState == "LadderMining";
        public int LadderMiningDirection => LadderMiningSequence.Direction;
        /// <summary>B-176: 사다리를 잡은 채 다치는 중. 이동 방향 대신 LadderDamageDirection으로 고정한다.</summary>
        public bool IsShowingLadderDamageVisual => currentState == "Damage" && IsHoldingLadder;
        /// <summary>B-210: 지금(또는 마지막) 다침 그림 종류와 그 길이.</summary>
        public PlayerDamageVisual DamageVisual => damageVisual;
        public float DamageVisualDuration => damageVisualDuration;
        public bool IsShowingHitStunVisual => currentState == "Damage" && damageIsHitStun;
        /// <summary>B-211: 기절(행동불능) 그림 중. 찌그러짐 같은 몸 연출을 끈다.</summary>
        public bool IsShowingKnockoutVisual => wasIncapacitated;
        private float HitStunSeconds => survival != null ? survival.HitStunSeconds : PlayerSurvivalSettings.DefaultHitStunSeconds;
        private bool UsesLadderCycle => ladderFrames != null && ladderFrames.Length > 5;
        public int LadderDamageDirection => ladderDamageDirection;
        public bool CanMineOnLadder => movement != null && !movement.IsJumpInProgress
            && !movement.IsApproachingLadderFromAbove
            && (movement.IsClimbing || (movement.IsTouchingLadder
                && (IsShowingLadderVisual || IsShowingLadderMiningVisual || miningOnLadder)));
        private PlayerMiningAnimationSequence MiningSequence
        {
            get
            {
                if (miningSequence == null)
                {
                    miningSequence = new PlayerMiningAnimationSequence(miningAnimation);
                    miningSequence.SetFrameCount(Count(miningFrames));
                }

                return miningSequence;
            }
        }

        private PlayerLadderMiningAnimationSequence LadderMiningSequence
        {
            get
            {
                if (ladderMiningSequence == null)
                {
                    ladderMiningSequence = new PlayerLadderMiningAnimationSequence(ladderMiningAnimation);
                    ladderMiningSequence.SetFrameCount(Count(ladderMiningFrames));
                    ladderMiningSequence.SetFrameRanges(ladderMiningRanges);
                }

                return ladderMiningSequence;
            }
        }

        private PlayerJumpAnimationSequence JumpSequence
        {
            get
            {
                if (jumpSequence == null)
                {
                    jumpSequence = new PlayerJumpAnimationSequence(jumpAnimation);
                    jumpSequence.SetFrameCounts(Count(jumpReadyFrames), Count(jumpRiseFrames), Count(jumpFallFrames));
                }

                return jumpSequence;
            }
        }
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
                observedJumpCutCount = movement.JumpCutCount;
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

            SyncSequenceFrameCounts();
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
            SyncSequenceFrameCounts();
        }

        /// <summary>
        /// B-208: 성별 그림 세트로 바꾼다. 해당 세트가 연결되지 않았으면 false를 돌려주고 그림을 그대로 둔다.
        /// </summary>
        public bool SetGender(PlayerGender gender)
        {
            var set = gender == PlayerGender.Female ? femaleAnimationSet : maleAnimationSet;
            if (set == null)
            {
                return false;
            }

            ApplyAnimationSet(set);
            return true;
        }

        /// <summary>
        /// 그림 배열만 바꾼다. 상태·채굴/점프 진행·시간은 그대로 두고 다음 그리기부터 새 그림을 쓴다.
        /// 진행 중인 번호가 새 세트 장수를 넘으면 그리기 단계에서 마지막 장으로 자른다.
        /// </summary>
        public void ApplyAnimationSet(PlayerAnimationSet set)
        {
            if (set == null)
            {
                return;
            }

            idleFrames = set.idleFrames;
            walkFrames = set.walkFrames;
            jumpReadyFrames = set.jumpReadyFrames;
            jumpRiseFrames = set.jumpRiseFrames;
            jumpFallFrames = set.jumpFallFrames;
            ladderFrames = set.ladderFrames;
            miningFrames = set.miningFrames;
            ladderMiningFrames = set.ladderMiningFrames;
            damageFrames = set.damageFrames;
            knockoutFrames = set.knockoutFrames;
            gasDamageFrames = set.gasDamageFrames;
            ladderHitFrames = set.ladderHitFrames;
            ladderStepFrames = set.ladderStepFrames;
            ladderMiningRanges = set.ladderMiningRanges;
            Gender = set.gender;
            SyncSequenceFrameCounts();
        }

        private void SyncSequenceFrameCounts()
        {
            MiningSequence.SetFrameCount(Count(miningFrames));
            LadderMiningSequence.SetFrameCount(Count(ladderMiningFrames));
            LadderMiningSequence.SetFrameRanges(ladderMiningRanges);
            JumpSequence.SetFrameCounts(Count(jumpReadyFrames), Count(jumpRiseFrames), Count(jumpFallFrames));
        }

        private static int Count(Sprite[] frames) => frames != null ? frames.Length : 0;

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
            // B-211: 손을 떼 위 속도가 잘리면 상승 그림이 건너뛰지 않게 남은 상승을 다시 나눈다.
            bool cut = movement.JumpCutCount != observedJumpCutCount;
            observedJumpCutCount = movement.JumpCutCount;

            if (incapacitated || IsHoldingLadder)
            {
                JumpSequence.Reset();
                return;
            }

            if (started)
            {
                JumpSequence.StartJump(movement.LastJumpLaunchSpeed);
            }
            else if (cut)
            {
                JumpSequence.ContinueAfterSpeedCut(movement.VerticalSpeed);
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
                StartDamageVisual();
            }

            previousHealth = health.Current;
        }

        /// <summary>
        /// B-210: 가스·낙하·붕괴 경직이면 경직 끝까지 그 원인 그림(사다리를 잡고 있으면 원인과 관계없이 사다리 피격),
        /// 그 밖의 피해(산소 0 틱 등)는 지금처럼 damageFrames를 HitStunSeconds 동안 보인다.
        /// 경직 그림 중 다시 다쳐도 늘리거나 다시 시작하지 않는다.
        /// </summary>
        private void StartDamageVisual()
        {
            float now = Time.unscaledTime;
            if (damageIsHitStun && now < damageUntil)
            {
                return;
            }

            // 사다리 채굴 중 맞으면 그 방향, 아니면 사다리 그림처럼 오른쪽을 본다.
            ladderDamageDirection = LadderMiningSequence.IsActive ? LadderMiningSequence.Direction : 1;
            if (survival != null && survival.IsHitStunned)
            {
                damageIsHitStun = true;
                damageUntil = Mathf.Max(now + 0.01f, survival.HitStunEndsAt);
                damageVisualDuration = damageUntil - now;
                damageVisual = IsHoldingLadder ? PlayerDamageVisual.LadderHit
                    : survival.State != null && survival.State.LastDamageCause == SubTerra.Shared.RunFailureCause.GasExposure
                        ? PlayerDamageVisual.GasDamage
                        : PlayerDamageVisual.Damage;
                if (currentState == "Damage")
                {
                    stateStartedAt = now;
                }

                return;
            }

            damageIsHitStun = false;
            damageVisual = PlayerDamageVisual.Damage;
            damageVisualDuration = HitStunSeconds;
            damageUntil = now + damageVisualDuration;
        }

        private Sprite[] ResolveHitStunFrames() => damageVisual switch
        {
            PlayerDamageVisual.GasDamage when HasFrames(gasDamageFrames) => gasDamageFrames,
            PlayerDamageVisual.LadderHit when HasFrames(ladderHitFrames) => ladderHitFrames,
            _ => null
        };

        // 장 번호 = 경과 ÷ 길이 × 장수. 화면 프레임에 맞춰 건너뛰고 마지막 장에서 멈춘다.
        private bool TryPlayCompressedDamage()
        {
            var frames = ResolveHitStunFrames();
            if (frames == null)
            {
                return false;
            }

            float duration = Mathf.Max(0.01f, damageVisualDuration);
            float elapsed = Mathf.Clamp(duration - (damageUntil - Time.unscaledTime), 0f, duration);
            int index = Mathf.Clamp(Mathf.FloorToInt(elapsed / duration * frames.Length), 0, frames.Length - 1);
            if (frames[index] != null)
            {
                spriteRenderer.sprite = frames[index];
            }

            return true;
        }

        private void HandleFailureRequested(SubTerra.Shared.RunFailureInputDto dto)
        {
            damageUntil = 0f;
            damageIsHitStun = false;
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

            if (stateName == "Damage" && TryPlayCompressedDamage())
            {
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
            else if (stateName == "Knockout" && frames.Length > KnockoutBreathingFirstFrame
                && elapsed >= frames.Length / Mathf.Max(0.01f, knockoutFps)
                && knockoutCause == SubTerra.Shared.RunFailureCause.GasExposure)
                // 끝까지 재생한 뒤 37~마지막 장(남자 48, 여자 47)을 숨쉬기로 반복한다.
                frameIndex = KnockoutBreathingFirstFrame - 1
                    + Mathf.FloorToInt((elapsed - frames.Length / Mathf.Max(0.01f, knockoutFps))
                        * knockoutBreathingFps) % (frames.Length - KnockoutBreathingFirstFrame + 1);

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
            if (UsesLadderCycle)
            {
                return ResolveLadderCycleFrameIndex();
            }

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

        // B-210: 세트 전체를 한 바퀴로 쓴다. 올라가면 1→N, 내려가면 N→1. 발소리는 ladderStepFrames 장에 닿을 때.
        private int ResolveLadderCycleFrameIndex()
        {
            int count = ladderFrames.Length;
            float distancePerFrame = Mathf.Max(0.01f, ladderCycleDistance) / count;
            int steps = Mathf.FloorToInt(Mathf.Abs(ladderTravelDistance) / distancePerFrame);
            if (steps > 0)
            {
                int direction = ladderTravelDistance > 0f ? 1 : -1;
                ladderTravelDistance -= direction * steps * distancePerFrame;
                bool step = false;
                for (int i = 0; i < steps; i++)
                {
                    ladderSequenceIndex = ((ladderSequenceIndex + direction) % count + count) % count;
                    step |= IsLadderStepFrame(ladderSequenceIndex, count);
                }

                if (step)
                {
                    SfxRequests.Play(movement.LadderSfx.OnClimbStep());
                }
            }

            return ladderSequenceIndex;
        }

        private bool IsLadderStepFrame(int frameIndex, int count)
        {
            if (ladderStepFrames != null && ladderStepFrames.Length > 0)
            {
                return System.Array.IndexOf(ladderStepFrames, frameIndex + 1) >= 0;
            }

            // 발소리 장이 비어 있는 세트는 한 바퀴의 1/4·3/4 지점에서 낸다.
            return frameIndex == count / 4 || frameIndex == count * 3 / 4;
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
