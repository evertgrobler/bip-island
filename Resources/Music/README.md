# Background music

The calm loops that play on the map and the island screens (rules in `godot/BipCore/MusicRules.cs`).
The repository is public, so every file here records where it came from and on what terms.

| File | Made | Source | Prompt (instruments) |
| --- | --- | --- | --- |
| `music_map.m4a` | 11 Oct 2026 | ElevenLabs Music v2.5, owner's first ElevenLabs account | soft marimba, ukulele, shaker and soft bongo, ~85 BPM |
| `music_letters.m4a` | 11 Oct 2026 | ElevenLabs Music v2.5, owner's second ElevenLabs account | glockenspiel and xylophone over fingerpicked guitar, ~85 BPM |
| `music_numbers.m4a` | 11 Oct 2026 | ElevenLabs Music v2.5, owner's second account | pizzicato strings, marimba, finger clicks and woodblock, ~90 BPM |
| `music_words.m4a` | 11 Oct 2026 | ElevenLabs Music v2.5, owner's second account | strummed guitar, wooden flute and hand drum, ~80 BPM |
| `music_coding.m4a` | 11 Oct 2026 | ElevenLabs Music v2.5, owner's second account | soft synth arpeggio, music-box chimes, marimba and a light beat, ~88 BPM |
| `music_art.m4a` | 11 Oct 2026 | ElevenLabs Music v2.5, owner's second account | kalimba and harp over soft pads and brushed percussion, ~78 BPM |

All six were generated on request for Bip Island: instrumental, 90 seconds. Each full prompt also asked
for "calm, gentle children's background music … major key … even energy throughout with no build or
drop, so it loops smoothly; no vocals, no sudden loud notes". Each was turned into an 87-second loop
with `scripts/make_music_loop.sh` (a 3-second blended seam, one fixed gain to -20 LUFS, AAC 160 kbps).
None uses samples from anyone else's recording.

## Terms

The loops fall under ElevenLabs' terms for generated audio and the plan of the account that made them.
Bip Island is a free family game: no sales, ads or store listing, just a direct download.

- **Still to confirm (owner):** which plan each account was on when the loops were made (11 October
  2026), and that the plan allows the audio to be published in a public repository and shipped in a
  free game. If a plan doesn't allow it, regenerate that loop on an account whose plan does, with the
  same prompt, and replace the file.
- The ElevenLabs flows with the exact prompts: "Bip Island music" (first account, map loop) and
  "Bip Island island music" (second account, island loops).
