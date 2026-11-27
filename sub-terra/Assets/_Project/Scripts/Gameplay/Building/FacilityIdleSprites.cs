using System;
using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    /// <summary>
    /// B-214: 주기 연출·포탈 회전이 쓰는 코드로 그린 스프라이트. 한 번만 만들어 모든 시설이 공유하므로
    /// 시설이 늘어도 Texture·Sprite가 늘지 않는다. 흰색 알파 모양은 렌더러 색으로 물들인다.
    /// </summary>
    public static class FacilityIdleSprites
    {
        private static Sprite square, glow, ring, disc, plus, mask, bolt;
        private static Texture2D sheet;
        private static bool sheetLoaded;
        private static readonly Sprite[] sheetCells = new Sprite[16];
        private const string SheetResource = "Facilities/SettlementCoinSheet";   // Aseprite로 그린 128x32, 16x16 셀 8열 x 2행
        private static readonly Sprite[] arcs = new Sprite[3];

        // 충전기 번개·보건소 하트 차오름 시트와 보관함 영상 시트(배경 제거). 한 번 잘라 모든 시설이 공유한다.
        private const string ChargerIconResource = "Facilities/ChargerIdleIconSheet";
        private const string ClinicIconResource = "Facilities/ClinicIdleIconSheet";
        private const string StorageOpenResource = "Facilities/StorageOpenSheet";
        private static Texture2D chargerIconSheet, clinicIconSheet, storageOpenSheet;
        private static bool chargerIconLoaded, clinicIconLoaded, storageOpenLoaded;
        private static readonly Sprite[] chargerIconCells = new Sprite[FacilityIdleAnchors.IconFillFrames];
        private static readonly Sprite[] clinicIconCells = new Sprite[FacilityIdleAnchors.IconFillFrames];
        private static readonly Sprite[] storageOpenCells = new Sprite[FacilityIdleAnchors.StorageOpenFrames];
        private static Sprite storageOpenSource;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            square = glow = ring = disc = plus = mask = bolt = null;
            sheet = null;
            sheetLoaded = false;
            Array.Clear(sheetCells, 0, sheetCells.Length);
            Array.Clear(arcs, 0, arcs.Length);
            chargerIconSheet = clinicIconSheet = storageOpenSheet = null;
            chargerIconLoaded = clinicIconLoaded = storageOpenLoaded = false;
            Array.Clear(chargerIconCells, 0, chargerIconCells.Length);
            Array.Clear(clinicIconCells, 0, clinicIconCells.Length);
            Array.Clear(storageOpenCells, 0, storageOpenCells.Length);
            storageOpenSource = null;
        }

        /// <summary>지름 1 월드 단위 정사각형(흰색).</summary>
        public static Sprite Square => Get(ref square, "IdleFxSquare", 4, FilterMode.Point, (x, y, s) => White(1f));

        /// <summary>지름 1 월드 단위 부드러운 방사형 빛.</summary>
        public static Sprite Glow => Get(ref glow, "IdleFxGlow", 64, FilterMode.Bilinear, (x, y, s) =>
        {
            float r = Radius(x, y, s);
            return White((1f - r) * (1f - r));
        });

        /// <summary>지름 1 월드 단위 얇은 고리.</summary>
        public static Sprite Ring => Get(ref ring, "IdleFxRing", 128, FilterMode.Bilinear, (x, y, s) =>
            White(Mathf.Clamp01(1f - Mathf.Abs(Radius(x, y, s) - 0.9f) / 0.1f)));

        /// <summary>지름 1 월드 단위 속이 찬 원(와이파이 점).</summary>
        public static Sprite Disc => Get(ref disc, "IdleFxDisc", 32, FilterMode.Bilinear, (x, y, s) =>
            White(Mathf.Clamp01((1f - Radius(x, y, s)) * 6f)));

        /// <summary>포탈 홀 마스크용 속이 찬 원(지름 1 월드 단위).</summary>
        public static Sprite Mask => Get(ref mask, "IdleFxMask", 128, FilterMode.Bilinear, (x, y, s) =>
            White(Radius(x, y, s) < 0.985f ? 1f : 0f));

        /// <summary>와이파이 호(0=가장 안쪽 … 2=가장 바깥). 위쪽을 향한 약 90° 호이며 지름 1 월드 단위.</summary>
        public static Sprite Arc(int index)
        {
            index = Mathf.Clamp(index, 0, arcs.Length - 1);
            if (arcs[index] != null) return arcs[index];
            float radius = 0.26f + 0.24f * index;
            arcs[index] = CreateSprite("IdleFxArc" + index, 64, FilterMode.Bilinear, (x, y, s) =>
            {
                float half = s * 0.5f;
                float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;          // 위쪽 = 90
                float spread = Mathf.Clamp01(1f - Mathf.Abs(Mathf.DeltaAngle(angle, 90f)) / 48f);
                float band = Mathf.Clamp01(1f - Mathf.Abs(r - radius) / 0.075f);
                return White(Mathf.Min(1f, spread * 3f) * band);
            });
            return arcs[index];
        }

        /// <summary>보건소 회복 연출용 부드러운 십자(+). 지름 1 월드 단위, 흰색.</summary>
        public static Sprite Plus => Get(ref plus, "IdleFxPlus", 32, FilterMode.Bilinear, (x, y, s) =>
        {
            float half = s * 0.5f;
            float dx = Mathf.Abs((x + 0.5f - half) / half), dy = Mathf.Abs((y + 0.5f - half) / half);
            const float arm = 0.32f;
            float horizontal = Mathf.Min(Mathf.Clamp01((arm - dy) * 12f + 0.5f), Mathf.Clamp01((0.92f - dx) * 12f + 0.5f));
            float vertical = Mathf.Min(Mathf.Clamp01((arm - dx) * 12f + 0.5f), Mathf.Clamp01((0.92f - dy) * 12f + 0.5f));
            return White(Mathf.Max(horizontal, vertical));
        });

        /// <summary>충전기 정전기용 지그재그 번개(흰색, Point). 위에서 아래로 뻗는다. 가로 0.625·세로 1 월드 단위.</summary>
        public static Sprite Bolt => Get(ref bolt, "IdleFxBolt", 16, FilterMode.Point, (x, y, s) =>
        {
            // 위(y=15)에서 아래(y=0)로 내려가는 2칸 굵기 지그재그.
            int[] centers = { 9, 9, 10, 10, 9, 8, 8, 7, 7, 8, 9, 9, 8, 7, 7, 6 };
            int center = centers[15 - y];
            return White(x == center || x == center + 1 ? 1f : 0f);
        });

        /// <summary>정산기 도트 시트가 있는가(없으면 정산기 연출은 그 주기를 건너뛴다).</summary>
        public static bool HasSettlementSheet => LoadSheet() != null;

        /// <summary>원석 도트(0 구리, 1 철, 2 리튬). 16×16, 가로 1 월드 단위.</summary>
        public static Sprite Ore(int index) => Cell(Mathf.Clamp(index, 0, 2), 0);

        /// <summary>원석이 금화로 바뀔 때의 섬광 3프레임.</summary>
        public static Sprite Poof(int frame) => Cell(3 + Mathf.Clamp(frame, 0, 2), 0);

        /// <summary>금화 회전 프레임(0 정면, 1 기울어짐, 2 옆면, 3 반대로 기울어짐, 4 반짝임).</summary>
        public static Sprite CoinFrame(int frame) => Cell(Mathf.Clamp(frame, 0, 4), 1);

        /// <summary>충전 완료창 번개 차오름 프레임(0 빈 껍데기 … 24 원래 아이콘). 가로 1 월드 단위.</summary>
        public static Sprite ChargerIcon(int frame) => IconCell(ChargerIconResource, ref chargerIconSheet, ref chargerIconLoaded, chargerIconCells, frame);

        /// <summary>회복창 하트 차오름 프레임(0 빈 껍데기 … 24 원래 아이콘). 가로 1 월드 단위.</summary>
        public static Sprite ClinicIcon(int frame) => IconCell(ClinicIconResource, ref clinicIconSheet, ref clinicIconLoaded, clinicIconCells, frame);

        public static bool HasStorageOpenSheet => LoadOnce(StorageOpenResource, ref storageOpenSheet, ref storageOpenLoaded) != null;

        /// <summary>
        /// 보관함 영상 프레임. 시설 그림(artwork)과 같은 로컬 좌표에 놓이도록 피벗과 PPU를 맞춘다.
        /// 그림이 바뀌면 다시 자른다. 시트가 없으면 null.
        /// </summary>
        public static Sprite StorageOpenFrame(int frame, Sprite artwork)
        {
            if (artwork == null) return null;
            Texture2D texture = LoadOnce(StorageOpenResource, ref storageOpenSheet, ref storageOpenLoaded);
            if (texture == null) return null;
            if (storageOpenSource != artwork)
            {
                foreach (Sprite old in storageOpenCells) DestroySprite(old);
                Array.Clear(storageOpenCells, 0, storageOpenCells.Length);
                storageOpenSource = artwork;
            }
            frame = Mathf.Clamp(frame, 0, storageOpenCells.Length - 1);
            if (storageOpenCells[frame] != null) return storageOpenCells[frame];

            Bounds visible = FacilityGroundedVisual.GetVisibleBounds(artwork);
            Rect ratio = FacilityIdleAnchors.StorageOpenCellRatio;
            Rect local = Rect.MinMaxRect(
                visible.min.x + ratio.xMin * visible.size.x, visible.min.y + ratio.yMin * visible.size.y,
                visible.min.x + ratio.xMax * visible.size.x, visible.min.y + ratio.yMax * visible.size.y);
            int w = FacilityIdleAnchors.StorageOpenCellWidth, h = FacilityIdleAnchors.StorageOpenCellHeight;
            Rect rect = CellRect(texture, frame, FacilityIdleAnchors.StorageOpenColumns, w, h);
            if (rect.width <= 0f || local.width <= 0f || local.height <= 0f) return null;
            var pivot = new Vector2(-local.xMin / local.width, -local.yMin / local.height);
            Sprite sprite = Sprite.Create(texture, rect, pivot, w / local.width, 0, SpriteMeshType.FullRect);
            sprite.name = "StorageOpen_" + frame;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            storageOpenCells[frame] = sprite;
            return sprite;
        }

        private static Sprite IconCell(string resource, ref Texture2D texture, ref bool loaded, Sprite[] cells, int frame)
        {
            frame = Mathf.Clamp(frame, 0, cells.Length - 1);
            if (cells[frame] != null) return cells[frame];
            Texture2D sheetTexture = LoadOnce(resource, ref texture, ref loaded);
            if (sheetTexture == null) return null;
            int size = FacilityIdleAnchors.IconFillCell;
            Rect rect = CellRect(sheetTexture, frame, FacilityIdleAnchors.IconFillColumns, size, size);
            if (rect.width <= 0f) return null;
            Sprite sprite = Sprite.Create(sheetTexture, rect, new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
            sprite.name = sheetTexture.name + "_" + frame;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            cells[frame] = sprite;
            return sprite;
        }

        // 셀은 위쪽 줄부터 왼쪽→오른쪽 순서이고, 셀마다 둘레에 SheetPadding 픽셀 투명 여백이 있다(텍스처 좌표는 아래가 0).
        private static Rect CellRect(Texture2D texture, int index, int columns, int width, int height)
        {
            int pad = FacilityIdleAnchors.SheetPadding;
            int strideX = width + pad * 2, strideY = height + pad * 2;
            int column = index % columns, row = index / columns;
            var rect = new Rect(column * strideX + pad, texture.height - (row + 1) * strideY + pad, width, height);
            if (rect.xMax > texture.width || rect.yMin < 0f) return Rect.zero;
            return rect;
        }

        private static Texture2D LoadOnce(string resource, ref Texture2D texture, ref bool loaded)
        {
            if (!loaded)
            {
                texture = Resources.Load<Texture2D>(resource);
                loaded = true;
            }
            return texture;
        }

        private static void DestroySprite(Sprite sprite)
        {
            if (sprite == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(sprite); else UnityEngine.Object.DestroyImmediate(sprite);
        }

        private static Texture2D LoadSheet()
        {
            if (!sheetLoaded)
            {
                sheet = Resources.Load<Texture2D>(SheetResource);
                sheetLoaded = true;
            }
            return sheet;
        }

        // 시트의 위쪽 줄이 행 0이다(텍스처 좌표는 아래가 0).
        private static Sprite Cell(int column, int row)
        {
            int index = row * 8 + column;
            if (sheetCells[index] != null) return sheetCells[index];
            Texture2D texture = LoadSheet();
            if (texture == null) return null;
            var rect = new Rect(column * 16, texture.height - 16 * (row + 1), 16, 16);
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
            sprite.name = "SettlementSheet_" + row + "_" + column;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            sheetCells[index] = sprite;
            return sprite;
        }


        private static Sprite Get(ref Sprite slot, string name, int size, FilterMode filter,
            Func<int, int, int, Color> pixel, Func<Color[]> custom = null)
        {
            if (slot != null) return slot;
            slot = CreateSprite(name, size, filter, pixel, custom);
            return slot;
        }

        private static Color White(float alpha) => new(1f, 1f, 1f, alpha);

        private static float Radius(int x, int y, int size)
        {
            float half = size * 0.5f;
            float dx = (x + 0.5f - half) / half;
            float dy = (y + 0.5f - half) / half;
            return Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
        }

        private static Sprite CreateSprite(string name, int size, FilterMode filter,
            Func<int, int, int, Color> pixel, Func<Color[]> custom = null)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color[] pixels = custom != null ? custom() : new Color[size * size];
            if (custom == null)
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = pixel(x, y, size);
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
