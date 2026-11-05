using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Integration;
using SubTerra.Gameplay.Power;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Tests.Audio
{
    /// <summary>B-186(#292): 시설 전력 켜짐 정전기 소리. BatchStarted만 듣고, 복원 중에는 무음.</summary>
    public sealed class PromptB186PowerOnSfxTests
    {
        private readonly List<(SfxId id, float scale)> played = new();
        private PowerOnSfx sfx;
        private float now;
        private float sinceConfirm;

        [SetUp]
        public void SetUp()
        {
            played.Clear();
            now = 10f;
            sinceConfirm = float.PositiveInfinity;
            sfx = new PowerOnSfx(
                id => id == PowerOnSfx.CctvConnectSfx ? sinceConfirm : float.PositiveInfinity,
                () => now,
                (id, _) => played.Add((id, 1f)),
                (id, _, scale) => played.Add((id, scale)));
            sfx.Bind();
        }

        [TearDown]
        public void TearDown()
        {
            sfx.Dispose();
            while (PowerOnEffect.IsSuppressed) PowerOnEffect.EndSuppression();
        }

        private static void Raise(int count)
        {
            var field = typeof(PowerOnEffect).GetField("BatchStarted", BindingFlags.Static | BindingFlags.NonPublic);
            var handler = (System.Action<int, Vector3>)field.GetValue(null);
            handler?.Invoke(count, Vector3.zero);
        }

        [Test]
        public void BatchOfOne_PlaysOnce()
        {
            Raise(1);
            Assert.That(played, Is.EqualTo(new[] { (SfxId.PowerOnStatic, 1f) }));
        }

        [Test]
        public void BatchOfThree_StillPlaysOnce_AtSameVolume()
        {
            Raise(3);
            Assert.That(played, Is.EqualTo(new[] { (SfxId.PowerOnStatic, 1f) }));
        }

        [Test]
        public void TwoWithinMinGap_PlaysOnce_ThenAfterGapPlaysAgain()
        {
            Raise(1);
            now += PowerOnSfx.PowerOnMinGap - 0.01f;
            Raise(1);
            Assert.That(played.Count, Is.EqualTo(1));

            now += 0.02f;
            Raise(1);
            Assert.That(played.Count, Is.EqualTo(2));
        }

        [Test]
        public void DuringSuppression_IsSilent()
        {
            PowerOnEffect.BeginSuppression();
            Raise(2);
            PowerOnEffect.EndSuppression();
            Assert.That(played, Is.Empty);

            Raise(2);
            Assert.That(played.Count, Is.EqualTo(1), "복원이 끝나면 다시 난다");
        }

        [Test]
        public void RightAfterCctvConnect_PlaysDucked()
        {
            sinceConfirm = PowerOnSfx.PowerOnStaticCctvGap - 0.01f;
            Raise(1);
            Assert.That(played, Is.EqualTo(new[] { (SfxId.PowerOnStatic, PowerOnSfx.PowerOnStaticDuckedVolume) }));

            now += 1f;
            sinceConfirm = PowerOnSfx.PowerOnStaticCctvGap + 0.01f;
            Raise(1);
            Assert.That(played[1], Is.EqualTo((SfxId.PowerOnStatic, 1f)));
        }

        [Test]
        public void AfterDispose_NoPlayback()
        {
            sfx.Dispose();
            Raise(1);
            Assert.That(played, Is.Empty);
        }

        [Test]
        public void Bind_Twice_DoesNotDoublePlay()
        {
            sfx.Bind();
            Raise(1);
            Assert.That(played.Count, Is.EqualTo(1));
        }

        [Test]
        public void SfxId_AddsOnlyPowerOnStatic_AndKeepsExistingValues()
        {
            Assert.That((int)SfxId.PowerOnStatic, Is.EqualTo(106));
            Assert.That((int)SfxId.LockedNodeRattle, Is.EqualTo(95));
            Assert.That((int)SfxId.ScanMarkClick, Is.EqualTo(99));
            Assert.That((int)SfxId.UnstableBlockShakeLoop, Is.EqualTo(101));
            Assert.That((int)SfxId.DroneBubbleUrgentSerious, Is.EqualTo(105));
            var values = System.Enum.GetValues(typeof(SfxId)).Cast<int>().ToList();
            Assert.That(values.Count(v => v == 106), Is.EqualTo(1));
            Assert.That(values.Max(), Is.EqualTo(106));
        }

        [Test]
        public void Defaults_RegisterWorldBusAt08()
        {
            var pair = SfxLibraryDefaults.Create().Single(p => p.Entry.Id == SfxId.PowerOnStatic);
            Assert.That(pair.FileName, Is.EqualTo("power_on_static"));
            Assert.That(pair.Entry.Bus, Is.EqualTo(SfxBus.World));
            Assert.That(pair.Entry.Volume, Is.EqualTo(0.8f));
        }

        [Test]
        public void LibraryAsset_HasEntry_AndWavExists()
        {
            var library = Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            Assert.That(library.TryGetEntry(SfxId.PowerOnStatic, out var entry), Is.True);
            Assert.That(entry.Bus, Is.EqualTo(SfxBus.World));
            Assert.That(entry.Volume, Is.EqualTo(0.8f));

            var wav = Path.Combine(Application.dataPath, "_Project/Audio/SFX/World/power_on_static.wav");
            if (!File.Exists(wav)) Assert.Ignore("미검증: power_on_static.wav 아직 없음(PM이 넣기 전)");
            Assert.That(entry.Clip, Is.Not.Null);
            Assert.That(entry.Clip.name, Is.EqualTo("power_on_static"));
            Assert.That(entry.Clip.length, Is.InRange(0.3f, 1.0f));
        }
    }
}
