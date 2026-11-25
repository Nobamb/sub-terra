"""B-210: 남/여 새 모션 4종(가스 피해·사다리 오르기·사다리 피격·사다리 채굴)을 런타임 프레임으로 복사한다.

- 원본: work_process/images/player-new-motions-20261124/source/{new_motion_nukki,female_new_nukki}/<모션>/frames/0001.png…
  (PM이 누끼·잡티 정리를 끝낸 256×256 투명 RGBA. zip을 그 자리에 풀어 둔다.)
- 크로마키·배경 제거·크기/위치 보정은 하지 않는다. 바이트 그대로 복사한다(발 위치가 기존 256 그림과 맞음).
- 여자 폴더의 webm·sheet.png는 읽지 않는다.
- 미리보기 GIF·시트·checks.json을 work_process/images/player-new-motions-20261124/에 남긴다.
"""
from pathlib import Path
import hashlib
import json
import os
import shutil
import sys

from PIL import Image, ImageDraw

sys.dont_write_bytecode = True

ROOT = Path(__file__).resolve().parents[2]
WORK = ROOT / 'work_process/images/player-new-motions-20261124'
SOURCE = WORK / 'source'
GAME = ROOT / 'sub-terra/Assets/_Project/Art/Characters/Player'
CANVAS = 256
ALPHA_EDGE = 32

# (원본 모션 폴더, 출력 폴더, 출력 이름 앞부분, 미리보기 fps)
MOTIONS = [
    ('damage_new', 'GasDamage', 'gas_damage', 85),      # 0.4초에 34장
    ('new_ladder', 'LadderClimb', 'ladder_climb', 16),  # 걷는 속도 기준 한 바퀴 약 2초
    ('ladder_hit', 'LadderHit', 'ladder_hit', 62),      # 0.4초에 25장
    ('laddermining', 'LadderMiningV2', 'ladder_mining_v2', 20),
]
SETS = [
    ('male', SOURCE / 'new_motion_nukki', GAME / 'Frames',
     {'damage_new': 34, 'new_ladder': 32, 'ladder_hit': 25, 'laddermining': 25}),
    ('female', SOURCE / 'female_new_nukki', GAME / 'Female/Frames',
     {'damage_new': 34, 'new_ladder': 25, 'ladder_hit': 25, 'laddermining': 25}),
]


def copy_atomic(source, target):
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists() and target.read_bytes() == source.read_bytes():
        return
    temporary = target.with_suffix('.png.tmp')
    shutil.copyfile(source, temporary)
    os.replace(temporary, target)


def bounds(image):
    return image.getchannel('A').point(lambda a: 255 if a >= ALPHA_EDGE else 0).getbbox()


def on_background(frame, color='#d8d8d8'):
    canvas = Image.new('RGBA', frame.size, color)
    canvas.alpha_composite(frame)
    return canvas.convert('RGB')


def contact_sheet(path, frames, label):
    columns = 8
    rows = (len(frames) + columns - 1) // columns
    sheet = Image.new('RGB', (columns * 128, rows * 144), '#202428')
    draw = ImageDraw.Draw(sheet)
    for i, frame in enumerate(frames):
        x, y = (i % columns) * 128, (i // columns) * 144
        sheet.paste(on_background(frame).resize((128, 128), Image.Resampling.LANCZOS), (x, y + 16))
        draw.text((x + 3, y + 2), f'{label} {i + 1}', fill='white')
    sheet.save(path)


def preview(path, male, female, fps):
    """왼쪽 남자, 오른쪽 여자. 각자 장수대로 반복한다. 용량 때문에 절반 크기."""
    length = max(len(male), len(female)) * 2
    pages = []
    for step in range(length):
        page = Image.new('RGB', (512, 276), '#202428')
        draw = ImageDraw.Draw(page)
        m, f = step % len(male), step % len(female)
        page.paste(on_background(male[m]), (0, 20))
        page.paste(on_background(female[f]), (256, 20))
        draw.text((6, 4), f'male {m + 1}/{len(male)}', fill='white')
        draw.text((262, 4), f'female {f + 1}/{len(female)}', fill='white')
        pages.append(page.resize((256, 138), Image.Resampling.LANCZOS))
    duration = max(20, round(1000 / fps / 10) * 10)
    pages[0].save(path, save_all=True, append_images=pages[1:], duration=duration, loop=0, disposal=2)


def main():
    WORK.mkdir(parents=True, exist_ok=True)
    report = {'canvas': CANVAS, 'sets': {}}
    loaded = {}
    for gender, source_root, output_root, counts in SETS:
        report['sets'][gender] = {}
        for motion, folder, prefix, _ in MOTIONS:
            sources = sorted((source_root / motion / 'frames').glob('*.png'))
            assert len(sources) == counts[motion], (gender, motion, len(sources))
            frames, records = [], []
            for index, source in enumerate(sources, start=1):
                assert source.name == f'{index:04}.png', source
                image = Image.open(source)
                assert image.size == (CANVAS, CANVAS) and image.mode == 'RGBA', (source, image.size, image.mode)
                target = output_root / folder / f'{prefix}_{index:02}.png'
                copy_atomic(source, target)
                frame = image.convert('RGBA')
                box = bounds(frame)
                records.append({'source': source.name, 'output': target.name, 'bbox': list(box),
                                'foot_from_bottom': CANVAS - box[3],
                                'sha256': hashlib.sha256(target.read_bytes()).hexdigest()})
                frames.append(frame)
            written = sorted((output_root / folder).glob(prefix + '_[0-9][0-9].png'))
            assert len(written) == counts[motion], (gender, folder, len(written))
            loaded[(gender, motion)] = frames
            contact_sheet(WORK / f'{folder.lower()}-{gender}-sheet.png', frames, prefix.replace('ladder_', 'l_'))
            report['sets'][gender][folder] = {'source': f'{source_root.name}/{motion}', 'count': len(frames),
                                              'frames': records}

    for motion, folder, _, fps in MOTIONS:
        preview(WORK / f'{folder.lower()}-male-vs-female.gif', loaded[('male', motion)],
                loaded[('female', motion)], fps)

    (WORK / 'checks.json').write_text(json.dumps(report, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    print(json.dumps({g: {m: v['count'] for m, v in s.items()} for g, s in report['sets'].items()}))


if __name__ == '__main__':
    main()
