using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class SellPanelTestRunner
{
    private const string LogPath =
        @"C:\Users\USER\AppData\Local\Temp\grok-goal-b501140294d7\implementer\sell-editmode.log";

    [MenuItem("SubTerra/Tests/Run Sell Panel EditMode Tests")]
    public static void RunFromMenu()
    {
        Run();
    }

    public static void Run()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
        File.WriteAllText(LogPath, "Starting Sell Panel EditMode tests...\n");
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Callbacks());
        var filter = new Filter
        {
            testMode = TestMode.EditMode,
            groupNames = new[]
            {
                "SubTerra.App.Tests.Economy",
                "SubTerra.App.Tests.UI.PromptBSellPanelLayoutTests",
                "SubTerra.App.Tests.UI.SellPanelPrefabStructureTests"
            }
        };
        api.Execute(new ExecutionSettings(filter));
    }

    private sealed class Callbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            int total = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
            File.AppendAllText(LogPath,
                "Finished. Total=" + total
                + " Passed=" + result.PassCount
                + " Failed=" + result.FailCount
                + " Skipped=" + result.SkipCount
                + "\n");
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.HasChildren) return;
            var line = result.TestStatus + ": " + result.FullName + "\n";
            File.AppendAllText(LogPath, line);
            if (result.TestStatus == TestStatus.Failed)
            {
                File.AppendAllText(LogPath, "MSG: " + result.Message + "\nSTACK: " + result.StackTrace + "\n\n");
            }
        }
    }
}
