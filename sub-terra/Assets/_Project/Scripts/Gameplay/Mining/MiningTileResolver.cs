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
        private bool initialized;

        public bool TryResolve(TileBase tile, out MiningTileDto definition)
        {
            BuildLookup();
            definition = default;
            return tile != null && lookup.TryGetValue(tile, out definition);
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
            if (initialized) return;
            initialized = true;
            lookup.Clear();
            foreach (Entry entry in entries)
            {
                if (entry.tile != null) lookup[entry.tile] = entry.definition;
            }
        }
    }
}
