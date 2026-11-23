"""B-209 광부 선택 화면 그림 정리.

Codex(gpt-6-luna)가 검은 배경 위에 그린 청록 발광 UI 그림을
1) 밝기 → 알파(검정은 투명, 빛 번짐은 반투명으로 남김, 색은 premultiply 해제),
2) 내용 영역으로 자르고 여백을 붙인 뒤,
3) 게임에서 쓸 크기로 줄여 Resources/UI/MinerSelect/에 저장한다.
버튼은 꺼진 상태용 회색 판도 만든다.

사용: python prepare_miner_select_art.py <codex 출력 폴더> <Assets/_Project/Resources/UI/MinerSelect>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

# (원본 파일, 출력 이름, 목표 가로 px, 자른 뒤 여백 px)
JOBS = [
    ("miner-card-frame.png", "miner-card-frame.png", 828, 6),
    ("miner-pedestal.png", "miner-pedestal.png", 640, 8),
    ("miner-button-frame.png", "miner-button-frame.png", 1100, 6),
    ("miner-screen-frame.png", "miner-screen-frame.png", 1600, 4),
]


def black_to_alpha(rgb: np.ndarray) -> np.ndarray:
    """검은 배경의 발광 그림을 RGBA로. 알파는 가장 밝은 채널, 색은 알파로 나눠 되돌린다."""
    rgb = rgb.astype(np.float32) / 255.0
    alpha = rgb.max(axis=2)
    alpha = np.where(alpha < 4.0 / 255.0, 0.0, alpha)
    safe = np.maximum(alpha, 1e-4)[..., None]
    color = np.clip(rgb / safe, 0.0, 1.0)
    out = np.dstack([color, alpha])
    return (out * 255.0 + 0.5).astype(np.uint8)


def crop_to_content(rgba: np.ndarray, pad: int) -> np.ndarray:
    ys, xs = np.where(rgba[..., 3] > 8)
    y0, y1 = max(0, ys.min() - pad), min(rgba.shape[0], ys.max() + 1 + pad)
    x0, x1 = max(0, xs.min() - pad), min(rgba.shape[1], xs.max() + 1 + pad)
    return rgba[y0:y1, x0:x1]


def resize_premultiplied(rgba: np.ndarray, width: int) -> Image.Image:
    """알파 가장자리에 검은 테가 생기지 않게 premultiply 상태로 줄인다."""
    f = rgba.astype(np.float32) / 255.0
    f[..., :3] *= f[..., 3:4]
    h = max(1, round(rgba.shape[0] * width / rgba.shape[1]))
    channels = [
        np.asarray(Image.fromarray((f[..., i] * 255.0).astype(np.uint8)).resize((width, h), Image.LANCZOS), dtype=np.float32) / 255.0
        for i in range(4)
    ]
    out = np.dstack(channels)
    safe = np.maximum(out[..., 3:4], 1e-4)
    out[..., :3] = np.clip(out[..., :3] / safe, 0.0, 1.0)
    out[..., :3] *= (out[..., 3:4] > 0)
    return Image.fromarray((out * 255.0 + 0.5).astype(np.uint8), "RGBA")


def grey_version(image: Image.Image) -> Image.Image:
    """꺼진 버튼: 밝기만 남기고 회청색으로 낮춘다."""
    a = np.asarray(image, dtype=np.float32) / 255.0
    lum = (a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114)[..., None]
    tint = np.array([0.62, 0.68, 0.7], dtype=np.float32)
    rgb = np.clip(lum * tint * 1.25, 0.0, 1.0)
    alpha = a[..., 3:4] * 0.75
    return Image.fromarray((np.dstack([rgb, alpha]) * 255.0 + 0.5).astype(np.uint8), "RGBA")


def main() -> None:
    src = Path(sys.argv[1])
    dst = Path(sys.argv[2])
    dst.mkdir(parents=True, exist_ok=True)
    for name, out_name, width, pad in JOBS:
        rgb = np.asarray(Image.open(src / name).convert("RGB"))
        rgba = crop_to_content(black_to_alpha(rgb), pad)
        image = resize_premultiplied(rgba, width)
        image.save(dst / out_name, optimize=True)
        print(out_name, image.size)
        if out_name == "miner-button-frame.png":
            grey = grey_version(image)
            grey.save(dst / "miner-button-frame-off.png", optimize=True)
            print("miner-button-frame-off.png", grey.size)


if __name__ == "__main__":
    main()
