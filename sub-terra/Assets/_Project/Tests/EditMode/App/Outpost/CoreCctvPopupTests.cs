using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Outpost;
using SubTerra.App.UI.Outpost;
using SubTerra.Shared;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.Outpost
{
    /// <summary>전진기지 코어 CCTV 팝업: 시설 목록 모델, 시간표, 프리팹 구조, 표시 상태 전환.</summary>
    public sealed class CoreCctvPopupTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/OutpostPanel.prefab";

        private GameObject instance;

        [TearDown]
        public void TearDown()
        {
            if (instance != null)
            {
                Object.DestroyImmediate(instance);
                instance = null;
            }
        }

        // ---- 시설 목록 모델 ----

        [Test]
        public void List_ShowsOnlyActiveFacilities_WithDisplayNames()
        {
            var list = new CoreCctvFacilityList();

            list.Update(new[]
            {
                Facility("charger-0001", DataIds.Buildings.ChargerBasic, true),
                Facility("clinic-0001", DataIds.Buildings.ClinicBasic, false),
                Facility("settle-0001", DataIds.Buildings.SettlementBasic, true)
            });

            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list.Items[0].DisplayName, Is.EqualTo("충전기"));
            Assert.That(list.Items[1].DisplayName, Is.EqualTo("정산 콘솔"));
        }

        [Test]
        public void List_Order_IsStableRegardlessOfInputOrder()
        {
            var forward = new CoreCctvFacilityList();
            var reversed = new CoreCctvFacilityList();
            var facilities = new List<OutpostFacilityReadModel>
            {
                Facility("clinic-0002", DataIds.Buildings.ClinicBasic, true),
                Facility("charger-0003", DataIds.Buildings.ChargerBasic, true),
                Facility("charger-0001", DataIds.Buildings.ChargerBasic, true),
                Facility("settle-0001", DataIds.Buildings.SettlementBasic, true),
                Facility("clinic-0001", DataIds.Buildings.ClinicBasic, true)
            };

            forward.Update(facilities);
            facilities.Reverse();
            reversed.Update(facilities);

            Assert.That(Ids(forward), Is.EqualTo(new[]
            {
                "charger-0001", "charger-0003", "clinic-0001", "clinic-0002", "settle-0001"
            }));
            Assert.That(Ids(reversed), Is.EqualTo(Ids(forward)));
        }

        [Test]
        public void List_SameKindFacilities_KeepSeparateInstances()
        {
            var list = new CoreCctvFacilityList();

            list.Update(new[]
            {
                Facility("charger-0001", DataIds.Buildings.ChargerBasic, true),
                Facility("charger-0002", DataIds.Buildings.ChargerBasic, true)
            });

            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list.Items[0].InstanceId, Is.Not.EqualTo(list.Items[1].InstanceId));
            Assert.That(list.Items[0].DisplayName, Is.EqualTo(list.Items[1].DisplayName));
        }

        [Test]
        public void List_DuplicateInstanceIds_AreShownOnce()
        {
            var list = new CoreCctvFacilityList();

            list.Update(new[]
            {
                Facility("charger-0001", DataIds.Buildings.ChargerBasic, true),
                Facility("charger-0001", DataIds.Buildings.ChargerBasic, true)
            });

            Assert.That(list.Count, Is.EqualTo(1));
        }

        [Test]
        public void List_UnchangedUpdate_ReportsNoChange_AndKeepsSelection()
        {
            var list = new CoreCctvFacilityList();
            var facilities = Three();
            list.Update(facilities);
            list.ResetSelection();
            list.Select("charger-0002");

            Assert.That(list.Update(Three()), Is.False);
            Assert.That(list.SelectedInstanceId, Is.EqualTo("charger-0002"));
        }

        [Test]
        public void List_FirstSelection_IsFirstInStableOrder()
        {
            var list = new CoreCctvFacilityList();
            list.Update(Three());

            list.ResetSelection();

            Assert.That(list.SelectedInstanceId, Is.EqualTo("charger-0001"));
            Assert.That(list.SelectedIndex, Is.EqualTo(0));
        }

        [Test]
        public void List_SelectedStillValid_KeepsSelectionWhenOthersChange()
        {
            var list = new CoreCctvFacilityList();
            list.Update(Three());
            list.ResetSelection();
            list.Select("charger-0002");

            var changed = list.Update(new[]
            {
                Facility("charger-0002", DataIds.Buildings.ChargerBasic, true),
                Facility("clinic-0001", DataIds.Buildings.ClinicBasic, true),
                Facility("clinic-0009", DataIds.Buildings.ClinicBasic, true)
            });

            Assert.That(changed, Is.True);
            Assert.That(list.SelectedInstanceId, Is.EqualTo("charger-0002"));
        }

        [Test]
        public void List_SelectedRemoved_HandsOverToSameSlot_ThenToLast()
        {
            var list = new CoreCctvFacilityList();
            list.Update(Three());
            list.ResetSelection();
            list.Select("charger-0002");

            list.Update(new[]
            {
                Facility("charger-0001", DataIds.Buildings.ChargerBasic, true),
                Facility("clinic-0001", DataIds.Buildings.ClinicBasic, true)
            });
            Assert.That(list.SelectedInstanceId, Is.EqualTo("clinic-0001"), "같은 자리의 다음 시설로 넘어간다.");

            list.Update(new[] { Facility("charger-0001", DataIds.Buildings.ChargerBasic, true) });
            Assert.That(list.SelectedInstanceId, Is.EqualTo("charger-0001"), "마지막이 사라지면 남은 마지막 시설.");
        }

        [Test]
        public void List_AllRemoved_BecomesEmpty_AndFirstArrivalIsSelected()
        {
            var list = new CoreCctvFacilityList();
            list.Update(Three());
            list.ResetSelection();

            list.Update(new List<OutpostFacilityReadModel>());
            Assert.That(list.Count, Is.Zero);
            Assert.That(list.SelectedInstanceId, Is.Empty);
            Assert.That(list.Step(1), Is.False);

            list.Update(Three());
            Assert.That(list.SelectedInstanceId, Is.EqualTo("charger-0001"));
        }

        [Test]
        public void List_Step_StopsAtEnds_AndSelectSameDoesNothing()
        {
            var list = new CoreCctvFacilityList();
            list.Update(Three());
            list.ResetSelection();

            Assert.That(list.Step(-1), Is.False);
            Assert.That(list.Step(1), Is.True);
            Assert.That(list.Step(1), Is.True);
            Assert.That(list.Step(1), Is.False);
            Assert.That(list.SelectedInstanceId, Is.EqualTo("clinic-0001"));
            Assert.That(list.Select("clinic-0001"), Is.False);
            Assert.That(list.Select("missing"), Is.False);
        }

        // ---- 시간표 ----

        [Test]
        public void Timeline_IntroTotal_IsAboutOneToOnePointFourSeconds()
        {
            Assert.That(CoreCctvTimeline.IntroDuration(1), Is.InRange(1.0f, 1.4f));
            Assert.That(CoreCctvTimeline.IntroDuration(8), Is.InRange(1.0f, 1.4f));
            Assert.That(CoreCctvTimeline.IntroDuration(0), Is.LessThanOrEqualTo(1.4f));
        }

        [Test]
        public void Timeline_Window_LineFirst_ThenExpands_ThenScreenThenTitle()
        {
            var line = CoreCctvTimeline.Window(0.08f);
            Assert.That(line.LineWidth, Is.GreaterThan(0.5f));
            Assert.That(line.Open, Is.Zero, "가로선이 펼쳐진 뒤에 세로로 확장된다.");

            var opening = CoreCctvTimeline.Window(0.22f);
            Assert.That(opening.Open, Is.InRange(0.2f, 0.95f));
            Assert.That(opening.ScreenAlpha, Is.LessThan(0.2f));
            Assert.That(opening.TitleAlpha, Is.Zero, "내부 글자는 프레임이 펼쳐진 뒤에 나타난다.");

            var done = CoreCctvTimeline.Window(CoreCctvTimeline.OpenEnd + 0.1f);
            Assert.That(done.Open, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(done.ScreenAlpha, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(done.TitleAlpha, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(done.Flash, Is.LessThan(0.01f));
            Assert.That(done.EdgeNoise, Is.Zero);
        }

        [Test]
        public void Timeline_Flash_IsShortAtStart()
        {
            Assert.That(CoreCctvTimeline.Window(0.05f).Flash, Is.GreaterThan(0.8f));
            Assert.That(CoreCctvTimeline.Window(0.3f).Flash, Is.LessThan(0.01f));
        }

        [Test]
        public void Timeline_Terminal_TypesLinesInOrder_ThenConnectedBlinksOnlyWhenConnected()
        {
            for (var i = 1; i < CoreCctvTimeline.TerminalLineCount; i++)
            {
                Assert.That(CoreCctvTimeline.TerminalLineStart(i), Is.GreaterThan(CoreCctvTimeline.TerminalLineStart(i - 1)));
            }

            Assert.That(CoreCctvTimeline.TerminalTyping(3, CoreCctvTimeline.TerminalLineStart(2)), Is.Zero);
            Assert.That(CoreCctvTimeline.TerminalTyping(0, CoreCctvTimeline.DecideTime), Is.EqualTo(1f));

            Assert.That(CoreCctvTimeline.TerminalAlpha(CoreCctvTimeline.DecideTime - 0.01f, true), Is.EqualTo(1f));
            Assert.That(CoreCctvTimeline.TerminalAlpha(CoreCctvTimeline.DecideTime + 0.01f, true), Is.Zero);
            Assert.That(CoreCctvTimeline.ConnectedAlpha(CoreCctvTimeline.DecideTime - 0.01f), Is.Zero);
            Assert.That(CoreCctvTimeline.ConnectedAlpha(CoreCctvTimeline.DecideTime + 0.02f), Is.EqualTo(1f));
            Assert.That(CoreCctvTimeline.ConnectedAlpha(CoreCctvTimeline.DecideTime + 0.07f), Is.Zero, "깜빡인다.");
            Assert.That(CoreCctvTimeline.ConnectedAlpha(CoreCctvTimeline.DecideTime + CoreCctvTimeline.ConnectedDuration + 0.01f), Is.Zero);
        }

        [Test]
        public void Timeline_EmptyTerminal_FadesAway_ForBlackScreen()
        {
            var end = CoreCctvTimeline.DecideTime + CoreCctvTimeline.EmptyTerminalFade + 0.01f;
            Assert.That(CoreCctvTimeline.TerminalAlpha(end, false), Is.Zero);
        }

        [Test]
        public void Timeline_Items_AppearTopToBottom_AndStaggerIsCapped()
        {
            Assert.That(CoreCctvTimeline.ItemDelay(1, 2), Is.GreaterThan(CoreCctvTimeline.ItemDelay(0, 2)));
            Assert.That(CoreCctvTimeline.ItemDelay(0, 3), Is.Zero);
            Assert.That(CoreCctvTimeline.ItemDelay(29, 30), Is.LessThanOrEqualTo(CoreCctvTimeline.ItemStaggerSpan + 0.0001f));
            Assert.That(CoreCctvTimeline.ItemAlpha(0.02f, 2, 3), Is.Zero);
            Assert.That(CoreCctvTimeline.ItemAlpha(1f, 2, 3), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Timeline_Power_StartsBlack_AndEndsFullBright_WithNoiseGone()
        {
            var start = CoreCctvTimeline.Power(0f);
            Assert.That(start.Alpha, Is.Zero);
            Assert.That(start.Rec, Is.Zero);

            var mid = CoreCctvTimeline.Power(0.08f);
            Assert.That(mid.BarAlpha, Is.GreaterThan(0.2f), "짧은 수평 노이즈.");
            Assert.That(mid.Brightness, Is.InRange(0.2f, 0.99f));

            var end = CoreCctvTimeline.Power(CoreCctvTimeline.VideoOnDuration);
            Assert.That(end.Alpha, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(end.Brightness, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(end.BarAlpha, Is.Zero);
            Assert.That(end.NoiseBoost, Is.Zero);
            Assert.That(end.Rec, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Timeline_CameraMove_IsFastWithEasedEnds()
        {
            Assert.That(CoreCctvTimeline.MoveDuration, Is.InRange(0.2f, 0.35f));
            Assert.That(CoreCctvTimeline.MoveEase(0f), Is.Zero);
            Assert.That(CoreCctvTimeline.MoveEase(CoreCctvTimeline.MoveDuration), Is.EqualTo(1f));
            Assert.That(CoreCctvTimeline.MoveSpeed(0f), Is.Zero);
            Assert.That(CoreCctvTimeline.MoveSpeed(CoreCctvTimeline.MoveDuration), Is.Zero, "이동이 끝나면 잔상이 없다.");
            Assert.That(CoreCctvTimeline.MoveSpeed(CoreCctvTimeline.MoveDuration * 0.5f), Is.EqualTo(1f).Within(0.0001f));
            // 출발·도착은 완만하다(중간보다 변화가 작다).
            var early = CoreCctvTimeline.MoveEase(0.02f);
            var middle = CoreCctvTimeline.MoveEase(CoreCctvTimeline.MoveDuration * 0.5f + 0.01f)
                - CoreCctvTimeline.MoveEase(CoreCctvTimeline.MoveDuration * 0.5f - 0.01f);
            Assert.That(early, Is.LessThan(middle));
        }

        [Test]
        public void Timeline_Exit_IsShort_FoldsToLine_AndEndsAtZero()
        {
            Assert.That(CoreCctvTimeline.ExitDuration, Is.InRange(0.2f, 0.35f));
            var start = CoreCctvTimeline.Exit(0f);
            Assert.That(start.Content, Is.EqualTo(1f));
            Assert.That(start.Open, Is.EqualTo(1f));

            var line = CoreCctvTimeline.Exit(0.18f);
            Assert.That(line.Content, Is.Zero, "영상과 내부 요소가 먼저 어두워진다.");
            Assert.That(line.Open, Is.LessThan(0.05f));
            Assert.That(line.LineWidth, Is.GreaterThan(0.9f), "세로로 접혀 가로선이 된다.");

            var end = CoreCctvTimeline.Exit(CoreCctvTimeline.ExitDuration);
            Assert.That(end.Open, Is.Zero);
            Assert.That(end.LineWidth, Is.Zero);
            Assert.That(end.Glint, Is.Zero);
            Assert.That(end.Alpha, Is.Zero);
        }

        // ---- 목록 배치·스크롤 ----

        [Test]
        public void Layout_NeedsScrollOnlyWhenContentExceedsViewport()
        {
            const float viewport = 386f;
            Assert.That(CoreCctvListLayout.NeedsScroll(0, viewport), Is.False);
            Assert.That(CoreCctvListLayout.NeedsScroll(2, viewport), Is.False);
            Assert.That(CoreCctvListLayout.NeedsScroll(4, viewport), Is.False);
            Assert.That(CoreCctvListLayout.NeedsScroll(5, viewport), Is.True);
        }

        [Test]
        public void Layout_ScrollToReveal_KeepsSelectedItemFullyVisible()
        {
            const float viewport = 386f;
            const int count = 9;
            var scroll = 0f;
            for (var index = 0; index < count; index++)
            {
                scroll = CoreCctvListLayout.ScrollToReveal(index, count, viewport, scroll);
                var top = CoreCctvListLayout.ItemTop(index) - scroll;
                var bottom = top + CoreCctvListLayout.ItemHeight;
                Assert.That(top, Is.GreaterThanOrEqualTo(-0.01f), "항목 " + index + " 위쪽");
                Assert.That(bottom, Is.LessThanOrEqualTo(viewport + 0.01f), "항목 " + index + " 아래쪽");
            }

            for (var index = count - 1; index >= 0; index--)
            {
                scroll = CoreCctvListLayout.ScrollToReveal(index, count, viewport, scroll);
                var top = CoreCctvListLayout.ItemTop(index) - scroll;
                Assert.That(top, Is.GreaterThanOrEqualTo(-0.01f));
                Assert.That(top + CoreCctvListLayout.ItemHeight, Is.LessThanOrEqualTo(viewport + 0.01f));
            }

            Assert.That(scroll, Is.Zero);
        }

        [Test]
        public void Layout_ScrollToReveal_DoesNotMoveWhenAlreadyVisible()
        {
            var scroll = CoreCctvListLayout.ScrollToReveal(2, 9, 386f, 0f);
            Assert.That(CoreCctvListLayout.ScrollToReveal(2, 9, 386f, scroll), Is.EqualTo(scroll));
            Assert.That(CoreCctvListLayout.ScrollToReveal(0, 3, 386f, 0f), Is.Zero);
        }

        // ---- 프리팹 ----

        [Test]
        public void Prefab_HasCorePopupWired_AndKeepsExistingPanelAndServicePopup()
        {
            var view = Spawn();
            var popup = view.CorePopup;

            Assert.That(popup, Is.Not.Null);
            Assert.That(popup.HasRequiredReferences(), Is.True);
            Assert.That(popup.gameObject.activeSelf, Is.False);
            Assert.That(popup.transform.parent, Is.EqualTo(view.transform));
            Assert.That(view.CoreCloseButton, Is.Not.Null);
            Assert.That(view.ServicePopup, Is.Not.Null, "충전기·보건소 팝업은 그대로여야 한다.");
            Assert.That(view.PanelRoot.transform.Find("CoreRoot"), Is.Not.Null, "기존 패널 계층은 건드리지 않는다.");
            Assert.That(view.PanelRoot.transform.Find("CloseButton"), Is.Not.Null);
        }

        [Test]
        public void Prefab_Layout_ListIsAboutAThird_AndCctvTakesTheRest_WithoutOverlap()
        {
            var view = Spawn();
            var popup = view.CorePopup;
            var window = (RectTransform)popup.transform;
            var list = (RectTransform)popup.transform.Find("ContentClip/ContentInner/Body/ListPanel");
            var cctv = (RectTransform)popup.transform.Find("ContentClip/ContentInner/Body/CctvArea");

            var body = list.rect.width + cctv.rect.width;
            Assert.That(list.rect.width / body, Is.InRange(0.30f, 0.36f));
            Assert.That(cctv.anchoredPosition.x, Is.GreaterThan(list.anchoredPosition.x + list.rect.width));
            Assert.That(cctv.anchoredPosition.x + cctv.rect.width, Is.LessThanOrEqualTo(window.rect.width));
            Assert.That(list.rect.height, Is.EqualTo(cctv.rect.height));
            Assert.That(popup.transform.Find("ContentClip/ContentInner/Body/CctvArea/Screen/Video"), Is.Not.Null);
        }

        [Test]
        public void Prefab_HasNoRemovedElements_OrDebugInfo()
        {
            var view = Spawn();
            var popup = view.CorePopup;
            var forbidden = new[] { "공급", "소비", "활성", "전력 연결", "연결 완료", "[", "(", "building.", "runtime" };

            foreach (var text in popup.GetComponentsInChildren<TMP_Text>(true))
            {
                foreach (var word in forbidden)
                {
                    Assert.That(text.text, Does.Not.Contain(word), text.name + " 에 불필요한 문구");
                }
            }

            Assert.That(popup.transform.Find("ContentClip/ContentInner/Body/CctvArea/Info"), Is.Null, "CCTV 아래 설명 패널은 없다.");
            Assert.That(popup.transform.Find("ContentClip/ContentInner/StatusBar"), Is.Null);
        }

        [Test]
        public void Prefab_ConnectedLabel_IsOnlyUsedInsideTheCctvScreen_AndStartsHidden()
        {
            var view = Spawn();
            var popup = view.CorePopup;
            var texts = new List<string>();
            foreach (var text in popup.GetComponentsInChildren<TMP_Text>(true))
            {
                texts.Add(text.text);
            }

            Assert.That(texts, Does.Contain(CoreCctvPopupView.ConnectedLabel));
            Assert.That(popup.ConnectedLabelAlpha, Is.Zero);
            Assert.That(popup.RecAlpha, Is.Zero);
        }

        // ---- 표시 상태 ----

        [Test]
        public void CoreMode_UsesCctvPopup_NotLegacyPanel()
        {
            var view = Spawn();

            view.SetMode(OutpostPanelMode.Core);
            view.SetVisible(true);

            Assert.That(view.CorePopup.IsShown, Is.True);
            Assert.That(view.PanelRoot.activeSelf, Is.False);
            Assert.That(view.ActiveWindowRoot, Is.EqualTo(view.CorePopup.gameObject));
            Assert.That(view.transform.Find("PanelRoot/CoreRoot").gameObject.activeSelf, Is.False);
            Assert.That(view.ServicePopup.State, Is.EqualTo(FacilityServicePopupView.PlayState.Hidden));
        }

        [Test]
        public void ServiceModes_StillUseServicePopup_AndCoreStaysHidden()
        {
            var view = Spawn();

            view.SetMode(OutpostPanelMode.Clinic);
            view.SetVisible(true);

            Assert.That(view.ServicePopup.IsShown, Is.True);
            Assert.That(view.CorePopup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Hidden));
        }

        [Test]
        public void Popup_IntroOnlyShowsBlackScreen_FirstThenTerminal_ListAndVideoComeLater()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(2));

            popup.Tick(0.20f);
            Assert.That(popup.WindowOpen, Is.GreaterThan(0f));
            Assert.That(popup.ListAlpha, Is.Zero, "시설 목록은 아직 표시하지 않는다.");
            Assert.That(popup.VideoLevel, Is.Zero, "실제 영상도 아직 켜지지 않는다.");
            Assert.That(popup.RecAlpha, Is.Zero);

            popup.Tick(0.30f);
            Assert.That(popup.TerminalAlpha, Is.EqualTo(1f));
            Assert.That(popup.ListAlpha, Is.Zero);
            Assert.That(popup.RevealStarted, Is.False);
        }

        [Test]
        public void Popup_Intro_ShowsConnectedLabelThenListThenVideo_AndSelectsFirst()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(3));

            popup.Tick(CoreCctvTimeline.DecideTime + 0.02f);
            Assert.That(popup.TerminalAlpha, Is.Zero, "터미널 문구가 사라진다.");
            Assert.That(popup.ConnectedLabelAlpha, Is.EqualTo(1f));
            Assert.That(popup.RevealStarted, Is.False);

            popup.Tick(CoreCctvTimeline.RevealStartConnected - popup.Clock + 0.02f);
            Assert.That(popup.RevealStarted, Is.True);
            Assert.That(popup.ListAlpha, Is.EqualTo(1f));
            Assert.That(popup.ItemCount, Is.EqualTo(3));
            Assert.That(popup.SelectedInstanceId, Is.EqualTo(Facilities(3)[0].InstanceId));
            Assert.That(popup.ItemAt(0).IsSelected, Is.True);
            Assert.That(popup.ItemAt(1).IsSelected, Is.False);

            popup.Tick(CoreCctvTimeline.IntroDuration(3));
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));
            Assert.That(popup.ConnectedLabelAlpha, Is.Zero, "연결되었습니다는 등장 연출에서만 쓴다.");
            Assert.That(popup.Video, Is.EqualTo(CoreCctvPopupView.VideoState.On));
            Assert.That(popup.RecAlpha, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(popup.ItemAt(2).Alpha, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Popup_Items_AppearOneAfterAnother_TopToBottom()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(3));
            popup.Tick(CoreCctvTimeline.RevealStartConnected + 0.001f);

            popup.Tick(0.06f);
            Assert.That(popup.ItemAt(0).Alpha, Is.GreaterThan(popup.ItemAt(1).Alpha));
            Assert.That(popup.ItemAt(1).Alpha, Is.GreaterThan(popup.ItemAt(2).Alpha));
        }

        [Test]
        public void Popup_NoFacilities_KeepsBlackScreen_WithOnlyEmptyListLabel()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(0));

            popup.Tick(CoreCctvTimeline.DecideTime + 0.05f);
            Assert.That(popup.ConnectedLabelAlpha, Is.Zero, "연결되었습니다는 표시하지 않는다.");

            popup.Tick(2f);
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));
            Assert.That(popup.Video, Is.EqualTo(CoreCctvPopupView.VideoState.Off));
            Assert.That(popup.RecAlpha, Is.Zero);
            Assert.That(popup.VideoLevel, Is.Zero);
            Assert.That(popup.ItemCount, Is.Zero);
            Assert.That(popup.EmptyLabelAlpha, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(popup.TerminalAlpha, Is.Zero);
        }

        [Test]
        public void Popup_ClickingItem_ChangesSelectionOnly_AndAlreadySelectedDoesNothing()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(3));
            popup.Tick(3f);
            var second = Facilities(3)[1].InstanceId;
            var secondIndex = popup.List.IndexOf(second);

            popup.SelectFacility(second);

            Assert.That(popup.SelectedInstanceId, Is.EqualTo(second));
            Assert.That(popup.ItemAt(secondIndex).IsSelected, Is.True, "선택 강조는 즉시 옮겨진다.");
            Assert.That(popup.ItemAt(0).IsSelected, Is.False);

            popup.SelectFacility(second);
            Assert.That(popup.SelectedInstanceId, Is.EqualTo(second));
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live), "선택은 등장 연출을 다시 시작하지 않는다.");
        }

        [Test]
        public void Popup_StepSelection_MovesHighlightAndKeepsItemVisible()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(9));
            popup.Tick(3f);

            var viewport = popup.ListViewport.rect.height;
            for (var i = 0; i < 8; i++)
            {
                popup.StepSelection(1);
                var item = popup.ItemAt(popup.List.SelectedIndex);
                var top = -item.Rect.anchoredPosition.y - popup.ListContent.anchoredPosition.y;
                Assert.That(top, Is.GreaterThanOrEqualTo(-0.5f));
                Assert.That(top + item.Rect.rect.height, Is.LessThanOrEqualTo(viewport + 0.5f));
            }

            Assert.That(popup.ListContent.anchoredPosition.y, Is.GreaterThan(0f));
            for (var i = 0; i < 8; i++)
            {
                popup.StepSelection(-1);
            }

            Assert.That(popup.ListContent.anchoredPosition.y, Is.Zero);
        }

        [Test]
        public void Popup_SetFacilities_WithSameList_DoesNotRestartIntro()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(2));
            popup.Tick(3f);
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));
            var clock = popup.Clock;

            popup.SetFacilities(Facilities(2));
            view.SetFacilities(Facilities(2));

            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));
            Assert.That(popup.Clock, Is.EqualTo(clock));
            Assert.That(popup.ItemCount, Is.EqualTo(2));
        }

        [Test]
        public void Popup_FacilityAddedAndRemovedWhileOpen_UpdatesListWithoutRestartingIntro()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(2));
            popup.Tick(3f);
            var kept = Facilities(2)[1].InstanceId;
            popup.SelectFacility(kept);

            popup.SetFacilities(Facilities(4));
            Assert.That(popup.ItemCount, Is.EqualTo(4));
            Assert.That(popup.SelectedInstanceId, Is.EqualTo(kept), "선택한 시설이 남아 있으면 선택을 유지한다.");
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));

            var removed = new List<OutpostFacilityReadModel>(Facilities(4));
            removed.RemoveAt(1);
            popup.SetFacilities(removed);
            Assert.That(popup.ItemCount, Is.EqualTo(3));
            Assert.That(popup.SelectedInstanceId, Is.Not.EqualTo(kept));
            Assert.That(popup.List.IndexOf(popup.SelectedInstanceId), Is.GreaterThanOrEqualTo(0));
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));
        }

        [Test]
        public void Popup_AllFacilitiesRemoved_ReturnsToBlackScreenAndEmptyList_ThenReturnsWhenAdded()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(2));
            popup.Tick(3f);

            popup.SetFacilities(Facilities(0));
            popup.Tick(0.5f);
            Assert.That(popup.ItemCount, Is.Zero);
            Assert.That(popup.Video, Is.EqualTo(CoreCctvPopupView.VideoState.Off));
            Assert.That(popup.RecAlpha, Is.Zero);
            Assert.That(popup.EmptyLabelAlpha, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));

            popup.SetFacilities(Facilities(2));
            Assert.That(popup.ItemCount, Is.EqualTo(2));
            Assert.That(popup.SelectedInstanceId, Is.EqualTo(Facilities(2)[0].InstanceId));
            popup.Tick(1f);
            Assert.That(popup.Video, Is.EqualTo(CoreCctvPopupView.VideoState.On));
            Assert.That(popup.EmptyLabelAlpha, Is.Zero);
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live), "목록 갱신만으로 등장 연출을 다시 시작하지 않는다.");
        }

        [Test]
        public void Popup_FacilitiesArrivingDuringTerminal_AreUsedWhenDeciding()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(0));
            popup.Tick(0.4f);

            view.SetFacilities(Facilities(2));
            popup.Tick(2f);

            Assert.That(popup.ItemCount, Is.EqualTo(2));
            Assert.That(popup.Video, Is.EqualTo(CoreCctvPopupView.VideoState.On));
        }

        [Test]
        public void Popup_Close_PlaysShortExit_ThenDeactivatesWithNoResidualEffects()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(2));
            popup.Tick(3f);

            view.SetVisible(false);
            view.SetMode(OutpostPanelMode.None);
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Exiting));
            Assert.That(popup.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);

            popup.Tick(CoreCctvTimeline.ExitDuration * 0.5f);
            Assert.That(popup.gameObject.activeSelf, Is.True);
            popup.Tick(CoreCctvTimeline.ExitDuration);

            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Hidden));
            Assert.That(popup.gameObject.activeSelf, Is.False);
            Assert.That(popup.RecAlpha, Is.Zero);
            Assert.That(popup.VideoLevel, Is.Zero);
            Assert.That(popup.GhostsVisible, Is.False);
            Assert.That(popup.Video, Is.EqualTo(CoreCctvPopupView.VideoState.Off));
        }

        [Test]
        public void Popup_CloseDuringIntro_StillClosesCleanly()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(2));
            popup.Tick(0.15f);

            view.SetVisible(false);
            view.SetMode(OutpostPanelMode.None);
            popup.Tick(0.05f);
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Exiting));
            popup.Tick(CoreCctvTimeline.ExitDuration + 0.05f);

            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Hidden));
            Assert.That(popup.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Popup_RapidCloseAndReopen_RestartsCleanly()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(2));
            popup.Tick(3f);

            view.SetVisible(false);
            view.SetMode(OutpostPanelMode.None);
            popup.Tick(0.05f);
            view.SetMode(OutpostPanelMode.Core);
            view.SetVisible(true);

            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Intro));
            Assert.That(popup.Clock, Is.Zero);
            Assert.That(popup.RevealStarted, Is.False);
            Assert.That(popup.ListAlpha, Is.Zero);
            Assert.That(popup.RecAlpha, Is.Zero);
            popup.Tick(3f);
            Assert.That(popup.ItemCount, Is.EqualTo(2));
            Assert.That(popup.SelectedInstanceId, Is.EqualTo(Facilities(2)[0].InstanceId), "다시 열면 첫 시설부터 시작한다.");
        }

        [Test]
        public void Popup_RepeatedShowCalls_DoNotRestartIntro()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(2));
            popup.Tick(0.4f);

            view.SetVisible(true);
            view.SetMode(OutpostPanelMode.Core);
            view.SetFacilities(Facilities(2));

            Assert.That(popup.Clock, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void Popup_TimeScaleZero_StillPlaysThroughUnscaledTime()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(2));
            var previous = Time.timeScale;
            Time.timeScale = 0f;
            try
            {
                popup.Tick(3f);
                Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));
                Assert.That(popup.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            }
            finally
            {
                Time.timeScale = previous;
            }
        }

        [Test]
        public void Popup_TextAndIcons_AreNeverScaledWhileFrameUnfolds()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(2));
            var inner = popup.transform.Find("ContentClip/ContentInner");
            var backdrop = popup.transform.Find("PanelBackdrop");

            for (var t = 0f; t < 1.4f; t += 0.04f)
            {
                popup.Tick(0.04f);
                Assert.That(inner.localScale, Is.EqualTo(Vector3.one));
                foreach (var text in popup.GetComponentsInChildren<TMP_Text>(true))
                {
                    Assert.That(text.transform.lossyScale, Is.EqualTo(popup.transform.lossyScale), text.name);
                }
            }

            Assert.That(backdrop.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void Popup_FacilityIcons_UseExistingBuildingIcons()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(1));
            popup.Tick(3f);
            var charger = AssetDatabase.LoadAssetAtPath<BuildingData>(
                "Assets/_Project/Data/Buildings/Building_Charger_Basic.asset");

            Assert.That(popup.ItemAt(0).Icon.sprite, Is.Not.Null);
            Assert.That(popup.ItemAt(0).Icon.sprite, Is.EqualTo(charger.Icon));
            Assert.That(popup.ItemAt(0).NameText.text, Is.EqualTo("충전기"));
        }

        [Test]
        public void Popup_BlocksInputBehindIt()
        {
            var view = Spawn();
            var popup = OpenWith(view, Facilities(1));

            var shield = popup.transform.Find("InputShield").GetComponent<UnityEngine.UI.Image>();
            Assert.That(shield.raycastTarget, Is.True);
            Assert.That(popup.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
        }

        // ---- 코어 상호작용 흐름(Presenter·Service 연결) ----

        [Test]
        public void Flow_InteractionOpensCctv_WithConnectedFacilitiesFromSnapshot()
        {
            var (service, presenter, view) = CreateFlow();
            service.ApplyRuntimeStatus(CoreStatus(true, "charger-0001", "clinic-0001"));
            Assert.That(view.CorePopup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Hidden), "E를 누르기 전에는 열리지 않는다.");

            presenter.ToggleInteractionPanel();
            view.CorePopup.Tick(3f);

            Assert.That(view.CorePopup.IsShown, Is.True);
            Assert.That(view.CorePopup.ItemCount, Is.EqualTo(2));
            Assert.That(view.CorePopup.ItemAt(0).NameText.text, Is.EqualTo("충전기"));
            Assert.That(view.CorePopup.ItemAt(1).NameText.text, Is.EqualTo("보건소"));
            presenter.Unbind();
        }

        [Test]
        public void Flow_InactiveFacilities_AreNotListed()
        {
            var (service, presenter, view) = CreateFlow();
            var status = CoreStatus(true, "charger-0001");
            status.connectedFacilities.Add(new ConnectedFacilityStatusDto
            {
                instanceId = "clinic-0001",
                buildingId = DataIds.Buildings.ClinicBasic,
                isActive = false,
                inactiveReasonId = "power_disconnected"
            });
            service.ApplyRuntimeStatus(status);

            presenter.ToggleInteractionPanel();
            view.CorePopup.Tick(3f);

            Assert.That(view.CorePopup.ItemCount, Is.EqualTo(1));
            presenter.Unbind();
        }

        [Test]
        public void Flow_CloseWithX_DoesNotReopenWhileStillInRange_AndInteractReopens()
        {
            var (service, presenter, view) = CreateFlow();
            service.ApplyRuntimeStatus(CoreStatus(true, "charger-0001"));
            presenter.ToggleInteractionPanel();
            view.CorePopup.Tick(3f);

            presenter.DismissInteractionPanel();
            Assert.That(view.CorePopup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Exiting));
            view.CorePopup.Tick(1f);
            Assert.That(view.CorePopup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Hidden));

            // 같은 범위 안에서 상태가 매 프레임 갱신돼도 다시 열리지 않는다.
            for (var frame = 0; frame < 30; frame++)
            {
                service.ApplyRuntimeStatus(CoreStatus(true, "charger-0001"));
                Assert.That(view.CorePopup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Hidden), "프레임 " + frame);
                Assert.That(presenter.IsInteractionPanelOpen, Is.False);
            }

            presenter.ToggleInteractionPanel();
            Assert.That(view.CorePopup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Intro));
            presenter.Unbind();
        }

        [Test]
        public void Flow_LeavingCoreRange_ClosesWithPowerOffExit()
        {
            var (service, presenter, view) = CreateFlow();
            service.ApplyRuntimeStatus(CoreStatus(true, "charger-0001"));
            presenter.ToggleInteractionPanel();
            view.CorePopup.Tick(0.2f);

            service.ApplyRuntimeStatus(new OutpostStatusDto
            {
                isInInteractionRange = false,
                connectedFacilities = new List<ConnectedFacilityStatusDto>()
            });

            Assert.That(view.CorePopup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Exiting), "등장 도중에도 범위를 벗어나면 닫힌다.");
            view.CorePopup.Tick(1f);
            Assert.That(view.CorePopup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Hidden));
            presenter.Unbind();
        }

        [Test]
        public void Flow_SnapshotUpdatesWhileOpen_RefreshListWithoutRestartingIntro()
        {
            var (service, presenter, view) = CreateFlow();
            service.ApplyRuntimeStatus(CoreStatus(true, "charger-0001"));
            presenter.ToggleInteractionPanel();
            view.CorePopup.Tick(3f);
            var clock = view.CorePopup.Clock;

            service.ApplyRuntimeStatus(CoreStatus(true, "charger-0001", "charger-0002", "clinic-0001"));
            Assert.That(view.CorePopup.ItemCount, Is.EqualTo(3));
            Assert.That(view.CorePopup.Clock, Is.EqualTo(clock));

            service.ApplyRuntimeStatus(CoreStatus(true));
            view.CorePopup.Tick(0.5f);
            Assert.That(view.CorePopup.ItemCount, Is.Zero);
            Assert.That(view.CorePopup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));
            presenter.Unbind();
        }

        [Test]
        public void Flow_SelectingFacility_NeverUsesIt_OrChangesPlayerState()
        {
            var (service, presenter, view) = CreateFlow();
            var operations = 0;
            service.OperationCompleted += _ => operations++;
            service.ApplyRuntimeStatus(CoreStatus(true, "charger-0001", "clinic-0001"));
            presenter.ToggleInteractionPanel();
            view.CorePopup.Tick(3f);

            view.CorePopup.SelectFacility("clinic-0001");
            view.CorePopup.StepSelection(-1);
            view.CorePopup.StepSelection(1);

            Assert.That(operations, Is.Zero);
            Assert.That(service.IsFacilityInteraction, Is.True);
            Assert.That(service.InteractionFacilityBuildingId, Is.EqualTo(DataIds.Buildings.OutpostCoreBasic));
            presenter.Unbind();
        }

        // ---- helpers ----

        private (OutpostService Service, OutpostPanelPresenter Presenter, OutpostPanelView View) CreateFlow()
        {
            var catalog = new SubTerra.App.Inventory.InMemoryMineralCatalog();
            var state = SubTerra.App.State.GameState.CreateNew();
            var inventory = new SubTerra.App.Inventory.InventoryService(catalog, 100f, state);
            var service = new OutpostService(inventory, catalog, state);
            var view = Spawn();
            var presenter = new OutpostPanelPresenter(new CorePanelProbe(view));
            presenter.Bind(service);
            return (service, presenter, view);
        }

        /// <summary>
        /// 실제 View로 전달하되, 편집 모드에서 Destroy를 부르는 광물 선택 목록(코어 팝업과 무관)은 건너뛴다.
        /// </summary>
        private sealed class CorePanelProbe : IOutpostPanelView
        {
            private readonly OutpostPanelView view;

            public CorePanelProbe(OutpostPanelView view)
            {
                this.view = view;
            }

            public void SetVisible(bool visible) => view.SetVisible(visible);
            public void SetMode(OutpostPanelMode mode) => view.SetMode(mode);
            public void SetPower(float supply, float consumption, bool active, string inactiveReasonId) =>
                view.SetPower(supply, consumption, active, inactiveReasonId);
            public void SetFacilities(IReadOnlyList<OutpostFacilityReadModel> facilities) => view.SetFacilities(facilities);
            public void SetCargo(string playerCargo, string storageCargo) => view.SetCargo(playerCargo, storageCargo);
            public void SetSettlementCargo(string cargo) => view.SetSettlementCargo(cargo);
            public void SetCheckpoint(string checkpoint) => view.SetCheckpoint(checkpoint);
            public void SetSelectedMineral(string summary) => view.SetSelectedMineral(summary);
            public void SetMineralOptions(IReadOnlyList<OutpostMineralOption> options, string selectedMineralId) { }
            public void ClearMineralSearch() { }
            public void SetResult(string message, bool isError) => view.SetResult(message, isError);
            public void ShowTemporaryMessage(string message, float durationSeconds) { }
            public void SetTutorialVisible(bool visible) => view.SetTutorialVisible(visible);
            public void SetBusy(bool busy) => view.SetBusy(busy);
        }

        private static SubTerra.Shared.OutpostStatusDto CoreStatus(bool inRange, params string[] facilityIds)
        {
            var status = new SubTerra.Shared.OutpostStatusDto
            {
                outpostInstanceId = "core-0001",
                isActive = true,
                isInInteractionRange = inRange,
                interactionFacilityInstanceId = "core-0001",
                interactionFacilityBuildingId = DataIds.Buildings.OutpostCoreBasic,
                totalPowerSupply = 10f,
                totalPowerConsumption = 3f,
                connectedFacilities = new List<SubTerra.Shared.ConnectedFacilityStatusDto>()
            };
            foreach (var id in facilityIds)
            {
                status.connectedFacilities.Add(new SubTerra.Shared.ConnectedFacilityStatusDto
                {
                    instanceId = id,
                    buildingId = id.StartsWith("clinic") ? DataIds.Buildings.ClinicBasic : DataIds.Buildings.ChargerBasic,
                    isActive = true,
                    inactiveReasonId = string.Empty
                });
            }

            return status;
        }

        private OutpostPanelView Spawn()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            return instance.GetComponent<OutpostPanelView>();
        }

        private static CoreCctvPopupView OpenWith(OutpostPanelView view, IReadOnlyList<OutpostFacilityReadModel> facilities)
        {
            view.SetMode(OutpostPanelMode.Core);
            view.SetVisible(true);
            view.SetFacilities(facilities);
            return view.CorePopup;
        }

        private static OutpostFacilityReadModel Facility(string instanceId, string buildingId, bool active)
        {
            return new OutpostFacilityReadModel(instanceId, buildingId, active, active ? string.Empty : "power_disconnected");
        }

        private static OutpostFacilityReadModel[] Three()
        {
            return new[]
            {
                Facility("charger-0001", DataIds.Buildings.ChargerBasic, true),
                Facility("charger-0002", DataIds.Buildings.ChargerBasic, true),
                Facility("clinic-0001", DataIds.Buildings.ClinicBasic, true)
            };
        }

        /// <summary>안정된 순서에서 i번째가 항상 같은 시설이 되도록 충전기·보건소를 번갈아 만든다.</summary>
        private static List<OutpostFacilityReadModel> Facilities(int count)
        {
            var result = new List<OutpostFacilityReadModel>();
            for (var i = 0; i < count; i++)
            {
                var charger = i % 2 == 0;
                result.Add(Facility(
                    (charger ? "charger-" : "clinic-") + i.ToString("D4"),
                    charger ? DataIds.Buildings.ChargerBasic : DataIds.Buildings.ClinicBasic,
                    true));
            }

            return result;
        }

        private static string[] Ids(CoreCctvFacilityList list)
        {
            var ids = new string[list.Count];
            for (var i = 0; i < ids.Length; i++)
            {
                ids[i] = list.Items[i].InstanceId;
            }

            return ids;
        }
    }
}
