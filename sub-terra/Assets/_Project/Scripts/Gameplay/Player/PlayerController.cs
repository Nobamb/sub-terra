using SubTerra.Shared;
using SubTerra.Shared.Audio;
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

        private const float PadRaisePitch = 1.08f;
        private const float PadLowerPitch = 0.92f;

        private PlayerMovement movement;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction interactAction;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            ResolveActions();
            // 첫 사용 안내 홀로그램. 프리팹을 건드리지 않고 런타임에 한 번 붙인다.
            if (GetComponent<CushionPadHintHologram>() == null)
            {
                gameObject.AddComponent<CushionPadHintHologram>();
            }

            // B-211 점프·착지 손맛. 프리팹 YAML 대신 런타임에 붙인다.
            if (GetComponent<PlayerSquashStretch>() == null)
            {
                gameObject.AddComponent<PlayerSquashStretch>();
            }

            if (GetComponent<PlayerLandingDust>() == null)
            {
                gameObject.AddComponent<PlayerLandingDust>();
            }
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
                // B-211: 뗀 순간(canceled)으로 가변 점프 높이를 정한다.
                jumpAction.canceled += OnJumpCanceled;
                jumpAction.Enable();
            }

            interactAction?.Enable();
        }

        private void Update()
        {
            // 완충판 위: 위/아래 화살표는 높이 조절 전용(이동·사다리·채굴 방향에서 빠진다).
            var onPad = movement.IsOnAdjustablePlatform && !ControlPreferences.IsSettingsOpen;
            PlayerKeyboardControls.AdjustablePadActive = onPad;
            if (onPad)
            {
                var step = PlayerKeyboardControls.ReadPadStep();
                if (step != 0 && movement.TryAdjustStandingPlatform(step))
                {
                    // 새 소리 없이 기존 키캡 소리만 쓰고, 오르내림은 피치로 구분한다.
                    SfxRequests.Play(SfxId.KeycapPop, step > 0 ? PadRaisePitch : PadLowerPitch);
                }
            }

            // Move InputAction은 WASD+방향키 한 합성이다. activeControl.device가 null인
            // 경우가 있어 채굴 전용 키가 이동으로 새어 나간다. 키보드는 항상 선택 방식을 따른다.
            var input = PlayerKeyboardControls.ReadMovement(ControlPreferences.Scheme);
            if (input == Vector2.zero
                && moveAction != null
                && !PlayerKeyboardControls.IsCompositeMoveKeyPressed())
            {
                input = moveAction.ReadValue<Vector2>();
            }

            if (ControlPreferences.IsSettingsOpen) input = Vector2.zero;
            movement.SetMoveInput(input.x);
            movement.SetVerticalMoveInput(input.y);
        }

        private void OnDisable()
        {
            PlayerKeyboardControls.AdjustablePadActive = false;
            if (moveAction != null)
            {
                moveAction.Disable();
            }

            if (jumpAction != null)
            {
                jumpAction.started -= OnJumpStarted;
                jumpAction.canceled -= OnJumpCanceled;
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

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            // started와 같이 설정 창이 열려 있으면 무시한다.
            if (!context.canceled || ControlPreferences.IsSettingsOpen)
            {
                return;
            }

            movement.ReleaseJump();
        }
    }
}
