using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.Save;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.Save
{
    public sealed class SaveSlotUiStaticTests
    {
        [OneTimeSetUp]
        public void BuildPrefab()
        {
            PhaseKSaveSlotPrefabBuilder.Build();
        }

        [Test]
        public void K_SaveSlotPanelPrefab_HasThreeSlotsAndRecoveryControls()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhaseKSaveSlotPrefabBuilder.PrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(
                prefab.GetComponent<SaveSlotPanelView>().HasRequiredReferences(),
                Is.True);
        }
    }
}
