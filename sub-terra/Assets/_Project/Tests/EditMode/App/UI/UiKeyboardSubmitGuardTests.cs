using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 35-3: Enter Submit이 단축키/시설 버튼에 남지 않도록 하는 가드 검증.</summary>
    public sealed class UiKeyboardSubmitGuardTests
    {
        [Test]
        public void ClearSelection_RemovesCurrentSelectedGameObject()
        {
            var eventSystemGo = new GameObject("EventSystem");
            var eventSystem = eventSystemGo.AddComponent<EventSystem>();
            var selected = new GameObject("SelectedButton");
            selected.AddComponent<RectTransform>();
            selected.AddComponent<Image>();
            selected.AddComponent<Button>();

            try
            {
                eventSystem.SetSelectedGameObject(selected);
                Assert.That(eventSystem.currentSelectedGameObject, Is.EqualTo(selected));

                SubTerra.App.UI.UiKeyboardSubmitGuard.ClearSelection();

                Assert.That(eventSystem.currentSelectedGameObject, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(selected);
                Object.DestroyImmediate(eventSystemGo);
            }
        }

        [Test]
        public void ConfigurePointerPreferredButton_DisablesNavigationAndClearsAfterClick()
        {
            var eventSystemGo = new GameObject("EventSystem");
            var eventSystem = eventSystemGo.AddComponent<EventSystem>();
            var buttonGo = new GameObject("GuideButton");
            buttonGo.AddComponent<RectTransform>();
            buttonGo.AddComponent<Image>();
            var button = buttonGo.AddComponent<Button>();
            var clicked = false;
            button.onClick.AddListener(() => clicked = true);

            try
            {
                SubTerra.App.UI.UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(button);

                Assert.That(button.navigation.mode, Is.EqualTo(Navigation.Mode.None));

                eventSystem.SetSelectedGameObject(buttonGo);
                Assert.That(eventSystem.currentSelectedGameObject, Is.EqualTo(buttonGo));

                // 클릭 시 업무 리스너와 함께 선택이 해제되어야 한다.
                button.onClick.Invoke();
                Assert.That(clicked, Is.True);
                Assert.That(eventSystem.currentSelectedGameObject, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(buttonGo);
                Object.DestroyImmediate(eventSystemGo);
            }
        }

        [Test]
        public void ConfigureButtonsUnder_AppliesToAllChildButtons()
        {
            var root = new GameObject("ShortcutBar");
            root.AddComponent<RectTransform>();
            var childA = new GameObject("A");
            childA.transform.SetParent(root.transform);
            childA.AddComponent<RectTransform>();
            childA.AddComponent<Image>();
            var buttonA = childA.AddComponent<Button>();
            var childB = new GameObject("B");
            childB.transform.SetParent(root.transform);
            childB.AddComponent<RectTransform>();
            childB.AddComponent<Image>();
            var buttonB = childB.AddComponent<Button>();

            try
            {
                // 기본 Navigation은 Automatic.
                Assert.That(buttonA.navigation.mode, Is.Not.EqualTo(Navigation.Mode.None));

                SubTerra.App.UI.UiKeyboardSubmitGuard.ConfigureButtonsUnder(root.transform);

                Assert.That(buttonA.navigation.mode, Is.EqualTo(Navigation.Mode.None));
                Assert.That(buttonB.navigation.mode, Is.EqualTo(Navigation.Mode.None));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
