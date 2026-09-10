using System.Collections.Generic;
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
        [SerializeField] private Transform playerOrigin;

        private readonly List<Vector3Int> footprintPreviewCells = new();

        private void Update()
        {
            if (placementSystem == null || placementSystem.Selection == null)
            {
                HidePreview();
                return;
            }

            // C: 플레이어 근접 최적 칸 1회 설치(prompt-B 71).
            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            {
                placementSystem.TryPlaceNearest(enterFacingDirection);
                return;
            }

            if (Mouse.current == null)
            {
                HidePreview();
                return;
            }

            Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToUse == null) return;
            SpriteRenderer sourceRenderer = placementSystem.Selection.RuntimePrefab.GetComponentInChildren<SpriteRenderer>();
            if (preview != null)
            {
                preview.Configure(sourceRenderer != null ? sourceRenderer.sprite : null);
            }
            Vector3 screen = Mouse.current.position.ReadValue();
            Vector3 world = cameraToUse.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cameraToUse.transform.position.z));
            Vector3Int cursorCell = placementSystem.WorldToCell(world);
            float playerWorldX = playerOrigin != null
                ? playerOrigin.position.x
                : transform.position.x;
            Vector3Int origin = BuildingPlacementSystem.ResolveFootprintOrigin(
                cursorCell,
                placementSystem.Selection.Footprint,
                playerWorldX,
                world.x);
            bool isValid = placementSystem.CanPlaceAt(origin, out _);
            placementSystem.GetFootprintCells(origin, footprintPreviewCells);
            if (footprintPreviewCells.Count > 1)
            {
                if (preview != null)
                {
                    preview.SetCells(GetTerrainTilemap(), footprintPreviewCells, isValid);
                }
            }
            else
            {
                if (preview != null)
                {
                    preview.SetCell(GetTerrainTilemap(), origin, isValid);
                }
            }
            UpdateSupportRange(origin);

            if (Mouse.current.leftButton.wasPressedThisFrame && isValid)
            {
                placementSystem.TryPlaceAt(origin);
            }
        }

        private UnityEngine.Tilemaps.Tilemap GetTerrainTilemap()
        {
            return GetComponent<BuildingPlacementSceneReferences>()?.TerrainTilemap;
        }

        private void UpdateSupportRange(Vector3Int origin)
        {
            if (preview == null || placementSystem.Selection == null) return;
            SubTerra.Gameplay.Structural.StructuralSupport support = placementSystem.Selection.RuntimePrefab != null
                ? placementSystem.Selection.RuntimePrefab.GetComponent<SubTerra.Gameplay.Structural.StructuralSupport>()
                : null;
            if (support == null)
            {
                preview.HideSupportRange();
                return;
            }

            preview.SetSupportRange(
                GetTerrainTilemap(),
                origin,
                support.Radius,
                placementSystem.StructuralSystem);
        }

        private void HidePreview()
        {
            // UnityEngine.Object의 파괴 후 래퍼는 C# null이 아닐 수 있으므로
            // null-conditional(?.) 대신 Unity의 null 연산자로 확인한다.
            if (preview != null)
            {
                preview.Hide();
            }
        }
    }
}
