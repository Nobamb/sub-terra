using System;
using System.Globalization;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>붕괴·가스·전력 고갈을 하나의 Player 행동불능 입력으로 정규화한다.</summary>
    public sealed class PlayerSurvivalController : MonoBehaviour, IPlayerHealthSource
    {
        [SerializeField] private PlayerSurvivalSettings settings;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private PlayerMovement playerMovement;

        private int tokenSequence;
        private IPlayerHealthUpgradeProvider healthUpgrades;
        private bool wasGrounded;
        private bool trackingFall;
        private bool usedLadderDuringFall;
        private float fallApexY;

        public PlayerSurvivalState State { get; private set; }
        public event Action<PlayerSurvivalState> StateChanged;
        public event Action<PlayerHealthReadModel> HealthChanged;
        public event Action<RunFailureInputDto> FailureRequested;

        private void Awake()
        {
            EnsureState();
            ResetFallTracking();
        }

        private void Update()
        {
            EnsureState();
            if (State.AdvanceRegeneration(Time.deltaTime))
            {
                PublishStateChanged();
            }

            TrackFall();
        }

        public void BindTarget(Transform target)
        {
            playerTarget = target;
        }

        public void BindMovement(PlayerMovement movement)
        {
            playerMovement = movement;
            ResetFallTracking();
        }

        public void BindUpgradeEffects(IPlayerHealthUpgradeProvider upgrades)
        {
            healthUpgrades = upgrades;
            EnsureState();
            if (State.ApplyUpgradeEffects(ResolveMaximumHealth(), ResolveRegeneration()))
            {
                PublishStateChanged();
            }
        }

        public void Configure(PlayerSurvivalSettings survivalSettings, Transform target)
        {
            settings = survivalSettings;
            playerTarget = target;
            State = null;
            EnsureState();
            ResetFallTracking();
        }

        public bool ApplyCollapse(StructuralCollapseEventDto collapse)
        {
            int hitCount = CountCollapseHits(collapse);
            if (hitCount == 0)
            {
                return false;
            }

            // 멀리서 함께 무너진 칸은 피해에 포함하지 않는다. 직접 맞은 2칸 이상도 50으로 제한한다.
            int damage = hitCount == 1
                ? settings.MinorCollapseDamage
                : settings.MajorCollapseDamage;
            var token = "collapse:" + collapse.worldSeed.ToString(CultureInfo.InvariantCulture)
                + ":" + CollapseCellKey(collapse);
            return ApplyDamage(
                RunFailureCause.StructuralCollapse,
                damage,
                false,
                token,
                "structural_collapse");
        }

        public bool ApplyGasFailure(GasExposureFailureInputDto input)
        {
            if (input == null)
            {
                return false;
            }

            var token = "gas:" + (input.gasZoneId ?? string.Empty)
                + ":" + input.cumulativeExposureSeconds.ToString("0.###", CultureInfo.InvariantCulture);
            return ApplyDamage(
                RunFailureCause.GasExposure,
                input.severity == GasExposureFailureSeverity.Damage
                    ? Math.Max(1, input.damage)
                    : 0,
                input.severity == GasExposureFailureSeverity.RescueRequired,
                token,
                input.gasZoneId);
        }

        public bool ApplyFall(float fallDistance, bool usedLadder = false)
        {
            EnsureState();
            var damage = PlayerFallDamageRules.CalculateDamage(
                fallDistance,
                usedLadder,
                settings.MinimumFallDamageHeight,
                settings.FallDamageAtThreshold,
                settings.FallDamagePerAdditionalMeter);
            if (damage <= 0)
            {
                return false;
            }

            tokenSequence++;
            return ApplyDamage(
                RunFailureCause.Fall,
                damage,
                false,
                "fall:" + tokenSequence.ToString(CultureInfo.InvariantCulture),
                "fall_height");
        }

        public bool ApplyPowerDepletion()
        {
            tokenSequence++;
            return ApplyDamage(
                RunFailureCause.PowerDepleted,
                0,
                true,
                "power:" + tokenSequence.ToString(CultureInfo.InvariantCulture),
                "player_energy");
        }

        public void RestoreAfterRescue()
        {
            EnsureState();
            State.RestoreFull();
            PublishStateChanged();
            ResetFallTracking();
        }

        public PlayerHealthReadModel GetHealth()
        {
            EnsureState();
            return new PlayerHealthReadModel(State.Health, State.MaximumHealth);
        }

        private bool ApplyDamage(
            RunFailureCause cause,
            int damage,
            bool forceIncapacitate,
            string failureToken,
            string sourceId)
        {
            EnsureState();
            if (!State.TryApplyDamage(
                    cause,
                    damage,
                    Time.unscaledTime,
                    settings.InvulnerabilitySeconds,
                    forceIncapacitate,
                    out var becameIncapacitated))
            {
                return false;
            }

            PublishStateChanged();
            if (becameIncapacitated)
            {
                FailureRequested?.Invoke(new RunFailureInputDto
                {
                    failureToken = failureToken,
                    cause = cause,
                    sourceId = sourceId ?? string.Empty,
                    damage = damage,
                    remainingHealth = Mathf.CeilToInt(State.Health),
                    returnToElevator = cause != RunFailureCause.PowerDepleted
                });
            }

            return true;
        }

        private int CountCollapseHits(StructuralCollapseEventDto collapse)
        {
            if (settings == null
                || playerTarget == null
                || collapse == null
                || collapse.cells == null
                || collapse.cells.Count == 0)
            {
                return 0;
            }

            var position = (Vector2)playerTarget.position;
            var radius = settings.CollapseHitRadius;
            int hitCount = 0;
            for (var i = 0; i < collapse.cells.Count; i++)
            {
                var center = new Vector2(collapse.cells[i].x + 0.5f, collapse.cells[i].y + 0.5f);
                if (Vector2.Distance(position, center) <= radius)
                {
                    hitCount++;
                }
            }

            return hitCount;
        }

        private static string CollapseCellKey(StructuralCollapseEventDto collapse)
        {
            if (collapse.cells == null || collapse.cells.Count == 0)
            {
                return "none";
            }

            var first = collapse.cells[0];
            return first.x.ToString(CultureInfo.InvariantCulture)
                + "," + first.y.ToString(CultureInfo.InvariantCulture)
                + ":" + collapse.severity;
        }

        private void EnsureState()
        {
            if (State != null)
            {
                return;
            }

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            }

            State = new PlayerSurvivalState(ResolveMaximumHealth(), ResolveRegeneration());
        }

        private int ResolveMaximumHealth()
        {
            var baseMaximum = settings != null ? settings.MaximumHealth : 100;
            return healthUpgrades != null
                ? healthUpgrades.GetMaximumHealth(baseMaximum)
                : baseMaximum;
        }

        private float ResolveRegeneration()
        {
            return healthUpgrades != null
                ? healthUpgrades.GetHealthRegenerationPerSecond()
                : 0f;
        }

        private void PublishStateChanged()
        {
            StateChanged?.Invoke(State);
            HealthChanged?.Invoke(GetHealth());
        }

        private void TrackFall()
        {
            if (playerMovement == null || playerTarget == null || State == null || !State.CanAct)
            {
                return;
            }

            var grounded = playerMovement.IsGrounded;
            var descendingLadder = playerMovement.IsDescendingLadder;
            var currentY = playerTarget.position.y;
            if (!grounded)
            {
                if (!trackingFall && wasGrounded)
                {
                    trackingFall = true;
                    fallApexY = currentY;
                    usedLadderDuringFall = descendingLadder;
                }

                if (trackingFall)
                {
                    fallApexY = Mathf.Max(fallApexY, currentY);
                    usedLadderDuringFall |= descendingLadder;
                }
            }
            else if (trackingFall && !wasGrounded)
            {
                var distance = Mathf.Max(0f, fallApexY - currentY);
                var ladderExempt = usedLadderDuringFall;
                trackingFall = false;
                usedLadderDuringFall = false;
                ApplyFall(distance, ladderExempt);
            }

            wasGrounded = grounded;
        }

        private void ResetFallTracking()
        {
            trackingFall = false;
            usedLadderDuringFall = false;
            fallApexY = playerTarget != null ? playerTarget.position.y : 0f;
            wasGrounded = playerMovement != null && playerMovement.IsGrounded;
        }
    }
}
