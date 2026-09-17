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
                Assert.That(slider.transform.Find("SliderShadow"), Is.Null, "전체 트랙 그림자 글로우는 제거한다");
                var fillGlow = slider.fillRect.Find("FillGlow") as RectTransform;
                Assert.That(fillGlow, Is.Not.Null, "필 영역 글로우");
                Assert.That(fillGlow.anchorMin.x, Is.EqualTo(0f));
                Assert.That(fillGlow.anchorMax.x, Is.EqualTo(1f), "글로우 너비는 필을 따른다");
                Assert.That(fillGlow.sizeDelta.y, Is.GreaterThan(slider.fillRect.rect.height), "위아래로 필보다 넓게 빛난다");
                Assert.That(fillGlow.GetComponent<UnityEngine.UI.Image>().sprite.name.StartsWith("slider-fill-glow"), Is.True);
                Assert.That(fillGlow.GetComponent<UnityEngine.UI.Image>().color.a, Is.EqualTo(1f).Within(0.001f));
                slider.value = 0.25f;
                float narrow = slider.fillRect.rect.width;
                slider.value = 1f;
                Assert.That(slider.fillRect.rect.width, Is.GreaterThan(narrow), "필 너비가 값에 따라 변한다");
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
                Assert.That(card.Find("SettingsDefaults").GetComponent<UnityEngine.UI.Image>().sprite.name.StartsWith("button-active-off"), Is.True, "버튼 기본 off 스프라이트");
                Assert.That(card.Find("SettingsDefaults/HoverOverlay").GetComponent<UnityEngine.UI.Image>().sprite.name.StartsWith("button-active-on"), Is.True, "버튼 호버 on 스프라이트");
                Assert.That(card.Find("SettingsDefaults").GetComponent<MenuSpriteButtonSkin>(), Is.Not.Null, "설정 버튼 페이드 스킨");
                Assert.That(card.Find("SettingsCancel").GetComponent<MenuSpriteButtonSkin>(), Is.Not.Null);
                Assert.That(card.Find("SettingsApply").GetComponent<MenuSpriteButtonSkin>(), Is.Not.Null);
                Assert.That(card.Find("SettingsDefaults").GetComponent<UnityEngine.UI.Button>().transition,
                    Is.EqualTo(UnityEngine.UI.Selectable.Transition.None));
                Assert.That(card.Find("ChangeControls").GetComponent<UnityEngine.UI.Image>().sprite.name.StartsWith("button-wide-off"), Is.True, "키 조작 변경 와이드 off");
                Assert.That(card.Find("ChangeControls/HoverOverlay").GetComponent<UnityEngine.UI.Image>().sprite.name.StartsWith("button-wide-on"), Is.True, "키 조작 변경 와이드 on");
                Assert.That(((RectTransform)card.Find("ChangeControls")).sizeDelta.x,
                    Is.GreaterThan(((RectTransform)card.Find("SettingsDefaults")).sizeDelta.x), "키 조작 변경 버튼이 더 넓다");

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

                var toggle = card.Find("ReduceMotionGroup").GetComponentInChildren<UnityEngine.UI.Toggle>(true);
                var toggleTrack = toggle.targetGraphic as UnityEngine.UI.Image;
                Assert.That(toggleTrack, Is.Not.Null);
                Assert.That(toggleTrack.sprite.name.StartsWith("toggle-active-off"), Is.True, "토글 비활성 회색 스프라이트");
                Assert.That(toggleTrack.transform.Find("SwitchOn").GetComponent<UnityEngine.UI.Image>().sprite.name.StartsWith("toggle-active-on"), Is.True, "토글 활성 청록 스프라이트");
                Assert.That(toggleTrack.rectTransform.sizeDelta, Is.EqualTo(new Vector2(62, 30)), "토글 컨셉 에셋 크기");
                Assert.That(slider.GetComponent<UnityEngine.EventSystems.EventTrigger>(), Is.Not.Null, "슬라이더 파티클 트리거");
                Assert.That(toggle.GetComponent<UnityEngine.EventSystems.EventTrigger>(), Is.Not.Null, "토글 파티클 트리거");

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

        [Test]
        public void Prompt104_4_MainMenuActionButtonsUseOffOnSpritesAndKeyboardNav()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PromptB104SettingsMenuBuilder.MainPrefab);
            var instance = Object.Instantiate(prefab);
            try
            {
                var content = instance.transform.Find("MenuContent");
                Assert.That(content, Is.Not.Null);
                var names = new[] { "ContinueButton", "NewGameButton", "SettingsButton", "QuitButton" };
                var labels = new[] { "이어하기", "새 게임", "설정", "종료" };
                for (var i = 0; i < names.Length; i++)
                {
                    var button = content.Find(names[i]);
                    Assert.That(button, Is.Not.Null, names[i]);
                    Assert.That(button.GetComponent<UnityEngine.UI.Image>().sprite.name.StartsWith("button-active-off"),
                        Is.True, names[i] + " off");
                    var overlay = button.Find("HoverOverlay");
                    Assert.That(overlay, Is.Not.Null, names[i] + " overlay");
                    Assert.That(overlay.GetComponent<UnityEngine.UI.Image>().sprite.name.StartsWith("button-active-on"),
                        Is.True, names[i] + " on");
                    Assert.That(overlay.GetComponent<UnityEngine.UI.Image>().raycastTarget, Is.False);
                    var label = button.GetComponentInChildren<TMP_Text>(true);
                    Assert.That(label, Is.Not.Null, names[i] + " text");
                    Assert.That(label.text, Does.Contain(labels[i]).IgnoreCase, names[i] + " label");
                    Assert.That(button.GetComponent<MenuSpriteButtonSkin>(), Is.Not.Null, names[i] + " skin");
                    Assert.That(MenuSpriteButtonSkin.HighlightDurationSeconds, Is.EqualTo(0.2f),
                        "세이브 슬롯 선택과 같은 페이드 시간");
                    Assert.That(button.GetComponent<UnityEngine.UI.Button>().navigation.mode,
                        Is.EqualTo(UnityEngine.UI.Navigation.Mode.None));
                    Assert.That(button.GetComponent<UnityEngine.UI.Button>().transition,
                        Is.EqualTo(UnityEngine.UI.Selectable.Transition.None));
                }

                var view = instance.GetComponent<MainMenuView>();
                int continues = 0, news = 0, settings = 0, quits = 0;
                view.ContinueClicked += () => continues++;
                view.NewGameClicked += () => news++;
                view.SettingsClicked += () => settings++;
                view.QuitClicked += () => quits++;
                Invoke(view, "OnEnable");

                Assert.That(view.CurrentActionButtonIndex, Is.EqualTo(-1));
                view.NavigateActionButton(1);
                Assert.That(view.CurrentActionButtonIndex, Is.EqualTo(0));
                Assert.That(content.Find("ContinueButton").GetComponent<MenuSpriteButtonSkin>().IsHighlighted, Is.True);
                Assert.That(content.Find("NewGameButton").GetComponent<MenuSpriteButtonSkin>().IsHighlighted, Is.False);

                view.NavigateActionButton(1);
                Assert.That(view.CurrentActionButtonIndex, Is.EqualTo(1));
                view.ActivateFocusedActionButton();
                Assert.That(news, Is.EqualTo(1));
                Assert.That(continues, Is.EqualTo(0));

                view.NavigateActionButton(1);
                view.NavigateActionButton(1);
                Assert.That(view.CurrentActionButtonIndex, Is.EqualTo(3));
                view.NavigateActionButton(1);
                Assert.That(view.CurrentActionButtonIndex, Is.EqualTo(0), "오른쪽 끝에서 이어하기로 순환");
                view.NavigateActionButton(-1);
                Assert.That(view.CurrentActionButtonIndex, Is.EqualTo(3), "왼쪽 끝에서 종료로 순환");
                view.ActivateFocusedActionButton();
                Assert.That(quits, Is.EqualTo(1));

                Invoke(view, "OnDisable");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [TestCase(PromptB104SettingsMenuBuilder.MainPrefab)]
        [TestCase(PromptB104SettingsMenuBuilder.SurfacePrefab)]
        public void Prompt104_4_SettingsFooterNavAndFillGlow(string path)
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                var root = instance.transform.Find("SettingsPanel");
                var card = root.Find("SettingsCard");
                var skin = root.GetComponent<SettingsMenuSkin>();
                Invoke(skin, "OnEnable");

                Assert.That(skin.CurrentFooterButtonIndex, Is.EqualTo(-1));
                var defaults = card.Find("SettingsDefaults").GetComponent<MenuSpriteButtonSkin>();
                var cancel = card.Find("SettingsCancel").GetComponent<MenuSpriteButtonSkin>();
                var apply = card.Find("SettingsApply").GetComponent<MenuSpriteButtonSkin>();
                Assert.That(defaults.IsHighlighted, Is.False);
                Assert.That(cancel.IsHighlighted, Is.False);
                Assert.That(apply.IsHighlighted, Is.False);

                skin.NavigateFooterButton(1);
                Assert.That(skin.CurrentFooterButtonIndex, Is.EqualTo(0));
                Assert.That(defaults.IsHighlighted, Is.True);
                Assert.That(cancel.IsHighlighted, Is.False);
                Assert.That(apply.IsHighlighted, Is.False);

                skin.NavigateFooterButton(1);
                Assert.That(skin.CurrentFooterButtonIndex, Is.EqualTo(1));
                Assert.That(cancel.IsHighlighted, Is.True);
                Assert.That(defaults.IsHighlighted, Is.False);

                skin.NavigateFooterButton(1);
                Assert.That(skin.CurrentFooterButtonIndex, Is.EqualTo(2));
                skin.NavigateFooterButton(1);
                Assert.That(skin.CurrentFooterButtonIndex, Is.EqualTo(0), "오른쪽 끝에서 기본값으로 순환");
                skin.NavigateFooterButton(-1);
                Assert.That(skin.CurrentFooterButtonIndex, Is.EqualTo(2), "왼쪽 끝에서 적용으로 순환");

                int applied = 0;
                var main = instance.GetComponent<MainMenuView>();
                var surface = instance.GetComponent<SurfaceBaseView>();
                if (main != null) main.SettingsApplyClicked += () => applied++;
                else surface.SettingsApplyClicked += () => applied++;
                if (main != null) Invoke(main, "OnEnable");
                else Invoke(surface, "OnEnable");
                skin.ActivateFocusedFooterButton();
                Assert.That(applied, Is.EqualTo(1));

                var change = card.Find("ChangeControls");
                Assert.That(change.GetComponent<MenuSpriteButtonSkin>(), Is.Not.Null, "키 조작 변경도 호버 페이드");
                Assert.That(change.GetComponent<MenuSpriteButtonSkin>().IsHighlighted, Is.False,
                    "좌우 키는 하단 세 버튼만 활성화");

                Invoke(skin, "OnDisable");
                if (main != null) Invoke(main, "OnDisable");
                else Invoke(surface, "OnDisable");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void Invoke(Component component, string method) => component.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
    }
}
