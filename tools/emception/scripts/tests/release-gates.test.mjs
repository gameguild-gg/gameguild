import assert from 'node:assert/strict';
import { cp, mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { test } from 'node:test';

const PACKAGE_DIRECTORIES = ['core', 'toolchain', 'browser', 'xterm', 'react', 'webcomponent', 'ide'];
const PACKAGE_NAMES = {
  core: 'emception',
  toolchain: '@gameguild/emception-toolchain',
  browser: '@gameguild/emception-browser',
  xterm: '@gameguild/emception-xterm',
  react: '@gameguild/emception-react',
  webcomponent: '@gameguild/emception-webcomponent',
  ide: '@gameguild/emception-ide',
};

async function fixture(context) {
  const root = await mkdtemp(path.join(tmpdir(), 'emception-release-gates-'));
  context.after(() => rm(root, { recursive: true, force: true }));
  for (const directory of PACKAGE_DIRECTORIES) {
    const packageRoot = path.join(root, 'packages', directory);
    await mkdir(packageRoot, { recursive: true });
    await writeFile(
      path.join(packageRoot, 'package.json'),
      `${JSON.stringify({ name: PACKAGE_NAMES[directory], version: '7.8.9' })}\n`,
    );
    if (directory !== 'toolchain') {
      await mkdir(path.join(packageRoot, 'dist'), { recursive: true });
      await writeFile(path.join(packageRoot, 'dist', 'index.js'), 'export {};');
    }
  }
  const canonical = path.join(root, 'artifacts', 'toolchain', 'release', 'cdn');
  await mkdir(path.join(canonical, 'usr', 'lib'), { recursive: true });
  await writeFile(path.join(canonical, 'manifest.json'), `${JSON.stringify({
    schemaVersion: 2,
    artifactVersion: '7.8.9',
    toolchainLockHash: 'a'.repeat(64),
    buildReceiptHash: 'b'.repeat(64),
    sourceProvenance: { llvm: { version: '23', sha256: 'c'.repeat(64) } },
  })}\n`);
  await writeFile(path.join(canonical, 'brotli_wasm.js'), 'export {};');
  await writeFile(path.join(canonical, 'brotli_wasm.wasm'), 'wasm');
  await writeFile(path.join(canonical, 'usr', 'lib', 'clang.tar.br'), 'bundle');
  await cp(canonical, path.join(root, 'packages', 'toolchain', 'cdn'), { recursive: true });
  await cp(canonical, path.join(root, 'packages', 'core', 'cdn'), { recursive: true });
  await mkdir(path.join(root, 'artifacts', 'toolchain', 'receipts'), { recursive: true });
  for (const receipt of ['manifest', 'bundles', 'release']) {
    await writeFile(path.join(root, 'artifacts', 'toolchain', 'receipts', `${receipt}.json`), '{}\n');
  }
  const generated = path.join(root, 'packages', 'browser', 'src', 'generated');
  await mkdir(generated, { recursive: true });
  await writeFile(
    path.join(generated, 'toolchain-manifest-url.ts'),
    "export const DEFAULT_MANIFEST_URL = 'https://cdn.jsdelivr.net/npm/@gameguild/emception-toolchain@7.8.9/cdn/manifest.json';\n",
  );
  return root;
}

test('release gates enforce version, provenance, receipts, dist, and compatibility CDN invariants', async (context) => {
  const root = await fixture(context);
  const { verifyEmceptionRelease } = await import('../lib/verify-release.mjs');

  const result = await verifyEmceptionRelease(root);
  assert.equal(result.version, '7.8.9');
  assert.equal(result.packageCount, 7);

  await writeFile(path.join(root, 'packages', 'core', 'cdn', 'brotli_wasm.wasm'), 'tampered');
  await assert.rejects(verifyEmceptionRelease(root), /compatibility CDN differs/);

  await cp(
    path.join(root, 'packages', 'toolchain', 'cdn', 'brotli_wasm.wasm'),
    path.join(root, 'packages', 'core', 'cdn', 'brotli_wasm.wasm'),
  );
  const manifestFile = path.join(root, 'packages', 'toolchain', 'cdn', 'manifest.json');
  const manifest = JSON.parse(await readFile(manifestFile, 'utf8'));
  delete manifest.toolchainLockHash;
  await writeFile(manifestFile, JSON.stringify(manifest));
  await assert.rejects(verifyEmceptionRelease(root), /toolchainLockHash/);
});
