using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using SubTerra.App.Editor.DataValidation;

/// <summary>
/// One-shot: apply Surface Base sell layout + run Economy/Sell EditMode tests.
/// Writes evidence logs under the goal scratch path when present, else Temp/.
/// </summary>
public static class SellPanelBootstrapOnce
{
    private const string SessionKey = "SellPanelBootstrap_PR12345_v1";
    private const string ScratchRoot =
        @"C:\Users\USER\AppData\Local\Temp\grok-goal-b501140294d7\implementer";

    static SellPanelBootstrapOnce()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Run;
            return;
        }

        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);

        try
        {
            Directory.CreateDirectory(ScratchRoot);
            var layoutLog = Path.Combine(ScratchRoot, "sell-layout.log");
            var report = PromptB_SellPanelLayoutBuilder.Build();
            File.WriteAllText(layoutLog, report);
            Debug.Log("[SubTerra] SellPanelBootstrap layout done ??" + layoutLog);

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                groupNames = new[]
                {
                    "SubTerra.App.Tests.Economy",
                    "SubTerra.App.Tests.UI.PromptBSellPanelLayoutTests"
                }
            }));
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SubTerra] SellPanelBootstrap failed: " + ex);
            File.WriteAllText(
                Path.Combine(ScratchRoot, "sell-bootstrap-error.txt"),
                ex.ToString());
        }
    }

    private sealed class Callbacks : ICallbacks
    {
        private readonly string logPath = Path.Combine(ScratchRoot, "sell-unity-editmode.log");

        public void RunStarted(ITestAdaptor testsToRun)
        {
            File.WriteAllText(logPath, "Starting Unity EditMode sell tests...\n");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            int total = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
            File.AppendAllText(
                logPath,
                "Finished. Total=" + total
                + " Passed=" + result.PassCount
                + " Failed=" + result.FailCount
                + " Skipped=" + result.SkipCount
                + "\n");
            // also copy summary into static/bind/gate logs for verification plan
            File.WriteAllText(
                Path.Combine(ScratchRoot, "sell-static.log"),
                File.ReadAllText(logPath));
            File.WriteAllText(
                Path.Combine(ScratchRoot, "sell-gate.log"),
                "See sell-unity-editmode.log / headless sell-editmode.log for gate matrix.\n"
                + File.ReadAllText(logPath));
            Debug.Log("[SubTerra] SellPanelBootstrap tests finished. Failed=" + result.FailCount);
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

            File.AppendAllText(logPath, result.TestStatus + ": " + result.FullName + "\n");
            if (result.TestStatus == TestStatus.Failed)
            {
                File.AppendAllText(
                    logPath,
                    "MSG: " + result.Message + "\nSTACK: " + result.StackTrace + "\n\n");
            }
        }
    }
}
