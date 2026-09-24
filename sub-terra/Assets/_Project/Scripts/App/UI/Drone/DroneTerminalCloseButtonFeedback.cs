using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.Drone
{
    /// <summary>
    /// Digger-Bot 터미널의 닫기 버튼에만 쓰는 짧은 호버·눌림 피드백.
    /// 패널을 닫기 전 0.08초 동안 눌림을 유지해 클릭 결과를 인지할 수 있게 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DroneTerminalCloseButtonFeedback : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler
    {
        public const float HoverSeconds = 0.12f;
        public const float ClosePressSeconds = 0.08f;

        private const int ParticleCount = 8;
        private const float HoverScale = 1.06f;
        private const float PressScale = 0.94f;

        private static readonly Color RestBackground = new Color(0.07f, 0.18f, 0.22f, 0.98f);
        private static readonly Color HoverBackground = new Color(0.08f, 0.62f, 0.7f, 1f);
        private static readonly Color AccentCyan = new Color(0.18f, 0.84f, 0.92f, 1f);

        private Button button;
        private Image background;
        private TMP_Text label;
        private RectTransform buttonRect;
        private Vector3 baseScale = Vector3.one;
        private bool pointed;
        private bool pressed;
        private bool closePending;
        private float closeElapsed;
        private float hoverProgress;
        private Action closeAction;

        private Image glow;
        private RectTransform particleRoot;
        private Sprite fallbackParticleSprite;
        private Particle[] particles;

        private sealed class Particle
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public float MaxLife;
            public float Alpha;
            public bool Active;
        }

        public void Configure(Image buttonBackground, TMP_Text buttonLabel)
        {
            button ??= GetComponent<Button>();
            background = buttonBackground != null ? buttonBackground : GetComponent<Image>();
            label = buttonLabel != null ? buttonLabel : GetComponentInChildren<TMP_Text>(true);
            buttonRect ??= transform as RectTransform;
            if (buttonRect != null)
            {
                baseScale = buttonRect.localScale;
            }

            ApplyVisualState();
        }

        public void SetCloseAction(Action action)
        {
            closeAction = action;
        }

        private void Awake()
        {
            Configure(null, null);
        }

        private void OnDisable()
        {
            pointed = false;
            pressed = false;
            closePending = false;
            closeElapsed = 0f;
            hoverProgress = 0f;
            if (buttonRect != null)
            {
                buttonRect.localScale = baseScale;
            }

            ClearParticles();
            ApplyVisualState();
        }

        private void Update()
        {
            var deltaTime = Time.unscaledDeltaTime;
            var interactable = button == null || button.IsInteractable();
            var targetHover = pointed && interactable && !closePending ? 1f : 0f;
            hoverProgress = Mathf.MoveTowards(hoverProgress, targetHover, deltaTime / HoverSeconds);

            if (closePending)
            {
                closeElapsed += deltaTime;
                if (closeElapsed >= ClosePressSeconds)
                {
                    closePending = false;
                    closeAction?.Invoke();
                }
            }

            ApplyVisualState();
            UpdateGlow();
            UpdateParticles(deltaTime);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (closePending || (button != null && !button.IsInteractable()))
            {
                return;
            }

            pointed = true;
            EmitHoverParticles();
            StartGlowPulse();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button == null || button.IsInteractable())
            {
                pressed = true;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            RequestClose();
        }

        /// <summary>마우스 클릭과 키보드 Submit이 같은 눌림 후 닫기 경로를 사용한다.</summary>
        public void RequestClose()
        {
            if (closeAction == null || closePending || (button != null && !button.IsInteractable()))
            {
                return;
            }

            closePending = true;
            closeElapsed = 0f;
            pressed = false;
        }

        private void ApplyVisualState()
        {
            if (background != null)
            {
                background.color = Color.Lerp(RestBackground, HoverBackground, hoverProgress);
            }

            if (label != null)
            {
                label.color = Color.Lerp(AccentCyan, Color.white, hoverProgress);
            }

            if (buttonRect != null)
            {
                var scale = closePending || pressed
                    ? PressScale
                    : Mathf.Lerp(1f, HoverScale, hoverProgress);
                buttonRect.localScale = baseScale * scale;
            }
        }

        private void StartGlowPulse()
        {
            EnsureGlow();
            if (glow == null)
            {
                return;
            }

            glow.gameObject.SetActive(true);
            glow.fillAmount = 0f;
        }

        private void EnsureGlow()
        {
            if (glow != null || buttonRect == null)
            {
                return;
            }

            var glowObject = new GameObject("HoverGlow", typeof(RectTransform), typeof(Image), typeof(Outline));
            glowObject.transform.SetParent(buttonRect, false);
            glowObject.transform.SetAsFirstSibling();
            var rect = glowObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-2f, -2f);
            rect.offsetMax = new Vector2(2f, 2f);
            glow = glowObject.GetComponent<Image>();
            glow.color = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0f);
            glow.raycastTarget = false;
            glow.type = Image.Type.Filled;
            glow.fillMethod = Image.FillMethod.Horizontal;
            var outline = glowObject.GetComponent<Outline>();
            outline.effectColor = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.7f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;
            glow.gameObject.SetActive(false);
        }

        private void UpdateGlow()
        {
            if (glow == null || !glow.gameObject.activeSelf)
            {
                return;
            }

            glow.fillAmount += Time.unscaledDeltaTime / 0.22f;
            var progress = Mathf.Clamp01(glow.fillAmount);
            glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.65f, progress);
            var outline = glow.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, (1f - progress) * 0.7f);
            }

            if (progress >= 1f)
            {
                glow.gameObject.SetActive(false);
                glow.rectTransform.localScale = Vector3.one;
            }
        }

        private void EmitHoverParticles()
        {
            if (!Application.isPlaying || buttonRect == null)
            {
                return;
            }

            EnsureParticlePool();
            var origin = buttonRect.TransformPoint(Vector3.zero);
            for (var i = 0; i < ParticleCount; i++)
            {
                var direction = UnityEngine.Random.insideUnitCircle.normalized;
                if (direction.sqrMagnitude < 0.001f)
                {
                    direction = Vector2.up;
                }

                SpawnParticle(
                    origin + buttonRect.TransformVector(new Vector3(
                        UnityEngine.Random.Range(-7f, 7f),
                        UnityEngine.Random.Range(-7f, 7f),
                        0f)),
                    direction * UnityEngine.Random.Range(24f, 46f));
            }
        }

        private void EnsureParticlePool()
        {
            if (particles != null || buttonRect == null)
            {
                return;
            }

            var parent = buttonRect.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var container = new GameObject("CloseButtonParticles", typeof(RectTransform), typeof(CanvasGroup));
            container.transform.SetParent(parent, false);
            container.transform.SetAsLastSibling();
            particleRoot = container.GetComponent<RectTransform>();
            particleRoot.anchorMin = Vector2.zero;
            particleRoot.anchorMax = Vector2.one;
            particleRoot.offsetMin = Vector2.zero;
            particleRoot.offsetMax = Vector2.zero;
            var group = container.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var sprite = ParticleSpriteOrFallback();
            particles = new Particle[ParticleCount];
            for (var i = 0; i < particles.Length; i++)
            {
                var particleObject = new GameObject("P_" + i, typeof(RectTransform), typeof(Image));
                particleObject.transform.SetParent(particleRoot, false);
                var image = particleObject.GetComponent<Image>();
                image.sprite = sprite;
                image.raycastTarget = false;
                image.preserveAspect = true;
                var rect = particleObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                particleObject.SetActive(false);
                particles[i] = new Particle { Rect = rect, Image = image };
            }
        }

        private Sprite ParticleSpriteOrFallback()
        {
            if (fallbackParticleSprite != null)
            {
                return fallbackParticleSprite;
            }

            const int size = 12;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    var alpha = Mathf.Clamp01(1f - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            fallbackParticleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            fallbackParticleSprite.hideFlags = HideFlags.HideAndDontSave;
            return fallbackParticleSprite;
        }

        private void SpawnParticle(Vector3 worldPosition, Vector2 velocity)
        {
            if (particles == null || particleRoot == null)
            {
                return;
            }

            for (var i = 0; i < particles.Length; i++)
            {
                var particle = particles[i];
                if (particle.Active)
                {
                    continue;
                }

                particle.Active = true;
                particle.Position = particleRoot.InverseTransformPoint(worldPosition);
                particle.Velocity = velocity;
                particle.Life = 0f;
                particle.MaxLife = UnityEngine.Random.Range(0.28f, 0.46f);
                particle.Alpha = UnityEngine.Random.Range(0.55f, 0.9f);
                particle.Rect.anchoredPosition = particle.Position;
                var size = UnityEngine.Random.Range(2f, 3.6f);
                particle.Rect.sizeDelta = new Vector2(size, size);
                particle.Image.color = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, particle.Alpha);
                particle.Image.gameObject.SetActive(true);
                return;
            }
        }

        private void UpdateParticles(float deltaTime)
        {
            if (particles == null)
            {
                return;
            }

            for (var i = 0; i < particles.Length; i++)
            {
                var particle = particles[i];
                if (!particle.Active)
                {
                    continue;
                }

                particle.Life += deltaTime;
                if (particle.Life >= particle.MaxLife)
                {
                    particle.Active = false;
                    particle.Image.gameObject.SetActive(false);
                    continue;
                }

                particle.Position += particle.Velocity * deltaTime;
                particle.Velocity *= 0.92f;
                particle.Rect.anchoredPosition = particle.Position;
                var fade = 1f - particle.Life / particle.MaxLife;
                particle.Image.color = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, particle.Alpha * fade);
            }
        }

        private void ClearParticles()
        {
            if (particles == null)
            {
                return;
            }

            for (var i = 0; i < particles.Length; i++)
            {
                particles[i].Active = false;
                if (particles[i].Image != null)
                {
                    particles[i].Image.gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (fallbackParticleSprite == null)
            {
                return;
            }

            var texture = fallbackParticleSprite.texture;
            if (Application.isPlaying)
            {
                Destroy(fallbackParticleSprite);
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(fallbackParticleSprite);
                DestroyImmediate(texture);
            }
        }
    }
}
