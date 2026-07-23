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
                jumpAction.performed += OnJumpPerformed;
                jumpAction.Enable();
            }
        }

        private void Update()
        {
            movement.SetMoveInput(moveAction?.ReadValue<Vector2>().x ?? 0f);
        }

        private void OnDisable()
        {
            if (moveAction != null)
            {
                moveAction.Disable();
            }

            if (jumpAction != null)
            {
                jumpAction.performed -= OnJumpPerformed;
                jumpAction.Disable();
            }

            movement?.SetMoveInput(0f);
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

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            movement.RequestJump();
        }
    }
}
