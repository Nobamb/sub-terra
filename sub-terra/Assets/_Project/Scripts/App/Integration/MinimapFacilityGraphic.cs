using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 시설을 실제 점유 셀 크기의 장비 외곽 + 종류별 기호로 한 메시에 그린다.
    /// 시설 ID당 한 번만 그리며, 바닥(점유 하단)이 아래 블록 윗면에 닿는다.
    /// </summary>
    public sealed class MinimapFacilityGraphic : MaskableGraphic
    {
        public const float InactiveAlpha = 0.4f;
        /// <summary>긴급 탈출 포탈 내부 아이콘이 한 바퀴 도는 시간(초).</summary>
        public const float PortalSpinSeconds = 8f;

        private readonly Dictionary<string, Rect> drawnFootprints = new();
        private MinimapFacilityRegistry registry;
        private MinimapWindow window;
        private float now;

        public int DrawnCount => drawnFootprints.Count;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void Configure(MinimapFacilityRegistry facilities, MinimapWindow view, float time)
        {
            registry = facilities;
            window = view;
            now = time;
            SetVerticesDirty();
        }

        /// <summary>마지막으로 그린 시설 점유 사각형(콘텐츠 픽셀).</summary>
        public bool TryGetDrawnFootprint(string id, out Rect footprint) =>
            drawnFootprints.TryGetValue(id ?? string.Empty, out footprint);

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            Fill(mesh);
        }

        public void Fill(VertexHelper mesh)
        {
            mesh.Clear();
            drawnFootprints.Clear();
            if (registry == null || !window.IsValid) return;
            // 사다리·버팀목·엘리베이터 승강로를 먼저, 장비형 시설을 위에 그린다.
            for (int pass = 0; pass < 2; pass++)
            {
                var records = registry.Records;
                for (int i = 0; i < records.Count; i++)
                {
                    var record = records[i];
                    bool background = record.Kind == MinimapFacilityKind.Ladder
                        || record.Kind == MinimapFacilityKind.Support
                        || record.Kind == MinimapFacilityKind.Elevator;
                    if ((pass == 0) != background || !window.Overlaps(record.Cells)) continue;
                    Rect footprint = window.CellRectToContent(record.Cells);
                    drawnFootprints[record.Id] = footprint;
                    Draw(mesh, record, footprint, window.CellPixels, MinimapFacilityRegistry.FlashProgress(record, now), now);
                }
            }
        }

        /// <summary>포탈 내부 소용돌이의 현재 회전각(라디안). unscaled 시간 기준으로 천천히 한 방향으로 돈다.</summary>
        public static float PortalSpin(float time) => Mathf.Repeat(time / PortalSpinSeconds, 1f) * Mathf.PI * 2f;

        private static void Draw(VertexHelper mesh, MinimapFacilityRecord record, Rect footprint, float px, float flash, float time)
        {
            float alpha = record.Active ? 1f : InactiveAlpha;
            Color tint = MinimapPalette.ForKind(record.Kind);
            if (!record.Active) tint = new Color(tint.r * 0.6f, tint.g * 0.6f, tint.b * 0.6f, tint.a);
            tint.a *= alpha;
            MinimapGlyph glyph = MinimapPalette.GlyphForKind(record.Kind);

            if (record.Kind == MinimapFacilityKind.Ladder || record.Kind == MinimapFacilityKind.Support)
            {
                float side = Mathf.Max(1f, px * 0.12f);
                MinimapMeshKit.Glyph(mesh, glyph, Rect.MinMaxRect(footprint.xMin + side, footprint.yMin, footprint.xMax - side, footprint.yMax), MinimapPalette.WithAlpha(tint, 0.7f));
                if (flash >= 0f) DrawFlash(mesh, footprint, px, flash);
                return;
            }

            if (record.Kind == MinimapFacilityKind.Elevator)
            {
                DrawElevator(mesh, footprint, px, tint);
                if (flash >= 0f) DrawFlash(mesh, footprint, px, flash);
                return;
            }

            if (record.Kind == MinimapFacilityKind.Light)
            {
                // 조명은 상자 없이 전구 모양 자체로 그린다. 바닥이 점유 하단에 닿는다.
                float side = Mathf.Max(1f, px * 0.1f);
                var lamp = Rect.MinMaxRect(footprint.xMin + side, footprint.yMin, footprint.xMax - side, footprint.yMax - px * 0.05f);
                MinimapMeshKit.Glyph(mesh, glyph, lamp, tint);
                if (flash >= 0f) DrawFlash(mesh, lamp, px, flash);
                return;
            }

            float inset = Mathf.Max(1f, px * 0.08f);
            float feet = Mathf.Max(2f, Mathf.Round(px * 0.1f));
            var body = Rect.MinMaxRect(footprint.xMin + inset, footprint.yMin + feet, footprint.xMax - inset, footprint.yMax - px * 0.1f);
            if (body.width <= 0f || body.height <= 0f) return;
            float stroke = Mathf.Max(1.5f, px * 0.06f);
            float fill = 0.16f + (flash >= 0f ? 0.3f * (1f - flash) : 0f);
            MinimapMeshKit.Quad(mesh, body, MinimapPalette.WithAlpha(tint, fill));
            MinimapMeshKit.Outline(mesh, body, stroke, tint);
            float foot = Mathf.Max(2f, body.width * 0.14f);
            MinimapMeshKit.Quad(mesh, Rect.MinMaxRect(body.xMin + stroke, footprint.yMin, body.xMin + stroke + foot, body.yMin), tint);
            MinimapMeshKit.Quad(mesh, Rect.MinMaxRect(body.xMax - stroke - foot, footprint.yMin, body.xMax - stroke, body.yMin), tint);
            if (record.Kind == MinimapFacilityKind.Core)
            {
                float tab = Mathf.Max(2f, body.width * 0.12f);
                float tabHeight = Mathf.Min(px * 0.08f + 1f, footprint.yMax - body.yMax);
                MinimapMeshKit.Quad(mesh, Rect.MinMaxRect(body.xMin + tab, body.yMax, body.xMin + tab * 2f, body.yMax + tabHeight), tint);
                MinimapMeshKit.Quad(mesh, Rect.MinMaxRect(body.xMax - tab * 2f, body.yMax, body.xMax - tab, body.yMax + tabHeight), tint);
            }

            Rect glyphRect;
            if (record.Kind == MinimapFacilityKind.Settlement)
            {
                // 1x2 계산기: 세로로 긴 영역을 그대로 쓴다.
                float gw = body.width * 0.72f;
                float gh = body.height * 0.76f;
                glyphRect = new Rect(body.center.x - gw * 0.5f, body.center.y - gh * 0.5f, gw, gh);
            }
            else
            {
                float ratio = record.Kind == MinimapFacilityKind.Storage ? 0.74f
                    : record.Kind == MinimapFacilityKind.Portal ? 0.72f
                    : 0.62f;
                float size = Mathf.Min(body.width, body.height) * ratio;
                glyphRect = new Rect(body.center.x - size * 0.5f, body.center.y - size * 0.5f, size, size);
            }

            float spin = record.Kind == MinimapFacilityKind.Portal ? PortalSpin(time) : 0f;
            MinimapMeshKit.Glyph(mesh, glyph, glyphRect, tint, spin);
            if (flash >= 0f) DrawFlash(mesh, body, px, flash);
        }

        /// <summary>승강로를 사각형 대신 위·아래 화살표 아이콘을 세로로 늘어놓아 표시한다.</summary>
        private static void DrawElevator(VertexHelper mesh, Rect footprint, float px, Color tint)
        {
            float side = Mathf.Clamp(footprint.width * 0.7f, 6f, Mathf.Max(6f, px * 2.2f));
            float pitch = side * 1.5f;
            int count = Mathf.Clamp(Mathf.FloorToInt(footprint.height / pitch), 1, 40);
            float rail = Mathf.Max(1f, px * 0.06f);
            float cx = footprint.center.x;
            MinimapMeshKit.Quad(mesh, Rect.MinMaxRect(cx - rail * 0.5f, footprint.yMin, cx + rail * 0.5f, footprint.yMax),
                MinimapPalette.WithAlpha(tint, 0.22f));
            float start = footprint.yMin + (footprint.height - count * pitch) * 0.5f + (pitch - side) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                var icon = new Rect(cx - side * 0.5f, start + i * pitch, side, side);
                MinimapMeshKit.Glyph(mesh, MinimapGlyph.Arrows, icon, tint);
            }
        }

        private static void DrawFlash(VertexHelper mesh, Rect rect, float px, float progress)
        {
            float grow = 1f + 3f * (1f - progress);
            var bounds = Rect.MinMaxRect(rect.xMin - grow, rect.yMin - grow, rect.xMax + grow, rect.yMax + grow);
            float arm = Mathf.Clamp(Mathf.Min(bounds.width, bounds.height) * 0.3f, 3f, px * 0.5f);
            MinimapMeshKit.Brackets(mesh, bounds, arm, 1.5f, MinimapPalette.WithAlpha(MinimapPalette.MinedEdge, 1f - progress));
        }
    }
}
