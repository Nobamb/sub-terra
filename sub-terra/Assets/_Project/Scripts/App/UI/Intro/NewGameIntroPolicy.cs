using SubTerra.App.State;

namespace SubTerra.App.UI.Intro
{
    /// <summary>
    /// 새 게임 인트로 영상 재생 여부. 시작 브리핑이 뜨는 조건(목표 진행이 비어 있는 첫 광산 진입)과
    /// 같은 기준을 써서 이어하기·재진입에서는 다시 나오지 않는다.
    /// </summary>
    public static class NewGameIntroPolicy
    {
        public static bool IsFirstMineEntry(ProgressState progress)
        {
            return progress != null
                && string.IsNullOrEmpty(progress.CurrentObjectiveId)
                && progress.CompletedObjectives <= 0
                && !progress.IsDemoComplete;
        }

        /// <summary>
        /// ReduceMotion이면 화면 전체 영상은 생략하고 켜짐 연출만 짧게 쓴다.
        /// 같은 Scene 수명에서 한 번 재생했거나 영상 파일이 없으면 재생하지 않는다.
        /// </summary>
        public static bool ShouldPlayVideo(bool isFirstMineEntry, bool reduceMotion, bool videoAvailable, bool alreadyPlayed)
        {
            return isFirstMineEntry && !reduceMotion && videoAvailable && !alreadyPlayed;
        }

        /// <summary>영상은 없지만 첫 진입이라 게임 화면 켜짐 연출만 쓸지.</summary>
        public static bool ShouldPlayTurnOnOnly(bool isFirstMineEntry, bool alreadyPlayed)
        {
            return isFirstMineEntry && !alreadyPlayed;
        }
    }
}
