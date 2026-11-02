using System.Collections.Generic;
using SubTerra.App.UI.Guide;
using UnityEngine;

namespace SubTerra.App.UI.ControlTerminal
{
    /// <summary>
    /// 조작 단말 시연 장면. 가이드 창의 DemoClip(키프레임)과 GameGuideSkin의 실제 게임 스프라이트
    /// (플레이어 프레임·광산 타일·사다리)로 만든 표시 전용 반복 장면이며 영상 파일·실제 게임 호출이 없다.
    /// 좌표는 무대 중앙 기준, 위쪽이 +y이고 화면은 320x180이다. 같은 기능의 여러 키는 한 장면을 공유한다.
    /// </summary>
    public static class ControlTerminalDemoClips
    {
        private const float Unit = 38f;
        private const float GroundTop = -24f;
        private const float PlayerHeight = 44f;
        private const float PlayerY = GroundTop + PlayerHeight * 0.5f - 1f;

        private static readonly Color Navy = new Color(0.03f, 0.055f, 0.08f, 1f);
        private static readonly Color Teal = new Color(0.42f, 0.94f, 1f, 1f);
        private static readonly Color Amber = new Color(1f, 0.78f, 0.28f, 1f);

        private static readonly Dictionary<ControlKeyFunction, DemoClip> Cache = new Dictionary<ControlKeyFunction, DemoClip>();

        public static DemoClip Get(ControlKeyFunction function)
        {
            if (function == ControlKeyFunction.None) return null;
            if (!Cache.TryGetValue(function, out var clip))
            {
                clip = Create(function);
                Cache[function] = clip;
            }

            return clip;
        }

        private static DemoClip Create(ControlKeyFunction function)
        {
            switch (function)
            {
                case ControlKeyFunction.MoveLeft: return Walk(function, -1);
                case ControlKeyFunction.MoveRight: return Walk(function, 1);
                case ControlKeyFunction.MoveUp: return Climb(function, true);
                case ControlKeyFunction.MoveDown: return Climb(function, false);
                case ControlKeyFunction.Jump: return Jump(function);
                case ControlKeyFunction.MineLeft: return Mine(function, -1, 0, false);
                case ControlKeyFunction.MineRight: return Mine(function, 1, 0, false);
                case ControlKeyFunction.MineUp: return Mine(function, 0, 1, false);
                case ControlKeyFunction.MineDown: return Mine(function, 0, -1, false);
                case ControlKeyFunction.MineFacing: return Mine(function, 1, 0, false);
                default: return Mine(function, 1, 0, true);
            }
        }

        // ------------------------------------------------------------------ 만들기 도우미

        private static DemoClip New(ControlKeyFunction function, float duration, float thumb)
        {
            return new DemoClip { Id = "terminal." + function, Duration = duration, ThumbTime = thumb };
        }

        private static DemoTrack S(params float[] pairs) => DemoTrack.Smooth(pairs);
        private static DemoTrack H(params float[] pairs) => DemoTrack.Hold(pairs);
        private static DemoTrack K(float value) => DemoTrack.Const(value);

        private static DemoLayer Spr(DemoClip clip, string key, float height, float x, float y)
        {
            var layer = new DemoLayer { Kind = DemoLayerKind.Sprite, SpriteKey = key, Size = new Vector2(0f, height) }.At(x, y);
            clip.Layers.Add(layer);
            return layer;
        }

        private static DemoLayer Box(DemoClip clip, float width, float height, float x, float y, Color color)
        {
            var layer = new DemoLayer { Kind = DemoLayerKind.Rect, Size = new Vector2(width, height), Tint = color }.At(x, y);
            clip.Layers.Add(layer);
            return layer;
        }

        private static DemoLayer Tile(DemoClip clip, string key, float x, float y)
        {
            var layer = new DemoLayer { Kind = DemoLayerKind.Sprite, SpriteKey = key, Size = new Vector2(0f, Unit), Pixel = true }.At(x, y);
            clip.Layers.Add(layer);
            return layer;
        }

        private static void Backdrop(DemoClip clip)
        {
            Box(clip, 340f, 200f, 0f, 0f, Navy);
            Spr(clip, "art.soft", 170f, -80f, 20f).Color_(new Color(0.12f, 0.3f, 0.36f, 0.35f)).Sx(K(2.2f));
        }

        /// <summary>바닥 두 줄. skipX에 해당하는 칸은 비워 둔다(아래 채굴 장면).</summary>
        private static void Floor(DemoClip clip, float skipX = float.NaN)
        {
            for (var x = -171f; x <= 171.1f; x += Unit)
            {
                if (!float.IsNaN(skipX) && Mathf.Abs(x - skipX) < 1f) continue;
                Tile(clip, "tile.ground", x, GroundTop - Unit * 0.5f);
                Tile(clip, "tile.hard", x, GroundTop - Unit * 1.5f);
            }
        }

