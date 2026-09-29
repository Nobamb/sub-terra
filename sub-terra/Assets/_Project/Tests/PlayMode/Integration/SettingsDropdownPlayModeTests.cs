using System.Collections;
using NUnit.Framework;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class SettingsDropdownPlayModeTests
    {
        [UnityTest]
        public IEnumerator MainMenuDropdownsOpenSelectSwitchAndClose()
        {
            yield return VerifyDropdowns("Assets/_Project/Prefabs/UI/MainMenuPanel.prefab");
        }

        [UnityTest]
        public IEnumerator SurfaceBaseDropdownsOpenSelectSwitchAndClose()
        {
            yield return VerifyDropdowns("Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab");
        }

        private static IEnumerator VerifyDropdowns(string path)
        {
#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null);
            var eventSystem = EventSystem.current == null
                ? new GameObject("SettingsTestEventSystem", typeof(EventSystem))
                : null;
            var instance = Object.Instantiate(prefab);
            try
            {
                var main = instance.GetComponent<MainMenuView>();
                var surface = instance.GetComponent<SurfaceBaseView>();
                if (main != null) main.SetSettingsVisible(true);
                else surface.SetSettingsVisible(true);
                yield return new WaitForSecondsRealtime(0.4f);

                var settings = instance.transform.Find("SettingsPanel");
                var card = settings.Find("SettingsCard");
                var resolution = card.Find("ResolutionDropdown").GetComponent<TMP_Dropdown>();
                var frame = card.Find("FrameRateDropdown").GetComponent<TMP_Dropdown>();
                var modalCanvas = settings.GetComponent<Canvas>();

                resolution.Show();
                yield return new WaitForSecondsRealtime(0.2f);
                var list = resolution.transform.Find("Dropdown List");
                Assert.That(list, Is.Not.Null);
                Assert.That(list.GetComponent<Canvas>().sortingOrder,
                    Is.GreaterThan(modalCanvas.sortingOrder));
                Assert.That(list.GetComponent<GraphicRaycaster>(), Is.Not.Null);
                Assert.That(list.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.01f));

                var options = list.GetComponentsInChildren<Toggle>();
                Assert.That(options.Length, Is.GreaterThanOrEqualTo(2));
                options[1].isOn = true;
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(resolution.value, Is.EqualTo(1));
                Assert.That(resolution.IsExpanded, Is.False);

                resolution.Show();
                yield return null;
                ExecuteEvents.Execute(frame.gameObject, new PointerEventData(EventSystem.current),
                    ExecuteEvents.pointerDownHandler);
                frame.Show();
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(resolution.IsExpanded, Is.False);
                Assert.That(frame.IsExpanded, Is.True);
                Assert.That(frame.transform.Find("Dropdown List").GetComponent<Canvas>().sortingOrder,
                    Is.GreaterThan(modalCanvas.sortingOrder));

                if (main != null) main.SetSettingsVisible(false);
                else surface.SetSettingsVisible(false);
                yield return new WaitForSecondsRealtime(0.6f);
                Assert.That(frame.IsExpanded, Is.False);
            }
            finally
            {
                Object.Destroy(instance);
                if (eventSystem != null) Object.Destroy(eventSystem);
            }
#else
            yield break;
#endif
        }
    }
}
