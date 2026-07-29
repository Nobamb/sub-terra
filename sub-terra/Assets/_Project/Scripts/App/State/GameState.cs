using System;
using SubTerra.App.Outpost;

namespace SubTerra.App.State
{
    /// <summary>구조 안정도 표시 등급. A 계산 결과 수신용이며 UI에서 재계산하지 않는다.</summary>
    public enum StructuralRiskLevel
    {
        Safe = 0,
        Caution = 1,
        Critical = 2
    }

    /// <summary>가스 노출 표시 등급. A 계산 결과 수신용이며 UI에서 재계산하지 않는다.</summary>
    public enum GasRiskLevel
    {
        Safe = 0,
        Elevated = 1,
        Hazard = 2
    }

    /// <summary>전력 현재/최대 읽기 모델. HUD 이벤트 payload.</summary>
    public readonly struct EnergyReadModel
    {
        public int Current { get; }
        public int Max { get; }

        public EnergyReadModel(int current, int max)
        {
            Current = current;
            Max = max;
        }
    }

    /// <summary>화물 중량·미정산 가치 읽기 모델. HUD 이벤트 payload.</summary>
    public readonly struct InventoryReadModel
    {
        public float CargoWeight { get; }
        public float UnsettledValue { get; }

        public InventoryReadModel(float cargoWeight, float unsettledValue)
        {
            CargoWeight = cargoWeight;
            UnsettledValue = unsettledValue;
        }
    }

    /// <summary>건설 선택 읽기 모델. 빈 ID는 미선택.</summary>
    public readonly struct BuildingSelectionReadModel
    {
        public string BuildingId { get; }
        public string DisplayName { get; }

        public BuildingSelectionReadModel(string buildingId, string displayName)
        {
            BuildingId = buildingId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }

        public bool HasSelection => !string.IsNullOrEmpty(BuildingId);
    }

    /// <summary>
    /// 플레이어 런타임 수치. Unity Object 참조를 두지 않는다.
    /// </summary>
    [Serializable]
    public sealed class PlayerState
    {
        public int Energy { get; private set; }
        public int MaxEnergy { get; private set; }
        public int Gold { get; private set; }
        public float Cargo { get; private set; }
        public float UnsettledValue { get; private set; }
        public float Progress { get; private set; }

        private PlayerState() { }

        // 세이브 복원·테스트 대역에서 값 조립용. 외부에서 소유 GameState 교체는 FromParts로만 한다.
        public PlayerState(int energy, int gold, float cargo, float progress)
            : this(energy, 100, gold, cargo, 0f, progress)
        {
        }

        public PlayerState(int energy, int maxEnergy, int gold, float cargo, float unsettledValue, float progress)
        {
            Energy = energy;
            MaxEnergy = maxEnergy < 0 ? 0 : maxEnergy;
            Gold = gold;
            Cargo = cargo;
            UnsettledValue = unsettledValue;
            Progress = progress;
        }

        /// <summary>의도 기반 골드 변경. 음수는 차감으로 처리하되 0 미만으로 내려가지 않는다.</summary>
        public void AddGold(int amount)
        {
            var next = Gold + amount;
            Gold = next < 0 ? 0 : next;
        }

        internal void ApplyEnergy(int current, int max)
        {
            Energy = current < 0 ? 0 : current;
            MaxEnergy = max < 0 ? 0 : max;
            if (Energy > MaxEnergy)
            {
                Energy = MaxEnergy;
            }
        }

        internal void ApplyGold(int gold)
        {
            Gold = gold < 0 ? 0 : gold;
        }

        internal void ApplyCargo(float cargo)
        {
            Cargo = cargo < 0f ? 0f : cargo;
        }

        internal void ApplyUnsettledValue(float value)
        {
            UnsettledValue = value < 0f ? 0f : value;
        }
    }

    /// <summary>진행 목표 카운트. 하위 시스템 교체 없이 GameState가 소유한다.</summary>
    [Serializable]
    public sealed class ProgressState
    {
        public int CompletedObjectives { get; private set; }
        public bool HasSeenOutpostTutorial { get; private set; }

        private ProgressState() { }

        public ProgressState(int completedObjectives, bool hasSeenOutpostTutorial = false)
        {
            CompletedObjectives = completedObjectives;
            HasSeenOutpostTutorial = hasSeenOutpostTutorial;
        }

        internal void MarkOutpostTutorialSeen()
        {
            HasSeenOutpostTutorial = true;
        }
    }

    /// <summary>현재 Run(깊이·위험) 상태. 직렬화 가능한 값만 보관한다.</summary>
    [Serializable]
    public sealed class RunState
    {
        public int Depth { get; private set; }
        public bool IsSafe { get; private set; }
        public StructuralRiskLevel StructuralRisk { get; private set; }
        public GasRiskLevel GasExposure { get; private set; }

