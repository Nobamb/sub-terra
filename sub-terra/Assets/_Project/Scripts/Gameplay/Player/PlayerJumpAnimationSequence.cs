using System;

namespace SubTerra.Gameplay.Player
{
    [Serializable]
    public sealed class PlayerJumpAnimationSettings
    {
        // 그림 번호는 1부터. ready=도약 준비 8장, rise=processing 24장, fall=24장.
        public int takeOffLastFrame = 5;
        public float takeOffFps = 40f;
        public int riseLastFrame = 24;
        public int apexLastFrame = 15;
        public float apexFps = 40f;
        public int fallLoopFirstFrame = 18;
        public int fallLoopLastFrame = 24;
        public float fallLoopFps = 12f;
        public int landFirstFrame = 8;
        public float landFps = 30f;
        // 0이면 끔, 1이면 fall 1~15의 그림 안 출렁임을 전부 되돌린다.
        public float fallBobCompensation;
        // fall 1~15의 장별 발 위치 차이(게임 픽셀, 아래로 +). 정렬 스크립트가 잰 값이다.
        public float[] fallBobOffsets = Array.Empty<float>();
    }

    public enum PlayerJumpPhase { None, TakeOff, Rise, Apex, FallLoop, Land }

    public enum PlayerJumpClip { Ready, Rise, Fall }

    // 점프 그림 단계만 계산한다. 점프 물리·소리는 이 순서에 의존하지 않는다.
    public sealed class PlayerJumpAnimationSequence
    {
        private readonly PlayerJumpAnimationSettings settings;
        private float elapsed;
        private float launchSpeed;
        private float progress;
        private float riseStartProgress;

