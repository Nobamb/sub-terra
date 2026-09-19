using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SubTerra.App.UI.HUD
{
    /// <summary>기능·단축키는 기존 HUD에 맡기고 메뉴의 두 시각 상태만 전환한다.</summary>
    public sealed class GameplaySideMenuController : MonoBehaviour
    {
        public const float ExitSeconds = 0.5f;
        public const float EnterSeconds = 0.3f;
        [SerializeField] private RectTransform openRoot;
        [SerializeField] private RectTransform closedRoot;
        [SerializeField] private CanvasGroup inputGroup;
        public bool IsOpen { get; private set; } = true;
        public bool IsTransitioning { get; private set; }

        private void OnEnable() => ResetOpen();

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.slashKey.wasPressedThisFrame) return;
            if (IsTypingInField()) return;
            CloseIfOpen();
        }

        /// <summary>열린 우측 메뉴를 닫는다. 이미 닫혀 있거나 전환 중이면 무시한다.</summary>
        public void CloseIfOpen()
        {
            if (!IsOpen || IsTransitioning) return;
            Toggle();
        }

        private static bool IsTypingInField()
        {
            var selected = EventSystem.current;
            return selected != null
                && selected.currentSelectedGameObject != null
                && selected.currentSelectedGameObject.GetComponent<TMP_InputField>() != null;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ResetOpen();
        }

        private void ResetOpen()
        {
            IsOpen = true;
            IsTransitioning = false;
            if (openRoot != null)
            {
                SetX(openRoot, 0f);
                openRoot.gameObject.SetActive(true);
            }
            if (closedRoot != null)
            {
                SetX(closedRoot, 0f);
                closedRoot.gameObject.SetActive(false);
            }
            if (inputGroup != null) inputGroup.interactable = true;
        }

        public void Toggle()
        {
            if (IsTransitioning || !isActiveAndEnabled || openRoot == null || closedRoot == null) return;
            StartCoroutine(Transition());
        }

        private IEnumerator Transition()
        {
            IsTransitioning = true;
            inputGroup.interactable = false;
            var outgoing = IsOpen ? openRoot : closedRoot;
            var incoming = IsOpen ? closedRoot : openRoot;
            // 우측 앵커·피벗 기준: 왼쪽 끝까지 화면 밖으로 나간 후에만 교체한다.
            yield return Slide(outgoing, false, ExitSeconds);
            outgoing.gameObject.SetActive(false);
            SetX(incoming, incoming.rect.width + 8f);
            incoming.gameObject.SetActive(true);
            yield return Slide(incoming, true, EnterSeconds);
            IsOpen = !IsOpen;
            inputGroup.interactable = true;
            IsTransitioning = false;
        }

        private static IEnumerator Slide(RectTransform panel, bool entering, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetX(panel, (panel.rect.width + 8f) * (entering ? 1f - t : t));
                yield return null;
            }
            SetX(panel, entering ? 0f : panel.rect.width + 8f);
        }

        private static void SetX(RectTransform panel, float x)
        {
            var position = panel.anchoredPosition;
            position.x = x;
            panel.anchoredPosition = position;
        }
    }
}
