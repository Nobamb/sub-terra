"""B-208: 여자 캐릭터 연번 PNG(512)를 남자와 같은 파일 이름의 256 런타임 프레임으로 만든다.

- 원본: work_process/MVP2/female_sprite/female_ver2 (압축 그대로 푼 폴더, 배경은 이미 투명)
- 파일 번호 __{2k-2} ≈ 남자 k번째 그림. 홀수 번호는 중간 그림이라 쓰지 않는다.
- 크기·위치 보정 없이 같은 캔버스를 LANCZOS로 축소한다(발 위치·키는 원본에서 이미 맞춤).
- 마지막 장이 없으면 복제하지 않고 뺀다. webm·mov는 읽지 않는다.
"""
from pathlib import Path
import hashlib
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

sys.dont_write_bytecode = True

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'work_process/MVP2/female_sprite/female_ver2'
GAME = ROOT / 'sub-terra/Assets/_Project/Art/Characters/Player'
OUTPUT = GAME / 'Female/Frames'
MALE = GAME / 'Frames'
PREVIEW = ROOT / 'work_process/images/player-female-20261121'
CANVAS = 256
ALPHA_EDGE = 32


def even(count):
    return [2 * k for k in range(count)]


# (모션, 원본 연번, [(원본 번호, 출력 이름)], 미리보기 fps)
DAMAGE = [(0, 'damage_01'), (6, 'damage_1_2'), (11, 'damage_02'), (15, 'damage_2_3'),
          (22, 'damage_03'), (26, 'damage_3_4'), (30, 'damage_04'), (35, 'damage_4_idle')]
LADDER = [(8, 'ladder_back_neutral_v2'), (0, 'ladder_back_style_02'), (2, 'ladder_back_style_03'),
          (4, 'ladder_back_style_04'), (6, 'ladder_back_style_05')]
MOTIONS = [
    ('Walk', '00051', [(n, f'walk_{i + 1:02}') for i, n in enumerate(even(39))], 40),
    ('Mining', '00047', [(n, f'mining_{i + 1:02}') for i, n in enumerate(even(23))], 20),
    ('LadderMining', '00043', [(n, f'ladder_mining_{i + 1:02}') for i, n in enumerate(even(23))], 20),
    ('JumpReady', '00015', [(n, f'jump_ready_{i + 1:02}') for i, n in enumerate(even(7))], 20),
    ('JumpRise', '00019', [(n, f'jump_rise_{i + 1:02}') for i, n in enumerate(even(23))], 24),
    ('JumpFall', '00021', [(n, f'jump_fall_{i + 1:02}') for i, n in enumerate(even(23))], 24),
    ('Knockout', '00023', [(n, f'knockout_{i + 1:02}') for i, n in enumerate(even(47))], 24),
    ('LadderBack', '00033', LADDER, 8),
    ('Damage', '00013', DAMAGE, 20),
]
EXPECTED_COUNTS = {'Idle': 1, 'Walk': 39, 'Mining': 23, 'LadderMining': 23, 'JumpReady': 7,
                   'JumpRise': 23, 'JumpFall': 23, 'Knockout': 47, 'LadderBack': 5, 'Damage': 8}
# 초록 점 잡티는 사다리 style_05가 될 0006 한 장에만 있다.
SPECKLE_SOURCES = {('LadderBack', 6)}
GREEN_MARGIN = 40


def source_path(motion, sequence, number):
    return SOURCE / motion / 'video' / f'ComfyUI_{sequence}__frames' / f'ComfyUI_{sequence}__{number:04}.png'


def remove_green_speckles(image):
    """몸과 떨어진 작은 덩어리 중 초록이 강한 것만 투명하게 지운다. 몸 안의 색은 건드리지 않는다."""
    data = np.asarray(image).copy()
    alpha = data[:, :, 3] > 0
    rgb = data[:, :, :3].astype(int)
    green = alpha & (rgb[:, :, 1] - np.maximum(rgb[:, :, 0], rgb[:, :, 2]) > GREEN_MARGIN)
    labels, count = ndimage.label(alpha, structure=np.ones((3, 3)))
    sizes = ndimage.sum(alpha, labels, range(1, count + 1))
    body = int(np.argmax(sizes)) + 1
    removed = 0
    for index in range(1, count + 1):
        if index == body:
            continue
        part = labels == index
        if green[part].sum() == 0 or green[part].mean() < 0.3:
            continue
        removed += int(part.sum())
        data[part] = 0
    remaining = int((green & (data[:, :, 3] > 0)).sum())
    return Image.fromarray(data, 'RGBA'), removed, remaining


def resize(image):
    # 투명 가장자리가 어두워지지 않게 premultiplied(RGBa)로 거른다.
    return image.convert('RGBa').resize((CANVAS, CANVAS), Image.Resampling.LANCZOS).convert('RGBA')


