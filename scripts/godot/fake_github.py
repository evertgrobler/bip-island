#!/usr/bin/env python3
"""A stand-in for GitHub Releases, for the Mac and Windows install-and-update tests in CI.

Serves one folder of Velopack release files (from `vpk pack`) as a single published GitHub Release,
the way Velopack's GithubSource reads them. For an address that isn't github.com, GithubSource asks
<host>/api/v3/repos/<owner>/<repo>/releases, then downloads each asset's browser_download_url. So the
installed game updates through exactly the code it uses at home, from this machine, with no token.

Usage: python3 fake_github.py <release folder> <port>
Repo address for the game: http://127.0.0.1:<port>/test/bip-island
"""
import json
import os
import sys
from datetime import datetime, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import unquote, urlsplit

folder = os.path.abspath(sys.argv[1])
port = int(sys.argv[2])
base = f"http://127.0.0.1:{port}"
published = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def release_list():
    assets = [
        {
            "name": name,
            "url": f"{base}/download/{name}",
            "browser_download_url": f"{base}/download/{name}",
            "content_type": "application/octet-stream",
        }
        for name in sorted(os.listdir(folder))
        if os.path.isfile(os.path.join(folder, name))
    ]
    return [{"name": "test", "tag_name": "test", "prerelease": False, "draft": False,
             "published_at": published, "assets": assets}]


class Handler(BaseHTTPRequestHandler):
    def do_GET(self):
        path = urlsplit(self.path).path
        if path.startswith("/api/v3/repos/") and path.endswith("/releases"):
            body = json.dumps(release_list()).encode()
            return self.send(200, "application/json", body)
        if path.startswith("/download/"):
            name = os.path.basename(unquote(path[len("/download/"):]))
            file = os.path.join(folder, name)
            if os.path.isfile(file):
                with open(file, "rb") as f:
                    return self.send(200, "application/octet-stream", f.read())
        self.send(404, "text/plain", b"not found")

    def send(self, status, content_type, body):
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)


print(f"Serving {folder} as {base}/test/bip-island", flush=True)
ThreadingHTTPServer(("127.0.0.1", port), Handler).serve_forever()
