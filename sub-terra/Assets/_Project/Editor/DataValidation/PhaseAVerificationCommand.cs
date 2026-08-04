using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SubTerra.App.Core.Data;
using SubTerra.App.Readiness;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// Phase A 게이트를 Test Runner UI 없이 실행한다(MCP/배치 친화).
    /// Edit Mode 테스트와 동일 경로의 실제 API를 호출한다.
    /// </summary>
    public static class PhaseAVerificationCommand
    {
        [MenuItem("SubTerra/MVP2/Verify Phase A Gates")]
        public static void VerifyFromMenu()
        {
            var report = RunAll();
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var path = Path.Combine(projectRoot, "Temp", "mvp2-a-phase-verify.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? projectRoot);
            File.WriteAllText(path, report.Log);
            Debug.Log("[SubTerra] Phase A verify pass=" + report.Pass + " fail=" + report.Fail + " → " + path);
            if (report.Fail > 0)
            {
                Debug.LogError("[SubTerra] Phase A failures:\n" + report.FailDetails);
            }
        }

        /// <summary>배치 진입점.</summary>
        public static void BatchVerifyThenQuit()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                VerifyFromMenu();
                var report = RunAll();
                EditorApplication.Exit(report.Fail > 0 ? 1 : 0);
            }
            catch (Exception exception)
            {
                Debug.LogError("[SubTerra] Phase A batch failed: " + exception);
                EditorApplication.Exit(1);
            }
        }

        private static TestRunnerApi activeApi;
        private static ReadinessTestCallbacks activeCallbacks;

        /// <summary>
        /// NUnit Edit Mode 테스트(SubTerra.App.Tests.Readiness)를 비동기로 실행하고 결과 파일을 남긴다.
        /// </summary>
        [MenuItem("SubTerra/MVP2/Run Phase A NUnit EditMode Tests")]
        public static void RunReadinessNunitTests()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var defaultPath = Path.Combine(projectRoot, "Temp", "mvp2-a-nunit-editmode.log");
            RunReadinessNunitTestsTo(defaultPath);
        }

        public static void RunReadinessNunitTestsTo(string resultPath)
        {
            if (activeApi != null)
            {
                Debug.LogWarning("[SubTerra] Phase A NUnit run already active.");
                return;
            }

            var dir = Path.GetDirectoryName(resultPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(resultPath))
            {
                File.Delete(resultPath);
            }

            if (File.Exists(resultPath + ".done"))
            {
                File.Delete(resultPath + ".done");
            }

            activeApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            activeCallbacks = new ReadinessTestCallbacks(resultPath, ReleaseNunitRunner);
            activeApi.RegisterCallbacks(activeCallbacks);
            var filter = new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "SubTerra.App.Tests.EditMode" },
                groupNames = new[] { "SubTerra.App.Tests.Readiness" }
            };
            activeApi.Execute(new ExecutionSettings(filter));
            Debug.Log("[SubTerra] Phase A NUnit readiness tests requested → " + resultPath);
        }

        private static void ReleaseNunitRunner()
        {
            if (activeApi != null && activeCallbacks != null)
            {
                activeApi.UnregisterCallbacks(activeCallbacks);
            }

            if (activeApi != null)
            {
                UnityEngine.Object.DestroyImmediate(activeApi);
            }

            activeApi = null;
            activeCallbacks = null;
        }

        private sealed class ReadinessTestCallbacks : ICallbacks
        {
            private readonly string path;
            private readonly Action onFinished;
            private readonly StringBuilder sb = new StringBuilder();
            private int pass;
            private int fail;
            private int skip;

            public ReadinessTestCallbacks(string path, Action onFinished)
            {
                this.path = path;
                this.onFinished = onFinished;
                sb.AppendLine("MVP2 Phase A NUnit EditMode");
                sb.AppendLine(DateTime.Now.ToString("o"));
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                sb.AppendLine("RunStarted: " + testsToRun.Name);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                sb.AppendLine("RunFinished");
                sb.AppendLine("Result: " + result.TestStatus);
                sb.AppendLine("Pass: " + pass + " Fail: " + fail + " Skip: " + skip);
                sb.AppendLine("DurationSec: " + result.Duration);
                try
                {
                    File.WriteAllText(path, sb.ToString());
                    File.WriteAllText(path + ".done", result.TestStatus.ToString());
                }
                catch (Exception ex)
                {
                    Debug.LogError("[SubTerra] NUnit result write failed: " + ex.GetType().Name);
                }

                Debug.Log("[SubTerra] Phase A NUnit finished: " + result.TestStatus + " P=" + pass + " F=" + fail);
                onFinished?.Invoke();
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren)
                {
                    return;
                }

                if (result.TestStatus == TestStatus.Passed)
                {
                    pass++;
                }
                else if (result.TestStatus == TestStatus.Failed)
                {
                    fail++;
                    sb.AppendLine("FAIL: " + result.FullName);
                    sb.AppendLine("  " + result.Message);
                }
                else
                {
                    skip++;
                }
            }
        }

        public static Report RunAll()
        {
            var log = new StringBuilder();
            var fails = new StringBuilder();
            var pass = 0;
            var fail = 0;

            void Check(string name, bool ok, string detail = "")
            {
                if (ok)
                {
                    pass++;
                    log.AppendLine("PASS " + name);
                }
                else
                {
                    fail++;
                    log.AppendLine("FAIL " + name + " " + detail);
                    fails.AppendLine(name + ": " + detail);
                }
            }

            log.AppendLine("MVP2 Phase A gate verification");
            log.AppendLine(DateTime.Now.ToString("o"));

            // A-S01 PRD mapping
            var features = Mvp2EssentialFeatureMatrix.CreateBaselineEntries();
            Check("A-S01-feature-count", features != null && features.Count >= 17,
                "count=" + (features == null ? -1 : features.Count));
            var ids = new HashSet<string>();
            var incomplete = 0;
            var badLabel = 0;
            var surrogatePromoted = 0;
            if (features != null)
            {
                for (var i = 0; i < features.Count; i++)
                {
                    var f = features[i];
                    if (string.IsNullOrEmpty(f.FeatureId) || !ids.Add(f.FeatureId))
                    {
                        badLabel++;
                    }

                    var labels = f.StatusLabels();
                    for (var l = 0; l < labels.Count; l++)
                    {
                        if (!ReadinessStatusLabels.IsAllowedLabel(labels[l]))
                        {
                            badLabel++;
                        }
                    }

                    if (f.OverallStatus != ReadinessStatus.Complete)
                    {
                        incomplete++;
                    }

                    if (ReadinessStatusRules.IsInvalidSurrogatePromotion(f.Evidence, f.OverallStatus))
                    {
                        surrogatePromoted++;
                    }
                }
            }

            Check("A-S01-unique-ids-and-labels", badLabel == 0, "bad=" + badLabel);
            Check("A-S01-incomplete-rows-exist", incomplete > 0, "incomplete=" + incomplete);
            Check("A-S01-no-surrogate-complete", surrogatePromoted == 0, "promoted=" + surrogatePromoted);

            var prd = Mvp2EssentialFeatureMatrix.RequiredPrdCompletionConditionIds();
            var stages = Mvp2EssentialFeatureMatrix.PrdCompletionConditionStages();
            var prdGaps = 0;
            for (var i = 0; i < prd.Count; i++)
            {
                if (!stages.ContainsKey(prd[i]) || string.IsNullOrEmpty(stages[prd[i]]))
                {
                    prdGaps++;
                }
            }

            Check("A-S01-prd-conditions", prd.Count == 16 && prdGaps == 0, "gaps=" + prdGaps);

            // A-F03 surrogate rules
            var surrogateEvidence = EvidenceKind.Definition | EvidenceKind.SurrogateTest;
            Check(
                "A-F03-surrogate-runtime",
                ReadinessStatusRules.EvaluateGate(ReadinessGateLevel.Runtime, surrogateEvidence)
                == ReadinessStatus.Partial);
            Check(
                "A-F03-surrogate-play",
                ReadinessStatusRules.EvaluateGate(ReadinessGateLevel.Play, surrogateEvidence)
                == ReadinessStatus.Partial);
            Check(
                "A-F03-surrogate-overall-not-complete",
                ReadinessStatusRules.EvaluateOverall(surrogateEvidence, true) != ReadinessStatus.Complete);

            var fullEvidence = EvidenceKind.Definition
                | EvidenceKind.RuntimePrefab
                | EvidenceKind.Restore
                | EvidenceKind.Play;
            Check(
                "A-F03-full-complete",
                ReadinessStatusRules.EvaluateOverall(fullEvidence, true) == ReadinessStatus.Complete);

            // A-S04 classifier
            Check(
                "A-S04-placeholder-token",
                PlaceholderRuntimeClassifier.IsPlaceholder(
                    "BuildingPlaceholder",
                    "Assets/x/BuildingPlaceholder.prefab"));
            Check(
                "A-S04-real-prefab",
                PlaceholderRuntimeClassifier.ClassifyLabel(
                    "SupportPillar",
                    "Assets/_Project/Prefabs/Gameplay/Buildings/SupportPillar.prefab",
                    false) == "real");

            // A-S05 isolation
            var root = FinalRunTestPaths.CreateIsolatedSaveRoot("phase-a-verify");
            try
            {
                Check("A-S05-isolated-temp", FinalRunTestPaths.IsIsolatedTempRoot(root));
                Check(
                    "A-S05-not-user-slot",
                    !FinalRunTestPaths.IsUserPersistentSavePath(root, Application.persistentDataPath));
                var userSlot = Path.Combine(Application.persistentDataPath, "save_slot_1.json");
                Check(
                    "A-S05-detects-user-slot",
                    FinalRunTestPaths.IsUserPersistentSavePath(userSlot, Application.persistentDataPath));
                var skeleton = FinalRunResultRecord.CreateSkeleton(root);
                Check("A-S05-skeleton-steps", skeleton.Steps.Count == 12);
                Check("A-S05-skeleton-isolated", skeleton.UsedIsolatedSaveRoot);
                Check(
                    "A-S05-entry-path",
                    skeleton.EntryPath == FinalRunResultRecord.EntryPathContract);
            }
            finally
            {
                FinalRunTestPaths.TryDeleteRoot(root);
            }

            // A-S02/S03 Integration audit (real scene, read-only)
            var sceneFindings = new List<IntegrationAuditFinding>();
            Mvp2IntegrationAuditor.AuditIntegrationScene(sceneFindings);
            var missingScripts = 0;
            var structureGaps = 0;
            for (var i = 0; i < sceneFindings.Count; i++)
            {
                if (sceneFindings[i].Kind == IntegrationFindingKind.MissingScript)
                {
                    missingScripts++;
                }

                if (sceneFindings[i].Kind == IntegrationFindingKind.RequiredStructure)
                {
                    structureGaps++;
                }
            }

            Check("A-S02-structure", structureGaps == 0, "gaps=" + structureGaps);
            Check("A-S03-missing-scripts", missingScripts == 0, "count=" + missingScripts);

            // A-S04 placeholders on real catalog
            var prefabFindings = new List<IntegrationAuditFinding>();
            Mvp2IntegrationAuditor.AuditBuildingRuntimePrefabs(prefabFindings);
            var placeholders = 0;
            var missingPrefabs = 0;
            for (var i = 0; i < prefabFindings.Count; i++)
            {
                if (prefabFindings[i].Kind == IntegrationFindingKind.PlaceholderRuntime)
                {
                    placeholders++;
                }

                if (prefabFindings[i].Kind == IntegrationFindingKind.MissingReference
                    && prefabFindings[i].FieldName == "runtimePrefab")
                {
                    missingPrefabs++;
                }
            }

            // Phase I: 공용 BuildingPlaceholder 0 — 시설별 실제 Prefab 사용.
            Check("A-S04-placeholders-cleared", placeholders == 0, "placeholders=" + placeholders);
            Check("A-S04-no-null-prefab", missingPrefabs == 0, "missing=" + missingPrefabs);

            // A-F01 read-only report
            var before = CaptureFingerprint();
            var readiness = Mvp2ReadinessMenu.GenerateReport();
            var after = CaptureFingerprint();
            Check("A-F01-readonly-fingerprint", before == after, "asset fingerprint changed");
            Check("A-F01-feature-count", readiness.Features.Count >= 17);
            Check("A-F01-sections", readiness.FormatText().Contains("## MissingScripts")
                && readiness.FormatText().Contains("## Placeholders"));
            // 섹션은 항상 존재하며, Phase I 이후 placeholder 개수는 0일 수 있다.
            Check("A-F01-placeholders-section", readiness.Placeholders != null);

            // A-F02 missing ref fixture via CatalogValidator (same shipped validator path)
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
                var findings = Mvp2IntegrationAuditor.FindingsFromCatalogValidation(validation);
                IntegrationAuditFinding hit = null;
                for (var i = 0; i < findings.Count; i++)
                {
                    if (findings[i].FieldName == "runtimePrefab")
                    {
                        hit = findings[i];
                        break;
                    }
                }

                Check("A-F02-detects-missing-prefab", hit != null && !validation.IsValid,
                    hit == null ? "no finding" : hit.ToString());
                Check(
                    "A-F02-field-name",
                    hit != null && hit.FieldName == "runtimePrefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(broken);
                UnityEngine.Object.DestroyImmediate(catalog);
            }

            // Scene open cleanliness
            var scene = EditorSceneManager.OpenScene(
                Mvp2IntegrationAuditor.IntegrationScenePath,
                OpenSceneMode.Additive);
            try
            {
                Check("A-S02-scene-valid", scene.IsValid() && !scene.isDirty);
                Check(
                    "A-S02-event-system",
                    FindInScene<EventSystem>(scene) != null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            log.AppendLine("Pass: " + pass + " Fail: " + fail);
            return new Report
            {
                Pass = pass,
                Fail = fail,
                Log = log.ToString(),
                FailDetails = fails.ToString()
            };
        }

        private static string CaptureFingerprint()
        {
            var paths = new[]
            {
                Mvp2IntegrationAuditor.IntegrationScenePath,
                Mvp2IntegrationAuditor.DefaultCatalogPath,
                "Assets/_Project/Data/Buildings/Building_Support_Basic.asset",
                "Assets/_Project/Data/Prefabs/Buildings/BuildingPlaceholder.prefab"
            };
            var sb = new StringBuilder();
            for (var i = 0; i < paths.Length; i++)
            {
                var full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", paths[i]));
                if (!File.Exists(full))
                {
                    sb.Append(paths[i]).Append(":missing|");
                    continue;
                }

                var info = new FileInfo(full);
                sb.Append(paths[i]).Append(':').Append(info.Length).Append(':')
                    .Append(info.LastWriteTimeUtc.Ticks).Append('|');
            }

            return sb.ToString();
        }

        private static T FindInScene<T>(UnityEngine.SceneManagement.Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var c = root.GetComponentInChildren<T>(true);
                if (c != null)
                {
                    return c;
                }
            }

            return null;
        }

        public sealed class Report
        {
            public int Pass;
            public int Fail;
            public string Log = string.Empty;
            public string FailDetails = string.Empty;
        }
    }
}
