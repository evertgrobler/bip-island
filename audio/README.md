# Audio

`script.csv` is the master voice script: every clip the game plays, with the exact words and pronunciation notes for ElevenLabs.

| Column | Meaning |
| --- | --- |
| `file` | File name in `Resources/Audio/` (the naming contract in `CLAUDE.md`) |
| `text` | Exactly what the narrator says |
| `notes` | How to say it, and how to trim it (important for the pure sounds) |
| `placeholder` | Optional: what the temporary computer voice says instead, if different from `text` |

## Placeholders until the real voice is ready

The real narrator (a female South African English voice made in ElevenLabs) is being designed separately. Until then, every build makes **placeholder** clips with the Mac's built-in robot voice (`scripts/make_placeholder_audio.py`, run in GitHub Actions). They are clearly placeholders: robotic, and listed in the parent area as "placeholder voice clips".

Placeholder phonics sounds are *not* good enough to teach with ("t, as in tent" instead of a clean "t"). Swap in the real clips before relying on the game for phonics.

## Adding the real clips

1. Generate each line in ElevenLabs, trim it, and export as `.m4a` (AAC).
2. Name it exactly as in the `file` column, e.g. `snd_s.m4a`.
3. Put it in `Resources/Audio/` and merge to `main`. The next build uses it and stops making a placeholder for it.

When the game needs a new clip, add a row here at the same time. A unit test fails if the game and this script disagree.

## Bip's beeps

Bip's beeps and whirrs, the soft "try again" sound and the bubble pops are made by code (`BipIsland/Audio/BipSounds.swift`), so they need no files.
