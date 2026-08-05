using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class PrintTestFailuresOnLoad
{
    private static string FailureLogPath
    {
        get
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? Application.dataPath;
            var resultDirectory = Path.Combine(projectRoot, "Library", "TestResults");
            Directory.CreateDirectory(resultDirectory);
            return Path.Combine(resultDirectory, "TestFailures.txt");
        }
    }

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
        public void RunStarted(ITestAdaptor testsToRun) { File.WriteAllText(FailureLogPath, "Running...\n"); }
        public void RunFinished(ITestResultAdaptor result) { File.AppendAllText(FailureLogPath, "Done."); }
        public void TestStarted(ITestAdaptor test) {}
        public void TestFinished(ITestResultAdaptor result) 
        {
            if (result.TestStatus == TestStatus.Failed)
            {
                File.AppendAllText(FailureLogPath, "FAIL: " + result.FullName + "\nMSG: " + result.Message + "\n\n");
            }
        }
    }
}
