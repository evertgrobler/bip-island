#!/usr/bin/env python3
"""Validate Bip Island content packs and write the asset manifest.

Run from the repo root:  python3 scripts/validate_content.py
Exits non-zero on any problem, so CI can block a bad update.
Writes Content/asset_manifest.json: every voice clip (with the text to speak)
and every picture the content needs.

Each other home language (docs/LANGUAGES.md) has its own folder, Content/<lang>/, read over the
shared files. When Content/af/ exists the same checks run again for it (--lang af), writing
Content/af/asset_manifest.json, with Afrikaans number words, audio/af/script.csv and the
Afrikaans spelling check (af_spelling.py) in place of the South African English one.
"""
import csv, json, re, subprocess, sys, unicodedata
from collections import deque
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import sa_english, af_spelling, spoken_numbers
af_spelling.self_test()

LANG = sys.argv[sys.argv.index("--lang") + 1] if "--lang" in sys.argv else ""
CONTENT = Path(__file__).resolve().parent.parent / "Content"
# The language's own folder (where its manifest is written); English is the base, Content/ itself.
ROOT = CONTENT / LANG if LANG else CONTENT
REPO = CONTENT.parent
SCRIPT = REPO / "audio" / (f"{LANG}/script.csv" if LANG else "script.csv")
errors = []

def load(rel):
    """A content file: the language's own copy when it has one, else the shared English file."""
    path = ROOT / rel if (ROOT / rel).exists() else CONTENT / rel
    with open(path, encoding="utf-8") as f:
        return json.load(f)

def err(msg):
    errors.append(msg)

objectives = {o["code"]: o for o in load("curriculum/objectives.json")["objectives"]}
ph = load("phonics/graphemes.json")
G = {g["id"]: g for g in ph["graphemes"]}
words_doc = load("words/words.json")
WORDS = {w["word"]: w for w in words_doc["words"]}
tricky = load("words/tricky_words.json")
sentences = load("words/sentences.json")["sentences"]
endings = load("words/endings.json")
homophones = load("words/homophones.json")["sets"]
contractions = load("words/contractions.json")["pairs"]
numbers = load("numbers/numbers.json")
seqs = load("coding/sequences.json")["sets"]
patterns = load("coding/patterns.json")
levels_doc = load("coding/levels.json")
skills_doc = load("curriculum/skills.json")
games = load("curriculum/games.json")["games"]
shapes_doc = load("art/shapes.json")
paints_doc = load("art/paints.json")
mirror_doc = load("art/mirror.json")
language_doc = load("language/words.json")

# --- objectives referenced anywhere must exist
def check_objs(where, codes):
    for c in codes:
        if c not in objectives:
            err(f"{where}: unknown objective {c}")

for grp in ph["groups"]:
    check_objs(f"phonics group {grp['group']}", grp["objectives"])

# --- graphemes
ids_seen = set()
for g in ph["graphemes"]:
    if g["id"] in ids_seen: err(f"duplicate grapheme id {g['id']}")
    ids_seen.add(g["id"])
    if g["kind"] not in ("stretchy", "bouncy", "vowel", "digraph"): err(f"grapheme {g['id']}: bad kind")
    if g.get("alternativeOf") and g["alternativeOf"] not in G: err(f"grapheme {g['id']}: unknown alternativeOf")

# --- words: spelling, decodability, objectives
def spell(parts):
    out, tail = "", ""
    for p in parts:
        gr = G[p]["grapheme"]
        if "-" in gr: out += gr[0]; tail = "e"
        else: out += gr
    return out + tail

# South African English (CLAUDE.md): words a South African child uses, South African spelling. The
# list lives in sa_english.py; it checks everything a child or parent sees or hears (see the end).
# Words fine to read but never shown as a picture, because a child would name the picture another way:
NOT_SA_PICTURES = {"corn": "a South African child calls the cob a mealie"}
for w in words_doc["words"]:
    if w["picturable"] and w["word"] in NOT_SA_PICTURES: err(f"word {w['word']}: no picture ({NOT_SA_PICTURES[w['word']]})")

for w in words_doc["words"]:
    parts = w["graphemes"]
    missing = [p for p in parts if p not in G]
    if missing: err(f"word {w['word']}: unknown graphemes {missing}"); continue
    if spell(parts) != w["word"]: err(f"word {w['word']}: graphemes spell '{spell(parts)}'")
    if max(G[p]["group"] for p in parts) != w["decodableFromGroup"]: err(f"word {w['word']}: wrong decodableFromGroup")
    if w["picturable"] and not w.get("picture"): err(f"word {w['word']}: picturable but no picture id")
    check_objs(f"word {w['word']}", w["objectives"])

# Words whose letters don't all make their taught sound must say so ("blendable": false), or Sound Buttons
# and Word Rocket would blend them into another word. Afrikaans (graphemes.json teaches e as /ɛ/ and i as
# /ə/): an e after the first vowel is a schwa (appel), a single a, e, o or u before one consonant and a
# vowel is long (hamer, motor), and an i straight after a vowel is a glide (koei).
def is_vowel(gid): return all(c in "aeiouy" for c in G[gid]["grapheme"])
def af_unblendable(parts):
    vowels = [i for i, gid in enumerate(parts) if is_vowel(gid)]
    for i, gid in enumerate(parts):
        if gid == "e" and i != vowels[0]: return "its e after the first vowel is a schwa"
        if gid in ("a", "e", "o", "u") and i + 2 < len(parts) and not is_vowel(parts[i + 1]) and is_vowel(parts[i + 2]):
            return f"its {gid} in an open syllable is long"
        if gid == "i" and i > 0 and is_vowel(parts[i - 1]): return "its i after a vowel is a glide"
    return None
UNBLENDABLE = {"af": af_unblendable}.get(LANG, lambda parts: None)
for w in words_doc["words"]:
    if not isinstance(w.get("blendable", True), bool): err(f"word {w['word']}: blendable must be true or false")
    if any(p not in G for p in w["graphemes"]): continue
    why = UNBLENDABLE(w["graphemes"])
    if why and w.get("blendable", True): err(f"word {w['word']}: {why}, so it needs \"blendable\": false")
    if not why and w.get("blendable", True) is False: err(f"word {w['word']}: blends sound by sound, so drop \"blendable\": false")

# enough words per group for rounds of 3 options with distinct first sounds
for grp in range(1, max(g["group"] for g in ph["graphemes"]) + 1):
    pool = [w for w in words_doc["words"] if w["decodableFromGroup"] <= grp and w["picturable"]]
    if len({w["firstSound"] for w in pool}) < 3:
        err(f"group {grp}: fewer than 3 different first sounds among picturable words")

# --- sentences decodable at their group
# Little words every sentence may use (Afrikaans: the article, "is" and the small joining words).
LITTLE_WORDS = {"af": {"'n", "die", "is", "in", "op", "en", "het", "my", "sy", "hy", "se"}}.get(LANG, {"in", "on", "it", "a", "is", "has", "at", "an"})
known_tricky = {t.lower() for t in tricky["stage1"]["words"] + tricky["stage2"]["words"]} | LITTLE_WORDS

# One reading rule for every sentence a child reads (Silly Sentences, Bip's Stories level 1).
def reading_words(text):
    """The words of a sentence as a child reads them: lower case, without punctuation or quotes ("'n" stays)."""
    for tok in text.split():
        t = tok.lower()
        t = t if t == "'n" else t.strip(".,!?\"'")
        if t: yield t

assert list(reading_words("'Stop!' she said. 'Hello,' \"Yes?\" 'n Pop.")) == ["stop", "she", "said", "hello", "yes", "'n", "pop"]

def decodable_group(word):
    """The phonics group a word can be sounded out from (a plural -s counts as its root), or None."""
    base = word[:-1] if word.endswith("s") and word[:-1] in WORDS else word
    return WORDS[base]["decodableFromGroup"] if base in WORDS else None

