/**
 * Build raylib + raygui + physac + rlights as static libraries for Emscripten.
 *
 * Strategy:
 *   - raylib: emcmake + cmake -DPLATFORM=Web (raylib's official Web build path)
 *   - raygui / physac / rlights: header-only single-file libs; compile a tiny
 *     wrapper TU that defines *_IMPLEMENTATION before including the header.
 *
 * Outputs:
 *   - sysroot/usr/lib/libraylib.a
 *   - sysroot/usr/lib/libraygui.a
 *   - sysroot/usr/lib/libphysac.a
 *   - sysroot/usr/lib/librlights.a
 *   - sysroot/usr/include/raylib/{raylib,raymath,rlgl,raygui,physac,rlights}.h
 *   - sysroot/usr/lib/emscripten/raylib-runtime.mjs  (MODULARIZE=1 JS factory)
 *
 * Versions: exact entries from toolchain.lock.json.
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import { toolchainPaths } from './toolchain/paths.ts';
import shell from 'shelljs';
import { buildCanvasRuntimePair } from './lib/canvas-runtime-build.ts';
import { getEmsdkDir, setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { loadToolchainStateSync, lockedVersion } from './toolchain/config.ts';
import type { ToolName } from './toolchain/lock.ts';
import { ensureLockedSource } from './toolchain/sources.ts';

enableBuildKeepalive('build-raylib');

const ROOT = process.cwd();
const P = toolchainPaths(ROOT);
shell.config.fatal = true;

const { lock } = loadToolchainStateSync(ROOT);
const EMSDK_VERSION = lockedVersion(lock, 'emsdk');
setupEmsdk(EMSDK_VERSION);

const EMSDK_DIR = getEmsdkDir();
const EMCC = path.join(EMSDK_DIR, 'upstream', 'emscripten', 'emcc');

const SOURCE_ROOT = path.join(P.sources, 'raylib');
const BUILD_DIR = path.join(P.builds, 'raylib');
const SYSROOT_LIB = path.join(P.sysroot, 'usr', 'lib');
const SYSROOT_INC = path.join(P.sysroot, 'usr', 'include');
const RAYLIB_INC = path.join(SYSROOT_INC, 'raylib');
const CONCURRENCY = os.cpus().length;

shell.mkdir('-p', SOURCE_ROOT);
shell.mkdir('-p', BUILD_DIR);
shell.mkdir('-p', SYSROOT_LIB);
shell.mkdir('-p', RAYLIB_INC);

// ─────────────── 1. raylib via CMake ───────────────

const RAYLIB_TAG = lockedVersion(lock, 'raylib');
const RAYLIB_SRC = ensureLockedSource(
    ROOT,
    lock,
    'raylib',
    path.join(SOURCE_ROOT, `raylib-${RAYLIB_TAG}`),
    'CMakeLists.txt',
);

const RAYLIB_BUILD = path.join(BUILD_DIR, 'raylib-build');
shell.mkdir('-p', RAYLIB_BUILD);

console.log('Configuring raylib (PLATFORM=Web)...');
const raylibCmakeCmd = [
    'emcmake cmake',
    `-S "${RAYLIB_SRC}"`,
    `-B "${RAYLIB_BUILD}"`,
    '-DCMAKE_BUILD_TYPE=Release',
    '-DPLATFORM=Web',
    '-DBUILD_EXAMPLES=OFF',
    '-DBUILD_GAMES=OFF',
    '-DSUPPORT_GIF_RECORDING=OFF',
    '-DGRAPHICS=GRAPHICS_API_OPENGL_ES3',
    '-DBUILD_SHARED_LIBS=OFF',
].join(' ');
console.log(raylibCmakeCmd);
shell.exec(raylibCmakeCmd);

console.log('Building raylib...');
shell.exec(`cmake --build "${RAYLIB_BUILD}" --parallel ${CONCURRENCY} --target raylib`);

// raylib's CMake puts libraylib.a under raylib/ subdir
const candidates = [
    path.join(RAYLIB_BUILD, 'raylib', 'libraylib.a'),
    path.join(RAYLIB_BUILD, 'libraylib.a'),
];
const raylibArchive = candidates.find((c) => fs.existsSync(c));
if (!raylibArchive) {
    throw new Error(`libraylib.a not found in ${RAYLIB_BUILD} (checked: ${candidates.join(', ')})`);
}
shell.cp('-f', raylibArchive, path.join(SYSROOT_LIB, 'libraylib.a'));
console.log(`Deployed libraylib.a from ${raylibArchive}`);

// Copy raylib headers
const RAYLIB_SRC_DIR = path.join(RAYLIB_SRC, 'src');
for (const h of ['raylib.h', 'raymath.h', 'rlgl.h', 'rcamera.h', 'rgestures.h']) {
    const hp = path.join(RAYLIB_SRC_DIR, h);
    if (fs.existsSync(hp)) shell.cp('-f', hp, path.join(RAYLIB_INC, '/'));
}
console.log('Deployed raylib headers to sysroot/usr/include/raylib/');

// ── Step 1b: generate raylib-runtime.mjs ─────────────────────────────────────
//
// Compile a minimal stub with emcc -sMODULARIZE=1 linking libraylib.a.
// Because libraylib.a references _emscripten_gl*, malloc/free, and
// emscripten_set_main_loop, emcc emits a full MODULARIZE JS factory that
// includes all GL infrastructure (GL.createContext, GL.makeContextCurrent,
// GLctx, RAF MainLoop) used by the browser canvas API. The generated WASM is
// retained as the glue's ABI anchor and recorded in the release manifest.

const EMSCRIPTEN_DIR = path.join(P.sysroot, 'usr', 'lib', 'emscripten');
shell.mkdir('-p', EMSCRIPTEN_DIR);

const RAYLIB_STUB_C = path.join(os.tmpdir(), 'raylib_runtime_stub.c');
fs.writeFileSync(
    RAYLIB_STUB_C,
    `#include <raylib.h>
#include <emscripten.h>
#include <stdlib.h>

static void loop_iter(void) {
    BeginDrawing();
    ClearBackground((Color){ 0, 0, 0, 255 });
    DrawText("raylib-runtime", 10, 10, 20, (Color){ 255, 255, 255, 255 });
    EndDrawing();
}

int main(void) {
    InitWindow(640, 480, "raylib-runtime");
    emscripten_set_main_loop(loop_iter, 0, 1);
    CloseWindow();
    return 0;
}
`,
);

console.log('Generating raylib-runtime.mjs (MODULARIZE JS factory)...');
const runtimePair = buildCanvasRuntimePair({
    compiler: EMCC,
    sourcePath: RAYLIB_STUB_C,
    libraryPaths: [path.join(SYSROOT_LIB, 'libraylib.a')],
    includeDirectories: [RAYLIB_INC],
    flags: [
        '-sENVIRONMENT=web',
        '-sALLOW_MEMORY_GROWTH=1',
        '-sUSE_GLFW=3',
        '-sMAX_WEBGL_VERSION=2',
        '-sMIN_WEBGL_VERSION=2',
        '-sMODULARIZE=1',
        '-sEXPORT_NAME=createRaylibModule',
        '-sEXPORT_ES6=1',
        '-sNO_EXIT_RUNTIME=1',
        '-sEXPORTED_FUNCTIONS=_main,_malloc,_free',
        '-sEXPORTED_RUNTIME_METHODS=ccall,cwrap,getValue,setValue,UTF8ToString,stringToUTF8,lengthBytesUTF8',
        '-O2',
    ],
    outputDirectory: EMSCRIPTEN_DIR,
    runtimeName: 'raylib-runtime',
});
const mjsSize = (fs.statSync(runtimePair.gluePath).size / 1024).toFixed(1);
console.log(`Saved raylib runtime pair (${mjsSize} KB glue) → ${path.relative(ROOT, EMSCRIPTEN_DIR)}`);

// ─────────────── 2. companion libs ───────────────

type HeaderOnlyLib = {
    readonly libName: string;        // libNAME.a
    readonly headerName: string;     // raygui.h
    readonly implMacro: string;      // RAYGUI_IMPLEMENTATION
    readonly prelude?: string;       // extra source emitted before #include of the header
    readonly extraHeaders?: readonly string[];
    // Source = remote tarball OR local path inside an existing source tree.
    readonly tool?: ToolName;
    readonly headerSubpath?: string;
    readonly localHeader?: string;   // absolute path to header on disk (skips download)
};

const COMPANIONS: readonly HeaderOnlyLib[] = [
    {
        tool: 'raygui',
        libName: 'raygui',
        headerName: 'raygui.h',
        implMacro: 'RAYGUI_IMPLEMENTATION',
        headerSubpath: 'src/raygui.h',
    },
    {
        tool: 'physac',
        libName: 'physac',
        headerName: 'physac.h',
        implMacro: 'PHYSAC_IMPLEMENTATION',
        headerSubpath: 'src/physac.h',
        // physac.h references raylib's Vector2 + C99 bool but does not include them itself.
        // PHYSAC_NO_THREADS: avoid pthread dependency (browser/emscripten); user calls PhysicsThread() manually or per frame.
        prelude: '#include <stdbool.h>\n#include <raylib.h>\n#define PHYSAC_NO_THREADS\n',
    },
    {
        // rlights ships inside raylib's examples (no standalone repo).
        libName: 'rlights',
        headerName: 'rlights.h',
        implMacro: 'RLIGHTS_IMPLEMENTATION',
        localHeader: path.join(RAYLIB_SRC, 'examples', 'shaders', 'rlights.h'),
        // rlights references raylib types (Shader, Vector3, Color, ...).
        prelude: '#include <raylib.h>\n',
    },
];

// Common emcc flags so headers (including raylib.h) resolve correctly.
const COMPANION_CFLAGS = [
    '-Os',
    `-I"${RAYLIB_INC}"`,
    `-I"${SYSROOT_INC}"`,
].join(' ');

for (const lib of COMPANIONS) {
    let headerPath: string;
    if (lib.localHeader) {
        if (!fs.existsSync(lib.localHeader)) {
            console.warn(`  ${lib.libName}: local header ${lib.localHeader} not found, skipping`);
            continue;
        }
        headerPath = lib.localHeader;
    } else {
        if (!lib.tool) {
            console.warn(`  ${lib.libName}: missing tool identity, skipping`);
            continue;
        }
        const tag = lockedVersion(lock, lib.tool);
        const keyFile = lib.headerSubpath || lib.headerName;
        const srcDir = ensureLockedSource(
            ROOT,
            lock,
            lib.tool,
            path.join(SOURCE_ROOT, `${lib.libName}-${tag}`),
            keyFile,
        );
        headerPath = lib.headerSubpath
            ? path.join(srcDir, lib.headerSubpath)
            : path.join(srcDir, lib.headerName);
        if (!fs.existsSync(headerPath)) {
            console.warn(`  ${lib.tool}: header ${headerPath} not found, skipping`);
            continue;
        }
    }

    // Wrapper TU
    const wrapperPath = path.join(BUILD_DIR, `${lib.libName}-impl.c`);
    fs.writeFileSync(
        wrapperPath,
        `/* Auto-generated implementation TU for ${lib.libName} */
${lib.prelude ?? ''}#define ${lib.implMacro}
#include "${headerPath}"
`,
    );

    const objPath = path.join(BUILD_DIR, `${lib.libName}-impl.o`);
    console.log(`Compiling ${lib.libName} implementation...`);
    shell.exec(`emcc ${COMPANION_CFLAGS} -c "${wrapperPath}" -o "${objPath}"`);

    const archivePath = path.join(BUILD_DIR, `lib${lib.libName}.a`);
    shell.exec(`emar rcs "${archivePath}" "${objPath}"`);
    shell.cp('-f', archivePath, path.join(SYSROOT_LIB, `lib${lib.libName}.a`));

    // Deploy header
    shell.cp('-f', headerPath, path.join(RAYLIB_INC, lib.headerName));
    console.log(`Deployed lib${lib.libName}.a + ${lib.headerName}`);
}

console.log('>>> raylib + companions build complete.');
