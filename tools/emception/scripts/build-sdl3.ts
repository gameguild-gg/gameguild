/**
 * Prepare SDL3 for the in-browser Emscripten toolchain.
 *
 * Strategy: use the emsdk port system (emcc -sUSE_SDL=3) rather than building
 * SDL3 from source.  Running emcc with -sUSE_SDL=3 on a minimal stub triggers
 * the port download+build once and caches the result inside the emsdk tree.
 *
 * This script produces all SDL3 artifacts in one pass:
 *   1. Compiles a minimal stub with -sUSE_SDL=3 -sMODULARIZE
 *      → populates the emsdk cache with libSDL3.a + headers
 *      → produces sdl3-runtime.mjs (the JS MODULARIZE factory used at runtime)
 *   2. Copies libSDL3.a + SDL3 headers from the emsdk cache into our sysroot
 *   3. Copies libSDL3.a to the FROZEN_CACHE path aliased by tool-runner.ts
 *   4. Writes the .emscripten_url port marker so FROZEN_CACHE early-outs work
 *
 * Outputs:
 *   sysroot/usr/lib/libSDL3.a
 *   sysroot/usr/include/SDL3/*.h
 *   sysroot/usr/lib/emscripten/cache-lib/wasm32-emscripten/libSDL3.a
 *   sysroot/usr/lib/emscripten/sdl3-runtime.mjs
 *   sysroot/usr/lib/emscripten_ports/sdl3/.emscripten_url
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import shell from 'shelljs';
import { getEmsdkDir, setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { PINNED } from './lib/pinned-versions.ts';

enableBuildKeepalive('build-sdl3');

const ROOT = process.cwd();
shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || PINNED.EMSDK_VERSION;
setupEmsdk(EMSDK_VERSION);

const EMSDK_DIR = getEmsdkDir();
const EMCC = path.join(EMSDK_DIR, 'upstream', 'emscripten', 'emcc');

const SYSROOT = path.join(ROOT, 'sysroot');
const SYSROOT_LIB = path.join(SYSROOT, 'usr', 'lib');
const SYSROOT_INC = path.join(SYSROOT, 'usr', 'include');
const EMSCRIPTEN_DIR = path.join(SYSROOT_LIB, 'emscripten');
const CACHE_LIB_DIR = path.join(EMSCRIPTEN_DIR, 'cache-lib', 'wasm32-emscripten');

shell.mkdir('-p', SYSROOT_LIB);
shell.mkdir('-p', path.join(SYSROOT_INC, 'SDL3'));
shell.mkdir('-p', EMSCRIPTEN_DIR);
shell.mkdir('-p', CACHE_LIB_DIR);

// ── Step 1: compile stub with -sUSE_SDL=3 / -sMODULARIZE ─────────────────
//
// This single emcc invocation does three things simultaneously:
//   a) triggers the emsdk port system to download + build SDL3 (cached)
//   b) produces the MODULARIZE JS factory (sdl3-runtime.mjs) we ship
//   c) exercises SDL3 APIs needed in wasmImports (main_loop, memory growth…)

const STUB_C = path.join(os.tmpdir(), 'sdl3_port_stub.c');
const TMP_JS = path.join(os.tmpdir(), 'sdl3-runtime.js');
const TMP_WASM = path.join(os.tmpdir(), 'sdl3-runtime.wasm');

fs.writeFileSync(
    STUB_C,
    `
#include <SDL3/SDL.h>
#include <emscripten.h>
#include <stdlib.h>

static SDL_Window   *g_window;
static SDL_GLContext g_glctx;

static void loop_iter(void) {
    SDL_Event e;
    while (SDL_PollEvent(&e)) {
        if (e.type == SDL_EVENT_QUIT) emscripten_cancel_main_loop();
    }
    SDL_GL_SwapWindow(g_window);
}

/* Force emscripten_set_main_loop_arg into wasmImports. */
__attribute__((used)) static void _force_loop_arg(void) {
    emscripten_set_main_loop_arg(NULL, NULL, 0, 0);
}

