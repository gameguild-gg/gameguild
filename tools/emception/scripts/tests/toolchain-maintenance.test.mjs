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

test('updating EMSDK replaces the complete derived component group without mutating the input lock', async () => {
  const { planToolchainUpdate } = await import('../toolchain/sources.ts');
  const config = {
    schemaVersion: 1,
    runtimeAbi: 'emception-browser-v1',
    constraints: { cmake: '<4' },
    emsdkGroup: ['llvm', 'binaryen', 'python', 'sdl3'],
  };
  const source = (name, version, derivedFrom) => ({
    version,
    ...(derivedFrom ? { derivedFrom } : {}),
    source: {
      kind: 'emsdk-component',
      emsdkVersion: version,
      revision: `${name}-${version}`,
      contentHash: name[0].repeat(64),
    },
  });
  const original = {
    schemaVersion: 1,
    configHash: 'c'.repeat(64),
    tools: {
      emsdk: source('emsdk', '5.0.7'),
      llvm: source('llvm', '23.1.0', 'emsdk'),
      binaryen: source('binaryen', '129', 'emsdk'),
      python: source('python', '3.13.3', 'emsdk'),
      sdl3: source('sdl3', 'emsdk-5.0.7', 'emsdk'),
      cmake: {
        version: '3.31.12',
        source: { kind: 'archive', url: 'https://example.invalid/cmake', sha256: 'd'.repeat(64) },
      },
    },
  };
  const before = JSON.stringify(original);
  const provider = {
    async resolve(name, requested) {
      assert.equal(name, 'emsdk');
      assert.equal(requested, 'latest');
      return source('emsdk', '6.0.5');
    },
    async inspectEmsdk() {
      return {
        llvm: source('llvm', '24.0.0', 'emsdk'),
        binaryen: source('binaryen', '130', 'emsdk'),
        python: source('python', '3.14.0', 'emsdk'),
        sdl3: source('sdl3', 'emsdk-6.0.5', 'emsdk'),
      };
    },
    async latestVersion() {
      return '6.0.5';
    },
  };

  const updated = await planToolchainUpdate(config, original, 'emsdk', 'latest', provider);

  assert.equal(JSON.stringify(original), before);
  assert.equal(updated.tools.emsdk.version, '6.0.5');
  assert.equal(updated.tools.llvm.version, '24.0.0');
  assert.equal(updated.tools.binaryen.version, '130');
  assert.equal(updated.tools.python.version, '3.14.0');
  assert.equal(updated.tools.sdl3.version, 'emsdk-6.0.5');
  assert.equal(updated.tools.cmake.version, '3.31.12');
});

test('outdated is read-only and derived tools must be updated through EMSDK', async () => {
  const { findOutdatedTools, planToolchainUpdate } = await import('../toolchain/sources.ts');
  const config = {
    schemaVersion: 1,
    runtimeAbi: 'emception-browser-v1',
    constraints: { cmake: '<4' },
    emsdkGroup: ['llvm'],
  };
  const lock = {
    schemaVersion: 1,
    configHash: 'e'.repeat(64),
    tools: {
      emsdk: {
        version: '5.0.7',
        source: { kind: 'archive', url: 'https://example.invalid/emsdk', sha256: 'a'.repeat(64) },
      },
      llvm: {
        version: '23.1.0',
        derivedFrom: 'emsdk',
        source: { kind: 'archive', url: 'https://example.invalid/llvm', sha256: 'b'.repeat(64) },
      },
      cmake: {
        version: '3.31.12',
        source: { kind: 'archive', url: 'https://example.invalid/cmake', sha256: 'c'.repeat(64) },
      },
    },
  };
  const before = JSON.stringify(lock);
  const provider = {
    async latestVersion(name) {
      return name === 'cmake' ? '3.31.13' : '5.0.7';
    },
    async resolve() {
      throw new Error('not used');
    },
    async inspectEmsdk() {
      throw new Error('not used');
    },
  };

  assert.deepEqual(await findOutdatedTools(config, lock, provider), [
    { name: 'cmake', current: '3.31.12', latest: '3.31.13' },
  ]);
  assert.equal(JSON.stringify(lock), before);
  await assert.rejects(planToolchainUpdate(config, lock, 'llvm', '24.0.0', provider), /controlled by emsdk/);
});

test('clean scopes remove only generated toolchain state', async (context) => {
  const { cleanToolchain } = await import('../toolchain/clean.ts');
  const root = await temporaryRoot(context);
  const cacheMarker = path.join(root, '.cache', 'toolchain', 'sources', 'source.txt');
  const artifactMarker = path.join(root, 'artifacts', 'toolchain', 'tools', 'clang.wasm');
  const dependencyMarker = path.join(root, 'node_modules', 'keep.txt');
  const overlayMarker = path.join(root, 'toolchain', 'overlays', 'cpython', 'patches', 'keep.patch');
  for (const marker of [cacheMarker, artifactMarker, dependencyMarker, overlayMarker]) {
    await mkdir(path.dirname(marker), { recursive: true });
    await writeFile(marker, 'keep');
  }
  const exists = async (filename) => readFile(filename, 'utf8').then(() => true, () => false);

  await cleanToolchain(root, 'artifacts');
  assert.equal(await exists(artifactMarker), false);
  assert.equal(await exists(cacheMarker), true);
  assert.equal(await exists(dependencyMarker), true);
  assert.equal(await exists(overlayMarker), true);

  await mkdir(path.dirname(artifactMarker), { recursive: true });
  await writeFile(artifactMarker, 'again');
  await cleanToolchain(root, 'cache');
  assert.equal(await exists(cacheMarker), false);
  assert.equal(await exists(artifactMarker), true);

  await cleanToolchain(root, 'all');
  assert.equal(await exists(artifactMarker), false);
  assert.equal(await exists(dependencyMarker), true);
  assert.equal(await exists(overlayMarker), true);
});
