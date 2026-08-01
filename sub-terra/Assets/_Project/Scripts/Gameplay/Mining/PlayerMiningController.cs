using SubTerra.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.Gameplay.Mining
{
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerMiningController : MonoBehaviour
    {
        [SerializeField] private MiningSystem miningSystem;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string mineActionPath = "Player/Attack";
        [SerializeField, Min(0f)] private float reach = 1.35f;

        private PlayerMovement movement;
        private InputAction mineAction;
        private bool startPending;
        private bool pendingPointerTarget;
        private Vector2 pendingWorldPoint;
        private bool miningInputPressedLastFrame;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            if (inputActions != null) mineAction = inputActions.FindAction(mineActionPath, false);
        }

        private void OnEnable() => mineAction?.Enable();

        private void OnDisable()
        {
            mineAction?.Disable();
            miningSystem?.CancelMining();
            startPending = false;
            miningInputPressedLastFrame = false;
        }

        private void Update()
        {
            if (miningSystem == null)
            {
                return;
            }

            bool miningInputPressed = IsMiningInputPressed();
            if (miningInputPressed && !miningInputPressedLastFrame)
            {
                CaptureCurrentTarget();
            }

            miningInputPressedLastFrame = miningInputPressed;
            if (!miningInputPressed)
            {
                startPending = false;
                miningSystem.CancelMining();
                return;
            }

            if (movement.IsMovementRequested)
            {
                miningSystem.CancelMining();
                return;
            }

            if (!miningSystem.IsMining && startPending)
            {
                startPending = false;
                TryStartPendingTarget();
            }

            if (!miningSystem.IsMining)
            {
                return;
            }

            miningSystem.TickMining(Time.deltaTime, movement.Position, reach);
        }

        private bool IsMiningInputPressed()
        {
            return (Keyboard.current != null && Keyboard.current.enterKey.isPressed)
                || (Mouse.current != null && Mouse.current.leftButton.isPressed)
                || (mineAction != null && mineAction.IsPressed());
        }

        private void CaptureCurrentTarget()
        {
            pendingPointerTarget = Mouse.current != null
                && Mouse.current.leftButton.isPressed;
            if (pendingPointerTarget && Camera.main != null)
            {
                Vector3 world = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                pendingWorldPoint = world;
            }
            else
            {
                pendingPointerTarget = false;
            }

            startPending = true;
        }

        private void TryStartPendingTarget()
        {
            if (pendingPointerTarget)
            {
                miningSystem.TryStartMiningAtWorldPoint(
                    pendingWorldPoint,
                    movement.Position,
                    reach);
            }
            else
            {
                miningSystem.TryStartMiningFrom(
                    movement.Position,
                    movement.FacingDirection,
                    reach);
            }
        }
    }
}
