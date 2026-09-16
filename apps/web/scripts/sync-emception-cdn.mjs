import { promises as fs } from 'node:fs';
import { execFile } from 'node:child_process';
import os from 'node:os';
import path from 'node:path';
import { promisify } from 'node:util';
import { fileURLToPath } from 'node:url';
import { extract } from 'tar';

const REPO_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../..');
const execFileAsync = promisify(execFile);
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
export const TOOLCHAIN_PACKAGE_JSON = path.join(
  REPO_ROOT,
  'tools',
  'emception',
  'packages',
  'toolchain',
  'package.json',
);

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

async function readReleaseManifest(srcDir) {
  const manifestPath = path.join(srcDir, 'manifest.json');
  try {
    return JSON.parse(await fs.readFile(manifestPath, 'utf8'));
  } catch (error) {
    const detail = error instanceof Error ? error.message : String(error);
    throw new Error(`canonical Toolchain release is unavailable at ${srcDir}: ${detail}`);
  }
}

async function releaseTreesMatch(srcDir, tgtDir) {
  if (!await manifestsMatch(
    path.join(srcDir, 'manifest.json'),
    path.join(tgtDir, 'manifest.json'),
  )) return false;

  try {
    const sourceFiles = await fs.readdir(srcDir, { recursive: true });
    const targetFiles = await fs.readdir(tgtDir, { recursive: true });
    if (sourceFiles.length !== targetFiles.length) return false;

    const targetSet = new Set(targetFiles);
    for (const entry of sourceFiles) {
      if (!targetSet.has(entry)) return false;
      const [sourceStat, targetStat] = await Promise.all([
        fs.stat(path.join(srcDir, entry)),
        fs.stat(path.join(tgtDir, entry)),
      ]);
      if (sourceStat.isFile() !== targetStat.isFile()) return false;
      if (sourceStat.isFile() && sourceStat.size !== targetStat.size) return false;
    }
    return true;
  } catch {
    return false;
  }
}

async function validateCanonicalRelease(srcDir) {
  const manifest = await readReleaseManifest(srcDir);
  if (manifest?.schemaVersion !== 2) {
    throw new Error(`canonical Toolchain release must use manifest schemaVersion 2: ${srcDir}`);
  }
  return manifest;
}

async function validatePublishedRelease(srcDir, version) {
  const manifest = await readReleaseManifest(srcDir);
  if (manifest?.schemaVersion === 2) {
    if (manifest.artifactVersion !== version) {
      throw new Error(
        `published Toolchain artifact version ${manifest.artifactVersion ?? '<missing>'} does not match ${version}`,
      );
    }
    return manifest;
  }
  if (manifest?.version !== 1) {
    throw new Error(`published Toolchain release uses an unsupported manifest schema: ${srcDir}`);
  }
  return manifest;
}

export async function extractPackageArchive(archivePath, destination, extractArchive = extract) {
  await extractArchive({ cwd: destination, file: archivePath, strict: true });
}

async function hydratePublishedRelease({ srcDir, version, log }) {
  if (!/^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$/.test(version)) {
    throw new Error(`invalid Toolchain package version: ${version}`);
  }

  const tempDir = await fs.mkdtemp(path.join(os.tmpdir(), 'gameguild-emception-'));
  try {
    const npmCommand = process.platform === 'win32' ? 'npm.cmd' : 'npm';
    const { stdout } = await execFileAsync(
      npmCommand,
      ['pack', `emception@${version}`, '--pack-destination', tempDir, '--json'],
      {
        maxBuffer: 10 * 1024 * 1024,
        // Windows exposes npm as npm.cmd, which requires cmd.exe dispatch.
        shell: process.platform === 'win32',
      },
    );
    const packResults = JSON.parse(stdout);
    const filename = Array.isArray(packResults) ? packResults.at(-1)?.filename : undefined;
    if (!filename) throw new Error('npm pack did not return a Toolchain archive filename');

    const archivePath = path.join(tempDir, filename);
    await extractPackageArchive(archivePath, tempDir);
    const publishedCdn = path.join(tempDir, 'package', 'cdn');
    const manifest = await validatePublishedRelease(publishedCdn, version);

    await fs.rm(srcDir, { recursive: true, force: true });
    await fs.mkdir(srcDir, { recursive: true });
    await fs.cp(publishedCdn, srcDir, { recursive: true });
    const schemaVersion = manifest.schemaVersion ?? manifest.version;
    log(`hydrated Toolchain release from emception@${version} (manifest schema ${schemaVersion})`);
  } finally {
    await fs.rm(tempDir, { recursive: true, force: true });
  }
}

export async function ensureCanonicalRelease({
  srcDir = SOURCE_CANONICAL,
  versionPackagePath = TOOLCHAIN_PACKAGE_JSON,
  hydrate = hydratePublishedRelease,
  log = console.log,
} = {}) {
  const { version } = JSON.parse(await fs.readFile(versionPackagePath, 'utf8'));
  try {
    const manifest = await validateCanonicalRelease(srcDir);
    if (manifest.artifactVersion === version) {
      return { action: 'skip', source: srcDir, version, schemaVersion: 2 };
    }
  } catch {
    try {
      const manifest = await validatePublishedRelease(srcDir, version);
      return {
        action: 'skip',
        source: srcDir,
        version,
        schemaVersion: manifest.schemaVersion ?? manifest.version,
      };
    } catch {
      // A missing or invalid local release is hydrated from the pinned package.
    }
  }

  await hydrate({ srcDir, version, log });
  const manifest = await validatePublishedRelease(srcDir, version);
  return {
    action: 'hydrated',
    source: srcDir,
    version,
    schemaVersion: manifest.schemaVersion ?? manifest.version,
  };
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

export async function syncEmceptionCdn(options = {}) {
  const srcDir = options.srcDir ?? SOURCE_CANONICAL;
  const tgtDir = options.tgtDir ?? TARGET;
  const log = options.log ?? console.log;
  let sourceSchemaVersion = 2;
  if (!Object.hasOwn(options, 'srcDir')) {
    const release = await ensureCanonicalRelease({ srcDir, log });
    sourceSchemaVersion = release.schemaVersion;
  }
  if (sourceSchemaVersion === 1) {
    const { version } = JSON.parse(await fs.readFile(TOOLCHAIN_PACKAGE_JSON, 'utf8'));
    await validatePublishedRelease(srcDir, version);
  } else {
    await validateCanonicalRelease(srcDir);
  }
  if (await releaseTreesMatch(srcDir, tgtDir)) {
    log('emception CDN is up to date (validated Toolchain release tree)');
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