/* Force emscripten_notify_memory_growth into wasmImports. */
__attribute__((used)) static void _force_mem_growth(void) {
    void *p = malloc(64 * 1024 * 1024);
    if (p) free(p);
}

int main(void) {
    /* Request GLES 3.0 (= WebGL2) so the runtime canvas context is WebGL2.
     * SDL_CreateRenderer(w, NULL) may pick SDL3's WebGPU "gpu" renderer on
     * modern browsers, which never calls emscripten_webgl_make_context_current
     * and leaves GLctx undefined for user WASM OpenGL calls.  Using
     * SDL_GL_CreateContext explicitly guarantees the WebGL2 context is
     * created and made current (GLctx is set) regardless of which SDL3
     * renderer would otherwise be chosen. */
    SDL_GL_SetAttribute(SDL_GL_CONTEXT_PROFILE_MASK, SDL_GL_CONTEXT_PROFILE_ES);
    SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, 3);
    SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, 0);
    if (!SDL_Init(SDL_INIT_VIDEO | SDL_INIT_EVENTS)) return 1;
    g_window = SDL_CreateWindow("sdl3-runtime", 640, 480,
                                SDL_WINDOW_OPENGL | SDL_WINDOW_RESIZABLE);
    g_glctx = SDL_GL_CreateContext(g_window);
    SDL_GL_MakeCurrent(g_window, g_glctx);
    emscripten_set_main_loop(loop_iter, 0, 1);
    SDL_GL_DestroyContext(g_glctx);
    SDL_DestroyWindow(g_window);
    SDL_Quit();
    return 0;
}
`,
);

console.log('Building SDL3 via emsdk port (-sUSE_SDL=3) — downloads SDL3 on first run...');
console.log(`  emcc: ${EMCC}`);

const result = shell.exec(
    [
        `"${EMCC}"`,
        `"${STUB_C}"`,
        '-sUSE_SDL=3',
        '-sENVIRONMENT=web',
        '-sALLOW_MEMORY_GROWTH=1',
        '-sMODULARIZE=1',
        '-sEXPORT_NAME=createSDL3Module',
        '-sEXPORT_ES6=1',
        '-sEXPORTED_RUNTIME_METHODS=ccall,cwrap,getValue,setValue,UTF8ToString,stringToUTF8,lengthBytesUTF8',
        '-sUSE_WEBGL2=1',
        '-sMIN_WEBGL_VERSION=1',
        '-sMAX_WEBGL_VERSION=2',
        '-O2',
        `-o "${TMP_JS}"`,
    ].join(' '),
    { silent: false },
);

if (result.code !== 0) {
    console.error('emcc SDL3 stub compilation failed');
    process.exit(1);
}

// ── Step 2: save sdl3-runtime.mjs ─────────────────────────────────────────
const OUTPUT_MJS = path.join(EMSCRIPTEN_DIR, 'sdl3-runtime.mjs');
const OUTPUT_WASM = path.join(EMSCRIPTEN_DIR, 'sdl3-runtime.wasm');
fs.copyFileSync(TMP_JS, OUTPUT_MJS);
fs.copyFileSync(TMP_WASM, OUTPUT_WASM);
const mjsSize = (fs.statSync(OUTPUT_MJS).size / 1024).toFixed(1);
console.log(`Saved canvas runtime pair (${mjsSize} KB glue) → ${path.relative(ROOT, EMSCRIPTEN_DIR)}`);

// ── Step 3: copy libSDL3.a + headers from the emsdk cache ─────────────────
//
// After emcc -sUSE_SDL=3 completes, SDL3 is compiled and cached at:
//   <EM_CACHE>/sysroot/lib/wasm32-emscripten/libSDL3.a
//   <EM_CACHE>/sysroot/include/SDL3/*.h
//
// EM_CACHE is set by emsdk_env.sh (captured by setupEmsdk()).
// Fall back to the known default location inside the emsdk tree.
const emCache =
    process.env.EM_CACHE ||
    path.join(EMSDK_DIR, 'upstream', 'emscripten', 'cache');

const cacheLibSDL3 = path.join(emCache, 'sysroot', 'lib', 'wasm32-emscripten', 'libSDL3.a');
const cacheIncSDL3 = path.join(emCache, 'sysroot', 'include', 'SDL3');

// Locate libSDL3.a (may be in cache/sysroot or the port build dir)
let libSrc = cacheLibSDL3;
if (!fs.existsSync(libSrc)) {
    const found = shell
        .find(path.join(emCache, 'ports', 'sdl3'))
        .filter((f: string) => f.endsWith('libSDL3.a') || f.endsWith('SDL3.a'));
    if (found.length === 0) {
        throw new Error(
            `libSDL3.a not found in emsdk cache.\n` +
            `  Expected: ${cacheLibSDL3}\n` +
            `  EM_CACHE: ${emCache}`,
        );
    }
    libSrc = found[0];
    console.log(`  libSDL3.a found via port build dir: ${libSrc}`);
}

shell.cp('-f', libSrc, path.join(SYSROOT_LIB, 'libSDL3.a'));
console.log(`Deployed libSDL3.a → sysroot/usr/lib/`);

// Also put libSDL3.a in the FROZEN_CACHE path.
// tool-runner.ts aliases /home/user/.emscripten_cache/sysroot/lib
//                      → /usr/lib/emscripten/cache-lib
// so cache.py resolves libSDL3.a here without triggering cache.lock().
shell.cp('-f', path.join(SYSROOT_LIB, 'libSDL3.a'), CACHE_LIB_DIR);
console.log(`Deployed libSDL3.a → ${path.relative(ROOT, CACHE_LIB_DIR)}/`);

// Copy SDL3 headers
if (fs.existsSync(cacheIncSDL3)) {
    shell.cp('-rf', path.join(cacheIncSDL3, '*.h'), path.join(SYSROOT_INC, 'SDL3', '/'));
    console.log(`Deployed SDL3 headers → sysroot/usr/include/SDL3/`);
} else {
    console.warn(`Warning: SDL3 headers not found in emsdk cache at ${cacheIncSDL3}`);
    console.warn(`SDL3 headers are needed for user programs to compile with -sUSE_SDL=3.`);
}

// ── Step 4: write the .emscripten_url port marker ─────────────────────────
//
// FROZEN_CACHE=True forces cache.py to check this marker before attempting
// a file-system lock (which is forbidden in the browser sandbox).
// tool-runner.ts aliases /home/user/.emscripten_cache/ports
//                      → /usr/lib/emscripten_ports
const sdl3PortPy = path.join(SYSROOT, 'usr', 'lib', 'emscripten', 'tools', 'ports', 'sdl3.py');
if (fs.existsSync(sdl3PortPy)) {
    const src = fs.readFileSync(sdl3PortPy, 'utf-8');
    const m = src.match(/^VERSION\s*=\s*['"]([^'"]+)['"]/m);
    if (m) {
        const portUrl = `https://github.com/libsdl-org/SDL/archive/release-${m[1]}.zip`;
        const portDir = path.join(SYSROOT, 'usr', 'lib', 'emscripten_ports', 'sdl3');
        shell.mkdir('-p', portDir);
        fs.writeFileSync(path.join(portDir, '.emscripten_url'), portUrl);
        console.log(`Wrote port marker for SDL3 ${m[1]} → ${path.relative(ROOT, portDir)}/.emscripten_url`);
    } else {
        console.warn('Warning: could not parse VERSION from sdl3.py — port marker not written.');
        console.warn('  -sUSE_SDL=3 may fail at runtime with FROZEN_CACHE error.');
    }
} else {
    console.warn(`Warning: sdl3.py not found at ${sdl3PortPy} — port marker not written.`);
}

console.log('>>> SDL3 build complete.');
