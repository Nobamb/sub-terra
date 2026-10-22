"""B-155 심층 구역 업그레이드 창 썸네일 생성기.

Codex가 만든 원본 장면(source/codex-deep-zone-lv1~3.png, 3:1)을 480x160 썸네일로 줄이고,
같은 크기의 광물 발광 마스크(deep-zone-lvN-minerals.png)를 따로 뽑는다.
마스크는 썸네일과 픽셀 단위로 정렬되므로 Unity에서 겹쳐 놓고 알파만 바꾸면 광물이 빛난다.
확인용 대조표(preview-thumbnails-masks.png)는 out_dir가 아닌 이 도구 폴더의 상위에 쓴다.

사용: python build_deep_zone_thumbnails.py <source_dir> <out_dir>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

THUMB_W, THUMB_H = 480, 160
PREVIEW_SCALE = 2

# 레벨별 광물 판정 범위. hue는 도(0~360), s·v는 0~1. hue가 None이면 무채색(은빛 철) 판정.
# 각 범위: (hue_min, hue_max, s_min, s_max, v_min)
CYAN = (170.0, 225.0, 0.40, 1.01, 0.68)  # 바닥에 번진 청록 빛은 제외
LEVEL_RULES = {
    1: [CYAN],
    2: [CYAN,
        (0.0, 40.0, 0.50, 1.01, 0.45),       # 구리 (그늘진 쪽은 붉은 기가 돈다)
        (None, None, 0.00, 0.25, 0.50)],     # 철
    3: [(20.0, 60.0, 0.45, 1.01, 0.45),      # 금 (작은 알갱이는 주황 쪽으로 기운다)
        (270.0, 330.0, 0.35, 1.01, 0.40)],   # 자홍 결정
}

MIN_SPECK_PX = 3          # 썸네일 기준 이보다 작은 점은 버린다
CORE_COVERAGE = 0.30      # 원본 마스크를 줄였을 때 이 비율 이상 덮인 픽셀을 광물 핵심으로 본다
GLOW_GAIN = 1.35
GLOW_LIFT = 0.15


def fit_3x1(source):
    """원본이 3:1이 아니면 가운데를 3:1로 잘라낸다."""
    sw, sh = source.size
    target = THUMB_W / THUMB_H
    if abs(sw / sh - target) < 1e-3:
        return source
    if sw / sh > target:
        cw = int(round(sh * target))
        left = (sw - cw) // 2
        return source.crop((left, 0, left + cw, sh))
    ch = int(round(sw / target))
    top = (sh - ch) // 2
    return source.crop((0, top, sw, top + ch))


def hsv_arrays(image):
    hsv = np.asarray(image.convert("HSV")).astype(np.float32)
    return hsv[..., 0] * (360.0 / 255.0), hsv[..., 1] / 255.0, hsv[..., 2] / 255.0


def mineral_mask_source(source, rules):
    """원본 해상도에서 광물 픽셀을 참/거짓으로 고른다."""
    h, s, v = hsv_arrays(source)
    mask = np.zeros(h.shape, dtype=bool)
    for hue_min, hue_max, s_min, s_max, v_min in rules:
        hit = (s >= s_min) & (s < s_max) & (v >= v_min)
        if hue_min is not None:
            hit &= (h >= hue_min) & (h <= hue_max)
        mask |= hit
    # 원본 해상도의 외톨이 점을 먼저 지운다.
    return ndimage.binary_opening(mask, structure=np.ones((2, 2), bool))


def shrink_mask(mask_src):
    """원본 마스크를 썸네일 크기 덮임 비율(0~1)로 줄인다. 썸네일과 같은 크기 변환이라 정렬이 맞는다."""
    img = Image.fromarray((mask_src * 255).astype(np.uint8), "L")
    return np.asarray(img.resize((THUMB_W, THUMB_H), Image.BOX)).astype(np.float32) / 255.0


def clean_alpha(coverage):
    """점 제거 → 1px 팽창 → 반경 1 흐림. 핵심부는 완전 불투명으로 유지한다."""
    core = coverage >= CORE_COVERAGE
    labels, count = ndimage.label(core, structure=np.ones((3, 3), bool))
    if count:
        sizes = ndimage.sum(core, labels, index=np.arange(1, count + 1))
        keep = np.zeros(count + 1, bool)
        keep[1:] = sizes >= MIN_SPECK_PX
        core = keep[labels]
    grown = ndimage.binary_dilation(core, structure=np.ones((3, 3), bool))
    soft = Image.fromarray((grown * 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(1))
    alpha = np.asarray(soft).astype(np.float32)
    alpha[core] = 255.0
    return np.clip(alpha, 0, 255).astype(np.uint8), core


def glow_layer(thumb, alpha):
    rgb = np.asarray(thumb).astype(np.float32) / 255.0
    rgb = np.clip(rgb * GLOW_GAIN, 0.0, 1.0)
    rgb = rgb + (1.0 - rgb) * GLOW_LIFT
    rgba = np.dstack([np.clip(rgb * 255.0, 0, 255).astype(np.uint8), alpha])
    rgba[alpha == 0, :3] = 0
    return Image.fromarray(rgba, "RGBA")


def preview_row(thumb, layer):
    alpha = np.asarray(layer.getchannel("A")).astype(np.float32) / 255.0
    rgb = np.asarray(thumb).astype(np.float32)
    luma = (0.2126 * rgb[..., 0] + 0.7152 * rgb[..., 1] + 0.0722 * rgb[..., 2]) * 0.6
    base = np.dstack([luma] * 3)
    glow = np.asarray(layer.convert("RGB")).astype(np.float32)
    comp = base * (1.0 - alpha[..., None]) + glow * alpha[..., None]
    gray = Image.fromarray((alpha * 255).astype(np.uint8), "L").convert("RGB")
    tiles = [thumb, gray, Image.fromarray(np.clip(comp, 0, 255).astype(np.uint8), "RGB")]
    size = (THUMB_W * PREVIEW_SCALE, THUMB_H * PREVIEW_SCALE)
    return [t.resize(size, Image.NEAREST) for t in tiles]


def main(source_dir, out_dir):
    src = Path(source_dir)
    out = Path(out_dir)
    out.mkdir(parents=True, exist_ok=True)
    rows = []
    for level, rules in LEVEL_RULES.items():
        source = fit_3x1(Image.open(src / f"codex-deep-zone-lv{level}.png").convert("RGB"))
        thumb = source.resize((THUMB_W, THUMB_H), Image.LANCZOS)
        thumb.save(out / f"deep-zone-lv{level}.png")

        coverage = shrink_mask(mineral_mask_source(source, rules))
        alpha, core = clean_alpha(coverage)
        layer = glow_layer(thumb, alpha)
        layer.save(out / f"deep-zone-lv{level}-minerals.png")

        # 광물 무리 수: 가까운 조각(결정 가지 등)을 한 무리로 묶어서 센다.
        _, clusters = ndimage.label(ndimage.binary_dilation(alpha > 128, iterations=3))
        pct = (alpha > 128).mean() * 100.0
        print(f"lv{level}: coverage {pct:.2f}% clusters {clusters}")
        rows.append(preview_row(thumb, layer))

    tile_w, tile_h = rows[0][0].size
    gap = 8
    sheet = Image.new("RGB", (tile_w, (tile_h + gap) * 3 * len(rows) - gap), (40, 40, 40))
    y = 0
    for row in rows:
        for tile in row:
            sheet.paste(tile, (0, y))
            y += tile_h + gap
    preview = Path(__file__).resolve().parent.parent / "preview-thumbnails-masks.png"
    sheet.save(preview)
    print("preview", preview.name)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
