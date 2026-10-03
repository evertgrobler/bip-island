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
    (re.compile(r"\btraveled|traveling\b", re.I), "travelled / travelling"),
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


# C# string literals ("...", $"...", @"..."): on-screen text in the game.
CS_STRING = re.compile(r'\$?@?"((?:[^"\\\n]|\\.)*)"')


def cs_strings(source):
    """The string literals in C# source, skipping attribute and interpolation hole contents."""
    for m in CS_STRING.finditer(source):
        text = re.sub(r"\{[^{}]*\}", " ", m.group(1))
        if text.strip():
            yield text


HTML_TAG = re.compile(r"<script.*?</script>|<style.*?</style>|<[^>]+>", re.S | re.I)


def html_text(source):
    return HTML_TAG.sub(" ", source)
