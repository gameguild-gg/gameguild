import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { test } from 'node:test';

import {
  applyCanvasRuntimePatches,
  applyGluePatches,
  patchGlueDirectory,
} from '../lib/glue-patches.mjs';

const SYSTEM_FALLBACK = 'function __emscripten_system(command){if(!command)return 0;return-52}';
const OPENAT = 'path=SYSCALLS.getStr(path);path=SYSCALLS.calculateAt(dirfd,path);var mode=varargs?syscallGetVarargI():0;';
const CMAKE_PIPE_POLL_LEGACY = 'poll(stream,timeout,notifyCallback){var pipe=stream.node.pipe;if((stream.flags&2097155)===1){return 256|4}for(var bucket of pipe.buckets){if(bucket.offset-bucket.roffset>0){return 64|1}}return 0}';
const CMAKE_SYSCALL_POLL_LEGACY = 'if(stream.stream_ops.poll){flags=stream.stream_ops.poll(stream,-1)}else{flags=5}';
const CMAKE_PIPE_POLL_ASYNC = 'if(notifyCallback)pipe.registerReadableHandler(notifyCallback);return 0';
const CMAKE_SYSCALL_POLL_ASYNC = 'if(isAsyncContext&&timeout){flags=stream.stream_ops.poll(stream,timeout,makeNotifyCallback(stream,pollfd))}else flags=stream.stream_ops.poll(stream,-1)';
const CANVAS_COMMON = [
  'var wasmBinary;var ABORT=false',
  'instantiateAsync(binary,binaryFile,imports){if(!binary){try{var response=fetch(',
  'var callUserCallback=func=>{if(ABORT){return}try{return func()}catch(e){handleException(e)}finally{maybeExit()}}',
  'var handleException=e=>{if(e instanceof ExitStatus||e=="unwind"){return EXITSTATUS}quit_(1,e)}',
].join(';');
const SDL_RUNTIME = [
  CANVAS_COMMON,
  'var _main,_SDL_free,',
  '_malloc=wasmExports["malloc"]',
  '_SDL_free=Module["_SDL_free"]=wasmExports["SDL_free"]',
  'var stringToNewUTF8=str=>{var size=lengthBytesUTF8(str)+1;var ret=_malloc(size)',
  'var runEmAsmFunction=(code,sigPtr,argbuf)=>{var args=readEmAsmArgs(sigPtr,argbuf);return ASM_CONSTS[code](...args)}',
  'var runMainThreadEmAsm=(emAsmAddr,sigPtr,argbuf,sync)=>{var args=readEmAsmArgs(sigPtr,argbuf);return ASM_CONSTS[emAsmAddr](...args)}',
  'var keyEventHandlerFunc=e=>{var keyEventData=JSEvents.keyEvent',
].join(';');

test('applyCanvasRuntimePatches freezes runtime-only fixes into release glue', () => {
  const first = applyCanvasRuntimePatches(SDL_RUNTIME, 'sdl3-runtime.mjs');
  const second = applyCanvasRuntimePatches(first.content, 'sdl3-runtime.mjs');

  assert.match(first.content, /wasmBinary=Module\["wasmBinary"\]/);
  assert.match(first.content, /if\(binary\)\{return WebAssembly\.instantiate/);
  assert.match(first.content, /e\.target!==Module\["canvas"\]/);
  assert.match(first.content, /ASM_CONSTS\[code\]=eval/);
  assert.match(first.content, /e instanceof WebAssembly\.RuntimeError/);
  assert.ok(first.applied.length >= 8);
  assert.equal(second.content, first.content);
  assert.deepEqual(second.applied, []);
});

test('applyCanvasRuntimePatches rejects an unknown generated runtime shape', () => {
  assert.throws(
    () => applyCanvasRuntimePatches('export default function runtime() {}', 'raylib-runtime.mjs'),
    /unsupported raylib-runtime\.mjs generated shape/,
  );
});

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

test('applyGluePatches accepts the current async CMake pipe poll ABI', () => {
  const source = `${CMAKE_PIPE_POLL_ASYNC}${CMAKE_SYSCALL_POLL_ASYNC}`;
  const result = applyGluePatches(source, 'cmake.mjs');

  assert.equal(result.content, source);
  assert.deepEqual(result.applied, []);
});

test('applyGluePatches upgrades the legacy CMake pipe poll ABI once', () => {
  const source = `${CMAKE_PIPE_POLL_LEGACY}${CMAKE_SYSCALL_POLL_LEGACY}`;
  const first = applyGluePatches(source, 'cmake.mjs');
  const second = applyGluePatches(first.content, 'cmake.mjs');

  assert.deepEqual(first.applied, ['pipefs-pollhup', 'syscall-poll-pipe-hup']);
  assert.match(first.content, /pipe\.refcnt<=1/);
  assert.equal(second.content, first.content);
  assert.deepEqual(second.applied, []);
});

test('applyGluePatches rejects an unknown CMake pipe poll ABI', () => {
  assert.throws(
    () => applyGluePatches('var PIPEFS={};function ___syscall_poll(){}', 'cmake.mjs'),
    /unsupported pipe poll shape/,
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
  const stagedRuntimes = path.join(stagedLib, 'emscripten');
  const stagedTools = path.join(stagedLib, 'emscripten', 'tools');
  context.after(() => rm(root, { recursive: true, force: true }));

  await mkdir(workingLib, { recursive: true });
  await mkdir(stagedTools, { recursive: true });
  await writeFile(path.join(workingLib, 'clang.mjs'), 'var ENV={};');
  await writeFile(path.join(stagedLib, 'clang.mjs'), 'var ENV={};');
  await writeFile(path.join(stagedLib, 'clang.wasm'), new Uint8Array([0, 97, 115, 109]));
  await writeFile(path.join(stagedRuntimes, 'sdl3-runtime.mjs'), SDL_RUNTIME);
  await writeFile(path.join(stagedRuntimes, 'raylib-runtime.mjs'), CANVAS_COMMON);
  await writeFile(path.join(stagedRuntimes, 'allegro-runtime.mjs'), CANVAS_COMMON);
  await writeFile(path.join(stagedRuntimes, 'sdl3-runtime.wasm'), new Uint8Array([0, 97, 115, 109]));
  await writeFile(path.join(stagedRuntimes, 'raylib-runtime.wasm'), new Uint8Array([0, 97, 115, 109]));
  await writeFile(path.join(stagedRuntimes, 'allegro-runtime.wasm'), new Uint8Array([0, 97, 115, 109]));
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
  assert.match(await readFile(path.join(stagedRuntimes, 'sdl3-runtime.mjs'), 'utf8'), /e\.target!==Module\["canvas"\]/);
  assert.match(await readFile(path.join(stagedTools, 'colored_logger.py'), 'utf8'), /except ImportError/);
});
