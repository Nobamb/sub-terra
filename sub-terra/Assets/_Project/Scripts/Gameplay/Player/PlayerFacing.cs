using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    [DefaultExecutionOrder(100)]
    public sealed class PlayerFacing : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;

        private PlayerMovement movement;
        private PlayerAnimationController animationController;
        private float lastDirection = 1f;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            animationController = GetComponentInChildren<PlayerAnimationController>(true);
        }

        private void LateUpdate()
        {
            if (visualRoot == null)
            {
                return;
            }

            // 표시 상태를 LateUpdate 순서와 무관하게 읽도록 애니메이션 갱신 후 실행한다.
            lastDirection = animationController != null && animationController.IsShowingLadderMiningVisual
                ? animationController.LadderMiningDirection
                : animationController != null && animationController.IsShowingLadderDamageVisual
                    ? animationController.LadderDamageDirection
                : animationController != null && animationController.IsShowingLadderVisual
                    ? 1f : movement.FacingDirection;
            Vector3 scale = visualRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * lastDirection;
            visualRoot.localScale = scale;
        }
    }
}
