using System.Collections.Generic;

namespace SubTerra.App.Readiness
{
    /// <summary>PRD 필수 기능 한 줄의 기계 판독 가능 기준선 항목.</summary>
    public sealed class ReadinessFeatureEntry
    {
        public string FeatureId { get; }
        public string DisplayName { get; }
        public string OwningStage { get; }
        public bool RequiresRestore { get; }
        public EvidenceKind Evidence { get; }
        public string EvidenceNotes { get; }
        public ReadinessStatus OverallStatus { get; }
        public ReadinessStatus DefinitionStatus { get; }
        public ReadinessStatus RuntimeStatus { get; }
        public ReadinessStatus RestoreStatus { get; }
        public ReadinessStatus PlayStatus { get; }

        public ReadinessFeatureEntry(
            string featureId,
            string displayName,
            string owningStage,
            bool requiresRestore,
            EvidenceKind evidence,
            string evidenceNotes)
        {
            FeatureId = featureId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            OwningStage = owningStage ?? string.Empty;
            RequiresRestore = requiresRestore;
            Evidence = evidence;
            EvidenceNotes = evidenceNotes ?? string.Empty;
            OverallStatus = ReadinessStatusRules.EvaluateOverall(evidence, requiresRestore);
            DefinitionStatus = ReadinessStatusRules.EvaluateGate(ReadinessGateLevel.Definition, evidence);
            RuntimeStatus = ReadinessStatusRules.EvaluateGate(ReadinessGateLevel.Runtime, evidence);
            RestoreStatus = RequiresRestore
                ? ReadinessStatusRules.EvaluateGate(ReadinessGateLevel.Restore, evidence)
                : ReadinessStatus.Complete;
            PlayStatus = ReadinessStatusRules.EvaluateGate(ReadinessGateLevel.Play, evidence);
        }

        public IReadOnlyList<string> StatusLabels()
        {
            return new[]
            {
                ReadinessStatusLabels.ToLabel(OverallStatus),
                ReadinessStatusLabels.ToLabel(DefinitionStatus),
                ReadinessStatusLabels.ToLabel(RuntimeStatus),
                ReadinessStatusLabels.ToLabel(RestoreStatus),
                ReadinessStatusLabels.ToLabel(PlayStatus)
            };
        }
    }
}
