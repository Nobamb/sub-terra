using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>
    /// 웜홀 중심으로 빨려 드는 청록 줄기(속도선)를 한 번에 그리는 UI 그래픽. 이미지 파일 없이 정점 색만 쓴다.
    /// 줄기는 바깥에서 안으로 가속하며 약간 휘어 들어가고, 머리는 밝고 꼬리는 투명하다.
    /// 좌표는 이 오브젝트의 로컬 원점(웜홀 중심) 기준 픽셀이며 시간만 같으면 항상 같은 모양이다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EscapeWormholeSuctionGraphic : MaskableGraphic
    {
        public const int StreakCount = 110;

        private static readonly Color Cool = new Color(0.30f, 0.95f, 0.95f, 1f);
        private static readonly Color Bright = new Color(0.86f, 1f, 1f, 1f);

        private float time;
        private float strength;
        private float outerRadius = 600f;
        private float holeRadius = 80f;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        /// <param name="animationTime">연출 시작부터의 시간(초)</param>
        /// <param name="strengthValue">0이면 아무것도 그리지 않는다. 1이 최대 세기.</param>
        /// <param name="outer">줄기가 시작되는 바깥 반지름(픽셀)</param>
        /// <param name="hole">줄기가 사라지는 웜홀 가장자리 반지름(픽셀)</param>
        public void Configure(float animationTime, float strengthValue, float outer, float hole)
        {
            time = animationTime;
            strength = Mathf.Clamp01(strengthValue);
            outerRadius = Mathf.Max(40f, outer);
            holeRadius = Mathf.Clamp(hole, 4f, outerRadius * 0.9f);
            SetVerticesDirty();
        }

        /// <summary>한 줄기의 위치(0~1 수명)와 반지름. 테스트에서 안쪽으로만 움직이는지 검사한다.</summary>
        public static float RadiusAt(float lifeT, float outer, float hole)
        {
            var remain = 1f - Mathf.Clamp01(lifeT);
            return hole + ((outer - hole) * Mathf.Pow(remain, 2.2f));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (strength <= 0.003f)
            {
                return;
            }

            var span = outerRadius - holeRadius;
            for (var i = 0; i < StreakCount; i++)
            {
                var angle = Hash(i, 1) * Mathf.PI * 2f;
                var period = Mathf.Lerp(0.55f, 1.05f, Hash(i, 2));
                var life = Mathf.Repeat((time / period) + Hash(i, 3), 1f);
                var headR = RadiusAt(life, outerRadius, holeRadius);
                // 빠를수록(안쪽일수록) 꼬리가 길게 늘어난다.
                var speedStretch = 0.25f + (1.1f * life);
                var length = span * 0.17f * speedStretch * Mathf.Lerp(0.7f, 1.3f, Hash(i, 4)) * strength;
                var tailR = Mathf.Min(outerRadius, headR + length);
                // 안쪽으로 갈수록 소용돌이 방향으로 휘어 든다.
                var twist = 0.9f;
                var headAngle = angle + (twist * (1f - (headR / outerRadius)));
                var tailAngle = angle + (twist * (1f - (tailR / outerRadius)));
                var head = new Vector2(Mathf.Cos(headAngle), Mathf.Sin(headAngle)) * headR;
                var tail = new Vector2(Mathf.Cos(tailAngle), Mathf.Sin(tailAngle)) * tailR;
                var along = (head - tail);
                if (along.sqrMagnitude < 0.01f)
                {
                    continue;
                }

                var normal = new Vector2(-along.y, along.x).normalized;
                var width = Mathf.Lerp(2.5f, 7f, Hash(i, 5)) * Mathf.Lerp(0.55f, 1f, life);
                // 나타날 때 서서히, 웜홀 가장자리에서 삼켜지며 사라진다.
                var fade = Mathf.Clamp01(life / 0.12f) * (1f - Mathf.Clamp01((life - 0.9f) / 0.1f));
                var alpha = fade * Mathf.Sqrt(strength) * Mathf.Lerp(0.55f, 1f, Hash(i, 6));
                if (alpha <= 0.01f)
                {
                    continue;
                }

                var headColor = Color.Lerp(Cool, Bright, Hash(i, 7) > 0.7f ? 0.85f : 0.35f);
                headColor.a = alpha;
                var tailColor = Cool;
                tailColor.a = 0f;
                var start = vh.currentVertCount;
                vh.AddVert(head + (normal * width * 0.5f), headColor, Vector2.zero);
                vh.AddVert(head - (normal * width * 0.5f), headColor, Vector2.zero);
                vh.AddVert(tail - (normal * width * 0.2f), tailColor, Vector2.zero);
                vh.AddVert(tail + (normal * width * 0.2f), tailColor, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }

        // 시드가 고정인 간단한 해시(0~1).
        private static float Hash(int index, int salt)
        {
            var x = Mathf.Sin((index * 12.9898f) + (salt * 78.233f)) * 43758.5453f;
            return x - Mathf.Floor(x);
        }
    }
}
