using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Progression;
using SubTerra.App.UI;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.UI
{
    /// <summary>B-169: 업그레이드 노드 해금 연출·소리, 심층 썸네일 해금 소리, 창 TV 켜짐·꺼짐, 업그레이드 창 자물쇠 소리.</summary>
    public sealed partial class PromptB169UnlockAndTvTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float Frame = 0.016f;

        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly List<Object> created = new List<Object>();
        private readonly List<SfxId> requests = new List<SfxId>();
        private bool previousReduceMotion;
        private float now;

        [SetUp]
        public void SetUp()
        {
            requests.Clear();
            now = 100f;
            previousReduceMotion = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
            UpgradeWindowMotion.ResetSessionForTests();
            SfxRequests.PlayRequested += Record;
            WindowTvSfx.ResetForTests(Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath), () => now, () => false);
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            WindowTvSfx.ReleaseForTests();
            foreach (var go in spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            foreach (var asset in created)
            {
                if (asset != null) Object.DestroyImmediate(asset);
            }

            spawned.Clear();
            created.Clear();
            requests.Clear();
            AccessibilityPreferences.ReduceMotion = previousReduceMotion;
            UpgradeWindowMotion.ResetSessionForTests();
        }

        private void Record(SfxId id, float pitch) => requests.Add(id);

        private int Count(SfxId id)
        {
            var count = 0;
            for (var i = 0; i < requests.Count; i++)
            {
                if (requests[i] == id) count++;
            }

            return count;
        }

        // ---------- A-1 해금 대기 추적 ----------

        [Test]
        public void Tracker_FirstDrawAndReset_NoPending_LockedToUnlocked_PendsOnce()
        {
            var tracker = new UpgradeUnlockRevealTracker();
            Assert.That(tracker.Observe("a", false), Is.False);
            Assert.That(tracker.Observe("b", true), Is.False, "처음 그릴 때 풀려 있으면 연출 없음");
            Assert.That(tracker.HasPending, Is.False);

            Assert.That(tracker.Observe("a", true), Is.True, "그린 뒤 잠김 → 풀림은 대기");
            Assert.That(tracker.PendingCount, Is.EqualTo(1));
            Assert.That(tracker.Observe("a", true), Is.True, "다시 그려도 대기 유지, 중복 없음");
            Assert.That(tracker.PendingCount, Is.EqualTo(1));
            Assert.That(tracker.Observe("b", true), Is.False);

            Assert.That(tracker.MarkStarted("a"), Is.True);
            Assert.That(tracker.Observe("a", true), Is.False, "재생을 시작한 노드는 다시 대기가 되지 않음");
            Assert.That(tracker.PendingCount, Is.EqualTo(0));

            // 불러오기 직후: 다음 그리기를 첫 그리기로 본다.
            tracker.Observe("c", false);
            tracker.Reset();
            Assert.That(tracker.Observe("c", true), Is.False);
            Assert.That(tracker.HasPending, Is.False);
        }

        [Test]
        public void Tree_UnlockWhileClosed_PlaysOnceOnNextOpen_ThenNotAgain()
        {
            var instance = Spawn();
            var view = instance.GetComponent<SurfaceBaseView>();
            var tree = instance.GetComponentInChildren<UpgradeTreeView>(true);
            tree.ManualTick = true;
            var a = tree.Nodes[0];
            var b = tree.Nodes[1];
            a.ManualTick = b.ManualTick = true;

            tree.Render(new[] { Snapshot(a.UpgradeId, false), Snapshot(b.UpgradeId, false) });
            tree.Render(new[] { Snapshot(a.UpgradeId, true), Snapshot(b.UpgradeId, false) });
            Assert.That(tree.UnlockTracker.PendingCount, Is.EqualTo(1));
            Assert.That(a.IsRevealPending, Is.True);
            Assert.That(a.BlindAlpha, Is.EqualTo(1f), "대기 중 Apply(풀림) 뒤에도 블라인드 유지");
            Assert.That(a.IsLockVisible, Is.True, "대기 중 자물쇠 유지");

            tree.TickUnlockReveal(now);
            Assert.That(tree.ScheduledUnlockCount, Is.EqualTo(0), "창이 닫힌 동안은 시작하지 않음");

            view.SetUpgradeVisible(true);
            tree.TickUnlockReveal(now);
            Assert.That(tree.ScheduledUnlockCount, Is.EqualTo(1));
            Assert.That(a.IsUnlockPlaying, Is.False, "커넥터 흐름이 먼저");
            tree.TickUnlockReveal(now + 0.23f);
            Assert.That(a.IsUnlockPlaying, Is.True);
            Assert.That(tree.UnlockTracker.PendingCount, Is.EqualTo(0));

            StepNode(a, 1.05f);
            Assert.That(a.IsUnlockPlaying, Is.False);
            Assert.That(a.BlindAlpha, Is.EqualTo(0f));
            Assert.That(a.IsLockVisible, Is.False);
            Assert.That(a.VisualState, Is.Not.EqualTo(UpgradeNodeVisualState.Locked));

            view.SetUpgradeVisible(false);
            view.SetUpgradeVisible(true);
            tree.Render(new[] { Snapshot(a.UpgradeId, true), Snapshot(b.UpgradeId, false) });
            tree.TickUnlockReveal(now + 5f);
            Assert.That(tree.ScheduledUnlockCount, Is.EqualTo(0), "재생 뒤 다시 열면 대기 0");
            Assert.That(a.IsUnlockPlaying, Is.False);
        }

        [Test]
        public void Tree_WaitsForOpenMotion_AndClosingDuringOpenKeepsPending()
        {
            var instance = Spawn();
            var view = instance.GetComponent<SurfaceBaseView>();
            var tree = instance.GetComponentInChildren<UpgradeTreeView>(true);
            tree.ManualTick = true;
            var a = tree.Nodes[0];
            a.ManualTick = true;
            tree.Render(new[] { Snapshot(a.UpgradeId, false) });
            tree.Render(new[] { Snapshot(a.UpgradeId, true) });

            view.RequestUpgradeOpen();
            var motion = view.UpgradeMotion;
            Run(motion, 0.3f);
            tree.TickUnlockReveal(now);
            Assert.That(tree.ScheduledUnlockCount, Is.EqualTo(0), "열기 연출 중에는 시작하지 않음");

            view.RequestUpgradeClose();
            Run(motion, 0.4f);
            Assert.That(tree.UnlockTracker.PendingCount, Is.EqualTo(1), "열다 닫으면 대기 유지");

            view.RequestUpgradeOpen();
            Run(motion, 0.25f);
            tree.TickUnlockReveal(now);
            Assert.That(tree.ScheduledUnlockCount, Is.EqualTo(0));
            Run(motion, 0.6f);
            Assert.That(motion.IsSettled, Is.True);
            tree.TickUnlockReveal(now);
            tree.TickUnlockReveal(now + 0.23f);
            Assert.That(a.IsUnlockPlaying, Is.True, "열기 연출이 끝난 뒤 재생");
        }

        // ---------- A-2 노드 연출 ----------

        [Test]
        public void Node_PendingKeepsBlindAndLock_ThenFallsAndRevealsInOneSecond()
        {
            var node = OpenTreeNode();
            node.ManualTick = true;
            node.Apply(Snapshot(node.UpgradeId, false));
            node.SetRevealPending(true);
            node.Apply(Snapshot(node.UpgradeId, true));
            Assert.That(node.BlindAlpha, Is.EqualTo(1f));
            Assert.That(node.IsLockVisible, Is.True);

            var lockRoot = FindChild(node.transform, "Lock");
            Assert.That(lockRoot, Is.Not.Null);
            var baseY = lockRoot.anchoredPosition.y;
            node.BeginUnlock(null);
            StepNode(node, 0.07f);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, lockRoot.localEulerAngles.z)), Is.GreaterThan(1f), "흔들림");
            StepNode(node, 0.18f);
            Assert.That(lockRoot.localScale.x, Is.GreaterThan(1f), "딸깍 펄스");
            StepNode(node, 0.3f);
            Assert.That(node.IsLockVisible, Is.True);
            Assert.That(lockRoot.anchoredPosition.y, Is.LessThan(baseY - 5f), "아래로 떨어짐");
            Assert.That(Mathf.DeltaAngle(0f, lockRoot.localEulerAngles.z), Is.LessThan(-5f), "기울며");
            Assert.That(node.BlindAlpha, Is.EqualTo(1f), "떨어지는 동안 블라인드 유지");
            StepNode(node, 0.15f);
            Assert.That(node.IsLockVisible, Is.False, "0.65 뒤 자물쇠 사라짐");
            Assert.That(node.BlindAlpha, Is.LessThan(1f));
            StepNode(node, 0.35f);
            Assert.That(node.IsUnlockPlaying, Is.False);
            Assert.That(node.BlindAlpha, Is.EqualTo(0f));
            Assert.That(node.IsLockVisible, Is.False);
            Assert.That(lockRoot.anchoredPosition.y, Is.EqualTo(baseY).Within(0.001f), "끝나면 자물쇠 자리 복귀");
            foreach (var fragment in node.GetComponentsInChildren<Image>(true))
            {
                if (fragment.name.StartsWith("Fragment"))
                {
                    Assert.That(fragment.gameObject.activeSelf, Is.False, "파편은 쓰지 않음");
                }
            }
        }

        [Test]
        public void Node_DisableMidRevealEndsUnlocked()
        {
            var node = OpenTreeNode();
            node.ManualTick = true;
            node.Apply(Snapshot(node.UpgradeId, false));
            node.SetRevealPending(true);
            node.Apply(Snapshot(node.UpgradeId, true));
            node.BeginUnlock(null);
            StepNode(node, 0.4f);
            typeof(UpgradeTreeNodeView).GetMethod("OnDisable", Private).Invoke(node, null);
            Assert.That(node.IsUnlockPlaying, Is.False);
            Assert.That(node.IsRevealPending, Is.False);
            Assert.That(node.BlindAlpha, Is.EqualTo(0f));
        }

        // ---------- A-3 소리 ----------

        [Test]
        public void Node_CuesRattleClickGlowOnceEach_EvenWhenFramesSkip()
        {
            var node = OpenTreeNode();
            node.ManualTick = true;
            var cues = new List<SfxId>();

            node.BeginUnlock(cues.Add);
            Assert.That(cues, Is.EqualTo(new[] { SfxId.UnlockRattle }), "0.0 Rattle");
            StepNode(node, 0.19f);
            Assert.That(cues.Count, Is.EqualTo(1));
            StepNode(node, 0.03f);
            Assert.That(cues, Is.EqualTo(new[] { SfxId.UnlockRattle, SfxId.UnlockClick }), "0.2 Click");
            StepNode(node, 0.42f);
            Assert.That(cues.Count, Is.EqualTo(2));
            StepNode(node, 0.5f);
            Assert.That(cues, Is.EqualTo(new[] { SfxId.UnlockRattle, SfxId.UnlockClick, SfxId.UnlockGlow }), "0.65 Glow");

            foreach (var skip in new[] { 0.5f, 1f })
            {
                cues.Clear();
                node.BeginUnlock(cues.Add);
                node.StepUnlock(skip);
                node.StepUnlock(skip);
                node.StepUnlock(skip);
                Assert.That(cues, Is.EqualTo(new[] { SfxId.UnlockRattle, SfxId.UnlockClick, SfxId.UnlockGlow }), "프레임 " + skip + "초 건너뛰기");
            }
        }

        [Test]
        public void Tree_FiveNodesUnlockTogether_OnlyThreeMakeSound_StaggeredBy012()
        {
            var instance = Spawn();
            var view = instance.GetComponent<SurfaceBaseView>();
            var tree = instance.GetComponentInChildren<UpgradeTreeView>(true);
            Assert.That(tree.Nodes.Count, Is.GreaterThanOrEqualTo(5));
            tree.ManualTick = true;
            var ids = new string[5];
            for (var i = 0; i < 5; i++)
            {
                ids[i] = tree.Nodes[i].UpgradeId;
                tree.Nodes[i].ManualTick = true;
            }

            tree.Render(Snapshots(ids, false));
            view.SetUpgradeVisible(true);
            tree.Render(Snapshots(ids, true));
            Assert.That(tree.UnlockTracker.PendingCount, Is.EqualTo(5));

            // 프레임 길이를 들쭉날쭉하게 해 0.12초 간격 노드가 프레임 경계로 0.1초 안에 붙어도 소리가 빠지지 않는지 본다.
            var starts = new float[5];
            var start = now;
            var step = 0;
            for (var t = 0f; t < 2.5f; step++)
            {
                var dt = step % 3 == 0 ? 0.05f : 0.011f;
                now = start + t;
                tree.TickUnlockReveal(now);
                for (var i = 0; i < 5; i++)
                {
                    var node = tree.FindNode(ids[i]);
                    if (node.IsUnlockPlaying)
                    {
                        if (starts[i] <= 0f) starts[i] = t;
                        node.StepUnlock(dt);
                    }
                }

                t += dt;
            }

            Assert.That(Count(SfxId.UnlockRattle), Is.EqualTo(3), "소리 내는 노드는 처음 3개");
            Assert.That(Count(SfxId.UnlockClick), Is.EqualTo(3));
            Assert.That(Count(SfxId.UnlockGlow), Is.EqualTo(3));
            for (var i = 1; i < 5; i++)
            {
                Assert.That(starts[i] - starts[i - 1], Is.EqualTo(UpgradeTreeView.UnlockStagger).Within(0.051f), "0.12초 간격 " + i);
            }

            for (var i = 0; i < 5; i++)
            {
                Assert.That(tree.FindNode(ids[i]).BlindAlpha, Is.EqualTo(0f), "연출만 하는 노드도 풀림 " + i);
            }
        }

        [Test]
        public void Tree_PurchaseSuccess_LevelUpOnly_UnlockNotPlayedTwice()
        {
            var instance = Spawn();
            var view = instance.GetComponent<SurfaceBaseView>();
            var tree = instance.GetComponentInChildren<UpgradeTreeView>(true);
            tree.ManualTick = true;
            var a = tree.Nodes[0];
            var b = tree.Nodes[1];
            a.ManualTick = b.ManualTick = true;
            tree.Render(new[] { Snapshot(a.UpgradeId, true), Snapshot(b.UpgradeId, false) });
            view.SetUpgradeVisible(true);

            // 서비스 순서: UpgradeChanged(Render) → PurchaseCompleted(피드백).
            tree.Render(new[] { Snapshot(a.UpgradeId, true), Snapshot(b.UpgradeId, true) });
            tree.PlayPurchaseFeedback(ProgressionPurchaseResult.Success(a.UpgradeId, 0, 1, 1f, new List<string> { b.UpgradeId }));
            Assert.That(requests, Is.EqualTo(new[] { SfxId.UpgradeLevelUp }), "레벨업이 먼저, 해금 소리는 아직");
            Assert.That(b.IsUnlockPlaying, Is.False, "피드백이 직접 재생하지 않음");
            tree.TickUnlockReveal(now);
            tree.TickUnlockReveal(now + 0.23f);
            Assert.That(b.IsUnlockPlaying, Is.True);
            Assert.That(Count(SfxId.UnlockRattle), Is.EqualTo(1));
            tree.TickUnlockReveal(now + 0.5f);
            Assert.That(tree.ScheduledUnlockCount, Is.EqualTo(0));
            Assert.That(Count(SfxId.UnlockRattle), Is.EqualTo(1), "두 번 재생하지 않음");
        }

        // ---------- A-4 심층 썸네일 ----------

        [Test]
        public void DeepZone_SharedPoseEqualsPreviousFormula()
        {
            var style = new DeepZoneThumbnailStyle();
            for (var u = 0f; u < 0.65f; u += 0.0025f)
            {
                var pose = UnlockRevealTimeline.SampleLock(u, style.shackleKick, style.lockDrop, style.lockDropAngle, DeepZoneThumbnailStrip.LockDrift);
                OldThumbnailPose(u, style, out var offset, out var angle, out var scale, out var lift, out var alpha);
                Assert.That(pose.Visible, Is.True, "u=" + u);
                Assert.That(pose.Offset, Is.EqualTo(offset), "offset u=" + u);
                Assert.That(pose.Angle, Is.EqualTo(angle), "angle u=" + u);
                Assert.That(pose.Scale, Is.EqualTo(scale), "scale u=" + u);
                Assert.That(pose.ShackleLift, Is.EqualTo(lift), "lift u=" + u);
                Assert.That(pose.Alpha, Is.EqualTo(alpha), "alpha u=" + u);
            }

            Assert.That(UnlockRevealTimeline.SampleLock(0.65f, 6f, 40f, 25f, 4f).Visible, Is.False);
            Assert.That(UnlockRevealTimeline.SampleLock(1f, 6f, 40f, 25f, 4f).Visible, Is.False);

            // 실제 썸네일 자물쇠 RectTransform도 이전 식과 같다.
            var settings = DeepZoneSettings();
            var strip = NewStrip();
            strip.Apply(settings, new ZoneAccessResult(false, false, "x"));
            var lockRect = (RectTransform)strip.GetCellRect(0).Find("Clip/Lock");
            foreach (var u in new[] { 0.05f, 0.13f, 0.25f, 0.42f, 0.6f })
            {
                strip.SampleReveal(0, u);
                OldThumbnailPose(u, settings.ThumbnailStyle, out var offset, out var angle, out var scale, out _, out _);
                Assert.That(lockRect.anchoredPosition, Is.EqualTo(offset), "u=" + u);
                Assert.That(lockRect.localRotation, Is.EqualTo(Quaternion.Euler(0f, 0f, angle)), "u=" + u);
                Assert.That(lockRect.localScale.x, Is.EqualTo(scale), "u=" + u);
            }

            strip.SampleReveal(0, 0.7f);
            Assert.That(strip.GetCellState(0).LockVisible, Is.False);
        }

        [Test]
        public void DeepZone_RevealPlaysOneCueSet_FinishImmediatelyIsSilent()
        {
            var settings = DeepZoneSettings();
            var strip = NewStrip();
            strip.ManualTick = true;
            strip.CueClock = () => now;
            strip.Apply(settings, new ZoneAccessResult(false, false, "x"));
            strip.Apply(settings, new ZoneAccessResult(true, true, string.Empty));
            Assert.That(strip.TryPlayPending(), Is.True);
            Assert.That(strip.IsRevealing, Is.True);
            Assert.That(requests, Is.EqualTo(new[] { SfxId.UnlockRattle }));
            for (var t = 0f; t < 1.2f; t += Frame)
            {
                now += Frame;
                strip.TickReveal(Frame);
            }

            Assert.That(strip.IsRevealing, Is.False);
            Assert.That(requests, Is.EqualTo(new[] { SfxId.UnlockRattle, SfxId.UnlockClick, SfxId.UnlockGlow }), "한 세트");
            Assert.That(strip.GetCellState(0).Unlocked, Is.True);

            requests.Clear();
            var direct = NewStrip();
            direct.Apply(settings, new ZoneAccessResult(false, false, "x"));
            direct.Apply(settings, new ZoneAccessResult(true, true, string.Empty));
            Assert.That(direct.TryPlayPending(), Is.True, "에디트 모드는 끝 상태로 바로(FinishReveal)");
            Assert.That(requests, Is.Empty, "FinishReveal은 무음");
        }

        // ---------- B 창 TV ----------

        [Test]
        public void WindowTv_OpenCloseOncePerVisibleChange_SecondPlayCutsFirst_SingleSource()
        {
            var a = NewWindow("WindowA");
            var b = NewWindow("WindowB");

            TerminalWindowOpenMotion.Open(a);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvOpen));
            var source = WindowTvSfx.Source;
            Assert.That(source, Is.Not.Null);

            now += 0.1f;
            TerminalWindowOpenMotion.Open(b);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(2));
            Assert.That(WindowTvSfx.InterruptedCount, Is.EqualTo(1), "두 번째가 첫 번째를 끊음");
            Assert.That(WindowTvSfx.Source, Is.SameAs(source), "AudioSource 하나");
            Assert.That(source.clip != null && source.clip.name == "window_tv_open", Is.True);

            TerminalWindowOpenMotion.Open(a);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(2), "이미 열린 창(내용 갱신)은 무음");

            // 펼치는 중(진행 0보다 큼)에 닫아야 닫힘 연출이 돈다. 진행 0이면 바로 끝나 창이 꺼진다.
            a.GetComponent<TerminalWindowOpenMotion>().Advance(0.05f);
            now += 2f;
            TerminalWindowOpenMotion.Close(a, () => a.SetActive(false));
            Assert.That(a.GetComponent<TerminalWindowOpenMotion>().IsClosing, Is.True);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(3));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvClose));
            Assert.That(WindowTvSfx.InterruptedCount, Is.EqualTo(1), "앞 소리가 끝난 뒤는 끊은 것으로 세지 않음");
            TerminalWindowOpenMotion.Close(a, null);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(3), "닫는 중 다시 닫기 무음");

            // 닫는 중 다시 열기: 꺼짐을 끊고 켜짐.
            now += 0.05f;
            TerminalWindowOpenMotion.Open(a);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(4));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvOpen));
            Assert.That(WindowTvSfx.InterruptedCount, Is.EqualTo(2));

            // 숨은 창(인벤토리처럼 루트는 켠 채)을 다시 닫으면 무음.
            var motion = b.GetComponent<TerminalWindowOpenMotion>();
            TerminalWindowOpenMotion.Close(b, null);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(5));
            for (var i = 0; i < 20; i++) motion.Advance(Frame);
            Assert.That(motion.IsShown, Is.False);
            TerminalWindowOpenMotion.Close(b, null);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(5), "이미 닫힌 창 무음");
            Assert.That(requests, Is.Empty, "SfxPlayer 풀을 거치지 않음");
        }

        [Test]
        public void WindowTv_ReduceMotion_NoMotionButSoundStillPlays()
        {
            AccessibilityPreferences.ReduceMotion = true;
            var window = NewWindow("Reduced");
            TerminalWindowOpenMotion.Open(window);
            var motion = window.GetComponent<TerminalWindowOpenMotion>();
            Assert.That(motion.IsPlaying, Is.False, "모션 감소: 연출 없음");
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvOpen));

            var closed = false;
            TerminalWindowOpenMotion.Close(window, () => closed = true);
            Assert.That(closed, Is.True, "바로 닫힘");
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(2));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvClose));
        }

        [Test]
        public void WindowTv_MutedPlaysNothing()
        {
            WindowTvSfx.ResetForTests(Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath), () => now, () => true);
            TerminalWindowOpenMotion.Open(NewWindow("Muted"));
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(0));
        }

        // ---------- 도우미 ----------

        /// <summary>지상 기지 업그레이드 창을 연출 없이 열고 첫 노드를 돌려준다.</summary>
        private UpgradeTreeNodeView OpenTreeNode()
        {
            var instance = Spawn();
            instance.GetComponent<SurfaceBaseView>().SetUpgradeVisible(true);
            var tree = instance.GetComponentInChildren<UpgradeTreeView>(true);
            tree.ManualTick = true;
            var node = tree.Nodes[0];
            node.gameObject.SetActive(true);
            return node;
        }

        private static RectTransform FindChild(Transform root, string name)
        {
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == name) return rect;
            }

            return null;
        }

        private GameObject Spawn()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            spawned.Add(instance);
            return instance;
        }

        private GameObject NewWindow(string name)
        {
            var parent = new GameObject(name + "Parent", typeof(RectTransform));
            spawned.Add(parent);
            var window = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            window.transform.SetParent(parent.transform, false);
            ((RectTransform)window.transform).sizeDelta = new Vector2(400f, 300f);
            return window;
        }

        private DeepZoneThumbnailStrip NewStrip()
        {
            var root = new GameObject("B169Strip", typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta = new Vector2(1006f, 106f);
            spawned.Add(root);
            return DeepZoneThumbnailStrip.Create((RectTransform)root.transform, null);
        }

        private DeepZoneDisplaySettings DeepZoneSettings()
        {
            var settings = ScriptableObject.CreateInstance<DeepZoneDisplaySettings>();
            settings.EditorSet(new[]
            {
                new DeepZoneDisplayLevel(36, 40, true, string.Empty),
                new DeepZoneDisplayLevel(41, 75, false, "준비 중")
            });
            created.Add(settings);
            return settings;
        }

        /// <summary>B-169 이전 DeepZoneThumbnailStrip.SampleReveal의 자물쇠 식(그대로 옮김).</summary>
        private static void OldThumbnailPose(float u, DeepZoneThumbnailStyle s, out Vector2 offset, out float angle,
            out float scale, out float lift, out float alpha)
        {
            offset = Vector2.zero;
            angle = 0f;
            scale = 1f;
            lift = 0f;
            alpha = 1f;
            if (u < 0.2f)
            {
                var p = u / 0.2f;
                angle = Mathf.Sin(p * Mathf.PI * 8f) * 12f * (0.5f + p * 0.5f);
            }
            else if (u < 0.3f)
            {
                var p = (u - 0.2f) / 0.1f;
                scale = 1f + 0.1f * UpgradeTreeTween.Pulse(p);
                lift = s.shackleKick * UpgradeTreeTween.EaseOut(p);
            }
            else
            {
                var p = (u - 0.3f) / 0.35f;
                var fall = p * p;
                offset = new Vector2(4f * fall, -s.lockDrop * fall);
                angle = -s.lockDropAngle * fall;
                lift = s.shackleKick;
                alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, p));
            }
        }

        private void StepNode(UpgradeTreeNodeView node, float seconds)
        {
            for (var t = 0f; t < seconds - 0.0001f; t += Frame)
            {
                node.StepUnlock(Mathf.Min(Frame, seconds - t));
            }
        }

        /// <summary>모션 시간을 진행하면서 테스트 시계(now)도 같이 흘린다.</summary>
        private void Run(UpgradeWindowMotion motion, float seconds)
        {
            motion.ManualTick = true;
            for (var t = 0f; t < seconds; t += Frame)
            {
                now += Frame;
                motion.Tick(Frame);
            }
        }

        private static UpgradeSnapshot Snapshot(string id, bool unlocked)
        {
            return new UpgradeSnapshot(
                id,
                id,
                unlocked ? 1 : 0,
                3,
                0f,
                1f,
                Array.Empty<ItemCostDto>(),
                true,
                null,
                null,
                unlocked,
                unlocked ? null : "잠김");
        }

        private static UpgradeSnapshot[] Snapshots(string[] ids, bool unlocked)
        {
            var result = new UpgradeSnapshot[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                result[i] = Snapshot(ids[i], unlocked);
            }

            return result;
        }

        private PanelToggleController CreateMineRig(out RectTransform card)
        {
            var layoutObject = new GameObject("PanelLayout", typeof(RectTransform));
            spawned.Add(layoutObject);
            var cardObject = new GameObject("UpgradePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Canvas), typeof(PopupWindowDrag));
            cardObject.transform.SetParent(layoutObject.transform, false);
            card = (RectTransform)cardObject.transform;
            card.sizeDelta = new Vector2(1500f, 820f);

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
    }
}
