#!/usr/bin/env python3
"""Validate Bip Island content packs and write the asset manifest.

Run from the repo root:  python3 scripts/validate_content.py
Exits non-zero on any problem, so CI can block a bad update.
Writes Content/asset_manifest.json: every voice clip (with the text to speak)
and every picture the content needs.
"""
import json, sys
from collections import deque
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent / "Content"
errors = []

def load(rel):
    with open(ROOT / rel, encoding="utf-8") as f:
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

for w in words_doc["words"]:
    parts = w["graphemes"]
    missing = [p for p in parts if p not in G]
    if missing: err(f"word {w['word']}: unknown graphemes {missing}"); continue
    if spell(parts) != w["word"]: err(f"word {w['word']}: graphemes spell '{spell(parts)}'")
    if max(G[p]["group"] for p in parts) != w["decodableFromGroup"]: err(f"word {w['word']}: wrong decodableFromGroup")
    if w["picturable"] and not w.get("picture"): err(f"word {w['word']}: picturable but no picture id")
    check_objs(f"word {w['word']}", w["objectives"])

# enough words per group for rounds of 3 options with distinct first sounds
for grp in range(1, max(g["group"] for g in ph["graphemes"]) + 1):
    pool = [w for w in words_doc["words"] if w["decodableFromGroup"] <= grp and w["picturable"]]
    if len({w["firstSound"] for w in pool}) < 3:
        err(f"group {grp}: fewer than 3 different first sounds among picturable words")

# --- sentences decodable at their group
known_tricky = {t.lower() for t in tricky["stage1"]["words"] + tricky["stage2"]["words"]} | {"in", "on", "it", "a", "is", "has", "at", "an"}
for s in sentences:
    for tok in s["text"].split():
        t = tok.strip(".,!?").lower()
        base = t[:-1] if t.endswith("s") and t[:-1] in WORDS else t
        if base in WORDS:
            if WORDS[base]["decodableFromGroup"] > s["decodableFromGroup"]: err(f"{s['id']}: '{t}' needs a later group")
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

# --- every phonics group is taught by the skill snd_g<group> (the game links them by this id)
for grp in ph["groups"]:
    if f"snd_g{grp['group']}" not in SK: err(f"phonics group {grp['group']}: no skill snd_g{grp['group']} in skills.json")

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
LEVEL_KEYS = {"band", "countTo", "choices", "flashTenths", "bubbles", "speedPercent", "soundCounts", "gridBand", "cards", "spares", "note"}
BANDS = ["foundation", "stage1", "stage2", "stage3"]
for gm in games:
    levels = gm.get("levels", [])
    for i, lv in enumerate(levels):
        where = f"game {gm['id']} level {i + 1}"
        for k in lv:
            if k not in LEVEL_KEYS: err(f"{where}: unknown key {k}")
        for k in ("band", "gridBand"):
            if k in lv and lv[k] not in BANDS: err(f"{where}: unknown {k} {lv[k]}")
        for k in ("countTo", "choices", "flashTenths", "bubbles", "speedPercent", "cards"):
            if k in lv and (not isinstance(lv[k], int) or lv[k] < 1): err(f"{where}: {k} must be a positive whole number")
        if "choices" in lv and not 2 <= lv["choices"] <= 5: err(f"{where}: choices must be 2 to 5")
        if "bubbles" in lv and not 3 <= lv["bubbles"] <= 8: err(f"{where}: bubbles must be 3 to 8")
        if "spares" in lv and (not isinstance(lv["spares"], int) or not 0 <= lv["spares"] <= 4): err(f"{where}: spares must be 0 to 4")
        if "cards" in lv and not 2 <= lv["cards"] <= 6: err(f"{where}: cards must be 2 to 6")
        if "soundCounts" in lv and not any(w["picturable"] and w["soundCount"] in lv["soundCounts"] for w in words_doc["words"]):
            err(f"{where}: no picturable words with {lv['soundCounts']} sounds")
    # A level without a band carries on the band before it.
    current, bands = 0, []
    for lv in levels:
        current = BANDS.index(lv["band"]) if lv.get("band") in BANDS else current
        bands.append(current)
    if bands != sorted(bands): err(f"game {gm['id']}: level bands must not go backwards")
