using UnityEngine;

namespace SubTerra.App.UI.GameplayCursor
{
    /// <summary>시설 옮기기 상태를 커서에 알려 주는 쪽(통합 계층의 배치 브리지).</summary>
    public interface IGameplayCursorStateSource
    {
        /// <summary>우클릭·메뉴로 시설 옮기기가 켜져 있는지(대상 고르기 포함).</summary>
        bool CursorMoveMode { get; }

        /// <summary>시설을 집어 옮기는 중인지.</summary>
        bool CursorCarrying { get; }

        /// <summary>커서 아래에 옮길 수 있는 시설이 있는지(이동 안내 표시 조건과 같다).</summary>
        bool CursorHoverGrabbable { get; }
    }

    /// <summary>App 커서가 통합 계층을 직접 참조하지 않도록 상태 제공자를 등록받는다.</summary>
    public static class GameplayCursorState
    {
        public static IGameplayCursorStateSource Source { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Source = null;
        }

        public static void Register(IGameplayCursorStateSource source)
        {
            if (source != null) Source = source;
        }

        public static void Unregister(IGameplayCursorStateSource source)
        {
            if (ReferenceEquals(Source, source)) Source = null;
        }

        public static GameplayCursorInput Read(bool leftHeld)
        {
            var input = new GameplayCursorInput { LeftHeld = leftHeld };
            var source = Source;
            // 파괴된 MonoBehaviour 제공자는 Unity null 비교로 걸러낸다.
            if (source is Object unityObject && unityObject == null) source = null;
            if (source == null) return input;
            input.MoveMode = source.CursorMoveMode;
            input.Carrying = source.CursorCarrying;
            input.HoverGrabbable = source.CursorHoverGrabbable;
            return input;
        }
    }
}
