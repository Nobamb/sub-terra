using UnityEngine;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>구조 점수와 단계 임계치의 단일 데이터 원천.</summary>
    [CreateAssetMenu(fileName = "StructuralRiskSettings", menuName = "SubTerra/Structural/Risk Settings")]
    public sealed class StructuralRiskSettings : ScriptableObject
    {
        [SerializeField, Min(0f)] private float unsupportedTileWeight = 20f;
        [SerializeField, Min(0f)] private float cautionThreshold = 30f;
        [SerializeField, Min(0f)] private float dangerThreshold = 60f;
        [SerializeField, Min(0f)] private float collapseImminentThreshold = 90f;
        [SerializeField, Range(0.45f, 0.7f)] private float collapseTelegraphSeconds = 0.6f;
        [SerializeField, Min(0.05f)] private float collapseFallSeconds = 0.18f;
        [SerializeField, Range(0f, 1f)] private float cautionCollapseChancePerMining = 0.15f;
        [SerializeField, Range(0f, 1f)] private float dangerCollapseChancePerMining = 0.30f;
        [SerializeField, Min(0)] private int probabilisticRollRadiusCells = 2;
        [SerializeField, Min(0.01f)] private float probabilisticTelegraphSeconds = 1f;
        [SerializeField, Range(0f, 1f)] private float probabilisticCollapseDamageMultiplier = 0.5f;
        [SerializeField, Min(1)] private int maxSimultaneousProbabilistic = 2;
        [SerializeField, Min(0f)] private float probabilisticGlobalCooldownSeconds = 1.5f;
        [SerializeField, Min(0.01f)] private float probabilisticActiveSeconds = 30f;
        [SerializeField, Min(0)] private int offscreenMarginCells = 3;

        public float UnsupportedTileWeight => unsupportedTileWeight;
        public float CautionThreshold => cautionThreshold;
        public float DangerThreshold => dangerThreshold;
        public float CollapseImminentThreshold => collapseImminentThreshold;
        public float CollapseTelegraphSeconds => collapseTelegraphSeconds;
        public float CollapseFallSeconds => collapseFallSeconds;
        public float CautionCollapseChancePerMining => cautionCollapseChancePerMining;
        public float DangerCollapseChancePerMining => dangerCollapseChancePerMining;
        public int ProbabilisticRollRadiusCells => probabilisticRollRadiusCells;
        public float ProbabilisticTelegraphSeconds => probabilisticTelegraphSeconds;
        public float ProbabilisticCollapseDamageMultiplier => probabilisticCollapseDamageMultiplier;
        public int MaxSimultaneousProbabilistic => maxSimultaneousProbabilistic;
        public float ProbabilisticGlobalCooldownSeconds => probabilisticGlobalCooldownSeconds;
        public float ProbabilisticActiveSeconds => probabilisticActiveSeconds;
        public int OffscreenMarginCells => offscreenMarginCells;

        private void OnValidate()
        {
            dangerThreshold = Mathf.Max(cautionThreshold, dangerThreshold);
            collapseImminentThreshold = Mathf.Max(dangerThreshold, collapseImminentThreshold);
        }
    }
}
