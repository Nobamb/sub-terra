using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.UI;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using T = SubTerra.App.UI.SurfaceBase.UpgradeWindowMotionTimeline;

namespace SubTerra.App.Tests.UI
{
    /// <summary>B-167 업그레이드 창 자물쇠 열기·닫기 연출.</summary>
    public sealed class PromptB167UpgradeWindowMotionTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        private const float Eps = 0.0001f;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private bool previousReduceMotion;
        private readonly List<GameObject> spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            previousReduceMotion = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
            UpgradeWindowMotion.ResetSessionForTests();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            spawned.Clear();
            AccessibilityPreferences.ReduceMotion = previousReduceMotion;
            UpgradeWindowMotion.ResetSessionForTests();
        }

        // ---------- 시간표 ----------

        [Test]
        public void Timeline_EndsFullyOpenOrClosedAtEachDuration()
        {
            foreach (var full in new[] { true, false })
            {
                var open = T.Open(T.OpenDuration(full), full);
                Assert.That(open.Expand, Is.EqualTo(1f).Within(Eps));
                Assert.That(open.WindowAlpha, Is.EqualTo(1f).Within(Eps));
                Assert.That(open.Dim, Is.EqualTo(1f).Within(Eps));
                Assert.That(open.LockAlpha, Is.EqualTo(0f).Within(Eps));
                Assert.That(open.ShardAlpha, Is.EqualTo(0f).Within(Eps));

                var closed = T.Close(T.CloseDuration, open);
                Assert.That(closed.Expand, Is.EqualTo(0f).Within(Eps));
                Assert.That(closed.WindowAlpha, Is.EqualTo(0f).Within(Eps));
                Assert.That(closed.Dim, Is.EqualTo(0f).Within(Eps));
                Assert.That(closed.LockAlpha, Is.EqualTo(0f).Within(Eps));
                Assert.That(closed.ShardAlpha, Is.EqualTo(0f).Within(Eps));
            }

            Assert.That(T.FullOpenDuration, Is.EqualTo(1.2f));
            Assert.That(T.QuickOpenDuration, Is.EqualTo(0.5f));
            Assert.That(T.CloseDuration, Is.EqualTo(0.3f));
        }

        [Test]
        public void Timeline_OnlyFirstOpenShakes()
        {
            var fullMax = 0f;
            for (var t = 0f; t <= T.FullOpenDuration; t += 0.005f)
            {
                fullMax = Mathf.Max(fullMax, Mathf.Abs(T.Open(t, true).LockAngle));
            }

            Assert.That(fullMax, Is.GreaterThan(3f), "처음 열기는 흔들린다.");
            var openEnd = T.Open(T.QuickOpenDuration, false);
            for (var t = 0f; t <= T.QuickOpenDuration; t += 0.005f)
            {
                Assert.That(T.Open(t, false).LockAngle, Is.EqualTo(0f), "다시 열기 t=" + t);
            }

            for (var u = 0f; u <= T.CloseDuration; u += 0.005f)
            {
                Assert.That(T.Close(u, openEnd).LockAngle, Is.EqualTo(0f), "닫기 u=" + u);
            }
        }

        [Test]
        public void Timeline_OrdersCrackShatterExpandAndReverseOnClose()
        {
            foreach (var full in new[] { true, false })
            {
                Assert.That(T.CrackStartTime(full), Is.LessThan(T.ShatterTime(full)));
                Assert.That(T.ShatterTime(full), Is.LessThan(T.ExpandStartTime(full)));
                Assert.That(T.Open(T.CrackStartTime(full) - 0.001f, full).Crack, Is.EqualTo(0f));
                Assert.That(T.Open(T.ShatterTime(full) - 0.001f, full).Shatter, Is.EqualTo(0f));
                Assert.That(T.Open(T.ExpandStartTime(full) - 0.001f, full).Expand, Is.EqualTo(0f));
            }

            Assert.That(T.ShakeStartTime, Is.LessThan(T.FullCrackStartTime));
            Assert.That(T.CloseFoldEndTime, Is.LessThanOrEqualTo(T.RegatherTime));
            Assert.That(T.RegatherTime, Is.LessThan(T.CrackClearTime));
            Assert.That(T.CrackClearTime, Is.LessThan(T.CloseDuration));

            var from = T.Open(T.FullOpenDuration, true);
            Assert.That(T.Close(T.CloseFoldEndTime, from).Expand, Is.EqualTo(0f).Within(Eps));
            Assert.That(T.Close(T.CloseFoldEndTime, from).Shatter, Is.GreaterThan(0.5f), "접힘이 끝날 때 조각은 아직 흩어져 있다.");
            Assert.That(T.Close(T.RegatherTime, from).Shatter, Is.EqualTo(0f).Within(Eps));
            Assert.That(T.Close(T.RegatherTime, from).Crack, Is.GreaterThan(0.5f), "조각이 모일 때 금은 남아 있다.");
            Assert.That(T.Close(T.CrackClearTime, from).Crack, Is.EqualTo(0f).Within(Eps));
            Assert.That(T.Close(T.CrackClearTime, from).LockAlpha, Is.GreaterThan(0.5f), "금이 사라질 때 자물쇠는 남아 있다.");
        }

        [Test]
        public void Timeline_CloseNeverGrowsAndStartsFromCurrentValues()
        {
            foreach (var full in new[] { true, false })
            {
                for (var t = 0f; t <= T.OpenDuration(full); t += 0.03f)
                {
                    var from = T.Open(t, full);
                    AssertSameShape(T.Close(0f, from), from, "닫기 시작 t=" + t);
                    for (var u = 0f; u <= T.CloseDuration; u += 0.02f)
                    {
                        var f = T.Close(u, from);
                        var at = " t=" + t + " u=" + u;
                        Assert.That(f.Expand, Is.LessThanOrEqualTo(from.Expand + Eps), "Expand" + at);
                        Assert.That(f.WindowAlpha, Is.LessThanOrEqualTo(from.WindowAlpha + Eps), "WindowAlpha" + at);
                        Assert.That(f.Dim, Is.LessThanOrEqualTo(from.Dim + Eps), "Dim" + at);
                        Assert.That(f.Presence, Is.LessThanOrEqualTo(from.Presence + Eps), "Presence" + at);
                        Assert.That(f.Crack, Is.LessThanOrEqualTo(from.Crack + Eps), "Crack" + at);
                        Assert.That(f.Shatter, Is.LessThanOrEqualTo(from.Shatter + Eps), "Shatter" + at);
                        Assert.That(f.LockScale, Is.LessThanOrEqualTo(from.LockScale + Eps), "LockScale" + at);
                        Assert.That(f.ShackleLift, Is.LessThanOrEqualTo(from.ShackleLift + Eps), "ShackleLift" + at);
                    }
                }
            }
        }

        [Test]
        public void Timeline_ShardsUseSameFixedPathForOpenAndClose()
        {
            var openEnd = T.Open(T.FullOpenDuration, true);
            for (var i = 0; i < T.ShardCount; i++)
            {
                T.ShardPose(i, 0f, out var start, out var startAngle);
                Assert.That(Vector2.Distance(start, T.ShardStart(i) * T.LockUnit), Is.LessThan(Eps));
                Assert.That(startAngle, Is.EqualTo(0f));
                T.ShardPose(i, 1f, out var end, out _);
                Assert.That(Vector2.Distance(start, end), Is.GreaterThan(100f), "조각은 바깥으로 흩어진다.");
            }

            // 같은 부서짐 진행이면 열기·닫기 모두 같은 퍼짐(= 같은 위치)이다.
            for (var u = 0f; u <= T.CloseDuration; u += 0.02f)
            {
                var close = T.Close(u, openEnd);
                Assert.That(close.ShardSpread, Is.EqualTo(T.EaseOut(close.Shatter)).Within(Eps));
            }

            for (var t = 0f; t <= T.FullOpenDuration; t += 0.02f)
            {
                var open = T.Open(t, true);
                Assert.That(open.ShardSpread, Is.EqualTo(T.EaseOut(open.Shatter)).Within(Eps));
            }

            T.ShardPose(3, 0.42f, out var a, out var angleA);
            T.ShardPose(3, 0.42f, out var b, out var angleB);
            Assert.That(a, Is.EqualTo(b));
            Assert.That(angleA, Is.EqualTo(angleB));
        }

        [Test]
        public void Timeline_LockIsLargeAndNeverFallsLikeNodeLock()
        {
            Assert.That(T.LockSize.x, Is.GreaterThanOrEqualTo(30f * 3f));
            Assert.That(T.LockSize.y, Is.GreaterThanOrEqualTo(34f * 3f));
            foreach (var full in new[] { true, false })
            {
                var previous = T.Open(0f, full).LockY;
                for (var t = 0f; t <= T.OpenDuration(full); t += 0.005f)
                {
                    var y = T.Open(t, full).LockY;
                    Assert.That(y, Is.GreaterThanOrEqualTo(previous - Eps), "열기 중 자물쇠가 내려감 t=" + t);
                    previous = y;
                }

                var from = T.Open(T.OpenDuration(full), full);
                for (var u = 0f; u <= T.CloseDuration; u += 0.01f)
                {
                    Assert.That(T.Close(u, from).LockY, Is.EqualTo(from.LockY), "닫기 중 자물쇠가 내려감 u=" + u);
                }
            }
        }

        // ---------- 컴포넌트 ----------

        [Test]
        public void Motion_FirstOpenIsFullThenReopenIsQuick()
        {
            var motion = CreateRig(out _, out _);
            motion.BeginOpen();
            Assert.That(motion.IsFullOpen, Is.True);
            Run(motion, 1.3f);
            Assert.That(motion.IsSettled, Is.True);
            motion.BeginClose(null);
            Run(motion, 0.4f);
            Assert.That(motion.State, Is.EqualTo(UpgradeWindowMotion.MotionState.Hidden));

            motion.BeginOpen();
            Assert.That(motion.IsFullOpen, Is.False);
            Run(motion, 0.45f);
            Assert.That(motion.IsOpening, Is.True);
            Run(motion, 0.1f);
            Assert.That(motion.IsSettled, Is.True);
        }

        [Test]
        public void Motion_LocksCardDuringMotionAndRestoresExactValues()
        {
            var motion = CreateRig(out var card, out var dim);
            card.localScale = new Vector3(0.9f, 0.9f, 1f);
            var dimAlpha = dim.color.a;

            motion.BeginOpen();
            var group = card.GetComponent<CanvasGroup>();
            Assert.That(group, Is.Not.Null);
            Assert.That(group.interactable, Is.False);
            Assert.That(card.sizeDelta, Is.EqualTo(new Vector2(1500f, 820f)), "크기는 sizeDelta가 아니라 scale로만 움직인다.");
            Run(motion, 0.9f);
            Assert.That(group.interactable, Is.False);
            Assert.That(motion.EffectsVisible, Is.True);
            Run(motion, 0.4f);
            AssertRestored(card, group, dim, dimAlpha, 0.9f);
            Assert.That(motion.EffectsVisible, Is.False);

            // 연타 20번(여는 중 닫기·닫는 중 열기) 뒤에도 원래 값으로 돌아온다.
            var random = new System.Random(167);
            for (var i = 0; i < 20; i++)
            {
                if (i % 2 == 0) motion.BeginClose(null);
                else motion.BeginOpen();
                motion.Tick((float)random.NextDouble() * 0.2f);
            }

            motion.BeginOpen();
            Run(motion, 1.3f);
            Assert.That(motion.IsSettled, Is.True);
            AssertRestored(card, group, dim, dimAlpha, 0.9f);
            Assert.That(card.sizeDelta, Is.EqualTo(new Vector2(1500f, 820f)));
        }

        [Test]
        public void Motion_RaisesHooksOnceInOrder()
        {
            var motion = CreateRig(out _, out _);
            var log = Record(motion);
            motion.BeginOpen();
            Run(motion, 1.3f);
            Assert.That(log, Is.EqualTo(new[] { "Shake", "Crack", "Shattered", "OpenCompleted" }));

            log.Clear();
            motion.BeginClose(null);
            Run(motion, 0.4f);
            Assert.That(log, Is.EqualTo(new[] { "CloseStarted", "Regathered", "CloseCompleted" }));

            log.Clear();
            motion.BeginOpen();
            Run(motion, 0.6f);
            Assert.That(log, Is.EqualTo(new[] { "Crack", "Shattered", "OpenCompleted" }), "다시 열기에는 흔들림 훅이 없다.");
        }

        [Test]
        public void Motion_LargeFrameSkipStillRaisesEachHookOnce()
        {
            var motion = CreateRig(out _, out _);
            var log = Record(motion);
            motion.BeginOpen();
            motion.Tick(0.6f);
            motion.Tick(0.6f);
            motion.Tick(0.6f);
            Assert.That(log, Is.EqualTo(new[] { "Shake", "Crack", "Shattered", "OpenCompleted" }));
        }

        [Test]
        public void Motion_ReopenWhileClosingDoesNotRepeatPassedHooks()
        {
            var motion = CreateRig(out _, out _);
            motion.BeginOpen();
            Run(motion, 1.3f);
            var log = Record(motion);
            motion.BeginClose(null);
            Run(motion, 0.05f);
            motion.BeginOpen();
            Run(motion, 0.6f);
            Assert.That(log, Is.EqualTo(new[] { "CloseStarted", "OpenCompleted" }));
        }

        [Test]
        public void Motion_ToggleMidwayContinuesFromCurrentShapeWithoutJump()
        {
            var motion = CreateRig(out var card, out _);
            motion.BeginOpen();
            Run(motion, 1.3f);
            var group = card.GetComponent<CanvasGroup>();
            foreach (var closeFor in new[] { 0.03f, 0.08f, 0.15f, 0.24f })
            {
                motion.BeginClose(null);
                Run(motion, closeFor);
                AssertSmoothSteps(motion, card, group, "닫는 중 열기 " + closeFor, open: true);
                Assert.That(motion.IsSettled, Is.True);
            }

            foreach (var openFor in new[] { 0.05f, 0.2f, 0.35f })
            {
                motion.BeginClose(null);
                Run(motion, 0.4f);
                motion.BeginOpen();
                Run(motion, openFor);
                AssertSmoothSteps(motion, card, group, "여는 중 닫기 " + openFor, open: false);
                Assert.That(motion.State, Is.EqualTo(UpgradeWindowMotion.MotionState.Hidden));
                motion.BeginOpen();
                Run(motion, 0.6f);
            }
        }

        [Test]
        public void Motion_ReduceMotionOpensAndClosesImmediately()
        {
            var motion = CreateRig(out var card, out var dim);
            var dimAlpha = dim.color.a;
            var log = Record(motion);
            AccessibilityPreferences.ReduceMotion = true;
            motion.BeginOpen();
            Assert.That(motion.IsSettled, Is.True);
            Assert.That(motion.EffectsVisible, Is.False);
            AssertRestored(card, card.GetComponent<CanvasGroup>(), dim, dimAlpha);
            var closed = false;
            motion.BeginClose(() => closed = true);
            Assert.That(closed, Is.True);
            Assert.That(log, Is.EqualTo(new[] { "OpenCompleted", "CloseCompleted" }));

            // 연출 중에 켜지면 바로 끝 상태로.
            AccessibilityPreferences.ReduceMotion = false;
            log.Clear();
            motion.BeginOpen();
            Run(motion, 0.1f);
            AccessibilityPreferences.ReduceMotion = true;
            motion.Tick(0.016f);
            Assert.That(motion.IsSettled, Is.True);
            Assert.That(log, Is.EqualTo(new[] { "OpenCompleted" }));
        }

        // ---------- SurfaceBase 연결 ----------

        [Test]
        public void SurfaceView_RequestCloseKeepsRootUntilMotionEnds()
        {
            var instance = Spawn();
            var view = instance.GetComponent<SurfaceBaseView>();
            var modal = instance.transform.Find("SurfaceBaseContent/UpgradeModal").gameObject;
            view.RequestUpgradeOpen();
            var motion = view.UpgradeMotion;
            Assert.That(motion.IsOpening, Is.True);
            Assert.That(view.IsUpgradeVisible, Is.True);
            Run(motion, 1.3f);
            Assert.That(motion.IsSettled, Is.True);

            view.RequestUpgradeClose();
            Assert.That(motion.IsClosing, Is.True);
            Assert.That(view.IsUpgradeVisible, Is.False, "닫는 중에는 이미 닫힌 것으로 본다.");
            Assert.That(modal.activeSelf, Is.True, "루트는 닫기 연출이 끝난 뒤 꺼진다.");
            Run(motion, 0.4f);
            Assert.That(modal.activeSelf, Is.False);

            // 즉시 함수는 연출 없이 켜고 끈다.
            view.SetUpgradeVisible(true);
            Assert.That(view.IsUpgradeVisible, Is.True);
            Assert.That(motion.IsSettled, Is.True);
            view.RequestUpgradeClose();
            view.SetSettingsVisible(true);
            Assert.That(modal.activeSelf, Is.False, "설정 창은 닫는 중인 업그레이드 창도 바로 정리한다.");
            Assert.That(motion.State, Is.EqualTo(UpgradeWindowMotion.MotionState.Hidden));
            view.SetSettingsVisible(false);
        }

        [Test]
        public void SurfaceView_RefreshAndPurchaseDoNotReplayMotion()
        {
            var instance = Spawn();
            var view = instance.GetComponent<SurfaceBaseView>();
            view.RequestUpgradeOpen();
            var motion = view.UpgradeMotion;
            Run(motion, 1.3f);
            var progression = instance.GetComponentInChildren<ProgressionPanelView>(true);
            progression.SetPurchaseResult("구매 완료", string.Empty);
            progression.SetDeepZoneAccess(new SubTerra.App.Progression.ZoneAccessResult(false, false, string.Empty));
            view.RequestUpgradeOpen();
            Assert.That(motion.IsSettled, Is.True);
            Assert.That(motion.EffectsVisible, Is.False);
            Assert.That(motion.Card.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void SurfaceView_DeepZoneRevealWaitsUntilOpenCompletes()
        {
            var instance = Spawn();
            var view = instance.GetComponent<SurfaceBaseView>();
            var progression = instance.GetComponentInChildren<ProgressionPanelView>(true);
            var blocked = typeof(ProgressionPanelView).GetMethod("IsDeepZoneRevealBlocked", Private);
            Assert.That(blocked, Is.Not.Null);
            view.RequestUpgradeOpen();
            Assert.That((bool)blocked.Invoke(progression, null), Is.True);
            Run(view.UpgradeMotion, 1.3f);
            Assert.That((bool)blocked.Invoke(progression, null), Is.False);
        }

        [Test]
        public void SurfaceBinder_ButtonEscXKeyAndCloseButtonUseSameRequests()
        {
            var instance = Spawn();
            var view = instance.GetComponent<SurfaceBaseView>();
            var host = new GameObject("B167Binder");
            spawned.Add(host);
            var binder = host.AddComponent<SurfaceBaseBinder>();
            typeof(SurfaceBaseBinder).GetField("view", Private).SetValue(binder, view);
            typeof(SurfaceBaseBinder).GetField("progressionBinder", Private)
                .SetValue(binder, instance.GetComponentInChildren<ProgressionPanelBinder>(true));
            typeof(SurfaceBaseBinder).GetField("settings", Private)
                .SetValue(binder, new SubTerra.App.UI.MainMenu.SettingsSession());
            typeof(SurfaceBaseView).GetMethod("OnEnable", Private).Invoke(view, null);
            view.UpgradeCloseClicked += (Action)Delegate.CreateDelegate(typeof(Action), binder,
                typeof(SurfaceBaseBinder).GetMethod("OnUpgradeCloseClicked", Private));

            // 업그레이드 버튼 경로.
            typeof(SurfaceBaseBinder).GetMethod("OnUpgradeClicked", Private).Invoke(binder, null);
            var motion = view.UpgradeMotion;
            Assert.That(motion.IsOpening, Is.True);
            Run(motion, 1.3f);

            // 창 X 버튼 경로.
            instance.transform.Find("SurfaceBaseContent/UpgradeModal/ProgressionPanel/CloseUpgradeButton")
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(motion.IsClosing, Is.True);
            Run(motion, 0.4f);

            WithKeyboard(keyboard =>
            {
                foreach (var key in new[] { Key.Escape, Key.X })
                {
                    view.RequestUpgradeOpen();
                    Run(motion, 0.6f);
                    Assert.That(motion.IsSettled, Is.True);
                    Press(keyboard, key);
                    typeof(SurfaceBaseBinder).GetMethod("Update", Private).Invoke(binder, null);
                    Assert.That(motion.IsClosing, Is.True, key + " 키는 닫기 연출 경로를 탄다.");
                    Run(motion, 0.4f);
                }
            });
        }

        // ---------- 광산(PanelToggleController) ----------

        [Test]
        public void MinePanel_UOpensWithSameMotion_ClosesAfterMotion_AndKeepsWindowBackground()
        {
            var panels = CreateMineRig(out var layout, out var card, out var background);
            var backgroundColor = background.color;
            Assert.That(card.gameObject.activeSelf, Is.False, "시작 숨김은 연출 없이 바로 꺼진다.");

            panels.ToggleUpgrade();
            var motion = card.GetComponent<UpgradeWindowMotion>();
            motion.ManualTick = true;
            Assert.That(panels.IsVisible(RuntimePanelId.Upgrade), Is.True);
            Assert.That(card.gameObject.activeSelf, Is.True);
            Assert.That(motion.IsOpening, Is.True);
            Assert.That(motion.IsFullOpen, Is.True);
            Assert.That(motion.Card, Is.SameAs(card));
            Assert.That(motion.EffectsRoot.parent, Is.SameAs(layout), "자물쇠는 카드 scale에 같이 줄지 않도록 카드 옆에 둔다.");
            Assert.That(background.color, Is.EqualTo(backgroundColor), "광산 창 배경 Image를 딤으로 쓰지 않는다.");
            Run(motion, 0.9f);
            Assert.That(motion.EffectsVisible, Is.True);
            Assert.That(card.localScale.x, Is.LessThan(1f));
            Assert.That(card.GetComponent<CanvasGroup>().interactable, Is.False);
            Run(motion, 0.4f);
            Assert.That(motion.IsSettled, Is.True);
            Assert.That(card.localScale, Is.EqualTo(Vector3.one));
            Assert.That(card.GetComponent<CanvasGroup>().interactable, Is.True);
            Assert.That(background.color, Is.EqualTo(backgroundColor));

            panels.CloseUpgrade();
            Assert.That(panels.IsVisible(RuntimePanelId.Upgrade), Is.False, "닫기 요청 즉시 닫힌 것으로 본다.");
            Assert.That(card.gameObject.activeSelf, Is.True, "창은 닫기 연출이 끝난 뒤 꺼진다.");
            Assert.That(PopupWindowSorting.Top, Is.Not.SameAs(card.GetComponent<Canvas>()), "닫는 중인 창은 X키 맨 위 창이 아니다.");
            Run(motion, 0.15f);
            panels.ToggleUpgrade();
            Assert.That(motion.IsOpening, Is.True, "닫는 중 다시 열면 이어서 연다.");
            Run(motion, 0.6f);
            Assert.That(motion.IsSettled, Is.True);

            panels.CloseUpgrade();
            Run(motion, 0.4f);
            Assert.That(card.gameObject.activeSelf, Is.False);
            Assert.That(motion.EffectsVisible, Is.False);

            panels.ToggleUpgrade();
            Assert.That(motion.IsFullOpen, Is.False, "두 번째부터는 다시 열기 규칙이다.");
            Run(motion, 0.6f);
            Assert.That(motion.IsSettled, Is.True);
        }

        private PanelToggleController CreateMineRig(out RectTransform layout, out RectTransform card, out Image background)
        {
            var layoutObject = new GameObject("PanelLayout", typeof(RectTransform));
            spawned.Add(layoutObject);
            layout = (RectTransform)layoutObject.transform;
            var cardObject = new GameObject("UpgradePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Canvas), typeof(PopupWindowDrag));
            cardObject.transform.SetParent(layout, false);
            card = (RectTransform)cardObject.transform;
            card.sizeDelta = new Vector2(1500f, 820f);
            background = cardObject.GetComponent<Image>();
            background.color = new Color(0.06f, 0.16f, 0.19f, 1f);

            var panels = layoutObject.AddComponent<PanelToggleController>();
            var referenceType = typeof(PanelToggleController).GetNestedType("PanelReference", BindingFlags.NonPublic);
            var reference = Activator.CreateInstance(referenceType, true);
            referenceType.GetField("panelId", Private).SetValue(reference, RuntimePanelId.Upgrade);
            referenceType.GetField("panelRoot", Private).SetValue(reference, cardObject);
            var array = Array.CreateInstance(referenceType, 1);
            array.SetValue(reference, 0);
            typeof(PanelToggleController).GetField("panels", Private).SetValue(panels, array);
            typeof(PanelToggleController).GetMethod("Awake", Private).Invoke(panels, null);
            return panels;
        }

        // ---------- 도우미 ----------

        private UpgradeWindowMotion CreateRig(out RectTransform card, out Image dim)
        {
            var root = new GameObject("UpgradeModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Canvas));
            spawned.Add(root);
            dim = root.GetComponent<Image>();
            dim.color = new Color(0.005f, 0.02f, 0.035f, 0.88f);
            var cardObject = new GameObject("ProgressionPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(PopupWindowDrag));
            cardObject.transform.SetParent(root.transform, false);
            card = (RectTransform)cardObject.transform;
            card.sizeDelta = new Vector2(1500f, 820f);
            var motion = root.AddComponent<UpgradeWindowMotion>();
            motion.ManualTick = true;
            return motion;
        }

        private GameObject Spawn()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            spawned.Add(instance);
            return instance;
        }

        private static void Run(UpgradeWindowMotion motion, float seconds)
        {
            motion.ManualTick = true;
            for (var t = 0f; t < seconds; t += 0.016f)
            {
                motion.Tick(0.016f);
            }
        }

        private static List<string> Record(UpgradeWindowMotion motion)
        {
            var log = new List<string>();
            motion.ShakeStarted += () => log.Add("Shake");
            motion.CrackStarted += () => log.Add("Crack");
            motion.Shattered += () => log.Add("Shattered");
            motion.OpenCompleted += () => log.Add("OpenCompleted");
            motion.CloseStarted += () => log.Add("CloseStarted");
            motion.FragmentsRegathered += () => log.Add("Regathered");
            motion.CloseCompleted += () => log.Add("CloseCompleted");
            return log;
        }

        private static void AssertRestored(RectTransform card, CanvasGroup group, Image dim, float dimAlpha, float scale = 1f)
        {
            Assert.That(Vector3.Distance(card.localScale, new Vector3(scale, scale, 1f)), Is.LessThan(Eps));
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(group.interactable, Is.True);
            Assert.That(group.blocksRaycasts, Is.True);
            Assert.That(dim.color.a, Is.EqualTo(dimAlpha).Within(Eps));
        }

        // 방향을 바꾼 그 순간 모양이 그대로이고(튀지 않음), 이후에도 한 프레임(16ms)에 크게 건너뛰지 않는다.
        private static void AssertSmoothSteps(UpgradeWindowMotion motion, RectTransform card, CanvasGroup group, string label, bool open)
        {
            var scale = card.localScale.x;
            var alpha = group.alpha;
            var shatter = motion.Frame.Shatter;
            if (open) motion.BeginOpen();
            else motion.BeginClose(null);
            Assert.That(card.localScale.x, Is.EqualTo(scale).Within(0.01f), label + " 전환 scale");
            Assert.That(group.alpha, Is.EqualTo(alpha).Within(0.01f), label + " 전환 alpha");
            Assert.That(motion.Frame.Shatter, Is.EqualTo(shatter).Within(0.01f), label + " 전환 shatter");
            for (var i = 0; i < 120 && (motion.IsOpening || motion.IsClosing); i++)
            {
                motion.Tick(0.016f);
                if (!motion.IsOpening && !motion.IsClosing) break;
                Assert.That(Mathf.Abs(card.localScale.x - scale), Is.LessThan(0.45f), label + " scale");
                Assert.That(Mathf.Abs(group.alpha - alpha), Is.LessThan(0.45f), label + " alpha");
                scale = card.localScale.x;
                alpha = group.alpha;
            }
        }

        private static void AssertSameShape(UpgradeWindowFrame a, UpgradeWindowFrame b, string message)
        {
            Assert.That(a.Expand, Is.EqualTo(b.Expand).Within(Eps), message);
            Assert.That(a.WindowAlpha, Is.EqualTo(b.WindowAlpha).Within(Eps), message);
            Assert.That(a.Dim, Is.EqualTo(b.Dim).Within(Eps), message);
            Assert.That(a.Presence, Is.EqualTo(b.Presence).Within(Eps), message);
            Assert.That(a.Crack, Is.EqualTo(b.Crack).Within(Eps), message);
            Assert.That(a.Shatter, Is.EqualTo(b.Shatter).Within(Eps), message);
            Assert.That(a.LockScale, Is.EqualTo(b.LockScale).Within(Eps), message);
            Assert.That(a.LockAngle, Is.EqualTo(b.LockAngle).Within(Eps), message);
            Assert.That(a.LockAlpha, Is.EqualTo(b.LockAlpha).Within(Eps), message);
        }

        private static void WithKeyboard(Action<Keyboard> body)
        {
            const string editModeInput = "RUN_PLAYER_UPDATES_IN_EDIT_MODE";
            var inputSettings = InputSystem.settings;
            var hadEditModeInput = (bool)inputSettings.GetType()
                .GetMethod("IsFeatureEnabled", Private)
                .Invoke(inputSettings, new object[] { editModeInput });
            inputSettings.SetInternalFeatureFlag(editModeInput, true);
            var previousKeyboard = Keyboard.current;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                body(keyboard);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                if (previousKeyboard != null) previousKeyboard.MakeCurrent();
                inputSettings.SetInternalFeatureFlag(editModeInput, hadEditModeInput);
            }
        }

        private static void Press(Keyboard keyboard, Key key)
        {
            _ = keyboard[key].wasPressedThisFrame;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            InputSystem.Update();
            keyboard.MakeCurrent();
            Assert.That(keyboard[key].wasPressedThisFrame, Is.True, key + " 눌림이 전달되어야 한다.");
        }
    }
}
