using System;
using System.Collections.Generic;
using SubTerra.App.Drone.Dialogue;

namespace SubTerra.App.AI
{
    /// <summary>세션 상한, 이벤트 상한, 중복 억제, 전체 쿨다운과 동시 요청 1개를 한 곳에서 판정한다.</summary>
    public sealed class CloudDialoguePolicy
    {
        private readonly object sync = new object();
        private readonly IDroneClock clock;
        private readonly CloudDialogueOptions options;
        private readonly Dictionary<CloudDialogueEvent, int> eventCalls =
            new Dictionary<CloudDialogueEvent, int>();
        private readonly Dictionary<CloudDialogueEvent, double> lastEventAt =
            new Dictionary<CloudDialogueEvent, double>();

        private int sessionCalls;
        private double lastAcceptedAt = double.NegativeInfinity;
        private bool requestInFlight;

        public int SessionCalls
        {
            get
            {
                lock (sync)
                {
                    return sessionCalls;
                }
            }
        }

        public CloudDialoguePolicy(IDroneClock droneClock, CloudDialogueOptions dialogueOptions)
        {
            clock = droneClock ?? throw new ArgumentNullException(nameof(droneClock));
            options = dialogueOptions
                ?? throw new ArgumentNullException(nameof(dialogueOptions));
        }

        public bool TryBegin(CloudDialogueEvent eventType, out IDisposable lease)
        {
            lock (sync)
            {
                lease = null;
                if (!IsAllowedEvent(eventType)
                    || requestInFlight
                    || sessionCalls >= options.MaxSessionCalls
                    || clock.Now - lastAcceptedAt < options.GlobalCooldownSeconds)
                {
                    return false;
                }

                eventCalls.TryGetValue(eventType, out var eventCallCount);
                if (eventCallCount >= options.MaxCallsPerEvent)
                {
                    return false;
                }

                if (lastEventAt.TryGetValue(eventType, out var previous)
                    && clock.Now - previous < options.DuplicateEventWindowSeconds)
                {
                    return false;
                }

                requestInFlight = true;
                sessionCalls++;
                eventCalls[eventType] = eventCallCount + 1;
                lastAcceptedAt = clock.Now;
                lastEventAt[eventType] = clock.Now;
                lease = new RequestLease(this);
                return true;
            }
        }

        private static bool IsAllowedEvent(CloudDialogueEvent eventType)
        {
            return eventType >= CloudDialogueEvent.NewDepthZone
                && eventType <= CloudDialogueEvent.ManualAnalysis;
        }

        private void EndRequest()
        {
            lock (sync)
            {
                requestInFlight = false;
            }
        }

        private sealed class RequestLease : IDisposable
        {
            private CloudDialoguePolicy owner;

            public RequestLease(CloudDialoguePolicy policy)
            {
                owner = policy;
            }

            public void Dispose()
            {
                var current = owner;
                owner = null;
                current?.EndRequest();
            }
        }
    }
}
