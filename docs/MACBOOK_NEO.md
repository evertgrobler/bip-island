# Bip Island on a MacBook Neo

Written 18 October 2026, 17:26 SAST. Research only: nothing in the repo was changed.

## Short answer

The MacBook Neo is a real Mac, and Bip Island should install and run well on it. The screen is the
same shape the game is built for, and the chip is easily fast enough. Five things need attention:

1. **First install:** the "right-click → Open" step in our docs no longer works on the macOS it ships with.
2. **Trackpad:** a 4-year-old will struggle with a click-and-hold trackpad, especially Letter Trace. Use a mouse.
3. **Laptop keys** the kid lock can't stop: mute and volume, brightness, Spotlight, Siri and the lock key.
4. **The download page** says "Apple chip (M1 or later)". The Neo has an A18 Pro, so a parent may think it isn't supported.
5. **macOS 27:** our Mac test machine in CI runs macOS 26, but a new Neo may already be on macOS 27.

## 1. What the MacBook Neo is

Apple announced it on 4 March 2026 as its cheapest laptop (US$599 to $699).

| | MacBook Neo | Confidence |
| --- | --- | --- |
| Chip | **A18 Pro**, the iPhone 16 Pro chip (not an M-series chip): 6-core CPU (2 performance, 4 efficiency), 5-core GPU | Confirmed (several sources) |
| Memory | **8 GB, fixed**, no 16 GB option | Confirmed |
| Screen | 13-inch IPS, **2408 × 1506** (219 ppi, 500 nits, sRGB). That is 1.599:1, so almost exactly **16:10** | Confirmed |
| Notch | Not confirmed either way | Unconfirmed |
| macOS | Shipped with **macOS 26 Tahoe** (26.3.x). macOS 27 came out on 14 September 2026 and reportedly supports the A18 Pro | Tahoe confirmed; macOS 27 support from secondary sources only |
| Keyboard | Unlit. The top row is the usual Mac function row: F1/F2 brightness, F3 Mission Control, F4 Spotlight, F5 Dictation/Siri, F6 Do Not Disturb, F7–F9 media, F10–F12 mute and volume, Globe/fn key bottom left. Top right is a lock key, or a Touch ID key on the dearer model. **No Touch Bar.** | Function row from Apple's guide. I found no source that names the **Esc** and **Option** keys outright. Every current Mac keyboard has both, so they are almost certainly there. |
| Trackpad | **Mechanical**, with no Force Touch. It clicks the same anywhere on its surface. Multi-touch gestures and Tap to Click are supported | Confirmed (reviews) |
| Ports | 2 × USB-C (one 10 Gb/s with display out, one USB 2), headphone jack. One external display up to 4K 60 Hz | Confirmed |
| Graphics | Apple GPU with Metal. Apple's OpenGL still works: Minecraft Java (OpenGL) runs at 50–300 fps at 1080p, and 2D OpenGL games run well | Metal support is implied (same macOS as M-series Macs), not stated in a source. OpenGL is shown by game tests. |

