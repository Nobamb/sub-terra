using System;
using System.Collections.Generic;

namespace SubTerra.Shared
{
    /// <summary>
    /// 전진기지에 연결된 시설의 Runtime 판정 결과
    /// </summary>
    [Serializable]
    public sealed class ConnectedFacilityStatusDto
    {
        public string instanceId;
        public string buildingId;
        public bool isActive;
        public string inactiveReasonId;
    }

    /// <summary>
    /// A가 계산한 전진기지 연결·전력·상호작용 상태를 B에 전달하는 DTO
    /// </summary>
    [Serializable]
    public sealed class OutpostStatusDto
    {
        public string outpostInstanceId;
        public bool isActive;
        public bool isInInteractionRange;
        public string inactiveReasonId;
        public float totalPowerSupply;
        public float totalPowerConsumption;
        public List<ConnectedFacilityStatusDto> connectedFacilities =
            new List<ConnectedFacilityStatusDto>();
        public string checkpointId;
        public int checkpointX;
        public int checkpointY;
    }
}
