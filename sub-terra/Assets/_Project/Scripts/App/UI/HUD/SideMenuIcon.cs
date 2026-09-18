using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>컨셉의 시설·가방·상향·책·톱니·전원 아이콘을 청록 선으로 표현한다.</summary>
    public sealed class SideMenuIcon : UnityEngine.UI.MaskableGraphic
    {
        [SerializeField, Range(0, 5)] private int kind;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            switch (kind)
            {
                case 0:
                    Box(mesh, 0.1f, 0.08f, 0.32f, 0.46f); Box(mesh, 0.39f, 0.08f, 0.61f, 0.46f);
                    Box(mesh, 0.68f, 0.08f, 0.9f, 0.46f); Box(mesh, 0.39f, 0.54f, 0.61f, 0.94f); break;
                case 1:
                    Box(mesh, 0.16f, 0.08f, 0.84f, 0.78f); Box(mesh, 0.32f, 0.78f, 0.68f, 0.93f);
                    Box(mesh, 0.3f, 0.2f, 0.7f, 0.5f); break;
                case 2:
                    Line(mesh, 0.1f, 0.48f, 0.5f, 0.9f); Line(mesh, 0.5f, 0.9f, 0.9f, 0.48f);
                    Line(mesh, 0.1f, 0.16f, 0.5f, 0.58f); Line(mesh, 0.5f, 0.58f, 0.9f, 0.16f);
                    Line(mesh, 0.1f, 0.16f, 0.9f, 0.16f); break;
                case 3:
                    Box(mesh, 0.08f, 0.14f, 0.5f, 0.86f); Box(mesh, 0.5f, 0.14f, 0.92f, 0.86f);
                    for (int i = 0; i < 3; i++)
                    { float y = 0.35f + i * 0.17f; Line(mesh, 0.19f, y, 0.39f, y); Line(mesh, 0.61f, y, 0.81f, y); }
                    break;
                case 4:
                    for (int i = 0; i < 32; i++)
                    {
                        float a = i * Mathf.PI / 16f, b = (i + 1) * Mathf.PI / 16f;
                        float r = i % 4 < 2 ? 0.44f : 0.34f, s = (i + 1) % 4 < 2 ? 0.44f : 0.34f;
                        Line(mesh, 0.5f + Mathf.Cos(a) * r, 0.5f + Mathf.Sin(a) * r,
                            0.5f + Mathf.Cos(b) * s, 0.5f + Mathf.Sin(b) * s);
                    }
                    Arc(mesh, 0f, 360f, 0.16f); break;
                case 5:
                    Arc(mesh, 130f, 410f, 0.38f); Line(mesh, 0.5f, 0.98f, 0.5f, 0.53f); break;
            }
        }

        private void Arc(UnityEngine.UI.VertexHelper mesh, float start, float end, float radius)
        {
            for (int i = 0; i < 32; i++)
            {
                float a = Mathf.Lerp(start, end, i / 32f) * Mathf.Deg2Rad;
                float b = Mathf.Lerp(start, end, (i + 1) / 32f) * Mathf.Deg2Rad;
                Line(mesh, 0.5f + Mathf.Cos(a) * radius, 0.5f + Mathf.Sin(a) * radius,
                    0.5f + Mathf.Cos(b) * radius, 0.5f + Mathf.Sin(b) * radius);
            }
        }
        private void Box(UnityEngine.UI.VertexHelper mesh, float x, float y, float right, float top)
        {
            Line(mesh, x, y, right, y); Line(mesh, right, y, right, top);
            Line(mesh, right, top, x, top); Line(mesh, x, top, x, y);
        }
        private void Line(UnityEngine.UI.VertexHelper mesh, float x, float y, float xx, float yy)
        {
            var rect = rectTransform.rect;
            var a = new Vector2(rect.xMin + x * rect.width, rect.yMin + y * rect.height);
            var b = new Vector2(rect.xMin + xx * rect.width, rect.yMin + yy * rect.height);
            var delta = (b - a).normalized;
            var perpendicular = new Vector2(-delta.y, delta.x) * rect.width * 0.032f;
            int index = mesh.currentVertCount;
            mesh.AddVert(a - perpendicular, color, Vector2.zero); mesh.AddVert(a + perpendicular, color, Vector2.zero);
            mesh.AddVert(b + perpendicular, color, Vector2.zero); mesh.AddVert(b - perpendicular, color, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2); mesh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
