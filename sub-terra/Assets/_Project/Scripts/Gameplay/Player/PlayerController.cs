using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.Gameplay.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string moveActionPath = "Player/Move";
        [SerializeField] private string jumpActionPath = "Player/Jump";

        private PlayerMovement movement;
        private InputAction moveAction;
        private InputAction jumpAction;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            ResolveActions();
        }

        private void OnEnable()
        {
            ResolveActions();

            if (moveAction != null)
            {
                moveAction.Enable();
            }

            if (jumpAction != null)
            {
                // started: 누른 순간 1회만. performed 연타/홀드 반복을 피한다.
                jumpAction.started += OnJumpStarted;
                jumpAction.Enable();
            }
        }

        private void Update()
        {
            var input = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            movement.SetMoveInput(input.x);
            movement.SetVerticalMoveInput(input.y);
        }

        private void OnDisable()
        {
            if (moveAction != null)
            {
                moveAction.Disable();
            }

            if (jumpAction != null)
            {
                jumpAction.started -= OnJumpStarted;
                jumpAction.Disable();
            }

            movement?.SetMoveInput(0f);
            movement?.SetVerticalMoveInput(0f);
        }

        private void ResolveActions()
        {
            if (inputActions == null)
            {
                return;
            }

            moveAction ??= inputActions.FindAction(moveActionPath, false);
            jumpAction ??= inputActions.FindAction(jumpActionPath, false);
        }

        private void OnJumpStarted(InputAction.CallbackContext context)
        {
            if (!context.started)
            {
                return;
            }

            movement.RequestJump();
        }
    }
}
