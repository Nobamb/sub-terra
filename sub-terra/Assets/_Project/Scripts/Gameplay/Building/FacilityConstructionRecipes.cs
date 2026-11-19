using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    public enum ConstructionEase
    {
        OutCubic,
        /// <summary>목표를 살짝 넘었다가 돌아온다(조립·튀어나옴).</summary>
        OutBack,
        /// <summary>빠르게 내려꽂히고 한 번 튕긴다(무거운 시설).</summary>
        SlamBounce
    }

    public enum ConstructionShapeKind { Square, Glow, Ring }

    /// <summary>
    /// 완성 그림의 일부(가로·세로 비율 구간)를 잘라 낸 조각. 시간은 전체 연출 길이 대비 0~1.
    /// Source는 몸체 렌더러 이름(Artwork, Cap, Post).
    /// </summary>
    public readonly struct ConstructionPiece
    {
        public readonly string Source;
        public readonly float X0, X1, Y0, Y1;
        public readonly Vector2 From;
        public readonly Vector2 ScaleFrom;
        public readonly float Delay, Duration;
        public readonly ConstructionEase Ease;
        public readonly float AlphaFrom;

        public ConstructionPiece(string source, float x0, float x1, float y0, float y1, Vector2 from,
            float delay, float duration, ConstructionEase ease, Vector2? scaleFrom = null, float alphaFrom = 0.15f)
        {
            Source = source;
            X0 = x0; X1 = x1; Y0 = y0; Y1 = y1;
            From = from;
            ScaleFrom = scaleFrom ?? Vector2.one;
            Delay = delay;
            Duration = duration;
            Ease = ease;
            AlphaFrom = alphaFrom;
        }
    }

    /// <summary>스캔 선·섬광·충격 고리 같은 홀로그램 덧그림. 위치는 시설 보이는 영역 비율, 크기는 보이는 폭 비율.</summary>
    public readonly struct ConstructionShape
    {
        public readonly ConstructionShapeKind Kind;
        public readonly Vector2 PosFrom, PosTo;
        public readonly Vector2 SizeFrom, SizeTo;
        public readonly Color Color;
        public readonly float Start, End;
        /// <summary>구간 안에서 알파가 가장 짙은 시점(0~1).</summary>
        public readonly float Peak;
        public readonly bool Flicker;

        public ConstructionShape(ConstructionShapeKind kind, Vector2 posFrom, Vector2 posTo,
            Vector2 sizeFrom, Vector2 sizeTo, Color color, float start, float end, float peak = 0.4f, bool flicker = false)
        {
            Kind = kind;
            PosFrom = posFrom; PosTo = posTo;
            SizeFrom = sizeFrom; SizeTo = sizeTo;
            Color = color;
            Start = start; End = end;
            Peak = Mathf.Clamp(peak, 0.05f, 0.95f);
            Flicker = flicker;
        }
    }

    public sealed class ConstructionRecipe
    {
        public string Style { get; }
        public IReadOnlyList<ConstructionPiece> Pieces { get; }
        public IReadOnlyList<ConstructionShape> Shapes { get; }

        public ConstructionRecipe(string style, List<ConstructionPiece> pieces, List<ConstructionShape> shapes)
        {
            Style = style;
            Pieces = pieces;
            Shapes = shapes;
        }
    }

    /// <summary>
    /// B-206: 시설마다 다른 건설 과정. 모두 같은 0.5초 안에서 끝나고, 청록 홀로그램이 실제 완성 그림으로 굳는 톤을 쓴다.
    /// 숫자는 PM 조정용이라 한곳에 모았다.
    /// </summary>
    public static class FacilityConstructionRecipes
    {
        public const string Body = "Artwork";

        private static readonly Color Cyan = new(0.26f, 0.9f, 1f, 0.9f);
        private static readonly Color CyanCore = new(0.78f, 1f, 1f, 0.95f);
        private static readonly Color Warm = new(1f, 0.9f, 0.55f, 0.8f);
        private static readonly Color Medical = new(0.45f, 1f, 0.72f, 0.9f);
        private static readonly Color Dust = new(0.72f, 0.82f, 0.86f, 0.5f);

        private static Dictionary<string, ConstructionRecipe> cache;

        public static IEnumerable<string> BuildingIds => All.Keys;

        private static Dictionary<string, ConstructionRecipe> All => cache ??= Create();

        public static bool TryGet(string buildingId, out ConstructionRecipe recipe)
        {
            recipe = null;
            return !string.IsNullOrEmpty(buildingId) && All.TryGetValue(buildingId, out recipe);
        }

        private static Dictionary<string, ConstructionRecipe> Create() => new()
        {
            ["building.ladder.basic"] = Ladder(),
            ["building.storage.basic"] = Storage(),
            ["building.charger.basic"] = Charger(),
            ["building.light.basic"] = LightLamp(),
            ["building.clinic.basic"] = Clinic(),
            ["building.settlement.basic"] = Settlement(),
            ["building.outpost_core.basic"] = Outpost(),
            ["building.escape_portal.emergency"] = Portal(),
            ["building.support.basic"] = Cushion()
        };

        private static ConstructionPiece Piece(string source, float x0, float x1, float y0, float y1, Vector2 from,
            float delay, float duration, ConstructionEase ease, Vector2? scaleFrom = null, float alphaFrom = 0.15f)
            => new(source, x0, x1, y0, y1, from, delay, duration, ease, scaleFrom, alphaFrom);

        private static ConstructionShape Shape(ConstructionShapeKind kind, Vector2 pos, Vector2 posTo,
            Vector2 size, Vector2 sizeTo, Color color, float start, float end, float peak = 0.4f, bool flicker = false)
            => new(kind, pos, posTo, size, sizeTo, color, start, end, peak, flicker);

        /// <summary>사다리: 단이 아래에서 위로 하나씩 올라오고 청록 빛이 훑고 지나간다.</summary>
        private static ConstructionRecipe Ladder()
        {
            var pieces = new List<ConstructionPiece>();
            const int steps = 5;
            for (int i = 0; i < steps; i++)
                pieces.Add(Piece(Body, 0f, 1f, i / (float)steps, (i + 1) / (float)steps, new Vector2(0f, -0.32f),
                    i * 0.12f, 0.4f, ConstructionEase.OutBack, new Vector2(0.7f, 1f)));
            var shapes = new List<ConstructionShape>
            {
                Shape(ConstructionShapeKind.Square, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                    new Vector2(0.9f, 0.05f), new Vector2(0.9f, 0.05f), CyanCore, 0f, 0.92f, 0.5f)
            };
            return new ConstructionRecipe("ladder-rise", pieces, shapes);
        }

        /// <summary>보관함: 좌우 함체가 맞물리고 뚜껑이 내려앉는다. 먼지가 튄다.</summary>
        private static ConstructionRecipe Storage()
        {
            var pieces = new List<ConstructionPiece>
            {
                Piece(Body, 0f, 0.5f, 0f, 0.6f, new Vector2(-0.9f, 0f), 0f, 0.45f, ConstructionEase.OutCubic),
                Piece(Body, 0.5f, 1f, 0f, 0.6f, new Vector2(0.9f, 0f), 0.05f, 0.45f, ConstructionEase.OutCubic),
                Piece(Body, 0f, 1f, 0.6f, 1f, new Vector2(0f, 0.8f), 0.3f, 0.5f, ConstructionEase.SlamBounce, new Vector2(1.05f, 1f))
            };
            var shapes = new List<ConstructionShape>
            {
                Shape(ConstructionShapeKind.Glow, new Vector2(0.08f, 0.03f), new Vector2(-0.08f, 0.06f),
                    new Vector2(0.1f, 0.05f), new Vector2(0.4f, 0.14f), Dust, 0.38f, 0.85f, 0.25f),
                Shape(ConstructionShapeKind.Glow, new Vector2(0.92f, 0.03f), new Vector2(1.08f, 0.06f),
                    new Vector2(0.1f, 0.05f), new Vector2(0.4f, 0.14f), Dust, 0.38f, 0.85f, 0.25f),
                Shape(ConstructionShapeKind.Square, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f),
                    new Vector2(0.95f, 0.04f), new Vector2(0.95f, 0.02f), CyanCore, 0.55f, 0.9f, 0.3f)
            };
            return new ConstructionRecipe("storage-assemble", pieces, shapes);
        }

        /// <summary>충전기: 세 줄의 코일이 차례로 솟고, 스캔 후 방전 섬광이 튄다.</summary>
        private static ConstructionRecipe Charger()
        {
            var pieces = new List<ConstructionPiece>();
            for (int i = 0; i < 3; i++)
                pieces.Add(Piece(Body, i / 3f, (i + 1) / 3f, 0f, 1f, new Vector2(0f, -0.7f), i * 0.1f, 0.4f,
                    ConstructionEase.OutCubic, new Vector2(1f, 0.6f)));
            var shapes = new List<ConstructionShape>
            {
                Shape(ConstructionShapeKind.Square, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                    new Vector2(0.9f, 0.05f), new Vector2(0.9f, 0.05f), Cyan, 0.15f, 0.72f, 0.5f),
                Shape(ConstructionShapeKind.Square, new Vector2(0.3f, 0.5f), new Vector2(0.3f, 0.5f),
                    new Vector2(0.03f, 0.8f), new Vector2(0.03f, 0.8f), CyanCore, 0.6f, 0.88f, 0.4f, true),
                Shape(ConstructionShapeKind.Square, new Vector2(0.7f, 0.5f), new Vector2(0.7f, 0.5f),
                    new Vector2(0.03f, 0.8f), new Vector2(0.03f, 0.8f), CyanCore, 0.66f, 0.94f, 0.4f, true),
                Shape(ConstructionShapeKind.Glow, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
                    new Vector2(0.2f, 0.2f), new Vector2(1.1f, 1.1f), CyanCore, 0.65f, 1f, 0.25f)
            };
            return new ConstructionRecipe("charger-coil", pieces, shapes);
        }

        /// <summary>조명: 기둥이 솟은 뒤 등이 톡 튀어나오며 두 번 깜빡인다.</summary>
        private static ConstructionRecipe LightLamp()
        {
            var pieces = new List<ConstructionPiece>
            {
                Piece(Body, 0f, 1f, 0f, 0.55f, new Vector2(0f, -0.5f), 0f, 0.4f, ConstructionEase.OutCubic),
                Piece(Body, 0f, 1f, 0.55f, 1f, new Vector2(0f, -0.15f), 0.3f, 0.55f, ConstructionEase.OutBack, new Vector2(0.3f, 0.3f))
            };
            var shapes = new List<ConstructionShape>
            {
                Shape(ConstructionShapeKind.Glow, new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f),
                    new Vector2(0.3f, 0.3f), new Vector2(1.5f, 1.5f), Warm, 0.55f, 0.78f, 0.3f),
                Shape(ConstructionShapeKind.Glow, new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f),
                    new Vector2(0.3f, 0.3f), new Vector2(1.8f, 1.8f), Warm, 0.74f, 1f, 0.2f)
            };
            return new ConstructionRecipe("light-bloom", pieces, shapes);
        }

        /// <summary>보건소: 벽 패널이 좌우에서 번갈아 끼워지고 지붕이 얹힌 뒤 녹청색 십자가 반짝인다.</summary>
        private static ConstructionRecipe Clinic()
        {
            var pieces = new List<ConstructionPiece>
            {
                Piece(Body, 0f, 1f, 0f, 0.4f, new Vector2(-1f, 0f), 0f, 0.4f, ConstructionEase.OutCubic),
                Piece(Body, 0f, 1f, 0.4f, 0.72f, new Vector2(1f, 0f), 0.12f, 0.4f, ConstructionEase.OutCubic),
                Piece(Body, 0f, 1f, 0.72f, 1f, new Vector2(0f, 0.7f), 0.28f, 0.5f, ConstructionEase.OutBack)
            };
            var shapes = new List<ConstructionShape>
            {
                Shape(ConstructionShapeKind.Glow, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
                    new Vector2(0.2f, 0.2f), new Vector2(0.9f, 0.9f), Medical, 0.6f, 1f, 0.3f),
                Shape(ConstructionShapeKind.Square, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
                    new Vector2(0.1f, 0.1f), new Vector2(0.34f, 0.1f), Medical, 0.6f, 1f, 0.35f),
                Shape(ConstructionShapeKind.Square, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
                    new Vector2(0.1f, 0.1f), new Vector2(0.1f, 0.34f), Medical, 0.6f, 1f, 0.35f)
            };
            return new ConstructionRecipe("clinic-panels", pieces, shapes);
        }

        /// <summary>정산 콘솔: 본체가 올라오고 화면이 위에서 아래로 줄지어 부팅한다.</summary>
        private static ConstructionRecipe Settlement()
        {
            var pieces = new List<ConstructionPiece>
            {
                Piece(Body, 0f, 1f, 0f, 0.5f, new Vector2(0f, -0.4f), 0f, 0.35f, ConstructionEase.OutCubic)
            };
            const int lines = 5;
            for (int i = 0; i < lines; i++)
            {
                int fromTop = lines - 1 - i;
                pieces.Add(Piece(Body, 0f, 1f, 0.5f + 0.5f * i / lines, 0.5f + 0.5f * (i + 1) / lines, Vector2.zero,
                    0.3f + fromTop * 0.08f, 0.2f, ConstructionEase.OutCubic, new Vector2(1f, 0.2f), 0f));
            }
            var shapes = new List<ConstructionShape>
            {
                Shape(ConstructionShapeKind.Square, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.9f, 0.04f), new Vector2(0.9f, 0.04f), CyanCore, 0.3f, 0.78f, 0.5f, true),
                Shape(ConstructionShapeKind.Glow, new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f),
                    new Vector2(0.2f, 0.12f), new Vector2(0.9f, 0.5f), Cyan, 0.75f, 1f, 0.3f)
            };
            return new ConstructionRecipe("settlement-boot", pieces, shapes);
        }

        /// <summary>전진기지 코어: 거대한 모듈이 위에서 차례로 내려꽂히며 충격 고리가 퍼진다.</summary>
        private static ConstructionRecipe Outpost()
        {
            var pieces = new List<ConstructionPiece>
            {
                Piece(Body, 0f, 1f, 0f, 0.45f, new Vector2(0f, 1.2f), 0f, 0.5f, ConstructionEase.SlamBounce, new Vector2(1f, 1.1f)),
                Piece(Body, 0f, 1f, 0.45f, 0.8f, new Vector2(0f, 1.4f), 0.12f, 0.5f, ConstructionEase.SlamBounce, new Vector2(1f, 1.1f)),
                Piece(Body, 0f, 1f, 0.8f, 1f, new Vector2(0f, 1.6f), 0.25f, 0.5f, ConstructionEase.OutBack, new Vector2(0.9f, 1.3f))
            };
            var shapes = new List<ConstructionShape>
            {
                Shape(ConstructionShapeKind.Ring, new Vector2(0.5f, 0.02f), new Vector2(0.5f, 0.02f),
                    new Vector2(0.3f, 0.08f), new Vector2(1.7f, 0.2f), Cyan, 0.3f, 0.75f, 0.15f),
                Shape(ConstructionShapeKind.Ring, new Vector2(0.5f, 0.02f), new Vector2(0.5f, 0.02f),
                    new Vector2(0.3f, 0.08f), new Vector2(1.3f, 0.16f), CyanCore, 0.45f, 0.9f, 0.15f),
                Shape(ConstructionShapeKind.Glow, new Vector2(0.15f, 0.04f), new Vector2(-0.1f, 0.07f),
                    new Vector2(0.1f, 0.06f), new Vector2(0.45f, 0.16f), Dust, 0.32f, 0.85f, 0.25f),
                Shape(ConstructionShapeKind.Glow, new Vector2(0.85f, 0.04f), new Vector2(1.1f, 0.07f),
                    new Vector2(0.1f, 0.06f), new Vector2(0.45f, 0.16f), Dust, 0.32f, 0.85f, 0.25f),
                Shape(ConstructionShapeKind.Glow, new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.95f),
                    new Vector2(0.15f, 0.15f), new Vector2(0.8f, 0.8f), CyanCore, 0.72f, 1f, 0.3f)
            };
            return new ConstructionRecipe("outpost-slam", pieces, shapes);
        }

        /// <summary>비상 탈출 포탈: 바깥의 세로 조각이 중심으로 모여 홀을 이루고 고리가 번진다.</summary>
        private static ConstructionRecipe Portal()
        {
            var pieces = new List<ConstructionPiece>();
            const int columns = 7;
            for (int i = 0; i < columns; i++)
            {
                float center = (i + 0.5f) / columns - 0.5f;
                float edge = Mathf.Abs(center) * 2f;
                pieces.Add(Piece(Body, i / (float)columns, (i + 1) / (float)columns, 0f, 1f,
                    new Vector2(center * 2f, 0f), (1f - edge) * 0.22f, 0.42f, ConstructionEase.OutCubic,
                    new Vector2(0.6f, 0.6f), 0f));
            }
            var shapes = new List<ConstructionShape>
            {
                Shape(ConstructionShapeKind.Ring, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.2f, 0.2f), new Vector2(1.25f, 1.25f), Cyan, 0.35f, 0.92f, 0.3f),
                Shape(ConstructionShapeKind.Glow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.2f, 0.2f), new Vector2(1.0f, 1.0f), CyanCore, 0.45f, 1f, 0.25f)
            };
            return new ConstructionRecipe("portal-converge", pieces, shapes);
        }

        /// <summary>완충판: 기둥이 땅에서 자라고 윗면이 내려앉으며 충격 고리가 번진다.</summary>
        private static ConstructionRecipe Cushion()
        {
            var pieces = new List<ConstructionPiece>
            {
                Piece("Post", 0f, 1f, 0f, 1f, Vector2.zero, 0f, 0.45f, ConstructionEase.OutCubic, new Vector2(1f, 0.1f)),
                Piece("Cap", 0f, 1f, 0f, 1f, new Vector2(0f, 0.9f), 0.35f, 0.55f, ConstructionEase.SlamBounce, new Vector2(1.1f, 1f))
            };
            var shapes = new List<ConstructionShape>
            {
                Shape(ConstructionShapeKind.Ring, new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f),
                    new Vector2(0.4f, 0.1f), new Vector2(1.5f, 0.2f), Cyan, 0.6f, 1f, 0.2f),
                Shape(ConstructionShapeKind.Glow, new Vector2(0.5f, 0.05f), new Vector2(0.5f, 0.05f),
                    new Vector2(0.2f, 0.1f), new Vector2(0.9f, 0.25f), Dust, 0.3f, 0.7f, 0.3f)
            };
            return new ConstructionRecipe("cushion-drop", pieces, shapes);
        }
    }
}
