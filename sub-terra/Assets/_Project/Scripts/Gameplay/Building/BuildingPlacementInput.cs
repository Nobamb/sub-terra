using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.Gameplay.Building
{
    /// <summary>
    /// 테스트·단독 씬용 입력 어댑터.
    /// 통합 씬에서는 GameplayBuildingPlacementBridge가 Preview/확정을 담당한다.
    /// </summary>
    public sealed class BuildingPlacementInput : MonoBehaviour
    {
        [SerializeField] private BuildingPlacementSystem placementSystem;
        [SerializeField] private BuildingPlacementPreview preview;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float enterFacingDirection = 1f;

        private void Update()
        {
            if (placementSystem == null || placementSystem.Selection == null)
            {
                preview?.Hide();
                return;
            }

            // Enter: 플레이어 근접 최적 칸 1회 설치(prompt-B 35 규칙과 동일 경로).
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                placementSystem.TryPlaceNearest(enterFacingDirection);
                return;
            }

            if (Mouse.current == null)
            {
                preview?.Hide();
                return;
            }

            Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToUse == null) return;
            SpriteRenderer sourceRenderer = placementSystem.Selection.RuntimePrefab.GetComponentInChildren<SpriteRenderer>();
            preview?.Configure(sourceRenderer != null ? sourceRenderer.sprite : null);
            Vector3 screen = Mouse.current.position.ReadValue();
            Vector3 world = cameraToUse.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cameraToUse.transform.position.z));
            Vector3Int cell = placementSystem.WorldToCell(world);
            bool isValid = placementSystem.CanPlaceAt(cell, out _);
            preview?.SetCell(GetTerrainTilemap(), cell, isValid);
            if (Mouse.current.leftButton.wasPressedThisFrame && isValid) placementSystem.TryPlaceAt(cell);
        }

        private UnityEngine.Tilemaps.Tilemap GetTerrainTilemap()
        {
            return GetComponent<BuildingPlacementSceneReferences>()?.TerrainTilemap;
        }
    }
}
