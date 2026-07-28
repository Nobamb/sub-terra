using System;
using System.Collections.Generic;

namespace SubTerra.Shared
{
    /// <summary>
    /// 채굴 상태 변경점을 저장하는 DTO
    /// </summary>
    [Serializable]
    public struct MiningSnapshotDto
    {
        public int x;
        public int y;
        public bool isDestroyed;
        public float remainingDurability;
    }

    /// <summary>
    /// 붕괴 및 구조적 안정성 변경점을 저장하는 DTO
    /// </summary>
    [Serializable]
    public struct CollapseSnapshotDto
    {
        public int x;
        public int y;
        public bool isCollapsed;
        public float structuralIntegrity;
    }

    /// <summary>
    /// 건설된 건물의 상태를 저장하는 DTO
    /// </summary>
    [Serializable]
    public struct BuildingSnapshotDto
    {
        public string instanceId;
        public string buildingTypeId;
        public int x;
        public int y;
        public float health;
        public bool isActive;
    }

    /// <summary>
    /// 특정 타일 또는 영역의 가스 농도를 저장하는 DTO
    /// </summary>
    [Serializable]
    public struct GasSnapshotDto
    {
        public string gasZoneId;
        public int x;
        public int y;
        public float concentrationLevel;
        public float remainingDuration;
    }

    /// <summary>
    /// 전체 전력망 및 관련 시스템 상태를 저장하는 DTO
    /// </summary>
    [Serializable]
    public struct PowerSnapshotDto
    {
        public float totalStoredPower;
        public float gridMaxCapacity;
        public bool isGridActive;
        public List<PowerConnectionSnapshotDto> cableConnections;
    }

    /// <summary>
    /// 전력망을 구성하는 두 Runtime 인스턴스 사이의 케이블 연결을 저장하는 DTO
    /// </summary>
    [Serializable]
    public struct PowerConnectionSnapshotDto
    {
        public string nodeAInstanceId;
        public string nodeBInstanceId;
    }
}
