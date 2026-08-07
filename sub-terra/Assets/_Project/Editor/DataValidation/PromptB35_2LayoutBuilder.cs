using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 35-2:
    /// Surface Base 창·설정 창 레이아웃을 main 브랜치(MVP2-fix 체인 결과) 기준으로 재적용한다.
    /// B-35 등 후속 작업에서 설정/SurfaceBase 프리팹이 회귀했을 때 복구용.
    /// </summary>
    public static class PromptB35_2LayoutBuilder
    {
        // main(33-2): SurfaceBase 본문 +10% (980×900 → 1078×990)
        public const float SurfaceBaseContentWidth = 980f * 1.1f;
        public const float SurfaceBaseContentHeight = 900f * 1.1f;

        // main(33-2): 설정 창 세로 50% (anchor 0.25~0.75), 너비 600
        public const float SettingsPanelWidth = 600f;
        public const float SettingsAnchorMinY = 0.25f;
        public const float SettingsAnchorMaxY = 0.75f;

        public const string SurfaceBasePrefabPath =
            "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        public const string MainMenuPrefabPath =
            "Assets/_Project/Prefabs/UI/MainMenuPanel.prefab";
        public const string SurfaceBaseScenePath =
            "Assets/_Project/Scenes/App/SurfaceBase.unity";
        public const string MainMenuScenePath =
            "Assets/_Project/Scenes/App/MainMenu.unity";

        [MenuItem("SubTerra/UI/Build Prompt-B 35-2 SurfaceBase Settings From Main")]
        public static void BuildFromMenu()
        {
            var report = Build();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "prompt-b-35-2-layout.txt"),
                report);
        }

        /// <summary>
        /// main 기준 레이아웃 재적용:
        /// 1) SurfaceBase MVP2-fix 체인(33-2 본문/설정 → 33-3 레벨 → 33-4 중복 정리)
        /// 2) MainMenu 설정 패널(해상도·언어·프레임 드롭다운 포함)
        /// </summary>
        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Prompt-B 35-2 SurfaceBase + Settings (main baseline)");

            // SurfaceBase: 본문 크기/단일열 + 설정 50% + 레벨 요약 + 중복 정리.
            sb.AppendLine(PromptB33_4LayoutBuilder.RebuildSurfaceBaseFromMvp2FixChain());

            // MainMenu/SurfaceBase 설정 드롭다운·비례 앵커 레이아웃을 한 번 더 고정.
            sb.AppendLine(PromptB33_2LayoutBuilder.BuildSettingsDropdownsOnly());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sb.ToString();
        }

        [MenuItem("SubTerra/Tests/Run Prompt-B 35-2 Layout Tests")]
        public static void RunLayoutTestsFromMenu()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter
            {
                testMode = TestMode.EditMode,
                groupNames = new[]
                {
                    "SubTerra.App.Tests.UI.PromptB35_2SurfaceSettingsLayoutTests"
                }
            };
            api.RegisterCallbacks(new LayoutTestResultWriter());
            api.Execute(new ExecutionSettings(filter));
            Debug.Log("[SubTerra] Prompt-B 35-2 layout tests requested");
        }

        private sealed class LayoutTestResultWriter : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var summary =
                    "pass=" + result.PassCount
                    + " fail=" + result.FailCount
                    + " skip=" + result.SkipCount
                    + " result=" + result.ResultState
                    + "\n" + result.Message;
                Debug.Log("[SubTerra] Prompt-B 35-2 tests: " + summary);
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
                File.WriteAllText(
                    Path.Combine(projectRoot, "Temp", "prompt-b-35-2-tests.txt"),
                    summary);
            }
        }
    }
}
