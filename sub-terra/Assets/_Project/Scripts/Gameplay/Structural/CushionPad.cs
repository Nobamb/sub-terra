using System;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>
    /// 완충판. 설치 원점(셀 중심)은 그대로 두고, 윗면(착지면 콜라이더·윗면 그림)만 높이에 따라 오르내린다.
    /// 기둥은 Tiled SpriteRenderer를 늘려 윗면 아래를 채운다. 높이는 지면 기준 윗면까지의 거리다.
    /// 기본 높이는 최소 높이이며 만적 점프로도 올라갈 수 있게 낮게 둔다.
    /// </summary>
    public sealed class CushionPad : MonoBehaviour, IAdjustableLandingPlatform
    {
        [SerializeField, Min(0.1f)] private float minHeight = CushionPadRules.DefaultMinHeight;
        [SerializeField, Min(0.1f)] private float maxHeight = CushionPadRules.DefaultMaxHeight;
        [SerializeField, Min(0.01f)] private float stepHeight = CushionPadRules.DefaultStepHeight;
        [SerializeField, Tooltip("설치 원점(루트)에서 지면까지의 거리. 셀 중심 설치라 0.5.")]
        private float pivotAboveGround = 0.5f;
        [SerializeField, Min(0.05f), Tooltip("윗면 콜라이더 두께.")]
        private float surfaceThickness = 0.4f;
        [SerializeField, Min(0f), Tooltip("윗면 그림 아래로 기둥이 겹쳐 들어가는 길이(틈 방지).")]
        private float postOverlap = 0.04f;
        [SerializeField] private BoxCollider2D surfaceCollider;
        [SerializeField] private SpriteRenderer topRenderer;
        [SerializeField] private SpriteRenderer postRenderer;

        private float currentHeight;
        private bool initialized;

        public float MinHeight => minHeight;
        public float MaxHeight => Mathf.Max(minHeight, maxHeight);
        public float StepHeight => stepHeight;
        public float CurrentHeight
        {
            get
            {
                EnsureInitialized();
                return currentHeight;
            }
        }

        /// <summary>윗면 그림(피벗: 윗변 중앙).</summary>
        public SpriteRenderer TopRenderer => topRenderer;

        /// <summary>반복 기둥(Tiled, 피벗: 바닥 중앙). 설치 미리보기가 같은 모습을 그릴 때 쓴다.</summary>
        public SpriteRenderer PostRenderer => postRenderer;

        public event Action<CushionPad> HeightChanged;

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;
            if (surfaceCollider == null) surfaceCollider = GetComponent<BoxCollider2D>();
            currentHeight = minHeight;
            ApplyLayout();
        }

        /// <summary>저장값 등으로 높이를 직접 정한다. 0 이하·깨진 값은 최소 높이다. 변화량을 돌려준다.</summary>
        public float SetHeight(float height)
        {
            EnsureInitialized();
            float next = CushionPadRules.NormalizeHeight(height, minHeight, MaxHeight);
            float delta = next - currentHeight;
            currentHeight = next;
            ApplyLayout();
            if (Mathf.Abs(delta) > 0.0001f) HeightChanged?.Invoke(this);
            return delta;
        }

        public bool TryStepHeight(int direction, out float heightDelta, Func<float, bool> canApplyDelta = null)
        {
            EnsureInitialized();
            float next = CushionPadRules.StepHeight(currentHeight, direction, minHeight, MaxHeight, stepHeight);
            heightDelta = next - currentHeight;
            if (Mathf.Abs(heightDelta) <= 0.0001f || (canApplyDelta != null && !canApplyDelta(heightDelta)))
            {
                heightDelta = 0f;
                return false;
            }

            SetHeight(next);
            return true;
        }

        /// <summary>윗면 콜라이더와 그림, 기둥 길이를 현재 높이에 맞춘다. 윗면 로컬 y = 높이 - 원점~지면 거리.</summary>
        public void ApplyLayout()
        {
            float surfaceY = currentHeight - pivotAboveGround;
            if (surfaceCollider != null)
            {
                surfaceCollider.size = new Vector2(surfaceCollider.size.x, surfaceThickness);
                surfaceCollider.offset = new Vector2(surfaceCollider.offset.x, surfaceY - surfaceThickness * 0.5f);
            }

            // 윗면 그림은 피벗이 윗변 중앙이라 윗면 높이에 그대로 놓는다.
            float topArtHeight = 0f;
            if (topRenderer != null)
            {
                topRenderer.transform.localPosition = new Vector3(0f, surfaceY, 0f);
                if (topRenderer.sprite != null) topArtHeight = topRenderer.sprite.bounds.size.y;
            }

            // 기둥은 피벗이 바닥 중앙이다. 지면에서 윗면 그림 아래(조금 겹쳐서)까지 반복 조각을 늘린다.
            if (postRenderer != null)
            {
                float groundY = -pivotAboveGround;
                float postHeight = Mathf.Max(0.01f, currentHeight - topArtHeight + postOverlap);
                postRenderer.transform.localPosition = new Vector3(0f, groundY, 0f);
                postRenderer.size = new Vector2(postRenderer.size.x, postHeight);
            }
        }

        private void OnValidate()
        {
            if (maxHeight < minHeight) maxHeight = minHeight;
        }
    }
}
