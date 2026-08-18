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
        [SerializeField, Min(0)] private int minorCollapseDamage = 25;
        [SerializeField, Min(0)] private int majorCollapseDamage = 50;
        [SerializeField, Min(0)] private int severeCollapseDamage = 100;
        [SerializeField, Min(0f)] private float collapseHitRadius = 1.25f;
        [SerializeField, Min(0f)] private float minimumFallDamageHeight = 10f;
        [SerializeField, Min(0)] private int fallDamageAtThreshold = 10;
        [SerializeField, Min(0f)] private float fallDamagePerAdditionalMeter = 1f;

        public int MaximumHealth => maximumHealth;
        public float InvulnerabilitySeconds => invulnerabilitySeconds;
        public int MinorCollapseDamage => minorCollapseDamage;
        public int MajorCollapseDamage => majorCollapseDamage;
        public int SevereCollapseDamage => severeCollapseDamage;
        public float CollapseHitRadius => collapseHitRadius;
        public float MinimumFallDamageHeight => minimumFallDamageHeight;
        public int FallDamageAtThreshold => fallDamageAtThreshold;
        public float FallDamagePerAdditionalMeter => fallDamagePerAdditionalMeter;
    }
}
