namespace SubTerra.Shared
{
    /// <summary>
    /// Scene 전환에도 유지되는 접근성 선택.
    /// ReduceMotion(화면 진동 억제)이 true이면 구조 위험·붕괴 화면 흔들림을 끈다.
    /// </summary>
    public static class AccessibilityPreferences
    {
        /// <summary>true면 화면 진동(카메라 쉐이크)을 억제한다.</summary>
        public static bool ReduceMotion { get; set; }
    }
}
