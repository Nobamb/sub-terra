using System.Collections.Generic;
using SubTerra.App.Core.Data;
using UnityEngine;

namespace SubTerra.App.UI.Guide
{
    /// <summary>
    /// 가이드 시연 장면 모음. 실제 게임 스프라이트(플레이어 프레임·타일·시설·HUD 아이콘)를 키프레임으로 움직이는
    /// 무음 반복 장면이며 영상 패키지가 필요 없다. 목록 카드는 ThumbTime의 정지 장면만 쓴다.
    /// 좌표는 무대 중앙 기준, 위쪽이 +y, 무대 크기는 320x120이다.
    /// </summary>
    public static class GuideDemoLibrary
    {
        public const float StageWidth = 320f;
        public const float StageHeight = 120f;

        private const float Unit = 34f;
        private const float GroundTop = -26f;
        private const float PlayerHeight = 38f;
        private const float PlayerY = GroundTop + PlayerHeight * 0.5f - 1f;

        private static readonly Color Teal = new Color(0.42f, 0.94f, 1f, 1f);
        private static readonly Color Navy = new Color(0.03f, 0.055f, 0.08f, 1f);
        private static readonly Color Panel = new Color(0.04f, 0.09f, 0.13f, 0.96f);
        private static readonly Color Warn = new Color(1f, 0.46f, 0.38f, 1f);
        private static readonly Color Amber = new Color(1f, 0.78f, 0.28f, 1f);
        private static readonly Color Green = new Color(0.55f, 1f, 0.45f, 1f);

        private static Dictionary<string, DemoClip> clips;

        public static IEnumerable<DemoClip> All
        {
            get
            {
                Ensure();
                return clips.Values;
            }
        }

        public static bool TryGet(string id, out DemoClip clip)
        {
            Ensure();
            clip = null;
            return !string.IsNullOrEmpty(id) && clips.TryGetValue(id, out clip);
        }

        private static void Ensure()
        {
            if (clips != null)
            {
                return;
            }

            clips = new Dictionary<string, DemoClip>();
            Register(Move()); Register(Ladder()); Register(Elevator()); Register(Interact()); Register(Mine());
            Register(Build()); Register(Inventory()); Register(Drone()); Register(Minimap()); Register(Clock());
            Register(Guide());
            Register(Base()); Register(Health()); Register(Power()); Register(Cargo()); Register(Grid());
            Register(Core()); Register(Structure()); Register(Gas()); Register(Reset());
            Register(ResourceScene("res.copper", "tile.ore.copper", "item:" + DataIds.Minerals.Copper, string.Empty));
            Register(ResourceScene("res.iron", "tile.ore.iron", "item:" + DataIds.Minerals.Iron, string.Empty));
            Register(ResourceScene("res.lithium", "tile.ore.lithium", "item:" + DataIds.Minerals.Lithium, string.Empty));
            Register(ResourceScene("res.fuel", "tile.hard", "item:" + DataIds.RareItems.EngineFuel, "희귀 신호"));
            Register(ResourceScene("res.gold", "tile.gold", "icon.gold", string.Empty));
            Register(FacilityScene("fac.storage", "fac.storage", 56f, true, Teal));
            Register(FacilityScene("fac.charger", "fac.charger", 60f, true, Teal));
            Register(FacilityScene("fac.clinic", "fac.clinic", 58f, true, Green));
            Register(FacilityScene("fac.settlement", "fac.settlement", 62f, true, Amber));
            Register(FacilityScene("fac.core", "fac.core", 72f, true, Teal));
            Register(FacilityScene("fac.light", "fac.light", 68f, false, Amber));
            Register(FacilityScene("fac.portal", "art.portal", 54f, true, Teal));
            Register(FacilityScene("fac.elevator", "fac.elevator", 88f, true, Teal));
            Register(LadderFacility());
            Register(SupportFacility());
            Register(FirstEnter()); Register(FirstStatus()); Register(FirstSell());

            // 첫 탐사 안내의 채굴·귀환 단계는 기본 조작 시연을 그대로 쓴다.
            clips["first.mine"] = clips["mine"];
            clips["first.return"] = clips["elevator"];
        }

        private static void Register(DemoClip clip)
        {
            clips[clip.Id] = clip;
        }

        // ------------------------------------------------------------------ 만들기 도우미

        private static DemoClip New(string id, float duration, float thumb)
        {
            return new DemoClip { Id = id, Duration = duration, ThumbTime = thumb };
        }

        private static DemoTrack S(params float[] pairs) => DemoTrack.Smooth(pairs);
        private static DemoTrack H(params float[] pairs) => DemoTrack.Hold(pairs);
        private static DemoTrack K(float v) => DemoTrack.Const(v);

        private static DemoLayer Spr(DemoClip c, string key, float height, float x, float y)
        {
            var layer = new DemoLayer { Kind = DemoLayerKind.Sprite, SpriteKey = key, Size = new Vector2(0f, height) }.At(x, y);
            c.Layers.Add(layer);
            return layer;
        }

        private static DemoLayer Box(DemoClip c, float w, float h, float x, float y, Color color)
        {
            var layer = new DemoLayer { Kind = DemoLayerKind.Rect, Size = new Vector2(w, h), Tint = color }.At(x, y);
            c.Layers.Add(layer);
            return layer;
        }

        private static DemoLayer Txt(DemoClip c, string text, float size, float x, float y, float w, Color color, bool bold = false)
        {
            var layer = new DemoLayer
            {
                Kind = DemoLayerKind.Text,
                Content = text,
                FontSize = size,
                Size = new Vector2(w, size * 1.5f),
                Tint = color,
                Bold = bold
            }.At(x, y);
            c.Layers.Add(layer);
            return layer;
        }

        /// <summary>어두운 광산 배경.</summary>
        private static void Backdrop(DemoClip c)
        {
            Box(c, StageWidth + 4f, StageHeight + 4f, 0f, 0f, Navy);
            Spr(c, "art.soft", 150f, -90f, 10f).Color_(new Color(0.12f, 0.3f, 0.36f, 0.35f)).Sx(K(2.2f));
        }

