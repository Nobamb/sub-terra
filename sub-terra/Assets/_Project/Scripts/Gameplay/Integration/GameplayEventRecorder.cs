using System.Collections.Generic;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Integration
{
    /// <summary>Demo/test-only sink. B replaces it with its App-facing event sink in the final assembly.</summary>
    public sealed class GameplayEventRecorder : MonoBehaviour, IGameplayEventSink
    {
        private readonly List<GameplayEventDto> events = new();
        public IReadOnlyList<GameplayEventDto> Events => events;
        public void Publish(GameplayEventDto gameplayEvent)
        {
            if (gameplayEvent != null) events.Add(gameplayEvent);
        }
    }
}
