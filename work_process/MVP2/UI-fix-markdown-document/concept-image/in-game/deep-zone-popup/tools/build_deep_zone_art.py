"""B-154-1 심층 구역 해금 창 그림 레이어 생성기.

Codex가 만든 원본 장면(source/codex-deep-zone-c.png)을 아래로 좁아지는 팔각 엠블럼으로 잘라내고,
광산 초기화 육각형(mine-reset-hex-*)처럼 같은 크기의 발광 마스크를 따로 뽑는다.
모든 레이어는 같은 캔버스 크기라 Unity에서 겹쳐 놓고 알파만 바꾸면 된다.

사용: python build_deep_zone_art.py <source.png> <out_dir>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

# 2배 해상도로 그린다. Unity에서는 절반 크기(688x428)로 표시한다.
SCALE = 2
BODY_W, BODY_H = 640 * SCALE, 380 * SCALE
PAD = 24 * SCALE
W, H = BODY_W + PAD * 2, BODY_H + PAD * 2

CYAN = (51, 255, 245)
CYAN_SOFT = (150, 245, 255)
NAVY = (6, 18, 28)


def polygon(inset=0.0):
    """아래로 좁아지는 팔각형. 위 모서리는 짧게, 아래 모서리는 깊게 깎아 수직갱 입구처럼 보이게 한다."""
    m = PAD + inset
    top_cut = 28 * SCALE
    bottom_cut_x = 86 * SCALE
    bottom_cut_y = 74 * SCALE
    x0, y0, x1, y1 = m, m, W - m, H - m
    return [
        (x0 + top_cut, y0), (x1 - top_cut, y0),
        (x1, y0 + top_cut), (x1, y1 - bottom_cut_y),
        (x1 - bottom_cut_x, y1), (x0 + bottom_cut_x, y1),
        (x0, y1 - bottom_cut_y), (x0, y0 + top_cut),
    ]


def mask_from_polygon(points, blur=0):
    mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(mask).polygon(points, fill=255)
    if blur:
        mask = mask.filter(ImageFilter.GaussianBlur(blur))
    return mask


def stroke(points, width, color, alpha=255):
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    closed = points + [points[0]]
    draw.line(closed, fill=color + (alpha,), width=width, joint="curve")
    return layer


def fit_scene(source):
    """원본을 몸통 비율로 자르고 크기를 맞춘다. 아래쪽 심연 빛이 가운데 아래에 오도록 세로 위치를 고정한다."""
    sw, sh = source.size
    target_ratio = BODY_W / BODY_H
    crop_h = int(round(sw / target_ratio))
    top = int((sh - crop_h) * 0.45)
    scene = source.crop((0, top, sw, top + crop_h)).resize((BODY_W, BODY_H), Image.LANCZOS)
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    canvas.paste(scene.convert("RGBA"), (PAD, PAD))
    return canvas


def glow_mask(scene_rgba, body_mask):
    """밝은 청록(리튬 결정·심연 빛)만 남긴 0~1 마스크."""
    rgb = np.asarray(scene_rgba.convert("RGB")).astype(np.float32) / 255.0
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b
    cyan = np.clip((g + b) * 0.5 - r, 0.0, 1.0)
    value = np.clip((luminance - 0.32) / 0.45, 0.0, 1.0) * np.clip(cyan / 0.28, 0.0, 1.0)
    value = value ** 1.15
    value *= np.asarray(body_mask).astype(np.float32) / 255.0
    return value


def to_layer(alpha, color, blur, gain=1.0):
    image = Image.fromarray(np.clip(alpha * 255.0 * gain, 0, 255).astype(np.uint8), "L")
    if blur:
        image = image.filter(ImageFilter.GaussianBlur(blur))
    layer = Image.new("RGBA", (W, H), color + (0,))
    layer.putalpha(image)
    return layer


def main(source_path, out_dir):
    out = Path(out_dir)
    out.mkdir(parents=True, exist_ok=True)
    scene = fit_scene(Image.open(source_path))
    body = polygon()
    body_mask = mask_from_polygon(body)

    # 1) 엠블럼: 장면 + 가장자리로 갈수록 어두워지는 안쪽 그림자.
    inner_shadow = mask_from_polygon(polygon(18 * SCALE), blur=22 * SCALE)
    shade = np.asarray(inner_shadow).astype(np.float32) / 255.0
    rgb = np.asarray(scene).astype(np.float32)
    darken = 0.45 + 0.55 * shade
    rgb[..., :3] *= darken[..., None]
    emblem = Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8), "RGBA")
    emblem.putalpha(body_mask)
    emblem.save(out / "deep-zone-emblem.png")

    # 2) 테두리: 어두운 바깥선 + 청록 본선 + 안쪽 밝은 실선, 위·아래 중앙에 깊이 눈금.
    border = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    border.alpha_composite(stroke(body, 12 * SCALE, NAVY, 230))
    border.alpha_composite(stroke(body, 5 * SCALE, CYAN))
    border.alpha_composite(stroke(polygon(5 * SCALE), 1 * SCALE, CYAN_SOFT, 170))
    draw = ImageDraw.Draw(border)
    cx = W // 2
    for y, direction in ((PAD, 1), (H - PAD, -1)):
        draw.polygon([(cx - 26 * SCALE, y), (cx + 26 * SCALE, y), (cx, y + direction * 10 * SCALE)],
                     fill=CYAN + (255,))
    for i, offset in enumerate((-1, 1)):
        x = cx + offset * 60 * SCALE
        draw.line([(x - 14 * SCALE, PAD), (x + 14 * SCALE, PAD)], fill=(255, 255, 255, 220), width=2 * SCALE)
    border.save(out / "deep-zone-border.png")

    # 3) 테두리 발광: 본선을 넓게 흐린 빛.
    halo = stroke(body, 14 * SCALE, CYAN_SOFT)
    halo = halo.filter(ImageFilter.GaussianBlur(10 * SCALE))
    halo_alpha = np.asarray(halo.getchannel("A")).astype(np.float32)
    halo.putalpha(Image.fromarray(np.clip(halo_alpha * 1.8, 0, 255).astype(np.uint8), "L"))
    halo.save(out / "deep-zone-border-glow.png")

    # 4) 탐지 파동 테두리: 가는 선 + 약한 번짐.
    ring = stroke(body, 3 * SCALE, CYAN_SOFT)
    ring_soft = stroke(body, 9 * SCALE, CYAN).filter(ImageFilter.GaussianBlur(5 * SCALE))
    ring_soft.alpha_composite(ring)
    ring_soft.save(out / "deep-zone-ring.png")

    # 5) 발광 마스크: 왼쪽·오른쪽 결정 무리와 가운데 아래 심연 빛을 따로 호흡시키기 위해 나눈다.
    glow = glow_mask(scene, body_mask)
    ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
    nx = (xs - PAD) / BODY_W
    ny = (ys - PAD) / BODY_H

    # 심연 중심: 아래쪽 가운데 영역에서 가장 밝은 곳.
    region = glow.copy()
    region[(nx < 0.3) | (nx > 0.7) | (ny < 0.45)] = 0.0
    blurred = np.asarray(Image.fromarray((region * 255).astype(np.uint8)).filter(
        ImageFilter.GaussianBlur(30 * SCALE))).astype(np.float32)
    core_y, core_x = np.unravel_index(np.argmax(blurred), blurred.shape)
    dist = np.sqrt(((xs - core_x) / (BODY_W * 0.2)) ** 2 + ((ys - core_y) / (BODY_H * 0.32)) ** 2)
    core_weight = np.clip(1.0 - dist, 0.0, 1.0) ** 1.5

    side = np.clip((nx - 0.5) / 0.08, -1.0, 1.0)
    left_weight = np.clip(-side, 0.0, 1.0) * (1.0 - core_weight)
    right_weight = np.clip(side, 0.0, 1.0) * (1.0 - core_weight)

    to_layer(glow * left_weight, CYAN_SOFT, 3 * SCALE, 1.6).save(out / "deep-zone-glow-crystals-left.png")
    to_layer(glow * right_weight, CYAN_SOFT, 3 * SCALE, 1.6).save(out / "deep-zone-glow-crystals-right.png")

    # 심연 빛은 결정 마스크에 둥근 빛을 더해 아래에서 솟는 느낌을 준다.
    body_alpha = np.asarray(body_mask).astype(np.float32) / 255.0
    core = np.maximum(glow * core_weight * 1.4, core_weight ** 2 * 0.55) * body_alpha
    to_layer(core, CYAN_SOFT, 6 * SCALE, 1.0).save(out / "deep-zone-glow-core.png")

    print("canvas", W, H, "core", int(core_x), int(core_y))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
