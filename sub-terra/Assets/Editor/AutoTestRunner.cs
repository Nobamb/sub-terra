using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using System.IO;

public static class AutoTestRunner
{
    static AutoTestRunner()
    {
        if (SessionState.GetBool("AutoTestRun_V2", false)) return;
        EditorApplication.update += OnUpdate;
    }

    private static void OnUpdate()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        
        EditorApplication.update -= OnUpdate;
        SessionState.SetBool("AutoTestRun_V2", true);
        
        File.WriteAllText("TestResultLog_V2.txt", "Starting EditMode tests...\n");
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Callbacks());
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
    }
    
    private class Callbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) {}
        public void RunFinished(ITestResultAdaptor result) 
        {
            // ITestResultAdaptor has PassCount/FailCount/SkipCount/InconclusiveCount — not TestCount.
            int total = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
            File.AppendAllText(
                "TestResultLog_V2.txt",
                "Finished. Total tests: " + total
                + ", Passed: " + result.PassCount
                + ", Failed: " + result.FailCount
                + ", Skipped: " + result.SkipCount
                + ", Inconclusive: " + result.InconclusiveCount
                + "\n");
        }
        public void TestStarted(ITestAdaptor test) {}
        public void TestFinished(ITestResultAdaptor result) 
        {
            if (result.TestStatus == TestStatus.Failed)
            {
                File.AppendAllText("TestResultLog_V2.txt", "FAIL: " + result.FullName + "\nMSG: " + result.Message + "\nSTACK: " + result.StackTrace + "\n\n");
            }
        }
    }
}
