using System;
using System.Collections.Generic;
using SubTerra.Shared;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 단일 A Producer 이벤트를 여러 B Consumer sink로 전달한다.
    /// null sink는 건너뛰며, 한 sink 예외가 다른 sink를 막지 않는다.
    /// </summary>
    public sealed class IntegrationEventFanOut : IGameplayEventSink
    {
        private readonly List<IGameplayEventSink> sinks = new List<IGameplayEventSink>();

        public int SinkCount => sinks.Count;
        public int PublishCount { get; private set; }

        public IntegrationEventFanOut(params IGameplayEventSink[] initialSinks)
        {
            if (initialSinks == null)
            {
                return;
            }

            for (var i = 0; i < initialSinks.Length; i++)
            {
                Add(initialSinks[i]);
            }
        }

        public void Add(IGameplayEventSink sink)
        {
            if (sink == null || ReferenceEquals(sink, this) || sinks.Contains(sink))
            {
                return;
            }

            sinks.Add(sink);
        }

        public void Clear()
        {
            sinks.Clear();
        }

        public void Publish(GameplayEventDto gameplayEvent)
        {
            if (gameplayEvent == null)
            {
                return;
            }

            PublishCount++;
            for (var i = 0; i < sinks.Count; i++)
            {
                try
                {
                    sinks[i]?.Publish(gameplayEvent);
                }
                catch (Exception)
                {
                    // 개별 Consumer 실패가 다른 HUD/Service 갱신을 막지 않게 한다.
                }
            }
        }
    }
}
