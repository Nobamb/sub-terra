# prompt-B 123-2: '새 광산 구역' 팝업 스프라이트 생성 스크립트.
# 컨셉 이미지(init-mine-popup-concept.png)에서 프레임·광산 배경·광산 입구·패널·버튼·아이콘을 잘라
# 글자를 지우고 알파를 분리한다. 발광·육각 테두리·링·스캔 라인은 코드로 그린다.
# 이미지 생성 AI는 쓰지 않는다. 저장소 루트에서 `python work_process/.../make_init_mine_popup_art.py` 로 실행.
import json
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(HERE, "..", "..", "..", ".."))
CONCEPT = os.path.join(
    REPO, "work_process", "MVP2", "UI-fix-markdown-document", "concept-image", "in-game", "init-mine",
    "init-mine-popup-concept.png")
OUT = os.environ.get("MINE_RESET_ART_OUT") or os.path.join(
    REPO, "sub-terra", "Assets", "_Project", "Art", "UI", "SurfaceBase", "MineReset")

# 컨셉 1px = 게임 기준 해상도(1920x1080) 1.148 단위. 스프라이트는 2배로 저장해 4K에서도 덜 흐리게 한다.
UPSCALE = 2
GAME_SCALE = 1920.0 / 1672.0
# 팝업(프레임 외곽) 중심. 게임 좌표 원점이 된다.
CX, CY = 836.0, 467.5
FRAME_RECT = (256, 23, 1416, 912)
FRAME_OUTLINE = (259.5, 27.5, 1410.5, 906.5)
# 내부 기본 금속색(컨셉 빈 영역 평균)
BASE = np.array([0.5, 11.0, 17.0], np.float32)

HEX_CENTER = (836.0, 350.0)
HEX_POINTS = [(735.0, 212.5), (937.0, 212.5), (1018.5, 350.0), (937.0, 487.5), (735.0, 487.5), (653.5, 350.0)]

src = np.asarray(Image.open(CONCEPT).convert("RGB")).astype(np.float32)
H, W = src.shape[:2]
meta = {}


def cy_metric(rgb):
    return (rgb[..., 1] + rgb[..., 2]) / 2.0 - rgb[..., 0]


def gauss(a, s):
    return ndimage.gaussian_filter(a, s)


def crop(rect):
    x0, y0, x1, y1 = rect
    return src[y0:y1, x0:x1].copy()


def fill(rgb, mask, sigmas=(2, 4, 8, 16, 32, 64)):
    """mask 영역을 주변 색으로 점진적으로 채운다(정규화 합성곱)."""
    out = rgb.copy()
    known = (~mask).astype(np.float32)
    todo = mask.copy()
    for s in sigmas:
        den = gauss(known, s)
        est = np.stack([gauss(out[..., c] * known, s) for c in range(3)], -1) / np.maximum(den, 1e-4)[..., None]
        ok = todo & (den > 0.02)
        out[ok] = est[ok]
        known[ok] = 1.0
        todo &= ~ok
    if todo.any():
        out[todo] = rgb[~mask].mean(0)
    return out


def row_fill(rgb, box, seed):
    """box(로컬 좌표) 안을 줄마다 좌우 가장자리 색으로 선형 보간하고, 주변과 같은 세기의 잔결을 얹는다.
    가로 결무늬·세로 그라데이션이 있는 판과 버튼 면의 글자 지우기에 쓴다."""
    x0, y0, x1, y1 = box
    out = rgb.copy()
    left = rgb[y0:y1, max(0, x0 - 10):x0 - 2].mean(1)
    right = rgb[y0:y1, x1 + 2:x1 + 10].mean(1)
    t = np.linspace(0, 1, x1 - x0, dtype=np.float32)[None, :, None]
    body = left[:, None, :] * (1 - t) + right[:, None, :] * t
    body = ndimage.gaussian_filter1d(body, 1.2, axis=0)
    ref = np.concatenate([rgb[y0:y1, max(0, x0 - 40):x0 - 2], rgb[y0:y1, x1 + 2:x1 + 40]], 1)
    hp = ref - np.stack([gauss(ref[..., c], 2) for c in range(3)], -1)
    detail = min(2.0, float(np.median(np.abs(hp))) * 1.4)
    noise = gauss(np.random.default_rng(seed).normal(0, 1, body.shape[:2]).astype(np.float32), 0.6) * 1.8
    body += noise[..., None] * detail
    out[y0:y1, x0:x1] = body
    return out


