namespace SubTerra.Editor
{
    // 기존 자동화 진입점도 새 원화를 사용한다. 단일 Idle 원화의 관절 변형은 실행하지 않는다.
    public static class SmoothWalkCycleBaker
    {
        public static void Bake() => RedrawnWalkCycleBuilder.Build();
        public static void BakeKeyPoses() => RedrawnWalkCycleBuilder.BuildPreview();
        public static void BakeAndValidate() => RedrawnWalkCycleBuilder.BuildAndValidate();
    }
}
