"""Align the B-176 in-between damage drawings (1-2, 2-3, 3-4, 4-idle) between the existing damage 1~4."""
from pathlib import Path
import hashlib
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

sys.dont_write_bytecode = True
from prepare_b172_frames import alpha_bounds, sheet, save_gif

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'work_process/images/player-damage-8f-20261029'
SOURCE = FOLDER / 'source'
OUTPUT = FOLDER / 'runtime-frames'
GAME = ROOT / 'sub-terra/Assets/_Project/Art/Characters/Player/Frames/Damage'
IDLE = ROOT / 'sub-terra/Assets/_Project/Art/Characters/Player/Frames/Idle/player_idle_01.png'
# (원본 이름, 출력 이름, 배율, 가로 이동 px). 배율·이동은 PM이 앞뒤 게임 그림 실루엣에 맞춘 값.
FRAMES = [
    ('damage-1-2-center.png', 'damage-1-2.png', 0.209, -8),
    ('damage-2-3-center.png', 'damage-2-3.png', 0.211, -9),
    ('damage-3-4-center.png', 'damage-3-4.png', 0.213, -7),
    ('damage-4-idle.png', 'damage-4-idle.png', 0.229, 0),
]
SOURCE_SIZE = (1254, 1254)
CANVAS = (256, 256)
CENTER_X = 128
# 기존 damage_01~04 발끝(알파 32 이상 마지막 줄).
TARGET_FOOT_Y = 242
NOISE_ALPHA = 8


def centroid_x(image):
    alpha = np.asarray(image.getchannel('A')).astype(float)
    xs = np.indices(alpha.shape)[1]
    return float((alpha * xs).sum() / alpha.sum())


def clean(image):
    # 테두리 쪽 알파 1 잡티를 축소 전에 지운다.
    data = np.asarray(image).copy()
    data[data[:, :, 3] < NOISE_ALPHA] = 0
    return Image.fromarray(data, 'RGBA')


def resize(image, size):
    # Premultiplied (RGBa) filtering avoids dark fringes around the transparent edge.
    return image.convert('RGBa').resize((size, size), Image.Resampling.LANCZOS).convert('RGBA')


def save_atomic(image, path):
    temporary = path.with_suffix('.png.tmp')
    image.save(temporary, format='PNG')
    if path.exists() and path.read_bytes() == temporary.read_bytes():
        temporary.unlink()
        return
    os.replace(temporary, path)


def mask(image):
    return np.asarray(image.getchannel('A')) >= 32


def iou(a, b):
    left, right = mask(a), mask(b)
    return float((left & right).sum() / max((left | right).sum(), 1))


def silhouette_difference(a, b):
    left = np.asarray(a.getchannel('A')).astype(float) / 255
    right = np.asarray(b.getchannel('A')).astype(float) / 255
    return float(np.abs(left - right).sum() / max(right.sum(), 1))


def measure(frame):
    x0, y0, x1, y1 = alpha_bounds(frame)
    alpha = np.asarray(frame.getchannel('A'))
    return {'bounds': [x0, y0, x1, y1], 'height': y1 - y0, 'foot_y': y1 - 1,
            'centroid_x': round(centroid_x(frame), 2),
            'edge_clearance': {'left': x0, 'top': y0, 'right': CANVAS[0] - x1, 'bottom': CANVAS[1] - y1},
            'alpha_1_to_7_pixels': int(((alpha > 0) & (alpha < NOISE_ALPHA)).sum())}


