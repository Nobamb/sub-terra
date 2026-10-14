using SubTerra.App.UI.Outpost;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>
    /// EscapeWormholeParticleField의 입자를 한 번에 그리는 UI 그래픽. 입자마다 오브젝트를 만들지 않고 사각형만 채운다.
    /// 값 적용만 하며 시간은 View가 Tick으로 넘겨 준다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EscapeWormholeParticleGraphic : MaskableGraphic
    {
        private static readonly Color Spark = new Color(0.32f, 1f, 0.92f, 1f);
        private static readonly Color SparkBright = new Color(0.82f, 1f, 1f, 1f);

        [SerializeField] private float ringRadius = 76f;

        private readonly EscapeWormholeParticleField field = new EscapeWormholeParticleField();

        public EscapeWormholeParticleField Field
        {
            get
            {
                field.RingRadius = ringRadius;
                return field;
            }
        }

        public override Texture mainTexture
        {
            get
            {
                var dot = CoreCctvArt.Dot();
                return dot != null ? dot.texture : s_WhiteTexture;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void Tick(float dt, bool emit)
        {
            Field.Tick(dt, emit);
            SetVerticesDirty();
        }

        public void Prime(float seconds)
        {
            Field.Prime(seconds);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for (var i = 0; i < field.Count; i++)
            {
                var p = field[i];
                var progress = p.Progress;
                var alpha = EscapeWormholeParticleField.AlphaAt(progress) * color.a;
                if (alpha <= 0.002f)
                {
                    continue;
                }

                var tint = Color.Lerp(Spark, SparkBright, 0.5f + 0.5f * Mathf.Sin(p.Phase));
                tint.a = alpha;
                var half = p.Size * EscapeWormholeParticleField.SizeScaleAt(progress) * 0.5f;
                var start = vh.currentVertCount;
                Color32 c = tint;
                vh.AddVert(new Vector3(p.Position.x - half, p.Position.y - half), c, new Vector2(0f, 0f));
                vh.AddVert(new Vector3(p.Position.x - half, p.Position.y + half), c, new Vector2(0f, 1f));
                vh.AddVert(new Vector3(p.Position.x + half, p.Position.y + half), c, new Vector2(1f, 1f));
                vh.AddVert(new Vector3(p.Position.x + half, p.Position.y - half), c, new Vector2(1f, 0f));
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
