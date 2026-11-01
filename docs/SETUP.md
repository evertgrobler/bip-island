# Setting up Bip Island (Mac and Windows)

Once a day (if `main` changed), the game is built on GitHub, tested on a real Mac and a real Windows
PC, and published to the download page. Installed copies update themselves once a grown-up says yes.
You won't need Xcode, Visual Studio or a terminal.

## How it fits together

```
Claude Code ──► GitHub (bip-island, public)
                 │  once a day at 20:52 SAST, if main changed (or a manual run)
                 ▼
         GitHub Actions (Linux, Mac and Windows machines in the cloud)
         builds, tests, installs, updates and checks the child lock
                 │
                 ▼
         GitHub (bip-island): one release per version (installers and
         update files) + the download page on GitHub Pages
                 │  the game checks when it opens
                 ▼
         Your Mac / Windows PC: "Update ready" for a grown-up to install
```

- **Download page:** https://evertgrobler.github.io/bip-island/
- **Releases:** https://github.com/evertgrobler/bip-island/releases (the newest 5 are kept; older ones
  are deleted by CI).
- The download buttons always fetch the newest version:
  `https://github.com/evertgrobler/bip-island/releases/latest/download/BipIsland-mac.dmg` and
  `.../BipIsland-win-Setup.exe`.

