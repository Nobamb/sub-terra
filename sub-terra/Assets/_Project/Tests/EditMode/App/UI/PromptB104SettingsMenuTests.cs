using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Shared;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB104SettingsMenuTests
    {
        [TestCase(PromptB104SettingsMenuBuilder.MainPrefab)]
        [TestCase(PromptB104SettingsMenuBuilder.SurfacePrefab)]
        public void Skin_PreservesExistingDraftControlsAndCancelEvents(string path)
        {
            bool hadControls = PlayerPrefs.HasKey(SettingsRuntimeApplier.PrefControls);
            int savedControls = PlayerPrefs.GetInt(SettingsRuntimeApplier.PrefControls, 0);
            var oldScheme = ControlPreferences.Scheme;
            bool oldBlocked = ControlPreferences.IsSettingsOpen;
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                var main = instance.GetComponent<MainMenuView>();
                var surface = instance.GetComponent<SurfaceBaseView>();
                var view = main != null ? (Component)main : surface;
                // EditMode에서는 MonoBehaviour 활성 콜백을 테스트가 직접 실행한다.
                Invoke(view, "OnEnable");
                var draft = SettingsValues.CreateDefaults();
                draft.MasterVolume = 0.37f;
                draft.ReduceMotion = true;
                var root = instance.transform.Find("SettingsPanel");
                var card = root.Find("SettingsCard");
                var skin = root.GetComponent<SettingsMenuSkin>();
                Invoke(skin, "OnEnable");
                Assert.That(root.GetComponent<SettingsMenuSkin>(), Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(card.GetComponent<UnityEngine.UI.Image>().sprite),
                    Is.EqualTo(PromptB104SettingsMenuBuilder.ArtPath));
                Assert.That(card.GetComponent<UnityEngine.UI.Image>().color.a, Is.EqualTo(0.90f).Within(0.001f), "배경 10% 투명도");

                int cancelled = 0;
                if (main != null)
                {
                    main.SettingsCancelClicked += () => cancelled++;
                    main.SetSettingsDraft(draft);
                    main.SetSettingsVisible(true);
                    Assert.That(main.ReadSettingsDraft(draft).MasterVolume, Is.EqualTo(0.37f).Within(0.001f));
                    Assert.That(main.ReadSettingsDraft(draft).ReduceMotion, Is.True);
                }
                else
                {
                    surface.SettingsCancelClicked += () => cancelled++;
                    surface.SetSettingsDraft(draft);
                    surface.SetSettingsVisible(true);
                    Assert.That(surface.ReadSettingsDraft(draft).MasterVolume, Is.EqualTo(0.37f).Within(0.001f));
                    Assert.That(surface.ReadSettingsDraft(draft).ReduceMotion, Is.True);
                }
                Assert.That(card.Find("MasterVolumeLabel").GetComponent<TMP_Text>().text, Is.EqualTo("37%"));
                var slider = card.GetComponentInChildren<UnityEngine.UI.Slider>();
                slider.value = 0.62f;
                Assert.That(card.Find("MasterVolumeLabel").GetComponent<TMP_Text>().text, Is.EqualTo("62%"));
                Assert.That(slider.fillRect.Find("FillGlow"), Is.Not.Null, "슬라이더 활성 영역 청록 글로우");
                Assert.That(slider.handleRect.GetComponent<UnityEngine.UI.Image>().sprite.name.StartsWith("slider-knob"), Is.True, "슬라이더 2px 흰색 테두리 청록 노브");
                Assert.That(slider.handleRect.GetComponent<UnityEngine.UI.Image>().preserveAspect, Is.True, "슬라이더 핸들 원형 비율 보존");

                foreach (var dropdown in card.GetComponentsInChildren<TMP_Dropdown>())
                {
                    Assert.That(dropdown.template, Is.Not.Null);
                    Assert.That(dropdown.options.Count, Is.GreaterThanOrEqualTo(2));
                }
                var frameDropdown = card.Find("FrameRateDropdown").GetComponent<TMP_Dropdown>();
                Assert.That(frameDropdown.template.pivot.y, Is.EqualTo(1f), "프레임 드롭다운 하향 전개");

                Assert.That(root.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                    .Count(b => b.name == "ChangeControls"), Is.EqualTo(1));
                card.Find("ChangeControls").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Assert.That(root.GetComponent<ControlSchemePanel>().IsOpen, Is.True);
                if (main != null) main.TryCloseControlSchemePanel(); else surface.TryCloseControlSchemePanel();

                var close = card.Find("SettingsClose").GetComponent<UnityEngine.UI.Button>();
                Assert.That(card.Find("SettingsClose").GetComponent<UnityEngine.UI.Image>().sprite.name.StartsWith("setting-close-normal"), Is.True, "X버튼 노멀 에셋");
                Assert.That(card.Find("SettingsClose/HoverOverlay").GetComponent<UnityEngine.UI.Image>().sprite.name.StartsWith("setting-close-hover"), Is.True, "X버튼 호버 에셋");
                Assert.That(card.Find("SettingsDefaults").GetComponent<UnityEngine.UI.Button>().colors.highlightedColor.a, Is.EqualTo(0.50f).Within(0.01f), "버튼 50% 반투명 청록 호버");
                Assert.That(card.Find("SettingsDefaults/Edge0"), Is.Not.Null, "버튼 상시 1px 청록 테두리");
                Assert.That(card.Find("SettingsDefaults/InnerGlow"), Is.Not.Null, "버튼 내부 가장자리 은은한 불빛");

                close.onClick.Invoke();
                root.gameObject.SetActive(false);
                Invoke(skin, "OnDisable");
                root.gameObject.SetActive(true);
                Invoke(skin, "OnEnable");
                close.onClick.Invoke();
                Assert.That(cancelled, Is.EqualTo(2), "X must reuse Cancel once per click across reopen.");
                Assert.That(card.Find("SettingsClose/EdgeTop"), Is.Null);
                Assert.That(card.Find("SettingsDefaults/EdgeTop"), Is.Null);
                Assert.That(card.Find("ResolutionDropdown/ChevronLeft"), Is.Null);
                Assert.That(card.Find("ResolutionDropdown/DropdownArrow"), Is.Not.Null);

                var toggle = card.GetComponentInChildren<UnityEngine.UI.Toggle>();
                var toggleTrack = toggle.targetGraphic as UnityEngine.UI.Image;
                Assert.That(toggleTrack, Is.Not.Null);
                Assert.That(toggleTrack.sprite.name.StartsWith("toggle-pill-track"), Is.True, "캡슐형 토글 트랙");
                Assert.That(toggleTrack.rectTransform.sizeDelta, Is.EqualTo(new Vector2(60, 30)), "토글 스위치 2:1 비율");

                var handle = card.GetComponentsInChildren<RectTransform>(true)
                    .First(t => t.name == "SwitchHandle");
                toggle.isOn = true;
                Invoke(skin, "LateUpdate");
                Assert.That(handle.anchoredPosition.x, Is.GreaterThan(0f));
                toggle.isOn = false;
                Invoke(skin, "LateUpdate");
                Assert.That(handle.anchoredPosition.x, Is.LessThan(0f));
                Invoke(skin, "OnDisable");
                Invoke(view, "OnDisable");
            }
            finally
            {
                Object.DestroyImmediate(instance);
                ControlPreferences.Scheme = oldScheme;
                ControlPreferences.IsSettingsOpen = oldBlocked;
                if (hadControls) PlayerPrefs.SetInt(SettingsRuntimeApplier.PrefControls, savedControls);
                else PlayerPrefs.DeleteKey(SettingsRuntimeApplier.PrefControls);
                PlayerPrefs.Save();
            }
        }

        private static void Invoke(Component component, string method) => component.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
    }
}
