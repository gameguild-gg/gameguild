/**
 * Build Allegro 5 as static libraries for Emscripten.
 *
 * Strategy: download the liballeg/allegro5 tarball, configure with
 * `emcmake cmake -DPLATFORM=Emscripten -DSHARED=off -DALLEGRO_SDL=on`,
 * build core + addons, deploy archives + headers, then emit a MODULARIZE
 * runtime mjs (mirrors build-raylib.ts) so the browser API can drive it as a
 * canvas runtime.
 *
 * Backend: Allegro 5 upstream removed the native HTML5 backend; current
 * releases require `-DALLEGRO_SDL=on` for Emscripten. SDL2 is provided by
 * emsdk's port system (`-sUSE_SDL=2`); we materialize `libSDL2.a` via
 * `embuilder build sdl2` and copy it into the sysroot so the browser-side
 * wasm-ld step can link against it.
 *
 * Outputs:
 *   - sysroot/usr/lib/liballegro.a
 *   - sysroot/usr/lib/liballegro_image.a
 *   - sysroot/usr/lib/liballegro_primitives.a
 *   - sysroot/usr/lib/liballegro_font.a
 *   - sysroot/usr/lib/liballegro_ttf.a
 *   - sysroot/usr/lib/liballegro_audio.a
 *   - sysroot/usr/lib/liballegro_acodec.a
 *   - sysroot/usr/lib/liballegro_color.a
 *   - sysroot/usr/lib/liballegro_main.a
 *   - sysroot/usr/lib/libSDL2.a                       (copied from emsdk cache)
 *   - sysroot/usr/include/allegro5/**\/*.h
 *   - sysroot/usr/lib/emscripten/allegro-runtime.mjs  (MODULARIZE=1 JS factory)
 *
 * Version: pinned default (override via ALLEGRO_VERSION env var).
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import shell from 'shelljs';
import { buildCanvasRuntimePair } from './lib/canvas-runtime-build.ts';
import { getEmsdkDir, setupEmsdk } from './lib/emsdk.ts';
import { ensureGitHubSource } from './lib/github-source.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { PINNED } from './lib/pinned-versions.ts';

enableBuildKeepalive('build-allegro');

const ROOT = process.cwd();
shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || PINNED.EMSDK_VERSION;
setupEmsdk(EMSDK_VERSION);

const EMSDK_DIR = getEmsdkDir();
const EMCC = path.join(EMSDK_DIR, 'upstream', 'emscripten', 'emcc');

const USERLAND_DIR = path.join(ROOT, 'userland', 'allegro');
const BUILD_DIR = path.join(ROOT, 'build', 'allegro');
const SYSROOT_LIB = path.join(ROOT, 'sysroot', 'usr', 'lib');
const SYSROOT_INC = path.join(ROOT, 'sysroot', 'usr', 'include');
const ALLEGRO_INC = path.join(SYSROOT_INC, 'allegro5');
const CONCURRENCY = os.cpus().length;

shell.mkdir('-p', USERLAND_DIR);
shell.mkdir('-p', BUILD_DIR);
shell.mkdir('-p', SYSROOT_LIB);
shell.mkdir('-p', ALLEGRO_INC);

// ─────────────── 1. Allegro 5 via CMake ───────────────

const ALLEGRO_TAG = process.env.ALLEGRO_VERSION ?? PINNED.ALLEGRO_VERSION;
const ALLEGRO_SRC = ensureGitHubSource({
    repository: 'liballeg/allegro5',
    version: ALLEGRO_TAG,
    destination: path.join(USERLAND_DIR, `allegro-${ALLEGRO_TAG}`),
});

const ALLEGRO_BUILD = path.join(BUILD_DIR, 'allegro-build');
if (fs.existsSync(ALLEGRO_BUILD)) shell.rm('-rf', ALLEGRO_BUILD);
shell.mkdir('-p', ALLEGRO_BUILD);

console.log('Configuring Allegro 5 (PLATFORM=Emscripten, ALLEGRO_SDL=on)...');
// Allegro upstream now mandates the SDL backend for Emscripten. We pass
// `-sUSE_SDL=2` via CMAKE_C/CXX_FLAGS so emcc-as-compiler injects the SDL2
// headers + flags during configure-time checks and the build itself.
// Allegro's CMake uses find_package(SDL2) which doesn't know about emsdk's
// port system, so we point SDL2_INCLUDE_DIR / SDL2_LIBRARY at the cache.
const SDL2_INC = path.join(EMSDK_DIR, 'upstream', 'emscripten', 'cache', 'sysroot', 'include', 'SDL2');
const SDL2_LIB = path.join(EMSDK_DIR, 'upstream', 'emscripten', 'cache', 'sysroot', 'lib', 'wasm32-emscripten', 'libSDL2.a');
// CMAKE_*_FLAGS gets `-I${SDL2_INC}` explicitly: emcc's `-sUSE_SDL=2` is a
// link-time port directive and does not always inject the include path at
// compile time, so Allegro core's `#include <SDL.h>` fails without it.
const COMPILER_FLAGS = `-sUSE_SDL=2 -I${SDL2_INC}`;
const allegroCmakeCmd = [
    'emcmake cmake',
    `-S "${ALLEGRO_SRC}"`,
    `-B "${ALLEGRO_BUILD}"`,
    '-DCMAKE_BUILD_TYPE=Release',
    '-DCMAKE_POLICY_VERSION_MINIMUM=3.5',
    '-DSHARED=off',
    '-DWANT_DEMO=off',
    '-DWANT_EXAMPLES=off',
    '-DWANT_TESTS=off',
    '-DWANT_DOCS=off',
    '-DALLEGRO_SDL=on',
    '-DWANT_ALLOW_SSE=off',
    `"-DCMAKE_C_FLAGS=${COMPILER_FLAGS}"`,
    `"-DCMAKE_CXX_FLAGS=${COMPILER_FLAGS}"`,
    `-DSDL2_INCLUDE_DIR="${SDL2_INC}"`,
    `-DSDL2_LIBRARY="${SDL2_LIB}"`,
    // Enabled addons (build/v1 scope; native_dialog/video/physfs intentionally
    // excluded). Note: acodec is auto-built with WANT_AUDIO and has no flag.
    '-DWANT_IMAGE=on',
    '-DWANT_PRIMITIVES=on',
    '-DWANT_FONT=on',
    '-DWANT_TTF=on',
    '-DWANT_AUDIO=on',
    '-DWANT_OPENAL=off',
    '-DWANT_COLOR=on',
    '-DWANT_MEMFILE=on',
    '-DWANT_NATIVE_DIALOG=off',
    '-DWANT_VIDEO=off',
    '-DWANT_PHYSFS=off',
].join(' ');
shell.exec(allegroCmakeCmd);

console.log('Building Allegro 5...');
shell.exec(`cmake --build "${ALLEGRO_BUILD}" --parallel ${CONCURRENCY}`);

// Allegro's CMake puts static archives in build/lib/ with `_static` suffix.
// Map { sysrootName: candidate basenames } and pick the first that exists.
type ArchiveSpec = {
    readonly sysrootName: string;
    readonly candidates: readonly string[];
    readonly required: boolean;
};

const archives: readonly ArchiveSpec[] = [
    { sysrootName: 'liballegro.a', candidates: ['liballegro-static.a', 'liballegro_static.a', 'liballegro.a'], required: true },
    { sysrootName: 'liballegro_main.a', candidates: ['liballegro_main-static.a', 'liballegro_main_static.a', 'liballegro_main.a'], required: true },
    { sysrootName: 'liballegro_image.a', candidates: ['liballegro_image-static.a', 'liballegro_image_static.a', 'liballegro_image.a'], required: true },
    { sysrootName: 'liballegro_primitives.a', candidates: ['liballegro_primitives-static.a', 'liballegro_primitives_static.a', 'liballegro_primitives.a'], required: true },
    { sysrootName: 'liballegro_font.a', candidates: ['liballegro_font-static.a', 'liballegro_font_static.a', 'liballegro_font.a'], required: true },
    { sysrootName: 'liballegro_ttf.a', candidates: ['liballegro_ttf-static.a', 'liballegro_ttf_static.a', 'liballegro_ttf.a'], required: false },
    { sysrootName: 'liballegro_audio.a', candidates: ['liballegro_audio-static.a', 'liballegro_audio_static.a', 'liballegro_audio.a'], required: true },
    { sysrootName: 'liballegro_acodec.a', candidates: ['liballegro_acodec-static.a', 'liballegro_acodec_static.a', 'liballegro_acodec.a'], required: true },
    { sysrootName: 'liballegro_color.a', candidates: ['liballegro_color-static.a', 'liballegro_color_static.a', 'liballegro_color.a'], required: true },
    { sysrootName: 'liballegro_memfile.a', candidates: ['liballegro_memfile-static.a', 'liballegro_memfile_static.a', 'liballegro_memfile.a'], required: false },
];

const searchRoots = [
    path.join(ALLEGRO_BUILD, 'lib'),
    path.join(ALLEGRO_BUILD, 'lib', 'wasm32-emscripten'),
    ALLEGRO_BUILD,
];

function locateArchive(candidates: readonly string[]): string | undefined {
    for (const root of searchRoots) {
        for (const c of candidates) {
            const p = path.join(root, c);
            if (fs.existsSync(p)) return p;
        }
    }
    const recursiveEntries = fs.readdirSync(ALLEGRO_BUILD, { recursive: true });
    const match = recursiveEntries.find((entry) => candidates.includes(path.basename(entry)));
    return match ? path.join(ALLEGRO_BUILD, match) : undefined;
}

for (const a of archives) {
    const found = locateArchive(a.candidates);
    if (!found) {
        if (a.required) {
            throw new Error(`${a.sysrootName} not found (looked for ${a.candidates.join(', ')} under ${ALLEGRO_BUILD})`);
        }
        console.warn(`  ${a.sysrootName}: not built (candidates: ${a.candidates.join(', ')})`);
        continue;
    }
    shell.cp('-f', found, path.join(SYSROOT_LIB, a.sysrootName));
    console.log(`Deployed ${a.sysrootName} from ${path.relative(ROOT, found)}`);
}

// ── Deploy headers ────────────────────────────────────────────────
// Allegro splits headers between include/ (core) and addons/<name>/.

function copyHeaderTree(srcDir: string): void {
    if (!fs.existsSync(srcDir)) return;
    // shelljs `cp -R src/. dst/` does not always behave like POSIX cp; iterate
    // the top-level entries and recurse explicitly.
    for (const entry of fs.readdirSync(srcDir)) {
        shell.cp('-Rf', path.join(srcDir, entry), SYSROOT_INC);
    }
}

// Core public headers: <srcRoot>/include/allegro5/{*.h,allegro5.h}
const CORE_INC = path.join(ALLEGRO_SRC, 'include');
copyHeaderTree(CORE_INC);

// Addon headers: <srcRoot>/addons/<name>/allegro5/<name>.h
const ADDONS_DIR = path.join(ALLEGRO_SRC, 'addons');
if (fs.existsSync(ADDONS_DIR)) {
    for (const addon of fs.readdirSync(ADDONS_DIR)) {
        const addonHeaderDir = path.join(ADDONS_DIR, addon, 'allegro5');
        if (fs.existsSync(addonHeaderDir)) {
            for (const f of fs.readdirSync(addonHeaderDir, { recursive: true })) {
                const src = path.join(addonHeaderDir, f);
                if (fs.statSync(src).isDirectory()) continue;
                const rel = path.relative(addonHeaderDir, src);
                const dst = path.join(ALLEGRO_INC, rel);
                shell.mkdir('-p', path.dirname(dst));
                shell.cp('-f', src, dst);
            }
        }
    }
}

// Also copy build-generated <allegro5/platform/*.h> (e.g. alplatf.h) from
// the CMake build dir, where they're written under include/allegro5/.
const GEN_INC = path.join(ALLEGRO_BUILD, 'include');
if (fs.existsSync(GEN_INC)) {
    copyHeaderTree(GEN_INC);
}

console.log('Deployed Allegro 5 headers to sysroot/usr/include/allegro5/');

// ── 1b. SDL2 (Allegro's only Emscripten backend) ──────────────────
// Allegro's SDL platform delegates window/GL/input to SDL2. SDL2 is provided
// by emsdk's port system; materialize it (idempotent) and copy libSDL2.a +
// headers into our sysroot so the browser wasm-ld step can resolve symbols.

console.log('Materializing SDL2 (embuilder build sdl2)...');
const EMBUILDER = path.join(EMSDK_DIR, 'upstream', 'emscripten', 'embuilder');
const embuilderResult = shell.exec(`"${EMBUILDER}" build sdl2`, { silent: false, fatal: false });
if (embuilderResult.code !== 0) {
    console.warn('  embuilder build sdl2 failed; relying on already-cached libSDL2.a');
}

const SDL2_CACHED_LIB = path.join(
    EMSDK_DIR, 'upstream', 'emscripten', 'cache', 'sysroot', 'lib', 'wasm32-emscripten', 'libSDL2.a',
);
if (!fs.existsSync(SDL2_CACHED_LIB)) {
    throw new Error(`libSDL2.a not found in emsdk cache at ${SDL2_CACHED_LIB}`);
}
shell.cp('-f', SDL2_CACHED_LIB, path.join(SYSROOT_LIB, 'libSDL2.a'));
console.log('Deployed libSDL2.a to sysroot/usr/lib/');
// NOTE: SDL2 headers are NOT deployed to sysroot. Allegro's public API does not
// expose any SDL2 types — SDL2 is an implementation detail hidden inside the
// compiled static libs. User Allegro code never needs #include <SDL.h>.
// The CC1_ALLEGRO_EXTRA only adds /usr/include/allegro5 to the search path.

// ── 2. Generate allegro-runtime.mjs (MODULARIZE JS factory) ──────
//
// Same rationale as raylib-runtime.mjs: compile a minimal stub linked against
// the Allegro static libs so emcc emits a full MODULARIZE ES6 factory with
// GL infrastructure (createContext, makeContextCurrent, GLctx, RAF MainLoop)
// + emscripten_set_main_loop glue. The generated WASM is retained as the
// glue's ABI anchor and recorded in the release manifest.

const EMSCRIPTEN_DIR = path.join(SYSROOT_LIB, 'emscripten');

const ALLEGRO_STUB_C = path.join(os.tmpdir(), 'allegro_runtime_stub.c');

fs.writeFileSync(
    ALLEGRO_STUB_C,
    `#include <allegro5/allegro.h>
#include <allegro5/allegro_primitives.h>
#include <allegro5/allegro_font.h>
#include <emscripten.h>
#include <stdlib.h>

static ALLEGRO_DISPLAY* display = NULL;

static void loop_iter(void) {
    al_clear_to_color(al_map_rgb(0, 0, 0));
    al_flip_display();
}

int main(void) {
    if (!al_init()) return 1;
    al_init_primitives_addon();
    al_init_font_addon();
    display = al_create_display(640, 480);
    if (!display) return 1;
    emscripten_set_main_loop(loop_iter, 0, 1);
    return 0;
}
`,
);

console.log('Generating allegro-runtime.mjs (MODULARIZE JS factory)...');
const runtimeLibraryPaths = [
    'liballegro_main.a',
    'liballegro_image.a',
    'liballegro_primitives.a',
    'liballegro_font.a',
    'liballegro_audio.a',
    'liballegro_acodec.a',
    'liballegro_color.a',
    'liballegro.a',
].filter((l) => fs.existsSync(path.join(SYSROOT_LIB, l)))
    .map((library) => path.join(SYSROOT_LIB, library));

const runtimePair = buildCanvasRuntimePair({
    compiler: EMCC,
    sourcePath: ALLEGRO_STUB_C,
    libraryPaths: runtimeLibraryPaths,
    includeDirectories: [SYSROOT_INC],
    flags: [
        '-sENVIRONMENT=web',
        '-sALLOW_MEMORY_GROWTH=1',
        // Allegro 5 SDL backend on Emscripten — must use the same SDL2 port
        // that Allegro itself was compiled against.
        '-sUSE_SDL=2',
        '-sMAX_WEBGL_VERSION=2',
        '-sMIN_WEBGL_VERSION=2',
        // Allegro 5 primitives addon uses client-side vertex arrays (passing C
        // pointers directly to glVertexAttribPointer). WebGL requires all vertex
        // data to come from a VBO. FULL_ES2 enables Emscripten's client-side
        // array emulation (libglemu.js) which automatically uploads data to a
        // temporary VBO before each draw call.
        '-sFULL_ES2=1',
        '-sMODULARIZE=1',
        '-sEXPORT_NAME=createAllegroModule',
        '-sEXPORT_ES6=1',
        '-sNO_EXIT_RUNTIME=1',
        '-sEXPORTED_FUNCTIONS=_main,_malloc,_free',
        '-sEXPORTED_RUNTIME_METHODS=ccall,cwrap,getValue,setValue,UTF8ToString,stringToUTF8,lengthBytesUTF8',
        '-O2',
    ],
    outputDirectory: EMSCRIPTEN_DIR,
    runtimeName: 'allegro-runtime',
});
const mjsSize = (fs.statSync(runtimePair.gluePath).size / 1024).toFixed(1);
console.log(`Saved allegro runtime pair (${mjsSize} KB glue) → ${path.relative(ROOT, EMSCRIPTEN_DIR)}`);

console.log('>>> Allegro 5 build complete.');
