using System.Collections.Generic;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private Vector2 pendingDirection;
        private Vector2 lastMiningDirection;
        private bool miningInputPressedLastFrame;
        private PlayerAnimationController animationController;
        private static readonly List<RaycastResult> PointerHits = new(8);

        public bool IsMining => miningSystem != null && miningSystem.IsMining;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            animationController = GetComponentInChildren<PlayerAnimationController>(true);
            if (inputActions != null) mineAction = inputActions.FindAction(mineActionPath, false);
        }

        private void OnEnable() => mineAction?.Enable();

        private void OnDisable()
        {
            mineAction?.Disable();
            miningSystem?.CancelMining();
            startPending = false;
            miningInputPressedLastFrame = false;
            lastMiningDirection = Vector2.zero;
        }

        private void Update()
        {
            if (miningSystem == null)
            {
                animationController?.SetMining(false);
                return;
            }

            if (ControlPreferences.IsSettingsOpen)
            {
                miningSystem.CancelMining();
                startPending = false;
                miningInputPressedLastFrame = false;
                lastMiningDirection = Vector2.zero;
                if (animationController != null) animationController.SetMining(false);
                return;
            }

            Vector2 direction = PlayerKeyboardControls.ReadMiningDirection(Keyboard.current, ControlPreferences.Scheme);
            bool miningInputPressed = IsMiningInputPressed();
            if (direction != Vector2.zero && direction != lastMiningDirection)
            {
                miningSystem.CancelMining();
                CaptureCurrentTarget();
            }
            lastMiningDirection = direction;
            if (miningInputPressed && !miningInputPressedLastFrame && !miningSystem.IsMining)
            {
                CaptureCurrentTarget();
            }

            miningInputPressedLastFrame = miningInputPressed;
            if (movement.IsMovementRequested)
            {
                startPending = false;
                miningSystem.CancelMining();
                animationController?.SetMining(false);
                miningSystem.ClearFailureIfDirectionalTargetMineable(
                    movement.Position,
                    movement.FacingDirection,
                    reach);
                return;
            }

            if (!miningSystem.IsMining && startPending)
            {
                startPending = false;
                TryStartPendingTarget();
            }

            if (!miningSystem.IsMining)
            {
                animationController?.SetMining(false);
                return;
            }

            animationController?.SetMining(true);
            miningSystem.TickMining(Time.deltaTime, movement.Position, reach);
        }

        private bool IsMiningInputPressed()
        {
            if (PlayerKeyboardControls.ReadMiningDirection(Keyboard.current, ControlPreferences.Scheme) != Vector2.zero)
                return true;
            bool enterMining = Keyboard.current != null
                && Keyboard.current.enterKey.isPressed;
            if (enterMining)
            {
                return true;
            }

            bool mousePressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
            bool actionPressed = mineAction != null && mineAction.IsPressed();
            if (!mousePressed && !actionPressed)
            {
                return false;
            }

            // 머리 위 구출 칩·팝업 등 UI 클릭이 채굴로 통과하지 않게 한다.
            if (mousePressed && IsPointerOverUi())
            {
                return false;
            }

            return true;
        }

        private static bool IsPointerOverUi()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (eventSystem.IsPointerOverGameObject())
            {
                return true;
            }

            if (Mouse.current == null)
            {
                return false;
            }

            var eventData = new PointerEventData(eventSystem)
            {
                position = Mouse.current.position.ReadValue()
            };
            PointerHits.Clear();
            eventSystem.RaycastAll(eventData, PointerHits);
            return PointerHits.Count > 0;
        }

        private void CaptureCurrentTarget()
        {
            pendingDirection = PlayerKeyboardControls.ReadMiningDirection(Keyboard.current, ControlPreferences.Scheme);
            pendingPointerTarget = Mouse.current != null
                && Mouse.current.leftButton.isPressed && pendingDirection == Vector2.zero;
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
            if (pendingDirection != Vector2.zero)
            {
                miningSystem.TryStartMiningInDirection(movement.Position, pendingDirection, reach);
            }
            else if (pendingPointerTarget)
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
