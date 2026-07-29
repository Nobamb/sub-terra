using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.UI.Drone;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.Drone
{
    public sealed class DroneUiStaticStructureTests
    {
        [OneTimeSetUp]
        public void BuildAssets()
        {
            PhaseIDroneUiPrefabBuilder.BuildAll();
        }

        [Test]
        public void RequiredDroneUiPrefabs_HaveWiredViewsAndCompositeBinder()
        {
            var dialogue = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/DroneDialoguePanel.prefab");
            var reason = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/DroneReasonPanel.prefab");
            var composite = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/DroneAnalysisUI.prefab");

            Assert.That(
                dialogue.GetComponent<DroneDialoguePanelView>().HasRequiredReferences(),
                Is.True);
            Assert.That(
                reason.GetComponent<DroneReasonPanelView>().HasRequiredReferences(),
                Is.True);
            Assert.That(composite.GetComponent<DroneUiBinder>().HasRequiredReferences(), Is.True);
        }

        [Test]
        public void IntegrationAdapter_ImplementsSharedProvider_WithoutChangingGameplay()
        {
            Assert.That(
                typeof(IDroneContextProvider).IsAssignableFrom(
                    typeof(DroneContextProviderAdapter)),
                Is.True);
        }
    }
}
