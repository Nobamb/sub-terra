using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.Shared.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    /// <summary>B-166: 같은 클릭으로 onClick이 불린 버튼은 그 처리로 막혀도 실패음을 내지 않는다.</summary>
    public sealed class PromptB166SfxFixTests
    {
        private readonly List<Object> created = new List<Object>();
        private readonly List<SfxId> played = new List<SfxId>();

        [SetUp]
        public void SetUp()
        {
            played.Clear();
            SfxRequests.PlayRequested += Record;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }

            created.Clear();
        }

        private void Record(SfxId id, float pitch) => played.Add(id);

        private int Count(SfxId id)
        {
            var count = 0;
            foreach (var entry in played)
            {
                if (entry == id) count++;
            }

            return count;
        }

        private Button MakeButton()
        {
            var go = new GameObject("B166Button", typeof(RectTransform), typeof(Image), typeof(Button));
            created.Add(go);
            var button = go.GetComponent<Button>();
            UiSfx.Attach(button, SfxId.None);
            return button;
        }

        // EventSystem처럼 같은 오브젝트의 클릭 처리기를 붙은 순서대로 부른다(Button → UiSfxButton).
        private static void Click(Button button)
        {
            ExecuteEvents.Execute(button.gameObject,
                new PointerEventData(null) { button = PointerEventData.InputButton.Left },
                ExecuteEvents.pointerClickHandler);
        }

        [Test]
        public void ClickThatBlocksButtonInOnClick_NoFailSfx()
        {
            var button = MakeButton();
            var clicked = 0;
            // 이동 성공처럼 onClick 처리 중 창이 닫혀 버튼이 막힌다.
            button.onClick.AddListener(() =>
            {
                clicked++;
                button.interactable = false;
            });

            Click(button);

            Assert.That(clicked, Is.EqualTo(1));
            Assert.That(button.IsInteractable(), Is.False);
            Assert.That(Count(SfxId.UiCancelFail), Is.Zero);
        }

        [Test]
        public void ClickOnAlreadyDisabledButton_PlaysFailOnce()
        {
            var button = MakeButton();
            var clicked = 0;
            button.onClick.AddListener(() => clicked++);
            button.interactable = false;

            Click(button);

            Assert.That(clicked, Is.Zero);
            Assert.That(Count(SfxId.UiCancelFail), Is.EqualTo(1));
        }

        [Test]
        public void SameFrameMarkIsConsumed_NextDisabledClickStillFails()
        {
            var button = MakeButton();
            button.onClick.AddListener(() => button.interactable = false);

            Click(button);
            Click(button);

            Assert.That(Count(SfxId.UiCancelFail), Is.EqualTo(1), "onClick 표시는 한 번의 클릭에만 쓰인다");
        }
    }
}
