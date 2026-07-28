using UnityEngine;

namespace SubTerra.App.Drone
{
    /// <summary>드론 임계값과 점수를 코드 수정 없이 조정하는 정적 설정.</summary>
    [CreateAssetMenu(
        fileName = "DroneAnalysisSettings",
        menuName = "SubTerra/Drone/Analysis Settings",
        order = 60)]
    public sealed class DroneAnalysisSettings : ScriptableObject
    {
        [Header("Thresholds")]
        [SerializeField, Range(0f, 1f)] private float structuralWarningThreshold = 0.5f;
        [SerializeField, Range(0f, 1f)] private float structuralCriticalThreshold = 0.2f;
        [SerializeField, Range(0f, 1f)] private float gasWarningThreshold = 0.4f;
        [SerializeField, Range(0f, 1f)] private float gasCriticalThreshold = 0.8f;
        [SerializeField, Min(0)] private int energyReserve = 5;
        [SerializeField, Min(0)] private long highCargoValueThreshold = 100;
        [SerializeField, Min(0f)] private float nearbyBaseDistance = 12f;
        [SerializeField, Min(0)] private int outpostMinimumDepth = 25;
        [SerializeField, Min(0f)] private float outpostDistance = 40f;

        [Header("Scores")]
        [SerializeField] private int lowEnergyReturnScore = 40;
        [SerializeField] private int cargoReturnScore = 20;
        [SerializeField] private int supportScore = 30;
        [SerializeField] private int gasExitScore = 50;
        [SerializeField] private int lithiumScore = 20;
        [SerializeField] private int rechargeScore = 15;
        [SerializeField] private int outpostScore = 15;
        [SerializeField] private int descendScore = 5;
        [SerializeField] private int criticalRiskBonus = 50;

        [Header("Dialogue")]
        [SerializeField, Min(0f)] private float regularDialogueCooldownSeconds = 10f;
        [SerializeField, Min(0f)] private float urgentDialogueRepeatSeconds = 3f;

        public float StructuralWarningThreshold => structuralWarningThreshold;
        public float StructuralCriticalThreshold => structuralCriticalThreshold;
        public float GasWarningThreshold => gasWarningThreshold;
        public float GasCriticalThreshold => gasCriticalThreshold;
        public int EnergyReserve => energyReserve;
        public long HighCargoValueThreshold => highCargoValueThreshold;
        public float NearbyBaseDistance => nearbyBaseDistance;
        public int OutpostMinimumDepth => outpostMinimumDepth;
        public float OutpostDistance => outpostDistance;
        public int LowEnergyReturnScore => lowEnergyReturnScore;
        public int CargoReturnScore => cargoReturnScore;
        public int SupportScore => supportScore;
        public int GasExitScore => gasExitScore;
        public int LithiumScore => lithiumScore;
        public int RechargeScore => rechargeScore;
        public int OutpostScore => outpostScore;
        public int DescendScore => descendScore;
        public int CriticalRiskBonus => criticalRiskBonus;
        public float RegularDialogueCooldownSeconds => regularDialogueCooldownSeconds;
        public float UrgentDialogueRepeatSeconds => urgentDialogueRepeatSeconds;

#if UNITY_EDITOR
        public void EditorSetDefaults()
        {
            structuralWarningThreshold = 0.5f;
            structuralCriticalThreshold = 0.2f;
            gasWarningThreshold = 0.4f;
            gasCriticalThreshold = 0.8f;
            energyReserve = 5;
            highCargoValueThreshold = 100;
            nearbyBaseDistance = 12f;
            outpostMinimumDepth = 25;
            outpostDistance = 40f;
            lowEnergyReturnScore = 40;
            cargoReturnScore = 20;
            supportScore = 30;
            gasExitScore = 50;
            lithiumScore = 20;
            rechargeScore = 15;
            outpostScore = 15;
            descendScore = 5;
            criticalRiskBonus = 50;
            regularDialogueCooldownSeconds = 10f;
            urgentDialogueRepeatSeconds = 3f;
        }
#endif
    }
}
