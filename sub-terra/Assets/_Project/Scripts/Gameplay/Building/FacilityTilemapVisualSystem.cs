using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Building
{
    /// <summary>
    /// 이전 빌드가 시설 뒤에 복제한 암석을 정리한다.
    /// 시설은 원본 암석 앞에 직접 그려지고 지형 타일·Collider는 그대로 유지한다.
    /// </summary>
    public sealed class FacilityTilemapVisualSystem : MonoBehaviour
    {
        [SerializeField] private BuildingPlacementSystem buildingPlacementSystem;
        [SerializeField] private Tilemap sourceTerrainTilemap;
        [SerializeField] private Tilemap facilityBackWallTilemap;
        [SerializeField] private Tilemap facilitySurfaceTilemap;
        [SerializeField] private Tilemap facilityForegroundTilemap;
        [SerializeField] private Vector3Int supportingGroundOffset = Vector3Int.down;

        private void OnEnable()
        {
            ClearAll();
            if (buildingPlacementSystem == null) return;
            buildingPlacementSystem.WorldRestorePreparing += ClearAll;
        }

        private void OnDisable()
        {
            if (buildingPlacementSystem == null) return;
            buildingPlacementSystem.WorldRestorePreparing -= ClearAll;
        }

        private void ClearAll()
        {
            facilityBackWallTilemap?.ClearAllTiles();
            facilitySurfaceTilemap?.ClearAllTiles();
            facilityForegroundTilemap?.ClearAllTiles();
        }

    }
}
