namespace SubTerra.App.UI.MainMenu
{
    public enum QuitDecision
    {
        QuitImmediately = 0,
        SaveThenQuit = 1,
        /// <summary>저장 진행 중이면 종료를 미룬다(데이터 손상 방지).</summary>
        DeferWhileSaving = 2
    }

    /// <summary>종료 전 dirty/저장 중 검사. 플랫폼별 종료 호출은 바인더가 담당한다.</summary>
    public static class QuitPolicy
    {
        public static QuitDecision Decide(bool isDirty, bool saveInProgress)
        {
            if (saveInProgress)
            {
                return QuitDecision.DeferWhileSaving;
            }

            return isDirty ? QuitDecision.SaveThenQuit : QuitDecision.QuitImmediately;
        }
    }
}