        private static void Floor(DemoClip c, float fromX = -153f, float toX = 153f)
        {
            for (var x = fromX; x <= toX + 0.1f; x += Unit)
            {
                Tile(c, "tile.ground", x, GroundTop - Unit * 0.5f);
                Tile(c, "tile.hard", x, GroundTop - Unit * 1.5f);
            }
        }

        private static DemoLayer Tile(DemoClip c, string key, float x, float y, float size = Unit)
        {
            var layer = new DemoLayer { Kind = DemoLayerKind.Sprite, SpriteKey = key, Size = new Vector2(0f, size), Pixel = true }.At(x, y);
            c.Layers.Add(layer);
            return layer;
        }

        private static DemoLayer Player(DemoClip c, float x)
        {
            return Spr(c, "player.idle", PlayerHeight, x, PlayerY);
        }

        private static DemoLayer Walker(DemoClip c, DemoTrack x, DemoTrack active)
        {
            return Spr(c, "player.idle", PlayerHeight, 0f, PlayerY).PosX(x).Animate("player.walk", 10, 14f, "player.idle", active);
        }

        private static DemoKeyCap Key(DemoClip c, string label, float x, float y, DemoTrack press)
        {
            var key = new DemoKeyCap { Label = label, X = x, Y = y, Press = press };
            c.Keys.Add(key);
            return key;
        }

        private static DemoKeyCap MouseKey(DemoClip c, float x, float y, DemoTrack press)
        {
            var key = new DemoKeyCap { Mouse = true, X = x, Y = y, Press = press };
            c.Keys.Add(key);
            return key;
        }

        /// <summary>테두리 있는 패널(면 + 윤곽). 같은 알파 트랙을 모든 층에 쓴다.</summary>
        private static void PanelBox(DemoClip c, float x, float y, float w, float h, DemoTrack alpha, Color line)
        {
            var fill = Spr(c, "art.fill", 0f, x, y);
            fill.Size = new Vector2(w, h);
            fill.Tint = Panel;
            fill.Fade(alpha);
            fill.SizeIsRect = true;
            var outline = Spr(c, "art.line", 0f, x, y);
            outline.Size = new Vector2(w, h);
            outline.Tint = line;
            outline.Fade(alpha);
            outline.SizeIsRect = true;
        }

        // ------------------------------------------------------------------ 기본 조작

        private static DemoClip Move()
        {
            var c = New("move", 3.4f, 0.9f);
            Backdrop(c);
            Floor(c);
            var x = S(0f, -70f, 1.5f, 62f, 1.75f, 62f, 3.15f, -70f, 3.4f, -70f);
            var active = H(0f, 0f, 0.1f, 1f, 1.5f, 0f, 1.75f, 1f, 3.15f, 0f);
            Walker(c, x, active).Sx(H(0f, 1f, 1.7f, -1f, 3.3f, 1f));
            Spr(c, "art.arrow", 16f, 0f, 26f).Color_(Teal).PosX(x)
                .Rot(H(0f, 0f, 1.7f, 180f, 3.3f, 0f))
                .Fade(S(0f, 0f, 0.15f, 1f, 1.45f, 1f, 1.55f, 0f, 1.8f, 0f, 1.9f, 1f, 3.05f, 1f, 3.15f, 0f));
            Key(c, "D", -103f, 42f, DemoTrack.Pulse(0.1f, 1.5f));
            Key(c, "A", -135f, 42f, DemoTrack.Pulse(1.75f, 3.15f));
            return c;
        }

        private static DemoClip Ladder()
        {
            var c = New("ladder", 3.6f, 1.0f);
            Backdrop(c);
            for (var xx = -153f; xx <= 17f; xx += Unit)
            {
                Tile(c, "tile.ground", xx, GroundTop - Unit * 0.5f);
                Tile(c, "tile.hard", xx, GroundTop - Unit * 1.5f);
            }

            for (var i = 0; i < 4; i++)
            {
                Spr(c, "fac.ladder", Unit, 56f, GroundTop + Unit * 0.5f + i * Unit);
            }

            Tile(c, "tile.ground", 90f, -43f);
            Spr(c, "player.ladder.1", PlayerHeight, 56f, 0f)
                .Animate("player.ladder", 5, 8f, "player.ladder.1", H(0f, 0f, 0.25f, 1f, 1.55f, 0f, 1.85f, 1f, 3.15f, 0f))
                .PosY(S(0f, -8f, 0.25f, -8f, 1.55f, 34f, 1.85f, 34f, 3.15f, -8f, 3.6f, -8f));
            Spr(c, "art.arrowup", 16f, 84f, 14f).Color_(Teal)
                .Fade(S(0f, 0f, 0.3f, 1f, 1.45f, 1f, 1.55f, 0f, 3.6f, 0f));
            Spr(c, "art.arrowup", 16f, 84f, 14f).Color_(Teal).Rot(K(180f))
                .Fade(S(0f, 0f, 1.8f, 0f, 1.95f, 1f, 3.05f, 1f, 3.15f, 0f));
            Key(c, "W", -135f, 42f, DemoTrack.Pulse(0.25f, 1.55f));
            Key(c, "S", -103f, 42f, DemoTrack.Pulse(1.85f, 3.15f));
            return c;
        }

        private static DemoClip Elevator()
        {
            var c = New("elevator", 4f, 1.55f);
            Backdrop(c);
            Floor(c);
            var rise = S(0f, 0f, 1.95f, 0f, 3.2f, 74f, 4f, 74f);
            var vanish = S(0f, 0f, 0.2f, 1f, 2.6f, 1f, 3.3f, 0f, 4f, 0f);
            Spr(c, "fac.elevator", 88f, 24f, GroundTop + 40f).Fade(vanish).PosY(S(0f, GroundTop + 40f, 1.95f, GroundTop + 40f, 3.2f, GroundTop + 114f, 4f, GroundTop + 114f));
            Spr(c, "player.idle", PlayerHeight, 0f, PlayerY).Animate("player.walk", 10, 14f, "player.idle", H(0f, 1f, 1.2f, 0f))
                .PosX(S(0f, -120f, 1.2f, 24f, 4f, 24f))
                .PosY(S(0f, PlayerY, 1.95f, PlayerY, 3.2f, PlayerY + 74f, 4f, PlayerY + 74f)).Fade(vanish);
            Box(c, 96f, 16f, 24f, 56f, new Color(0.1f, 0.35f, 0.4f, 0.55f)).Fade(S(0f, 0f, 1.1f, 0f, 1.4f, 1f, 1.9f, 1f, 2.1f, 0f));
            Txt(c, "E키를 눌러 귀환", 10f, 24f, 56f, 96f, Teal, true).Fade(S(0f, 0f, 1.1f, 0f, 1.4f, 1f, 1.9f, 1f, 2.1f, 0f));
            Key(c, "E", -135f, 42f, DemoTrack.Pulse(1.55f, 1.95f));
            return c;
        }

