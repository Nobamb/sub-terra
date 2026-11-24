using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>Player 체력, 무적 시간과 위험별 피해를 편집 가능한 데이터로 보관한다.</summary>
    [CreateAssetMenu(
        fileName = "PlayerSurvivalSettings",
        menuName = "SubTerra/Player/Survival Settings")]
    public sealed class PlayerSurvivalSettings : ScriptableObject
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;
        [SerializeField, Min(0f)] private float invulnerabilitySeconds = 0.75f;
        [SerializeField, Min(0f), Tooltip("B-210: 가스·낙하·붕괴 피해 뒤 조작을 막는 시간. 무적 시간보다 짧아야 영구 경직이 생기지 않는다.")]
        private float hitStunSeconds = DefaultHitStunSeconds;
        [SerializeField, Min(0)] private int minorCollapseDamage = 25;
        [SerializeField, Min(0)] private int majorCollapseDamage = 50;
        [SerializeField, Min(0)] private int severeCollapseDamage = 100;
        [SerializeField, Min(0f)] private float collapseHitRadius = 1.25f;
        [SerializeField, Min(0f)] private float minimumFallDamageHeight = 10f;
        [SerializeField, Min(0)] private int fallDamageAtThreshold = 10;
        [SerializeField, Min(0f)] private float fallDamagePerAdditionalMeter = 1f;
        [SerializeField, Range(0f, 1f), Tooltip("완충판 위 착지 피해 = 같은 낙하·같은 짐의 일반 피해 × 이 비율. 0이면 피해 없음.")]
        private float cushionLandingDamageRatio = SubTerra.Shared.CushionPadRules.DefaultLandingDamageRatio;
        [SerializeField, Min(1f)] private float maximumOxygen = 100f;
        [SerializeField, Min(0f)] private float oxygenPerSecond = 0.5f;
        [SerializeField, Min(0f)] private float oxygenPerSecondPerMeter = 0.02f;
        [SerializeField, Min(0f), Tooltip("0이면 기본 산소 소모율을 얕은 구간 회복률로 사용합니다.")]
        private float shallowOxygenRecoveryPerSecond;
        [SerializeField, Min(1f)] private float gasOxygenDrainMultiplier = 2f;
        [SerializeField, Min(0.1f)] private float oxygenDamageInterval = 1f;
        [SerializeField, Min(0f)] private float oxygenDepletionDamage = 5f;
        [SerializeField, Range(0f, 1f)] private float oxygenCautionRatio = 0.35f;
        [SerializeField, Range(0f, 1f)] private float oxygenCriticalRatio = 0.15f;
        [SerializeField, Min(1f)] private float oxygenMovementMultiplier = 1.5f;
        [SerializeField, Min(1f)] private float oxygenLadderMultiplier = 1.5f;
        [SerializeField, Min(1f)] private float oxygenFullCargoMultiplier = 1.5f;
        [SerializeField, Min(1f)] private float oxygenLadderCargoCap = 1.2f;
        [SerializeField, Min(0.1f)] private float expeditionLadderSpeedMultiplier = 1.5f;

        public float MaximumOxygen => maximumOxygen;
        public float OxygenPerSecond => oxygenPerSecond;
        public float OxygenPerSecondPerMeter => oxygenPerSecondPerMeter;
        public float ShallowOxygenRecoveryPerSecond =>
            shallowOxygenRecoveryPerSecond > 0f ? shallowOxygenRecoveryPerSecond : oxygenPerSecond;
        public float GasOxygenDrainMultiplier => Mathf.Max(1f, gasOxygenDrainMultiplier);
        public float OxygenDamageInterval => oxygenDamageInterval;
        public float OxygenDepletionDamage => oxygenDepletionDamage;
        public float OxygenCautionRatio => oxygenCautionRatio;
        public float OxygenCriticalRatio => oxygenCriticalRatio;
        public float OxygenMovementMultiplier => oxygenMovementMultiplier;
        public float OxygenLadderMultiplier => oxygenLadderMultiplier;
        public float OxygenFullCargoMultiplier => oxygenFullCargoMultiplier;
        public float OxygenLadderCargoCap => oxygenLadderCargoCap;
        public float ExpeditionLadderSpeedMultiplier => expeditionLadderSpeedMultiplier;

        public int MaximumHealth => maximumHealth;
        public float InvulnerabilitySeconds => invulnerabilitySeconds;
        public const float DefaultHitStunSeconds = 0.4f;
        public float HitStunSeconds => hitStunSeconds;

        /// <summary>경직이 무적 시간 이상이면 피해가 끊기지 않고 이어져 계속 굳을 수 있다.</summary>
        public static bool IsHitStunLongerThanInvulnerability(float hitStun, float invulnerability) =>
            hitStun > 0f && hitStun >= invulnerability;

        private void OnValidate()
        {
            if (IsHitStunLongerThanInvulnerability(hitStunSeconds, invulnerabilitySeconds))
            {
                Debug.LogWarning("[PlayerSurvivalSettings] hitStunSeconds(" + hitStunSeconds
                    + ")가 invulnerabilitySeconds(" + invulnerabilitySeconds + ") 이상입니다. 경직이 계속 이어질 수 있습니다.", this);
            }
        }
        public int MinorCollapseDamage => minorCollapseDamage;
        public int MajorCollapseDamage => majorCollapseDamage;
        public int SevereCollapseDamage => severeCollapseDamage;
        public float CollapseHitRadius => collapseHitRadius;
        public float MinimumFallDamageHeight => minimumFallDamageHeight;
        public int FallDamageAtThreshold => fallDamageAtThreshold;
        public float FallDamagePerAdditionalMeter => fallDamagePerAdditionalMeter;
        public float CushionLandingDamageRatio => cushionLandingDamageRatio;
    }
}
