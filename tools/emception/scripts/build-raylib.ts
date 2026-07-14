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
 * Versions: latest GitHub release tags (override via *_VERSION env vars).
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import shell from 'shelljs';
import { getEmsdkDir, setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { PINNED } from './lib/pinned-versions.ts';

enableBuildKeepalive('build-raylib');

const ROOT = process.cwd();
shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || PINNED.EMSDK_VERSION;
setupEmsdk(EMSDK_VERSION);

const EMSDK_DIR = getEmsdkDir();
const EMCC = path.join(EMSDK_DIR, 'upstream', 'emscripten', 'emcc');

const USERLAND_DIR = path.join(ROOT, 'userland', 'raylib');
const BUILD_DIR = path.join(ROOT, 'build', 'raylib');
const SYSROOT_LIB = path.join(ROOT, 'sysroot', 'usr', 'lib');
const SYSROOT_INC = path.join(ROOT, 'sysroot', 'usr', 'include');
const RAYLIB_INC = path.join(SYSROOT_INC, 'raylib');
const CONCURRENCY = os.cpus().length;

shell.mkdir('-p', USERLAND_DIR);
shell.mkdir('-p', BUILD_DIR);
shell.mkdir('-p', SYSROOT_LIB);
shell.mkdir('-p', RAYLIB_INC);

// ─────────────── version detection ───────────────

function curlJson(url: string): any {
    const exec = (extra = '') =>
        shell.exec(`curl -fsSL ${extra} "${url}"`, { silent: true, fatal: false });
    let res = process.env.GITHUB_TOKEN
        ? exec(`-H "Authorization: Bearer ${process.env.GITHUB_TOKEN}"`)
        : exec();
    if (res.code !== 0 && process.env.GITHUB_TOKEN) {
        console.warn(`  Authenticated GitHub call failed for ${url}, retrying without token...`);
        res = exec();
    }
    if (res.code !== 0) throw new Error(`Failed to GET ${url}`);
    return JSON.parse(res.stdout);
}

function detectLatestTag(repo: string, envVar: string, fallback: string): string {
    const env = process.env[envVar];
    if (env) return env;
    console.log(`Detecting latest release for ${repo}...`);
    let tag: string | undefined;
    try {
        tag = curlJson(`https://api.github.com/repos/${repo}/releases/latest`).tag_name;
    } catch {
        // GitHub API unavailable
    }
    if (tag) {
        console.log(`  ${repo} latest: ${tag}`);
        return tag;
    }
    console.warn(`  GitHub API unavailable for ${repo}, using pinned ${fallback}`);
    return fallback;
}

function detectLatestDefaultBranchSha(repo: string, envVar: string, fallback: string): string {
    // Fallback for repos without releases (raygui/physac/rlights publish via tags or only default branch).
    const env = process.env[envVar];
    if (env) return env;
    console.log(`Detecting latest tag/branch for ${repo}...`);
    // Try latest release first
    const releasesRes = shell.exec(
        `curl -fsSL "https://api.github.com/repos/${repo}/releases/latest"`,
        { silent: true, fatal: false },
    );
    if (releasesRes.code === 0) {
        try {
            const tag = JSON.parse(releasesRes.stdout).tag_name;
            if (tag) {
                console.log(`  ${repo} latest release: ${tag}`);
                return tag;
            }
        } catch {
            // fall through
        }
    }
    // GitHub API unavailable — use pinned fallback
    console.warn(`  GitHub API unavailable for ${repo}, using pinned ${fallback}`);
    return fallback;
}

// ─────────────── source download ───────────────

function downloadTarball(repo: string, tag: string, destName: string, keyFile = 'CMakeLists.txt'): string {
    const destDir = path.join(USERLAND_DIR, destName);
    const isSourceValid = fs.existsSync(path.join(destDir, keyFile));
    if (isSourceValid) {
        console.log(`Using existing source: ${destName}`);
        return destDir;
    }
    if (fs.existsSync(destDir)) {
        console.log(`Removing incomplete source dir: ${destName}`);
        shell.rm('-rf', destDir);
    }
    shell.mkdir('-p', destDir);
    const tarball = path.join(USERLAND_DIR, `${destName}.tar.gz`);
    console.log(`Downloading ${repo} @ ${tag}...`);
    // Try refs/tags first, then refs/heads (for branch fallback).
    const tryUrls = [
        `https://github.com/${repo}/archive/refs/tags/${tag}.tar.gz`,
        `https://github.com/${repo}/archive/refs/heads/${tag}.tar.gz`,
    ];
    let ok = false;
    for (const url of tryUrls) {
        const res = shell.exec(`curl -fSL -o "${tarball}" "${url}"`, { silent: true, fatal: false });
        if (res.code === 0) {
            ok = true;
            break;
        }
    }
    if (!ok) throw new Error(`Failed to download ${repo} @ ${tag}`);
    shell.exec(`tar xzf "${tarball}" --strip-components=1 -C "${destDir}"`);
    shell.rm('-f', tarball);
    return destDir;
}

// ─────────────── 1. raylib via CMake ───────────────

const RAYLIB_TAG = detectLatestTag('raysan5/raylib', 'RAYLIB_VERSION', PINNED.RAYLIB_VERSION);
const RAYLIB_SRC = downloadTarball('raysan5/raylib', RAYLIB_TAG, `raylib-${RAYLIB_TAG}`);

const RAYLIB_BUILD = path.join(BUILD_DIR, 'raylib-build');
if (fs.existsSync(RAYLIB_BUILD)) shell.rm('-rf', RAYLIB_BUILD);
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
shell.exec(`emmake make -C "${RAYLIB_BUILD}" -j${CONCURRENCY} raylib`);

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
// GLctx, RAF MainLoop) that the IDE needs to run user WASM at runtime.
//
// The stub itself does nothing at runtime — the IDE patches it in-browser:
//   - GLFW stubs → real GL.createContext / GL.makeContextCurrent calls
//   - wasmImports extended with emscripten_webgl_* entries

const EMSCRIPTEN_DIR = path.join(ROOT, 'sysroot', 'usr', 'lib', 'emscripten');
shell.mkdir('-p', EMSCRIPTEN_DIR);

const RAYLIB_STUB_C = path.join(os.tmpdir(), 'raylib_runtime_stub.c');
const TMP_RAYLIB_JS = path.join(os.tmpdir(), 'raylib-runtime.js');
const TMP_RAYLIB_WASM = path.join(os.tmpdir(), 'raylib-runtime.wasm');

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
const runtimeResult = shell.exec(
    [
        `"${EMCC}"`,
        `"${RAYLIB_STUB_C}"`,
        `"${path.join(SYSROOT_LIB, 'libraylib.a')}"`,
        `-I"${RAYLIB_INC}"`,
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
        `-o "${TMP_RAYLIB_JS}"`,
    ].join(' '),
    { silent: false },
);

if (runtimeResult.code !== 0) {
    console.error('emcc raylib-runtime.mjs generation failed');
    process.exit(1);
}

const OUTPUT_MJS = path.join(EMSCRIPTEN_DIR, 'raylib-runtime.mjs');
fs.copyFileSync(TMP_RAYLIB_JS, OUTPUT_MJS);
const mjsSize = (fs.statSync(OUTPUT_MJS).size / 1024).toFixed(1);
console.log(`Saved raylib-runtime.mjs (${mjsSize} KB) → ${path.relative(ROOT, OUTPUT_MJS)}`);

if (fs.existsSync(TMP_RAYLIB_WASM)) fs.rmSync(TMP_RAYLIB_WASM);

// ─────────────── 2. companion libs ───────────────

interface HeaderOnlyLib {
    libName: string;        // libNAME.a
    headerName: string;     // raygui.h
    implMacro: string;      // RAYGUI_IMPLEMENTATION
    prelude?: string;       // extra source emitted before #include of the header
    extraHeaders?: string[];
    // Source = remote tarball OR local path inside an existing source tree.
    repo?: string;
    envVar?: string;
    fallback?: string;      // pinned version used when GitHub API is unavailable
    headerSubpath?: string;
    localHeader?: string;   // absolute path to header on disk (skips download)
}

const COMPANIONS: HeaderOnlyLib[] = [
    {
        repo: 'raysan5/raygui',
        envVar: 'RAYGUI_VERSION',
        fallback: PINNED.RAYGUI_VERSION,
        libName: 'raygui',
        headerName: 'raygui.h',
        implMacro: 'RAYGUI_IMPLEMENTATION',
        headerSubpath: 'src/raygui.h',
    },
    {
        repo: 'victorfisac/Physac',
        envVar: 'PHYSAC_VERSION',
        fallback: PINNED.PHYSAC_VERSION,
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
        if (!lib.repo || !lib.envVar) {
            console.warn(`  ${lib.libName}: missing repo/envVar, skipping`);
            continue;
        }
        const tag = detectLatestDefaultBranchSha(lib.repo, lib.envVar, lib.fallback ?? 'master');
        const keyFile = lib.headerSubpath || lib.headerName;
        let srcDir: string;
        try {
            srcDir = downloadTarball(lib.repo, tag, `${lib.libName}-${tag}`, keyFile);
        } catch (e) {
            console.warn(`  ${lib.repo}: download failed, skipping (${(e as Error).message})`);
            continue;
        }
        headerPath = lib.headerSubpath
            ? path.join(srcDir, lib.headerSubpath)
            : path.join(srcDir, lib.headerName);
        if (!fs.existsSync(headerPath)) {
            console.warn(`  ${lib.repo}: header ${headerPath} not found, skipping`);
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
