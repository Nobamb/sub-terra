using System.IO;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.UI.PlayerStatus;
using UnityEngine;

namespace SubTerra.App.Tests.PlayerStatus
{
    public sealed class PlayerHeadMessageStackTests
    {
        private const float Step = 0.05f;

        [Test]
        public void WarningFlash_DipsTwiceThenHoldsOpaque()
        {
            var dips = 0;
            var wasBright = true;
            for (var t = 0f; t < PlayerStatusTimeline.FlashDuration; t += 0.005f)
            {
                var bright = PlayerStatusTimeline.FlashAlpha(t) > 0.5f;
                if (wasBright && !bright)
                {
                    dips++;
                }

                wasBright = bright;
            }

            Assert.That(dips, Is.EqualTo(PlayerStatusTimeline.BlinkCount));
            Assert.That(PlayerStatusTimeline.BlinkCount, Is.InRange(2, 3));
            Assert.That(PlayerStatusTimeline.FlashAlpha(PlayerStatusTimeline.FlashDuration), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(PlayerStatusTimeline.FlashAlpha(PlayerStatusTimeline.FlashDuration + 5f), Is.EqualTo(1f));
        }

        [Test]
        public void Warning_StaysForHoldTimeThenLeaves()
        {
            var model = new PlayerHeadMessageStackModel();
            model.ShowWarning("mining.failure", "화물이 가득 찼습니다.");

            var elapsed = 0f;
            while (model.Entries.Any(e => e.Key == "mining.failure" && e.Wanted) && elapsed < 5f)
            {
                model.Advance(Step);
                elapsed += Step;
            }

            Assert.That(elapsed, Is.EqualTo(PlayerStatusTimeline.HoldSeconds).Within(Step * 1.5f));
            Assert.That(PlayerStatusTimeline.HoldSeconds, Is.EqualTo(2.5f));

            // 퇴장 연출이 끝나면 목록에서 빠지고 Finished에 한 번 실린다.
            var removed = 0;
            for (var i = 0; i < 20; i++)
            {
                model.Advance(Step);
                removed += model.Finished.Count;
            }

            Assert.That(removed, Is.EqualTo(1));
            Assert.That(model.Entries.Count, Is.EqualTo(0));
        }

        [Test]
        public void ArrivalOrder_EarlierItemSitsBelow()
        {
            var model = new PlayerHeadMessageStackModel();
            model.RegisterSlot("gold.popup", 44f);
            model.ShowInfo("mining.progress", "채굴 10%");
            model.ShowWarning("mining.failure", "채굴 범위를 벗어났습니다.");
            model.Advance(0f);

            Assert.That(model.Entries.Select(e => e.Key).ToArray(),
                Is.EqualTo(new[] { "gold.popup", "mining.progress", "mining.failure" }));
            Assert.That(model.OffsetOf("gold.popup"), Is.EqualTo(0f).Within(0.01f));
            Assert.That(model.OffsetOf("mining.progress"), Is.EqualTo(44f + PlayerStatusTimeline.StackGap).Within(0.01f));
            Assert.That(model.OffsetOf("mining.failure"),
                Is.EqualTo(44f + PlayerStatusTimeline.StackGap + 40f + PlayerStatusTimeline.StackGap).Within(0.01f));
        }

        [Test]
        public void LowerItemLeaves_UpperItemSlidesDownSmoothly()
        {
            var model = new PlayerHeadMessageStackModel();
            model.ShowInfo("a", "첫 번째");
            model.ShowInfo("b", "두 번째");
            model.Advance(0f);
            var before = model.OffsetOf("b");
            Assert.That(before, Is.EqualTo(46f).Within(0.01f));

            model.Dismiss("a");
            model.Advance(Step);
            var midway = model.OffsetOf("b");

            Assert.That(midway, Is.LessThan(before).And.GreaterThan(0f), "슬라이드 중에는 시작과 목표 사이 값이어야 한다");

            for (var i = 0; i < 10; i++)
            {
                model.Advance(Step);
            }

            Assert.That(model.OffsetOf("b"), Is.EqualTo(0f).Within(0.01f));
            Assert.That(model.Finished.Any(e => e.Key == "a") || model.Entries.All(e => e.Key != "a"), Is.True);
        }

        [Test]
        public void SameKeyAndText_DoesNotAddEntry_ButExtendsTimer()
        {
            var model = new PlayerHeadMessageStackModel();
            model.ShowWarning("mining.failure", "화물이 가득 찼습니다.");
            for (var i = 0; i < 40; i++)
            {
                model.Advance(Step);
            }

            model.ShowWarning("mining.failure", "화물이 가득 찼습니다.");

            Assert.That(model.Entries.Count, Is.EqualTo(1));
            for (var i = 0; i < 48; i++)
            {
                model.Advance(Step);
            }

            Assert.That(model.Entries.Single().Wanted, Is.True, "다시 받은 시점부터 표시 시간을 새로 잰다");
            // 표시 시간이 끝나면 퇴장을 시작한다(퇴장 연출이 끝나기 전이라 아직 목록에 남아 있다).
            for (var i = 0; i < 4; i++)
            {
                model.Advance(Step);
            }

            Assert.That(model.Entries.Single().Wanted, Is.False);
        }

        [Test]
        public void Progress_UpdatesSameEntryAndTurnsIntoResultInPlace()
        {
            var model = new PlayerHeadMessageStackModel();
            var first = model.SetProgress("mining.progress", "채굴 10%", 0.1f);
            for (var i = 0; i < 200; i++)
            {
                model.Advance(Step);
            }

            Assert.That(first.Wanted, Is.True, "진행 문구는 시간 제한 없이 떠 있다");

            var second = model.SetProgress("mining.progress", "채굴 55%", 0.55f);

            Assert.That(model.Entries.Count, Is.EqualTo(1));
            Assert.That(ReferenceEquals(first, second), Is.True);
            Assert.That(second.Text, Is.EqualTo("채굴 55%"));
            Assert.That(second.Progress, Is.EqualTo(0.55f).Within(0.0001f));

            var result = model.ShowInfo("mining.progress", "광석 +3 (+1 수확)");
            Assert.That(ReferenceEquals(first, result), Is.True, "결과 문구는 같은 자리에서 바뀐다");
            Assert.That(result.Style, Is.EqualTo(PlayerStatusStyle.Info));
            Assert.That(result.Text, Is.EqualTo("광석 +3 (+1 수확)"));

            for (var i = 0; i < 60; i++)
            {
                model.Advance(Step);
            }

            Assert.That(result.Wanted, Is.False, "결과 문구는 2.5초 뒤 사라진다");
        }

        [Test]
        public void StyleChange_OldEntryLeavesAndNewWarningStacksAbove()
        {
            var model = new PlayerHeadMessageStackModel();
            model.SetProgress("mining.progress", "채굴 40%", 0.4f);
            model.ShowWarning("mining.progress", "채굴할 수 없습니다.");

            Assert.That(model.Entries.Count(e => e.Wanted), Is.EqualTo(1));
            Assert.That(model.Entries.Single(e => e.Wanted).Style, Is.EqualTo(PlayerStatusStyle.Warning));
        }

        [Test]
        public void Cap_OverflowEvictsOldestMessageOnly()
        {
            var model = new PlayerHeadMessageStackModel();
            model.RegisterSlot("gold.popup", 44f);
            model.ShowInfo("m1", "하나");
            model.ShowInfo("m2", "둘");
            model.ShowInfo("m3", "셋");
            model.ShowInfo("m4", "넷");

            var wanted = model.Entries.Where(e => e.Wanted).Select(e => e.Key).ToArray();
            Assert.That(wanted.Length, Is.EqualTo(PlayerStatusTimeline.MaxEntries));
            Assert.That(wanted, Does.Contain("gold.popup"), "골드 슬롯은 밀려나지 않는다");
            Assert.That(wanted, Does.Not.Contain("m1"), "가장 오래된 문구부터 퇴장");
            Assert.That(model.Entries.Single(e => e.Key == "m1").Wanted, Is.False);
        }

        [Test]
        public void Slot_HeightChangeMovesItemsAboveSmoothly()
        {
            var model = new PlayerHeadMessageStackModel();
            model.RegisterSlot("gold.popup", 44f);
            model.ShowInfo("m1", "하나");
            model.Advance(0f);
            Assert.That(model.OffsetOf("m1"), Is.EqualTo(50f).Within(0.01f));

            model.RegisterSlot("gold.popup", 66f);
            model.Advance(Step);
            var midway = model.OffsetOf("m1");

            Assert.That(midway, Is.GreaterThan(50f).And.LessThan(72f));
            for (var i = 0; i < 10; i++)
            {
                model.Advance(Step);
            }

            Assert.That(model.OffsetOf("m1"), Is.EqualTo(72f).Within(0.01f));
        }

        [Test]
        public void UnregisterSlot_RemovesGoldSlotImmediately()
        {
            var model = new PlayerHeadMessageStackModel();
            model.RegisterSlot("gold.popup", 44f);
            model.UnregisterSlot("gold.popup");

            Assert.That(model.Entries.Count, Is.EqualTo(0));
        }

        [Test]
        public void TwoLineLabel_UsesTallerHeight()
        {
            Assert.That(SubTerra.App.UI.FacilityNameTag.FacilityNameTagVisual.HeightFor("한 줄"),
                Is.EqualTo(SubTerra.App.UI.FacilityNameTag.FacilityNameTagVisual.Height));
            Assert.That(SubTerra.App.UI.FacilityNameTag.FacilityNameTagVisual.HeightFor("첫 줄\n둘째 줄"),
                Is.EqualTo(SubTerra.App.UI.FacilityNameTag.FacilityNameTagVisual.TwoLineHeight));
        }

        [Test]
        public void SlideOffset_InterpolatesBetweenEndpoints()
        {
            Assert.That(PlayerStatusTimeline.SlideOffset(0f, 10f, 0f), Is.EqualTo(0f));
            Assert.That(PlayerStatusTimeline.SlideOffset(0f, 10f, PlayerStatusTimeline.SlideSeconds), Is.EqualTo(10f));
            var mid = PlayerStatusTimeline.SlideOffset(0f, 10f, PlayerStatusTimeline.SlideSeconds * 0.5f);
            Assert.That(mid, Is.InRange(0.01f, 9.99f));
        }

        [Test]
        public void MiningHud_RoutesProgressResultAndFailureToExpectedStyles()
        {
            var source = File.ReadAllText(Path.Combine(
                Application.dataPath, "_Project", "Scripts", "App", "Integration", "MiningProgressHud.cs"));

            Assert.That(source, Does.Contain("messages.SetProgress("));
            Assert.That(source, Does.Contain("messages.ShowInfo(ProgressKey, pendingYieldText)"));
            Assert.That(source, Does.Contain("messages.ShowWarning(FailureKey, failureMessage)"));
            Assert.That(source, Does.Not.Contain("ShowTemporaryMessage"));
        }

        [Test]
        public void MiningHud_NonMiningTargetsWhilePlacing_DoNotClaimPlacementIsInvalid()
        {
            var notMineable = SubTerra.Gameplay.Mining.MiningFailureReason.NotMineable;

            Assert.That(
                SubTerra.App.Integration.MiningProgressHud.FailureMessage(notMineable, true),
                Is.Empty);
            Assert.That(
                SubTerra.App.Integration.MiningProgressHud.FailureMessage(notMineable, false),
                Is.EqualTo("채굴할 수 없는 지형입니다."));
            // 빈 칸의 채굴 실패도 설치 불가의 근거가 되지 않는다.
            Assert.That(
                SubTerra.App.Integration.MiningProgressHud.FailureMessage(
                    SubTerra.Gameplay.Mining.MiningFailureReason.InvalidTarget, true),
                Is.Empty);
            // 다른 채굴 실패는 설치 준비 중에도 기존 문구 그대로.
            Assert.That(
                SubTerra.App.Integration.MiningProgressHud.FailureMessage(
                    SubTerra.Gameplay.Mining.MiningFailureReason.InventoryFull, true),
                Is.EqualTo("화물이 가득 찼습니다. 귀환해 화물을 비우세요."));
        }

        [Test]
        public void OutpostPresenter_SendsPowerAndCooldownToWarningSink()
        {
            var source = File.ReadAllText(Path.Combine(
                Application.dataPath, "_Project", "Scripts", "App", "UI", "Outpost", "OutpostPanelPresenter.cs"));

            Assert.That(source, Does.Contain("ShowFacilityWarning(message)"));
            Assert.That(source, Does.Contain("ShowFacilityWarning(cooldownMessage)"));
            Assert.That(source, Does.Not.Contain("ShowTemporaryMessage(message, 3f)"));
            Assert.That(source, Does.Not.Contain("ShowTemporaryMessage(cooldownMessage, 3f)"));
        }
    }
}
