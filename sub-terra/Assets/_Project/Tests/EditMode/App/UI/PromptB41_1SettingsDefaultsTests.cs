using NUnit.Framework;
using SubTerra.App.UI.MainMenu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 41-1: 새 설정의 초기 마스터 음량은 50%다.</summary>
    public sealed class PromptB41_1SettingsDefaultsTests
    {
        [TestCase("Assets/_Project/Prefabs/UI/MainMenuPanel.prefab")]
        [TestCase("Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab")]
        public void SettingsPanel_InitialVolumeAndLabel_AreFiftyPercent(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var settingsPanel = prefab.transform.Find("SettingsPanel");
            var slider = settingsPanel.GetComponentInChildren<Slider>(true);
            var label = settingsPanel.Find("MasterVolumeLabel").GetComponent<TMP_Text>();

            Assert.That(slider.value, Is.EqualTo(0.5f));
            Assert.That(label.text, Is.EqualTo("마스터 음량: 50%"));
        }

        [Test]
        public void SettingsValues_DefaultMasterVolume_IsFiftyPercent()
        {
            Assert.That(SettingsValues.CreateDefaults().MasterVolume, Is.EqualTo(0.5f));
        }
    }
}
