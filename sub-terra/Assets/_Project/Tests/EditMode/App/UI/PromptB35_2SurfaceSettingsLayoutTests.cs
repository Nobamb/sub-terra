using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    /// <summary>
    /// prompt-B 35-2: Surface Base 창·설정 창 레이아웃이 main 기준과 일치하는지 검증.
    /// </summary>
    public sealed class PromptB35_2SurfaceSettingsLayoutTests
    {
        [OneTimeSetUp]
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
        public void SurfaceBaseSettingsPanel_MatchesMainHalfHeightLayout()
        {
            AssertSettingsPanelLayout(
                PromptB35_2LayoutBuilder.SurfaceBasePrefabPath,
                typeof(SurfaceBaseView));
        }

        [Test]
        public void MainMenuSettingsPanel_MatchesMainHalfHeightLayout()
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
        public void SettingsPanel_ChildrenUseProportionalAnchors()
        {
            AssertProportionalChild(
                PromptB35_2LayoutBuilder.SurfaceBasePrefabPath,
                "SettingsTitle",
                0.94f);
            AssertProportionalChild(
                PromptB35_2LayoutBuilder.MainMenuPrefabPath,
                "SettingsTitle",
                0.94f);
            AssertProportionalChild(
                PromptB35_2LayoutBuilder.SurfaceBasePrefabPath,
                "ResolutionDropdown",
                0.64f);
            AssertProportionalChild(
                PromptB35_2LayoutBuilder.MainMenuPrefabPath,
                "FrameRateDropdown",
                0.25f);
        }

        private static void AssertSettingsPanelLayout(string prefabPath, System.Type viewType)
        {
            var prefab = LoadPrefab(prefabPath);
            var settings = FindChild(prefab.transform, "SettingsPanel") as RectTransform;
            Assert.That(settings, Is.Not.Null, prefabPath + " SettingsPanel");

            Assert.That(settings.anchorMin.x, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(settings.anchorMax.x, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(
                settings.anchorMin.y,
                Is.EqualTo(PromptB35_2LayoutBuilder.SettingsAnchorMinY).Within(0.001f));
            Assert.That(
                settings.anchorMax.y,
                Is.EqualTo(PromptB35_2LayoutBuilder.SettingsAnchorMaxY).Within(0.001f));
            Assert.That(settings.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(
                settings.sizeDelta.x,
                Is.EqualTo(PromptB35_2LayoutBuilder.SettingsPanelWidth).Within(0.5f));
            // 세로는 앵커 50% 구간이므로 sizeDelta.y 는 0.
            Assert.That(settings.sizeDelta.y, Is.EqualTo(0f).Within(0.5f));

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

        private static void AssertProportionalChild(
            string prefabPath,
            string childName,
            float expectedAnchorY)
        {
            var prefab = LoadPrefab(prefabPath);
            var settings = FindChild(prefab.transform, "SettingsPanel");
            Assert.That(settings, Is.Not.Null);
            var child = settings.Find(childName) as RectTransform;
            Assert.That(child, Is.Not.Null, prefabPath + " " + childName);
            Assert.That(child.anchorMin.y, Is.EqualTo(expectedAnchorY).Within(0.001f));
            Assert.That(child.anchorMax.y, Is.EqualTo(expectedAnchorY).Within(0.001f));
            Assert.That(child.anchoredPosition, Is.EqualTo(Vector2.zero));
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
