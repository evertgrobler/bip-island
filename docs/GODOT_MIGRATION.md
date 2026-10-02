# Moving Bip Island to Godot (Mac + Windows, one game, both auto-updating)

> Status: **approved 1 October 2026, Phase 0 not started.** Tick the checklist at the bottom as phases finish.

## Context

Bip Island is built only with Apple technology: Swift, SpriteKit, SwiftUI, SwiftData, AppKit and Sparkle. The owner wants the same game on Windows, getting the same updates as the Mac. We'll rebuild the game in **Godot 4** so that one codebase produces both a Mac and a Windows app on every merge to `main`.

**Owner decisions (1 Oct 2026):**
- Pause new games during the move. The current Mac app gets bug fixes only. The 17 remaining games are built once, in Godot.
- Carry children's progress over from the Mac app to the Godot version.
- **Full rebuild in one go.** The owner can't test right now, so there are no playtest gates between phases. Automated checks in CI replace them, and the owner tests once, at the switchover.
- **No stores.** Direct download only: `.dmg` for the Mac, `Setup.exe` for Windows, linked from the Vercel download page.
- **Updates need a grown-up** (owner rule from 1 Oct, as in the Swift app): the game checks and downloads quietly, but installs only after the parent gate, through an "Update ready" button.
- **Hosting: Vercel Blob.** The Godot builds (about 400 MB for both computers) are too big for a Vercel Hobby deployment (100 MB), so the installers and update packages go in a public Vercel Blob store (`bip-island-downloads`), created automatically with the existing `VERCEL_TOKEN`. Only the latest version is kept there, to stay inside the free storage.
- **Free and unsigned.** Ad-hoc signed on the Mac, unsigned on Windows. Each computer needs one "Open anyway" click on first install. Paid signing can be switched on later through secrets.

**How the rebuild stays safe without owner testing:**
- The Godot game lives in a new `godot/` folder with its own CI workflow.
- It publishes only to a separate **test feed** (`godot-test/` in the Blob store). The family feed keeps serving the Swift app untouched until the switchover.
- The Swift app stays the fallback the whole time.

## What carries over unchanged, and what gets rebuilt

