using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    public enum MinimapGlyph
    {
        None = 0,
        Hexagon = 1,
        Bolt = 2,
        Cross = 3,
        Arrows = 4,
        Person = 5,
        Ring = 6,
        Generic = 7,
        Ladder = 8,
        Support = 9,
        Beacon = 10,
        Lamp = 11,
        Cube = 12,
        Calculator = 13,
        Wormhole = 14
    }

    /// <summary>미니맵 전용 단색 도형 메시 도우미. 텍스처 없이 정점색만 쓰며 할당하지 않는다.</summary>
    public static class MinimapMeshKit
    {
        private static readonly Vector2[] Octagon = new Vector2[8];

        public static void Quad(VertexHelper mesh, Rect rect, Color tint)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;
            Quad(mesh, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin, rect.yMax),
                new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.yMin), tint);
        }

        public static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, tint, Vector2.zero);
            mesh.AddVert(b, tint, Vector2.zero);
            mesh.AddVert(c, tint, Vector2.zero);
            mesh.AddVert(d, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }

        public static void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, tint, Vector2.zero);
            mesh.AddVert(b, tint, Vector2.zero);
            mesh.AddVert(c, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
        }

        public static void Segment(VertexHelper mesh, Vector2 a, Vector2 b, float thickness, Color tint)
        {
            Vector2 direction = b - a;
            float length = direction.magnitude;
            if (length <= 0.0001f) return;
            Vector2 normal = new Vector2(-direction.y, direction.x) / length * (thickness * 0.5f);
            Quad(mesh, a - normal, a + normal, b + normal, b - normal, tint);
        }

        public static void Outline(VertexHelper mesh, Rect rect, float thickness, Color tint)
        {
            float t = Mathf.Min(thickness, Mathf.Min(rect.width, rect.height) * 0.5f);
            Quad(mesh, Rect.MinMaxRect(rect.xMin, rect.yMax - t, rect.xMax, rect.yMax), tint);
            Quad(mesh, Rect.MinMaxRect(rect.xMin, rect.yMin, rect.xMax, rect.yMin + t), tint);
            Quad(mesh, Rect.MinMaxRect(rect.xMin, rect.yMin + t, rect.xMin + t, rect.yMax - t), tint);
            Quad(mesh, Rect.MinMaxRect(rect.xMax - t, rect.yMin + t, rect.xMax, rect.yMax - t), tint);
        }

        public static void Circle(VertexHelper mesh, Vector2 center, float radius, Color tint, int segments = 20)
        {
            if (radius <= 0f) return;
            int start = mesh.currentVertCount;
            mesh.AddVert(center, tint, Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
                if (i > 0) mesh.AddTriangle(start, start + i, start + i + 1);
            }
        }

        public static void Ring(VertexHelper mesh, Vector2 center, float radius, float thickness, Color tint, int segments = 32)
        {
            if (radius <= 0f || thickness <= 0f) return;
            float inner = Mathf.Max(0f, radius - thickness);
            int start = mesh.currentVertCount;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                mesh.AddVert(center + direction * inner, tint, Vector2.zero);
                mesh.AddVert(center + direction * radius, tint, Vector2.zero);
                if (i == 0) continue;
                int a = start + (i - 1) * 2;
                mesh.AddTriangle(a, a + 1, a + 3);
                mesh.AddTriangle(a, a + 3, a + 2);
            }
        }

        /// <summary>정다각형 외곽선(flat-top 육각형 등).</summary>
        public static void PolygonOutline(VertexHelper mesh, Vector2 center, float radius, int sides, float rotation, float thickness, Color tint)
        {
            for (int i = 0; i < sides; i++)
            {
                float a0 = rotation + i * Mathf.PI * 2f / sides;
                float a1 = rotation + (i + 1) * Mathf.PI * 2f / sides;
                Vector2 p0 = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
                Vector2 p1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
                // 모서리 틈이 생기지 않게 두께 절반만큼 연장한다.
                Vector2 extend = (p1 - p0).normalized * (thickness * 0.5f);
                Segment(mesh, p0 - extend, p1 + extend, thickness, tint);
            }
        }

        public static void PolygonFill(VertexHelper mesh, Vector2 center, float radius, int sides, float rotation, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(center, tint, Vector2.zero);
            for (int i = 0; i <= sides; i++)
            {
                float angle = rotation + i * Mathf.PI * 2f / sides;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
                if (i > 0) mesh.AddTriangle(start, start + i, start + i + 1);
            }
        }

        /// <summary>각진 모서리(모따기) 사각형 채우기.</summary>
        public static void ChamferFill(VertexHelper mesh, Rect rect, float chamfer, Color tint)
        {
            FillOctagon(rect, chamfer);
            int start = mesh.currentVertCount;
            mesh.AddVert(rect.center, tint, Vector2.zero);
            for (int i = 0; i < 8; i++)
            {
                mesh.AddVert(Octagon[i], tint, Vector2.zero);
            }

            for (int i = 0; i < 8; i++)
            {
                mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 8);
            }
        }

        public static void ChamferOutline(VertexHelper mesh, Rect rect, float chamfer, float thickness, Color tint)
        {
            FillOctagon(rect, chamfer);
            for (int i = 0; i < 8; i++)
            {
                Vector2 p0 = Octagon[i];
                Vector2 p1 = Octagon[(i + 1) % 8];
                Vector2 extend = (p1 - p0).normalized * (thickness * 0.5f);
                Segment(mesh, p0 - extend, p1 + extend, thickness, tint);
            }
        }

        /// <summary>L자 모서리 브래킷. dir은 모서리에서 안쪽을 향하는 부호(±1).</summary>
        public static void Bracket(VertexHelper mesh, Vector2 corner, float dirX, float dirY, float arm, float thickness, Color tint)
        {
            Quad(mesh, Rect.MinMaxRect(
                Mathf.Min(corner.x, corner.x + dirX * arm), Mathf.Min(corner.y, corner.y + dirY * thickness),
                Mathf.Max(corner.x, corner.x + dirX * arm), Mathf.Max(corner.y, corner.y + dirY * thickness)), tint);
            Quad(mesh, Rect.MinMaxRect(
                Mathf.Min(corner.x, corner.x + dirX * thickness), Mathf.Min(corner.y + dirY * thickness, corner.y + dirY * arm),
                Mathf.Max(corner.x, corner.x + dirX * thickness), Mathf.Max(corner.y + dirY * thickness, corner.y + dirY * arm)), tint);
        }

        public static void Brackets(VertexHelper mesh, Rect rect, float arm, float thickness, Color tint)
        {
            Bracket(mesh, new Vector2(rect.xMin, rect.yMin), 1f, 1f, arm, thickness, tint);
            Bracket(mesh, new Vector2(rect.xMax, rect.yMin), -1f, 1f, arm, thickness, tint);
            Bracket(mesh, new Vector2(rect.xMin, rect.yMax), 1f, -1f, arm, thickness, tint);
            Bracket(mesh, new Vector2(rect.xMax, rect.yMax), -1f, -1f, arm, thickness, tint);
        }

        /// <summary>정사각 영역 안에 종류별 기호를 그린다. 기호는 rect 밖으로 나가지 않는다. spin은 웜홀 내부 회전각(라디안)이다.</summary>
        public static void Glyph(VertexHelper mesh, MinimapGlyph glyph, Rect rect, Color tint, float spin = 0f)
        {
            float size = Mathf.Min(rect.width, rect.height);
            if (size <= 0f) return;
            Vector2 c = rect.center;
            float h = size * 0.5f;
            float line = Mathf.Max(1.2f, size * 0.12f);
            switch (glyph)
            {
                case MinimapGlyph.Hexagon:
                    PolygonOutline(mesh, c, h * 0.92f - line * 0.5f, 6, 0f, line, tint);
                    PolygonFill(mesh, c, h * 0.42f, 6, 0f, tint);
                    break;
                case MinimapGlyph.Bolt:
                    Triangle(mesh, c + new Vector2(0.16f, 1f) * h, c + new Vector2(-0.56f, -0.12f) * h, c + new Vector2(0.12f, -0.12f) * h, tint);
                    Triangle(mesh, c + new Vector2(-0.16f, -1f) * h, c + new Vector2(0.56f, 0.12f) * h, c + new Vector2(-0.12f, 0.12f) * h, tint);
                    break;
                case MinimapGlyph.Cross:
                {
                    float arm = h * 0.9f;
                    float bar = h * 0.32f;
                    Quad(mesh, Rect.MinMaxRect(c.x - arm, c.y - bar, c.x + arm, c.y + bar), tint);
                    Quad(mesh, Rect.MinMaxRect(c.x - bar, c.y - arm, c.x + bar, c.y - bar), tint);
                    Quad(mesh, Rect.MinMaxRect(c.x - bar, c.y + bar, c.x + bar, c.y + arm), tint);
                    break;
                }
                case MinimapGlyph.Arrows:
                {
                    float gap = h * 0.12f;
                    float w = h * 0.72f;
                    Triangle(mesh, c + new Vector2(-w, gap), c + new Vector2(0f, h * 0.95f), c + new Vector2(w, gap), tint);
                    Triangle(mesh, c + new Vector2(-w, -gap), c + new Vector2(w, -gap), c + new Vector2(0f, -h * 0.95f), tint);
                    break;
                }
                case MinimapGlyph.Ring:
                    Ring(mesh, c, h, Mathf.Max(1f, size * 0.05f), tint);
                    break;
                case MinimapGlyph.Person:
                    Person(mesh, rect, tint);
                    break;
                case MinimapGlyph.Ladder:
                {
                    float rail = Mathf.Max(1f, rect.width * 0.08f);
                    Quad(mesh, Rect.MinMaxRect(rect.xMin + rect.width * 0.22f, rect.yMin, rect.xMin + rect.width * 0.22f + rail, rect.yMax), tint);
                    Quad(mesh, Rect.MinMaxRect(rect.xMax - rect.width * 0.22f - rail, rect.yMin, rect.xMax - rect.width * 0.22f, rect.yMax), tint);
                    int rungs = Mathf.Max(2, Mathf.RoundToInt(rect.height / Mathf.Max(4f, rect.width * 0.35f)));
                    for (int i = 0; i < rungs; i++)
                    {
                        float y = rect.yMin + (i + 0.5f) * rect.height / rungs;
                        Quad(mesh, Rect.MinMaxRect(rect.xMin + rect.width * 0.22f, y - rail * 0.5f, rect.xMax - rect.width * 0.22f, y + rail * 0.5f), tint);
                    }

                    break;
                }
                case MinimapGlyph.Support:
                {
                    float plate = Mathf.Max(1f, rect.height * 0.1f);
                    Quad(mesh, Rect.MinMaxRect(rect.xMin + rect.width * 0.12f, rect.yMax - plate, rect.xMax - rect.width * 0.12f, rect.yMax), tint);
                    Quad(mesh, Rect.MinMaxRect(rect.xMin + rect.width * 0.12f, rect.yMin, rect.xMax - rect.width * 0.12f, rect.yMin + plate), tint);
                    Quad(mesh, Rect.MinMaxRect(c.x - rect.width * 0.1f, rect.yMin + plate, c.x + rect.width * 0.1f, rect.yMax - plate), tint);
                    break;
                }
                case MinimapGlyph.Generic:
                    PolygonOutline(mesh, c, h * 0.62f, 4, Mathf.PI * 0.25f, line, tint);
                    break;
                case MinimapGlyph.Beacon:
                    Circle(mesh, c, h, new Color(tint.r, tint.g, tint.b, tint.a * 0.28f), 20);
                    Circle(mesh, c, h * 0.78f, new Color(tint.r, tint.g, tint.b, tint.a * 0.5f), 20);
                    Circle(mesh, c, h * 0.52f, tint, 16);
                    break;
                case MinimapGlyph.Lamp:
                    Lamp(mesh, rect, tint);
                    break;
                case MinimapGlyph.Cube:
                    Cube(mesh, c, h * 0.95f, tint);
                    break;
                case MinimapGlyph.Calculator:
                    Calculator(mesh, rect, tint);
                    break;
                case MinimapGlyph.Wormhole:
                    Wormhole(mesh, c, h * 0.95f, tint, spin);
                    break;
            }
        }

        /// <summary>전구 + 받침대 + 빛살. 세로로 긴 영역(1x2)에도 맞는다.</summary>
        public static void Lamp(VertexHelper mesh, Rect rect, Color tint)
        {
            float w = rect.width;
            float h = rect.height;
            if (w <= 0f || h <= 0f) return;
            float cx = rect.center.x;
            float r = Mathf.Min(w * 0.24f, h * 0.2f);
            float cy = rect.yMin + h * 0.62f;
            float line = Mathf.Max(1f, Mathf.Min(w, h) * 0.08f);
            Circle(mesh, new Vector2(cx, cy), r, tint, 16);
            float neckTop = cy - r * 0.7f;
            float neckBottom = neckTop - Mathf.Max(h * 0.1f, r * 0.8f);
            float neckHalf = r * 0.55f;
            Quad(mesh, Rect.MinMaxRect(cx - neckHalf, neckBottom, cx + neckHalf, neckTop), WithAlphaScale(tint, 0.8f));
            float baseHeight = Mathf.Max(1f, h * 0.07f);
            float poleHalf = Mathf.Max(0.5f, w * 0.05f);
            if (neckBottom > rect.yMin + baseHeight)
                Quad(mesh, Rect.MinMaxRect(cx - poleHalf, rect.yMin + baseHeight, cx + poleHalf, neckBottom), tint);
            Quad(mesh, Rect.MinMaxRect(cx - w * 0.28f, rect.yMin, cx + w * 0.28f, rect.yMin + baseHeight), tint);
            // 위쪽 반원 방향으로 퍼지는 빛살.
            for (int i = 0; i < 5; i++)
            {
                float angle = Mathf.PI - i * Mathf.PI * 0.25f;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Segment(mesh, new Vector2(cx, cy) + dir * (r * 1.4f), new Vector2(cx, cy) + dir * (r * 1.9f), line * 0.8f, WithAlphaScale(tint, 0.75f));
            }
        }

        /// <summary>등각 정육면체. 윗면이 가장 밝고 왼쪽·오른쪽 면이 어둡다.</summary>
        public static void Cube(VertexHelper mesh, Vector2 center, float radius, Color tint)
        {
            if (radius <= 0f) return;
            float dx = radius * 0.866f;
            float dy = radius * 0.5f;
            Vector2 top = center + new Vector2(0f, radius);
            Vector2 bottom = center - new Vector2(0f, radius);
            Vector2 upperLeft = center + new Vector2(-dx, dy);
            Vector2 upperRight = center + new Vector2(dx, dy);
            Vector2 lowerLeft = center + new Vector2(-dx, -dy);
            Vector2 lowerRight = center + new Vector2(dx, -dy);
            Quad(mesh, top, upperRight, center, upperLeft, tint);
            Quad(mesh, upperLeft, center, bottom, lowerLeft, Shade(tint, 0.5f));
            Quad(mesh, center, upperRight, lowerRight, bottom, Shade(tint, 0.74f));
        }

        /// <summary>계산기: 외곽 + 화면 + 3x3 버튼. 가로보다 세로가 긴 비율로 rect 안에 맞춘다.</summary>
        public static void Calculator(VertexHelper mesh, Rect rect, Color tint)
        {
            float height = rect.height;
            float width = Mathf.Min(rect.width, height * 0.78f);
            if (width <= 0f || height <= 0f) return;
            var body = new Rect(rect.center.x - width * 0.5f, rect.yMin, width, height);
            float line = Mathf.Max(1f, width * 0.1f);
            Outline(mesh, body, line, tint);
            float pad = line * 1.3f;
            float screenTop = body.yMax - pad;
            float screenBottom = screenTop - height * 0.24f;
            Quad(mesh, Rect.MinMaxRect(body.xMin + pad, screenBottom, body.xMax - pad, screenTop), WithAlphaScale(tint, 0.85f));
            var keys = Rect.MinMaxRect(body.xMin + pad, body.yMin + pad, body.xMax - pad, screenBottom - pad);
            if (keys.width <= 0f || keys.height <= 0f) return;
            float pitchX = keys.width / 3f;
            float pitchY = keys.height / 3f;
            float dot = Mathf.Max(1f, Mathf.Min(pitchX, pitchY) * 0.6f);
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
            {
                var key = new Vector2(keys.xMin + (col + 0.5f) * pitchX, keys.yMin + (row + 0.5f) * pitchY);
                Quad(mesh, Rect.MinMaxRect(key.x - dot * 0.5f, key.y - dot * 0.5f, key.x + dot * 0.5f, key.y + dot * 0.5f), tint);
            }
        }

        /// <summary>웜홀: 고정 바깥 고리 + 회전하는 나선 팔 3개. spin만 바뀌며 바깥 고리는 움직이지 않는다.</summary>
        public static void Wormhole(VertexHelper mesh, Vector2 center, float radius, Color tint, float spin)
        {
            if (radius <= 0f) return;
            float line = Mathf.Max(1f, radius * 0.16f);
            Ring(mesh, center, radius, Mathf.Max(1f, radius * 0.12f), WithAlphaScale(tint, 0.9f), 28);
            const int arms = 3;
            const int steps = 7;
            for (int arm = 0; arm < arms; arm++)
            {
                float baseAngle = spin + arm * Mathf.PI * 2f / arms;
                Vector2 previous = default;
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    float angle = baseAngle + t * 1.6f;
                    var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * (0.2f + 0.62f * t));
                    if (i > 0) Segment(mesh, previous, point, line, WithAlphaScale(tint, 0.5f + 0.5f * t));
                    previous = point;
                }
            }

            Circle(mesh, center, radius * 0.15f, tint, 12);
        }

        private static Color Shade(Color color, float factor) => new(color.r * factor, color.g * factor, color.b * factor, color.a);

        private static Color WithAlphaScale(Color color, float alpha) => new(color.r, color.g, color.b, color.a * alpha);

        /// <summary>발이 rect 하단에 닿는 사람 실루엣.</summary>
        public static void Person(VertexHelper mesh, Rect rect, Color tint)
        {
            float w = rect.width;
            float h = rect.height;
            float cx = rect.center.x;
            float head = Mathf.Min(w * 0.36f, h * 0.15f);
            Circle(mesh, new Vector2(cx, rect.yMax - head), head, tint, 14);
            float torsoTop = rect.yMax - head * 2.15f;
            float hip = rect.yMin + h * 0.42f;
            Quad(mesh, Rect.MinMaxRect(cx - w * 0.24f, hip, cx + w * 0.24f, torsoTop), tint);
            float arm = Mathf.Max(1f, w * 0.12f);
            Quad(mesh, Rect.MinMaxRect(cx - w * 0.42f, hip + h * 0.06f, cx - w * 0.42f + arm, torsoTop - h * 0.02f), tint);
            Quad(mesh, Rect.MinMaxRect(cx + w * 0.42f - arm, hip + h * 0.06f, cx + w * 0.42f, torsoTop - h * 0.02f), tint);
            float leg = Mathf.Max(1f, w * 0.17f);
            Quad(mesh, Rect.MinMaxRect(cx - w * 0.21f, rect.yMin, cx - w * 0.21f + leg, hip), tint);
            Quad(mesh, Rect.MinMaxRect(cx + w * 0.21f - leg, rect.yMin, cx + w * 0.21f, hip), tint);
        }

        private static void FillOctagon(Rect rect, float chamfer)
        {
            float k = Mathf.Clamp(chamfer, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            Octagon[0] = new Vector2(rect.xMin + k, rect.yMin);
            Octagon[1] = new Vector2(rect.xMax - k, rect.yMin);
            Octagon[2] = new Vector2(rect.xMax, rect.yMin + k);
            Octagon[3] = new Vector2(rect.xMax, rect.yMax - k);
            Octagon[4] = new Vector2(rect.xMax - k, rect.yMax);
            Octagon[5] = new Vector2(rect.xMin + k, rect.yMax);
            Octagon[6] = new Vector2(rect.xMin, rect.yMax - k);
            Octagon[7] = new Vector2(rect.xMin, rect.yMin + k);
        }
    }
}
