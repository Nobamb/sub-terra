using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Core;
using SubTerra.App.Save;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class Issue271MainMenuHoverTests
    {
        private UiTestEnvironment environment;
        private readonly Dictionary<string, int?> originalPrefs = new Dictionary<string, int?>();
        private SettingsValues originalSettings;
        private float originalListenerVolume;
        private int hoverRequests;
        private SfxPlayer player;
        private readonly List<string> evidence = new List<string>();
        private static T Field<T>(object target, string name) => (T)target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            SfxRequests.PlayRequested -= Record;
            environment?.Dispose();
            environment = null;
            var empty = SceneManager.CreateScene("MainMenuHoverCleanup");
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
                if (SceneManager.GetSceneAt(i) != empty)
                    yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i));
            foreach (var pair in originalPrefs)
            {
                if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value);
                else PlayerPrefs.DeleteKey(pair.Key);
            }
            if (originalPrefs.Count > 0) PlayerPrefs.Save();
            if (originalSettings != null) SettingsRuntimeApplier.RestoreAppliedVolume(originalSettings);
            AudioListener.volume = originalListenerVolume;
            originalPrefs.Clear();
            string directory = Path.GetFullPath("Builds/Issue271-20261103");
            Directory.CreateDirectory(directory);
            File.WriteAllLines(Path.Combine(directory, "pointer-audio-evidence.tsv"), evidence);
        }

        [UnityTest]
        public IEnumerator Bootstrap_MainMenu_PointerHoverReentrySettingsAndAudioGates()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-subterra-save-root");
            Assert.That(at, Is.GreaterThanOrEqualTo(0), "Isolated QA save root required");
            Assert.That(Path.GetFullPath(args[at + 1]).StartsWith(Path.GetFullPath("Temp")
                + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True);
            originalSettings = SettingsRuntimeApplier.LoadOrDefaults();
            originalListenerVolume = AudioListener.volume;
            foreach (string key in new[] { SettingsRuntimeApplier.PrefMasterMuted,
                SettingsRuntimeApplier.PrefSfxMuted, SettingsRuntimeApplier.PrefControls })
                originalPrefs[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
            hoverRequests = 0;
            evidence.Clear();
            evidence.Add("phase\trequests\tstarted\teffectiveSfx\tlistener\tclip");
            SfxRequests.PlayRequested += Record;
            environment = new UiTestEnvironment();
            GameBootstrapper.ResetInstanceForTests();
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap/Bootstrap.unity");
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == "MainMenu"
                && Object.FindAnyObjectByType<MainMenuBinder>() != null
                && Object.FindAnyObjectByType<MainMenuBinder>().IsBound, "main menu bound", 60f);
            // Populate Continue through the real save lifecycle, only in the asserted Temp root.
            var runtime = SaveRuntimeController.Instance;
            Assert.That(runtime.StartNewGame(2, true), Is.True);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == "SurfaceBase"
                && runtime.IsUiReady, "QA surface ready", 60f);
            Assert.That(runtime.SaveCurrent().IsSuccess, Is.True);
            Assert.That(GameBootstrapper.Instance.Scenes.Load("MainMenu"), Is.True);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == "MainMenu"
                && Object.FindAnyObjectByType<MainMenuBinder>() != null
                && Object.FindAnyObjectByType<MainMenuBinder>().IsBound, "returned menu bound", 60f);
            var binder = Object.FindAnyObjectByType<MainMenuBinder>();
            var view = binder.GetComponent<MainMenuView>();
            binder.Presenter.SelectSlot(2);
            yield return UiTestWait.Until(() => !view.Intro.BlocksInput, "main menu intro complete", 30f);
            yield return environment.Resolution.Set(1920, 1080);
            environment.BindInput();
            player = SfxPlayer.Instance;
            Assert.That(player, Is.Not.Null);
            Assert.That(player.Library.TryGetEntry(SfxId.UiHover, out var entry), Is.True);
            Assert.That(entry.Clip, Is.Not.Null);
            SettingsRuntimeApplier.SetMasterMuted(false, 1f);
            SettingsRuntimeApplier.SetMuted(AudioChannel.Sfx, false, 1f);
            SettingsRuntimeApplier.PreviewVolume(AudioChannel.Sfx, 1f);
            var buttons = new[] { "newGameButton", "continueButton", "settingsButton", "quitButton" }
                .Select(name => Field<Button>(view, name)).ToArray();
            foreach (var button in buttons)
            {
                Assert.That(button.IsInteractable(), Is.True, button.name);
                yield return Probe(button, "normal-" + button.name, true, true);
                int requests = hoverRequests, starts = player.StartedCount;
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(hoverRequests, Is.EqualTo(requests), "Stationary pointer repeated hover");
                Assert.That(player.StartedCount, Is.EqualTo(starts));
                yield return Probe(button, "reentry-" + button.name, true, true);
            }
            var settings = buttons[2];
            yield return Move(Center(settings));
            yield return Click();
            yield return UiTestWait.Until(() => binder.Presenter.Settings.IsOpen, "settings opened");
            yield return new WaitForSecondsRealtime(1f);
            yield return UiTestWait.Press(environment.Keyboard, Key.Escape);
            var settingsRoot = Field<GameObject>(view, "settingsRoot");
            yield return UiTestWait.Until(() => !settingsRoot.activeInHierarchy, "settings close animation");
            SettingsRuntimeApplier.PreviewVolume(AudioChannel.Sfx, 1f);
            AudioListener.volume = 1f;
            foreach (var button in buttons) yield return Probe(button, "after-settings-" + button.name, true, true);
            buttons[0].interactable = false;
            yield return Probe(buttons[0], "disabled", false, false);
            buttons[0].interactable = true;
            SettingsRuntimeApplier.SetMasterMuted(true, 1f);
            yield return Probe(settings, "master-muted", true, false);
            SettingsRuntimeApplier.SetMasterMuted(false, 1f);
            SettingsRuntimeApplier.SetMuted(AudioChannel.Sfx, true, 1f);
            yield return Probe(settings, "sfx-muted", true, false);
            SettingsRuntimeApplier.SetMuted(AudioChannel.Sfx, false, 0f);
            yield return Probe(settings, "sfx-volume-zero", true, true);
            Assert.That(SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx), Is.Zero);
            SettingsRuntimeApplier.PreviewVolume(AudioChannel.Sfx, 1f);
            yield return Probe(settings, "restored", true, true);
        }

        private void Record(SfxId id, float pitch) { if (id == SfxId.UiHover) hoverRequests++; }
        private static Vector2 Center(Button button)
        {
            var rect = (RectTransform)button.transform;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }
        private IEnumerator Move(Vector2 position)
        {
            InputSystem.QueueStateEvent(environment.Mouse, new MouseState { position = position });
            yield return null;
            yield return null;
        }
        private IEnumerator Click()
        {
            var position = environment.Mouse.position.ReadValue();
            InputSystem.QueueStateEvent(environment.Mouse, new MouseState { position = position, buttons = 1 });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(environment.Mouse, new MouseState { position = position });
            yield return null;
            yield return null;
        }
        private IEnumerator Probe(Button button, string phase, bool expectRequest, bool expectStart)
        {
            yield return Move(new Vector2(Screen.width - 4f, Screen.height - 4f));
            yield return new WaitForSecondsRealtime(.08f); // Existing SfxPlayer global debounce is 50ms.
            int requests = hoverRequests, starts = player.StartedCount;
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current) { position = Center(button) };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, phase);
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button), phase);
            yield return Move(Center(button));
            int requestDelta = hoverRequests - requests, startDelta = player.StartedCount - starts;
            Assert.That(player.Library.TryGetEntry(SfxId.UiHover, out var entry), Is.True);
            evidence.Add(phase + "\t" + requestDelta + "\t" + startDelta + "\t"
                + SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx) + "\t" + AudioListener.volume + "\t" + entry.Clip.name);
            Assert.That(requestDelta, Is.EqualTo(expectRequest ? 1 : 0), phase + " pointer→UiHover");
            Assert.That(startDelta, Is.EqualTo(expectStart ? 1 : 0), phase + " UiHover→SfxPlayer");
        }
    }
}
