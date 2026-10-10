using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>광산 관측 전광판 색. 남은 블록은 중간 밝기, 빈 공간은 어둡게, 시설·플레이어는 더 밝게.</summary>
    public static class MinimapPalette
    {
        public static readonly Color Cyan = new(0.26f, 0.9f, 1f, 1f);
        public static readonly Color CyanSoft = new(0.26f, 0.9f, 1f, 0.42f);
        public static readonly Color Glow = new(0.2f, 0.85f, 1f, 0.07f);
        public static readonly Color Metal = new(0.075f, 0.09f, 0.105f, 1f);
        public static readonly Color MetalEdge = new(0.24f, 0.29f, 0.32f, 1f);
        public static readonly Color HeaderBar = new(0.045f, 0.07f, 0.095f, 1f);
        /// <summary>지도 바탕·빈 공간. 뒤 화면과 섞이지 않는 불투명 남색.</summary>
        public static readonly Color Navy = new(0.025f, 0.045f, 0.08f, 1f);
        /// <summary>월드 경계 밖 미관측 영역. 빈 공간보다 더 어둡고 점 격자가 없다.</summary>
        public static readonly Color Void = new(0.008f, 0.014f, 0.028f, 1f);
        public static readonly Color VoidHatch = new(0.06f, 0.1f, 0.14f, 1f);
        public static readonly Color Block = new(0.2f, 0.33f, 0.37f, 1f);
        public static readonly Color BlockTop = new(0.32f, 0.48f, 0.52f, 1f);
        /// <summary>장식용 LED 점. 셀 모서리에만 낮은 밝기로 찍는다.</summary>
        public static readonly Color LedDot = new(0.3f, 0.78f, 0.88f, 0.16f);
        public static readonly Color MinedEdge = new(0.55f, 1f, 1f, 1f);
        public static readonly Color Scan = new(0.45f, 0.95f, 1f, 0.09f);
        public static readonly Color Beam = new(0.7f, 1f, 1f, 1f);
        public static readonly Color PlayerBody = new(0.93f, 0.99f, 1f, 1f);
        public static readonly Color PlayerGlow = new(0.3f, 0.92f, 1f, 0.45f);
        public static readonly Color PlayerRing = new(0.35f, 0.95f, 1f, 1f);
        public static readonly Color Title = new(0.86f, 0.97f, 1f, 1f);
        public static readonly Color Label = new(0.74f, 0.86f, 0.9f, 1f);

        public static readonly Color Core = new(0.22f, 0.93f, 0.95f, 1f);
        public static readonly Color Charger = new(1f, 0.76f, 0.18f, 1f);
        public static readonly Color Clinic = new(0.32f, 1f, 0.58f, 1f);
        public static readonly Color Elevator = new(0.42f, 0.8f, 1f, 1f);
        public static readonly Color Generic = new(0.72f, 0.82f, 0.86f, 1f);
        public static readonly Color Light = new(1f, 0.93f, 0.45f, 1f);
        public static readonly Color Storage = new(1f, 0.62f, 0.3f, 1f);
        public static readonly Color Settlement = new(0.78f, 0.62f, 1f, 1f);
        public static readonly Color Portal = new(1f, 0.42f, 0.86f, 1f);
        /// <summary>플레이어 위치 표시등. 깜빡이는 붉은 불빛.</summary>
        public static readonly Color Beacon = new(1f, 0.16f, 0.14f, 1f);
        public static readonly Color BeaconGlow = new(1f, 0.2f, 0.16f, 0.4f);

        public static Color ForKind(MinimapFacilityKind kind) => kind switch
        {
            MinimapFacilityKind.Core => Core,
            MinimapFacilityKind.Charger => Charger,
            MinimapFacilityKind.Clinic => Clinic,
            MinimapFacilityKind.Elevator => Elevator,
            MinimapFacilityKind.Light => Light,
            MinimapFacilityKind.Storage => Storage,
            MinimapFacilityKind.Settlement => Settlement,
            MinimapFacilityKind.Portal => Portal,
            _ => Generic
        };

        public static MinimapGlyph GlyphForKind(MinimapFacilityKind kind) => kind switch
        {
            MinimapFacilityKind.Core => MinimapGlyph.Hexagon,
            MinimapFacilityKind.Charger => MinimapGlyph.Bolt,
            MinimapFacilityKind.Clinic => MinimapGlyph.Cross,
            MinimapFacilityKind.Elevator => MinimapGlyph.Arrows,
            MinimapFacilityKind.Ladder => MinimapGlyph.Ladder,
            MinimapFacilityKind.Support => MinimapGlyph.Support,
            MinimapFacilityKind.Light => MinimapGlyph.Lamp,
            MinimapFacilityKind.Storage => MinimapGlyph.Cube,
            MinimapFacilityKind.Settlement => MinimapGlyph.Calculator,
            MinimapFacilityKind.Portal => MinimapGlyph.Wormhole,
            _ => MinimapGlyph.Generic
        };

        public static Color WithAlpha(Color color, float alpha) => new(color.r, color.g, color.b, color.a * alpha);
    }
}