It's free for a public repository, with no storage or download limits to run into, and it needs no
token: the workflow publishes with its own. (Until 1 November 2026 this was on Vercel, which paused the
whole account, the owner's other site included, when the free Blob store limit was reached.)

**If the repository is ever made private,** installed games can no longer read its releases and
GitHub Pages stops on the free plan. Move the releases to a separate public repository first (ask
Claude: commit 2c805df, the first version of pull request #65, did that).

## The one-time setup

1. In `bip-island` → **Settings** → **Pages** → **Build and deployment** → **Source**, choose
   **GitHub Actions**.
2. **Actions** → **Godot** → **Run workflow** on `main` publishes the first release and the page.

The old secrets `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID` and `SPARKLE_PRIVATE_KEY` aren't
used any more and can be deleted.

## Moving from Vercel (once, November 2026)

Copies installed before this change check the old Vercel address for updates, so they won't update any
more. **Install once more from the new download page** (same steps as below). The new copy replaces
the old one, and the children's progress is kept: it's saved separately from the game.

When Vercel unpauses the old site, Claude can put a small page there that sends visitors to the new
download page.

## Installing (once per computer)

Open the download page (https://evertgrobler.github.io/bip-island/) and pick your computer. Under each
button, **Or install without the warning** has a line to paste into Terminal (Mac) or PowerShell
(Windows) instead, which skips the steps below.

**Mac** (macOS 14 Sonoma or newer)
1. Click **Download for Mac** and open the `.dmg`. Drag **Bip Island** into **Applications**.
2. The first time only: open Bip Island and click **Done** (or **OK**) on the warning, then within the
   hour **System Settings → Privacy & Security** → scroll down → **Open Anyway** → the Mac's password,
   and **Open Anyway** once more on the last warning (if the button has gone, open the app again first). (Since macOS 15
   right-click → Open no longer gets past the warning; on macOS 14 Sonoma it still works.)

Tip for a trackpad-only Mac (a MacBook Neo, say): a mouse is best for small hands, because several games
drag things around. Without one, turn on **Tap to click** (System Settings → Trackpad) and tap-to-drag
(Accessibility → Pointer Control → Trackpad Options → Dragging style).

**Windows** (Windows 10 or 11)
1. Click **Download for Windows** and run `BipIsland-win-Setup.exe`.
2. The first time only, Windows may say **"Windows protected your PC"**: click **More info** →
   **Run anyway**.

The game opens full screen, and the first time it opens a short **setup guide** walks you through
it (about 3 minutes): practising the way into the grown-up area (hold **Esc** for 3 seconds), an
optional passcode, your children, play time and breaks, and what your child will see. You can skip it,
and open it again any time from the parent area (**Settings → Show the setup guide again**).

## The child lock

- **Mac:** the Dock, menu bar, Cmd-Tab, Cmd-H and Cmd-Q are blocked. Logging out, restarting and
  shutting down always work.
- **Windows:** full screen; the Windows key, Alt-Tab, Alt-Esc, Ctrl-Esc and Alt-F4 are blocked while
  the game is in front. Ctrl-Alt-Del always works.
- **To get out:** hold **Esc** for **3 seconds** (a ring fills in the corner), then answer the maths
  question or enter your passcode. The parent area has **Quit** in Settings.
- **Parent mode:** hold **Option** (Mac) or **Alt** (Windows) while the game opens, for a normal
  window without the lock.
- With a second screen plugged in, the mouse stays on the game's screen while the game is in front.

## How updates arrive

- The game checks for a new version when it opens and downloads it quietly. It never installs
  anything by itself.
- When one is ready, an **Update ready** button appears at the bottom of the screen. Tap it, answer
  the maths question (or passcode), and the game restarts on the new version.
- Parent area → **Settings** shows the version, and has **Check for updates now** / **Install the update**.
- Updates never need the "Open Anyway" or "Run anyway" step again.

## If something goes wrong

| Problem | What to do |
| --- | --- |
| The **Godot** run fails at **Publish the release** with a 403 | **Settings** → **Actions** → **General** → **Workflow permissions** must allow the workflow to write (the publish job asks for "contents: write" itself, but an organisation or repository setting can forbid it). |
| The run warns "the download page wasn't published" | **Settings** → **Pages** → **Source** must be **GitHub Actions** (setup step 1). The release is out either way: installed games still update. |
| The Mac won't open the app | Use the Privacy & Security → **Open Anyway** step above. |
| The game won't let me quit | Hold Esc for a full 3 seconds without pressing anything else, then answer the sum. As a last resort: hold the power button (Mac) or Ctrl-Alt-Del (Windows). |
| Want a nicer web address | **Settings** → **Pages** → **Custom domain**. Installed copies don't mind: they update from the releases, not the page. |

## Later: proper signing

Without paid certificates, the first install needs "Open Anyway" (Mac) or "Run anyway" (Windows)
step. An Apple Developer account (about US$99 a year) and a Windows code-signing service can be added
later through GitHub secrets, with no game changes. Updates are already checked by Velopack either way.

## Going private (when the game is finished)

The owner plans to make `evertgrobler/bip-island` private. Before or when you do:

- **Move the releases out first.** Today installed games update from this repository's releases,
  and the download page is its GitHub Pages site; both stop when it goes private. Before switching,
  ask Claude to move publishing to a separate public repository (say `bip-island-downloads`, with a
  `DOWNLOADS_TOKEN` secret; commit 2c805df, the first version of pull request #65, did that), and publish at least one build to
  both places, so installed copies pick up the new address through a normal update.
- **GitHub Actions minutes:** a private repository on the free plan gets 2,000 minutes a month, and a
  Mac minute counts as 10 (Windows as 2). A 15-minute Mac job alone counts as 150, so running the Mac
  test on every merge would use the month in about a dozen merges (a run's **Usage** page shows the
  real numbers). So:
  - in `.github/workflows/godot.yml`, run the Mac and Windows install-and-update jobs only on a
    schedule (say weekly) or by hand (**Run workflow**), not on every merge, and let the publish job
    run without them (check its `needs:` line);
  - keep pull requests on the Linux job only (they already are).
- **The PC session needs a GitHub login:** cloning or pushing a private repository on the owner's
  Windows PC needs `gh auth login` (or Git Credential Manager) with the `evertgrobler` account.
  Cloud sessions keep working through the Claude GitHub app, as long as it can see the repository.
- **The download page moves** with the releases, to the new repository's GitHub Pages address.
