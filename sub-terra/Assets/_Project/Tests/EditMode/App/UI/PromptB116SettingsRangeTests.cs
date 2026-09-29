using NUnit.Framework;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared.Localization;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 116-1: 해상도·프레임 범위와 자동 해상도 저장.</summary>
    public sealed class PromptB116SettingsRangeTests
    {
        [Test]
        public void Defaults_UseAutomaticResolution()
        {
            var defaults = SettingsValues.CreateDefaults();
            Assert.That(defaults.ResolutionAutomatic, Is.True);
            Assert.That(defaults.ResolutionWidth, Is.EqualTo(1920));
            Assert.That(defaults.ResolutionHeight, Is.EqualTo(1080));
        }

        [Test]
        public void ResolutionList_IncludesLowerHigherAndUltrawide()
        {
            Assert.That(ResolutionPresets.All.Count, Is.EqualTo(11));
            Assert.That(ResolutionPresets.FindIndex(960, 540), Is.EqualTo(1));
            Assert.That(ResolutionPresets.FindIndex(1024, 768), Is.EqualTo(2));
            Assert.That(ResolutionPresets.FindIndex(800, 600), Is.EqualTo(3));
            Assert.That(ResolutionPresets.FindIndex(3840, 2160), Is.EqualTo(8));
            Assert.That(ResolutionPresets.FindIndex(2560, 1080), Is.EqualTo(9));
            Assert.That(ResolutionPresets.FindIndex(3440, 1440), Is.EqualTo(10));
            Assert.That(ResolutionPresets.FindIndex(0, 0), Is.EqualTo(6));
        }

        [Test]
        public void ApplyStored_OldSizeWithoutAutoFlag_StaysExplicit()
        {
            var values = SettingsValues.CreateDefaults();
            ResolutionPresets.ApplyStored(values, false, false, true, 1600, 900);
            Assert.That(values.ResolutionAutomatic, Is.False);
            Assert.That(values.ResolutionWidth, Is.EqualTo(1600));
            Assert.That(values.ResolutionHeight, Is.EqualTo(900));
        }

        [Test]
        public void ApplyStored_AutoFlag_IgnoresSavedPixels()
        {
            var values = SettingsValues.CreateDefaults();
            ResolutionPresets.ApplyStored(values, true, true, true, 1280, 720);
            Assert.That(values.ResolutionAutomatic, Is.True);
            Assert.That(values.ResolutionWidth, Is.EqualTo(1920));
        }

        [Test]
        public void ResolveAppliedResolution_AutoUsesNative_FixedUsesSaved()
        {
            var automatic = SettingsValues.CreateDefaults();
            int width;
            int height;
            SettingsRuntimeApplier.ResolveAppliedResolution(automatic, 2560, 1440, out width, out height);
            Assert.That(width, Is.EqualTo(2560));
            Assert.That(height, Is.EqualTo(1440));

            var fixedSize = SettingsValues.CreateDefaults();
            fixedSize.ResolutionAutomatic = false;
            fixedSize.ResolutionWidth = 800;
            fixedSize.ResolutionHeight = 600;
            SettingsRuntimeApplier.ResolveAppliedResolution(fixedSize, 2560, 1440, out width, out height);
            Assert.That(width, Is.EqualTo(800));
            Assert.That(height, Is.EqualTo(600));
        }

        [Test]
        public void Clone_CopiesAutomaticResolution()
        {
            var source = SettingsValues.CreateDefaults();
            source.ResolutionAutomatic = false;
            source.ResolutionWidth = 800;
            source.ResolutionHeight = 600;
            var clone = source.Clone();
            Assert.That(clone.ResolutionAutomatic, Is.False);
            Assert.That(clone.ResolutionWidth, Is.EqualTo(800));

            var other = SettingsValues.CreateDefaults();
            other.CopyFrom(source);
            Assert.That(other.ResolutionAutomatic, Is.False);
            Assert.That(other.ResolutionHeight, Is.EqualTo(600));
        }

        [Test]
        public void FrameRateLabels_FollowDisplayOrder()
        {
            var previous = LocalizationService.Current;
            LocalizationService.SetLanguage(GameLanguage.Korean);
            try
            {
                var labels = FrameRatePresets.BuildOptionLabels();
                Assert.That(labels.Count, Is.EqualTo(9));
                Assert.That(labels[0], Is.EqualTo("자동(기본값)"));
                Assert.That(labels[5], Is.EqualTo("165"));
                Assert.That(labels[6], Is.EqualTo("180"));
                Assert.That(labels[7], Is.EqualTo("240"));
                Assert.That(labels[8], Is.EqualTo("제한없음"));
            }
            finally
            {
                LocalizationService.SetLanguage(previous);
            }
        }

        [Test]
        public void DropdownReselect_ClosesOnlyWhenAlreadyOpen()
        {
            Assert.That(SettingsDropdownToggleClose.ShouldClose(true), Is.True);
            Assert.That(SettingsDropdownToggleClose.ShouldClose(false), Is.False);
        }
    }
}
