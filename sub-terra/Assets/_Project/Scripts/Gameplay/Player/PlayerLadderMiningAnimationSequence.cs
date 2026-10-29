using System;

namespace SubTerra.Gameplay.Player
{
    [Serializable]
    public sealed class PlayerLadderMiningAnimationSettings
    {
        public int startLastFrame = 12;
        public int workFirstFrame = 13;
        public int workLastFrame = 16;
        public int finishFirstFrame = 17;
        public int finishLastFrame = 24;
        public float startFps = 24f;
        public float workFps = 12f;
        public float finishFps = 60f;
        public float cancelMaxSeconds = 0.3f;
        public float restartGraceSeconds = 0.15f;
    }

    // Unity 시간·채굴 판정과 분리된 사다리 전용 그림 순서. 설정의 그림 번호는 1부터다.
    public sealed class PlayerLadderMiningAnimationSequence
    {
        private enum Phase { Idle, Start, Work, Finish, Cancel }
        private readonly PlayerLadderMiningAnimationSettings settings;
        private Phase phase;
        private float elapsed;
        private float sinceCancelled = float.PositiveInfinity;
        private float cancelFrameSeconds;
        private int cancelledDirection;
        private int workStep = 1;

        public int FrameIndex { get; private set; }
        public int Direction { get; private set; } = 1;
        public bool IsActive => phase != Phase.Idle;

        public PlayerLadderMiningAnimationSequence(PlayerLadderMiningAnimationSettings settings)
        {
            this.settings = settings;
        }

        public void Start(int direction)
        {
            Direction = direction < 0 ? -1 : 1;
            bool resume = phase != Phase.Finish && Direction == cancelledDirection
                && sinceCancelled <= settings.restartGraceSeconds;
            FrameIndex = resume ? settings.workFirstFrame - 1 : 0;
            phase = resume ? Phase.Work : Phase.Start;
            workStep = 1;
            elapsed = 0f;
            sinceCancelled = float.PositiveInfinity;
        }

        public void Complete()
        {
            FrameIndex = settings.finishFirstFrame - 1;
            phase = Phase.Finish;
            elapsed = 0f;
            sinceCancelled = float.PositiveInfinity;
        }

        public void Stop()
        {
            if (!IsActive || phase == Phase.Finish || phase == Phase.Cancel) return;
            RememberCancellation();
            cancelFrameSeconds = Positive(settings.cancelMaxSeconds) / (FrameIndex + 1);
            phase = Phase.Cancel;
            elapsed = 0f;
        }

        public void Interrupt()
        {
            if (IsActive && phase != Phase.Finish && phase != Phase.Cancel) RememberCancellation();
            phase = Phase.Idle;
            elapsed = 0f;
        }

        public void ResumeAfterDamage()
        {
            FrameIndex = settings.workFirstFrame - 1;
            phase = Phase.Work;
            workStep = 1;
            elapsed = 0f;
        }

        public void Reset()
        {
            phase = Phase.Idle;
            FrameIndex = 0;
            elapsed = 0f;
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
                switch (phase)
                {
                    case Phase.Start:
                        if (++FrameIndex >= settings.startLastFrame)
                        {
                            FrameIndex = settings.workFirstFrame - 1;
                            phase = Phase.Work;
                        }
                        break;
                    case Phase.Work:
                        if (FrameIndex >= settings.workLastFrame - 1) workStep = -1;
                        else if (FrameIndex <= settings.workFirstFrame - 1) workStep = 1;
                        FrameIndex += workStep;
                        break;
                    case Phase.Finish:
                        if (FrameIndex < settings.finishLastFrame - 1) FrameIndex++;
                        else phase = Phase.Idle;
                        break;
                    case Phase.Cancel:
                        if (FrameIndex > 0) FrameIndex--;
                        else phase = Phase.Idle;
                        break;
                }
            }
        }

        private float FrameSeconds() => phase == Phase.Cancel ? cancelFrameSeconds
            : 1f / Positive(phase == Phase.Start ? settings.startFps
                : phase == Phase.Finish ? settings.finishFps : settings.workFps);

        private void RememberCancellation()
        {
            cancelledDirection = Direction;
            sinceCancelled = 0f;
        }

        private static float Positive(float value) => Math.Max(0.001f, value);
    }
}
