using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.UI.Tutorial
{
    public sealed class QuestClearCloseHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Coroutine hover;

        public void OnPointerEnter(PointerEventData eventData) => Animate(1.15f);
        public void OnPointerExit(PointerEventData eventData) => Animate(1f);

        private void OnDisable()
        {
            if (hover != null) StopCoroutine(hover);
            hover = null;
            transform.localScale = Vector3.one;
        }

        private void Animate(float target)
        {
            if (hover != null) StopCoroutine(hover);
            hover = StartCoroutine(ScaleTo(target));
        }

        private IEnumerator ScaleTo(float target)
        {
            var initial = transform.localScale.x;
            var elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / 0.3f);
                transform.localScale = Vector3.one * Mathf.Lerp(initial, target, t * t * (3f - 2f * t));
                yield return null;
            }

            transform.localScale = Vector3.one * target;
            hover = null;
        }
    }
}
