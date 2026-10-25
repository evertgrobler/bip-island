#!/usr/bin/env python3
"""GitHub Releases chores for publishing a version to the downloads repository.

Velopack's `vpk upload github` uploads each computer's update files; this does the rest, with the
GitHub REST API and the same token.

  trim   <folder> <channel>   before uploading a vpk output folder: drop the Portable.zip (not used
                              for installs or updates) and name the Windows installer
                              BipIsland-win-Setup.exe, the name the download page links to
  clear  <tag>                remove leftovers before publishing: unfinished drafts, and any release
                              (and tag) already named <tag>, so a re-run starts clean
  attach <tag> <file>...      upload extra files (the Mac .dmg) to the release tagged <tag>
  finish <tag> --keep N       check the release is published with both computers' files, mark it
                              the latest (the download page's links follow "latest"), then delete
                              all but the newest N releases and their tags

Env: DOWNLOADS_TOKEN (contents read/write on the repository), DOWNLOADS_REPO (owner/name),
     optional GITHUB_API_URL (default https://api.github.com; set by GitHub Actions anyway).
"""
import argparse
import json
import os
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
        with urllib.request.urlopen(request, timeout=1800) as response:
            text = response.read().decode() or "null"
            return json.loads(text)
    except urllib.error.HTTPError as e:
        if ok404 and e.code == 404:
            return None
        sys.exit(f"GitHub said {e.code} to {method} {url}: {e.read().decode()[:500]}")


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
    """Edits vpk's list of files to upload (assets.<channel>.json, VPK_VERSION 1.2.161's format)."""
    listing = os.path.join(folder, f"assets.{channel}.json")
    with open(listing) as f:
        assets = json.load(f)
    kept = []
    for asset in assets:
        name = asset["RelativeFileName"]
        if asset["Type"] == "Portable":
            print(f"Not uploading {name}")
            continue
        if asset["Type"] == "Installer" and name.endswith("-win-Setup.exe") and name != WIN_SETUP:
            os.replace(os.path.join(folder, name), os.path.join(folder, WIN_SETUP))
            asset["RelativeFileName"] = WIN_SETUP
            print(f"Renamed {name} to {WIN_SETUP}")
        kept.append(asset)
    with open(listing, "w") as f:
        json.dump(kept, f)


def clear(tag):
    for release in releases():
        if release["draft"] or release["tag_name"] == tag:
            delete_release(release)
    # A tag left behind by a release deleted by hand.
    call("DELETE", f"/git/refs/tags/{urllib.parse.quote(tag)}", ok404=True)


def attach(tag, files):
    release = by_tag(tag)
    upload = release["upload_url"].split("{")[0]
    for file in files:
        name = os.path.basename(file)
        with open(file, "rb") as f:
            call("POST", f"{upload}?name={urllib.parse.quote(name)}", data=f, content_type="application/octet-stream")
        print(f"Uploaded {name} ({os.path.getsize(file) / 1e6:.1f} MB) to {tag}")


def finish(tag, keep):
    release = by_tag(tag)
    if release["draft"]:
        sys.exit(f"Release {tag} is still a draft.")
    names = {a["name"] for a in release["assets"]}
    missing = [n for n in REQUIRED if n not in names]
    if missing:
        sys.exit(f"Release {tag} is missing {', '.join(missing)}.")
    call("PATCH", f"/releases/{release['id']}", body={"make_latest": "true"})
    print(f"Release {tag} is the latest, with {len(names)} files.")
    published = [r for r in releases() if not r["draft"]]
    published.sort(key=lambda r: r.get("published_at") or "", reverse=True)
    for old in published[keep:]:
        if old["tag_name"] != tag:
            delete_release(old)


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
    args = parser.parse_args()
    if args.command == "trim":
        return trim(args.folder, args.channel)
    if not TOKEN or "/" not in REPO:
        sys.exit("Set DOWNLOADS_TOKEN and DOWNLOADS_REPO (owner/name).")
    if args.command == "clear":
        clear(args.tag)
    elif args.command == "attach":
        attach(args.tag, args.files)
    else:
        if args.keep < 2:
            sys.exit("Keep at least 2 releases.")
        finish(args.tag, args.keep)


if __name__ == "__main__":
    main()
