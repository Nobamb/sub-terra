using NUnit.Framework;
using SubTerra.Shared.Localization;

namespace SubTerra.Shared.Tests
{
    public sealed class LocalizationServiceTests
    {
        [Test]
        public void DefaultLanguage_IsKorean_AndEnglishFallbackWorks()
        {
            LocalizationService.SetLanguage(GameLanguage.Korean);
            Assert.That(
                LocalizationService.Get("settings.master_volume"),
                Is.EqualTo("마스터 음량"));
            Assert.That(
                LocalizationService.FormatMasterVolume(1f),
                Is.EqualTo("마스터 음량: 100%"));

            LocalizationService.SetLanguage(GameLanguage.English);
            Assert.That(
                LocalizationService.Get("settings.master_volume"),
                Is.EqualTo("Master Volume"));
            Assert.That(
                LocalizationService.FormatMasterVolume(0.5f),
                Is.EqualTo("Master Volume: 50%"));

            // 테스트 후 기본 한국어로 복구.
            LocalizationService.SetLanguage(GameLanguage.Korean);
        }
    }
}
