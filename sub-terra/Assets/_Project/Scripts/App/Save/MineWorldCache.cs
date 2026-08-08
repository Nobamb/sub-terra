using System;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Save
{
    /// <summary>
    /// 마지막 유효 Mine 월드 스냅샷 런타임 캐시.
    /// Surface Base 등 IWorldSnapshotProvider가 없을 때 빈 world로 세이브를 덮어쓰지 않게 한다.
    /// 엘리베이터 귀환 직전 Capture → 재진입 Restore 경로가 이 캐시를 공유한다.
    /// </summary>
    public sealed class MineWorldCache
    {
        private WorldSnapshotDto cached;

        /// <summary>캐시에 스냅샷이 있으면 true.</summary>
        public bool HasSnapshot => cached != null;

        /// <summary>현재 캐시 복사본. 없으면 null.</summary>
        public WorldSnapshotDto Peek()
        {
            return Clone(cached);
        }

        /// <summary>
        /// Provider 경로 Capture 결과로 캐시를 교체한다.
        /// 의미 있는 변경점이 없어도 Provider가 준 값을 신뢰한다(전부 복구된 맵 등).
        /// null이면 기존 캐시를 유지한다(실패 시 오염 방지).
        /// </summary>
        public void ReplaceFromProvider(WorldSnapshotDto snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            cached = Clone(snapshot);
        }

        /// <summary>
        /// 세이브 로드 결과로 캐시를 시드한다.
        /// 의미 있는 데이터가 있을 때만 승격하고, 빈 world로는 기존 캐시를 지우지 않는다.
        /// 슬롯 전환 시에는 호출 전에 Clear를 먼저 호출한다.
        /// </summary>
        public void SeedFromSave(WorldSnapshotDto snapshot)
        {
            if (!HasMeaningfulContent(snapshot))
            {
                return;
            }

            cached = Clone(snapshot);
        }

        /// <summary>새 게임·슬롯 전환 시 캐시를 비운다.</summary>
        public void Clear()
        {
            cached = null;
        }

        /// <summary>
        /// 저장·복원에 의미가 있는 스냅샷인지 판정한다.
        /// 빈 DTO로 유효 캐시를 덮거나, 빈 스냅샷을 복원 대상으로 쓰지 않기 위한 가드.
        /// </summary>
        public static bool HasMeaningfulContent(WorldSnapshotDto snapshot)
        {
            if (snapshot == null)
            {
                return false;
            }

            if (snapshot.worldSeed != 0)
            {
                return true;
            }

            if (snapshot.miningChanges != null && snapshot.miningChanges.Count > 0)
            {
                return true;
            }

            if (snapshot.changedTiles != null && snapshot.changedTiles.Count > 0)
            {
                return true;
            }

            if (snapshot.collapseChanges != null && snapshot.collapseChanges.Count > 0)
            {
                return true;
            }

            if (snapshot.buildings != null && snapshot.buildings.Count > 0)
            {
                return true;
            }

            if (snapshot.gasChanges != null && snapshot.gasChanges.Count > 0)
            {
                return true;
            }

            if (snapshot.discoveredChunkIds != null && snapshot.discoveredChunkIds.Count > 0)
            {
                return true;
            }

            // powerState는 struct이므로 null 비교 없이 케이블 목록만 본다.
            if (snapshot.powerState.cableConnections != null
                && snapshot.powerState.cableConnections.Count > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>리스트 공유를 막기 위한 깊은 복사(Json 왕복).</summary>
        public static WorldSnapshotDto Clone(WorldSnapshotDto source)
        {
            if (source == null)
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<WorldSnapshotDto>(JsonUtility.ToJson(source));
            }
            catch (Exception)
            {
                // 직렬화 실패 시 원본을 그대로 두지 않고 null로 실패를 드러낸다.
                return null;
            }
        }
    }
}