for s in sentences:
    for t in reading_words(s["text"]):
        group = decodable_group(t)
        if group is not None:
            if group > s["decodableFromGroup"]: err(f"{s['id']}: '{t}' needs a later group")
        elif t not in known_tricky:
            err(f"{s['id']}: '{t}' is neither decodable nor a tricky word")

for e in endings["plurals_s"] + endings["plurals_es"] + endings["verbs"]:
    if e["word"] not in WORDS: err(f"endings: '{e['word']}' not in word bank")
for hs in homophones:
    for s in hs["sentences"]:
        if s["answer"] not in hs["words"]: err(f"homophones {hs['words']}: answer '{s['answer']}' not in set")
        if s["text"].count("___") != 1: err(f"homophones: '{s['text']}' needs exactly one blank")

# --- skills graph: known objectives, known prerequisites, no cycles
SK = {s["id"]: s for s in skills_doc["skills"]}
for s in SK.values():
    check_objs(f"skill {s['id']}", s["objectives"])
    if not isinstance(s.get("ageSkip", True), bool): err(f"skill {s['id']}: ageSkip must be true or false")
    # A second language isn't learnt by growing older: these skills are never known by age.
    if s["island"] == "language" and s.get("ageSkip", True): err(f"skill {s['id']}: Language Island skills need \"ageSkip\": false")
    for p in s["prerequisites"]:
        if p not in SK: err(f"skill {s['id']}: unknown prerequisite {p}")
state = {}
def visit(n, stack=()):
    if state.get(n) == 2: return
    if state.get(n) == 1: err(f"skill cycle: {' -> '.join(stack + (n,))}"); return
    state[n] = 1
    for p in SK[n]["prerequisites"]:
        if p in SK: visit(p, stack + (n,))
    state[n] = 2
for n in SK: visit(n)

# --- every phonics group is taught by the skill snd_g<group> (the game links them by this id), in the
# group's band (the band decides which groups an older child is treated as knowing), and every snd_g
# skill has a group in this language to master (a language with fewer groups has its own skills.json).
GROUP_NUMBERS = {grp["group"] for grp in ph["groups"]}
for grp in ph["groups"]:
    skill = SK.get(f"snd_g{grp['group']}")
    if skill is None: err(f"phonics group {grp['group']}: no skill snd_g{grp['group']} in skills.json")
    elif skill["band"] != grp["band"]: err(f"phonics group {grp['group']}: band {grp['band']}, but skill snd_g{grp['group']} is {skill['band']}")
def phonics_group(skill_id):
    m = re.fullmatch(r"snd_g(\d+)", skill_id)
    return int(m.group(1)) if m else None
for sid in SK:
    if phonics_group(sid) is not None and phonics_group(sid) not in GROUP_NUMBERS:
        err(f"skill {sid}: there is no phonics group {phonics_group(sid)} in this language's graphemes.json")

# --- a language's own skills.json is the shared one with its own phonics skills: the same other skills
# (only their snd_g prerequisites may differ), mastery rules and starting bands, so the two never drift.
if LANG and (ROOT / "curriculum" / "skills.json").exists():
    with open(CONTENT / "curriculum" / "skills.json", encoding="utf-8") as f: shared_skills = json.load(f)
    for key in ("mastery", "masteryRules", "startingBand"):
        if skills_doc.get(key) != shared_skills.get(key): err(f"{LANG}/curriculum/skills.json: {key} differs from the shared skills.json")
    shared_sk = {x["id"]: x for x in shared_skills["skills"] if phonics_group(x["id"]) is None}
    own_sk = {x["id"]: x for x in skills_doc["skills"] if phonics_group(x["id"]) is None}
    for sid in sorted(set(shared_sk) ^ set(own_sk)): err(f"{LANG}/curriculum/skills.json: skill {sid} is in only one of it and the shared skills.json")
    for sid in sorted(set(shared_sk) & set(own_sk)):
        mine, theirs = own_sk[sid], shared_sk[sid]
        if {k: v for k, v in mine.items() if k != "prerequisites"} != {k: v for k, v in theirs.items() if k != "prerequisites"} or \
           [x for x in mine["prerequisites"] if phonics_group(x) is None] != [x for x in theirs["prerequisites"] if phonics_group(x) is None]:
            err(f"{LANG}/curriculum/skills.json: skill {sid} differs from the shared one (only its snd_g prerequisites may)")

# --- mastery rules the game reads (the prose in "mastery" is for people)
rules = skills_doc.get("masteryRules", {})
for key in ("correctInARowToMoveUp", "missesInARowToDropBack", "masteredWindow", "masteredPercent", "masteredDistinctDays"):
    if not isinstance(rules.get(key), int) or rules[key] < 1: err(f"masteryRules.{key} must be a whole number of at least 1")
if isinstance(rules.get("masteredPercent"), int) and rules["masteredPercent"] > 100: err("masteryRules.masteredPercent is over 100")
review = rules.get("reviewAfterDays")
if not (isinstance(review, list) and review and all(isinstance(d, int) and d > 0 for d in review) and review == sorted(set(review))):
    err("masteryRules.reviewAfterDays must be increasing whole numbers of days")
for age, band in skills_doc["startingBand"].items():
    if band not in ("foundation", "stage1", "stage2", "stage3"): err(f"startingBand {age}: unknown band {band}")

# --- games
games_doc = load("curriculum/games.json")
if not isinstance(games_doc.get("session", {}).get("roundsPerSession"), int) or games_doc["session"]["roundsPerSession"] < 1:
    err("games.json: session.roundsPerSession must be a whole number of at least 1")
game_ids = [gm["id"] for gm in games]
if len(game_ids) != len(set(game_ids)): err("games.json: duplicate game id")
LEVEL_KEYS = {"band", "countTo", "choices", "flashTenths", "bubbles", "speedPercent", "soundCounts", "gridBand", "cards", "spares",
              "modes", "shapes", "pots", "mirror", "from", "steps", "storiesUpTo", "sameThemeFirst", "note"}
BANDS = ["foundation", "stage1", "stage2", "stage3"]
for gm in games:
    levels = gm.get("levels", [])
    for i, lv in enumerate(levels):
        where = f"game {gm['id']} level {i + 1}"
        for k in lv:
            if k not in LEVEL_KEYS: err(f"{where}: unknown key {k}")
        if "sameThemeFirst" in lv and not isinstance(lv["sameThemeFirst"], bool): err(f"{where}: sameThemeFirst must be true or false")
        for k in ("band", "gridBand", "storiesUpTo"):
            if k in lv and lv[k] not in BANDS: err(f"{where}: unknown {k} {lv[k]}")
        for k in ("countTo", "choices", "flashTenths", "bubbles", "speedPercent", "cards"):
            if k in lv and (not isinstance(lv[k], int) or lv[k] < 1): err(f"{where}: {k} must be a positive whole number")
        if "choices" in lv and not 2 <= lv["choices"] <= 5: err(f"{where}: choices must be 2 to 5")
        if "bubbles" in lv and not 3 <= lv["bubbles"] <= 8: err(f"{where}: bubbles must be 3 to 8")
        if "spares" in lv and (not isinstance(lv["spares"], int) or not 0 <= lv["spares"] <= 4): err(f"{where}: spares must be 0 to 4")
        if "cards" in lv and not 2 <= lv["cards"] <= 6: err(f"{where}: cards must be 2 to 6")
        if "soundCounts" in lv and not any(w["picturable"] and w.get("blendable", True) and w["soundCount"] in lv["soundCounts"] for w in words_doc["words"]):
            err(f"{where}: no picturable words with {lv['soundCounts']} sounds")
    # A level without a band carries on the band before it.
    current, bands = 0, []
    for lv in levels:
        current = BANDS.index(lv["band"]) if lv.get("band") in BANDS else current
        bands.append(current)
    if bands != sorted(bands): err(f"game {gm['id']}: level bands must not go backwards")
