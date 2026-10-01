# Bip Island — project brief for Claude Code

A full-screen native macOS learning game for children aged 4–8. Kids explore four islands — Letters & Sounds, Numbers, Words & Spelling, Coding — guided by a robot companion called Bip. Every instruction is spoken, so no reading is needed to play.

The full game design is in `docs/PLAN.md`. Read it before starting a new phase.

## Who this is for right now

The owner's own family first, to test how it plays. Not for sale yet, no App Store. English only. UK / South African spelling everywhere (colour, mum, favourite) — in code comments, UI text and docs.

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
- Parent gate to exit or open settings: hold Esc 3 seconds, then answer an adult maths question.
- No network access except Sparkle's update check. No ads, analytics, accounts or data collection.
- No timers, lives or "game over". Wrong answer → soft sound, retry, then a spoken hint after 2 misses.
- Click targets at least 120 pt. Design for a mouse, not a trackpad. Any key = "play that sound again".
- Sessions of ~15–20 minutes; parent can set a daily limit.

## Phonics rules

- Teach letter **sounds** first (s = "sss", never "ess"), letter names only after all 26 sounds.
- Order: Jolly Phonics groups — `s a t p i n` / `m d g o c k` / `ck e u r` / `h b f l` / `j v w x y z qu` / then `sh ch th ng ee oo ai` for ages 6–8.
- Stop sounds (t p k c b d g) must be clipped — no "tuh". This is the hardest audio problem; see `docs/PLAN.md`.

## Art and voice

- Art style: **hand-drawn** — wobbly ink outlines, paper texture, warm bright colours. Bip is a small, friendly hand-drawn robot.
- Narrator voice (**official, locked in 1 October 2026**): ElevenLabs voice **"Bip Island Narrator"**, voice ID `Mq5hYfc3xyDzuW3pPMck` — a warm, friendly female South African English teacher voice. Every spoken clip in the game uses this voice and nothing else. Generate clips with the `eleven_v3` model unless a test shows another model pronounces pure phonemes better. Until real clips exist, use placeholder clips (macOS `say` in CI, or silent stubs) and keep the file-name contract below so real clips drop in.
- Bip himself makes short robot sound effects (beeps, whirrs), not speech.

Audio file-name contract (`Resources/Audio/`):
- `snd_<sound>.m4a` — pure phonemes (`snd_s`, `snd_sh`)
- `word_<word>.m4a` — words (`word_sun`)
- `num_<n>.m4a` — numbers
- `vo_<key>.m4a` — instructions (`vo_find_the_sound`)
- `praise_NN.m4a`, `hint_NN.m4a`
Keep the master script in `audio/script.csv` (file, text, notes).

## Working rules for sessions

- Work on a branch, open a PR, and merge to `main` only when the CI build is green — `main` is what ships to the family Mac.
- Write unit tests for game logic (mastery tracking, phonics ordering, coding-puzzle interpreter) — they run in CI.
- Update `docs/PLAN.md` "Build phases" status when a phase finishes.
