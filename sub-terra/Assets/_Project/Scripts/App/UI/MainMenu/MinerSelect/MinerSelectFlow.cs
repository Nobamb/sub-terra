using SubTerra.Shared;

namespace SubTerra.App.UI.MainMenu
{
    public enum MinerSelectResult
    {
        Ignored = 0,
        Focused = 1,
        /// <summary>처음 고름. 안내 글자가 이때만 사라진다.</summary>
        Selected = 2,
        /// <summary>다른 카드로 바꿔 고름.</summary>
        Switched = 3,
        /// <summary>이미 고른 카드를 다시 누름(변화 없음).</summary>
        Reselected = 4,
        Confirmed = 5,
        /// <summary>고르기 전에 탐사 시작을 누름.</summary>
        Rejected = 6,
        Back = 7
    }

    /// <summary>
    /// B-209 광부 선택 화면의 순수 상태. 카드 0=남자, 1=여자.
    /// 확정·돌아가기는 한 번만 통과하고, 그 뒤 입력은 모두 무시한다(연타·전환 중 입력 방지).
    /// </summary>
    public sealed class MinerSelectFlow
    {
        public const int CardCount = 2;

        private int focusIndex = -1;
        private int selectedIndex = -1;
        private bool selectedOnce;
        private bool confirmed;
        private bool closed;

        public int FocusIndex => focusIndex;
        public int SelectedIndex => selectedIndex;
        public bool HasSelection => selectedIndex >= 0;
        public PlayerGender Selected => GenderAt(selectedIndex);
        /// <summary>안내 글자 `광부를 선택해 주세요`. 처음 고른 뒤로는 바꿔 골라도 다시 나오지 않는다.</summary>
        public bool HintVisible => !selectedOnce;
        public bool IsConfirmed => confirmed;
        public bool IsClosed => closed;
        public bool IsLocked => confirmed || closed;
        public bool CanConfirm => HasSelection && !IsLocked;

        public static PlayerGender GenderAt(int index) => index == 1 ? PlayerGender.Female : PlayerGender.Male;

        public static int IndexOf(PlayerGender gender) => gender == PlayerGender.Female ? 1 : 0;

        /// <summary>화면을 열 때마다 처음 상태(둘 다 실루엣, 안내 글자 보임)로 되돌린다.</summary>
        public void Reset()
        {
            focusIndex = -1;
            selectedIndex = -1;
            selectedOnce = false;
            confirmed = false;
            closed = false;
        }

        /// <summary>←/→ 카드 이동. 끝에서 넘어가지 않는다.</summary>
        public MinerSelectResult MoveFocus(int direction)
        {
            if (IsLocked || direction == 0) return MinerSelectResult.Ignored;
            int next;
            if (focusIndex < 0)
            {
                next = selectedIndex >= 0 ? selectedIndex : (direction > 0 ? 0 : CardCount - 1);
                if (selectedIndex >= 0) next = Clamp(next + (direction > 0 ? 1 : -1));
            }
            else
            {
                next = Clamp(focusIndex + (direction > 0 ? 1 : -1));
            }

            if (next == focusIndex) return MinerSelectResult.Ignored;
            focusIndex = next;
            return MinerSelectResult.Focused;
        }

        /// <summary>마우스 올림. 포커스만 옮긴다.</summary>
        public MinerSelectResult Hover(int index)
        {
            if (IsLocked || !IsValid(index) || focusIndex == index) return MinerSelectResult.Ignored;
            focusIndex = index;
            return MinerSelectResult.Focused;
        }

        /// <summary>마우스가 카드를 떠남. 그 카드에 있던 포커스를 푼다.</summary>
        public void Unhover(int index)
        {
            if (!IsLocked && focusIndex == index) focusIndex = -1;
        }

        public MinerSelectResult Select(int index)
        {
            if (IsLocked || !IsValid(index)) return MinerSelectResult.Ignored;
            focusIndex = index;
            if (selectedIndex == index) return MinerSelectResult.Reselected;
            var first = !selectedOnce;
            selectedIndex = index;
            selectedOnce = true;
            return first ? MinerSelectResult.Selected : MinerSelectResult.Switched;
        }

        /// <summary>Enter/Space. 포커스 카드를 고르고, 이미 고른 카드면 탐사 시작.</summary>
        public MinerSelectResult Submit()
        {
            if (IsLocked) return MinerSelectResult.Ignored;
            if (focusIndex >= 0 && focusIndex != selectedIndex) return Select(focusIndex);
            return TryConfirm();
        }

        public MinerSelectResult TryConfirm()
        {
            if (IsLocked) return MinerSelectResult.Ignored;
            if (!HasSelection) return MinerSelectResult.Rejected;
            confirmed = true;
            return MinerSelectResult.Confirmed;
        }

        public MinerSelectResult Back()
        {
            if (IsLocked) return MinerSelectResult.Ignored;
            closed = true;
            return MinerSelectResult.Back;
        }

        /// <summary>새 게임 시작이 실패하면 화면을 다시 조작할 수 있게 확정만 푼다(고른 광부는 유지).</summary>
        public void Unlock()
        {
            confirmed = false;
            closed = false;
        }

        private static bool IsValid(int index) => index >= 0 && index < CardCount;

        private static int Clamp(int index) => index < 0 ? 0 : (index >= CardCount ? CardCount - 1 : index);
    }

    /// <summary>
    /// 슬롯 하나에 대한 선택 화면 세션. 새 게임 진입점(StartNewGame)은 확정한 뒤 TryStart에서만, 한 번만 부른다.
    /// 돌아가기로 끝난 세션은 진입점을 부르지 않으므로 슬롯·저장이 그대로다.
    /// </summary>
    public sealed class MinerSelectSession
    {
        /// <summary>(slot, confirmOverwrite, gender) → 시작 성공 여부. 런타임에서는 SaveRuntimeController.StartNewGame.</summary>
        public delegate bool StartNewGameEntry(int slotId, bool confirmOverwrite, PlayerGender gender);

        private readonly StartNewGameEntry start;
        private bool started;

        public MinerSelectSession(int slotId, StartNewGameEntry startNewGame)
        {
            SlotId = slotId;
            start = startNewGame;
            Flow = new MinerSelectFlow();
        }

        public int SlotId { get; }
        public MinerSelectFlow Flow { get; }
        public bool HasStarted => started;

        public bool TryStart()
        {
            if (started || start == null || !Flow.IsConfirmed) return false;
            started = true;
            // 덮어쓰기는 이 시점에만 일어난다(선택 화면 앞의 확인 창은 허락만 받는다).
            if (start(SlotId, true, Flow.Selected)) return true;
            started = false;
            return false;
        }
    }
}