for gm in games:
    for s in gm["skills"]:
        # A game lists every phonics group it practises; a language without that group just skips it.
        if s not in SK and not (phonics_group(s) is not None and phonics_group(s) not in GROUP_NUMBERS):
            err(f"game {gm['id']}: unknown skill {s}")
    check_objs(f"game {gm['id']}", gm["objectives"])


# --- Bip's Balance story sums: known animals, recorded clips, both numbers and the animals said
script_clips = set()
with open(SCRIPT, encoding="utf-8") as f:
    for row in csv.DictReader(f): script_clips.add(row["file"].rsplit(".", 1)[0])
numbers_json = load("numbers/numbers.json")
known_things = {o["id"] for o in numbers_json["countingObjects"]}
for st in numbers_json.get("storySums", []):
    where = f"story sum {st['id']}"
    if st["op"] not in ("plus", "minus"): err(f"{where}: op must be plus or minus")
    for slot in ("{a}", "{b}", "{things}"):
        if st["clips"].count(slot) != 1: err(f"{where}: says {slot} {st['clips'].count(slot)} times (once needed)")
    for clip in st["clips"]:
        if not clip.startswith("{") and clip not in script_clips: err(f"{where}: clip {clip} is not in audio/script.csv")
    for thing in st["things"]:
        if thing not in known_things: err(f"{where}: unknown counting object {thing}")
    if not st["things"]: err(f"{where}: needs at least one counting object")

# --- Bip's Balance: each level starts no earlier than the skill it records (a stage 3 skill isn't
# offered to a stage 2 child as their first level)
BALANCE_SKILL = {"add": "add_sub_20", "takeaway": "add_sub_20", "past10": "add_sub_20", "factfamily": "add_sub_20",
                 "missing": "missing_number"}
for gm in games:
    if gm["id"] != "bips_balance": continue
    band = 0
    for i, lv in enumerate(gm.get("levels", [])):
        band = BANDS.index(lv["band"]) if lv.get("band") in BANDS else band
        for mode in lv.get("modes", []):
            skill = SK.get(BALANCE_SKILL.get(mode, ""))
            if skill is None: err(f"game bips_balance level {i + 1}: no skill for mode {mode}")
            elif BANDS.index(skill["band"]) > band:
                err(f"game bips_balance level {i + 1}: band {BANDS[band]} is before {skill['id']}'s band {skill['band']}")
            elif skill["id"] not in gm["skills"]: err(f"game bips_balance: skill {skill['id']} is not in its skills list")

# --- Art Island: shapes, paints and mirror pictures
import math
ISLANDS = {"letters", "numbers", "words", "coding", "art", "language"}
for gm in games:
    if gm["island"] not in ISLANDS: err(f"game {gm['id']}: unknown island {gm['island']}")
for s_ in skills_doc["skills"]:
    if s_["island"] not in ISLANDS: err(f"skill {s_['id']}: unknown island {s_['island']}")
GAME_MODES = {"shape_builder": {"find", "fill", "sides", "turn", "count", "regular"},
              "paint_pots": {"make", "predict"}, "mirror_magic": {"same", "finish", "pegs", "line"},
              "word_rocket": {"letter", "cvc", "longer", "tricky"},
              "block_towers": {"build", "which", "join", "takeaway", "maketen", "doubles", "oddeven", "tensones", "pattern"},
              "bips_balance": {"add", "takeaway", "past10", "missing", "factfamily"}}
SHAPES = {sh["id"]: sh for sh in shapes_doc["shapes"]}
COLOURS = {c["id"]: c for c in paints_doc["colours"]}
POTS = {pt["colour"]: pt for pt in paints_doc["pots"]}
ART_COLOURS = {"sun", "red", "orange", "brown", "sky", "ink", "white", "teal", "leaf", "sand", "pink", "purple"}

def outline_points(sh, n=360):
    """The shape's outline at its card size, as points (curves sampled)."""
    w, h = sh["card"]
    o = sh["outline"]
    if o["kind"] == "polygon": return [(x * w, y * h) for x, y in o["points"]]
    if o["kind"] == "ellipse": return [(w * math.cos(2 * math.pi * i / n), h * math.sin(2 * math.pi * i / n)) for i in range(n)]
    if o["kind"] == "semicircle":  # flat side down, centred on its bounding box
        return [(w * math.cos(math.pi * i / n), h * (2 * math.sin(math.pi * i / n) - 1)) for i in range(n + 1)]
    err(f"shape {sh['id']}: unknown outline kind {o['kind']}"); return []

def same_after_turn(pts, angle):
    cx = sum(x for x, _ in pts) / len(pts); cy = sum(y for _, y in pts) / len(pts)
    moved = [((x - cx) * math.cos(angle) - (y - cy) * math.sin(angle), (x - cx) * math.sin(angle) + (y - cy) * math.cos(angle)) for x, y in pts]
    rel = [(x - cx, y - cy) for x, y in pts]
    return all(min(math.hypot(mx - x, my - y) for x, y in rel) < 0.02 for mx, my in moved)

for sh in SHAPES.values():
    where = f"shape {sh['id']}"
    if sh["band"] not in BANDS: err(f"{where}: unknown band")
    pts = outline_points(sh)
    if not pts: continue
    kind = sh["outline"]["kind"]
    if kind == "polygon":
        sides = [math.dist(pts[i], pts[(i + 1) % len(pts)]) for i in range(len(pts))]
        if sh["straightSides"] != len(pts) or sh["curved"]: err(f"{where}: a polygon with {len(pts)} straight sides and no curves")
        regular = max(sides) - min(sides) < 0.02 and same_after_turn(pts, 2 * math.pi / len(pts))
        if sh.get("regular") != regular: err(f"{where}: regular should be {regular}")
    else:
        want = {"ellipse": 0, "semicircle": 1}[kind]
        if sh["straightSides"] != want or not sh["curved"] or sh.get("regular") is not None:
            err(f"{where}: a curved shape has {want} straight sides, curved true and no regular")
    turns = 0 if kind == "ellipse" and sh["card"][0] == sh["card"][1] else \
        max(k for k in range(1, 13) if same_after_turn(pts, 2 * math.pi / k))
    if sh["lookSameTurns"] != turns: err(f"{where}: looks the same {turns} times in a full turn, not {sh['lookSameTurns']}")
for pic in shapes_doc["pictures"]:
    where = f"shape picture {pic['id']}"
    if not any(not pc.get("fixed") for pc in pic["pieces"]): err(f"{where}: needs a piece that can go missing")
    for pc in pic["pieces"]:
        if pc["shape"] not in SHAPES: err(f"{where}: unknown shape {pc['shape']}")
        if pc["colour"] not in ART_COLOURS: err(f"{where}: unknown colour {pc['colour']}")
        if not pc.get("fixed") and min(pc["w"], pc["h"]) < 60: err(f"{where}: a {pc['shape']} that can go missing must be at least 60 pt")
        if abs(pc["x"]) + pc["w"] / 2 > 330 or abs(pc["y"]) + pc["h"] / 2 > 240: err(f"{where}: {pc['shape']} sticks out of the frame")
for cid, col in COLOURS.items():
    if not (col["hex"].startswith("#") and len(col["hex"]) == 7): err(f"colour {cid}: hex must look like #RRGGBB")
MIX = {}
for mx in paints_doc["mixes"]:
    for k in ("a", "b", "makes"):
        if mx[k] not in COLOURS: err(f"mix {mx}: unknown colour {mx[k]}")
    MIX[frozenset((mx["a"], mx["b"]))] = mx["makes"]
for pt in POTS.values():
    if pt["colour"] not in COLOURS: err(f"pot {pt['colour']}: unknown colour")
pal = mirror_doc["paints"]
for letter, cid in pal.items():
    if cid not in COLOURS: err(f"mirror paint {letter}: unknown colour {cid}")
