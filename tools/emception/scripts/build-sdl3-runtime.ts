/**
 * Build the pre-generated SDL3 JS runtime shell (sdl3-runtime.js).
 *
 * The SDL3 runtime shell is a MODULARIZE ES6 emscripten factory produced by
 * compiling a minimal stub C program against the SDL3 port.  It contains all
 * WebGL, HTML5, and emscripten runtime bindings — roughly 300 KB of JS.
 *
 * End users compile their C programs to main.wasm (no JS generation), then
 * we instantiate that WASM binary through this pre-built factory at runtime,
 * passing { wasmBinary, canvas } as moduleArg.
 *
 * Output: sysroot/usr/lib/emscripten/sdl3-runtime.mjs
 *         (served from CDN as part of the emscripten-core bundle)
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import shell from 'shelljs';
import { getEmsdkDir, setupEmsdk } from './lib/emsdk.ts';

const ROOT = process.cwd();
shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || 'latest';
setupEmsdk(EMSDK_VERSION);

const EMSDK_DIR = getEmsdkDir();
const EMCC = path.join(EMSDK_DIR, 'upstream', 'emscripten', 'emcc');
const SYSROOT = path.join(ROOT, 'sysroot');
const OUT_DIR = path.join(ROOT, 'sysroot', 'usr', 'lib', 'emscripten');
const OUTPUT_JS = path.join(OUT_DIR, 'sdl3-runtime.mjs');
const STUB_C = path.join(os.tmpdir(), 'sdl3_runtime_stub.c');

shell.mkdir('-p', OUT_DIR);

// Minimal SDL3 stub that exercises the renderer + event loop, ensuring all
// necessary import bindings are compiled into the runtime.
// IMPORTANT: use emscripten_set_main_loop (not a raw while loop) so that
// the DCE-based wasmImports generation includes emscripten_set_main_loop and
// emscripten_set_main_loop_arg — without these the user's WASM will fail to
// instantiate with "function import requires a callable".
fs.writeFileSync(
    STUB_C,
    `
#include <SDL3/SDL.h>
#include <emscripten.h>
#include <stdlib.h>

static SDL_Renderer *g_renderer;

static void loop_iter(void) {
    SDL_Event e;
    while (SDL_PollEvent(&e)) {
        if (e.type == SDL_EVENT_QUIT) emscripten_cancel_main_loop();
    }
    SDL_SetRenderDrawColor(g_renderer, 30, 30, 30, 255);
    SDL_RenderClear(g_renderer);
    SDL_RenderPresent(g_renderer);
}

/* Never called — forces emscripten_set_main_loop_arg into wasmImports. */
__attribute__((used)) static void _force_loop_arg(void) {
    emscripten_set_main_loop_arg(NULL, NULL, 0, 0);
}

/* Force emscripten_notify_memory_growth into wasmImports.
 * DCE drops the binding when the stub never grows memory; allocating a
 * large block ensures the ALLOW_MEMORY_GROWTH path (and its JS callback)
 * is compiled in.  The block is immediately freed so it doesn't persist. */
__attribute__((used)) static void _force_mem_growth(void) {
    void *p = malloc(64 * 1024 * 1024);
    if (p) free(p);
}

int main(void) {
    if (!SDL_Init(SDL_INIT_VIDEO | SDL_INIT_EVENTS)) return 1;
    SDL_Window *w = SDL_CreateWindow("sdl3-runtime", 640, 480,
                                     SDL_WINDOW_OPENGL | SDL_WINDOW_RESIZABLE);
    g_renderer = SDL_CreateRenderer(w, NULL);
    emscripten_set_main_loop(loop_iter, 0, 1);
    SDL_DestroyRenderer(g_renderer);
    SDL_DestroyWindow(w);
    SDL_Quit();
    return 0;
}
`,
);

console.log('Building SDL3 runtime shell...');
console.log(`  emcc: ${EMCC}`);
console.log(`  output: ${OUTPUT_JS}`);

const tmpJs = path.join(os.tmpdir(), 'sdl3-runtime.js');
const tmpWasm = path.join(os.tmpdir(), 'sdl3-runtime.wasm');

const result = shell.exec(
    [
        `"${EMCC}"`,
        `"${STUB_C}"`,
        '-sUSE_SDL=3',
        `-I"${path.join(SYSROOT, 'usr', 'include')}"`,
        '-sENVIRONMENT=web',
        '-sALLOW_MEMORY_GROWTH=1',
        '-sMODULARIZE=1',
        '-sEXPORT_NAME=createSDL3Module',
        '-sEXPORT_ES6=1',
        '-sEXPORTED_RUNTIME_METHODS=ccall,cwrap,getValue,setValue,UTF8ToString,stringToUTF8,lengthBytesUTF8',
        // Keep reasonably small; users can strip further.
        '-O2',
        `-o "${tmpJs}"`,
    ].join(' '),
    { silent: false },
);

if (result.code !== 0) {
    console.error('emcc failed to build SDL3 runtime shell');
    process.exit(1);
}

// The WASM that came out of the stub is not bundled — only the JS glue is.
// The user's program WASM provides the actual logic.
// Copy only the JS (converted to .mjs) to the sysroot.
fs.copyFileSync(tmpJs, OUTPUT_JS);

// Clean up the stub WASM — it is NOT deployed (users supply their own WASM).
if (fs.existsSync(tmpWasm)) fs.rmSync(tmpWasm);

const size = (fs.statSync(OUTPUT_JS).size / 1024).toFixed(1);
console.log(`SDL3 runtime shell built: ${OUTPUT_JS} (${size} KB)`);
