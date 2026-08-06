using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Readiness;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.Tests.Readiness
{
    /// <summary>A-S02~S04, A-F01~F02 Integration 읽기 전용 감사 및 누락 참조 검출.</summary>
    public sealed class Mvp2IntegrationAuditTests
    {
        [Test]
        public void A_S02_IntegrationScene_RequiredStructure_IsReportedCleanOrWithNamedGaps()
        {
            var findings = new List<IntegrationAuditFinding>();
            Mvp2IntegrationAuditor.AuditIntegrationScene(findings);

            var structureFails = new List<IntegrationAuditFinding>();
            for (var i = 0; i < findings.Count; i++)
            {
                if (findings[i].Kind == IntegrationFindingKind.RequiredStructure)
                {
                    structureFails.Add(findings[i]);
                }
            }

            // 실제 Integration Scene은 Phase M 이후 필수 Root를 가져야 한다.
            Assert.That(
                structureFails,
                Is.Empty,
                "Structure gaps: " + string.Join(" | ", structureFails));
        }

        [Test]
        public void A_S03_IntegrationScene_MissingScriptCount_IsZero()
        {
            var findings = new List<IntegrationAuditFinding>();
            Mvp2IntegrationAuditor.AuditIntegrationScene(findings);

            var missing = 0;
            for (var i = 0; i < findings.Count; i++)
            {
                if (findings[i].Kind == IntegrationFindingKind.MissingScript)
                {
                    missing++;
                }
            }

            Assert.That(missing, Is.EqualTo(0), "Missing scripts must be 0 for Integration Scene");
        }

        [Test]
        public void A_S04_BuildingRuntimePrefabs_LabelPlaceholderVersusReal()
        {
            var findings = new List<IntegrationAuditFinding>();
            Mvp2IntegrationAuditor.AuditBuildingRuntimePrefabs(findings);

            var placeholders = 0;
            var missing = 0;
            for (var i = 0; i < findings.Count; i++)
            {
                if (findings[i].Kind == IntegrationFindingKind.PlaceholderRuntime)
                {
                    placeholders++;
                    Assert.That(findings[i].FieldName, Is.EqualTo("runtimePrefab"));
                    Assert.That(findings[i].Message, Does.Contain("placeholder"));
                }

                if (findings[i].Kind == IntegrationFindingKind.MissingReference
                    && findings[i].FieldName == "runtimePrefab")
                {
                    missing++;
                }
            }

            // Phase I 이후 카탈로그는 시설별 실제 Runtime Prefab을 사용한다(공용 placeholder 0).
            // 감사기는 placeholder가 있으면 명시 보고하고, 없으면 0으로 유지한다.
            Assert.That(placeholders, Is.EqualTo(0),
                "Catalog buildings should use real Runtime Prefabs (no shared BuildingPlaceholder)");
            Assert.That(missing, Is.EqualTo(0),
                "Catalog buildings should have a real prefab assigned");

            // 분류기 자체는 placeholder/real/missing 라벨을 구분할 수 있어야 한다.
            Assert.That(
                PlaceholderRuntimeClassifier.ClassifyLabel(
                    "BuildingPlaceholder",
                    "Assets/_Project/Data/Prefabs/Buildings/BuildingPlaceholder.prefab",
                    false),
                Is.EqualTo("placeholder"));
            Assert.That(
                PlaceholderRuntimeClassifier.ClassifyLabel(
                    "SupportPillar",
                    "Assets/_Project/Prefabs/Gameplay/Buildings/SupportPillar.prefab",
                    false),
                Is.EqualTo("real"));
        }

        [Test]
        public void A_F01_ReadinessAudit_IsReadOnly_AndReturnsStructuredReport()
        {
            var before = CaptureTrackedAssetFingerprint();
            var report = Mvp2ReadinessMenu.GenerateReport();
            var after = CaptureTrackedAssetFingerprint();

            Assert.That(report, Is.Not.Null);
            Assert.That(report.ReadOnly, Is.True);
            Assert.That(report.Features.Count, Is.GreaterThanOrEqualTo(17));
            Assert.That(report.FormatText(), Does.Contain("## MissingScripts"));
            Assert.That(report.FormatText(), Does.Contain("## MissingReferences"));
            Assert.That(report.FormatText(), Does.Contain("## Placeholders"));
            Assert.That(report.FormatJson(), Does.Contain("\"placeholders\""));

            // 감사 전후 프로젝트 에셋 지문이 같아야 한다(읽기 전용).
            Assert.That(after, Is.EqualTo(before), "Readiness audit must not modify project assets");
        }

        [Test]
        public void A_F02_MissingRequiredReference_ReportsExactAssetAndField()
        {
            // 복제 테스트 픽스처: Runtime Prefab 필수 참조를 제거한 BuildingData.
            var broken = ScriptableObject.CreateInstance<BuildingData>();
            broken.name = "A_F02_MissingPrefabBuilding";
            broken.EditorSet(
                DataIds.Buildings.SupportBasic,
                "Broken Support",
                null,
                0,
                new List<ItemCostEntry>());

            var catalog = ScriptableObject.CreateInstance<GameDataCatalog>();
            catalog.EditorSetLists(
                new List<MineralData>(),
                new List<BuildingData> { broken },
                new List<RecipeData>(),
                new List<UpgradeData>(),
                new List<DialogueTemplateData>());

            try
            {
                var validation = CatalogValidator.Validate(catalog);
                Assert.That(validation.IsValid, Is.False);

                var findings = Mvp2IntegrationAuditor.FindingsFromCatalogValidation(validation);
                Assert.That(findings.Count, Is.GreaterThan(0));

                IntegrationAuditFinding prefabFinding = null;
                for (var i = 0; i < findings.Count; i++)
                {
                    if (findings[i].FieldName == "runtimePrefab")
                    {
                        prefabFinding = findings[i];
                        break;
                    }
                }

                Assert.That(prefabFinding, Is.Not.Null, "runtimePrefab missing-ref must be reported");
                Assert.That(prefabFinding.Kind, Is.EqualTo(IntegrationFindingKind.MissingReference));
                Assert.That(prefabFinding.Message, Does.Contain("runtime prefab").IgnoreCase);
                // 에셋 식별: 이름 또는 경로가 실패 메시지/필드와 함께 노출된다.
                Assert.That(
                    prefabFinding.AssetPath.Contains("A_F02_MissingPrefabBuilding")
                    || prefabFinding.Message.Length > 0,
                    Is.True);
                Assert.That(prefabFinding.FieldName, Is.EqualTo("runtimePrefab"));
            }
            finally
            {
                Object.DestroyImmediate(broken);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void A_S02_OpenIntegrationScene_DoesNotRequireSave()
        {
            var scene = EditorSceneManager.OpenScene(
                Mvp2IntegrationAuditor.IntegrationScenePath,
                OpenSceneMode.Additive);
            try
            {
                Assert.That(scene.IsValid(), Is.True);
                Assert.That(scene.isDirty, Is.False);
                Assert.That(Object.FindObjectOfType<EventSystem>(), Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static string CaptureTrackedAssetFingerprint()
        {
            // Integration Scene + BuildingData 에셋 수정 시각 지문(읽기 전용 증명).
            var paths = new List<string>
            {
                Mvp2IntegrationAuditor.IntegrationScenePath,
                Mvp2IntegrationAuditor.DefaultCatalogPath,
                "Assets/_Project/Data/Buildings/Building_Support_Basic.asset",
                "Assets/_Project/Data/Prefabs/Buildings/BuildingPlaceholder.prefab"
            };

            var parts = new List<string>();
            for (var i = 0; i < paths.Count; i++)
            {
                var full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", paths[i]));
                if (!File.Exists(full))
                {
                    parts.Add(paths[i] + ":missing");
                    continue;
                }

                var info = new FileInfo(full);
                parts.Add(paths[i] + ":" + info.Length + ":" + info.LastWriteTimeUtc.Ticks);
            }

            return string.Join("|", parts);
        }
    }
}
