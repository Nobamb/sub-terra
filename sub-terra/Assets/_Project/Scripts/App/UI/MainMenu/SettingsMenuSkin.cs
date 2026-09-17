using System;
using System.Collections;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>104-1번: 설정창 등장/퇴장 바운스, 토글 0.5초 애니메이션 및 파티클, 슬라이더 글로우/파티클 및 스킨 관리</summary>
    public sealed class SettingsMenuSkin : MonoBehaviour
    {
        [SerializeField] private RectTransform card;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Toggle reduceMotion;
        [SerializeField] private RectTransform switchHandle;
        [SerializeField] private Image switchTrack;
        [SerializeField] private Image switchGlow;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Image volumeFillGlow;
        [SerializeField] private TMP_Text volumeCaption;
        [SerializeField] private TMP_Text[] sectionTitles;

        private bool lastSwitchState;
        private float currentScale = 1f;
        private Coroutine openRoutine;
        private Coroutine closeRoutine;
        private Coroutine toggleRoutine;
        private bool isClosing;

        private struct UIParticle
        {
            public RectTransform Rect;
            public Image Img;
            public Vector2 Pos;
            public Vector2 Vel;
            public float Life;
            public float MaxLife;
            public float BaseAlpha;
            public bool Active;
        }

        private UIParticle[] particlePool;
        private RectTransform particleRoot;

        private void OnEnable()
        {
            isClosing = false;
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Cancel);
                closeButton.onClick.AddListener(Cancel);
            }
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            RefreshScale();
            RefreshSwitch(instant: true);
            RefreshVolumeGlow();

            if (Application.isPlaying)
            {
                if (openRoutine != null) StopCoroutine(openRoutine);
                openRoutine = StartCoroutine(AnimateOpen());
            }
            else if (card != null)
            {
                card.anchoredPosition = new Vector2(0, -50f * currentScale);
            }
        }

        private void OnDisable()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Cancel);
            if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            if (openRoutine != null) { StopCoroutine(openRoutine); openRoutine = null; }
            if (closeRoutine != null) { StopCoroutine(closeRoutine); closeRoutine = null; }
            if (toggleRoutine != null) { StopCoroutine(toggleRoutine); toggleRoutine = null; }
            isClosing = false;
        }

        private void Cancel()
        {
            if (!Application.isPlaying)
            {
                if (cancelButton != null) cancelButton.onClick.Invoke();
                return;
            }

            PlayCloseAnimation(() =>
            {
                if (cancelButton != null) cancelButton.onClick.Invoke();
            });
        }

        public void PlayCloseAnimation(Action onComplete)
        {
            if (!gameObject.activeInHierarchy || !Application.isPlaying || isClosing)
            {
                onComplete?.Invoke();
                return;
            }

            isClosing = true;
            if (openRoutine != null) { StopCoroutine(openRoutine); openRoutine = null; }
            if (closeRoutine != null) StopCoroutine(closeRoutine);
            closeRoutine = StartCoroutine(AnimateClose(() =>
            {
                isClosing = false;
                onComplete?.Invoke();
            }));
        }

        private IEnumerator AnimateOpen()
        {
            if (card == null) yield break;
            float startY = 850f;
            float targetY = -50f * currentScale;
            float duration = 0.35f;
            float elapsed = 0f;
            card.anchoredPosition = new Vector2(0, startY);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float p = 1f - Mathf.Pow(1f - t, 2.5f);
                float bounce = Mathf.Sin(t * Mathf.PI * 2.5f) * (1f - t) * (18f * currentScale);
                float y = Mathf.Lerp(startY, targetY, p) - bounce;
                card.anchoredPosition = new Vector2(0, y);
                yield return null;
            }

            card.anchoredPosition = new Vector2(0, targetY);
            openRoutine = null;
        }

        private IEnumerator AnimateClose(Action onComplete)
        {
            if (card == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float startY = card.anchoredPosition.y;
            float endY = 850f;
            float duration = 0.28f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // 104-2번: 중간에 멈추거나 끊김 없이 부드럽게 상승하는 단일 연속 이징
                float dip = Mathf.Sin(t * Mathf.PI) * (1f - t) * (14f * currentScale);
                float rise = t * t * t;
                float y = Mathf.Lerp(startY, endY, rise) - dip;
                card.anchoredPosition = new Vector2(0, y);
                yield return null;
            }

            card.anchoredPosition = new Vector2(0, endY);
            closeRoutine = null;
            onComplete?.Invoke();
        }

        private void LateUpdate()
        {
            if (reduceMotion != null && reduceMotion.isOn != lastSwitchState)
            {
                RefreshSwitch(instant: !Application.isPlaying);
            }
            UpdateParticles();
        }

        private void OnRectTransformDimensionsChange() => RefreshScale();

        private void RefreshScale()
        {
            if (card == null) return;
            var bounds = ((RectTransform)transform).rect;
            currentScale = Mathf.Min(1f, bounds.width / 1120f, bounds.height / 1000f);
            currentScale = Mathf.Max(0.01f, currentScale);
            card.localScale = Vector3.one * currentScale;

            if (!Application.isPlaying || (openRoutine == null && closeRoutine == null))
            {
                card.anchoredPosition = new Vector2(0, -50f * currentScale);
            }
        }

        private void RefreshSwitch(bool instant = false)
        {
            if (reduceMotion == null || switchHandle == null || switchTrack == null) return;
            lastSwitchState = reduceMotion.isOn;
            float targetX = lastSwitchState ? 15f : -15f;

            if (switchGlow != null)
            {
                switchGlow.color = lastSwitchState
                    ? new Color(0.29f, 0.88f, 0.95f, 0.45f)
                    : new Color(0.29f, 0.88f, 0.95f, 0.12f);
            }

            if (instant || !Application.isPlaying)
            {
                if (toggleRoutine != null) { StopCoroutine(toggleRoutine); toggleRoutine = null; }
                switchHandle.anchoredPosition = new Vector2(targetX, 0);
                ApplySwitchVisuals(lastSwitchState ? 1f : 0f);
                return;
            }

            if (toggleRoutine != null) StopCoroutine(toggleRoutine);
            toggleRoutine = StartCoroutine(AnimateToggle(targetX));
        }

        private IEnumerator AnimateToggle(float targetX)
        {
            float startX = switchHandle.anchoredPosition.x;
            float duration = 0.5f;
            float elapsed = 0f;
            float particleTimer = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float s = t * t * (3f - 2f * t);
                float currentX = Mathf.Lerp(startX, targetX, s);
                switchHandle.anchoredPosition = new Vector2(currentX, 0);

                float stateRatio = Mathf.InverseLerp(-15f, 15f, currentX);
                ApplySwitchVisuals(stateRatio);

                // 104-2번: 토글 움직일 때 외부에서 은은하게 빛 방출
                if (switchGlow != null)
                {
                    float motionGlow = Mathf.Sin(t * Mathf.PI) * 0.55f;
                    float baseGlow = Mathf.Lerp(0.12f, 0.45f, stateRatio);
                    switchGlow.color = new Color(0.29f, 0.88f, 0.95f, Mathf.Max(baseGlow, motionGlow));
                }

                // 104-2번: 토글 파티클 1.5배 증가 (스폰 주기 단축 0.08f -> 0.05f)
                particleTimer += Time.unscaledDeltaTime;
                if (particleTimer >= 0.05f)
                {
                    particleTimer = 0f;
                    Vector2 spawnPos = switchHandle.TransformPoint(Vector3.zero);
                    SpawnParticle(spawnPos, new Vector2(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(18f, 32f)));
                }

                yield return null;
            }

            switchHandle.anchoredPosition = new Vector2(targetX, 0);
            ApplySwitchVisuals(lastSwitchState ? 1f : 0f);
            if (switchGlow != null)
            {
                switchGlow.color = lastSwitchState
                    ? new Color(0.29f, 0.88f, 0.95f, 0.45f)
                    : new Color(0.29f, 0.88f, 0.95f, 0.12f);
            }
            toggleRoutine = null;
        }

        private void ApplySwitchVisuals(float onRatio)
        {
            // 104-2번: 기본 상태에서도 은은한 내부 청록 빛 유지
            if (switchTrack != null)
            {
                switchTrack.color = Color.Lerp(
                    new Color(0.08f, 0.28f, 0.35f, 0.85f),
                    new Color(0.18f, 0.78f, 0.86f, 1f),
                    onRatio);
            }
            var handleImage = switchHandle != null ? switchHandle.GetComponent<Image>() : null;
            if (handleImage != null)
            {
                handleImage.color = Color.Lerp(
                    new Color(0.65f, 0.88f, 0.92f),
                    new Color(0.95f, 1f, 1f),
                    onRatio);
            }
        }

        private void OnVolumeChanged(float val)
        {
            RefreshVolumeGlow();
            if (Application.isPlaying && volumeSlider != null && volumeSlider.handleRect != null)
            {
                Vector2 spawnPos = volumeSlider.handleRect.TransformPoint(Vector3.zero);
                SpawnParticle(spawnPos, new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(20f, 40f)));
            }
        }

        private void RefreshVolumeGlow()
        {
            if (volumeFillGlow == null) return;
            float val = volumeSlider != null ? volumeSlider.value : 0.5f;
            // 104-2번: 활성화 부분 은은하고 선명한 청록빛
            volumeFillGlow.color = val > 0.005f
                ? new Color(0.35f, 0.95f, 1f, 0.75f)
                : Color.clear;
        }

        private void EnsureParticlePool()
        {
            if (particlePool != null && particlePool.Length > 0) return;
            var container = new GameObject("SettingsParticles", typeof(RectTransform));
            // 104-2번: SettingsCard 하위 최상단으로 배치하여 z-index 우선순위 확보
            container.transform.SetParent(card != null ? card : (RectTransform)transform, false);
            particleRoot = container.GetComponent<RectTransform>();
            particleRoot.anchorMin = Vector2.zero;
            particleRoot.anchorMax = Vector2.one;
            particleRoot.offsetMin = Vector2.zero;
            particleRoot.offsetMax = Vector2.zero;
            particleRoot.SetAsLastSibling();

            int count = 40;
            particlePool = new UIParticle[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("P_" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(particleRoot, false);
                var rt = go.GetComponent<RectTransform>();
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                go.SetActive(false);
                particlePool[i] = new UIParticle
                {
                    Rect = rt,
                    Img = img,
                    Active = false
                };
            }
        }

        private void SpawnParticle(Vector3 worldPos, Vector2 vel)
        {
            EnsureParticlePool();
            if (particleRoot == null || particlePool == null) return;
            // 슬라이더 및 기타 컨트롤보다 항상 앞(위)에 그려지도록 z-index 보장
            particleRoot.SetAsLastSibling();
            for (int i = 0; i < particlePool.Length; i++)
            {
                if (!particlePool[i].Active)
                {
                    particlePool[i].Active = true;
                    particlePool[i].Img.gameObject.SetActive(true);

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        particleRoot,
                        RectTransformUtility.WorldToScreenPoint(null, worldPos),
                        null,
                        out Vector2 localPos);

                    // 104-2번: 좀 더 작은 알갱이 크기 (1.5f ~ 2.8f)
                    float size = UnityEngine.Random.Range(1.5f, 2.8f);
                    particlePool[i].Rect.sizeDelta = new Vector2(size, size);
                    particlePool[i].Pos = localPos;
                    particlePool[i].Rect.anchoredPosition = localPos;
                    particlePool[i].Vel = vel;
                    particlePool[i].Life = 0f;
                    particlePool[i].MaxLife = UnityEngine.Random.Range(0.45f, 0.75f);
                    // 104-2번: 불투명도 10% 상향 (0.7f ~ 1.0f)
                    particlePool[i].BaseAlpha = UnityEngine.Random.Range(0.70f, 1.0f);

                    Color c = Color.Lerp(new Color(0.35f, 0.92f, 1f), Color.white, UnityEngine.Random.value * 0.35f);
                    particlePool[i].Img.color = new Color(c.r, c.g, c.b, particlePool[i].BaseAlpha);
                    return;
                }
            }
        }

        private void UpdateParticles()
        {
            if (particlePool == null) return;
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;

            for (int i = 0; i < particlePool.Length; i++)
            {
                if (!particlePool[i].Active) continue;
                particlePool[i].Life += dt;
                if (particlePool[i].Life >= particlePool[i].MaxLife)
                {
                    particlePool[i].Active = false;
                    particlePool[i].Img.gameObject.SetActive(false);
                    continue;
                }
                particlePool[i].Pos += particlePool[i].Vel * dt;
                particlePool[i].Vel.y += 12f * dt;
                particlePool[i].Rect.anchoredPosition = particlePool[i].Pos;

                float alpha = Mathf.Lerp(particlePool[i].BaseAlpha, 0f, particlePool[i].Life / particlePool[i].MaxLife);
                var c = particlePool[i].Img.color;
                particlePool[i].Img.color = new Color(c.r, c.g, c.b, alpha);
            }
        }

        public static string FormatVolume(GameObject root, float volume)
        {
            var skin = root != null ? root.GetComponent<SettingsMenuSkin>() : null;
            if (skin == null) return LocalizationService.FormatMasterVolume(volume);
            if (skin.volumeCaption != null)
                skin.volumeCaption.text = LocalizationService.Get("settings.master_volume", "마스터 음량");
            bool english = LocalizationService.Current == GameLanguage.English;
            string[] titles = english
                ? new[] { "Audio", "Display", "System", "Controls" }
                : new[] { "오디오", "디스플레이", "시스템", "조작" };
            for (int i = 0; i < skin.sectionTitles.Length && i < titles.Length; i++)
                if (skin.sectionTitles[i] != null) skin.sectionTitles[i].text = titles[i];
            return Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f) + "%";
        }
    }
}