| Carries over as is | Rebuilt in Godot |
|---|---|
| `Content/` JSON (all games, curriculum, words) | `BipCore/` logic: 3.2k lines plus 1.3k lines of tests (a straight port) |
| `scripts/validate_content.py`, asset manifest | `BipIsland/` app: 7.2k lines (scenes, drawing, audio, parent area) |
| 702 narrator clips (converted to `.ogg`, because Godot can't play `.m4a`) | Kid lock, saves, update system, CI |
| Atkinson Hyperlegible fonts, docs/GAMES.md rules | |

## Key technical choices

1. **Godot 4 .NET with C#**, not GDScript.
   - C# can call Mac and Windows system functions directly, which the kid lock needs.
   - C# has a ready-made update library (Velopack).
   - Swift logic ports almost line for line, and tests run with `dotnet test`.
2. **Logic lives in a plain C# library (`BipCore`) that knows nothing about Godot.**
   - This mirrors today's `BipCore/` package.
   - All 109 tests are ported to xUnit.
   - Big win: **cloud sessions can run the tests themselves on Linux.** Today only CI can.
3. **Updates: Velopack** on both Mac and Windows. The update feed is static files in the public Vercel Blob store, served over HTTPS, with one folder per computer (`win/`, `osx/`).
   - Phase 0 must prove that Velopack works with ad-hoc signing on the Mac.
   - Fallback if it doesn't: a small built-in updater that reads a JSON feed, checks an Ed25519 signature (the same idea as Sparkle) and swaps in the new app.
4. **Kid lock:**
   - **Mac:** call `NSApp.setPresentationOptions` directly, the same options as `BipIsland/App/KidLock.swift`.
   - **Windows:** borderless full screen, a keyboard hook that blocks the Windows key, Alt+Tab and Alt+F4, and a "can't close" rule.
   - **Both:** Godot's close request is refused unless the parent gate passes.
   - **Limit:** Windows never lets an app block Ctrl+Alt+Del. `docs/SETUP.md` will explain how to turn on Windows "Assigned Access" (kiosk mode) for a full lock.
5. **Saves:** a JSON file in Godot's per-user folder, replacing SwiftData. Today's `ChildProgress` is already JSON in `progressData`, so the format maps across directly. The break state stays shared by the whole computer and uses the wall clock.
6. **Pictures:** today's stand-in pictures are Apple emoji, which look different on Windows.
   - Bundle an open-licence colour emoji font (Noto Color Emoji or Twemoji) so the pictures look the same on both computers.
   - Hand-drawn pictures (`Sketch.swift`, `StepPictures.swift`, `PictureNode.swift`) are ported as Godot custom drawing with the same seeded wobble.
7. **CI:**
   - A Linux job validates content, runs the C# tests and exports the Windows build.
   - A macOS job exports the Mac `.app`, ad-hoc signs it and packages it.
   - Velopack packs both, and everything is deployed to Vercel.
   - Version numbering stays `0.MINOR.PATCH` plus the run number.

## Phases (each ends with a PR and green CI; there are no owner playtests until Phase 5)

**Automated checks that replace owner testing (built in Phase 0, then run on every PR):**
- **Tests:** the C# logic tests pass with `dotnet test`. Cloud sessions run them on Linux before each push.
- **Screenshots:** CI opens every scene headless (map, islands, every game, parent area, charging screen) and saves a screenshot to the run.
  - The run fails if a scene errors or a picture, font or clip is missing.
  - The screenshots are a gallery the owner can glance at from their phone.
- **Clip check:** every clip in `asset_manifest.json` exists as `.ogg`, and the game can load it.
- **Install and update test on real GitHub Mac and Windows runners:**
  1. Install build N from the `.dmg` / `Setup.exe`.
  2. Publish build N+1 to a throwaway feed.
  3. Launch build N and confirm it updates itself to N+1 and that saved progress survives.
- **Kid-lock check:** an automated check that the app starts full screen and refuses to close without the parent gate. Real Cmd-Tab and Windows-key blocking can only be confirmed by hand at the switchover.

**Phase 0 — Prove the hard parts, about 2 sessions.** No game porting until all of this passes in CI:
- A Godot project skeleton: one screen with Bip, Atkinson text, one narrator clip (`.ogg`), and an emoji picture.
- CI builds both Mac and Windows from one push.
- The automated install-and-update test is green on both the Mac and Windows runners.

**Phase 1 — Port the logic, about 2 sessions.**
- `BipCore` becomes a C# library: content loader, mastery, phonics order, lesson planner, recommender, play breaks, stickers, progress report, parent gate, and the 11 game round generators plus GridWalker.
- Port all 109 tests. The ported tests must pass, including the round-fairness tests.
- Audio: keep `.m4a` as the source until the switchover; `scripts/godot/prepare_assets.py` converts to `.ogg` at build time. Swap the contract, `make_placeholder_audio.py` and the validator to `.ogg` in Phase 5.

**Phase 2 — Game shell, about 3 sessions.**
- Port the drawing kit from `BipIsland/Drawing/`: Palette, Sketch, Buttons, BipNode, PictureNode, EmojiPictures, StepPictures, Avatars.
- Audio: VoicePlayer, plus BipSounds (made in code at runtime, as now).
- BaseScene and GameCoordinator.
- Input: arrow-key glow, Enter/Space, 1–3 number keys, any other key replays the sound, and the big 72 pt cursor.
- Screens: map, the four island screens, profile picker, charging screen.

**Phase 3 — The 11 games, about 4–5 sessions.**
- Meet, Hunt, Pop, Trace, Monster, Count & Tap, Quick Look, Sound Buttons, Word Builder, Morning Order (tap-to-place), Bip's Path.
- Each game is checked side by side against the Mac version.
- Game levels in every game (games.json `levels`, per-child `gameLevels`, level stars, 8 questions a visit, end-of-visit celebration).

**Phase 4 — Systems and parent area, about 2 sessions.**
- Profiles (up to 4), saves, play breaks, sticker book, mystery box, recommendations and nudge.
- Parent gate: hold Esc for 3 seconds, then a maths question, or the optional parent passcode (salted SHA-256; 3 wrong tries fall back to maths).
- Sticker book: the two **th**, **oo** and **ow** stickers look identical (same in Swift); add the mnemonic picture under each.
- "Update ready" button: installs a downloaded update only after the parent gate.
- Parent area tabs (Progress, Children, Settings), rebuilt with Godot's UI controls.

**Phase 5 — Switch over, about 1–2 sessions. This is the owner's single test.**
- The owner installs the test-feed build on the Mac and the Windows PC and plays it for a few days. Fixes are pushed to the test feed.
- Only when the owner is happy does the family feed switch to Godot.
- The last Swift update (via Sparkle) adds **"export progress"**. It writes every child's progress to a file in a shared folder, and the Godot app imports it on first launch.
- The owner installs the Godot Mac app once by hand and removes the old one. Installing on the Windows PC is likewise a one-time step.
- Retire the Swift code, `project.yml` and the Xcode CI.
- Switch the audio source and contract to `.ogg` (`Resources/Audio`, `make_placeholder_audio.py`, the validator, `AudioScriptTests`).
- Update `CLAUDE.md` (tech table, audio contract, kid-lock rules for Windows), `docs/SETUP.md`, `docs/PLAN.md` and `docs/STATUS.md`.
- Then resume new games (waves 2b/3/4), built once in Godot.

**Total: roughly 14–16 sessions.** The Mac app keeps working throughout.

## Costs and risks for the owner

- **Windows warning on install:** without a code-signing certificate (about $100–400 a year), Windows shows "Windows protected your PC" → More info → Run anyway, once. Optional; it can be added later through secrets.
- **Windows kid lock is weaker** than the Mac's unless kiosk mode is set up on that PC.
- **No new games for a few weeks** while the port happens.
- **New secrets:** no Windows secrets are needed at first. The Vercel secrets are reused. The Sparkle key retires after the switchover.

## Work order

Run Phases 0 → 4 back to back, one PR per phase (or per group of games), each merged when CI is green. Godot builds go only to the test feed. Stop and report to the owner only if a Phase 0 check can't be made to pass; if that happens, try the fallback updater (key choice 3) first.

## Verification

- **Phase 0 gate:** the automated install-and-update test is green on the Mac and Windows runners, and the screenshot and clip checks run.
- **Phase 1:** the ported tests pass with `dotnet test`, both in a cloud session and in CI.
- **Phases 2–4:** every PR produces Mac and Windows builds on the test feed, plus a screenshot gallery of every scene. The CI checks above must pass.
- **Phase 5 (the owner's one test):** the owner plays the test-feed build on both computers. The kid lock is checked by hand. After the import, children's stars, stickers and sound stages match the Swift app.

## Checklist

- [x] Phase 0 — Godot skeleton, `godot.yml` CI, test feed, install-and-update test green on Mac and Windows
- [x] Phase 1 — `BipCore` in C# (`godot/BipCore`), every Swift test ported and passing (`godot/BipCore.Tests`, 148 xUnit tests, run in CI)
- [x] Phase 1 — audio: the source clips stay `.m4a` in `Resources/Audio` (the Swift app still uses them); `scripts/godot/prepare_assets.py` converts them to `.ogg` at build time. The source switches to `.ogg` in Phase 5.
- [ ] Phase 2 — Drawing kit, audio, coordinator, input, map, islands, profiles and charging screens
  - [x] Drawing kit: Palette, Sketch, Bip, Buttons, PictureNode, EmojiPictures, StepPictures, Avatars (picture gallery dev scene)
  - [x] Game shell: SaveStore (one JSON save), GameCoordinator, BaseScreen (clicks, keys, timers), big cursor, Bip's sounds, map, four islands, Who's playing?, charging, sticker book, mystery box; a click-through walk test (`--bip-walk`) in `check_all.sh` and CI. Games show "Coming soon!" until Phase 3.
- [ ] Phase 3 — The 11 games
- [ ] Phase 4 — Profiles, saves, breaks, stickers, mystery box, recommendations, parent gate and parent area
  - [x] Profiles, saves, breaks, stickers, mystery box, recommendations (with the Phase 2 shell)
  - [x] Parent gate (hold Esc → maths or passcode), parent area (Progress, Children, Settings), "Update ready" button that installs only after the gate
  - [x] Native kid lock: Mac (borderless cover window + presentation options; quits when the Mac powers off) and Windows (exclusive full screen + keyboard hook for the Windows key, Alt-Tab, Alt/Ctrl-Esc). Option/Alt at launch = parent mode. First proof on real machines: the `--bip-kidlock-check` step in the Mac/Windows CI jobs (a warning until it has passed once).
- [ ] Phase 5 — Owner test, progress export/import, family feed switched, Swift code retired, docs updated

## Port notes from the Swift sessions (1 Oct 2026)

- **Levels:** games.json `levels` (checked by the validator). `GameEntry.startingLevel` is the last level whose band is ≤ the child's band. `ChildProgress.recordGameAnswer` goes up a level after 3 right answers in a row and back one after 2 misses, clamped to the game's range. `GameLevelTests` is the spec.
- **Parent passcode:** `BipCore/ParentPasscode.swift`: a salted SHA-256 hash, 4–8 digits, 3 wrong tries fall back to maths. Swift stores it under the UserDefaults key `bip.parentPasscode`; migrate it with the progress export.
- **Update prompt:** `UpdateController.swift` and `RootView`'s `UpdateReadyButton`. The gate model's `openForUpdate()` installs only after unlocking.
- **Word Builder:** tiles show `PhonicsSound.grapheme` (not ids). Split-digraph words are left out. Spare tiles come from unlocked, non-confusable sounds, with `spares` set per level.
- **Bip's Path:** `GridWalker.facings` drives the arrow that shows which way Bip is facing.
