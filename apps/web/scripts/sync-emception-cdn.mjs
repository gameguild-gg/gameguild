// Sync the emception CDN payload into apps/web/public/emception (gitignored,
// served at build/dev time as /emception/*). Wired into prebuild/predev.
//
// Three-tier fallback chain:
//   Tier 1 (dev primary): tools/emception/public/cdn (local WASM build)
//   Tier 2 (Docker always; dev fallback): npm emception@latest tarball, cdn/
//                                         subset extracted on demand into cache.
//   Tier 3 (last resort): jsDelivr URL — no sync, instruct env var so the app
//                         fetches WASM at runtime.
//
// Skips the 108MB copy when the target manifest already matches the source.
import { promises as fs } from 'node:fs';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const execFileP = promisify(execFile);

const REPO_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../..');
export const SOURCE_LOCAL = path.join(REPO_ROOT, 'tools/emception/public/cdn');
export const SOURCE_NPM_CACHE = path.join(REPO_ROOT, 'node_modules/.cache/emception-cdn');
export const TARGET = path.join(REPO_ROOT, 'apps/web/public/emception');
export const JSDELIVR_MANIFEST_URL = 'https://cdn.jsdelivr.net/npm/emception@latest/cdn/manifest.json';
const NPM_REGISTRY_URL = 'https://registry.npmjs.org/emception/latest';

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

async function exists(p) {
  try {
    await fs.stat(p);
    return true;
  } catch {
    return false;
  }
}

async function directoryNotEmpty(dir) {
  if (!(await exists(dir))) return false;
  const entries = await fs.readdir(dir);
  return entries.length > 0;
}

// Resolve the npm tarball URL via the registry API (no npm CLI dependency).
async function resolveNpmTarballUrlWith(fetchImpl) {
  const resp = await fetchImpl(NPM_REGISTRY_URL);
  if (!resp.ok) throw new Error(`npm registry returned ${resp.status}`);
  const meta = await resp.json();
  const tarballUrl = meta?.dist?.tarball;
  if (!tarballUrl) throw new Error('npm registry response missing dist.tarball');
  return tarballUrl;
}

// ponytail: shell out to system `tar` instead of pulling in the npm `tar`
// package — every target (macOS dev, alpine Docker) ships tar in $PATH.
async function extractTarball(tgzPath, destDir) {
  await fs.mkdir(destDir, { recursive: true });
  await execFileP('tar', ['-xzf', tgzPath, '-C', destDir]);
}

// Tier 2: download emception@latest tarball, extract package/cdn/ subset into
// the cache dir. Re-uses the cache if a previous extraction is present.
export async function fetchNpmTarball({ cacheDir, log = console.log, fetchImpl = fetch, extractImpl = extractTarball }) {
  // npm tarballs already wrap their contents in a top-level `package/` dir, so
  // extracting straight into cacheDir yields cacheDir/package/{cdn,dist,...}.
  const cachedTarball = path.join(cacheDir, 'package.tgz');
  const cachedExtractedRoot = cacheDir;
  const cachedCdn = path.join(cachedExtractedRoot, 'package', 'cdn');

  if (await directoryNotEmpty(cachedCdn)) {
    log('npm emception tarball cache hit');
    return cachedCdn;
  }

  await fs.mkdir(cacheDir, { recursive: true });

  log('fetching emception@latest tarball URL from npm registry...');
  const tarballUrl = await resolveNpmTarballUrlWith(fetchImpl);

  log(`downloading ${tarballUrl}...`);
  const tarballResp = await fetchImpl(tarballUrl);
  if (!tarballResp.ok) throw new Error(`tarball download failed: ${tarballResp.status}`);
  const tarballBuf = Buffer.from(await tarballResp.arrayBuffer());
  await fs.writeFile(cachedTarball, tarballBuf);

  await fs.rm(path.join(cachedExtractedRoot, 'package'), { recursive: true, force: true });
  log('extracting cdn/ subset from tarball...');
  await extractImpl(cachedTarball, cachedExtractedRoot);

  if (!(await directoryNotEmpty(cachedCdn))) {
    throw new Error('npm tarball did not contain package/cdn/ — emception@latest may be malformed');
  }
  return cachedCdn;
}

