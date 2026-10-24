#!/usr/bin/env python3
"""Give the bundled Atkinson Hyperlegible fonts a plain zero (owner decision, 18 October 2026).

Atkinson Hyperlegible draws 0 with a slash, so 10 read as "1Ø" to children learning numbers. This
replaces the zero's two half-holes (split by the slash) with one oval hole: the outer outline,
shrunk to the same stroke widths as the original. `zero.tf` (tabular figures) uses `zero`, so it
follows. The font is under the SIL OFL 1.1 with no Reserved Font Name, so a changed copy may keep
its name. Safe to run again: a zero that already has one hole is left alone.

Usage: python3 scripts/fonts/plain_zero.py [font.ttf ...]
(default: the fonts in Resources/Fonts/ and their copies in site/fonts/)
"""
import shutil
import sys
from pathlib import Path

from fontTools.ttLib import TTFont
from fontTools.ttLib.tables._g_l_y_f import GlyphCoordinates

ROOT = Path(__file__).resolve().parents[2]
NAMES = ["AtkinsonHyperlegible-Regular.ttf", "AtkinsonHyperlegible-Bold.ttf"]


def contours(glyph, glyf):
    coords, ends, flags = glyph.getCoordinates(glyf)
    start = 0
    for end in ends:
        yield [tuple(p) for p in coords[start:end + 1]], list(flags[start:end + 1])
        start = end + 1


def bounds(points):
    xs = [p[0] for p in points]
    ys = [p[1] for p in points]
    return min(xs), max(xs), min(ys), max(ys)


def plain_zero(path: Path) -> bool:
    font = TTFont(path)
    glyf = font["glyf"]
    zero = glyf["zero"]
    parts = list(contours(zero, glyf))
    if len(parts) == 2:
        return False  # already plain
    if len(parts) != 3:
        raise SystemExit(f"{path.name}: expected a slashed zero with 3 outlines, found {len(parts)}")
    # The outer outline is the biggest; the other two are the halves of the hole.
    parts.sort(key=lambda part: -(bounds(part[0])[1] - bounds(part[0])[0]))
    (outer, outer_flags), *halves = parts
    ox0, ox1, oy0, oy1 = bounds(outer)
    hx0 = min(bounds(h[0])[0] for h in halves)
    hx1 = max(bounds(h[0])[1] for h in halves)
    hy0 = min(bounds(h[0])[2] for h in halves)
    hy1 = max(bounds(h[0])[3] for h in halves)
    # One hole with the original stroke widths: the outer outline mapped onto the halves' box,
    # wound the other way so it cuts out.
    hole = [
        (round(hx0 + (x - ox0) * (hx1 - hx0) / (ox1 - ox0)), round(hy0 + (y - oy0) * (hy1 - hy0) / (oy1 - oy0)))
        for x, y in outer
    ]
    hole.reverse()
    hole_flags = list(reversed(outer_flags))
    zero.coordinates = GlyphCoordinates(outer + hole)
    zero.flags = bytearray(outer_flags + hole_flags)
    zero.endPtsOfContours = [len(outer) - 1, len(outer) + len(hole) - 1]
    zero.numberOfContours = 2
    zero.program = type(zero.program)()  # the old hinting fits the old outline
    zero.program.fromBytecode(b"")
    zero.recalcBounds(glyf)
    font.save(path)
    return True


def main() -> None:
    paths = [Path(p) for p in sys.argv[1:]] or [ROOT / "Resources" / "Fonts" / n for n in NAMES]
    for path in paths:
        print(f"{path.relative_to(ROOT) if path.is_relative_to(ROOT) else path}: "
              f"{'plain zero now' if plain_zero(path) else 'already plain'}")
    if not sys.argv[1:]:
        for name in NAMES:  # the download page uses the same font
            shutil.copyfile(ROOT / "Resources" / "Fonts" / name, ROOT / "site" / "fonts" / name)


if __name__ == "__main__":
    main()
