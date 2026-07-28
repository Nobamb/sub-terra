using NUnit.Framework;
using SubTerra.Shared;
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
    }
}
