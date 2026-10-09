using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 지도 창. 셀 좌표(타일 1칸 = 1)에서 본 표시 범위와 셀당 픽셀 수를 담는다.
    /// 셀은 항상 가로·세로 같은 픽셀이다.
    /// </summary>
    public readonly struct MinimapWindow
    {
        public readonly Vector2 Min;
        public readonly Vector2 Size;
        public readonly float CellPixels;

        public MinimapWindow(Vector2 min, Vector2 size, float cellPixels)
        {
            Min = min;
            Size = size;
            CellPixels = cellPixels;
        }

        public bool IsValid => CellPixels > 0f && Size.x > 0f && Size.y > 0f;
        public Vector2 Max => Min + Size;
        public Vector2Int BaseCell => new(Mathf.FloorToInt(Min.x), Mathf.FloorToInt(Min.y));

        /// <summary>창을 덮는 정수 셀 개수(경계 걸친 셀 포함).</summary>
        public Vector2Int CellCount
        {
            get
            {
                Vector2Int origin = BaseCell;
                return new Vector2Int(
                    Mathf.Max(0, Mathf.CeilToInt(Max.x - 0.0001f) - origin.x),
                    Mathf.Max(0, Mathf.CeilToInt(Max.y - 0.0001f) - origin.y));
            }
        }

        /// <summary>정수 셀 원점을 기준으로 그린 콘텐츠를 뷰포트에 맞추는 오프셋(픽셀, 0 이하).</summary>
        public Vector2 ContentOffset => ((Vector2)BaseCell - Min) * CellPixels;

        public Vector2 CellToViewport(Vector2 cell) => (cell - Min) * CellPixels;

        /// <summary>셀 사각형을 정수 셀 원점 기준 콘텐츠 픽셀로 바꾼다. 크기는 바꾸지 않는다.</summary>
        public Rect CellRectToContent(Rect cells)
        {
            Vector2Int origin = BaseCell;
            return new Rect(
                (cells.x - origin.x) * CellPixels,
                (cells.y - origin.y) * CellPixels,
                cells.width * CellPixels,
                cells.height * CellPixels);
        }

        public bool Overlaps(Rect cells) =>
            cells.xMax > Min.x && cells.xMin < Max.x && cells.yMax > Min.y && cells.yMin < Max.y;
    }

    /// <summary>
    /// 전광판 프레임·뷰포트·셀 크기·표시 범위의 순수 계산.
    /// 바깥 프레임 비율: 가로형 2:1, 정사각형 1:1(같은 높이, 폭 절반). 헤더·푸터를 포함한다.
    /// </summary>
    public static class MinimapBoardLayout
    {
        public const float FrameHeight = 300f;
        public const float WideWidth = FrameHeight * 2f;
        public const float SquareWidth = FrameHeight;
        public const float HeaderHeight = 28f;
        public const float FooterHeight = 32f;
        public const float ViewportSideInset = 6f;
        public const float ViewportGap = 3f;
        /// <summary>외곽 발광이 마스크에 잘리지 않도록 확보하는 여백.</summary>
        public const float GlowMargin = 6f;
        /// <summary>화면 가장자리에서 띄우는 거리(우하단 고정).</summary>
        public const float ScreenMargin = 10f;
        public const float MinimumFitScale = 0.25f;

        public static float FrameWidth(float widthBlend) =>
            Mathf.Lerp(SquareWidth, WideWidth, Mathf.Clamp01(widthBlend));

        public static Vector2 FrameSize(MinimapBoardMode mode) =>
            new(mode == MinimapBoardMode.Square ? SquareWidth : WideWidth, FrameHeight);

        /// <summary>프레임 좌하단 기준 지도 뷰포트. 높이는 두 모드가 같다.</summary>
        public static Rect ViewportRect(float frameWidth) => Rect.MinMaxRect(
            ViewportSideInset,
            FooterHeight + ViewportGap,
            Mathf.Max(ViewportSideInset, frameWidth - ViewportSideInset),
            FrameHeight - HeaderHeight - ViewportGap);

        public static Vector2 WideViewportSize => ViewportRect(WideWidth).size;

        /// <summary>작은 화면에서 프레임 전체에 같은 배율을 적용한다. 확대는 하지 않는다.</summary>
        public static float FitScale(Vector2 parentSize)
        {
            if (parentSize.x <= 0f || parentSize.y <= 0f)
            {
                return 1f;
            }

            float reserve = (ScreenMargin + GlowMargin) * 2f;
            float scale = Mathf.Min(1f,
                Mathf.Min((parentSize.x - reserve) / WideWidth, (parentSize.y - reserve) / FrameHeight));
            return Mathf.Max(MinimumFitScale, scale);
        }

        /// <summary>
        /// 셀당 픽셀. 가로형 뷰포트가 카메라에 보이는 범위보다 넓은 곳을 보여 주지 않도록
        /// 두 축 중 큰 값을 올림해 쓴다(기존 공개 범위 = 카메라 화면 유지). 두 모드 공통.
        /// </summary>
        public static float CellPixels(Vector2 wideViewportSize, Vector2 cameraCells)
        {
            if (cameraCells.x <= 0f || cameraCells.y <= 0f || wideViewportSize.x <= 0f || wideViewportSize.y <= 0f)
            {
                return 0f;
            }

            return Mathf.Ceil(Mathf.Max(wideViewportSize.x / cameraCells.x, wideViewportSize.y / cameraCells.y));
        }

        /// <summary>
        /// 초점(플레이어) 중심의 창을 카메라 셀 범위 안으로 제한한다.
        /// 창이 카메라보다 크지 않으므로 숨겨진 영역을 새로 보이지 않는다.
        /// </summary>
        public static MinimapWindow ComputeWindow(Vector2 viewportSize, float cellPixels, Vector2 focusCell, Rect cameraCells)
        {
            if (cellPixels <= 0f || viewportSize.x <= 0f || viewportSize.y <= 0f)
            {
                return default;
            }

            Vector2 size = viewportSize / cellPixels;
            Vector2 min = focusCell - size * 0.5f;
            // 픽셀 단위로 맞춰 셀 경계가 흔들리지 않게 한다.
            min.x = Mathf.Round(min.x * cellPixels) / cellPixels;
            min.y = Mathf.Round(min.y * cellPixels) / cellPixels;
            min.x = ClampAxis(min.x, size.x, cameraCells.xMin, cameraCells.xMax);
            min.y = ClampAxis(min.y, size.y, cameraCells.yMin, cameraCells.yMax);
            return new MinimapWindow(min, size, cellPixels);
        }

        private static float ClampAxis(float min, float size, float lower, float upper)
        {
            if (upper - lower <= size)
            {
                return (lower + upper - size) * 0.5f;
            }

            return Mathf.Clamp(min, lower, upper - size);
        }

        /// <summary>최소 간격으로도 넘칠 때 범례 전체(아이콘·글자·간격)에 같은 비율로 적용할 축소 배율.</summary>
        public static float LegendScale(float available, float[] labelWidths, float iconSize, float iconGap, float minItemGap)
        {
            if (labelWidths == null || labelWidths.Length == 0) return 1f;
            float required = minItemGap * (labelWidths.Length - 1);
            for (int i = 0; i < labelWidths.Length; i++) required += iconSize + iconGap + labelWidths[i];
            return required <= available ? 1f : Mathf.Max(0.6f, available / required);
        }

        /// <summary>
        /// 범례 항목 x 위치를 채운다. 각 항목 폭 = 아이콘 + 간격 + 라벨.
        /// 남는 폭은 항목 사이 간격으로 나누되 최대 간격을 넘지 않는다. 넘치면 false.
        /// </summary>
        public static bool LayoutLegend(
            float available,
            float[] labelWidths,
            float iconSize,
            float iconGap,
            float minItemGap,
            float maxItemGap,
            float[] itemX)
        {
            if (labelWidths == null || itemX == null || itemX.Length < labelWidths.Length)
            {
                return false;
            }

            int count = labelWidths.Length;
            float content = 0f;
            for (int i = 0; i < count; i++)
            {
                content += iconSize + iconGap + labelWidths[i];
            }

            int gaps = Mathf.Max(1, count - 1);
            float gap = count > 1 ? Mathf.Clamp((available - content) / gaps, minItemGap, maxItemGap) : 0f;
            float x = 0f;
            for (int i = 0; i < count; i++)
            {
                itemX[i] = x;
                x += iconSize + iconGap + labelWidths[i] + gap;
            }

            return content + gap * (count - 1) <= available + 0.01f;
        }
    }
}