        private static DemoClip Interact()
        {
            var c = New("interact", 3.6f, 2.3f);
            Backdrop(c);
            Floor(c);
            var chargerName = ItemDisplayNames.Building(DataIds.Buildings.ChargerBasic);
            Spr(c, "art.soft", 90f, 52f, 6f).Color_(new Color(0.3f, 0.9f, 1f, 1f))
                .Fade(S(0f, 0f, 1.9f, 0f, 2.3f, 0.55f, 3.0f, 0f));
            Spr(c, "fac.charger", 56f, 52f, GroundTop + 28f);
            Walker(c, S(0f, -100f, 1.15f, 6f, 3.6f, 6f), H(0f, 1f, 1.15f, 0f));
            Box(c, 48f, 14f, 52f, 44f, new Color(0.1f, 0.35f, 0.4f, 0.6f)).Fade(S(0f, 0f, 0.9f, 0f, 1.2f, 1f, 3.3f, 1f, 3.6f, 0f));
            Txt(c, chargerName, 10f, 52f, 44f, 48f, Teal, true).Fade(S(0f, 0f, 0.9f, 0f, 1.2f, 1f, 3.3f, 1f, 3.6f, 0f));
            Spr(c, "hud.bolt", 24f, 52f, 40f).Fade(S(0f, 0f, 1.95f, 0f, 2.15f, 1f, 3.0f, 0f))
                .PosY(S(0f, 30f, 1.95f, 30f, 3.0f, 56f));
            Key(c, "E", -135f, 42f, DemoTrack.Pulse(1.65f, 1.95f));
            return c;
        }

        private static DemoClip Mine()
        {
            var c = New("mine", 3.3f, 1.9f);
            Backdrop(c);
            Floor(c);
            Tile(c, "tile.ground", 56f, GroundTop + Unit * 0.5f);
            Tile(c, "tile.ground", 24f, GroundTop + Unit * 1.5f);
            Tile(c, "tile.ground", 56f, GroundTop + Unit * 1.5f);
            Tile(c, "tile.ore.copper", 24f, GroundTop + Unit * 0.5f)
                .Scale(S(0f, 1f, 1.75f, 1f, 2.05f, 0f, 3.3f, 0f))
                .Fade(H(0f, 1f, 2.05f, 0f));
            Spr(c, "player.idle", PlayerHeight, -10f, PlayerY).Animate("player.mine", 8, 11f, "player.idle", H(0f, 0f, 0.3f, 1f, 2.0f, 0f));
            for (var i = 0; i < 5; i++)
            {
                var dx = -16f + i * 12f;
                var dy = 18f + (i % 2) * 14f;
                Box(c, 5f, 5f, 24f, -9f, new Color(0.78f, 0.42f, 0.2f, 1f))
                    .PosX(S(0f, 24f, 1.9f, 24f, 2.5f, 24f + dx))
                    .PosY(S(0f, -9f, 1.9f, -9f, 2.2f, -9f + dy, 2.6f, -30f))
                    .Fade(S(0f, 0f, 1.85f, 0f, 1.95f, 1f, 2.5f, 0f));
            }

            Spr(c, "icon.cargo", 24f, 128f, 40f).Fade(S(0f, 0.25f, 3.3f, 0.25f));
            Spr(c, "item:" + DataIds.Minerals.Copper, 18f, 24f, -9f)
                .PosX(S(0f, 24f, 2.0f, 24f, 2.8f, 126f)).PosY(S(0f, -9f, 2.0f, -9f, 2.4f, 18f, 2.8f, 38f))
                .Fade(S(0f, 0f, 1.95f, 0f, 2.05f, 1f, 2.7f, 1f, 2.85f, 0f));
            MouseKey(c, -135f, 42f, DemoTrack.Pulse(0.3f, 1.15f));
            Key(c, "Enter", -96f, 42f, DemoTrack.Pulse(1.2f, 2.0f));
            return c;
        }

        private static DemoClip Build()
        {
            var c = New("build", 4f, 2.7f);
            Backdrop(c);
            Floor(c);
            Player(c, -80f);
            Spr(c, "icon.facility", 26f, 128f, 40f).Fade(S(0f, 0f, 0.5f, 0f, 0.8f, 1f, 3.6f, 1f, 3.9f, 0f));
            var ghostH = 52f;
            Box(c, 30f, ghostH + 4f, 10f, GroundTop + ghostH * 0.5f, new Color(0.42f, 0.94f, 1f, 0.14f))
                .Fade(S(0f, 0f, 0.8f, 0f, 1.0f, 1f, 2.2f, 1f, 2.3f, 0f));
            Spr(c, "fac.support", ghostH, 10f, GroundTop + ghostH * 0.5f).Color_(new Color(0.5f, 0.95f, 1f, 0.55f))
                .Fade(S(0f, 0f, 0.8f, 0f, 1.0f, 1f, 2.2f, 1f, 2.3f, 0f));
            Spr(c, "art.soft", 80f, 10f, GroundTop + ghostH * 0.5f).Color_(Teal)
                .Fade(S(0f, 0f, 2.25f, 0f, 2.4f, 0.8f, 3.0f, 0f));
            Spr(c, "fac.support", ghostH, 10f, GroundTop + ghostH * 0.5f)
                .Fade(S(0f, 0f, 2.2f, 0f, 2.35f, 1f, 3.9f, 1f, 4f, 1f));
            Key(c, "B", -135f, 42f, DemoTrack.Pulse(0.25f, 0.55f));
            Key(c, "C", -103f, 42f, DemoTrack.Pulse(2.0f, 2.3f));
            return c;
        }

