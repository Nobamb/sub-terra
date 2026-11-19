using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Power
{
    /// <summary>전진기지 코어의 전력 공급·가스 정화 공통 범위를 파란 원으로 표시한다.</summary>
    public sealed class PowerSupplyRangeIndicator : MonoBehaviour
    {
        public const float DefaultRadius = 10f;

        private const int CircleSegments = 96;
        private static readonly Color RangeColor = new(0.12f, 0.48f, 0.68f, 0.25f);

        [SerializeField, Min(0.1f)] private float radius = DefaultRadius;
        [SerializeField] private LineRenderer rangeLine;
        private Material runtimeMaterial;
        private Vector3[] circlePoints;
        private float spreadProgress = 1f;
        private float spreadDelay;

        /// <summary>전력 공급·정화 판정 반지름. 퍼짐 연출 중에도 항상 최종값이다.</summary>
        public float Radius => radius;

        /// <summary>화면에 그려지는 원의 반지름. 퍼짐 연출 중에만 Radius보다 작다.</summary>
        public float VisualRadius => radius * RangeSpreadMath.RadiusFraction(spreadProgress);

        public bool IsSpreading => spreadProgress < 1f;
        public LineRenderer RangeLine => rangeLine;

        private void Awake()
        {
            EnsureLine();
            RebuildCircle();
        }

        private void OnEnable()
        {
            EnsureLine();
            RebuildCircle();
        }

        public void Configure(float supplyRadius)
        {
            radius = Mathf.Max(0.1f, supplyRadius);
            spreadProgress = 1f;
            EnsureLine();
            RebuildCircle();
        }

        /// <summary>
        /// B-204: 원 표시만 중심에서 최종 반지름까지 빠르게 퍼뜨린다. Contains(전력 판정)는 처음부터 최종 반지름이다.
        /// 모션 감소면 연출 없이 최종 원으로 둔다. 복원·기존 코어는 이 메서드를 부르지 않는다.
        /// </summary>
        public void BeginSpread(float delaySeconds = 0f)
        {
            if (AccessibilityPreferences.ReduceMotion)
            {
                spreadProgress = 1f;
                spreadDelay = 0f;
            }
            else
            {
                spreadProgress = 0f;
                // B-206: 건설 연출이 끝난 뒤 퍼지도록 시작만 미룬다. 판정 반지름에는 영향 없다.
                spreadDelay = Mathf.Max(0f, delaySeconds);
            }

            EnsureLine();
            RebuildCircle();
        }

        /// <summary>퍼짐을 deltaTime만큼 진행한다. Update가 부르며 테스트도 직접 부를 수 있다.</summary>
        public void AdvanceSpread(float deltaTime)
        {
            if (spreadProgress >= 1f)
            {
                return;
            }

            if (spreadDelay > 0f)
            {
                spreadDelay -= Mathf.Max(0f, deltaTime);
                if (spreadDelay > 0f)
                {
                    return;
                }

                deltaTime = -spreadDelay;
                spreadDelay = 0f;
            }

            spreadProgress = AccessibilityPreferences.ReduceMotion
                ? 1f
                : RangeSpreadMath.Advance(spreadProgress, deltaTime, RangeSpreadMath.CoreSpreadSeconds);
            RebuildCircle();
        }

        private void Update()
        {
            AdvanceSpread(Time.deltaTime);
        }

        public bool Contains(Vector3 worldPosition)
        {
            Vector2 delta = worldPosition - transform.position;
            return delta.sqrMagnitude <= radius * radius;
        }

        private void OnDestroy()
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeMaterial);
            }
            else
            {
                DestroyImmediate(runtimeMaterial);
            }
        }

        private void EnsureLine()
        {
            if (rangeLine == null)
            {
                rangeLine = GetComponent<LineRenderer>();
            }

            if (rangeLine == null)
            {
                rangeLine = gameObject.AddComponent<LineRenderer>();
            }

            rangeLine.useWorldSpace = false;
            rangeLine.loop = true;
            rangeLine.positionCount = CircleSegments;
            rangeLine.startWidth = 0.035f;
            rangeLine.endWidth = 0.035f;
            rangeLine.startColor = RangeColor;
            rangeLine.endColor = RangeColor;
            rangeLine.numCornerVertices = 2;
            rangeLine.numCapVertices = 2;
            rangeLine.sortingOrder = 2;

            if (rangeLine.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    runtimeMaterial = new Material(shader)
                    {
                        name = "OutpostPowerRange_Runtime",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    rangeLine.sharedMaterial = runtimeMaterial;
                }
            }
        }

        private void RebuildCircle()
        {
            if (rangeLine == null)
            {
                return;
            }

            circlePoints ??= new Vector3[CircleSegments];
            var drawRadius = VisualRadius;
            for (var index = 0; index < CircleSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / CircleSegments;
                circlePoints[index] = new Vector3(Mathf.Cos(angle) * drawRadius, Mathf.Sin(angle) * drawRadius, 0f);
            }

            rangeLine.SetPositions(circlePoints);
        }
    }
}
