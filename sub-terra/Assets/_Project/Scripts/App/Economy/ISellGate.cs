namespace SubTerra.App.Economy
{
    /// <summary>
    /// 판매 허용 게이트. UI/Binder가 컨텍스트(Surface Base 등)에 따라 토글한다.
    /// null 주입 시 EconomyService는 판매를 허용한다(기존 단위 테스트 호환).
    /// </summary>
    public interface ISellGate
    {
        /// <summary>
        /// get/set 모두 인터페이스에 둔다.
        /// Binder가 ISellGate 참조만으로 IsSellAllowed = true/false 할 수 있어야 하며,
        /// get-only면 컴파일 실패 또는 concrete cast가 강제된다.
        /// </summary>
        bool IsSellAllowed { get; set; }

        /// <summary>거부 시 사용자 메시지. 허용 중이면 빈 문자열.</summary>
        string DenyReason { get; }
    }

    /// <summary>씬 컨텍스트 기반 판매 게이트 기본 구현.</summary>
    public sealed class SceneSellGate : ISellGate
    {
        public bool IsSellAllowed { get; set; }

        public string DenyReason => IsSellAllowed
            ? string.Empty
            : "Surface Base에서만 판매할 수 있습니다.";
    }
}