def grain(shape, seed, amount):
    rng = np.random.default_rng(seed)
    n = rng.normal(0.0, 1.0, shape[:2]).astype(np.float32)
    return n[..., None] * amount


def unblend(rgb, bg, floor=0.03):
    """bg 위에 합성하면 rgb가 되는 (색, 알파)를 구한다."""
    bg = np.asarray(bg, np.float32)
    up = np.clip((rgb - bg) / np.maximum(255.0 - bg, 1.0), 0, 1)
    down = np.clip((bg - rgb) / np.maximum(bg, 1.0), 0, 1)
    alpha = np.max(np.maximum(up, down), axis=-1)
    alpha = np.clip((alpha - floor) / (1 - floor), 0, 1)
    color = bg + (rgb - bg) / np.maximum(alpha, 1e-3)[..., None]
    return np.clip(color, 0, 255), alpha


def poly_mask(shape, points, offset, scale=4):
    """다각형 마스크(안티앨리어싱). points는 컨셉 좌표, offset은 잘라낸 사각형의 좌상단."""
    h, w = shape
    img = Image.new("L", (w * scale, h * scale), 0)
    pts = [((x - offset[0]) * scale, (y - offset[1]) * scale) for x, y in points]
    ImageDraw.Draw(img).polygon(pts, fill=255)
    img = img.resize((w, h), Image.LANCZOS)
    return np.asarray(img).astype(np.float32) / 255.0


def chamfer(rect, c):
    x0, y0, x1, y1 = rect
    return [(x0 + c, y0), (x1 - c, y0), (x1, y0 + c), (x1, y1 - c), (x1 - c, y1), (x0 + c, y1), (x0, y1 - c), (x0, y0 + c)]


def save(name, rgb, alpha, rect, upscale=UPSCALE, sharpen=True):
    """rect(컨셉 좌표)를 게임 로컬 좌표로 바꿔 메타에 남기고 PNG로 저장한다."""
    rgba = np.dstack([np.clip(rgb, 0, 255), np.clip(alpha, 0, 1) * 255]).astype(np.uint8)
    img = Image.fromarray(rgba, "RGBA")
    if upscale != 1:
        # 알파 가장자리 색 번짐을 막기 위해 premultiplied 상태로 키운다.
        pm = rgba.astype(np.float32)
        pm[..., :3] *= pm[..., 3:4] / 255.0
        big = Image.fromarray(np.clip(pm, 0, 255).astype(np.uint8), "RGBA").resize(
            (img.width * upscale, img.height * upscale), Image.LANCZOS)
        if sharpen:
            big = big.filter(ImageFilter.UnsharpMask(radius=1.6, percent=60, threshold=2))
        b = np.asarray(big).astype(np.float32)
        a = b[..., 3:4] / 255.0
        b[..., :3] = np.where(a > 1e-3, b[..., :3] / np.maximum(a, 1e-3), 0)
        img = Image.fromarray(np.clip(b, 0, 255).astype(np.uint8), "RGBA")
    os.makedirs(OUT, exist_ok=True)
    img.save(os.path.join(OUT, name + ".png"))
    x0, y0, x1, y1 = rect
    meta[name] = {
        "x": round(((x0 + x1) / 2.0 - CX) * GAME_SCALE, 1),
        "y": round((CY - (y0 + y1) / 2.0) * GAME_SCALE, 1),
        "w": round((x1 - x0) * GAME_SCALE, 1),
        "h": round((y1 - y0) * GAME_SCALE, 1),
        "px": [img.width, img.height],
    }


