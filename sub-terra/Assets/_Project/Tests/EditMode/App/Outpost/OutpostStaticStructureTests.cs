using System;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Outpost;
using SubTerra.App.UI.Outpost;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.Outpost
{
    public sealed class OutpostStaticStructureTests
    {
        [Test]
        public void H_S01_ServiceContract_DoesNotExposePhysicsOrGameplayImplementationTypes()
        {
            var publicMethods = typeof(OutpostService).GetMethods(
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.DeclaredOnly);
            var exposedTypes = publicMethods
                .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType))
                .Concat(publicMethods.Select(method => method.ReturnType))
                .ToArray();

            Assert.That(exposedTypes.Any(type => type.Namespace != null
                && type.Namespace.StartsWith("SubTerra.Gameplay", StringComparison.Ordinal)), Is.False);
            Assert.That(exposedTypes.Any(type => typeof(UnityEngine.Object).IsAssignableFrom(type)), Is.False);
        }

        [Test]
        public void H_S02_S03_OutpostState_HasSeparateStorageAndNoUnityObjectFields()
        {
            var state = new OutpostState();
            Assert.That(state.Storage, Is.Not.Null);
            Assert.That(state.Storage.Count, Is.Zero);

            var fields = typeof(OutpostState).GetFields(
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic);
            Assert.That(fields.Any(field => typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)), Is.False);
        }

        [Test]
        public void H_OutpostPanelPrefab_ExistsWithRequiredReferences()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/OutpostPanel.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<OutpostPanelBinder>(), Is.Not.Null);
            var view = prefab.GetComponent<OutpostPanelView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasRequiredReferences(), Is.True);
        }
    }
}
