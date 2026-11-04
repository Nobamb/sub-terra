using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    public sealed class PromptB183SfxMenuDroneTests
    {
        private readonly List<(SfxId id, float pitch, float scale)> played = new List<(SfxId, float, float)>();
        private readonly List<GameObject> objects = new List<GameObject>();
        private SfxPlayer player;
        private float now;
        private bool muted;
        private SfxLibrary Library => Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);

        [SetUp]
        public void SetUp()
        {
            now = 10f;
            muted = false;
            played.Clear();
            SfxRequests.PlayRequested += Record;
            SfxRequests.ScaledPlayRequested += RecordScaled;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            SfxRequests.ScaledPlayRequested -= RecordScaled;
            if (player != null) player.ReleaseForTests();
            for (var i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        private void Record(SfxId id, float pitch) => played.Add((id, pitch, 1f));
        private void RecordScaled(SfxId id, float pitch, float scale) => played.Add((id, pitch, scale));
        private GameObject NewObject(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            objects.Add(go);
            return go;
        }

        private static object Call(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [TestCase(SfxId.UiSlotSelect, 16, "ui_slot_select", SfxBus.UI, 0.65f, 0.3f)]
        [TestCase(SfxId.EmptySlotNoiseLoop, 102, "empty_slot_noise_loop", SfxBus.UI, 0.4f, 2f)]
        [TestCase(SfxId.DronePriorityAlert, 103, "drone_priority_alert", SfxBus.UI, 0.5f, 0.6f)]
        [TestCase(SfxId.DroneBubbleUrgent, 104, "drone_bubble_urgent", SfxBus.World, 0.6f, 0.23f)]
        [TestCase(SfxId.DroneBubbleUrgentSerious, 105, "drone_bubble_urgent_serious", SfxBus.World, 0.6f, 0.23f)]
        public void Slots_AssetAndDefaultsMatch(SfxId id, int number, string file, SfxBus bus, float volume, float seconds)
        {
            Assert.That((int)id, Is.EqualTo(number));
            var pair = SfxLibraryDefaults.Create().Single(p => p.Entry.Id == id);
            Assert.That(pair.FileName, Is.EqualTo(file));
            Assert.That(pair.Entry.Bus, Is.EqualTo(bus));
            Assert.That(pair.Entry.Volume, Is.EqualTo(volume));
            Assert.That(Library.TryGetEntry(id, out var entry), Is.True);
            Assert.That(entry.Clip, Is.Not.Null);
            Assert.That(entry.Clip.name, Is.EqualTo(file));
            Assert.That(entry.Clip.length, Is.EqualTo(seconds).Within(0.025f));
            Assert.That(entry.Bus, Is.EqualTo(bus));
            Assert.That(entry.Volume, Is.EqualTo(volume));
        }

        [Test]
        public void NoiseImport_PcmWithoutNormalize_PreservesExistingAndReservedIds()
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath("Assets/_Project/Audio/SFX/UI/empty_slot_noise_loop.wav");
            Assert.That(importer, Is.Not.Null);
            Assert.That(new SerializedObject(importer).FindProperty("m_Normalize").boolValue, Is.False);
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.PCM));
            Assert.That(SfxLibraryDefaults.IsLoopFile("empty_slot_noise_loop"), Is.False);
            Assert.That((int)SfxId.LockedNodeRattle, Is.EqualTo(95));
            foreach (var id in new[] { 99, 100, 101 }) Assert.That(Library.TryGetEntry((SfxId)id, out _), Is.False);
        }

        [TestCase(0.05f, 1)]
        [TestCase(0.2f, 2)]
        public void SlotSelection_RapidClicksAreLimited(float interval, int count)
        {
            player = SfxPlayer.CreateForTests(Library, () => now, () => false);
            var binder = NewObject("Binder").AddComponent<MainMenuBinder>();
            Call(binder, "OnSlotSelectedSfx", 1);
            now += interval;
            Call(binder, "OnSlotSelectedSfx", 2);
            Assert.That(played.Count(p => p.id == SfxId.UiSlotSelect), Is.EqualTo(count));
        }

        private EmptySlotNoiseAudio Noise()
        {
            var noise = NewObject("Noise").AddComponent<EmptySlotNoiseAudio>();
            noise.ConfigureForTests(Library, () => muted);
            return noise;
        }

        [Test]
        public void Noise_SelectionExpiresAndFades_HoverContinuesThenFades()
        {
            var noise = Noise();
            noise.Tick(0.01f, false);
            Assert.That(noise.Source, Is.Null, "기본 선택 복원은 무음");
            noise.SelectSlot(true);
            noise.Tick(0.01f, false);
            Assert.That(noise.Source.loop, Is.True);
            Assert.That(noise.Source.volume, Is.GreaterThan(0f));
            Assert.That(noise.Source.clip.name, Is.EqualTo("empty_slot_noise_loop"));
            for (var i = 0; i < 90; i++) noise.Tick(0.01f, false);
            Assert.That(noise.Source.volume, Is.Zero);
            noise.SetHover(true);
            for (var i = 0; i < 150; i++) noise.Tick(0.01f, false);
            Assert.That(noise.Source.volume, Is.GreaterThan(0f));
            noise.SetHover(false);
            for (var i = 0; i < 30; i++) noise.Tick(0.01f, false);
            Assert.That(noise.Source.volume, Is.Zero);
        }

        [TestCase("modal")]
        [TestCase("mute")]
        [TestCase("disable")]
        public void Noise_BlockedStopsImmediatelyAndDoesNotResume(string reason)
        {
            var noise = Noise();
            noise.SetHover(true);
            noise.SelectSlot(true);
            noise.Tick(0.01f, false);
            if (reason == "mute") muted = true;
            if (reason == "disable") Call(noise, "OnDisable");
            else noise.Tick(0.01f, reason == "modal");
            Assert.That(noise.Source.volume, Is.Zero);
            muted = false;
            noise.Tick(0.01f, false);
            Assert.That(noise.Source.volume, Is.Zero, "중단한 노이즈를 뒤늦게 재생하지 않음");
        }

        [Test]
        public void Noise_DataSlotAndMissingClipAreSilent()
        {
            var noise = Noise();
            noise.SelectSlot(false);
            noise.SetHover(false);
            noise.Tick(0.01f, false);
            Assert.That(noise.Source, Is.Null);
            var empty = ScriptableObject.CreateInstance<SfxLibrary>();
            try
            {
                noise.ConfigureForTests(empty, () => false);
                noise.SelectSlot(true);
                noise.Tick(0.01f, false);
                Assert.That(noise.Source, Is.Null);
            }
            finally { Object.DestroyImmediate(empty); }
        }

        [Test]
        public void Typing_OneTickPerFrame_MinGap_ReasonPitchAndQuietUpdates()
        {
            var view = NewObject("Terminal").AddComponent<DroneDialoguePanelView>();
            Set(view, "sfxClock", (Func<float>)(() => now));
            Call(view, "PlayTypingSfx", 5, 0, true);
            now += 0.02f;
            Call(view, "PlayTypingSfx", 10, 0, true);
            now += 0.05f;
            Call(view, "PlayTypingSfx", 15, 1, true);
            Assert.That(played.Count(p => p.id == SfxId.MainMenuTypeTick), Is.EqualTo(2));
            Assert.That(played.First(p => p.id == SfxId.MainMenuTypeTick).scale, Is.EqualTo(0.5f));
            Call(view, "PlayTypingSfx", 15, 3, true);
            Assert.That(played.Where(p => p.id == SfxId.KeycapPop).Select(p => p.pitch),
                Is.EqualTo(new[] { 1f, 1.03f, 1.06f }).Within(0.0001f));
            played.Clear();
            Call(view, "PlayTypingSfx", 100, 6, false);
            now += 1f;
            Call(view, "PlayTypingSfx", 100, 6, true);
            Assert.That(played, Is.Empty, "건너뛰기·조용한 갱신 뒤 남은 소리 없음");
        }

        [TestCase(DataIds.Dialogue.DroneEmergency, true, SfxId.DroneBubbleUrgentSerious)]
        [TestCase(DataIds.Dialogue.DroneStructuralWarning, true, SfxId.DroneBubbleUrgentSerious)]
        [TestCase(DataIds.Dialogue.DroneGasWarning, true, SfxId.DroneBubbleUrgentSerious)]
        [TestCase(DataIds.Dialogue.LowPowerWarning, true, SfxId.DroneBubbleUrgent)]
        [TestCase(DataIds.Dialogue.DroneEmergency, false, SfxId.WorldFacilityTagBeep)]
        public void Bubble_TemplateClassificationFeedsActualSocket(string template, bool urgent, SfxId expected)
        {
            var socket = Socket();
            var line = new DroneDialogueResult(template, "새 대사", false, false, urgent);
            Assert.That(DroneUrgentBubbleRule.IsSerious(line), Is.EqualTo(expected == SfxId.DroneBubbleUrgentSerious));
            socket.SetDialogue(line);
            Assert.That(played.Single().id, Is.EqualTo(expected));
            Assert.That(played.Single().pitch, Is.EqualTo(urgent ? 1f : 1.35f));
        }

        private DroneDialogueSocket Socket()
        {
            var go = NewObject("Socket");
            var group = go.AddComponent<CanvasGroup>();
            var text = NewObject("Text");
            text.transform.SetParent(go.transform, false);
            var label = text.AddComponent<TextMeshProUGUI>();
            var socket = go.AddComponent<DroneDialogueSocket>();
            Set(socket, "dialogueText", label);
            Set(socket, "canvasGroup", group);
            socket.ConfigureSfxForTests(() => now, id => float.PositiveInfinity, () => false);
            socket.SetVisible(true);
            socket.SetDialogue(new DroneDialogueResult("initial", "초기화", false, false));
            now += 1.1f;
            played.Clear();
            return socket;
        }

        [Test]
        public void Bubble_PriorityAlertSuppressesUrgentOnly_RescueReminderStaysRegular()
        {
            var socket = Socket();
            socket.ConfigureSfxForTests(() => now,
                id => id == SfxId.DronePriorityAlert ? 0.1f : float.PositiveInfinity, () => false);
            socket.SetDialogue(new DroneDialogueResult(DataIds.Dialogue.DroneGasWarning, "위험", false, false, true));
            Assert.That(played, Is.Empty);
            socket.SetDialogue(new DroneDialogueResult(DataIds.Dialogue.DroneEmergency, "구조 안내", false, false, false));
            Assert.That(played.Single().id, Is.EqualTo(SfxId.WorldFacilityTagBeep));
        }
    }
}
