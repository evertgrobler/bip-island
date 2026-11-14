"""What Letter Trace's shapes are made from, shared by trace_shapes.py (which makes them) and validate_content.py
(which checks they are up to date). No numpy or Pillow here, so the validator never needs them."""
import hashlib, json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
FONT = ROOT / "Resources" / "Fonts" / "AtkinsonHyperlegible-Bold.ttf"
# English first, then every other language folder (Content/<lang>/phonics/graphemes.json), so a new language is covered.
LISTS = [ROOT / "Content" / "phonics" / "graphemes.json",
         *sorted((ROOT / "Content").glob("*/phonics/graphemes.json"))]
SIZE = 400          # pixels per em while measuring
SPACING = 0.11      # ems between checkpoints along a stroke
FEWEST = 3          # the fewest checkpoints a shape may have (BipCore.TraceCheckpoints.Fewest says the same)


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
    """The font, the graphemes and the generator itself: when any of them changes, the shapes are out of date."""
    recipe = f"{SIZE} {SPACING} {FEWEST}\n" + "\n".join(sorted(graphemes()))
    # The generator and this file (how the graphemes are gathered), line endings normalised so a Windows checkout
    # (CRLF) reads the same.
    generator = b"".join(Path(__file__).with_name(name).read_bytes().replace(b"\r\n", b"\n")
                         for name in ("trace_shapes.py", "trace_source.py"))
    return hashlib.sha1(FONT.read_bytes() + generator + recipe.encode("utf-8")).hexdigest()
