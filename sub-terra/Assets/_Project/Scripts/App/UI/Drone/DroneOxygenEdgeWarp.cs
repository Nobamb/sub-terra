using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Drone
{
    /// <summary>
    /// B-213 산소 부족 경고 중 패널 배경(바깥 틀)만 가로 띠로 잘라 일부 띠를 옆으로 밀어 신호가 찢긴 듯 왜곡한다.
    /// 배경 Image의 메시만 바꾸므로 자식 글자·버튼은 움직이지 않는다. topExclusion(px) 위쪽 띠는 밀지 않는다.
    /// 같은 오브젝트의 Outline·Shadow보다 뒤에 붙어 그 복사본(빨간 테두리)도 같은 모양으로 찢긴다.
    /// JitterStep이 바뀔 때만 메시를 다시 만든다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DroneOxygenEdgeWarp : BaseMeshEffect
    {
        public const int StripCount = 22;
        public const float MinShift = 4f;
        public const float MaxShift = 12f;
        /// <summary>띠 기준 사각형 밖으로 이만큼(px) 넘게 벗어난 조각(후광 위아래 줄 등)은 밀지 않는다.</summary>
        public const float OutsideTolerance = 2f;

        private static readonly List<UIVertex> Input = new List<UIVertex>(64);
        private static readonly List<UIVertex> Output = new List<UIVertex>(1024);

        private float topExclusion;
        private RectTransform reference;
        private bool warping;
        private float frameElapsed;
        private int frameStep = -1;

        public bool IsWarping => warping;

        /// <param name="headerExclusion">위쪽에서 이 높이(px)는 밀지 않는다.</param>
        /// <param name="stripReference">
        /// 띠를 나눌 기준 패널. 비우면 자기 자신. 패널의 자식(후광)이 같은 띠로 함께 찢기게 할 때 패널을 넘긴다.
        /// </param>
        public void Configure(float headerExclusion, RectTransform stripReference = null)
        {
            topExclusion = Mathf.Max(0f, headerExclusion);
            reference = stripReference;
        }

        public void SetFrame(float elapsed)
        {
            var step = DroneOxygenWarningFx.StepOf(elapsed);
            if (warping && step == frameStep)
            {
                return;
            }

            warping = true;
            frameStep = step;
            frameElapsed = elapsed;
            Rebuild();
        }

        public void Clear()
        {
            if (!warping)
            {
                return;
            }

            warping = false;
            frameStep = -1;
            Rebuild();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!warping || !IsActive() || vh == null || graphic == null)
            {
                return;
            }

            var own = graphic.rectTransform;
            var rect = own.rect;
            if (reference != null && reference != own && own.parent == reference)
            {
                // 패널 자식의 로컬 좌표 = 패널 로컬 - 자식 위치(회전·크기 없음). 패널 열림 스케일이 0이어도 안전하다.
                var r = reference.rect;
                var lp = own.localPosition;
                rect = new Rect(r.x - lp.x, r.y - lp.y, r.width, r.height);
            }

            Input.Clear();
            vh.GetUIVertexStream(Input);
            Output.Clear();
            WarpStream(Input, Output, rect, topExclusion, frameElapsed);
            vh.Clear();
            vh.AddUIVertexTriangleStream(Output);
        }

        /// <summary>
        /// 삼각형 스트림의 축 정렬 사각형(6정점)을 가로 띠로 잘라 띠마다 가로로 민다. 사각형이 아니면 그대로 둔다.
        /// Simple·Sliced Image와 Outline·Shadow 복사본이 모두 축 정렬 사각형이다.
        /// </summary>
        public static void WarpStream(List<UIVertex> input, List<UIVertex> output, Rect rect, float topExclusion, float elapsed)
        {
            var stripHeight = rect.height / StripCount;
            if (stripHeight <= 0f)
            {
                output.AddRange(input);
                return;
            }

            var excludeAbove = rect.yMax - Mathf.Max(0f, topExclusion);
            for (var i = 0; i + 5 < input.Count; i += 6)
            {
                if (!TryReadQuad(input, i, out var min, out var max, out var uvMin, out var uvMax, out var color))
                {
                    for (var j = 0; j < 6; j++) output.Add(input[i + j]);
                    continue;
                }

                var y0 = min.y;
                var guard = 0;
                while (y0 < max.y - 0.001f && guard++ < StripCount + 4)
                {
                    var index = Mathf.FloorToInt((y0 - rect.yMin) / stripHeight + 0.0001f);
                    float y1;
                    if (index < 0)
                    {
                        y1 = Mathf.Min(max.y, rect.yMin);
                    }
                    else if (index >= StripCount)
                    {
                        y1 = max.y;
                    }
                    else
                    {
                        y1 = Mathf.Min(max.y, rect.yMin + (index + 1) * stripHeight);
                    }

                    if (y1 <= y0)
                    {
                        y1 = max.y;
                    }

                    var mid = (y0 + y1) * 0.5f;
                    var strip = Mathf.Clamp(Mathf.FloorToInt((mid - rect.yMin) / stripHeight), 0, StripCount - 1);
                    var outside = mid < rect.yMin - OutsideTolerance || mid > rect.yMax + OutsideTolerance;
                    var shift = DroneOxygenWarningFx.StripOffset(elapsed, strip, StripCount, outside || mid > excludeAbove);
                    AddStrip(output, min, max, uvMin, uvMax, color, y0, y1, shift);
                    y0 = y1;
                }
            }
        }

        private static bool TryReadQuad(
            List<UIVertex> input, int start,
            out Vector3 min, out Vector3 max, out Vector4 uvMin, out Vector4 uvMax, out Color32 color)
        {
            min = new Vector3(float.MaxValue, float.MaxValue, 0f);
            max = new Vector3(float.MinValue, float.MinValue, 0f);
            for (var j = 0; j < 6; j++)
            {
                var p = input[start + j].position;
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            uvMin = Vector4.zero;
            uvMax = Vector4.zero;
            color = input[start].color;
            var foundMin = false;
            var foundMax = false;
            for (var j = 0; j < 6; j++)
            {
                var v = input[start + j];
                var onX = Mathf.Approximately(v.position.x, min.x) || Mathf.Approximately(v.position.x, max.x);
                var onY = Mathf.Approximately(v.position.y, min.y) || Mathf.Approximately(v.position.y, max.y);
                if (!onX || !onY)
                {
                    return false;
                }

                if (Mathf.Approximately(v.position.x, min.x) && Mathf.Approximately(v.position.y, min.y))
                {
                    uvMin = v.uv0;
                    foundMin = true;
                }
                else if (Mathf.Approximately(v.position.x, max.x) && Mathf.Approximately(v.position.y, max.y))
                {
                    uvMax = v.uv0;
                    foundMax = true;
                }
            }

            return foundMin && foundMax && max.x > min.x && max.y > min.y;
        }

        private static void AddStrip(
            List<UIVertex> output, Vector3 min, Vector3 max, Vector4 uvMin, Vector4 uvMax, Color32 color,
            float y0, float y1, float shift)
        {
            var t0 = Mathf.InverseLerp(min.y, max.y, y0);
            var t1 = Mathf.InverseLerp(min.y, max.y, y1);
            var bl = Vertex(min.x + shift, y0, min.z, uvMin.x, Mathf.Lerp(uvMin.y, uvMax.y, t0), color);
            var tl = Vertex(min.x + shift, y1, min.z, uvMin.x, Mathf.Lerp(uvMin.y, uvMax.y, t1), color);
            var tr = Vertex(max.x + shift, y1, min.z, uvMax.x, Mathf.Lerp(uvMin.y, uvMax.y, t1), color);
            var br = Vertex(max.x + shift, y0, min.z, uvMax.x, Mathf.Lerp(uvMin.y, uvMax.y, t0), color);
            output.Add(bl);
            output.Add(tl);
            output.Add(tr);
            output.Add(tr);
            output.Add(br);
            output.Add(bl);
        }

        private static UIVertex Vertex(float x, float y, float z, float u, float v, Color32 color)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(x, y, z);
            vertex.uv0 = new Vector4(u, v, 0f, 0f);
            vertex.color = color;
            return vertex;
        }

        private void Rebuild()
        {
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }
    }
}