        private static DemoClip Inventory()
        {
            var c = New("inventory", 3.6f, 1.7f);
            Backdrop(c);
            Floor(c);
            Player(c, -92f);
            var a = S(0f, 0f, 0.6f, 0f, 0.9f, 1f, 3.3f, 1f, 3.6f, 0f);
            PanelBox(c, 78f, 4f, 128f, 94f, a, Teal);
            var ids = new[] { DataIds.Minerals.Copper, DataIds.Minerals.Iron, DataIds.Minerals.Lithium };
            var widths = new[] { 0.7f, 0.45f, 0.25f };
            for (var i = 0; i < 3; i++)
            {
                var y = 26f - i * 20f;
                Spr(c, "item:" + ids[i], 18f, 34f, y).Fade(a);
                Box(c, 70f, 6f, 76f, y, new Color(1f, 1f, 1f, 0.1f)).Fade(a);
                Box(c, 70f * widths[i], 6f, 41f + 35f * widths[i], y, Teal).Fade(a);
            }

            Spr(c, "icon.cargo", 16f, 34f, -33f).Fade(a);
            Box(c, 70f, 6f, 76f, -33f, new Color(1f, 1f, 1f, 0.1f)).Fade(a);
            Box(c, 70f * 0.55f, 6f, 41f + 35f * 0.55f, -33f, Amber).Fade(a);
            Key(c, "I", -135f, 42f, DemoTrack.Pulse(0.3f, 0.6f));
            return c;
        }

        private static DemoClip Drone()
        {
            var c = New("drone", 3.6f, 1.8f);
            Backdrop(c);
            Floor(c);
            Player(c, -78f);
            var bob = S(0f, 20f, 0.9f, 25f, 1.8f, 20f, 2.7f, 25f, 3.6f, 20f);
            Spr(c, "drone.idle", 30f, -28f, 22f).PosY(bob);
            var a = S(0f, 0f, 0.7f, 0f, 1.0f, 1f, 3.3f, 1f, 3.6f, 0f);
            PanelBox(c, 62f, 38f, 118f, 28f, a, Teal);
            Spr(c, "art.warning", 16f, 14f, 38f).Color_(Amber).Fade(a);
            Txt(c, "구조 위험 주의!", 10f, 66f, 38f, 90f, new Color(0.9f, 0.97f, 1f, 1f), true).Fade(a);
            Key(c, "Tab", -131f, 42f, DemoTrack.Pulse(0.3f, 0.65f));
            return c;
        }

        private static DemoClip Minimap()
        {
            var c = New("minimap", 4f, 2.6f);
            Backdrop(c);
            Floor(c);
            Player(c, -92f);
            // M: 닫힘 → 가로형 → 정사각형 → 닫힘. 관측판은 불투명한 남색 전광판이다.
            var a = S(0f, 0f, 0.3f, 0f, 0.52f, 1f, 3.3f, 1f, 3.46f, 0f, 4f, 0f);
            PanelBox(c, 98f, 14f, 100f, 76f, a, Teal);
            var rng = new System.Random(7);
            for (var gx = 0; gx < 7; gx++)
            {
                for (var gy = 0; gy < 5; gy++)
                {
                    if (rng.NextDouble() < 0.28)
                    {
                        continue;
                    }

                    Box(c, 8f, 8f, 66f + gx * 11f, 32f - gy * 11f, new Color(0.2f, 0.33f, 0.37f, 1f)).Fade(a);
                }
            }

            Box(c, 5f, 9f, 88f, 22f, new Color(0.93f, 0.99f, 1f, 1f)).Fade(a);
            Key(c, "M", -97f, 42f, DemoTrack.Pulses(0.3f, 0.55f, 1.9f, 2.15f, 3.3f, 3.55f));
            return c;
        }

        private static DemoClip Clock()
        {
            var c = New("clock", 3.6f, 1.6f);
            Backdrop(c);
            Floor(c);
            Player(c, -70f);
            var a = S(0f, 0f, 0.6f, 0f, 0.9f, 1f, 3.3f, 1f, 3.6f, 0f);
            PanelBox(c, 0f, 34f, 132f, 40f, a, Teal);
            Txt(c, "광산 초기화까지", 8f, 0f, 44f, 120f, new Color(0.6f, 0.8f, 0.86f, 1f)).Fade(a);
            Txt(c, "02:41:09", 17f, 0f, 29f, 120f, Teal, true).Fade(a);
            Key(c, "T", -135f, 42f, DemoTrack.Pulse(0.3f, 0.6f));
            return c;
        }

        private static DemoClip Guide()
        {
            var c = New("guide", 3.4f, 2.1f);
            Backdrop(c);
            Spr(c, "icon.guide", 40f, 0f, 0f).Scale(S(0f, 0.4f, 0.55f, 0.4f, 0.8f, 1.15f, 1.0f, 1f, 1.5f, 1f, 1.7f, 0.5f, 3.4f, 0.5f))
                .Fade(S(0f, 0f, 0.55f, 0f, 0.75f, 1f, 1.5f, 1f, 1.8f, 0f));
            var a = S(0f, 0f, 1.45f, 0f, 1.8f, 1f, 3.0f, 1f, 3.4f, 0f);
            PanelBox(c, 0f, 0f, 190f, 92f, a, Teal);
            Txt(c, "SUB-TERRA 게임 가이드", 11f, 0f, 30f, 180f, new Color(0.84f, 0.98f, 1f, 1f), true).Fade(a);
            for (var i = 0; i < 3; i++)
            {
                Box(c, 52f, 22f, -58f + i * 58f, -4f, new Color(0.1f, 0.2f, 0.26f, 1f)).Fade(a);
            }

            Key(c, "G", -135f, 42f, DemoTrack.Pulse(0.3f, 0.6f));
            return c;
        }

        // ------------------------------------------------------------------ 핵심 메커니즘

