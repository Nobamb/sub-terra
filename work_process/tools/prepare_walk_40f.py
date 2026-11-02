"""Align all B-171 drawings with one scale/translation and premultiplied resizing."""
from pathlib import Path
import hashlib
import json

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'work_process/images/player-walk-40f-20261028'


def bounds(image):
    return image.getchannel('A').point(lambda a: 255 if a >= 32 else 0).getbbox()


def main():
    source = FOLDER / 'source'
    assert {p.name for p in source.glob('*.png')} == {f'{i}.png' for i in range(1, 41)}
    images = [Image.open(source / f'{i}.png').convert('RGBA') for i in range(1, 41)]
    assert all(im.size == (960, 960) for im in images)
    boxes = [bounds(im) for im in images]
    mean_height = sum(b[3] - b[1] for b in boxes) / 40
    size = round(960 * 227 / mean_height)
    scale = size / 960
    dx = round(128 - sum((b[0] + b[2]) / 2 for b in boxes) / 40 * scale)
    dy = round(240 - sum(b[3] for b in boxes) / 40 * scale)
    output = FOLDER / 'runtime-frames'
    output.mkdir(exist_ok=True)
    records, runtime = [], []
    for i, (image, box) in enumerate(zip(images, boxes), 1):
        image.putalpha(image.getchannel('A').point(lambda a: 255 if a >= 254 else a))
        # RGBa explicitly premultiplies RGB before filtering; unpremultiply afterward.
        resized = image.convert('RGBa').resize((size, size), Image.Resampling.LANCZOS).convert('RGBA')
        frame = Image.new('RGBA', (256, 256))
        frame.alpha_composite(resized, (dx, dy))
        left, top, right, bottom = bounds(frame)
        assert 14 <= 256 - bottom <= 18, (i, 'ground', bottom)
        assert 218 <= bottom - top <= 236, (i, 'height', bottom - top)
        assert left > 4 and right - 1 < 251, (i, 'clipped')
        path = output / f'walk-{i:02}.png'
        frame.save(path)
        runtime.append(frame)
        records.append(dict(frame=i, source_height=box[3] - box[1], source_foot_y=box[3] - 1,
                            source_center_x=(box[0] + box[2]) / 2, height=bottom - top,
                            foot_from_bottom=256 - bottom, center_x=(left + right) / 2,
                            left=left, right=right - 1, sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    assert len({r['sha256'] for r in records}) == 40
    report = dict(frame_count=40, fps=40, duration_seconds=1, alpha_threshold=32,
                  scale=scale, resized_size=size, translation=[dx, dy], frames=records)
    (FOLDER / 'checks.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    sheet = Image.new('RGB', (1280, 1200))
    draw = ImageDraw.Draw(sheet)
    for i, frame in enumerate(runtime):
        x, y = (i % 10) * 128, (i // 10) * 300
        for offset, color in [(0, '#eeeeee'), (150, '#181818')]:
            canvas = Image.new('RGBA', (256, 256), color)
            canvas.alpha_composite(frame)
            sheet.paste(canvas.convert('RGB').resize((128, 128), Image.Resampling.LANCZOS), (x, y + offset + 22))
            draw.text((x + 4, y + offset + 4), str(i + 1), fill='white')
    sheet.save(FOLDER / 'all-frames-light-dark.png')
    contacts = Image.new('RGB', (1024, 512), '#eeeeee')
    for j, i in enumerate([40, 1, 2, 3, 20, 21, 22, 23]):
        canvas = Image.new('RGBA', (256, 256), '#eeeeee')
        canvas.alpha_composite(runtime[i - 1])
        contacts.paste(canvas.convert('RGB'), ((j % 4) * 256, (j // 4) * 256))
        ImageDraw.Draw(contacts).text(((j % 4) * 256 + 8, (j // 4) * 256 + 8), str(i), fill='black')
    contacts.save(FOLDER / 'contacts-and-loop.png')
    preview = []
    for frame in runtime:
        canvas = Image.new('RGB', (512, 256))
        for x, color in [(0, '#eeeeee'), (256, '#181818')]:
            background = Image.new('RGBA', (256, 256), color)
            background.alpha_composite(frame)
            canvas.paste(background.convert('RGB'), (x, 0))
        preview.append(canvas)
    # GIF timestamps use 10ms units: alternate 20/30ms to preserve the 1s period.
    preview[0].save(FOLDER / 'walk-1s-preview.gif', save_all=True, append_images=preview[1:],
                    duration=[20, 30] * 20, loop=0, disposal=2)
    print(json.dumps({k: v for k, v in report.items() if k != 'frames'}))
    print('Height:', min(r['height'] for r in records), max(r['height'] for r in records),
          'feet:', min(r['foot_from_bottom'] for r in records), max(r['foot_from_bottom'] for r in records))


if __name__ == '__main__':
    main()
