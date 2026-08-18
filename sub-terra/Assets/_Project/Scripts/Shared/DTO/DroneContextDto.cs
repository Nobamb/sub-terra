using System;
using System.Collections.Generic;

namespace SubTerra.Shared
{
    /// <summary>
    /// 결정론적 드론 추천에 사용하는 Unity 비의존 실제 상태 DTO
    /// </summary>
    [Serializable]
    public sealed class DroneContextDto
    {
        public int depth;
        public int currentEnergy;
        public int returnEnergyEstimate;
        public float structuralIntegrity;
        public string structuralCauseId = string.Empty;
        public bool structuralTelegraphing;
        public float gasRisk;
        public long unsettledCargoValue;
        public float cargoWeight;
        public float maxCargoWeight;
        public float nearestBaseDistance;
        public List<string> nearbyMineralIds = new List<string>();
        public bool returnPathAvailable;
    }
}
