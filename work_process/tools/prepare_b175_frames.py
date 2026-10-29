"""Align the B-175 jump drawings (ready 8, processing 24, fall 24) with one scale and one translation."""
from pathlib import Path
import hashlib
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

sys.dont_write_bytecode = True
from prepare_b172_frames import alpha_bounds, solid_bounds, helmet_bounds, sheet, save_gif

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'work_process/images/player-jump-20261029'
SOURCE = FOLDER / 'source'
OUTPUT = FOLDER / 'runtime-frames'
IDLE = ROOT / 'sub-terra/Assets/_Project/Art/Characters/Player/Frames/Idle/player_idle_01.png'
GROUPS = [('ready', 'jump-ready', 8), ('processing', 'jump-rise', 24), ('fall', 'jump-fall', 24)]
SCALE = 0.267
CANVAS = (256, 256)
# ready 1 발끝(알파 32 이상 마지막 줄)을 대기 그림과 같은 줄에 둔다.
TARGET_FOOT_Y = 241
APEX_LAST_FRAME = 15


def load_group(folder, count):
    expected = {f'{i}.png' for i in range(1, count + 1)}
    found = {path.name for path in folder.glob('*.png')}
    if found != expected:
        raise FileNotFoundError(f'{folder}: missing={sorted(expected - found)}, extra={sorted(found - expected)}')
    images = [Image.open(folder / f'{i}.png').convert('RGBA') for i in range(1, count + 1)]
    if any(image.size != (960, 960) for image in images):
        raise ValueError(f'{folder}: expected 960x960, got {[image.size for image in images]}')
    return images


def centroid_x(image):
    alpha = np.asarray(image.getchannel('A')).astype(float)
    xs = np.indices(alpha.shape)[1]
    return float((alpha * xs).sum() / alpha.sum())


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


def silhouette_difference(a, b):
    left = np.asarray(a.getchannel('A')).astype(float) / 255
    right = np.asarray(b.getchannel('A')).astype(float) / 255
    return float(np.abs(left - right).sum() / max(right.sum(), 1))


