using SubTerra.App.Core.Data;
using UnityEngine;

namespace SubTerra.App.UI.Progression
{
    /// <summary>prompt-B 118-1: 업그레이드 노드 호버 시 아이콘 연출 종류.</summary>
    public enum UpgradeIconFxKind
    {
        None = 0,
        DrillSpeed = 1,
        DrillEfficiency = 2,
        MaximumCargo = 3,
        CargoYield = 4,
        CargoGold = 5,
        DroneScan = 6,
        DroneRescue = 7,
        GasResistance = 8,
        MaximumEnergy = 9,
        MaximumHealth = 10,
        HealthRegeneration = 11
    }

    /// <summary>
    /// 아이콘 연출의 시간 곡선. 모든 값은 호버 시작 후 경과 시간(초)의 순수 함수라
    /// 같은 시간에는 항상 같은 모양이 나오고 누적 변형이 생기지 않는다.
    /// </summary>
    public static class UpgradeIconFx
    {
        public const float DrillCycle = 0.42f;
        public const int DrillCycles = 2;
        public const float LidOpenEnd = 0.22f;
        public const float LidCloseStart = 1.2f;
        public const float LidCloseEnd = 1.4f;
        public const float CargoItemDuration = 0.45f;
        public const float SpinSeconds = 0.5f;

        /// <summary>화물 연출에서 구리·철·리튬이 차례로 나타나는 시각.</summary>
        public static readonly float[] CargoItemStarts = { 0.24f, 0.44f, 0.64f };

        /// <summary>드론 구조 보존: 뚜껑이 없으므로 바로 넣기 시작한다.</summary>
        public static readonly float[] DroneItemStarts = { 0.05f, 0.25f, 0.45f };

        public static UpgradeIconFxKind KindFor(string upgradeId)
        {
            switch (upgradeId)
            {
                case DataIds.Upgrades.DrillSpeed: return UpgradeIconFxKind.DrillSpeed;
                case DataIds.Upgrades.DrillEfficiency: return UpgradeIconFxKind.DrillEfficiency;
                case DataIds.Upgrades.MaximumCargo: return UpgradeIconFxKind.MaximumCargo;
                case DataIds.Upgrades.CargoYield: return UpgradeIconFxKind.CargoYield;
                case DataIds.Upgrades.CargoGold: return UpgradeIconFxKind.CargoGold;
                case DataIds.Upgrades.DroneScan: return UpgradeIconFxKind.DroneScan;
                case DataIds.Upgrades.DroneRescue: return UpgradeIconFxKind.DroneRescue;
                case DataIds.Upgrades.GasResistance: return UpgradeIconFxKind.GasResistance;
                case DataIds.Upgrades.MaximumEnergy: return UpgradeIconFxKind.MaximumEnergy;
                case DataIds.Upgrades.MaximumHealth: return UpgradeIconFxKind.MaximumHealth;
                case DataIds.Upgrades.HealthRegeneration: return UpgradeIconFxKind.HealthRegeneration;
                default: return UpgradeIconFxKind.None;
            }
        }

        /// <summary>한 번 재생되는 순서 연출의 길이. 이 시간이 지나면 기본 모양으로 돌아온다.</summary>
        public static float SequenceDuration(UpgradeIconFxKind kind)
        {
            switch (kind)
            {
                case UpgradeIconFxKind.DrillSpeed: return DrillCycle * DrillCycles;
                case UpgradeIconFxKind.MaximumCargo: return LidCloseEnd;
                case UpgradeIconFxKind.CargoYield: return 0.9f;
                case UpgradeIconFxKind.CargoGold: return 0.9f;
                case UpgradeIconFxKind.DroneRescue: return DroneItemStarts[DroneItemStarts.Length - 1] + CargoItemDuration + 0.1f;
                case UpgradeIconFxKind.MaximumHealth: return 0.8f;
                case UpgradeIconFxKind.HealthRegeneration: return 0.8f;
                case UpgradeIconFxKind.DrillEfficiency: return 0.3f;
                default: return 0f;
            }
        }

        /// <summary>드릴 속도: 아래로 찌르고 올라오는 동작을 두 번. 0=제자리, 1=가장 아래.</summary>
        public static float DrillThrust(float t)
        {
            if (t <= 0f || t >= DrillCycle * DrillCycles)
            {
                return 0f;
            }

            var u = Mathf.Repeat(t, DrillCycle) / DrillCycle;
            if (u < 0.4f)
            {
                var a = u / 0.4f;
                return a * a;
            }

            var b = (u - 0.4f) / 0.6f;
            return 1f - (1f - (1f - b) * (1f - b));
        }

        /// <summary>최대 화물 중량: 뚜껑 열림 정도(0=닫힘, 1=열림).</summary>
        public static float LidOpen(float t)
        {
            if (t <= 0f || t >= LidCloseEnd)
            {
                return 0f;
            }

            if (t < LidOpenEnd)
            {
                return EaseOut(t / LidOpenEnd);
            }

            if (t < LidCloseStart)
            {
                return 1f;
            }

            return 1f - EaseIn((t - LidCloseStart) / (LidCloseEnd - LidCloseStart));
        }

        /// <summary>화물 하나의 진행도(0~1). 시작 전이거나 끝났으면 음수.</summary>
        public static float ItemPhase(float t, float start)
        {
            var p = (t - start) / CargoItemDuration;
            return p < 0f || p > 1f ? -1f : p;
        }

        /// <summary>화물이 나타났다가 상자(드론) 안으로 들어가며 점점 투명해지는 불투명도.</summary>
        public static float ItemAlpha(float phase)
        {
            if (phase < 0f)
            {
                return 0f;
            }

            if (phase < 0.18f)
            {
                return phase / 0.18f;
            }

            return phase < 0.5f ? 1f : 1f - (phase - 0.5f) / 0.5f;
        }

        /// <summary>플러그 점등: 처음에 두어 번 깜빡인 뒤 켜진다.</summary>
        public static float PowerOn(float t)
        {
            if (t < 0.05f) return 0f;
            if (t < 0.09f) return 1f;
            if (t < 0.14f) return 0.15f;
            if (t < 0.2f) return 1f;
            if (t < 0.24f) return 0.45f;
            return 1f;
        }

        /// <summary>한 바퀴 회전 진행도(0~1). 가속 → 최고속 → 감속(기지 '새 광산 초기화' 아이콘과 같은 곡선).</summary>
        public static float SpinTurn(float seconds)
        {
            var t = Mathf.Clamp(seconds, 0f, SpinSeconds);
            if (t < 0.2f)
            {
                var u = t / 0.2f;
                return u * u / 3f;
            }

            if (t < 0.3f)
            {
                return 1f / 3f + (t - 0.2f) / 0.1f / 3f;
            }

            var v = (t - 0.3f) / 0.2f;
            return 2f / 3f + (1f - (1f - v) * (1f - v)) / 3f;
        }

        public static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        public static float EaseIn(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }
    }
}
