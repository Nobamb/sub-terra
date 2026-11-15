using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>Represents a placed support pillar or reinforcement in the gameplay layer.</summary>
    public sealed class StructuralSupport : MonoBehaviour
    {
        [SerializeField, Min(0)] private int supportHalfWidthCells = 1;
        [SerializeField, Min(1)] private int supportVerticalReachCells = 3;
        [SerializeField, Min(0)] private int strength = 35;

        public int HalfWidthCells => Mathf.Max(0, supportHalfWidthCells);
        public int VerticalReachCells => Mathf.Max(1, supportVerticalReachCells);
        public int Strength => strength;
        private bool isAvailable;
        private bool availabilityInitialized;

        public bool IsAvailable => availabilityInitialized
            ? isAvailable
            : enabled && gameObject.activeInHierarchy;
        public event Action<StructuralSupport> AvailabilityChanged;

        private void OnEnable()
        {
            availabilityInitialized = true;
            isAvailable = true;
            AvailabilityChanged?.Invoke(this);
        }

        private void OnDisable()
        {
            availabilityInitialized = true;
            isAvailable = false;
            AvailabilityChanged?.Invoke(this);
        }

        public bool Supports(Vector3Int cell, Tilemap tilemap)
        {
            return GetSupportBounds(tilemap, tilemap.WorldToCell(transform.position)).Contains(cell);
        }

        public BoundsInt GetSupportBounds(Tilemap tilemap, Vector3Int braceCell)
        {
            var minimum = braceCell + new Vector3Int(-HalfWidthCells, 1, 0);
            for (int height = 1; height <= VerticalReachCells; height++)
            {
                if (tilemap.HasTile(braceCell + Vector3Int.up * height))
                    return new BoundsInt(minimum, new Vector3Int(HalfWidthCells * 2 + 1, height, 1));
            }

            return new BoundsInt(minimum, Vector3Int.zero);
        }
    }
}
