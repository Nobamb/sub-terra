using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Readiness;

namespace SubTerra.App.Tests.Readiness
{
    /// <summary>A-S01 PRD 추적성, A-F03 대역/Runtime 분리, 상태 라벨 규칙.</summary>
    public sealed class Mvp2ReadinessStatusTests
    {
        [Test]
        public void A_S01_PrdEssentialFeatures_AreMappedWithZeroGaps()
        {
            var features = Mvp2EssentialFeatureMatrix.CreateBaselineEntries();
            Assert.That(features, Is.Not.Null);
            Assert.That(features.Count, Is.GreaterThanOrEqualTo(17));

            var ids = new HashSet<string>();
            for (var i = 0; i < features.Count; i++)
            {
                var entry = features[i];
                Assert.That(entry.FeatureId, Is.Not.Null.And.Not.Empty, "Feature id missing at " + i);
                Assert.That(ids.Add(entry.FeatureId), Is.True, "Duplicate feature id: " + entry.FeatureId);
                Assert.That(entry.DisplayName, Is.Not.Empty);
                Assert.That(entry.OwningStage, Is.Not.Empty);
                Assert.That(
                    ReadinessStatusLabels.IsAllowedLabel(ReadinessStatusLabels.ToLabel(entry.OverallStatus)),
                    Is.True);
            }

            var prdConditions = Mvp2EssentialFeatureMatrix.RequiredPrdCompletionConditionIds();
            var stages = Mvp2EssentialFeatureMatrix.PrdCompletionConditionStages();
            Assert.That(prdConditions.Count, Is.EqualTo(16));
            for (var i = 0; i < prdConditions.Count; i++)
            {
                Assert.That(
                    stages.ContainsKey(prdConditions[i]),
                    Is.True,
                    "PRD completion condition missing stage mapping: " + prdConditions[i]);
                Assert.That(stages[prdConditions[i]], Is.Not.Empty);
            }
        }

        [Test]
        public void A_S01_BaselineStatuses_UseOnlyAllowedLabels_AndIncompleteRowsExist()
        {
            var features = Mvp2EssentialFeatureMatrix.CreateBaselineEntries();
            var incomplete = 0;
            for (var i = 0; i < features.Count; i++)
            {
                var labels = features[i].StatusLabels();
                for (var l = 0; l < labels.Count; l++)
                {
                    Assert.That(
                        ReadinessStatusLabels.IsAllowedLabel(labels[l]),
                        Is.True,
                        features[i].FeatureId + " invalid label " + labels[l]);
                }

                if (features[i].OverallStatus != ReadinessStatus.Complete)
                {
                    incomplete++;
                }
            }

            Assert.That(incomplete, Is.GreaterThan(0), "At least one incomplete PRD row is expected in Phase A");
        }

        [Test]
        public void A_F03_SurrogateOnly_IsNeverRuntimeOrPlayComplete()
        {
            var evidence = EvidenceKind.Definition | EvidenceKind.SurrogateTest;
            var runtime = ReadinessStatusRules.EvaluateGate(ReadinessGateLevel.Runtime, evidence);
            var play = ReadinessStatusRules.EvaluateGate(ReadinessGateLevel.Play, evidence);
            var overall = ReadinessStatusRules.EvaluateOverall(evidence, requiresRestore: true);

            Assert.That(runtime, Is.EqualTo(ReadinessStatus.Partial));
            Assert.That(play, Is.EqualTo(ReadinessStatus.Partial));
            Assert.That(overall, Is.Not.EqualTo(ReadinessStatus.Complete));
            Assert.That(
                ReadinessStatusRules.IsInvalidSurrogatePromotion(evidence, ReadinessStatus.Complete),
                Is.True);

            var entry = new ReadinessFeatureEntry(
                "cargo-speed-like",
                "surrogate only fixture",
                "E",
                requiresRestore: false,
                evidence,
                "unit only");
            Assert.That(entry.RuntimeStatus, Is.EqualTo(ReadinessStatus.Partial));
            Assert.That(entry.PlayStatus, Is.EqualTo(ReadinessStatus.Partial));
            Assert.That(entry.OverallStatus, Is.EqualTo(ReadinessStatus.Partial));
        }

        [Test]
        public void A_F03_RealRuntimeAndPlay_CanBeComplete()
        {
            var evidence = EvidenceKind.Definition
                | EvidenceKind.RuntimePrefab
                | EvidenceKind.Play
                | EvidenceKind.Restore;
            Assert.That(
                ReadinessStatusRules.EvaluateOverall(evidence, requiresRestore: true),
                Is.EqualTo(ReadinessStatus.Complete));
            Assert.That(
                ReadinessStatusRules.EvaluateGate(ReadinessGateLevel.Runtime, evidence),
                Is.EqualTo(ReadinessStatus.Complete));
            Assert.That(
                ReadinessStatusRules.EvaluateGate(ReadinessGateLevel.Play, evidence),
                Is.EqualTo(ReadinessStatus.Complete));
        }

        [Test]
        public void A_S04_PlaceholderClassifier_LabelsPlaceholderAndReal()
        {
            Assert.That(
                PlaceholderRuntimeClassifier.IsPlaceholder("BuildingPlaceholder", "Assets/x/BuildingPlaceholder.prefab"),
                Is.True);
            Assert.That(
                PlaceholderRuntimeClassifier.ClassifyLabel("BuildingPlaceholder", "Assets/x/BuildingPlaceholder.prefab", false),
                Is.EqualTo("placeholder"));
            Assert.That(
                PlaceholderRuntimeClassifier.ClassifyLabel("SupportPillar", "Assets/_Project/Prefabs/Gameplay/Buildings/SupportPillar.prefab", false),
                Is.EqualTo("real"));
            Assert.That(
                PlaceholderRuntimeClassifier.ClassifyLabel(null, null, true),
                Is.EqualTo("missing"));
        }

        [Test]
        public void Report_AllowsEmptyFindingSections_AndBlocksIncompleteStages()
        {
            var features = Mvp2EssentialFeatureMatrix.CreateBaselineEntries();
            var report = Mvp2ReadinessReport.Build(
                features,
                new List<IntegrationAuditFinding>(),
                "2026-01-01T00:00:00Z");

            Assert.That(report.ReadOnly, Is.True);
            Assert.That(report.MissingScripts.Count, Is.EqualTo(0));
            Assert.That(report.MissingReferences.Count, Is.EqualTo(0));
            Assert.That(report.Placeholders.Count, Is.EqualTo(0));
            Assert.That(report.BlockedStages.Count, Is.GreaterThan(0));
            Assert.That(report.BlockedStages, Does.Contain("B"));

            var text = report.FormatText();
            Assert.That(text, Does.Contain("## MissingScripts"));
            Assert.That(text, Does.Contain("## Placeholders"));
            Assert.That(text, Does.Contain("## Features"));

            var json = report.FormatJson();
            Assert.That(json, Does.Contain("\"overall\""));
            Assert.That(json, Does.Contain("missingScripts"));
        }
    }
}
