using NUnit.Framework;
using SubTerra.App.UI.MainMenu;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 33-2: 프레임 프리셋·적용 경로.</summary>
    public sealed class PromptB33_2SettingsFrameTests
    {
        [Test]
        public void FrameRatePresets_DefaultIsAuto()
        {
            var defaults = SettingsValues.CreateDefaults();
            Assert.That(defaults.FrameRate, Is.EqualTo(FrameRateMode.Auto));
            Assert.That(FrameRatePresets.ToIndex(FrameRateMode.Auto), Is.EqualTo(0));
            Assert.That(FrameRatePresets.UsesVSync(FrameRateMode.Auto), Is.True);
        }

        [Test]
        public void FrameRatePresets_FixedAndUnlimited()
        {
            Assert.That(FrameRatePresets.ToTargetFrameRate(FrameRateMode.Fps30), Is.EqualTo(30));
            Assert.That(FrameRatePresets.ToTargetFrameRate(FrameRateMode.Fps60), Is.EqualTo(60));
            Assert.That(FrameRatePresets.ToTargetFrameRate(FrameRateMode.Fps120), Is.EqualTo(120));
            Assert.That(FrameRatePresets.ToTargetFrameRate(FrameRateMode.Fps144), Is.EqualTo(144));
            Assert.That(FrameRatePresets.ToTargetFrameRate(FrameRateMode.Unlimited), Is.EqualTo(-1));
            Assert.That(FrameRatePresets.UsesVSync(FrameRateMode.Unlimited), Is.False);
            Assert.That(FrameRatePresets.UsesVSync(FrameRateMode.Fps60), Is.False);
        }

        [Test]
        public void SettingsValues_CloneCopiesFrameRate()
        {
            var source = SettingsValues.CreateDefaults();
            source.FrameRate = FrameRateMode.Fps120;
            var clone = source.Clone();
            Assert.That(clone.FrameRate, Is.EqualTo(FrameRateMode.Fps120));

            var other = SettingsValues.CreateDefaults();
            other.CopyFrom(source);
            Assert.That(other.FrameRate, Is.EqualTo(FrameRateMode.Fps120));
        }

        [Test]
        public void ApplyFrameRate_AutoEnablesVSync()
        {
            SettingsRuntimeApplier.ApplyFrameRate(FrameRateMode.Auto);
            Assert.That(QualitySettings.vSyncCount, Is.EqualTo(1));
            Assert.That(Application.targetFrameRate, Is.EqualTo(-1));
        }

        [Test]
        public void ApplyFrameRate_SixtyDisablesVSync()
        {
            SettingsRuntimeApplier.ApplyFrameRate(FrameRateMode.Fps60);
            Assert.That(QualitySettings.vSyncCount, Is.EqualTo(0));
            Assert.That(Application.targetFrameRate, Is.EqualTo(60));
        }

        [TearDown]
        public void RestoreDefaults()
        {
            SettingsRuntimeApplier.ApplyFrameRate(FrameRateMode.Auto);
        }
    }
}
