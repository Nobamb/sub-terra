using System;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.Audio
{
    public sealed class PromptB1791SfxFeedbackFixTests
    {
        private const int Frequency = 44100;
        private const int LoopSamples = 7481;

        [Test]
        public void HeadEnd_UsesExactSampleCount_AndReleaseDuringHeadSkipsLoop()
        {
            const double start = 10.125;
            const int headSamples = 23991;
            var end = SliderDragTiming.HeadEnd(start, headSamples, Frequency);
            Assert.That((end - start) * Frequency, Is.EqualTo(headSamples).Within(0.000001));
            Assert.That(SliderDragTiming.ReleaseAt(start, end, LoopSamples, Frequency), Is.EqualTo(end));
            Assert.That(SliderDragTiming.ReleaseAt(end - 1d / Frequency, end, LoopSamples, Frequency), Is.EqualTo(end));
            Assert.That(SliderDragTiming.ReleaseAt(end, end, LoopSamples, Frequency), Is.EqualTo(end));
        }

        [TestCase(0.001, 1)]
        [TestCase(0.25, 1)]
        [TestCase(0.999, 1)]
        [TestCase(1.001, 2)]
        [TestCase(6.75, 7)]
        public void ReleaseDuringLoop_UsesNextWholeLoopEnd(double cycles, int expectedCycles)
        {
            const double loopStart = 10.544;
            var length = (double)LoopSamples / Frequency;
            var now = loopStart + cycles * length;
            var end = SliderDragTiming.ReleaseAt(now, loopStart, LoopSamples, Frequency);
            Assert.That(end, Is.EqualTo(loopStart + expectedCycles * length).Within(0.000000001));
            Assert.That(end, Is.GreaterThanOrEqualTo(now));
            Assert.That(end - now, Is.LessThanOrEqualTo(length));
        }

        [TestCase(0d, 3)]
        [TestCase(1d, 6)]
        [TestCase(10d, 3)]
        [TestCase(100d, 2)]
        [TestCase(10000d, 6)]
        public void ReleaseExactlyOnBoundary_DoesNotAddAnotherLoopForDoubleRounding(double loopStart, int cycles)
        {
            var boundary = loopStart + cycles * ((double)LoopSamples / Frequency);
            Assert.That(SliderDragTiming.ReleaseAt(boundary, loopStart, LoopSamples, Frequency),
                Is.EqualTo(boundary).Within(0.000000001));
        }

        [TestCase(SfxId.SettingsSliderDrag, 91, "settings_slider_drag", 0.698f)]
        [TestCase(SfxId.SettingsSliderDragHead, 92, "settings_slider_drag_head", 0.544f)]
        [TestCase(SfxId.SettingsSliderDragLoop, 93, "settings_slider_drag_loop", 0.1696f)]
        [TestCase(SfxId.SettingsSliderDragTail, 94, "settings_slider_drag_tail", 0.164f)]
        public void SliderSlots_MatchIdsDefaultsAssetFilesAndImport(SfxId id, int number, string file, float seconds)
        {
            Assert.That((int)id, Is.EqualTo(number));
            var row = SfxLibraryDefaults.Create().Single(pair => pair.Entry.Id == id);
            Assert.That(row.FileName, Is.EqualTo(file));
            AssertEntry(row.Entry, SfxBus.UI, 0.55f);

            var library = Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            Assert.That(library, Is.Not.Null);
            Assert.That(library.TryGetEntry(id, out var entry), Is.True);
            AssertEntry(entry, SfxBus.UI, 0.55f);
            Assert.That(entry.Clip, Is.Not.Null);
            var path = SfxLibraryDefaults.ClipRoot + "/UI/" + file + ".wav";
            Assert.That(AssetDatabase.GetAssetPath(entry.Clip), Is.EqualTo(path));
            Assert.That(entry.Clip.length, Is.EqualTo(seconds).Within(0.002f));
            Assert.That(entry.Clip.channels, Is.EqualTo(1));
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.forceToMono, Is.True);
            Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
            if (id == SfxId.SettingsSliderDrag) return;
            Assert.That(new SerializedObject(importer).FindProperty("m_Normalize").boolValue,
                Is.False, "조각별 피크 정규화는 이음새 음량을 바꾼다");
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.PCM));
            Assert.That(SfxLibraryDefaults.IsLoopFile(file), Is.False, "긴 반복용 압축 임포트와 구분한다");
            if (id == SfxId.SettingsSliderDragLoop)
            {
                Assert.That(entry.Clip.frequency, Is.EqualTo(Frequency));
                Assert.That(entry.Clip.samples, Is.EqualTo(LoopSamples));
            }
        }

        [Test]
        public void AddedThreeSlots_KeepUniqueIds_ExistingSlotsAndBusVolumes()
        {
            var defaults = SfxLibraryDefaults.Create();
            var library = Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            Assert.That(defaults.Count(), Is.EqualTo(102));
            Assert.That(defaults.Select(pair => pair.Entry.Id).Distinct().Count(), Is.EqualTo(102));
            Assert.That(library.Entries.Count, Is.EqualTo(102));
            Assert.That(library.Entries.Select(entry => entry.Id).Distinct().Count(), Is.EqualTo(102));
            foreach (SfxBus bus in Enum.GetValues(typeof(SfxBus)))
                Assert.That(library.GetBusVolume(bus), Is.EqualTo(1f), bus.ToString());
            foreach (var row in new[]
            {
                (SfxId.UiScanTick, 8, "ui_scan_tick", SfxBus.UI, 0.5f),
                (SfxId.QuestReward, 87, "quest_reward", SfxBus.UI, 0.5f),
                (SfxId.GlitchPopupOpen, 88, "glitch_popup_open", SfxBus.UI, 0.6f),
                (SfxId.GlitchPopupClose, 89, "glitch_popup_close", SfxBus.UI, 0.6f),
                (SfxId.DroneScanPing, 90, "drone_scan_ping", SfxBus.World, 0.3f)
            })
            {
                Assert.That((int)row.Item1, Is.EqualTo(row.Item2));
                var actual = defaults.Single(pair => pair.Entry.Id == row.Item1);
                Assert.That(actual.FileName, Is.EqualTo(row.Item3));
                Assert.That(actual.Entry.Bus, Is.EqualTo(row.Item4));
                Assert.That(actual.Entry.Volume, Is.EqualTo(row.Item5));
                Assert.That(library.TryGetEntry(row.Item1, out var entry), Is.True);
                Assert.That(entry.Clip.name, Is.EqualTo(row.Item3));
                Assert.That(entry.Bus, Is.EqualTo(row.Item4));
                Assert.That(entry.Volume, Is.EqualTo(row.Item5));
            }
        }

        private static void AssertEntry(SfxEntry entry, SfxBus bus, float volume)
        {
            Assert.That(entry.Bus, Is.EqualTo(bus));
            Assert.That(entry.Volume, Is.EqualTo(volume));
            Assert.That(entry.Pitch, Is.EqualTo(1f));
            Assert.That(entry.StartTime, Is.Zero);
            Assert.That(entry.MaxDuration, Is.Zero);
            Assert.That(entry.FadeOut, Is.Zero);
        }
    }
}