        private static DemoClip Base()
        {
            var c = New("base", 3.6f, 0.9f);
            Backdrop(c);
            var names = new[]
            {
                "지하 탐사 시작",
                "자원 판매",
                "업그레이드",
                "새 광산 초기화"
            };
            var icons = new[] { "icon.mine", "icon.sell", "icon.upgrade", "icon.reset" };
            var xs = new[] { 0f, -100f, 0f, 100f };
            var ys = new[] { 28f, -10f, -10f, -10f };
            var ws = new[] { 150f, 92f, 92f, 92f };
            var hs = new[] { 30f, 26f, 26f, 26f };
            for (var i = 0; i < 4; i++)
            {
                var t0 = 0.3f + i * 0.8f;
                var lit = S(0f, 0f, t0, 0f, t0 + 0.15f, 1f, t0 + 0.65f, 1f, t0 + 0.8f, 0f, 3.6f, 0f);
                var fill = Spr(c, "art.fill", 0f, xs[i], ys[i]);
                fill.Size = new Vector2(ws[i], hs[i]);
                fill.SizeIsRect = true;
                fill.Tint = new Color(0.04f, 0.1f, 0.15f, 1f);
                var line = Spr(c, "art.line", 0f, xs[i], ys[i]);
                line.Size = new Vector2(ws[i], hs[i]);
                line.SizeIsRect = true;
                line.Tint = Teal;
                var glow = Spr(c, "art.fill", 0f, xs[i], ys[i]);
                glow.Size = new Vector2(ws[i], hs[i]);
                glow.SizeIsRect = true;
                glow.Tint = new Color(0.42f, 0.94f, 1f, 0.35f);
                glow.Fade(lit);
                Spr(c, icons[i], 16f, xs[i] - ws[i] * 0.5f + 14f, ys[i]);
                Txt(c, names[i], i == 0 ? 11f : 8.5f, xs[i] + 8f, ys[i], ws[i] - 22f, Color.white, true);
            }

            Spr(c, "hud.bolt", 18f, -112f, -43f);
            Spr(c, "hud.energy.bar", 9f, -98f, -43f).PivotAt(0f, 0.5f);
            Txt(c, "도착하면 전력 가득 충전", 9f, 36f, -43f, 130f, new Color(0.62f, 0.88f, 0.95f, 1f));
            return c;
        }

        private static DemoClip Health()
        {
            var c = New("health", 3.6f, 2.5f);
            Backdrop(c);
            Floor(c);
            Player(c, -50f);
            Spr(c, "art.soft", 90f, 50f, 4f).Color_(new Color(0.4f, 1f, 0.55f, 1f)).Fade(S(0f, 0f, 1.5f, 0f, 1.9f, 0.55f, 2.9f, 0f));
            Spr(c, "fac.clinic", 56f, 50f, GroundTop + 28f);
            Spr(c, "hud.heart", 18f, -138f, 44f);
            Spr(c, "hud.hp.empty", 10f, -122f, 44f).PivotAt(0f, 0.5f);
            Spr(c, "hud.hp.bar", 10f, -122f, 44f).PivotAt(0f, 0.5f)
                .Sx(S(0f, 0.35f, 1.5f, 0.35f, 2.7f, 1f, 3.6f, 1f));
            Txt(c, "+", 20f, 50f, 40f, 24f, Green, true).Fade(S(0f, 0f, 1.6f, 0f, 1.9f, 1f, 2.8f, 0f)).PosY(S(0f, 34f, 1.6f, 34f, 2.8f, 50f));
            Key(c, "E", -135f, 18f, DemoTrack.Pulse(1.2f, 1.5f));
            return c;
        }

        private static DemoClip Power()
        {
            var c = New("power", 4.2f, 2.3f);
            Backdrop(c);
            Floor(c);
            Player(c, -70f);
            Spr(c, "hud.bolt", 18f, -138f, 44f);
            Spr(c, "hud.energy.empty", 10f, -122f, 44f).PivotAt(0f, 0.5f);
            Spr(c, "hud.energy.bar", 10f, -122f, 44f).PivotAt(0f, 0.5f).Sx(S(0f, 1f, 0.3f, 1f, 1.5f, 0.02f, 4.2f, 0.02f));
            var pop = S(0f, 0f, 1.7f, 0f, 1.95f, 1f, 2.55f, 1f, 2.85f, 0f);
            PanelBox(c, 54f, 8f, 132f, 62f, pop, Warn);
            Spr(c, "art.warning", 18f, 0f, 24f).Color_(Warn).Fade(pop);
            Txt(c, "전력 고갈", 12f, 62f, 24f, 100f, Warn, true).Fade(pop);
            Txt(c, "비용을 확인하고 구출", 8.5f, 54f, 6f, 120f, new Color(0.85f, 0.94f, 0.98f, 1f)).Fade(pop);
            Box(c, 60f, 14f, 54f, -12f, new Color(0.1f, 0.5f, 0.6f, 1f)).Fade(pop);
            Txt(c, "구출", 9f, 54f, -12f, 60f, Color.white, true).Fade(pop);
            var chip = S(0f, 0f, 2.7f, 0f, 3.0f, 1f, 4.0f, 1f, 4.2f, 0f);
            PanelBox(c, -70f, 40f, 44f, 20f, chip, Teal);
            Spr(c, "art.arrowup", 12f, -78f, 40f).Color_(Teal).Fade(chip);
            Txt(c, "R", 10f, -62f, 40f, 16f, Teal, true).Fade(chip);
            Key(c, "R", -135f, 18f, DemoTrack.Pulse(3.2f, 3.5f));
            return c;
        }

