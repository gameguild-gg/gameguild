import assert from 'node:assert/strict';
import { test } from 'node:test';
import { readFile } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import { CODE_TOOLCHAIN_V1, CODE_TOOLCHAIN_V2, CODE_TOOLCHAIN_V3, CODE_TOOLCHAIN_V2_HISTORY, requireFrozenCodeToolchain } from './toolchain-binding.mjs';

function canonicalLockHash(lock) {
  function sortDeep(value) {
    if (Array.isArray(value)) return value.map(sortDeep);
    if (value && typeof value === 'object')
      return Object.fromEntries(
        Object.entries(value)
          .sort(([left], [right]) => left.localeCompare(right))
          .map(([key, entry]) => [key, sortDeep(entry)]),
      );
    return value;
  }
  return createHash('sha256')
    .update(JSON.stringify(sortDeep(lock), null, 2) + '\n')
    .digest('hex');
}

function manifestFor(identity) {
  return { schemaVersion: 2, ...identity, buildReceiptHash: 'a'.repeat(64), buildFingerprint: 'b'.repeat(64) };
}

test('current adapter version 3 matches the reviewed patched lock, ABI and package', async () => {
  const [lock, config, pkg] = await Promise.all(
    ['toolchain/toolchain.lock.json', 'toolchain/toolchain.config.json', 'packages/toolchain/package.json'].map(async (path) =>
      JSON.parse(await readFile(new URL('../' + path, import.meta.url), 'utf8')),
    ),
  );
  assert.equal(canonicalLockHash(lock), CODE_TOOLCHAIN_V3.toolchainLockHash);
  assert.equal(config.runtimeAbi, CODE_TOOLCHAIN_V3.runtimeAbi);
  assert.equal(pkg.version, CODE_TOOLCHAIN_V3.artifactVersion);
});

test('version 1 retains its original lock and is not rebound to the patched compiler', async () => {
  const lock = JSON.parse(await readFile(new URL('./frozen-toolchains/code-v1-lock.json', import.meta.url), 'utf8'));
  assert.equal(canonicalLockHash(lock), CODE_TOOLCHAIN_V1.toolchainLockHash);
  assert.deepEqual(CODE_TOOLCHAIN_V1, {
    artifactVersion: '4.4.0',
    runtimeAbi: 'emception-browser-v1',
    toolchainLockHash: 'eb7e07aacb067ae32910fbebcb3762902da756b1a7ce64746f14f77a25057720',
  });
  assert.notEqual(CODE_TOOLCHAIN_V1.toolchainLockHash, CODE_TOOLCHAIN_V2.toolchainLockHash);
  assert.notEqual(CODE_TOOLCHAIN_V1.artifactVersion, CODE_TOOLCHAIN_V2.artifactVersion);
});

for (const [version, identity] of [
  [1, CODE_TOOLCHAIN_V1],
  [2, CODE_TOOLCHAIN_V2],
  [3, CODE_TOOLCHAIN_V3],
]) {
  test('version ' + version + ' matches the server adapter source constants', async () => {
    const source = await readFile(
      new URL('../../../apps/api/Source/Modules/GameGuild.Learning.Assessments/Grading/Code/CodeAssessmentTypeAdapter.cs', import.meta.url),
      'utf8',
    );
    for (const value of Object.values(identity)) assert.ok(source.includes('"' + value + '"'));
    assert.doesNotThrow(() => requireFrozenCodeToolchain(identity, manifestFor(identity)));
  });

  for (const [name, patch] of Object.entries({
    legacy: { schemaVersion: undefined },
    changedCompiler: { artifactVersion: '999' },
    changedAbi: { runtimeAbi: 'next' },
    changedLock: { toolchainLockHash: 'c'.repeat(64) },
    missingReceipt: { buildReceiptHash: undefined },
    corruptFingerprint: { buildFingerprint: 'bad' },
  })) {
    test('version ' + version + ' rejects ' + name + ' artifacts without executing learner code', () => {
      assert.throws(() => requireFrozenCodeToolchain(identity, { ...manifestFor(identity), ...patch }));
    });
  }

  test('version ' + version + ' rejects unknown, extended and inherited request bindings', () => {
    assert.throws(() => requireFrozenCodeToolchain({ ...identity, artifactVersion: '999' }, manifestFor(identity)));
    assert.throws(() => requireFrozenCodeToolchain({ ...identity, newest: true }, manifestFor(identity)));
    assert.throws(() => requireFrozenCodeToolchain(Object.assign(Object.create(identity), { a: 1, b: 2, c: 3 }), manifestFor(identity)));
  });
}

test('both directions reject cross-version compiler artifacts', () => {
  assert.throws(() => requireFrozenCodeToolchain(CODE_TOOLCHAIN_V1, manifestFor(CODE_TOOLCHAIN_V2)));
  assert.throws(() => requireFrozenCodeToolchain(CODE_TOOLCHAIN_V2, manifestFor(CODE_TOOLCHAIN_V1)));
});

// A patch release may share compiler bytes while remaining a different frozen identity.
test('version 2 retains its released 4.5.1 identity and reviewed lock', async () => {
  const lock = JSON.parse(await readFile(new URL('./frozen-toolchains/code-v2-lock.json', import.meta.url), 'utf8'));
  assert.equal(canonicalLockHash(lock), CODE_TOOLCHAIN_V2.toolchainLockHash);
  assert.deepEqual(CODE_TOOLCHAIN_V2, {
    artifactVersion: '4.5.1',
    runtimeAbi: 'emception-browser-v1',
    toolchainLockHash: '405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37',
  });
});

test('versions 2 and 3 reject cross-release artifacts despite matching lock and ABI', () => {
  assert.throws(() => requireFrozenCodeToolchain(CODE_TOOLCHAIN_V2, manifestFor(CODE_TOOLCHAIN_V3)));
  assert.throws(() => requireFrozenCodeToolchain(CODE_TOOLCHAIN_V3, manifestFor(CODE_TOOLCHAIN_V2)));
});

test('V2 history retains every exact identity issued by prior releases', () => {
  assert.deepEqual(CODE_TOOLCHAIN_V2_HISTORY.map(identity => identity.artifactVersion), ['4.4.1', '4.5.0', '4.5.1', '4.5.2']);
  for (const identity of CODE_TOOLCHAIN_V2_HISTORY) {
    assert.doesNotThrow(() => requireFrozenCodeToolchain(identity, manifestFor(identity)));
    assert.throws(() => requireFrozenCodeToolchain(identity, { ...manifestFor(identity), toolchainLockHash: 'f'.repeat(64) }));
    assert.throws(() => requireFrozenCodeToolchain({ ...identity, artifactVersion: '999' }, manifestFor(identity)));
  }
});
