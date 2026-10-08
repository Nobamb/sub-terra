using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 보관함 등장 연출의 수납 상자. 앞면·옆면이 보이는 사선 투영 정육면체를 청록 면 + 흰 외곽선으로 그린다.
    /// 윗면은 앞·뒤 두 장의 뚜껑이며 Lid(0 닫힘 → 1 열림)에 따라 경첩을 축으로 젖혀진다.
    /// 좌표는 이 RectTransform 피벗 기준이고, 입구(윗면) 중심은 항상 (0, Size/2)이다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StorageBoxGraphic : MaskableGraphic
    {
        public const float FrontFlapMaxDegrees = 210f;
        public const float BackFlapMaxDegrees = 160f;
        private const float OutlineWidth = 3f;

        private static readonly Color FrontFill = new Color(0.10f, 0.70f, 0.80f, 0.94f);
        private static readonly Color SideFill = new Color(0.05f, 0.44f, 0.54f, 0.96f);
        private static readonly Color FlapFill = new Color(0.32f, 0.88f, 0.95f, 0.96f);
        private static readonly Color FlapInner = new Color(0.08f, 0.52f, 0.62f, 0.96f);
        private static readonly Color InsideFill = new Color(0.015f, 0.10f, 0.14f, 1f);
        private static readonly Color Outline = new Color(1f, 1f, 1f, 0.95f);

        [SerializeField] private float size = 150f;
        [SerializeField] private float lid;

        public float Size
        {
            get => size;
            set
            {
                if (!Mathf.Approximately(size, value))
                {
                    size = value;
                    SetVerticesDirty();
                }
            }
        }

        public float Lid
        {
            get => lid;
            set
            {
                var clamped = Mathf.Clamp01(value);
                if (!Mathf.Approximately(lid, clamped))
                {
                    lid = clamped;
                    SetVerticesDirty();
                }
            }
        }

        /// <summary>사선 투영의 깊이 방향(오른쪽 위).</summary>
        public static Vector2 Depth(float boxSize)
        {
            return new Vector2(0.36f * boxSize, 0.30f * boxSize);
        }

        public static Vector2 Mouth(float boxSize)
        {
            return new Vector2(0f, boxSize * 0.5f);
        }

        /// <summary>앞 뚜껑의 경첩(앞 윗변)에서 끝까지의 벡터. 닫히면 깊이 방향 절반, 열리면 앞쪽 아래로 넘어간다.</summary>
        public static Vector2 FrontFlapEdge(float boxSize, float lidOpen)
        {
            var angle = Mathf.Clamp01(lidOpen) * FrontFlapMaxDegrees * Mathf.Deg2Rad;
            return 0.5f * (Mathf.Cos(angle) * Depth(boxSize) + Mathf.Sin(angle) * new Vector2(0f, boxSize));
        }

        /// <summary>뒤 뚜껑의 경첩(뒤 윗변)에서 끝까지의 벡터. 닫히면 앞쪽 절반, 열리면 뒤로 젖혀진다.</summary>
        public static Vector2 BackFlapEdge(float boxSize, float lidOpen)
        {
            var angle = Mathf.Clamp01(lidOpen) * BackFlapMaxDegrees * Mathf.Deg2Rad;
            return 0.5f * (-Mathf.Cos(angle) * Depth(boxSize) + Mathf.Sin(angle) * new Vector2(0f, boxSize));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var s = size;
            var depth = Depth(s);
            var o = -depth * 0.5f;
            var fbl = o + new Vector2(-s * 0.5f, -s * 0.5f);
            var fbr = o + new Vector2(s * 0.5f, -s * 0.5f);
            var ftr = o + new Vector2(s * 0.5f, s * 0.5f);
            var ftl = o + new Vector2(-s * 0.5f, s * 0.5f);
            var btl = ftl + depth;
            var btr = ftr + depth;
            var bbr = fbr + depth;

            var front = FrontFlapEdge(s, lid);
            var back = BackFlapEdge(s, lid);
            var frontAngle = lid * FrontFlapMaxDegrees;
            var backAngle = lid * BackFlapMaxDegrees;

            // 뒤에서 앞으로: 입구 안쪽 → 뒤 뚜껑 → 옆면 → 앞면 → 앞 뚜껑.
            Quad(vh, ftl, ftr, btr, btl, InsideFill);
            Quad(vh, btl, btr, btr + back, btl + back, backAngle > 90f ? FlapInner : FlapFill);
            Quad(vh, fbr, bbr, btr, ftr, SideFill);
            Quad(vh, fbl, fbr, ftr, ftl, FrontFill);
            Quad(vh, ftl, ftr, ftr + front, ftl + front, frontAngle > 90f ? FlapInner : FlapFill);
        }

        private void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color fill)
        {
            var tint = fill * color;
            var start = vh.currentVertCount;
            vh.AddVert(a, tint, Vector2.zero);
            vh.AddVert(b, tint, Vector2.zero);
            vh.AddVert(c, tint, Vector2.zero);
            vh.AddVert(d, tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);

            var line = Outline * color;
            Segment(vh, a, b, line);
            Segment(vh, b, c, line);
            Segment(vh, c, d, line);
            Segment(vh, d, a, line);
        }

        private static void Segment(VertexHelper vh, Vector2 a, Vector2 b, Color tint)
        {
            var dir = b - a;
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            dir.Normalize();
            var half = OutlineWidth * 0.5f;
            var normal = new Vector2(-dir.y, dir.x) * half;
            var ext = dir * half;
            var start = vh.currentVertCount;
            vh.AddVert(a - ext - normal, tint, Vector2.zero);
            vh.AddVert(a - ext + normal, tint, Vector2.zero);
            vh.AddVert(b + ext + normal, tint, Vector2.zero);
            vh.AddVert(b + ext - normal, tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
