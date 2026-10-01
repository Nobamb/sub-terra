# prompt-B 118-1: 업그레이드 창 프레임·아이콘 연출용 스프라이트 생성 스크립트.
# 기존 프로젝트 아트(설정창 프레임, 화물상자, 플레이어, 동전, 플러그)를 가공하거나 코드로 그린다.
# 이미지 생성 AI는 쓰지 않는다. 저장소 루트에서 `python work_process/.../make_upgrade_art.py` 로 실행.
import math
import os
import random

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "..", "..", "sub-terra", "Assets", "_Project", "Art")
ROOT = os.path.normpath(ROOT)
OUT = os.path.join(ROOT, "UI", "Upgrade")
ICONS = os.path.join(OUT, "Icons")
FX = os.path.join(OUT, "Fx")


def p(*parts):
    return os.path.join(ROOT, *parts)


def load(*parts):
    return Image.open(p(*parts)).convert("RGBA")


# ------------------------------------------------------------------ 창 프레임
def make_window_frame():
    """설정창 프레임에서 안쪽 섹션 박스·아이콘·장식선만 지우고 외곽 프레임과 질감은 남긴다."""
    src = load("UI", "MainMenu", "Settings", "setting-menu-asset.png")
    arr = np.asarray(src).astype(np.float32)
    h, w = arr.shape[:2]
    rgb = arr[:, :, :3]
    alpha = arr[:, :, 3]

    # 안쪽 영역(프레임 선 안쪽)에서 밝은 선 픽셀을 찾는다.
    x0, x1, y0, y1 = 122, 1326, 26, 1056
    region = np.zeros((h, w), bool)
    region[y0:y1, x0:x1] = True
    bright = (rgb[:, :, 1] > 46) | (rgb[:, :, 2] > 58)
    mask = region & bright
    mask_img = Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(11))
    mask = (np.asarray(mask_img) > 0) & region

    # 정규화 합성곱으로 선을 뺀 배경을 채운다. 주변 표본이 없는 곳은 안쪽 평균색을 쓴다.
    keep = (~mask).astype(np.float32)
    mean = rgb[region & ~mask].mean(axis=0)
    weight = np.asarray(
        Image.fromarray((keep * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(14))
    ).astype(np.float32) / 255.0
    filled = np.zeros_like(rgb)
    for c in range(3):
        channel = Image.fromarray(np.clip(rgb[:, :, c] * keep, 0, 255).astype(np.uint8))
        blurred = np.asarray(channel.filter(ImageFilter.GaussianBlur(14))).astype(np.float32)
        value = blurred / np.maximum(weight, 1e-3)
        trust = np.clip(weight / 0.25, 0, 1)
        filled[:, :, c] = value * trust + mean[c] * (1 - trust)

    # 원래 질감의 잔결을 조금 얹고, 경계는 부드럽게 섞는다.
    rng = np.random.default_rng(118)
    noise = rng.normal(0.0, 1.0, (h, w, 1)).astype(np.float32)
    filled = np.clip(filled + noise, 0, 255)
    soft = np.asarray(
        Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(2))
    ).astype(np.float32)[:, :, None] / 255.0
    soft = np.where(region[:, :, None], soft, 0)
    out = rgb * (1 - soft) + filled * soft

    result = np.dstack([out, alpha]).astype(np.uint8)
    img = Image.fromarray(result, "RGBA")
    bbox = img.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox()
    img = img.crop(bbox)
    img.save(os.path.join(OUT, "upgrade-window-frame.png"))
    print("frame", img.size, "bbox", bbox)


# ------------------------------------------------------------------ 화물상자 몸통/뚜껑
BOX_CUT = [(0, 106), (150, 150), (300, 106)]
BOX_OPENING = [(22, 104), (146, 52), (281, 101), (150, 146)]


def cut_y(x):
    (ax, ay), (bx, by), (cx, cy) = BOX_CUT
    if x <= bx:
        return ay + (by - ay) * (x - ax) / float(bx - ax)
    return by + (cy - by) * (x - bx) / float(cx - bx)


