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

        public float UnsupportedTileWeight => unsupportedTileWeight;
        public float CautionThreshold => cautionThreshold;
        public float DangerThreshold => dangerThreshold;
        public float CollapseImminentThreshold => collapseImminentThreshold;

        private void OnValidate()
        {
            dangerThreshold = Mathf.Max(cautionThreshold, dangerThreshold);
            collapseImminentThreshold = Mathf.Max(dangerThreshold, collapseImminentThreshold);
        }
    }
}