        private RunState() { }

        public RunState(int depth, bool safe)
            : this(depth, safe, StructuralRiskLevel.Safe, GasRiskLevel.Safe)
        {
        }

        public RunState(int depth, bool safe, StructuralRiskLevel structuralRisk, GasRiskLevel gasExposure)
        {
            Depth = depth;
            IsSafe = safe;
            StructuralRisk = structuralRisk;
            GasExposure = gasExposure;
        }

        internal void ApplyDepth(int depth)
        {
            Depth = depth < 0 ? 0 : depth;
        }

        internal void ApplyStructuralRisk(StructuralRiskLevel level)
        {
            StructuralRisk = level;
        }

        internal void ApplyGasExposure(GasRiskLevel level)
        {
            GasExposure = level;
        }

        internal void ApplyIsSafe(bool safe)
        {
            IsSafe = safe;
        }
    }

    /// <summary>
    /// 전역 게임 상태 루트. Player/Progress/Run을 소유하며
    /// 외부에서 하위 State 인스턴스를 임의 교체하지 못하게 한다.
    /// HUD 변경 이벤트는 의도 메서드 경로에서만 발행하고 동일 값 재설정은 억제한다.
    /// </summary>
    [Serializable]
    public sealed class GameState
    {
        public PlayerState Player { get; private set; }
        public ProgressState Progress { get; private set; }
        public RunState Run { get; private set; }
        public OutpostState Outpost { get; private set; }

        public string SelectedBuildingId { get; private set; }
        public string SelectedBuildingDisplayName { get; private set; }
        public string InteractionPrompt { get; private set; }

        public event Action<EnergyReadModel> EnergyChanged;
        public event Action<int> CreditsChanged;
        public event Action<InventoryReadModel> InventoryChanged;
        public event Action<int> DepthChanged;
        public event Action<StructuralRiskLevel> StructuralRiskChanged;
        public event Action<GasRiskLevel> GasExposureChanged;
        public event Action<BuildingSelectionReadModel> BuildingSelectionChanged;
        public event Action<string> InteractionPromptChanged;

        private GameState() { }

        /// <summary>새 게임용 안전 기본값. 골드·화물 0, 전력 100/100, 안전 Run, 선택 없음.</summary>
        public static GameState CreateNew()
        {
            return new GameState
            {
                Player = new PlayerState(100, 100, 0, 0f, 0f, 0f),
                Progress = new ProgressState(0),
                Run = new RunState(0, true, StructuralRiskLevel.Safe, GasRiskLevel.Safe),
                Outpost = new OutpostState(),
                SelectedBuildingId = string.Empty,
                SelectedBuildingDisplayName = string.Empty,
                InteractionPrompt = string.Empty
            };
        }

        /// <summary>
        /// 세이브 복원용 팩터리. 하위 상태가 하나라도 없으면 null을 반환해
        /// 불완전한 상태로 MainMenu에 진입하지 않게 한다.
        /// </summary>
        public static GameState FromParts(
            PlayerState player,
            ProgressState progress,
            RunState run,
            OutpostState outpost = null)
        {
            if (player == null || progress == null || run == null)
            {
                return null;
            }

            return new GameState
            {
                Player = player,
                Progress = progress,
                Run = run,
                Outpost = outpost ?? new OutpostState(),
                SelectedBuildingId = string.Empty,
                SelectedBuildingDisplayName = string.Empty,
                InteractionPrompt = string.Empty
            };
        }

        /// <summary>부트스트랩·세이브 경로에서 사용 가능한 완전 상태인지 검사한다.</summary>
        public static bool IsComplete(GameState state)
        {
            return state != null
                && state.Player != null
                && state.Progress != null
                && state.Run != null
                && state.Outpost != null;
        }

        public EnergyReadModel GetEnergy()
        {
            return new EnergyReadModel(Player.Energy, Player.MaxEnergy);
        }

        public InventoryReadModel GetInventory()
        {
            return new InventoryReadModel(Player.Cargo, Player.UnsettledValue);
        }

        public BuildingSelectionReadModel GetBuildingSelection()
        {
            return new BuildingSelectionReadModel(SelectedBuildingId, SelectedBuildingDisplayName);
        }

        /// <summary>전력 현재/최대 설정. 동일 값이면 이벤트를 발행하지 않는다.</summary>
        public void SetEnergy(int current, int max)
        {
            if (Player.Energy == current && Player.MaxEnergy == max)
            {
                return;
            }

            Player.ApplyEnergy(current, max);
            EnergyChanged?.Invoke(GetEnergy());
        }

