using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 꺾인 선(여러 가닥)을 두께가 있는 띠로 그리는 UI 그래픽.
    /// 보건소 심전도가 진행하며 그려지는 느낌과 충전기 전기 아크의 들쭉날쭉한 선에 쓴다.
    /// 좌표는 이 RectTransform의 피벗 기준이다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FacilityServiceLineGraphic : MaskableGraphic
    {
        [SerializeField] private float thickness = 3f;

        private readonly List<Vector2> points = new List<Vector2>();
        private readonly List<int> strandLengths = new List<int>();

        public float Thickness
        {
            get => thickness;
            set
            {
                if (!Mathf.Approximately(thickness, value))
                {
                    thickness = value;
                    SetVerticesDirty();
                }
            }
        }

        public bool HasLines => points.Count >= 2;

        public void Clear()
        {
            if (points.Count == 0)
            {
                return;
            }

            points.Clear();
            strandLengths.Clear();
            SetVerticesDirty();
        }

        /// <summary>한 가닥짜리 선을 설정한다.</summary>
        public void SetSingle(IReadOnlyList<Vector2> line)
        {
            points.Clear();
            strandLengths.Clear();
            if (line != null && line.Count >= 2)
            {
                for (var i = 0; i < line.Count; i++)
                {
                    points.Add(line[i]);
                }

                strandLengths.Add(line.Count);
            }

            SetVerticesDirty();
        }

        public void BeginStrands()
        {
            points.Clear();
            strandLengths.Clear();
        }

        public void AddStrand(IReadOnlyList<Vector2> line)
        {
            if (line == null || line.Count < 2)
            {
                return;
            }

            for (var i = 0; i < line.Count; i++)
            {
                points.Add(line[i]);
            }

            strandLengths.Add(line.Count);
        }

        public void EndStrands()
        {
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var half = Mathf.Max(0.1f, thickness) * 0.5f;
            var offset = 0;
            for (var s = 0; s < strandLengths.Count; s++)
            {
                var count = strandLengths[s];
                for (var i = 0; i < count - 1; i++)
                {
                    AddSegment(vh, points[offset + i], points[offset + i + 1], half);
                }

                offset += count;
            }
        }

        private void AddSegment(VertexHelper vh, Vector2 a, Vector2 b, float half)
        {
            var dir = b - a;
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            dir.Normalize();
            var normal = new Vector2(-dir.y, dir.x) * half;
            // 꼭짓점이 끊겨 보이지 않도록 양 끝을 두께의 절반만큼 늘린다.
            var ext = dir * half;
            var start = vh.currentVertCount;
            vh.AddVert(a - ext - normal, color, Vector2.zero);
            vh.AddVert(a - ext + normal, color, Vector2.zero);
            vh.AddVert(b + ext + normal, color, Vector2.zero);
            vh.AddVert(b + ext - normal, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
