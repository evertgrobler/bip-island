# Setting up Bip Island (Mac and Windows)

Every change merged to `main` is built on GitHub, tested on a real Mac and a real Windows PC, and
published to the download page. Installed copies update themselves once a grown-up says yes. You
won't need Xcode, Visual Studio or a terminal.

## How it fits together

```
Claude Code ──► GitHub (bip-island, public)
                 │  every merge to main
                 ▼
         GitHub Actions (Linux, Mac and Windows machines in the cloud)
         builds, tests, installs, updates and checks the kid lock
                 │
                 ▼
         Vercel: download page + Blob store (installers and update feeds)
                 │  the game checks when it opens
                 ▼
         Your Mac / Windows PC: "Update ready" for a grown-up to install
```

Only the finished game, its update feeds and the download page are on Vercel. No family details or
keys are ever in the repository.

## The one-time setup (already done)

The repository needs three **secrets** (GitHub → **Settings** → **Secrets and variables** → **Actions**):

| Secret | What it is |
| --- | --- |
| `VERCEL_TOKEN` | A Vercel token (vercel.com/account/settings/tokens, **No Expiration**) |
| `VERCEL_ORG_ID` | From the **Set up the download site** run |
| `VERCEL_PROJECT_ID` | From the **Set up the download site** run |

If the Vercel project is ever lost: make a new token, save it as `VERCEL_TOKEN`, then **Actions** →
**Set up the download site** → **Run workflow**. Its summary shows the two IDs to save. Then
**Actions** → **Godot** → **Run workflow** on `main` publishes everything again.

The old Swift app's secret `SPARKLE_PRIVATE_KEY` isn't used any more and can be deleted.

## Installing (once per computer)

Open the download page and pick your computer.

**Mac** (macOS 14 Sonoma or newer)
1. Click **Download for Mac** and open the `.dmg`. Drag **Bip Island** into **Applications**.
2. The first time only: **right-click** Bip Island in Applications → **Open** → **Open**. On macOS 15
   or newer, if it only shows a warning: **System Settings → Privacy & Security** → scroll down →
   **Open Anyway**.

**Windows** (Windows 10 or 11)
1. Click **Download for Windows** and run `Setup.exe`.
2. The first time only, Windows may say **"Windows protected your PC"**: click **More info** →
   **Run anyway**.

The game opens full screen. Set up each child in the parent area (**Settings → Children**), and a
parent passcode if you'd like one.

## The kid lock

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
- Updates never need the right-click or "Run anyway" step again.

## If something goes wrong

| Problem | What to do |
| --- | --- |
| The **Godot** run fails at **Publish** | A Vercel secret is missing or wrong. Check the three names above, or redo the Vercel setup. |
| The Mac won't open the app | Use the Privacy & Security → **Open Anyway** step above. |
| The game won't let me quit | Hold Esc for a full 3 seconds without pressing anything else, then answer the sum. As a last resort: hold the power button (Mac) or Ctrl-Alt-Del (Windows). |
| Want a nicer web address | Add your own domain to the Vercel project, then a repository **variable** `UPDATE_FEED_URL` set to `https://your.domain/appcast.xml` (the page address is taken from it). Installed copies keep their update address. |

## Later: proper signing

Without paid certificates, the first install needs the right-click (Mac) or "Run anyway" (Windows)
step. An Apple Developer account (about US$99 a year) and a Windows code-signing service can be added
later through GitHub secrets, with no game changes. Updates are already checked by Velopack either way.
