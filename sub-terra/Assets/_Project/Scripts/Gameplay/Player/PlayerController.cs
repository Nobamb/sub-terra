using SubTerra.Shared;
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
        [SerializeField] private string interactActionPath = "Player/Interact";

        private PlayerMovement movement;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction interactAction;

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

            interactAction?.Enable();
        }

        private void Update()
        {
            var input = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            if (ControlPreferences.Scheme != ControlScheme.Classic)
            {
                // 키보드 합성 Move의 채굴 키를 제외하되 다른 장치의 입력은 유지한다.
                var keyboardInput = PlayerKeyboardControls.ReadMovement(Keyboard.current, ControlPreferences.Scheme);
                input = moveAction?.activeControl?.device is Keyboard || input == Vector2.zero
                    ? keyboardInput : input;
            }
            if (ControlPreferences.IsSettingsOpen) input = Vector2.zero;
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

            interactAction?.Disable();

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
            interactAction ??= inputActions.FindAction(interactActionPath, false);
        }

        private void OnJumpStarted(InputAction.CallbackContext context)
        {
            if (!context.started || ControlPreferences.IsSettingsOpen)
            {
                return;
            }

            movement.RequestJump();
        }
    }
}
