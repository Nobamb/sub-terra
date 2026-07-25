using System;
using System.Collections.Generic;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>
    /// Tracks mining-induced instability around a Tilemap and removes a small deterministic
    /// set of unsupported ceiling tiles when the area becomes critical.
    /// </summary>
    public sealed class StructuralIntegritySystem : MonoBehaviour
    {
        [SerializeField] private Tilemap foregroundTilemap;
        [SerializeField, Min(1)] private int scanRadius = 3;
        [SerializeField, Min(1)] private int maximumCollapseTiles = 3;
        [SerializeField, Min(1f)] private float miningImpactMultiplier = 100f;
        [SerializeField] private StructuralSupport[] supports = Array.Empty<StructuralSupport>();

        private readonly Dictionary<Vector3Int, float> accumulatedImpact = new();

        public StructuralRiskLevel CurrentRisk { get; private set; } = StructuralRiskLevel.Stable;
        public event Action<StructuralRiskLevel> RiskChanged;
        public event Action<IReadOnlyList<Vector3Int>> PartialCollapseTriggered;

        public void NotifyTileMined(Vector3Int cell, MiningTileDto tile)
        {
            float impact = Mathf.Max(0f, tile.structuralImpact) * miningImpactMultiplier;
            accumulatedImpact.TryGetValue(cell, out float currentImpact);
            accumulatedImpact[cell] = currentImpact + impact;
            Reevaluate(cell);
        }

        public void RegisterSupport(StructuralSupport support)
        {
            if (support == null || Array.IndexOf(supports, support) >= 0) return;
            Array.Resize(ref supports, supports.Length + 1);
            supports[^1] = support;
        }

        public void UnregisterSupport(StructuralSupport support)
        {
            int index = Array.IndexOf(supports, support);
            if (index < 0) return;
            supports[index] = supports[^1];
            Array.Resize(ref supports, supports.Length - 1);
        }

        public StructuralRiskLevel EvaluateAt(Vector3Int center)
        {
            int unsupportedTiles = CountUnsupportedCeilingTiles(center);
            int supportStrength = GetSupportStrength(center);
            float impact = GetAccumulatedImpact(center);
            return StructuralRiskEvaluator.Evaluate(impact, unsupportedTiles, supportStrength);
        }

        private void Reevaluate(Vector3Int center)
        {
            StructuralRiskLevel risk = EvaluateAt(center);
            SetRisk(risk);
            if (risk != StructuralRiskLevel.Critical) return;

            List<Vector3Int> collapsedCells = CollapseUnsupportedCeiling(center);
            if (collapsedCells.Count > 0)
            {
                PartialCollapseTriggered?.Invoke(collapsedCells);
                SetRisk(EvaluateAt(center));
            }
        }

        private int CountUnsupportedCeilingTiles(Vector3Int center) => FindUnsupportedCeilingTiles(center).Count;

        private List<Vector3Int> FindUnsupportedCeilingTiles(Vector3Int center)
        {
            var cells = new List<Vector3Int>();
            if (foregroundTilemap == null) return cells;

            for (int x = center.x - scanRadius; x <= center.x + scanRadius; x++)
            for (int y = center.y + 1; y <= center.y + scanRadius; y++)
            {
                var cell = new Vector3Int(x, y, center.z);
                if (!foregroundTilemap.HasTile(cell) || foregroundTilemap.HasTile(cell + Vector3Int.down)) continue;
                if (GetSupportStrength(cell) > 0) continue;
                cells.Add(cell);
            }

            cells.Sort((left, right) =>
            {
                int height = right.y.CompareTo(left.y);
                return height != 0 ? height : left.x.CompareTo(right.x);
            });
            return cells;
        }

        private List<Vector3Int> CollapseUnsupportedCeiling(Vector3Int center)
        {
            List<Vector3Int> candidates = FindUnsupportedCeilingTiles(center);
            int count = Mathf.Min(maximumCollapseTiles, candidates.Count);
            var collapsed = new List<Vector3Int>(count);
            for (int index = 0; index < count; index++)
            {
                foregroundTilemap.SetTile(candidates[index], null);
                collapsed.Add(candidates[index]);
            }
            return collapsed;
        }

        private float GetAccumulatedImpact(Vector3Int center)
        {
            float total = 0f;
            foreach (KeyValuePair<Vector3Int, float> pair in accumulatedImpact)
            {
                if (Mathf.Abs(pair.Key.x - center.x) <= scanRadius && Mathf.Abs(pair.Key.y - center.y) <= scanRadius)
                    total += pair.Value;
            }
            return total;
        }

        private int GetSupportStrength(Vector3Int cell)
        {
            if (foregroundTilemap == null) return 0;
            Vector3 worldPosition = foregroundTilemap.GetCellCenterWorld(cell);
            int total = 0;
            foreach (StructuralSupport support in supports)
            {
                if (support != null && support.isActiveAndEnabled && support.Supports(worldPosition))
                    total += support.Strength;
            }
            return total;
        }

        private void SetRisk(StructuralRiskLevel risk)
        {
            if (CurrentRisk == risk) return;
            CurrentRisk = risk;
            RiskChanged?.Invoke(risk);
        }
    }
}
