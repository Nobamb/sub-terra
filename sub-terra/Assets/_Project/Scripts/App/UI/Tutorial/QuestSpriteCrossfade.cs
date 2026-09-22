using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 같은 자리의 두 스프라이트를 0.5초 알파 크로스페이드한다.
    /// 클릭 판정은 이 오브젝트의 투명 Image가 맡고, 그림은 자식만 그린다.
    /// </summary>
    public sealed class QuestSpriteCrossfade : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const float DurationSeconds = 0.5f;

        [SerializeField] private Image normalImage;
        [SerializeField] private Image hoverImage;
        [SerializeField] private Selectable selectable;

        private bool pointed;
        private float alpha;

        public float HoverAlpha => alpha;

        private void OnEnable()
        {
            pointed = false;
            alpha = 0f;
            Apply();
        }

        private void OnDisable()
        {
            pointed = false;
            alpha = 0f;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointed = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointed = false;
        }

        /// <summary>편집 모드 검증에서 포인터 상태를 직접 넣는다.</summary>
        public void SetPointed(bool value)
        {
            pointed = value;
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Tick(Time.unscaledDeltaTime);
        }

        /// <summary>호버 알파를 deltaTime만큼 목표로 이동한다. 0.5초면 끝이 난다.</summary>
        public void Tick(float deltaTime)
        {
            var target = pointed && IsInteractable() ? 1f : 0f;
            var step = DurationSeconds <= 0f ? 1f : deltaTime / DurationSeconds;
            alpha = Mathf.MoveTowards(alpha, target, step);
            Apply();
        }

        private bool IsInteractable()
        {
            return selectable == null || selectable.IsInteractable();
        }

        private void Apply()
        {
            SetAlpha(normalImage, 1f);
            SetAlpha(hoverImage, alpha);
        }

        private static void SetAlpha(Image image, float value)
        {
            if (image == null)
            {
                return;
            }

            var color = image.color;
            color.a = value;
            image.color = color;
        }
    }
}
