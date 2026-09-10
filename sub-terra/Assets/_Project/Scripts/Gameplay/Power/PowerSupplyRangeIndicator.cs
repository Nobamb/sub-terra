using UnityEngine;

namespace SubTerra.Gameplay.Power
{
    /// <summary>전진기지 코어의 전력 공급·가스 정화 공통 범위를 파란 원으로 표시한다.</summary>
    public sealed class PowerSupplyRangeIndicator : MonoBehaviour
    {
        public const float DefaultRadius = 10f;

        private const int CircleSegments = 96;
        private static readonly Color RangeColor = new(0.1f, 0.55f, 1f, 0.78f);

        [SerializeField, Min(0.1f)] private float radius = DefaultRadius;
        [SerializeField] private LineRenderer rangeLine;
        private Material runtimeMaterial;

        public float Radius => radius;
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
            EnsureLine();
            RebuildCircle();
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
            rangeLine.startWidth = 0.08f;
            rangeLine.endWidth = 0.08f;
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

            for (var index = 0; index < CircleSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / CircleSegments;
                rangeLine.SetPosition(
                    index,
                    new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }
    }
}
