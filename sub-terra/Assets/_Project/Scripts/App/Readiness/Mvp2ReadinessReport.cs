using System.Collections.Generic;
using System.Text;

namespace SubTerra.App.Readiness
{
    public enum IntegrationFindingKind
    {
        MissingScript = 0,
        MissingReference = 1,
        DuplicateSystem = 2,
        PlaceholderRuntime = 3,
        RequiredStructure = 4,
        Info = 5
    }

    /// <summary>Integration/Prefab/카탈로그 감사의 단일 발견 항목.</summary>
    public sealed class IntegrationAuditFinding
    {
        public IntegrationFindingKind Kind { get; }
        public string AssetPath { get; }
        public string FieldName { get; }
        public string Message { get; }

        public IntegrationAuditFinding(
            IntegrationFindingKind kind,
            string assetPath,
            string fieldName,
            string message)
        {
            Kind = kind;
            AssetPath = assetPath ?? string.Empty;
            FieldName = fieldName ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public override string ToString()
        {
            var path = string.IsNullOrEmpty(AssetPath) ? "(unknown)" : AssetPath;
            var field = string.IsNullOrEmpty(FieldName) ? "-" : FieldName;
            return $"[{Kind}] {path} :: {field} — {Message}";
        }
    }

    /// <summary>MVP2 Phase A 기준선 보고서. 상태·감사·차단 단계를 한곳에 모은다.</summary>
    public sealed class Mvp2ReadinessReport
    {
        public string GeneratedAtUtc { get; set; } = string.Empty;
        public bool ReadOnly { get; set; } = true;
        public IReadOnlyList<ReadinessFeatureEntry> Features { get; set; } =
            new List<ReadinessFeatureEntry>();
        public IReadOnlyList<IntegrationAuditFinding> MissingScripts { get; set; } =
            new List<IntegrationAuditFinding>();
        public IReadOnlyList<IntegrationAuditFinding> MissingReferences { get; set; } =
            new List<IntegrationAuditFinding>();
        public IReadOnlyList<IntegrationAuditFinding> Duplicates { get; set; } =
            new List<IntegrationAuditFinding>();
        public IReadOnlyList<IntegrationAuditFinding> Placeholders { get; set; } =
            new List<IntegrationAuditFinding>();
        public IReadOnlyList<IntegrationAuditFinding> StructureFindings { get; set; } =
            new List<IntegrationAuditFinding>();
        public IReadOnlyList<string> BlockedStages { get; set; } = new List<string>();

        public string FormatText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("MVP2 Readiness Report");
            sb.AppendLine("GeneratedAtUtc: " + GeneratedAtUtc);
            sb.AppendLine("ReadOnly: " + ReadOnly);
            sb.AppendLine();
            sb.AppendLine("## Features");
            for (var i = 0; i < Features.Count; i++)
            {
                var f = Features[i];
                sb.AppendLine(
                    $"- {f.FeatureId} | {f.DisplayName} | overall={ReadinessStatusLabels.ToLabel(f.OverallStatus)} " +
                    $"| def={ReadinessStatusLabels.ToLabel(f.DefinitionStatus)} " +
                    $"| runtime={ReadinessStatusLabels.ToLabel(f.RuntimeStatus)} " +
                    $"| restore={ReadinessStatusLabels.ToLabel(f.RestoreStatus)} " +
                    $"| play={ReadinessStatusLabels.ToLabel(f.PlayStatus)} " +
                    $"| stage={f.OwningStage} | evidence={EvidenceKindLabels.Format(f.Evidence)}");
                if (!string.IsNullOrEmpty(f.EvidenceNotes))
                {
                    sb.AppendLine("  notes: " + f.EvidenceNotes);
                }
            }

            AppendSection(sb, "MissingScripts", MissingScripts);
            AppendSection(sb, "MissingReferences", MissingReferences);
            AppendSection(sb, "Duplicates", Duplicates);
            AppendSection(sb, "Placeholders", Placeholders);
            AppendSection(sb, "RequiredStructure", StructureFindings);

            sb.AppendLine();
            sb.AppendLine("## BlockedStages");
            if (BlockedStages.Count == 0)
            {
                sb.AppendLine("(none)");
            }
            else
            {
                for (var i = 0; i < BlockedStages.Count; i++)
                {
                    sb.AppendLine("- " + BlockedStages[i]);
                }
            }

            return sb.ToString();
        }