def make_box_parts():
    box = load("UI", "Upgrade", "Icons", "upgrade-icon-box.png")
    w, h = box.size
    lid_mask = Image.new("L", box.size, 0)
    for x in range(w):
        y = int(round(cut_y(x)))
        ImageDraw.Draw(lid_mask).line([(x, 0), (x, y)], fill=255)
    lid = Image.new("RGBA", box.size, (0, 0, 0, 0))
    lid.paste(box, (0, 0), lid_mask)
    lid.save(os.path.join(ICONS, "upgrade-icon-box-lid.png"))

    body_mask = lid_mask.point(lambda v: 255 - v)
    body = Image.new("RGBA", box.size, (0, 0, 0, 0))
    body.paste(box, (0, 0), body_mask)

    # 열린 입구: 어두운 안쪽 + 뒤쪽 벽 윗면 테두리.
    inner = Image.new("RGBA", box.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(inner)
    d.polygon(BOX_OPENING, fill=(8, 18, 24, 255))
    # 안쪽 바닥 쪽으로 갈수록 살짝 밝게(청록 반사).
    glow = Image.new("RGBA", box.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    cx, cy = 150, 110
    for r in range(70, 0, -2):
        a = int(70 * (1 - r / 70.0))
        gd.ellipse([cx - r * 1.6, cy - r * 0.7, cx + r * 1.6, cy + r * 0.7], fill=(40, 170, 200, a))
    glow_masked = Image.new("RGBA", box.size, (0, 0, 0, 0))
    opening_mask = Image.new("L", box.size, 0)
    ImageDraw.Draw(opening_mask).polygon(BOX_OPENING, fill=255)
    glow_masked.paste(glow, (0, 0), opening_mask)
    inner = Image.alpha_composite(inner, glow_masked)
    d = ImageDraw.Draw(inner)
    left, back, right, front = BOX_OPENING
    d.line([left, back, right], fill=(150, 175, 185, 255), width=7)
    d.line([left, back, right], fill=(70, 220, 245, 255), width=2)
    d.line([left, front, right], fill=(25, 40, 48, 255), width=3)
    body = Image.alpha_composite(inner, body)
    body.save(os.path.join(ICONS, "upgrade-icon-box-body.png"))


# ------------------------------------------------------------------ 가스 저항
GAS_BUST = (58, 12, 202, 162)
GAS_SCALE = 1.86


def gas_cloud():
    """캐릭터 뒤의 반투명 녹색 가스 구름: 둥근 덩어리로 이어진 외곽 + 밝은 결."""
    shape = Image.new("L", (300, 300), 0)
    d = ImageDraw.Draw(shape)
    cx, cy = 150, 158
    d.ellipse([cx - 112, cy - 104, cx + 112, cy + 112], fill=255)
    for i in range(14):
        ang = i / 14.0 * math.tau
        r = 40 + (i % 3) * 7
        x = cx + math.cos(ang) * 110
        y = cy + math.sin(ang) * 104
        d.ellipse([x - r, y - r, x + r, y + r], fill=255)
    shape = shape.filter(ImageFilter.GaussianBlur(5))
    cloud = Image.new("RGBA", (300, 300), (104, 190, 62, 0))
    cloud.putalpha(shape.point(lambda v: int(v * 0.5)))

    # 밝은 결과 어두운 소용돌이로 가스 질감을 낸다.
    detail = Image.new("RGBA", (300, 300), (0, 0, 0, 0))
    dd = ImageDraw.Draw(detail)
    rnd = random.Random(5)
    for _ in range(9):
        x, y = rnd.randint(40, 260), rnd.randint(40, 250)
        rx, ry = rnd.randint(28, 50), rnd.randint(14, 24)
        dd.ellipse([x - rx, y - ry, x + rx, y + ry], fill=(178, 238, 104, 60))
    for _ in range(5):
        x, y = rnd.randint(50, 250), rnd.randint(50, 250)
        dd.arc([x - 34, y - 18, x + 34, y + 18], rnd.randint(0, 180), rnd.randint(200, 330),
               fill=(70, 140, 40, 90), width=5)
    detail = detail.filter(ImageFilter.GaussianBlur(4))
    detail.putalpha(Image.fromarray(np.minimum(np.asarray(detail.getchannel("A")), np.asarray(shape))))
    return Image.alpha_composite(cloud, detail)


def gas_front():
    """캐릭터 앞을 덮는 옅은 녹색 막 + 아래쪽 가스 띠. 얼굴은 비교적 맑게 둔다."""
    layer = Image.new("RGBA", (300, 300), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.ellipse([30, 40, 270, 300], fill=(120, 206, 70, 46))
    d.ellipse([110, 70, 250, 200], fill=(0, 0, 0, 0))
    for x, y, rx, ry, a in [(70, 250, 80, 30, 95), (210, 262, 95, 30, 90), (150, 285, 140, 26, 110),
                            (35, 190, 34, 22, 70), (270, 205, 34, 24, 70)]:
        d.ellipse([x - rx, y - ry, x + rx, y + ry], fill=(132, 214, 74, a))
    rnd = random.Random(7)
    for _ in range(10):
        r = rnd.randint(4, 8)
        x = rnd.choice([rnd.randint(22, 78), rnd.randint(232, 282)])
        y = rnd.randint(40, 270)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(196, 250, 128, rnd.randint(90, 150)))
    return layer.filter(ImageFilter.GaussianBlur(4))


def make_gas_icon():
    player = load("Characters", "Player", "Frames", "Idle", "player_idle_01.png")
    bust = player.crop(GAS_BUST)
    bw, bh = bust.size
    bust = bust.resize((int(bw * GAS_SCALE), int(bh * GAS_SCALE)), Image.LANCZOS)
    ox = (300 - bust.size[0]) // 2
    oy = 300 - bust.size[1]
    # 아래로 갈수록 몸통을 흐리게 잘라 흉상처럼 보이게 한다.
    fade = Image.new("L", bust.size, 255)
    fd = ImageDraw.Draw(fade)
    for y in range(bust.size[1]):
        t = (y - bust.size[1] * 0.78) / (bust.size[1] * 0.22)
        if t > 0:
            fd.line([(0, y), (bust.size[0], y)], fill=int(255 * max(0.0, 1 - t)))
    bust.putalpha(Image.fromarray(np.minimum(np.asarray(bust.getchannel("A")), np.asarray(fade))))

    icon = Image.new("RGBA", (300, 300), (0, 0, 0, 0))
    icon.alpha_composite(gas_cloud())
    icon.alpha_composite(bust, (ox, oy))
    icon.alpha_composite(gas_front())
    icon.save(os.path.join(ICONS, "upgrade-icon-gas.png"))

    # 입을 막는 손: 피격 프레임의 장갑 낀 주먹을 잘라 입 위치에 맞춘다.
    damage = load("Characters", "Player", "Frames", "Damage", "damage_02.png")
    fist = damage.crop((149, 95, 181, 129))
    fist_mask = Image.new("L", fist.size, 0)
    ImageDraw.Draw(fist_mask).ellipse([0, 0, fist.size[0] - 1, fist.size[1] - 1], fill=255)
    fist.putalpha(Image.fromarray(np.minimum(np.asarray(fist.getchannel("A")), np.asarray(fist_mask))))
    fist = fist.resize((int(fist.size[0] * GAS_SCALE * 1.05), int(fist.size[1] * GAS_SCALE * 1.05)), Image.LANCZOS)
    mouth = ((157 - GAS_BUST[0]) * GAS_SCALE + ox, (97 - GAS_BUST[1]) * GAS_SCALE + oy)
    hand = Image.new("RGBA", (300, 300), (0, 0, 0, 0))
    hand.alpha_composite(fist, (int(mouth[0] - fist.size[0] * 0.5), int(mouth[1] - fist.size[1] * 0.42)))
    hand.save(os.path.join(ICONS, "upgrade-icon-gas-hand.png"))
    print("gas mouth", mouth)


# ------------------------------------------------------------------ 플러그 점등
def make_plug_lit():
    plug = load("UI", "Upgrade", "Icons", "upgrade-icon-plug.png")
    arr = np.asarray(plug).astype(np.float32)
    r, g, b, a = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2], arr[:, :, 3]
    cyan = np.clip((b - r - 20) / 80.0, 0, 1)
    lit = arr.copy()
    lit[:, :, 0] = np.clip(r + cyan * 120 + 18, 0, 255)
    lit[:, :, 1] = np.clip(g * (1 + cyan * 0.6) + 22, 0, 255)
    lit[:, :, 2] = np.clip(b * (1 + cyan * 0.4) + 26, 0, 255)
    lit_img = Image.fromarray(lit.astype(np.uint8), "RGBA")
    bloom_mask = Image.fromarray((cyan * a).astype(np.uint8)).filter(ImageFilter.GaussianBlur(10))
    bloom = Image.new("RGBA", plug.size, (110, 240, 255, 0))
    bloom.putalpha(bloom_mask.point(lambda v: min(255, int(v * 1.6))))
    out = Image.alpha_composite(bloom, lit_img)
    out.save(os.path.join(ICONS, "upgrade-icon-plug-lit.png"))


# ------------------------------------------------------------------ 연출 파츠
def make_spark():
    size = 96
    rnd = random.Random(3)
    core = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(core)
    pts = [(10, 48)]
    x = 10
    while x < 84:
        x += rnd.randint(9, 14)
        pts.append((min(x, 86), 48 + rnd.randint(-15, 15)))
    d.line(pts, fill=(255, 255, 255, 255), width=4, joint="curve")
    branch = [pts[2], (pts[2][0] + 10, pts[2][1] - 18), (pts[2][0] + 18, pts[2][1] - 24)]
    d.line(branch, fill=(255, 255, 255, 230), width=2)
    glow = core.filter(ImageFilter.GaussianBlur(5))
    out = Image.alpha_composite(glow, glow)
    out = Image.alpha_composite(out, core)
    out.save(os.path.join(FX, "upgrade-fx-spark.png"))


def make_ring():
    size = 128
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    arr = np.zeros((size, size, 4), np.float32)
    yy, xx = np.mgrid[0:size, 0:size]
    rr = np.sqrt((xx - 63.5) ** 2 + (yy - 63.5) ** 2) / 63.5
    ring = np.exp(-((rr - 0.86) / 0.07) ** 2)
    inner = np.clip(1 - rr, 0, 1) ** 2 * 0.25
    a = np.clip(ring + inner, 0, 1)
    arr[:, :, 0:3] = 255
    arr[:, :, 3] = a * 255
    Image.fromarray(arr.astype(np.uint8), "RGBA").save(os.path.join(FX, "upgrade-fx-ring.png"))


def make_shard():
    size = 40
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    outline = [(6, 18), (16, 5), (33, 9), (35, 26), (20, 35), (8, 30)]
    d.polygon(outline, fill=(150, 66, 34, 255))
    d.polygon([(6, 18), (16, 5), (33, 9), (21, 19)], fill=(238, 140, 88, 255))
    d.polygon([(21, 19), (33, 9), (35, 26)], fill=(196, 96, 52, 255))
    d.line([(16, 5), (33, 9)], fill=(255, 205, 160, 255), width=2)
    d.line(outline + [outline[0]], fill=(70, 28, 14, 255), width=2)
    img.save(os.path.join(FX, "upgrade-fx-shard.png"))


def make_coin():
    # 동전 아이콘 오른쪽의 세워진 동전 하나만 기울어진 타원 마스크로 떼어 낸다.
    coins = load("UI", "Upgrade", "Icons", "upgrade-icon-coins.png")
    mask = Image.new("L", coins.size, 0)
    ImageDraw.Draw(mask).ellipse([212 - 60, 181 - 73, 212 + 60, 181 + 73], fill=255)
    mask = mask.rotate(22, center=(212, 181), resample=Image.BICUBIC)
    coin = Image.new("RGBA", coins.size, (0, 0, 0, 0))
    coin.paste(coins, (0, 0), mask)
    crop = coin.crop(coin.getchannel("A").point(lambda a: 255 if a > 20 else 0).getbbox())
    side = max(crop.size)
    out = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    out.alpha_composite(crop, ((side - crop.size[0]) // 2, (side - crop.size[1]) // 2))
    out = out.resize((64, 64), Image.LANCZOS)
    out.save(os.path.join(FX, "upgrade-fx-coin.png"))


def make_plus():
    size = 48
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([19, 6, 29, 42], radius=4, fill=(255, 255, 255, 255))
    d.rounded_rectangle([6, 19, 42, 29], radius=4, fill=(255, 255, 255, 255))
    glow = img.filter(ImageFilter.GaussianBlur(3))
    Image.alpha_composite(glow, img).save(os.path.join(FX, "upgrade-fx-plus.png"))


if __name__ == "__main__":
    os.makedirs(FX, exist_ok=True)
    make_window_frame()
    make_box_parts()
    make_gas_icon()
    make_plug_lit()
    make_spark()
    make_ring()
    make_shard()
    make_coin()
    make_plus()
