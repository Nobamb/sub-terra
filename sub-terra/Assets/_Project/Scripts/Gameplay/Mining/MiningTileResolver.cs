using System;
using System.Collections.Generic;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Mining
{
    public sealed class MiningTileResolver : MonoBehaviour
    {
        [Serializable]
        private struct Entry
        {
            public TileBase tile;
            public MiningTileDto definition;
        }

        [SerializeField] private List<Entry> entries = new();
        private readonly Dictionary<TileBase, MiningTileDto> lookup = new();
        [NonSerialized] private bool initialized;

        public bool TryResolve(TileBase tile, out MiningTileDto definition)
        {
            BuildLookup();
            definition = default;
            return tile != null && lookup.TryGetValue(tile, out definition);
        }

        /// <summary>
        /// 저장 DTO의 영구 tileId로 TileBase를 역조회한다.
        /// 변경 타일 복원 시 사용하며, 매칭이 없으면 false.
        /// </summary>
        public bool TryFindTileById(string tileId, out TileBase tile)
        {
            BuildLookup();
            tile = null;
            if (string.IsNullOrEmpty(tileId)) return false;
            foreach (KeyValuePair<TileBase, MiningTileDto> pair in lookup)
            {
                if (pair.Key != null
                    && string.Equals(pair.Value.tileId, tileId, StringComparison.Ordinal))
                {
                    tile = pair.Key;
                    return true;
                }
            }

            return false;
        }

        public void RegisterRuntime(TileBase tile, MiningTileDto definition)
        {
            if (tile == null) return;
            BuildLookup();
            lookup[tile] = definition;
        }

#if UNITY_EDITOR
        public void EditorSetEntries(TileBase[] tiles, MiningTileDto[] definitions)
        {
            entries.Clear();
            int count = Mathf.Min(tiles?.Length ?? 0, definitions?.Length ?? 0);
            for (int index = 0; index < count; index++)
            {
                entries.Add(new Entry { tile = tiles[index], definition = definitions[index] });
            }

            initialized = false;
        }
#endif

        private void BuildLookup()
        {
            if (initialized && (lookup.Count > 0 || entries.Count == 0)) return;
            initialized = true;
            lookup.Clear();
            foreach (Entry entry in entries)
            {
                if (entry.tile != null) lookup[entry.tile] = entry.definition;
            }
        }
    }
}
