namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// Basic / Structural / Gas View를 하나의 IHudView로 합성한다.
    /// Presenter는 합성 View만 알면 되고 개별 Prefab 경계를 유지한다.
    /// </summary>
    public sealed class CompositeHudView : IHudView
    {
        private readonly BasicHudView basic;
        private readonly StructuralHudView structural;
        private readonly GasWarningPanelView gas;

        public CompositeHudView(BasicHudView basic, StructuralHudView structural, GasWarningPanelView gas)
        {
            this.basic = basic;
            this.structural = structural;
            this.gas = gas;
        }

        public void SetEnergy(string text)
        {
            if (basic != null)
            {
                basic.SetEnergy(text);
            }
        }

        public void SetDepth(string text)
        {
            if (basic != null)
            {
                basic.SetDepth(text);
            }
        }

        public void SetGold(string text)
        {
            if (basic != null)
            {
                basic.SetGold(text);
            }
        }

        public void SetCargo(string text)
        {
            if (basic != null)
            {
                basic.SetCargo(text);
            }
        }

        public void SetUnsettledValue(string text)
        {
            if (basic != null)
            {
                basic.SetUnsettledValue(text);
            }
        }

        public void SetStructuralRisk(string text)
        {
            if (structural != null)
            {
                structural.SetStructuralRisk(text);
            }
        }

        public void SetGasRisk(string text)
        {
            if (gas != null)
            {
                gas.SetGasRisk(text);
            }
        }

        public void SetGasWarningVisible(bool visible)
        {
            if (gas != null)
            {
                gas.SetGasWarningVisible(visible);
            }
        }

        public void SetBuildingSelection(string text)
        {
            if (basic != null)
            {
                basic.SetBuildingSelection(text);
            }
        }

        public void SetInteractionPrompt(string text)
        {
            if (basic != null)
            {
                basic.SetInteractionPrompt(text);
            }
        }
    }
}
