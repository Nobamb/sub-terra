namespace SubTerra.App.Integration
{
    /// <summary>
    /// 이어하기 복원 순서: B State 준비 → 월드 복원·파생 재계산 → UI/입력 활성.
    /// 순수 플래그 게이트로 단위 테스트 가능하며, MonoBehaviour 수명과 분리한다.
    /// </summary>
    public sealed class IntegrationActivationGate
    {
        public bool IsStateReady { get; private set; }
        public bool IsWorldRestored { get; private set; }
        public bool IsDerivedRecalculated { get; private set; }
        public bool IsUiActivated { get; private set; }

        /// <summary>UI/입력을 켜도 되는 상태인지. 네 단계가 모두 끝난 뒤에만 true.</summary>
        public bool CanActivateUi =>
            IsStateReady && IsWorldRestored && IsDerivedRecalculated && !IsUiActivated;

        /// <summary>새 게임·탐사 진입처럼 월드 복원이 필요 없을 때 한 번에 준비 완료로 표시한다.</summary>
        public void MarkReadyForNewSession()
        {
            IsStateReady = true;
            IsWorldRestored = true;
            IsDerivedRecalculated = true;
        }

        public void MarkStateReady()
        {
            IsStateReady = true;
        }

        public void MarkWorldRestored()
        {
            if (!IsStateReady)
            {
                return;
            }

            IsWorldRestored = true;
        }

        public void MarkDerivedRecalculated()
        {
            if (!IsWorldRestored)
            {
                return;
            }

            IsDerivedRecalculated = true;
        }

        public bool TryActivateUi()
        {
            if (!CanActivateUi)
            {
                return false;
            }

            IsUiActivated = true;
            return true;
        }

        public void Reset()
        {
            IsStateReady = false;
            IsWorldRestored = false;
            IsDerivedRecalculated = false;
            IsUiActivated = false;
        }
    }
}
