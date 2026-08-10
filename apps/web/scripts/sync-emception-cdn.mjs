// Build-time sync of the emception CDN payload from tools/emception into
// apps/web/public/emception (gitignored, served at build/dev time as /emception/*).
// Idempotent: when the target manifest already matches the source, skips the
// 108MB copy and prints "emception cdn up to date". Run via
// `pnpm --filter @game-guild/web sync:emception` (wired into prebuild/predev).
import { promises as fs } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const REPO_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../..');
export const SOURCE = path.join(REPO_ROOT, 'tools/emception/public/cdn');
export const TARGET = path.join(REPO_ROOT, 'apps/web/public/emception');

// Byte-compare of the two manifest.json files. Any missing/unreadable file on
// either side counts as "no match" so the caller re-copies.
export async function manifestsMatch(srcManifestPath, tgtManifestPath) {
  try {
    const [src, tgt] = await Promise.all([
      fs.readFile(srcManifestPath),
      fs.readFile(tgtManifestPath),
    ]);
    return src.equals(tgt);
  } catch {
    return false;
  }
}

export async function syncEmceptionCdn({ srcDir, tgtDir, log = console.log }) {
  const srcManifest = path.join(srcDir, 'manifest.json');
  const tgtManifest = path.join(tgtDir, 'manifest.json');

  const tgtManifestExists = await fs.stat(tgtManifest).then(() => true, () => false);
  if (tgtManifestExists && (await manifestsMatch(srcManifest, tgtManifest))) {
    log('emception cdn up to date');
    return { copied: false, reason: 'up-to-date' };
  }

  await fs.cp(srcDir, tgtDir, { recursive: true });

  // Count files + total bytes from the source so the log is checkable.
  let fileCount = 0;
  let bytes = 0;
  for (const entry of await fs.readdir(srcDir, { recursive: true })) {
    const stat = await fs.stat(path.join(srcDir, entry));
    if (stat.isFile()) {
      fileCount += 1;
      bytes += stat.size;
    }
  }
  log(`copied ${fileCount} files (${bytes} bytes) to ${tgtDir}`);
  return { copied: true, reason: tgtManifestExists ? 're-copied' : 'fresh' };
}

// Guard: only run the real sync when executed directly (`node scripts/...`),
// never when the module is imported by the vitest unit test.
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    await fs.stat(SOURCE);
  } catch {
    console.error(`error: emception CDN source not found: ${SOURCE}`);
    console.error('run `pnpm run build:emception` (tools/emception build:packages) first');
    process.exit(1);
  }
  try {
    await syncEmceptionCdn({ srcDir: SOURCE, tgtDir: TARGET });
  } catch (err) {
    console.error(`error: emception CDN sync failed: ${err.message}`);
    process.exit(1);
  }
}
