using System;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.State
{
    /// <summary>
    /// B-208: 지금 진행 중인 게임의 캐릭터 성별을 그림 쪽(플레이어·가이드 시연)에 알린다.
    /// 값의 원본은 GameState.Player.Gender이고, SaveRuntimeController가 상태를 붙이거나 성별이 바뀔 때 갱신한다.
    /// </summary>
    public static class PlayerGenderPresentation
    {
        public static PlayerGender Current { get; private set; }

        /// <summary>바뀔 때마다 1씩 늘어난다. 매 프레임 비교용.</summary>
        public static int Version { get; private set; }

        public static event Action<PlayerGender> Changed;

        public static void Set(PlayerGender gender)
        {
            var next = gender == PlayerGender.Female ? PlayerGender.Female : PlayerGender.Male;
            if (next == Current)
            {
                return;
            }

            Current = next;
            Version++;
            Changed?.Invoke(next);
        }

        /// <summary>테스트·도메인 리로드 없는 Play 진입에서 남자 기본값으로 돌린다.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetForTests()
        {
            Changed = null;
            Current = PlayerGender.Male;
            Version++;
        }
    }
}
