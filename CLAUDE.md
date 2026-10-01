# Bip Island — project brief for Claude Code

**Start here: read `docs/STATUS.md` first.** It has the current build state, locked
decisions, the workflow that works on this machine, and gotchas. When you finish a
session, update it (dated log entry + current state) and commit it with your changes.

A full-screen native macOS learning game for children aged 4–8. Kids explore four islands — Letters & Sounds, Numbers, Words & Spelling, Coding — guided by a robot companion called Bip. Every instruction is spoken, so no reading is needed to play.

The full game design is in `docs/PLAN.md`. Read it before starting a new phase.

The mini-game catalogue, the freshness system (random rounds, skins, Bip's planner, surprises) and the plug-in architecture are in `docs/GAMES.md`. Every mini-game must follow its template: one game type file, content from data lists, rounds generated at random and validated by tests.

The game follows the **Cambridge curriculum** (Early Years, then Primary English 0058, Mathematics 0096 and Computing 0059, Stages 1–3). `docs/CURRICULUM.md` maps every game to Cambridge objective codes and explains the content data. **All game content lives in `Content/` as JSON** (phonics, words, numbers, coding levels, skills): never hard-code words, numbers or levels in Swift. Run `python3 scripts/validate_content.py` after any content change; it must pass, and CI should run it on every pull request.

## Who this is for right now

The owner's own family first, to test how it plays. Not for sale yet, no App Store. English only. UK / South African spelling everywhere (colour, mum, favourite) — in code comments, UI text and docs. All words the child hears or sees are proper South African English: no slang (say "corner shop", not "spaza"; "picnic", not "braai").

## How the owner works

- The owner drives everything through **Claude Code cloud sessions**. They do not have Xcode set up and will not build locally.
- Cloud sessions run on Linux: they cannot run `xcodebuild` or launch the app. All building happens in **GitHub Actions on a macOS runner**.
- The app **updates itself** on the owner's Mac via **Sparkle**. Every merge to `main` must produce a new versioned build that the installed app picks up automatically.
- Keep explanations to the owner short and non-technical; they run a digital agency and are comfortable with web tech, less so with Xcode.

## Tech decisions (fixed)

| Part | Choice |
| --- | --- |
| Language | Swift 5.10+, macOS 14 Sonoma minimum |
| Project file | XcodeGen (`project.yml`) — never commit a hand-edited `.xcodeproj`; CI generates it |
| Game scenes | SpriteKit |
| Menus, parent area | SwiftUI |
| Audio | AVFoundation, bundled `.m4a` clips in `Resources/Audio/` |
| Persistence | SwiftData, local only, up to 4 child profiles |
| Updates | Sparkle 2 (Swift Package), EdDSA-signed |
| CI/CD | GitHub Actions, `macos-latest` runner |
| Update hosting | The repo is **private**, so release assets can't be fetched anonymously. Host `appcast.xml` + the zipped `.app` on a small public static site (Vercel). The source stays private. |
| Signing | No paid Apple Developer account yet: ad-hoc sign (`codesign -s -`). First launch needs right-click → Open; Sparkle updates work after that. Design so Developer ID signing + notarisation can be switched on later via secrets. |

Required GitHub Actions secrets (the owner adds these; document exactly how in `docs/SETUP.md`):
- `SPARKLE_PRIVATE_KEY` — EdDSA key from Sparkle's `generate_keys`. The public key goes in `Info.plist` (`SUPublicEDKey`).
- `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID` — to deploy the update feed.

Versioning: `CFBundleShortVersionString` = `0.MINOR.PATCH`, `CFBundleVersion` = GitHub run number, so every build is newer than the last.

## Kid-safety and UX rules (non-negotiable)

- App opens straight into full screen with a kid lock: hide Dock and menu bar, disable process switching and Cmd-Q (`NSApplication.PresentationOptions` + `applicationShouldTerminate`).
- Parent gate to exit or open settings: hold Esc 3 seconds, then answer an adult maths question, or enter the parent passcode if a parent has set one (4–8 digits, stored only as a salted hash; after 3 wrong tries it falls back to maths).
- No network access except Sparkle's update check. No ads, analytics, accounts or data collection.
- No time pressure inside games: no countdown timers, lives or "game over". Wrong answer → soft sound, retry, then a spoken hint after 2 misses.
- Click targets at least 120 pt. Design for a mouse, not a trackpad. Any key = "play that sound again".
- **Play-time breaks:** after 20 minutes of play, Bip's battery runs low; the child finishes the current game, then Bip "charges" for a 20-minute break during which games stay closed. Track time with the wall clock in persisted storage so quitting and reopening the app cannot skip a break. Parents set play length, break length and an optional daily maximum behind the parent gate, and can end a break early.
- **Free choice:** children pick any unlocked game on the map. Bip only recommends (one island glows), and nudges towards another island when one island dominates recent play. Never force a game.
- **Rewards** are collected, never bought: stars → stickers → creatures → decorations → new areas (see `docs/GAMES.md`).

## Phonics rules

- Teach letter **sounds** first (s = "sss", never "ess"); letter names only after phonics group 5.
- Order: the 9 groups in `Content/phonics/graphemes.json` (UK Letters and Sounds sequence, matching Cambridge Stage 1–2): `s a t p i n` / `m d g o c k` / `ck e u r` / `h b f l ff ll ss` / `j v w x y z zz qu` / `ch sh th ng` / `ai ee igh oa oo` / `ar or ur ow oi er` / split digraphs and alternative spellings.
- Only use words whose `decodableFromGroup` the child has unlocked.
- Stop sounds (t p k c b d g) must be clipped — no "tuh". This is the hardest audio problem; see `docs/PLAN.md`.

## Art and voice

- Art style: **hand-drawn** — wobbly ink outlines, paper texture, warm bright colours. Bip is a small, friendly hand-drawn robot.
- Font (**owner decision, 1 October 2026**): **Atkinson Hyperlegible** for *all* text: the letters and words children learn, labels, speech bubbles and the parent area. No Andika, Patrick Hand or system fonts. The owner knows its "a" differs from the school-taught shape and chose it anyway. The font files are bundled in `Resources/Fonts/` (OFL licence, never fetched over the network); use `Fonts.letters`, `Fonts.regular`/`Fonts.bold` or `Fonts.ui(size, bold:)` from `BipIsland/Drawing/Palette.swift`.
- Narrator voice (**official, locked in 1 October 2026**): ElevenLabs voice **"Bip Island Narrator"**, voice ID `Mq5hYfc3xyDzuW3pPMck` — a warm, friendly female South African English teacher voice. Every spoken clip in the game uses this voice and nothing else. Generate clips with the `eleven_v3` model unless a test shows another model pronounces pure phonemes better. Until real clips exist, use placeholder clips (macOS `say` in CI, or silent stubs) and keep the file-name contract below so real clips drop in.
- Bip himself makes short robot sound effects (beeps, whirrs), not speech.

Audio file-name contract (`Resources/Audio/`):
- `snd_<sound>.m4a` — pure phonemes (`snd_s`, `snd_sh`)
- `word_<word>.m4a` — words (`word_sun`)
- `num_<n>.m4a` — numbers
- `vo_<key>.m4a` — instructions (`vo_find_the_sound`)
- `praise_NN.m4a`, `hint_NN.m4a`
- `name_<letter>.m4a` (letter names), `money_<coin>.m4a`, `word_contraction_<x>.m4a`
`Content/asset_manifest.json` (generated by the validator) lists every clip the content needs with the exact text to speak. Game instructions, praise and hints go in `audio/script.csv` (file, text, notes).

## Working rules for sessions

- Work on a branch, open a PR, and merge to `main` only when the CI build is green — `main` is what ships to the family Mac.
- Write unit tests for game logic (mastery tracking, phonics ordering, coding-puzzle interpreter) — they run in CI.
- Update `docs/PLAN.md` "Build phases" status when a phase finishes.
