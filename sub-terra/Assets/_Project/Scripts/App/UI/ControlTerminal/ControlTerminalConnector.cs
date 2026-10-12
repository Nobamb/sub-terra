using UnityEngine;

namespace SubTerra.App.UI.ControlTerminal
{
    /// <summary>
    /// 키 모서리에서 고정된 시연 화면까지 가는 각진 연결선의 기하. 순수 계산이다.
    /// 시작점 → (통로 끝, 시작 높이) → (통로 끝, 화면 높이) → 화면 왼쪽. 꺾임은 최대 두 번이다.
    /// 시작점은 키 윗면을 따라 움직이고 끝점은 화면에 고정된다.
    /// </summary>
    public static class ControlTerminalConnector
    {
        public const int MaxSegments = 3;
        /// <summary>키 윗면 모서리에서 틈 쪽으로 띄우는 거리. 연결선이 이웃 키 글자를 가리지 않게 한다.</summary>
        public const float CornerLift = 4f;

        public static Vector2 StartOf(in ControlKeyDef key, float pressPixels)
        {
            if (key.IsMouse) return new Vector2(key.Right + 2f, key.Center.y - pressPixels);
            return new Vector2(key.Right, key.Top + CornerLift - pressPixels);
        }

        /// <summary>꺾임점을 points[0..3]에 채우고 겹치는 점은 그대로 둔다(길이 0 구간).</summary>
        public static void Build(Vector2 start, float exitX, Vector2 screenLeft, Vector2[] points)
        {
            points[0] = start;
            points[1] = new Vector2(exitX, start.y);
            points[2] = new Vector2(exitX, screenLeft.y);
            points[3] = screenLeft;
        }

        public static float Length(Vector2[] points)
        {
            var total = 0f;
            for (int i = 0; i < 3; i++) total += Vector2.Distance(points[i], points[i + 1]);
            return total;
        }

        /// <summary>
        /// progress(0~1)만큼 시작점에서 뻗은 구간을 segments[0..2]에 (시작,끝) 쌍으로 채우고
        /// 끝 점(선 머리)을 돌려준다. 길이가 0인 구간은 시작=끝이다.
        /// </summary>
        public static Vector2 Evaluate(Vector2[] points, float progress, Vector2[] segmentStarts, Vector2[] segmentEnds)
        {
            var remaining = Mathf.Clamp01(progress) * Length(points);
            var head = points[0];
            for (int i = 0; i < 3; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                var length = Vector2.Distance(a, b);
                segmentStarts[i] = a;
                if (length <= 0.0001f || remaining <= 0f)
                {
                    segmentEnds[i] = a;
                    continue;
                }

                var used = Mathf.Min(length, remaining);
                segmentEnds[i] = a + (b - a) * (used / length);
                head = segmentEnds[i];
                remaining -= used;
            }

            return head;
        }
    }
}
