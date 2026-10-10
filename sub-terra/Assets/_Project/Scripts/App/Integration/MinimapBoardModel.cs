using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>지도 셀 표시 구분. 남은 블록 / 빈 공간(채굴·공기) / 월드 밖 미관측.</summary>
    public enum MinimapCellKind
    {
        Void = 0,
        Empty = 1,
        Block = 2
    }

    public enum MinimapFacilityKind
    {
        Generic = 0,
        Core = 1,
        Charger = 2,
        Clinic = 3,
        Elevator = 4,
        Ladder = 5,
        Support = 6,
        Light = 7,
        Storage = 8,
        Settlement = 9,
        Portal = 10
    }

    /// <summary>지형 셀 조회. 타일 상태를 읽기만 한다.</summary>
    public interface IMinimapTerrainSource
    {
        MinimapCellKind Sample(int x, int y);
    }

    public static class MinimapTerrainClassifier
    {
        public static MinimapCellKind Classify(bool insideWorld, bool hasTile) =>
            !insideWorld ? MinimapCellKind.Void : hasTile ? MinimapCellKind.Block : MinimapCellKind.Empty;
    }

    public sealed class MinimapFacilityRecord
    {
        public string Id;
        public string BuildingId;
        public MinimapFacilityKind Kind;
        /// <summary>셀 좌표 점유 사각형. 좌하단 = 배치 원점, 크기 = 실제 점유 셀 수.</summary>
        public Rect Cells;
        public bool Active = true;
        /// <summary>최초 건설 연출 시작 시각(unscaled). 없으면 음수.</summary>
        public float BuiltAt = -1f;
    }

    /// <summary>
    /// 시설 ID 단위 표시 목록. 다중 셀 시설도 한 번만 등록하고, 같은 ID는 갱신(이동)으로 처리한다.
    /// 건설 연출은 시설 ID당 한 번만 허용한다.
    /// </summary>
    public sealed class MinimapFacilityRegistry
    {
        public const float BuildFlashDuration = 0.6f;
        public const int MaximumDerivedFootprint = 8;

        private readonly List<MinimapFacilityRecord> records = new();
        private readonly Dictionary<string, MinimapFacilityRecord> byId = new();
        private readonly HashSet<string> flashedIds = new();

        public IReadOnlyList<MinimapFacilityRecord> Records => records;
        public int Count => records.Count;
        /// <summary>목록·위치·상태가 바뀔 때마다 증가한다. View는 이 값으로만 재생성 여부를 판단한다.</summary>
        public int Version { get; private set; }

        public static MinimapFacilityKind Classify(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return MinimapFacilityKind.Generic;
            if (buildingId.StartsWith("building.outpost_core")) return MinimapFacilityKind.Core;
            if (buildingId.StartsWith("building.charger")) return MinimapFacilityKind.Charger;
            if (buildingId.StartsWith("building.clinic")) return MinimapFacilityKind.Clinic;
            if (buildingId.StartsWith("building.ladder")) return MinimapFacilityKind.Ladder;
            if (buildingId.StartsWith("building.support")) return MinimapFacilityKind.Support;
            if (buildingId.StartsWith("building.light")) return MinimapFacilityKind.Light;
            if (buildingId.StartsWith("building.storage")) return MinimapFacilityKind.Storage;
            if (buildingId.StartsWith("building.settlement")) return MinimapFacilityKind.Settlement;
            if (buildingId.StartsWith("building.escape_portal")) return MinimapFacilityKind.Portal;
            if (buildingId.StartsWith("elevator")) return MinimapFacilityKind.Elevator;
            return MinimapFacilityKind.Generic;
        }

        /// <summary>배치 원점(좌하단 셀)과 점유 크기. 실제 점유는 회전과 무관하게 +x/+y로 펼쳐진다.</summary>
        public static Rect FootprintCells(int originX, int originY, int width, int height) =>
            new(originX, originY, Mathf.Max(1, width), Mathf.Max(1, height));

        /// <summary>
        /// 런타임 시설 중심(셀 좌표)과 원점 셀로 실제 설치 크기를 역산한다.
        /// BuildingPlacementSystem은 좌하단~우상단 셀 중심의 중간점에 시설을 둔다.
        /// </summary>
        public static bool TryDeriveFootprint(Vector2 centerCell, Vector2Int origin, out Vector2Int footprint)
        {
            int width = Mathf.RoundToInt((centerCell.x - (origin.x + 0.5f)) * 2f) + 1;
            int height = Mathf.RoundToInt((centerCell.y - (origin.y + 0.5f)) * 2f) + 1;
            bool valid = width >= 1 && height >= 1
                && width <= MaximumDerivedFootprint && height <= MaximumDerivedFootprint;
            footprint = valid ? new Vector2Int(width, height) : Vector2Int.one;
            return valid;
        }

        public bool TryGet(string id, out MinimapFacilityRecord record) =>
            byId.TryGetValue(id ?? string.Empty, out record);

        /// <summary>등록 또는 갱신. newlyBuilt는 실제 건설 이벤트일 때만 true다(복원·재열기 제외).</summary>
        public MinimapFacilityRecord Upsert(string id, string buildingId, Rect cells, bool newlyBuilt, float now)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (!byId.TryGetValue(id, out var record))
            {
                record = new MinimapFacilityRecord { Id = id };
                byId[id] = record;
                records.Add(record);
            }

            record.BuildingId = buildingId ?? string.Empty;
            record.Kind = Classify(buildingId);
            record.Cells = cells;
            if (newlyBuilt && flashedIds.Add(id))
            {
                record.BuiltAt = now;
            }

            Version++;
            return record;
        }

        public bool Remove(string id)
        {
            if (string.IsNullOrEmpty(id) || !byId.TryGetValue(id, out var record)) return false;
            byId.Remove(id);
            records.Remove(record);
            Version++;
            return true;
        }

        public bool SetActive(string id, bool active)
        {
            if (!byId.TryGetValue(id ?? string.Empty, out var record) || record.Active == active) return false;
            record.Active = active;
            Version++;
            return true;
        }

        /// <summary>월드 재생성·복원 준비. 건설 시설만 비우고 고정 시설(엘리베이터)은 남긴다.</summary>
        public void ClearBuildings()
        {
            for (int i = records.Count - 1; i >= 0; i--)
            {
                if (records[i].Kind == MinimapFacilityKind.Elevator) continue;
                byId.Remove(records[i].Id);
                records.RemoveAt(i);
            }

            flashedIds.Clear();
            Version++;
        }

        public void Clear()
        {
            records.Clear();
            byId.Clear();
            flashedIds.Clear();
            Version++;
        }

        /// <summary>건설 연출 진행도 0~1. 진행 중이 아니면 -1.</summary>
        public static float FlashProgress(MinimapFacilityRecord record, float now)
        {
            if (record == null || record.BuiltAt < 0f) return -1f;
            float t = (now - record.BuiltAt) / BuildFlashDuration;
            return t >= 0f && t < 1f ? t : -1f;
        }

        /// <summary>창 안에 보이는 포탈이 있으면 내부 회전 때문에 매 프레임 다시 그려야 한다.</summary>
        public bool AnyAnimatedVisible(MinimapWindow window)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].Kind == MinimapFacilityKind.Portal && window.Overlaps(records[i].Cells)) return true;
            }

            return false;
        }

        public bool AnyFlashActive(float now)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (FlashProgress(records[i], now) >= 0f) return true;
            }

            return false;
        }
    }

    /// <summary>채굴 직후 빈 셀의 짧은 가장자리 점등. 셀 제거 자체는 지연하지 않는다.</summary>
    public sealed class MinimapMinedFlashes
    {
        public const float Duration = 0.25f;
        public const int Capacity = 32;

        private readonly Vector2Int[] cells = new Vector2Int[Capacity];
        private readonly float[] times = new float[Capacity];
        private int next;
        private int count;

        public int Count => count;

        public void Add(Vector2Int cell, float now)
        {
            cells[next] = cell;
            times[next] = now;
            next = (next + 1) % Capacity;
            count = Mathf.Min(Capacity, count + 1);
        }

        public void Clear()
        {
            next = 0;
            count = 0;
        }

        public bool TryGet(int index, float now, out Vector2Int cell, out float progress)
        {
            cell = cells[index];
            progress = (now - times[index]) / Duration;
            return index < count && progress >= 0f && progress < 1f;
        }

        public bool AnyActive(float now)
        {
            for (int i = 0; i < count; i++)
            {
                if (TryGet(i, now, out _, out _)) return true;
            }

            return false;
        }
    }
}
