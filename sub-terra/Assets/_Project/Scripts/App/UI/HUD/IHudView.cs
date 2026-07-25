namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// HUD View 계약. 표시 문자열/활성만 받으며 State를 읽거나 쓰지 않는다.
    /// </summary>
    public interface IHudView
    {
        void SetEnergy(string text);
        void SetDepth(string text);
        void SetGold(string text);
        void SetCargo(string text);
        void SetUnsettledValue(string text);
        void SetStructuralRisk(string text);
        void SetGasRisk(string text);
        void SetGasWarningVisible(bool visible);
        void SetBuildingSelection(string text);
        void SetInteractionPrompt(string text);
    }
}
