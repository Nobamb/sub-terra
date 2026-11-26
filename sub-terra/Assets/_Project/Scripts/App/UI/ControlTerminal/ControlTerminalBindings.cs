using System.Collections.Generic;
using static SubTerra.Shared.Localization.LocalizationService;
using SubTerra.Shared;

namespace SubTerra.App.UI.ControlTerminal
{
    /// <summary>키 하나가 현재 조작 방식에서 맡는 기능. 표시 전용이며 실제 입력 바인딩은 Gameplay가 가진다.</summary>
    public enum ControlKeyFunction
    {
        None = 0,
        MoveLeft,
        MoveRight,
        MoveUp,
        MoveDown,
        Jump,
        MineLeft,
        MineRight,
        MineUp,
        MineDown,
        MineFacing,
        MineMouse
    }

    public enum ControlKeyRole
    {
        None = 0,
        Move,
        Jump,
        Mine
    }

    /// <summary>
    /// 조작 방식 → 키 기능 매핑과 안내 문구. 상태가 없는 순수 계산이라 EditMode에서 검증한다.
    /// 규칙은 PlayerKeyboardControls와 같다: WASD/방향키 중 이동 쪽이 아닌 세트는 상하좌우 채굴이 된다.
    /// </summary>
    public static class ControlTerminalBindings
    {
        public const string SpaceKey = "Space";
        public const string EnterKey = "Enter";
        public const string MouseKey = "마우스 클릭";
        public const int SchemeCount = 3;

        public static string Title(ControlScheme scheme) =>
            string.Format(Text("키 조작 방식 {0} / {1}", "Control Scheme {0} / {1}"), (int)scheme + 1, SchemeCount);

        public const string Caption = "키보드 · 이동 / 채굴";
        public const string HoverHint = "키에 마우스를 올리면 동작을 미리 볼 수 있습니다.";

        public static string Description(ControlScheme scheme)
        {
            if (scheme == ControlScheme.Classic)
            {
                return Text("WASD / 방향키: 이동 (위·아래는 사다리)\n마우스 클릭: 선택 블록 채굴 · Enter: 바라보는 방향 채굴",
                    "WASD / arrows: move (up and down on ladders)\nClick to mine a block · Enter to mine ahead");
            }

            return (scheme == ControlScheme.WasdMove
                    ? Text("WASD: 이동 · 방향키: 상하좌우 채굴", "WASD: move · Arrows: mine in four directions")
                    : Text("방향키: 이동 · WASD: 상하좌우 채굴", "Arrows: move · WASD: mine in four directions"))
                + "\n" + Text("캐릭터 위치 기준 인접 블록 채굴 · 마우스 / Enter도 사용 가능",
                    "Mine adjacent blocks. You can also click or press Enter.");
        }

        public static bool IsWasd(string key) => key == "W" || key == "A" || key == "S" || key == "D";
        public static bool IsArrow(string key) => key == "↑" || key == "←" || key == "↓" || key == "→";

        /// <summary>W/↑ 위, S/↓ 아래, A/← 왼쪽, D/→ 오른쪽. 방향 키가 아니면 (0,0).</summary>
        public static (int x, int y) DirectionOf(string key)
        {
            switch (key)
            {
                case "W":
                case "↑": return (0, 1);
                case "S":
                case "↓": return (0, -1);
                case "A":
                case "←": return (-1, 0);
                case "D":
                case "→": return (1, 0);
                default: return (0, 0);
            }
        }

        public static ControlKeyFunction FunctionOf(ControlScheme scheme, string key)
        {
            if (key == SpaceKey) return ControlKeyFunction.Jump;
            if (key == EnterKey) return ControlKeyFunction.MineFacing;
            if (key == MouseKey) return ControlKeyFunction.MineMouse;

            bool wasd = IsWasd(key);
            bool arrows = IsArrow(key);
            if (!wasd && !arrows) return ControlKeyFunction.None;

            bool move = wasd && scheme != ControlScheme.ArrowsMove || arrows && scheme != ControlScheme.WasdMove;
            var direction = DirectionOf(key);
            if (direction.x < 0) return move ? ControlKeyFunction.MoveLeft : ControlKeyFunction.MineLeft;
            if (direction.x > 0) return move ? ControlKeyFunction.MoveRight : ControlKeyFunction.MineRight;
            if (direction.y > 0) return move ? ControlKeyFunction.MoveUp : ControlKeyFunction.MineUp;
            return move ? ControlKeyFunction.MoveDown : ControlKeyFunction.MineDown;
        }

