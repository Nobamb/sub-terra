using System;
using System.Globalization;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>붕괴·가스·체력 피해를 Player 행동불능 입력으로 정규화한다.</summary>
    public sealed class PlayerSurvivalController :
        MonoBehaviour,
        IPlayerHealthSource,
        IPlayerHealthCommand,
        ICollapseDamageReceiver
    {
        private const float DamageFlashInterval = 0.08f;
        private const float DamageFlashAlphaMultiplier = 0.35f;

        [SerializeField] private PlayerSurvivalSettings settings;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerCameraFollow cameraFollow;

        private int tokenSequence;
        private IPlayerHealthUpgradeProvider healthUpgrades;
        private bool wasGrounded;
        private bool trackingFall;
        private bool usedLadderDuringFall;
        private float fallApexY;
        private SpriteRenderer[] flashRenderers = Array.Empty<SpriteRenderer>();
        private Color[] flashBaseColors = Array.Empty<Color>();
        private float damageFlashStartedAt;
        private float damageFlashUntil;
        private float cargoFallImpactMultiplier = 1f;

        public PlayerSurvivalState State { get; private set; }
        public event Action<PlayerSurvivalState> StateChanged;
        public event Action<PlayerHealthReadModel> HealthChanged;
        public event Action<RunFailureInputDto> FailureRequested;
        public float CurrentCargoFallImpactMultiplier => cargoFallImpactMultiplier;

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

            UpdateDamageFlash();
            TrackFall();
        }

        private void OnDisable()
        {
            RestoreDamageFlash();
        }

        public void BindTarget(Transform target)
        {
            RestoreDamageFlash();
            playerTarget = target;
            CacheFlashRenderers();
        }

        public void BindCameraFollow(PlayerCameraFollow follow)
        {
            cameraFollow = follow;
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

        public void SetCargoFallImpactMultiplier(float multiplier)
        {
            cargoFallImpactMultiplier = Mathf.Max(0f, multiplier);
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

        public bool IsCollapseContact(float fromX, float fromY, float toX, float toY)
        {
            if (settings == null || playerTarget == null)
            {
                return false;
            }

            return DistanceToSegment(
                    playerTarget.position,
                    new Vector2(fromX, fromY),
                    new Vector2(toX, toY))
                <= settings.CollapseHitRadius;
        }

        public bool ApplyCollapseImpact()
        {
            EnsureState();
            tokenSequence++;
            return ApplyDamage(
                RunFailureCause.StructuralCollapse,
                settings.MinorCollapseDamage,
                false,
                "collapse-impact:" + tokenSequence.ToString(CultureInfo.InvariantCulture),
                "structural_falling_rock");
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
            var damage = PlayerFallDamageRules.ScaleDamage(
                PlayerFallDamageRules.CalculateDamage(
                    fallDistance,
                    usedLadder,
                    settings.MinimumFallDamageHeight,
                    settings.FallDamageAtThreshold,
                    settings.FallDamagePerAdditionalMeter),
                cargoFallImpactMultiplier);
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
            // Prompt-B 81: 전력 0은 행동불능/RunFailure가 아니다.
            // 구버전 바인딩이 남아 호출해도 걷기·점프·사다리 상태를 바꾸지 않는다.
            return false;
        }

        public void RestoreAfterRescue()
        {
            EnsureState();
            RestoreDamageFlash();
            State.RestoreFull();
            PublishStateChanged();
            ResetFallTracking();
        }

        public bool RestoreFull()
        {
            EnsureState();
            RestoreDamageFlash();
            var changed = State.RestoreFull();
            if (changed)
            {
                PublishStateChanged();
            }

            return changed;
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
            float healthBeforeDamage = State.Health;
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

            StartDamageFeedback(healthBeforeDamage - State.Health);
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

        public static float ResolveDamageShakeAmplitude(float damage, int maximumHealth)
        {
            float ratio = Mathf.Clamp01(Mathf.Max(0f, damage) / Mathf.Max(1, maximumHealth));
            return Mathf.Lerp(0.12f, 0.5f, ratio);
        }

        private static float ResolveDamageShakeDuration(float damage, int maximumHealth)
        {
            float ratio = Mathf.Clamp01(Mathf.Max(0f, damage) / Mathf.Max(1, maximumHealth));
            return Mathf.Lerp(0.18f, 0.38f, ratio);
        }

        private void StartDamageFeedback(float appliedDamage)
        {
            if (appliedDamage <= 0f)
            {
                return;
            }

            PlayerCameraFollow follow = cameraFollow;
            if (follow == null)
            {
                Camera main = Camera.main;
                if (main != null)
                {
                    follow = main.GetComponent<PlayerCameraFollow>();
                }
            }

            if (follow != null)
            {
                follow.RequestShake(
                    ResolveDamageShakeAmplitude(appliedDamage, State.MaximumHealth),
                    ResolveDamageShakeDuration(appliedDamage, State.MaximumHealth));
            }

            RestoreDamageFlash();
            CacheFlashRenderers();
            damageFlashStartedAt = Time.unscaledTime;
            damageFlashUntil = damageFlashStartedAt + settings.InvulnerabilitySeconds;
            UpdateDamageFlash();
        }

        private void CacheFlashRenderers()
        {
            if (playerTarget == null)
            {
                flashRenderers = Array.Empty<SpriteRenderer>();
                flashBaseColors = Array.Empty<Color>();
                return;
            }

            flashRenderers = playerTarget.GetComponentsInChildren<SpriteRenderer>(true);
            flashBaseColors = new Color[flashRenderers.Length];
            for (var i = 0; i < flashRenderers.Length; i++)
            {
                flashBaseColors[i] = flashRenderers[i] != null
                    ? flashRenderers[i].color
                    : Color.white;
            }
        }

        private void UpdateDamageFlash()
        {
            if (damageFlashUntil <= 0f)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now >= damageFlashUntil || State == null || !State.IsInvulnerable(now))
            {
                RestoreDamageFlash();
                return;
            }

            bool dimmed = Mathf.FloorToInt((now - damageFlashStartedAt) / DamageFlashInterval) % 2 == 0;
            for (var i = 0; i < flashRenderers.Length; i++)
            {
                SpriteRenderer renderer = flashRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Color color = flashBaseColors[i];
                if (dimmed)
                {
                    color.a *= DamageFlashAlphaMultiplier;
                }

                renderer.color = color;
            }
        }

        private void RestoreDamageFlash()
        {
            int count = Mathf.Min(flashRenderers.Length, flashBaseColors.Length);
            for (var i = 0; i < count; i++)
            {
                if (flashRenderers[i] != null)
                {
                    flashRenderers[i].color = flashBaseColors[i];
                }
            }

            damageFlashStartedAt = 0f;
            damageFlashUntil = 0f;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float squaredLength = segment.sqrMagnitude;
            if (squaredLength <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, start);
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / squaredLength);
            return Vector2.Distance(point, start + segment * t);
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
            CacheFlashRenderers();
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
