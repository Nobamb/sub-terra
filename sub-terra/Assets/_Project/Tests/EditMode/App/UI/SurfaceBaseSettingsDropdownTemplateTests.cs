using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    public sealed class SurfaceBaseSettingsDropdownTemplateTests
    {
        private const string SurfaceBasePrefabPath =
            "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        private const string MainMenuPrefabPath =
            "Assets/_Project/Prefabs/UI/MainMenuPanel.prefab";

        [TestCase(SurfaceBasePrefabPath)]
        [TestCase(MainMenuPrefabPath)]
        public void SettingsPanel_IsAFullScreenInputBlocker_WithAnOpaqueCentralCard(
            string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null);

            var settings = FindChild(prefab.transform, "SettingsPanel") as RectTransform;
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(settings.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(settings.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(settings.offsetMax, Is.EqualTo(Vector2.zero));
            Assert.That(settings.GetComponent<Image>().raycastTarget, Is.True);

            var card = settings.Find("SettingsCard") as RectTransform;
            Assert.That(card, Is.Not.Null);
            Assert.That(card.sizeDelta.x, Is.GreaterThanOrEqualTo(640f));
            Assert.That(card.sizeDelta.y, Is.GreaterThanOrEqualTo(780f));
            Assert.That(card.GetComponent<Image>().color.a, Is.EqualTo(1f).Within(0.001f));
        }

        [TestCase(SurfaceBasePrefabPath, "ResolutionDropdown", false)]
        [TestCase(SurfaceBasePrefabPath, "LanguageDropdown", false)]
        [TestCase(SurfaceBasePrefabPath, "FrameRateDropdown", true)]
        [TestCase(MainMenuPrefabPath, "ResolutionDropdown", false)]
        [TestCase(MainMenuPrefabPath, "LanguageDropdown", false)]
        [TestCase(MainMenuPrefabPath, "FrameRateDropdown", true)]
        public void DropdownTemplate_UsesExpectedDirectionAndOpaquePopup(
            string prefabPath,
            string dropdownName,
            bool opensUpward)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null);

            var settings = FindChild(prefab.transform, "SettingsPanel");
            Assert.That(settings, Is.Not.Null);
            var dropdown = FindChild(settings, dropdownName).GetComponent<TMP_Dropdown>();
            Assert.That(dropdown, Is.Not.Null);
            Assert.That(dropdown.template, Is.Not.Null);

            Assert.That(dropdown.template.anchorMin.y, Is.EqualTo(opensUpward ? 1f : 0f));
            Assert.That(dropdown.template.anchorMax.y, Is.EqualTo(opensUpward ? 1f : 0f));
            Assert.That(dropdown.template.pivot.y, Is.EqualTo(opensUpward ? 0f : 1f));
            Assert.That(dropdown.template.sizeDelta.y, Is.LessThanOrEqualTo(90f));
            Assert.That(dropdown.template.GetComponent<Image>().color.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(dropdown.template.GetComponent<ScrollRect>().vertical, Is.True);

            var viewport = dropdown.template.Find("Viewport") as RectTransform;
            var content = viewport.Find("Content") as RectTransform;
            var item = content.Find("Item") as RectTransform;
            Assert.That(content.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(content.anchorMax, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(content.pivot, Is.EqualTo(new Vector2(0.5f, 1f)));
            Assert.That(item.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(item.anchorMax, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(item.pivot, Is.EqualTo(new Vector2(0.5f, 1f)));
            Assert.That(item.sizeDelta.y, Is.EqualTo(28f).Within(0.01f));
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            Assert.Fail("Missing child: " + name);
            return null;
        }
    }
}
