using System;
using SubTerra.App.Core.Data;
using SubTerra.App.Economy;
using SubTerra.App.UI;
using SubTerra.App.UI.Building;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Integration
{
    [Serializable]
    public sealed class BuildingPlacementBinding
    {
        [SerializeField] private string buildingId;
        [SerializeField] private BuildingPlacementDefinition definition;

        public string BuildingId => buildingId;
        public BuildingPlacementDefinition Definition => definition;
    }

    /// <summary>
    /// B의 메뉴/지갑과 A의 배치 시스템을 연결한다.
    /// A의 CanPlaceAt·TryPlaceAt만 호출하며 지형·구조 계산을 복제하지 않는다.
    /// </summary>
    public sealed class GameplayBuildingPlacementBridge :
        MonoBehaviour,
        IBuildingPlacementPort,
        IBuildingResourceWallet
    {
        [SerializeField] private BuildingPlacementSystem placementSystem;
        [SerializeField] private BuildingPlacementPreview preview;
        [SerializeField] private BuildingPlacementSceneReferences sceneReferences;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private BuildingPlacementBinding[] bindings =
            Array.Empty<BuildingPlacementBinding>();

        private IResourceWallet wallet;
        private GameDataCatalog catalog;
        private string selectedBuildingId = string.Empty;
        private BuildingPlacementState lastState = BuildingPlacementState.None;
        private string lastReason = string.Empty;
        private int lastX = int.MinValue;
        private int lastY = int.MinValue;

        public event Action<BuildingPlacementResultDto> PlacementChanged;

        private void OnEnable()
        {
            if (placementSystem != null)
            {
                placementSystem.BuildingPlaced += OnBuildingPlaced;
                placementSystem.PlacementRejected += OnPlacementRejected;
            }
        }

        private void OnDisable()
        {
            if (placementSystem != null)
            {
                placementSystem.BuildingPlaced -= OnBuildingPlaced;
                placementSystem.PlacementRejected -= OnPlacementRejected;
            }

            ClearRuntimeSelection();
        }

        private void Update()
        {
            if (placementSystem == null || placementSystem.Selection == null)
            {
                preview?.Hide();
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelPreview();
                return;
            }

            // Enter: 커서 위치가 아니라 플레이어 근접 최적 칸에 1회 설치(prompt-B 35).
            // prompt-B 35-3: Enter 배치 직전에 UI 선택을 지워 Submit이 가이드 등을 토글하지 않게 한다.
            if (Keyboard.current != null
                && (Keyboard.current.enterKey.wasPressedThisFrame
                    || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
            {
                UiKeyboardSubmitGuard.ClearSelection();
                TryPlaceNearestByEnter();
                return;
            }

            if (Mouse.current == null)
            {
                preview?.Hide();
                return;
            }

            var cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToUse == null)
            {
                return;
            }

            var screen = Mouse.current.position.ReadValue();
            var world = cameraToUse.ScreenToWorldPoint(
                new Vector3(screen.x, screen.y, -cameraToUse.transform.position.z));
            var cell = placementSystem.WorldToCell(world);
            var canPlace = placementSystem.CanPlaceAt(cell, out var failure);

            // A는 위치 검사를 먼저 하고 마지막에 비용을 확인한다.
            // CannotAfford는 위치 자체는 유효하므로 B 비용 실패와 분리해 표시한다.
            var locationValid = canPlace || failure == BuildingPlacementFailure.CannotAfford;
            var state = locationValid
                ? BuildingPlacementState.Valid
                : BuildingPlacementState.Invalid;
            var reason = ToReasonId(failure);
            PublishIfChanged(state, reason, cell);

            var sourceRenderer = placementSystem.Selection.RuntimePrefab != null
                ? placementSystem.Selection.RuntimePrefab.GetComponentInChildren<SpriteRenderer>()
                : null;
            preview?.Configure(sourceRenderer != null ? sourceRenderer.sprite : null);
            preview?.SetCell(GetTerrainTilemap(), cell, locationValid && CanAfford(selectedBuildingId));

            if (Mouse.current.leftButton.wasPressedThisFrame
                && locationValid
                && CanAfford(selectedBuildingId)
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                placementSystem.TryPlaceAt(cell);
            }
        }

        /// <summary>
        /// Enter 설치: 6칸 이내·CanPlaceAt 통과·최근접(동률 시 발밑→전방→아래/옆).
        /// 후보 없음 → 설치 없이 사유만 Invalid로 알림(선택 유지).
        /// 성공 → TryPlaceAt과 동일하게 비용 1회·선택 해제.
        /// </summary>
        private void TryPlaceNearestByEnter()
        {
            float facing = ResolveFacingDirection();
            if (!placementSystem.TryFindBestPlacementCell(
                    facing,
                    out var cell,
                    out var failure))
            {
                PublishIfChanged(
                    BuildingPlacementState.Invalid,
                    ToReasonId(failure),
                    cell);
                return;
            }

            // 마우스 좌클릭과 동일한 확정 경로(성공 시 비용 1회·선택 해제 이벤트).
            placementSystem.TryPlaceAt(cell);
        }

        private float ResolveFacingDirection()
        {
            if (playerMovement == null)
            {
                playerMovement = FindFirstObjectByType<PlayerMovement>();
            }

            return playerMovement != null ? playerMovement.FacingDirection : 1f;
        }

        public void BindWallet(IResourceWallet resourceWallet, GameDataCatalog dataCatalog)
        {
            wallet = resourceWallet;
            catalog = dataCatalog;
        }

        public bool BeginPreview(string buildingId)
        {
            if (placementSystem == null
                || string.IsNullOrEmpty(buildingId)
                || !TryGetDefinition(buildingId, out var definition)
                || definition == null
                || definition.RuntimePrefab == null)
            {
                return false;
            }

            selectedBuildingId = buildingId;
            placementSystem.Select(definition);
            ResetPublishedState();
            Publish(new BuildingPlacementResultDto
            {
                state = BuildingPlacementState.Previewing,
                buildingId = buildingId
            });
            return true;
        }

        public void CancelPreview()
        {
            var cancelledId = selectedBuildingId;
            var hadSelection = !string.IsNullOrEmpty(cancelledId)
                || (placementSystem != null && placementSystem.Selection != null);
            ClearRuntimeSelection();
            if (hadSelection)
            {
                Publish(new BuildingPlacementResultDto
                {
                    state = BuildingPlacementState.Cancelled,
                    buildingId = cancelledId
                });
            }
        }

        public bool CanAfford(string buildingId)
        {
            return TryGetCosts(buildingId, out var costs)
                && wallet != null
                && wallet.CanAfford(costs);
        }

        public bool TrySpend(string buildingId)
        {
            return TryGetCosts(buildingId, out var costs)
                && wallet != null
                && wallet.TrySpend(costs);
        }

        private void OnBuildingPlaced(BuildingPlacementResult result)
        {
            Publish(new BuildingPlacementResultDto
            {
                state = BuildingPlacementState.Placed,
                buildingId = result.BuildingId,
                instanceId = result.InstanceId,
                x = result.Cell.x,
                y = result.Cell.y
            });
            ClearRuntimeSelection();
        }

        private void OnPlacementRejected(BuildingPlacementResult result)
        {
            Publish(new BuildingPlacementResultDto
            {
                state = BuildingPlacementState.Failed,
                buildingId = result.BuildingId,
                reasonId = ToReasonId(result.Failure),
                x = result.Cell.x,
                y = result.Cell.y
            });
            ClearRuntimeSelection();
        }

        private void PublishIfChanged(
            BuildingPlacementState state,
            string reason,
            Vector3Int cell)
        {
            if (lastState == state
                && lastReason == reason
                && lastX == cell.x
                && lastY == cell.y)
            {
                return;
            }

            lastState = state;
            lastReason = reason;
            lastX = cell.x;
            lastY = cell.y;
            Publish(new BuildingPlacementResultDto
            {
                state = state,
                buildingId = selectedBuildingId,
                reasonId = reason,
                x = cell.x,
                y = cell.y
            });
        }

        private void Publish(BuildingPlacementResultDto result)
        {
            PlacementChanged?.Invoke(result);
        }

        private bool TryGetDefinition(
            string buildingId,
            out BuildingPlacementDefinition definition)
        {
            definition = null;
            if (bindings == null)
            {
                return false;
            }

            for (var i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding != null && binding.BuildingId == buildingId)
                {
                    definition = binding.Definition;
                    return definition != null;
                }
            }

            return false;
        }

        private bool TryGetCosts(string buildingId, out System.Collections.Generic.List<ItemCostDto> costs)
        {
            costs = null;
            if (catalog == null
                || !catalog.TryGetBuilding(buildingId, out var data)
                || data == null)
            {
                return false;
            }

            costs = ItemCostMapping.ToDtoList(data.BuildCosts);
            return true;
        }

        private Tilemap GetTerrainTilemap()
        {
            return sceneReferences != null ? sceneReferences.TerrainTilemap : null;
        }

        private void ClearRuntimeSelection()
        {
            placementSystem?.ClearSelection();
            preview?.Hide();
            selectedBuildingId = string.Empty;
            ResetPublishedState();
        }

        private void ResetPublishedState()
        {
            lastState = BuildingPlacementState.None;
            lastReason = string.Empty;
            lastX = int.MinValue;
            lastY = int.MinValue;
        }

        private static string ToReasonId(BuildingPlacementFailure failure)
        {
            switch (failure)
            {
                case BuildingPlacementFailure.Occupied:
                    return "occupied";
                case BuildingPlacementFailure.MissingGround:
                    return "missing_ground";
                case BuildingPlacementFailure.InvalidDefinition:
                    return "invalid_definition";
                case BuildingPlacementFailure.InstantiateFailed:
                    return "instantiate_failed";
                case BuildingPlacementFailure.SpendFailed:
                    return "spend_failed";
                case BuildingPlacementFailure.CannotAfford:
                    return "cannot_afford";
                case BuildingPlacementFailure.ResourceWalletUnavailable:
                    return "wallet_unavailable";
                case BuildingPlacementFailure.NoSelection:
                    return "no_selection";
                case BuildingPlacementFailure.OutOfRange:
                    return "out_of_range";
                case BuildingPlacementFailure.OutsideAllowedArea:
                    return "outside_allowed_area";
                default:
                    return string.Empty;
            }
        }
    }
}
