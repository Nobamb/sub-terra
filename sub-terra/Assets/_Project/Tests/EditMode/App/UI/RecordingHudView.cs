namespace SubTerra.App.Tests.UI
{
    using SubTerra.App.UI.HUD;

    /// <summary>갱신 횟수·마지막 표시값을 기록하는 테스트용 View.</summary>
    public sealed class RecordingHudView : IHudView
    {
        public int EnergyCount;
        public int DepthCount;
        public int GoldCount;
        public int CargoCount;
        public int UnsettledValueCount;
        public int StructuralCount;
        public int GasRiskCount;
        public int GasVisibleCount;
        public int BuildingCount;
        public int InteractionCount;

        public string Energy;
        public string Depth;
        public string Gold;
        public string Cargo;
        public string UnsettledValue;
        public string Structural;
        public string GasRisk;
        public bool GasVisible;
        public string Building;
        public string Interaction;

        public void ResetCounts()
        {
            EnergyCount = 0;
            DepthCount = 0;
            GoldCount = 0;
            CargoCount = 0;
            UnsettledValueCount = 0;
            StructuralCount = 0;
            GasRiskCount = 0;
            GasVisibleCount = 0;
            BuildingCount = 0;
            InteractionCount = 0;
        }

        public void SetEnergy(string text)
        {
            Energy = text;
            EnergyCount++;
        }

        public void SetDepth(string text)
        {
            Depth = text;
            DepthCount++;
        }

        public void SetGold(string text)
        {
            Gold = text;
            GoldCount++;
        }

        public void SetCargo(string text)
        {
            Cargo = text;
            CargoCount++;
        }

        public void SetUnsettledValue(string text)
        {
            UnsettledValue = text;
            UnsettledValueCount++;
        }

        public void SetStructuralRisk(string text)
        {
            Structural = text;
            StructuralCount++;
        }

        public void SetGasRisk(string text)
        {
            GasRisk = text;
            GasRiskCount++;
        }

        public void SetGasWarningVisible(bool visible)
        {
            GasVisible = visible;
            GasVisibleCount++;
        }

        public void SetBuildingSelection(string text)
        {
            Building = text;
            BuildingCount++;
        }

        public void SetInteractionPrompt(string text)
        {
            Interaction = text;
            InteractionCount++;
        }
    }
}
