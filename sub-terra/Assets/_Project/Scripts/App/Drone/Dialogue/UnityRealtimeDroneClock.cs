using UnityEngine;

namespace SubTerra.App.Drone.Dialogue
{
    /// <summary>일시 정지 중에도 대사 쿨다운이 흐르도록 실시간 시계를 사용한다.</summary>
    public sealed class UnityRealtimeDroneClock : IDroneClock
    {
        public double Now => Time.realtimeSinceStartupAsDouble;
    }
}
