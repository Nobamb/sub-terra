using NUnit.Framework;
using SubTerra.Gameplay.Integration;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Integration.Tests
{
    public sealed class GameplayEventRecorderTests
    {
        [Test]
        public void Publish_RecordsNonNullGameplayEvent()
        {
            GameObject host = new("Recorder");
            GameplayEventRecorder recorder = host.AddComponent<GameplayEventRecorder>();
            recorder.Publish(new GameplayEventDto { type = GameplayEventType.TileMined, entityId = "tile.copper" });

            Assert.That(recorder.Events, Has.Count.EqualTo(1));
            Assert.That(recorder.Events[0].type, Is.EqualTo(GameplayEventType.TileMined));
            Object.DestroyImmediate(host);
        }
    }
}
