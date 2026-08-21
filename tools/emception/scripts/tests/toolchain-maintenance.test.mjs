import assert from 'node:assert/strict';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import test from 'node:test';

async function temporaryRoot(context) {
  const root = await import('node:fs/promises').then(({ mkdtemp }) =>
    mkdtemp(path.join(tmpdir(), 'emception-toolchain-')),
  );
  context.after(async () => {
    const { rm } = await import('node:fs/promises');
    await rm(root, { recursive: true, force: true });
  });
  await mkdir(path.join(root, 'toolchain'), { recursive: true });
  return root;
}

test('toolchain paths separate tracked policy, disposable cache, and release artifacts', async () => {
  const { toolchainPaths } = await import('../toolchain/paths.ts');
  const root = path.resolve('/workspace/emception');

  assert.deepEqual(toolchainPaths(root), {
    root,
    configFile: path.join(root, 'toolchain', 'toolchain.config.json'),
    lockFile: path.join(root, 'toolchain', 'toolchain.lock.json'),
    overlays: path.join(root, 'toolchain', 'overlays'),
    cache: path.join(root, '.cache', 'toolchain'),
    downloads: path.join(root, '.cache', 'toolchain', 'downloads'),
    sources: path.join(root, '.cache', 'toolchain', 'sources'),
    builds: path.join(root, '.cache', 'toolchain', 'builds'),
    emsdk: path.join(root, '.cache', 'toolchain', 'emsdk'),
    artifacts: path.join(root, 'artifacts', 'toolchain'),
    tools: path.join(root, 'artifacts', 'toolchain', 'tools'),
    sysroot: path.join(root, 'artifacts', 'toolchain', 'sysroot'),
    stagedSysroot: path.join(root, 'artifacts', 'toolchain', 'stage', 'sysroot'),
    receipts: path.join(root, 'artifacts', 'toolchain', 'receipts'),
    releaseCdn: path.join(root, 'artifacts', 'toolchain', 'release', 'cdn'),
    manifestFile: path.join(root, 'artifacts', 'toolchain', 'release', 'cdn', 'manifest.json'),
    publicCdn: path.join(root, 'public', 'cdn'),
    packageCdn: path.join(root, 'packages', 'toolchain', 'cdn'),
    compatibilityCdn: path.join(root, 'packages', 'core', 'cdn'),
  });
});

test('toolchain lock serialization is stable and validates the CMake major policy', async (context) => {
  const root = await temporaryRoot(context);
  const { calculateConfigHash, loadToolchainState, serializeToolchainLock } = await import('../toolchain/lock.ts');
  const config = {
    schemaVersion: 1,
    runtimeAbi: 'emception-browser-v1',
    constraints: { cmake: '<4' },
    emsdkGroup: [],
  };
  const configHash = calculateConfigHash(config);
  const lock = {
    schemaVersion: 1,
    configHash,
    tools: {
      emsdk: {
        version: '5.0.7',
        source: {
          kind: 'git-archive',
          repository: 'emscripten-core/emsdk',
          commit: '0123456789abcdef0123456789abcdef01234567',
          url: 'https://codeload.github.com/emscripten-core/emsdk/tar.gz/0123456789abcdef0123456789abcdef01234567',
          sha256: 'a'.repeat(64),
        },
      },
      cmake: {
        version: '3.31.12',
        source: {
          kind: 'archive',
          url: 'https://example.invalid/cmake.tar.gz',
          sha256: 'b'.repeat(64),
        },
      },
    },
  };

  const first = serializeToolchainLock(lock);
  const second = serializeToolchainLock({ ...lock, tools: { cmake: lock.tools.cmake, emsdk: lock.tools.emsdk } });
  assert.equal(first, second);
  assert.equal(first.includes('generatedAt'), false);

  await writeFile(path.join(root, 'toolchain', 'toolchain.config.json'), `${JSON.stringify(config, null, 2)}\n`);
  await writeFile(path.join(root, 'toolchain', 'toolchain.lock.json'), first);
  const state = await loadToolchainState(root);
  assert.equal(state.lock.configHash, configHash);
  assert.equal(state.lock.tools.cmake.version, '3.31.12');

  const invalid = JSON.parse(first);
  invalid.tools.cmake.version = '4.0.0';
  await writeFile(path.join(root, 'toolchain', 'toolchain.lock.json'), `${JSON.stringify(invalid, null, 2)}\n`);
  await assert.rejects(loadToolchainState(root), /CMake 4\.0\.0 violates configured constraint <4/);
});

test('checked-in toolchain policy and lock agree without dynamic timestamps', async () => {
  const { loadToolchainState } = await import('../toolchain/lock.ts');
  const root = path.resolve(import.meta.dirname, '..', '..');
  const state = await loadToolchainState(root);
  const source = await readFile(path.join(root, 'toolchain', 'toolchain.lock.json'), 'utf8');

  assert.equal(state.config.runtimeAbi, 'emception-browser-v1');
  assert.equal(state.lock.tools.emsdk.version, '5.0.7');
  assert.equal(state.lock.tools.cmake.version, '3.31.12');
  assert.equal(source.includes('generatedAt'), false);
});
