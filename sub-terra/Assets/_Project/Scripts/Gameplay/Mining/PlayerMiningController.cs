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
        private MiningDebrisVisual debrisVisual;
        private MiningSystem debrisMiningSystem;
        private MiningSystem animationMiningSystem;
        private static readonly List<RaycastResult> PointerHits = new(8);
        private System.Func<bool> pointerMiningBlocker;
        private bool pointerClaimedByPlacement;
        private bool enterMiningSuppressed;
        private bool activeMiningOnLadder;
        private bool hasMiningAnimationContext;

        /// <summary>B-166: 창을 Enter로 확인한 뒤 그 누르기가 채굴로 이어지지 않게, Enter를 한 번 뗄 때까지 채굴 키로 보지 않는다.</summary>
        public void SuppressEnterUntilReleased()
        {
            enterMiningSuppressed = true;
        }

        public void SetPointerMiningBlocker(System.Func<bool> blocker)
        {
            pointerMiningBlocker = blocker;
        }

        public bool IsMining => miningSystem != null && miningSystem.IsMining;

        /// <summary>B-164: 채굴 키를 새로 누른 순간(누르기 시작·방향 바꿈). 실패음 반복 판정을 풀 때 쓴다.</summary>
        public event System.Action MiningPressed;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            animationController = GetComponentInChildren<PlayerAnimationController>(true);
            if (inputActions != null) mineAction = inputActions.FindAction(mineActionPath, false);
            EnsureDebrisVisual();
        }

        private void OnEnable()
        {
            mineAction?.Enable();
            BindAnimationEvents();
        }

        private void OnDisable()
        {
            mineAction?.Disable();
            miningSystem?.CancelMining();
            if (animationMiningSystem != null)
                animationMiningSystem.ProgressChanged -= HandleAnimationProgress;
            animationMiningSystem = null;
            if (animationController != null) animationController.SetMining(false);
            startPending = false;
            miningInputPressedLastFrame = false;
            lastMiningDirection = Vector2.zero;
            hasMiningAnimationContext = false;
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
                pointerClaimedByPlacement = false;
            else if (pointerMiningBlocker != null && pointerMiningBlocker())
                pointerClaimedByPlacement = true;

            EnsureDebrisVisual();
            BindAnimationEvents();
            if (miningSystem == null)
            {
                animationController?.SetMining(false);
                return;
            }

            // 사다리/바닥 전환과 등반·점프는 역재생 없이 즉시 끊는다.
            bool onLadder = animationController != null && animationController.CanMineOnLadder;
            if (hasMiningAnimationContext && miningSystem.IsMining
                && ((activeMiningOnLadder != onLadder)
                    || (activeMiningOnLadder && (movement.IsMovementRequested || movement.IsJumpInProgress))))
            {
                miningSystem.CancelMining();
                if (animationController != null) animationController.InterruptMining();
                startPending = false;
                hasMiningAnimationContext = false;
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

            // 설치가 성공해 선택이 풀려도 같은 누르기 동작은 채굴로 재사용하지 않는다.
            if (pointerClaimedByPlacement && pendingPointerTarget)
            {
                miningSystem.CancelMining();
                startPending = false;
                pendingPointerTarget = false;
                miningInputPressedLastFrame = false;
            }

            Vector2 direction = PlayerKeyboardControls.ReadMiningDirection(ControlPreferences.Scheme);
            bool miningInputPressed = IsMiningInputPressed();
            bool directionChanged = direction != Vector2.zero && direction != lastMiningDirection;
            if (miningInputPressed && (!miningInputPressedLastFrame || directionChanged))
            {
                MiningPressed?.Invoke();
            }

            if (directionChanged)
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

            ShowMiningAnimation();
            miningSystem.TickMining(Time.deltaTime, movement.Position, reach);
        }

        private void EnsureDebrisVisual()
        {
            // Integration may supply the mining system after this controller's Awake.
            if (miningSystem == null || debrisMiningSystem == miningSystem) return;
            if (debrisVisual == null) debrisVisual = GetComponent<MiningDebrisVisual>();
            if (debrisVisual == null) debrisVisual = gameObject.AddComponent<MiningDebrisVisual>();
            debrisVisual.Configure(miningSystem, transform);
            debrisMiningSystem = miningSystem;
        }

        private void BindAnimationEvents()
        {
            if (animationMiningSystem == miningSystem) return;
            if (animationMiningSystem != null)
                animationMiningSystem.ProgressChanged -= HandleAnimationProgress;
            animationMiningSystem = miningSystem;
            if (animationMiningSystem != null)
                animationMiningSystem.ProgressChanged += HandleAnimationProgress;
        }

        private void HandleAnimationProgress(MiningProgressState progress)
        {
            if (animationController == null) return;
            if (progress.Phase == MiningPhase.Mining) ShowMiningAnimation();
            else if (progress.Phase == MiningPhase.Completed) animationController.NotifyMiningCompleted();
            else if (progress.Phase == MiningPhase.Cancelled) animationController.SetMining(false);
            if (progress.Phase != MiningPhase.Mining) hasMiningAnimationContext = false;
        }

        private void ShowMiningAnimation()
        {
            if (animationController == null) return;
            if (!hasMiningAnimationContext)
            {
                activeMiningOnLadder = animationController.CanMineOnLadder;
                hasMiningAnimationContext = true;
            }
            if (activeMiningOnLadder && miningSystem.ForegroundTilemap != null)
            {
                var playerCell = miningSystem.ForegroundTilemap.WorldToCell(movement.Position);
                animationController.SetLadderMining(miningSystem.ActiveCell.x - playerCell.x);
            }
            else animationController.SetMining(true);
        }

        private bool IsMiningInputPressed()
        {
            if (PlayerKeyboardControls.ReadMiningDirection(ControlPreferences.Scheme) != Vector2.zero)
                return true;
            bool enterHeld = Keyboard.current != null
                && Keyboard.current.enterKey.isPressed;
            if (!enterHeld)
            {
                enterMiningSuppressed = false;
            }

            bool enterMining = enterHeld && !enterMiningSuppressed;
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
            if (mousePressed && (pointerClaimedByPlacement || IsPointerOverUi()))
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
            pendingDirection = PlayerKeyboardControls.ReadMiningDirection(ControlPreferences.Scheme);
            pendingPointerTarget = Mouse.current != null
                && Mouse.current.leftButton.isPressed && !pointerClaimedByPlacement && pendingDirection == Vector2.zero;
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
            if (animationController != null && animationController.CanMineOnLadder)
            {
                if (miningSystem.ForegroundTilemap == null || pendingDirection.y != 0f) return;
                if (pendingPointerTarget)
                {
                    var map = miningSystem.ForegroundTilemap;
                    var playerCell = map.WorldToCell(movement.Position);
                    var targetCell = map.WorldToCell(pendingWorldPoint);
                    if (targetCell.y != playerCell.y || targetCell.x == playerCell.x) return;
                }
                else
                {
                    // Enter의 위·아래 대체 목표 탐색도 사다리에서는 사용하지 않는다.
                    var direction = pendingDirection != Vector2.zero ? pendingDirection
                        : new Vector2(movement.FacingDirection, 0f);
                    miningSystem.TryStartMiningInDirection(movement.Position, direction, reach);
                    return;
                }
            }
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
