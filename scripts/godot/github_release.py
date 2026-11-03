#!/usr/bin/env python3
"""GitHub Releases chores for publishing a version to the game's own repository (evertgrobler/bip-island).

Velopack's `vpk upload github` uploads each computer's update files; this does the rest, with the
GitHub REST API and the same token.

  trim   <folder> <channel>   before uploading a vpk output folder: drop the Portable.zip (not used
                              for installs or updates) and name the Windows installer
                              BipIsland-win-Setup.exe, the name the download page links to
  clear  <tag>                remove leftovers before publishing: this pipeline's unfinished drafts
                              ("Bip Island ..."), and an incomplete release (and tag) named <tag>
  attach <tag> <file>...      upload extra files (the Mac .dmg) to the release tagged <tag>
  finish <tag> --keep N       check the draft holds both computers' files and both downloads, then
                              publish it as the latest (the download page's links follow "latest"), note the commit
                              it was built from (GITHUB_SHA) in its description, then delete all but
                              the newest N releases and their tags
  published                   print the commit the latest release was built from (empty if none)
  done <tag>                  exit 0 if <tag> is already published with every file (a re-run)
  latest                      print the tag of the release GitHub calls latest
  prune  <tag> --keep N       delete all but the newest N (at least 2) published releases (never <tag>)

Env: DOWNLOADS_TOKEN (contents read/write on the repository; the workflow's own GITHUB_TOKEN),
     DOWNLOADS_REPO (owner/name; the game's own repository),
     optional GITHUB_API_URL (default https://api.github.com; set by GitHub Actions anyway).
"""
import argparse
import json
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request

API = os.environ.get("GITHUB_API_URL", "https://api.github.com").rstrip("/")
REPO = os.environ.get("DOWNLOADS_REPO", "")
TOKEN = os.environ.get("DOWNLOADS_TOKEN", "")
# What every published version must hold: the update index for each computer and the two downloads.
WIN_SETUP = "BipIsland-win-Setup.exe"
REQUIRED = ["releases.win.json", "releases.osx.json", WIN_SETUP, "BipIsland-mac.dmg"]


def call(method, url, body=None, data=None, content_type="application/json", ok404=False):
    if not url.startswith("http"):
        url = f"{API}/repos/{REPO}{url}"
    headers = {
        "Authorization": f"Bearer {TOKEN}",
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
        "User-Agent": "bip-island-publish",
    }
    if body is not None:
        data = json.dumps(body).encode()
    if data is not None:
        headers["Content-Type"] = content_type
        if hasattr(data, "fileno"):
            headers["Content-Length"] = str(os.fstat(data.fileno()).st_size)
    request = urllib.request.Request(url, data=data, method=method, headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=300) as response:
            text = response.read().decode() or "null"
            return json.loads(text)
    except urllib.error.HTTPError as e:
        detail = e.read().decode()
        # Deleting a tag that isn't there answers 422 "Reference does not exist", not 404: the same thing.
        if ok404 and (e.code == 404 or (e.code == 422 and "Reference does not exist" in detail)):
            return None
        sys.exit(f"GitHub said {e.code} to {method} {url}: {detail[:500]}")


def releases():
    """Every release, drafts included (the token can see them), newest first."""
    found, page = [], 1
    while True:
        batch = call("GET", f"/releases?per_page=100&page={page}")
        found += batch
        if len(batch) < 100:
            return found
        page += 1


def delete_release(release):
    call("DELETE", f"/releases/{release['id']}", ok404=True)
    # A draft's tag isn't made until it's published, so there may be no tag to delete.
    call("DELETE", f"/git/refs/tags/{urllib.parse.quote(release['tag_name'])}", ok404=True)
    state = "draft" if release["draft"] else "release"
    print(f"Deleted {state} {release['tag_name']} ({release.get('name') or ''})")


def by_tag(tag):
    match = [r for r in releases() if r["tag_name"] == tag]
    if not match:
        sys.exit(f"No release tagged {tag} in {REPO}.")
    return match[0]


def trim(folder, channel):
    """Edits vpk's list of files to upload (assets.<channel>.json, VPK_VERSION 1.2.161's format).
    Stops the run if the listing isn't what it expects, before anything is uploaded."""
    listing = os.path.join(folder, f"assets.{channel}.json")
    with open(listing) as f:
        assets = json.load(f)
    kept, dropped, installer = [], 0, False
    for asset in assets:
        name = asset["RelativeFileName"]
        if asset["Type"] == "Portable":
            print(f"Not uploading {name}")
            dropped += 1
            continue
        if asset["Type"] == "Installer" and name.endswith("-win-Setup.exe"):
            if name != WIN_SETUP:
                os.replace(os.path.join(folder, name), os.path.join(folder, WIN_SETUP))
                asset["RelativeFileName"] = WIN_SETUP
                print(f"Renamed {name} to {WIN_SETUP}")
            installer = True
        kept.append(asset)
    if not dropped:
        print(f"::warning::{listing} has no Portable asset: vpk's listing may have changed; check trim against it.")
    if channel == "win" and not installer:
        sys.exit(f"{listing} has no Windows installer (Type Installer, *-win-Setup.exe): check trim against it.")
    with open(listing, "w") as f:
        json.dump(kept, f)


