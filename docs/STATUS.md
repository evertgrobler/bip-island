# Bip Island — Session Status (read this first)

> Every agent session starts here. Read this file, then `docs/PLAN.md` and `docs/GAMES.md`
> for the design. **When you finish work, update this file**: append a dated entry under
> "Session log", refresh "Where things stand", and commit it with your changes.

## Where things stand (1 October 2026, late evening)

- **Phase 0 (voice): done.** All 702 narrator clips bundled in `Resources/Audio/` (ElevenLabs
  "Bip Island Narrator", `eleven_v3`). Trimmed deaf (by waveform, not ear): stops 0.1 s,
  vowels 0.4 s, stretchies ~1 s. Owner is ear-checking `t`/`p`/`k` in playtests.
- **Phase 1 + 2a (games): done, shipped.** 11 of 28 games live across all 4 open islands:
  Letters (Meet, Hunt, Pop, Trace, Monster), Numbers (Count & Tap, Quick Look),
  Words (Sound Buttons, Word Builder), Coding (Morning Order, Bip's Path + GridWalker).
- **Systems live:** Bip's recommendations + nudge (map glow), wall-clock play breaks +
  charging scene + parent settings, sticker book (118), daily mystery box, parent area.
- **Keyboard play live:** arrows move an orange glow, Enter/Space chooses, 1-3 picks,
  coding fully keyboard drivable (arrows + Enter + Backspace, 1-7 palette). Other keys replay.
- **Pictures:** 15 hand-drawn + emoji stand-ins (`BipIsland/Drawing/EmojiPictures.swift`).
  Each table row is the still-to-draw list. Full coverage of all 256 manifest pictures.
  Morning Order's 123 step cards (30 stories: 20 foundation, 8 stage 1, 2 stage 2) are small scenes (`StepPictures.swift`): shared ground per set
  (soil, sand, pond), hand-drawn bread/frogs/oven/sandcastle + emoji, each step builds on the last.
- **Morning Order is tap-to-place** (no dragging): tap a card → it flies to the next numbered
  space and its line is spoken; tap a placed card to send it back; auto-checks when full.
- **Big cursor:** `BipIsland/App/BigCursor.swift` — 72 pt orange hand-drawn arrow on the game view
  (`GameSKView` cursor rects). Parent gate panel keeps the normal system pointer.
- **Build:** CI green on `main` (109 unit tests). Latest family build: **0.1.0 build 25**,
  `Downloads/bip-island-keyboard/` on the owner's Mac. App updates itself via Sparkle.
- **Font:** Atkinson Hyperlegible everywhere (game text and parent area), bundled in
  `Resources/Fonts/`. Owner decision, see CLAUDE.md "Art and voice".
- **Profiles:** up to 4 children. "Who's playing?" opens the game when there's more than one
  (animal pictures, name, stars); an animal badge bottom-left on the map switches child.
  Parents add, rename, set age and picture, or remove children in the parent area.
- **Parent area:** Progress tab per child (stars, stickers, minutes today/this week, answers
  this week, every skill's status by island, every phonics sound's stage, what needs practice,
  reviews due, recent games), Children tab, Settings tab.
- **Play-time break is shared by the whole Mac** (stored in UserDefaults `bip.breakState`), so
  switching profiles can't skip it. Play minutes are still counted per child.
- **Download page** (`site/index.html`): intro, the four islands with screenshots, a parents section,
  and the Mac download + first-install steps. Screenshots are real: CI opens the built app with
  `BIP_SCREENSHOTS=<folder>` (`BipIsland/App/ScreenshotMode.swift`, `scripts/ci/take_screenshots.sh`),
  saves 12 scenes, shrinks them to JPEGs and deploys them to `screenshots/`. If that step fails the page
  shows marked empty frames and the release still ships. The download area has one card per platform,
  ready for a Windows card after the Godot move (the Godot build needs its own screenshot mode then).
- **Known open items:** `vo_who_is_playing` still a placeholder clip (ElevenLabs was busy), waves 2b/3/4
  games (17 left), golden rounds, real art, music, Developer ID signing.

## Locked decisions (don't relitigate)

- Font: Atkinson Hyperlegible for all text, bundled (no Andika/Patrick Hand/system fonts).
- Voice: "Bip Island Narrator" `Mq5hYfc3xyDzuW3pPMck`, `eleven_v3`, SA English teacher.
- Phonics: sounds first, GK-clipped stops, UK/SA spelling, no slang.
- No timers/lives/game-over. Wrong → boop → retry → hint after 2 misses. 120 pt targets.
- 20 min play → finish game → 20 min break (wall clock, persisted). Parents configure it.
- Free choice: Bip recommends, never forces. Rewards collected, never bought.
- Content lives in `Content/` JSON (never hard-code). Validator must pass after content edits.
- `project.yml` (XcodeGen) — never commit `.xcodeproj`. Ad-hoc sign for now.
- GitHub account for this project: **`evertgrobler`** (`evertgrobler/bip-island`, private).
  Other `gh` accounts on this Mac (kloutcreator, evert-del, Musicdeed) are off limits.

## Workflow that works

1. Branch off `main`, PR back, merge only when CI is green. Squash-merge feature PRs.
2. This Mac has **no Xcode** (CLT only): `swiftc -parse` for syntax, **never** `swift test`
   (XCTest needs Xcode — CI runs the 109 tests on `macos-latest`, ~6 min when queued).
3. CI failure loop that works: `gh run view <id> --log-failed | grep error`, fix, push, poll.
4. Owner playtests CI artifacts: `gh run download <main-run> -n <name> -D ~/Downloads/<label>`,
   unzip, right-click → Open. Kid-lock exit: hold Esc 3 s + maths. Dark Mode: keep panels light.

## Gotchas learned the hard way

- `UInt64(negativeInt)` **traps at runtime** (crashed Letters Island, build 17). Use enumerated indices.
- `Sketch.node` requires an explicit `seed:` (no default). `CGPoint + CGPoint` doesn't exist.
- Cross-module: `internal` memberwise inits are invisible to `BipIsland` (add `public init`).
- BaseScene subclass re-declaring `init(coordinator:)` needs `override`.
- `break` is a Swift keyword (test helper param). `onChange(of:)` needs the 2-arg form.
- Test closures in `playSessions` are non-throwing — no `try XCTUnwrap` inside them.
- SwiftUI panels must pin `.colorScheme(.light)` + ink text or Dark Mode makes white-on-cream.
- ElevenLabs keys are often created **restricted** (no `voices_read`/`user_read`): reads fail but
  TTS works. A transient `401` under parallel load goes away with 2 workers + backoff.
- The narrator voice belongs to one ElevenLabs account; keys from other accounts get
  `voice_not_found`. Ask the owner for a key from the voice-owner account.
- Session-only secrets live in `/tmp` (`opencode/.elkey`, `gen_batch.py`, `jobs.json`,
  `bip-full/raw/`, `bip-test/`): never commit, never print.
- `gh` CLI active account must be `evertgrobler` for this repo. Don't touch keyring/Desktop app.
- `/Users/evertgrobler/Desktop/Game` (parent folder) has its own empty git repo — ignore it.
  The project repo is `bip-island-main/` inside it.

## Session log

- **1 Oct AM:** Linked folder to `evertgrobler/bip-island`, verified Phase 1 build (build 10).
- **1 Oct midday:** Generated all 702 voice clips (ElevenLabs), trimmed to spec, bundled.
- **1 Oct PM:** Built Phase 2a (8 games + GridWalker + fairness tests), 11 scenes, 3 islands,
  breaks/stickers/mystery/recommendations/parent settings. Fixed 4 CI failures blind.
  Merged PR #4, shipped build 17 → **crashed on Letters Island** (UInt64 trap).
- **1 Oct PM:** Fixed crash (PR #6), fixed Dark-Mode parent panel (PR #7), merged both.
- **1 Oct PM:** Emoji pictures for all 242 missing word pictures (PR #8, build 23).
- **1 Oct eve:** Keyboard play for all games + coding (PR #9, build 25). Owner playtesting.
- **1 Oct, later:** Switched all text to Atkinson Hyperlegible (bundled TTFs + OFL licence,
  `ATSApplicationFontsPath`), parent area included. CI now fails if the font is missing.
- **1 Oct late:** Owner feedback: Morning Order hard to play, pictures didn't match. Redid it as
  tap-to-place with numbered spaces, 36 new step pictures, plus a big custom cursor.
- **1 Oct, later:** Profiles (picker scene, map badge, parent Children tab), parent Progress tab
  (`ProgressReport` in BipCore, tested), Mac-wide break, per-child play minutes.
- **1 Oct late:** Phonics clips i, n, u, s replaced: i/u were letter names ("eye", "you") because a
  lone letter was sent to ElevenLabs and trimmed. New method (in audio/script.csv): say a word like
  "It." / "Up." and cut the vowel before the stop; stretchy sounds as "Nnnnn." with fades.
  ElevenLabs then disabled the account's free tier ("unusual activity", proxy) — needs a paid plan.
- **1 Oct, night:** Owner: Morning Order repeats the same stories. Added 22 new stories (87 cards,
  pictures in `StepPictures.swift`). Their `vo_<set>_<n>` clips are **not recorded yet** (ElevenLabs
  account paused): cards play silence until generated. List = new `vo_` rows in `asset_manifest.json`.
- **1 Oct late:** Optional parent passcode (Settings tab): holding Esc then asks for it instead of
  maths; 3 wrong tries fall back to maths. Stored as a salted SHA-256 hash in UserDefaults.
- **1 Oct, night:** Levels in every game (games.json `levels`, per-child `gameLevels` in
  ChildProgress, level stars top-centre, 8 questions a visit, end-of-visit celebration). Morning
  Order levels added too (`cards` 3, 4, 6; the Morning Order session wires it up).
- **1 Oct, night:** Updates now need a grown-up: Sparkle checks on launch and hourly but never
  installs alone (`SUAutomaticallyUpdate` off, gentle reminders in `UpdateController`). A found
  update shows an "Update ready" button top-right; it opens the parent gate, then Sparkle's window.
- **1 Oct, night (2):** Generated all 87 new Morning Order clips with the ElevenLabs connector
  (narrator voice, `eleven_v3`), trimmed silence, 44.1 kHz mono AAC. Every manifest clip now has a file.
  The connector works even though the owner's API key was flagged; ~1 in 5 generations fails with
  "Free Tier disabled, unusual activity" and succeeds on retry. Batches of 6 avoid the 429 rate limit.
- **1 Oct, night (3):** Morning Order uses the shared game levels (games.json `cards`: 3, 4, whole
  story for stage 1+). `MorningOrderGame.trimmed` cuts a story to its first N steps (never under 3).
- **1 Oct, night:** Building play deepened. Word Builder: tiles show real graphemes (was showing ids
  like `oo_short`), tap a tile to drop it in the next space (drag still works), each tile says its
  sound as it lands, spare tiles come only from taught sounds that can't spell the word (`spares` in
  games.json levels: 1, 2, 2, 3), split-digraph words left out, and a right answer is sounded out tile
  by tile. Bip's Path: footprints show the route, an arrow shows which way Bip faces on turning
  puzzles (`GridWalker.facings`), plus take-back and clear buttons.
- **1 Oct, night:** New download page for parents: intro, a section per island with screenshots, parent
  features (kid lock, breaks, parent area, 4 children, no ads/data, Cambridge), download + install steps.
  Font and icon bundled in `site/`. Screenshots come from a CI-only screenshot mode in the Swift app
  (step is `continue-on-error`, artifact `screenshots-<run>` on every run). Swift change kept to that.