def main():
    groups = {name: load_group(SOURCE / name, count) for name, _, count in GROUPS}
    idle = Image.open(IDLE).convert('RGBA')
    size = round(960 * SCALE)
    actual_scale = size / 960
    scaled = {name: [resize(image, size) for image in images] for name, images in groups.items()}

    # 이동량 하나: ready 1의 발끝과 몸 무게중심을 대기 그림에 맞춘다(세 묶음 56장 공통).
    anchor = scaled['ready'][0]
    anchor_bounds = alpha_bounds(anchor)
    dy = TARGET_FOOT_Y - (anchor_bounds[3] - 1)
    idle_center = centroid_x(idle)
    dx = round(idle_center - centroid_x(anchor))

    OUTPUT.mkdir(parents=True, exist_ok=True)
    frames, records = {}, {}
    for name, prefix, _ in GROUPS:
        frames[name], records[name] = [], []
        for index, (source, resized) in enumerate(zip(groups[name], scaled[name]), 1):
            frame = Image.new('RGBA', CANVAS)
            frame.alpha_composite(resized, (dx, dy))
            bounds = alpha_bounds(frame)
            path = OUTPUT / f'{prefix}-{index:02}.png'
            save_atomic(frame, path)
            source_bounds = alpha_bounds(source)
            records[name].append({
                'frame': index, 'source_bounds': list(source_bounds),
                'output_bounds': list(bounds), 'output_height': bounds[3] - bounds[1],
                'foot_y': bounds[3] - 1, 'center_x': (bounds[0] + bounds[2]) / 2,
                'centroid_x': round(centroid_x(frame), 2),
                'edge_clearance': {'left': bounds[0], 'top': bounds[1],
                                   'right': CANVAS[0] - bounds[2], 'bottom': CANVAS[1] - bounds[3]},
                'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
            frames[name].append(frame)

    all_records = [record for name in records for record in records[name]]
    for record in all_records:
        assert min(record['edge_clearance'].values()) >= 2, record
    hashes = [record['sha256'] for record in all_records]
    assert len(set(hashes)) == len(hashes), 'Duplicate output frames'

    # fall 1~15의 장별 발 위치 차이(게임 픽셀, 아래로 +). 보정은 이 값만큼 그림을 반대로 옮긴다.
    fall_feet = [record['foot_y'] for record in records['fall'][:APEX_LAST_FRAME]]
    bob = [round(fall_feet[0] - foot, 2) for foot in fall_feet]

    ready_first = frames['ready'][0]
    idle_helmet = helmet_bounds(idle)
    ready_helmet = helmet_bounds(ready_first)
    idle_bounds = alpha_bounds(idle)
    ready_bounds = alpha_bounds(ready_first)
    report = {
        'scale': SCALE, 'actual_scale': actual_scale, 'resized_size': size,
        'translation': [dx, dy], 'canvas': list(CANVAS), 'ppu': 256, 'pivot': [0.5, 0.5],
        'alignment': 'ready 1 foot row -> y241, alpha-weighted body centroid -> idle centroid; one translation for all 56 frames',
        'idle': {'bounds': list(idle_bounds), 'height': idle_bounds[3] - idle_bounds[1],
                 'foot_y': idle_bounds[3] - 1, 'centroid_x': round(idle_center, 2),
                 'helmet_bounds': list(idle_helmet), 'helmet_width': idle_helmet[2] - idle_helmet[0]},
        'ready_1': {'bounds': list(ready_bounds), 'height': ready_bounds[3] - ready_bounds[1],
                    'foot_y': ready_bounds[3] - 1, 'centroid_x': round(centroid_x(ready_first), 2),
                    'helmet_bounds': list(ready_helmet), 'helmet_width': ready_helmet[2] - ready_helmet[0],
                    'silhouette_difference_vs_idle': round(silhouette_difference(ready_first, idle), 4)},
        'range': {'top_min': min(r['output_bounds'][1] for r in all_records),
                  'bottom_max': max(r['output_bounds'][3] for r in all_records),
                  'left_min': min(r['output_bounds'][0] for r in all_records),
                  'right_max': max(r['output_bounds'][2] for r in all_records)},
        'fall_bob_offsets_px': bob,
        'frames': records,
    }
    (FOLDER / 'measurements.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')

    sheet(frames['ready'] + frames['processing'] + frames['fall'], FOLDER / 'comparison-contact-sheet.png', 8, (128, 128))
    # 실제 재생 순서 미리보기: rise 1~24, fall 1~15, fall 18~24 두 번, ready 8~1.
    order = (frames['processing'] + frames['fall'][:APEX_LAST_FRAME] + frames['fall'][17:] * 2
             + list(reversed(frames['ready'])))
    save_gif(order, FOLDER / 'runtime-preview.gif', 42)
    comparison = Image.new('RGB', (256 * 6, 256 + 28), '#eeeeee')
    draw = ImageDraw.Draw(comparison)
    for col, (label, frame) in enumerate([('idle', idle), ('ready 1', ready_first), ('ready 8', frames['ready'][7]),
                                          ('rise 5', frames['processing'][4]), ('rise 12', frames['processing'][11]),
                                          ('fall 18', frames['fall'][17])]):
        background = Image.new('RGBA', CANVAS, '#eeeeee')
        background.alpha_composite(frame)
        comparison.paste(background.convert('RGB'), (col * 256, 28))
        draw.line((col * 256, 28 + TARGET_FOOT_Y, col * 256 + 255, 28 + TARGET_FOOT_Y), fill='red')
        draw.text((col * 256 + 5, 6), label, fill='black')
    comparison.save(FOLDER / 'idle-comparison.png')
    print(json.dumps({k: report[k] for k in ['actual_scale', 'translation', 'idle', 'ready_1', 'range', 'fall_bob_offsets_px']}, indent=1))


if __name__ == '__main__':
    main()
