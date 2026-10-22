using SubTerra.App.UI.Drone;
using SubTerra.App.UI.RunFailure;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI
{
    /// <summary>드론 터미널의 가로선·세로 펼침을 재사용하는 짧은 창 열기 연출.</summary>
    [DisallowMultipleComponent]
    public sealed class TerminalWindowOpenMotion : MonoBehaviour
    {
        public const float Duration = 0.18f;
        private readonly DroneTerminalFxSettings settings = new DroneTerminalFxSettings { openDuration = Duration };
        private RectTransform window;
        private Image line;
        private Vector3 originalScale;
        private Vector2 centerOffset;
        private float elapsed;
        public bool IsPlaying { get; private set; }

        public static void Open(GameObject root)
        {
            if (root == null || !(root.transform is RectTransform)) return;
            var motion = root.GetComponent<TerminalWindowOpenMotion>();
            if (motion == null) motion = root.AddComponent<TerminalWindowOpenMotion>();
            motion.enabled = true;
            motion.Play();
        }

        public static void Stop(GameObject root)
        {
            if (root != null) root.GetComponent<TerminalWindowOpenMotion>()?.Restore();
        }

        private void Play()
        {
            if (IsPlaying) Restore();
            window = (RectTransform)transform;
            originalScale = window.localScale;
            elapsed = 0f;
            if (AccessibilityPreferences.ReduceMotion || !gameObject.activeInHierarchy) return;
            EnsureLine();
            IsPlaying = true;
            ApplyFrame();
        }

        private void Update() => Advance(Time.unscaledDeltaTime);

        public void Advance(float deltaTime)
        {
            if (!IsPlaying) return;
            elapsed += Mathf.Max(0f, deltaTime);
            if (elapsed >= Duration || AccessibilityPreferences.ReduceMotion)
            {
                Restore();
                return;
            }
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            var frame = DroneTerminalTimeline.Open(elapsed, settings);
            var height = Mathf.Max(0.003f, frame.Open);
            // 좌상단 피벗인 건설창도 중앙 가로선을 기준으로 위아래로 펼친다.
            // 드래그 등 외부 위치 변경은 유지하고 연출이 더한 보정값만 회수한다.
            window.anchoredPosition -= centerOffset;
            centerOffset = new Vector2(0f, (0.5f - window.pivot.y) * window.rect.height * originalScale.y * (1f - height));
            window.anchoredPosition += centerOffset;
            window.localScale = new Vector3(originalScale.x, originalScale.y * height, originalScale.z);
            line.gameObject.SetActive(true);
            line.rectTransform.sizeDelta = new Vector2(window.rect.width * frame.LineWidth, 2.5f);
            // 본문이 접혀 있어도 가로선의 두께는 일정하게 유지한다.
            line.rectTransform.localScale = new Vector3(1f, 1f / height, 1f);
            var color = settings.lineColor;
            color.a *= frame.LineAlpha;
            line.color = color;
        }

        private void EnsureLine()
        {
            if (line != null) return;
            var beam = new GameObject("WindowOpenLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            beam.transform.SetParent(transform, false);
            beam.GetComponent<LayoutElement>().ignoreLayout = true;
            line = beam.GetComponent<Image>();
            line.sprite = RunFailureGlitchArt.GetCrtBeamSprite();
            line.raycastTarget = false;
            line.rectTransform.anchorMin = line.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private void OnDisable() => Restore();

        private void Restore()
        {
            if (IsPlaying && window != null)
            {
                window.localScale = originalScale;
                window.anchoredPosition -= centerOffset;
            }
            centerOffset = Vector2.zero;
            IsPlaying = false;
            if (line != null) line.gameObject.SetActive(false);
        }
    }
}
