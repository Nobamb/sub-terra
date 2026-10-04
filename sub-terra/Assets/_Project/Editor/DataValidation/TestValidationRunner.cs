using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    public static class TestValidationRunner
    {
        private const string Flag = "Temp/subterra-validation.json";
        private const string StateKey = "SubTerra.Validation.Request";
        private const string FailureKey = "SubTerra.Validation.Failed";
        private const string StartSceneKey = "SubTerra.Validation.StartScene";
        private const string ModeKey = "SubTerra.Validation.Mode";
        private static TestRunnerApi api;
        private static ResultWriter activeWriter;
        public static bool IsRunning => !string.IsNullOrEmpty(SessionState.GetString(StateKey, ""));

        [Serializable]
        public sealed class Request
        {
            public string scope = "daily";
            public string mode = "PlayMode";
            public string filter = "";
            public string output = "Temp/validation";
            public bool quit;
            public string legacyResultPath = "";
        }

        [InitializeOnLoadMethod]
        private static void Watch()
        {
            // UTF callbacks are not serialized across the PlayMode domain reload.
            var pending = SessionState.GetString(StateKey, "");
            var pendingMode = SessionState.GetString(ModeKey, "");
            if (!string.IsNullOrEmpty(pending) && !string.IsNullOrEmpty(pendingMode))
            {
                var request = JsonUtility.FromJson<Request>(pending);
                var mode = (TestMode)Enum.Parse(typeof(TestMode), pendingMode);
                var names = File.ReadAllLines(Path.Combine(request.output, pendingMode + "-selected.txt"))
                    .Select(line => line.Split('\t')[0]).ToArray();
                activeWriter = new ResultWriter(request, mode, names, false);
                TestRunnerApi.RegisterTestCallback(activeWriter);
            }
            EditorApplication.update += () =>
            {
                const string cancel = "Temp/subterra-validation-cancel.flag";
                if (File.Exists(cancel))
                {
                    File.Delete(cancel);
                    if (IsRunning)
                    {
                        SessionState.SetBool(FailureKey, true);
                        TestRunnerApi.CancelTestRun(SessionState.GetString("SubTerra.Validation.RunId", ""));
                    }
                }
                if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode
                    || !string.IsNullOrEmpty(SessionState.GetString(StateKey, "")) || !File.Exists(Flag)) return;
                var request = JsonUtility.FromJson<Request>(File.ReadAllText(Flag));
                File.Delete(Flag);
                try { Run(request); }
                catch (Exception exception)
                {
                    Directory.CreateDirectory(request.output);
                    File.WriteAllText(Path.Combine(request.output, request.mode + "-summary.txt"),
                        "FAILED: " + exception + "\nExitCode: 1\n");
                    File.WriteAllText(Path.Combine(request.output, "complete.txt"), "ExitCode: 1");
                    Debug.LogError("[SubTerra] Validation launch failed: " + exception.Message);
                }
            };
        }

        [MenuItem("SubTerra/Tests/Validation/Daily PlayMode")]
        public static void Daily() => Run(new Request());

        [MenuItem("SubTerra/Tests/Validation/Visual PlayMode")]
        public static void Visual() => Run(new Request { scope = "visual" });

        [MenuItem("SubTerra/Tests/Validation/Full EditMode and PlayMode")]
        public static void Full() => Run(new Request { scope = "full", mode = "Both" });

        public static void Batch()
        {
            var args = Environment.GetCommandLineArgs();
            string Arg(string name, string fallback)
            {
                var index = Array.IndexOf(args, name);
                return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
            }
            try
            {
                Run(new Request { scope = Arg("-validationScope", "daily"), mode = Arg("-validationMode", "Both"),
                    filter = Arg("-validationFilter", ""), output = Arg("-validationOutput", "Temp/validation"), quit = true });
            }
            catch (Exception exception)
            {
                Debug.LogError("[SubTerra] Validation launch failed: " + exception.Message);
                EditorApplication.Exit(1);
            }
        }

        public static void Run(Request request)
        {
            if (EditorUtility.scriptCompilationFailed) throw new InvalidOperationException("Fix compilation errors before running tests.");
            if (!new[] { "EditMode", "PlayMode", "Both" }.Contains(request.mode)) throw new ArgumentException("Unknown test mode: " + request.mode);
            if (!string.IsNullOrEmpty(SessionState.GetString(StateKey, ""))) throw new InvalidOperationException("A validation run is already active.");
            Directory.CreateDirectory(request.output);
            SessionState.SetString(StateKey, JsonUtility.ToJson(request));
            SessionState.SetBool(FailureKey, false);
            SessionState.SetString(StartSceneKey, EditorSceneManager.playModeStartScene != null
                ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) : "");
            EditorSceneManager.playModeStartScene = null;
            Start(request, request.mode == "Both" ? TestMode.EditMode : (TestMode)Enum.Parse(typeof(TestMode), request.mode));
        }

        private static void Start(Request request, TestMode mode)
        {
            try
            {
                if (!new[] { "quick", "daily", "visual", "full" }.Contains(request.scope))
                    throw new ArgumentException("Unknown validation scope: " + request.scope);
                if (request.scope == "quick" && string.IsNullOrWhiteSpace(request.filter))
                    throw new ArgumentException("Quick validation requires a fixture/name filter.");
                api = ScriptableObject.CreateInstance<TestRunnerApi>();
                var assemblies = new HashSet<string>(Directory.GetFiles("Assets/_Project/Tests/" + mode,
                    "*.asmdef", SearchOption.AllDirectories).Select(file =>
                    JsonUtility.FromJson<AssemblyName>(File.ReadAllText(file)).name));
                api.RetrieveTestList(mode, root =>
                {
                    var discovered = Leaves(root).Where(test => IsProjectTest(test, assemblies)).ToArray();
                    var tests = discovered
                        .Where(test => string.IsNullOrEmpty(request.filter)
                            || request.filter.Split(';').Any(part => test.FullName.Contains(part)))
                        .Where(test => request.scope == "full" || IsVisual(test) == (request.scope == "visual"))
                        .OrderBy(test => test.FullName, StringComparer.Ordinal).ToArray();
                    var path = Path.Combine(request.output, mode.ToString());
                    File.WriteAllLines(path + "-discovered.txt", discovered.Select(test =>
                        test.FullName + "\t" + (IsVisual(test) ? "Visual" : "Functional") + "\t" + test.RunState));
                    File.WriteAllLines(path + "-selected.txt", tests.Select(test =>
                        test.FullName + "\t" + test.RunState + "\t" + string.Join(",", test.Categories)));
                    if (tests.Length == 0)
                    {
                        File.WriteAllText(path + "-summary.txt", "FAILED: selected 0 tests\nExitCode: 1\n");
                        Complete(request, mode, true);
                        return;
                    }
                    if (tests.Any(IsVisual) && (Application.isBatchMode
                        || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null))
                    {
                        File.WriteAllText(path + "-summary.txt", "FAILED: Visual requires a rendering, non-batch Editor.\nExitCode: 1\n");
                        Complete(request, mode, true);
                        return;
                    }
                    var writer = new ResultWriter(request, mode, tests.Select(test => test.FullName).ToArray());
                    activeWriter = writer;
                    SessionState.SetString(ModeKey, mode.ToString());
                    File.WriteAllText("Temp/ui-test-stages.tsv", "");
                    api.RegisterCallbacks(writer);
                    EditorSceneManager.playModeStartScene = null;
                    var guid = api.Execute(new ExecutionSettings(new Filter { testMode = mode,
                        testNames = tests.Select(test => test.FullName).ToArray() }));
                    SessionState.SetString("SubTerra.Validation.RunId", guid);
                });
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(request.output, mode + "-summary.txt"), "FAILED: " + exception + "\nExitCode: 1\n");
                Complete(request, mode, true);
            }
        }

        private static IEnumerable<ITestAdaptor> Leaves(ITestAdaptor root)
        {
            if (!root.IsSuite) { yield return root; yield break; }
            foreach (var child in root.Children)
                foreach (var leaf in Leaves(child)) yield return leaf;
        }

        private static bool IsProjectTest(ITestAdaptor test, HashSet<string> assemblies)
        {
            var assembly = test;
            while (assembly != null && !assembly.IsTestAssembly) assembly = assembly.Parent;
            return assembly != null && assemblies.Contains(assembly.Name.Replace(".dll", ""));
        }

        [Serializable] private sealed class AssemblyName { public string name; }

        private static bool IsVisual(ITestAdaptor test)
        {
            for (var node = test; node != null; node = node.Parent)
                if (node.Categories.Contains("Visual")) return true;
            return false;
        }

        private static void Complete(Request request, TestMode mode, bool failed)
        {
            if (activeWriter != null) TestRunnerApi.UnregisterTestCallback(activeWriter);
            activeWriter = null;
            SessionState.SetBool(FailureKey, SessionState.GetBool(FailureKey, false) || failed);
            EditorApplication.delayCall += () =>
            {
                if (api != null) UnityEngine.Object.DestroyImmediate(api);
                api = null;
                if (request.mode == "Both" && mode == TestMode.EditMode) { Start(request, TestMode.PlayMode); return; }
                var previous = SessionState.GetString(StartSceneKey, "");
                EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
                var exitCode = SessionState.GetBool(FailureKey, false) ? 1 : 0;
                if (!string.IsNullOrEmpty(request.legacyResultPath))
                {
                    File.Copy(Path.Combine(request.output, mode + "-summary.txt"), request.legacyResultPath, true);
                    File.WriteAllText(request.legacyResultPath + ".done", exitCode == 0 ? "Passed" : "Failed");
                }
                File.WriteAllText(Path.Combine(request.output, "complete.txt"), "ExitCode: " + exitCode);
                SessionState.EraseString(StateKey);
                SessionState.EraseString(ModeKey);
                SessionState.EraseString("SubTerra.Validation.RunId");
                if (request.quit) EditorApplication.Exit(exitCode);
            };
        }

        private sealed class ResultWriter : IErrorCallbacks
        {
            private readonly Request request;
            private readonly TestMode mode;
            private readonly string[] expected;
            private readonly string path;
            public ResultWriter(Request request, TestMode mode, string[] expected, bool initialize = true)
            {
                this.request = request; this.mode = mode; this.expected = expected;
                path = Path.Combine(request.output, mode.ToString());
                if (initialize) File.WriteAllText(path + "-progress.txt", "");
            }
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void OnError(string message)
            {
                TestRunnerApi.UnregisterTestCallback(this);
                File.WriteAllText(path + "-summary.txt", "FAILED: " + message + "\nExitCode: 1\n");
                Complete(request, mode, true);
            }
            public void TestStarted(ITestAdaptor test)
            {
                if (!test.IsSuite) File.AppendAllText(path + "-progress.txt", "START\t" + test.FullName + "\n");
            }
            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.Test.IsSuite) File.AppendAllText(path + "-progress.txt", result.ResultState + "\t"
                    + result.FullName + "\t" + result.Duration.ToString("F6", CultureInfo.InvariantCulture) + "\n");
            }
            public void RunFinished(ITestResultAdaptor result)
            {
                TestRunnerApi.UnregisterTestCallback(this);
                TestRunnerApi.SaveResultToFile(result, path + "-results.xml");
                if (File.Exists("Temp/ui-test-stages.tsv")) File.Copy("Temp/ui-test-stages.tsv", path + "-stages.tsv", true);
                var leaves = ResultLeaves(result).ToArray();
                var actual = new HashSet<string>(leaves.Select(test => test.FullName));
                var missing = expected.Where(name => !actual.Contains(name)).ToArray();
                var unexpected = actual.Except(expected).ToArray();
                var executed = result.PassCount + result.FailCount;
                var failed = result.FailCount > 0 || result.InconclusiveCount > 0 || executed == 0
                    || missing.Length > 0 || unexpected.Length > 0 || leaves.Length != expected.Length || result.TestStatus == TestStatus.Failed;
                File.WriteAllText(path + "-summary.txt", $"Mode: {mode}\nScope: {request.scope}\nSelected: {expected.Length}\nExecuted: {executed}\nPass: {result.PassCount}\nFail: {result.FailCount}\nSkip: {result.SkipCount}\nInconclusive: {result.InconclusiveCount}\nDurationSec: {result.Duration.ToString("F6", CultureInfo.InvariantCulture)}\nMissing: {string.Join(",", missing)}\nUnexpected: {string.Join(",", unexpected)}\nExitCode: {(failed ? 1 : 0)}\n");
                Complete(request, mode, failed);
            }
            private static IEnumerable<ITestResultAdaptor> ResultLeaves(ITestResultAdaptor root)
            {
                if (!root.Test.IsSuite) { yield return root; yield break; }
                foreach (var child in root.Children)
                    foreach (var leaf in ResultLeaves(child)) yield return leaf;
            }
        }
    }
}
