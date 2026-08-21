import assert from 'node:assert/strict';
import { mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { test } from 'node:test';

import { stageCdnPackage } from '../lib/stage-cdn-package.mjs';

test('stageCdnPackage copies only publishable release artifacts', async (context) => {
  const root = await mkdtemp(path.join(tmpdir(), 'emception-package-cdn-'));
  const sourceCdn = path.join(root, 'build', 'cdn');
  const targetCdn = path.join(root, 'packages', 'toolchain', 'cdn');
  context.after(() => rm(root, { recursive: true, force: true }));
  await mkdir(path.join(sourceCdn, 'usr', 'lib'), { recursive: true });
  await mkdir(targetCdn, { recursive: true });
  await writeFile(path.join(sourceCdn, 'manifest.json'), '{"schemaVersion":2}');
  await writeFile(path.join(sourceCdn, 'brotli_wasm.js'), 'export default {};');
  await writeFile(path.join(sourceCdn, 'brotli_wasm.wasm'), 'wasm');
  await writeFile(path.join(sourceCdn, 'usr', 'lib', 'clang.tar.br'), 'bundle');
  await writeFile(path.join(sourceCdn, 'usr', 'lib', 'clang.wasm'), 'raw');
  await writeFile(path.join(targetCdn, 'stale.tar.br'), 'stale');

  const result = await stageCdnPackage({ sourceCdn, targetCdn });

  assert.equal(result.bundleCount, 1);
  assert.equal(await readFile(path.join(targetCdn, 'manifest.json'), 'utf8'), '{"schemaVersion":2}');
  await assert.rejects(readFile(path.join(targetCdn, 'usr', 'lib', 'clang.wasm')));
  await assert.rejects(readFile(path.join(targetCdn, 'stale.tar.br')));
});

test('stageCdnPackage refuses a source without the complete release metadata', async (context) => {
  const root = await mkdtemp(path.join(tmpdir(), 'emception-package-cdn-'));
  const sourceCdn = path.join(root, 'build', 'cdn');
  const targetCdn = path.join(root, 'target');
  context.after(() => rm(root, { recursive: true, force: true }));
  await mkdir(sourceCdn, { recursive: true });
  await writeFile(path.join(sourceCdn, 'manifest.json'), '{}');

  await assert.rejects(stageCdnPackage({ sourceCdn, targetCdn }), /brotli_wasm\.js/);
});

test('toolchain package owns the canonical CDN artifact exports', async () => {
  const packageFile = path.resolve(import.meta.dirname, '../../packages/toolchain/package.json');
  const packageJson = JSON.parse(await readFile(packageFile, 'utf8'));

  assert.equal(packageJson.name, '@gameguild/emception-toolchain');
  assert.deepEqual(packageJson.files, ['cdn', 'README.md']);
  assert.equal(packageJson.exports['./manifest.json'], './cdn/manifest.json');
  assert.equal(packageJson.exports['./cdn/*'], './cdn/*');
});
