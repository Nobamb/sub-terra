using System;
using System.Collections.Generic;

namespace SubTerra.Shared
{
    /// <summary>
    /// 월드 변경점 스냅샷 DTO.
    /// 기본 월드는 worldSeed+generatorVersion으로 재생성하고,
    /// 채굴·변경 타일·건물·가스·붕괴·발견 구역·케이블만 변경점으로 저장한다.
    /// Unity Object/Prefab/TileBase 참조를 넣지 않는다.
    /// </summary>
    [Serializable]
    public class WorldSnapshotDto
    {
        public string version = "1.2";
        public long timestamp;
        /// <summary>기본 월드 결정론 생성 Seed.</summary>
        public long worldSeed;
        /// <summary>생성기 버전. 불일치 시 복원은 실패 신호를 낸다.</summary>
        public int generatorVersion = 1;

        public List<MiningSnapshotDto> miningChanges = new List<MiningSnapshotDto>();
        public List<ChangedTileSnapshotDto> changedTiles = new List<ChangedTileSnapshotDto>();
        public List<CollapseSnapshotDto> collapseChanges = new List<CollapseSnapshotDto>();
        /// <summary>사다리·발판·버팀목·시설·코어 등 Runtime 건물. instanceId로 멱등 복원.</summary>
        public List<BuildingSnapshotDto> buildings = new List<BuildingSnapshotDto>();
        public List<GasSnapshotDto> gasChanges = new List<GasSnapshotDto>();
        public List<string> discoveredChunkIds = new List<string>();
        /// <summary>전력 케이블 토폴로지만. 공급/소비 파생값은 복원 후 재계산.</summary>
        public PowerSnapshotDto powerState = new PowerSnapshotDto
        {
            cableConnections = new List<PowerConnectionSnapshotDto>()
        };
    }
}