        public static ControlKeyRole RoleOf(ControlKeyFunction function)
        {
            switch (function)
            {
                case ControlKeyFunction.MoveLeft:
                case ControlKeyFunction.MoveRight:
                case ControlKeyFunction.MoveUp:
                case ControlKeyFunction.MoveDown:
                    return ControlKeyRole.Move;
                case ControlKeyFunction.Jump:
                    return ControlKeyRole.Jump;
                case ControlKeyFunction.None:
                    return ControlKeyRole.None;
                default:
                    return ControlKeyRole.Mine;
            }
        }

        /// <summary>키캡 아래 줄에 쓰는 짧은 역할. 방향 키는 "← 이동"처럼 방향을 함께 적는다.</summary>
        public static string RoleLabel(ControlScheme scheme, string key)
        {
            var function = FunctionOf(scheme, key);
            switch (RoleOf(function))
            {
                case ControlKeyRole.Move:
                case ControlKeyRole.Mine:
                    var role = RoleOf(function) == ControlKeyRole.Move ? Text("이동", "Move") : Text("채굴", "Mine");
                    if (IsWasd(key) || IsArrow(key)) return DirectionGlyph(key) + " " + role;
                    return role;
                case ControlKeyRole.Jump:
                    return Text("점프", "Jump");
                default:
                    return string.Empty;
            }
        }

        public static string DirectionGlyph(string key)
        {
            var direction = DirectionOf(key);
            return direction.y > 0 ? "↑" : direction.y < 0 ? "↓" : direction.x < 0 ? "←" : "→";
        }

        /// <summary>시연 화면 아래에 한 줄로 쓰는 기능명.</summary>
        public static string FunctionName(ControlKeyFunction function)
        {
            switch (function)
            {
                case ControlKeyFunction.MoveLeft: return Text("왼쪽 이동", "Move left");
                case ControlKeyFunction.MoveRight: return Text("오른쪽 이동", "Move right");
                case ControlKeyFunction.MoveUp: return Text("사다리 위로", "Climb up");
                case ControlKeyFunction.MoveDown: return Text("사다리 아래로", "Climb down");
                case ControlKeyFunction.Jump: return Text("점프", "Jump");
                case ControlKeyFunction.MineLeft: return Text("왼쪽 블록 채굴", "Mine left");
                case ControlKeyFunction.MineRight: return Text("오른쪽 블록 채굴", "Mine right");
                case ControlKeyFunction.MineUp: return Text("위쪽 블록 채굴", "Mine above");
                case ControlKeyFunction.MineDown: return Text("아래쪽 블록 채굴", "Mine below");
                case ControlKeyFunction.MineFacing: return Text("바라보는 방향 채굴", "Mine ahead");
                case ControlKeyFunction.MineMouse: return Text("선택한 블록 채굴", "Mine selected block");
                default: return string.Empty;
            }
        }

        /// <summary>한 페이지에서 시연할 기능 키를 기능별로 하나씩, 배치 순서대로 모은다(자동 시연용).</summary>
        public static List<string> DemoKeys(ControlScheme scheme)
        {
            var result = new List<string>();
            var seen = new HashSet<ControlKeyFunction>();
            var keys = ControlTerminalLayout.Keys;
            for (int i = 0; i < keys.Count; i++)
            {
                var function = FunctionOf(scheme, keys[i].Id);
                if (function != ControlKeyFunction.None && seen.Add(function)) result.Add(keys[i].Id);
            }

            return result;
        }
    }
}