Sources:
[LaptopMedia](https://laptopmedia.com/series/apple-macbook-neo/) ·
[Neowin, Neo vs Air](https://www.neowin.net/news/specs-appeal-comparing-macbook-neo-with-macbook-air/) ·
[Wikipedia](https://en.wikipedia.org/wiki/MacBook_Neo) ·
[Apple: Magic Keyboard for MacBook Neo](https://support.apple.com/guide/macbook-neo/magic-keyboard-apdab672d5e9/mac) ·
[MacMost, 15 differences](https://macmost.com/15-ways-the-macbook-neo-is-different-from-other-macbooks.html) ·
[Macworld review](https://www.macworld.com/article/3081612/macbook-neo-a18-pro-review.html) ·
[HotHardware review](https://hothardware.com/reviews/apple-macbook-neo-review) ·
[heise, disadvantages](https://heise.de/-11201788) ·
[9to5Mac, 10 games tested](https://9to5mac.com/2026/03/11/how-well-does-the-new-macbook-neo-handle-gaming-andrew-tsai-tested-10-games-to-find-out/) ·
[TechRadar, gaming](https://www.techradar.com/computing/someone-tested-the-macbook-neos-gaming-chops-and-the-results-ranged-from-miracle-scores-to-completely-unplayable) ·
[android.com.pl, macOS 27](https://android.com.pl/tech/1038871-macbook-neo-wiekszy-plan-apple-macos/)

I couldn't open apple.com, Wikipedia or the review sites directly from this session. These facts
come from search results that quote those pages, so check them on Apple's own spec page if any of them matter.

## 2. How the game fits it

### Will it install and run? Yes.
- The Mac build is **universal** (`godot/export_presets.cfg`: `binary_format/architecture="universal"`),
  so it includes Apple-chip (arm64) code, and the A18 Pro runs ordinary Apple-chip Mac apps.
  The minimum macOS for the arm64 part is 11, and the docs ask for 14 or newer. Tahoe (26) and 27 both meet that.
- **First launch:** the app is ad-hoc signed (`codesign/codesign=1` with no identity, then
  `vpk --signAppIdentity "-"`), with no Apple notarisation. **Since macOS 15 Sequoia, right-click → Open
  no longer gets past this.** Apple: "users will no longer be able to Control-click to override
  Gatekeeper… They'll need to visit System Settings > Privacy & Security"
  ([AppleInsider](https://appleinsider.com/inside/macos-sequoia/tips/whats-changed-in-runtime-protection-for-macos-sequoia),
  [The Hacker News](https://thehackernews.com/2024/08/apples-new-macos-sequoia-tightens.html),
  [Michael Tsai](https://mjtsai.com/blog/?p=43993)). On a Neo it goes like this:
  1. Open Bip Island, get the "can't verify" warning and click Done.
  2. Go to System Settings → Privacy & Security, scroll down and click **Open Anyway**.
  3. Enter the Mac's password, then click Open.

  This is needed once only. Velopack updates don't trigger it again.
- **What our docs say:** `docs/SETUP.md` (lines 48–50) and `site/index.html` (lines 326–327) still lead with right-click → Open,
  and mention "Open Anyway" only as a fallback "on macOS 15 or newer". The Neo only ever had
  macOS 26 or later, so the first instruction never works on it. `CLAUDE.md` (Signing row) says "right-click → Open" too.
- The download page's "What you need" says "**An Apple chip (M1 or later)** or an Intel processor"
  (`site/index.html` line 319). That's technically fine, but a Neo owner may read it as "not my Mac".

### Speed, screen and memory: fine.
- **Renderer:** Compatibility (OpenGL, `renderer/rendering_method="gl_compatibility"`), 2D only. Apple
  still ships OpenGL on Apple chips (it runs on top of Metal), and OpenGL games run well on the Neo.
  A hand-drawn 2D game is a very light load for this chip.
- **Screen shape:** the game's base canvas is **1600 × 1000 (16:10)** with `stretch/mode="canvas_items"`,
  `aspect="expand"` and `display/high_res=true`. The Neo's 2408 × 1506 is the same shape, so there are
  **no black bars**, and the game draws at the screen's full sharpness.
  If the screen has a notch (unconfirmed), the top-centre strip would be hidden: check that nothing important sits there.
- **Physical size:** the 120-point buttons are about 7.5% of the screen width. On a 13-inch screen
  that's about 2 cm, smaller than on a desktop monitor but still a good size for a mouse.
- **Memory:** 8 GB is twice the 4 GB the download page asks for. Reviewers say the 8 GB only limits
  big 3D games. I haven't measured Bip Island's memory use. Other open apps (browser tabs) will compete for it.
- **Battery:** the game redraws all the time (no low-power mode set), which costs some battery on a laptop.
  It's minor, but plug it in for long sessions. Closing the lid is handled: a gap of more than 90 s counts as no play time.

### Input: designed for a mouse. On this trackpad, use one.
- The trackpad is **mechanical**: a child has to physically press it down. Tap to Click is **off** by default.
- Most games are single clicks, which a child manages fine once Tap to Click is on.
- **Letter Trace needs the button held while moving** (`LetterTraceScreen.cs` only draws when the left
  button is held). Word Builder and Morning Order dragging work the same way, though tapping also
  works there. Pressing down while sliding a finger is very hard for a 4–6-year-old on a laptop trackpad.
- **Fix:** plug in a USB-C mouse, or a USB-A mouse with an adapter (the game is designed for a mouse anyway).
  Otherwise, turn on Tap to Click and **three-finger drag**
  (System Settings → Accessibility → Pointer Control → Trackpad Options → "Use trackpad for dragging" →
  "Three-finger drag"). Then tracing needs no pressing.
- **Keys:** the parent gate (hold **Esc** 3 s), parent mode (hold **Option** at launch) and "any key replays"
  all use standard keys that a Mac laptop keyboard has. With no Touch Bar, Esc is a real key. But the
  top row's media keys (brightness, mute, volume, Spotlight, Siri) are handled by macOS, not the game.
  A child can **mute the narrator or dim the screen to black**, and a key press won't replay a sound in that case.

### The kid lock
- It uses AppKit presentation options (`KidLockRules.MacLockOptions`), which hide the Dock and menu bar
  and block process switching, Force Quit, Hide and the Apple menu. These options work the same on
  every Mac model. Apple's documentation says blocking process switching also turns off Mission Control
  (Exposé), so F3 and the swipe gesture should be covered. I'm going from memory of Apple's docs here and haven't checked it on a Neo.
- CI proves the lock switches on with `--bip-kidlock-check` on GitHub's `macos-latest` runner, which
  is now **macOS 26 on Apple chips** (GitHub moved the label to macOS 26 in June–July 2026:
  [GitHub changelog](https://github.blog/changelog/2026-05-14-github-actions-upcoming-image-migrations),
  [runner image notes](https://newreleases.io/project/github/actions/runner-images/release/macos-26-arm64%2F20260715.0248)).
  That matches the Neo's own macOS. **macOS 27 isn't tested**, and the check only reads the settings back. It doesn't try real keys or gestures.
- **The lock can't stop these on a laptop:**
  - the **lock / Touch ID key**, which locks the screen and needs the Mac password;
  - **Spotlight** (F4 or Cmd-Space) and **Siri/Dictation** (F5);
  - the Globe/fn emoji picker;
  - mute, volume and brightness;
  - possibly the Notification Centre swipe from the right edge of the trackpad.

  Spotlight is the one that matters: a child could open another app from it. None of these is new to
  the Neo, but a laptop puts them right under small fingers. The Swift and Godot locks were never tested against them.
- Second screens still aren't covered (a known gap). The Neo can drive one external display, so the gap applies if one is plugged in.
- Logging out and shutting down are never blocked (`NSWorkspaceWillPowerOffNotification`). That's good.

### Updates (Velopack)
- The Mac CI job installs an older build, updates it from a local feed and checks that saved data survives, on the same
  macOS 26 Apple-chip setup. So updates should work on the Neo on macOS 26.
- Velopack downloads updates itself, so they aren't flagged as "downloaded from the internet" and
  don't bring back the Open Anyway step.
- **Not tested:** the app sitting in /Applications while a **standard (non-admin) account** is signed in.
  A family might give a child their own standard account on a new laptop. CI installs into a folder the
  test user owns. Also not tested: macOS 27.

## 3. Risks and recommended actions (most important first)

**Changes to the game, docs or CI**
1. **Fix the first-install steps** for macOS 15 and newer, which covers every Neo. Lead with: open it,
   click Done, then System Settings → Privacy & Security → Open Anyway → password → Open. Keep right-click → Open
   only for macOS 14. Files: `site/index.html` (download card), `docs/SETUP.md` (install steps and
   troubleshooting row), and the Signing row in `CLAUDE.md`.
2. **Download page "What you need":** change "An Apple chip (M1 or later)" to something like "Any Mac with an
   Apple chip (M1 or later, or MacBook Neo) or an Intel processor", and suggest a mouse for laptops
   (the shared line already says "a mouse").
3. **Make tracing work without a held button:** a tap-to-start, tap-to-stop trace mode, or count the
   finger moving after a tap. Then trackpad children can do Letter Trace.
4. **Test the lock against Spotlight, Siri and the trackpad gestures** on a real Mac. If Spotlight can open
   another app, look at bringing the game back to the front when it loses focus.
5. **Add a macOS 27 runner** to the Mac CI job once GitHub offers one, so the lock and update checks cover
   the version a new Neo may already run.

**Things to try on the real Neo**
1. Install from the download page. Note exactly what macOS shows and whether Open Anyway asks for a password.
2. Plug in a mouse. Without one, turn on Tap to Click and three-finger drag, then try Letter Trace with a child.
3. While locked, press F3, F4 (Spotlight), Cmd-Space, F5, the Globe key and the lock key, and try 3- and
   4-finger swipes and the right-edge swipe. Write down anything that leaves the game.
4. Check the top edge for a notch hiding anything, and that there are no black bars.
5. If the child uses their own standard account, check that an update installs (Settings → Check for updates now).
6. Turn up the volume before handing over, and remind children the top-row keys change the sound.
