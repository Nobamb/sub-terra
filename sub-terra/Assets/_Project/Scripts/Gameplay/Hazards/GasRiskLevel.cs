namespace SubTerra.Gameplay.Hazards
{
    /// <summary>Gameplay layer gas danger level. App/HUD translates this value when needed.</summary>
    public enum GasRiskLevel
    {
        Safe = 0,
        Caution = 1,
        Critical = 2
    }

    public enum GasType
    {
        Unknown = 0,
        Toxic = 1
    }
}
