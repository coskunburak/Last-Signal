#!/usr/bin/env python3
"""Author project-owned S021 inventory silhouettes; no Unity invocation."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
INK = (235, 228, 211, 255)
SCALE = 4


def render(name, shapes, holes=()):
    canvas = Image.new('RGBA', (128 * SCALE, 128 * SCALE))
    draw = ImageDraw.Draw(canvas)
    for polygon in shapes:
        draw.polygon([(x * SCALE, y * SCALE) for x, y in polygon], fill=INK)
    for polygon in holes:
        draw.polygon([(x * SCALE, y * SCALE) for x, y in polygon], fill=(0, 0, 0, 0))
    canvas.resize((128, 128), Image.Resampling.LANCZOS).save(
        ROOT / 'Assets/LastSignal/Art/ItemIcons' / (name + '.png'))


if __name__ == '__main__':
    render('weapon.pistol', [
        [(14, 32), (108, 32), (114, 38), (112, 51), (47, 51), (26, 57), (14, 53)],
        [(24, 50), (83, 50), (80, 64), (65, 76), (44, 74), (35, 104), (14, 99)],
        [(24, 28), (34, 28), (34, 34), (24, 34)],
        [(99, 28), (104, 28), (104, 34), (99, 34)]
    ], [[(50, 56), (74, 56), (73, 63), (62, 69), (47, 68)],
        [(32, 68), (35, 69), (27, 94), (24, 93)]])
    render('weapon.blade', [
        [(38, 78), (93, 18), (110, 12), (110, 27), (102, 42), (51, 88)],
        [(38, 80), (48, 89), (24, 116), (12, 106)],
        [(33, 79), (40, 73), (56, 88), (50, 94)]
    ], [[(24, 104), (27, 101), (30, 104), (27, 107)]])
    render('weapon.axe', [
        [(20, 111), (29, 117), (92, 33), (83, 26)],
        [(61, 20), (87, 29), (99, 18), (111, 16), (117, 30), (119, 48),
         (111, 62), (91, 52), (85, 42), (72, 36), (59, 32)]
    ])