def save_atomic(image, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix('.png.tmp')
    image.save(temporary, format='PNG')
    if path.exists() and path.read_bytes() == temporary.read_bytes():
        temporary.unlink()
        return
    os.replace(temporary, path)


def bounds(image):
    return image.getchannel('A').point(lambda a: 255 if a >= ALPHA_EDGE else 0).getbbox()


def on_background(frame, color):
    canvas = Image.new('RGBA', frame.size, color)
    canvas.alpha_composite(frame)
    return canvas.convert('RGB')


def male_frames(motion, names):
    frames = []
    for name in names:
        path = MALE / motion / f'{name}.png'
        if path.exists():
            image = Image.open(path).convert('RGBA')
            frames.append(image if image.size == (CANVAS, CANVAS) else resize(image))
    return frames


def male_names(motion):
    if motion == 'Damage':
        return [name for _, name in DAMAGE]
    if motion == 'LadderBack':
        return [name for _, name in LADDER]
    prefix = {'Walk': 'walk', 'Mining': 'mining', 'LadderMining': 'ladder_mining', 'JumpReady': 'jump_ready',
              'JumpRise': 'jump_rise', 'JumpFall': 'jump_fall', 'Knockout': 'knockout'}[motion]
    return sorted(p.stem for p in (MALE / motion).glob(prefix + '_[0-9][0-9].png'))


def save_preview(motion, female, male, fps):
    """왼쪽 남자, 오른쪽 여자. 각자 장수대로 반복해 짧은 세트의 이음새를 볼 수 있게 한다."""
    order = list(range(len(female)))
    male_order = list(range(len(male)))
    if motion == 'LadderBack':
        order = male_order = [0, 1, 2, 1, 0, 3, 4, 3]
    length = max(len(order), len(male_order)) * 2
    pages = []
    for step in range(length):
        page = Image.new('RGB', (512, 276), '#202428')
        draw = ImageDraw.Draw(page)
        if male:
            page.paste(on_background(male[male_order[step % len(male_order)]], '#d8d8d8'), (0, 20))
        page.paste(on_background(female[order[step % len(order)]], '#d8d8d8'), (256, 20))
        draw.text((6, 4), f'male {male_order[step % len(male_order)] + 1}/{len(male)}', fill='white')
        draw.text((262, 4), f'female {order[step % len(order)] + 1}/{len(female)}', fill='white')
        # 저장소 용량을 줄이려고 절반 크기로 남긴다(rule 17).
        pages.append(page.resize((256, 138), Image.Resampling.LANCZOS))
    duration = max(20, round(1000 / fps / 10) * 10)
    pages[0].save(PREVIEW / f'{motion.lower()}-male-vs-female.gif', save_all=True,
                  append_images=pages[1:], duration=duration, loop=0, disposal=2)


def contact_sheet(motion, frames, names):
    columns = 8
    rows = (len(frames) + columns - 1) // columns
    sheet = Image.new('RGB', (columns * 128, rows * 144), '#202428')
    draw = ImageDraw.Draw(sheet)
    for i, (frame, name) in enumerate(zip(frames, names)):
        x, y = (i % columns) * 128, (i // columns) * 144
        sheet.paste(on_background(frame, '#d8d8d8').resize((128, 128), Image.Resampling.LANCZOS), (x, y + 16))
        draw.text((x + 3, y + 2), name.replace('ladder_back_', '').replace('ladder_', 'l_'), fill='white')
    sheet.save(PREVIEW / f'{motion.lower()}-sheet.png')


def main():
    PREVIEW.mkdir(parents=True, exist_ok=True)
    report = {'source': 'work_process/MVP2/female_sprite/female_ver2', 'canvas': CANVAS, 'motions': {}}

    idle_source = SOURCE / 'Idle/player_idle_01.png'
    idle = Image.open(idle_source).convert('RGBA')
    assert idle.size == (CANVAS, CANVAS), idle.size
    save_atomic(idle, OUTPUT / 'Idle/player_idle_01.png')
    report['motions']['Idle'] = {'count': 1, 'frames': [{'source': 'Idle/player_idle_01.png',
                                                         'output': 'player_idle_01'}]}

    for motion, sequence, picks, fps in MOTIONS:
        frames, records = [], []
        for number, name in picks:
            path = source_path(motion, sequence, number)
            assert path.exists(), path
            image = Image.open(path).convert('RGBA')
            assert image.size == (512, 512), (path, image.size)
            record = {'source': f'{number:04}', 'output': name}
            if (motion, number) in SPECKLE_SOURCES:
                image, removed, remaining = remove_green_speckles(image)
                assert removed > 0, removed
                record['speckle_pixels_removed_512'] = removed
                record['green_pixels_left_512'] = remaining
            frame = resize(image)
            out = OUTPUT / motion / f'{name}.png'
            save_atomic(frame, out)
            box = bounds(frame)
            record.update(bbox=list(box), foot_from_bottom=CANVAS - box[3],
                          sha256=hashlib.sha256(out.read_bytes()).hexdigest())
            frames.append(frame)
            records.append(record)
        assert len(frames) == EXPECTED_COUNTS[motion], (motion, len(frames))
        names = [name for _, name in picks]
        male = male_frames(motion, male_names(motion))
        save_preview(motion, frames, male, fps)
        contact_sheet(motion, frames, names)
        report['motions'][motion] = {'sequence': sequence, 'count': len(frames), 'male_count': len(male),
                                     'frames': records}

    for motion, count in EXPECTED_COUNTS.items():
        written = sorted((OUTPUT / motion).glob('*.png'))
        assert len(written) == count, (motion, len(written))
    (PREVIEW / 'checks.json').write_text(json.dumps(report, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    print(json.dumps({m: v['count'] for m, v in report['motions'].items()}))


if __name__ == '__main__':
    main()
