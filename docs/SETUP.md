# Setting up Bip Island (Mac and Windows)

Every change merged to `main` is built on GitHub, tested on a real Mac and a real Windows PC, and
published to the download page. Installed copies update themselves once a grown-up says yes. You
won't need Xcode, Visual Studio or a terminal.

## How it fits together

```
Claude Code ──► GitHub (bip-island)
                 │  every merge to main
                 ▼
         GitHub Actions (Linux, Mac and Windows machines in the cloud)
         builds, tests, installs, updates and checks the child lock
                 │
                 ▼
         GitHub (bip-island-downloads, public): one release per version
         (installers and update files) + the download page on GitHub Pages
                 │  the game checks when it opens
                 ▼
         Your Mac / Windows PC: "Update ready" for a grown-up to install
```

- **Download page:** https://evertgrobler.github.io/bip-island-downloads/
- **Releases:** https://github.com/evertgrobler/bip-island-downloads/releases (the newest 5 are kept;
  older ones are deleted by CI).
- The download buttons always fetch the newest version:
  `https://github.com/evertgrobler/bip-island-downloads/releases/latest/download/BipIsland-mac.dmg` and
  `.../BipIsland-win-Setup.exe`.

Only the finished game, its update files and the download page go to `bip-island-downloads`. Never
code, content, family details or keys. It's free, with no storage or download limits to run into.
(Until 25 October 2026 this was on Vercel, which paused the account when the free Blob store limit
was reached.)

## The one-time setup

1. **Make the downloads repository** (done by the owner on 25 October 2026): on GitHub, a new
   **public** repository `evertgrobler/bip-island-downloads`, with a README. Then its **Settings** →
   **Pages** → **Deploy from a branch** → `main`, folder `/ (root)`.
2. **Make a token for it:** GitHub → your picture → **Settings** → **Developer settings** →
   **Personal access tokens** → **Fine-grained tokens** → **Generate new token**.
   - Repository access: **Only select repositories** → `bip-island-downloads`.
   - Permissions: **Contents: Read and write** (nothing else).
   - Expiration: as long as GitHub allows; put a reminder in your calendar to renew it.
3. **Save it in the game repository** (`bip-island` → **Settings** → **Secrets and variables** →
   **Actions** → **New repository secret**):

| Secret | What it is |
| --- | --- |
| `DOWNLOADS_TOKEN` | The token from step 2 |

4. **Actions** → **Godot** → **Run workflow** on `main` publishes the first release and the page.

The old secrets `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID` and `SPARKLE_PRIVATE_KEY` aren't
used any more and can be deleted.

## Moving from Vercel (once, October 2026)

Copies installed before 25 October 2026 check the old Vercel address for updates, and Vercel has
paused it, so they won't update any more. **Install once more from the new download page** (same
steps as below). The new copy replaces the old one, and the children's progress is kept: it's saved
separately from the game.

When Vercel unpauses the old site, Claude can put a small page there that sends visitors to the new
download page.

## Installing (once per computer)

Open the download page (https://evertgrobler.github.io/bip-island-downloads/) and pick your computer.

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
- Only the main screen is covered. If the Mac gets a second monitor, tell Claude so the lock can
  cover it too.

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
| The **Godot** run warns "DOWNLOADS_TOKEN isn't set" | Add the secret (setup step 3). |
| The **Godot** run fails at **Publish** with a 401 or 403 | The token has expired or can't write to `bip-island-downloads`. Make a new one (setup step 2) and replace the secret. |
| The download page is old or missing | `bip-island-downloads` → **Settings** → **Pages** must say **Deploy from a branch**, `main`, `/ (root)`. |
| The Mac won't open the app | Use the Privacy & Security → **Open Anyway** step above. |
| The game won't let me quit | Hold Esc for a full 3 seconds without pressing anything else, then answer the sum. As a last resort: hold the power button (Mac) or Ctrl-Alt-Del (Windows). |
| Want a nicer web address | `bip-island-downloads` → **Settings** → **Pages** → **Custom domain**. Installed copies don't mind: they update from the releases, not the page. |

## Later: proper signing

Without paid certificates, the first install needs "Open Anyway" (Mac) or "Run anyway" (Windows)
step. An Apple Developer account (about US$99 a year) and a Windows code-signing service can be added
later through GitHub secrets, with no game changes. Updates are already checked by Velopack either way.

## Going private (when the game is finished)

The owner plans to make `evertgrobler/bip-island` private. Before or when you do:

- **Keep `bip-island-downloads` public.** Installed games read its releases without a password, and
  the download page lives there. If it went private, every installed copy would stop updating.
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
- **Nothing else changes:** `DOWNLOADS_TOKEN` keeps working, and the download page and update links
  stay the same.
