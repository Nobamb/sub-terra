using System.Collections.Generic;

namespace SubTerra.App.UI.ControlTerminal
{
    /// <summary>
    /// 유휴 시 기능 키를 한 개씩 짧게 눌러 보여 주는 일정표. 한 번에 키 하나만 눌리고 사이에 쉬는 시간이 있다.
    /// 호버·버튼 포커스(blocked)가 생기면 즉시 멈추고, 풀리면 유휴 시간을 처음부터 다시 센다.
    /// 영상 화면은 열지 않는다(눌림 표시만 한다).
    /// </summary>
    public sealed class ControlTerminalAutoDemo
    {
        public const float IdleSeconds = 1.6f;
        public const float PressSeconds = 0.5f;
        public const float GapSeconds = 0.9f;

        private enum Phase
        {
            Idle,
            Press,
            Gap
        }

        private readonly List<string> keys = new List<string>();
        private Phase phase;
        private int index;
        private float timer;
        private bool blocked;
        private bool enabled = true;

        /// <summary>지금 눌러 보이는 키. 쉬는 중이면 null.</summary>
        public string CurrentKey => phase == Phase.Press && index < keys.Count ? keys[index] : null;
        public int KeyCount => keys.Count;
        public bool IsBlocked => blocked;

        public void SetKeys(IReadOnlyList<string> next)
        {
            keys.Clear();
            if (next != null)
            {
                for (int i = 0; i < next.Count; i++) keys.Add(next[i]);
            }

            Restart();
        }

        /// <summary>유휴 시간부터 다시 센다(페이지 교체·등장 완료·재열기).</summary>
        public void Restart()
        {
            phase = Phase.Idle;
            index = 0;
            timer = 0f;
        }

        public void SetEnabled(bool value)
        {
            enabled = value;
            if (!value) Restart();
        }

        public void SetBlocked(bool value)
        {
            if (value == blocked) return;
            blocked = value;
            Restart();
        }

        public void Tick(float deltaSeconds)
        {
            if (!enabled || blocked || keys.Count == 0 || deltaSeconds <= 0f) return;
            timer += deltaSeconds;
            switch (phase)
            {
                case Phase.Idle:
                    if (timer >= IdleSeconds) Begin(Phase.Press);
                    break;
                case Phase.Press:
                    if (timer < PressSeconds) break;
                    index++;
                    if (index >= keys.Count)
                    {
                        index = 0;
                        Begin(Phase.Idle);
                    }
                    else
                    {
                        Begin(Phase.Gap);
                    }

                    break;
                default:
                    if (timer >= GapSeconds) Begin(Phase.Press);
                    break;
            }
        }

        private void Begin(Phase next)
        {
            phase = next;
            timer = 0f;
        }
    }
}
