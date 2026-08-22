import { createHash } from 'node:crypto';
import { readFile, readdir, stat } from 'node:fs/promises';
import path from 'node:path';

const PACKAGES = [
  ['core', 'emception'],
  ['toolchain', '@gameguild/emception-toolchain'],
  ['browser', '@gameguild/emception-browser'],
  ['xterm', '@gameguild/emception-xterm'],
  ['react', '@gameguild/emception-react'],
  ['webcomponent', '@gameguild/emception-webcomponent'],
  ['ide', '@gameguild/emception-ide'],
];

async function json(filename) {
  return JSON.parse(await readFile(filename, 'utf8'));
}

async function requireFile(filename, label) {
  const metadata = await stat(filename).catch(() => null);
  if (!metadata?.isFile()) throw new Error(`${label} is missing: ${filename}`);
}

async function requireDirectory(filename, label) {
  const metadata = await stat(filename).catch(() => null);
  if (!metadata?.isDirectory()) throw new Error(`${label} is missing: ${filename}`);
}

async function directoryContents(directory) {
  const contents = {};
  async function walk(current) {
    const entries = await readdir(current, { withFileTypes: true });
    entries.sort((left, right) => left.name.localeCompare(right.name));
    for (const entry of entries) {
      const absolute = path.join(current, entry.name);
      if (entry.isDirectory()) await walk(absolute);
      else if (entry.isFile()) {
        const relative = path.relative(directory, absolute).split(path.sep).join('/');
        contents[relative] = createHash('sha256').update(await readFile(absolute)).digest('hex');
      }
    }
  }
  await requireDirectory(directory, 'CDN directory');
  await walk(directory);
  return contents;
}

export async function verifyEmceptionRelease(root = process.cwd()) {
  const manifests = {};
  for (const [directory, expectedName] of PACKAGES) {
    const packageRoot = path.join(root, 'packages', directory);
    const manifest = await json(path.join(packageRoot, 'package.json'));
    if (manifest.name !== expectedName) {
      throw new Error(`Unexpected package in ${directory}: expected ${expectedName}, got ${manifest.name}`);
    }
    manifests[expectedName] = manifest;
    if (directory !== 'toolchain') await requireDirectory(path.join(packageRoot, 'dist'), `${expectedName} dist`);
  }

  const versions = new Set(Object.values(manifests).map((manifest) => manifest.version));
  if (versions.size !== 1) throw new Error(`Emception package versions differ: ${[...versions].join(', ')}`);
  const [version] = versions;

  const canonicalManifest = await json(path.join(root, 'artifacts', 'toolchain', 'release', 'cdn', 'manifest.json'));
  const packageManifest = await json(path.join(root, 'packages', 'toolchain', 'cdn', 'manifest.json'));
  if (canonicalManifest.artifactVersion !== version || packageManifest.artifactVersion !== version) {
    throw new Error(`Toolchain manifest artifactVersion must equal package version ${version}`);
  }
  for (const field of ['toolchainLockHash', 'buildReceiptHash']) {
    if (!/^[a-f0-9]{64}$/.test(packageManifest[field] ?? '')) throw new Error(`Toolchain manifest is missing valid ${field}`);
  }
  if (!packageManifest.sourceProvenance || Object.keys(packageManifest.sourceProvenance).length === 0) {
    throw new Error('Toolchain manifest is missing sourceProvenance');
  }

  const expectedUrl = `https://cdn.jsdelivr.net/npm/@gameguild/emception-toolchain@${version}/cdn/manifest.json`;
  const generatedUrl = await readFile(
    path.join(root, 'packages', 'browser', 'src', 'generated', 'toolchain-manifest-url.ts'),
    'utf8',
  );
  if (!generatedUrl.includes(expectedUrl)) throw new Error(`Browser DEFAULT_MANIFEST_URL must contain ${expectedUrl}`);

  for (const receipt of ['manifest', 'bundles', 'release']) {
    await requireFile(path.join(root, 'artifacts', 'toolchain', 'receipts', `${receipt}.json`), `${receipt} receipt`);
  }

  const toolchainCdn = await directoryContents(path.join(root, 'packages', 'toolchain', 'cdn'));
  const compatibilityCdn = await directoryContents(path.join(root, 'packages', 'core', 'cdn'));
  if (JSON.stringify(toolchainCdn) !== JSON.stringify(compatibilityCdn)) {
    throw new Error('emception compatibility CDN differs from @gameguild/emception-toolchain CDN');
  }

  return { version, packageCount: PACKAGES.length, expectedUrl };
}
