using System;
using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    [Serializable]
    public sealed class SlotFrameMagnetSettings
    {
        [Tooltip("조각이 날아와 맞물리는 시간")]
        public float attachDuration = 0.25f;
        [Tooltip("조각이 풀려 사라지는 시간")]
        public float detachDuration = 0.18f;
        [Tooltip("붙기 시작할 때 카드 바깥 거리(px). 24~40 권장")]
        public float attachDistance = 32f;
        public float detachDistance = 28f;
        [Tooltip("맞물릴 때 튕기는 정도(EaseOutBack 계수)")]
        public float overshoot = 1.7f;
        [Tooltip("붙는 진행도 중 이음새 빛이 번쩍이는 시점(0~1)")]
        public float flashAt = 0.78f;
        public float flashDuration = 0.16f;
    }

    public enum SlotFramePhase
    {
        Off = 0,
        Attaching = 1,
        Attached = 2,
        Detaching = 3
    }

    /// <summary>한 카드 프레임 조각의 바깥 거리·알파·이음새 빛.</summary>
    public readonly struct SlotFrameSample
    {
        public SlotFramePhase Phase { get; }
        /// <summary>제자리에서 바깥쪽으로 떨어진 거리(px). 음수면 맞물리며 살짝 안쪽으로 튕긴 상태.</summary>
        public float Distance { get; }
        public float Alpha { get; }
        public float Flash { get; }

        public SlotFrameSample(SlotFramePhase phase, float distance, float alpha, float flash)
        {
            Phase = phase;
            Distance = distance;
            Alpha = alpha;
            Flash = flash;
        }
    }

    /// <summary>
    /// B-150 자석 철제 프레임. 선택(클릭·위/아래 키)된 카드와 마우스가 올라간 카드에 프레임이 붙는다.
    /// 선택 카드는 항상 하나, 호버 카드도 하나이므로 프레임은 최대 두 카드에만 있다.
    /// 다른 슬롯을 고르면 이전 카드는 떨어지고 새 카드에 붙으며, 연타 시 진행 중 연출을 끊고 넘어간다.
    /// </summary>
    public sealed class SlotFrameMagnetTimeline
    {
        private readonly SlotFrameMagnetSettings settings;
        private readonly SlotFramePhase[] phases;
        private readonly float[] elapsed;
        private int holder = -1;
        private int hover = -1;

        public SlotFrameMagnetTimeline(int slotCount = 3, SlotFrameMagnetSettings settings = null)
        {
            this.settings = settings ?? new SlotFrameMagnetSettings();
            phases = new SlotFramePhase[Mathf.Max(1, slotCount)];
            elapsed = new float[phases.Length];
        }

        public SlotFrameMagnetSettings Settings => settings;
        public int SlotCount => phases.Length;
        /// <summary>선택되어 프레임이 붙어 있거나 붙는 중인 카드. 없으면 -1.</summary>
        public int Holder => holder;
        /// <summary>마우스가 올라가 있어 프레임이 붙는 카드. 없으면 -1.</summary>
        public int Hover => hover;
        public SlotFramePhase PhaseOf(int index) => Valid(index) ? phases[index] : SlotFramePhase.Off;

        /// <summary>붙어 있거나 붙는 중인 카드 수. 선택 1 + 호버 1이라 최대 2다.</summary>
        public int HolderCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < phases.Length; i++)
                    if (phases[i] == SlotFramePhase.Attaching || phases[i] == SlotFramePhase.Attached) count++;
                return count;
            }
        }

        public void Select(int index)
        {
            if (!Valid(index) || index == holder) return;
            holder = index;
            Refresh();
        }

        /// <summary>선택 카드의 프레임을 떼어 낸다. 마우스가 그 카드 위에 있으면 호버 프레임으로 남는다.</summary>
        public void Release()
        {
            if (holder < 0) return;
            holder = -1;
            Refresh();
        }

        /// <summary>마우스가 올라간 카드. 호버는 프레임만 붙이고 선택은 바꾸지 않는다. -1이면 호버 해제.</summary>
        public void SetHover(int index)
        {
            var next = Valid(index) ? index : -1;
            if (next == hover) return;
            hover = next;
            Refresh();
        }

        // 선택·호버 카드는 붙이고, 나머지는 떼어 낸다. 진행 중인 연출은 현재 상태에서 이어 간다.
        private void Refresh()
        {
            for (var i = 0; i < phases.Length; i++)
            {
                var wanted = i == holder || i == hover;
                var attached = phases[i] == SlotFramePhase.Attaching || phases[i] == SlotFramePhase.Attached;
                if (wanted && !attached)
                {
                    var visible = phases[i] == SlotFramePhase.Detaching ? Sample(i).Alpha : 0f;
                    phases[i] = SlotFramePhase.Attaching;
                    // 떨어지던 도중이면 지금 보이는 만큼에서 다시 붙는다.
                    elapsed[i] = visible * settings.attachDuration * 0.45f;
                }
                else if (!wanted && attached)
                {
                    var visible = Sample(i).Alpha;
                    phases[i] = SlotFramePhase.Detaching;
                    // 붙던 도중이면 지금 보이는 만큼에서 이어서 사라진다.
                    elapsed[i] = (1f - visible) * settings.detachDuration;
                }
            }
        }

        /// <summary>건너뛰기·초기 표시용. 선택 카드는 즉시 붙은 상태, 나머지는 꺼진 상태.</summary>
        public void Snap(int index)
        {
            for (var i = 0; i < phases.Length; i++)
            {
                phases[i] = SlotFramePhase.Off;
                elapsed[i] = 0f;
            }

            holder = -1;
            hover = -1;
            if (!Valid(index)) return;
            holder = index;
            phases[index] = SlotFramePhase.Attached;
            elapsed[index] = settings.attachDuration + settings.flashDuration;
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;
            for (var i = 0; i < phases.Length; i++)
            {
                switch (phases[i])
                {
                    case SlotFramePhase.Attaching:
                        elapsed[i] += deltaSeconds;
                        if (elapsed[i] >= settings.attachDuration) phases[i] = SlotFramePhase.Attached;
                        break;
                    case SlotFramePhase.Attached:
                        elapsed[i] = Mathf.Min(elapsed[i] + deltaSeconds, settings.attachDuration + settings.flashDuration);
                        break;
                    case SlotFramePhase.Detaching:
                        elapsed[i] += deltaSeconds;
                        if (elapsed[i] >= settings.detachDuration)
                        {
                            phases[i] = SlotFramePhase.Off;
                            elapsed[i] = 0f;
                        }
                        break;
                }
            }
        }

        public SlotFrameSample Sample(int index)
        {
            if (!Valid(index)) return new SlotFrameSample(SlotFramePhase.Off, settings.detachDistance, 0f, 0f);
            var phase = phases[index];
            var t = elapsed[index];
            switch (phase)
            {
                case SlotFramePhase.Attaching:
                case SlotFramePhase.Attached:
                {
                    var u = Mathf.Clamp01(t / Mathf.Max(0.0001f, settings.attachDuration));
                    var distance = settings.attachDistance * (1f - EaseOutBack(u, settings.overshoot));
                    var alpha = MainMenuBootTimeline.Smooth(MainMenuBootTimeline.Range(u, 0f, 0.45f));
                    var sinceSnap = t - settings.attachDuration * settings.flashAt;
                    var flash = sinceSnap < 0f ? 0f : 1f - MainMenuBootTimeline.Range(sinceSnap, 0f, settings.flashDuration);
                    return new SlotFrameSample(phase, distance, alpha, flash);
                }
                case SlotFramePhase.Detaching:
                {
                    var u = Mathf.Clamp01(t / Mathf.Max(0.0001f, settings.detachDuration));
                    return new SlotFrameSample(phase, settings.detachDistance * u * u, 1f - MainMenuBootTimeline.Smooth(u), 0f);
                }
                default:
                    return new SlotFrameSample(phase, settings.detachDistance, 0f, 0f);
            }
        }

        public static float EaseOutBack(float x, float overshoot)
        {
            var c3 = overshoot + 1f;
            var p = x - 1f;
            return 1f + c3 * p * p * p + overshoot * p * p;
        }

        private bool Valid(int index) => index >= 0 && index < phases.Length;
    }
}