        /// <summary>현재 전력만 변경. 최대는 유지한다.</summary>
        public void SetCurrentEnergy(int current)
        {
            SetEnergy(current, Player.MaxEnergy);
        }

        /// <summary>골드 절대값 설정. 동일 값이면 이벤트 없음.</summary>
        public void SetGold(int gold)
        {
            var clamped = gold < 0 ? 0 : gold;
            if (Player.Gold == clamped)
            {
                return;
            }

            Player.ApplyGold(clamped);
            CreditsChanged?.Invoke(Player.Gold);
        }

        /// <summary>의도 기반 골드 증감. 실제 값이 바뀔 때만 CreditsChanged를 발행한다.</summary>
        public void AddGold(int amount)
        {
            var before = Player.Gold;
            Player.AddGold(amount);
            if (Player.Gold == before)
            {
                return;
            }

            CreditsChanged?.Invoke(Player.Gold);
        }

        /// <summary>화물 중량 설정. 동일 값이면 이벤트 없음.</summary>
        public void SetCargoWeight(float weight)
        {
            var clamped = weight < 0f ? 0f : weight;
            if (Approximately(Player.Cargo, clamped))
            {
                return;
            }

            Player.ApplyCargo(clamped);
            InventoryChanged?.Invoke(GetInventory());
        }

        /// <summary>미정산 가치 설정. 동일 값이면 이벤트 없음.</summary>
        public void SetUnsettledValue(float value)
        {
            var clamped = value < 0f ? 0f : value;
            if (Approximately(Player.UnsettledValue, clamped))
            {
                return;
            }

            Player.ApplyUnsettledValue(clamped);
            InventoryChanged?.Invoke(GetInventory());
        }

        /// <summary>화물·가치를 한 번에 갱신. 둘 다 같으면 이벤트 없음.</summary>
        public void SetInventory(float cargoWeight, float unsettledValue)
        {
            var cargo = cargoWeight < 0f ? 0f : cargoWeight;
            var value = unsettledValue < 0f ? 0f : unsettledValue;
            if (Approximately(Player.Cargo, cargo) && Approximately(Player.UnsettledValue, value))
            {
                return;
            }

            Player.ApplyCargo(cargo);
            Player.ApplyUnsettledValue(value);
            InventoryChanged?.Invoke(GetInventory());
        }

        /// <summary>깊이 설정. 동일 값이면 이벤트 없음.</summary>
        public void SetDepth(int depth)
        {
            var clamped = depth < 0 ? 0 : depth;
            if (Run.Depth == clamped)
            {
                return;
            }

            Run.ApplyDepth(clamped);
            DepthChanged?.Invoke(Run.Depth);
        }

        /// <summary>구조 안정도 표시 등급 설정. 동일 값이면 이벤트 없음.</summary>
        public void SetStructuralRisk(StructuralRiskLevel level)
        {
            if (Run.StructuralRisk == level)
            {
                return;
            }

            Run.ApplyStructuralRisk(level);
            StructuralRiskChanged?.Invoke(Run.StructuralRisk);
        }

        /// <summary>가스 노출 표시 등급 설정. 동일 값이면 이벤트 없음.</summary>
        public void SetGasExposure(GasRiskLevel level)
        {
            if (Run.GasExposure == level)
            {
                return;
            }

            Run.ApplyGasExposure(level);
            GasExposureChanged?.Invoke(Run.GasExposure);
        }

        /// <summary>건설 선택 설정. 빈 ID는 미선택. 동일 값이면 이벤트 없음.</summary>
        public void SetBuildingSelection(string buildingId, string displayName = null)
        {
            var id = buildingId ?? string.Empty;
            var name = displayName ?? string.Empty;
            if (SelectedBuildingId == id && SelectedBuildingDisplayName == name)
            {
                return;
            }

            SelectedBuildingId = id;
            SelectedBuildingDisplayName = name;
            BuildingSelectionChanged?.Invoke(GetBuildingSelection());
        }

        /// <summary>상호작용 안내 문구 설정. 동일 값이면 이벤트 없음.</summary>
        public void SetInteractionPrompt(string prompt)
        {
            var text = prompt ?? string.Empty;
            if (InteractionPrompt == text)
            {
                return;
            }

            InteractionPrompt = text;
            InteractionPromptChanged?.Invoke(InteractionPrompt);
        }

        /// <summary>첫 전진기지 튜토리얼 표시 여부를 저장 가능한 진행 상태에 기록한다.</summary>
        public void MarkOutpostTutorialSeen()
        {
            Progress?.MarkOutpostTutorialSeen();
        }

        private static bool Approximately(float a, float b)
        {
            return Math.Abs(a - b) < 0.0001f;
        }
    }
}
