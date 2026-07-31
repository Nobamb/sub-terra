using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>
/// One-shot runner for SubTerra.Gameplay.DemoWorld.EditModeTests.
/// Writes results to a path from SessionState or a default under Temp.
/// Drop Temp/subterra-run-demoworld-editmode.flag to trigger from outside the Editor.
/// </summary>
public static class DemoWorldEditModeTestRunner
{
    private const string FlagPath = "Temp/subterra-run-demoworld-editmode.flag";
    private const string DefaultOutPath =
        @"C:\Users\USER\AppData\Local\Temp\grok-goal-f3bf65499df7\implementer\demoworld-editmode-results.txt";
    private const string DefaultDonePath =
        @"C:\Users\USER\AppData\Local\Temp\grok-goal-f3bf65499df7\implementer\demoworld-editmode-done.flag";
    private const string SessionKeyOut = "DemoWorldEditModeTestRunner.OutPath";
    private const string SessionKeyDone = "DemoWorldEditModeTestRunner.DonePath";

    private static TestRunnerApi activeApi;
    private static Callbacks activeCallbacks;

    [InitializeOnLoadMethod]
    private static void WatchFlag()
    {
        EditorApplication.update += PollFlag;
    }

    private static void PollFlag()
    {
        if (!File.Exists(FlagPath))
        {
            return;
        }

        try
        {
            File.Delete(FlagPath);
            Run(DefaultOutPath, DefaultDonePath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[DemoWorldEditModeTestRunner] flag handling failed: "
                + exception.GetType().Name);
        }
    }

    [MenuItem("Tools/SubTerra/MVP2/Run DemoWorld EditMode Tests")]
    public static void RunFromMenu()
    {
        Run(DefaultOutPath, DefaultDonePath);
    }

    public static void Run(string outPath, string donePath)
    {
        SessionState.SetString(SessionKeyOut, outPath ?? DefaultOutPath);
        SessionState.SetString(SessionKeyDone, donePath ?? DefaultDonePath);

        string path = SessionState.GetString(SessionKeyOut, DefaultOutPath);
        string done = SessionState.GetString(SessionKeyDone, DefaultDonePath);

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        if (File.Exists(done))
        {
            File.Delete(done);
        }

        File.WriteAllText(path, "Starting SubTerra.Gameplay.DemoWorld.EditModeTests...\n");
        Release();

        activeApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        activeCallbacks = new Callbacks(path, done);
        activeApi.RegisterCallbacks(activeCallbacks);
        activeApi.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.EditMode,
            assemblyNames = new[] { "SubTerra.Gameplay.DemoWorld.EditModeTests" }
        }));
        Debug.Log("[DemoWorldEditModeTestRunner] started → " + path);
    }

    private static void Release()
    {
        if (activeApi != null && activeCallbacks != null)
        {
            try
            {
                activeApi.UnregisterCallbacks(activeCallbacks);
            }
            catch
            {
                // ignored
            }
        }

        if (activeApi != null)
        {
            UnityEngine.Object.DestroyImmediate(activeApi);
        }

        activeApi = null;
        activeCallbacks = null;
    }

    private sealed class Callbacks : ICallbacks
    {
        private readonly string path;
        private readonly string donePath;
        private readonly StringBuilder sb = new StringBuilder();

        public Callbacks(string outPath, string done)
        {
            path = outPath;
            donePath = done;
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            sb.AppendLine("Starting EditMode tests: " + testsToRun.Name);
            sb.AppendLine("TestCaseCount: " + testsToRun.TestCaseCount);
            File.WriteAllText(path, sb.ToString());
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            sb.AppendLine();
            sb.AppendLine("Finished.");
            sb.AppendLine("PassCount: " + result.PassCount);
            sb.AppendLine("FailCount: " + result.FailCount);
            sb.AppendLine("SkipCount: " + result.SkipCount);
            sb.AppendLine("InconclusiveCount: " + result.InconclusiveCount);
            sb.AppendLine("ResultState: " + result.ResultState);
            File.WriteAllText(path, sb.ToString());
            File.WriteAllText(donePath, "done FailCount=" + result.FailCount);
            Debug.Log(
                "[DemoWorldEditModeTestRunner] Finished Pass="
                + result.PassCount
                + " Fail="
                + result.FailCount);
            EditorApplication.delayCall += Release;
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

            sb.AppendLine(result.TestStatus + ": " + result.FullName);
            if (result.TestStatus == TestStatus.Failed)
            {
                sb.AppendLine("  MSG: " + result.Message);
                sb.AppendLine("  STACK: " + result.StackTrace);
            }

            File.WriteAllText(path, sb.ToString());
        }
    }
}
