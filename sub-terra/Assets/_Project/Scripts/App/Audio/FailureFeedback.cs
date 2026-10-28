using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Audio
{
    /// <summary>
    /// B-168: 실패음 공통 입구. 같은 프레임에는 어떤 실패음이든 한 번만 내고,
    /// 같은 key는 0.3초 안에 다시 오면 무시한다(연타만 거르고 다시 시도할 때마다 난다).
    /// </summary>
    public static class FailureFeedback
    {
        public const float SameKeySeconds = 0.3f;

        private static readonly Dictionary<string, float> lastPlayedAt = new Dictionary<string, float>();
        private static Func<float> clock = DefaultClock;
        private static Func<int> frame = DefaultFrame;
        private static int lastFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ResetForTests(null, null);
        }

        /// <returns>실제로 재생 요청을 보냈으면 true.</returns>
        public static bool Play(SfxId id, string key)
        {
            if (id == SfxId.None)
            {
                return false;
            }

            var currentFrame = frame();
            if (currentFrame == lastFrame)
            {
                return false;
            }

            var now = clock();
            var slot = key ?? string.Empty;
            if (lastPlayedAt.TryGetValue(slot, out var last) && now - last < SameKeySeconds)
            {
                return false;
            }

            lastFrame = currentFrame;
            lastPlayedAt[slot] = now;
            SfxPlayer.Play(id);
            return true;
        }

        /// <summary>테스트용. 시계·프레임을 주입하고 기록을 비운다. null이면 실제 값을 쓴다.</summary>
        public static void ResetForTests(Func<float> testClock, Func<int> testFrame)
        {
            clock = testClock ?? DefaultClock;
            frame = testFrame ?? DefaultFrame;
            lastFrame = -1;
            lastPlayedAt.Clear();
        }

        private static float DefaultClock() => Time.realtimeSinceStartup;

        private static int DefaultFrame() => Time.frameCount;
    }

    /// <summary>B-168: 시설 설치·이동 성공과 설치 실패 소리 고르기.</summary>
    public static class BuildingPlacementSfx
    {
        public const string PlaceFailureKey = "building.place";
        public const string MoveFailureKey = "building.move";

        /// <summary>사다리·버팀목·조명은 짧은 "철컥", 그 밖 시설은 묵직한 "쿵-철컹".</summary>
        public static SfxId ResolvePlaced(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId))
            {
                return SfxId.None;
            }

            switch (buildingId)
            {
                case DataIds.Buildings.LadderBasic:
                case DataIds.Buildings.SupportBasic:
                case DataIds.Buildings.LightBasic:
                    return SfxId.BuildPlaceSmall;
                default:
                    return SfxId.BuildPlaceHeavy;
            }
        }

        /// <summary>자원 부족은 "띵-딩", 겹침·바닥 없음·범위 밖·엘리베이터 자리 등 놓을 수 없는 자리는 "부-".</summary>
        public static SfxId ResolveFailure(bool resourceShort, string reasonId)
        {
            return resourceShort || reasonId == "cannot_afford" || reasonId == "spend_failed"
                ? SfxId.BuildFailResource
                : SfxId.BuildFailBlocked;
        }
    }
}