        /// <summary>반복 이음매에서 튀지 않도록 장면 전체 층에 쓰는 등장·퇴장 알파.</summary>
        private static DemoTrack LoopFade(float duration, float fadeIn, float fadeOutStart)
        {
            return S(0f, 0f, fadeIn, 1f, fadeOutStart, 1f, duration - 0.05f, 0f, duration, 0f);
        }

        private static DemoLayer Arrow(DemoClip clip, string key, float x, float y, Color color, float rotation, DemoTrack fade)
        {
            return Spr(clip, key, 16f, x, y).Color_(color).Rot(K(rotation)).Fade(fade);
        }

        // ------------------------------------------------------------------ 이동·사다리·점프

        private static DemoClip Walk(ControlKeyFunction function, int dir)
        {
            const float duration = 2.2f;
            var clip = New(function, duration, 0.8f);
            Backdrop(clip);
            Floor(clip);
            var x = S(0f, -70f * dir, 1.5f, 70f * dir, duration, 70f * dir);
            var fade = LoopFade(duration, 0.15f, 1.7f);
            Spr(clip, "player.idle", PlayerHeight, 0f, PlayerY).PosX(x).Sx(K(dir)).Fade(fade)
                .Animate("player.walk", GuideDemoLibrary.PlayerWalkFrameCount, GuideDemoLibrary.PlayerWalkFrameRate, "player.idle", H(0f, 1f, 1.5f, 0f));
            Arrow(clip, "art.arrow", 0f, 28f, Teal, dir < 0 ? 180f : 0f, fade).PosX(x);
            return clip;
        }

        private static DemoClip Climb(ControlKeyFunction function, bool up)
        {
            const float duration = 2.4f;
            var clip = New(function, duration, 0.9f);
            Backdrop(clip);
            for (var xx = -171f; xx <= 39f; xx += Unit)
            {
                Tile(clip, "tile.ground", xx, GroundTop - Unit * 0.5f);
                Tile(clip, "tile.hard", xx, GroundTop - Unit * 1.5f);
            }

            for (var i = 0; i < 3; i++) Spr(clip, "fac.ladder", Unit, 56f, GroundTop + Unit * 0.5f + i * Unit);
            Tile(clip, "tile.ground", 94f, GroundTop - Unit * 0.5f);
            var low = -4f;
            var high = 40f;
            var from = up ? low : high;
            var to = up ? high : low;
            var fade = LoopFade(duration, 0.15f, 1.75f);
            var ladder = Spr(clip, "player.ladder.1", PlayerHeight, 56f, from).Fade(fade)
                .Animate("player.ladder", 8, 8f, "player.ladder.1", H(0f, 0f, 0.2f, 1f, 1.5f, 0f))
                .PosY(S(0f, from, 0.2f, from, 1.5f, to, duration, to));
            ladder.FrameKeys = new[] { "player.ladder.1", "player.ladder.2", "player.ladder.3", "player.ladder.2", "player.ladder.1", "player.ladder.4", "player.ladder.5", "player.ladder.4" };
            Arrow(clip, "art.arrowup", 92f, 24f, Teal, up ? 0f : 180f, fade);
            return clip;
        }

        private static DemoClip Jump(ControlKeyFunction function)
        {
            const float duration = 2.0f;
            var clip = New(function, duration, 0.55f);
            Backdrop(clip);
            Floor(clip);
            var fade = LoopFade(duration, 0.12f, 1.55f);
            // 땅에 있을 때는 서 있는 모습, 떠 있는 동안은 점프 자세를 겹쳐 바꾼다.
            Spr(clip, "player.idle", PlayerHeight, 0f, PlayerY).Fade(S(0f, 0f, 0.12f, 1f, 0.3f, 1f, 0.32f, 0f, 1.0f, 0f, 1.02f, 1f, 1.55f, 1f, 1.95f, 0f, duration, 0f));
            Spr(clip, "player.jump.4", PlayerHeight, 0f, PlayerY)
                .PosY(S(0f, PlayerY, 0.3f, PlayerY, 0.62f, PlayerY + 42f, 1.02f, PlayerY, duration, PlayerY))
                .Fade(H(0f, 0f, 0.32f, 1f, 1.02f, 0f));
            Spr(clip, "art.soft", 14f, 0f, GroundTop + 2f).Color_(new Color(0f, 0f, 0f, 0.5f)).Sx(S(0f, 2.4f, 0.3f, 2.4f, 0.62f, 1.4f, 1.02f, 2.4f, duration, 2.4f)).Fade(fade);
            Arrow(clip, "art.arrowup", 40f, 20f, Teal, 0f, S(0f, 0f, 0.3f, 0f, 0.4f, 1f, 0.95f, 1f, 1.05f, 0f, duration, 0f));
            return clip;
        }

