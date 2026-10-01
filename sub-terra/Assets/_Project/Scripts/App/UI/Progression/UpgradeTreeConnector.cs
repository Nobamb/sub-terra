using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 상위 노드에서 하위 노드로 이어지는 회로형(직각) 연결선.
    /// 기본선(어두운 청록)과 점등선(밝은 청록)을 분리해, 해금 시 점등선만 부모에서 자식 방향으로 흐르게 한다.
    /// 연결선은 위치·관계만 보여 주므로 미해금 노드에도 항상 표시된다.
    /// </summary>
    public sealed class UpgradeTreeConnector : MonoBehaviour
    {
        [SerializeField] private string parentId;
        [SerializeField] private string childId;
        [SerializeField] private RectTransform litA;
        [SerializeField] private RectTransform litB;
        [SerializeField] private Vector2 startA;
        [SerializeField] private Vector2 dirA;
        [SerializeField] private float lengthA;
        [SerializeField] private Vector2 startB;
        [SerializeField] private Vector2 dirB;
        [SerializeField] private float lengthB;

        private bool childUnlocked;
        private Coroutine flowRoutine;

        public string ParentId => parentId;
        public string ChildId => childId;
        public bool IsFlowing => flowRoutine != null;
        public bool IsLit => childUnlocked && flowRoutine == null;

        public void Configure(
            string parent,
            string child,
            RectTransform lit1,
            Vector2 start1,
            Vector2 direction1,
            float length1,
            RectTransform lit2,
            Vector2 start2,
            Vector2 direction2,
            float length2)
        {
            parentId = parent ?? string.Empty;
            childId = child ?? string.Empty;
            litA = lit1;
            startA = start1;
            dirA = direction1;
            lengthA = length1;
            litB = lit2;
            startB = start2;
            dirB = direction2;
            lengthB = length2;
        }

        private void OnEnable()
        {
            flowRoutine = null;
            ApplySteady();
        }

        private void OnDisable()
        {
            flowRoutine = null;
            StopAllCoroutines();
            ApplySteady();
        }

        /// <summary>자식 노드가 해금되었는지에 따라 점등선을 켜거나 끈다. 흐름 연출 중에는 건드리지 않는다.</summary>
        public void SetChildUnlocked(bool unlocked)
        {
            childUnlocked = unlocked;
            if (flowRoutine == null)
            {
                ApplySteady();
            }
        }

        /// <summary>신규 해금 시 점등선이 부모에서 자식으로 흐른다.</summary>
        public void PlayFlow(float duration)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (flowRoutine != null)
            {
                StopCoroutine(flowRoutine);
            }

            flowRoutine = StartCoroutine(FlowRoutine(duration));
        }

        private IEnumerator FlowRoutine(float duration)
        {
            var total = Mathf.Max(0.01f, lengthA + lengthB);
            yield return UpgradeTreeTween.Run(duration, t =>
            {
                var travelled = total * UpgradeTreeTween.EaseOut(t);
                SetSegment(litA, startA, dirA, lengthA, Mathf.Clamp(travelled, 0f, lengthA));
                SetSegment(litB, startB, dirB, lengthB, Mathf.Clamp(travelled - lengthA, 0f, lengthB));
            });
            flowRoutine = null;
            ApplySteady();
        }

        private void ApplySteady()
        {
            var fill = childUnlocked ? 1f : 0f;
            SetSegment(litA, startA, dirA, lengthA, lengthA * fill);
            SetSegment(litB, startB, dirB, lengthB, lengthB * fill);
        }

        private static void SetSegment(RectTransform rect, Vector2 start, Vector2 dir, float fullLength, float length)
        {
            if (rect == null)
            {
                return;
            }

            var visible = length > 0.01f;
            var image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = visible;
            }

            if (!visible)
            {
                return;
            }

            var horizontal = Mathf.Abs(dir.x) > 0.5f;
            var size = rect.sizeDelta;
            if (horizontal)
            {
                size.x = length;
            }
            else
            {
                size.y = length;
            }

            rect.sizeDelta = size;
            rect.anchoredPosition = start + dir * (length * 0.5f);
        }
    }
}