for pic in mirror_doc["pictures"]:
    where = f"mirror picture {pic['id']}"
    r = pic["rows"]; n = len(r)
    if any(len(row) != n for row in r): err(f"{where}: must be square"); continue
    if any(ch != "." and ch not in pal for row in r for ch in row): err(f"{where}: unknown paint letter")
    lines = {"vertical": all(row == row[::-1] for row in r), "horizontal": r == r[::-1],
             "diagonal": all(r[i][j] == r[j][i] for i in range(n) for j in range(n)),
             "other diagonal": all(r[i][j] == r[n - 1 - j][n - 1 - i] for i in range(n) for j in range(n))}
    if [k for k, v in lines.items() if v] != [pic["mirror"]]: err(f"{where}: must fold only along its {pic['mirror']} line, folds along {[k for k, v in lines.items() if v]}")
pegs = mirror_doc["pegs"]
if pegs["size"] % 2 or not 1 <= pegs["fewest"] <= pegs["most"] <= pegs["size"] * pegs["size"] // 2: err("mirror pegs: even size, and fewest/most must fit half the board")
if pegs["colour"] not in COLOURS: err("mirror pegs: unknown colour")
for gm in games:
    modes = GAME_MODES.get(gm["id"])
    for i, lv in enumerate(gm.get("levels", [])):
        where = f"game {gm['id']} level {i + 1}"
        if ("modes" in lv) != (modes is not None): err(f"{where}: modes are only for {sorted(GAME_MODES)}"); continue
        if modes is None: continue
        if not lv["modes"] or not set(lv["modes"]) <= modes: err(f"{where}: modes must be some of {sorted(modes)}")
        if gm["id"] == "word_rocket" and len(lv["modes"]) != 1: err(f"{where}: a Word Rocket level asks one kind of question")
        if gm["id"] == "shape_builder":
            shs = lv.get("shapes", [])
            if any(x not in SHAPES for x in shs): err(f"{where}: unknown shape")
            if len(shs) < 3: err(f"{where}: needs at least 3 shapes to choose from")
            if "fill" in lv["modes"] and not any(all(pc["shape"] in shs or pc.get("fixed") for pc in pic["pieces"]) for pic in shapes_doc["pictures"]):
                err(f"{where}: no picture uses only these shapes")
            if "turn" in lv["modes"]:
                same = [x for x in shs if SHAPES[x]["lookSameTurns"] % 4 == 0]
                if not same or len(shs) - len(same) < 2: err(f"{where}: turn rounds need shapes that do and don't survive a quarter turn")
            if "count" in lv["modes"] and any(SHAPES[x]["lookSameTurns"] < 2 for x in shs): err(f"{where}: count rounds need shapes that look the same at least twice")
            if "regular" in lv["modes"]:
                reg = [x for x in shs if SHAPES[x].get("regular")]; irr = [x for x in shs if SHAPES[x].get("regular") is False]
                if not reg or len(irr) < 2: err(f"{where}: regular rounds need regular shapes and at least two irregular ones")
        elif gm["id"] == "paint_pots":
            pots = lv.get("pots", [])
            if len(pots) < 3 or any(x not in POTS for x in pots): err(f"{where}: needs at least 3 known pots")
            made = [MIX.get(frozenset((x, y))) for i2, x in enumerate(pots) for y in pots[i2 + 1:]]
            if None in made: err(f"{where}: some pairs of these pots have no mix")
            elif len(made) != len(set(made)): err(f"{where}: two pairs of pots make the same colour")
        elif gm["id"] == "block_towers":
            up = lv.get("countTo", 0)
            if not 3 <= up <= 99: err(f"{where}: countTo must be 3 to 99")
            if "tensones" in lv["modes"] and up < 20: err(f"{where}: tens and ones needs numbers past 20")
            if set(lv["modes"]) - {"tensones"} and up > 20: err(f"{where}: towers stop at 20 (only tens and ones go higher)")
            steps = lv.get("steps", [])
            if ("pattern" in lv["modes"]) != bool(steps): err(f"{where}: pattern levels (and only they) list their steps")
            elif steps and (any(not isinstance(d, int) or d < 1 for d in steps) or 1 + 3 * min(steps) > up):
                err(f"{where}: steps must be whole numbers from 1 that fit a pattern of four towers up to {up}")
            if "tensones" in lv["modes"] and not 11 <= lv.get("from", 11) <= up: err(f"{where}: tens and ones start from 11 up to countTo")
        elif gm["id"] == "bips_balance":
            up = lv.get("countTo", 0)
            if not 5 <= up <= 20: err(f"{where}: countTo must be 5 to 20")
            if "past10" in lv["modes"] and up < 12: err(f"{where}: going past 10 needs numbers up to at least 12")
        elif gm["id"] == "mirror_magic":
            if lv["modes"] != ["line"] and lv.get("mirror", "vertical") not in ("vertical", "horizontal"): err(f"{where}: mirror is vertical or horizontal")
            if "finish" in lv["modes"] and sum(p["mirror"] == lv.get("mirror", "vertical") for p in mirror_doc["pictures"]) < 3:
                err(f"{where}: needs at least 3 pictures with a {lv.get('mirror', 'vertical')} mirror line")

# --- coding levels: solvable, optimal, bugs really are bugs
DIRS = {"up": (-1, 0), "down": (1, 0), "left": (0, -1), "right": (0, 1)}
ORDER = ["up", "right", "down", "left"]
def bfs(rows, cols, start, goal, blocked):
    q = deque([(start, 0)]); seen = {start}
    while q:
        (r, c), d = q.popleft()
        if (r, c) == goal: return d
        for dr, dc in DIRS.values():
            n = (r + dr, c + dc)
            if 0 <= n[0] < rows and 0 <= n[1] < cols and n not in blocked and n not in seen:
                seen.add(n); q.append((n, d + 1))
    return None

def run(lv, prog):
    rows, cols = lv["grid"]["rows"], lv["grid"]["cols"]
    blocked = {tuple(b) for b in lv["rocks"]}
    r, c = lv["start"]; facing = lv.get("startFacing", "up")
    for step in prog:
        if step == "turnLeft": facing = ORDER[(ORDER.index(facing) - 1) % 4]; continue
        if step == "turnRight": facing = ORDER[(ORDER.index(facing) + 1) % 4]; continue
        dr, dc = DIRS[facing if step == "forward" else step]
        r, c = r + dr, c + dc
        if not (0 <= r < rows and 0 <= c < cols) or (r, c) in blocked: return None
    return [r, c]

ids = set()
for lv in levels_doc["levels"]:
    if lv["id"] in ids: err(f"level {lv['id']}: duplicate id")
    ids.add(lv["id"])
    check_objs(f"level {lv['id']}", lv["objectives"])
    blocked = {tuple(b) for b in lv["rocks"]}
    if tuple(lv["start"]) in blocked or tuple(lv["goal"]) in blocked: err(f"level {lv['id']}: start or goal on a rock")
    shortest = bfs(lv["grid"]["rows"], lv["grid"]["cols"], tuple(lv["start"]), tuple(lv["goal"]), blocked)
    if shortest is None: err(f"level {lv['id']}: not solvable"); continue
    if run(lv, lv["optimalProgram"]) != lv["goal"]: err(f"level {lv['id']}: optimalProgram does not reach the goal")
    moves = sum(1 for s in lv["optimalProgram"] if s not in ("turnLeft", "turnRight"))
    if moves != shortest: err(f"level {lv['id']}: optimalProgram is not a shortest route")
    for b in set(lv["optimalProgram"]):
        if b not in lv["blocks"]: err(f"level {lv['id']}: solution uses block '{b}' not offered")
    if "buggyProgram" in lv:
        if run(lv, lv["buggyProgram"]) == lv["goal"]: err(f"level {lv['id']}: buggyProgram already works")
        fixed = list(lv["buggyProgram"]); fixed[lv["bugIndex"]] = lv["fix"]
        if run(lv, fixed) != lv["goal"]: err(f"level {lv['id']}: applying the fix does not reach the goal")