        // ------------------------------------------------------------------ 채굴

        /// <summary>
        /// 캐릭터 옆(위·아래) 블록 한 칸을 채굴한다. highlight이면 마우스로 고른 블록이다.
        /// 위·아래 채굴은 옆모습 프레임을 그대로 쓰고 테두리로 대상 칸을 알려 준다(전용 프레임 없음).
        /// </summary>
        private static DemoClip Mine(ControlKeyFunction function, int dx, int dy, bool highlight)
        {
            const float duration = 3.0f;
            var clip = New(function, duration, 1.5f);
            Backdrop(clip);
            var playerX = -19f; // 바닥 격자(-171 + 38n) 위
            var blockX = playerX + dx * Unit;
            var blockY = GroundTop + Unit * 0.5f + (dy > 0 ? Unit : 0f);
            if (dy < 0)
            {
                blockY = GroundTop - Unit * 0.5f;
                Floor(clip, blockX);
            }
            else
            {
                Floor(clip);
            }

            // 위 칸은 벽 조각으로 감싸 허공에 떠 보이지 않게 한다.
            if (dy > 0)
            {
                Tile(clip, "tile.ground", blockX - Unit, blockY);
                Tile(clip, "tile.ground", blockX + Unit, blockY);
            }
            else if (dx != 0)
            {
                Tile(clip, "tile.ground", blockX + dx * Unit, blockY);
                Tile(clip, "tile.ground", blockX, blockY + Unit);
                Tile(clip, "tile.ground", blockX + dx * Unit, blockY + Unit);
            }

            var mineStart = highlight ? 0.7f : 0.35f;
            var miningFrameOffset = Mathf.FloorToInt(mineStart * 12f);
            var mineEnd = (miningFrameOffset + 13) / 12f;
            var gone = S(0f, 1f, mineEnd - 0.3f, 1f, mineEnd, 0f, duration - 0.35f, 0f, duration - 0.15f, 1f, duration, 1f);
            Tile(clip, "tile.ore.copper", blockX, blockY).Fade(gone)
                .Scale(S(0f, 1f, mineEnd - 0.3f, 1f, mineEnd, 0.2f, duration - 0.35f, 0.2f, duration - 0.15f, 1f, duration, 1f));
            Spr(clip, "player.idle", PlayerHeight, playerX, PlayerY).Sx(K(dx < 0 ? -1f : 1f))
                .AnimateMining(mineStart, mineEnd);

            // 채굴 대상 블록을 금빛 테두리로 먼저 강조한 뒤 채굴한다.
            var outline = Spr(clip, "art.line", 0f, blockX, blockY).Color_(Amber);
            outline.Size = new Vector2(Unit + 8f, Unit + 8f);
            outline.SizeIsRect = true;
            outline.Fade(S(0f, 0f, 0.15f, 1f, 0.45f, 0.55f, 0.7f, 1f, mineEnd, 1f, mineEnd + 0.15f, 0f, duration, 0f));

            if (highlight)
            {
                // 마우스: 블록 옆에 작은 마우스가 나타나 왼쪽 버튼을 한 번 누른다.
                Spr(clip, "art.mouse", 30f, blockX + 30f, blockY + 30f)
                    .Scale(S(0f, 1f, 0.5f, 1f, 0.62f, 0.82f, 0.78f, 1f, duration, 1f))
                    .Fade(S(0f, 0f, 0.15f, 1f, mineEnd, 1f, mineEnd + 0.15f, 0f, duration, 0f));
            }

            for (var i = 0; i < 5; i++)
            {
                var ox = -14f + i * 7f;
                var oy = 16f + (i % 2) * 12f;
                Box(clip, 5f, 5f, blockX, blockY, new Color(0.78f, 0.42f, 0.2f, 1f))
                    .PosX(S(0f, blockX, mineEnd - 0.05f, blockX, mineEnd + 0.55f, blockX + ox))
                    .PosY(S(0f, blockY, mineEnd - 0.05f, blockY, mineEnd + 0.3f, blockY + oy, mineEnd + 0.7f, blockY - 26f))
                    .Fade(S(0f, 0f, mineEnd - 0.1f, 0f, mineEnd, 1f, mineEnd + 0.6f, 0f));
            }

            return clip;
        }
    }
}
