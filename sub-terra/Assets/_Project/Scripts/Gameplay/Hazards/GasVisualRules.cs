using UnityEngine;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>
    /// 유독가스 생성/접근 연출과 조명 시야 구멍의 공통 수치.
    /// 월드 단위는 Grid cellSize 1을 1칸으로 본다.
    /// </summary>
    public static class GasVisualRules
    {
        public const float SpawnDurationSeconds = 1f;
        public const float ApproachFadeSeconds = 1f;
        public const float GasRadiusBlocks = 5f;
        public const float GasVisualOpacity = 0.70f;
        public const float InitialApproachOpacity = 0.35f;
        public const float FullApproachOpacity = 0.95f;
        public const float LightClearRadiusBlocks = 5f;
        public const float LightClearRedOpacity = 0.05f;

        public const string GasCloudChildName = "GasCloud";
        public const string LightClearanceChildName = "GasClearance";

        // 채도를 낮춘 가스 녹색·조명 붉은색. 색 정체성(녹/적)은 유지한다.
        public static readonly Color GasTint = new(0.30f, 0.66f, 0.38f, 1f);
        public static readonly Color LightClearTint = new(0.92f, 0.30f, 0.24f, 1f);
        public static readonly Color VeilDarkColor = new(0.04f, 0.05f, 0.05f, 1f);
        public static readonly Color HazeDarkColor = new(0.07f, 0.18f, 0.10f, 1f);

        // 조명 구멍은 이 비율(반지름 대비)까지 완전히 뚫리고, 그 바깥 경계까지 부드럽게 사라진다.
        public const float LightHoleInnerFraction = 0.35f;

        public const string CloudShaderName = "SubTerra/GasCloud";
        public const string SoftSpriteName = "gas-soft-radial";
    }
}
