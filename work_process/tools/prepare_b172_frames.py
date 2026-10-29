"""Prepare the B-172 character drawings with one scale and translation per action."""
from pathlib import Path
import hashlib
import json

import numpy as np
from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
WALK = ROOT / 'work_process/images/player-walk-40f-20261028'
KNOCKOUT = ROOT / 'work_process/images/player-knockout-48f-20261029'
MINING = ROOT / 'work_process/images/player-mining-24f-20261029'
LADDER = ROOT / 'work_process/images/player-ladder-5f-20261029'
GAME_LADDER = ROOT / 'sub-terra/Assets/_Project/Art/Characters/Player/Frames/LadderBack'
ALPHA_THRESHOLD = 32
WALK_SCALE = 0.307


def alpha_bounds(image):
    return image.getchannel('A').point(lambda a: 255 if a >= ALPHA_THRESHOLD else 0).getbbox()


def solid_bounds(image):
    alpha = np.asarray(image.getchannel('A'))
    ys, xs = np.where(alpha >= 254)
    if len(xs) == 0:
        raise ValueError('No fully opaque sprite pixels')
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def helmet_bounds(image):
    """Measure the orange helmet, excluding the lamp and lower orange body details."""
    data = np.asarray(image.convert('RGBA'))
    alpha = data[:, :, 3]
    all_bounds = alpha_bounds(image)
    x0, y0, x1, y1 = all_bounds
    upper = np.zeros(alpha.shape, dtype=bool)
    # Measure the orange crown shell through its base, before the orange brim/strap.
    upper[y0:y0 + round((y1 - y0) * 0.22), x0:x1] = True
    r, g, b = [data[:, :, i].astype(np.int16) for i in range(3)]
    # The headlamp lens is the rightmost near-white patch; cap the orange mask
    # just before it so the lens and its orange bezel do not inflate the helmet.
    yy, xx = np.indices(alpha.shape)
    light = (upper & (alpha >= 128) & (r >= 220) & (g >= 210) & (b >= 175) &
             (r - b < 95) & (xx >= x0 + round((x1 - x0) * 0.85)) &
             (xx < x1))
    light_y, light_x = np.where(light)
    if len(light_x) < 10:
        raise ValueError('Could not locate the helmet lamp lens for exclusion')
    x_cap = int(light_x.min()) - 8
    mask = (upper & (xx < x_cap) & (alpha >= 128) & (r >= 105) & (r > g * 1.22) &
            (g > b * 1.18) & (g >= 38) & (g <= 205))
    ys, xs = np.where(mask)
    if len(xs) < 20:
        raise ValueError('Could not isolate the orange helmet for measurement')
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def load_sequence(folder, count, size):
    source = folder / 'source'
    aliases = {'13(minig-process).png': '13.png', '14(mining-complete).png': '14.png'}
    for old_name, new_name in aliases.items():
        old = source / old_name
        new = source / new_name
        if old.exists():
            if new.exists():
                raise FileExistsError(f'Both {old.name} and {new.name} exist; will not overwrite either')
            old.rename(new)
    expected = {f'{i}.png' for i in range(1, count + 1)}
    found = {path.name for path in source.glob('*.png')}
    if found != expected:
        raise FileNotFoundError(f'{source}: missing={sorted(expected-found)}, extra={sorted(found-expected)}')
    images = [Image.open(source / f'{i}.png').convert('RGBA') for i in range(1, count + 1)]
    if any(image.size != size for image in images):
        raise ValueError(f'{source}: expected {size}, got {[image.size for image in images]}')
    return images


def measure(image, include_helmet=True):
    x0, y0, x1, y1 = alpha_bounds(image)
    result = {'bounds': [x0, y0, x1, y1], 'height': y1 - y0,
              'foot_y': y1 - 1, 'center_x': (x0 + x1) / 2}
    if include_helmet:
        hx0, hy0, hx1, hy1 = helmet_bounds(image)
        result.update({'helmet_bounds': [hx0, hy0, hx1, hy1], 'helmet_width': hx1 - hx0})
    return result


