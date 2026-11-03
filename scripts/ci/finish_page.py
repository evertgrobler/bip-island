#!/usr/bin/env python3
"""Last touches to the built download page (deploy_site.sh), so it never offers something that won't work:

- removes the narrator sample buttons whose clip isn't recorded yet (MISSING_CLIPS, space-separated);
- removes a computer's "install without the warning" line when CI's check of it failed on this build
  (MAC_LINE_OK / WIN_LINE_OK = "failure"; anything else, or unset, keeps it).

Usage: finish_page.py <built index.html>
"""
import os
import re
import sys

path = sys.argv[1]
page = open(path, encoding="utf-8").read()

for clip in os.environ.get("MISSING_CLIPS", "").split():
    page, n = re.subn(r'[ \t]*<button class="voice"[^>]*data-clip="%s".*?</button>\n?' % re.escape(clip), "", page, flags=re.S)
    if n != 1:
        sys.exit(f"Couldn't find the sample button for {clip}.")
    print(f"Left out the sample {clip} (not recorded yet).")

for computer, pre in (("MAC", "cmd-mac"), ("WIN", "cmd-win")):
    if os.environ.get(f"{computer}_LINE_OK") == "failure":
        page, n = re.subn(r'[ \t]*<details>(?:(?!</details>).)*?id="%s".*?</details>\n?' % pre, "", page, flags=re.S)
        if n != 1:
            sys.exit(f"Couldn't find the install line block {pre}.")
        print(f"::warning::Left the {pre} install line off the page: CI's check of it failed on this build.")

open(path, "w", encoding="utf-8").write(page)
