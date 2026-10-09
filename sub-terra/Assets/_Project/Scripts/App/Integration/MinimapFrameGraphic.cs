using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 어두운 금속 프레임·얇은 청록 선·각진 모서리 브래킷·절제한 외곽 발광·헤더/푸터 구분선·상태등.
    /// 폭이 바뀔 때만 메시를 다시 만든다.
    /// </summary>
    public sealed class MinimapFrameGraphic : MaskableGraphic
    {
        public const float Chamfer = 8f;
        public const float BracketArm = 20f;
        public const float BracketThickness = 2f;
        public static readonly Vector2 StatusLampCenterFromTopLeft = new(17f, 14f);

        private float dividerFromRight;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        /// <summary>푸터의 범례와 M키 안내 사이 세로 구분선 위치(오른쪽 끝 기준).</summary>
        public void SetDivider(float fromRight)
        {
            if (Mathf.Approximately(dividerFromRight, fromRight)) return;
            dividerFromRight = fromRight;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Fill(mesh, rectTransform.rect, dividerFromRight);
        }

        public static void Fill(VertexHelper mesh, Rect frame, float dividerFromRight)
        {
            if (frame.width <= 0f || frame.height <= 0f) return;

            // 외곽 발광은 두 겹의 낮은 알파만 사용한다.
            MinimapMeshKit.ChamferFill(mesh, Expand(frame, 5f), Chamfer + 4f, MinimapPalette.Glow);
            MinimapMeshKit.ChamferFill(mesh, Expand(frame, 2.5f), Chamfer + 2f, MinimapPalette.WithAlpha(MinimapPalette.Glow, 1.6f));
            MinimapMeshKit.ChamferFill(mesh, frame, Chamfer, MinimapPalette.Metal);
            MinimapMeshKit.ChamferOutline(mesh, Expand(frame, -0.5f), Chamfer, 1f, MinimapPalette.MetalEdge);

            float top = frame.yMax - MinimapBoardLayout.HeaderHeight;
            float bottom = frame.yMin + MinimapBoardLayout.FooterHeight;
            MinimapMeshKit.Quad(mesh, Rect.MinMaxRect(frame.xMin + 4f, top, frame.xMax - 4f, frame.yMax - 4f), MinimapPalette.HeaderBar);
            MinimapMeshKit.Quad(mesh, Rect.MinMaxRect(frame.xMin + 4f, frame.yMin + 4f, frame.xMax - 4f, bottom), MinimapPalette.HeaderBar);

            Rect viewport = MinimapBoardLayout.ViewportRect(frame.width);
            viewport.position += frame.position;
            MinimapMeshKit.Quad(mesh, Expand(viewport, 1f), MinimapPalette.MetalEdge);
            MinimapMeshKit.Quad(mesh, viewport, MinimapPalette.Navy);

            MinimapMeshKit.ChamferOutline(mesh, Expand(frame, -3f), Chamfer - 2f, 1f, MinimapPalette.CyanSoft);
            Color separator = MinimapPalette.WithAlpha(MinimapPalette.Cyan, 0.32f);
            MinimapMeshKit.Quad(mesh, Rect.MinMaxRect(frame.xMin + 6f, top, frame.xMax - 6f, top + 1f), separator);
            MinimapMeshKit.Quad(mesh, Rect.MinMaxRect(frame.xMin + 6f, bottom - 1f, frame.xMax - 6f, bottom), separator);
            if (dividerFromRight > 0f)
            {
                float x = frame.xMax - dividerFromRight;
                MinimapMeshKit.Quad(mesh, Rect.MinMaxRect(x, frame.yMin + 8f, x + 1f, bottom - 7f), separator);
            }

            MinimapMeshKit.Brackets(mesh, Expand(frame, -1f), BracketArm, BracketThickness, MinimapPalette.Cyan);

            var lamp = new Vector2(frame.xMin + StatusLampCenterFromTopLeft.x, frame.yMax - StatusLampCenterFromTopLeft.y);
            MinimapMeshKit.Circle(mesh, lamp, 7f, MinimapPalette.WithAlpha(MinimapPalette.Cyan, 0.22f));
            MinimapMeshKit.Circle(mesh, lamp, 3.5f, MinimapPalette.Cyan);
        }

        private static Rect Expand(Rect rect, float amount) =>
            Rect.MinMaxRect(rect.xMin - amount, rect.yMin - amount, rect.xMax + amount, rect.yMax + amount);
    }
}