for pz in levels_doc["puddleRules"]:
    check_objs(pz["id"], pz["objectives"])
    for m in pz["maps"]:
        pos = 0
        for _ in range(pz["solution"]["repeat"]):
            if pos >= pz["corridorLength"] - 1: break
            if pos + 1 in m["puddlesAt"]:
                pos += 2
            else:
                pos += 1
        if pos < pz["corridorLength"] - 1: err(f"{pz['id']}: solution does not reach the end of map {m['puddlesAt']}")
        for p in m["puddlesAt"]:
            if not 0 < p < pz["corridorLength"] - 1: err(f"{pz['id']}: puddle {p} out of range")
            if p + 1 in m["puddlesAt"]: err(f"{pz['id']}: two puddles in a row cannot be jumped")

# --- asset manifest
audio, pictures = {}, {}
def a(id_, text, notes=""):
    if id_ in audio and audio[id_]["text"] != text: err(f"audio {id_}: two different texts")
    audio[id_] = {"id": id_, "text": text, "notes": notes}
def p(id_, brief):
    pictures.setdefault(id_, {"id": id_, "brief": brief})
def word_clip(text):
    """A word's clip, as AudioCatalogue.WordClip names it: yo-yo -> word_yoyo (file names have no hyphens),
    vlieër -> word_vlieer (file names are plain ASCII: a Mac may write ë decomposed)."""
    plain = "".join(c for c in unicodedata.normalize("NFD", text) if unicodedata.category(c) != "Mn")
    return f"word_{plain.lower().replace('-', '')}"

for g in ph["graphemes"]:
    a(g["audio"], g["grapheme"], f"PURE SOUND /{g['ipa']}/ ({g['kind']}). {g['narratorHint']}")
    if g.get("letterName"): a(f"name_{g['id']}", g["letterName"], "Letter name, for the letter-names activity only.")
    p(g["mnemonicPicture"], f"{g['mnemonicWord']} (picture for the sound {g['grapheme']})")
    a(word_clip(g["mnemonicWord"]), g["mnemonicWord"])
for w in words_doc["words"]:
    if w["audio"] != word_clip(w["word"]): err(f"word {w['word']}: audio must be {word_clip(w['word'])}")
    a(w["audio"], w["word"])
    if w["picturable"]: p(w["picture"], w["word"])
for t in tricky["stage1"]["words"] + tricky["stage2"]["words"]:
    a(word_clip(t), t)
for s in sentences:
    a(s["audio"], s["text"])
    p(s["pictureRight"], s["pictureRightBrief"]); p(s["pictureWrong"], s["pictureWrongBrief"])
for e in endings["plurals_s"] + endings["plurals_es"]:
    a(word_clip(e["plural"]), e["plural"])
for e in endings["verbs"]:
    for k in ("s", "ed", "ing"):
        if k in e: a(word_clip(e[k]), e[k])
for i, hs in enumerate(homophones, 1):
    for j, s in enumerate(hs["sentences"], 1):
        a(f"vo_hom_{i:02d}_{j}", s["text"].replace("___", s["answer"]), "Read the whole sentence naturally.")
for pr in contractions:
    key = pr["short"].lower().replace("'", "")
    a(f"word_contraction_{key}", pr["short"]); a(f"word_{pr['long'].lower().replace(' ', '_')}", pr["long"])
for n in range(0, 101):
    a(f"num_{n}", str(n), "Say the number.")

# Numbers said inside a sentence: whole recorded sentences (vo_say_...) and, for bigger numbers,
# num_<n>_mid clips the game joins to the linking words (numbers.json "spokenNumbers").
EVEN_ODD = spoken_numbers.EVEN_ODD[LANG]
def number_word(n): return spoken_numbers.number_word(n, LANG)
spoken = numbers.get("spokenNumbers") or {}
join_to = spoken.get("joinTo", 0)
if not 10 <= join_to <= 20: err("spokenNumbers.joinTo must be 10-20")
for n in range(0, join_to + 1):
    a(f"num_{n}_mid", number_word(n).capitalize() + ",",
      "A number in the middle of a sentence (\"seven… plus three\"): carry on, don't finish.")
def number_pairs(sen):
    kind, lo, hi = sen.get("numbers"), sen.get("from", 1), sen.get("to", 0)
    if not 1 <= lo <= hi <= 20: err(f"spoken sentence {sen.get('id')}: from/to must be 1-20"); return []
    if kind == "one": return [(x, None) for x in range(lo, hi + 1)]
    if kind == "sum": return [(x, y) for x in range(1, hi) for y in range(1, hi) if x + y <= hi]
    if kind == "takeaway": return [(x, y) for x in range(max(lo, 2), hi + 1) for y in range(1, x)]
    err(f"spoken sentence {sen.get('id')}: numbers must be one, sum or takeaway"); return []
seen_spoken = set()
for sen in spoken.get("sentences", []):
    sid = sen.get("id", "")
    if not re.fullmatch(r"[a-z][a-z0-9_]*", sid) or sid in seen_spoken: err(f"spoken sentence id {sid!r}: lower-case and unique")
    seen_spoken.add(sid)
    for x, y in number_pairs(sen):
        values = {"a": x, "b": y, "sum": x + (y or 0), "left": x - (y or 0), "double": 2 * x, "rest": 10 - x}
        def fill(m):
            key = m.group(1)
            if key == "parity": return EVEN_ODD[0] if x % 2 == 0 else EVEN_ODD[1]
            if values.get(key) is None or not 0 <= values[key] <= 20: err(f"spoken sentence {sid}: no number for {{{key}}}"); return "?"
            return number_word(values[key])
        text = re.sub(r"\{([a-z]+)\}", fill, sen.get("text", ""))
        text = text[0].upper() + text[1:]
        a(f"vo_say_{sid}_{x}" + (f"_{y}" if y is not None else ""), text, "One whole sentence, said naturally.")
# A voice-script clip must never look like a recorded sentence (speak_clips.py finds those by
# vo_say_<sentence id>_*, and AudioCatalogue.SentenceClip names them the same way).
for clip in script_clips:
    for sid in seen_spoken:
        if clip.startswith(f"vo_say_{sid}_"): err(f"audio/script.csv {clip}: clashes with spoken sentence {sid} (vo_say_{sid}_...)")
for o in numbers["countingObjects"]:
    p(o["picture"], o["id"])
    plural = o["audioPlural"][len("word_"):]
    if not o["audioPlural"].startswith("word_"): err(f"counting object {o['id']}: plural clip must be word_<plural>")
    a(o["audioPlural"], plural.replace("_", " "))
cur = numbers["currency"]
for c in cur["coins"] + cur["notes"]:
    p(f"pic_money_{c['id']}", f"South African {c['label']} {'coin' if c in cur['coins'] else 'note'}, in the game's own hand-drawn style")
    lab = c["label"]
    cents = "sent" if LANG == "af" else "cents"
    a(f"money_{c['id']}", f"{lab[1:]} rand" if lab.startswith("R") else f"{lab[:-1]} {cents}")
for it in cur["shopItems"]:
    p(it["picture"], it["id"])
for st in seqs:
    for c in st["cards"]:
        a(c["audio"], c["text"]); p(c["picture"], f"{st['id']} step {c['n']}: {c['text']}")

# --- Language Island (docs/LANGUAGES.md): every word in both languages, with a picture; its clip
# learn_<id> says it in this run's language (each language's folder has its own).
THEMES = {t["id"]: t for t in language_doc["themes"]}
if len(THEMES) != len(language_doc["themes"]): err("language/words.json: two themes share an id")
if not any(t["fromLevel"] == 0 for t in THEMES.values()): err("language/words.json: no theme opens at level 0")
# Every theme opens at some level of both word games, so none stays closed for good.
for gid in ("show_me", "what_is_it"):
    count = len(next((g.get("levels", []) for g in games if g["id"] == gid), []))
    for t in THEMES.values():
        if not isinstance(t["fromLevel"], int) or not 0 <= t["fromLevel"] < max(count, 1):
            err(f"language theme {t['id']}: fromLevel {t['fromLevel']} is past {gid}'s {count} levels")
