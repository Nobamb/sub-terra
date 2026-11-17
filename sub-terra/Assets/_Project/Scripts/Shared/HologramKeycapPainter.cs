using System;

namespace SubTerra.Shared
{
    /// <summary>한 픽셀의 straight alpha 색.</summary>
    public readonly struct HologramPixel
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte A;

        public HologramPixel(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }

    /// <summary>
    /// 완충판 방향키 홀로그램(#349)과 긴급탈출 R키(#358)가 함께 쓰는 각진 청록 키캡 그리기.
    /// 모서리를 45도로 깎은 팔각 윗면 + 아래로 두께가 보이는 옆면이며, 좌상단은 밝고 우하단은 어두운 모따기 면과
    /// 가로 주사선이 있는 안쪽 판으로 이루어진다. 윗면 글리프(화살표 등)는 호출자가 <c>glyphLevel</c>로 얹는다.
    /// UnityEngine에 의존하지 않아 App·Gameplay 양쪽에서 같은 모양·색을 만든다.
    /// </summary>
    public static class HologramKeycapPainter
    {
        public const int KeySize = 40;

        // 키캡 그림 치수(텍스처 좌표, y가 위쪽). 윗면 위에 두께(Depth)만큼 내려간 옆면이 붙는다.
        public const int FaceX0 = 3;
        public const int FaceX1 = 36;
        public const int FaceY0 = 8;
        public const int FaceY1 = 36;
        public const int Depth = 5;
        public const int Chamfer = 6;

        /// <summary>윗면 안쪽 판에서 글리프 위치를 알려 주는 함수. 0 없음, 1 둘레 빛, 2 밝은 면, 3 어두운 면.</summary>
        public delegate int GlyphLevel(int x, int y);

        public static readonly HologramPixel Clear = new HologramPixel(0, 0, 0, 0);

        /// <summary>40x40 텍스처의 (x, y) 픽셀. 키캡 밖이면 투명.</summary>
        public static HologramPixel Shade(int x, int y, GlyphLevel glyphLevel)
        {
            if (InsideCap(x, y, FaceY0, FaceY1)) return ShadeTopFace(x, y, glyphLevel);
            if (InsideCap(x, y, FaceY0 - Depth, FaceY1 - Depth)) return ShadeSide(x, y);
            return Clear;
        }

        /// <summary>모서리를 45도로 깎은 팔각형 안쪽인지.</summary>
        public static bool InsideCap(int x, int y, int y0, int y1)
        {
            if (x < FaceX0 || x > FaceX1 || y < y0 || y > y1) return false;
            int dx = Math.Min(x - FaceX0, FaceX1 - x);
            int dy = Math.Min(y - y0, y1 - y);
            return dx + dy >= Chamfer;
        }

        private static HologramPixel ShadeSide(int x, int y)
        {
            // 윗면 바로 아래로 보이는 두께. 아래 가장자리는 빛나는 선, 오른쪽으로 갈수록 어둡다.
            if (!InsideCap(x, y - 1, FaceY0 - Depth, FaceY1 - Depth)) return new HologramPixel(48, 208, 214, 255);
            float t = InverseLerp(FaceX0, FaceX1, x);
            return Lerp(new HologramPixel(16, 118, 132, 238), new HologramPixel(6, 62, 78, 238), t);
        }

        private static HologramPixel ShadeTopFace(int x, int y, GlyphLevel glyphLevel)
        {
            int dl = x - FaceX0;
            int dr = FaceX1 - x;
            int db = y - FaceY0;
            int dt = FaceY1 - y;
            const float diagonal = 0.7071f;
            // 가장자리까지 거리와 가장 가까운 면(0 위·1 왼·2 아래·3 오른·4 좌상·5 우상·6 좌하·7 우하).
            float[] distances =
            {
                dt, dl, db, dr,
                (dl + dt - Chamfer) * diagonal, (dr + dt - Chamfer) * diagonal,
                (dl + db - Chamfer) * diagonal, (dr + db - Chamfer) * diagonal
            };
            int nearest = 0;
            for (int i = 1; i < distances.Length; i++)
            {
                if (distances[i] < distances[nearest]) nearest = i;
            }

            float edge = distances[nearest];
            if (edge < 1f) return new HologramPixel(72, 246, 240, 255);
            if (edge < 3.5f)
            {
                // 모따기 면 명암: 위·왼쪽은 밝고 아래·오른쪽은 어둡다. 우상·좌하 모서리는 중간.
                if (nearest == 0 || nearest == 1 || nearest == 4) return new HologramPixel(64, 198, 202, 248);
                if (nearest == 2 || nearest == 3 || nearest == 7) return new HologramPixel(14, 92, 108, 248);
                return new HologramPixel(38, 146, 156, 248);
            }

            if (edge < 4.5f) return new HologramPixel(4, 40, 50, 240);

            // 안쪽 판: 위가 약간 밝은 세로 그라데이션 + 가로 주사선(홀로그램 느낌).
            float t = InverseLerp(FaceY0, FaceY1, y);
            HologramPixel face = Lerp(new HologramPixel(8, 54, 64, 216), new HologramPixel(18, 92, 104, 216), t);
            if (y % 3 == 0) face = new HologramPixel(face.R, face.G, face.B, (byte)(face.A * 0.82f));

            int level = glyphLevel != null ? glyphLevel(x, y) : 0;
            switch (level)
            {
                case 2: return new HologramPixel(206, 255, 250, 255);
                case 3: return new HologramPixel(112, 216, 220, 255);
                case 1: return Over(face, new HologramPixel(40, 232, 226, 120));
                default: return face;
            }
        }

        private static float InverseLerp(float a, float b, float value)
        {
            if (a == b) return 0f;
            float t = (value - a) / (b - a);
            return t < 0f ? 0f : (t > 1f ? 1f : t);
        }

        private static byte LerpByte(byte a, byte b, float t)
        {
            return (byte)(int)Math.Round(a + (b - a) * t);
        }

        private static HologramPixel Lerp(HologramPixel a, HologramPixel b, float t)
        {
            return new HologramPixel(LerpByte(a.R, b.R, t), LerpByte(a.G, b.G, t), LerpByte(a.B, b.B, t), LerpByte(a.A, b.A, t));
        }

        /// <summary>straight alpha 두 색을 알파 가중으로 합성한다(색만 평균 내면 경계가 튄다).</summary>
        private static HologramPixel Over(HologramPixel dst, HologramPixel src)
        {
            float sa = src.A / 255f;
            float da = dst.A / 255f;
            float oa = sa + da * (1f - sa);
            if (oa <= 0f) return Clear;
            float r = (src.R * sa + dst.R * da * (1f - sa)) / oa;
            float g = (src.G * sa + dst.G * da * (1f - sa)) / oa;
            float b = (src.B * sa + dst.B * da * (1f - sa)) / oa;
            return new HologramPixel((byte)(int)Math.Round(r), (byte)(int)Math.Round(g), (byte)(int)Math.Round(b), (byte)(int)Math.Round(oa * 255f));
        }
    }
}
