// Notes in the Blob store which commit was published last (<prefix>published.json). The daily build
// (godot.yml, the "changed" job) reads it to skip a day when nothing that goes into the game has
// changed since. Run as the publish job's last step, so a release whose upload, feed check or
// download page didn't finish isn't noted, and is built again.
//
// Usage: node mark_published.mjs <prefix, e.g. godot-test/>
// Env:   BLOB_READ_WRITE_TOKEN, GITHUB_SHA, optional VERSION
import { put } from '@vercel/blob';

const [prefix] = process.argv.slice(2);
const token = process.env.BLOB_READ_WRITE_TOKEN;
const sha = process.env.GITHUB_SHA;
if (!prefix || !token || !sha) {
  console.error('Usage: node mark_published.mjs <prefix>, with BLOB_READ_WRITE_TOKEN and GITHUB_SHA set');
  process.exit(2);
}

await put(`${prefix}published.json`, JSON.stringify({ sha, version: process.env.VERSION ?? '' }), {
  access: 'public',
  token,
  addRandomSuffix: false,
  allowOverwrite: true,
  contentType: 'application/json',
  cacheControlMaxAge: 60,
});
console.log(`Noted ${sha.slice(0, 7)} as published.`);
