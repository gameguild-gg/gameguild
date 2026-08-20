import assert from 'node:assert/strict';
import { mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { test } from 'node:test';

import { stageSysroot } from '../stage-sysroot.mjs';

async function fixture() {
  const root = await mkdtemp(path.join(tmpdir(), 'emception-stage-'));
  const source = path.join(root, 'sysroot');
  const target = path.join(root, 'build', 'stage', 'sysroot');
  const receipt = path.join(root, 'build', 'stage', 'sysroot-receipt.json');

  await mkdir(path.join(source, 'usr', 'lib'), { recursive: true });
  await writeFile(path.join(source, 'usr', 'lib', 'clang.mjs'), 'export default 1;');
  await writeFile(path.join(source, 'usr', 'lib', 'clang.wasm'), new Uint8Array([0, 97, 115, 109]));

  return { root, source, target, receipt };
}

test('stageSysroot replaces stale output and records a stable snapshot fingerprint', async (context) => {
  const paths = await fixture();
  context.after(() => rm(paths.root, { recursive: true, force: true }));

  await mkdir(paths.target, { recursive: true });
  await writeFile(path.join(paths.target, 'stale.txt'), 'stale');

  const first = await stageSysroot(paths);
  const stagedGlue = await readFile(path.join(paths.target, 'usr', 'lib', 'clang.mjs'), 'utf8');
  const receipt = JSON.parse(await readFile(paths.receipt, 'utf8'));

  assert.equal(stagedGlue, 'export default 1;');
  await assert.rejects(readFile(path.join(paths.target, 'stale.txt')));
  assert.equal(first.fileCount, 2);
  assert.equal(receipt.schemaVersion, 1);
  assert.equal(receipt.fingerprint, first.fingerprint);

  const second = await stageSysroot(paths);
  assert.equal(second.fingerprint, first.fingerprint);
});

test('stageSysroot rejects an absent or empty working sysroot', async (context) => {
  const paths = await fixture();
  context.after(() => rm(paths.root, { recursive: true, force: true }));

  await rm(paths.source, { recursive: true, force: true });
  await assert.rejects(stageSysroot(paths), /working sysroot does not exist/);

  await mkdir(paths.source, { recursive: true });
  await assert.rejects(stageSysroot(paths), /working sysroot is empty/);
});
