using System.Collections.Generic;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using UnityEngine;

namespace SubTerra.App.UI.Guide
{
    public readonly struct GuideItemInfo
    {
        public string Id { get; }
        public string Name { get; }
        public Sprite Icon { get; }
        public float UnitWeight { get; }
        public int UnitPrice { get; }
        public bool IsRare { get; }

        public GuideItemInfo(string id, string name, Sprite icon, float unitWeight, int unitPrice, bool isRare)
        {
            Id = id;
            Name = name;
            Icon = icon;
            UnitWeight = unitWeight;
            UnitPrice = unitPrice;
            IsRare = isRare;
        }
    }

    public readonly struct GuideCost
    {
        public string ItemName { get; }
        public int Quantity { get; }

        public GuideCost(string itemName, int quantity)
        {
            ItemName = itemName;
            Quantity = quantity;
        }
    }

    public readonly struct GuideBuildingInfo
    {
        public string Id { get; }
        public string Name { get; }
        public Sprite Icon { get; }
        public string Description { get; }
        public int PowerDraw { get; }
        public IReadOnlyList<GuideCost> Costs { get; }

        public GuideBuildingInfo(string id, string name, Sprite icon, string description, int powerDraw, IReadOnlyList<GuideCost> costs)
        {
            Id = id;
            Name = name;
            Icon = icon;
            Description = description;
            PowerDraw = powerDraw;
            Costs = costs;
        }
    }

    /// <summary>
    /// 가이드가 읽는 게임 데이터 창구. 가격·무게·전력 요구·건설 재료를 코드에 박지 않고 카탈로그에서 읽는다.
    /// </summary>
    public interface IGuideData
    {
        bool TryGetItem(string itemId, out GuideItemInfo info);
        bool TryGetBuilding(string buildingId, out GuideBuildingInfo info);
        IReadOnlyList<GuideBuildingInfo> Buildings { get; }
        bool IsUpgradeMaterial(string itemId);
    }

    /// <summary>GameDataCatalog를 읽는 실제 구현.</summary>
    public sealed class GameDataGuideData : IGuideData
    {
        private readonly GameDataCatalog catalog;
        private List<GuideBuildingInfo> buildings;

        public GameDataGuideData(GameDataCatalog catalog)
        {
            this.catalog = catalog;
        }

        public static IGuideData FromBootstrap()
        {
            var bootstrap = GameBootstrapper.Instance;
            var catalog = bootstrap != null ? bootstrap.AssignedCatalog as GameDataCatalog : null;
            return catalog != null ? new GameDataGuideData(catalog) : null;
        }

        public bool TryGetItem(string itemId, out GuideItemInfo info)
        {
            info = default;
            if (catalog == null || string.IsNullOrEmpty(itemId)
                || !catalog.TryGetInventoryItem(itemId, out var data) || data == null)
            {
                return false;
            }

            var name = ItemDisplayNames.Mineral(itemId);
            if (name == itemId && !string.IsNullOrEmpty(data.DisplayName))
            {
                name = data.DisplayName;
            }

            info = new GuideItemInfo(
                itemId, name, data.Icon, data.UnitWeight, data.UnitPrice, DataIds.RareItems.IsRare(itemId));
            return true;
        }

        public bool TryGetBuilding(string buildingId, out GuideBuildingInfo info)
        {
            info = default;
            if (catalog == null || string.IsNullOrEmpty(buildingId)
                || !catalog.TryGetBuilding(buildingId, out var data) || data == null)
            {
                return false;
            }

            info = Convert(data);
            return true;
        }

        public IReadOnlyList<GuideBuildingInfo> Buildings
        {
            get
            {
                if (buildings == null)
                {
                    buildings = new List<GuideBuildingInfo>();
                    if (catalog != null)
                    {
                        for (var i = 0; i < catalog.Buildings.Count; i++)
                        {
                            if (catalog.Buildings[i] != null)
                            {
                                buildings.Add(Convert(catalog.Buildings[i]));
                            }
                        }
                    }
                }

                return buildings;
            }
        }

        public bool IsUpgradeMaterial(string itemId)
        {
            if (catalog == null || string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            for (var i = 0; i < catalog.Upgrades.Count; i++)
            {
                var upgrade = catalog.Upgrades[i];
                if (upgrade == null || upgrade.Levels == null)
                {
                    continue;
                }

                for (var level = 0; level < upgrade.Levels.Count; level++)
                {
                    var costs = upgrade.Levels[level]?.Costs;
                    for (var c = 0; costs != null && c < costs.Count; c++)
                    {
                        if (costs[c].ItemId == itemId)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static GuideBuildingInfo Convert(BuildingData data)
        {
            var name = ItemDisplayNames.Building(data.Id);
            if (name == data.Id && !string.IsNullOrEmpty(data.DisplayName))
            {
                name = data.DisplayName;
            }

            var costs = new List<GuideCost>();
            for (var i = 0; data.BuildCosts != null && i < data.BuildCosts.Count; i++)
            {
                costs.Add(new GuideCost(ItemDisplayNames.Mineral(data.BuildCosts[i].ItemId), data.BuildCosts[i].Quantity));
            }

            return new GuideBuildingInfo(data.Id, name, data.Icon, data.Description, data.PowerDraw, costs);
        }
    }
}
