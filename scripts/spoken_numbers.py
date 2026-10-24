"""Numbers as the narrator says them, shared by validate_content.py (the text of each clip) and
speak_clips.py (the Whisper check), so the two can't drift apart."""

NUMBER_WORDS = ("zero one two three four five six seven eight nine ten eleven twelve thirteen fourteen "
                "fifteen sixteen seventeen eighteen nineteen twenty").split()


def number_word(n):
    """0-20 as a word: 7 -> "seven"."""
    return NUMBER_WORDS[n]
