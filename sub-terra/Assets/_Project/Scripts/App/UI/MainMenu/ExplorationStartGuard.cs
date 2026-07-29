using System;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// 탐사 시작 단일 비행 가드.
    /// Run 준비와 Scene 로드를 각각 최대 한 번만 수행하고, 연타 시 후속 호출을 무시한다.
    /// </summary>
    public sealed class ExplorationStartGuard
    {
        private bool inFlight;
        private bool runPrepared;
        private bool sceneLoadAttempted;
        private int runPrepareCount;
        private int sceneLoadCount;

        public bool IsInFlight => inFlight;
        public int RunPrepareCount => runPrepareCount;
        public int SceneLoadCount => sceneLoadCount;

        /// <summary>
        /// 탐사 시작. prepareRun은 성공 경로에서 한 번만, loadScene도 한 번만 호출된다.
        /// loadScene이 false이면 가드를 풀어 재시도 가능하게 한다.
        /// </summary>
        public bool TryStart(Action prepareRun, Func<bool> loadScene)
        {
            if (inFlight)
            {
                return false;
            }

            if (loadScene == null)
            {
                return false;
            }

            inFlight = true;

            if (!runPrepared)
            {
                prepareRun?.Invoke();
                runPrepared = true;
                runPrepareCount++;
            }

            if (sceneLoadAttempted)
            {
                return false;
            }

            sceneLoadAttempted = true;
            sceneLoadCount++;
            var loaded = loadScene();
            if (!loaded)
            {
                // 로드 실패 시 재시도 가능. 준비 카운트는 유지해 "준비 1회" 계약을 보존한다.
                inFlight = false;
                sceneLoadAttempted = false;
                return false;
            }

            return true;
        }

        /// <summary>Scene 전환 완료 또는 포기 후 다음 탐사를 허용한다.</summary>
        public void Complete()
        {
            inFlight = false;
            runPrepared = false;
            sceneLoadAttempted = false;
        }

        public void Reset()
        {
            inFlight = false;
            runPrepared = false;
            sceneLoadAttempted = false;
            runPrepareCount = 0;
            sceneLoadCount = 0;
        }
    }
}
