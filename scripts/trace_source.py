"""What Letter Trace's shapes are made from, shared by trace_shapes.py (which makes them) and validate_content.py
(which checks they are up to date). No numpy or Pillow here, so the validator never needs them."""
import hashlib, json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
FONT = ROOT / "Resources" / "Fonts" / "AtkinsonHyperlegible-Bold.ttf"
LISTS = [ROOT / "Content" / "phonics" / "graphemes.json", ROOT / "Content" / "af" / "phonics" / "graphemes.json"]
SIZE = 400          # pixels per em while measuring
SPACING = 0.11      # ems between checkpoints along a stroke
VERSION = 2         # raise when trace_shapes.py changes how it makes the shapes


def graphemes():
    """Every grapheme of every language, in first-seen order."""
    found = []
    for path in LISTS:
        if path.exists():
            for g in json.loads(path.read_text(encoding="utf-8"))["graphemes"]:
                if g["grapheme"] not in found:
                    found.append(g["grapheme"])
    return found


def source_hash():
    """The font, the graphemes and how the shapes are made: when any of them changes, the shapes are out of date."""
    recipe = f"{VERSION} {SIZE} {SPACING}\n" + "\n".join(sorted(graphemes()))
    return hashlib.sha1(FONT.read_bytes() + recipe.encode("utf-8")).hexdigest()
