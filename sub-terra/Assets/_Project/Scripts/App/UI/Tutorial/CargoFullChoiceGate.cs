namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 화물 부족 선택(버릴 자원 선택·보상 버리기) 한 번만 통과시키는 문.
    /// 열기 연출 중에는 닫혀 있고, 한쪽을 누르면 다시 열 때까지 두 버튼 모두 잠근다.
    /// </summary>
    public sealed class CargoFullChoiceGate
    {
        private bool ready;
        private bool consumed;

        public bool IsReady => ready && !consumed;
        public bool IsConsumed => consumed;

        /// <summary>새로 열기 시작. 연출이 끝나 Arm하기 전까지 입력을 받지 않는다.</summary>
        public void Reset()
        {
            ready = false;
            consumed = false;
        }

        public void Arm()
        {
            ready = true;
        }

        public void Disarm()
        {
            ready = false;
        }

        public bool TryConsume()
        {
            if (!ready || consumed)
            {
                return false;
            }

            consumed = true;
            return true;
        }
    }
}
