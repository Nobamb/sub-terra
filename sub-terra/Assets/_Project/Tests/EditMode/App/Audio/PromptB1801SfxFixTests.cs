using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Progression;
using SubTerra.App.UI;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.App.UI.Tutorial;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    /// <summary>
    /// B-180-1: 잠긴 업그레이드 노드 클릭의 자물쇠 흔들림·쇠 소리, 퀘스트 클리어 창 닫힘음(브리핑 종료음).
    /// 체크 등장 시점의 완료음은 코루틴이라 QuestClearPopupMotionPlayModeTests에서 확인한다.
    /// </summary>
    public sealed class PromptB1801SfxFixTests
    {
        private const string SurfacePrefabPath = "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float Frame = 0.016f;

        private readonly List<SfxId> played = new List<SfxId>();
        private readonly List<GameObject> created = new List<GameObject>();
        private bool previousReduceMotion;

        [SetUp]
        public void SetUp()
        {
            played.Clear();
            previousReduceMotion = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
            UpgradeWindowMotion.ResetSessionForTests();
            SfxRequests.PlayRequested += Record;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            SetCompactSfxInEditMode(false);
            AccessibilityPreferences.ReduceMotion = previousReduceMotion;
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }

            created.Clear();
            UiPauseGate.Release(StartBriefingPopupMotion.PauseOwner);
        }

        private void Record(SfxId id, float pitch) => played.Add(id);

        private int Count(SfxId id)
        {
            var count = 0;
            foreach (var value in played) if (value == id) count++;
            return count;
        }

        // ---------- B. 잠긴 업그레이드 클릭 ----------

        [Test]
        public void LockedNode_Click_RattlesLockOnce_NoSelectionClick_KeepsLock()
        {
            var node = OpenTreeNode(out _);
            node.Apply(Snapshot(node.UpgradeId, false));
            var button = node.GetComponent<Button>();
            var sound = button.GetComponent<UiSfxButton>();
            Assert.That(sound.ClickSfx, Is.EqualTo(SfxId.None), "잠긴 노드는 선택 클릭음 대신 쇠 소리");
            Assert.That(sound.Hover, Is.True, "호버음은 그대로");

            var lockRoot = FindChild(node.transform, "Lock");
            Assert.That(lockRoot, Is.Not.Null);
            var basePosition = lockRoot.anchoredPosition;
            var clicked = 0;
            node.Clicked += _ => clicked++;
            played.Clear();

            // 실제 클릭처럼 버튼 onClick과 노드 클릭 처리를 둘 다 거친다.
            button.onClick.Invoke();
            node.OnPointerClick(null);
            Assert.That(played, Is.EqualTo(new[] { UpgradeTreeNodeView.LockedClickSfx }));
            Assert.That(UpgradeTreeNodeView.LockedClickSfx, Is.EqualTo(SfxId.LockedNodeRattle));
            Assert.That(clicked, Is.EqualTo(1), "잠김 안내(선택)는 그대로");
            Assert.That(node.IsLockRattlePlaying, Is.True);

            // 0.03초 = 진행 0.067, 첫 번째 꼭대기 근처(sin이 0이 되는 지점은 피한다).
            StepRattle(node, 0.03f);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, lockRoot.localEulerAngles.z)), Is.GreaterThan(5f), "자물쇠가 흔들림");
            Assert.That(lockRoot.anchoredPosition, Is.EqualTo(basePosition), "제자리에서 흔들림(떨어지지 않음)");

            // 흔들리는 중 연타: 소리·흔들림이 다시 시작되거나 쌓이지 않는다.
            node.OnPointerClick(null);
            node.OnPointerClick(null);
            Assert.That(Count(UpgradeTreeNodeView.LockedClickSfx), Is.EqualTo(1));
            Assert.That(clicked, Is.EqualTo(3));

            StepRattle(node, UpgradeTreeNodeView.LockRattleDuration);
            Assert.That(node.IsLockRattlePlaying, Is.False);
            Assert.That(Mathf.DeltaAngle(0f, lockRoot.localEulerAngles.z), Is.EqualTo(0f).Within(0.001f));
            Assert.That(lockRoot.anchoredPosition, Is.EqualTo(basePosition));
            Assert.That(lockRoot.localScale, Is.EqualTo(Vector3.one));
            Assert.That(node.IsLockVisible, Is.True, "잠금 유지");
            Assert.That(node.BlindAlpha, Is.EqualTo(1f), "블라인드 유지");
            Assert.That(node.VisualState, Is.EqualTo(UpgradeNodeVisualState.Locked));

            node.OnPointerClick(null);
            Assert.That(Count(UpgradeTreeNodeView.LockedClickSfx), Is.EqualTo(2), "끝난 뒤 다시 누르면 다시 한 번");
            Assert.That(Count(SfxId.UiClick), Is.Zero);
        }

        [Test]
        public void UnlockedNode_Click_KeepsUiClick_NoRattle()
        {
            var node = OpenTreeNode(out _);
            node.Apply(Snapshot(node.UpgradeId, false));
            node.Apply(Snapshot(node.UpgradeId, true));
            var button = node.GetComponent<Button>();
            Assert.That(button.GetComponent<UiSfxButton>().ClickSfx, Is.EqualTo(SfxId.UiClick), "풀리면 다시 UiClick");
            Assert.That(button.GetComponents<UiSfxButton>().Length, Is.EqualTo(1));
            played.Clear();

            button.onClick.Invoke();
            node.OnPointerClick(null);
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiClick }));
            Assert.That(node.IsLockRattlePlaying, Is.False);
        }

        [Test]
        public void LockedNode_WindowTransitionOrUnlockPlaying_IgnoresRattle()
        {
            var node = OpenTreeNode(out var motion);
            node.Apply(Snapshot(node.UpgradeId, false));
            foreach (var state in new[] { UpgradeWindowMotion.MotionState.Opening, UpgradeWindowMotion.MotionState.Closing })
            {
                Set(motion, "state", state);
                played.Clear();
                node.OnPointerClick(null);
                Assert.That(played, Is.Empty, state + " 중에는 무음");
                Assert.That(node.IsLockRattlePlaying, Is.False, state + " 중에는 흔들지 않음");
            }

            Set(motion, "state", UpgradeWindowMotion.MotionState.Open);
            node.SetRevealPending(true);
            node.Apply(Snapshot(node.UpgradeId, true));
            node.BeginUnlock(null);
            played.Clear();
            node.OnPointerClick(null);
            Assert.That(Count(UpgradeTreeNodeView.LockedClickSfx), Is.Zero, "해금 연출 중에는 쇠 소리 없음");
            Assert.That(node.IsLockRattlePlaying, Is.False);
        }

        [Test]
        public void LockedNode_DisableMidRattle_StopsAndRestoresLock()
        {
            var node = OpenTreeNode(out _);
            node.Apply(Snapshot(node.UpgradeId, false));
            var lockRoot = FindChild(node.transform, "Lock");
            var basePosition = lockRoot.anchoredPosition;
            node.OnPointerClick(null);
            StepRattle(node, 0.05f);
            Assert.That(node.IsLockRattlePlaying, Is.True);

            Call(node, "OnDisable");
            Assert.That(node.IsLockRattlePlaying, Is.False);
            Assert.That(Mathf.DeltaAngle(0f, lockRoot.localEulerAngles.z), Is.EqualTo(0f).Within(0.001f));
            Assert.That(lockRoot.anchoredPosition, Is.EqualTo(basePosition));

            // 다시 열면 자리·크기가 그대로이고 다시 누르면 한 번 더 반응한다.
            Call(node, "OnEnable");
            Assert.That(node.IsLockVisible, Is.True);
            Assert.That(lockRoot.localScale, Is.EqualTo(Vector3.one));
            played.Clear();
            node.OnPointerClick(null);
            Assert.That(played, Is.EqualTo(new[] { UpgradeTreeNodeView.LockedClickSfx }));
        }

        [Test]
        public void LockedNodeRattle_Slot95_PlaysWholeRattle_Slot76Unchanged()
        {
            Assert.That((int)SfxId.LockedNodeRattle, Is.EqualTo(95));
            (SfxEntry Entry, string File) locked = default, shared = default;
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                if (pair.Entry.Id == SfxId.LockedNodeRattle) locked = (pair.Entry, pair.FileName);
                if (pair.Entry.Id == SfxId.UnlockRattle) shared = (pair.Entry, pair.FileName);
            }

            Assert.That(locked.File, Is.EqualTo("unlock_rattle"), "같은 음원 재사용");
            Assert.That(locked.Entry.Bus, Is.EqualTo(SfxBus.UI));
            Assert.That(locked.Entry.MaxDuration, Is.EqualTo(0f), "덜그럭까지 자르지 않음");
            Assert.That(locked.Entry.StartTime, Is.EqualTo(0.15f).Within(0.0001f), "앞의 차오르는 부분만 건너뜀");
            Assert.That(shared.Entry.MaxDuration, Is.EqualTo(0.3f).Within(0.0001f), "창·해금 연출용 76번은 그대로");
            Assert.That(shared.Entry.StartTime, Is.EqualTo(0f));

            var library = Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            Assert.That(library.TryGetEntry(SfxId.LockedNodeRattle, out var asset), Is.True);
            Assert.That(asset.Clip, Is.Not.Null);
            Assert.That(asset.Clip.name, Is.EqualTo("unlock_rattle"));
            Assert.That(asset.Volume, Is.EqualTo(locked.Entry.Volume).Within(0.0001f));
            Assert.That(asset.StartTime, Is.EqualTo(0.15f).Within(0.0001f));
            Assert.That(asset.MaxDuration, Is.EqualTo(0f));
            Assert.That(library.TryGetEntry(SfxId.UnlockRattle, out var old), Is.True);
            Assert.That(old.MaxDuration, Is.EqualTo(0.3f).Within(0.0001f));

            // 흔들림은 덜그럭이 끝나는 무렵까지(재생 0.52초 중 큰 소리는 0.1~0.45초).
            var plan = SfxPlayback.Plan(asset.Clip.length, asset.StartTime, asset.MaxDuration, asset.FadeOut, 1f);
            Assert.That(plan.RealDuration, Is.GreaterThan(0.45f));
            Assert.That(UpgradeTreeNodeView.LockRattleDuration, Is.LessThanOrEqualTo(plan.RealDuration));
        }

        // ---------- 심층 구역 썸네일 자물쇠 ----------

        [Test]
        public void DeepZoneLock_PartsAreFlatColor_NoShadowOrOutline()
        {
            var root = new GameObject("B1801Strip", typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta = new Vector2(1006f, 106f);
            created.Add(root);
            var strip = DeepZoneThumbnailStrip.Create((RectTransform)root.transform, null);
            Assert.That(strip, Is.Not.Null);
            var settings = ScriptableObject.CreateInstance<DeepZoneDisplaySettings>();
            settings.EditorSet(new[]
            {
                new DeepZoneDisplayLevel(36, 40, true, string.Empty),
                new DeepZoneDisplayLevel(41, 75, false, "준비 중")
            });
            try
            {
                strip.Apply(settings, new ZoneAccessResult(false, false, "조건 부족"));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }

            var parts = 0;
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.transform.parent == null || image.transform.parent.name != "Lock") continue;
                parts++;
                Assert.That(image.GetComponent<Shadow>(), Is.Null, image.name + "에 그림자·외곽선 없음");
                Assert.That(image.color.a, Is.EqualTo(1f), image.name);
            }

            Assert.That(parts, Is.GreaterThanOrEqualTo(4), "칸마다 자물쇠 4조각");
        }

        // ---------- C. 퀘스트 클리어 창 닫힘음 ----------

        [Test]
        public void QuestClear_CloseUsesBriefingClose_Once_NoCompletionOnClose()
        {
            Assert.That(QuestClearPopupMotion.CloseSfx, Is.EqualTo(SfxId.BriefingClose));
            Assert.That(QuestClearPopupMotion.CheckAppearSfx, Is.EqualTo(SfxId.QuestReward));
            SetCompactSfxInEditMode(true);
            var popup = NewQuestClearPopup(out var transition);
            Assert.That(GetField(transition, "compactCloseSfx"), Is.EqualTo(SfxId.BriefingClose));

            Call(transition, "BeginIntro");
            for (var t = 0f; t < StartBriefingTimeline.IntroDuration + 0.1f; t += 0.02f) Call(transition, "Tick", 0.02f);
            Assert.That(transition.IsShown, Is.True);
            played.Clear();

            popup.Close();
            popup.Close();
            for (var t = 0f; t < StartBriefingTimeline.CloseDuration + 0.05f; t += 0.02f) Call(transition, "Tick", 0.02f);
            Assert.That(Count(SfxId.BriefingClose), Is.EqualTo(1), "확인·X·닫기 키 모두 이 경로로 한 번");
            Assert.That(Count(SfxId.GlitchPopupClose), Is.Zero, "작은 창 공용 닫힘음과 겹치지 않음");
            Assert.That(Count(SfxId.QuestReward), Is.Zero, "닫을 때 완료음 없음");
        }

        [Test]
        public void OtherCompactPopups_KeepGlitchPopupClose()
        {
            var root = new GameObject("B1801Compact", typeof(RectTransform));
            created.Add(root);
            var motion = root.AddComponent<StartBriefingPopupMotion>();
            Call(motion, "ConfigureCompact", new Vector2(900f, 450f), new Vector2(395f, 155f), null, false,
                "Unlock", null, 1f);
            Assert.That(GetField(motion, "compactCloseSfx"), Is.EqualTo(StartBriefingPopupMotion.CompactCloseSfx));
            Assert.That(StartBriefingPopupMotion.CompactCloseSfx, Is.EqualTo(SfxId.GlitchPopupClose), "공용 상수는 그대로");
        }

        // ---------- 도우미 ----------

        private UpgradeTreeNodeView OpenTreeNode(out UpgradeWindowMotion motion)
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePrefabPath));
            created.Add(instance);
            instance.GetComponent<SurfaceBaseView>().SetUpgradeVisible(true);
            var tree = instance.GetComponentInChildren<UpgradeTreeView>(true);
            tree.ManualTick = true;
            var node = tree.Nodes[0];
            node.gameObject.SetActive(true);
            node.ManualTick = true;
            // EditMode에서는 OnEnable이 불리지 않는다.
            Call(node, "OnEnable");
            motion = node.GetComponentInParent<UpgradeWindowMotion>(true);
            if (motion == null) motion = node.transform.parent.gameObject.AddComponent<UpgradeWindowMotion>();
            Set(motion, "state", UpgradeWindowMotion.MotionState.Open);
            Assert.That(node.CanPlayButtonSfx(), Is.True, "준비: 창이 열려 있음");
            return node;
        }

        private QuestClearPopupMotion NewQuestClearPopup(out StartBriefingPopupMotion transition)
        {
            var canvas = new GameObject("B1801Canvas", typeof(RectTransform), typeof(Canvas));
            created.Add(canvas);
            var root = new GameObject("B1801QuestClear", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            ((RectTransform)root.transform).sizeDelta = new Vector2(900f, 450f);
            var close = new GameObject(QuestClearPopupMotion.CloseButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            close.transform.SetParent(root.transform, false);
            var popup = root.AddComponent<QuestClearPopupMotion>();
            Call(popup, "Awake");
            transition = root.GetComponent<StartBriefingPopupMotion>();
            Call(transition, "Awake");
            Call(transition, "OnEnable");
            return popup;
        }

        private static void StepRattle(UpgradeTreeNodeView node, float seconds)
        {
            for (var t = 0f; t < seconds - 0.0001f; t += Frame)
            {
                node.StepLockRattle(Mathf.Min(Frame, seconds - t));
            }
        }

        private static RectTransform FindChild(Transform root, string name)
        {
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == name) return rect;
            }

            return null;
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

        private static void SetCompactSfxInEditMode(bool value)
        {
            var field = typeof(StartBriefingPopupMotion).GetField("CompactSfxInEditModeForTests",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(field, Is.Not.Null);
            field.SetValue(null, value);
        }

        private static object Call(object target, string name, params object[] args)
        {
            var method = target.GetType().GetMethod(name, Private | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, name);
            return method.Invoke(target, args);
        }

        private static object GetField(object target, string name)
        {
            var field = target.GetType().GetField(name, Private);
            Assert.That(field, Is.Not.Null, name);
            return field.GetValue(target);
        }

        private static void Set(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, Private);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
