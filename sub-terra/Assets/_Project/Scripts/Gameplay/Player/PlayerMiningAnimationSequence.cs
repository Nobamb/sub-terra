using System;

namespace SubTerra.Gameplay.Player
{
    [Serializable]
    public sealed class PlayerMiningAnimationSettings
    {
        public float startFps = 20f;
        public float workFps = 12f;
        public float returnFps = 30f;
        public int miningReturnFrame = 5;
        public float completedHoldSeconds = 0.12f;
        public float finishFps = 20f;
        public float cancelMaxSeconds = 0.3f;
        public float restartGraceSeconds = 0.15f;
    }

    // 그림 번호만 계산한다. 채굴 판정·시간·소리는 이 순서에 의존하지 않는다.
    public sealed class PlayerMiningAnimationSequence
    {
        private enum Phase { Idle, Start, Work, Return, Completed, Finish, Cancel }
        private readonly PlayerMiningAnimationSettings settings;
        private Phase phase;
        private float elapsed;
        private float sinceCancelled = float.PositiveInfinity;
        private float cancelFrameSeconds;
        private int resumeFrame = 4;
        private bool completed;

        public PlayerMiningAnimationSequence(PlayerMiningAnimationSettings settings)
        {
            this.settings = settings;
        }

        public int FrameIndex { get; private set; }
        public bool IsActive => phase != Phase.Idle;

        public void Start()
        {
            bool resume = !completed && sinceCancelled <= settings.restartGraceSeconds;
            completed = false;
            FrameIndex = resume ? Math.Max(4, Math.Min(12, resumeFrame)) : 0;
            phase = resume ? Phase.Work : Phase.Start;
            elapsed = 0f;
            sinceCancelled = float.PositiveInfinity;
        }

        public void Complete()
        {
            if (!IsActive) Start();
            completed = true;
            // 역재생·시작 구간에서 깨져도 현재 그림부터 13까지 정재생한다.
            if (phase == Phase.Start || phase == Phase.Return || phase == Phase.Cancel)
                phase = Phase.Work;
            elapsed = 0f;
        }

        public void Stop()
        {
            if (!IsActive || completed || phase == Phase.Cancel) return;
            RememberCancellation();
            phase = Phase.Cancel;
            cancelFrameSeconds = Math.Min(1f / Positive(settings.returnFps),
                Positive(settings.cancelMaxSeconds) / (FrameIndex + 1));
            elapsed = 0f;
        }

        public void Interrupt()
        {
            if (IsActive && !completed) RememberCancellation();
            phase = Phase.Idle;
            elapsed = 0f;
        }

        public void ResumeAfterDamage()
        {
            completed = false;
            phase = Phase.Work;
            FrameIndex = 4;
            elapsed = 0f;
        }

        public void Reset()
        {
            phase = Phase.Idle;
            FrameIndex = 0;
            elapsed = 0f;
            completed = false;
            sinceCancelled = float.PositiveInfinity;
        }

        public void Tick(float deltaSeconds)
        {
            float delta = Math.Max(0f, deltaSeconds);
            sinceCancelled += delta;
            elapsed += delta;
            while (IsActive && elapsed + 0.000001f >= FrameSeconds())
            {
                elapsed = Math.Max(0f, elapsed - FrameSeconds());
                Advance();
            }
        }

        private float FrameSeconds()
        {
            if (phase == Phase.Cancel) return cancelFrameSeconds;
            if (phase == Phase.Completed) return Positive(settings.completedHoldSeconds);
            if (phase == Phase.Finish) return 1f / Positive(settings.finishFps);
            if (completed || phase == Phase.Return) return 1f / Positive(settings.returnFps);
            return 1f / Positive(phase == Phase.Start ? settings.startFps : settings.workFps);
        }

        private void Advance()
        {
            switch (phase)
            {
                case Phase.Start:
                    if (++FrameIndex == 4) phase = Phase.Work;
                    break;
                case Phase.Work:
                    if (FrameIndex < 12) FrameIndex++;
                    else if (completed)
                    {
                        FrameIndex = 13;
                        phase = Phase.Completed;
                    }
                    else
                    {
                        FrameIndex = 11;
                        phase = Phase.Return;
                    }
                    break;
                case Phase.Return:
                    int returnIndex = Math.Max(5, Math.Min(12, settings.miningReturnFrame)) - 1;
                    if (FrameIndex > returnIndex) FrameIndex--;
                    else
                    {
                        FrameIndex++;
                        phase = Phase.Work;
                    }
                    break;
                case Phase.Completed:
                    FrameIndex = 14;
                    phase = Phase.Finish;
                    break;
                case Phase.Finish:
                    if (FrameIndex < 23) FrameIndex++;
                    else phase = Phase.Idle;
                    break;
                case Phase.Cancel:
                    if (FrameIndex > 0) FrameIndex--;
                    else phase = Phase.Idle;
                    break;
            }
        }

        private void RememberCancellation()
        {
            resumeFrame = FrameIndex;
            sinceCancelled = 0f;
        }

        private static float Positive(float value) => Math.Max(0.001f, value);
    }
}
