using System.Linq;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.HUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB105SideMenuTests
    {
        [Test]
        public void Scene_PreservesSixActionsAndUsesOnlySuppliedSprites()
        {
            var scene = EditorSceneManager.OpenScene(PromptB105SideMenuBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameplaySideMenuController>(true)).Single();
                var open = menu.transform.Find("OpenMenuRoot");
                var closed = menu.transform.Find("ClosedMenuRoot");
                Assert.That(open.gameObject.activeSelf, Is.True);
                Assert.That(closed.gameObject.activeSelf, Is.False);
                Assert.That(open.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length, Is.EqualTo(7));
                Assert.That(closed.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length, Is.EqualTo(1));
                var names = new[] { "Open0", "Open1", "Open2", "Open3", "SettingsShortcut", "QuitShortcut" };
                var methods = new[] { "ToggleBuildingMenu", "ToggleInventoryPanel", "ToggleUpgrade", "ToggleGameGuide", "OpenSettings", "RequestQuit" };
                var icons = new[] { "icon-facility", "icon-inventory", "icon-upgrade", "icon-guide", "icon-settings", "icon-quit" };
                var bar = open.Find("PanelShortcutBar");
                for (int i = 0; i < names.Length; i++)
                {
                    var button = bar.Find(names[i]).GetComponent<UnityEngine.UI.Button>();
                    Assert.That(button.onClick.GetPersistentEventCount(), Is.EqualTo(1), names[i]);
                    Assert.That(button.onClick.GetPersistentTarget(0), Is.Not.Null, names[i]);
                    Assert.That(button.onClick.GetPersistentMethodName(0), Is.EqualTo(methods[i]), names[i]);
                    Assert.That(((RectTransform)button.transform).sizeDelta, Is.EqualTo(new Vector2(270, 70)));
                    Assert.That(button.GetComponent<SideMenuButtonView>(), Is.Not.Null);
                    var icon = button.transform.Find("MenuIcon");
                    Assert.That(icon.GetComponent<SideMenuIcon>(), Is.Not.Null);
                    var iconImage = icon.GetComponent<UnityEngine.UI.Image>();
                    Assert.That(iconImage.sprite, Is.Not.Null, names[i]);
                    Assert.That(iconImage.sprite.name, Is.EqualTo(icons[i]));
                    Assert.That(AssetDatabase.GetAssetPath(iconImage.sprite),
                        Does.StartWith(PromptB105SideMenuBuilder.ArtFolder));
                    Assert.That(((RectTransform)icon).anchorMin.x, Is.EqualTo(0f));
                    Assert.That(((RectTransform)icon).anchoredPosition.x, Is.InRange(16f, 28f));
                    Assert.That(button.transform.Find("NormalImage").GetComponent<UnityEngine.UI.Image>().sprite.name,
                        Is.EqualTo("menu-button-active-off"));
                    Assert.That(GetSprite(button.GetComponent<SideMenuButtonView>(), "particleSprite"), Is.Not.Null);
                }
                var openToggle = open.Find("MenuToggleButton");
                var closedToggle = closed.Find("MenuToggleButton");
                Assert.That(openToggle.Find("NormalImage").GetComponent<UnityEngine.UI.Image>().sprite.name,
                    Is.EqualTo("menu-collapse-off"));
                Assert.That(openToggle.Find("HoverImage").GetComponent<UnityEngine.UI.Image>().sprite.name,
                    Is.EqualTo("menu-collapse-on"));
                Assert.That(closedToggle.Find("NormalImage").GetComponent<UnityEngine.UI.Image>().sprite.name,
                    Is.EqualTo("menu-close-off"));
                Assert.That(closedToggle.Find("HoverImage").GetComponent<UnityEngine.UI.Image>().sprite.name,
                    Is.EqualTo("menu-close-on"));
                Assert.That(((RectTransform)closedToggle).anchoredPosition,
                    Is.EqualTo(PromptB105SideMenuBuilder.ClosedTogglePosition));
                Assert.That(((RectTransform)closedToggle).anchoredPosition.y, Is.GreaterThan(-40f));
                foreach (var image in menu.GetComponentsInChildren<UnityEngine.UI.Image>(true).Where(i => i.sprite != null))
                    Assert.That(AssetDatabase.GetAssetPath(image.sprite), Does.StartWith(PromptB105SideMenuBuilder.ArtFolder));
                foreach (var panel in new[] { open, closed })
                {
                    var rect = (RectTransform)panel;
                    Assert.That(rect.anchorMin, Is.EqualTo(Vector2.one));
                    Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
                    Assert.That(rect.pivot, Is.EqualTo(Vector2.one));
                    Assert.That(rect.anchoredPosition.x, Is.Zero);
                }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        private static UnityEngine.Object GetSprite(SideMenuButtonView view, string field)
        {
            var serialized = new SerializedObject(view);
            return serialized.FindProperty(field).objectReferenceValue;
        }
    }
}
