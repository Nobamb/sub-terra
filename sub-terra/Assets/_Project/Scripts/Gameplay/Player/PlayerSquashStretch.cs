using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>
    /// B-211: 점프 순간 위로 늘어나고 착지 순간 옆으로 찌그러졌다가 돌아온다. VisualRoot의 크기만 바꾸고 충돌체·물리는 건드리지 않는다.
    /// Update에서 지난 프레임 값을 걷어 내 PlayerAnimationController(ApplyJumpBob)·PlayerFacing(100)이 기본 값을 보게 하고,
    /// 그 뒤 LateUpdate(200)에서 좌우 부호를 유지한 채 크기와 발 기준 위치 보정을 다시 얹는다.
    /// </summary>
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerSquashStretch : MonoBehaviour
    {
        private const float DefaultFootFromPivot = -0.35f;

        [SerializeField] private Transform visualRoot;
        [Tooltip("0이면 꺼짐. 줄인 움직임에서도 유지한다.")]
        [SerializeField, Range(0f, 1.5f)] private float squashStrength = 1f;
        [SerializeField] private Vector2 jumpStretch = new(0.88f, 1.12f);
        [Tooltip("0.3칸 착지(가로, 세로).")]
        [SerializeField] private Vector2 lightLandingSquash = new(1.05f, 0.95f);
        [Tooltip("3칸 이상 착지(가로, 세로).")]
        [SerializeField] private Vector2 heavyLandingSquash = new(1.18f, 0.82f);
        [SerializeField, Min(0.01f)] private float squashRecoverSeconds = 0.12f;

        private PlayerMovement movement;
        private PlayerAnimationController animationController;
        private Vector2 baseScale = Vector2.one;
        private float footFromPivot = DefaultFootFromPivot;
        private Vector2 peak = Vector2.one;
        private float elapsed;
        private bool active;
        private bool allowDuringHitStun;
        private int observedJumpCount;
        private bool applied;
        private float appliedOffsetY;
        private bool subscribed;

        public bool IsActive => active;
        public Vector2 CurrentFactor { get; private set; } = Vector2.one;
        public float SquashStrength
        {
            get => squashStrength;
            set => squashStrength = Mathf.Clamp(value, 0f, 1.5f);
        }

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            animationController = GetComponentInChildren<PlayerAnimationController>(true);
            if (visualRoot == null)
            {
                visualRoot = animationController != null ? animationController.transform : transform.Find("VisualRoot");
            }

            if (visualRoot != null)
            {
                var scale = visualRoot.localScale;
                baseScale = new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                footFromPivot = ResolveFootFromPivot(visualRoot.localPosition.y);
            }
        }

        private void OnEnable()
        {
            if (movement != null)
            {
                observedJumpCount = movement.JumpCount;
                if (!subscribed)
                {
                    movement.Landed += OnLanded;
                    subscribed = true;
                }
            }
        }

        private void OnDisable()
        {
            if (movement != null && subscribed)
            {
                movement.Landed -= OnLanded;
            }

            subscribed = false;
            active = false;
            RemoveApplied();
        }

        private void Update()
        {
            RemoveApplied();
        }

        private void LateUpdate()
        {
            if (visualRoot == null || movement == null)
            {
                return;
            }

            if (movement.JumpCount != observedJumpCount)
            {
                observedJumpCount = movement.JumpCount;
                if (!movement.IsClimbing) Begin(jumpStretch, false);
            }

            if (active && ShouldCancel())
            {
                active = false;
            }

            var factor = Vector2.one;
            if (active)
            {
                elapsed += Time.deltaTime;
                float weight = PlayerLandingFeedbackRules.RecoverWeight(elapsed, squashRecoverSeconds);
                if (weight <= 0f)
                {
                    active = false;
                }
                else
                {
                    var target = PlayerLandingFeedbackRules.ApplyStrength(peak, squashStrength);
                    factor = Vector2.one + (target - Vector2.one) * weight;
                }
            }

            Apply(factor);
        }

        private void OnLanded(PlayerLandingInfo landing)
        {
            if (!PlayerLandingFeedbackRules.ShouldSquash(landing) || movement.IsClimbing)
            {
                active = false;
                return;
            }

            Begin(PlayerLandingFeedbackRules.LandingSquash(landing.Distance, lightLandingSquash, heavyLandingSquash),
                landing.Damaged);
        }

        private void Begin(Vector2 nextPeak, bool keepThroughHitStun)
        {
            if (squashStrength <= 0f)
            {
                active = false;
                return;
            }

            peak = nextPeak;
            elapsed = 0f;
            allowDuringHitStun = keepThroughHitStun;
            active = true;
        }

        // 사다리·경직 그림·기절에서는 진행 중인 효과를 바로 1로 되돌린다. 피해 착지 자신의 찌그러짐만 경직 그림 위에서 유지한다.
        private bool ShouldCancel()
        {
            if (squashStrength <= 0f || movement.IsClimbing) return true;
            if (animationController == null) return false;
            return animationController.IsShowingLadderVisual
                || animationController.IsShowingKnockoutVisual
                || (animationController.IsShowingHitStunVisual && !allowDuringHitStun);
        }

        private void Apply(Vector2 factor)
        {
            CurrentFactor = factor;
            if (factor == Vector2.one)
            {
                return;
            }

            var scale = visualRoot.localScale;
            float sign = scale.x < 0f ? -1f : 1f;
            scale.x = sign * baseScale.x * factor.x;
            scale.y = baseScale.y * factor.y;
            visualRoot.localScale = scale;

            appliedOffsetY = PlayerLandingFeedbackRules.FootAnchorOffset(footFromPivot, factor.y);
            var position = visualRoot.localPosition;
            position.y += appliedOffsetY;
            visualRoot.localPosition = position;
            applied = true;
        }

        private void RemoveApplied()
        {
            if (!applied || visualRoot == null)
            {
                applied = false;
                return;
            }

            var scale = visualRoot.localScale;
            scale.x = (scale.x < 0f ? -1f : 1f) * baseScale.x;
            scale.y = baseScale.y;
            visualRoot.localScale = scale;
            var position = visualRoot.localPosition;
            position.y -= appliedOffsetY;
            visualRoot.localPosition = position;
            appliedOffsetY = 0f;
            applied = false;
        }

        // 발바닥 = 몸 충돌체 바닥. 그림 뿌리 기준 세로 거리로 바꾼다.
        private float ResolveFootFromPivot(float visualLocalY)
        {
            // 생성 직후에는 물리 Bounds가 아직 없을 수 있어 모양 값으로 직접 잰다.
            float foot;
            switch (GetComponent<Collider2D>())
            {
                case CapsuleCollider2D capsule:
                    foot = capsule.offset.y - capsule.size.y * 0.5f;
                    break;
                case BoxCollider2D box:
                    foot = box.offset.y - box.size.y * 0.5f - box.edgeRadius;
                    break;
                case CircleCollider2D circle:
                    foot = circle.offset.y - circle.radius;
                    break;
                default:
                    foot = DefaultFootFromPivot;
                    break;
            }

            return foot - visualLocalY;
        }
    }
}
