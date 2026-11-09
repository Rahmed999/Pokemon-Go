using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class ExplorerChecks
{
    [MenuItem("Wildlife Explorer/Run automated zone and encounter checks")]
    public static void Run()
    {
        var api=ScriptableObject.CreateInstance<TestRunnerApi>();var callbacks=new Results();
        api.RegisterCallbacks(callbacks);
        try{api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,assemblyNames=new[]{"WildlifeExplorer.Tests"}}){runSynchronously=true});}
        finally{api.UnregisterCallbacks(callbacks);Object.DestroyImmediate(api);}
    }
    sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun){}
        public void TestStarted(ITestAdaptor test){}
        public void TestFinished(ITestResultAdaptor result){}
        public void RunFinished(ITestResultAdaptor result)
        {
            Directory.CreateDirectory("TestResults");
            TestRunnerApi.SaveResultToFile(result,"TestResults/WildlifeExplorer.xml");
            if(result.FailCount>0||result.PassCount<13)Debug.LogError($"WILDLIFE_TESTS_FAILED: {result.PassCount} passed, {result.FailCount} failed.");
            else Debug.Log($"WILDLIFE_TESTS_OK: {result.PassCount} passed, {result.FailCount} failed.");
        }
    }
}
