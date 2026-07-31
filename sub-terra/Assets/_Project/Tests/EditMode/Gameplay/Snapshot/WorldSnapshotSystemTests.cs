using NUnit.Framework;
using SubTerra.Shared;
using System.Reflection;
using UnityEngine;

namespace SubTerra.Gameplay.Snapshot.Tests
{
    public sealed class WorldSnapshotSystemTests
    {
        [Test]
        public void CaptureSnapshot_ReturnsInitializedChangeCollections()
        {
            GameObject host = new("Snapshot");
            WorldSnapshotSystem system = host.AddComponent<WorldSnapshotSystem>();

            WorldSnapshotDto snapshot = system.CaptureSnapshot();

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.miningChanges, Is.Not.Null);
            Assert.That(snapshot.collapseChanges, Is.Not.Null);
            Assert.That(snapshot.buildings, Is.Not.Null);
            Assert.That(snapshot.gasChanges, Is.Not.Null);
            Object.DestroyImmediate(host);
        }

        [Test]
        public void RestoreSnapshot_AllowsNullSnapshot()
        {
            GameObject host = new("Snapshot");
            WorldSnapshotSystem system = host.AddComponent<WorldSnapshotSystem>();

            Assert.DoesNotThrow(() => system.RestoreSnapshot(null));
            Object.DestroyImmediate(host);
        }

        [Test]
        public void CaptureAndRestore_PreserveBaseWorldGeneratorIdentity()
        {
            GameObject host = new("Snapshot");
            WorldSnapshotSystem system = host.AddComponent<WorldSnapshotSystem>();
            BaseWorldGeneratorSpy generator = host.AddComponent<BaseWorldGeneratorSpy>();
            SetField(system, "baseWorldGeneratorBehaviour", generator);
            system.ConfigureBaseWorldIdentity(7123L, 4);

            WorldSnapshotDto captured = system.CaptureSnapshot();
            Assert.That(captured.worldSeed, Is.EqualTo(7123L));
            Assert.That(captured.generatorVersion, Is.EqualTo(4));

            system.RestoreSnapshot(captured);
            Assert.That(generator.CallCount, Is.EqualTo(1));
            Assert.That(generator.LastSeed, Is.EqualTo(7123L));
            Assert.That(generator.LastVersion, Is.EqualTo(4));
            Object.DestroyImmediate(host);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private sealed class BaseWorldGeneratorSpy : MonoBehaviour, IWorldBaseGenerator
        {
            public int CallCount { get; private set; }
            public long LastSeed { get; private set; }
            public int LastVersion { get; private set; }

            public bool Regenerate(long worldSeed, int generatorVersion)
            {
                CallCount++;
                LastSeed = worldSeed;
                LastVersion = generatorVersion;
                return true;
            }
        }
    }
}
