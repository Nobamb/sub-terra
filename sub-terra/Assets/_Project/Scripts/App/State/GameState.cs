using System;

namespace SubTerra.App.State
{
    /// <summary>
    /// 플레이어 런타임 수치. Unity Object 참조를 두지 않는다.
    /// </summary>
    [Serializable]
    public sealed class PlayerState
    {
        public int Energy { get; private set; }
        public int Gold { get; private set; }
        public float Cargo { get; private set; }
        public float Progress { get; private set; }

        private PlayerState() { }

        // 세이브 복원·테스트 대역에서 값 조립용. 외부에서 소유 GameState 교체는 FromParts로만 한다.
        public PlayerState(int energy, int gold, float cargo, float progress)
        {
            Energy = energy;
            Gold = gold;
            Cargo = cargo;
            Progress = progress;
        }

        /// <summary>의도 기반 골드 변경. 음수는 차감으로 처리하되 0 미만으로 내려가지 않는다.</summary>
        public void AddGold(int amount)
        {
            var next = Gold + amount;
            Gold = next < 0 ? 0 : next;
        }
    }

    /// <summary>진행 목표 카운트. 하위 시스템 교체 없이 GameState가 소유한다.</summary>
    [Serializable]
    public sealed class ProgressState
    {
        public int CompletedObjectives { get; private set; }

        private ProgressState() { }

        public ProgressState(int completedObjectives)
        {
            CompletedObjectives = completedObjectives;
        }
    }

    /// <summary>현재 Run(깊이·안전) 상태. 직렬화 가능한 값만 보관한다.</summary>
    [Serializable]
    public sealed class RunState
    {
        public int Depth { get; private set; }
        public bool IsSafe { get; private set; }

        private RunState() { }

        public RunState(int depth, bool safe)
        {
            Depth = depth;
            IsSafe = safe;
        }
    }

    /// <summary>
    /// 전역 게임 상태 루트. Player/Progress/Run을 소유하며
    /// 외부에서 하위 State 인스턴스를 임의 교체하지 못하게 한다.
    /// </summary>
    [Serializable]
    public sealed class GameState
    {
        public PlayerState Player { get; private set; }
        public ProgressState Progress { get; private set; }
        public RunState Run { get; private set; }

        private GameState() { }

        /// <summary>새 게임용 안전 기본값. 골드·화물 0, 전력 100, 안전 Run.</summary>
        public static GameState CreateNew()
        {
            return new GameState
            {
                Player = new PlayerState(100, 0, 0f, 0f),
                Progress = new ProgressState(0),
                Run = new RunState(0, true)
            };
        }

        /// <summary>
        /// 세이브 복원용 팩터리. 하위 상태가 하나라도 없으면 null을 반환해
        /// 불완전한 상태로 MainMenu에 진입하지 않게 한다.
        /// </summary>
        public static GameState FromParts(PlayerState player, ProgressState progress, RunState run)
        {
            if (player == null || progress == null || run == null)
            {
                return null;
            }

            return new GameState
            {
                Player = player,
                Progress = progress,
                Run = run
            };
        }

        /// <summary>부트스트랩·세이브 경로에서 사용 가능한 완전 상태인지 검사한다.</summary>
        public static bool IsComplete(GameState state)
        {
            return state != null
                && state.Player != null
                && state.Progress != null
                && state.Run != null;
        }
    }
}