# Picture words for colours are drawn in Paint Pots' paints (art/paints.json).
for w in language_doc["words"]:
    if w["picture"].startswith("pic_colour_") and w["picture"][len("pic_colour_"):] not in COLOURS:
        err(f"language word {w['id']}: {w['picture']} names no colour in art/paints.json")
seen_ids, seen_pictures = set(), set()
for w in language_doc["words"]:
    where = f"language word {w['id']}"
    if w["id"] in seen_ids: err(f"{where}: duplicate id")
    if w["picture"] in seen_pictures: err(f"{where}: picture {w['picture']} is used by another word")
    seen_ids.add(w["id"]); seen_pictures.add(w["picture"])
    if w["theme"] not in THEMES: err(f"{where}: unknown theme {w['theme']}")
    if not w["en"].strip() or not w["af"].strip(): err(f"{where}: needs both an English and an Afrikaans word")
    if not re.fullmatch(r"[a-z0-9_]+", w["id"]): err(f"{where}: ids are lower case letters, digits and _")
    a(f"learn_{w['id']}", w["af" if LANG == "af" else "en"], "Language Island: the word on its own, clear and friendly.")
    p(w["picture"], w["en"])
if not LANG:
    for t in THEMES.values():
        for problem in af_spelling.problems(t["af"]): err(f"language theme {t['id']}: {problem}")
    for w in language_doc["words"]:
        for problem in af_spelling.problems(w["af"]): err(f"language word {w['id']}: {problem}")
# Bip's Day pairs each Morning Order card with the same card in the other language: same stories, same pictures.
if LANG:
    english_sets = {st["id"]: st for st in json.loads((CONTENT / "coding" / "sequences.json").read_text(encoding="utf-8"))["sets"]}
    for st in seqs:
        other = english_sets.get(st["id"])
        if other is None: err(f"sequence {st['id']}: no English story with this id (Bip's Day pairs them)"); continue
        # The same picture and clip name card by card: Bip's Day plays the card's clip from the other language's folder.
        if [(c["n"], c["picture"], c["audio"]) for c in st["cards"]] != [(c["n"], c["picture"], c["audio"]) for c in other["cards"]]:
            err(f"sequence {st['id']}: cards differ from the English story (Bip's Day pairs them card by card, picture and clip)")

for sh in SHAPES.values():
    a(word_clip(sh["name"].replace(" ", "_")), sh["name"])
for col in COLOURS.values():
    a(f"word_{col['id']}", col["name"])
for pt in POTS.values():
    p(pt["label"], f"label for the {pt['colour']} paint pot")

# --- Bip's Stories (docs/GAMES.md): 20 stories a language, four per level, on scenes both languages share.
# Checked last, so an answer can reuse any word clip the rest of the content has (its clip is word_<text>),
# and story pictures must be pictures the game already draws, or ones drawn for the stories.
scenes_doc = load("stories/scenes.json")
stories = load("stories/stories.json")["stories"]
STORY_LEVELS = {  # scenes, sentences a scene, questions, choices a question, most words in a sentence
    1: dict(scenes=(2, 2), sentences=(1, 1), questions=1, choices=(2, 2), words=9),
    2: dict(scenes=(3, 3), sentences=(1, 2), questions=2, choices=(2, 3), words=8),
    3: dict(scenes=(3, 3), sentences=(1, 3), questions=3, choices=(3, 3), words=12),
    4: dict(scenes=(4, 4), sentences=(1, 3), questions=3, choices=(3, 3), words=14),
    5: dict(scenes=(5, 6), sentences=(1, 3), questions=3, choices=(3, 3), words=16),
}
STORIES_PER_LEVEL = 4
QUESTION_KINDS = {"who", "where", "what", "why", "feel"}
# Level 1 is for early readers: most words decodable at an early phonics group (or tricky words).
EARLY_GROUPS = {"af": {1: 4}}.get(LANG, {1: 5})
EARLY_SHARE = {1: 0.75}
# Hand-drawn or emoji objects with faces; the oldest level (ages 9-10) keeps faces to people and animals.
FACED_OBJECTS = {"pic_sun", "pic_mouth", "pic_morning_2", "pic_bedtime_1", "pic_biscuits_6", "pic_kiss", "pic_chin"}
FRAME_W, FRAME_H, PICTURE_HALF = 480, 220, 110  # half the scene frame; half a picture at scale 1 (a 220 box)
backgrounds = set(scenes_doc["backgrounds"])
# The backgrounds the game draws (StoryScenes.Backgrounds, each its own case; anything else would quietly
# draw the park): scenes.json lists exactly those.
scenes_cs = REPO / "godot" / "Scripts" / "Drawing" / "StoryScenes.cs"
scenes_src = scenes_cs.read_text(encoding="utf-8") if scenes_cs.exists() else ""
drawn_list = re.search(r"Backgrounds\s*=\s*\[(.*?)\];", scenes_src, re.S)
drawn_backgrounds = set(re.findall(r'"([a-z_]+)"', drawn_list.group(1))) if drawn_list else set()
if backgrounds != drawn_backgrounds:
    err(f"stories/scenes.json backgrounds must match StoryScenes.Backgrounds in godot/Scripts/Drawing/StoryScenes.cs: "
        f"only in scenes.json {sorted(backgrounds - drawn_backgrounds)}, only in C# {sorted(drawn_backgrounds - backgrounds)}")
for b in sorted(drawn_backgrounds - {"park"}):  # the park is the default case
    if f'case "{b}"' not in scenes_src: err(f"StoryScenes.cs lists background {b} but has no case drawing it")
new_pictures = {np_["id"]: np_["brief"] for np_ in scenes_doc["newPictures"]}
people_cs = REPO / "godot" / "Scripts" / "Drawing" / "StoryPeople.cs"
people_src = people_cs.read_text(encoding="utf-8") if people_cs.exists() else ""
for pid in new_pictures:
    if f'"{pid}"' not in people_src: err(f"stories: new picture {pid} is not drawn in godot/Scripts/Drawing/StoryPeople.cs")
    if pid in pictures and not LANG: err(f"stories: new picture {pid} already exists; reuse it instead")
# The game draws a picture by its id whatever the language, so the English pictures count too.
english_briefs = {} if not LANG else {pc["id"]: pc["brief"] for pc in json.loads((CONTENT / "asset_manifest.json").read_text(encoding="utf-8"))["pictures"]}
english_pictures = set(english_briefs)
drawable = set(pictures) | english_pictures | set(new_pictures)

def story_picture(pid, fallback):
    """Lists a story picture with the brief it already has (shared pictures keep the English brief in every language)."""
    p(pid, new_pictures.get(pid) or english_briefs.get(pid) or fallback)
SETS = {}
for ss in scenes_doc["sets"]:
    if ss["id"] in SETS: err(f"scene set {ss['id']}: duplicate id")
    SETS[ss["id"]] = ss
    for i, sc in enumerate(ss["scenes"], 1):
        where = f"scene set {ss['id']} scene {i}"
        if sc["background"] not in backgrounds: err(f"{where}: unknown background {sc['background']}")
        if not sc["pictures"]: err(f"{where}: no pictures")
        for pl in sc["pictures"]:
            if pl["picture"] not in drawable: err(f"{where}: picture {pl['picture']} doesn't exist")
            if not 0.2 <= pl["scale"] <= 1.8: err(f"{where}: {pl['picture']} scale must be 0.2-1.8")
            half = PICTURE_HALF * pl["scale"]
            if abs(pl["x"]) + half > FRAME_W or abs(pl["y"]) + half > FRAME_H:
                err(f"{where}: {pl['picture']} at ({pl['x']}, {pl['y']}) spills out of the frame")
