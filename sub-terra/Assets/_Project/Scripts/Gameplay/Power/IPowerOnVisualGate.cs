using System;

namespace SubTerra.Gameplay.Power
{
    /// <summary>
    /// B-206: 시설이 켜짐 연출(PowerOnEffect)을 보여 주기 전에 먼저 끝내야 할 시각 연출(건설)이 있을 때 구현한다.
    /// 전력·상호작용 판정은 건드리지 않고 켜짐 연출의 시작 시점만 미룬다. Power는 Building을 참조하지 않으므로 인터페이스로 잇는다.
    /// </summary>
    public interface IPowerOnVisualGate
    {
        /// <summary>
        /// 아직 진행 중이면 release를 연출이 끝날 때 한 번 부르도록 맡고 true.
        /// 진행 중이 아니면 false(호출한 쪽이 바로 켜짐 연출을 시작한다).
        /// </summary>
        bool TryHold(Action release);
    }
}
