import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { test } from 'node:test';

import { applyGluePatches, patchGlueDirectory } from '../lib/glue-patches.mjs';

const SYSTEM_FALLBACK = 'function __emscripten_system(command){if(!command)return 0;return-52}';
const OPENAT = 'path=SYSCALLS.getStr(path);path=SYSCALLS.calculateAt(dirfd,path);var mode=varargs?syscallGetVarargI():0;';

test('applyGluePatches applies required hooks once and is idempotent', () => {
  const source = `var ENV={};${SYSTEM_FALLBACK}${OPENAT}`;

  const first = applyGluePatches(source, 'clang.mjs');
  const second = applyGluePatches(first.content, 'clang.mjs');

  assert.deepEqual(first.applied.sort(), ['env', 'openat', 'system'].sort());
  assert.match(first.content, /moduleArg\["ENV"\]/);
  assert.match(first.content, /systemCallbackSync/);
  assert.match(first.content, /onPreOpen/);
  assert.equal(second.content, first.content);
  assert.deepEqual(second.applied, []);
});

test('applyGluePatches fails when a known system import has an unknown generated shape', () => {
  assert.throws(
    () => applyGluePatches('function __emscripten_system(command){return -99}', 'python.mjs'),
    /unsupported __emscripten_system shape/,
  );
});

test('patchGlueDirectory requires matched wasm and glue files', async (context) => {
  const root = await mkdtemp(path.join(tmpdir(), 'emception-glue-'));
  const libDirectory = path.join(root, 'usr', 'lib');
  context.after(() => rm(root, { recursive: true, force: true }));
  await mkdir(libDirectory, { recursive: true });
  await writeFile(path.join(libDirectory, 'clang.wasm'), new Uint8Array([0, 97, 115, 109]));

  await assert.rejects(
    patchGlueDirectory({ libDirectory, tools: ['clang'] }),
    /clang\.wasm exists without clang\.mjs/,
  );

  await writeFile(path.join(libDirectory, 'clang.mjs'), 'var ENV={};');
  const first = await patchGlueDirectory({ libDirectory, tools: ['clang'] });
  const second = await patchGlueDirectory({ libDirectory, tools: ['clang'] });

  assert.equal(first.foundFiles, 1);
  assert.equal(first.changedFiles, 1);
  assert.equal(second.changedFiles, 0);
  assert.match(await readFile(path.join(libDirectory, 'clang.mjs'), 'utf8'), /moduleArg\["ENV"\]/);
});

test('patch-glue CLI patches only the frozen staged sysroot', async (context) => {
  const root = await mkdtemp(path.join(tmpdir(), 'emception-patch-cli-'));
  const workingLib = path.join(root, 'sysroot', 'usr', 'lib');
  const stagedRoot = path.join(root, 'build', 'stage', 'sysroot');
  const stagedLib = path.join(stagedRoot, 'usr', 'lib');
  const stagedTools = path.join(stagedLib, 'emscripten', 'tools');
  context.after(() => rm(root, { recursive: true, force: true }));

  await mkdir(workingLib, { recursive: true });
  await mkdir(stagedTools, { recursive: true });
  await writeFile(path.join(workingLib, 'clang.mjs'), 'var ENV={};');
  await writeFile(path.join(stagedLib, 'clang.mjs'), 'var ENV={};');
  await writeFile(path.join(stagedLib, 'clang.wasm'), new Uint8Array([0, 97, 115, 109]));
  await writeFile(
    path.join(stagedTools, 'colored_logger.py'),
    'import ctypes\nimport logging\n  kernel32 = ctypes.windll.kernel32\n',
  );

  const tsxCli = path.resolve(import.meta.dirname, '../../node_modules/tsx/dist/cli.mjs');
  const patchScript = path.resolve(import.meta.dirname, '../patch-glue.ts');
  const result = spawnSync(process.execPath, [tsxCli, patchScript], {
    cwd: path.resolve(import.meta.dirname, '../..'),
    env: { ...process.env, STAGED_SYSPATH: stagedRoot },
    encoding: 'utf8',
  });

  assert.equal(result.status, 0, result.stderr);
  assert.equal(await readFile(path.join(workingLib, 'clang.mjs'), 'utf8'), 'var ENV={};');
  assert.match(await readFile(path.join(stagedLib, 'clang.mjs'), 'utf8'), /moduleArg\["ENV"\]/);
  assert.match(await readFile(path.join(stagedTools, 'colored_logger.py'), 'utf8'), /except ImportError/);
});