        public PlayerJumpAnimationSequence(PlayerJumpAnimationSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>#265 연출(착지 먼지 등)이 구독할 단계 변화 알림. (이전, 다음)</summary>
        public event Action<PlayerJumpPhase, PlayerJumpPhase> PhaseChanged;

        public PlayerJumpPhase Phase { get; private set; }
        public PlayerJumpClip Clip { get; private set; } = PlayerJumpClip.Fall;
        public int FrameIndex { get; private set; }
        public bool IsActive => Phase != PlayerJumpPhase.None;
        public bool IsAirborne => Phase != PlayerJumpPhase.None && Phase != PlayerJumpPhase.Land;

        public void StartJump(float launchVerticalSpeed)
        {
            launchSpeed = launchVerticalSpeed;
            progress = 0f;
            riseStartProgress = 0f;
            Show(PlayerJumpClip.Rise, 0);
            Enter(PlayerJumpPhase.TakeOff);
        }

        public void Reset() => Enter(PlayerJumpPhase.None);

        /// <param name="airborne">유예를 거친 공중 여부(사다리 제외).</param>
        public void Tick(float deltaSeconds, bool airborne, float verticalSpeed, bool moveRequested)
        {
            float delta = Math.Max(0f, deltaSeconds);
            if (!airborne)
            {
                if (IsAirborne) BeginLand();
                else if (Phase == PlayerJumpPhase.Land && moveRequested) Enter(PlayerJumpPhase.None);
                else if (Phase == PlayerJumpPhase.Land) Advance(delta);
                return;
            }

            if (!IsAirborne)
            {
                // 점프 없이 발판에서 떨어지면 정점 이후 그림부터 쓴다.
                Show(PlayerJumpClip.Fall, 0);
                Enter(PlayerJumpPhase.Apex);
                return;
            }

            if (Phase == PlayerJumpPhase.TakeOff || Phase == PlayerJumpPhase.Rise)
            {
                // 화물로 낮아진 점프도 정점에서 1이 되도록 시작 속도 대비 남은 속도로 잰다.
                float next = launchSpeed > 0.0001f ? 1f - verticalSpeed / launchSpeed : 1f;
                progress = Math.Max(progress, Clamp01(next));
            }

            if (Phase == PlayerJumpPhase.Rise)
            {
                if (verticalSpeed > 0f) ShowRise();
                // 프레임 간격 때문에 마지막 칸을 건너뛰었으면 정점에서 24를 한 번 보여 준다.
                else if (FrameIndex < settings.riseLastFrame - 1) Show(PlayerJumpClip.Rise, settings.riseLastFrame - 1);
                else BeginApex();
                return;
            }

            Advance(delta);
            if (Phase == PlayerJumpPhase.Rise && verticalSpeed <= 0f) BeginApex();
        }

        /// <summary>현재 그림의 발 위치 보정(게임 픽셀, 아래로 +). 보정 꺼짐이면 0.</summary>
        public float BobOffsetPixels()
        {
            var offsets = settings.fallBobOffsets;
            if (settings.fallBobCompensation <= 0f || Phase != PlayerJumpPhase.Apex
                || offsets == null || FrameIndex >= offsets.Length)
                return 0f;
            return offsets[FrameIndex] * Clamp01(settings.fallBobCompensation);
        }

        private void Advance(float delta)
        {
            elapsed += delta;
            while (IsActive && Phase != PlayerJumpPhase.Rise && elapsed + 0.000001f >= FrameSeconds())
            {
                elapsed = Math.Max(0f, elapsed - FrameSeconds());
                Step();
            }
        }

        private void Step()
        {
            switch (Phase)
            {
                case PlayerJumpPhase.TakeOff:
                    if (FrameIndex < settings.takeOffLastFrame - 1) Show(PlayerJumpClip.Rise, FrameIndex + 1);
                    else BeginRise();
                    break;
                case PlayerJumpPhase.Apex:
                    if (FrameIndex < settings.apexLastFrame - 1) Show(PlayerJumpClip.Fall, FrameIndex + 1);
                    else
                    {
                        Show(PlayerJumpClip.Fall, settings.fallLoopFirstFrame - 1);
                        Enter(PlayerJumpPhase.FallLoop);
                    }
                    break;
                case PlayerJumpPhase.FallLoop:
                    Show(PlayerJumpClip.Fall, FrameIndex < settings.fallLoopLastFrame - 1
                        ? FrameIndex + 1 : settings.fallLoopFirstFrame - 1);
                    break;
                case PlayerJumpPhase.Land:
                    if (FrameIndex > 0) Show(PlayerJumpClip.Ready, FrameIndex - 1);
                    else Enter(PlayerJumpPhase.None);
                    break;
            }
        }

        private void BeginRise()
        {
            // 도약 1~5가 끝난 시점부터 남은 상승을 6~24에 나눠 6번에서 이어 간다.
            riseStartProgress = progress;
            Enter(PlayerJumpPhase.Rise);
            ShowRise();
        }

        private void ShowRise()
        {
            int first = settings.takeOffLastFrame;
            int count = Math.Max(1, settings.riseLastFrame - first);
            float span = 1f - riseStartProgress;
            float local = span > 0.0001f ? Clamp01((progress - riseStartProgress) / span) : 1f;
            int step = Math.Min(count - 1, (int)Math.Floor(local * count));
            Show(PlayerJumpClip.Rise, Math.Max(FrameIndex, first + step));
        }

        private void BeginApex()
        {
            Show(PlayerJumpClip.Fall, 0);
            Enter(PlayerJumpPhase.Apex);
        }

        private void BeginLand()
        {
            Show(PlayerJumpClip.Ready, settings.landFirstFrame - 1);
            Enter(PlayerJumpPhase.Land);
        }

        private void Show(PlayerJumpClip clip, int frameIndex)
        {
            Clip = clip;
            FrameIndex = Math.Max(0, frameIndex);
        }

        private void Enter(PlayerJumpPhase next)
        {
            elapsed = 0f;
            var previous = Phase;
            Phase = next;
            if (previous != next) PhaseChanged?.Invoke(previous, next);
        }

        private float FrameSeconds() => 1f / Math.Max(0.001f, Phase switch
        {
            PlayerJumpPhase.TakeOff => settings.takeOffFps,
            PlayerJumpPhase.Apex => settings.apexFps,
            PlayerJumpPhase.FallLoop => settings.fallLoopFps,
            _ => settings.landFps
        });

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
