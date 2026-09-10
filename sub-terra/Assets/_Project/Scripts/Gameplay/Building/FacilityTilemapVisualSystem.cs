using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Building
{
    /// <summary>
    /// 시설 본체와 분리된 Tilemap에 시설의 벽면·바닥 시각을 그린다.
    /// 지형의 Collider, 채굴, 배치 판정은 원본 terrain Tilemap만 사용하므로 바꾸지 않는다.
    /// </summary>
    public sealed class FacilityTilemapVisualSystem : MonoBehaviour
    {
        private const string ClinicBuildingId = "building.clinic.basic";

        [SerializeField] private BuildingPlacementSystem buildingPlacementSystem;
        [SerializeField] private Tilemap sourceTerrainTilemap;
        [SerializeField] private Tilemap facilityBackWallTilemap;
        [SerializeField] private Tilemap facilitySurfaceTilemap;
        [SerializeField] private Tilemap facilityForegroundTilemap;
        [SerializeField] private Vector3Int supportingGroundOffset = Vector3Int.down;

        private void OnEnable()
        {
            if (buildingPlacementSystem == null) return;
            buildingPlacementSystem.BuildingPlaced += OnBuildingPlaced;
            buildingPlacementSystem.BuildingRestored += OnBuildingRestored;
            buildingPlacementSystem.WorldRestorePreparing += ClearAll;
        }

        private void OnDisable()
        {
            if (buildingPlacementSystem == null) return;
            buildingPlacementSystem.BuildingPlaced -= OnBuildingPlaced;
            buildingPlacementSystem.BuildingRestored -= OnBuildingRestored;
            buildingPlacementSystem.WorldRestorePreparing -= ClearAll;
        }

        private void OnBuildingPlaced(BuildingPlacementResult result) => Apply(result);
        private void OnBuildingRestored(BuildingPlacementResult result) => Apply(result);

        private void Apply(BuildingPlacementResult result)
        {
            if (!result.IsSuccess
                || !string.Equals(result.BuildingId, ClinicBuildingId, StringComparison.Ordinal)
                || sourceTerrainTilemap == null)
            {
                return;
            }

            Vector3Int groundCell = result.Cell + supportingGroundOffset;
            TileBase supportingTile = sourceTerrainTilemap.GetTile(groundCell);
            if (supportingTile == null) return;

            // 시설이 설치되는 빈 칸에는 같은 광산 타일을 '뒤 벽'으로 깔아,
            // 본체가 빈 검은 공간에 떠 보이지 않도록 한다.
            facilityBackWallTilemap?.SetTile(result.Cell, supportingTile);

            // 바닥은 원본 지형과 같은 타일을 별도 표면 레이어에 복제한다.
            // 이후 시설 종류별 바닥 타일로 바꾸더라도 충돌·채굴 데이터는 안전하다.
            facilitySurfaceTilemap?.SetTile(groundCell, supportingTile);

            // 전경은 현재 비워 둔다. 완성된 얇은 립 전용 타일이 생길 때만
            // 시설 하단을 아주 조금 가리는 용도로 사용한다.
        }

        private void ClearAll()
        {
            facilityBackWallTilemap?.ClearAllTiles();
            facilitySurfaceTilemap?.ClearAllTiles();
            facilityForegroundTilemap?.ClearAllTiles();
        }
    }
}
