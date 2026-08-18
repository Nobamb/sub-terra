using System;
using System.Collections.Generic;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>
    /// 채굴 변경점 주변만 재계산하고 확정된 균열·부분 붕괴 결과를 외부에 전달한다.
    /// prompt-B 36: 위험 점수는 천장 타일 단위로, 가로 localRiskRadius 안의 충격·비지지 개수만 반영한다.
    /// 먼 위치·맞은편 채굴이 다른 구역의 주의 단계를 위험으로 끌어올리지 않는다.
    /// </summary>
    public sealed class StructuralIntegritySystem : MonoBehaviour
    {
        [SerializeField] private Tilemap foregroundTilemap;
        [SerializeField] private StructuralCrackOverlay crackOverlay;
        [SerializeField] private StructuralRiskSettings riskSettings;
        /// <summary>채굴 지점 위쪽으로 비지지 천장을 찾는 세로/탐색 범위.</summary>
        [SerializeField, Min(1)] private int scanRadius = 3;
        /// <summary>
        /// 가로 국소 구역 반경. 충격 합산·비지지 개수·재평가가 이 반경 안에서만 섞인다.
        /// scanRadius와 분리해 맞은편 벽 등 다른 굴착 구역과 가중치가 섞이지 않게 한다.
        /// </summary>
        [SerializeField, Min(0)] private int localRiskRadius = 1;
        [SerializeField, Min(1)] private int maximumCollapseTiles = 3;
        [SerializeField, Min(1f)] private float miningImpactMultiplier = 100f;
        [SerializeField] private long worldSeed = 20260731L;
        [SerializeField] private TileBase[] protectedTiles = Array.Empty<TileBase>();
        [SerializeField] private Vector3Int[] protectedCells = Array.Empty<Vector3Int>();
        [SerializeField] private StructuralSupport[] supports = Array.Empty<StructuralSupport>();

        /// <summary>채굴 셀 → 누적 충격.</summary>
        private readonly Dictionary<Vector3Int, float> accumulatedImpact = new();

        /// <summary>비지지 천장 셀 → 현재 위험 단계(구역 독립).</summary>
        private readonly Dictionary<Vector3Int, StructuralRiskLevel> tileRisks = new();
        private readonly Dictionary<Vector3Int, float> tileScores = new();
        private readonly Dictionary<Vector3Int, StructuralRiskCause> tileCauses = new();
        private readonly HashSet<Vector3Int> deferredCandidates = new();
        private readonly Dictionary<Vector3Int, float> telegraphRemaining = new();
        private readonly List<PendingCollapse> pendingCollapses = new();

        private StructuralRiskSettings runtimeSettings;

        private sealed class PendingCollapse
        {
            public StructuralCollapseEventDto Event;
            public float Remaining;
        }

        public StructuralRiskLevel CurrentRisk { get; private set; } = StructuralRiskLevel.Stable;
        public long WorldSeed => worldSeed;
        public event Action<StructuralRiskLevel> RiskChanged;
        public event Action StructuralStatusChanged;
        public event Action<StructuralCollapseEventDto> CollapseTriggered;

        public void ConfigureWorldSeed(long seed) => worldSeed = seed;

        private void Update()
        {
            AdvanceSimulation(Time.unscaledDeltaTime);
        }

        /// <summary>예고와 낙하 충돌 시점을 한 경로에서 전진시킨다. 테스트도 동일 경로를 사용한다.</summary>
        public void AdvanceSimulation(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            AdvanceTelegraphs(deltaTime);
            AdvancePendingCollapses(deltaTime);
        }

        /// <summary>
        /// prompt-B 36-1: 런타임 충격·위험 타일·균열 표시를 비운다.
        /// 세이브 복원 직전/직후 재계산 전에 호출한다.
        /// </summary>
        public void ClearRuntimeRiskState()
        {
            accumulatedImpact.Clear();
            deferredCandidates.Clear();
            telegraphRemaining.Clear();
            pendingCollapses.Clear();
            if (tileRisks.Count > 0)
            {
                var tracked = new List<Vector3Int>(tileRisks.Keys);
                for (int i = 0; i < tracked.Count; i++)
                {
                    ClearTileRisk(tracked[i]);
                }
            }

            crackOverlay?.ClearAll();
            if (CurrentRisk != StructuralRiskLevel.Stable)
            {
                CurrentRisk = StructuralRiskLevel.Stable;
                RiskChanged?.Invoke(CurrentRisk);
            }
            StructuralStatusChanged?.Invoke();
        }

        /// <summary>
        /// prompt-B 36-1: 복원된 채굴 변경점과 현재 맵 기하를 기준으로 구조 위험을 재구성한다.
        /// 붕괴는 발동하지 않아 로드 중 추가 붕괴가 생기지 않는다.
        /// 기본 충격은 카탈로그 일반 암석(0.25)과 동일한 값을 사용한다.
        /// </summary>
        public void RebuildRiskFromMinedCells(
            IEnumerable<Vector3Int> minedCells,
            float defaultStructuralImpact = 0.25f)
        {
            ClearRuntimeRiskState();
            if (minedCells == null)
            {
                UpdateCurrentRisk();
                return;
            }

            float impact = Mathf.Max(0f, defaultStructuralImpact) * miningImpactMultiplier;
            var affected = new HashSet<Vector3Int>();
            foreach (Vector3Int cell in minedCells)
            {
                if (impact > 0f)
                {
                    accumulatedImpact.TryGetValue(cell, out float currentImpact);
                    accumulatedImpact[cell] = currentImpact + impact;
                }

                foreach (Vector3Int ceiling in EnumerateUnsupportedCeilingsNearMine(cell))
                {
                    affected.Add(ceiling);
                }
            }

            // 충격 없이도 비지지 천장 개수만으로 주의/위험이 될 수 있으므로
            // 추적된 채굴 주변 천장을 전부 재평가한다.
            ReevaluateTiles(
                affected,
                allowCollapse: false,
                Vector3Int.zero,
                StructuralRiskCause.Unsupported,
                0f);
        }

        public void NotifyTileMined(Vector3Int cell, MiningTileDto tile)
        {
            float impact = Mathf.Max(0f, tile.structuralImpact) * miningImpactMultiplier;
            accumulatedImpact.TryGetValue(cell, out float currentImpact);
            accumulatedImpact[cell] = currentImpact + impact;

            var affected = CollectAffectedCeilingTiles(cell);
            ReevaluateTiles(
                affected,
                allowCollapse: true,
                cell,
                StructuralRiskCause.MiningImpact,
                impact);
        }

        public void RegisterSupport(StructuralSupport support)
        {
            if (support == null || Array.IndexOf(supports, support) >= 0) return;
            Array.Resize(ref supports, supports.Length + 1);
            supports[^1] = support;
            support.AvailabilityChanged += OnSupportAvailabilityChanged;
            ReevaluateAffectedBySupport(support, false);
        }

        public void UnregisterSupport(StructuralSupport support)
        {
            int index = Array.IndexOf(supports, support);
            if (index < 0) return;
            support.AvailabilityChanged -= OnSupportAvailabilityChanged;
            supports[index] = supports[^1];
            Array.Resize(ref supports, supports.Length - 1);
            ReevaluateAffectedBySupport(support, true);
        }

        public void RegisterProtectedCell(Vector3Int cell)
        {
            if (Array.IndexOf(protectedCells, cell) >= 0) return;
            Array.Resize(ref protectedCells, protectedCells.Length + 1);
            protectedCells[^1] = cell;
        }

        /// <summary>
        /// 지정 위치 인근(가로 localRiskRadius, 위쪽 scanRadius) 비지지 천장 중 최고 위험 단계.
        /// </summary>
        public StructuralRiskLevel EvaluateAt(Vector3Int center)
        {
            return EvaluateStatusAt(center).Level;
        }

        /// <summary>플레이어 주변의 가장 높은 구조 상태와 그 원인을 반환한다.</summary>
        public StructuralRiskStatus EvaluateStatusAt(Vector3Int center)
        {
            StructuralRiskStatus highest = StructuralRiskStatus.Stable(center);
            foreach (Vector3Int ceiling in EnumerateUnsupportedCeilingsNearMine(center))
            {
                float score = ComputeTileScore(ceiling);
                StructuralRiskLevel scoreLevel = StructuralRiskEvaluator.EvaluateScore(score, Settings);
                bool telegraphing = telegraphRemaining.ContainsKey(ceiling);
                StructuralRiskLevel displayLevel = ToDisplayRisk(scoreLevel, telegraphing);
                if (displayLevel < highest.Level
                    || (displayLevel == highest.Level && score <= highest.Score))
                {
                    continue;
                }

                StructuralRiskCause cause = tileCauses.TryGetValue(ceiling, out StructuralRiskCause value)
                    ? value
                    : StructuralRiskCause.Unsupported;
                highest = new StructuralRiskStatus(
                    ceiling,
                    score,
                    displayLevel,
                    cause,
                    telegraphing);
            }

            return highest;
        }

        public StructuralRiskStatus EvaluateStatusAtWorld(Vector3 worldPosition)
        {
            return foregroundTilemap == null
                ? StructuralRiskStatus.Stable(Vector3Int.zero)
                : EvaluateStatusAt(foregroundTilemap.WorldToCell(worldPosition));
        }

        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            return foregroundTilemap != null
                ? foregroundTilemap.WorldToCell(worldPosition)
                : Vector3Int.FloorToInt(worldPosition);
        }

        public bool HasRiskAtCell(Vector3Int cell)
        {
            return tileRisks.ContainsKey(cell);
        }

        private StructuralRiskSettings Settings
        {
            get
            {
                if (riskSettings != null) return riskSettings;
                if (runtimeSettings == null)
                {
                    runtimeSettings = ScriptableObject.CreateInstance<StructuralRiskSettings>();
                    runtimeSettings.hideFlags = HideFlags.HideAndDontSave;
                }

                return runtimeSettings;
            }
        }

        /// <summary>
        /// 이번 채굴로 점수가 바뀔 수 있는 천장 셀만 모은다.
        /// 원격 주의 타일은 포함하지 않아 재평가·색 덮어쓰기가 발생하지 않는다.
        /// </summary>
        private HashSet<Vector3Int> CollectAffectedCeilingTiles(Vector3Int minedCell)
        {
            var affected = new HashSet<Vector3Int>();

            // 채굴 셀 자체(천장을 직접 캔 경우 위험 고착 해소).
            affected.Add(minedCell);

            foreach (Vector3Int ceiling in EnumerateUnsupportedCeilingsNearMine(minedCell))
            {
                affected.Add(ceiling);
            }

            // 이미 추적 중인 위험 타일 중, 이번 채굴의 가로 국소 범위에 들어오는 것만 재검사.
            int horizontal = Mathf.Max(0, localRiskRadius);
            int vertical = Mathf.Max(scanRadius, horizontal);
            foreach (Vector3Int tracked in tileRisks.Keys)
            {
                if (Mathf.Abs(tracked.x - minedCell.x) <= horizontal
                    && Mathf.Abs(tracked.y - minedCell.y) <= vertical)
                {
                    affected.Add(tracked);
                }
            }

            return affected;
        }

        private void ReevaluateTiles(
            HashSet<Vector3Int> affected,
            bool allowCollapse,
            Vector3Int actionCell,
            StructuralRiskCause actionCause,
            float actionImpact)
        {
            var collapseCandidates = new List<StructuralCollapseCandidate>();

            foreach (Vector3Int cell in affected)
            {
                tileScores.TryGetValue(cell, out float previousScore);
                if (!IsUnsupportedCeiling(cell))
                {
                    ClearTileRisk(cell);
                    continue;
                }

                float score = ComputeTileScore(cell);
                StructuralRiskLevel risk = StructuralRiskEvaluator.EvaluateScore(score, Settings);
                if (risk == StructuralRiskLevel.Stable)
                {
                    ClearTileRisk(cell);
                    continue;
                }

                tileRisks[cell] = risk;
                tileScores[cell] = score;
                StructuralRiskCause cause = ResolveCause(
                    cell,
                    actionCell,
                    actionCause,
                    actionImpact,
                    previousScore);
                tileCauses[cell] = cause;
                float intensity = Mathf.InverseLerp(
                    Settings.CautionThreshold,
                    Settings.CollapseImminentThreshold,
                    score);
                StructuralRiskLevel displayRisk = ToDisplayRisk(
                    risk,
                    telegraphRemaining.ContainsKey(cell));
                crackOverlay?.SetCell(cell, displayRisk, intensity, cause);
                if (score > previousScore + 0.001f && actionCause != StructuralRiskCause.None)
                {
                    crackOverlay?.PulseCell(cell);
                }

                bool newlyCrossed = previousScore < Settings.CollapseImminentThreshold;
                if (allowCollapse
                    && risk == StructuralRiskLevel.CollapseImminent
                    && !telegraphRemaining.ContainsKey(cell)
                    && (newlyCrossed || deferredCandidates.Contains(cell)))
                {
                    collapseCandidates.Add(new StructuralCollapseCandidate(cell, score));
                }
            }

            PruneOrphanImpacts();
            PruneDeferredCandidates();

            if (allowCollapse && collapseCandidates.Count > 0)
            {
                List<Vector3Int> selected = telegraphRemaining.Count > 0
                    ? new List<Vector3Int>()
                    : DeterministicCollapseSelector.Select(
                        collapseCandidates,
                        actionCell,
                        worldSeed,
                        Mathf.Min(maximumCollapseTiles, collapseCandidates.Count));

                var selectedSet = new HashSet<Vector3Int>(selected);
                for (int i = 0; i < collapseCandidates.Count; i++)
                {
                    Vector3Int cell = collapseCandidates[i].Cell;
                    if (selectedSet.Contains(cell)) deferredCandidates.Remove(cell);
                    else deferredCandidates.Add(cell);
                }

                BeginTelegraph(selected);
            }

            UpdateCurrentRisk();
            StructuralStatusChanged?.Invoke();
        }

        private StructuralRiskLevel ComputeTileRisk(Vector3Int ceiling)
        {
            return StructuralRiskEvaluator.EvaluateScore(ComputeTileScore(ceiling), Settings);
        }

        private float ComputeTileScore(Vector3Int ceiling)
        {
            return StructuralRiskEvaluator.CalculateScore(
                GetLocalImpactForCeiling(ceiling),
                CountLocalUnsupportedCeilings(ceiling),
                GetSupportStrength(ceiling),
                Settings);
        }

        /// <summary>
        /// 천장 타일 기준으로 가로 localRiskRadius·세로 scanRadius 안의 채굴 충격만 합산한다.
        /// </summary>
        private float GetLocalImpactForCeiling(Vector3Int ceiling)
        {
            float total = 0f;
            int horizontal = Mathf.Max(0, localRiskRadius);
            int vertical = Mathf.Max(1, scanRadius);
            foreach (KeyValuePair<Vector3Int, float> pair in accumulatedImpact)
            {
                Vector3Int mine = pair.Key;
                if (Mathf.Abs(mine.x - ceiling.x) > horizontal) continue;
                // 천장보다 아래(또는 동일 높이) 채굴만 해당 천장 구조에 기여한다.
                if (mine.y > ceiling.y) continue;
                if (ceiling.y - mine.y > vertical) continue;
                total += pair.Value;
            }

            return total;
        }

        /// <summary>천장 타일 주변 가로 국소 반경의 비지지 천장 개수.</summary>
        private int CountLocalUnsupportedCeilings(Vector3Int ceiling)
        {
            int count = 0;
            int radius = Mathf.Max(0, localRiskRadius);
            for (int x = ceiling.x - radius; x <= ceiling.x + radius; x++)
            for (int y = ceiling.y - radius; y <= ceiling.y + radius; y++)
            {
                var cell = new Vector3Int(x, y, ceiling.z);
                if (IsUnsupportedCeiling(cell)) count++;
            }

            return count;
        }

        /// <summary>
        /// 채굴 지점 위·옆으로 비지지 천장을 열거한다.
        /// 가로는 localRiskRadius, 세로는 scanRadius로 분리한다.
        /// </summary>
        private IEnumerable<Vector3Int> EnumerateUnsupportedCeilingsNearMine(Vector3Int mine)
        {
            if (foregroundTilemap == null) yield break;

            int horizontal = Mathf.Max(0, localRiskRadius);
            int vertical = Mathf.Max(1, scanRadius);
            for (int x = mine.x - horizontal; x <= mine.x + horizontal; x++)
            for (int y = mine.y + 1; y <= mine.y + vertical; y++)
            {
                var cell = new Vector3Int(x, y, mine.z);
                if (IsUnsupportedCeiling(cell)) yield return cell;
            }
        }

        private bool IsUnsupportedCeiling(Vector3Int cell)
        {
            if (foregroundTilemap == null || !foregroundTilemap.HasTile(cell)) return false;
            if (foregroundTilemap.HasTile(cell + Vector3Int.down)) return false;
            if (IsProtected(cell)) return false;
            if (GetSupportStrength(cell) > 0) return false;
            return true;
        }

        private void ClearTileRisk(Vector3Int cell)
        {
            bool hadRisk = tileRisks.Remove(cell);
            tileScores.Remove(cell);
            tileCauses.Remove(cell);
            deferredCandidates.Remove(cell);
            telegraphRemaining.Remove(cell);
            crackOverlay?.SetTelegraphing(cell, false);
            if (hadRisk)
            {
                crackOverlay?.ClearCell(cell);
            }
        }

        private StructuralRiskCause ResolveCause(
            Vector3Int ceiling,
            Vector3Int actionCell,
            StructuralRiskCause actionCause,
            float actionImpact,
            float previousScore)
        {
            if (actionCause == StructuralRiskCause.SupportRemoved)
                return StructuralRiskCause.SupportRemoved;
            if (actionCause != StructuralRiskCause.MiningImpact)
                return tileCauses.TryGetValue(ceiling, out StructuralRiskCause prior)
                    ? prior
                    : StructuralRiskCause.Unsupported;

            float unsupportedDelta = previousScore <= 0f
                && ceiling + Vector3Int.down == actionCell
                    ? Settings.UnsupportedTileWeight
                    : 0f;
            return unsupportedDelta >= actionImpact && unsupportedDelta > 0f
                ? StructuralRiskCause.Unsupported
                : StructuralRiskCause.MiningImpact;
        }

        /// <summary>주변에 비지지 천장이 더 이상 없는 채굴 충격을 제거한다.</summary>
        private void PruneOrphanImpacts()
        {
            if (accumulatedImpact.Count == 0) return;

            var remove = new List<Vector3Int>();
            foreach (Vector3Int mine in accumulatedImpact.Keys)
            {
                if (!HasPotentialCeilingNearMine(mine)) remove.Add(mine);
            }

            for (int i = 0; i < remove.Count; i++)
            {
                accumulatedImpact.Remove(remove[i]);
            }
        }

        /// <summary>버팀목은 위험 후보만 끄며 과거 채굴 충격 기록까지 지우지는 않는다.</summary>
        private bool HasPotentialCeilingNearMine(Vector3Int mine)
        {
            if (foregroundTilemap == null) return false;
            int horizontal = Mathf.Max(0, localRiskRadius);
            int vertical = Mathf.Max(1, scanRadius);
            for (int x = mine.x - horizontal; x <= mine.x + horizontal; x++)
            for (int y = mine.y + 1; y <= mine.y + vertical; y++)
            {
                var cell = new Vector3Int(x, y, mine.z);
                if (foregroundTilemap.HasTile(cell)
                    && !foregroundTilemap.HasTile(cell + Vector3Int.down)
                    && !IsProtected(cell))
                {
                    return true;
                }
            }

            return false;
        }

        private void PruneDeferredCandidates()
        {
            if (deferredCandidates.Count == 0) return;
            var remove = new List<Vector3Int>();
            foreach (Vector3Int cell in deferredCandidates)
            {
                if (!IsUnsupportedCeiling(cell)
                    || ComputeTileScore(cell) < Settings.CollapseImminentThreshold)
                {
                    remove.Add(cell);
                }
            }

            for (int i = 0; i < remove.Count; i++)
                deferredCandidates.Remove(remove[i]);
        }

        private void BeginTelegraph(IReadOnlyList<Vector3Int> selected)
        {
            if (selected == null || selected.Count == 0) return;
            float duration = Mathf.Max(0.01f, Settings.CollapseTelegraphSeconds);
            for (int i = 0; i < selected.Count; i++)
            {
                Vector3Int cell = selected[i];
                telegraphRemaining[cell] = duration;
                crackOverlay?.SetTelegraphing(cell, true);
                if (tileScores.TryGetValue(cell, out float score))
                {
                    StructuralRiskCause cause = tileCauses.TryGetValue(cell, out StructuralRiskCause value)
                        ? value
                        : StructuralRiskCause.Unsupported;
                    crackOverlay?.SetCell(cell, StructuralRiskLevel.CollapseImminent, 1f, cause);
                }
            }

            UpdateCurrentRisk();
            StructuralStatusChanged?.Invoke();
        }

        private void AdvanceTelegraphs(float deltaTime)
        {
            if (telegraphRemaining.Count == 0) return;
            var expired = new List<Vector3Int>();
            var cancelled = new List<Vector3Int>();
            var cells = new List<Vector3Int>(telegraphRemaining.Keys);
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3Int cell = cells[i];
                if (!IsUnsupportedCeiling(cell))
                {
                    cancelled.Add(cell);
                    continue;
                }

                float remaining = telegraphRemaining[cell] - deltaTime;
                if (remaining <= 0f) expired.Add(cell);
                else telegraphRemaining[cell] = remaining;
            }

            for (int i = 0; i < cancelled.Count; i++)
                ClearTileRisk(cancelled[i]);
            for (int i = 0; i < expired.Count; i++)
            {
                telegraphRemaining.Remove(expired[i]);
                crackOverlay?.SetTelegraphing(expired[i], false);
            }

            if (expired.Count > 0)
                CollapseUnsupportedCeiling(expired);
            else if (cancelled.Count > 0)
            {
                UpdateCurrentRisk();
                StructuralStatusChanged?.Invoke();
            }
        }

        private StructuralCollapseEventDto CollapseUnsupportedCeiling(
            IReadOnlyList<Vector3Int> selected)
        {
            var collapse = new StructuralCollapseEventDto
            {
                worldSeed = worldSeed,
                severity = ResolveSeverity(selected.Count)
            };
            for (int i = 0; i < selected.Count; i++)
            {
                Vector3Int cell = selected[i];
                if (!IsUnsupportedCeiling(cell)) continue;
                crackOverlay?.PlayCollapse(cell, foregroundTilemap, Settings.CollapseFallSeconds);
                foregroundTilemap.SetTile(cell, null);
                collapse.cells.Add(new CollapseCellDto { x = cell.x, y = cell.y });
                ClearTileRisk(cell);
            }

            if (collapse.cells.Count == 0)
            {
                UpdateCurrentRisk();
                StructuralStatusChanged?.Invoke();
                return collapse;
            }

            collapse.severity = ResolveSeverity(collapse.cells.Count);
            foregroundTilemap.RefreshAllTiles();
            TilemapCollider2D collider = foregroundTilemap.GetComponent<TilemapCollider2D>();
            if (collider != null && collider.hasTilemapChanges)
                collider.ProcessTilemapChanges();

            pendingCollapses.Add(new PendingCollapse
            {
                Event = collapse,
                Remaining = Mathf.Max(0.01f, Settings.CollapseFallSeconds)
            });

            var affected = new HashSet<Vector3Int>();
            for (int i = 0; i < collapse.cells.Count; i++)
            {
                var removed = new Vector3Int(collapse.cells[i].x, collapse.cells[i].y, 0);
                affected.Add(removed);
                foreach (Vector3Int nearby in EnumerateUnsupportedCeilingsNearMine(removed))
                    affected.Add(nearby);
            }
            ReevaluateTiles(
                affected,
                allowCollapse: false,
                Vector3Int.zero,
                StructuralRiskCause.None,
                0f);
            return collapse;
        }

        private void AdvancePendingCollapses(float deltaTime)
        {
            for (int i = pendingCollapses.Count - 1; i >= 0; i--)
            {
                PendingCollapse pending = pendingCollapses[i];
                pending.Remaining -= deltaTime;
                if (pending.Remaining > 0f) continue;
                pendingCollapses.RemoveAt(i);
                CollapseTriggered?.Invoke(pending.Event);
            }
        }

        private StructuralCollapseSeverity ResolveSeverity(int collapsedCount)
        {
            if (collapsedCount >= maximumCollapseTiles) return StructuralCollapseSeverity.Severe;
            return collapsedCount > 1
                ? StructuralCollapseSeverity.Major
                : StructuralCollapseSeverity.Minor;
        }

        private bool IsProtected(Vector3Int cell)
        {
            if (Array.IndexOf(protectedCells, cell) >= 0) return true;
            if (foregroundTilemap == null) return false;
            TileBase tile = foregroundTilemap.GetTile(cell);
            return tile != null && Array.IndexOf(protectedTiles, tile) >= 0;
        }

        private int GetSupportStrength(Vector3Int cell)
        {
            if (foregroundTilemap == null) return 0;
            Vector3 worldPosition = foregroundTilemap.GetCellCenterWorld(cell);
            int total = 0;
            foreach (StructuralSupport support in supports)
            {
                if (support != null && support.IsAvailable && support.Supports(worldPosition))
                    total += support.Strength;
            }

            return total;
        }

        private void OnSupportAvailabilityChanged(StructuralSupport support)
        {
            ReevaluateAffectedBySupport(support, support == null || !support.IsAvailable);
        }

        private void ReevaluateAffectedBySupport(StructuralSupport support, bool supportRemoved)
        {
            if (support == null || foregroundTilemap == null)
            {
                return;
            }

            var affected = new HashSet<Vector3Int>();
            float reach = support.Radius + Mathf.Max(localRiskRadius, 1);

            foreach (Vector3Int tracked in tileRisks.Keys)
            {
                Vector3 world = foregroundTilemap.GetCellCenterWorld(tracked);
                if (Vector2.Distance(world, support.transform.position) <= reach)
                    affected.Add(tracked);
            }

            foreach (Vector3Int mine in accumulatedImpact.Keys)
            {
                Vector3 world = foregroundTilemap.GetCellCenterWorld(mine);
                if (Vector2.Distance(world, support.transform.position) > reach) continue;
                foreach (Vector3Int ceiling in EnumerateUnsupportedCeilingsNearMine(mine))
                    affected.Add(ceiling);
            }

            if (affected.Count == 0)
            {
                Vector3Int supportCell = foregroundTilemap.WorldToCell(support.transform.position);
                foreach (Vector3Int ceiling in EnumerateUnsupportedCeilingsNearMine(supportCell))
                    affected.Add(ceiling);
                // 버팀목이 천장 높이에 있을 수도 있다.
                if (IsUnsupportedCeiling(supportCell) || tileRisks.ContainsKey(supportCell))
                    affected.Add(supportCell);

                // 버팀목 반경 안의 비지지 천장을 직접 스캔한다.
                int cellReach = Mathf.CeilToInt(support.Radius) + localRiskRadius;
                for (int x = supportCell.x - cellReach; x <= supportCell.x + cellReach; x++)
                for (int y = supportCell.y - cellReach; y <= supportCell.y + cellReach; y++)
                {
                    var cell = new Vector3Int(x, y, supportCell.z);
                    if (IsUnsupportedCeiling(cell))
                        affected.Add(cell);
                }
            }

            Vector3Int supportActionCell = foregroundTilemap.WorldToCell(support.transform.position);
            ReevaluateTiles(
                affected,
                allowCollapse: supportRemoved,
                supportActionCell,
                supportRemoved ? StructuralRiskCause.SupportRemoved : StructuralRiskCause.None,
                0f);
        }

        private void UpdateCurrentRisk()
        {
            StructuralRiskLevel highest = StructuralRiskLevel.Stable;
            foreach (KeyValuePair<Vector3Int, StructuralRiskLevel> pair in tileRisks)
            {
                StructuralRiskLevel risk = ToDisplayRisk(
                    pair.Value,
                    telegraphRemaining.ContainsKey(pair.Key));
                if (risk > highest) highest = risk;
            }

            if (CurrentRisk == highest) return;
            CurrentRisk = highest;
            RiskChanged?.Invoke(highest);
        }

        private static StructuralRiskLevel ToDisplayRisk(
            StructuralRiskLevel scoreLevel,
            bool telegraphing)
        {
            return scoreLevel == StructuralRiskLevel.CollapseImminent && !telegraphing
                ? StructuralRiskLevel.Danger
                : scoreLevel;
        }

        private void OnDestroy()
        {
            foreach (StructuralSupport support in supports)
            {
                if (support != null) support.AvailabilityChanged -= OnSupportAvailabilityChanged;
            }

            if (runtimeSettings == null) return;
            if (Application.isPlaying) Destroy(runtimeSettings);
            else DestroyImmediate(runtimeSettings);
        }
    }
}