        private static DemoClip Cargo()
        {
            var c = New("cargo", 4f, 2.0f);
            Backdrop(c);
            Floor(c);
            Player(c, -80f);
            Spr(c, "fac.storage", 46f, 70f, GroundTop + 23f);
            Spr(c, "icon.cargo", 18f, -138f, 44f);
            Box(c, 96f, 8f, -80f, 44f, new Color(1f, 1f, 1f, 0.12f));
            Box(c, 96f, 8f, -128f, 44f, Amber).PivotAt(0f, 0.5f).Sx(S(0f, 0.15f, 1.8f, 1f, 2.6f, 1f, 3.4f, 0.25f, 4f, 0.25f)).At(-128f, 44f);
            Txt(c, "가득 참", 10f, -52f, 44f, 60f, Amber, true).Fade(S(0f, 0f, 1.7f, 0f, 1.9f, 1f, 2.5f, 1f, 2.7f, 0f));
            var ids = new[] { DataIds.Minerals.Copper, DataIds.Minerals.Iron, DataIds.Minerals.Lithium };
            for (var i = 0; i < 3; i++)
            {
                var t0 = 2.5f + i * 0.2f;
                Spr(c, "item:" + ids[i], 16f, -70f, 2f)
                    .PosX(S(0f, -70f, t0, -70f, t0 + 0.7f, 66f))
                    .PosY(S(0f, 2f, t0, 2f, t0 + 0.35f, 26f, t0 + 0.7f, 0f))
                    .Fade(S(0f, 0f, t0 - 0.01f, 0f, t0, 1f, t0 + 0.65f, 1f, t0 + 0.75f, 0f));
            }

            Key(c, "E", -135f, 18f, DemoTrack.Pulse(2.3f, 2.55f));
            return c;
        }

        private static DemoClip Grid()
        {
            var c = New("grid", 3.6f, 1.5f);
            Backdrop(c);
            Floor(c);
            var ringAlpha = S(0f, 0.45f, 0.9f, 0.8f, 1.8f, 0.45f, 2.7f, 0.8f, 3.6f, 0.45f);
            Spr(c, "art.soft", 190f, -62f, 0f).Color_(new Color(0.1f, 0.5f, 0.9f, 0.3f));
            Spr(c, "art.ring", 190f, -62f, 0f).Color_(new Color(0.2f, 0.65f, 1f, 1f)).Fade(ringAlpha);
            Spr(c, "fac.core", 62f, -62f, GroundTop + 31f);
            Spr(c, "fac.charger", 46f, -12f, GroundTop + 23f);
            Spr(c, "fac.clinic", 46f, 62f, GroundTop + 23f).Color_(new Color(0.45f, 0.5f, 0.55f, 1f));
            Spr(c, "fac.settlement", 50f, 116f, GroundTop + 25f).Color_(new Color(0.45f, 0.5f, 0.55f, 1f));
            Txt(c, "연결됨", 9f, -12f, -41f, 60f, Teal, true);
            Txt(c, "미연결", 9f, 62f, -41f, 60f, Warn, true);
            Txt(c, "미연결", 9f, 116f, -41f, 60f, Warn, true);
            Spr(c, "hud.bolt", 16f, -12f, 30f).Fade(S(0f, 0.4f, 0.6f, 1f, 1.2f, 0.4f, 1.8f, 1f, 3.6f, 0.4f));
            return c;
        }

        private static DemoClip Core()
        {
            var c = New("core", 4f, 1.8f);
            Backdrop(c);
            Floor(c);
            Spr(c, "art.ring", 120f, -84f, 0f).Color_(new Color(0.2f, 0.65f, 1f, 0.7f));
            Spr(c, "fac.core", 64f, -84f, GroundTop + 32f);
            Player(c, -126f);
            PanelBox(c, 66f, 6f, 168f, 100f, K(1f), Teal);
            var names = new[]
            {
                ItemDisplayNames.Building(DataIds.Buildings.ChargerBasic),
                ItemDisplayNames.Building(DataIds.Buildings.ClinicBasic),
                ItemDisplayNames.Building(DataIds.Buildings.SettlementBasic)
            };
            var previews = new[] { "fac.charger", "fac.clinic", "fac.settlement" };
            for (var i = 0; i < 3; i++)
            {
                var y = 30f - i * 22f;
                var t0 = 0.3f + i * 1.2f;
                var sel = S(0f, 0f, t0, 0f, t0 + 0.12f, 1f, t0 + 1.08f, 1f, t0 + 1.2f, 0f, 4f, 0f);
                var row = Spr(c, "art.fill", 0f, 36f, y);
                row.Size = new Vector2(66f, 18f);
                row.SizeIsRect = true;
                row.Tint = new Color(0.42f, 0.94f, 1f, 0.35f);
                row.Fade(sel);
                Txt(c, names[i], 9f, 36f, y, 62f, Color.white, true);
                Spr(c, previews[i], 50f, 100f, -2f).Fade(sel);
            }

            Box(c, 6f, 6f, 126f, 44f, Warn).Fade(S(0f, 1f, 0.5f, 0.2f, 1f, 1f, 1.5f, 0.2f, 2f, 1f, 2.5f, 0.2f, 3f, 1f, 3.5f, 0.2f, 4f, 1f));
            Txt(c, "REC", 7f, 138f, 44f, 20f, Warn, true);
            Key(c, "E", -135f, 42f, DemoTrack.Pulse(0.1f, 0.3f));
            return c;
        }

        private static DemoClip Structure()
        {
            var c = New("structure", 4.2f, 1.9f);
            Backdrop(c);
            for (var x = -153f; x <= 153.1f; x += Unit)
            {
                Tile(c, "tile.ground", x, GroundTop - Unit * 0.5f);
                Tile(c, "tile.hard", x, GroundTop - Unit * 1.5f);
                Tile(c, "tile.ground", x, 26f + Unit * 0.5f);
            }

            Player(c, -78f);
            Tile(c, "tile.crack.yellow", 56f, 26f + Unit * 0.5f).Fade(S(0f, 0f, 0.3f, 1f, 1.0f, 1f, 1.2f, 0f, 4.2f, 0f));
            Tile(c, "tile.crack.orange", 56f, 26f + Unit * 0.5f).Fade(S(0f, 0f, 0.9f, 0f, 1.2f, 1f, 1.5f, 1f, 1.7f, 0f, 4.2f, 0f));
            Tile(c, "tile.crack.red", 56f, 26f + Unit * 0.5f)
                .Fade(S(0f, 0f, 1.5f, 0f, 1.7f, 1f, 1.9f, 0.4f, 2.1f, 1f, 2.3f, 0.4f, 2.5f, 1f, 2.6f, 0f, 4.2f, 0f));
            Tile(c, "tile.ground", 56f, 26f + Unit * 0.5f, 28f).Fade(S(0f, 0f, 2.55f, 0f, 2.6f, 1f, 3.05f, 1f, 3.1f, 0f))
                .PosY(S(0f, 43f, 2.6f, 43f, 3.05f, GroundTop + 14f));
            Spr(c, "art.soft", 56f, 56f, GroundTop + 4f).Color_(new Color(0.8f, 0.7f, 0.55f, 1f))
                .Fade(S(0f, 0f, 3.0f, 0f, 3.15f, 0.7f, 3.8f, 0f));
            Spr(c, "fac.support", 52f, 18f, GroundTop + 26f).Fade(S(0f, 0f, 3.4f, 0f, 3.6f, 1f, 4.2f, 1f));
            Spr(c, "hud.structural.critical", 22f, 56f, 8f).Fade(S(0f, 0f, 1.5f, 0f, 1.7f, 1f, 2.6f, 1f, 2.7f, 0f));
            return c;
        }

