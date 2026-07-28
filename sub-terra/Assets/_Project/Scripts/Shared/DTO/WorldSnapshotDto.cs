using System;
using System.Collections.Generic;

namespace SubTerra.Shared
{
    /// <summary>
    /// 월드의 전체 변경 상태를 캡처/복원하기 위한 공용 DTO 클래스
    /// </summary>
    [Serializable]
    public class WorldSnapshotDto
    {
        public string version = "1.1";
        public long timestamp;
        public int worldSeed;

        public List<MiningSnapshotDto> miningChanges = new List<MiningSnapshotDto>();
        public List<CollapseSnapshotDto> collapseChanges = new List<CollapseSnapshotDto>();
        public List<BuildingSnapshotDto> buildings = new List<BuildingSnapshotDto>();
        public List<GasSnapshotDto> gasChanges = new List<GasSnapshotDto>();
        public PowerSnapshotDto powerState = new PowerSnapshotDto
        {
            cableConnections = new List<PowerConnectionSnapshotDto>()
        };
    }
}