def main():
    expected = {name for name, *_ in FRAMES}
    found = {path.name for path in SOURCE.glob('*.png')}
    if found != expected:
        raise FileNotFoundError(f'{SOURCE}: missing={sorted(expected - found)}, extra={sorted(found - expected)}')
    sources = [Image.open(SOURCE / name).convert('RGBA') for name, *_ in FRAMES]
    if any(image.size != SOURCE_SIZE for image in sources):
        raise ValueError(f'expected {SOURCE_SIZE}, got {[image.size for image in sources]}')
    game = [Image.open(GAME / f'damage_0{i}.png').convert('RGBA') for i in range(1, 5)]
    idle = Image.open(IDLE).convert('RGBA')

    OUTPUT.mkdir(parents=True, exist_ok=True)
    new_frames, records = [], []
    for (source_name, output_name, scale, shift), source in zip(FRAMES, sources):
        resized = resize(clean(source), round(SOURCE_SIZE[0] * scale))
        # 축소 뒤에도 남는 1~7 알파 가장자리는 지운다(게임 그림에 잡티가 없게).
        resized = clean(resized)
        bounds = alpha_bounds(resized)
        dx = round(CENTER_X + shift - centroid_x(resized))
        dy = TARGET_FOOT_Y - (bounds[3] - 1)
        frame = Image.new('RGBA', CANVAS)
        frame.alpha_composite(resized, (dx, dy))
        path = OUTPUT / output_name
        save_atomic(frame, path)
        record = {'source': source_name, 'output': output_name, 'scale': scale,
                  'resized_size': resized.width, 'shift_x': shift, 'translation': [dx, dy]}
        record.update(measure(frame))
        record['sha256'] = hashlib.sha256(path.read_bytes()).hexdigest()
        records.append(record)
        new_frames.append(frame)

    # 재생 순서: 1, 1-2, 2, 2-3, 3, 3-4, 4, 4-idle (뒤에 대기 그림을 붙여 비교).
    order = [game[0], new_frames[0], game[1], new_frames[1], game[2], new_frames[2], game[3], new_frames[3]]
    labels = ['01', '1-2', '02', '2-3', '03', '3-4', '04', '4-idle']
    for index, record in enumerate(records):
        position = 2 * index + 1
        before = order[position - 1]
        after = order[position + 1] if position + 1 < len(order) else idle
        record['iou_prev'] = round(iou(order[position], before), 3)
        record['iou_next'] = round(iou(order[position], after), 3)
        record['silhouette_difference_prev'] = round(silhouette_difference(order[position], before), 3)
        record['silhouette_difference_next'] = round(silhouette_difference(after, order[position]), 3)
        assert min(record['edge_clearance'].values()) >= 2, record
        assert abs(record['foot_y'] - TARGET_FOOT_Y) <= 2, record
        assert record['alpha_1_to_7_pixels'] == 0, record
    hashes = [record['sha256'] for record in records]
    assert len(set(hashes)) == len(hashes), 'Duplicate output frames'

    steps = [round(silhouette_difference(order[i + 1], order[i]), 3) for i in range(len(order) - 1)]
    steps.append(round(silhouette_difference(idle, order[-1]), 3))
    report = {
        'canvas': list(CANVAS), 'ppu': 256, 'pivot': [0.5, 0.5], 'target_foot_y': TARGET_FOOT_Y,
        'alignment': 'alpha<8 cleared, premultiplied LANCZOS, alpha-weighted body centroid -> 128 + shift, foot row -> y242',
        'game_frames': [dict(measure(frame), name=f'damage_0{i + 1}') for i, frame in enumerate(game)],
        'order': labels,
        'silhouette_difference_steps': dict(zip([f'{a}->{b}' for a, b in zip(labels, labels[1:] + ['idle'])], steps)),
        'frames': records,
    }
    (FOLDER / 'measurements.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')

    sheet(order + [idle], FOLDER / 'comparison-contact-sheet.png', 9, (128, 128))
    save_gif(order + [idle] * 4, FOLDER / 'runtime-preview.gif', 50)
    comparison = Image.new('RGB', (256 * len(order), 256 + 28), '#eeeeee')
    draw = ImageDraw.Draw(comparison)
    for col, (label, frame) in enumerate(zip(labels, order)):
        background = Image.new('RGBA', CANVAS, '#eeeeee')
        background.alpha_composite(frame)
        comparison.paste(background.convert('RGB'), (col * 256, 28))
        draw.line((col * 256, 28 + TARGET_FOOT_Y, col * 256 + 255, 28 + TARGET_FOOT_Y), fill='red')
        draw.line((col * 256 + CENTER_X, 28, col * 256 + CENTER_X, 28 + 255), fill='#9999ff')
        draw.text((col * 256 + 5, 6), label, fill='black')
    comparison.save(FOLDER / 'damage-8f-comparison.png')
    print(json.dumps({'steps': report['silhouette_difference_steps'],
                      'frames': [{k: r[k] for k in ['output', 'translation', 'height', 'foot_y', 'centroid_x',
                                                     'edge_clearance', 'iou_prev', 'iou_next']} for r in records]},
                     indent=1))


if __name__ == '__main__':
    main()
