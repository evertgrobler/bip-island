"""South African English check (CLAUDE.md, "Who this is for right now").

Everything a child or parent sees or hears in Bip Island is South African English: South African words
(takkies, cooldrink, gumboots, a robot for a traffic light) and South African spelling, which follows
British spelling (colour, favourite, realise, grey, mum).
American words and spellings, and British-only words a South African child wouldn't use, are rejected.

Used by validate_content.py over the content, the voice script, the game's on-screen text and the
download page. Add a word here whenever the owner flags one.
"""
import re

# Word (or phrase) → the South African word to use instead.
NOT_SOUTH_AFRICAN = {
    # American
    "mom": "mum", "mommy": "mummy", "candy": "sweets", "cookie": "biscuit", "cookies": "biscuits",
    "diaper": "nappy", "diapers": "nappies", "flashlight": "torch", "trash": "rubbish", "garbage": "rubbish",
    "trash can": "dustbin", "garbage can": "dustbin", "sneakers": "takkies", "faucet": "tap",
    "stroller": "pram", "pacifier": "dummy", "band-aid": "plaster", "bandaid": "plaster", "fries": "chips",
    "french fries": "chips", "apartment": "flat", "elevator": "lift", "mailbox": "postbox", "gas station":
    "garage", "zucchini": "baby marrow", "eggplant": "brinjal", "cilantro": "dhania", "popsicle": "ice lolly",
    "jello": "jelly", "soda": "cooldrink", "pop": None, "sidewalk": "pavement", "parking lot": "parking",
    "kindergarten": "Grade R", "first grade": "Grade 1", "recess": "break", "math": "maths",
    "vacation": "holiday", "fall": None, "faucets": "taps", "closet": "cupboard", "pants": None,
    "eraser": None, "yard": None, "cell phone": "cellphone", "mobile phone": "cellphone",
    # British-only words South African children don't use
    "trainers": "takkies", "wellies": "gumboots", "wellingtons": "gumboots", "crisps": "chips",
    "lorry": "truck", "fizzy drink": "cooldrink", "squash drink": "cooldrink", "car park": "parking",
    "courgette": "baby marrow", "aubergine": "brinjal", "coriander": "dhania", "mobile": None,
    "telly": "TV", "loo": "toilet", "pup": "puppy",
    "kid": "child", "kids": "children", "yak": "an animal South African children know",
    # Slang and words the owner has asked to avoid for children (CLAUDE.md)
    "spaza": "corner shop", "braai": "picnic",
}
# Entries mapped to None are ambiguous ("pop" the sound, "fall" down, "yard" of a farm): not checked.
BANNED = {k: v for k, v in NOT_SOUTH_AFRICAN.items() if v is not None}

# Known hits waiting on something outside the code, printed as WAITING on every run so they can't be
# forgotten. Keep this short, and remove an entry the moment its reason is gone.
TEMPORARY = {
    "yak": "the y sound's picture word until word_yoyo can be recorded (owner chose yo-yo; ElevenLabs is blocked)",
}

# American spellings → South African (British) spelling. Whole words, case-insensitive.
SPELLING_RULES = [
    (re.compile(r"\b(colo)r(s|ful|ed|ing)?\b", re.I), "colour"),
    (re.compile(r"\b(favo)r(ite|ites|ed)?\b", re.I), "favourite"),
    (re.compile(r"\b(behavio)r(s)?\b", re.I), "behaviour"),
    (re.compile(r"\b(neighbo)r(s|hood)?\b", re.I), "neighbour"),
    (re.compile(r"\b(hono)r(s)?\b", re.I), "honour"),
    (re.compile(r"\b(humo)r\b", re.I), "humour"),
    (re.compile(r"\b(cent|theat|met|lit)er(s)?\b", re.I), "-re (centre, theatre, metre, litre)"),
    (re.compile(r"\bgray\b", re.I), "grey"),
    (re.compile(r"\b(organ|real|recogn|apolog|memor|favor|categor|custom|priorit)iz(e|es|ed|ing)\b", re.I), "-ise"),
    (re.compile(r"\b(?:traveled|traveling)\b", re.I), "travelled / travelling"),
    (re.compile(r"\bjewelry\b", re.I), "jewellery"),
    (re.compile(r"\bpajamas\b", re.I), "pyjamas"),
    (re.compile(r"\bcheck(?=s? (?:mark|box))\b", re.I), "tick"),
    (re.compile(r"\bcatalog\b", re.I), "catalogue"),
]


