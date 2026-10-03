# Bip Island — Session Status (read this first)

> Every agent session starts here. Read this file, then `docs/PLAN.md` and `docs/GAMES.md`
> for the design. **When you finish work, update this file**: append a dated entry under
> "Session log", refresh "Where things stand", and commit it with your changes.

## Where things stand (3 October 2026)

- **The game is the Godot version (Mac + Windows).** The Swift Mac app, its Xcode build and its
  Sparkle feed are retired (the owner installs the Godot app fresh; no progress export). How it
  was built: `docs/GODOT_MIGRATION.md`.
- **Games:** 14 live across 5 islands: Letters (Meet the Sound, Sound Hunt, Bubble Pop, Letter
  Trace, Feed the Monster), Numbers (Count & Tap, Quick Look), Words (Sound Buttons, Word Builder),
  Coding (Morning Order, Bip's Path), Art (Shape Builder, Paint Pots, Mirror Magic). Levels inside every
  game, 8-question visits, star celebration.
- **Art Island (3 Oct):** the fifth island, in the middle of the map. Content in `Content/art/`, game types
  in `godot/BipCore/Games/`, screens in `godot/Scripts/Games/`, drawing in `Scripts/Drawing/ArtDrawing.cs`.
  Curriculum and the codes still to confirm: `docs/CURRICULUM.md`, "Art Island".
- **Systems:** profiles (up to 4, "Who's playing?"), Bip's recommendations and nudge, wall-clock play
  breaks with Bip charging, sticker book, daily mystery box, keyboard play, big cursor, Bip's sounds.
- **Parent side:** hold Esc 3 s → maths or passcode → parent area (Progress, Children, Settings: play
  time, passcode, updates, quit). "Update ready" button installs only after the gate.
- **Kid lock:** Mac (presentation options, quits on power-off) and Windows (exclusive full screen +
  keyboard hook), proven on real runners on 3 Oct. Option/Alt at launch = parent mode. Second screens
  aren't covered yet.
- **Voice:** all narrator clips recorded (ElevenLabs "Bip Island Narrator"), `vo_who_is_playing` included.
- **Pictures:** 15 hand-drawn + emoji stand-ins (Noto Color Emoji, bundled) for every manifest picture;
  123 Morning Order step scenes.
- **Delivery:** every `main` merge is built, tested on real Mac and Windows machines, and published:
  installers + Velopack feeds in the Vercel Blob store, and the download page (`site/`: islands,
  parents section with the kid lock card, two download cards with the Option/Alt tip, Godot
  screenshots from `scripts/ci/site_screenshots.sh`) via `scripts/ci/deploy_site.sh`.
- **The repo is public since 3 Oct**, so GitHub Actions is free. Merge rule: green CI + another session's
  review; run `scripts/check_all.sh` before pushing (see CLAUDE.md "Working rules").
- **Known open items:** cover second screens in the kid lock; Windows exe
  icon; more games (waves 2b/3/4), real art, music; paid signing only if shared beyond the family.

## Locked decisions (don't relitigate)

- Engine move: Godot 4 .NET (C#), Velopack updates, direct `.dmg` / `Setup.exe` downloads, no stores,
  no paid signing for now. Details in `docs/GODOT_MIGRATION.md`.
- Font: Atkinson Hyperlegible for all text, bundled (no Andika/Patrick Hand/system fonts).
- Voice: "Bip Island Narrator" `Mq5hYfc3xyDzuW3pPMck`, `eleven_v3`, SA English teacher.
- Phonics: sounds first, GK-clipped stops, UK/SA spelling, no slang.
- No timers/lives/game-over. Wrong → boop → retry → hint after 2 misses. 120 pt targets.
- 20 min play → finish game → 20 min break (wall clock, persisted). Parents configure it.
- Free choice: Bip recommends, never forces. Rewards collected, never bought.
- Content lives in `Content/` JSON (never hard-code). Validator must pass after content edits.
- No paid signing for now: Mac ad-hoc signed, Windows unsigned (one-time "Run anyway").
- GitHub account for this project: **`evertgrobler`** (`evertgrobler/bip-island`, public).
  Other `gh` accounts on this Mac (kloutcreator, evert-del, Musicdeed) are off limits.

## Workflow that works

1. Branch off `main`, PR back, merge only when CI is green and another session has reviewed it.
   Squash-merge feature PRs.
2. In a cloud session, `scripts/check_all.sh` runs everything CI does except the real Mac/Windows
   install test (setup below). Look at every screenshot it saves.
3. CI failure loop: read the failed job's log (GitHub MCP `get_job_logs`), fix, push.
4. The real Mac/Windows install, update and kid-lock checks run on `main` builds (and on a manual
   "Run workflow" of **Godot** on any branch).

## Godot version (how-to)

- Project in `godot/` (Godot 4.7.2 .NET, C#, `BipIsland.csproj`). Shared assets are copied in by
  `python3 scripts/godot/prepare_assets.py` (fonts, Content JSON, voice clips converted to `.ogg`).
- In a cloud session: `apt-get install -y dotnet-sdk-8.0` (the dotnet.microsoft.com installer is
  blocked by the proxy), then `scripts/godot/install_godot.sh` prints the Godot binary path.
  `cd godot && dotnet build`, then `$GODOT --headless --path . --import`.
- Self-tests run the real game: `-- --bip-report out.json`, `-- --bip-screenshot out.png` (needs
  `xvfb-run` + `--rendering-driver opengl3`), `-- --bip-update-test <feed> <report>`.
- CI: `.github/workflows/godot.yml`. Linux exports both platforms and packs the Windows `Setup.exe`
  (`vpk "[win]" pack`); Windows and Mac runners install an older build and update it from a local feed.
  Main builds go to a public Vercel Blob store (`godot-test/`); the Swift app's feed is untouched.
- Drawing is y-down in Godot: when porting a Swift drawing, negate y values and angles.
- Drawing kit ported (Phase 2, part): `Buttons`, `PictureNode` (+ `PictureCard`), `EmojiPictures`
  (Noto via `Fonts.Emoji`), `StepPictures`, `Avatars`, `Sketch.StarPoints`. Ports keep the Swift numbers
  and seeds through `Up.cs` (`P`/`R` flip y, `Turn` negates angles, `Pen` = Sketch.Node with doubles).
  Tap names are node metadata (`Buttons.TapMeta`), since Godot node names can't hold a colon.
- Dev page: `--bip-scene res://Scenes/Dev/PictureGallery.tscn [--bip-gallery-page N]` shows every
  manifest picture (6 pages; the last has buttons, avatars, Bip, stars). Screenshot with the xvfb
  command plus `--bip-screenshot out.png`. The title counts ids "without art" (should be 0).
- Game logic: `godot/BipCore` (plain C# library, no Godot; namespace `BipCore`), referenced by
  `BipIsland.csproj`. Tests: `dotnet test godot/BipCore.Tests` (runs on Linux in a cloud session, about
  10 seconds). Saves use `BipJson` (camelCase, the same JSON as the Swift app's Codable, dates as
  seconds since 2001), so Swift-written progress, breaks and passcodes load unchanged. The Swift names
  map across (`makeRound(for:session:using:)` → `MakeRound(learner, session, rng)`, `SoundHuntGame.id`
  → `SoundHuntGame.GameId`, `Word.word` → `Word.Text`, `Grapheme.grapheme` → `Grapheme.Text`).
- Game shell (Phase 2): `Scenes/Start.tscn` is the main scene; the `Game` autoload
  (`Scripts/Game/GameCoordinator.cs`) opens saves and shows screens (`Scripts/Screens/`). Saves are one
  JSON file through `BipCore.SaveStore`; `--bip-save-dir <folder>` points them elsewhere (tests always
  use it). One screen at a time: `--bip-scene res://Scenes/Screens/<map|profiles|letters|…>.tscn`.
  `--bip-walk out.json` clicks round every screen and fails on any wrong step. Games say
  "Coming soon!" until their screen is ported (`GameCoordinator.StartGame`).
- Parent gate and area (Phase 4): `Scripts/Parent/` (`ParentLayer` over every screen, `ParentArea`
  tabs built from Godot controls, `ParentUi` theme); the rules are `BipCore.ParentGateFlow` (tested).
  Holding Esc no longer quits: quitting is a button in Settings. The game pauses (`SceneTree.Paused`)
  while the gate is open. Previews: `Scenes/Parent/{gate,progress,children,settings,update}.tscn`.
  `Updater.ReadyVersion`/`UpdateReady` drive the "Update ready" button; `InstallAndRestart()` only runs
  after the gate.
- Kid lock (Phase 4): `Scripts/App/KidLock.cs`, rules in `BipCore.KidLockRules` (tested). The project
  starts windowed; `Boot` calls `KidLock.Install`, which covers the screen and locks it (Mac), or goes
  exclusive full screen with a keyboard hook (Windows). Off when headless, with `--bip-windowed`, on
  Linux, or with Option/Alt held at launch (parent mode). Quitting or updating lifts it first. It
  can't run on Linux: the CI Mac/Windows jobs run `--bip-kidlock-check` on a real desktop.
  Known gap vs Swift: second screens aren't covered with paper windows yet (a click there could reach
  another app). Fine on a one-screen Mac; add cover windows if the family Mac gets a second display.

## Gotchas learned the hard way

- Godot 4.7.2 **segfaults now and then importing on worker threads** (dmesg: `WorkerThread … segfault`,
  log stops at `reimport | NotoColorEmoji.ttf`; about 1 run in 4). `project.godot` sets
  `editor/import/use_multiple_threads=false`: 0 crashes in 18 fresh imports, still about 7 s.
- The parent area is always light (cream panels, ink text) whatever the system theme.
- Godot: **commit the `.uid` file of every new C# script** (run `--headless --import` once to make
  them), or every check run leaves untracked files behind.
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
- **1 Oct, night:** Owner asked about Windows. Decided to rebuild in Godot 4 (C#) so one codebase
  ships to Mac and Windows with auto-updates. Full rebuild in one go with CI checks instead of
  playtests; the owner tests once at switchover. New games paused. Plan: `docs/GODOT_MIGRATION.md`.
- **1 Oct, night:** New download page for parents: intro, a section per island with screenshots, parent
  features (kid lock, breaks, parent area, 4 children, no ads/data, Cambridge), download + install steps.
  Font and icon bundled in `site/`. Screenshots come from a CI-only screenshot mode in the Swift app
  (step is `continue-on-error`, artifact `screenshots-<run>` on every run). Swift change kept to that.
- **1 Oct, night (late):** Godot Phase 0 CI green: Linux exports both, Windows (Setup.exe) and Mac
  (Velopack portable app, ad-hoc signed, `.dmg`) install an older build and update themselves from a
  local feed with saved data intact. Velopack's Mac packer needs a `.entitlements` file and has no
  `--skipVeloAppCheck` (Windows only).
- **1 Oct, night (4):** Ported the drawing kit to Godot (Buttons, PictureNode, EmojiPictures,
  StepPictures, Avatars, star helper) plus the PictureGallery dev scene and `--bip-scene`. All 343
  manifest pictures render with art; checked page by page from xvfb screenshots. Fixed `rocket_4`
  (moon spilled off the card) in both Swift and Godot.
- **2 Oct:** Godot Phase 1: all of BipCore ported to C# (`godot/BipCore`) with every Swift test as
  xUnit (`godot/BipCore.Tests`, 148 tests incl. 1,000-round fairness per game, levels, and loading a
  Swift-written save and passcode). CI runs them after the C# build. The Swift test
  `testReviewComesFirst` checked day 2 when the review is only due on day 3 (passed by luck); fixed in both.
- **2 Oct:** Godot Phase 0 done: the first main build (0.2.8) published to the public Vercel Blob store
  (created automatically). Test downloads (Phase 0 screen only, not the game yet):
  Mac https://x4dwtory3rjimb1l.public.blob.vercel-storage.com/godot-test/BipIsland-mac.dmg ·
  Windows https://x4dwtory3rjimb1l.public.blob.vercel-storage.com/godot-test/BipIsland-win-Setup.exe.
  Update feeds: `godot-test/win/` and `godot-test/osx/`. Next: the game shell (map, islands, profiles,
  saves, input), then the 11 games.
- **2 Oct, morning:** Actions minutes ran out (Mac runners). Added `scripts/check_all.sh` (content,
  C# build, tests, Godot self-test, screenshots of every scene, Mac + Windows export, Setup.exe pack,
  diff hygiene) as the merge gate until November, plus a cross-session review. Trimmed CI for when it
  returns: Swift build only on Swift/app changes, Godot PRs Linux-only, docs-only changes run nothing.
- **2 Oct, later:** Godot Phase 2 game shell: saves, coordinator, map, islands, Who's playing?,
  charging, sticker book, mystery box, keyboard play, big cursor, Bip's sounds. Every screen matches
  the Swift app in screenshots; a 31-step click-through test runs in `check_all.sh`. Games are next.
- **2 Oct, afternoon:** Godot Phase 3, Numbers Island: Count & Tap and Quick Look, on a shared game
  frame (`Scripts/Games/GameScreen.cs`: level stars, answers saved as they happen, the end-of-visit
  star count, back to the island). The walk-through now plays both games, including a whole
  8-question Quick Look visit. Words, Coding and Letters games follow, one PR per island.
- **2 Oct, later:** Godot parent gate and parent area (Phase 4, part 1): hold Esc → maths or passcode,
  Progress/Children/Settings tabs, "Update ready" button that installs only after the gate. The
  click-through test now covers the gate (48 steps). Native kid lock is the next part.
- **2 Oct, later still:** Godot Phase 3 finished: Words (Sound Buttons, Word Builder), Coding
  (Morning Order, Bip's Path) and Letters (Meet the Sound, Sound Hunt, Bubble Pop, Letter Trace, Feed
  the Monster), all 11 games on the shared frame. The walk-through plays every game (112 steps).
  Phase 3 ticked in `GODOT_MIGRATION.md`.
- **2 Oct, later:** Godot kid lock (Phase 4, part 2) written: Mac presentation options as in Swift plus
  quit-on-power-off (the Godot build would otherwise block shutdown), Windows exclusive full screen and
  a keyboard hook, parent mode with Option/Alt. Unproven on real machines until the CI Mac/Windows jobs
  run (minutes out until 1 Nov, or a brief public window).
- **3 Oct:** Owner decision: no progress export. The owner installs the Godot app fresh (children
  start again) and has tried the Godot build and finds it much better. Repo made public (and the owner
  chose to keep it public, so CI is free again). First real-machine run of the kid lock (Godot run
  37100738096): Windows "windows hook on", Mac "mac lock on", both installed and updated with saves kept.
- **3 Oct:** Owner: offer only the new version. The download page now has one card, "Bip Island for Windows
  and Mac" (Test version), linking the Godot builds in the Vercel Blob store (`godot-test/BipIsland-win-Setup.exe`,
  `godot-test/BipIsland-mac.dmg`, fixed addresses, latest build). The old Swift Mac download is gone from the
  page, but `deploy_vercel.sh` still publishes its zip + `appcast.xml`, so installed Swift copies keep updating.
  Page says plainly the kid lock isn't built yet; Mac step explains Replace / Keep Both (both apps are called
  Bip Island; progress isn't moved across yet). The builds there are already 0.2.31 (Godot run 31, #27).
- **3 Oct:** Owner made the repo public, so Actions minutes are free again; merge rule back to green CI
  plus a session review. History scanned for secrets: none. Godot run 31 (#27) had already published
  0.2.31 (all 11 games) to `godot-test/`; its last check failed only because the overwritten feed is
  cached for 60 s, so the check now retries for 3 minutes. Drags and tracing stop when the button is
  let go out of sight (#27 review follow-up).
- **3 Oct, later:** Download page switched to the Godot game:
  two cards with placeholders filled by godot.yml, Godot screenshots, deploy moved from build.yml to
  godot.yml (`deploy_site.sh`, `site_screenshots.sh`; `deploy_vercel.sh` and `take_screenshots.sh` removed).
  Still to do when PR #26 (kid lock) merges: drop the "kid lock still being built" line, bring back the
  "Kid lock" parent card, add the Option/Alt windowed start. `BipIsland/App/ScreenshotMode.swift` is now
  unused; it goes with the rest of the Swift app.
- **3 Oct, afternoon:** Kid lock merged (#26), so the download page brings back the "Kid lock" card (Mac: Dock,
  menu bar, Cmd-Tab, Cmd-Q blocked; Windows: Windows key, Alt-Tab blocked; shutting down and Ctrl-Alt-Del
  always work) and adds the Option/Alt windowed-start tip. Swift app retired: `deploy_site.sh` no longer
  carries `appcast.xml` and the Swift zip into the deploy (it still uses `resolve_feed_url.sh` for the site address).
- **3 Oct, later:** Swift app retired: `BipIsland/`, the Swift `BipCore/`, `project.yml`, `build.yml`,
  the Sparkle scripts and the placeholder-voice script removed. The real app icon moved to
  `godot/icons/` (Mac export uses it). CLAUDE.md, SETUP.md, PLAN.md and this file now describe the
  Godot setup only. "Set up updates" became "Set up the download site" (Vercel project only).
- **3 Oct:** Art Island (owner request: art with shapes, colour mixing and symmetry, kept on the Cambridge
  curriculum). Shape Builder (7 levels, 1Gg.01 to 3Gg.01), Paint Pots (4 levels, real paint mixes, label
  pictures on every pot), Mirror Magic (6 levels, 1Nf.01, 2Gg.09, 3Gg.09). Validator checks shape geometry,
  pot mixes and mirror lines; ArtGameTests play every level; the walk-through plays one round of each.
  38 narrator clips made with the ElevenLabs connector (about 1,000 credits; the account had 3,391 before),
  including the real `vo_who_is_playing`. ElevenLabs refuses some requests with "unusual activity":
  three at a time and retrying works. Review fixes: a replay mid-turn no longer stacks shape turns; a wrong
  mix only empties its own paint from the bowl.
