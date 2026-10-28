using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Editor.DataValidation;
using SubTerra.Shared.Audio;
using UnityEditor;

namespace SubTerra.App.Tests.Audio
{
    /// <summary>B-168-1: 고친 효과음 네 칸의 기본값과 Resources 자산을 고정한다.</summary>
    public sealed class PromptB168_1SfxFixTests
    {
        [TestCase(SfxId.GasPulseLoop, 0.38f, 0f)]
        [TestCase(SfxId.RescueHoloOpen, 0.8f, 0.12f)]
        [TestCase(SfxId.BuildPlaceSmall, 0.9f, 0f)]
        [TestCase(SfxId.BuildPlaceHeavy, 1.0f, 0f)]
        public void FixedSlot_DefaultsAndResourcesAssetMatch(SfxId id, float volume, float startTime)
        {
            var defaults = new Dictionary<SfxId, SfxEntry>();
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                defaults[pair.Entry.Id] = pair.Entry;
            }

            Assert.That(defaults.TryGetValue(id, out var entry), Is.True, id.ToString());
            Assert.That(entry.Bus, Is.EqualTo(SfxBus.World), id.ToString());
            Assert.That(entry.Volume, Is.EqualTo(volume).Within(0.0001f), id.ToString());
            Assert.That(entry.StartTime, Is.EqualTo(startTime).Within(0.0001f), id.ToString());

            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(PromptB156SfxLibraryBuilder.AssetPath);
            Assert.That(library, Is.Not.Null);
            Assert.That(library.TryGetEntry(id, out var asset), Is.True, id.ToString());
            Assert.That(asset.Bus, Is.EqualTo(SfxBus.World), id.ToString());
            Assert.That(asset.Volume, Is.EqualTo(volume).Within(0.0001f), id.ToString());
            Assert.That(asset.StartTime, Is.EqualTo(startTime).Within(0.0001f), id.ToString());
            Assert.That(asset.Volume, Is.EqualTo(entry.Volume).Within(0.0001f), id.ToString());
            Assert.That(asset.StartTime, Is.EqualTo(entry.StartTime).Within(0.0001f), id.ToString());
        }
    }
}
