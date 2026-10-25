// Uploads the Godot test release to the public Vercel Blob store, then removes older versions so the
// store keeps only the latest (to stay inside the free storage). Update feeds go up last, so an
// installed game never sees a feed that points at a package that isn't there yet.
//
// Usage: node publish_blob.mjs <windows release dir> <mac release dir> <prefix, e.g. godot-test/>
// Env:   BLOB_READ_WRITE_TOKEN
import { createMultipartUploader, del, list, put } from '@vercel/blob';
import fs from 'node:fs';
import path from 'node:path';

const [winDir, macDir, prefix] = process.argv.slice(2);
const token = process.env.BLOB_READ_WRITE_TOKEN;
if (!winDir || !macDir || !prefix || !token) {
  console.error('Usage: node publish_blob.mjs <windows dir> <mac dir> <prefix>, with BLOB_READ_WRITE_TOKEN set');
  process.exit(2);
}

const isFeed = (name) => /^(releases\..*\.json|assets\..*\.json|RELEASES)$/.test(name);
const contentType = (name) =>
  name.endsWith('.json') ? 'application/json'
  : name.endsWith('.dmg') ? 'application/x-apple-diskimage'
  : name === 'RELEASES' ? 'text/plain'
  : 'application/octet-stream';

// [local file, blob pathname]
const files = [];
for (const name of fs.readdirSync(winDir)) {
  if (name.endsWith('-Portable.zip')) continue;
  if (name.endsWith('-Setup.exe')) files.push([path.join(winDir, name), `${prefix}BipIsland-win-Setup.exe`]);
  else files.push([path.join(winDir, name), `${prefix}win/${name}`]);
}
const osxDir = path.join(macDir, 'osx');
for (const name of fs.readdirSync(osxDir)) {
  if (name.endsWith('-Portable.zip')) continue;
  files.push([path.join(osxDir, name), `${prefix}osx/${name}`]);
}
files.push([path.join(macDir, 'BipIsland-mac.dmg'), `${prefix}BipIsland-mac.dmg`]);
// Packages and installers first, feeds last.
files.sort((a, b) => Number(isFeed(path.basename(a[0]))) - Number(isFeed(path.basename(b[0]))));

// Every upload call is a Blob "advanced operation", and the free plan allows 2,000 a month. put()'s own
// multipart sends 8 MB parts, each counted (about 20 for one 150 MB installer), so big files go up in
// 64 MB parts instead.
const PART = 64 * 1024 * 1024;

async function upload(file, pathname, options) {
  const size = fs.statSync(file).size;
  if (size <= PART) return put(pathname, fs.readFileSync(file), options);
  const uploader = await createMultipartUploader(pathname, options);
  const parts = [];
  const fd = fs.openSync(file, 'r');
  try {
    for (let offset = 0, n = 1; offset < size; offset += PART, n++) {
      const chunk = Buffer.alloc(Math.min(PART, size - offset));
      fs.readSync(fd, chunk, 0, chunk.length, offset);
      parts.push(await uploader.uploadPart(n, chunk));
    }
  } finally {
    fs.closeSync(fd);
  }
  return uploader.complete(parts);
}

const uploaded = new Map();
for (const [file, pathname] of files) {
  const size = fs.statSync(file).size;
  const name = path.basename(file);
  const blob = await upload(file, pathname, {
    access: 'public',
    token,
    addRandomSuffix: false,
    allowOverwrite: true,
    contentType: contentType(name),
    // Feeds and the fixed-name installers change every release; versioned packages never do.
    cacheControlMaxAge: name.endsWith('.nupkg') ? 60 * 60 * 24 * 30 : 60,
  });
  uploaded.set(pathname, blob.url);
  console.log(`Uploaded ${pathname} (${(size / 1e6).toFixed(1)} MB)`);
}

// Last, a note of which commit this release was built from: the daily build reads it to skip a day
// when nothing that goes into the game has changed since (godot.yml, the "changed" job). It goes up
// only after everything else, so a release that didn't finish uploading is published again.
if (process.env.GITHUB_SHA) {
  const note = `${prefix}published.json`;
  await put(note, JSON.stringify({ sha: process.env.GITHUB_SHA, version: process.env.VERSION ?? '' }), {
    access: 'public',
    token,
    addRandomSuffix: false,
    allowOverwrite: true,
    contentType: 'application/json',
    cacheControlMaxAge: 60,
  });
  uploaded.set(note, '');
}

// Remove anything under the prefix that isn't part of this release.
const stale = [];
let cursor;
do {
  const page = await list({ prefix, cursor, token, limit: 1000 });
  for (const blob of page.blobs) if (!uploaded.has(blob.pathname)) stale.push(blob.url);
  cursor = page.hasMore ? page.cursor : undefined;
} while (cursor);
if (stale.length) {
  await del(stale, { token });
  console.log(`Removed ${stale.length} older file(s).`);
}

const out = process.env.GITHUB_OUTPUT;
const mac = uploaded.get(`${prefix}BipIsland-mac.dmg`);
const win = uploaded.get(`${prefix}BipIsland-win-Setup.exe`);
console.log(`Mac download: ${mac}\nWindows download: ${win}`);
if (out) fs.appendFileSync(out, `mac_url=${mac}\nwin_url=${win}\n`);