def ours(release):
    """A release this pipeline makes (vpk names them "Bip Island <version>"), not one made by hand."""
    return (release.get("name") or "").startswith("Bip Island ")


def uploaded(release):
    """The names of a release's files that finished uploading (a broken-off upload is a "starter")."""
    return {a["name"] for a in release["assets"] if a.get("state", "uploaded") == "uploaded"}


def complete(release):
    return release is not None and not release["draft"] and all(n in uploaded(release) for n in REQUIRED)


def clear(tag):
    """Removes this pipeline's unfinished drafts, and an incomplete release already tagged <tag>.
    Drafts made by hand are left alone."""
    for release in releases():
        if (release["draft"] and ours(release)) or (release["tag_name"] == tag and not complete(release)):
            delete_release(release)
    if not any(r["tag_name"] == tag for r in releases()):
        # A tag left behind by a release deleted by hand.
        call("DELETE", f"/git/refs/tags/{urllib.parse.quote(tag)}", ok404=True)


def done(tag):
    """Exit 0 if <tag> is already published with every file (a re-run of a publish that worked)."""
    sys.exit(0 if any(r["tag_name"] == tag and complete(r) for r in releases()) else 1)


def latest():
    """Print the tag of the release GitHub calls latest (what the download links fetch)."""
    release = call("GET", "/releases/latest", ok404=True)
    print((release or {}).get("tag_name", ""))


def attach(tag, files):
    release = by_tag(tag)
    upload = release["upload_url"].split("{")[0]
    for file in files:
        name = os.path.basename(file)
        with open(file, "rb") as f:
            call("POST", f"{upload}?name={urllib.parse.quote(name)}", data=f, content_type="application/octet-stream")
        print(f"Uploaded {name} ({os.path.getsize(file) / 1e6:.1f} MB) to {tag}")


def finish(tag, keep):
    """Checks the draft holds both computers' files and both downloads, and only then publishes it."""
    release = by_tag(tag)
    names = uploaded(release)
    missing = [n for n in REQUIRED if n not in names]
    if missing:
        sys.exit(f"Release {tag} is missing {', '.join(missing)}, so it stays a draft (installed games can't see it).")
    sha = os.environ.get("GITHUB_SHA", "")
    note = f"Bip Island {tag}. Built from commit {sha}." if sha else f"Bip Island {tag}."
    body = {"draft": False, "make_latest": "true", "body": note}
    if sha:
        body["target_commitish"] = sha  # the tag is made on publishing: point it at the commit built
    call("PATCH", f"/releases/{release['id']}", body=body)
    print(f"Release {tag} is published and the latest, with {len(names)} files.")
    prune(tag, keep)


def prune(tag, keep):
    """Deletes all but the newest <keep> (at least 2) of this pipeline's published releases (never <tag>).
    Never fails: the new version is already out, so any error here, network ones included, is a warning."""
    try:
        published = [r for r in releases() if not r["draft"] and ours(r)]
        published.sort(key=lambda r: r.get("published_at") or "", reverse=True)
        for old in published[max(keep, 2):]:
            if old["tag_name"] != tag:
                delete_release(old)
    except (SystemExit, Exception) as e:
        print(f"::warning::Couldn't remove old releases: {e}")


def published():
    """The commit the latest release says it was built from, or nothing."""
    latest = call("GET", "/releases/latest", ok404=True)
    match = re.search(r"Built from commit ([0-9a-f]{40})", (latest or {}).get("body") or "")
    print(match.group(1) if match else "")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)
    p = sub.add_parser("trim")
    p.add_argument("folder")
    p.add_argument("channel")
    sub.add_parser("clear").add_argument("tag")
    p = sub.add_parser("attach")
    p.add_argument("tag")
    p.add_argument("files", nargs="+")
    p = sub.add_parser("finish")
    p.add_argument("tag")
    p.add_argument("--keep", type=int, default=5)
    sub.add_parser("published")
    sub.add_parser("done").add_argument("tag")
    sub.add_parser("latest")
    p = sub.add_parser("prune")
    p.add_argument("tag")
    p.add_argument("--keep", type=int, default=5)
    args = parser.parse_args()
    if args.command == "trim":
        return trim(args.folder, args.channel)
    if not TOKEN or "/" not in REPO:
        sys.exit("Set DOWNLOADS_TOKEN and DOWNLOADS_REPO (owner/name).")
    if args.command == "clear":
        clear(args.tag)
    elif args.command == "attach":
        attach(args.tag, args.files)
    elif args.command == "published":
        published()
    elif args.command == "done":
        done(args.tag)
    elif args.command == "latest":
        latest()
    elif args.command == "prune":
        prune(args.tag, args.keep)
    else:
        finish(args.tag, args.keep)


if __name__ == "__main__":
    main()
