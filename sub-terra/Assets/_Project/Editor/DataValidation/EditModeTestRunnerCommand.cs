using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.State;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// 외부에서 플래그 파일을 두면 Edit Mode 테스트를 실행하고 결과를 기록한다.
    /// Unity MCP가 불안정할 때 검증용.
    /// </summary>
    public static class EditModeTestRunnerCommand
    {
        private const string FlagPath = "Temp/subterra-run-editmode.flag";
        private const string BuildDataFlagPath = "Temp/subterra-build-phaseb-data.flag";
        private const string EvidenceFlagPath = "Temp/subterra-phaseb-evidence.flag";
        private const string DefaultResultPath = "Temp/subterra-editmode-results.txt";
        private const string EvidenceDir = "Temp/phase-b-evidence";
        private static TestRunnerApi activeApi;
        private static ResultWriter activeReceiver;
        private static bool testRunActive;

        [InitializeOnLoadMethod]
        private static void WatchFlag()
        {
            EditorApplication.update += PollFlag;
        }

        private static void PollFlag()
        {
            if (File.Exists(FlagPath))
            {
                try
                {
                    File.Delete(FlagPath);
                    RunEditModeTests(DefaultResultPath);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "[SubTerra] Edit Mode test flag handling failed: " + exception.GetType().Name);
                }
            }

            if (File.Exists(BuildDataFlagPath))
            {
                try
                {
                    File.Delete(BuildDataFlagPath);
                    Debug.Log("[SubTerra] " + MvpDataAssetBuilder.BuildAll());
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        "[SubTerra] Phase B data build failed: " + exception.GetType().Name);
                }
            }

            if (File.Exists(EvidenceFlagPath))
            {
                try
                {
                    File.Delete(EvidenceFlagPath);
                    CapturePhaseBEvidence();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "[SubTerra] Phase B evidence flag handling failed: " + exception.GetType().Name);
                }
            }
        }

        [MenuItem("SubTerra/Tests/Run Edit Mode Tests")]
        public static void RunFromMenu()
        {
            RunEditModeTests(DefaultResultPath);
        }

        [MenuItem("SubTerra/Data/Capture Phase B Evidence")]
        public static void CaptureEvidenceMenu()
        {
            CapturePhaseBEvidence();
        }

        public static void CapturePhaseBEvidence()
        {
            Directory.CreateDirectory(EvidenceDir);
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(
                "Assets/_Project/Data/Catalog/GameDataCatalog.asset");

            var ids = new StringBuilder();
            ids.AppendLine("Phase B catalog ID dump");
            void Min(string id)
            {
                ids.AppendLine(catalog != null && catalog.TryGetMineral(id, out var d)
                    ? $"OK mineral {id} name={d.DisplayName} weight={d.UnitWeight} price={d.UnitPrice}"
                    : $"FAIL mineral {id}");
            }

            void Bld(string id)
            {
                ids.AppendLine(catalog != null && catalog.TryGetBuilding(id, out var d)
                    ? $"OK building {id} name={d.DisplayName} power={d.PowerDraw} prefab={(d.RuntimePrefab != null)} costs={d.BuildCosts.Count}"
                    : $"FAIL building {id}");
            }

            void Upg(string id)
            {
                ids.AppendLine(catalog != null && catalog.TryGetUpgrade(id, out var d)
                    ? $"OK upgrade {id} name={d.DisplayName} max={d.MaxLevel} levels={d.Levels.Count}"
                    : $"FAIL upgrade {id}");
            }

            Min(DataIds.Minerals.Copper);
            Min(DataIds.Minerals.Iron);
            Min(DataIds.Minerals.Lithium);
            Bld(DataIds.Buildings.SupportBasic);
            Bld(DataIds.Buildings.LightBasic);
            Bld(DataIds.Buildings.ChargerBasic);
            Bld(DataIds.Buildings.StorageBasic);
            Bld(DataIds.Buildings.SettlementBasic);
            Bld(DataIds.Buildings.OutpostCoreBasic);
            Upg(DataIds.Upgrades.DrillSpeed);
            Upg(DataIds.Upgrades.DrillEfficiency);
            Upg(DataIds.Upgrades.DroneScan);
            Upg(DataIds.Upgrades.DroneRescue);
            ids.AppendLine(catalog != null && catalog.TryGetRecipe(DataIds.Recipes.SupportBasic, out var recipe)
                ? $"OK recipe {recipe.Id} inputs={recipe.Inputs.Count}"
                : "FAIL recipe");
            ids.AppendLine(catalog != null && catalog.TryGetDialogue(DataIds.Dialogue.LowPowerWarning, out var dlg)
                ? $"OK dialogue {dlg.Id} situation={dlg.SituationKey}"
                : "FAIL dialogue");
            File.WriteAllText(Path.Combine(EvidenceDir, "phase-b-catalog-ids.txt"), ids.ToString());

            var val = new StringBuilder();
            val.AppendLine("Phase B editor validation");
            if (catalog != null)
            {
                var good = catalog.ValidateAll();
                val.AppendLine($"GOOD catalog IsValid={good.IsValid} Errors={good.ErrorCount} DictInit={good.DictionaryInitialized}");
                val.AppendLine(good.FormatAll());
            }

            var a = ScriptableObject.CreateInstance<MineralData>();
            a.name = "DupA";
            a.EditorSet(DataIds.Minerals.Copper, "A", 1f, 1);
            var b = ScriptableObject.CreateInstance<MineralData>();
            b.name = "DupB";
            b.EditorSet(DataIds.Minerals.Copper, "B", 1f, 1);
            var dup = ScriptableObject.CreateInstance<GameDataCatalog>();
            dup.EditorSetLists(new List<MineralData> { a, b }, new List<BuildingData>(), new List<RecipeData>(),
                new List<UpgradeData>(), new List<DialogueTemplateData>());
            var dupResult = dup.ValidateAll();
            val.AppendLine($"DUP fixture IsValid={dupResult.IsValid} DictInit={dupResult.DictionaryInitialized}");
            val.AppendLine(dupResult.FormatAll());

            var broken = ScriptableObject.CreateInstance<BuildingData>();
            broken.name = "MissingPrefabBld";
            broken.EditorSet(DataIds.Buildings.SupportBasic, "Broken", null, 0, new List<ItemCostEntry>());
            var miss = ScriptableObject.CreateInstance<GameDataCatalog>();
            miss.EditorSetLists(new List<MineralData>(), new List<BuildingData> { broken }, new List<RecipeData>(),
                new List<UpgradeData>(), new List<DialogueTemplateData>());
            var missResult = miss.ValidateAll();
            val.AppendLine($"MISSING_REF fixture IsValid={missResult.IsValid}");
            val.AppendLine(missResult.FormatAll());
            UnityEngine.Object.DestroyImmediate(a);
            UnityEngine.Object.DestroyImmediate(b);
            UnityEngine.Object.DestroyImmediate(dup);
            UnityEngine.Object.DestroyImmediate(broken);
            UnityEngine.Object.DestroyImmediate(miss);
            File.WriteAllText(Path.Combine(EvidenceDir, "phase-b-editor-validate.log"), val.ToString());

            var boot = new StringBuilder();
            boot.AppendLine("Phase B bootstrap injection");
            GameBootstrapper.ResetInstanceForTests();
            var scenesOk = new SceneProbe();
            var bootOk = new GameObject("EvidenceBootOk").AddComponent<GameBootstrapper>();
            var valid = bootOk.Initialize(catalog, new EmptySave(), scenesOk);
            boot.AppendLine(
                $"VALID Initialize={valid} Failed={bootOk.InitializationFailed} Scene={scenesOk.Name} StateComplete={GameState.IsComplete(bootOk.State)}");
            UnityEngine.Object.DestroyImmediate(bootOk.gameObject);
            GameBootstrapper.ResetInstanceForTests();

            var broken2 = ScriptableObject.CreateInstance<BuildingData>();
            broken2.name = "EvidenceBroken";
            broken2.EditorSet("building.bad.prefab", "Bad", null, 0, new List<ItemCostEntry>());
            var badCatalog = ScriptableObject.CreateInstance<GameDataCatalog>();
            badCatalog.EditorSetLists(new List<MineralData>(), new List<BuildingData> { broken2 }, new List<RecipeData>(),
                new List<UpgradeData>(), new List<DialogueTemplateData>());
            var scenesBad = new SceneProbe();
            var bootBad = new GameObject("EvidenceBootBad").AddComponent<GameBootstrapper>();
            var invalid = bootBad.Initialize(badCatalog, new EmptySave(), scenesBad);
            boot.AppendLine(
                $"INVALID Initialize={invalid} Failed={bootBad.InitializationFailed} SceneLoadCount={scenesBad.Count} Scene={scenesBad.Name ?? "(null)"}");
            badCatalog.Validate(out var reason);
            boot.AppendLine("INVALID reason=" + reason);
            UnityEngine.Object.DestroyImmediate(bootBad.gameObject);
            UnityEngine.Object.DestroyImmediate(broken2);
            UnityEngine.Object.DestroyImmediate(badCatalog);
            GameBootstrapper.ResetInstanceForTests();
            File.WriteAllText(Path.Combine(EvidenceDir, "phase-b-bootstrap.log"), boot.ToString());

            var notes = new StringBuilder();
            notes.AppendLine("Phase B manual notes");
            notes.AppendLine("Menu: SubTerra/Data/Validate Game Data Catalog");
            if (catalog != null)
            {
                var r = catalog.ValidateAll();
                notes.AppendLine("Project catalog validation: " + (r.IsValid ? "PASS" : "FAIL"));
            }

            notes.AppendLine("Play Mode via Unity MCP: not available this session.");
            notes.AppendLine("Fallback: Edit Mode injection of real GameDataCatalog covered in tests/bootstrap log.");
            notes.AppendLine("Runtime asmdef SubTerra.App has no UnityEditor reference.");
            notes.AppendLine("Editor code isolated in SubTerra.App.Editor (includePlatforms=Editor).");
            File.WriteAllText(Path.Combine(EvidenceDir, "phase-b-manual-notes.txt"), notes.ToString());
            File.WriteAllText(Path.Combine(EvidenceDir, "done.txt"), "ok");
            Debug.Log("[SubTerra] Phase B evidence captured under " + EvidenceDir);
        }

        public static void RunEditModeTests(string resultPath)
        {
            if (testRunActive)
            {
                Debug.LogWarning("[SubTerra] Edit Mode tests are already running.");
                return;
            }

            ReleaseRunner();
            activeApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "SubTerra.App.Tests.EditMode" }
            };

            activeReceiver = new ResultWriter(resultPath);
            activeApi.RegisterCallbacks(activeReceiver);
            testRunActive = true;
            activeApi.Execute(new ExecutionSettings(filter));
            Debug.Log("[SubTerra] Edit Mode test run requested → " + resultPath);
        }

        private static void ScheduleRunnerRelease(ResultWriter receiver)
        {
            if (receiver != activeReceiver)
            {
                return;
            }

            testRunActive = false;
            EditorApplication.delayCall += ReleaseRunner;
        }

        private static void ReleaseRunner()
        {
            if (activeApi != null && activeReceiver != null)
            {
                activeApi.UnregisterCallbacks(activeReceiver);
            }

            if (activeApi != null)
            {
                UnityEngine.Object.DestroyImmediate(activeApi);
            }

            activeApi = null;
            activeReceiver = null;
            testRunActive = false;
        }

        private sealed class SceneProbe : ISceneLoader
        {
            public string Name;
            public int Count;

            public bool Load(string sceneName)
            {
                Count++;
                Name = sceneName;
                return true;
            }
        }

        private sealed class ResultWriter : ICallbacks
        {
            private readonly string path;
            private readonly StringBuilder sb = new StringBuilder();
            private int pass;
            private int fail;
            private int skip;

            public ResultWriter(string path)
            {
                this.path = path;
                sb.AppendLine("SubTerra EditMode Test Run");
                sb.AppendLine("Started: " + DateTime.Now.ToString("o"));
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
                sb.AppendLine("Message: " + result.Message);
                sb.AppendLine("StackTrace: " + result.StackTrace);
                try
                {
                    var dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.WriteAllText(path, sb.ToString());
                    // 완료 신호 파일
                    File.WriteAllText(path + ".done", result.TestStatus.ToString());
                }
                catch (Exception ex)
                {
                    // 예외 메시지에는 로컬 경로가 포함될 수 있어 타입명만 기록한다.
                    Debug.LogError("[SubTerra] Failed to write test results: " + ex.GetType().Name);
                }

                Debug.Log("[SubTerra] Edit Mode finished: " + result.TestStatus + " P=" + pass + " F=" + fail);
                ScheduleRunnerRelease(this);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.HasChildren)
                {
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
        }
    }
}
