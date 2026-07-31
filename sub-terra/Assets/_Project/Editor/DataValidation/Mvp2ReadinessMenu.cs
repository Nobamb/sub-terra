using System;
using System.IO;
using SubTerra.App.Readiness;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// MVP2 Phase A 기준선 보고서 메뉴. 읽기 전용 감사 + PRD 매트릭스를 합쳐 Temp에 기록한다.
    /// </summary>
    public static class Mvp2ReadinessMenu
    {
        private const string DefaultTextPath = "Temp/mvp2-a-readiness-report.txt";
        private const string DefaultJsonPath = "Temp/mvp2-a-readiness-report.json";

        [MenuItem("SubTerra/MVP2/Run Phase A Readiness Audit")]
        public static void RunFromMenu()
        {
            var report = GenerateReport();
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var textPath = Path.Combine(projectRoot, DefaultTextPath);
            var jsonPath = Path.Combine(projectRoot, DefaultJsonPath);
            Directory.CreateDirectory(Path.GetDirectoryName(textPath) ?? projectRoot);
            File.WriteAllText(textPath, report.FormatText());
            File.WriteAllText(jsonPath, report.FormatJson());
            Debug.Log(
                "[SubTerra] MVP2 Phase A readiness report written to " + textPath +
                " features=" + report.Features.Count +
                " missingScripts=" + report.MissingScripts.Count +
                " missingRefs=" + report.MissingReferences.Count +
                " placeholders=" + report.Placeholders.Count);
        }

        /// <summary>배치/테스트 진입점. 에셋을 저장하지 않는다.</summary>
        public static Mvp2ReadinessReport GenerateReport()
        {
            var features = Mvp2EssentialFeatureMatrix.CreateBaselineEntries();
            var findings = Mvp2IntegrationAuditor.AuditAll(includeCatalogPlaceholders: true);
            return Mvp2ReadinessReport.Build(
                features,
                findings,
                DateTime.UtcNow.ToString("o"));
        }

        /// <summary>
        /// Batchmode 진입점: 보고서 생성 후 종료.
        /// -executeMethod SubTerra.App.Editor.DataValidation.Mvp2ReadinessMenu.BatchGenerateThenQuit
        /// </summary>
        public static void BatchGenerateThenQuit()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                RunFromMenu();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[SubTerra] MVP2 readiness batch failed: " +
                    exception.GetType().Name + " " + exception.Message);
                EditorApplication.Exit(1);
            }
        }
    }
}
