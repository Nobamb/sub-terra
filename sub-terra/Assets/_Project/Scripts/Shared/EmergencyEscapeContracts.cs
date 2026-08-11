namespace SubTerra.Shared
{
    public enum EmergencyEscapeDestination
    {
        Elevator = 0,
        OutpostCore = 1
    }

    /// <summary>Gameplay 포탈이 App의 상태·결제 구현을 직접 참조하지 않기 위한 경계.</summary>
    public interface IEmergencyEscapePortalPort
    {
        bool TryEscape(out EmergencyEscapeDestination destination, out string reason);
    }
}
