namespace SubTerra.App.Core.Data
{
    /// <summary>
    /// 영구 ID → 한국어 표시 이름. UI가 mineral.copper 같은 원문을 그대로 노출하지 않도록 한다.
    /// 카탈로그 DisplayName이 비어 있거나 영문일 때 폴백으로 사용한다.
    /// </summary>
    public static class ItemDisplayNames
    {
        public static string Mineral(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return string.Empty;
            }

            switch (itemId)
            {
                case DataIds.Minerals.Copper:
                    return "구리";
                case DataIds.Minerals.Iron:
                    return "철";
                case DataIds.Minerals.Lithium:
                    return "리튬";
                default:
                    return itemId;
            }
        }

        public static string Upgrade(string upgradeId)
        {
            if (string.IsNullOrEmpty(upgradeId))
            {
                return string.Empty;
            }

            switch (upgradeId)
            {
                case DataIds.Upgrades.DrillSpeed:
                    return "드릴 속도";
                case DataIds.Upgrades.DrillEfficiency:
                    return "드릴 전력 효율";
                case DataIds.Upgrades.MaximumEnergy:
                    return "최대 전력";
                case DataIds.Upgrades.MaximumCargo:
                    return "최대 화물 중량";
                case DataIds.Upgrades.DroneScan:
                    return "드론 스캔 범위";
                case DataIds.Upgrades.DroneRescue:
                    return "드론 구조 보존";
                case DataIds.Upgrades.GasResistance:
                    return "가스 저항";
                default:
                    return upgradeId;
            }
        }

        /// <summary>표시용 이름. 카탈로그 이름이 비어 있거나 ID와 같으면 한국어 폴백.</summary>
        public static string PreferDisplay(string permanentId, string catalogDisplayName)
        {
            if (!string.IsNullOrEmpty(catalogDisplayName)
                && catalogDisplayName != permanentId
                && catalogDisplayName.IndexOf('.') < 0)
            {
                // 영문 Copper/Iron 등도 한국어로 통일한다.
                if (catalogDisplayName == "Copper")
                {
                    return "구리";
                }

                if (catalogDisplayName == "Iron")
                {
                    return "철";
                }

                if (catalogDisplayName == "Lithium")
                {
                    return "리튬";
                }

                return catalogDisplayName;
            }

            if (permanentId != null && permanentId.StartsWith("mineral."))
            {
                return Mineral(permanentId);
            }

            if (permanentId != null && permanentId.StartsWith("upgrade."))
            {
                return Upgrade(permanentId);
            }

            return catalogDisplayName ?? permanentId ?? string.Empty;
        }
    }
}