def to_rgba_procedural(color, alpha):
    h, w = alpha.shape
    rgb = np.broadcast_to(np.asarray(color, np.float32), (h, w, 3)).copy()
    return rgb, alpha


# ------------------------------------------------------------------ 외곽 프레임 + 금속 내부 패널
def make_frame():
    rect = FRAME_RECT
    rgb = crop(rect)
    cy = cy_metric(rgb)
    # 바깥 희미한 외곽선을 따라 잰 깎인 사각형(모서리 48px). 그 바깥은 광산 배경이므로 버린다.
    cover = poly_mask(rgb.shape[:2], chamfer(FRAME_OUTLINE, 48), rect[:2])
    outside = cover < 0.5
    d_out = ndimage.distance_transform_edt(~outside)
    solid = (~outside) & (d_out <= 21.5)

    # 모서리 금속 브래킷: 띠와 이어진 밝은 덩어리. 버튼 영역은 제외한다.
    lum = rgb.mean(-1)
    bright = (lum > 50) | (cy > 70)
    zone = np.zeros_like(bright)
    h, w = bright.shape
    zone[:190, :190] = zone[:190, -190:] = zone[-200:, :170] = zone[-200:, -170:] = True
    for bx0, by0, bx1, by1 in [(372, 782, 820, 872), (845, 782, 1306, 872)]:
        zone[by0 - rect[1]:by1 - rect[1], bx0 - rect[0]:bx1 - rect[0]] = False
    lab, _ = ndimage.label(bright & zone)
    touching = set(np.unique(lab[solid & (lab > 0)])) - {0}
    bracket = ndimage.binary_fill_holes(np.isin(lab, list(touching)))
    bracket = ndimage.binary_dilation(bracket, iterations=1) & ~outside

    # 안쪽 발광: 띠 안쪽 56px까지 기본 금속색 기준으로 분리. 광산 배경 줄은 배경 그림에 이미 들어 있다.
    glow_zone = (~outside) & (d_out > 21.5) & (d_out <= 56) & ~bracket
    yy = np.arange(h)[:, None] + rect[1]
    xx = np.arange(w)[None, :] + rect[0]
    # 제목 글자와 하단 버튼 테두리는 프레임 그림에 넣지 않는다.
    for bx0, by0, bx1, by1 in [(560, 58, 1112, 138), (370, 780, 1310, 876)]:
        glow_zone &= ~((xx >= bx0) & (xx < bx1) & (yy >= by0) & (yy < by1))
    falloff = np.clip((56 - d_out) / 14.0, 0, 1)
    gcol, galpha = unblend(rgb, BASE, floor=0.02)
    # 광산 배경이 덮는 줄만큼 빼서 배경과 겹쳐 두 번 밝아지지 않게 한다.
    cave_cover = cave_rows(yy)
    galpha = galpha * falloff * glow_zone * (1 - cave_cover)

    hard = np.clip(gauss((solid | bracket).astype(np.float32), 0.6) * 1.15, 0, 1) * (~outside)
    alpha = np.maximum(hard, galpha)
    color = np.where((solid | bracket)[..., None], rgb, gcol)
    alpha *= cover
    save("mine-reset-frame", color, alpha, rect)

    # 등장 순간 밝게 번쩍일 띠 발광(테두리만)
    band = solid & (cy > 150)
    glow = np.clip(gauss(band.astype(np.float32), 2.5) * 2.2 + gauss(band.astype(np.float32), 9) * 1.4, 0, 1)
    save("mine-reset-frame-glow", *to_rgba_procedural((150, 250, 255), glow), rect)

    # 금속 내부 패널(프레임 아래에 깔림). 세로 결과 잔결만 넣어 컨셉의 어두운 청록 금속면을 만든다.
    inside = (~outside) & (d_out >= 8)
    rng = np.random.default_rng(1232)
    streak = gauss(rng.normal(0, 1, (1, w)).astype(np.float32), (0, 1.2))
    streak = np.repeat(streak, h, 0) * 1.6
    fine = gauss(rng.normal(0, 1, (h, w)).astype(np.float32), 0.6) * 1.1
    yy01 = np.arange(h)[:, None] / h
    xx01 = np.arange(w)[None, :] / w
    lift = 1.0 + 0.18 * np.exp(-(((xx01 - 0.5) / 0.35) ** 2 + ((yy01 - 0.2) / 0.3) ** 2))
    panel = BASE[None, None, :] * lift[..., None] + (streak + fine)[..., None] * np.array([0.25, 0.8, 1.0])
    panel_alpha = np.clip(gauss(inside.astype(np.float32), 0.8) * 1.4, 0, 1) * cover
    save("mine-reset-panel", panel, panel_alpha, rect, upscale=1)
    return outside


