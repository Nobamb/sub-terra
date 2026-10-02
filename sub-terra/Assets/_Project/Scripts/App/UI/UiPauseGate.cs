using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI
{
    /// <summary>
    /// 시간 정지 소유권 목록. 같은 소유자는 한 번만 잡히고,
    /// 해제할 때는 자기가 적용한 정지만 되돌려 다른 시스템의 정지 상태를 보존한다.
    /// </summary>
    public sealed class PauseOwnership
    {
        private readonly HashSet<string> owners = new HashSet<string>();
        private readonly Func<float> readScale;
        private readonly Action<float> writeScale;
        private float restoreScale = 1f;
        private bool appliedFreeze;

        public PauseOwnership(Func<float> readScale, Action<float> writeScale)
        {
            this.readScale = readScale;
            this.writeScale = writeScale;
        }

        public bool IsHeld => owners.Count > 0;
        public bool IsHeldBy(string owner) => !string.IsNullOrEmpty(owner) && owners.Contains(owner);
        public event Action Changed;

        public bool Acquire(string owner)
        {
            if (string.IsNullOrEmpty(owner) || !owners.Add(owner))
            {
                return false;
            }

            if (owners.Count == 1)
            {
                var current = readScale();
                // 이미 0이면 다른 시스템의 정지이므로 건드리지 않고, 풀 때도 되돌리지 않는다.
                appliedFreeze = current > 0f;
                if (appliedFreeze)
                {
                    restoreScale = current;
                    writeScale(0f);
                }
            }

            Changed?.Invoke();
            return true;
        }

        public bool Release(string owner)
        {
            if (string.IsNullOrEmpty(owner) || !owners.Remove(owner))
            {
                return false;
            }

            if (owners.Count == 0 && appliedFreeze)
            {
                appliedFreeze = false;
                // 그 사이 다른 시스템이 값을 바꿨다면 그 값을 존중한다.
                if (readScale() <= 0f)
                {
                    writeScale(restoreScale);
                }
            }

            Changed?.Invoke();
            return true;
        }

        public void ReleaseAll()
        {
            var snapshot = new List<string>(owners);
            for (var i = 0; i < snapshot.Count; i++)
            {
                Release(snapshot[i]);
            }
        }
    }

    /// <summary>
    /// 팝업 연출이 게임 진행을 멈출 때 쓰는 전역 정지 게이트.
    /// UI 연출은 Time.unscaledDeltaTime을 쓰므로 정지 중에도 재생된다.
    /// 실시간 시계(광산 초기화 타이머)와 키 입력 처리는 IsHeld를 직접 확인한다.
    /// </summary>
    public static class UiPauseGate
    {
        private static readonly PauseOwnership Shared = new PauseOwnership(
            () => Time.timeScale,
            value => Time.timeScale = value);

        public static bool IsHeld => Shared.IsHeld;

        public static event Action Changed
        {
            add => Shared.Changed += value;
            remove => Shared.Changed -= value;
        }

        public static bool Acquire(string owner) => Shared.Acquire(owner);
        public static bool Release(string owner) => Shared.Release(owner);
        public static bool IsHeldBy(string owner) => Shared.IsHeldBy(owner);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession()
        {
            // 도메인 리로드를 끈 에디터에서 이전 세션의 정지가 남지 않게 한다.
            Shared.ReleaseAll();
            Application.quitting -= Shared.ReleaseAll;
            Application.quitting += Shared.ReleaseAll;
        }
    }
}
