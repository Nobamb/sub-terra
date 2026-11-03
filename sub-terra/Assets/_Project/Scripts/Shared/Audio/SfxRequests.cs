using System;

namespace SubTerra.Shared.Audio
{
    /// <summary>
    /// 효과음 재생 요청 창구. Gameplay는 이 클래스만 부르고, 듣는 쪽(App의 SfxPlayer)이 없으면 아무 일도 없다.
    /// </summary>
    public static class SfxRequests
    {
        /// <summary>(소리, 피치 배율)</summary>
        public static event Action<SfxId, float> PlayRequested;

        /// <summary>B-181: (소리, 피치 배율, 음량 배율 0~1). 음량 배율을 넘기는 요청만 이 길로 온다.</summary>
        public static event Action<SfxId, float, float> ScaledPlayRequested;

        /// <summary>(소리, 반복 횟수, 반복 간격(초), 피치 배율)</summary>
        public static event Action<SfxId, int, float, float> AlarmRequested;

        public static void Play(SfxId id, float pitch = 1f)
        {
            if (id == SfxId.None)
            {
                return;
            }

            PlayRequested?.Invoke(id, pitch);
        }

        /// <summary>B-181: 칸 음량을 바꾸지 않고 이번 재생만 작게 낸다. 배율은 0~1로 자른다.</summary>
        public static void Play(SfxId id, float pitch, float volumeScale)
        {
            if (id == SfxId.None)
            {
                return;
            }

            ScaledPlayRequested?.Invoke(id, pitch, ClampVolumeScale(volumeScale));
        }

        public static float ClampVolumeScale(float volumeScale)
        {
            if (float.IsNaN(volumeScale) || volumeScale <= 0f)
            {
                return 0f;
            }

            return volumeScale >= 1f ? 1f : volumeScale;
        }

        public static void PlayAlarm(SfxId id, int repeat, float interval, float pitch)
        {
            if (id == SfxId.None || repeat <= 0)
            {
                return;
            }

            AlarmRequested?.Invoke(id, repeat, interval, pitch);
        }
    }
}
