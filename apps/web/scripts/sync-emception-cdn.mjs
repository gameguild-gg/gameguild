import { promises as fs } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const REPO_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../..');
export const SOURCE_CANONICAL = path.join(
  REPO_ROOT,
  'tools',
  'emception',
  'artifacts',
  'toolchain',
  'release',
  'cdn',
);
export const TARGET = path.join(REPO_ROOT, 'apps', 'web', 'public', 'emception');

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

async function validateCanonicalRelease(srcDir) {
  const manifestPath = path.join(srcDir, 'manifest.json');
  let manifest;
  try {
    manifest = JSON.parse(await fs.readFile(manifestPath, 'utf8'));
  } catch (error) {
    const detail = error instanceof Error ? error.message : String(error);
    throw new Error(`canonical Toolchain release is unavailable at ${srcDir}: ${detail}`);
  }
  if (manifest?.schemaVersion !== 2) {
    throw new Error(`canonical Toolchain release must use manifest schemaVersion 2: ${srcDir}`);
  }
}

async function copyDirCounted(srcDir, tgtDir, log) {
  await fs.rm(tgtDir, { recursive: true, force: true });
  await fs.cp(srcDir, tgtDir, { recursive: true });
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
}

export async function syncEmceptionCdn({
  srcDir = SOURCE_CANONICAL,
  tgtDir = TARGET,
  log = console.log,
} = {}) {
  await validateCanonicalRelease(srcDir);
  const srcManifest = path.join(srcDir, 'manifest.json');
  const tgtManifest = path.join(tgtDir, 'manifest.json');
  if (await manifestsMatch(srcManifest, tgtManifest)) {
    log('emception CDN is up to date (canonical Toolchain release)');
    return { action: 'skip', source: srcDir };
  }
  await copyDirCounted(srcDir, tgtDir, log);
  log(`synced from canonical Toolchain release: ${srcDir}`);
  return { action: 'synced', source: srcDir };
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  syncEmceptionCdn().catch((error) => {
    console.error(`error: emception CDN sync failed: ${error instanceof Error ? error.message : String(error)}`);
    process.exitCode = 1;
  });
}