        /// <summary>경량 JSON(외부 의존성 없이) 직렬화.</summary>
        public string FormatJson()
        {
            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"generatedAtUtc\":\"").Append(Escape(GeneratedAtUtc)).Append("\",");
            sb.Append("\"readOnly\":").Append(ReadOnly ? "true" : "false").Append(',');
            sb.Append("\"features\":[");
            for (var i = 0; i < Features.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                var f = Features[i];
                sb.Append('{');
                sb.Append("\"id\":\"").Append(Escape(f.FeatureId)).Append("\",");
                sb.Append("\"name\":\"").Append(Escape(f.DisplayName)).Append("\",");
                sb.Append("\"stage\":\"").Append(Escape(f.OwningStage)).Append("\",");
                sb.Append("\"overall\":\"").Append(ReadinessStatusLabels.ToLabel(f.OverallStatus)).Append("\",");
                sb.Append("\"definition\":\"").Append(ReadinessStatusLabels.ToLabel(f.DefinitionStatus)).Append("\",");
                sb.Append("\"runtime\":\"").Append(ReadinessStatusLabels.ToLabel(f.RuntimeStatus)).Append("\",");
                sb.Append("\"restore\":\"").Append(ReadinessStatusLabels.ToLabel(f.RestoreStatus)).Append("\",");
                sb.Append("\"play\":\"").Append(ReadinessStatusLabels.ToLabel(f.PlayStatus)).Append("\",");
                sb.Append("\"evidence\":\"").Append(Escape(EvidenceKindLabels.Format(f.Evidence))).Append("\",");
                sb.Append("\"notes\":\"").Append(Escape(f.EvidenceNotes)).Append('"');
                sb.Append('}');
            }

            sb.Append("],");
            AppendJsonArray(sb, "missingScripts", MissingScripts);
            sb.Append(',');
            AppendJsonArray(sb, "missingReferences", MissingReferences);
            sb.Append(',');
            AppendJsonArray(sb, "duplicates", Duplicates);
            sb.Append(',');
            AppendJsonArray(sb, "placeholders", Placeholders);
            sb.Append(',');
            AppendJsonArray(sb, "requiredStructure", StructureFindings);
            sb.Append(',');
            sb.Append("\"blockedStages\":[");
            for (var i = 0; i < BlockedStages.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append('"').Append(Escape(BlockedStages[i])).Append('"');
            }

            sb.Append("]}");
            return sb.ToString();
        }

        public static Mvp2ReadinessReport Build(
            IReadOnlyList<ReadinessFeatureEntry> features,
            IReadOnlyList<IntegrationAuditFinding> findings,
            string generatedAtUtc)
        {
            var missingScripts = new List<IntegrationAuditFinding>();
            var missingRefs = new List<IntegrationAuditFinding>();
            var duplicates = new List<IntegrationAuditFinding>();
            var placeholders = new List<IntegrationAuditFinding>();
            var structure = new List<IntegrationAuditFinding>();
            if (findings != null)
            {
                for (var i = 0; i < findings.Count; i++)
                {
                    var f = findings[i];
                    if (f == null)
                    {
                        continue;
                    }

                    switch (f.Kind)
                    {
                        case IntegrationFindingKind.MissingScript:
                            missingScripts.Add(f);
                            break;
                        case IntegrationFindingKind.MissingReference:
                            missingRefs.Add(f);
                            break;
                        case IntegrationFindingKind.DuplicateSystem:
                            duplicates.Add(f);
                            break;
                        case IntegrationFindingKind.PlaceholderRuntime:
                            placeholders.Add(f);
                            break;
                        default:
                            structure.Add(f);
                            break;
                    }
                }
            }

            var blocked = new List<string>();
            var seen = new HashSet<string>();
            if (features != null)
            {
                for (var i = 0; i < features.Count; i++)
                {
                    var entry = features[i];
                    if (entry == null || entry.OverallStatus == ReadinessStatus.Complete)
                    {
                        continue;
                    }

                    var stages = entry.OwningStage.Split(',');
                    for (var s = 0; s < stages.Length; s++)
                    {
                        var stage = stages[s].Trim();
                        if (stage.Length == 0 || !seen.Add(stage))
                        {
                            continue;
                        }

                        blocked.Add(stage);
                    }
                }
            }

            blocked.Sort();
            return new Mvp2ReadinessReport
            {
                GeneratedAtUtc = generatedAtUtc ?? string.Empty,
                ReadOnly = true,
                Features = features ?? new List<ReadinessFeatureEntry>(),
                MissingScripts = missingScripts,
                MissingReferences = missingRefs,
                Duplicates = duplicates,
                Placeholders = placeholders,
                StructureFindings = structure,
                BlockedStages = blocked
            };
        }

        private static void AppendSection(
            StringBuilder sb,
            string title,
            IReadOnlyList<IntegrationAuditFinding> items)
        {
            sb.AppendLine();
            sb.AppendLine("## " + title);
            if (items == null || items.Count == 0)
            {
                sb.AppendLine("(empty)");
                return;
            }

            for (var i = 0; i < items.Count; i++)
            {
                sb.AppendLine("- " + items[i]);
            }
        }

        private static void AppendJsonArray(
            StringBuilder sb,
            string name,
            IReadOnlyList<IntegrationAuditFinding> items)
        {
            sb.Append('"').Append(name).Append("\":[");
            if (items != null)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    var item = items[i];
                    sb.Append('{');
                    sb.Append("\"kind\":\"").Append(item.Kind).Append("\",");
                    sb.Append("\"assetPath\":\"").Append(Escape(item.AssetPath)).Append("\",");
                    sb.Append("\"fieldName\":\"").Append(Escape(item.FieldName)).Append("\",");
                    sb.Append("\"message\":\"").Append(Escape(item.Message)).Append('"');
                    sb.Append('}');
                }
            }

            sb.Append(']');
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }
}
