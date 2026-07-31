using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using System.IO;

public static class HeadlessTestRunner
{
    public static void Run()
    {
        File.WriteAllText("TestFailures.txt", "Starting tests...\n");
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Callbacks());
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode | TestMode.PlayMode }));
    }
    
    private class Callbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) {}
        public void RunFinished(ITestResultAdaptor result) 
        { 
            File.AppendAllText("TestFailures.txt", "Finished tests.\n");
            EditorApplication.Exit(0); 
        }
        public void TestStarted(ITestAdaptor test) {}
        public void TestFinished(ITestResultAdaptor result) 
        {
            if (result.TestStatus == TestStatus.Failed)
            {
                File.AppendAllText("TestFailures.txt", "FAILED: " + result.FullName + "\n" + result.Message + "\n" + result.StackTrace + "\n\n");
            }
        }
    }
}
