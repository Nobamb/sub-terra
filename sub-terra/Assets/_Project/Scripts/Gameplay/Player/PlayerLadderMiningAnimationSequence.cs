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

    /// <summary>
    /// B-210: 그림 세트별 사다리 채굴 구간(1부터). 비어 있으면(IsSet false) 컨트롤러 설정값을 쓴다.
    /// </summary>
    [Serializable]
    public struct PlayerLadderMiningFrameRanges
    {
        public int startLastFrame;
        public int workFirstFrame;
        public int workLastFrame;
        public int finishFirstFrame;
        public int finishLastFrame;

        public PlayerLadderMiningFrameRanges(int startLast, int workFirst, int workLast, int finishFirst, int finishLast)
        {
            startLastFrame = startLast;
            workFirstFrame = workFirst;
            workLastFrame = workLast;
            finishFirstFrame = finishFirst;
            finishLastFrame = finishLast;
        }

        public bool IsSet => startLastFrame > 0 && workFirstFrame > 0 && workLastFrame >= workFirstFrame
            && finishFirstFrame > 0 && finishLastFrame >= finishFirstFrame;
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
        private int frameCount;
        private PlayerLadderMiningFrameRanges ranges;

        public int FrameIndex { get; private set; }
        public int Direction { get; private set; } = 1;
        public bool IsActive => phase != Phase.Idle;

        /// <summary>B-208: 현재 그림 세트의 실제 장수. 0이면 설정값을 그대로 쓴다.</summary>
        public void SetFrameCount(int count)
        {
            frameCount = Math.Max(0, count);
        }

        /// <summary>B-210: 세트별 구간. 비어 있으면 설정값을 쓴다.</summary>
        public void SetFrameRanges(PlayerLadderMiningFrameRanges frameRanges)
        {
            ranges = frameRanges;
        }

        public int StartLastFrame => ranges.IsSet ? ranges.startLastFrame : settings.startLastFrame;
        public int WorkFirstFrame => ranges.IsSet ? ranges.workFirstFrame : settings.workFirstFrame;
        public int WorkLastFrame => ranges.IsSet ? ranges.workLastFrame : settings.workLastFrame;
        public int FinishFirstFrame => ranges.IsSet ? ranges.finishFirstFrame : settings.finishFirstFrame;

        public int FinishLastFrame
        {
            get
            {
                int last = ranges.IsSet ? ranges.finishLastFrame : settings.finishLastFrame;
                return frameCount > 0 ? Math.Min(last, frameCount) : last;
            }
        }

        public PlayerLadderMiningAnimationSequence(PlayerLadderMiningAnimationSettings settings)
        {
            this.settings = settings;
        }

        public void Start(int direction)
        {
            Direction = direction < 0 ? -1 : 1;
            bool resume = phase != Phase.Finish && Direction == cancelledDirection
                && sinceCancelled <= settings.restartGraceSeconds;
            FrameIndex = resume ? WorkFirstFrame - 1 : 0;
            phase = resume ? Phase.Work : Phase.Start;
            workStep = 1;
            elapsed = 0f;
            sinceCancelled = float.PositiveInfinity;
        }

        public void Complete()
        {
            FrameIndex = FinishFirstFrame - 1;
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
            FrameIndex = WorkFirstFrame - 1;
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
                        if (++FrameIndex >= StartLastFrame)
                        {
                            FrameIndex = WorkFirstFrame - 1;
                            phase = Phase.Work;
                        }
                        break;
                    case Phase.Work:
                        if (FrameIndex >= WorkLastFrame - 1) workStep = -1;
                        else if (FrameIndex <= WorkFirstFrame - 1) workStep = 1;
                        FrameIndex += workStep;
                        break;
                    case Phase.Finish:
                        if (FrameIndex < FinishLastFrame - 1) FrameIndex++;
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