for gm in games:
    for s in gm["skills"]:
        if s not in SK: err(f"game {gm['id']}: unknown skill {s}")
    check_objs(f"game {gm['id']}", gm["objectives"])

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

for g in ph["graphemes"]:
    a(g["audio"], g["grapheme"], f"PURE SOUND /{g['ipa']}/ ({g['kind']}). {g['narratorHint']}")
    if g.get("letterName"): a(f"name_{g['id']}", g["letterName"], "Letter name, for the letter-names activity only.")
    p(g["mnemonicPicture"], f"{g['mnemonicWord']} (picture for the sound {g['grapheme']})")
    a(f"word_{g['mnemonicWord']}", g["mnemonicWord"])
for w in words_doc["words"]:
    a(w["audio"], w["word"])
    if w["picturable"]: p(w["picture"], w["word"])
for t in tricky["stage1"]["words"] + tricky["stage2"]["words"]:
    a(f"word_{t.lower()}", t)
for s in sentences:
    a(s["audio"], s["text"])
    p(s["pictureRight"], s["pictureRightBrief"]); p(s["pictureWrong"], s["pictureWrongBrief"])
for e in endings["plurals_s"] + endings["plurals_es"]:
    a(f"word_{e['plural']}", e["plural"])
for e in endings["verbs"]:
    for k in ("s", "ed", "ing"):
        if k in e: a(f"word_{e[k]}", e[k])
for i, hs in enumerate(homophones, 1):
    for j, s in enumerate(hs["sentences"], 1):
        a(f"vo_hom_{i:02d}_{j}", s["text"].replace("___", s["answer"]), "Read the whole sentence naturally.")
for pr in contractions:
    key = pr["short"].lower().replace("'", "")
    a(f"word_contraction_{key}", pr["short"]); a(f"word_{pr['long'].lower().replace(' ', '_')}", pr["long"])
for n in range(0, 101):
    a(f"num_{n}", str(n), "Say the number.")
for o in numbers["countingObjects"]:
    p(o["picture"], o["id"])
    plural = o["audioPlural"][len("word_"):]
    if not o["audioPlural"].startswith("word_"): err(f"counting object {o['id']}: plural clip must be word_<plural>")
    a(o["audioPlural"], plural.replace("_", " "))
cur = numbers["currency"]
for c in cur["coins"] + cur["notes"]:
    p(f"pic_money_{c['id']}", f"South African {c['label']} {'coin' if c in cur['coins'] else 'note'}, in the game's own hand-drawn style")
    lab = c["label"]
    a(f"money_{c['id']}", f"{lab[1:]} rand" if lab.startswith("R") else f"{lab[:-1]} cents")
for it in cur["shopItems"]:
    p(it["picture"], it["id"])
for st in seqs:
    for c in st["cards"]:
        a(c["audio"], c["text"]); p(c["picture"], f"{st['id']} step {c['n']}: {c['text']}")

manifest = {"note": "Generated by scripts/validate_content.py. Every voice clip uses the official narrator (ElevenLabs 'Bip Island Narrator'). Game instructions (vo_find_the_sound etc.), praise and hints are added by each game, not listed here.",
            "audio": sorted(audio.values(), key=lambda x: x["id"]),
            "pictures": sorted(pictures.values(), key=lambda x: x["id"])}
with open(ROOT / "asset_manifest.json", "w", encoding="utf-8") as f:
    json.dump(manifest, f, ensure_ascii=False, indent=2); f.write("\n")

if errors:
    print(f"{len(errors)} problem(s):")
    for e in errors: print("  -", e)
    sys.exit(1)
print(f"Content OK: {len(objectives)} objectives, {len(G)} graphemes, {len(WORDS)} words, "
      f"{len(levels_doc['levels'])} grid levels, {len(SK)} skills, {len(games)} games. "
      f"Manifest: {len(audio)} voice clips, {len(pictures)} pictures.")
