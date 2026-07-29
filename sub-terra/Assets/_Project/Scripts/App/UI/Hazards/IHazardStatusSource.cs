using System;

namespace SubTerra.App.UI.Hazards
{
    /// <summary>Gameplay 결과를 B HUD에 전달하는 읽기 전용 이벤트 원천.</summary>
    public interface IHazardStatusSource
    {
        HazardStatusReadModel StructuralStatus { get; }
        HazardStatusReadModel GasStatus { get; }
        PowerStatusReadModel PowerStatus { get; }

        event Action<HazardStatusReadModel> StructuralStatusChanged;
        event Action<HazardStatusReadModel> GasStatusChanged;
        event Action<PowerStatusReadModel> PowerStatusChanged;
    }
}
