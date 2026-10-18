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


def problems(text):
    """Each wrong word in the text, as 'wrong spelling "x": use y'. Also the article: "'n", never "n"."""
    found = []
    for match in re.finditer(r"(?<![A-Za-zÀ-ÿ'])([A-Za-zÀ-ÿ']+)", text):
        word = match.group(1)
        if word == "n" and not text[:match.start()].endswith("'"):
            found.append('wrong spelling "n": the article is "\'n"')
        elif word.lower() in WRONG:
            found.append(f'wrong spelling "{word}": use {WRONG[word.lower()]}')
    return found


if __name__ == "__main__":
    assert problems("Bou 'n toring.") == []
    assert problems("Bou n toring.") and problems("Kom ons braai!")
    print("af_spelling OK")
