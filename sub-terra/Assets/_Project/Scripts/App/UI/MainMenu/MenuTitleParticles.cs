using System;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>Sub-Terra 로고 글자 주변에서 청록색 입자가 위로 피어오르는 UI 파티클.</summary>
    public sealed class MenuTitleParticles : MonoBehaviour
    {
        private sealed class Particle
        {
            public RectTransform Rect;
            public Image Image;
            public float X;
            public float Y;
            public float Speed;
            public float Life;
            public float MaxLife;
            public float WobbleSeed;
            public float BaseAlpha;
        }

        [SerializeField] private int particleCount = 22;
        [SerializeField] private float spawnWidth = 540f;
        [SerializeField] private float spawnHeight = 50f;
        private Particle[] particles;
        private Sprite particleSprite;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (particles != null && particles.Length > 0) return;

            var tex = Texture2D.whiteTexture;
            particleSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            particles = new Particle[particleCount];
            var container = new GameObject("ParticleContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            container.SetParent(transform, false);
            container.anchorMin = container.anchorMax = container.pivot = new Vector2(0.5f, 0.5f);
            container.anchoredPosition = Vector2.zero;
            container.sizeDelta = new Vector2(spawnWidth, spawnHeight);

            for (var i = 0; i < particleCount; i++)
            {
                var go = new GameObject("P_" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(container, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                var size = UnityEngine.Random.Range(2f, 3.5f);
                rect.sizeDelta = new Vector2(size, size);

                var img = go.GetComponent<Image>();
                img.sprite = particleSprite;
                img.raycastTarget = false;

                var cyanMix = UnityEngine.Random.Range(0.2f, 0.85f);
                var color = Color.Lerp(new Color(0.90f, 0.98f, 1.0f), new Color(0.22f, 0.85f, 0.92f), cyanMix);
                img.color = color;

                var p = new Particle
                {
                    Rect = rect,
                    Image = img,
                    MaxLife = UnityEngine.Random.Range(1.6f, 3.2f),
                    Speed = UnityEngine.Random.Range(22f, 45f),
                    WobbleSeed = UnityEngine.Random.Range(0f, 100f),
                    BaseAlpha = UnityEngine.Random.Range(0.45f, 0.85f)
                };
                ResetParticle(p, true);
                particles[i] = p;
            }
        }

        private void ResetParticle(Particle p, bool initialSpread)
        {
            p.X = UnityEngine.Random.Range(-spawnWidth * 0.48f, spawnWidth * 0.48f);
            p.Y = initialSpread
                ? UnityEngine.Random.Range(-spawnHeight * 0.5f, spawnHeight * 1.5f)
                : UnityEngine.Random.Range(-spawnHeight * 0.5f, -spawnHeight * 0.1f);
            p.Life = initialSpread ? UnityEngine.Random.Range(0f, p.MaxLife) : 0f;
            p.Speed = UnityEngine.Random.Range(20f, 44f);
        }

        private void Update()
        {
            if (particles == null) return;
            var dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;

            for (var i = 0; i < particles.Length; i++)
            {
                var p = particles[i];
                p.Life += dt;
                if (p.Life >= p.MaxLife)
                {
                    ResetParticle(p, false);
                }

                p.Y += p.Speed * dt;
                var wobble = Mathf.Sin(Time.unscaledTime * 1.8f + p.WobbleSeed) * 0.35f;
                p.Rect.anchoredPosition = new Vector2(p.X + wobble, p.Y);

                var progress = p.Life / p.MaxLife;
                var alpha = progress < 0.2f
                    ? progress / 0.2f
                    : progress > 0.65f
                        ? (1f - progress) / 0.35f
                        : 1f;

                var c = p.Image.color;
                c.a = alpha * p.BaseAlpha;
                p.Image.color = c;
            }
        }
    }
}
