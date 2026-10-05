using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Outpost;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>코어 CCTV 목록 한 줄. 실제 시설 인스턴스 하나와 1:1로 대응한다.</summary>
    public readonly struct CoreCctvFacility
    {
        public string InstanceId { get; }
        public string BuildingId { get; }
        public string DisplayName { get; }

        public CoreCctvFacility(string instanceId, string buildingId, string displayName)
        {
            InstanceId = instanceId ?? string.Empty;
            BuildingId = buildingId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }
    }

    /// <summary>
    /// 코어에 연결된(활성) 시설 목록과 현재 선택. 상태를 바꾸지 않는 표시 전용 모델이라 EditMode에서 검증한다.
    /// 목록 순서는 시설 종류 → 인스턴스 ID 순으로 고정해 갱신마다 뒤바뀌지 않는다.
    /// </summary>
    public sealed class CoreCctvFacilityList
    {
        private readonly List<CoreCctvFacility> items = new List<CoreCctvFacility>();

        public IReadOnlyList<CoreCctvFacility> Items => items;
        public int Count => items.Count;
        public string SelectedInstanceId { get; private set; } = string.Empty;
        public int SelectedIndex => IndexOf(SelectedInstanceId);

        /// <summary>
        /// 스냅샷의 연결 시설로 목록을 갱신한다. 항목이 달라졌으면 true.
        /// 선택한 시설이 남아 있으면 선택을 유지하고, 사라졌으면 같은 자리(없으면 마지막)의 시설로 넘긴다.
        /// </summary>
        public bool Update(IReadOnlyList<OutpostFacilityReadModel> facilities)
        {
            var next = Build(facilities);
            if (SameItems(next))
            {
                return false;
            }

            var previousIndex = SelectedIndex;
            items.Clear();
            items.AddRange(next);

            if (items.Count == 0)
            {
                SelectedInstanceId = string.Empty;
            }
            else if (IndexOf(SelectedInstanceId) < 0)
            {
                var index = previousIndex < 0 ? 0 : Math.Min(previousIndex, items.Count - 1);
                SelectedInstanceId = items[index].InstanceId;
            }

            return true;
        }

        /// <summary>지정 시설을 선택한다. 이미 선택돼 있거나 목록에 없으면 false.</summary>
        public bool Select(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)
                || instanceId == SelectedInstanceId
                || IndexOf(instanceId) < 0)
            {
                return false;
            }

            SelectedInstanceId = instanceId;
            return true;
        }

        /// <summary>새로 열 때의 기본 선택: 안정된 순서의 첫 시설. 없으면 선택 없음.</summary>
        public void ResetSelection()
        {
            SelectedInstanceId = items.Count > 0 ? items[0].InstanceId : string.Empty;
        }

        /// <summary>위(-1)·아래(+1)로 한 칸 이동한다. 양 끝에서는 멈춘다. 바뀌었으면 true.</summary>
        public bool Step(int delta)
        {
            if (items.Count == 0 || delta == 0)
            {
                return false;
            }

            var current = SelectedIndex;
            var target = current < 0
                ? 0
                : Math.Max(0, Math.Min(items.Count - 1, current + (delta > 0 ? 1 : -1)));
            return Select(items[target].InstanceId);
        }

        public int IndexOf(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return -1;
            }

            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].InstanceId == instanceId)
                {
                    return i;
                }
            }

            return -1;
        }

        public void Clear()
        {
            items.Clear();
            SelectedInstanceId = string.Empty;
        }

        private bool SameItems(List<CoreCctvFacility> next)
        {
            if (next.Count != items.Count)
            {
                return false;
            }

            for (var i = 0; i < next.Count; i++)
            {
                if (next[i].InstanceId != items[i].InstanceId
                    || next[i].BuildingId != items[i].BuildingId)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<CoreCctvFacility> Build(IReadOnlyList<OutpostFacilityReadModel> facilities)
        {
            var result = new List<CoreCctvFacility>();
            if (facilities == null)
            {
                return result;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < facilities.Count; i++)
            {
                var facility = facilities[i];
                // 기존 코어 정보창과 같은 기준: 활성(전력 연결)된 시설만 연결된 시설이다.
                if (!facility.IsActive
                    || string.IsNullOrEmpty(facility.InstanceId)
                    || !seen.Add(facility.InstanceId))
                {
                    continue;
                }

                result.Add(new CoreCctvFacility(
                    facility.InstanceId,
                    facility.BuildingId,
                    ItemDisplayNames.Building(facility.BuildingId)));
            }

            result.Sort(Compare);
            return result;
        }

        private static int Compare(CoreCctvFacility left, CoreCctvFacility right)
        {
            var byKind = Rank(left.BuildingId).CompareTo(Rank(right.BuildingId));
            if (byKind != 0)
            {
                return byKind;
            }

            return string.CompareOrdinal(left.InstanceId, right.InstanceId);
        }

        private static int Rank(string buildingId)
        {
            switch (buildingId)
            {
                case DataIds.Buildings.ChargerBasic:
                    return 0;
                case DataIds.Buildings.ClinicBasic:
                    return 1;
                case DataIds.Buildings.SettlementBasic:
                    return 2;
                default:
                    return 3;
            }
        }
    }
}
