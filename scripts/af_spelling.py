#!/usr/bin/env python3
"""Afrikaans spelling for everything an Afrikaans child sees or hears (docs/LANGUAGES.md).

Bip Island's Afrikaans follows the AWS (Afrikaanse Woordelys en Spelreëls) and, like the English side,
is everyday South African Afrikaans without slang. WRONG lists spellings that are wrong or not
standard, with what to write instead; validate_content.py --lang af checks every Afrikaans content
file and the Afrikaans voice script against it. When a reader flags a word, add it here in the same
change.
"""
import re

# Wrong or non-standard (lower case, whole words) -> what to write instead.
WRONG = {
    "aleen": "alleen",
    "asb": "asseblief (written out in full)",
    "seblief": "asseblief",
    "onmiddelik": "onmiddellik",
    "weereens": "weer eens",
    "brille": "bril",
    "boom's": "bome",
    "braai": "piekniek (no slang, the same rule as English)",
    "spaza": "hoekwinkel (no slang, the same rule as English)",
    "lekkers": "lekkergoed",
    "cool": "oulik",
}


# A lone n that is the article written without its apostrophe ("n kat", "N kat"): followed by a word of
# two or more letters, and not the letter itself ("die n", "letter n", or a list of letters "i n o k").
APOSTROPHES = "'\u2019"
LETTER_BEFORE = ("die", "letter", "klank", "lettergreep")


def _article_missing_apostrophe(text, start, end):
    if start > 0 and text[start - 1] in APOSTROPHES: return False
    after = re.match(r"\s+([A-Za-zÀ-ÿ]{2,})", text[end:])
    if not after: return False
    before = re.findall(r"[A-Za-zÀ-ÿ]+", text[:start])
    return not (before and before[-1].lower() in LETTER_BEFORE)


def problems(text):
    """Each wrong word in the text, as 'wrong spelling "x": use y'. Also the article: "'n" (or "\u2019n"), never "n"."""
    found = []
    for match in re.finditer(r"(?<![A-Za-zÀ-ÿ'\u2019])([A-Za-zÀ-ÿ'\u2019]+)", text):
        word = match.group(1)
        if word.lower() == "n":
            if _article_missing_apostrophe(text, match.start(), match.end()):
                found.append(f'wrong spelling "{word}": the article is "\'n"')
        elif word.lower() in WRONG:
            found.append(f'wrong spelling "{word}": use {WRONG[word.lower()]}')
    return found


def self_test():
    """The checker's own tests; validate_content.py runs them on every build."""
    assert problems("Bou 'n toring.") == []
    assert problems("Bou n toring.") and problems("Kom ons braai!")
    # The article at the start of a sentence, in either case, needs its apostrophe.
    assert problems("N kat sit op die mat.") and problems("n Kat sit.")
    # The typographic apostrophe is fine, and so is the letter n itself.
    assert problems("\u2019n Kat sit op die mat.") == []
    assert problems("'n Kat") == [] and problems("Bou \u2019n toring.") == []
    assert problems("Klik op die n.") == [] and problems("Druk die n en die a.") == []
    assert problems("Sounds group 2 (i n o k d r)") == [] and problems("Die letter n is maklik.") == []


if __name__ == "__main__":
    self_test()
    print("af_spelling OK")