# ------------------------------------------------------------------ 광산 배경(육각형·링 제거)
def cave_rows(y):
    """컨셉 y 좌표별 광산 배경의 불투명도(위는 부드럽게, 아래는 비용 패널 직전에 끝난다)."""
    top = np.clip((y - 188) / 57.0, 0, 1)
    bottom = np.clip((474 - y) / 12.0, 0, 1)
    return (top * top * (3 - 2 * top)) * bottom


def make_cave():
    rect = (282, 188, 1390, 482)
    rgb = crop(rect)
    h, w = rgb.shape[:2]
    x0, y0 = rect[0], rect[1]
    hexm = poly_mask((h, w), HEX_POINTS, (x0, y0)) > 0.02
    hexm = ndimage.binary_dilation(hexm, iterations=16)
    yy, xx = np.mgrid[0:h, 0:w]
    dx = xx + x0 - HEX_CENTER[0]
    dy = yy + y0 - HEX_CENTER[1]
    r = np.hypot(dx, dy)
    cy = cy_metric(rgb)
    tophat = cy - gauss(cy, 5)
    rings = (r > 150) & (r < 285) & (tophat > 10)
    ticks = (np.abs(dy) < 5) & (np.abs(dx) < 300) & (tophat > 6)
    # 윗변 가운데 섬광이 위로 번진 부분
    flare = (np.abs(dx) < 60) & (yy + y0 < 215)
    lines = ndimage.binary_dilation(rings | ticks, iterations=2)
    mask = hexm | lines | flare
    clean = fill(rgb, mask)
    # 채운 영역은 크게 흐려 줄무늬를 없애고, 잔결만 다시 얹는다.
    soft = np.stack([gauss(clean[..., c], 14) for c in range(3)], -1)
    blend = np.clip(gauss(mask.astype(np.float32), 6) * 1.4, 0, 1)[..., None]
    clean = clean * (1 - blend) + soft * blend
    clean += grain(rgb.shape, 77, 1.2) * blend
    # 중심(육각형 뒤)은 어둡게 눌러 등장 연출의 작은 빛이 잘 보이게 한다.
    center_dark = np.exp(-((dx / 190.0) ** 2 + (dy / 150.0) ** 2))[..., None] * 0.55
    clean = clean * (1 - center_dark) + BASE * center_dark
    alpha = np.broadcast_to(cave_rows(yy + y0), (h, w))
    save("mine-reset-cave", clean, alpha, rect)
    return clean, rect


def crystal_mask(rgb):
    b = rgb[..., 2]
    r = rgb[..., 0]
    return np.clip((b - 95) / 80.0, 0, 1) * np.clip((b - r - 45) / 60.0, 0, 1)


