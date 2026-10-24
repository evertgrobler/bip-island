"""Numbers as the narrator says them, shared by validate_content.py (the text of each clip) and
speak_clips.py (the Whisper check), so the two can't drift apart. English is the default; each other
home language (docs/LANGUAGES.md) has its own words."""

NUMBER_WORDS = ("zero one two three four five six seven eight nine ten eleven twelve thirteen fourteen "
                "fifteen sixteen seventeen eighteen nineteen twenty").split()

NUMBER_WORDS_BY_LANG = {
    "": NUMBER_WORDS,
    "af": ("nul een twee drie vier vyf ses sewe agt nege tien elf twaalf dertien veertien vyftien sestien "
           "sewentien agtien negentien twintig").split(),
}

# "even" and "odd" as said in a sentence ("Seven is odd").
EVEN_ODD = {"": ("even", "odd"), "af": ("ewe", "onewe")}


def number_word(n, lang=""):
    """0-20 as a word: 7 -> "seven" ("sewe" in Afrikaans)."""
    return NUMBER_WORDS_BY_LANG[lang][n]
