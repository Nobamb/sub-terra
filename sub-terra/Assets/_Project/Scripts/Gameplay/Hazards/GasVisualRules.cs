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

        // B-196-1: 켜진 조명이 주변을 분명히 밝힌다. 전부 시각 수치이며 판정 반지름(5칸)보다 커지지 않는다.
        /// <summary>가스 베일·암전 조명 구멍 중심 알파(권장 0.25~0.45). 예전 5%(LightClearRedOpacity)는 GasClearance 스프라이트 틴트로만 남는다.</summary>
        public const float LightHoleCenterOpacity = 0.40f;
        public const float LightHaloOpacity = 0.65f;
        /// <summary>전원·공급 범위가 없는 조명의 빛 세기 비율. 조명은 전원 없이도 가스 시야를 뚫으므로 완전히 끄지 않는다.</summary>
        public const float LightUnpoweredLevel = 0.6f;
        public const float LightHaloFlickerStrength = 0.06f;
        public const float LightHaloFadeSeconds = 0.15f;
        public const int MaxVisibleLightHalos = 32;
        public static readonly Color LightHaloTint = new(1f, 0.74f, 0.34f, 1f);
        public const string LightHaloChildName = "LightHalo";
        public const string LightHaloShaderName = "SubTerra/LightHalo";
        public const string LightHaloShaderResourceName = "LightHalo";

        // 깊은 곳 암전에 조명마다 뚫는 구멍. 중심에서 암전을 이 비율만큼 걷고(1이면 완전히), 바깥으로 부드럽게 사라진다.
        public const float DarknessHoleStrength = 0.88f;
        public const float DarknessHoleInnerFraction = 0.20f;

        public const string CloudShaderName = "SubTerra/GasCloud";
        public const string SoftSpriteName = "gas-soft-radial";
    }
}
