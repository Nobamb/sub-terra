using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SubTerra.Integration.Tests.Audio
{
    public sealed class PromptB183SfxMenuDronePlayModeTests
    {
        private readonly List<(SfxId id, float at, float pitch)> played = new List<(SfxId, float, float)>();
        private GameObject root;
        private DroneDialoguePanelView view;
        private SfxPlayer player;

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [SetUp]
        public void SetUp()
        {
            played.Clear();
            if (SfxPlayer.Instance != null) SfxPlayer.Instance.ReleaseForTests();
            player = SfxPlayer.CreateForTests(Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath), null, () => false);
            SfxRequests.PlayRequested += Record;
            SfxRequests.ScaledPlayRequested += RecordScaled;
            WindowTvSfx.ResetForTests(Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath), null, () => false);
            root = new GameObject("B183TerminalCanvas", typeof(RectTransform), typeof(Canvas));
            root.AddComponent<AudioListener>();
            root.SetActive(false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var panel = new GameObject("PanelRoot", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(root.transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(820, 260);
            Label(panel.transform, "SpeakerText", "", new Vector2(0, 90));
            var dialogue = Label(panel.transform, "DialogueText", "가스 구역 이탈을 권장합니다. 안전 경로를 확인하세요.", new Vector2(0, 40));
            var action = Label(panel.transform, "ActionText", "AI 추천 // 가스 구역 이탈", new Vector2(0, -10));
            var reasons = Label(panel.transform, "ReasonText", "[01] 가스 위험 72%\n[02] 전력 부족\n[03] 귀환 경로 확인", new Vector2(0, -75));
            view = root.AddComponent<DroneDialoguePanelView>();
            Set(view, "panelRoot", panel);
            Set(view, "dialogueText", dialogue);
            Set(view, "actionText", action);
            Set(view, "reasonText", reasons);
            Set(view, "reasonLines", new List<string> { "[01] 가스 위험 72%", "[02] 전력 부족", "[03] 귀환 경로 확인" });
            panel.SetActive(false);
            root.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            SfxRequests.ScaledPlayRequested -= RecordScaled;
            WindowTvSfx.ReleaseForTests();
            if (player != null) player.ReleaseForTests();
            if (root != null) Object.DestroyImmediate(root);
        }

        private void Record(SfxId id, float pitch) => played.Add((id, Time.realtimeSinceStartup, pitch));
        private void RecordScaled(SfxId id, float pitch, float scale) => Record(id, pitch);

        private static TMP_Text Label(Transform parent, string name, string text, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = 20;
            label.rectTransform.sizeDelta = new Vector2(760, 90);
            label.rectTransform.anchoredPosition = position;
            return label;
        }

        [UnityTest]
        public IEnumerator Terminal_ActualUpdate_OpenTypingReasonsPriorityClose()
        {
            view.SetVisible(true);
            view.SetVisible(true);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvOpen));
            Assert.That(WindowTvSfx.LastVolumeScale, Is.EqualTo(0.6f));
            Assert.That(WindowTvSfx.Source.isPlaying, Is.True);
            yield return new WaitForSecondsRealtime(1.1f);
            var ticks = played.Where(p => p.id == SfxId.MainMenuTypeTick).ToArray();
            Assert.That(ticks.Length, Is.GreaterThan(1));
            for (var i = 1; i < ticks.Length; i++) Assert.That(ticks[i].at - ticks[i - 1].at, Is.GreaterThanOrEqualTo(0.059f));
            Assert.That(played.Count(p => p.id == SfxId.KeycapPop), Is.EqualTo(3));
            view.SetOperationalState(DroneOperationalState.Priority);
            view.SetOperationalState(DroneOperationalState.Idle);
            view.SetOperationalState(DroneOperationalState.Priority);
            Assert.That(played.Count(p => p.id == SfxId.DronePriorityAlert), Is.EqualTo(1));
            yield return SaveScreen("terminal-priority");
            played.Clear();
            view.SetDialogue(new DroneDialogueResult("quiet", "같은 추천의 대사 교체", false, false));
            yield return null;
            Assert.That(played, Is.Empty);
            Assert.That(view.TryBeginAnimatedClose(null), Is.True);
            view.TryBeginAnimatedClose(null);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(2));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvClose));
            view.SetOperationalState(DroneOperationalState.Idle);
            view.SetOperationalState(DroneOperationalState.Priority);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(played, Is.Empty, "닫는 중 타이핑·근거·PRIORITY는 무음");
            view.SetVisible(false);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(2));
            Assert.That(view.TerminalPhase, Is.EqualTo(DroneTerminalPhase.Hidden));
        }

        [UnityTest]
        public IEnumerator Terminal_SkipAndHiddenPriorityAreSilent_AlertAndBubbleDoNotOverlap()
        {
            view.SetOperationalState(DroneOperationalState.Priority);
            Assert.That(played, Is.Empty);
            view.SetVisible(true);
            view.SkipTerminalReveal();
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(played, Is.Empty);
            var socketRoot = new GameObject("Bubble", typeof(RectTransform), typeof(CanvasGroup));
            socketRoot.transform.SetParent(root.transform, false);
            var label = Label(socketRoot.transform, "BubbleText", "", Vector2.zero);
            var socket = socketRoot.AddComponent<DroneDialogueSocket>();
            Set(socket, "dialogueText", label);
            Set(socket, "canvasGroup", socketRoot.GetComponent<CanvasGroup>());
            socket.SetVisible(true);
            socket.SetDialogue(new DroneDialogueResult("initial", "초기화", false, false));
            yield return new WaitForSecondsRealtime(1.1f);
            played.Clear();
            view.SetOperationalState(DroneOperationalState.Idle);
            view.SetOperationalState(DroneOperationalState.Priority);
            socket.SetDialogue(new DroneDialogueResult("gas", "새로운 위험", false, false, true));
            Assert.That(played.Select(p => p.id), Is.EqualTo(new[] { SfxId.DronePriorityAlert }));
            view.SetVisible(false);
            view.SetOperationalState(DroneOperationalState.Idle);
            view.SetOperationalState(DroneOperationalState.Priority);
            Assert.That(played.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Noise_RealPlayback_Fades_MuteAndDisableStop()
        {
            var noise = root.AddComponent<EmptySlotNoiseAudio>();
            var muted = false;
            noise.ConfigureForTests(Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath), () => muted);
            noise.SelectSlot(true);
            for (var i = 0; i < 5; i++) { noise.Tick(0.02f, false); yield return null; }
            Assert.That(noise.Source.isPlaying, Is.True);
            Assert.That(noise.Source.loop, Is.True);
            Assert.That(noise.Source.spatialBlend, Is.Zero);
            for (var i = 0; i < 50; i++) { noise.Tick(0.02f, false); yield return null; }
            Assert.That(noise.Source.isPlaying, Is.False);
            noise.SetHover(true);
            noise.Tick(0.02f, false);
            Assert.That(noise.Source.isPlaying, Is.True);
            muted = true;
            noise.Tick(0.02f, false);
            Assert.That(noise.Source.isPlaying, Is.False);
            muted = false;
            noise.SetHover(true);
            noise.Tick(0.02f, false);
            root.SetActive(false);
            yield return null;
            Assert.That(noise.Source.isPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator Menu_ClickKeyboardHoverAndModalReachNoise_InitializationAndDeletionStayQuiet()
        {
            var menuRoot = new GameObject("Menu", typeof(RectTransform));
            menuRoot.SetActive(false);
            menuRoot.transform.SetParent(root.transform, false);
            var buttons = new UnityEngine.UI.Button[3];
            for (var i = 0; i < 3; i++)
            {
                var card = new GameObject("Slot" + i, typeof(RectTransform), typeof(UnityEngine.UI.Button), typeof(SaveSlotCardView));
                card.transform.SetParent(menuRoot.transform, false);
                buttons[i] = card.GetComponent<UnityEngine.UI.Button>();
            }
            var menu = menuRoot.AddComponent<MainMenuView>();
            Set(menu, "slotButtons", buttons);
            var binder = menuRoot.AddComponent<MainMenuBinder>();
            binder.enabled = false;
            Set(binder, "view", menu);
            var select = typeof(MainMenuBinder).GetMethod("OnSlotSelectedSfx", BindingFlags.Instance | BindingFlags.NonPublic);
            menu.SlotSelected += id => { menu.SetSelectedSlot(id, false, ""); select.Invoke(binder, new object[] { id }); };
            menuRoot.SetActive(true);
            menu.SetSlotKind(1, SaveSlotCardKind.Ready);
            menu.SetSelectedSlot(1, true, "");
            yield return null;
            var noise = menuRoot.GetComponent<EmptySlotNoiseAudio>();
            Assert.That(noise.Source, Is.Null, "초기 선택 복원은 무음");
            buttons[1].onClick.Invoke();
            yield return null;
            Assert.That(noise.Source.isPlaying, Is.True, "빈 슬롯 클릭");
            Assert.That(played.Any(p => p.id == SfxId.UiSlotSelect), Is.True);
            yield return new WaitForSecondsRealtime(0.95f);
            Assert.That(noise.Source.isPlaying, Is.False, "선택돼 있기만 하면 무음");
            menu.NavigateSlot(1);
            yield return null;
            Assert.That(menu.CurrentSelectedSlotId, Is.EqualTo(3));
            Assert.That(noise.Source.isPlaying, Is.True, "키보드 선택도 같은 경로");
            noise.StopNow();
            var emptyCard = buttons[1].GetComponent<SaveSlotCardView>();
            emptyCard.OnPointerEnter(null);
            yield return null;
            Assert.That(noise.Source.isPlaying, Is.True);
            emptyCard.OnPointerExit(null);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(noise.Source.isPlaying, Is.False);
            buttons[0].GetComponent<SaveSlotCardView>().OnPointerEnter(null);
            yield return null;
            Assert.That(noise.Source.isPlaying, Is.False, "데이터 있는 카드 호버는 무음");
            menu.SetSlotKind(1, SaveSlotCardKind.Empty);
            yield return null;
            Assert.That(noise.Source.isPlaying, Is.False, "삭제로 빈 카드가 되는 순간은 무음");
            emptyCard.OnPointerEnter(null);
            yield return null;
            menu.SetSettingsVisible(true);
            Assert.That(noise.Source.isPlaying, Is.False, "설정창 열기는 즉시 중단");
            var modal = new GameObject("OverwriteConfirm", typeof(RectTransform));
            modal.transform.SetParent(menuRoot.transform, false);
            modal.SetActive(false);
            Set(menu, "overwriteConfirmRoot", modal);
            menu.SetOverwriteConfirmVisible(true, 2);
            menu.PlaySlotSelectedNoise(2);
            yield return null;
            Assert.That(noise.Source.isPlaying, Is.False, "모달이 열려 있으면 선택 노이즈도 차단");
            menu.SetOverwriteConfirmVisible(false, 2);
            menu.PlaySlotSelectedNoise(2);
            yield return null;
            Assert.That(noise.Source.isPlaying, Is.True);
            menu.PlayExitTransition(null);
            Assert.That(noise.Source.isPlaying, Is.False, "게임 시작 전환은 즉시 중단");
        }

        private static IEnumerator SaveScreen(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            var target = RenderTexture.GetTemporary(Mathf.Max(1, texture.width / 2), Mathf.Max(1, texture.height / 2));
            var previous = RenderTexture.active;
            Graphics.Blit(texture, target);
            RenderTexture.active = target;
            var small = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            small.Apply();
            Directory.CreateDirectory("Temp/b183-play-screens");
            File.WriteAllBytes("Temp/b183-play-screens/" + name + ".png", small.EncodeToPNG());
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            Object.Destroy(texture);
            Object.Destroy(small);
        }
    }
}
