using System;
using System.Collections.Generic;

namespace SubTerra.Shared
{
    /// <summary>Gameplay 위험이 같은 Run 실패 흐름으로 전달될 때의 확정 원인.</summary>
    public enum RunFailureCause
    {
        Unknown = 0,
        PowerDepleted = 1,
        StructuralCollapse = 2,
        GasExposure = 3,
        Fall = 4
    }

    /// <summary>Gameplay 생존 판정이 App 실패 처리기에 전달하는 Unity 비의존 입력.</summary>
    [Serializable]
    public sealed class RunFailureInputDto
    {
        public string failureToken;
        public RunFailureCause cause;
        public string sourceId;
        public int damage;
        public int remainingHealth;
        public bool returnToElevator;
    }

    [Serializable]
    public sealed class CargoLossEntryDto
    {
        public string mineralId;
        public int quantity;
        public float lostWeight;
        public float lostValue;
    }

    /// <summary>화물 손실과 실제 복귀 위치를 포함하는 확정 구조 결과.</summary>
    [Serializable]
    public sealed class PlayerRescueResultDto
    {
        public string failureToken;
        public RunFailureCause cause;
        public string returnTargetId;
        public int returnX;
        public int returnY;
        public bool usedCheckpoint;
        public float preservationRatio;
        public float lostWeight;
        public float lostValue;
        public List<CargoLossEntryDto> lostCargo = new List<CargoLossEntryDto>();
    }
}
