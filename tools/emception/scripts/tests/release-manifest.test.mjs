import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { test } from 'node:test';

import { generateReleaseManifest } from '../lib/release-manifest.mjs';

const EMPTY_WASM_MODULE = new Uint8Array([0, 97, 115, 109, 1, 0, 0, 0]);

async function fixture() {
  const root = await mkdtemp(path.join(tmpdir(), 'emception-manifest-'));
  const sysroot = path.join(root, 'build', 'stage', 'sysroot');
  const outputDir = path.join(root, 'build', 'cdn');
  const manifestFile = path.join(outputDir, 'manifest.json');
  await mkdir(path.join(sysroot, 'usr', 'lib', 'python3.13', 'test'), { recursive: true });
  await mkdir(path.join(sysroot, 'usr', 'lib', 'emscripten'), { recursive: true });
  await writeFile(path.join(sysroot, 'usr', 'lib', 'clang.mjs'), 'export default {};');
  await writeFile(path.join(sysroot, 'usr', 'lib', 'clang.wasm'), EMPTY_WASM_MODULE);
  await writeFile(path.join(sysroot, 'usr', 'lib', 'emscripten', 'sdl3-runtime.mjs'), 'export default {};');
  await writeFile(path.join(sysroot, 'usr', 'lib', 'emscripten', 'sdl3-runtime.wasm'), EMPTY_WASM_MODULE);
  await writeFile(path.join(sysroot, 'usr', 'lib', 'python3.13', 'os.py'), 'name = "posix"');
  await writeFile(path.join(sysroot, 'usr', 'lib', 'python3.13', 'test', 'ignored.py'), 'ignored');
  await writeFile(
    path.join(sysroot, '.emception-symlinks.json'),
    JSON.stringify({ '/usr/bin/clang': '/usr/lib/clang.wasm' }),
  );
  return { root, sysroot, outputDir, manifestFile };
}

test('generateReleaseManifest creates a clean schema-v2 release with wasm profiles', async (context) => {
  const paths = await fixture();
  context.after(() => rm(paths.root, { recursive: true, force: true }));
  await mkdir(paths.outputDir, { recursive: true });
  await writeFile(path.join(paths.outputDir, 'stale.txt'), 'stale');

  const manifest = await generateReleaseManifest({
    ...paths,
    baseUrl: '/cdn',
    artifactVersion: '4.2.0',
    runtimeAbi: 'emception-browser-v1',
    patchSetVersion: 'emception-glue-v3',
    toolVersions: { python: '3.13.3', pythonMajorMinor: '3.13', pythonMajorMinorCompact: '313' },
  });
  const written = JSON.parse(await readFile(paths.manifestFile, 'utf8'));

  assert.equal(manifest.schemaVersion, 2);
  assert.equal(manifest.version, 2);
  assert.equal(manifest.artifactVersion, '4.2.0');
  assert.equal(manifest.runtimeAbi, 'emception-browser-v1');
  assert.equal(manifest.patchSetVersion, 'emception-glue-v3');
  assert.match(manifest.buildFingerprint, /^[a-f0-9]{64}$/);
  assert.deepEqual(manifest.profiles.clang.imports, []);
  assert.deepEqual(manifest.profiles.clang.exports, []);
  assert.equal(manifest.profiles.clang.glue, '/usr/lib/clang.mjs');
  assert.equal(manifest.profiles['sdl3-runtime'].kind, 'canvas-runtime');
  assert.equal(manifest.profiles['sdl3-runtime'].glue, '/usr/lib/emscripten/sdl3-runtime.mjs');
  assert.equal(manifest.profiles['sdl3-runtime'].wasm, '/usr/lib/emscripten/sdl3-runtime.wasm');
  assert.deepEqual(manifest.files['/usr/bin/clang'], { symlink: '/usr/lib/clang.wasm' });
  assert.equal(written.buildFingerprint, manifest.buildFingerprint);
  assert.equal(manifest.files['/.emception-symlinks.json'], undefined);
  await assert.rejects(readFile(path.join(paths.outputDir, 'stale.txt')));
  await assert.rejects(readFile(path.join(paths.outputDir, 'usr', 'lib', 'python3.13', 'test', 'ignored.py')));
});

test('generateReleaseManifest rejects an unpatched mutable sysroot path', async (context) => {
  const paths = await fixture();
  context.after(() => rm(paths.root, { recursive: true, force: true }));

  await assert.rejects(
    generateReleaseManifest({
      ...paths,
      sysroot: path.join(paths.root, 'sysroot'),
      baseUrl: '/cdn',
      artifactVersion: '4.2.0',
      runtimeAbi: 'abi',
      patchSetVersion: 'patch',
      toolVersions: {},
    }),
    /release input must be a staged sysroot/,
  );
});

test('generate-manifest CLI assembles the canonical build paths', async (context) => {
  const paths = await fixture();
  context.after(() => rm(paths.root, { recursive: true, force: true }));
  await writeFile(path.join(paths.root, 'package.json'), '{"version":"9.8.7"}');

  const tsxCli = path.resolve(import.meta.dirname, '../../node_modules/tsx/dist/cli.mjs');
  const manifestScript = path.resolve(import.meta.dirname, '../generate-manifest.ts');
  const result = spawnSync(process.execPath, [tsxCli, manifestScript], {
    cwd: paths.root,
    env: { ...process.env, STAGED_SYSPATH: paths.sysroot },
    encoding: 'utf8',
  });
  const manifest = JSON.parse(await readFile(paths.manifestFile, 'utf8'));

  assert.equal(result.status, 0, result.stderr);
  assert.equal(manifest.artifactVersion, '9.8.7');
  assert.equal(manifest.schemaVersion, 2);
  assert.equal(manifest.profiles.clang.wasm, '/usr/lib/clang.wasm');
});