        private static DemoClip Gas()
        {
            var c = New("gas", 4f, 2.4f);
            Backdrop(c);
            for (var x = -153f; x <= 153.1f; x += Unit)
            {
                Tile(c, x > 20f ? "tile.gas" : "tile.ground", x, GroundTop - Unit * 0.5f);
                Tile(c, "tile.hard", x, GroundTop - Unit * 1.5f);
                Tile(c, "tile.ground", x, 26f + Unit * 0.5f);
            }

            Walker(c, S(0f, -100f, 1.1f, -14f, 3.0f, 40f, 4f, 40f), H(0f, 1f, 3.2f, 0f));
            Spr(c, "art.soft", 200f, 74f, 0f).Color_(new Color(0.5f, 1f, 0.3f, 0.45f)).Sx(K(1.3f));
            Box(c, StageWidth, StageHeight, 0f, 0f, new Color(0f, 0.04f, 0.02f, 1f)).Fade(S(0f, 0f, 1.2f, 0f, 3.0f, 0.5f, 4f, 0.5f));
            Spr(c, "hud.bolt", 18f, -138f, 44f);
            Spr(c, "hud.energy.empty", 10f, -122f, 44f).PivotAt(0f, 0.5f);
            Spr(c, "hud.energy.bar", 10f, -122f, 44f).PivotAt(0f, 0.5f).Sx(S(0f, 1f, 1.2f, 1f, 3.0f, 0.45f, 4f, 0.45f));
            Spr(c, "art.warning", 20f, 40f, 34f).Color_(Amber).Fade(S(0f, 0f, 1.3f, 0f, 1.5f, 1f, 1.9f, 0.5f, 2.3f, 1f, 2.7f, 0.5f, 3.1f, 1f, 4f, 1f));
            return c;
        }

        private static DemoClip Reset()
        {
            var c = New("reset", 4f, 2.5f);
            Backdrop(c);
            var a = S(0f, 1f, 1.6f, 1f, 2.0f, 0f, 4f, 0f);
            var b = S(0f, 0f, 1.8f, 0f, 2.2f, 1f, 3.6f, 1f, 4f, 0f);
            var kinds = new[] { "tile.ground", "tile.hard", "tile.ground", "tile.ore.copper", "tile.ground", "tile.hard" };
            var kinds2 = new[] { "tile.hard", "tile.ground", "tile.ore.lithium", "tile.ground", "tile.ore.iron", "tile.ground" };
            for (var i = 0; i < 6; i++)
            {
                var x = -105f + (i % 3) * 36f;
                var y = -26f - (i / 3) * 36f;
                Tile(c, kinds[i], x, y).Fade(a);
                Tile(c, kinds2[i], x + 144f, y).Fade(b);
            }

            Tile(c, "tile.crack.red", -87f, -26f).Fade(a);
            Spr(c, "art.arrow", 28f, 0f, -38f).Color_(Teal);
            var blink = S(0f, 1f, 0.5f, 0.45f, 1.0f, 1f, 1.5f, 0.45f, 2.0f, 1f, 2.5f, 0.45f, 3.0f, 1f, 4f, 1f);
            PanelBox(c, 0f, 34f, 132f, 40f, K(1f), Warn);
            Txt(c, "00:00:00", 17f, 0f, 30f, 120f, Warn, true).Fade(blink);
            Txt(c, "광산 초기화까지", 8f, 0f, 44f, 120f, new Color(0.9f, 0.7f, 0.68f, 1f));
            return c;
        }

        // ------------------------------------------------------------------ 자원·시설

        private static DemoClip ResourceScene(string id, string source, string icon, string caption)
        {
            var c = New(id, 3f, 0.6f);
            Backdrop(c);
            Spr(c, "art.soft", 110f, 56f, 4f).Color_(new Color(0.3f, 0.8f, 1f, 1f)).Fade(S(0f, 0.25f, 1.5f, 0.55f, 3f, 0.25f));
            Tile(c, source, -70f, 0f, 58f);
            Spr(c, "art.arrow", 24f, -14f, 0f).Color_(Teal);
            Spr(c, icon, 58f, 54f, 0f).PosY(S(0f, 0f, 0.75f, 5f, 1.5f, 0f, 2.25f, 5f, 3f, 0f));
            if (!string.IsNullOrEmpty(caption))
            {
                Txt(c, caption, 9f, -70f, -40f, 80f, Amber, true);
            }

            return c;
        }

        private static DemoClip FacilityScene(string id, string sprite, float height, bool withPlayer, Color glow)
        {
            var c = New(id, 3f, 0.8f);
            Backdrop(c);
            Floor(c);
            Spr(c, "art.soft", height * 1.8f, 30f, GroundTop + height * 0.5f).Color_(glow)
                .Fade(S(0f, 0.2f, 1.5f, 0.5f, 3f, 0.2f));
            Spr(c, sprite, height, 30f, GroundTop + height * 0.5f);
            if (withPlayer)
            {
                Spr(c, "player.idle", PlayerHeight, -70f, PlayerY);
            }

            return c;
        }