def make_cave_glows(clean, rect):
    """광산 배경의 청록 광물만 따로 떼어 덩어리별 발광 레이어로 만든다(호흡 시점을 다르게 주기 위해)."""
    m = crystal_mask(clean)
    # 아래 비용 패널 테두리와 좌우 프레임 안쪽 발광은 광물이 아니다.
    yy, xx = np.mgrid[0:m.shape[0], 0:m.shape[1]]
    m = m * ((yy + rect[1]) < 462) * ((xx + rect[0]) > 300) * ((xx + rect[0]) < 1372)
    soft = gauss(m, 3.0)
    lab, n = ndimage.label(soft > 0.06)
    comps = []
    for i in range(1, n + 1):
        ys, xs = np.where(lab == i)
        weight = m[lab == i].sum()
        if weight < 25:
            continue
        comps.append((weight, xs.min(), ys.min(), xs.max(), ys.max()))
    comps.sort(reverse=True)
    out = []
    for k, (_, cx0, cy0, cx1, cy1) in enumerate(comps[:6]):
        pad = 22
        gx0 = max(0, cx0 - pad); gy0 = max(0, cy0 - pad)
        gx1 = min(clean.shape[1], cx1 + pad); gy1 = min(clean.shape[0], cy1 + pad)
        local = m[gy0:gy1, gx0:gx1]
        glow = np.clip(local * 0.9 + gauss(local, 2.5) * 1.3 + gauss(local, 9) * 1.6, 0, 1)
        name = "mine-reset-cave-glow-%d" % k
        rr = (rect[0] + gx0, rect[1] + gy0, rect[0] + gx1, rect[1] + gy1)
        save(name, *to_rgba_procedural((140, 235, 255), glow), rr, sharpen=False)
        out.append(name)
    return out


# ------------------------------------------------------------------ 중앙 육각 광산 입구 + 발광
def make_hex():
    rect = (650, 209, 1022, 491)
    rgb = crop(rect)
    h, w = rgb.shape[:2]
    # 비용 패널에 가려지는 아래 줄(474~)은 바로 위 줄을 늘려 채운다.
    cut = 474 - rect[1]
    rgb[cut:] = rgb[cut - 1:cut]
    alpha = poly_mask((h, w), HEX_POINTS, rect[:2])
    alpha = np.clip(gauss(alpha, 0.5), 0, 1)
    save("mine-reset-hex-mine", rgb, alpha, rect)

    m = crystal_mask(rgb) * alpha
    yy, xx = np.mgrid[0:h, 0:w]
    gx = xx + rect[0]
    gy = yy + rect[1]
    side = (np.abs(gx - HEX_CENTER[0]) > 70) & (gy > 320)
    crystals = m * side
    glow = np.clip(crystals * 0.8 + gauss(crystals, 2.5) * 1.2 + gauss(crystals, 8) * 1.4, 0, 1) * alpha
    save("mine-reset-hex-glow-crystals", *to_rgba_procedural((150, 240, 255), glow), rect, sharpen=False)

    lumc = np.clip((rgb.mean(-1) - 120) / 90.0, 0, 1) * alpha
    centre = (np.abs(gx - HEX_CENTER[0]) < 60) & (gy > 240)
    tunnel = lumc * centre
    tglow = np.clip(gauss(tunnel, 3) * 1.4 + gauss(tunnel, 12) * 2.0, 0, 1) * alpha
    save("mine-reset-hex-glow-tunnel", *to_rgba_procedural((170, 245, 255), tglow), rect, sharpen=False)


def draw_lines(size, scale, segments, width):
    img = Image.new("L", (size[0] * scale, size[1] * scale), 0)
    d = ImageDraw.Draw(img)
    for (a, b) in segments:
        d.line([(a[0] * scale, a[1] * scale), (b[0] * scale, b[1] * scale)], fill=255, width=max(1, int(width * scale)))
    return img


def hex_poly(offset, inset=0.0):
    cx, cy = HEX_CENTER
    pts = []
    for x, y in HEX_POINTS:
        vx, vy = x - cx, y - cy
        n = math.hypot(vx, vy)
        k = (n - inset) / n
        pts.append((cx + vx * k - offset[0], cy + vy * k - offset[1]))
    return pts


