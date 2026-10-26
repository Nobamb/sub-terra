using System;
using SubTerra.App.UI.Outpost;
using SubTerra.App.UI.RunFailure;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.UI
{
    /// <summary>중앙 빛점·가로선·세로 펼침을 역순으로도 재생하는 공통 창 연출.</summary>
    [DisallowMultipleComponent]
    public sealed class TerminalWindowOpenMotion : MonoBehaviour
    {
        public const float Duration = 0.18f;
        public const float CloseDuration = 0.16f;
        private static readonly Color LightColor = new Color(0.5f, 0.98f, 1f, 1f);
        private RectTransform window;
        private RectTransform effects;
        private UnityEngine.UI.Image line, point, glow;
        private Canvas effectCanvas, windowCanvas;
        private CanvasGroup group;
        private Vector3 originalScale;
        private Vector2 centerOffset;
        private float originalAlpha, progress;
        private bool originalInteractable;
        private Action closeCompleted;

        public bool IsPlaying { get; private set; }
        public bool IsClosing { get; private set; }

        public static bool IsClosingWindow(GameObject root)
        {
            var motion = root != null ? root.GetComponent<TerminalWindowOpenMotion>() : null;
            return motion != null && motion.IsClosing;
        }

        public static void Open(GameObject root)
        {
            if (root == null || !(root.transform is RectTransform)) return;
            var motion = root.GetComponent<TerminalWindowOpenMotion>();
            if (motion == null) motion = root.AddComponent<TerminalWindowOpenMotion>();
            motion.enabled = true;
            motion.PlayOpen();
        }

        public static void Close(GameObject root, Action completed)
        {
            var motion = root != null ? root.GetComponent<TerminalWindowOpenMotion>() : null;
            if (motion == null || !motion.enabled || !root.activeInHierarchy || AccessibilityPreferences.ReduceMotion)
            {
                if (motion != null) motion.Restore();
                completed?.Invoke();
                return;
            }
            motion.PlayClose(completed);
        }

        public static void Stop(GameObject root)
        {
            var motion = root != null ? root.GetComponent<TerminalWindowOpenMotion>() : null;
            if (motion != null) motion.Restore();
        }

        private void CaptureWindow()
        {
            window = (RectTransform)transform;
            originalScale = window.localScale;
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            originalAlpha = group.alpha;
            originalInteractable = group.interactable;
            windowCanvas = GetComponentInParent<Canvas>();
        }

        private void PlayOpen()
        {
            if (IsPlaying)
            {
                if (!IsClosing) return;
                IsClosing = false;
                closeCompleted = null;
                group.interactable = originalInteractable;
            }
            else
            {
                CaptureWindow();
                if (AccessibilityPreferences.ReduceMotion || !gameObject.activeInHierarchy) return;
                progress = 0f;
                EnsureEffects();
                IsPlaying = true;
            }
            ApplyFrame();
        }

        private void PlayClose(Action completed)
        {
            if (IsClosing) return;
            if (!IsPlaying)
            {
                CaptureWindow();
                EnsureEffects();
                progress = 1f;
                IsPlaying = true;
            }
            IsClosing = true;
            closeCompleted = completed;
            // 배경으로 클릭이 빠지지 않도록 raycast는 유지하고 창 내부 조작만 막는다.
            group.interactable = false;
            if (progress <= 0f) FinishClose();
            else ApplyFrame();
        }

        private void Update() => Advance(Time.unscaledDeltaTime);

        public void Advance(float deltaTime)
        {
            if (!IsPlaying) return;
            if (AccessibilityPreferences.ReduceMotion)
            {
                if (IsClosing) FinishClose();
                else Restore();
                return;
            }
            progress = Mathf.Clamp01(progress + Mathf.Max(0f, deltaTime) * (IsClosing ? -1f / CloseDuration : 1f / Duration));
            if (IsClosing && progress <= 0f) FinishClose();
            else if (!IsClosing && progress >= 1f) Restore();
            else ApplyFrame();
        }

        private static float Smooth(float value, float from, float to)
            => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, value));

        private void ApplyFrame()
        {
            float height = Mathf.Max(0.003f, Smooth(progress, 0.42f, 1f));
            window.anchoredPosition -= centerOffset;
            centerOffset = new Vector2(0f, (0.5f - window.pivot.y) * window.rect.height * originalScale.y * (1f - height));
            window.anchoredPosition += centerOffset;
            window.localScale = new Vector3(originalScale.x, originalScale.y * height, originalScale.z);
            group.alpha = originalAlpha * Smooth(progress, 0.42f, 0.75f);

            // 본문과 형제로 두어 본문의 축소·페이드에도 점과 선의 크기·밝기를 유지한다.
            effects.gameObject.SetActive(true);
            effects.SetSiblingIndex(window.GetSiblingIndex() + 1);
            effects.position = window.TransformPoint(window.rect.center);
            effects.localScale = originalScale;
            if (effectCanvas != null && windowCanvas != null)
            {
                effectCanvas.sortingLayerID = windowCanvas.sortingLayerID;
                effectCanvas.sortingOrder = windowCanvas.sortingOrder;
            }
            float dotAlpha = Smooth(progress, 0f, 0.06f) * (1f - Smooth(progress, 0.22f, 0.45f));
            point.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(8f, 15f, Smooth(progress, 0f, 0.22f));
            glow.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(28f, 58f, Smooth(progress, 0f, 0.22f));
            SetLight(point, dotAlpha);
            SetLight(glow, dotAlpha * 0.45f);
            line.rectTransform.sizeDelta = new Vector2(window.rect.width * Smooth(progress, 0.16f, 0.42f), 2.5f);
            SetLight(line, Smooth(progress, 0.12f, 0.3f) * (1f - Smooth(progress, 0.7f, 1f)));
        }

        private void SetLight(UnityEngine.UI.Image graphic, float alpha)
        {
            var color = LightColor;
            color.a = alpha * originalAlpha;
            graphic.color = color;
        }

        private void EnsureEffects()
        {
            if (effects != null) return;
            var root = new GameObject("WindowMotionFx", typeof(RectTransform), typeof(UnityEngine.UI.LayoutElement));
            effects = (RectTransform)root.transform;
            effects.SetParent(transform.parent, false);
            root.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
            if (windowCanvas != null)
            {
                effectCanvas = root.AddComponent<Canvas>();
                effectCanvas.overrideSorting = true;
            }
            glow = CreateLight("WindowOpenGlow", CoreCctvArt.Soft());
            line = CreateLight("WindowOpenLine", RunFailureGlitchArt.GetCrtBeamSprite());
            point = CreateLight("WindowOpenPoint", CoreCctvArt.Dot());
        }

        private UnityEngine.UI.Image CreateLight(string name, Sprite sprite)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            obj.transform.SetParent(effects, false);
            var graphic = obj.GetComponent<UnityEngine.UI.Image>();
            graphic.sprite = sprite;
            graphic.raycastTarget = false;
            graphic.rectTransform.anchorMin = graphic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            graphic.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return graphic;
        }

        private void FinishClose()
        {
            var completed = closeCompleted;
            Restore();
            completed?.Invoke();
        }

        private void OnDisable()
        {
            if (IsClosing) FinishClose();
            else Restore();
        }

        private void OnDestroy()
        {
            if (effects != null) Destroy(effects.gameObject);
        }

        private void Restore()
        {
            if (IsPlaying && window != null)
            {
                window.localScale = originalScale;
                window.anchoredPosition -= centerOffset;
                if (group != null)
                {
                    group.alpha = originalAlpha;
                    group.interactable = originalInteractable;
                }
            }
            centerOffset = Vector2.zero;
            IsPlaying = false;
            IsClosing = false;
            closeCompleted = null;
            if (effects != null) effects.gameObject.SetActive(false);
        }
    }
}
