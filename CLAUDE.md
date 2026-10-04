# Bip Island — project brief for Claude Code

**Start here: read `docs/STATUS.md` first.** It has the current build state, locked
decisions, the workflow that works on this machine, and gotchas. When you finish a
session, update it (dated log entry + current state) and commit it with your changes.

A full-screen learning game for children aged 4–8, for Mac and Windows, built with Godot (C#). Kids explore five islands — Letters & Sounds, Numbers, Words & Spelling, Coding, Art — guided by a robot companion called Bip. Every instruction is spoken, so no reading is needed to play.

The full game design is in `docs/PLAN.md`. Read it before starting a new phase.

The mini-game catalogue, the freshness system (random rounds, skins, Bip's planner, surprises) and the plug-in architecture are in `docs/GAMES.md`. Every mini-game must follow its template: one game type file, content from data lists, rounds generated at random and validated by tests.

The game follows the **Cambridge curriculum** (Early Years, then Primary English 0058, Mathematics 0096 and Computing 0059, Stages 1–3). `docs/CURRICULUM.md` maps every game to Cambridge objective codes and explains the content data. **All game content lives in `Content/` as JSON** (phonics, words, numbers, coding levels, skills): never hard-code words, numbers or levels in code. Run `python3 scripts/validate_content.py` after any content change; it must pass, and CI should run it on every pull request.

## Who this is for right now

The owner's own family first, to test how it plays. Not for sale yet, no App Store. English only.

**South African English, always (owner rule, 3 October 2026; non-negotiable).** Everything in Bip Island is South African: the words, the spelling, the voice, the pictures and the everyday things in them. Not British, not American, not any other country.
- **Spelling:** South African spelling, which follows British spelling: colour, favourite, realise, grey, centre, maths, practise (verb), mum. Never American spelling (color, favorite, realize, gray, center, math, mom). This applies to UI text, voice lines, content, code comments and docs.
- **Words:** the words a South African child and parent actually use: takkies (not trainers or sneakers), gumboots (not wellies), chips (not crisps or fries), cooldrink (not soda or fizzy drink), biscuit (not cookie), sweets (not candy), nappy, dummy, plaster, torch, truck (not lorry), cellphone, dustbin, brinjal, baby marrow, Grade R and Grade 1, "puppy" (not "pup"), "child" (never "kid"; the parent area says "child lock"). A picture of a cob is a "mealie", so "corn" is never shown as a picture.
- **No slang** even when it's South African: say "corner shop", not "spaza"; "picnic", not "braai".
- **South African life:** money is rand and cents; seasons, food, animals, places and weather are South African. No snow-day, Thanksgiving, Halloween, Bonfire Night, robins or squirrels as everyday things.
- **Enforced:** `scripts/sa_english.py` lists the words and spellings; `validate_content.py` runs it over the content, `audio/script.csv`, every on-screen string in `godot/Scripts` and the download page, and fails the build on any hit. When the owner flags a word, add it to `sa_english.py` in the same change.

## How the owner works

- The owner drives everything through **Claude Code cloud sessions**. They will not build locally.
- Cloud sessions run on Linux. They can build, test, screenshot and export the Godot game (`scripts/check_all.sh`; setup in `docs/STATUS.md`), but not run it on a real Mac or Windows PC. GitHub Actions does that: the `godot.yml` Mac and Windows jobs install an older build, update it and check the kid lock.
- The app **updates itself** on the family's Mac and Windows PC via **Velopack**. Every merge to `main` produces a new versioned build, published to the Vercel Blob store, that installed copies pick up (a grown-up confirms the install).
- Keep explanations to the owner short and non-technical; they run a digital agency and are comfortable with web tech.

## Tech decisions (fixed)

| Part | Choice |
| --- | --- |
| Engine and language | Godot 4.7 .NET, C# (net8.0). Project in `godot/`; game logic in `godot/BipCore` (plain C#, no Godot), tested by `godot/BipCore.Tests` (xUnit) |
| Platforms | macOS 14 Sonoma or newer, Windows 10/11 |
| Drawing | Godot 2D nodes, hand-drawn style in code (`godot/Scripts/Drawing/`) |
| Parent area | Godot controls (`godot/Scripts/Parent/`) |
| Audio | `.m4a` clips in `Resources/Audio/` (the source), converted to `.ogg` at build time by `scripts/godot/prepare_assets.py` |
| Persistence | One JSON save file per computer (`BipCore.SaveStore`), local only, up to 4 child profiles |
| Updates | Velopack: feeds `win/` and `osx/` in the public Vercel Blob store; downloaded quietly, installed only after the parent gate |
| CI/CD | GitHub Actions (`.github/workflows/godot.yml`): Linux builds, tests, screenshots and exports both platforms; Mac and Windows runners install, update and check the kid lock; main publishes |
| Download page | `site/`, deployed to Vercel by `godot.yml`'s publish job (`scripts/ci/deploy_site.sh`) |
| Signing | No paid Apple or Windows certificate: the Mac app is ad-hoc signed (first launch: right-click → Open), Windows shows "More info → Run anyway" once. Updates are checked by Velopack either way. Keep it possible to add real signing later through secrets. |

Required GitHub Actions secrets (the owner adds these; see `docs/SETUP.md`):
- `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID` — the download page and the Blob store.

Versioning: `0.MINOR.PATCH`, with the patch number from the GitHub run, so every build is newer than the last (`scripts/godot/set_version.sh`).

## Kid-safety and UX rules (non-negotiable)

- App opens straight into full screen with a kid lock (`godot/Scripts/App/KidLock.cs`, rules in `BipCore.KidLockRules`). Mac: Dock and menu bar hidden, no Cmd-Tab, Force Quit, Cmd-H or Cmd-Q; never block logging out or shutting down. Windows: exclusive full screen, the Windows key, Alt-Tab, Alt/Ctrl-Esc and Alt-F4 blocked while the game is in front (Ctrl-Alt-Del always works). Holding Option (Mac) or Alt (Windows) at launch opens parent mode: a normal window, no lock.
- Parent gate to exit or open settings: hold Esc 3 seconds, then answer an adult maths question, or enter the parent passcode if a parent has set one (4–8 digits, stored only as a salted hash; after 3 wrong tries it falls back to maths).
- No network access except the update check. No ads, analytics, accounts or data collection.
- No time pressure inside games: no countdown timers, lives or "game over". Wrong answer → soft sound, retry, then a spoken hint after 2 misses.
- Click targets at least 120 pt. Design for a mouse, not a trackpad. Any key = "play that sound again".
- **Play-time breaks:** after 20 minutes of play, Bip's battery runs low; the child finishes the current game, then Bip "charges" for a 20-minute break during which games stay closed. Each child has their own play clock, break and daily total (owner decision, 3 October 2026); while one child rests, another can play, but switching away from a resting child to one who would then play needs the parent gate (a child who only tapped a resting sibling's picture can go back to their own freely; straight after the game opens, a sibling with play time left can start without the gate, owner decision 4 October 2026). Track time with the wall clock in persisted storage so quitting and reopening the app cannot skip a break. Parents set play length, break length and an optional daily maximum (shared by all children) behind the parent gate, and can end the playing child's break early.
- **Free choice:** children pick any unlocked game on the map. Bip only recommends (one island glows), and nudges towards another island when one island dominates recent play. Never force a game.
- **Rewards** are collected, never bought: stars → stickers → creatures → decorations → new areas (see `docs/GAMES.md`).

## Phonics rules

- Teach letter **sounds** first (s = "sss", never "ess"); letter names only after phonics group 5.
- Order: the 9 groups in `Content/phonics/graphemes.json` (UK Letters and Sounds sequence, matching Cambridge Stage 1–2): `s a t p i n` / `m d g o c k` / `ck e u r` / `h b f l ff ll ss` / `j v w x y z zz qu` / `ch sh th ng` / `ai ee igh oa oo` / `ar or ur ow oi er` / split digraphs and alternative spellings.
- Only use words whose `decodableFromGroup` the child has unlocked.
- Stop sounds (t p k c b d g) must be clipped — no "tuh". This is the hardest audio problem; see `docs/PLAN.md`.

## Art and voice

- Art style: **hand-drawn** — wobbly ink outlines, paper texture, warm bright colours. Bip is a small, friendly hand-drawn robot.
- Font (**owner decision, 1 October 2026**): **Atkinson Hyperlegible** for *all* text: the letters and words children learn, labels, speech bubbles and the parent area. No Andika, Patrick Hand or system fonts. The owner knows its "a" differs from the school-taught shape and chose it anyway. The font files are bundled in `Resources/Fonts/` (OFL licence, never fetched over the network); the Godot project copies them in (`prepare_assets.py`); use `Fonts.Letters`, `Fonts.Regular`/`Fonts.Bold` from `godot/Scripts/Drawing/Palette.cs`, and `ParentUi` for the parent area.
- Narrator voice (**official, locked in 1 October 2026**): ElevenLabs voice **"Bip Island Narrator"**, voice ID `Mq5hYfc3xyDzuW3pPMck` — a warm, friendly female South African English teacher voice. Every spoken clip in the game uses this voice and nothing else. Generate clips with the `eleven_v3` model unless a test shows another model pronounces pure phonemes better. A clip with no recording yet plays as a short silence; keep the file-name contract below so real clips drop in.
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
- **The repo is public (owner decision, 3 October 2026)**, so GitHub Actions is free again, Mac and Windows runners included. A PR merges only when:
  1. its GitHub CI is green;
  2. another session reviewed it (`/code-review` at high effort, findings on the PR) and every blocking finding is fixed.
  Before pushing, run `scripts/check_all.sh` in the session (it runs everything CI does except the real Mac/Windows install test) and open every screenshot in `build/check/screenshots/`.
- Public means everything committed is visible to anyone: never commit keys, tokens, `.env` files or family details. Secrets live only in GitHub Actions secrets.
- Write unit tests for game logic (mastery tracking, phonics ordering, coding-puzzle interpreter) — they run in CI.
- Update `docs/PLAN.md` "Build phases" status when a phase finishes.
