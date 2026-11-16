using SubTerra.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.Gameplay.Player
{
    public static class PlayerKeyboardControls
    {
        /// <summary>
        /// 완충판 위에 서 있는 동안 true(PlayerController가 매 프레임 갱신).
        /// 이때 위/아래 화살표는 판 높이 조절 전용이라 이동·채굴 방향으로 읽히지 않는다.
        /// </summary>
        public static bool AdjustablePadActive { get; set; }

        public static Vector2 ReadMovement(ControlScheme scheme)
        {
            var combined = Vector2.zero;
            var devices = InputSystem.devices;
            for (var i = 0; i < devices.Count; i++)
            {
                if (devices[i] is Keyboard keyboard)
                    combined += ReadMovement(keyboard, scheme);
            }

            return Vector2.ClampMagnitude(combined, 1f);
        }

        public static Vector2 ReadMovement(Keyboard keyboard, ControlScheme scheme)
        {
            if (keyboard == null) return Vector2.zero;
            var wasd = ReadDirections(keyboard, false);
            // 판 위에서는 위/아래 화살표가 높이 조절로 가므로 이동 입력에서 뺀다. WASD의 W/S는 그대로다.
            var arrows = ReadDirections(keyboard, true, AdjustablePadActive);
            return scheme == ControlScheme.WasdMove ? wasd
                : scheme == ControlScheme.ArrowsMove ? arrows
                : Vector2.ClampMagnitude(wasd + arrows, 1f);
        }

        /// <summary>이번 프레임에 새로 눌린 위(+1)/아래(-1) 화살표. 둘 다 눌렸거나 아무것도 없으면 0.</summary>
        public static int ReadPadStep()
        {
            var step = 0;
            var devices = InputSystem.devices;
            for (var i = 0; i < devices.Count; i++)
            {
                if (devices[i] is Keyboard keyboard) step += ReadPadStep(keyboard);
            }

            return Mathf.Clamp(step, -1, 1);
        }

        public static int ReadPadStep(Keyboard keyboard)
        {
            if (keyboard == null) return 0;
            return ReadPadStep(keyboard.upArrowKey.wasPressedThisFrame, keyboard.downArrowKey.wasPressedThisFrame);
        }

        /// <summary>위만 새로 눌리면 +1, 아래만이면 -1, 동시거나 없으면 0(W/S는 읽지 않는다).</summary>
        public static int ReadPadStep(bool upPressedThisFrame, bool downPressedThisFrame)
        {
            return upPressedThisFrame == downPressedThisFrame ? 0 : upPressedThisFrame ? 1 : -1;
        }

        public static Vector2 ReadMiningDirection(ControlScheme scheme)
        {
            var devices = InputSystem.devices;
            for (var i = 0; i < devices.Count; i++)
            {
                if (devices[i] is not Keyboard keyboard) continue;
                var direction = ReadMiningDirection(keyboard, scheme);
                if (direction != Vector2.zero) return direction;
            }

            return Vector2.zero;
        }

        public static Vector2 ReadMiningDirection(Keyboard keyboard, ControlScheme scheme)
        {
            if (keyboard == null || scheme == ControlScheme.Classic) return Vector2.zero;
            // WASD 스킴의 채굴 키는 화살표다. 판 위에서는 위/아래 화살표가 높이 조절이라 수평만 읽는다.
            var arrowsMine = scheme == ControlScheme.WasdMove;
            var direction = ReadDirections(keyboard, arrowsMine, arrowsMine && AdjustablePadActive);
            // 대각선 동시 입력은 수직 방향을 우선하여 항상 한 블록만 지정한다.
            return direction.y != 0f ? new Vector2(0f, Mathf.Sign(direction.y))
                : new Vector2(direction.x == 0f ? 0f : Mathf.Sign(direction.x), 0f);
        }

        /// <summary>
        /// WASD 또는 방향키가 눌렸는지. Move InputAction 합성에서 채굴 키가 이동으로 새는 것을 막을 때 쓴다.
        /// </summary>
        public static bool IsCompositeMoveKeyPressed()
        {
            var devices = InputSystem.devices;
            for (var i = 0; i < devices.Count; i++)
            {
                if (devices[i] is Keyboard keyboard && IsCompositeMoveKeyPressed(keyboard))
                    return true;
            }

            return false;
        }

        public static bool IsCompositeMoveKeyPressed(Keyboard keyboard)
        {
            if (keyboard == null) return false;
            return keyboard.wKey.isPressed || keyboard.aKey.isPressed
                || keyboard.sKey.isPressed || keyboard.dKey.isPressed
                || keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed
                || keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed;
        }

        private static Vector2 ReadDirections(Keyboard keyboard, bool arrows, bool ignoreVertical = false)
        {
            bool up = !ignoreVertical && (arrows ? keyboard.upArrowKey : keyboard.wKey).isPressed;
            bool down = !ignoreVertical && (arrows ? keyboard.downArrowKey : keyboard.sKey).isPressed;
            bool left = (arrows ? keyboard.leftArrowKey : keyboard.aKey).isPressed;
            bool right = (arrows ? keyboard.rightArrowKey : keyboard.dKey).isPressed;
            return new Vector2((right ? 1f : 0f) - (left ? 1f : 0f),
                (up ? 1f : 0f) - (down ? 1f : 0f)).normalized;
        }
    }
}
