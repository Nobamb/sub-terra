using UnityEngine;

namespace SubTerra.App.UI.GameplayCursor
{
    public enum GameplayCursorSheet
    {
        Drill,
        Claw
    }

    /// <summary>한 프레임의 커서 입력. 시설 옮기기 상태는 배치 브리지가 알려 준다.</summary>
    public struct GameplayCursorInput
    {
        public bool LeftHeld;
        public bool MoveMode;
        public bool Carrying;
        public bool HoverGrabbable;
    }

    /// <summary>
    /// 커서 연출 상태(B-151). 드릴 회전 속도·프레임, 집게 프레임, 전환 펄스, 청록 팁 빛 세기만 계산한다.
    /// 입력·채굴·시설 이동 로직은 건드리지 않는 순수 클래스라 EditMode에서 검증한다.
    /// </summary>
    public sealed class GameplayCursorModel
    {
        private readonly GameplayCursorTuning tuning;
        private float flickerPhase;
        private float pulseElapsed = float.MaxValue;
        private bool hasSheet;

        public GameplayCursorModel(GameplayCursorTuning tuning)
        {
            this.tuning = tuning ?? new GameplayCursorTuning();
        }

        /// <summary>0이면 정지(1번 칸), 1이면 최고속(8번 칸).</summary>
        public float Speed { get; private set; }
        public bool LeftHeld { get; private set; }
        public GameplayCursorSheet Sheet { get; private set; }
        public int DrillFrameIndex { get; private set; }
        public int ClawFrameIndex { get; private set; }
        public float GlowLevel { get; private set; }
        public bool ParticlesActive { get; private set; }
        public float PulseScale { get; private set; } = 1f;
        public float RingAlpha { get; private set; }
        public float JitterAmplitude => LeftHeld && Sheet == GameplayCursorSheet.Drill
            ? tuning.jitterPixels * Mathf.Lerp(0.45f, 1f, Speed)
            : 0f;

        public int FrameIndex => Sheet == GameplayCursorSheet.Drill ? DrillFrameIndex : ClawFrameIndex;

        public void Tick(float deltaTime, GameplayCursorInput input)
        {
            var dt = Mathf.Max(0f, deltaTime);
            LeftHeld = input.LeftHeld;

            // 감속 중 다시 누르면 지금 속도에서 이어서 올린다. 시작 속도보다 낮을 때만 시작 속도로 끌어올린다.
            if (LeftHeld)
                Speed = Mathf.Min(1f, Mathf.Max(Speed, tuning.startSpeed) + tuning.spinUpPerSecond * dt);
            else
                Speed = Mathf.Max(0f, Speed - tuning.spinDownPerSecond * dt);

            UpdateDrillFrame(dt);
            UpdateSheet(input.MoveMode, dt);
            ClawFrameIndex = !input.MoveMode ? 0 : input.Carrying ? 3 : input.HoverGrabbable ? 2 : 1;

            var glowTarget = input.HoverGrabbable && !input.Carrying ? 1f : 0f;
            GlowLevel = Mathf.MoveTowards(GlowLevel, glowTarget, tuning.glowFadePerSecond * dt);
            ParticlesActive = glowTarget > 0f;
        }

        private void UpdateDrillFrame(float dt)
        {
            const int last = GameplayCursorSkin.DrillFrameCount - 1;
            if (Speed <= 0.0001f)
            {
                DrillFrameIndex = 0;
                flickerPhase = 0f;
                return;
            }

            // 속도 단계(2~8번) 사이를 오가며 회전처럼 보이게 하고, 빠를수록 전환이 잦아진다.
            var level = 1 + Mathf.Min(last - 1, Mathf.FloorToInt(Speed * last));
            flickerPhase += Mathf.Lerp(tuning.minFlickerFps, tuning.maxFlickerFps, Speed) * dt;
            var alternate = Mathf.FloorToInt(flickerPhase) % 2 == 1;
            DrillFrameIndex = alternate && level > 1 ? level - 1 : level;
        }

        private void UpdateSheet(bool moveMode, float dt)
        {
            var next = moveMode ? GameplayCursorSheet.Claw : GameplayCursorSheet.Drill;
            if (hasSheet && next != Sheet) pulseElapsed = 0f;
            hasSheet = true;
            Sheet = next;

            var duration = Mathf.Max(0.01f, tuning.swapPulseSeconds);
            if (pulseElapsed < duration)
            {
                pulseElapsed += dt;
                var t = Mathf.Clamp01(pulseElapsed / duration);
                var bump = Mathf.Sin(t * Mathf.PI);
                PulseScale = 1f + (tuning.swapPulseScale - 1f) * bump;
                RingAlpha = 1f - t;
            }
            else
            {
                PulseScale = 1f;
                RingAlpha = 0f;
            }
        }
    }
}