def make_hex_border():
    pad = 60
    rect = (int(653.5 - pad), int(212.5 - pad), int(1018.5 + pad), int(487.5 + pad))
    S = 8  # 내부 슈퍼샘플(컨셉 1px당)
    size = (rect[2] - rect[0], rect[3] - rect[1])
    out = (size[0] * UPSCALE, size[1] * UPSCALE)

    def stroke(inset, width):
        pts = hex_poly(rect[:2], inset)
        segs = [(pts[i], pts[(i + 1) % 6]) for i in range(6)]
        img = draw_lines(size, S, segs, width)
        return np.asarray(img.resize(out, Image.LANCZOS)).astype(np.float32) / 255.0

    k = UPSCALE
    core = stroke(0, 2.4)
    inner = stroke(7.0, 1.1) * 0.45
    glow_a = gauss(stroke(0, 4.0), 3.5 * k) * 1.5
    glow_b = gauss(stroke(0, 6.0), 11 * k) * 1.1
    # 꼭짓점·윗변 가운데의 작은 섬광과 좌우 수평 눈금
    yy, xx = np.mgrid[0:out[1], 0:out[0]].astype(np.float32)
    flare = np.zeros_like(core)
    spots = [((836.0, 212.5), 1.0), ((653.5, 350.0), 0.7), ((1018.5, 350.0), 0.7)]
    for (sx, sy), power in spots:
        px = (sx - rect[0]) * k
        py = (sy - rect[1]) * k
        d2 = ((xx - px) / k) ** 2 + ((yy - py) / k) ** 2
        flare += power * (np.exp(-d2 / 10.0) + 0.35 * np.exp(-(((xx - px) / k / 26.0) ** 2 + ((yy - py) / k / 1.2) ** 2)))
    ticks = Image.new("L", out, 0)
    d = ImageDraw.Draw(ticks)
    for sx, dirx in [(653.5, -1), (1018.5, 1)]:
        px = (sx - rect[0]) * k
        py = (350.0 - rect[1]) * k
        d.line([(px + dirx * 10 * k, py), (px + dirx * 42 * k, py)], fill=200, width=max(1, int(1.2 * k)))
    tick = np.asarray(ticks).astype(np.float32) / 255.0
    tick = np.clip(tick + gauss(tick, 2 * k) * 1.2, 0, 1)

    alpha = np.clip(core + inner + glow_a * 0.85 + glow_b * 0.6 + flare + tick * 0.8, 0, 1)
    whiteness = np.clip(core + flare * 0.8, 0, 1)
    color = np.stack([
        40 + 175 * whiteness,
        215 + 40 * whiteness,
        np.full_like(whiteness, 255.0)], -1)
    save("mine-reset-hex-border", color, alpha, rect, upscale=1, sharpen=False)

    # 호흡·완성 섬광용 넓은 발광(선 없음)
    wide = np.clip(gauss(stroke(0, 5.0), 6 * k) * 1.8 + gauss(stroke(0, 8.0), 18 * k) * 1.3 + flare * 0.8, 0, 1)
    save("mine-reset-hex-border-glow", *to_rgba_procedural((120, 235, 255), wide), rect, upscale=1, sharpen=False)


