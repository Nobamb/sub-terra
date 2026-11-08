using System.Linq;
using NUnit.Framework;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.Outpost;
using SubTerra.Shared;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    /// <summary>B-189 드론 말풍선 등장 펼침 + 대사 변경 타이핑.</summary>
    public sealed class PromptB189DroneBubbleMotionTests
    {
        private const string ViewSocketPath = "Assets/_Project/Prefabs/UI/ViewSocket.prefab";
        private const string LongLine =
            "깊은 층의 균열이 넓어지고 있습니다. 지지대를 먼저 세우고, 가스 농도가 낮아질 때까지 "
            + "채굴을 잠시 멈추는 편이 안전합니다. 전력이 부족하면 조명부터 끄고 귀환 경로를 확보하세요.";

        private GameObject instance;
        private DroneDialogueSocket socket;
        private bool previousReduceMotion;

        [SetUp]
        public void SetUp()
        {
            previousReduceMotion = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
            DestroyOverlays();
        }

        [TearDown]
        public void TearDown()
        {
            AccessibilityPreferences.ReduceMotion = previousReduceMotion;
            if (instance != null)
            {
                Object.DestroyImmediate(instance);
            }

            DestroyOverlays();
        }

        private static void DestroyOverlays()
        {
            // 에디트 모드에서는 OnDestroy가 오지 않을 수 있어 남은 오버레이를 직접 지운다.
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (canvas != null && canvas.name == "DroneDialogueOverlayCanvas")
                {
                    Object.DestroyImmediate(canvas.gameObject);
                }
            }
        }

        [Test]
        public void Constants_MatchSpecRangesAndOutpostTyping()
        {
            Assert.That(DroneBubbleMotion.AppearDuration, Is.InRange(0.18f, 0.25f));
            Assert.That(DroneBubbleMotion.AppearRisePixels, Is.InRange(6f, 10f));
            Assert.That(DroneBubbleMotion.AppearOvershoot, Is.EqualTo(1.05f));
            Assert.That(DroneBubbleMotion.TypingLineDuration, Is.EqualTo(CoreCctvTimeline.TerminalLineTyping));
            Assert.That(DroneBubbleMotion.TypingLineInterval, Is.EqualTo(CoreCctvTimeline.TerminalLineInterval));
        }

        [Test]
        public void WidthScale_StartsAtZero_OvershootsThenSettlesAtOne()
        {
            Assert.That(DroneBubbleMotion.WidthScale(0f), Is.EqualTo(0f));
            var peak = 0f;
            var previous = 0f;
            var rising = true;
            for (var t = 0.005f; t < DroneBubbleMotion.AppearDuration; t += 0.005f)
            {
                var w = DroneBubbleMotion.WidthScale(t);
                if (rising && w < previous)
                {
                    rising = false;
                }

                if (rising)
                {
                    Assert.That(w, Is.GreaterThanOrEqualTo(previous), "넘침 전까지는 폭이 줄지 않는다");
                }

                peak = Mathf.Max(peak, w);
                previous = w;
            }

            Assert.That(peak, Is.GreaterThan(1f).And.LessThanOrEqualTo(DroneBubbleMotion.AppearOvershoot + 0.0001f));
            Assert.That(DroneBubbleMotion.WidthScale(DroneBubbleMotion.AppearDuration), Is.EqualTo(1f));
        }

        [Test]
        public void RiseOffset_StartsBelowAndEndsAtZero()
        {
            Assert.That(DroneBubbleMotion.RiseOffset(0f), Is.EqualTo(-DroneBubbleMotion.AppearRisePixels));
            Assert.That(DroneBubbleMotion.RiseOffset(DroneBubbleMotion.AppearDuration), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void VisibleCharacters_TypesLineByLineLikeOutpostTerminal()
        {
            var lines = new[] { 10, 5 };
            var interval = DroneBubbleMotion.TypingLineInterval;
            var duration = DroneBubbleMotion.TypingLineDuration;
            Assert.That(DroneBubbleMotion.VisibleCharacters(0f, lines), Is.EqualTo(0));
            Assert.That(DroneBubbleMotion.VisibleCharacters(duration * 0.5f, lines), Is.EqualTo(5));
            Assert.That(DroneBubbleMotion.VisibleCharacters(duration, lines), Is.EqualTo(10));
            Assert.That(DroneBubbleMotion.VisibleCharacters(interval + duration, lines), Is.EqualTo(15));
            Assert.That(DroneBubbleMotion.TypingDuration(2), Is.EqualTo(interval + duration).Within(0.0001f));
        }

        [Test]
        public void FirstAppear_UnfoldsBeforeTextThenTypes()
        {
            CreateSocket();
            Say("스캔 결과");
            var panel = Panel();
            var text = DialogueText();
            var baseWidth = panel.parent.GetComponent<RectTransform>().rect.width;

            var height = panel.sizeDelta.y;
            Assert.That(panel.sizeDelta.x, Is.EqualTo(-baseWidth).Within(0.01f), "폭 0에서 시작");
            Assert.That(panel.localScale.y * height, Is.EqualTo(DroneBubbleMotion.LineThickness).Within(0.01f), "가는 가로선으로 시작");
            var lineCenter = panel.anchoredPosition.y + panel.localScale.y * height * 0.5f;
            Assert.That(lineCenter, Is.LessThan(height * 0.5f), "가로선은 제자리 가운데보다 아래에서 시작");
            Assert.That(text.maxVisibleCharacters, Is.EqualTo(0), "펼치는 동안 글자는 숨긴다");
            Assert.That(Flash().enabled, Is.True, "가로선 단계는 섬광으로 밝다");

            Advance(DroneBubbleMotion.AppearDuration * 0.5f);
            Assert.That(text.maxVisibleCharacters, Is.EqualTo(0), "다 펼쳐지기 전엔 글자 없음");

            Advance(DroneBubbleMotion.AppearDuration);
            Assert.That(panel.sizeDelta.x, Is.EqualTo(0f).Within(0.01f), "100% 폭");
            Assert.That(panel.localScale.y, Is.EqualTo(1f), "위아래로 다 열림");
            Assert.That(panel.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f));

            Advance(2f);
            Assert.That(text.maxVisibleCharacters, Is.GreaterThanOrEqualTo(text.text.Length), "다 찍힘");
            Assert.That(Flash().enabled, Is.False, "섬광은 끝나면 꺼진다");
            Assert.That(Scanline().enabled, Is.False, "주사선은 끝나면 꺼진다");
        }

        [Test]
        public void TvOn_LineThenOpenFromCenter()
        {
            const float full = 132f;
            Assert.That(DroneBubbleMotion.HeightScale(0f, full) * full, Is.EqualTo(DroneBubbleMotion.LineThickness).Within(0.001f));
            Assert.That(DroneBubbleMotion.HeightScale(DroneBubbleMotion.LineDuration * 0.5f, full) * full,
                Is.EqualTo(DroneBubbleMotion.LineThickness).Within(0.001f), "가로선 단계엔 두께 유지");
            Assert.That(DroneBubbleMotion.WidthScale(DroneBubbleMotion.LineDuration),
                Is.EqualTo(DroneBubbleMotion.AppearOvershoot).Within(0.001f), "가로선이 다 뻗은 뒤 열린다");
            var mid = DroneBubbleMotion.HeightScale((DroneBubbleMotion.LineDuration + DroneBubbleMotion.AppearDuration) * 0.5f, full);
            Assert.That(mid, Is.GreaterThan(DroneBubbleMotion.LineThickness / full).And.LessThan(1f));
            Assert.That(DroneBubbleMotion.HeightScale(DroneBubbleMotion.AppearDuration, full), Is.EqualTo(1f));
        }

        [Test]
        public void TvOn_FlashFadesThenFlickersOnce_ScanSweepsAfterOpen()
        {
            Assert.That(DroneBubbleMotion.FlashAlpha(0f), Is.EqualTo(DroneBubbleMotion.FlashPeak));
            var afterFade = DroneBubbleMotion.LineDuration + DroneBubbleMotion.FlashFade;
            Assert.That(DroneBubbleMotion.FlashAlpha(afterFade + 0.005f), Is.EqualTo(0f).Within(0.001f), "섬광이 빠진다");
            Assert.That(DroneBubbleMotion.FlashAlpha(DroneBubbleMotion.FlickerAt), Is.EqualTo(DroneBubbleMotion.FlickerPeak).Within(0.001f), "한 번 깜빡임");
            Assert.That(DroneBubbleMotion.FlashAlpha(DroneBubbleMotion.TvEffectEnd), Is.EqualTo(0f));

            Assert.That(DroneBubbleMotion.ScanlineAlpha(DroneBubbleMotion.AppearDuration * 0.5f), Is.EqualTo(0f), "열리기 전엔 주사선 없음");
            var scanMid = DroneBubbleMotion.AppearDuration + DroneBubbleMotion.ScanDuration * 0.5f;
            Assert.That(DroneBubbleMotion.ScanlineAlpha(scanMid), Is.EqualTo(DroneBubbleMotion.ScanAlpha).Within(0.001f));
            Assert.That(DroneBubbleMotion.ScanPosition(scanMid), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(DroneBubbleMotion.ScanlineAlpha(DroneBubbleMotion.TvEffectEnd), Is.EqualTo(0f));
        }

        [Test]
        public void TvOn_EffectImagesDoNotBlockClicks()
        {
            CreateSocket();
            Say("스캔 결과");
            Assert.That(Flash().raycastTarget, Is.False);
            Assert.That(Scanline().raycastTarget, Is.False);
            Assert.That(Flash().transform.GetSiblingIndex(), Is.GreaterThan(DialogueText().transform.GetSiblingIndex()), "섬광은 화면 위에 덮인다");
        }

        [Test]
        public void ChangedLineWhileShowing_RetypesWithoutUnfolding()
        {
            CreateSocket();
            Say("스캔 결과");
            Advance(2f);
            var panel = Panel();
            var text = DialogueText();

            Say("새 광맥 감지");
            Assert.That(panel.sizeDelta.x, Is.EqualTo(0f).Within(0.01f), "펼침을 다시 하지 않는다");
            Assert.That(panel.localScale.y, Is.EqualTo(1f), "TV 켜짐을 다시 하지 않는다");
            Assert.That(Flash().enabled, Is.False);
            Assert.That(panel.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(text.maxVisibleCharacters, Is.EqualTo(0), "기존 글자를 지운다");

            Advance(DroneBubbleMotion.TypingLineDuration * 0.5f);
            Assert.That(text.maxVisibleCharacters, Is.GreaterThan(0).And.LessThan(text.text.Length), "한 글자씩 찍는다");

            Advance(2f);
            Assert.That(text.maxVisibleCharacters, Is.GreaterThanOrEqualTo(text.text.Length));
        }

        [Test]
        public void SameLine_DoesNotRestartTyping()
        {
            CreateSocket();
            Say("스캔 결과");
            Advance(2f);
            var text = DialogueText();
            var before = text.maxVisibleCharacters;

            Say("스캔 결과");
            Assert.That(text.maxVisibleCharacters, Is.EqualTo(before));
        }

        [Test]
        public void LongerLine_ResizesHeightSmoothly()
        {
            CreateSocket();
            Say("스캔 결과");
            Advance(2f);
            var panel = Panel();
            var from = panel.sizeDelta.y;

            Say(LongLine);
            Assert.That(panel.sizeDelta.y, Is.EqualTo(from).Within(0.01f), "한 프레임에 뚝 바뀌지 않는다");

            Advance(DroneBubbleMotion.ResizeDuration * 0.5f);
            var middle = panel.sizeDelta.y;
            Advance(2f);
            var to = panel.sizeDelta.y;
            Assert.That(to, Is.GreaterThan(from), "긴 대사에 맞게 커진다");
            Assert.That(middle, Is.GreaterThan(from).And.LessThan(to), "중간값을 지난다");
        }

        [Test]
        public void HideThenShow_UnfoldsAgain()
        {
            CreateSocket();
            Say("스캔 결과");
            Advance(2f);
            var panel = Panel();

            socket.SetVisible(false);
            socket.SetVisible(true);
            Assert.That(socket.IsShowing, Is.True);
            Assert.That(panel.sizeDelta.x, Is.LessThan(-1f), "다시 뜨면 펼침을 다시 한다");
        }

        [Test]
        public void ReduceMotion_ShowsImmediately()
        {
            AccessibilityPreferences.ReduceMotion = true;
            CreateSocket();
            Say("스캔 결과");
            var panel = Panel();
            var text = DialogueText();
            Assert.That(panel.sizeDelta.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(panel.localScale.y, Is.EqualTo(1f));
            Assert.That(panel.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(text.maxVisibleCharacters, Is.GreaterThanOrEqualTo(text.text.Length));
            Assert.That(Flash().enabled, Is.False, "모션 감소면 섬광 없음");
            Assert.That(Scanline().enabled, Is.False);
        }

        [Test]
        public void Bubble_StaysClickThrough()
        {
            CreateSocket();
            Say("스캔 결과");
            Advance(DroneBubbleMotion.AppearDuration * 0.5f);
            var group = Panel().GetComponentInParent<CanvasGroup>();
            Assert.That(group.interactable, Is.False);
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(socket.HasRequiredReferences(), Is.True);
        }

        private void CreateSocket()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ViewSocketPath);
            Assert.That(prefab, Is.Not.Null);
            instance = Object.Instantiate(prefab);
            socket = instance.GetComponent<DroneDialogueSocket>();
            socket.ConfigureSfxForTests(() => 0f, _ => float.PositiveInfinity, () => true);
            socket.SetVisible(true);
        }

        private void Say(string line)
        {
            socket.SetDialogue(new DroneDialogueResult("dialogue.b189", line, false, false, false));
        }

        private void Advance(float seconds)
        {
            const float step = 1f / 60f;
            for (var t = 0f; t < seconds; t += step)
            {
                socket.AdvancePresentation(step);
            }
        }

        private static RectTransform Overlay()
        {
            var overlay = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)
                .First(canvas => canvas.name == "DroneDialogueOverlayCanvas");
            return overlay.GetComponent<RectTransform>();
        }

        private static RectTransform Panel()
        {
            return Overlay().Find("WorldDialogueCanvas/VisualRoot") as RectTransform;
        }

        private static TMP_Text DialogueText()
        {
            return Panel().Find("DialogueText").GetComponent<TMP_Text>();
        }

        private static UnityEngine.UI.Image Flash()
        {
            return Panel().Find("PowerFlash").GetComponent<UnityEngine.UI.Image>();
        }

        private static UnityEngine.UI.Image Scanline()
        {
            return Panel().Find("PowerScanline").GetComponent<UnityEngine.UI.Image>();
        }
    }
}
