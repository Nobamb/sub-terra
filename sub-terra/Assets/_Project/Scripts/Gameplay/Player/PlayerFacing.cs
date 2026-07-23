using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerFacing : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;

        private PlayerMovement movement;
        private float lastDirection = 1f;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
        }

        private void LateUpdate()
        {
            if (visualRoot == null || Mathf.Approximately(lastDirection, movement.FacingDirection))
            {
                return;
            }

            lastDirection = movement.FacingDirection;
            Vector3 scale = visualRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * lastDirection;
            visualRoot.localScale = scale;
        }
    }
}
