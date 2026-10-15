using System;
using UnityEngine;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>
    /// 블랙홀 둘레에서 피어올라 위로 퍼지는 청록색 입자 시뮬레이션. 순수 계산이라 Edit Mode에서 검사할 수 있다.
    /// 좌표는 블랙홀 중심 기준 UI 픽셀이며 시드가 고정이라 같은 입력이면 같은 결과가 나온다.
    /// </summary>
    public sealed class EscapeWormholeParticleField
    {
        public const int Capacity = 96;
        public const float SpawnPerSecond = 52f;
        public const float MinLife = 1.3f;
        public const float MaxLife = 2.2f;
        private const int Seed = 20261014;

        public struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Age;
            public float Life;
            public float Size;
            public float Phase;

            /// <summary>수명 비율(0~1).</summary>
            public float Progress => Life > 0f ? Mathf.Clamp01(Age / Life) : 1f;
        }

        private readonly Particle[] particles = new Particle[Capacity];
        private readonly System.Random random = new System.Random(Seed);
        private int count;
        private float spawnDebt;

        /// <summary>입자가 태어나는 고리 반지름(UI 픽셀). 검은 구멍 가장자리에 맞춘다.</summary>
        public float RingRadius { get; set; } = 76f;

        public int Count => count;

        public Particle this[int index] => particles[index];

        public void Clear()
        {
            count = 0;
            spawnDebt = 0f;
        }

        /// <summary>창이 열리는 순간 이미 입자가 피어오르고 있도록 미리 돌려 둔다.</summary>
        public void Prime(float seconds)
        {
            Clear();
            const float step = 1f / 30f;
            for (var t = 0f; t < seconds; t += step)
            {
                Tick(step, true);
            }
        }

        public void Tick(float dt, bool emit)
        {
            if (dt <= 0f)
            {
                return;
            }

            if (emit)
            {
                spawnDebt = Math.Min(spawnDebt + SpawnPerSecond * dt, 6f);
                while (spawnDebt >= 1f && count < Capacity)
                {
                    spawnDebt -= 1f;
                    Spawn();
                }
            }
            else
            {
                spawnDebt = 0f;
            }

            for (var i = count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.Age += dt;
                if (p.Age >= p.Life)
                {
                    particles[i] = particles[count - 1];
                    count--;
                    continue;
                }

                // 위로 갈수록 빨라지고 좌우로 부채꼴처럼 벌어지며, 살짝 흔들린다.
                p.Velocity.y += 46f * dt;
                p.Velocity.x += p.Position.x * 0.55f * dt;
                p.Position += p.Velocity * dt;
                p.Position.x += Mathf.Sin(p.Age * 4.2f + p.Phase) * 16f * dt;
                particles[i] = p;
            }
        }

        /// <summary>수명에 따른 투명도: 빠르게 나타나 오래 남다가 위에서 옅어진다.</summary>
        public static float AlphaAt(float progress)
        {
            progress = Mathf.Clamp01(progress);
            var fadeIn = Mathf.Clamp01(progress / 0.12f);
            var fadeOut = 1f - Mathf.Clamp01((progress - 0.45f) / 0.55f);
            return fadeIn * fadeIn * fadeOut;
        }

        /// <summary>수명에 따른 크기 배율: 올라갈수록 작아진다.</summary>
        public static float SizeScaleAt(float progress)
        {
            return 1f - 0.55f * Mathf.Clamp01(progress);
        }

        private void Spawn()
        {
            var angle = (float)random.NextDouble() * Mathf.PI * 2f;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var radius = RingRadius * (0.95f + (float)random.NextDouble() * 0.3f);
            var outward = 8f + (float)random.NextDouble() * 26f;
            var rise = 140f + (float)random.NextDouble() * 120f;
            var p = new Particle
            {
                Position = direction * radius,
                // 위쪽 반원에서 태어난 입자는 조금 더 빨리 솟는다.
                Velocity = new Vector2(direction.x * outward, rise + Mathf.Max(0f, direction.y) * 40f),
                Age = 0f,
                Life = MinLife + (float)random.NextDouble() * (MaxLife - MinLife),
                Size = 5f + (float)random.NextDouble() * 8f,
                Phase = (float)random.NextDouble() * Mathf.PI * 2f
            };
            particles[count++] = p;
        }
    }
}
