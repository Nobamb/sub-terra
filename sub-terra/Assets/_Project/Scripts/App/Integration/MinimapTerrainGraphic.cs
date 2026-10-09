using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 표시 창 안의 셀만 한 메시로 그린다(타일마다 GameObject를 만들지 않는다).
    /// 남은 블록 = 얇은 간격의 정사각형, 채굴·빈 공간 = 셀 없는 어두운 바탕 + 모서리 LED 점,
    /// 월드 밖 = 더 어두운 미관측 칸. 콘텐츠 원점 = 정수 셀 baseCell의 좌하단.
    /// </summary>
    public sealed class MinimapTerrainGraphic : MaskableGraphic
    {
        private IMinimapTerrainSource source;
        private MinimapMinedFlashes flashes;
        private Vector2Int baseCell;
        private Vector2Int cellCount;
        private float cellPixels;
        private float now;

        public int BlockCount { get; private set; }
        public int EmptyCount { get; private set; }
        public int VoidCount { get; private set; }
        public int FlashCount { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void Configure(IMinimapTerrainSource terrain, MinimapMinedFlashes mined, Vector2Int origin, Vector2Int count, float pixels, float time)
        {
            source = terrain;
            flashes = mined;
            baseCell = origin;
            cellCount = count;
            cellPixels = pixels;
            now = time;
            SetVerticesDirty();
        }

        public static float CellGap(float pixels) => Mathf.Max(1f, Mathf.Round(pixels * 0.07f));

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            Fill(mesh);
        }

        public void Fill(VertexHelper mesh)
        {
            mesh.Clear();
            BlockCount = EmptyCount = VoidCount = FlashCount = 0;
            if (source == null || cellPixels <= 0f || cellCount.x <= 0 || cellCount.y <= 0) return;

            float px = cellPixels;
            float gap = CellGap(px);
            float half = gap * 0.5f;
            float dot = Mathf.Clamp(px * 0.07f, 1.5f, 2.5f);
            for (int iy = 0; iy < cellCount.y; iy++)
            {
                for (int ix = 0; ix < cellCount.x; ix++)
                {
                    int x = baseCell.x + ix;
                    int y = baseCell.y + iy;
                    var cell = new Rect(ix * px, iy * px, px, px);
                    switch (source.Sample(x, y))
                    {
                        case MinimapCellKind.Block:
                        {
                            BlockCount++;
                            var body = Rect.MinMaxRect(cell.xMin + half, cell.yMin + half, cell.xMax - half, cell.yMax - half);
                            MinimapMeshKit.Quad(mesh, body, MinimapPalette.Block);
                            // 윗면이 드러난 블록에만 얇은 밝은 선을 둬 층 구조를 읽게 한다.
                            if (source.Sample(x, y + 1) != MinimapCellKind.Block)
                            {
                                MinimapMeshKit.Quad(mesh, Rect.MinMaxRect(body.xMin, body.yMax - Mathf.Max(1f, px * 0.06f), body.xMax, body.yMax), MinimapPalette.BlockTop);
                            }

                            break;
                        }
                        case MinimapCellKind.Empty:
                            EmptyCount++;
                            // 점은 셀 중심이 아니라 좌하단 격자 교차점에 둔다(시설·블록으로 오인 방지).
                            MinimapMeshKit.Quad(mesh, new Rect(cell.xMin - dot * 0.5f, cell.yMin - dot * 0.5f, dot, dot), MinimapPalette.LedDot);
                            break;
                        default:
                            VoidCount++;
                            MinimapMeshKit.Quad(mesh, cell, MinimapPalette.Void);
                            MinimapMeshKit.Segment(mesh, new Vector2(cell.xMin + px * 0.2f, cell.yMin + px * 0.2f),
                                new Vector2(cell.xMax - px * 0.2f, cell.yMax - px * 0.2f), 1f, MinimapPalette.VoidHatch);
                            break;
                    }
                }
            }

            if (flashes == null) return;
            for (int i = 0; i < flashes.Count; i++)
            {
                if (!flashes.TryGet(i, now, out var mined, out float progress)) continue;
                int ix = mined.x - baseCell.x;
                int iy = mined.y - baseCell.y;
                if (ix < 0 || iy < 0 || ix >= cellCount.x || iy >= cellCount.y) continue;
                FlashCount++;
                var cell = new Rect(ix * px, iy * px, px, px);
                MinimapMeshKit.Outline(mesh, cell, Mathf.Max(1f, px * 0.06f), MinimapPalette.WithAlpha(MinimapPalette.MinedEdge, 1f - progress));
            }
        }
    }
}
