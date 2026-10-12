using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.ControlTerminal
{
    /// <summary>키 하나의 배치. 좌표는 카드 중앙 기준(오른쪽 +x, 위쪽 +y).</summary>
    public readonly struct ControlKeyDef
    {
        public string Id { get; }
        public Vector2 Center { get; }
        public Vector2 Size { get; }
        public int Row { get; }
        public bool IsMouse { get; }

        public ControlKeyDef(string id, float x, float y, float width, float height, int row, bool isMouse = false)
        {
            Id = id;
            Center = new Vector2(x, y);
            Size = new Vector2(width, height);
            Row = row;
            IsMouse = isMouse;
        }

        public float Top => Center.y + Size.y * 0.5f;
        public float Right => Center.x + Size.x * 0.5f;
    }

    /// <summary>
    /// 조작 단말의 고정 배치. 세 조작 방식이 같은 키 배열을 쓰고 역할·색·설명만 바뀐다.
    /// 행 사이 8디자인픽셀 틈이 연결선이 지나는 통로라 다른 키의 글자를 가리지 않는다.
    /// </summary>
    public static class ControlTerminalLayout
    {
        public const float CardWidth = 1280f;
        public const float CardHeight = 700f;
        public const float KeyWidth = 50f;
        public const float KeyHeight = 50f;
        public const float StepX = 56f;
        public const float StepY = 58f;
        /// <summary>정착 상태 키캡 옆면 깊이(디자인 px).</summary>
        public const float SideDepth = 4f;
        /// <summary>호버로 눌릴 때 윗면이 내려가는 거리.</summary>
        public const float PressTravel = 2.5f;

        public const float PlateLeft = -515f;
        public const float PlateRight = 187f;
        public const float PlateTop = 206f;
        public const float PlateBottom = -156f;

        public static readonly Vector2 ScreenCenter = new Vector2(385f, 100f);
        public static readonly Vector2 ScreenSize = new Vector2(320f, 180f);
        /// <summary>연결선이 키보드 오른쪽 가장자리를 벗어나 세로로 오르내리는 x.</summary>
        public const float ConnectorExitX = 205f;
        public const float PrevButtonX = -578f;
        public const float NextButtonX = 578f;

        private static List<ControlKeyDef> keys;

        public static IReadOnlyList<ControlKeyDef> Keys
        {
            get
            {
                if (keys == null) keys = Build();
                return keys;
            }
        }

        public static bool TryGet(string id, out ControlKeyDef def)
        {
            var all = Keys;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Id == id)
                {
                    def = all[i];
                    return true;
                }
            }

            def = default;
            return false;
        }

        public static Vector2 PlateCenter => new Vector2((PlateLeft + PlateRight) * 0.5f, (PlateTop + PlateBottom) * 0.5f);
        public static Vector2 PlateSize => new Vector2(PlateRight - PlateLeft, PlateTop - PlateBottom);
        public static Vector2 ScreenLeftMiddle => new Vector2(ScreenCenter.x - ScreenSize.x * 0.5f, ScreenCenter.y);

        private static List<ControlKeyDef> Build()
        {
            var list = new List<ControlKeyDef>();
            AddRow(list, new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" }, -470f, 165f, 0);
            AddRow(list, new[] { "A", "S", "D", "F", "G", "H", "J", "K", "L" }, -442f, 107f, 1);
            AddRow(list, new[] { "Z", "X", "C", "V", "B", "N", "M" }, -414f, 49f, 2);
            list.Add(new ControlKeyDef("↑", 84f, 49f, KeyWidth, KeyHeight, 2));
            list.Add(new ControlKeyDef("←", 28f, -9f, KeyWidth, KeyHeight, 3));
            list.Add(new ControlKeyDef("↓", 84f, -9f, KeyWidth, KeyHeight, 3));
            list.Add(new ControlKeyDef("→", 140f, -9f, KeyWidth, KeyHeight, 3));
            list.Add(new ControlKeyDef(ControlTerminalBindings.SpaceKey, -310f, -9f, 240f, KeyHeight, 3));
            list.Add(new ControlKeyDef(ControlTerminalBindings.EnterKey, -125f, -9f, 110f, KeyHeight, 3));
            list.Add(new ControlKeyDef(ControlTerminalBindings.MouseKey, -200f, -110f, 46f, 60f, 4, true));
            return list;
        }

        private static void AddRow(List<ControlKeyDef> list, string[] ids, float startX, float y, int row)
        {
            for (int i = 0; i < ids.Length; i++)
                list.Add(new ControlKeyDef(ids[i], startX + i * StepX, y, KeyWidth, KeyHeight, row));
        }
    }
}
