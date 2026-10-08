namespace SubTerra.App.UI.HUD
{
    public static class SevenSegmentGlyph
    {
        // 비트 순서: 위, 오른쪽 위, 오른쪽 아래, 아래, 왼쪽 아래, 왼쪽 위, 가운데.
        private static readonly byte[] Masks = { 0x3f, 0x06, 0x5b, 0x4f, 0x66, 0x6d, 0x7d, 0x07, 0x7f, 0x6f };

        public static byte GetMask(int digit)
        {
            return digit >= 0 && digit <= 9 ? Masks[digit] : (byte)0;
        }
    }
}
