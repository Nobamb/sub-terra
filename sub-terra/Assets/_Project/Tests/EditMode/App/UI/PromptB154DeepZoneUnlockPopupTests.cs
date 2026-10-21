using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Progression;
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.Tutorial;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>B-154 심층 구역 해금 창: 시작 브리핑 연출 재사용, 문구·깊이·폰트, 닫기 경로.</summary>
    public sealed class PromptB154DeepZoneUnlockPopupTests
    {
        private GameObject canvasRoot;
        private ProgressionPanelView view;

        [SetUp]
        public void SetUp()
        {
            canvasRoot = new GameObject("PromptB154Canvas", typeof(RectTransform), typeof(Canvas));
            var panel = new GameObject("ProgressionPanel", typeof(RectTransform));
            panel.transform.SetParent(canvasRoot.transform, false);
            var resultGo = new GameObject("ResultText", typeof(RectTransform));
            resultGo.transform.SetParent(panel.transform, false);
            var result = resultGo.AddComponent<TextMeshProUGUI>();
            view = panel.AddComponent<ProgressionPanelView>();
            SetField(view, "resultText", result);
        }

        [TearDown]
        public void TearDown()
        {
            if (canvasRoot != null)
            {
                Object.DestroyImmediate(canvasRoot);
            }
        }

        [Test]
        public void Text_FormatsRangeAndReservedLevelSlots()
        {
            Assert.That(DeepZoneUnlockPopupText.Header(), Is.EqualTo("심층 구역 해금"));
            Assert.That(DeepZoneUnlockPopupText.Header(1), Is.EqualTo("심층 구역 Lv.1 해금"));
            Assert.That(DeepZoneUnlockPopupText.Range(36, 40), Is.EqualTo("36~40M 진입 가능"));
            Assert.That(DeepZoneUnlockPopupText.Range(36, 40, 1), Is.EqualTo("심층 구역 Lv.1 · 36~40M"));
        }

        [Test]
        public void SkinDepth_MatchesMineLayerDistributionDeepBand()
        {
            var skin = LoadSkin();
            Assert.That(PromptB154DeepZoneUnlockPopupBuilder.TryReadDeepBand(out var min, out var max), Is.True);
            Assert.That(skin.minDepth, Is.EqualTo(min));
            Assert.That(skin.maxDepth, Is.EqualTo(max));

            view.ShowDeepZoneUnlockPopup();
            var body = Find("Message").GetComponent<TMP_Text>();
            Assert.That(body.text, Is.EqualTo(DeepZoneUnlockPopupText.Range(min, max)));
            Assert.That(Find("Title").GetComponent<TMP_Text>().text, Is.EqualTo(DeepZoneUnlockPopupText.Header()));
        }

        [Test]
        public void Skin_ReusesStartBriefingArtAndFont()
        {
            var skin = LoadSkin();
            Assert.That(skin.IsComplete, Is.True);
            Assert.That(AssetDatabase.GetAssetPath(skin.frame), Is.EqualTo(PromptB120StartBriefingBuilder.FramePath));
            Assert.That(AssetDatabase.GetAssetPath(skin.font), Is.EqualTo(PromptB120StartBriefingBuilder.BriefingFontPath));
            Assert.That(AssetDatabase.GetAssetPath(skin.buttonOff), Does.EndWith("button-active-off.png"));
            Assert.That(AssetDatabase.GetAssetPath(skin.buttonOn), Does.EndWith("button-active-on.png"));
            Assert.That(skin.font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static));
        }

        [Test]
        public void Font_HasEveryPopupCharacter()
        {
            var skin = LoadSkin();
            var samples = new[]
            {
                DeepZoneUnlockPopupText.Header(),
                DeepZoneUnlockPopupText.Header(1),
                DeepZoneUnlockPopupText.Header(3),
                DeepZoneUnlockPopupText.Range(skin.minDepth, skin.maxDepth),
                DeepZoneUnlockPopupText.Range(36, 40, 1),
                DeepZoneUnlockPopupText.Range(76, 100, 3),
                DeepZoneUnlockPopupText.CloseLabel,
                "0123456789"
            };
            foreach (var sample in samples)
            {
                Assert.That(skin.font.HasCharacters(sample, out System.Collections.Generic.List<char> missing), Is.True,
                    sample + " missing: " + new string(missing.ToArray()));
            }
        }

        [Test]
        public void Show_BuildsCompactBriefingWindowWithoutPause()
        {
            var skin = LoadSkin();
            view.ShowDeepZoneUnlockPopup();

            var root = Find(DeepZoneUnlockPopupMotion.RootName);
            Assert.That(root, Is.Not.Null);
            // 생성 시 ModalPanel+10이고, 열 때 PopupWindowSorting이 팝업 순서로 다시 올린다(기존 동작).
            Assert.That(root.GetComponent<Canvas>().overrideSorting, Is.True);
            Assert.That(root.GetComponent<Canvas>().sortingOrder, Is.GreaterThanOrEqualTo(UiLayerPriority.ModalPanel + 10));
            Assert.That(PopupWindowSorting.Top, Is.EqualTo(root.GetComponent<Canvas>()));
            var transition = root.GetComponent<StartBriefingPopupMotion>();
            Assert.That(transition, Is.Not.Null);
            Assert.That(GetField(transition, "pauseOnOpen"), Is.False);
            Assert.That(GetField(transition, "tearSlices"), Is.Null);
            Assert.That((Vector2)GetField(transition, "windowSize"), Is.EqualTo(skin.windowSize));
            // 시작 브리핑 1700x950보다 훨씬 작은 창.
            Assert.That(skin.windowSize.x, Is.LessThanOrEqualTo(600f));
            Assert.That(skin.windowSize.y, Is.LessThanOrEqualTo(320f));
            Assert.That(root.transform.Find("UnlockTransition/Frame"), Is.Not.Null);
            Assert.That(root.transform.Find("UnlockContent/CloseButton"), Is.Not.Null);
            Assert.That(root.transform.Find("UnlockGlitch"), Is.Not.Null);
            Assert.That(Find(DeepZoneUnlockPopupMotion.CloseButtonName).GetComponent<Button>(), Is.Not.Null);
        }

        [Test]
        public void UnlockEvent_ShowsOnlyWhenUnlockedNow()
        {
            LoadSkin();
            var binder = view.gameObject.AddComponent<ProgressionPanelBinder>();
            SetField(binder, "view", view);
            var handler = typeof(ProgressionPanelBinder).GetMethod(
                "OnDeepZoneAccessChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(handler, Is.Not.Null);

            handler.Invoke(binder, new object[] { new ZoneAccessResult(true, false, string.Empty) });
            Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.False);
            handler.Invoke(binder, new object[] { new ZoneAccessResult(false, false, string.Empty) });
            Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.False);
            handler.Invoke(binder, new object[] { new ZoneAccessResult(true, true, string.Empty) });
            Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.True);
        }

        [Test]
        public void CloseAnimation_StopsRaycastsThenDisablesRootAndRemovesSorting()
        {
            LoadSkin();
            view.ShowDeepZoneUnlockPopup();
            var root = Find(DeepZoneUnlockPopupMotion.RootName);
            var canvas = root.GetComponent<Canvas>();
            var transition = root.GetComponent<StartBriefingPopupMotion>();
            ForceShown(transition);
            Assert.That(PopupWindowSorting.Top, Is.EqualTo(canvas));

            Assert.That(view.TryHideDeepZoneUnlockPopup(), Is.True);

            Assert.That(transition.IsClosing, Is.True);
            Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.False);
            Assert.That(root.activeSelf, Is.True);
            Assert.That(root.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            Assert.That(view.TryHideDeepZoneUnlockPopup(), Is.False);

            Invoke(transition, "CompleteClose");

            Assert.That(root.activeSelf, Is.False);
            Assert.That(PopupWindowSorting.Top, Is.Not.EqualTo(canvas));
            Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.False);
        }

        [Test]
        public void ReopenWhileClosing_RestartsWithoutLeftoverLayers()
        {
            LoadSkin();
            view.ShowDeepZoneUnlockPopup();
            var root = Find(DeepZoneUnlockPopupMotion.RootName);
            var transition = root.GetComponent<StartBriefingPopupMotion>();
            var layerCount = root.GetComponentsInChildren<RectTransform>(true).Length;

            for (var i = 0; i < 3; i++)
            {
                ForceShown(transition);
                Assert.That(view.TryHideDeepZoneUnlockPopup(), Is.True);
                Assert.That(transition.IsClosing, Is.True);
                // 연출 재시작(OnDisable→OnEnable)은 PlayMode 테스트가 확인한다. 여기서는 열림 상태와 입력만 본다.
                view.ShowDeepZoneUnlockPopup();
                Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.True);
                Assert.That(root.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
                Assert.That(view.TryHideDeepZoneUnlockPopup(), Is.True);
                view.ShowDeepZoneUnlockPopup();
            }

            Assert.That(CountNamed(DeepZoneUnlockPopupMotion.RootName), Is.EqualTo(1));
            Assert.That(root.GetComponents<StartBriefingPopupMotion>().Length, Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<RectTransform>(true).Length, Is.EqualTo(layerCount));
        }

        [Test]
        public void QuestClear_ConfigureCompactKeepsPreviousWindowAndFrame()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PromptB120StartBriefingBuilder.FramePath);
            var panel = new GameObject("QuestClearRewardPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasRoot.transform, false);
            ((RectTransform)panel.transform).sizeDelta = new Vector2(900f, 450f);
            var source = panel.GetComponent<Image>();
            source.sprite = sprite;
            source.raycastTarget = true;
            var child = new GameObject("ClaimCheckSign", typeof(RectTransform));
            child.transform.SetParent(panel.transform, false);
            var motion = panel.AddComponent<StartBriefingPopupMotion>();

            Invoke(motion, "ConfigureQuestClear", (RectTransform)null);

            Assert.That((Vector2)GetField(motion, "windowSize"), Is.EqualTo(new Vector2(900f, 450f)));
            Assert.That((Vector2)GetField(motion, "frameHalfExtent"), Is.EqualTo(new Vector2(395f, 155f)));
            Assert.That(GetField(motion, "pauseOnOpen"), Is.False);
            Assert.That(GetField(motion, "introSpeed"), Is.EqualTo(1f));
            Assert.That(GetField(motion, "effectScale"), Is.EqualTo(1f));
            Assert.That(source.enabled, Is.False);
            var frame = panel.transform.Find("ClaimTransition/Frame").GetComponent<Image>();
            Assert.That(frame.sprite, Is.EqualTo(sprite));
            Assert.That(frame.raycastTarget, Is.True);
            Assert.That(frame.rectTransform.sizeDelta, Is.EqualTo(new Vector2(900f, 450f)));
            Assert.That(panel.transform.Find("ClaimContent/ClaimCheckSign"), Is.Not.Null);
            Assert.That(panel.transform.Find("ClaimGlitch"), Is.Not.Null);
            Assert.That(((Image[])GetField(motion, "edgeBars")).Length, Is.EqualTo(10));
            Assert.That(((Image[])GetField(motion, "signalMotes")).Length, Is.EqualTo(8));
            Assert.That(((Image[])GetField(motion, "screenBars")).Length, Is.EqualTo(8));
            Assert.That(GetField(motion, "tearSlices"), Is.Null);
        }

        private static DeepZoneUnlockPopupSkin LoadSkin()
        {
            var skin = DeepZoneUnlockPopupSkin.Load();
            Assert.That(skin, Is.Not.Null, "Run SubTerra/UI/Build Prompt-B 154 Deep Zone Unlock Popup first.");
            return skin;
        }

        // EditMode에서는 Update가 돌지 않으므로 상태 전이만 시작 브리핑과 같은 순서로 진행한다.
        private static void ForceShown(StartBriefingPopupMotion transition)
        {
            if (GetField(transition, "planner") == null)
            {
                Invoke(transition, "Awake");
            }

            var lifecycle = (BriefingLifecycle)GetField(transition, "lifecycle");
            lifecycle.Begin();
            lifecycle.TryStartIntro();
            lifecycle.TryFinishIntro();
            Assert.That(transition.IsShown, Is.True);
        }

        private GameObject Find(string name)
        {
            foreach (var t in canvasRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t.gameObject;
                }
            }

            return null;
        }

        private int CountNamed(string name)
        {
            var count = 0;
            foreach (var t in canvasRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) count++;
            }

            return count;
        }

        private static object GetField(object target, string name)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            return field.GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }

        private static void Invoke(object target, string name, params object[] args)
        {
            var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Missing method: " + name);
            method.Invoke(target, args);
        }
    }
}
