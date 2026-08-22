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

        public static string Building(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId))
            {
                return string.Empty;
            }

            switch (buildingId)
            {
                case DataIds.Buildings.SupportBasic:
                    return "버팀목";
                case DataIds.Buildings.LadderBasic:
                    return "사다리";
                case DataIds.Buildings.LightBasic:
                    return "조명";
                case DataIds.Buildings.ChargerBasic:
                    return "충전기";
                case DataIds.Buildings.StorageBasic:
                    return "보관함";
                case DataIds.Buildings.SettlementBasic:
                    return "정산 콘솔";
                case DataIds.Buildings.OutpostCoreBasic:
                    return "전진기지 코어";
                case DataIds.Buildings.EmergencyEscapePortal:
                    return "긴급 탈출 포탈";
                default:
                    return buildingId;
            }
        }

        /// <summary>
        /// 버팀목·사다리는 근접 말풍선에서 제외한다. 나머지 설치 시설만 시설명을 띄운다.
        /// </summary>
        public static bool ShowsProximityName(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId))
            {
                return false;
            }

            return buildingId == DataIds.Buildings.LightBasic
                || buildingId == DataIds.Buildings.ChargerBasic
                || buildingId == DataIds.Buildings.StorageBasic
                || buildingId == DataIds.Buildings.SettlementBasic
                || buildingId == DataIds.Buildings.OutpostCoreBasic
                || buildingId == DataIds.Buildings.EmergencyEscapePortal;
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
                case DataIds.Upgrades.MaximumHealth:
                    return "최대 체력";
                case DataIds.Upgrades.HealthRegeneration:
                    return "초당 체력 재생";
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

        /// <summary>
        /// prompt-B 33-4: 장비 업그레이드 상세 설명.
        /// 필요 재료 위에 표시해 각 장비가 무엇을 강화하는지 안내한다.
        /// </summary>
        public static string UpgradeDescription(string upgradeId)
        {
            if (string.IsNullOrEmpty(upgradeId))
            {
                return string.Empty;
            }

            switch (upgradeId)
            {
                case DataIds.Upgrades.DrillSpeed:
                    return "채굴에 걸리는 시간을 줄여 같은 구간을 더 빠르게 뚫습니다. 레벨이 오를수록 더 희귀한 자원을 채취할 수 있습니다.";
                case DataIds.Upgrades.DrillEfficiency:
                    return "채굴 시 소모되는 전력을 줄여 더 오래 탐사할 수 있습니다.";
                case DataIds.Upgrades.MaximumEnergy:
                    return "휴대 가능한 최대 전력량을 늘려 심층 탐사를 안정적으로 유지합니다.";
                case DataIds.Upgrades.MaximumHealth:
                    return "최대 체력을 늘려 붕괴·가스·낙하 피해를 더 오래 버팁니다.";
                case DataIds.Upgrades.HealthRegeneration:
                    return "시간이 지날수록 체력을 자동으로 회복합니다.";
                case DataIds.Upgrades.MaximumCargo:
                    return "한 번에 운반할 수 있는 화물 중량 한도를 늘립니다.";
                case DataIds.Upgrades.DroneScan:
                    return "Digger-Bot이 주변 광물·위험을 감지하는 범위를 확장합니다.";
                case DataIds.Upgrades.DroneRescue:
                    return "탐사 실패 시 미정산 화물 손실을 줄이는 구조 보존 성능을 강화합니다.";
                case DataIds.Upgrades.GasResistance:
                    return "독성 가스 노출 피해와 이동 페널티를 줄여 위험 지대를 견딥니다.";
                default:
                    return "장비 성능을 한 단계 강화합니다.";
            }
        }

        /// <summary>현재 단계에서만 새로 해금되는 채취 자원을 안내한다.</summary>
        public static string UpgradeUnlockDescription(string upgradeId, int currentLevel)
        {
            return upgradeId == DataIds.Upgrades.DrillSpeed && currentLevel == 0
                ? "Lv.1 업그레이드 시 철을 채취할 수 있습니다."
                : string.Empty;
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

            if (permanentId != null && permanentId.StartsWith("building."))
            {
                return Building(permanentId);
            }

            return catalogDisplayName ?? permanentId ?? string.Empty;
        }
    }
}