def make_hex_rings():
    """육각형 주변 HUD 동심 호. 위쪽 설명 글자와 아래 비용 패널 구간은 그리지 않는다."""
    rect = (556, 196, 1116, 476)
    size = (rect[2] - rect[0], rect[3] - rect[1])
    k = 4
    img = Image.new("L", (size[0] * k, size[1] * k), 0)
    d = ImageDraw.Draw(img)
    cx = (HEX_CENTER[0] - rect[0]) * k
    cy = (HEX_CENTER[1] - rect[1]) * k
    arcs = [
        (200, 34, 1.3, 150), (218, 26, 1.0, 110), (238, 40, 1.6, 120), (258, 20, 1.0, 80)]
    for radius, span, width, value in arcs:
        rr = radius * k
        box = [cx - rr, cy - rr, cx + rr, cy + rr]
        for centre in (0, 180):
            d.arc(box, centre - span, centre + span, fill=value, width=max(1, int(width * k)))
        # 끊어진 짧은 조각
        for centre in (0, 180):
            for off in (span + 6, -(span + 6)):
                d.arc(box, centre + off - 3, centre + off + 3, fill=int(value * 0.6), width=max(1, int(width * k)))
    for dirx in (-1, 1):
        x_from = cx + dirx * 190 * k
        x_to = cx + dirx * 266 * k
        d.line([(x_from, cy), (x_to, cy)], fill=110, width=max(1, int(1.0 * k)))
    arr = np.asarray(img.resize((size[0] * UPSCALE, size[1] * UPSCALE), Image.LANCZOS)).astype(np.float32) / 255.0
    arr = np.clip(arr + gauss(arr, 2.0 * UPSCALE) * 0.8, 0, 1)
    save("mine-reset-hex-rings", *to_rgba_procedural((70, 210, 240), arr), rect, upscale=1, sharpen=False)


def make_core_glow():
    w, h = 256, 128
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    dx = (xx - w / 2 + 0.5) / (w / 2)
    dy = (yy - h / 2 + 0.5) / (h / 2)
    blob = np.exp(-(dx ** 2 + dy ** 2) * 9.0)
    streak = np.exp(-(dx ** 2) * 2.2 - (dy ** 2) * 160.0)
    a = np.clip(blob + streak * 0.8, 0, 1)
    white = np.clip(np.exp(-(dx ** 2 + dy ** 2) * 40.0) + streak * 0.5, 0, 1)
    color = np.stack([60 + 195 * white, 220 + 35 * white, np.full_like(white, 255.0)], -1)
    save("mine-reset-core-glow", color, a, (0, 0, w, h), upscale=1, sharpen=False)
    meta.pop("mine-reset-core-glow")


def make_scanline():
    w, h = 1024, 48
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    dy = (yy - h / 2 + 0.5)
    t = xx / (w - 1)
    ends = np.clip(np.minimum(t, 1 - t) / 0.08, 0, 1)
    ends = ends * ends * (3 - 2 * ends)
    core = np.exp(-(dy ** 2) / 1.6)
    glow = np.exp(-(dy ** 2) / 60.0)
    a = np.clip(core + glow * 0.65, 0, 1) * ends
    white = core
    color = np.stack([70 + 185 * white, 225 + 30 * white, np.full_like(white, 255.0)], -1)
    save("mine-reset-scanline", color, a, (0, 0, w, h), upscale=1, sharpen=False)
    meta.pop("mine-reset-scanline")


# ------------------------------------------------------------------ 제목 장식선, 패널, 아이콘, 버튼
def make_divider():
    rect = (470, 134, 1202, 154)
    rgb = crop(rect)
    color, alpha = unblend(rgb, BASE, floor=0.05)
    save("mine-reset-title-divider", color, alpha, rect)


def plate(name, rect, chamfer_px, content_rects, glow_px=6, seed=0):
    """패널 판: 내부 글자·아이콘을 지우고, 깎인 모서리 다각형 바깥은 발광만 남긴다."""
    x0, y0, x1, y1 = rect
    outer = (x0 - glow_px, y0 - glow_px, x1 + glow_px + 1, y1 + glow_px + 1)
    rgb = crop(outer)
    h, w = rgb.shape[:2]
    clean = rgb
    for i, (cx0, cy0, cx1, cy1) in enumerate(content_rects):
        clean = row_fill(clean, (cx0 - outer[0], cy0 - outer[1], cx1 - outer[0], cy1 - outer[1]), 100 + seed * 10 + i)
    inside = poly_mask((h, w), chamfer((x0, y0, x1 + 1, y1 + 1), chamfer_px), outer[:2])
    gcol, galpha = unblend(clean, BASE, floor=0.04)
    color = np.where(inside[..., None] > 0.5, clean, gcol)
    alpha = np.maximum(inside, galpha * (1 - inside))
    save(name, color, alpha, outer)


