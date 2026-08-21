import assert from 'node:assert/strict';
import { test } from 'node:test';

import { ToolchainPreset } from 'emception';
import { createCanvasAPI } from '../dist/canvas.js';

test('canvas.build owns the preset, runtime glue, and wasm artifact paths', async () => {
  const calls = [];
  const files = new Map([
    ['/workspace/main.wasm', new Uint8Array([0, 97, 115, 109])],
    ['/usr/lib/emscripten/sdl3-runtime.mjs', new TextEncoder().encode('export default function () {}')],
  ]);
  const api = {
    workspace: {
      readFile: async (path) => files.get(path) ?? null,
    },
    run: async (tool, argv, options) => {
      calls.push({ tool, argv, options });
      return { exitCode: 0, stdout: '', stderr: '', durationMs: 1, timedOut: false };
    },
  };

  const canvas = createCanvasAPI(api);
  const result = await canvas.build({
    toolchain: ToolchainPreset.SDL_CPP,
    sourcePath: '/workspace/main.cpp',
    cwd: '/workspace',
  });

  assert.equal(calls.length, 2);
  assert.equal(calls[0].tool, 'clang');
  assert.equal(calls[1].tool, 'wasm-ld');
  assert.deepEqual(calls[0].options.preloadBundles, ['llvm', 'sdl3', 'imgui']);
  assert.equal(result.runtimeProfile, 'sdl3-runtime');
  assert.equal(result.runtimePath, '/usr/lib/emscripten/sdl3-runtime.mjs');
  assert.equal(result.wasmPath, '/workspace/main.wasm');
  assert.deepEqual(result.wasm, files.get('/workspace/main.wasm'));
});

test('canvas.build stops before linking after a compiler failure', async () => {
  let calls = 0;
  const api = {
    workspace: { readFile: async () => null },
    run: async () => {
      calls += 1;
      return { exitCode: 2, stdout: '', stderr: 'compile failed', durationMs: 1, timedOut: false };
    },
  };

  const result = await createCanvasAPI(api).build({
    toolchain: ToolchainPreset.Raylib_C,
    sourcePath: '/workspace/main.c',
  });

  assert.equal(calls, 1);
  assert.equal(result.phase, 'compile');
  assert.equal(result.compile.exitCode, 2);
});
