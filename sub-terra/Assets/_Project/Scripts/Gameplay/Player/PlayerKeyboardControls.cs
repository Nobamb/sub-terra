using SubTerra.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.Gameplay.Player
{
    public static class PlayerKeyboardControls
    {
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
            var arrows = ReadDirections(keyboard, true);
            return scheme == ControlScheme.WasdMove ? wasd
                : scheme == ControlScheme.ArrowsMove ? arrows
                : Vector2.ClampMagnitude(wasd + arrows, 1f);
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
            var direction = ReadDirections(keyboard, scheme == ControlScheme.WasdMove);
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

        private static Vector2 ReadDirections(Keyboard keyboard, bool arrows)
        {
            bool up = (arrows ? keyboard.upArrowKey : keyboard.wKey).isPressed;
            bool down = (arrows ? keyboard.downArrowKey : keyboard.sKey).isPressed;
            bool left = (arrows ? keyboard.leftArrowKey : keyboard.aKey).isPressed;
            bool right = (arrows ? keyboard.rightArrowKey : keyboard.dKey).isPressed;
            return new Vector2((right ? 1f : 0f) - (left ? 1f : 0f),
                (up ? 1f : 0f) - (down ? 1f : 0f)).normalized;
        }
    }
}
