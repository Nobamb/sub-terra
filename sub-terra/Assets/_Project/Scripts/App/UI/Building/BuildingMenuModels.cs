using System;
using System.Collections.Generic;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.UI.Building
{
    /// <summary>시설 비용과 현재 보유량을 함께 표시하기 위한 읽기 모델.</summary>
    public readonly struct BuildingCostReadModel
    {
        public string ItemId { get; }
        public int Required { get; }
        public int Owned { get; }
        public bool IsEnough => Owned >= Required;

        public BuildingCostReadModel(string itemId, int required, int owned)
        {
            ItemId = itemId ?? string.Empty;
            Required = required;
            Owned = owned < 0 ? 0 : owned;
        }
    }

    /// <summary>BuildingData에서 만든 시설 목록 항목. Gameplay 판정값은 포함하지 않는다.</summary>
    public sealed class BuildingMenuItemReadModel
    {
        public string BuildingId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public int PowerDraw { get; }
        public IReadOnlyList<BuildingCostReadModel> Costs { get; }

        public BuildingMenuItemReadModel(
            string buildingId,
            string displayName,
            string description,
            Sprite icon,
            int powerDraw,
            IReadOnlyList<BuildingCostReadModel> costs)
        {
            BuildingId = buildingId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            Icon = icon;
            PowerDraw = powerDraw < 0 ? 0 : powerDraw;
            Costs = costs ?? Array.Empty<BuildingCostReadModel>();
        }
    }

    /// <summary>
    /// A의 위치 판정과 B의 비용 판정을 조합한 화면 상태.
    /// 위치 유효성 자체는 DTO를 그대로 사용하고 UI에서 지형을 다시 계산하지 않는다.
    /// </summary>
    public readonly struct BuildingAvailabilityReadModel
    {
        public BuildingPlacementState PlacementState { get; }
        public bool CanAfford { get; }
        public string Message { get; }
        public bool CanPlace =>
            PlacementState == BuildingPlacementState.Valid && CanAfford;

        public BuildingAvailabilityReadModel(
            BuildingPlacementState placementState,
            bool canAfford,
            string message)
        {
            PlacementState = placementState;
            CanAfford = canAfford;
            Message = message ?? string.Empty;
        }
    }
}
