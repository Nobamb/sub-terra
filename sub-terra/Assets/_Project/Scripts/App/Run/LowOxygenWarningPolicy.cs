namespace SubTerra.App.Run
{
    /// <summary>
    /// B-213 산소 부족 드론 경고 발사 규칙(순수 로직).
    /// 산소가 위험(Critical) 아래로 내려가는 순간에만 한 번 발사하고, 위험 위로 회복되면 다시 무장한다.
    /// 처음 관찰(씬 로드·이어하기)이 이미 위험이면 떨어지는 순간이 아니므로 발사하지 않는다.
    /// 전력 상태는 입력으로 받지 않는다(전력만 0이면 이 경고는 없다).
    /// </summary>
    public sealed class LowOxygenWarningPolicy
    {
        private bool initialized;
        private bool armed;

        public bool IsArmed => initialized && armed;

        /// <summary>현재 상태를 기준점으로 삼는다. 이미 위험이면 회복 전까지 발사하지 않는다.</summary>
        public void Reset(bool oxygenCritical)
        {
            initialized = true;
            armed = !oxygenCritical;
        }

        /// <summary>관찰 전 상태로 되돌린다. 다음 관찰이 새 기준점이 된다.</summary>
        public void Clear()
        {
            initialized = false;
            armed = false;
        }

        /// <returns>이번 관찰이 위험으로 내려간 순간이면 true.</returns>
        public bool Observe(bool oxygenCritical)
        {
            if (!initialized)
            {
                Reset(oxygenCritical);
                return false;
            }

            if (!oxygenCritical)
            {
                armed = true;
                return false;
            }

            if (!armed)
            {
                return false;
            }

            armed = false;
            return true;
        }
    }
}