used_sets, story_ids = {}, set()
def early_words(story):
    """(decodable words, all words) in a story's sentences, for the early levels."""
    most = EARLY_GROUPS.get(story["level"], 99)
    good = total = 0
    for scene_text in story["text"]:
        for sentence in scene_text:
            for t in reading_words(sentence):
                total += 1
                group = decodable_group(t)
                if (group is not None and group <= most) or t in known_tricky: good += 1
    return good, total
for st in stories:
    sid, lv = st["id"], st["level"]
    where = f"story {sid}"
    if not re.fullmatch(r"[a-z][a-z0-9_]*", sid) or sid in story_ids: err(f"{where}: ids are lower case and unique")
    story_ids.add(sid)
    rule = STORY_LEVELS.get(lv)
    if rule is None: err(f"{where}: level must be 1-5"); continue
    sset = SETS.get(st["scenes"])
    if sset is None: err(f"{where}: unknown scene set {st['scenes']}"); continue
    if st["scenes"] in used_sets: err(f"{where}: scene set {st['scenes']} is already used by {used_sets[st['scenes']]}")
    used_sets[st["scenes"]] = sid
    count = len(sset["scenes"])
    if not rule["scenes"][0] <= count <= rule["scenes"][1]: err(f"{where}: level {lv} needs {rule['scenes']} scenes, its set has {count}")
    if len(st["text"]) != count: err(f"{where}: {len(st['text'])} scenes of text for {count} scenes")
    n = 0
    for i, scene_text in enumerate(st["text"], 1):
        if not rule["sentences"][0] <= len(scene_text) <= rule["sentences"][1]:
            err(f"{where} scene {i}: level {lv} has {rule['sentences']} sentences a scene")
        for sentence in scene_text:
            n += 1
            if len(sentence.split()) > rule["words"]: err(f"{where}: \"{sentence}\" is too long for level {lv} ({rule['words']} words at most)")
            a(f"vo_story_{sid}_{n}", sentence, f"Bip's Stories, {st['title']}: read naturally, like a story.")
    if lv in EARLY_SHARE:
        good, total = early_words(st)
        if total and good / total < EARLY_SHARE[lv]:
            err(f"{where}: only {good} of {total} words are decodable by group {EARLY_GROUPS[lv]} or tricky words (level {lv} needs {EARLY_SHARE[lv]:.0%})")
    if lv == 5:
        for sc in sset["scenes"]:
            for pl in sc["pictures"]:
                if pl["picture"] in FACED_OBJECTS: err(f"{where}: {pl['picture']} has a face; level 5 keeps faces to people and animals")
    questions = st["questions"]
    if len(questions) != rule["questions"]: err(f"{where}: level {lv} asks {rule['questions']} questions")
    kinds = [qu["kind"] for qu in questions]
    for k in kinds:
        if k not in QUESTION_KINDS: err(f"{where}: unknown question kind {k}")
    if lv <= 3 and any(k in ("why", "feel") for k in kinds): err(f"{where}: levels 1-3 ask literal questions only")
    if lv == 4 and (kinds.count("why") != 1 or "feel" in kinds): err(f"{where}: level 4 asks exactly one \"why\" question")
    if lv == 5 and kinds.count("feel") != 1: err(f"{where}: level 5 asks exactly one \"how did they feel\" question")
    answer_n = 0
    for j, qu in enumerate(questions, 1):
        qwhere = f"{where} question {j}"
        a(f"vo_story_{sid}_q{j}", qu["text"], "Bip's Stories question: ask it clearly, then pause.")
        if not 1 <= qu["scene"] <= count: err(f"{qwhere}: scene {qu['scene']} isn't in the story")
        choices = [qu["answer"]] + qu["others"]
        if not rule["choices"][0] <= len(choices) <= rule["choices"][1]: err(f"{qwhere}: level {lv} has {rule['choices']} choices")
        if len({c["text"].lower() for c in choices}) != len(choices): err(f"{qwhere}: two choices say the same")
        if qu["answer"]["text"].lower() in {c["text"].lower() for c in qu["others"]}: err(f"{qwhere}: the answer is among the other choices")
        pics = [c.get("picture") for c in choices]
        if any(pics) and not all(pics): err(f"{qwhere}: give every choice a picture, or none")
        if all(pics) and len(set(pics)) != len(pics): err(f"{qwhere}: two choices share a picture")
        if lv <= 2 and not all(pics): err(f"{qwhere}: levels 1 and 2 answer with pictures")
        for c in choices:
            answer_n += 1
            if c.get("picture"):
                if c["picture"] not in drawable: err(f"{qwhere}: picture {c['picture']} doesn't exist")
                if lv == 5 and c["picture"] in FACED_OBJECTS: err(f"{qwhere}: {c['picture']} has a face; level 5 keeps faces to people and animals")
                story_picture(c["picture"], c["text"])
            clip = word_clip(c["text"])
            if " " in c["text"] or clip not in audio:
                a(f"vo_story_{sid}_a{answer_n}", c["text"], "Bip's Stories answer choice: short and clear.")
stories_game = next((gm for gm in games if gm["id"] == "bips_stories"), None)
if stories_game is None or len(stories_game.get("levels", [])) != len(STORY_LEVELS):
    err(f"games.json bips_stories: needs {len(STORY_LEVELS)} levels, one for each story level")
for lv in STORY_LEVELS:
    have = sum(1 for st in stories if st["level"] == lv)
    if have != STORIES_PER_LEVEL: err(f"stories: level {lv} has {have} stories ({STORIES_PER_LEVEL} needed)")
if len(stories) != STORIES_PER_LEVEL * len(STORY_LEVELS): err(f"stories: {len(stories)} stories, {STORIES_PER_LEVEL * len(STORY_LEVELS)} needed")
if set(used_sets) != set(SETS): err(f"stories: scene sets without a story: {sorted(set(SETS) - set(used_sets))}")
for sc_set in SETS.values():
    for sc in sc_set["scenes"]:
        for pl in sc["pictures"]:
            story_picture(pl["picture"], pl["picture"])

# Clip and picture ids are file names: plain lower-case ASCII, so every computer spells them the same.
for id_ in list(audio) + list(pictures):
    if not re.fullmatch(r"[a-z0-9_]+", id_): err(f"asset id {id_!r}: file names are lower-case ASCII letters, digits and _")

manifest = {"note": "Generated by scripts/validate_content.py. Every voice clip uses the official narrator (CLAUDE.md, 'Art and voice'). Game instructions (vo_find_the_sound etc.), praise and hints are added by each game, not listed here.",
            "audio": sorted(audio.values(), key=lambda x: x["id"]),
            "pictures": sorted(pictures.values(), key=lambda x: x["id"])}
with open(ROOT / "asset_manifest.json", "w", encoding="utf-8") as f:
    json.dump(manifest, f, ensure_ascii=False, indent=2); f.write("\n")

# --- South African English everywhere a child or parent sees or hears: content, the voice script,
# the game's on-screen text and the download page. Ids, file names and notes for people are skipped.
SKIP_KEYS = {"id", "audio", "objectives", "graphemes", "ipa", "rime", "code", "kind", "band", "firstSound",
             "handwritingFamily", "note", "notes", "narratorHint", "mnemonicPicture", "picture", "skills",
             "prerequisites", "content", "island", "skin", "skins", "label", "colour", "shape", "letterName",
             "grapheme", "sequenceRules", "af"}