def problems(text):
    """Every non-South-African word or spelling in a piece of text, as messages."""
    found = []
    lower = text.lower()
    for word, instead in BANNED.items():
        if re.search(r"(?<![\w-])" + re.escape(word) + r"(?![\w-])", lower):
            found.append(f'"{word}" is not South African English: use "{instead}"')
    for rule, instead in SPELLING_RULES:
        m = rule.search(text)
        if m:
            found.append(f'"{m.group(0)}" is not South African spelling: use {instead}')
    return found


# C# string literals: on-screen text in the game.
def cs_strings(source):
    """
    Every string literal in C# source: "...", @"...", $"..." (the text outside each {hole}, and
    the literals inside the holes too, as in $"{(n == 1 ? "star" : "stars")}"), and raw triple-quoted strings.
    Comments and character literals are skipped.
    """
    out = []
    i, n = 0, len(source)

    def interpolated(start, verbatim):
        # Reads an interpolated string from just after its opening quote; returns the end index.
        text, j = [], start
        while j < n:
            c = source[j]
            if c == '"':
                if verbatim and source[j + 1:j + 2] == '"': text.append('"'); j += 2; continue
                out.append("".join(text)); return j + 1
            if c == "\\" and not verbatim: text.append(source[j:j + 2]); j += 2; continue
            if c == "{":
                if source[j + 1:j + 2] == "{": text.append("{"); j += 2; continue
                text.append(" ")
                j = hole(j + 1)
                continue
            text.append(c); j += 1
        out.append("".join(text)); return j

    def hole(j):
        # Reads an interpolation hole's code (with any literals in it) up to its closing brace.
        depth = 0
        while j < n:
            c = source[j]
            if c == '"' or (c in "$@" and source[j + 1:j + 2] == '"') or source[j:j + 3] in ('$@"', '@$"'):
                j = literal(j)
                continue
            if c == "'":
                j = source.find("'", j + 2) + 1 if source[j + 1:j + 2] != "\\" else source.find("'", j + 3) + 1
                continue
            if c == "{": depth += 1
            elif c == "}":
                if depth == 0: return j + 1
                depth -= 1
            j += 1
        return j

    def literal(j):
        # Reads one literal starting at j (its prefix included); returns the end index.
        prefix = ""
        while source[j] in "$@": prefix += source[j]; j += 1
        if source[j:j + 3] == '"""':
            end = source.find('"""', j + 3)
            end = n if end < 0 else end
            out.append(re.sub(r"\{[^{}]*\}", " ", source[j + 3:end]) if "$" in prefix else source[j + 3:end])
            return end + 3
        if "$" in prefix:
            return interpolated(j + 1, "@" in prefix)
        k, text = j + 1, []
        while k < n:
            c = source[k]
            if c == '"':
                if "@" in prefix and source[k + 1:k + 2] == '"': text.append('"'); k += 2; continue
                break
            if c == "\\" and "@" not in prefix: text.append(source[k:k + 2]); k += 2; continue
            if c == "\n" and "@" not in prefix: break
            text.append(c); k += 1
        out.append("".join(text))
        return k + 1

    while i < n:
        c = source[i]
        if source.startswith("//", i):
            i = source.find("\n", i); i = n if i < 0 else i; continue
        if source.startswith("/*", i):
            i = source.find("*/", i + 2); i = n if i < 0 else i + 2; continue
        if c == "'":
            close = source.find("'", i + (3 if source[i + 1:i + 2] == "\\" else 2))
            i = n if close < 0 else close + 1; continue
        if c == '"' or (c in "$@" and (source[i + 1:i + 2] == '"' or source[i + 1:i + 3] in ('@"', '$"'))):
            i = literal(i); continue
        i += 1
    return [t for t in out if t.strip()]


HTML_TAG = re.compile(r"<script.*?</script>|<style.*?</style>|<[^>]+>", re.S | re.I)


def html_text(source):
    return HTML_TAG.sub(" ", source)
