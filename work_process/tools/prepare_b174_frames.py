"""Align the approved B-174 drawings with one scale and translation for all frames."""
from pathlib import Path
import json
import sys
import numpy as np
from PIL import Image, ImageDraw
sys.dont_write_bytecode = True
from prepare_b172_frames import alpha_bounds, load_sequence, render_sequence, sheet, save_gif

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'work_process/images/player-ladder-mining-24f-20261029'


def crown_width(image, top, bottom, right):
    # Fixed crown-only measurement regions exclude brim, lamp and orange drill.
    data = np.asarray(image).astype(float)
    r, g, b, a = [data[:, :, i] for i in range(4)]
    yy, xx = np.indices(a.shape)
    mask = ((yy >= top) & (yy < bottom) & (xx < right) & (a >= 128)
            & (r >= 105) & (r > g * 1.22) & (g > b * 1.18) & (g >= 38) & (g <= 205))
    y, x = np.where(mask)
    return [int(x.min()), int(y.min()), int(x.max()) + 1, int(y.max()) + 1]


def main():
    images = load_sequence(FOLDER, 24, (960, 960))
    ladder = Image.open(ROOT / 'work_process/images/player-ladder-5f-20261029/source/1.png').convert('RGBA')
    source_box = crown_width(images[0], 0, 150, 610)
    ladder_box = crown_width(ladder, 62, 250, 820)
    source_width = source_box[2] - source_box[0]
    ladder_width = ladder_box[2] - ladder_box[0]
    ladder_scale = 0.8248048568950563
    scale = ladder_width * ladder_scale / 1084 * 256 / source_width
    # The helmet/body axis, rather than the drill-inclusive silhouette center.
    body_center = (source_box[0] + source_box[2]) / 2
    scaled_width = round(960 * scale)
    actual_scale = scaled_width / 960
    scaled_first = images[0].convert('RGBa').resize((scaled_width, scaled_width), Image.Resampling.LANCZOS).convert('RGBA')
    # render_sequence uses first silhouette center; compensate once for the drill.
    bounds = alpha_bounds(scaled_first)
    target_center = 128 + (bounds[0] + bounds[2]) / 2 - body_center * actual_scale
    frames, report = render_sequence(images, scale, (256, 256), 240, target_center,
                                    'ladder-mining', FOLDER / 'runtime-frames')
    report.update(source_helmet_bounds=source_box, source_helmet_width=source_width,
                  ladder_helmet_bounds=ladder_box, ladder_source_helmet_width=ladder_width,
                  ladder_scale=ladder_scale, source_body_center=body_center,
                  actual_scale=actual_scale, output_helmet_width=source_width * actual_scale,
                  reference_helmet_width=ladder_width * ladder_scale / 1084 * 256)
    for record in report['frames']:
        assert 14 <= record['foot_from_bottom'] <= 18, record
        assert min(record['edge_clearance'].values()) >= 2, record
    (FOLDER / 'measurements.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    sheet(frames, FOLDER / 'comparison-contact-sheet.png', 6, (128, 128))
    save_gif(frames, FOLDER / 'runtime-preview.gif', 42)
    reference = Image.open(ROOT / 'work_process/images/player-ladder-5f-20261029/runtime-frames/ladder-01.png').convert('RGBA')
    size = round(reference.width / 1084 * 256)
    scaled_reference = reference.convert('RGBa').resize((size, size), Image.Resampling.LANCZOS).convert('RGBA')
    reference_frame = Image.new('RGBA', (256, 256))
    reference_frame.alpha_composite(scaled_reference, ((256-size)//2, (256-size)//2))
    sheet([reference_frame] + [frames[i] for i in [0, 11, 12, 15, 16, 23]],
          FOLDER / 'ladder-comparison.png', 7, (192, 192))
    measurement = Image.new('RGB', (960, 480), '#eeeeee')
    draw = ImageDraw.Draw(measurement)
    for i, (img, box) in enumerate([(images[0], source_box), (ladder, ladder_box)]):
        s = 480 / img.width
        thumb = img.resize((480, round(img.height*s)), Image.Resampling.LANCZOS)
        measurement.paste(thumb, (i*480, 0), thumb)
        draw.rectangle((i*480+box[0]*s, box[1]*s, i*480+box[2]*s, box[3]*s), outline='magenta', width=2)
        draw.text((i*480+10, 450), f'crown width: {box[2]-box[0]} px', fill='black')
    measurement.save(FOLDER / 'helmet-measurement.png')
    print(json.dumps({k: report[k] for k in ['source_helmet_width', 'ladder_source_helmet_width', 'scale', 'actual_scale', 'translation', 'source_body_center', 'output_helmet_width', 'reference_helmet_width']}))


if __name__ == '__main__':
    main()
