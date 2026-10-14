using System.Collections;
using SubTerra.App.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Tutorial
{
    public sealed class QuestClearPopupMotion : MonoBehaviour
    {
        [SerializeField] private RectTransform checkSign;
        [SerializeField] private RectTransform borderGlint;
        [SerializeField] private Image borderGlintImage;

        private RectTransform panel;
        private StartBriefingPopupMotion transition;
        private Coroutine motion;
        private bool closing;
        public bool IsClosing => closing;

        private void Awake()
        {
            panel = (RectTransform)transform;
            transition = gameObject.AddComponent<StartBriefingPopupMotion>();
            transition.enabled = false;
            transition.ConfigureQuestClear(borderGlint);
        }

        private void OnEnable()
        {
            Open();
        }

        public void Open()
        {
            if (panel == null)
            {
                panel = (RectTransform)transform;
            }

            if (motion != null) StopCoroutine(motion);
            closing = false;
            panel.anchoredPosition = Vector2.zero;
            if (checkSign != null) checkSign.localScale = Vector3.zero;
            if (borderGlint != null) borderGlint.gameObject.SetActive(false);
            transition.enabled = false;
            transition.enabled = true;
            motion = StartCoroutine(Enter());
        }

        private void OnDisable()
        {
            if (motion != null) StopCoroutine(motion);
            motion = null;
            PopupWindowSorting.Remove(GetComponent<Canvas>());
        }

        public void Close()
        {
            if (closing) return;
            closing = true;
            if (motion != null) StopCoroutine(motion);
            if (checkSign != null) checkSign.localScale = Vector3.one;
            if (borderGlint != null) borderGlint.gameObject.SetActive(false);
            panel.anchoredPosition = Vector2.zero;
            transition.RequestQuestClose();
            motion = null;
        }

        private IEnumerator Enter()
        {
            while (!transition.IsShown) yield return null;
            if (checkSign != null)
            {
                yield return ScaleCheck(0f, 1.1f, 0.2f);
                yield return ScaleCheck(1.1f, 1f, 0.1f);
            }

            if (borderGlint != null)
            {
                borderGlint.gameObject.SetActive(true);
                var elapsed = 0f;
                while (elapsed < 0.3f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    PlaceGlint(Mathf.Clamp01(elapsed / 0.3f));
                    yield return null;
                }

                borderGlint.gameObject.SetActive(false);
            }

            motion = null;
        }

        private IEnumerator ScaleCheck(float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                checkSign.localScale = Vector3.one * Mathf.Lerp(from, to, t);
                yield return null;
            }

            checkSign.localScale = Vector3.one * to;
        }

        private void PlaceGlint(float progress)
        {
            // The frame art occupies the central outline of the 900 x 450 sprite.
            var perimeter = 2f * (790f + 310f);
            var distance = progress * perimeter;
            Vector2 position;
            float rotation;
            if (distance < 790f)
            {
                position = new Vector2(-395f + distance, 155f);
                rotation = 0f;
            }
            else if (distance < 1100f)
            {
                position = new Vector2(395f, 155f - (distance - 790f));
                rotation = 90f;
            }
            else if (distance < 1890f)
            {
                position = new Vector2(395f - (distance - 1100f), -155f);
                rotation = 0f;
            }
            else
            {
                position = new Vector2(-395f, -155f + (distance - 1890f));
                rotation = 90f;
            }

            borderGlint.anchoredPosition = new Vector2(
                position.x * panel.rect.width / 900f,
                position.y * panel.rect.height / 450f);
            borderGlint.localRotation = Quaternion.Euler(0f, 0f, rotation);
            if (borderGlintImage != null) borderGlintImage.color = new Color(0.18f, 1f, 0.96f, 0.95f);
        }
    }

}
