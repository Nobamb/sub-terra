using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 같은 자리의 두 스프라이트를 0.3초 알파 크로스페이드한다.
    /// 클릭은 불투명한 기본 스프라이트가 받고, 호버 그림은 그 위에만 겹친다.
    /// </summary>
    public sealed class QuestSpriteCrossfade : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const float DurationSeconds = 0.3f;

        [SerializeField] private Image normalImage;
        [SerializeField] private Image hoverImage;
        [SerializeField] private Selectable selectable;

        private bool pointed;
        private float alpha;

        public float HoverAlpha => alpha;

        private void OnEnable()
        {
            // 알파 0인 히트 이미지는 Cull Transparent Mesh 때문에 레이캐스트에서 빠진다.
            // 보이는 기본 스프라이트가 클릭과 호버를 받아야 상세창이 열린다.
            var hit = GetComponent<CanvasRenderer>();
            if (hit != null)
            {
                hit.cullTransparentMesh = false;
            }

            var graphic = GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
            }

            if (normalImage != null)
            {
                normalImage.raycastTarget = true;
            }

            if (hoverImage != null)
            {
                hoverImage.raycastTarget = false;
            }

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

        /// <summary>호버 알파를 deltaTime만큼 목표로 이동한다. 0.3초면 끝이 난다.</summary>
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
