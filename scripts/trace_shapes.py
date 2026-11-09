"""Writes Content/phonics/trace_shapes.json: where Letter Trace's checkpoints sit on each letter.

Letter Trace used one ring of checkpoints for every letter, so a narrow letter (i, j, l) could never be finished and
letters drawn in several strokes or back over themselves (a, d, j with its dot) didn't follow the letter at all
(owner, 9 Nov 2026). This draws each grapheme in the game's own letter font (Atkinson Hyperlegible Bold, as
Sketch.Letter draws it), thins the ink down to its centre lines and spaces checkpoints along them, so the child
traces the letter itself, in any order, lifting the pen as often as they like (the dot of i and j is its own
checkpoint).

Points are in ems from the centre of the letter's label box (Sketch.Label centres the text's advance width and
the font's line height on the letter's position): the game multiplies them by the font size it draws at.

  python3 scripts/trace_shapes.py      # after changing the graphemes or the font; validate_content.py checks it
"""
import json, sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
from trace_source import FONT, ROOT, SIZE, SPACING, graphemes, source_hash

OUT = ROOT / "Content" / "phonics" / "trace_shapes.json"


def thin(mask):
    """Zhang-Suen thinning: the ink's one-pixel centre lines."""
    img = np.pad(mask.astype(np.uint8), 1)
    changed = True
    while changed:
        changed = False
        for step in (0, 1):
            p = img
            p2, p3, p4 = p[:-2, 1:-1], p[:-2, 2:], p[1:-1, 2:]
            p5, p6, p7 = p[2:, 2:], p[2:, 1:-1], p[2:, :-2]
            p8, p9 = p[1:-1, :-2], p[:-2, :-2]
            ring = [p2, p3, p4, p5, p6, p7, p8, p9, p2]
            b = sum(ring[:8])
            a = sum(((ring[i] == 0) & (ring[i + 1] == 1)).astype(np.uint8) for i in range(8))
            if step == 0:
                c = p2 * p4 * p6
                d = p4 * p6 * p8
            else:
                c = p2 * p4 * p8
                d = p2 * p6 * p8
            centre = p[1:-1, 1:-1]
            remove = (centre == 1) & (b >= 2) & (b <= 6) & (a == 1) & (c == 0) & (d == 0)
            if remove.any():
                changed = True
                img = img.copy()
                img[1:-1, 1:-1][remove] = 0
    return img[1:-1, 1:-1].astype(bool)


def shape(text, font):
    ascent, descent = font.getmetrics()
    width = font.getlength(text)
    height = ascent + descent
    pad = SIZE // 2
    image = Image.new("L", (int(width) + 2 * pad, height + 2 * pad), 0)
    # Each letter at its place in the line; the hyphen of a split digraph (a-e) is a gap, not a stroke to trace.
    draw = ImageDraw.Draw(image)
    for i, ch in enumerate(text):
        if ch != "-":
            draw.text((pad + font.getlength(text[:i]), pad + ascent), ch, font=font, fill=255, anchor="ls")
    ink = np.array(image) > 127
    lines = thin(ink)
    ys, xs = np.nonzero(lines)
    # Space points along the centre lines: take the line pixels in reading order (top to bottom, left to right) and
    # keep each one at least SPACING from every point already kept.
    chosen = []
    gap = SPACING * SIZE
    order = np.lexsort((xs, ys))
    for i in order:
        x, y = xs[i], ys[i]
        if all((x - cx) ** 2 + (y - cy) ** 2 >= gap * gap for cx, cy in chosen):
            chosen.append((x, y))
    centre_x, centre_y = pad + width / 2, pad + height / 2
    points = [[round((x - centre_x) / SIZE, 3), round((y - centre_y) / SIZE, 3)] for x, y in chosen]
    # A stroke of its own with a single checkpoint (the dot of i and j) must be touched: the game can't let it be
    # the one checkpoint missed. Found from the centre lines' connected pieces, never guessed from distances.
    piece = pieces(lines)
    counts = {}
    for x, y in chosen:
        counts[piece[y, x]] = counts.get(piece[y, x], 0) + 1
    dots = [i for i, (x, y) in enumerate(chosen) if counts[piece[y, x]] == 1]
    return points, dots


def pieces(lines):
    """A label per pixel of the centre lines: which connected piece (8 neighbours) it belongs to."""
    label = np.zeros(lines.shape, dtype=np.int32)
    count = 0
    for y, x in zip(*np.nonzero(lines)):
        if label[y, x]:
            continue
        count += 1
        stack = [(y, x)]
        label[y, x] = count
        while stack:
            cy, cx = stack.pop()
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    ny, nx = cy + dy, cx + dx
                    if (0 <= ny < lines.shape[0] and 0 <= nx < lines.shape[1] and lines[ny, nx]
                            and not label[ny, nx]):
                        label[ny, nx] = count
                        stack.append((ny, nx))
    return label


def main():
    font = ImageFont.truetype(str(FONT), SIZE)
    made = {g: shape(g, font) for g in graphemes()}
    shapes = {g: points for g, (points, _) in made.items()}
    dots = {g: found for g, (_, found) in made.items() if found}
    OUT.write_text(json.dumps({
        "note": "Generated by scripts/trace_shapes.py from the letter font: Letter Trace's checkpoints on each "
                "grapheme's strokes, in ems from the centre of the letter's label. Run it again after changing the "
                "graphemes or the font. dots: the checkpoints that are a stroke on their own (the dot of i and j), "
                "which must be touched.",
        "font": FONT.name,
        "source": source_hash(),
        "shapes": shapes,
        "dots": dots,
    }, indent=1, ensure_ascii=False) + "\n")
    print(f"{len(shapes)} graphemes, {sum(len(p) for p in shapes.values())} checkpoints -> {OUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
