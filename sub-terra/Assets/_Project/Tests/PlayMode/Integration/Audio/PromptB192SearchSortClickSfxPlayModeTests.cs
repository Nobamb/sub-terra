using System.Collections;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Inventory;
using SubTerra.Shared.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class PromptB192SearchSortClickSfxPlayModeTests
    {
        private UiTestEnvironment environment;
        private int clicks;

        private void Record(SfxId id, float _)
        {
            if (id == SfxId.UiClick) clicks++;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            if (environment != null) environment.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator InventoryAndBuilding_RealPointerFocusAndDropdownSelection()
        {
            environment = new UiTestEnvironment();
            yield return environment.LoadIntegration();
            var inventory = Object.FindAnyObjectByType<InventoryPanelView>(FindObjectsInactive.Include);
            var building = Object.FindAnyObjectByType<BuildingMenuView>(FindObjectsInactive.Include);
            var chrome = Object.FindAnyObjectByType<HudPanelChromeController>();
            SfxRequests.PlayRequested += Record;

            foreach (var isBuilding in new[] { false, true })
            {
                if (isBuilding)
                {
                    chrome.CloseInventoryPanel();
                    yield return new WaitForSecondsRealtime(0.6f);
                    building.SetVisible(true);
                    yield return new WaitForSecondsRealtime(0.6f);
                    building.ToggleExpanded();
                }
                else inventory.SetVisible(true);
                yield return new WaitForSecondsRealtime(0.7f);
                var input = isBuilding ? building.SearchField : inventory.SearchField;
                var dropdown = isBuilding ? building.SortDropdown : inventory.SortDropdown;

                clicks = 0;
                yield return Click(dropdown.gameObject);
                Assert.That(dropdown.IsExpanded, Is.True);
                Assert.That(clicks, Is.EqualTo(1), "드롭다운 펼침");
                var toggles = dropdown.transform.Find("Dropdown List").GetComponentsInChildren<Toggle>();
                var next = (dropdown.value + 1) % dropdown.options.Count;
                clicks = 0;
                yield return Click(toggles[next].gameObject);
                Assert.That(dropdown.value, Is.EqualTo(next));
                Assert.That(clicks, Is.EqualTo(1), "다른 항목 선택");
                yield return new WaitForSecondsRealtime(0.3f);

                yield return Click(dropdown.gameObject);
                toggles = dropdown.transform.Find("Dropdown List").GetComponentsInChildren<Toggle>();
                clicks = 0;
                yield return Click(toggles[dropdown.value].gameObject);
                Assert.That(clicks, Is.Zero, "같은 항목 선택");
                yield return new WaitForSecondsRealtime(0.3f);

                yield return Click(dropdown.gameObject);
                clicks = 0;
                ExecuteEvents.Execute(dropdown.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerDownHandler);
                dropdown.Hide();
                dropdown.SetValueWithoutNotify(0);
                Assert.That(clicks, Is.Zero, "열린 상태 누르기·닫기·코드 초기화");
                yield return new WaitForSecondsRealtime(0.3f);

                clicks = 0;
                input.Select();
                input.ActivateInputField();
                yield return null;
                yield return null;
                Assert.That(input.isFocused, Is.True);
                Assert.That(clicks, Is.Zero, "코드 포커스");
                yield return Click(input.gameObject);
                Assert.That(clicks, Is.Zero, "코드로 포커스된 검색창 재클릭");
                EventSystem.current.SetSelectedGameObject(null);
                yield return null;

                clicks = 0;
                yield return Click(input.gameObject);
                Assert.That(input.isFocused, Is.True);
                Assert.That(clicks, Is.EqualTo(1), "마우스 첫 검색 포커스");
                clicks = 0;
                yield return Click(input.gameObject);
                input.text = isBuilding ? "ㅅㄷ" : "구";
                yield return null;
                Assert.That(clicks, Is.Zero, "검색창 재클릭·문자 입력");
                yield return UiTestWait.Capture(Path.GetFullPath("Builds/B192/" + (isBuilding ? "building" : "inventory") + "-search.png"));
                input.SetTextWithoutNotify(string.Empty);
                EventSystem.current.SetSelectedGameObject(null);
                yield return null;
                Assert.That(clicks, Is.Zero, "검색 초기화");
            }
        }

        private IEnumerator Click(GameObject target)
        {
            yield return UiTestWait.FocusGameView();
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)target.transform;
            var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var mouse = environment.Mouse;
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
            yield return null;
            yield return null; // 누르는 동안 TMP LateUpdate가 실제로 포커스를 활성화하도록 한다.
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
            yield return null;
        }
    }
}
