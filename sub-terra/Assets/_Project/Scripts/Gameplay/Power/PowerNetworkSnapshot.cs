using System;

namespace SubTerra.Gameplay.Power
{
    [Serializable]
    public readonly struct PowerNetworkSnapshot
    {
        public int Supply { get; }
        public int Demand { get; }
        public int ActiveFacilityCount { get; }

        public PowerNetworkSnapshot(int supply, int demand, int activeFacilityCount)
        {
            Supply = supply;
            Demand = demand;
            ActiveFacilityCount = activeFacilityCount;
        }
    }
}
