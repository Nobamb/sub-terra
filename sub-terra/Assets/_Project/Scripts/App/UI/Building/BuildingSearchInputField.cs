using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SubTerra.App.UI.Building
{
    /// <summary>IME 조합은 TMP에 맡기고, 레거시 이벤트에 빠진 Input System 확정 문자만 보완한다.</summary>
    public sealed class BuildingSearchInputField : TMP_InputField
    {
        private readonly List<char> pendingCharacters = new List<char>();
        private Keyboard keyboard;

        public override void OnSelect(BaseEventData eventData)
        {
            UnsubscribeKeyboard();
            keyboard = Keyboard.current;
            if (keyboard != null) keyboard.onTextInput += OnTextInput;
            base.OnSelect(eventData);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            UnsubscribeKeyboard();
        }

        protected override void OnDisable()
        {
            UnsubscribeKeyboard();
            base.OnDisable();
        }

        private void OnTextInput(char character)
        {
            if (isFocused && !char.IsControl(character)) pendingCharacters.Add(character);
        }

        protected override void Append(char character)
        {
            // Event.PopEvent로 먼저 처리한 문자는 보완 큐에서 빼서 두 번 쓰지 않는다.
            pendingCharacters.Remove(character);
            base.Append(character);
        }

        public override void OnUpdateSelected(BaseEventData eventData)
        {
            base.OnUpdateSelected(eventData);
            if (!isFocused || pendingCharacters.Count == 0) return;

            for (var i = 0; i < pendingCharacters.Count; i++) base.Append(pendingCharacters[i]);
            pendingCharacters.Clear();
            ForceLabelUpdate();
            eventData.Use();
        }

        private void UnsubscribeKeyboard()
        {
            if (keyboard != null) keyboard.onTextInput -= OnTextInput;
            keyboard = null;
            pendingCharacters.Clear();
        }
    }
}
