using System;
using SubTerra.App.State;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Core
{
    /// <summary>
    /// 데이터 카탈로그 검증 포트. 실제 카탈로그 구현은 후속 단계에서 주입한다.
    /// </summary>
    public interface IDataCatalogPort
    {
        bool Validate(out string reason);
    }

    /// <summary>
    /// 세이브 슬롯 확인/로드 포트. 실제 로컬 JSON 세이브는 후속 단계에서 주입한다.
    /// </summary>
    public interface ISavePort
    {
        bool HasSave { get; }
        GameState Load();
    }

    /// <summary>논리 Scene 전환 포트. UI에 씬 문자열을 흩뿌리지 않기 위한 경계.</summary>
    public interface ISceneLoader
    {
        bool Load(string sceneName);
    }

    /// <summary>Bootstrap·메뉴·기지·통합 Scene의 논리 이름 상수.</summary>
    public static class SceneNames
    {
        public const string Bootstrap = "Bootstrap";
        public const string MainMenu = "MainMenu";
        public const string SurfaceBase = "SurfaceBase";
        public const string Integration = "Mine_Demo_Integration";
    }

    /// <summary>
    /// Unity SceneManager 래퍼.
    /// LoadScene이 예외를 던지지 않는 경우에도 Build 등록 여부로 성공을 판정한다.
    /// </summary>
    public sealed class UnitySceneLoader : ISceneLoader
    {
        public bool Load(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[SubTerra] Scene load failed: empty scene name.");
                return false;
            }

            // 잘못된 이름이어도 LoadScene이 조용히 실패할 수 있으므로 사전 검사한다.
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("[SubTerra] Scene load failed: scene is not in build settings or name is invalid.");
                return false;
            }

            try
            {
                SceneManager.LoadScene(sceneName);
                return true;
            }
            catch (Exception)
            {
                // 메시지에 로컬 경로가 포함될 수 있어 상세 예외 본문은 남기지 않는다.
                Debug.LogError("[SubTerra] Scene load failed: exception during load.");
                return false;
            }
        }
    }

    /// <summary>Phase A용 카탈로그 스텁. 검증을 항상 통과시킨다.</summary>
    public sealed class NullCatalog : IDataCatalogPort
    {
        public bool Validate(out string reason)
        {
            reason = null;
            return true;
        }
    }

    /// <summary>Phase A용 세이브 스텁. 세이브 없음으로 취급한다.</summary>
    public sealed class EmptySave : ISavePort
    {
        public bool HasSave => false;

        public GameState Load() => null;
    }
}
