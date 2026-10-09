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
                    Draw(mesh, record, footprint, window.CellPixels, MinimapFacilityRegistry.FlashProgress(record, now));
                }
            }
        }

        private static void Draw(VertexHelper mesh, MinimapFacilityRecord record, Rect footprint, float px, float flash)
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

            float size = Mathf.Min(body.width, body.height) * 0.62f;
            var glyphRect = new Rect(body.center.x - size * 0.5f, body.center.y - size * 0.5f, size, size);
            MinimapMeshKit.Glyph(mesh, glyph, glyphRect, tint);
            if (flash >= 0f) DrawFlash(mesh, body, px, flash);
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
