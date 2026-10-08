using NUnit.Framework;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.UI.Outpost;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.Tests.UI
{
    /// <summary>B-138 보관함 Showbox 시간표·상자 기하·수량 미리보기의 순수 계산 검증.</summary>
    public sealed class PromptB138StoragePopupTests
    {
        private const float Step = 1f / 120f;

        // ---------- 시간표: 등장 ----------

        [Test]
        public void Durations_MatchTargets()
        {
            Assert.That(StorageBoxTimeline.OpenDuration, Is.InRange(1.0f, 1.1f));
            Assert.That(StorageBoxTimeline.CloseDuration, Is.InRange(0.4f, 0.5f));
            Assert.That(StorageBoxTimeline.FooterReveal + StorageBoxTimeline.RevealDuration,
                Is.LessThanOrEqualTo(StorageBoxTimeline.OpenDuration), "내용 공개는 등장 시간 안에 끝난다");
        }

        [Test]
        public void Open_PhasesRunInOrder_BoxRiseLidPopsPanel()
        {
            Assert.That(StorageBoxTimeline.BoxRiseEnd, Is.LessThanOrEqualTo(StorageBoxTimeline.LidOpenStart), "상자 도착 뒤 개봉");
            Assert.That(StorageBoxTimeline.LidOpenStart, Is.LessThan(StorageBoxTimeline.PopStart), "개봉 뒤 광물 팝");
            Assert.That(StorageBoxTimeline.PopStart, Is.LessThan(StorageBoxTimeline.PanelStart), "광물 팝 뒤 패널");
            Assert.That(StorageBoxTimeline.PanelStart, Is.LessThan(StorageBoxTimeline.FooterReveal));

            Assert.That(StorageBoxTimeline.BoxY(StorageBoxTimeline.BoxRise(0f)), Is.EqualTo(StorageBoxTimeline.BoxStartY));
            Assert.That(StorageBoxTimeline.BoxY(StorageBoxTimeline.BoxRise(StorageBoxTimeline.BoxRiseEnd)), Is.EqualTo(0f));
            Assert.That(StorageBoxTimeline.OpenLid(StorageBoxTimeline.LidOpenStart - 0.001f), Is.Zero, "도착 전에는 닫혀 있다");
            Assert.That(StorageBoxTimeline.OpenLid(StorageBoxTimeline.LidOpenStart + StorageBoxTimeline.LidOpenDuration),
                Is.EqualTo(1f).Within(1e-4f));
            Assert.That(StorageBoxTimeline.PanelPresence(StorageBoxTimeline.PanelStart), Is.Zero);
            Assert.That(StorageBoxTimeline.PanelPresence(StorageBoxTimeline.OpenDuration), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Open_BoxRise_IsEaseOut_FastStartSlowArrival_NoOvershoot()
        {
            var half = StorageBoxTimeline.BoxRise(StorageBoxTimeline.BoxRiseEnd * 0.5f);
            Assert.That(half, Is.GreaterThan(0.75f), "초반 빠르게");
            var earlyStep = StorageBoxTimeline.BoxRise(Step) - StorageBoxTimeline.BoxRise(0f);
            var lateStep = StorageBoxTimeline.BoxRise(StorageBoxTimeline.BoxRiseEnd)
                - StorageBoxTimeline.BoxRise(StorageBoxTimeline.BoxRiseEnd - Step);
            Assert.That(lateStep, Is.LessThan(earlyStep * 0.1f), "가운데 근처에서 감속");
            AssertMonotonic01(StorageBoxTimeline.BoxRise, 0f, StorageBoxTimeline.OpenDuration, "rise");
            AssertMonotonic01(StorageBoxTimeline.OpenLid, 0f, StorageBoxTimeline.OpenDuration, "lid");
            AssertMonotonic01(StorageBoxTimeline.PanelPresence, 0f, StorageBoxTimeline.OpenDuration, "panel");
        }

        [Test]
        public void Open_InputLockedUntilFooterReveal()
        {
            for (var t = 0f; t < StorageBoxTimeline.FooterReveal - 0.0001f; t += Step)
            {
                Assert.That(StorageBoxTimeline.IsInteractive(t), Is.False, "t=" + t);
            }

            Assert.That(StorageBoxTimeline.IsInteractive(StorageBoxTimeline.FooterReveal), Is.True);
            Assert.That(StorageBoxTimeline.IsInteractive(StorageBoxTimeline.OpenDuration), Is.True);
        }

        [Test]
        public void Open_PopsStaggerOneByOne_AndFadeBeforeSettle()
        {
            var count = StorageBoxTimeline.PopCount(9);
            Assert.That(count, Is.EqualTo(StorageBoxTimeline.MaxPops));
            Assert.That(StorageBoxTimeline.PopCount(0), Is.Zero, "빈 화물·빈 보관이면 광물 팝 생략");
            Assert.That(StorageBoxTimeline.PopCount(2), Is.EqualTo(2));

            for (var i = 1; i < count; i++)
            {
                var t = StorageBoxTimeline.PopStartTime(i) - 0.001f;
                Assert.That(StorageBoxTimeline.PopAlpha(t, i), Is.Zero, "광물 " + i + "은 앞 광물 뒤에 나온다");
                Assert.That(StorageBoxTimeline.PopAlpha(t, i - 1), Is.GreaterThan(0f));
            }

            for (var i = 0; i < count; i++)
            {
                Assert.That(StorageBoxTimeline.PopAlpha(StorageBoxTimeline.OpenDuration, i), Is.Zero, "정착 후 광물 아이콘이 남지 않는다");
                Assert.That(StorageBoxTimeline.PopProgress(StorageBoxTimeline.PopStartTime(i) + StorageBoxTimeline.PopDuration, i),
                    Is.EqualTo(1f).Within(1e-4f));
            }

            Assert.That(StorageBoxTimeline.OpenBoxAlpha(StorageBoxTimeline.OpenDuration), Is.Zero, "정착하면 상자는 사라진다");
            Assert.That(StorageBoxTimeline.MouthGlow(StorageBoxTimeline.OpenDuration), Is.Zero, "발광은 짧게 한 번");
        }

        [Test]
        public void Open_Pops_GrowToPeak_ThenFlyToListIconShrinking()
        {
            const float landScale = 0.44f;
            for (var i = 0; i < StorageBoxTimeline.MaxPops; i++)
            {
                var apexTime = StorageBoxTimeline.FlyStartTime(i);
                Assert.That(apexTime, Is.EqualTo(StorageBoxTimeline.PopStartTime(i) + StorageBoxTimeline.PopDuration).Within(1e-5f),
                    "정점에 닿는 즉시 날아간다");
                Assert.That(StorageBoxTimeline.PopScale(apexTime, i, landScale), Is.EqualTo(1f).Within(1e-4f), "정점에서 최대 크기");
                Assert.That(StorageBoxTimeline.FlyProgress(apexTime, i), Is.Zero);

                var end = StorageBoxTimeline.FlyEndTime(i);
                Assert.That(StorageBoxTimeline.FlyProgress(end, i), Is.EqualTo(1f).Within(1e-4f));
                Assert.That(StorageBoxTimeline.PopScale(end, i, landScale), Is.EqualTo(landScale).Within(1e-4f), "안착하면 목록 아이콘 크기");
                Assert.That(StorageBoxTimeline.PopAlpha(end, i), Is.EqualTo(1f).Within(1e-4f), "안착 전까지 보인다");
                Assert.That(StorageBoxTimeline.PopAlpha(end + StorageBoxTimeline.LandFadeDuration, i), Is.Zero, "안착 뒤 목록 아이콘이 이어받는다");
                Assert.That(end + StorageBoxTimeline.LandFadeDuration, Is.LessThanOrEqualTo(StorageBoxTimeline.OpenDuration),
                    "마지막 광물도 정착 시간 안에 안착");
            }

            var previous = 1f;
            for (var t = StorageBoxTimeline.FlyStartTime(0); t <= StorageBoxTimeline.FlyEndTime(0); t += Step)
            {
                var scale = StorageBoxTimeline.PopScale(t, 0, landScale);
                Assert.That(scale, Is.LessThanOrEqualTo(previous + 1e-4f), "날아가는 동안 계속 작아진다");
                previous = scale;
            }
        }

        [Test]
        public void PopApex_SpreadsUpwardSymmetrically()
        {
            var single = StorageBoxTimeline.PopApex(0, 1);
            Assert.That(single.x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(single.y, Is.GreaterThan(0f));
            var first = StorageBoxTimeline.PopApex(0, 5);
            var last = StorageBoxTimeline.PopApex(4, 5);
            Assert.That(first.x, Is.EqualTo(-last.x).Within(1e-3f));
            for (var i = 0; i < 5; i++)
            {
                Assert.That(StorageBoxTimeline.PopApex(i, 5).y, Is.GreaterThan(0f), "모두 위로 튀어나온다");
            }

            Assert.That(StorageBoxTimeline.PopPosition(0f, first), Is.EqualTo(Vector2.zero), "입구에서 출발");
            Assert.That(StorageBoxTimeline.PopPosition(1f, first), Is.EqualTo(first), "정점에서 멈춤");
        }

        // ---------- 시간표: 종료 ----------

        [Test]
        public void Close_FromFullyOpen_ReversesPanelIconsLidThenDescends()
        {
            const float from = StorageBoxTimeline.OpenDuration;
            const int popped = 4;
            Assert.That(StorageBoxTimeline.CloseStartOffset(from, popped), Is.Zero);
            Assert.That(StorageBoxTimeline.ClosePanel(0f, from), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(StorageBoxTimeline.ClosePanel(StorageBoxTimeline.PanelShrinkDuration, from), Is.Zero, "패널이 먼저 상자로 들어간다");

            // 역순: 마지막에 튀어나온 광물이 먼저 담긴다.
            for (var i = 0; i < popped - 1; i++)
            {
                Assert.That(StorageBoxTimeline.ReturnStartTime(i, popped),
                    Is.GreaterThan(StorageBoxTimeline.ReturnStartTime(i + 1, popped)));
            }

            var lastIconIn = StorageBoxTimeline.ReturnStartTime(0, popped) + StorageBoxTimeline.ReturnDuration;
            Assert.That(lastIconIn, Is.LessThanOrEqualTo(StorageBoxTimeline.LidCloseStart + StorageBoxTimeline.LidCloseDuration));
            Assert.That(StorageBoxTimeline.CloseLid(StorageBoxTimeline.LidCloseStart, from), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(StorageBoxTimeline.CloseLid(StorageBoxTimeline.DescentStart + 0.02f, from), Is.Zero, "하강 전 뚜껑이 닫힌다");
            Assert.That(StorageBoxTimeline.CloseBoxY(StorageBoxTimeline.DescentStart, from), Is.EqualTo(0f).Within(1e-3f));
            Assert.That(StorageBoxTimeline.CloseBoxY(StorageBoxTimeline.CloseDuration, from),
                Is.EqualTo(StorageBoxTimeline.BoxStartY).Within(1e-3f));
            Assert.That(StorageBoxTimeline.CloseBoxAlpha(StorageBoxTimeline.CloseDuration, from), Is.Zero);
            for (var i = 0; i < popped; i++)
            {
                Assert.That(StorageBoxTimeline.ReturnAlpha(StorageBoxTimeline.CloseDuration, i, popped), Is.Zero);
            }
        }

        [Test]
        public void Close_Descent_IsEaseIn_SlowStartFastExit()
        {
            const float from = StorageBoxTimeline.OpenDuration;
            var span = StorageBoxTimeline.CloseDuration - StorageBoxTimeline.DescentStart;
            var mid = StorageBoxTimeline.CloseBoxY(StorageBoxTimeline.DescentStart + span * 0.5f, from);
            Assert.That(mid / StorageBoxTimeline.BoxStartY, Is.LessThan(0.25f), "처음엔 천천히");
        }

        [Test]
        public void Close_DuringEarlyIntro_SkipsEmptyStepsAndStartsFromCurrentBoxPose()
        {
            // 상자가 올라오는 중(뚜껑 닫힘, 광물·패널 없음): 바로 하강.
            const float rising = 0.15f;
            Assert.That(StorageBoxTimeline.CloseStartOffset(rising, 5), Is.EqualTo(StorageBoxTimeline.DescentStart));
            Assert.That(StorageBoxTimeline.PoppedCount(rising, 5), Is.Zero);
            Assert.That(StorageBoxTimeline.CloseBoxY(StorageBoxTimeline.DescentStart, rising),
                Is.EqualTo(StorageBoxTimeline.BoxY(StorageBoxTimeline.BoxRise(rising))).Within(1e-3f), "현재 위치에서 이어서 내려간다");
            Assert.That(StorageBoxTimeline.ClosePanel(0f, rising), Is.Zero, "등장 전 패널이 갑자기 나타나지 않는다");

            // 뚜껑이 열리는 중: 뚜껑 닫기부터.
            const float opening = 0.40f;
            Assert.That(StorageBoxTimeline.CloseStartOffset(opening, 5), Is.EqualTo(StorageBoxTimeline.LidCloseStart));
            Assert.That(StorageBoxTimeline.CloseLid(StorageBoxTimeline.LidCloseStart, opening),
                Is.EqualTo(StorageBoxTimeline.OpenLid(opening)).Within(1e-4f));

            // 광물이 일부만 나왔을 때는 나온 것만 되돌린다.
            const float popping = 0.50f;
            var popped = StorageBoxTimeline.PoppedCount(popping, 5);
            Assert.That(popped, Is.EqualTo(2));
            Assert.That(StorageBoxTimeline.CloseStartOffset(popping, 5), Is.Zero);
            Assert.That(StorageBoxTimeline.ReturnAlpha(StorageBoxTimeline.ReturnStart, 3, popped), Is.Zero);
        }

        // ---------- 상자 기하 ----------

        [Test]
        public void BoxGeometry_ClosedFlapsFormTopFace_OpenFlapsFoldOut()
        {
            const float size = 150f;
            var depth = StorageBoxGraphic.Depth(size);
            Assert.That(depth.x, Is.GreaterThan(0f));
            Assert.That(depth.y, Is.GreaterThan(0f), "깊이 방향은 오른쪽 위(3D처럼 보이는 사선 투영)");
            Assert.That(StorageBoxGraphic.Mouth(size), Is.EqualTo(new Vector2(0f, size * 0.5f)));

            Assert.That(Vector2.Distance(StorageBoxGraphic.FrontFlapEdge(size, 0f), depth * 0.5f), Is.LessThan(1e-3f));
            Assert.That(Vector2.Distance(StorageBoxGraphic.BackFlapEdge(size, 0f), -depth * 0.5f), Is.LessThan(1e-3f),
                "닫히면 두 뚜껑이 윗면 가운데에서 맞닿는다");

            var front = StorageBoxGraphic.FrontFlapEdge(size, 1f);
            var back = StorageBoxGraphic.BackFlapEdge(size, 1f);
            Assert.That(front.y, Is.LessThan(0f), "앞 뚜껑은 앞으로 넘어간다");
            Assert.That(back.x, Is.GreaterThan(0f), "뒤 뚜껑은 뒤로 젖혀진다");
            Assert.That(back.y, Is.GreaterThan(0f));
            Assert.That(StorageBoxGraphic.FrontFlapEdge(size, 0.5f).y, Is.GreaterThan(depth.y * 0.5f), "열리는 중에는 위로 선다");
        }

        // ---------- 수량 미리보기 ----------

        [Test]
        public void Preview_ClampAndAmounts_FollowServiceTransferRule()
        {
            Assert.That(StorageTransferPreview.ClampInput(10, 3, 5), Is.EqualTo(5), "보관 시 보유 / 꺼내기 시 보관 중 큰 값까지");
            Assert.That(StorageTransferPreview.ClampInput(4, 3, 5), Is.EqualTo(4));
            Assert.That(StorageTransferPreview.ClampInput(0, 3, 5), Is.Zero);
            Assert.That(StorageTransferPreview.ClampInput(-2, 3, 5), Is.Zero);
            Assert.That(StorageTransferPreview.ClampInput(7, 0, 0), Is.Zero);

            for (var requested = -2; requested <= 12; requested++)
            {
                for (var available = 0; available <= 9; available += 3)
                {
                    Assert.That(StorageTransferPreview.DepositAmount(requested, available),
                        Is.EqualTo(OutpostTransferQuantity.ClampToAvailable(requested, available)));
                    Assert.That(StorageTransferPreview.WithdrawAmount(requested, available),
                        Is.EqualTo(OutpostTransferQuantity.ClampToAvailable(requested, available)));
                }
            }

            Assert.That(StorageTransferPreview.DepositAmount(10, 3), Is.EqualTo(3), "요청이 더 많으면 가진 전부");
        }

        [Test]
        public void Preview_LinesTotalsAndPopIds()
        {
            var cargo = new InventorySnapshot(19.5f, 60f, 0, new[]
            {
                new InventoryStackEntry("mineral.copper", "구리", 5, 1.5f, 10),
                new InventoryStackEntry("mineral.iron", "철", 6, 2f, 15),
                new InventoryStackEntry("mineral.empty", "빈 자원", 0, 1f, 1)
            });
            var storage = new InventorySnapshot(4f, float.MaxValue, 0, new[]
            {
                new InventoryStackEntry("mineral.iron", "철", 2, 2f, 15)
            });

            var lines = StorageTransferPreview.Lines(cargo);
            Assert.That(lines.Count, Is.EqualTo(2), "0개 자원은 행을 만들지 않는다");
            Assert.That(lines[0].DisplayName, Is.EqualTo("구리"));
            Assert.That(lines[0].Weight, Is.EqualTo(7.5f).Within(1e-4f));
            var totals = StorageTransferPreview.Totals(cargo);
            Assert.That(totals.Kinds, Is.EqualTo(2));
            Assert.That(totals.Quantity, Is.EqualTo(11));
            Assert.That(StorageTransferPreview.TotalsText(totals), Is.EqualTo("2종 · 11개 · 19.5kg"));
            Assert.That(StorageTransferPreview.TotalsText(StorageTransferPreview.Totals(null)), Is.EqualTo("0종 · 0개 · 0kg"));

            var ids = StorageTransferPreview.PopIds(cargo, storage, 5);
            Assert.That(ids, Is.EqualTo(new[] { "mineral.copper", "mineral.iron" }), "중복 없이 화물 → 보관 순");
            Assert.That(StorageTransferPreview.PopIds(cargo, storage, 1).Count, Is.EqualTo(1));
            Assert.That(StorageTransferPreview.PopIds(null, null, 5), Is.Empty);
        }

        [Test]
        public void SearchAndQuantityInputs_DoNotRenderImeUnderlineTags()
        {
            var root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var events = new GameObject("EventSystem", typeof(EventSystem));
            try
            {
                var popup = StoragePopupView.Create(root.transform);
                popup.gameObject.SetActive(true);
                popup.transform.Find("Card").gameObject.SetActive(true);
                var eventData = new BaseEventData(events.GetComponent<EventSystem>());

                AssertPlainInput(popup, "Card/Content/Controls/MineralPicker/SearchInput", eventData);
                AssertPlainInput(popup, "Card/Content/Controls/QuantityInput", eventData);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(events);
            }
        }

        private static void AssertPlainInput(StoragePopupView popup, string path, BaseEventData eventData)
        {
            var input = popup.transform.Find(path).GetComponent<TMP_InputField>();
            Assert.That(input.richText, Is.False, path + " 리치 텍스트");
            Assert.That(input.textComponent.richText, Is.False, path + " 글자 리치 텍스트");
            Assert.That(input.readOnly, Is.True, path + "는 포커스 전에 조합 문자열을 받지 않는다");

            input.OnSelect(eventData);
            Assert.That(input.readOnly, Is.False, path + "는 포커스 중 입력된다");

            input.OnDeselect(eventData);
            Assert.That(input.readOnly, Is.True, path + "는 포커스를 잃으면 다시 조합 문자열을 막는다");
        }

        private static void AssertMonotonic01(System.Func<float, float> curve, float from, float to, string label)
        {
            var previous = curve(from);
            for (var t = from; t <= to; t += Step)
            {
                var value = curve(t);
                Assert.That(value, Is.InRange(0f, 1f), label + " 범위 t=" + t);
                Assert.That(value, Is.GreaterThanOrEqualTo(previous - 1e-5f), label + " 반동 없음 t=" + t);
                previous = value;
            }
        }
    }
}