def icon(name, rect, bg=None, floor=0.05):
    rgb = crop(rect)
    if bg is None:
        ring = np.concatenate([rgb[0], rgb[-1], rgb[:, 0], rgb[:, -1]])
        bg = np.median(ring, axis=0)
    color, alpha = unblend(rgb, bg, floor=floor)
    save(name, color, alpha, rect)


def button(name, rect, chamfer_px, text_rect, glow_px=8, seed=0):
    x0, y0, x1, y1 = rect
    outer = (x0 - glow_px, y0 - glow_px, x1 + glow_px + 1, y1 + glow_px + 1)
    rgb = crop(outer)
    h, w = rgb.shape[:2]
    tx0, ty0, tx1, ty1 = text_rect
    clean = row_fill(rgb, (tx0 - outer[0], ty0 - outer[1], tx1 - outer[0], ty1 - outer[1]), 200 + seed)
    inside = poly_mask((h, w), chamfer((x0, y0, x1 + 1, y1 + 1), chamfer_px), outer[:2])
    # 프레임 브래킷 끝이 바깥 여백에 들어오지 않게 버튼 외곽 근처만 발광으로 남긴다.
    gcol, galpha = unblend(clean, BASE, floor=0.05)
    dist = ndimage.distance_transform_edt(inside < 0.5)
    galpha *= np.clip((glow_px - dist) / 4.0, 0, 1)
    color = np.where(inside[..., None] > 0.5, clean, gcol)
    alpha = np.maximum(inside, galpha * (1 - inside))
    save(name, color, alpha, outer)
    return color, alpha, outer


def hover_variant(name, color, alpha, outer, gain, tint):
    c = np.clip(color * gain + np.asarray(tint, np.float32), 0, 255)
    save(name, c, alpha, outer)


def main():
    outside = make_frame()
    clean, rect = make_cave()
    cave_glows = make_cave_glows(clean, rect)
    make_hex()
    make_hex_border()
    make_hex_rings()
    make_core_glow()
    make_scanline()
    make_divider()

    plate("mine-reset-cost-plate", (406, 475, 1265, 577), 12, [(420, 484, 1250, 570)], seed=1)
    plate("mine-reset-info-plate", (322, 593, 825, 696), 8, [(336, 600, 812, 690)], seed=2)
    plate("mine-reset-timer-plate", (323, 711, 1348, 770), 8, [(336, 716, 1336, 766)], seed=3)

    icon("mine-reset-icon-gold", (660, 482, 756, 546), floor=0.1)
    icon("mine-reset-badge-reset", (338, 602, 436, 690))
    icon("mine-reset-badge-keep", (862, 602, 960, 690))
    icon("mine-reset-icon-clock", (616, 716, 668, 768))
    icon("mine-reset-timer-divider", (822, 722, 836, 762))

    c_col, c_alpha, c_outer = button("mine-reset-button-cancel", (376, 788, 815, 867), 10, (548, 804, 650, 852), seed=1)
    f_col, f_alpha, f_outer = button("mine-reset-button-confirm", (850, 788, 1299, 867), 10, (928, 804, 1226, 852), glow_px=10, seed=2)
    hover_variant("mine-reset-button-cancel-hover", c_col, c_alpha, c_outer, 1.35, (0, 14, 20))
    hover_variant("mine-reset-button-confirm-hover", f_col, f_alpha, f_outer, 1.22, (10, 24, 26))

    meta["_cave_glows"] = cave_glows
    with open(os.path.join(HERE, "init_mine_popup_layout.json"), "w", encoding="utf-8") as f:
        json.dump(meta, f, indent=1, ensure_ascii=False)
    for k, v in meta.items():
        print(k, v)


if __name__ == "__main__":
    main()
