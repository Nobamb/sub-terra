using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// 광부 선택 카드의 홀로그램 입자(받침대에서 피어오르는 빛 알갱이, 스캔 줄 불꽃).
    /// Screen Space Overlay 캔버스에서는 ParticleSystem이 보이지 않아 메시 하나로 직접 그린다.
    /// 시간은 패널이 Tick으로 넣는다(입자 수 상한 있음, 매 프레임 할당 없음).
    /// </summary>
    public sealed class MinerSelectParticleField : MaskableGraphic
    {
        public const int MaxParticles = 220;

        private struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Age;
            public float Life;
            public float Size;
            public Color Color;
            public float Twinkle;
        }

        private readonly List<Particle> particles = new List<Particle>(MaxParticles);
        private Texture dot;

        public int Count => particles.Count;

        public override Texture mainTexture => dot != null ? dot : s_WhiteTexture;

        public static MinerSelectParticleField Create(Transform parent, string name, Vector2 size)
        {
            var rect = MinerSelectPanel.NewRect(parent, name, Vector2.zero, size);
            var field = rect.gameObject.AddComponent<MinerSelectParticleField>();
            field.raycastTarget = false;
            var sprite = MinerSelectArt.SoftDisc();
            field.dot = sprite != null ? sprite.texture : null;
            return field;
        }

        public void Emit(Vector2 position, Vector2 velocity, float size, float life, Color color)
        {
            if (particles.Count >= MaxParticles || life <= 0f) return;
            particles.Add(new Particle
            {
                Position = position,
                Velocity = velocity,
                Life = life,
                Size = size,
                Color = color,
                Twinkle = Random.value * 6.28f
            });
        }

        public void Clear()
        {
            particles.Clear();
            SetVerticesDirty();
        }

        public void Tick(float dt)
        {
            if (dt <= 0f && particles.Count == 0) return;
            for (var i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.Age += dt;
                if (p.Age >= p.Life)
                {
                    particles.RemoveAt(i);
                    continue;
                }

                p.Position += p.Velocity * dt;
                // 위로 오를수록 조금씩 느려지며 흔들린다.
                p.Velocity.x += Mathf.Sin(p.Age * 3f + p.Twinkle) * 6f * dt;
                p.Velocity *= 1f - 0.35f * dt;
                particles[i] = p;
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for (var i = 0; i < particles.Count; i++)
            {
                var p = particles[i];
                var k = p.Age / p.Life;
                // 빠르게 나타나 천천히 사라지고, 반짝인다.
                var fade = Mathf.Clamp01(k / 0.15f) * Mathf.Clamp01((1f - k) / 0.45f);
                var twinkle = 0.75f + 0.25f * Mathf.Sin(p.Age * 18f + p.Twinkle);
                var c = p.Color;
                c.a *= fade * twinkle;
                var h = p.Size * 0.5f * (0.7f + 0.3f * fade);
                var start = vh.currentVertCount;
                vh.AddVert(new Vector3(p.Position.x - h, p.Position.y - h), c, new Vector2(0f, 0f));
                vh.AddVert(new Vector3(p.Position.x - h, p.Position.y + h), c, new Vector2(0f, 1f));
                vh.AddVert(new Vector3(p.Position.x + h, p.Position.y + h), c, new Vector2(1f, 1f));
                vh.AddVert(new Vector3(p.Position.x + h, p.Position.y - h), c, new Vector2(1f, 0f));
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start + 2, start + 3, start);
            }
        }
    }
}
