using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>
    /// prompt-B 35-2: Surface Base 창·설정 창 레이아웃이 main 기준과 일치하는지 검증.
    /// </summary>
    public sealed class PromptB35_2SurfaceSettingsLayoutTests
    {
        public void RebuildMainBaseline()
        {
            PromptB35_2LayoutBuilder.Build();
        }

        [Test]
        public void SurfaceBaseContent_MatchesMainSizePlusTenPercent()
        {
            var prefab = LoadPrefab(PromptB35_2LayoutBuilder.SurfaceBasePrefabPath);
            var content = prefab.transform.Find("SurfaceBaseContent") as RectTransform;
            Assert.That(content, Is.Not.Null);
            Assert.That(content.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(content.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(content.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(
                content.sizeDelta.x,
                Is.EqualTo(PromptB35_2LayoutBuilder.SurfaceBaseContentWidth).Within(0.5f));
            Assert.That(
                content.sizeDelta.y,
                Is.EqualTo(PromptB35_2LayoutBuilder.SurfaceBaseContentHeight).Within(0.5f));
        }

        [Test]
        public void SurfaceBaseSettingsPanel_UsesFullScreenBlockerAndCenteredCard()
        {
            AssertSettingsPanelLayout(
                PromptB35_2LayoutBuilder.SurfaceBasePrefabPath,
                typeof(SurfaceBaseView));
        }

        [Test]
        public void MainMenuSettingsPanel_UsesFullScreenBlockerAndCenteredCard()
        {
            AssertSettingsPanelLayout(
                PromptB35_2LayoutBuilder.MainMenuPrefabPath,
                typeof(MainMenuView));
        }

        [Test]
        public void SettingsPanel_HasResolutionAndFrameRateDropdowns()
        {
            AssertDropdownsPresent(PromptB35_2LayoutBuilder.SurfaceBasePrefabPath);
            AssertDropdownsPresent(PromptB35_2LayoutBuilder.MainMenuPrefabPath);
        }

        [Test]
        public void SettingsPanel_ChildrenUseCenteredCardRows()
        {
            AssertCenteredCardRow(
                PromptB35_2LayoutBuilder.SurfaceBasePrefabPath,
                "SettingsTitle",
                340f);
            AssertCenteredCardRow(
                PromptB35_2LayoutBuilder.MainMenuPrefabPath,
                "SettingsTitle",
                340f);
            AssertCenteredCardRow(
                PromptB35_2LayoutBuilder.SurfaceBasePrefabPath,
                "ResolutionDropdown",
                100f);
            AssertCenteredCardRow(
                PromptB35_2LayoutBuilder.MainMenuPrefabPath,
                "FrameRateDropdown",
                -168f);
        }

        private static void AssertSettingsPanelLayout(string prefabPath, System.Type viewType)
        {
            var prefab = LoadPrefab(prefabPath);
            var settings = FindChild(prefab.transform, "SettingsPanel") as RectTransform;
            Assert.That(settings, Is.Not.Null, prefabPath + " SettingsPanel");

            Assert.That(settings.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(settings.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(settings.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(settings.offsetMax, Is.EqualTo(Vector2.zero));

            var blocker = settings.GetComponent<Image>();
            Assert.That(blocker, Is.Not.Null);
            Assert.That(blocker.raycastTarget, Is.True);

            var card = settings.Find("SettingsCard") as RectTransform;
            Assert.That(card, Is.Not.Null, prefabPath + " SettingsCard");
            Assert.That(card.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(card.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(card.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(card.sizeDelta, Is.EqualTo(new Vector2(680f, 800f)));
            var view = prefab.GetComponent(viewType);
            Assert.That(view, Is.Not.Null, viewType.Name);

            var so = new SerializedObject(view);
            var resProp = so.FindProperty("resolutionDropdown");
            var frameProp = so.FindProperty("frameRateDropdown");
            Assert.That(resProp, Is.Not.Null);
            Assert.That(frameProp, Is.Not.Null);
            Assert.That(
                resProp.objectReferenceValue,
                Is.Not.Null,
                viewType.Name + " resolutionDropdown 배선");
            Assert.That(
                frameProp.objectReferenceValue,
                Is.Not.Null,
                viewType.Name + " frameRateDropdown 배선");
        }

        private static void AssertDropdownsPresent(string prefabPath)
        {
            var prefab = LoadPrefab(prefabPath);
            var settings = FindChild(prefab.transform, "SettingsPanel");
            Assert.That(settings, Is.Not.Null);

            var resolution = settings.Find("ResolutionDropdown");
            var frame = settings.Find("FrameRateDropdown");
            var language = settings.Find("LanguageDropdown");
            Assert.That(resolution, Is.Not.Null, prefabPath + " ResolutionDropdown");
            Assert.That(frame, Is.Not.Null, prefabPath + " FrameRateDropdown");
            Assert.That(language, Is.Not.Null, prefabPath + " LanguageDropdown");
            Assert.That(resolution.GetComponent<TMP_Dropdown>(), Is.Not.Null);
            Assert.That(frame.GetComponent<TMP_Dropdown>(), Is.Not.Null);
            Assert.That(language.GetComponent<TMP_Dropdown>(), Is.Not.Null);
        }

        private static void AssertCenteredCardRow(
            string prefabPath,
            string childName,
            float expectedY)
        {
            var prefab = LoadPrefab(prefabPath);
            var settings = FindChild(prefab.transform, "SettingsPanel");
            Assert.That(settings, Is.Not.Null);
            var child = settings.Find(childName) as RectTransform;
            Assert.That(child, Is.Not.Null, prefabPath + " " + childName);
            Assert.That(child.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(child.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(child.anchoredPosition.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(child.anchoredPosition.y, Is.EqualTo(expectedY).Within(0.001f));
        }

        private static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            return prefab;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }
    }
}