async function checkJsdelivrReachable(fetchImpl = fetch) {
  try {
    const resp = await fetchImpl(JSDELIVR_MANIFEST_URL, { method: 'HEAD' });
    return resp.ok;
  } catch {
    return false;
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

// Returns { tier: 1|2|3, action: 'skip'|'synced'|'jsdelivr', source|url }.
// Throws if every tier is unavailable.
export async function syncEmceptionCdn({
  srcDir = SOURCE_LOCAL,
  tgtDir = TARGET,
  log = console.log,
  npmCacheDir = SOURCE_NPM_CACHE,
  fetchNpm = true,
  allowJsDelivr = true,
  fetchImpl = fetch,
  extractImpl = extractTarball,
} = {}) {
  // Tier 1: local WASM build.
  if (srcDir && (await directoryNotEmpty(srcDir))) {
    const srcManifest = path.join(srcDir, 'manifest.json');
    const tgtManifest = path.join(tgtDir, 'manifest.json');
    if ((await exists(tgtManifest)) && (await manifestsMatch(srcManifest, tgtManifest))) {
      log('emception cdn up to date (local build)');
      return { tier: 1, action: 'skip', source: srcDir };
    }
    await copyDirCounted(srcDir, tgtDir, log);
    log(`synced from local build: ${srcDir}`);
    return { tier: 1, action: 'synced', source: srcDir };
  }

  // Tier 2: npm emception@latest tarball.
  if (fetchNpm && npmCacheDir) {
    try {
      const npmCdnDir = await fetchNpmTarball({ cacheDir: npmCacheDir, log, fetchImpl, extractImpl });
      const npmManifest = path.join(npmCdnDir, 'manifest.json');
      const tgtManifest = path.join(tgtDir, 'manifest.json');
      if ((await exists(tgtManifest)) && (await manifestsMatch(npmManifest, tgtManifest))) {
        log('emception cdn up to date (npm emception@latest)');
        return { tier: 2, action: 'skip', source: npmCdnDir };
      }
      await copyDirCounted(npmCdnDir, tgtDir, log);
      log(`synced from npm emception@latest: ${npmCdnDir}`);
      return { tier: 2, action: 'synced', source: npmCdnDir };
    } catch (err) {
      log(`npm fetch failed: ${err.message}`);
    }
  }

  // Tier 3: jsDelivr runtime URL — no sync, just instruct env var.
  if (allowJsDelivr && (await checkJsdelivrReachable(fetchImpl))) {
    log('WARNING: no local emception CDN; falling back to jsDelivr runtime fetch.');
    log(`         Set NEXT_PUBLIC_EMCEPTION_MANIFEST_URL=${JSDELIVR_MANIFEST_URL} in your environment.`);
    log('         (For a local WASM build, run: pnpm --dir tools/emception run build:all)');
    return { tier: 3, action: 'jsdelivr', url: JSDELIVR_MANIFEST_URL };
  }

  throw new Error(
    'No emception CDN source available. Run `pnpm --dir tools/emception run build:all`, ' +
      'or set NEXT_PUBLIC_EMCEPTION_MANIFEST_URL manually.',
  );
}

// CLI entrypoint — only runs when executed directly, never on import (vitest).
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const noNpm = process.argv.includes('--no-npm');
  const jsdelivrOnly = process.argv.includes('--jsdelivr-only');
  try {
    const result = await syncEmceptionCdn({
      srcDir: SOURCE_LOCAL,
      tgtDir: TARGET,
      npmCacheDir: SOURCE_NPM_CACHE,
      fetchNpm: !jsdelivrOnly && !noNpm,
      allowJsDelivr: !noNpm,
    });
    if (result.tier === 3) {
      console.log(`\nNext step: export NEXT_PUBLIC_EMCEPTION_MANIFEST_URL=${result.url}`);
    }
  } catch (err) {
    console.error(`error: emception CDN sync failed: ${err.message}`);
    process.exit(1);
  }
}
