using UnityEngine;

namespace SubTerra.App.UI.ControlTerminal
{
    /// <summary>
    /// 기능 키 호버 → 연결선·시연 화면의 표시 상태. 순수 상태 기계라 EditMode에서 검증한다.
    /// 눌림 자체는 호버 즉시 시작하지만, 같은 키에 DwellSeconds 머물러야 연결선과 시연이 시작된다.
    /// 화면은 한 개만 쓰고, 키 사이를 오가도 켜짐을 다시 시작하지 않으며 선과 내용만 교체한다.
    /// </summary>
    public sealed class ControlTerminalHoverState
    {
        public const float DwellSeconds = 0.15f;
        public const float GraceSeconds = 0.10f;
        public const float LineSeconds = 0.12f;
        public const float RetractSeconds = 0.10f;
        public const float ScreenOnSeconds = 0.15f;
        public const float ScreenOffSeconds = 0.10f;
        public const float FadeSeconds = 0.08f;
        public const float DimmedAlpha = 0.3f;

        private string hover;
        private string candidate;
        private string active;
        private float dwell;
        private float grace;
        private float line;
        private float screen;
        private float content = 1f;
        private bool closing;

        /// <summary>포인터가 올라가 있는 기능 키.</summary>
        public string HoverKey => hover;
        /// <summary>0.15초 확인을 기다리는 키. 확인되면 ActiveKey가 된다.</summary>
        public string CandidateKey => candidate;
        /// <summary>연결선이 붙어 있고 시연 내용이 속한 키.</summary>
        public string ActiveKey => active;
        public float LineProgress => line;
        /// <summary>시연 화면 켜짐 정도(0 꺼짐, 1 완전히 켜짐).</summary>
        public float ScreenLevel => screen;
        /// <summary>시연 내용 불투명도. 새 후보를 기다리는 동안 이전 내용은 흐려진다.</summary>
        public float ContentAlpha => content;
        public bool IsClosing => closing;
        public bool IsIdle => active == null && hover == null && candidate == null;

        /// <summary>영상을 진행해도 되는 상태. 후보 확인 대기·켜짐/닫힘 중에는 멈춘다.</summary>
        public bool VideoPlaying => active != null && !closing && candidate == null && screen >= 0.999f;

        public void Enter(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            hover = key;
            grace = 0f;
            closing = false;
            if (key == active)
            {
                candidate = null;
                dwell = 0f;
            }
            else if (candidate != key)
            {
                candidate = key;
                dwell = 0f;
            }
        }

        public void Exit(string key)
        {
            if (hover != key) return;
            hover = null;
            candidate = null;
            dwell = 0f;
        }

        /// <summary>페이지 전환·닫힘·disable 때 대기·선·화면을 즉시 정리한다.</summary>
        public void Reset()
        {
            hover = null;
            candidate = null;
            active = null;
            dwell = 0f;
            grace = 0f;
            line = 0f;
            screen = 0f;
            content = 1f;
            closing = false;
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;
            if (hover != null)
            {
                grace = 0f;
                closing = false;
                if (hover == active)
                {
                    content = Mathf.Min(1f, content + deltaSeconds / FadeSeconds);
                }
                else
                {
                    dwell += deltaSeconds;
                    if (active != null) content = Mathf.Max(DimmedAlpha, content - deltaSeconds / FadeSeconds);
                    if (dwell >= DwellSeconds) Activate(hover);
                }
            }
            else if (active != null)
            {
                content = Mathf.Min(1f, content + deltaSeconds / FadeSeconds);
                grace += deltaSeconds;
                if (grace >= GraceSeconds) closing = true;
            }

            if (active == null) return;
            if (closing)
            {
                if (screen > 0f)
                {
                    screen = Mathf.Max(0f, screen - deltaSeconds / ScreenOffSeconds);
                }
                else
                {
                    line = Mathf.Max(0f, line - deltaSeconds / RetractSeconds);
                    if (line <= 0f)
                    {
                        active = null;
                        closing = false;
                        content = 1f;
                    }
                }

                return;
            }

            line = Mathf.Min(1f, line + deltaSeconds / LineSeconds);
            if (line >= 1f || screen > 0f)
            {
                screen = Mathf.Min(1f, screen + deltaSeconds / ScreenOnSeconds);
            }

            if (content < 1f && hover == active) content = Mathf.Min(1f, content + deltaSeconds / FadeSeconds);
        }

        private void Activate(string key)
        {
            active = key;
            candidate = null;
            dwell = 0f;
            line = 0f;
            // 화면이 이미 켜져 있으면 켜짐은 유지하고 내용만 새로 나타난다.
            content = screen > 0f ? 0f : 1f;
        }
    }
}
