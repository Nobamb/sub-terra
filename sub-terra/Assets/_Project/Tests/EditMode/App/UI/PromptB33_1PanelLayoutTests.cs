using System.Linq;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Inventory;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 33-1: 인벤토리 토글, 단축키 라벨, 중복 패널 제거.</summary>
    public sealed class PromptB33_1PanelLayoutTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        public void BuildLayout()
        {
            PromptB33_1LayoutBuilder.Build();
        }

        [Test]
        public void IntegrationScene_HasSingleHiddenInventoryPanel()
        {
            var scene = OpenIntegration();
            var panels = Object.FindObjectsByType<InventoryPanelView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(v => v.gameObject.scene == scene)
                .ToArray();
            Assert.That(panels.Length, Is.EqualTo(1), "중복 InventoryPanel이 있으면 안 된다.");

            var panel = panels[0];
            Assert.That(panel.gameObject.activeSelf, Is.True, "루트는 Binder 유지를 위해 활성.");
            if (panel.PanelRoot != null)
            {
                Assert.That(panel.PanelRoot.activeSelf, Is.False, "시작 시 PanelRoot는 숨김.");
            }
        }

        [Test]
        public void ShortcutBar_UsesInventoryLabelAndChromeToggle()
        {
            var scene = OpenIntegration();
            var bar = FindTransform(scene, "PanelShortcutBar");
            var chrome = Find<Canvas>(scene, "HUDCanvas")
                ?.GetComponent<HudPanelChromeController>();
            Assert.That(bar, Is.Not.Null);
            Assert.That(chrome, Is.Not.Null);

            var inventoryBtn = bar.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b =>
                {
                    var t = b.GetComponentInChildren<TMP_Text>(true);
                    return t != null && t.text.Contains("인벤토리");
                });
            Assert.That(inventoryBtn, Is.Not.Null);

            var label = inventoryBtn.GetComponentInChildren<TMP_Text>(true);
            Assert.That(label.text, Does.Contain("인벤토리"));
            Assert.That(label.text, Does.Not.Contain("화물"));

            var wired = false;
            for (var i = 0; i < inventoryBtn.onClick.GetPersistentEventCount(); i++)
            {
                if (inventoryBtn.onClick.GetPersistentTarget(i) == chrome
                    && inventoryBtn.onClick.GetPersistentMethodName(i)
                        == nameof(HudPanelChromeController.ToggleInventoryPanel))
                {
                    wired = true;
                    break;
                }
            }

            Assert.That(wired, Is.True);
        }

        [Test]
        public void Chrome_ToggleInventory_HidesAndShowsPanelRoot()
        {
            var scene = OpenIntegration();
            var chrome = Find<Canvas>(scene, "HUDCanvas")
                ?.GetComponent<HudPanelChromeController>();
            var panel = Object.FindObjectsByType<InventoryPanelView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(v => v.gameObject.scene == scene);
            Assert.That(chrome, Is.Not.Null);
            Assert.That(panel, Is.Not.Null);

            chrome.CloseInventoryPanel();
            Assert.That(chrome.IsInventoryPanelOpen, Is.False);
            if (panel.PanelRoot != null)
            {
                Assert.That(panel.PanelRoot.activeSelf, Is.False);
            }

            chrome.OpenInventoryPanel();
            Assert.That(chrome.IsInventoryPanelOpen, Is.True);
            if (panel.PanelRoot != null)
            {
                Assert.That(panel.PanelRoot.activeSelf, Is.True);
            }

            chrome.CloseInventoryPanel();
            Assert.That(chrome.IsInventoryPanelOpen, Is.False);
            if (panel.PanelRoot != null)
            {
                Assert.That(panel.PanelRoot.activeSelf, Is.False);
            }
        }

        private static Scene OpenIntegration()
        {
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static Transform FindTransform(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == objectName)
                    {
                        return t;
                    }
                }
            }

            return null;
        }

        private static T Find<T>(Scene scene, string objectName)
            where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<T>(true))
                {
                    if (component.name == objectName)
                    {
                        return component;
                    }
                }
            }

            return null;
        }
    }
}
