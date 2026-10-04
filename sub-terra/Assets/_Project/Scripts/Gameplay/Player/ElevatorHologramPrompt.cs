using System;
using TMPro;
using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>
    /// 엘리베이터 위 월드 홀로그램 안내. 표시 여부는 ElevatorController.PromptKind가 정하고,
    /// 이 컴포넌트는 펼침/수축 모션과 청록 사각 파티클 연출만 맡는다.
    /// </summary>
    public sealed class ElevatorHologramPrompt : MonoBehaviour
    {
        [SerializeField] private ElevatorController elevator;
        // 피벗은 패널 하단 중앙. 엘리베이터 상단에서 위로 펼쳐진다.
        [SerializeField] private Transform panel;
        [SerializeField] private SpriteRenderer[] panelSprites;
        [SerializeField] private TMP_Text label;
        [SerializeField] private SpriteRenderer[] particles;
        [SerializeField, Min(0.05f)] private float showDuration = 0.28f;
        [SerializeField, Min(0.05f)] private float hideDuration = 0.14f;
        [SerializeField, Min(0f)] private float riseOffset = 0.3f;
        [SerializeField] private Vector2 particleAreaCenter = new(0f, 0.3f);
        [SerializeField] private Vector2 particleAreaHalfSize = new(1.15f, 0.35f);
        [SerializeField, Min(0)] private int burstCount = 12;
        [SerializeField, Min(0.05f)] private float trickleInterval = 0.5f;

        private const float MaxStep = 0.05f;
        private const float HideParticleTail = 0.12f;
        private static readonly Color ParticleDim = new(0.20f, 0.88f, 0.92f, 1f);
        private static readonly Color ParticleBright = new(0.72f, 1f, 0.98f, 1f);

        private readonly System.Random random = new(119);
        private Color[] spriteBase;
        private Color labelBase = Color.white;
        private float[] age;
        private float[] life;
        private float[] rise;
        private float[] size;
        private Vector2[] origin;
        private Color[] tint;
        private int aliveCount;
        private float visibility;
        private float trickleTimer;
        private bool shown;
        private bool initialized;

        public bool IsShown => shown;
        public float Visibility => visibility;
        public string CurrentLabel => label != null ? label.text : string.Empty;

        private void OnEnable()
        {
            try
            {
                Initialize();
                if (elevator != null)
                {
                    elevator.PromptChanged += OnPromptChanged;
                    SetPrompt(elevator.PromptKind);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("[ElevatorHologramPrompt] 초기화 실패: " + exception.Message, this);
            }
        }

        private void OnDisable()
        {
            if (elevator != null)
            {
                elevator.PromptChanged -= OnPromptChanged;
            }

            HideImmediately();
        }

        private void Update()
        {
            float step = Mathf.Min(Time.unscaledDeltaTime, MaxStep);
            if (shown && visibility < 1f)
            {
                visibility = Mathf.MoveTowards(visibility, 1f, step / showDuration);
                ApplyPose();
            }
            else if (!shown && visibility > 0f)
            {
                visibility = Mathf.MoveTowards(visibility, 0f, step / hideDuration);
                ApplyPose();
                if (visibility <= 0f && panel != null)
                {
                    panel.gameObject.SetActive(false);
                }
            }

            if (shown && visibility >= 0.5f)
            {
                trickleTimer -= step;
                if (trickleTimer <= 0f)
                {
                    trickleTimer = trickleInterval;
                    SpawnParticle(0f);
                }
            }

            if (aliveCount > 0)
            {
                UpdateParticles(step);
            }
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (elevator == null)
            {
                elevator = GetComponentInParent<ElevatorController>();
            }

            int spriteCount = panelSprites != null ? panelSprites.Length : 0;
            spriteBase = new Color[spriteCount];
            for (int i = 0; i < spriteCount; i++)
            {
                spriteBase[i] = panelSprites[i] != null ? panelSprites[i].color : Color.white;
            }

            if (label != null)
            {
                labelBase = label.color;
            }

            int particleCount = particles != null ? particles.Length : 0;
            age = new float[particleCount];
            life = new float[particleCount];
            rise = new float[particleCount];
            size = new float[particleCount];
            origin = new Vector2[particleCount];
            tint = new Color[particleCount];
            initialized = true;
        }

        private void OnPromptChanged()
        {
            SetPrompt(elevator != null ? elevator.PromptKind : ElevatorPromptKind.None);
        }

        private void SetPrompt(ElevatorPromptKind kind)
        {
            if (kind == ElevatorPromptKind.None)
            {
                if (shown)
                {
                    BeginHide();
                }

                return;
            }

            string text = ElevatorPromptResolver.GetLabel(kind);
            if (label != null && label.text != text)
            {
                label.text = text;
            }

            if (!shown)
            {
                BeginShow();
            }
        }

        private void BeginShow()
        {
            shown = true;
            if (panel != null)
            {
                panel.gameObject.SetActive(true);
            }

            ApplyPose();
            trickleTimer = trickleInterval;
            for (int i = 0; i < burstCount; i++)
            {
                SpawnParticle(-(float)random.NextDouble() * 0.12f);
            }
        }

        private void BeginHide()
        {
            shown = false;
            // 창이 사라질 때 남은 사각 파티클도 짧게 정리한다.
            for (int i = 0; i < age.Length; i++)
            {
                if (life[i] <= 0f)
                {
                    continue;
                }

                if (age[i] < 0f)
                {
                    KillParticle(i);
                }
                else
                {
                    age[i] = Mathf.Max(age[i], life[i] - HideParticleTail);
                }
            }
        }

        private void HideImmediately()
        {
            shown = false;
            visibility = 0f;
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }

            if (age != null)
            {
                for (int i = 0; i < age.Length; i++)
                {
                    KillParticle(i);
                }
            }
        }

        private void ApplyPose()
        {
            if (panel == null)
            {
                return;
            }

            float v = visibility;
            float scaleX;
            float scaleY;
            float offsetY;
            float alpha;
            float textAlpha;
            if (shown)
            {
                // 가는 선처럼 먼저 가로로 번지고, 세로로 살짝 넘치며 펼쳐진다.
                scaleX = Mathf.Lerp(0.15f, 1f, EaseOutCubic(Mathf.Clamp01(v * 1.8f)));
                scaleY = EaseOutBack(v);
                offsetY = Mathf.Lerp(-riseOffset, 0f, EaseOutCubic(v));
                alpha = Mathf.Clamp01(v * 1.6f);
                textAlpha = Mathf.Clamp01((v - 0.4f) / 0.6f);
            }
            else
            {
                // 되감지 않고 아래로 빨려들듯 수축한다.
                scaleX = Mathf.Lerp(0.85f, 1f, v);
                scaleY = Mathf.Lerp(0.2f, 1f, v);
                offsetY = Mathf.Lerp(-riseOffset * 0.7f, 0f, v);
                alpha = v;
                textAlpha = v;
            }

            panel.localScale = new Vector3(scaleX, Mathf.Max(scaleY, 0.001f), 1f);
            panel.localPosition = new Vector3(0f, offsetY, 0f);

            for (int i = 0; i < spriteBase.Length; i++)
            {
                if (panelSprites[i] == null)
                {
                    continue;
                }

                Color color = spriteBase[i];
                color.a *= alpha;
                panelSprites[i].color = color;
            }

            if (label != null)
            {
                Color color = labelBase;
                color.a *= textAlpha;
                label.color = color;
            }
        }

        private void SpawnParticle(float startAge)
        {
            for (int i = 0; i < age.Length; i++)
            {
                if (life[i] > 0f || particles[i] == null)
                {
                    continue;
                }

                age[i] = startAge;
                life[i] = Mathf.Lerp(0.7f, 1.2f, (float)random.NextDouble());
                rise[i] = Mathf.Lerp(0.35f, 0.8f, (float)random.NextDouble());
                size[i] = Mathf.Lerp(0.035f, 0.09f, (float)random.NextDouble());
                origin[i] = particleAreaCenter + new Vector2(
                    ((float)random.NextDouble() * 2f - 1f) * particleAreaHalfSize.x,
                    ((float)random.NextDouble() * 2f - 1f) * particleAreaHalfSize.y);
                tint[i] = Color.Lerp(ParticleDim, ParticleBright, (float)random.NextDouble());
                aliveCount++;
                return;
            }
        }

        private void UpdateParticles(float step)
        {
            for (int i = 0; i < age.Length; i++)
            {
                if (life[i] <= 0f)
                {
                    continue;
                }

                age[i] += step;
                if (age[i] < 0f)
                {
                    continue;
                }

                float t = age[i] / life[i];
                if (t >= 1f)
                {
                    KillParticle(i);
                    continue;
                }

                SpriteRenderer renderer = particles[i];
                float fade = Mathf.Sin(t * Mathf.PI);
                float scale = size[i] * (1f - 0.35f * t);
                renderer.transform.localPosition = new Vector3(
                    origin[i].x, origin[i].y + rise[i] * (1f - (1f - t) * (1f - t)), 0f);
                renderer.transform.localScale = new Vector3(scale, scale, 1f);
                Color color = tint[i];
                color.a = fade * 0.9f;
                renderer.color = color;
                renderer.enabled = true;
            }
        }

        private void KillParticle(int index)
        {
            if (life[index] > 0f)
            {
                aliveCount = Mathf.Max(0, aliveCount - 1);
            }

            life[index] = 0f;
            age[index] = 0f;
            if (particles[index] != null)
            {
                particles[index].enabled = false;
            }
        }

        private static float EaseOutCubic(float t)
        {
            float inverse = 1f - Mathf.Clamp01(t);
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.2f;
            float shifted = Mathf.Clamp01(t) - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
        }
    }
}
