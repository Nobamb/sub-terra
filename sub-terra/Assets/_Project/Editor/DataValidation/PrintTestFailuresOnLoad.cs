using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class PrintTestFailuresOnLoad
{
    static PrintTestFailuresOnLoad()
    {
        if (SessionState.GetBool("TestsRun2", false)) return;
        SessionState.SetBool("TestsRun2", true);
        EditorApplication.delayCall += RunTests;
    }

    private static void RunTests()
    {
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Callbacks());
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
    }
    
    private class Callbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { File.WriteAllText("c:/Users/USER/Desktop/develop/sub-terra/sub-terra/TestFailures.txt", "Running...\n"); }
        public void RunFinished(ITestResultAdaptor result) { File.AppendAllText("c:/Users/USER/Desktop/develop/sub-terra/sub-terra/TestFailures.txt", "Done."); }
        public void TestStarted(ITestAdaptor test) {}
        public void TestFinished(ITestResultAdaptor result) 
        {
            if (result.TestStatus == TestStatus.Failed)
            {
                File.AppendAllText("c:/Users/USER/Desktop/develop/sub-terra/sub-terra/TestFailures.txt", "FAIL: " + result.FullName + "\nMSG: " + result.Message + "\n\n");
            }
        }
    }
}
