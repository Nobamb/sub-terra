using System;
using System.Globalization;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>붕괴·가스·전력 고갈을 하나의 Player 행동불능 입력으로 정규화한다.</summary>
    public sealed class PlayerSurvivalController : MonoBehaviour
    {
        [SerializeField] private PlayerSurvivalSettings settings;
        [SerializeField] private Transform playerTarget;

        private int tokenSequence;

        public PlayerSurvivalState State { get; private set; }
        public event Action<PlayerSurvivalState> StateChanged;
        public event Action<RunFailureInputDto> FailureRequested;

        private void Awake()
        {
            EnsureState();
        }

        public void BindTarget(Transform target)
        {
            playerTarget = target;
        }

        public void Configure(PlayerSurvivalSettings survivalSettings, Transform target)
        {
            settings = survivalSettings;
            playerTarget = target;
            State = null;
            EnsureState();
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
            if (input == null || input.severity != GasExposureFailureSeverity.RescueRequired)
            {
                return false;
            }

            var token = "gas:" + (input.gasZoneId ?? string.Empty)
                + ":" + input.cumulativeExposureSeconds.ToString("0.###", CultureInfo.InvariantCulture);
            return ApplyDamage(
                RunFailureCause.GasExposure,
                0,
                true,
                token,
                input.gasZoneId);
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
            StateChanged?.Invoke(State);
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

            StateChanged?.Invoke(State);
            if (becameIncapacitated)
            {
                FailureRequested?.Invoke(new RunFailureInputDto
                {
                    failureToken = failureToken,
                    cause = cause,
                    sourceId = sourceId ?? string.Empty,
                    damage = damage,
                    remainingHealth = State.Health
                });
            }

            return true;
        }

        private int CountCollapseHits(StructuralCollapseEventDto collapse)
        {
            if (settings == null
                || playerTarget == null
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

            State = new PlayerSurvivalState(settings.MaximumHealth);
        }
    }
}
