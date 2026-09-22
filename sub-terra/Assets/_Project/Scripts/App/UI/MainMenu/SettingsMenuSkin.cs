using System;
using System.Collections;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// 104-4번: 하단 기본값/취소/적용 좌우 키 포커스, 슬라이더 필 영역만 그라데이션 글로우,
    /// 토글/슬라이더 원형 파티클, 끊김 없는 닫기 상승.
    /// </summary>
    public sealed class SettingsMenuSkin : MonoBehaviour
    {
        [SerializeField] private RectTransform card;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button[] footerButtons;
        [SerializeField] private Toggle reduceMotion;
        [SerializeField] private RectTransform switchHandle;
        [SerializeField] private Image switchTrack;
        [SerializeField] private Image switchOnImage;
        [SerializeField] private Image switchGlow;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Image volumeFillGlow;
        [SerializeField] private TMP_Text volumeCaption;
        [SerializeField] private TMP_Text[] sectionTitles;
        [SerializeField] private Sprite particleSprite;
        [SerializeField] private float switchOnX = 16f;
        [SerializeField] private float switchOffX = -16f;

        private int footerIndex = -1;
        public int CurrentFooterButtonIndex => footerIndex;

        private bool lastSwitchState;
        private float currentScale = 1f;
        private Coroutine openRoutine;
        private Coroutine closeRoutine;
        private Coroutine toggleRoutine;
        private bool isClosing;
        private Action closeComplete;

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
        private Sprite fallbackParticleSprite;
        private float particleCooldown;
        private bool particleTriggersWired;

        private void OnEnable()
        {
            isClosing = false;
            closeComplete = null;
            footerIndex = -1;
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
            RefreshFooterHighlight();
            WireParticleTriggers();

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
            closeComplete = null;
            footerIndex = -1;
            RefreshFooterHighlight();
        }

        private void Update()
        {
            HandleFooterKeyboard();
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
            if (!gameObject.activeInHierarchy || !Application.isPlaying)
            {
                onComplete?.Invoke();
                return;
            }

            // 이미 닫히는 중이면 콜백만 이어 붙여 중간에서 애니메이션이 재시작되지 않게 한다.
            if (isClosing)
            {
                closeComplete += onComplete;
                return;
            }

            isClosing = true;
            closeComplete = onComplete;
            if (openRoutine != null) { StopCoroutine(openRoutine); openRoutine = null; }
            if (closeRoutine != null) StopCoroutine(closeRoutine);
            closeRoutine = StartCoroutine(AnimateClose());
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

        private IEnumerator AnimateClose()
        {
            if (card == null)
            {
                FinishClose();
                yield break;
            }

            float startY = card.anchoredPosition.y;
            float endY = 850f;
            float duration = 0.38f;
            float elapsed = 0f;
            float dip = 8f * currentScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // 초속 0이 되는 t^3 대신 ease-out으로 바로 상승하고, 약한 바운스는 같은 곡선에 섞는다.
                float ease = 1f - (1f - t) * (1f - t);
                float bounce = dip * t * (1f - t) * (1f - t);
                float y = Mathf.Lerp(startY, endY, ease) - bounce;
                card.anchoredPosition = new Vector2(0, y);
                yield return null;
            }

            card.anchoredPosition = new Vector2(0, endY);
            closeRoutine = null;
            FinishClose();
        }

        private void FinishClose()
        {
            var cb = closeComplete;
            closeComplete = null;
            cb?.Invoke();
            var extra = closeComplete;
            closeComplete = null;
            extra?.Invoke();
            isClosing = false;
        }

        private void LateUpdate()
        {
            if (particleCooldown > 0f)
                particleCooldown = Mathf.Max(0f, particleCooldown - Time.unscaledDeltaTime);
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

            if (!isClosing && (!Application.isPlaying || (openRoutine == null && closeRoutine == null)))
            {
                card.anchoredPosition = new Vector2(0, -50f * currentScale);
            }
        }

        private void RefreshSwitch(bool instant = false)
        {
            if (reduceMotion == null || switchHandle == null || switchTrack == null) return;
            lastSwitchState = reduceMotion.isOn;
            float targetX = lastSwitchState ? switchOnX : switchOffX;

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

                float stateRatio = Mathf.InverseLerp(switchOffX, switchOnX, currentX);
                ApplySwitchVisuals(stateRatio);

                if (switchGlow != null)
                {
                    float motionGlow = Mathf.Sin(t * Mathf.PI) * 0.55f;
                    float baseGlow = Mathf.Lerp(0.08f, 0.40f, stateRatio);
                    switchGlow.color = new Color(0.29f, 0.88f, 0.95f, Mathf.Max(baseGlow, motionGlow));
                }

                particleTimer += Time.unscaledDeltaTime;
                if (particleTimer >= 0.04f)
                {
                    particleTimer = 0f;
                    BurstAt(switchHandle, 3);
                }

                yield return null;
            }

            switchHandle.anchoredPosition = new Vector2(targetX, 0);
            toggleRoutine = null;
            ApplySwitchVisuals(lastSwitchState ? 1f : 0f);
        }

        private void ApplySwitchVisuals(float onRatio)
        {
            if (switchTrack != null)
                switchTrack.color = Color.white;
            if (switchOnImage != null)
            {
                var c = Color.white;
                c.a = onRatio;
                switchOnImage.color = c;
            }

            if (switchGlow != null && toggleRoutine == null)
            {
                switchGlow.color = new Color(0.29f, 0.88f, 0.95f, Mathf.Lerp(0.08f, 0.40f, onRatio));
            }

            var handleImage = switchHandle != null ? switchHandle.GetComponent<Image>() : null;
            if (handleImage != null)
                handleImage.color = Color.white;
        }

        private void OnVolumeChanged(float val)
        {
            RefreshVolumeGlow();
            EmitSliderParticles(4, respectCooldown: true);
        }

        private void RefreshVolumeGlow()
        {
            if (volumeFillGlow == null) return;
            volumeFillGlow.color = Color.white;
            volumeFillGlow.raycastTarget = false;
            volumeFillGlow.transform.SetAsLastSibling();
            volumeFillGlow.enabled = volumeSlider == null || volumeSlider.normalizedValue > 0.001f;
        }

        /// <summary>
        /// 하단 기본값/취소/적용만 좌우로 순환한다.
        /// direction &lt; 0: 왼쪽, direction &gt; 0: 오른쪽.
        /// </summary>
        public void NavigateFooterButton(int direction)
        {
            int count = FooterCount();
            if (direction == 0 || count <= 0) return;
            if (footerIndex < 0)
                footerIndex = direction > 0 ? 0 : count - 1;
            else
                footerIndex = (footerIndex + (direction > 0 ? 1 : -1) + count) % count;
            RefreshFooterHighlight();
        }

        public void ActivateFocusedFooterButton()
        {
            var button = GetFooterButton(footerIndex);
            if (button == null || !button.interactable) return;
            button.onClick.Invoke();
        }

        private void HandleFooterKeyboard()
        {
            if (!Application.isPlaying) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            bool left = keyboard.leftArrowKey.wasPressedThisFrame;
            bool right = keyboard.rightArrowKey.wasPressedThisFrame;
            bool activate = footerIndex >= 0
                && (keyboard.spaceKey.wasPressedThisFrame
                    || keyboard.enterKey.wasPressedThisFrame
                    || keyboard.numpadEnterKey.wasPressedThisFrame);
            if (!left && !right && !activate) return;
            if (BlocksFooterNav()) return;

            if (left) NavigateFooterButton(-1);
            else if (right) NavigateFooterButton(1);
            else ActivateFocusedFooterButton();
        }

        private bool BlocksFooterNav()
        {
            var scheme = GetComponent<ControlSchemePanel>();
            if (scheme != null && scheme.IsOpen) return true;
            var dropdowns = GetComponentsInChildren<TMP_Dropdown>(false);
            for (int i = 0; i < dropdowns.Length; i++)
            {
                if (dropdowns[i] != null && dropdowns[i].IsExpanded) return true;
            }
            return false;
        }

        private int FooterCount()
        {
            return footerButtons == null ? 0 : footerButtons.Length;
        }

        private Button GetFooterButton(int index)
        {
            if (footerButtons == null || index < 0 || index >= footerButtons.Length)
                return null;
            return footerButtons[index];
        }

        private void RefreshFooterHighlight()
        {
            if (footerButtons == null) return;
            for (int i = 0; i < footerButtons.Length; i++)
            {
                if (footerButtons[i] == null) continue;
                var skin = footerButtons[i].GetComponent<MenuSpriteButtonSkin>();
                if (skin != null) skin.SetKeyboardActive(i == footerIndex);
            }
        }

        private void WireParticleTriggers()
        {
            if (particleTriggersWired) return;
            particleTriggersWired = true;
            WireBurst(volumeSlider != null ? volumeSlider.gameObject : null, OnSliderParticleEvent);
            WireBurst(reduceMotion != null ? reduceMotion.gameObject : null, OnToggleParticleEvent);
        }

        private void WireBurst(GameObject target, UnityEngine.Events.UnityAction<BaseEventData> handler)
        {
            if (target == null) return;
            var trigger = target.GetComponent<EventTrigger>();
            if (trigger == null) trigger = target.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerDown, handler);
            AddTrigger(trigger, EventTriggerType.Drag, handler);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type,
            UnityEngine.Events.UnityAction<BaseEventData> handler)
        {
            for (int i = 0; i < trigger.triggers.Count; i++)
            {
                if (trigger.triggers[i] != null && trigger.triggers[i].eventID == type)
                    return;
            }

            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(handler);
            trigger.triggers.Add(entry);
        }

        private void OnSliderParticleEvent(BaseEventData data)
        {
            var pointer = data as PointerEventData;
            bool down = pointer != null && !pointer.dragging;
            EmitSliderParticles(down ? 12 : 5, respectCooldown: !down);
        }

        private void OnToggleParticleEvent(BaseEventData _)
        {
            if (!Application.isPlaying) return;
            var origin = switchHandle != null
                ? switchHandle
                : reduceMotion != null ? reduceMotion.transform as RectTransform : null;
            BurstAt(origin, 12);
            particleCooldown = 0.04f;
        }

        private void EmitSliderParticles(int count, bool respectCooldown)
        {
            if (!Application.isPlaying) return;
            if (respectCooldown && particleCooldown > 0f) return;
            particleCooldown = 0.04f;
            BurstAt(volumeSlider != null ? volumeSlider.handleRect : null, count);
        }

        private void BurstAt(RectTransform origin, int count)
        {
            if (origin == null) return;
            Vector3 world = origin.TransformPoint(Vector3.zero);
            for (int i = 0; i < count; i++)
            {
                var spread = origin.TransformVector(new Vector3(
                    UnityEngine.Random.Range(-16f, 16f),
                    UnityEngine.Random.Range(-4f, 4f),
                    0f));
                SpawnParticle(
                    world + spread,
                    new Vector2(UnityEngine.Random.Range(-14f, 14f), UnityEngine.Random.Range(22f, 48f)));
            }
        }

        private Sprite ParticleSpriteOrFallback()
        {
            if (particleSprite != null) return particleSprite;
            if (fallbackParticleSprite != null) return fallbackParticleSprite;
            var tex = Texture2D.whiteTexture;
            fallbackParticleSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return fallbackParticleSprite;
        }

        private void EnsureParticlePool()
        {
            if (particlePool != null && particlePool.Length > 0) return;
            var parent = card != null ? card : (RectTransform)transform;
            var container = new GameObject("SettingsParticles", typeof(RectTransform));
            container.transform.SetParent(parent, false);
            particleRoot = container.GetComponent<RectTransform>();
            particleRoot.anchorMin = Vector2.zero;
            particleRoot.anchorMax = Vector2.one;
            particleRoot.offsetMin = Vector2.zero;
            particleRoot.offsetMax = Vector2.zero;
            particleRoot.SetAsLastSibling();

            int count = 72;
            particlePool = new UIParticle[count];
            var sprite = ParticleSpriteOrFallback();
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("P_" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(particleRoot, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                img.preserveAspect = true;
                img.sprite = sprite;
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
            particleRoot.SetAsLastSibling();
            var sprite = ParticleSpriteOrFallback();
            Vector2 localPos = particleRoot.InverseTransformPoint(worldPos);
            for (int i = 0; i < particlePool.Length; i++)
            {
                if (!particlePool[i].Active)
                {
                    particlePool[i].Active = true;
                    particlePool[i].Img.gameObject.SetActive(true);
                    particlePool[i].Img.sprite = sprite;
                    particlePool[i].Img.preserveAspect = true;

                    float size = UnityEngine.Random.Range(2.2f, 4.4f);
                    particlePool[i].Rect.sizeDelta = new Vector2(size, size);
                    particlePool[i].Pos = localPos;
                    particlePool[i].Rect.anchoredPosition = localPos;
                    particlePool[i].Vel = vel;
                    particlePool[i].Life = 0f;
                    particlePool[i].MaxLife = UnityEngine.Random.Range(0.45f, 0.85f);
                    particlePool[i].BaseAlpha = UnityEngine.Random.Range(0.25f, 1.0f);

                    Color c = Color.Lerp(new Color(0.29f, 0.88f, 0.95f), Color.white, UnityEngine.Random.value * 0.35f);
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