        private static DemoClip LadderFacility()
        {
            var c = New("fac.ladder", 3f, 0.8f);
            Backdrop(c);
            Floor(c);
            for (var i = 0; i < 4; i++)
            {
                Spr(c, "fac.ladder", Unit, 30f, GroundTop + Unit * 0.5f + i * Unit);
            }

            Spr(c, "player.ladder.1", PlayerHeight, 30f, 6f).Animate("player.ladder", 5, 8f, "player.ladder.1", K(1f))
                .PosY(S(0f, -8f, 1.5f, 30f, 3f, -8f));
            return c;
        }

        private static DemoClip SupportFacility()
        {
            var c = New("fac.support", 3f, 0.8f);
            Backdrop(c);
            for (var x = -153f; x <= 153.1f; x += Unit)
            {
                Tile(c, "tile.ground", x, GroundTop - Unit * 0.5f);
                Tile(c, "tile.hard", x, GroundTop - Unit * 1.5f);
                Tile(c, "tile.ground", x, 26f + Unit * 0.5f);
            }

            Spr(c, "art.soft", 100f, 30f, 0f).Color_(Teal).Fade(S(0f, 0.15f, 1.5f, 0.4f, 3f, 0.15f));
            Spr(c, "fac.support", 52f, 30f, GroundTop + 26f);
            Player(c, -70f);
            return c;
        }

        // ------------------------------------------------------------------ 첫 탐사 안내 전용

        private static DemoClip FirstEnter()
        {
            var c = New("first.enter", 3.4f, 1.9f);
            Backdrop(c);
            var click = S(0f, 0f, 0.9f, 0f, 1.0f, 1f, 1.4f, 1f, 1.55f, 0f, 3.4f, 0f);
            var fill = Spr(c, "art.fill", 0f, -64f, 8f);
            fill.Size = new Vector2(124f, 34f);
            fill.SizeIsRect = true;
            fill.Tint = new Color(0.04f, 0.1f, 0.15f, 1f);
            var glow = Spr(c, "art.fill", 0f, -64f, 8f);
            glow.Size = new Vector2(124f, 34f);
            glow.SizeIsRect = true;
            glow.Tint = new Color(0.42f, 0.94f, 1f, 0.5f);
            glow.Fade(click);
            var line = Spr(c, "art.line", 0f, -64f, 8f);
            line.Size = new Vector2(124f, 34f);
            line.SizeIsRect = true;
            line.Tint = Teal;
            Spr(c, "icon.mine", 20f, -108f, 8f);
            Txt(c, "지하 탐사 시작", 10f, -58f, 8f, 100f, Color.white, true);
            Spr(c, "art.arrow", 22f, 4f, 8f).Color_(Teal).Fade(S(0f, 0.3f, 1.5f, 0.3f, 1.8f, 1f, 3.4f, 1f));
            Floor(c, 60f, 153f);
            Spr(c, "player.idle", PlayerHeight, 100f, PlayerY).Fade(S(0f, 0f, 1.6f, 0f, 2.0f, 1f, 3.4f, 1f));
            return c;
        }

        private static DemoClip FirstStatus()
        {
            var c = New("first.status", 3.4f, 1.0f);
            Backdrop(c);
            Floor(c, 60f, 153f);
            Spr(c, "player.idle", PlayerHeight, 104f, PlayerY);
            var icons = new[] { "hud.heart", "hud.bolt", "icon.cargo" };
            var empties = new[] { "hud.hp.empty", "hud.energy.empty", string.Empty };
            var fills = new[] { "hud.hp.bar", "hud.energy.bar", string.Empty };
            var amounts = new[] { 0.9f, 0.7f, 0.5f };
            for (var i = 0; i < 3; i++)
            {
                var y = 34f - i * 28f;
                Spr(c, icons[i], 22f, -128f, y);
                if (!string.IsNullOrEmpty(empties[i]))
                {
                    Spr(c, empties[i], 12f, -108f, y).PivotAt(0f, 0.5f);
                    Spr(c, fills[i], 12f, -108f, y).PivotAt(0f, 0.5f).Sx(S(0f, 0.4f, 0.8f, amounts[i], 3.4f, amounts[i]));
                }
                else
                {
                    Box(c, 94f, 9f, -61f, y, new Color(1f, 1f, 1f, 0.12f));
                    Box(c, 94f, 9f, -108f, y, Amber).PivotAt(0f, 0.5f).At(-108f, y).Sx(S(0f, 0.2f, 0.8f, amounts[i], 3.4f, amounts[i]));
                }
            }

            return c;
        }

        private static DemoClip FirstSell()
        {
            var c = New("first.sell", 3.4f, 1.9f);
            Backdrop(c);
            Floor(c);
            Spr(c, "fac.settlement", 60f, 70f, GroundTop + 30f);
            var ids = new[] { DataIds.Minerals.Copper, DataIds.Minerals.Iron, DataIds.Minerals.Lithium };
            for (var i = 0; i < 3; i++)
            {
                var t0 = 0.2f + i * 0.3f;
                Spr(c, "item:" + ids[i], 18f, -100f + i * 22f, 8f)
                    .PosX(S(0f, -100f + i * 22f, t0, -100f + i * 22f, t0 + 1.0f, 66f))
                    .PosY(S(0f, 8f, t0, 8f, t0 + 0.5f, 26f, t0 + 1.0f, 8f))
                    .Fade(S(0f, 1f, t0 + 0.95f, 1f, t0 + 1.05f, 0f, 3.4f, 0f));
            }

            for (var i = 0; i < 3; i++)
            {
                var t0 = 1.5f + i * 0.2f;
                Spr(c, "fx.coin", 16f, 70f, 14f)
                    .PosX(S(0f, 70f, t0, 70f, t0 + 0.8f, 70f + (i - 1) * 24f))
                    .PosY(S(0f, 14f, t0, 14f, t0 + 0.4f, 46f, t0 + 0.8f, 30f))
                    .Fade(S(0f, 0f, t0, 0f, t0 + 0.05f, 1f, 3.0f, 1f, 3.3f, 0f));
            }

            Spr(c, "icon.upgrade", 20f, 130f, 40f).Fade(S(0f, 0f, 2.4f, 0f, 2.7f, 1f, 3.2f, 1f, 3.4f, 0f));
            return c;
        }
    }
}
