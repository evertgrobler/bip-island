# Setting up Bip Island on your Mac

This is a one-time setup. When it's done, every change merged to `main` is built on GitHub, published to a small website on Vercel, and installed on your Mac automatically. You won't need Xcode or the Terminal.

It takes about 20 minutes. You'll need:

- this GitHub repository (you're the owner)
- a free Vercel account
- the Mac the children will use

## How it fits together

```
Claude Code ──► GitHub (bip-island, private)
                 │  every merge to main
                 ▼
         GitHub Actions (a Mac in the cloud)
         builds, tests and signs the app
                 │
                 ▼
         Vercel (a tiny public website)
         download page + update feed
                 │  the app checks every hour
                 ▼
         Your Mac: Bip Island updates itself
```

The code stays private. Only the finished app and its update feed go on the public Vercel site, at an address nobody will guess.

Updates are signed with a secret key that only GitHub knows. The app checks that signature before installing anything, so a fake update can't get in.

---

## Step 1: Make a Vercel account and token

1. Go to **vercel.com** and sign up (the free **Hobby** plan is fine). Signing up with your GitHub account is easiest.
2. Open **vercel.com/account/settings/tokens** (click your avatar → **Account Settings** → **Tokens**).
3. Click **Create Token**:
   - **Token name:** `bip-island-github`
   - **Scope:** your account (or your Hobby team, if it asks)
   - **Expiration:** **No Expiration** (otherwise updates stop when it expires)
4. Click **Create** and **copy the token**. Vercel only shows it once.

You don't need to create a Vercel project yourself. Step 3 does that for you.

## Step 2: Add the token to GitHub

1. Open the repository on GitHub → **Settings** (top bar) → **Secrets and variables** (left side) → **Actions**.
2. Stay on the **Secrets** tab and click **New repository secret**.
3. **Name:** `VERCEL_TOKEN`  **Secret:** paste the token. Click **Add secret**.

## Step 3: Run "Set up updates"

This makes the update signing keys and the Vercel project, without needing a Mac.

1. In the repository, click the **Actions** tab.
2. In the left list, click **Set up updates**.
3. Click **Run workflow** (on the right) → leave the options as they are → green **Run workflow** button.
4. Wait about a minute until it has a green tick, then click on the run.
5. The run's page shows a **summary** with three values. Add each one as a new repository secret, the same way as in step 2:

| Secret name | Where it comes from |
| --- | --- |
| `SPARKLE_PRIVATE_KEY` | "1. Update signing key" in the summary |
| `VERCEL_ORG_ID` | "2. Vercel" table in the summary |
| `VERCEL_PROJECT_ID` | "2. Vercel" table in the summary |

6. **Also save `SPARKLE_PRIVATE_KEY` in your password manager.** If it's ever lost, the installed app can't be updated and has to be reinstalled by hand.
7. Note the **download page** address from the summary (something like `https://bip-island-updates.vercel.app/`).
8. Delete this run, because its page shows the private key: the **⋯** menu at the top right of the run → **Delete workflow run**.

You should now have four secrets: `VERCEL_TOKEN`, `SPARKLE_PRIVATE_KEY`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID`.

> Running **Set up updates** again is safe: it won't replace keys you already have unless you tick "Make NEW update keys". Only do that if the key was lost or leaked; every Mac will then need a manual reinstall.

## Step 4: Publish the first build

1. **Actions** tab → **Build** (left list) → **Run workflow** → branch **main** → **Run workflow**.
2. Wait for the green tick (about 10–15 minutes). The run's summary says **Published version …** with the download page link.

From now on this happens by itself every time something is merged to `main`.

If the summary says **Built but not published**, one of the four secrets is missing or misspelt. Check the names in step 3 and run the build again.

## Step 5: Install it on the Mac (once)

1. On the children's Mac, open the download page in Safari and click **Download for Mac**.
2. Open **Downloads** and double-click the zip. Drag **Bip Island** into **Applications**.
3. The first time only, macOS needs your permission, because the app isn't from the App Store:
   - **Right-click** (or Control-click) **Bip Island** in Applications → **Open** → **Open**.
   - On **macOS 15 Sequoia or newer** that may just show a warning. If so, click **Done**, then go to **System Settings → Privacy & Security**, scroll down to *"Bip Island" was blocked…* and click **Open Anyway**, then enter your Mac password.
4. Bip Island opens full screen. Optionally, drag it from Applications into the Dock for the children.

## Using the kid lock

- The game fills the screen. The Dock, menu bar, Cmd-Tab and Cmd-Q are all blocked.
- **To get out:** hold **Esc** for **3 seconds**. A small ring fills in the top-right corner. Then answer the maths question. The parent area lets you **Quit**, **Check for updates now**, or go **Back to the game**.
- **Parent mode:** hold the **Option (⌥)** key while opening Bip Island to run it in a normal window without the kid lock (handy for trying things out).
- Shutting down, restarting or logging out of the Mac always works, even while the game is open.

## How updates arrive

- The app checks the update feed **when it opens** and **every hour** after that. It never installs anything by itself.
- When a new version is out, a small **Update ready** button appears in the top-right corner of the game.
- Tap it, then enter the parent passcode (or answer the maths question). The update window opens: choose **Install Update**, and the game restarts on the new version.
- You can also update from the parent area → Settings → **Install the update** (or **Check for updates now**).
- The parent area shows the version and build number, so you can check which build is installed. The build number matches the run number on GitHub's Actions tab.
- Updates never need the right-click → Open step again.

## The voice

Until the real narrator voice (made in ElevenLabs) is ready, the game uses the Mac's built-in computer voice as a **placeholder**. The parent area shows how many clips are still placeholders. The placeholder phonics sounds aren't accurate enough to teach with, so treat the game as a try-out until the real clips are in. See `audio/README.md` and `audio/script.csv` for the full recording script.

## If something goes wrong

| Problem | What to do |
| --- | --- |
| Build summary says **Built but not published** | A secret is missing or misspelt. Compare with the names in step 3. |
| Build fails at **Decide whether to publish** | Vercel rejected the token or IDs. Make a new Vercel token, update `VERCEL_TOKEN`, and run **Set up updates** again to get fresh IDs. |
| Build fails at **Publish to Vercel** with a login message | In Vercel → your project → **Settings → Deployment Protection**, turn **Vercel Authentication** off. The feed has to be public. |
| The download page won't open the app | Use step 5.3 (Privacy & Security → Open Anyway). |
| The game won't let me quit | Hold Esc for a full 3 seconds without pressing anything else, then answer the sum. As a last resort, hold the Mac's power button. |
| Want a nicer web address | Add your own domain to the Vercel project, then add a repository **variable** (Secrets and variables → Actions → **Variables** tab) called `UPDATE_FEED_URL` set to `https://your.domain/appcast.xml`. Do this **before** installing; existing installs keep using the old address. |

## Later: proper Apple signing

Right now the app is "ad-hoc" signed, which is why the first launch needs the extra permission step. With a paid Apple Developer account (US$99 a year), the build can be switched to Developer ID signing and notarisation. Then the first install is a plain double-click. This only needs changes in the build workflow and new secrets; the app and its updates carry on as they are.