# --- Voice bands (BipCore.VoiceBands): the script's older children's lines (<clip>_big) are exactly the
# ones the game plays, read from VoiceBands.cs so the two can't drift: each Big version of one of Bip's
# own vo_/hint_ lines has the young row it replaces, plus the Big praise pool (praise_01_big...), and no
# other clip has a _big row (word_big is a real word, never a Big line).
def big_clips():
    core = REPO / "godot" / "BipCore"
    catalogue = (core / "AudioCatalogue.cs").read_text(encoding="utf-8")
    bands = (core / "VoiceBands.cs").read_text(encoding="utf-8")
    lines = dict(re.findall(r'public const string (\w+) = "(\w+)";', catalogue))
    block = re.search(r"BigVersions =\s*\[(.*?)\];", bands, re.S)
    hints = int(re.search(r"HintCount = (\d+);", catalogue).group(1))
    praise = int(re.search(r"BigPraiseCount = (\d+);", bands).group(1))
    versions = [lines[n] for n in re.findall(r"VoiceLine\.(\w+)", block.group(1))] if block else []
    if block and "AudioCatalogue.HintClips" in block.group(1): versions += [f"hint_{n:02}" for n in range(1, hints + 1)]
    return set(versions), {f"{v}_big" for v in versions} | {f"praise_{n:02}_big" for n in range(1, praise + 1)}
big_versions, expected_big = big_clips()
if not big_versions: err("validate_content.py couldn't read BipCore.VoiceBands.BigVersions")
script_name = SCRIPT.relative_to(REPO)
for clip in sorted(c for c in script_clips if c.endswith("_big") and c not in expected_big):
    err(f"{script_name} {clip}: the game never plays this _big clip (only VoiceBands' lines have an older version)")
for clip in sorted(expected_big - script_clips): err(f"{script_name}: no {clip} row (an older children's line VoiceBands plays)")
for base in sorted(big_versions - script_clips): err(f"{script_name}: no {base} row for {base}_big to stand in for")

sa_waiting = set()
OTHER_LANGUAGES = {"af"}  # folders in Content/ that hold another language's own files

def sa_check(where, text):
    for problem in sa_english.problems(text):
        waiting = next((w for w in sa_english.TEMPORARY if f'"{w}"' in problem), None)
        if waiting: sa_waiting.add(f"{where}: {problem} (allowed for now: {sa_english.TEMPORARY[waiting]})")
        else: err(f"{where}: {problem}")

def af_check(where, text):
    for problem in af_spelling.problems(text): err(f"{where}: {problem}")

def walk(check, where, node):
    """Run check over every string a child or parent sees in a content file."""
    if isinstance(node, str): check(where, node)
    elif isinstance(node, list):
        for item in node: walk(check, where, item)
    elif isinstance(node, dict):
        for key, value in node.items():
            # Skip ids and notes for people, but never a whole list of records under the same key
            # (graphemes.json's "graphemes", skills.json's "skills", the currency "notes").
            is_ids = isinstance(value, str) or (isinstance(value, list) and all(isinstance(v, str) for v in value))
            if key in SKIP_KEYS and is_ids: continue
            walk(check, f"{where} {key}", value)

if LANG:
    # An other language's own files and voice script get its spelling check (the shared files are English
    # and were checked on the English run, with the game's on-screen text and the download page).
    for path in sorted(ROOT.rglob("*.json")):
        if path.name == "asset_manifest.json": continue
        walk(af_check, str(path.relative_to(REPO)), load(path.relative_to(ROOT)))
    with open(SCRIPT, encoding="utf-8") as f:
        for row in csv.DictReader(f): af_check(f"{SCRIPT.relative_to(REPO)} {row['file']}", row["text"])
else:
    for path in sorted(p for p in ROOT.rglob("*.json") if p.relative_to(ROOT).parts[0] not in OTHER_LANGUAGES):
        if path.name == "asset_manifest.json": continue
        walk(sa_check, str(path.relative_to(REPO)), load(path.relative_to(ROOT)))
    with open(REPO / "audio" / "script.csv", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            sa_check(f"audio/script.csv {row['file']}", row["text"])
    for path in sorted((REPO / "godot" / "Scripts").rglob("*.cs")):
        if "Dev" in path.parts: continue
        for text in sa_english.cs_strings(path.read_text(encoding="utf-8")):
            sa_check(str(path.relative_to(REPO)), text)
    # BipCore's Texts.cs holds the on-screen words in both languages: the English half of each
    # Pick(language, english, afrikaans) is checked here, the Afrikaans half by af_spelling, and any
    # other literal in it as English. Checked once, on the English run.
    texts_cs = REPO / "godot" / "BipCore" / "Texts.cs"
    pick_pairs, other_texts = sa_english.cs_pick_pairs(texts_cs.read_text(encoding="utf-8"))
    if not pick_pairs: err(f"{texts_cs.relative_to(REPO)}: no Pick(language, english, afrikaans) calls found to check")
    for text in [t for english, _ in pick_pairs for t in english] + other_texts:
        sa_check(f"{texts_cs.relative_to(REPO)} (English)", text)
    for _, afrikaans in pick_pairs:
        for text in afrikaans: af_check(f"{texts_cs.relative_to(REPO)} (Afrikaans)", text)
    site = REPO / "site" / "index.html"
    if site.exists(): sa_check("site/index.html", sa_english.html_text(site.read_text(encoding="utf-8")))

# Background music (checked once, on the English run): Resources/Music/<track>.m4a, one loop for the map and one per island (BipCore.MusicRules).
MUSIC_TRACKS = {"music_map"} | {f"music_{island}" for island in ISLANDS}
music_dir = REPO / "Resources" / "Music"
music_files = [] if LANG else sorted(p for p in music_dir.glob("*") if p.name != "README.md") if music_dir.exists() else []
if music_files and not (music_dir / "README.md").exists():
    err("Resources/Music/README.md is missing (where each loop came from and on what terms; the repo is public)")
for path in music_files:
    if path.suffix != ".m4a" or path.stem not in MUSIC_TRACKS:
        err(f"Resources/Music/{path.name}: music files are .m4a named one of {sorted(MUSIC_TRACKS)}")
if not LANG and not (music_dir / "music_map.m4a").exists():
    err("Resources/Music/music_map.m4a is missing (the map loop; islands without their own play it)")

for note in sorted(sa_waiting): print(f"WAITING (South African English): {note}")

# --- Text colours (BipCore.TextContrast checks the named ones in a unit test): a label, letter or
# parent text never gets a hand-written colour (an alpha, a darker shade, white or a raw hex). Shadows and
# emoji (whose colour fonts ignore the tint) are fine.
TEXT_CALL = re.compile(r"(Sketch\.(Label|FittedLabel|Letter)|ParentUi\.Text|\bp\.Text|\)\.Text)\(")
RAW_COLOUR = re.compile(r"WithAlpha\(|Palette\.White|new Color\(|Darkened\(|Lightened\(|Hex\(")
SCRIPTS = Path(__file__).resolve().parent.parent / "godot" / "Scripts"
for path in sorted(SCRIPTS.rglob("*.cs")):
    if "Dev" in path.parts: continue
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if not TEXT_CALL.search(line) or "Fonts.Emoji" in line: continue
        if RAW_COLOUR.search(re.sub(r"shadow:\s*[^,;]*", "", line)):
            err(f"{path.relative_to(SCRIPTS.parent.parent)}:{number}: a text colour must be a named Palette colour (SoftInk, QuietInk, AlertOrange…), not written by hand")

if errors:
    print(f"{len(errors)} problem(s):")
    for e in errors: print("  -", e)
    sys.exit(1)
print(f"Content OK{f' ({LANG})' if LANG else ''}: {len(objectives)} objectives, {len(G)} graphemes, {len(WORDS)} words, "
      f"{len(levels_doc['levels'])} grid levels, {len(SK)} skills, {len(games)} games. "
      f"Manifest: {len(audio)} voice clips, {len(pictures)} pictures. Music loops: {len(music_files)}.")

# Then each other language with a folder of its own, with the same checks; every language runs, and
# the build fails if any of them failed.
if not LANG:
    failed = [lang for lang in sorted(OTHER_LANGUAGES) if (CONTENT / lang).is_dir()
              and subprocess.run([sys.executable, __file__, "--lang", lang]).returncode != 0]
    if failed: sys.exit(1)
