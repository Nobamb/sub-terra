using System;
using System.Collections.Generic;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Audio
{
    /// <summary>
    /// B-169: 같은 소리를 정해 둔 간격 안에 한 번만 낸다(연타·여러 노드 동시 해금).
    /// 소리마다 따로 센다. 시계는 주입할 수 있고, 기본은 비스케일 실제 시각이다.
    /// </summary>
    public sealed class SfxRepeatGate
    {
        private readonly Dictionary<SfxId, float> lastPlayedAt = new Dictionary<SfxId, float>();
        private readonly float minInterval;

        public SfxRepeatGate(float minInterval, Func<float> clock = null)
        {
            this.minInterval = minInterval;
            Clock = clock ?? (() => Time.realtimeSinceStartup);
        }

        public Func<float> Clock { get; set; }

        /// <returns>재생 요청을 보냈으면 true.</returns>
        public bool TryPlay(SfxId id)
        {
            return TryPlay(id, Clock());
        }

        /// <summary>시각을 직접 넘긴다(예정된 시간표 시각으로 판정할 때).</summary>
        public bool TryPlay(SfxId id, float now)
        {
            if (id == SfxId.None)
            {
                return false;
            }

            if (lastPlayedAt.TryGetValue(id, out var last) && SfxPlayback.IsThrottled(now, last, minInterval))
            {
                return false;
            }

            lastPlayedAt[id] = now;
            SfxPlayer.Play(id);
            return true;
        }

        public void Reset()
        {
            lastPlayedAt.Clear();
        }
    }
}
