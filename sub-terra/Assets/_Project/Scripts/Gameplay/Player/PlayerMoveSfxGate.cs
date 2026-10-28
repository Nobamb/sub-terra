using System;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>
    /// B-164: 점프·착지 소리 판정. 점프와 착지 중 먼저 난 소리에서 0.1초 안에 오는 다른 하나는 생략한다(착지 직후 점프).
    /// 시계는 주입할 수 있고, 기본은 게임 시간이다.
    /// </summary>
    public sealed class PlayerMoveSfxGate
    {
        public const float MinLandHeight = 0.3f;
        public const float OverlapWindow = 0.1f;

        private readonly Func<float> clock;
        private float lastAt;
        private bool played;

        public PlayerMoveSfxGate(Func<float> clock = null)
        {
            this.clock = clock ?? (() => Time.time);
        }

        public SfxId ResolveJump()
        {
            return TryPass() ? SfxId.Jump : SfxId.None;
        }

        /// <param name="fallDistance">떨어진 높이</param>
        /// <param name="damaged">이 착지로 피해를 받았는가(화물 배율은 이미 반영됨)</param>
        /// <param name="usedLadder">사다리를 타고 내려와 닿았는가</param>
        /// <param name="alive">착지 뒤에도 행동할 수 있는가</param>
        public SfxId ResolveLanding(float fallDistance, bool damaged, bool usedLadder, bool alive)
        {
            // 체력 0이면 실패 창 소리에 양보한다.
            if (usedLadder || !alive)
            {
                return SfxId.None;
            }

            var id = damaged
                ? SfxId.LandHeavy
                : fallDistance >= MinLandHeight ? SfxId.Land : SfxId.None;
            if (id == SfxId.None)
            {
                return SfxId.None;
            }

            return TryPass() ? id : SfxId.None;
        }

        private bool TryPass()
        {
            var now = clock();
            if (played && now - lastAt < OverlapWindow)
            {
                return false;
            }

            played = true;
            lastAt = now;
            return true;
        }
    }

    /// <summary>
    /// B-164: 사다리 소리 판정. 트리거에 들어가기만 해도 사다리 모드가 켜지므로
    /// 공중에서 잡았거나 땅에서 켜진 뒤 처음 위아래로 움직였을 때만 잡음 소리를 내고,
    /// 잡음 소리를 냈던 경우에만 놓음 소리를 낸다. 걸음은 A·B를 번갈아 최소 간격을 두고 낸다.
    /// </summary>
    public sealed class LadderSfxGate
    {
        public const float MinStepInterval = 0.18f;

        private readonly Func<float> clock;
        private bool active;
        private bool grabbed;
        private bool stepPlayed;
        private float lastStepAt;
        private bool nextIsB;

        public LadderSfxGate(Func<float> clock = null)
        {
            this.clock = clock ?? (() => Time.time);
        }

        public bool IsGrabbed => grabbed;

        public SfxId OnEnter(bool grounded)
        {
            active = true;
            grabbed = !grounded;
            nextIsB = false;
            return grabbed ? SfxId.LadderGrab : SfxId.None;
        }

        public SfxId OnVerticalMove()
        {
            if (!active || grabbed)
            {
                return SfxId.None;
            }

            grabbed = true;
            return SfxId.LadderGrab;
        }

        /// <param name="silent">OnDisable·구조·위치 복원처럼 소리 없이 끝내는 경우</param>
        public SfxId OnExit(bool silent)
        {
            var release = active && grabbed && !silent;
            active = false;
            grabbed = false;
            return release ? SfxId.LadderGrab : SfxId.None;
        }

        /// <summary>등반 자세가 꼭대기에 닿을 때마다 부른다.</summary>
        public SfxId OnClimbStep()
        {
            var now = clock();
            if (stepPlayed && now - lastStepAt < MinStepInterval)
            {
                return SfxId.None;
            }

            stepPlayed = true;
            lastStepAt = now;
            var id = nextIsB ? SfxId.LadderClimbB : SfxId.LadderClimb;
            nextIsB = !nextIsB;
            return id;
        }
    }
}