def render_sequence(images, scale, canvas, foot_y, center_x, prefix, output,
                    anchor_bounds=None):
    resized_size = round(images[0].width * scale)
    scaled = []
    for image in images:
        # Pillow's RGBa mode filters premultiplied color to avoid dark alpha fringes.
        rgba = image.convert('RGBa').resize((resized_size, round(image.height * scale)),
                                            Image.Resampling.LANCZOS).convert('RGBA')
        scaled.append(rgba)
    # Align from the resized image's own bounds so resampling rounding cannot shift
    # the first frame away from the existing walk/ladder reference.
    first_bounds = alpha_bounds(scaled[0]) if anchor_bounds is None else solid_bounds(scaled[0])
    anchor_center_x = (first_bounds[0] + first_bounds[2]) / 2
    dx = round(center_x - anchor_center_x)
    dy = round(foot_y - first_bounds[3])
    output.mkdir(parents=True, exist_ok=True)
    frames, records = [], []
    for index, (source, resized) in enumerate(zip(images, scaled), 1):
        frame = Image.new('RGBA', canvas)
        frame.alpha_composite(resized, (dx, dy))
        bounds = alpha_bounds(frame)
        if bounds is None:
            raise ValueError(f'{prefix} {index}: empty output')
        x0, y0, x1, y1 = bounds
        opaque = solid_bounds(frame)
        clearances = {'left': x0, 'top': y0, 'right': canvas[0] - x1,
                      'bottom': canvas[1] - y1}
        path = output / f'{prefix}-{index:02}.png'
        frame.save(path)
        records.append({'frame': index, 'source': measure(source, include_helmet=False),
                        'output_bounds': list(bounds), 'output_height': y1-y0,
                        'foot_from_bottom': canvas[1]-y1,
                        'center_x': (x0+x1)/2, 'edge_clearance': clearances,
                        'opaque_bounds': list(opaque),
                        'opaque_height': opaque[3] - opaque[1],
                        'opaque_center_x': (opaque[0] + opaque[2]) / 2,
                        'opaque_foot_y': opaque[3] - 1,
                        'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
        frames.append(frame)
    return frames, {'scale': scale, 'resized_width': resized_size,
                    'translation': [dx, dy], 'canvas': list(canvas),
                    'frames': records}


def sheet(frames, path, columns, cell_size, backgrounds=('#eeeeee', '#181818')):
    rows = (len(frames) + columns - 1) // columns
    row_height = cell_size[1] * len(backgrounds)
    result = Image.new('RGB', (columns * cell_size[0], rows * row_height), '#888888')
    draw = ImageDraw.Draw(result)
    for i, frame in enumerate(frames):
        col, row = i % columns, i // columns
        for j, color in enumerate(backgrounds):
            bg = Image.new('RGBA', frame.size, color)
            bg.alpha_composite(frame)
            thumb = bg.convert('RGB').resize(cell_size, Image.Resampling.LANCZOS)
            x, y = col * cell_size[0], row * row_height + j * cell_size[1]
            result.paste(thumb, (x, y))
            draw.text((x + 4, y + 3), str(i + 1), fill='yellow' if j else 'black')
    result.save(path)


def save_gif(frames, path, duration_ms):
    rgb_frames = []
    for frame in frames:
        background = Image.new('RGBA', frame.size, '#252a30')
        background.alpha_composite(frame)
        rgb_frames.append(background.convert('RGB').quantize(colors=255, dither=Image.Dither.NONE))
    rgb_frames[0].save(path, save_all=True, append_images=rgb_frames[1:],
                       duration=duration_ms, loop=0, disposal=2)


def make_walk_comparison(walk_frame, action_frames, indices, path):
    chosen = [('walk-01', walk_frame)] + [(f'{i+1:02}', action_frames[i]) for i in indices]
    tile = 256
    result = Image.new('RGB', (tile * len(chosen), tile + 28), '#eeeeee')
    draw = ImageDraw.Draw(result)
    for col, (label, frame) in enumerate(chosen):
        bg = Image.new('RGBA', frame.size, '#eeeeee')
        bg.alpha_composite(frame)
        result.paste(bg.convert('RGB'), (col * tile, 28))
        draw.text((col * tile + 5, 6), label, fill='black')
    result.save(path)


def main():
    walk = Image.open(WALK / 'source/1.png').convert('RGBA')
    walk_measure = measure(walk)
    knockout = load_sequence(KNOCKOUT, 48, (960, 960))
    mining = load_sequence(MINING, 24, (960, 960))
    ladder = load_sequence(LADDER, 5, (1229, 1280))

    source_measures = {'knockout': measure(knockout[0]), 'mining': measure(mining[0])}
    individual_scales = {
        key: WALK_SCALE * walk_measure['helmet_width'] / value['helmet_width']
        for key, value in source_measures.items()
    }
    sheet(knockout, KNOCKOUT / 'source-contact-sheet.png', 8, (120, 120))
    sheet(mining, MINING / 'source-contact-sheet.png', 6, (160, 160))
    sheet(ladder, LADDER / 'source-contact-sheet.png', 5, (180, 180))
    old_ladder_names = ['ladder_back_neutral_v2.png'] + [f'ladder_back_style_{i:02}.png' for i in range(2, 6)]
    old_ladder_frames = [Image.open(GAME_LADDER / name).convert('RGBA') for name in old_ladder_names]
    comparison = Image.new('RGB', (5 * 210, 2 * 240), '#888888')
    comparison_draw = ImageDraw.Draw(comparison)
    for row, group in enumerate((old_ladder_frames, ladder)):
        for col, frame in enumerate(group):
            bg = Image.new('RGBA', frame.size, '#eeeeee')
            bg.alpha_composite(frame)
            thumb = bg.convert('RGB').resize((200, 200), Image.Resampling.LANCZOS)
            comparison.paste(thumb, (col * 210 + 5, row * 240 + 28))
            comparison_draw.text((col * 210 + 8, row * 240 + 5),
                                 ('old ' if row == 0 else 'new ') + str(col + 1), fill='black')
    comparison.save(LADDER / 'old-new-ladder-source.png')
    helmet_sheet = Image.new('RGB', (3 * 320, 360), '#dddddd')
    helmet_draw = ImageDraw.Draw(helmet_sheet)
    for col, (name, image) in enumerate((('walk', walk), ('knockout', knockout[0]), ('mining', mining[0]))):
        fitted = image.resize((320, 320), Image.Resampling.LANCZOS)
        helmet_sheet.paste(fitted, (col * 320, 35), fitted)
        box = helmet_bounds(image)
        scaled_box = (col * 320 + round(box[0] * 320 / image.width),
                      35 + round(box[1] * 320 / image.height),
                      col * 320 + round(box[2] * 320 / image.width),
                      35 + round(box[3] * 320 / image.height))
        helmet_draw.rectangle(scaled_box, outline='magenta', width=2)
        helmet_draw.text((col * 320 + 4, 4), f'{name}: {box}; width={box[2]-box[0]}', fill='black')
    helmet_sheet.save(KNOCKOUT / 'helmet-measurement-contact-sheet.png')
    if max(individual_scales.values()) / min(individual_scales.values()) > 1.10:
        raise ValueError(f'Action scales differ by >10%; needs a visual decision: {individual_scales}')
    # Keep the walk cycle's original 16px foot inset and shared body center.
    walk_foot_y = 240
    common_center_x = 127.5
    canvas = (256, 256)
    action_outputs = {}
    for key, images, folder, prefix in [
            ('knockout', knockout, KNOCKOUT, 'knockout'),
            ('mining', mining, MINING, 'mining')]:
        frames, report = render_sequence(images, individual_scales[key], canvas,
                                         walk_foot_y, common_center_x, prefix,
                                         folder / 'runtime-frames')
        action_outputs[key] = (frames, report)

    ladder_target = Image.open(GAME_LADDER / 'ladder_back_neutral_v2.png').convert('RGBA')
    ladder_target_bounds = solid_bounds(ladder_target)
    ladder_source = measure(ladder[0])
    ladder_source_solid_bounds = solid_bounds(ladder[0])
    ladder_target_height = ladder_target_bounds[3] - ladder_target_bounds[1]
    ladder_source_height = ladder_source_solid_bounds[3] - ladder_source_solid_bounds[1]
    ladder_scale = ladder_target_height / ladder_source_height
    ladder_canvas = (1254, 1254)
    ladder_target_foot_y = ladder_target_bounds[3]
    ladder_target_center_x = (ladder_target_bounds[0] + ladder_target_bounds[2]) / 2
    ladder_frames, ladder_report = render_sequence(
        ladder, ladder_scale, ladder_canvas, ladder_target_foot_y,
        ladder_target_center_x, 'ladder', LADDER / 'runtime-frames',
        anchor_bounds=ladder_source_solid_bounds)

    # The single known stray mark is removed from ladder frame 5 only.
    fifth_source = ladder[4]
    # Detect the small isolated source mark in the specified top-left corner.
    cleaned = fifth_source.copy()
    source_alpha = np.asarray(cleaned.getchannel('A'))
    mark_y, mark_x = np.where(source_alpha[:48, :48] > 0)
    if len(mark_x) == 0:
        raise ValueError('Expected the specified top-left mark in ladder frame 5')
    dot_source_box = (int(mark_x.min()), int(mark_y.min()),
                      int(mark_x.max()) + 1, int(mark_y.max()) + 1)
    ImageDraw.Draw(cleaned).rectangle(dot_source_box, fill=(0, 0, 0, 0))
    clean_resized = cleaned.convert('RGBa').resize(
        (round(cleaned.width * ladder_scale), round(cleaned.height * ladder_scale)),
        Image.Resampling.LANCZOS).convert('RGBA')
    fifth = Image.new('RGBA', ladder_canvas)
    fifth.alpha_composite(clean_resized,
                          (ladder_report['translation'][0], ladder_report['translation'][1]))
    ladder_frames[4] = fifth
    fifth_path = LADDER / 'runtime-frames/ladder-05.png'
    fifth.save(fifth_path)
    ladder_report['frames'][4]['removed_source_mark_bbox'] = list(dot_source_box)
    ladder_report['frames'][4]['sha256'] = hashlib.sha256(fifth_path.read_bytes()).hexdigest()
    ladder_report['frames'][4]['output_bounds'] = list(alpha_bounds(fifth))
    final_bounds = alpha_bounds(fifth)
    final_solid_bounds = solid_bounds(fifth)
    ladder_report['frames'][4]['source'] = measure(cleaned, include_helmet=False)
    ladder_report['frames'][4]['output_height'] = final_bounds[3] - final_bounds[1]
    ladder_report['frames'][4]['foot_from_bottom'] = ladder_canvas[1] - final_bounds[3]
    ladder_report['frames'][4]['center_x'] = (final_bounds[0] + final_bounds[2]) / 2
    ladder_report['frames'][4]['opaque_bounds'] = list(final_solid_bounds)
    ladder_report['frames'][4]['opaque_height'] = final_solid_bounds[3] - final_solid_bounds[1]
    ladder_report['frames'][4]['opaque_center_x'] = (final_solid_bounds[0] + final_solid_bounds[2]) / 2
    ladder_report['frames'][4]['opaque_foot_y'] = final_solid_bounds[3] - 1
    ladder_report['frames'][4]['edge_clearance'] = {
        'left': alpha_bounds(fifth)[0], 'top': alpha_bounds(fifth)[1],
        'right': ladder_canvas[0] - alpha_bounds(fifth)[2],
        'bottom': ladder_canvas[1] - alpha_bounds(fifth)[3]}

    all_reports = {
        'alpha_threshold': ALPHA_THRESHOLD,
        'walk_reference': walk_measure,
        'helmet_scale_formula_coefficient': WALK_SCALE,
        'walk_asset_actual_scale': 296 / 960,
        'helmet_derived_action_scales': individual_scales,
        'scale_choice': 'Each action uses its own helmet-derived scale so the measured orange crown width matches the walk reference when changing actions.',
        'source_numbering': {'knockout': '1.png through 48.png',
                             'mining': '1.png through 24.png; 13.png and 14.png were already numeric',
                             'ladder': '1.png through 5.png'},
        'action_ppu_recommendation': 256,
        'action_canvas': list(canvas),
        'action_foot_baseline_y': walk_foot_y,
        'action_center_x': common_center_x,
        'action_pivot_recommendation_normalized': [0.5, 0.5],
        'walk_runtime_reference': {'scale': 296 / 960, 'foot_from_bottom': 16,
                                   'center_x': 127.5, 'ppu': 256,
                                   'pivot_recommendation_normalized': [0.5, 0.5]},
        'actions': {key: report for key, (_, report) in action_outputs.items()},
        'ladder': {'target_file': str(GAME_LADDER / 'ladder_back_neutral_v2.png'),
                   'canvas': [1254, 1254],
                   'ppu_recommendation': 1084,
                   'existing_frames': [
                       {'file': name, 'opaque_bounds': list(solid_bounds(image)),
                        'height': solid_bounds(image)[3] - solid_bounds(image)[1],
                        'center_x': (solid_bounds(image)[0] + solid_bounds(image)[2]) / 2,
                        'foot_y': solid_bounds(image)[3] - 1}
                       for name, image in zip(old_ladder_names, old_ladder_frames)],
                   'target_bounds': list(ladder_target_bounds),
                   'source_frame_1_solid_bounds': list(ladder_source_solid_bounds),
                   'source_key_height': ladder_source_height,
                   'target_height': ladder_target_height,
                   'target_foot_y': ladder_target_foot_y,
                   'target_center_x': ladder_target_center_x,
                   'scale': ladder_scale,
                   'pivot_recommendation_normalized': [0.5, 0.5],
                   **ladder_report},
    }

    # Validate full frame counts and transparent margins before recording the final JSON.
    for key, (frames, report) in action_outputs.items():
        assert len(frames) == (48 if key == 'knockout' else 24)
        for record in report['frames']:
            assert min(record['edge_clearance'].values()) >= 2, (key, record['frame'], record['edge_clearance'])
    assert len(ladder_frames) == 5
    for record in ladder_report['frames']:
        assert min(record['edge_clearance'].values()) >= 2, ('ladder', record['frame'], record['edge_clearance'])

    walk_runtime = Image.open(WALK / 'runtime-frames/walk-01.png').convert('RGBA')
    for key, (frames, _) in action_outputs.items():
        folder = KNOCKOUT if key == 'knockout' else MINING
        sheet(frames, folder / 'comparison-contact-sheet.png', 8 if key == 'knockout' else 6,
              (128, 128))
        selected = [0, 17, 35 if key == 'knockout' else 13, len(frames) - 1]
        make_walk_comparison(walk_runtime, frames, selected,
                             folder / 'walk-comparison-contact-sheet.png')
        save_gif(frames, folder / 'runtime-preview.gif', 42 if key == 'knockout' else 100)
    sheet(ladder_frames, LADDER / 'comparison-contact-sheet.png', 5, (180, 180))
    ladder_comparison = Image.new('RGB', (5 * 210, 2 * 240), '#888888')
    ladder_comparison_draw = ImageDraw.Draw(ladder_comparison)
    for row, group in enumerate((old_ladder_frames, ladder_frames)):
        for col, frame in enumerate(group):
            bg = Image.new('RGBA', frame.size, '#eeeeee')
            bg.alpha_composite(frame)
            thumb = bg.convert('RGB').resize((200, 200), Image.Resampling.LANCZOS)
            ladder_comparison.paste(thumb, (col * 210 + 5, row * 240 + 28))
            ladder_comparison_draw.text((col * 210 + 8, row * 240 + 5),
                                        ('old ' if row == 0 else 'new ') + str(col + 1), fill='black')
    ladder_comparison.save(LADDER / 'old-new-ladder-contact-sheet.png')
    for key, (_, report) in action_outputs.items():
        all_reports['actions'][key]['frames'] = report['frames']
    (KNOCKOUT / 'measurements.json').write_text(json.dumps(all_reports, indent=2) + '\n', encoding='utf-8')
    (MINING / 'measurements.json').write_text(json.dumps(all_reports, indent=2) + '\n', encoding='utf-8')
    (LADDER / 'measurements.json').write_text(json.dumps(all_reports, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'helmet_widths': {'walk': walk_measure['helmet_width'],
                                        **{k: v['helmet_width'] for k, v in source_measures.items()}},
                      'scales': individual_scales,
                      'ladder_scale': ladder_scale,
                      'action_canvases': {k: v[1]['canvas'] for k, v in action_outputs.items()},
                      'ladder_translation': ladder_report['translation']}, indent=2))


if __name__ == '__main__':
    main()
